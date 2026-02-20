using Aster.Compiler.Frontend.Hir;
using Aster.Compiler.Frontend.TypeSystem;

namespace Aster.Compiler.MiddleEnd.Generics;

/// <summary>
/// Records a single instantiation of a generic function or struct.
/// For example, calling <c>identity(42)</c> produces an instantiation
/// of <c>identity</c> with type argument <c>i32</c>.
/// </summary>
public sealed class GenericInstantiation
{
    /// <summary>Original (unmangled) function or type name.</summary>
    public string OriginalName { get; }

    /// <summary>Concrete type arguments substituted for each type parameter.</summary>
    public IReadOnlyList<AsterType> TypeArguments { get; }

    /// <summary>Name-mangled identifier for the specialised version.</summary>
    public string MangledName { get; }

    public GenericInstantiation(string originalName, IReadOnlyList<AsterType> typeArguments, string mangledName)
    {
        OriginalName = originalName;
        TypeArguments = typeArguments;
        MangledName = mangledName;
    }

    public override string ToString() => $"{OriginalName}<{string.Join(", ", TypeArguments.Select(t => t.DisplayName))}> → {MangledName}";
}

/// <summary>
/// Table of all generic instantiations discovered during compilation.
/// Maps each unique (name, type-args) pair to its mangled name.
/// </summary>
public sealed class MonomorphizationTable
{
    private readonly List<GenericInstantiation> _instantiations = new();

    /// <summary>All recorded instantiations.</summary>
    public IReadOnlyList<GenericInstantiation> Instantiations => _instantiations;

    /// <summary>
    /// Record a new instantiation. Returns the mangled name.
    /// If the same instantiation was already recorded, returns the existing mangled name.
    /// </summary>
    public string Record(string originalName, IReadOnlyList<AsterType> typeArguments)
    {
        // Look for an existing identical instantiation
        foreach (var existing in _instantiations)
        {
            if (existing.OriginalName == originalName &&
                existing.TypeArguments.Count == typeArguments.Count &&
                existing.TypeArguments.Zip(typeArguments).All(pair => pair.First.DisplayName == pair.Second.DisplayName))
            {
                return existing.MangledName;
            }
        }

        var mangled = Mangle(originalName, typeArguments);
        _instantiations.Add(new GenericInstantiation(originalName, typeArguments, mangled));
        return mangled;
    }

    /// <summary>
    /// Produce a name-mangled identifier for a generic instantiation.
    /// <example>
    /// <c>identity</c> + <c>[i32]</c>  → <c>identity_i32</c>
    /// <c>first</c>    + <c>[i32, String]</c> → <c>first_i32_String</c>
    /// </example>
    /// </summary>
    public static string Mangle(string name, IReadOnlyList<AsterType> typeArguments)
    {
        if (typeArguments.Count == 0) return name;
        var suffix = string.Join("_", typeArguments.Select(t => MangleType(t)));
        return $"{name}_{suffix}";
    }

    private static string MangleType(AsterType type) => type switch
    {
        // String is a special-cased primitive so its Kind is "String" → would mangle to "string"
        // but PrimitiveKind.String.ToString() is "String" not "StringType".
        // Handle explicitly to be consistent with user-visible type names.
        PrimitiveType { Kind: PrimitiveKind.String } => "string",
        PrimitiveType pt => pt.Kind.ToString().ToLowerInvariant(),
        StructType st => st.Name,
        EnumType et => et.Name,
        GenericParameter gp => gp.Name,
        TypeApp ta => $"{MangleType(ta.Constructor)}_{string.Join("_", ta.Arguments.Select(MangleType))}",
        TypeVariable tv => $"T{tv.Id}",
        _ => "unknown",
    };
}

/// <summary>
/// Walks an HIR program to collect all instantiations of generic functions and structs.
/// Produces a <see cref="MonomorphizationTable"/> that maps each unique instantiation
/// to a name-mangled identifier (e.g. <c>identity_i32</c>).
///
/// Phase 2 Week 11 implementation.
/// Full monomorphization (code duplication) is deferred to the codegen phase;
/// this pass only records the table so later phases can use it.
/// </summary>
public sealed class Monomorphizer
{
    private readonly MonomorphizationTable _table = new();

    /// <summary>The collected instantiation table after <see cref="Run"/> is called.</summary>
    public MonomorphizationTable Table => _table;

    /// <summary>Walk the HIR program and collect all generic instantiations.</summary>
    public MonomorphizationTable Run(HirProgram program)
    {
        foreach (var decl in program.Declarations)
            VisitNode(decl);
        return _table;
    }

    private void VisitNode(HirNode node)
    {
        switch (node)
        {
            case HirFunctionDecl fn:
                VisitBlock(fn.Body);
                break;
            case HirBlock block:
                VisitBlock(block);
                break;
            case HirLetStmt let:
                if (let.Initializer != null) VisitNode(let.Initializer);
                break;
            case HirExprStmt es:
                VisitNode(es.Expression);
                break;
            case HirReturnStmt ret:
                if (ret.Value != null) VisitNode(ret.Value);
                break;
            case HirCallExpr call:
                VisitCallExpr(call);
                break;
            case HirBinaryExpr bin:
                VisitNode(bin.Left);
                VisitNode(bin.Right);
                break;
            case HirUnaryExpr un:
                VisitNode(un.Operand);
                break;
            case HirIfExpr ifExpr:
                VisitNode(ifExpr.Condition);
                VisitNode(ifExpr.ThenBranch);
                if (ifExpr.ElseBranch != null) VisitNode(ifExpr.ElseBranch);
                break;
            case HirWhileStmt ws:
                VisitNode(ws.Condition);
                VisitNode(ws.Body);
                break;
            case HirAssignExpr assign:
                VisitNode(assign.Target);
                VisitNode(assign.Value);
                break;
            case HirMemberAccessExpr ma:
                VisitNode(ma.Object);
                break;
            case HirStructInitExpr si:
                foreach (var f in si.Fields) VisitNode(f.Value);
                break;
        }
    }

    private void VisitBlock(HirBlock block)
    {
        foreach (var stmt in block.Statements)
            VisitNode(stmt);
        if (block.TailExpression != null)
            VisitNode(block.TailExpression);
    }

    private void VisitCallExpr(HirCallExpr call)
    {
        // Visit arguments first
        foreach (var arg in call.Arguments)
            VisitNode(arg);

        // Check if we're calling a generic function
        if (call.Callee is not HirIdentifierExpr id)
            return;

        var symbol = id.ResolvedSymbol;
        if (symbol?.Type is not FunctionType fnType)
            return;

        // Collect concrete argument types
        var argTypes = call.Arguments
            .Select(a => ResolveArgumentType(a))
            .ToList();

        // Only record if there are generic parameters in the function type
        if (!HasGenericParams(fnType))
            return;

        _table.Record(symbol.Name, argTypes);
    }

    /// <summary>
    /// Extract the resolved type of an argument node.
    /// Uses the symbol's type when available; falls back to inferred primitive types.
    /// </summary>
    private static AsterType ResolveArgumentType(HirNode arg) => arg switch
    {
        HirLiteralExpr lit => lit.LiteralKind switch
        {
            Frontend.Ast.LiteralKind.Integer => PrimitiveType.I32,
            Frontend.Ast.LiteralKind.Float => PrimitiveType.F64,
            Frontend.Ast.LiteralKind.String => PrimitiveType.StringType,
            Frontend.Ast.LiteralKind.Char => PrimitiveType.Char,
            Frontend.Ast.LiteralKind.Bool => PrimitiveType.Bool,
            _ => new TypeVariable(),
        },
        HirIdentifierExpr id => id.ResolvedSymbol?.Type ?? new TypeVariable(),
        _ => new TypeVariable(),
    };

    private static bool HasGenericParams(FunctionType fnType) =>
        fnType.ParameterTypes.Any(p => p is GenericParameter) ||
        fnType.ReturnType is GenericParameter;
}

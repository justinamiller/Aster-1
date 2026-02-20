using Aster.Compiler.Diagnostics;

namespace Aster.Compiler.Frontend.Hir;

/// <summary>
/// High-level IR node base class. Similar to AST but with resolved names
/// and symbol references. Produced by name resolution.
/// </summary>
public abstract class HirNode
{
    public Span Span { get; }
    public int Id { get; }

    private static int _nextId;

    protected HirNode(Span span)
    {
        Span = span;
        Id = Interlocked.Increment(ref _nextId);
    }
}

/// <summary>HIR program root.</summary>
public sealed class HirProgram : HirNode
{
    public IReadOnlyList<HirNode> Declarations { get; }
    public HirProgram(IReadOnlyList<HirNode> declarations, Span span) : base(span) => Declarations = declarations;
}

/// <summary>
/// A resolved generic type parameter in HIR (e.g. the T in fn foo&lt;T: Clone&gt;).
/// Carries the parameter name and the names of any trait bounds.
/// </summary>
public sealed class HirGenericParam
{
    public string Name { get; }
    /// <summary>Trait-bound names (e.g. ["Clone", "Debug"]).</summary>
    public IReadOnlyList<string> Bounds { get; }
    public HirGenericParam(string name, IReadOnlyList<string> bounds)
    { Name = name; Bounds = bounds; }
}

/// <summary>HIR function declaration with resolved symbol.</summary>
public sealed class HirFunctionDecl : HirNode
{
    public Symbol Symbol { get; }
    public IReadOnlyList<HirGenericParam> GenericParams { get; }
    public IReadOnlyList<HirParameter> Parameters { get; }
    public HirBlock Body { get; }
    public HirTypeRef? ReturnType { get; }
    public bool IsAsync { get; }

    public HirFunctionDecl(Symbol symbol, IReadOnlyList<HirGenericParam> genericParams, IReadOnlyList<HirParameter> parameters, HirBlock body, HirTypeRef? returnType, bool isAsync, Span span)
        : base(span) { Symbol = symbol; GenericParams = genericParams; Parameters = parameters; Body = body; ReturnType = returnType; IsAsync = isAsync; }
}

/// <summary>HIR parameter.</summary>
public sealed class HirParameter : HirNode
{
    public Symbol Symbol { get; }
    public HirTypeRef? TypeRef { get; }
    public bool IsMutable { get; }
    public HirParameter(Symbol symbol, HirTypeRef? typeRef, bool isMutable, Span span) : base(span)
    { Symbol = symbol; TypeRef = typeRef; IsMutable = isMutable; }
}

/// <summary>HIR type reference (resolved).</summary>
public sealed class HirTypeRef : HirNode
{
    public Symbol? ResolvedSymbol { get; }
    public string Name { get; }
    public IReadOnlyList<HirTypeRef> TypeArguments { get; }
    public HirTypeRef(string name, Symbol? resolved, IReadOnlyList<HirTypeRef> typeArguments, Span span)
        : base(span) { Name = name; ResolvedSymbol = resolved; TypeArguments = typeArguments; }
}

/// <summary>HIR block.</summary>
public sealed class HirBlock : HirNode
{
    public IReadOnlyList<HirNode> Statements { get; }
    public HirNode? TailExpression { get; }
    public HirBlock(IReadOnlyList<HirNode> statements, HirNode? tailExpression, Span span)
        : base(span) { Statements = statements; TailExpression = tailExpression; }
}

/// <summary>HIR let statement.</summary>
public sealed class HirLetStmt : HirNode
{
    public Symbol Symbol { get; }
    public bool IsMutable { get; }
    public HirTypeRef? TypeRef { get; }
    public HirNode? Initializer { get; }
    public HirLetStmt(Symbol symbol, bool isMutable, HirTypeRef? typeRef, HirNode? initializer, Span span)
        : base(span) { Symbol = symbol; IsMutable = isMutable; TypeRef = typeRef; Initializer = initializer; }
}

/// <summary>HIR expression statement.</summary>
public sealed class HirExprStmt : HirNode
{
    public HirNode Expression { get; }
    public HirExprStmt(HirNode expression, Span span) : base(span) => Expression = expression;
}

/// <summary>HIR return statement.</summary>
public sealed class HirReturnStmt : HirNode
{
    public HirNode? Value { get; }
    public HirReturnStmt(HirNode? value, Span span) : base(span) => Value = value;
}

/// <summary>HIR call expression.</summary>
public sealed class HirCallExpr : HirNode
{
    public HirNode Callee { get; }
    public IReadOnlyList<HirNode> Arguments { get; }
    public HirCallExpr(HirNode callee, IReadOnlyList<HirNode> arguments, Span span)
        : base(span) { Callee = callee; Arguments = arguments; }
}

/// <summary>HIR identifier expression with resolved symbol.</summary>
public sealed class HirIdentifierExpr : HirNode
{
    public Symbol? ResolvedSymbol { get; }
    public string Name { get; }
    public HirIdentifierExpr(string name, Symbol? resolvedSymbol, Span span) : base(span)
    { Name = name; ResolvedSymbol = resolvedSymbol; }
}

/// <summary>HIR path expression for namespaced names.</summary>
public sealed class HirPathExpr : HirNode
{
    public IReadOnlyList<string> Segments { get; }
    public HirPathExpr(IReadOnlyList<string> segments, Span span) : base(span) { Segments = segments; }
}

/// <summary>HIR literal expression.</summary>
public sealed class HirLiteralExpr : HirNode
{
    public object Value { get; }
    public Ast.LiteralKind LiteralKind { get; }
    public HirLiteralExpr(object value, Ast.LiteralKind kind, Span span) : base(span)
    { Value = value; LiteralKind = kind; }
}

/// <summary>HIR binary expression.</summary>
public sealed class HirBinaryExpr : HirNode
{
    public HirNode Left { get; }
    public Ast.BinaryOperator Operator { get; }
    public HirNode Right { get; }
    public HirBinaryExpr(HirNode left, Ast.BinaryOperator op, HirNode right, Span span)
        : base(span) { Left = left; Operator = op; Right = right; }
}

/// <summary>HIR unary expression.</summary>
public sealed class HirUnaryExpr : HirNode
{
    public Ast.UnaryOperator Operator { get; }
    public HirNode Operand { get; }
    public HirUnaryExpr(Ast.UnaryOperator op, HirNode operand, Span span)
        : base(span) { Operator = op; Operand = operand; }
}

/// <summary>HIR if expression.</summary>
public sealed class HirIfExpr : HirNode
{
    public HirNode Condition { get; }
    public HirBlock ThenBranch { get; }
    public HirNode? ElseBranch { get; }
    public HirIfExpr(HirNode condition, HirBlock thenBranch, HirNode? elseBranch, Span span)
        : base(span) { Condition = condition; ThenBranch = thenBranch; ElseBranch = elseBranch; }
}

/// <summary>HIR while statement.</summary>
public sealed class HirWhileStmt : HirNode
{
    public HirNode Condition { get; }
    public HirBlock Body { get; }
    public HirWhileStmt(HirNode condition, HirBlock body, Span span)
        : base(span) { Condition = condition; Body = body; }
}

/// <summary>HIR struct declaration.</summary>
public sealed class HirStructDecl : HirNode
{
    public Symbol Symbol { get; }
    public IReadOnlyList<HirGenericParam> GenericParams { get; }
    public IReadOnlyList<HirFieldDecl> Fields { get; }
    public HirStructDecl(Symbol symbol, IReadOnlyList<HirGenericParam> genericParams, IReadOnlyList<HirFieldDecl> fields, Span span)
        : base(span) { Symbol = symbol; GenericParams = genericParams; Fields = fields; }
}

/// <summary>HIR field declaration.</summary>
public sealed class HirFieldDecl : HirNode
{
    public string Name { get; }
    public HirTypeRef TypeRef { get; }
    public HirFieldDecl(string name, HirTypeRef typeRef, Span span) : base(span) { Name = name; TypeRef = typeRef; }
}

/// <summary>HIR enum declaration.</summary>
public sealed class HirEnumDecl : HirNode
{
    public Symbol Symbol { get; }
    public IReadOnlyList<HirEnumVariant> Variants { get; }
    public HirEnumDecl(Symbol symbol, IReadOnlyList<HirEnumVariant> variants, Span span)
        : base(span) { Symbol = symbol; Variants = variants; }
}

/// <summary>HIR enum variant.</summary>
public sealed class HirEnumVariant : HirNode
{
    public string Name { get; }
    public IReadOnlyList<HirTypeRef> Fields { get; }
    public HirEnumVariant(string name, IReadOnlyList<HirTypeRef> fields, Span span) : base(span)
    { Name = name; Fields = fields; }
}

/// <summary>HIR assign expression.</summary>
public sealed class HirAssignExpr : HirNode
{
    public HirNode Target { get; }
    public HirNode Value { get; }
    public HirAssignExpr(HirNode target, HirNode value, Span span) : base(span) { Target = target; Value = value; }
}

/// <summary>HIR member access.</summary>
public sealed class HirMemberAccessExpr : HirNode
{
    public HirNode Object { get; }
    public string Member { get; }
    public HirMemberAccessExpr(HirNode obj, string member, Span span) : base(span) { Object = obj; Member = member; }
}

/// <summary>HIR struct initialization.</summary>
public sealed class HirStructInitExpr : HirNode
{
    public string StructName { get; }
    public IReadOnlyList<HirFieldInit> Fields { get; }
    public HirStructInitExpr(string structName, IReadOnlyList<HirFieldInit> fields, Span span)
        : base(span) { StructName = structName; Fields = fields; }
}

/// <summary>HIR field initialization.</summary>
public sealed class HirFieldInit : HirNode
{
    public string FieldName { get; }
    public HirNode Value { get; }
    public HirFieldInit(string fieldName, HirNode value, Span span)
        : base(span) { FieldName = fieldName; Value = value; }
}

/// <summary>HIR module declaration (Week 17-19).</summary>
public sealed class HirModuleDecl : HirNode
{
    public Symbol Symbol { get; }
    public IReadOnlyList<HirNode> Members { get; }
    public HirModuleDecl(Symbol symbol, IReadOnlyList<HirNode> members, Span span)
        : base(span) { Symbol = symbol; Members = members; }
}

/// <summary>HIR trait method signature.</summary>
public sealed class HirTraitMethod : HirNode
{
    public string Name { get; }
    public IReadOnlyList<string> ParamTypeNames { get; }
    public string? ReturnTypeName { get; }
    public HirTraitMethod(string name, IReadOnlyList<string> paramTypeNames, string? returnTypeName, Span span)
        : base(span) { Name = name; ParamTypeNames = paramTypeNames; ReturnTypeName = returnTypeName; }
}

/// <summary>HIR trait declaration (Week 20).</summary>
public sealed class HirTraitDecl : HirNode
{
    public Symbol Symbol { get; }
    public IReadOnlyList<HirGenericParam> GenericParams { get; }
    public IReadOnlyList<HirTraitMethod> Methods { get; }
    public HirTraitDecl(Symbol symbol, IReadOnlyList<HirGenericParam> genericParams, IReadOnlyList<HirTraitMethod> methods, Span span)
        : base(span) { Symbol = symbol; GenericParams = genericParams; Methods = methods; }
}

/// <summary>HIR impl block declaration (Week 20).</summary>
public sealed class HirImplDecl : HirNode
{
    /// <summary>The type being implemented (e.g. "Point").</summary>
    public string TargetTypeName { get; }
    /// <summary>Trait being implemented, or null for inherent impls.</summary>
    public string? TraitName { get; }
    public IReadOnlyList<HirFunctionDecl> Methods { get; }
    public HirImplDecl(string targetTypeName, string? traitName, IReadOnlyList<HirFunctionDecl> methods, Span span)
        : base(span) { TargetTypeName = targetTypeName; TraitName = traitName; Methods = methods; }
}

// ========== Phase 2 Closeout: for / match / break / continue / index ==========

/// <summary>Kinds of patterns in match arms.</summary>
public enum PatternKind
{
    Wildcard,    // _
    Variable,    // x (binding)
    Literal,     // 42, "hello", true
    Constructor, // Some(x), Ok(v), EnumVariant(a,b)
}

/// <summary>for variable in iterable { body }</summary>
public sealed class HirForStmt : HirNode
{
    public Symbol Variable { get; }
    public HirNode Iterable { get; }
    public HirBlock Body { get; }
    public HirForStmt(Symbol variable, HirNode iterable, HirBlock body, Span span)
        : base(span) { Variable = variable; Iterable = iterable; Body = body; }
}

/// <summary>break statement inside a loop.</summary>
public sealed class HirBreakStmt : HirNode
{
    public HirBreakStmt(Span span) : base(span) { }
}

/// <summary>continue statement inside a loop.</summary>
public sealed class HirContinueStmt : HirNode
{
    public HirContinueStmt(Span span) : base(span) { }
}

/// <summary>Index expression: target[index]</summary>
public sealed class HirIndexExpr : HirNode
{
    public HirNode Target { get; }
    public HirNode Index { get; }
    public HirIndexExpr(HirNode target, HirNode index, Span span)
        : base(span) { Target = target; Index = index; }
}

/// <summary>Represents a single pattern in a match arm.</summary>
public sealed class HirPattern : HirNode
{
    /// <summary>Wildcard `_`, literal, variable binding, or constructor pattern.</summary>
    public PatternKind Kind { get; }
    /// <summary>Bound variable name (for variable/binding patterns).</summary>
    public string? Name { get; }
    /// <summary>Literal value (for literal patterns).</summary>
    public object? LiteralValue { get; }
    /// <summary>Constructor name (for enum/struct patterns).</summary>
    public string? Constructor { get; }
    /// <summary>Sub-patterns for constructor patterns.</summary>
    public IReadOnlyList<HirPattern> SubPatterns { get; }

    public HirPattern(PatternKind kind, string? name, object? literalValue, string? constructor,
        IReadOnlyList<HirPattern> subPatterns, Span span)
        : base(span)
    {
        Kind = kind; Name = name; LiteralValue = literalValue;
        Constructor = constructor; SubPatterns = subPatterns;
    }
}

/// <summary>A single arm in a match expression.</summary>
public sealed class HirMatchArm : HirNode
{
    public HirPattern Pattern { get; }
    public HirNode Body { get; }
    public HirMatchArm(HirPattern pattern, HirNode body, Span span)
        : base(span) { Pattern = pattern; Body = body; }
}

/// <summary>match scrutinee { arm1, arm2, ... }</summary>
public sealed class HirMatchExpr : HirNode
{
    public HirNode Scrutinee { get; }
    public IReadOnlyList<HirMatchArm> Arms { get; }
    public HirMatchExpr(HirNode scrutinee, IReadOnlyList<HirMatchArm> arms, Span span)
        : base(span) { Scrutinee = scrutinee; Arms = arms; }
}

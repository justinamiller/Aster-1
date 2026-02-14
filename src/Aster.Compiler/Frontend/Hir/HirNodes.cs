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

/// <summary>HIR function declaration with resolved symbol.</summary>
public sealed class HirFunctionDecl : HirNode
{
    public Symbol Symbol { get; }
    public IReadOnlyList<HirParameter> Parameters { get; }
    public HirBlock Body { get; }
    public HirTypeRef? ReturnType { get; }
    public bool IsAsync { get; }

    public HirFunctionDecl(Symbol symbol, IReadOnlyList<HirParameter> parameters, HirBlock body, HirTypeRef? returnType, bool isAsync, Span span)
        : base(span) { Symbol = symbol; Parameters = parameters; Body = body; ReturnType = returnType; IsAsync = isAsync; }
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
    public IReadOnlyList<HirFieldDecl> Fields { get; }
    public HirStructDecl(Symbol symbol, IReadOnlyList<HirFieldDecl> fields, Span span)
        : base(span) { Symbol = symbol; Fields = fields; }
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

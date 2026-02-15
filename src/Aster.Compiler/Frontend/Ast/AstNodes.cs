using Aster.Compiler.Diagnostics;

namespace Aster.Compiler.Frontend.Ast;

/// <summary>
/// Base class for all AST nodes. Immutable and carries source span.
/// </summary>
public abstract class AstNode
{
    /// <summary>Source location span.</summary>
    public Span Span { get; }

    protected AstNode(Span span) => Span = span;

    /// <summary>Accept a visitor for tree traversal.</summary>
    public abstract T Accept<T>(IAstVisitor<T> visitor);
}

// ========== Top-Level Declarations ==========

/// <summary>Root node of a parsed source file.</summary>
public sealed class ProgramNode : AstNode
{
    public IReadOnlyList<AstNode> Declarations { get; }
    public ProgramNode(IReadOnlyList<AstNode> declarations, Span span) : base(span) => Declarations = declarations;
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitProgram(this);
}

/// <summary>Module declaration: module name { ... }</summary>
public sealed class ModuleDeclNode : AstNode
{
    public string Name { get; }
    public IReadOnlyList<AstNode> Members { get; }
    public ModuleDeclNode(string name, IReadOnlyList<AstNode> members, Span span) : base(span) { Name = name; Members = members; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitModuleDecl(this);
}

/// <summary>Use declaration: use path::to::module::*;</summary>
public sealed class UseDeclNode : AstNode
{
    public IReadOnlyList<string> Path { get; }
    public bool IsGlob { get; }
    public UseDeclNode(IReadOnlyList<string> path, bool isGlob, Span span) : base(span) { Path = path; IsGlob = isGlob; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUseDecl(this);
}

/// <summary>Function declaration: fn name(params) -> ReturnType { body }</summary>
public sealed class FunctionDeclNode : AstNode
{
    public string Name { get; }
    public IReadOnlyList<ParameterNode> Parameters { get; }
    public TypeAnnotationNode? ReturnType { get; }
    public BlockExprNode Body { get; }
    public bool IsPublic { get; }
    public bool IsAsync { get; }
    public FunctionDeclNode(string name, IReadOnlyList<ParameterNode> parameters, TypeAnnotationNode? returnType, BlockExprNode body, bool isPublic, bool isAsync, Span span)
        : base(span) { Name = name; Parameters = parameters; ReturnType = returnType; Body = body; IsPublic = isPublic; IsAsync = isAsync; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitFunctionDecl(this);
}

/// <summary>Function parameter.</summary>
public sealed class ParameterNode : AstNode
{
    public string Name { get; }
    public TypeAnnotationNode? TypeAnnotation { get; }
    public bool IsMutable { get; }
    public ParameterNode(string name, TypeAnnotationNode? typeAnnotation, bool isMutable, Span span)
        : base(span) { Name = name; TypeAnnotation = typeAnnotation; IsMutable = isMutable; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitParameter(this);
}

/// <summary>Type annotation in source (e.g. : i32, : String).</summary>
public sealed class TypeAnnotationNode : AstNode
{
    public string Name { get; }
    public IReadOnlyList<TypeAnnotationNode> TypeArguments { get; }
    public TypeAnnotationNode(string name, IReadOnlyList<TypeAnnotationNode> typeArguments, Span span)
        : base(span) { Name = name; TypeArguments = typeArguments; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitTypeAnnotation(this);
}

/// <summary>Struct declaration: struct Name { fields }</summary>
public sealed class StructDeclNode : AstNode
{
    public string Name { get; }
    public IReadOnlyList<FieldDeclNode> Fields { get; }
    public bool IsPublic { get; }
    public IReadOnlyList<GenericParamNode> GenericParams { get; }
    public StructDeclNode(string name, IReadOnlyList<FieldDeclNode> fields, bool isPublic, IReadOnlyList<GenericParamNode> genericParams, Span span)
        : base(span) { Name = name; Fields = fields; IsPublic = isPublic; GenericParams = genericParams; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitStructDecl(this);
}

/// <summary>Field declaration within a struct.</summary>
public sealed class FieldDeclNode : AstNode
{
    public string Name { get; }
    public TypeAnnotationNode TypeAnnotation { get; }
    public bool IsPublic { get; }
    public FieldDeclNode(string name, TypeAnnotationNode typeAnnotation, bool isPublic, Span span)
        : base(span) { Name = name; TypeAnnotation = typeAnnotation; IsPublic = isPublic; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitFieldDecl(this);
}

/// <summary>Enum declaration: enum Name { variants }</summary>
public sealed class EnumDeclNode : AstNode
{
    public string Name { get; }
    public IReadOnlyList<EnumVariantNode> Variants { get; }
    public bool IsPublic { get; }
    public IReadOnlyList<GenericParamNode> GenericParams { get; }
    public EnumDeclNode(string name, IReadOnlyList<EnumVariantNode> variants, bool isPublic, IReadOnlyList<GenericParamNode> genericParams, Span span)
        : base(span) { Name = name; Variants = variants; IsPublic = isPublic; GenericParams = genericParams; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitEnumDecl(this);
}

/// <summary>Enum variant.</summary>
public sealed class EnumVariantNode : AstNode
{
    public string Name { get; }
    public IReadOnlyList<TypeAnnotationNode> Fields { get; }
    public EnumVariantNode(string name, IReadOnlyList<TypeAnnotationNode> fields, Span span)
        : base(span) { Name = name; Fields = fields; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitEnumVariant(this);
}

/// <summary>Trait declaration: trait Name { members }</summary>
public sealed class TraitDeclNode : AstNode
{
    public string Name { get; }
    public IReadOnlyList<FunctionDeclNode> Methods { get; }
    public bool IsPublic { get; }
    public IReadOnlyList<GenericParamNode> GenericParams { get; }
    public TraitDeclNode(string name, IReadOnlyList<FunctionDeclNode> methods, bool isPublic, IReadOnlyList<GenericParamNode> genericParams, Span span)
        : base(span) { Name = name; Methods = methods; IsPublic = isPublic; GenericParams = genericParams; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitTraitDecl(this);
}

/// <summary>Impl block: impl [Trait for] Type { methods }</summary>
public sealed class ImplDeclNode : AstNode
{
    public TypeAnnotationNode TargetType { get; }
    public TypeAnnotationNode? TraitType { get; }
    public IReadOnlyList<FunctionDeclNode> Methods { get; }
    public ImplDeclNode(TypeAnnotationNode targetType, TypeAnnotationNode? traitType, IReadOnlyList<FunctionDeclNode> methods, Span span)
        : base(span) { TargetType = targetType; TraitType = traitType; Methods = methods; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitImplDecl(this);
}

/// <summary>Generic parameter.</summary>
public sealed class GenericParamNode : AstNode
{
    public string Name { get; }
    public IReadOnlyList<TypeAnnotationNode> Bounds { get; }
    public GenericParamNode(string name, IReadOnlyList<TypeAnnotationNode> bounds, Span span)
        : base(span) { Name = name; Bounds = bounds; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitGenericParam(this);
}

// ========== Expressions ==========

/// <summary>Block expression: { stmts; expr }</summary>
public sealed class BlockExprNode : AstNode
{
    public IReadOnlyList<AstNode> Statements { get; }
    public AstNode? TailExpression { get; }
    public BlockExprNode(IReadOnlyList<AstNode> statements, AstNode? tailExpression, Span span)
        : base(span) { Statements = statements; TailExpression = tailExpression; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBlockExpr(this);
}

/// <summary>If expression: if cond { then } else { else }</summary>
public sealed class IfExprNode : AstNode
{
    public AstNode Condition { get; }
    public BlockExprNode ThenBranch { get; }
    public AstNode? ElseBranch { get; }
    public IfExprNode(AstNode condition, BlockExprNode thenBranch, AstNode? elseBranch, Span span)
        : base(span) { Condition = condition; ThenBranch = thenBranch; ElseBranch = elseBranch; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIfExpr(this);
}

/// <summary>Match expression.</summary>
public sealed class MatchExprNode : AstNode
{
    public AstNode Scrutinee { get; }
    public IReadOnlyList<MatchArmNode> Arms { get; }
    public MatchExprNode(AstNode scrutinee, IReadOnlyList<MatchArmNode> arms, Span span)
        : base(span) { Scrutinee = scrutinee; Arms = arms; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMatchExpr(this);
}

/// <summary>Match arm: pattern => expression</summary>
public sealed class MatchArmNode : AstNode
{
    public PatternNode Pattern { get; }
    public AstNode Body { get; }
    public MatchArmNode(PatternNode pattern, AstNode body, Span span)
        : base(span) { Pattern = pattern; Body = body; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMatchArm(this);
}

/// <summary>Pattern node for matching.</summary>
public sealed class PatternNode : AstNode
{
    public string Name { get; }
    public IReadOnlyList<PatternNode> SubPatterns { get; }
    public bool IsWildcard { get; }
    public LiteralExprNode? Literal { get; }
    public PatternNode(string name, IReadOnlyList<PatternNode> subPatterns, bool isWildcard, LiteralExprNode? literal, Span span)
        : base(span) { Name = name; SubPatterns = subPatterns; IsWildcard = isWildcard; Literal = literal; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitPattern(this);
}

/// <summary>Call expression: callee(args)</summary>
public sealed class CallExprNode : AstNode
{
    public AstNode Callee { get; }
    public IReadOnlyList<AstNode> Arguments { get; }
    public CallExprNode(AstNode callee, IReadOnlyList<AstNode> arguments, Span span)
        : base(span) { Callee = callee; Arguments = arguments; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitCallExpr(this);
}

/// <summary>Binary expression: left op right</summary>
public sealed class BinaryExprNode : AstNode
{
    public AstNode Left { get; }
    public BinaryOperator Operator { get; }
    public AstNode Right { get; }
    public BinaryExprNode(AstNode left, BinaryOperator op, AstNode right, Span span)
        : base(span) { Left = left; Operator = op; Right = right; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBinaryExpr(this);
}

/// <summary>Unary expression: op operand</summary>
public sealed class UnaryExprNode : AstNode
{
    public UnaryOperator Operator { get; }
    public AstNode Operand { get; }
    public UnaryExprNode(UnaryOperator op, AstNode operand, Span span)
        : base(span) { Operator = op; Operand = operand; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUnaryExpr(this);
}

/// <summary>Literal expression (integer, float, string, char, bool).</summary>
public sealed class LiteralExprNode : AstNode
{
    public object Value { get; }
    public LiteralKind LiteralKind { get; }
    public LiteralExprNode(object value, LiteralKind kind, Span span)
        : base(span) { Value = value; LiteralKind = kind; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLiteralExpr(this);
}

/// <summary>Array literal expression: [elem1, elem2, ...]</summary>
public sealed class ArrayLiteralExprNode : AstNode
{
    public IReadOnlyList<AstNode> Elements { get; }
    public ArrayLiteralExprNode(IReadOnlyList<AstNode> elements, Span span)
        : base(span) { Elements = elements; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitArrayLiteralExpr(this);
}

/// <summary>Identifier expression referencing a name.</summary>
public sealed class IdentifierExprNode : AstNode
{
    public string Name { get; }
    public IdentifierExprNode(string name, Span span) : base(span) => Name = name;
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIdentifierExpr(this);
}

/// <summary>Path expression for namespaced names: A::B::C</summary>
public sealed class PathExprNode : AstNode
{
    public IReadOnlyList<string> Segments { get; }
    public PathExprNode(IReadOnlyList<string> segments, Span span) : base(span) => Segments = segments;
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitPathExpr(this);
}

/// <summary>Member access expression: expr.member</summary>
public sealed class MemberAccessExprNode : AstNode
{
    public AstNode Object { get; }
    public string Member { get; }
    public MemberAccessExprNode(AstNode obj, string member, Span span) : base(span) { Object = obj; Member = member; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMemberAccessExpr(this);
}

/// <summary>Index expression: expr[index]</summary>
public sealed class IndexExprNode : AstNode
{
    public AstNode Object { get; }
    public AstNode Index { get; }
    public IndexExprNode(AstNode obj, AstNode index, Span span) : base(span) { Object = obj; Index = index; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIndexExpr(this);
}

/// <summary>Assignment expression: target = value</summary>
public sealed class AssignExprNode : AstNode
{
    public AstNode Target { get; }
    public AstNode Value { get; }
    public AssignExprNode(AstNode target, AstNode value, Span span) : base(span) { Target = target; Value = value; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitAssignExpr(this);
}

/// <summary>Struct initialization expression: StructName { field: value, ... }</summary>
public sealed class StructInitExprNode : AstNode
{
    public string StructName { get; }
    public IReadOnlyList<FieldInitNode> Fields { get; }
    public StructInitExprNode(string structName, IReadOnlyList<FieldInitNode> fields, Span span)
        : base(span) { StructName = structName; Fields = fields; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitStructInitExpr(this);
}

/// <summary>Field initialization in struct init: fieldName: value</summary>
public sealed class FieldInitNode : AstNode
{
    public string FieldName { get; }
    public AstNode Value { get; }
    public FieldInitNode(string fieldName, AstNode value, Span span)
        : base(span) { FieldName = fieldName; Value = value; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitFieldInit(this);
}

// ========== Statements ==========

/// <summary>Let statement: let [mut] name [: type] = value</summary>
public sealed class LetStmtNode : AstNode
{
    public string Name { get; }
    public bool IsMutable { get; }
    public TypeAnnotationNode? TypeAnnotation { get; }
    public AstNode? Initializer { get; }
    public LetStmtNode(string name, bool isMutable, TypeAnnotationNode? typeAnnotation, AstNode? initializer, Span span)
        : base(span) { Name = name; IsMutable = isMutable; TypeAnnotation = typeAnnotation; Initializer = initializer; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLetStmt(this);
}

/// <summary>Return statement: return expr</summary>
public sealed class ReturnStmtNode : AstNode
{
    public AstNode? Value { get; }
    public ReturnStmtNode(AstNode? value, Span span) : base(span) => Value = value;
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitReturnStmt(this);
}

/// <summary>For statement: for name in iterable { body }</summary>
public sealed class ForStmtNode : AstNode
{
    public string Variable { get; }
    public AstNode Iterable { get; }
    public BlockExprNode Body { get; }
    public ForStmtNode(string variable, AstNode iterable, BlockExprNode body, Span span)
        : base(span) { Variable = variable; Iterable = iterable; Body = body; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitForStmt(this);
}

/// <summary>While statement: while cond { body }</summary>
public sealed class WhileStmtNode : AstNode
{
    public AstNode Condition { get; }
    public BlockExprNode Body { get; }
    public WhileStmtNode(AstNode condition, BlockExprNode body, Span span)
        : base(span) { Condition = condition; Body = body; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitWhileStmt(this);
}

/// <summary>Loop statement (infinite loop): loop { body }</summary>
public sealed class LoopStmtNode : AstNode
{
    public BlockExprNode Body { get; }
    public LoopStmtNode(BlockExprNode body, Span span)
        : base(span) { Body = body; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLoopStmt(this);
}

/// <summary>Do-while statement: do { body } while cond;</summary>
public sealed class DoWhileStmtNode : AstNode
{
    public BlockExprNode Body { get; }
    public AstNode Condition { get; }
    public DoWhileStmtNode(BlockExprNode body, AstNode condition, Span span)
        : base(span) { Body = body; Condition = condition; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitDoWhileStmt(this);
}

/// <summary>Break statement.</summary>
public sealed class BreakStmtNode : AstNode
{
    public BreakStmtNode(Span span) : base(span) { }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBreakStmt(this);
}

/// <summary>Continue statement.</summary>
public sealed class ContinueStmtNode : AstNode
{
    public ContinueStmtNode(Span span) : base(span) { }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitContinueStmt(this);
}

/// <summary>Expression statement (an expression used as a statement).</summary>
public sealed class ExpressionStmtNode : AstNode
{
    public AstNode Expression { get; }
    public ExpressionStmtNode(AstNode expression, Span span) : base(span) => Expression = expression;
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitExpressionStmt(this);
}

// ========== Enums ==========

public enum BinaryOperator
{
    Add, Sub, Mul, Div, Mod,
    And, Or,
    BitAnd, BitOr, BitXor,
    Eq, Ne, Lt, Le, Gt, Ge,
    Range,
}

public enum UnaryOperator
{
    Negate, Not, BitNot, Ref, MutRef,
}

public enum LiteralKind
{
    Integer, Float, String, Char, Bool,
}

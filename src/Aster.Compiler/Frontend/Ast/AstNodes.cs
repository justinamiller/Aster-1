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

/// <summary>Function declaration: fn name<T>(params) -> ReturnType { body }</summary>
public sealed class FunctionDeclNode : AstNode
{
    public string Name { get; }
    public IReadOnlyList<GenericParamNode> GenericParams { get; }
    public IReadOnlyList<ParameterNode> Parameters { get; }
    public TypeAnnotationNode? ReturnType { get; }
    public BlockExprNode Body { get; }
    public bool IsPublic { get; }
    public bool IsAsync { get; }
    /// <summary>
    /// True for trait method signatures without a body (abstract/required methods).
    /// The Body will be an empty block used as a placeholder.
    /// </summary>
    public bool IsAbstract { get; }
    public FunctionDeclNode(string name, IReadOnlyList<GenericParamNode> genericParams, IReadOnlyList<ParameterNode> parameters, TypeAnnotationNode? returnType, BlockExprNode body, bool isPublic, bool isAsync, Span span, bool isAbstract = false)
        : base(span) { Name = name; GenericParams = genericParams; Parameters = parameters; ReturnType = returnType; Body = body; IsPublic = isPublic; IsAsync = isAsync; IsAbstract = isAbstract; }
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
    /// <summary>Phase 5: outer attributes (e.g. #[derive(Clone)]).</summary>
    public IReadOnlyList<AttributeNode> Attributes { get; }
    public StructDeclNode(string name, IReadOnlyList<FieldDeclNode> fields, bool isPublic, IReadOnlyList<GenericParamNode> genericParams, Span span, IReadOnlyList<AttributeNode>? attributes = null)
        : base(span) { Name = name; Fields = fields; IsPublic = isPublic; GenericParams = genericParams; Attributes = attributes ?? Array.Empty<AttributeNode>(); }
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
    /// <summary>Phase 5: outer attributes (e.g. #[derive(Clone)]).</summary>
    public IReadOnlyList<AttributeNode> Attributes { get; }
    public EnumDeclNode(string name, IReadOnlyList<EnumVariantNode> variants, bool isPublic, IReadOnlyList<GenericParamNode> genericParams, Span span, IReadOnlyList<AttributeNode>? attributes = null)
        : base(span) { Name = name; Variants = variants; IsPublic = isPublic; GenericParams = genericParams; Attributes = attributes ?? Array.Empty<AttributeNode>(); }
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
    /// <summary>Phase 4: associated type declarations inside the impl block.</summary>
    public IReadOnlyList<AssociatedTypeDeclNode> AssociatedTypes { get; }
    public ImplDeclNode(TypeAnnotationNode targetType, TypeAnnotationNode? traitType, IReadOnlyList<FunctionDeclNode> methods, IReadOnlyList<AssociatedTypeDeclNode> associatedTypes, Span span)
        : base(span) { TargetType = targetType; TraitType = traitType; Methods = methods; AssociatedTypes = associatedTypes; }
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

/// <summary>Closure expression: |x: T, y| body</summary>
public sealed class ClosureExprNode : AstNode
{
    public IReadOnlyList<(string Name, TypeAnnotationNode? Type)> Parameters { get; }
    public AstNode Body { get; }
    public ClosureExprNode(IReadOnlyList<(string, TypeAnnotationNode?)> parameters, AstNode body, Span span)
        : base(span) { Parameters = parameters; Body = body; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitClosureExpr(this);
}

/// <summary>Type alias declaration: type Name = SomeType;</summary>
public sealed class TypeAliasDeclNode : AstNode
{
    public string Name { get; }
    public TypeAnnotationNode Target { get; }
    public bool IsPublic { get; }
    public TypeAliasDeclNode(string name, TypeAnnotationNode target, bool isPublic, Span span)
        : base(span) { Name = name; Target = target; IsPublic = isPublic; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitTypeAliasDecl(this);
}

// ========== Phase 4 New Nodes ==========

/// <summary>Phase 4: Method call expression: receiver.method(args)</summary>
public sealed class MethodCallExprNode : AstNode
{
    public AstNode Receiver { get; }
    public string MethodName { get; }
    public IReadOnlyList<AstNode> Arguments { get; }
    public MethodCallExprNode(AstNode receiver, string methodName, IReadOnlyList<AstNode> arguments, Span span)
        : base(span) { Receiver = receiver; MethodName = methodName; Arguments = arguments; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMethodCallExpr(this);
}

/// <summary>Phase 4: Associated type declaration inside an impl or trait block: type Item = T;</summary>
public sealed class AssociatedTypeDeclNode : AstNode
{
    public string Name { get; }
    public TypeAnnotationNode? Type { get; }   // null for abstract assoc types in trait defs
    public bool IsPublic { get; }
    public AssociatedTypeDeclNode(string name, TypeAnnotationNode? type, bool isPublic, Span span)
        : base(span) { Name = name; Type = type; IsPublic = isPublic; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitAssociatedTypeDecl(this);
}

/// <summary>Phase 4: Simplified macro rule (pattern => body).</summary>
public sealed class MacroRuleNode : AstNode
{
    /// <summary>Parameter names extracted from the pattern (e.g. "x" from $x:expr).</summary>
    public IReadOnlyList<string> PatternParams { get; }
    public BlockExprNode Body { get; }
    public MacroRuleNode(IReadOnlyList<string> patternParams, BlockExprNode body, Span span)
        : base(span) { PatternParams = patternParams; Body = body; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMacroRule(this);
}

/// <summary>Phase 4: macro_rules! name { ... } declaration.</summary>
public sealed class MacroDeclNode : AstNode
{
    public string Name { get; }
    public IReadOnlyList<MacroRuleNode> Rules { get; }
    public bool IsPublic { get; }
    public MacroDeclNode(string name, IReadOnlyList<MacroRuleNode> rules, bool isPublic, Span span)
        : base(span) { Name = name; Rules = rules; IsPublic = isPublic; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMacroDecl(this);
}

/// <summary>Phase 4: Macro invocation expression: name!(args) or name![args]</summary>
public sealed class MacroInvocationExprNode : AstNode
{
    public string MacroName { get; }
    public IReadOnlyList<AstNode> Arguments { get; }
    public MacroInvocationExprNode(string macroName, IReadOnlyList<AstNode> arguments, Span span)
        : base(span) { MacroName = macroName; Arguments = arguments; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMacroInvocationExpr(this);
}

// ========== Phase 5 New Nodes ==========

/// <summary>Phase 5: A single attribute argument: an identifier or a list of identifiers.</summary>
public sealed class AttributeArgNode : AstNode
{
    public string Name { get; }
    public AttributeArgNode(string name, Span span) : base(span) => Name = name;
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitAttributeArg(this);
}

/// <summary>Phase 5: An outer attribute: #[name] or #[name(arg1, arg2, ...)]</summary>
public sealed class AttributeNode : AstNode
{
    public string Name { get; }
    public IReadOnlyList<AttributeArgNode> Arguments { get; }
    public AttributeNode(string name, IReadOnlyList<AttributeArgNode> arguments, Span span)
        : base(span) { Name = name; Arguments = arguments; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitAttribute(this);
}

// ========== Phase 6 New Nodes ==========

/// <summary>Phase 6: Cast expression — expr as Type.</summary>
public sealed class CastExprNode : AstNode
{
    public AstNode Expression { get; }
    public TypeAnnotationNode TargetType { get; }
    public CastExprNode(AstNode expression, TypeAnnotationNode targetType, Span span)
        : base(span) { Expression = expression; TargetType = targetType; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitCastExpr(this);
}

/// <summary>Phase 6: Array literal — [a, b, c].</summary>
public sealed class ArrayLiteralExprNode : AstNode
{
    public IReadOnlyList<AstNode> Elements { get; }
    public ArrayLiteralExprNode(IReadOnlyList<AstNode> elements, Span span)
        : base(span) => Elements = elements;
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitArrayLiteralExpr(this);
}

/// <summary>Phase 6: Slice or fixed-size array type annotation — [T] or [T; N].</summary>
public sealed class SliceTypeAnnotationNode : AstNode
{
    public TypeAnnotationNode ElementType { get; }
    /// <summary>Length expression for [T; N], or null for [T].</summary>
    public AstNode? Length { get; }
    public SliceTypeAnnotationNode(TypeAnnotationNode elementType, AstNode? length, Span span)
        : base(span) { ElementType = elementType; Length = length; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitSliceTypeAnnotation(this);
}

/// <summary>Phase 6b: Tuple expression — (a, b, c).</summary>
public sealed class TupleExprNode : AstNode
{
    public IReadOnlyList<AstNode> Elements { get; }
    public TupleExprNode(IReadOnlyList<AstNode> elements, Span span)
        : base(span) => Elements = elements;
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitTupleExpr(this);
}

/// <summary>Phase 6b: Tuple type annotation — (T1, T2, ...).</summary>
public sealed class TupleTypeAnnotationNode : AstNode
{
    public IReadOnlyList<TypeAnnotationNode> ElementTypes { get; }
    public TupleTypeAnnotationNode(IReadOnlyList<TypeAnnotationNode> elementTypes, Span span)
        : base(span) => ElementTypes = elementTypes;
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitTupleTypeAnnotation(this);
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
    Negate, Not, BitNot, Ref, MutRef, Try,
}

public enum LiteralKind
{
    Integer, Float, String, Char, Bool,
}

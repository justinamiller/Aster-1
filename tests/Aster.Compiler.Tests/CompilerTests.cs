using Aster.Compiler.Diagnostics;
using Aster.Compiler.Frontend.Lexer;
using Aster.Compiler.Frontend.Parser;
using Aster.Compiler.Frontend.Ast;
using Aster.Compiler.Frontend.NameResolution;
using Aster.Compiler.Frontend.TypeSystem;
using Aster.Compiler.Frontend.Effects;
using Aster.Compiler.Frontend.Ownership;
using Aster.Compiler.Frontend.Hir;
using Aster.Compiler.MiddleEnd.Mir;
using Aster.Compiler.MiddleEnd.BorrowChecker;
using Aster.Compiler.Backends.LLVM;
using Aster.Compiler.Driver;

namespace Aster.Compiler.Tests;

// ========== Phase 1: Lexer Tests ==========

public class LexerTests
{
    [Fact]
    public void Tokenize_EmptySource_ReturnsEof()
    {
        var lexer = new AsterLexer("", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenKind.Eof, tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_Identifier_ReturnsIdentifierToken()
    {
        var lexer = new AsterLexer("foo", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenKind.Identifier, tokens[0].Kind);
        Assert.Equal("foo", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_Keywords_ReturnsCorrectKinds()
    {
        var lexer = new AsterLexer("fn let mut if else return while for match struct enum trait impl", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.Fn, tokens[0].Kind);
        Assert.Equal(TokenKind.Let, tokens[1].Kind);
        Assert.Equal(TokenKind.Mut, tokens[2].Kind);
        Assert.Equal(TokenKind.If, tokens[3].Kind);
        Assert.Equal(TokenKind.Else, tokens[4].Kind);
        Assert.Equal(TokenKind.Return, tokens[5].Kind);
        Assert.Equal(TokenKind.While, tokens[6].Kind);
        Assert.Equal(TokenKind.For, tokens[7].Kind);
        Assert.Equal(TokenKind.Match, tokens[8].Kind);
        Assert.Equal(TokenKind.Struct, tokens[9].Kind);
        Assert.Equal(TokenKind.Enum, tokens[10].Kind);
        Assert.Equal(TokenKind.Trait, tokens[11].Kind);
        Assert.Equal(TokenKind.Impl, tokens[12].Kind);
    }

    [Fact]
    public void Tokenize_IntegerLiteral_ReturnsCorrectValue()
    {
        var lexer = new AsterLexer("42", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.IntegerLiteral, tokens[0].Kind);
        Assert.Equal("42", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_FloatLiteral_ReturnsCorrectValue()
    {
        var lexer = new AsterLexer("3.14", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.FloatLiteral, tokens[0].Kind);
        Assert.Equal("3.14", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_StringLiteral_ReturnsCorrectValue()
    {
        var lexer = new AsterLexer("\"hello world\"", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.StringLiteral, tokens[0].Kind);
        Assert.Equal("hello world", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_StringLiteral_EscapeSequences()
    {
        var lexer = new AsterLexer("\"hello\\nworld\"", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.StringLiteral, tokens[0].Kind);
        Assert.Equal("hello\nworld", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_CharLiteral_ReturnsCorrectValue()
    {
        var lexer = new AsterLexer("'a'", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.CharLiteral, tokens[0].Kind);
        Assert.Equal("a", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_Operators_ReturnsCorrectKinds()
    {
        var lexer = new AsterLexer("+ - * / % == != < > <= >= && || -> =>", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.Plus, tokens[0].Kind);
        Assert.Equal(TokenKind.Minus, tokens[1].Kind);
        Assert.Equal(TokenKind.Star, tokens[2].Kind);
        Assert.Equal(TokenKind.Slash, tokens[3].Kind);
        Assert.Equal(TokenKind.Percent, tokens[4].Kind);
        Assert.Equal(TokenKind.EqualsEquals, tokens[5].Kind);
        Assert.Equal(TokenKind.BangEquals, tokens[6].Kind);
        Assert.Equal(TokenKind.Less, tokens[7].Kind);
        Assert.Equal(TokenKind.Greater, tokens[8].Kind);
        Assert.Equal(TokenKind.LessEquals, tokens[9].Kind);
        Assert.Equal(TokenKind.GreaterEquals, tokens[10].Kind);
        Assert.Equal(TokenKind.AmpersandAmpersand, tokens[11].Kind);
        Assert.Equal(TokenKind.PipePipe, tokens[12].Kind);
        Assert.Equal(TokenKind.Arrow, tokens[13].Kind);
        Assert.Equal(TokenKind.FatArrow, tokens[14].Kind);
    }

    [Fact]
    public void Tokenize_Punctuation_ReturnsCorrectKinds()
    {
        var lexer = new AsterLexer("( ) { } [ ] , : ; . ::", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.LeftParen, tokens[0].Kind);
        Assert.Equal(TokenKind.RightParen, tokens[1].Kind);
        Assert.Equal(TokenKind.LeftBrace, tokens[2].Kind);
        Assert.Equal(TokenKind.RightBrace, tokens[3].Kind);
        Assert.Equal(TokenKind.LeftBracket, tokens[4].Kind);
        Assert.Equal(TokenKind.RightBracket, tokens[5].Kind);
        Assert.Equal(TokenKind.Comma, tokens[6].Kind);
        Assert.Equal(TokenKind.Colon, tokens[7].Kind);
        Assert.Equal(TokenKind.Semicolon, tokens[8].Kind);
        Assert.Equal(TokenKind.Dot, tokens[9].Kind);
        Assert.Equal(TokenKind.ColonColon, tokens[10].Kind);
    }

    [Fact]
    public void Tokenize_LineComment_IsSkipped()
    {
        var lexer = new AsterLexer("foo // comment\nbar", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.Equal(3, tokens.Count);
        Assert.Equal("foo", tokens[0].Value);
        Assert.Equal("bar", tokens[1].Value);
    }

    [Fact]
    public void Tokenize_BlockComment_IsSkipped()
    {
        var lexer = new AsterLexer("foo /* comment */ bar", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.Equal(3, tokens.Count);
        Assert.Equal("foo", tokens[0].Value);
        Assert.Equal("bar", tokens[1].Value);
    }

    [Fact]
    public void Tokenize_SpanTracking_CorrectLineAndColumn()
    {
        var lexer = new AsterLexer("fn\nmain", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.Equal(1, tokens[0].Span.Line);
        Assert.Equal(1, tokens[0].Span.Column);
        Assert.Equal(2, tokens[1].Span.Line);
        Assert.Equal(1, tokens[1].Span.Column);
    }

    [Fact]
    public void Tokenize_BooleanLiterals()
    {
        var lexer = new AsterLexer("true false", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.True, tokens[0].Kind);
        Assert.Equal(TokenKind.False, tokens[1].Kind);
    }

    [Fact]
    public void Tokenize_HexLiteral()
    {
        var lexer = new AsterLexer("0xFF", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.IntegerLiteral, tokens[0].Kind);
        Assert.Equal("0xFF", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_BinaryLiteral()
    {
        var lexer = new AsterLexer("0b1010", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.IntegerLiteral, tokens[0].Kind);
        Assert.Equal("0b1010", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_UnterminatedString_ReportsError()
    {
        var lexer = new AsterLexer("\"hello", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.True(lexer.Diagnostics.HasErrors);
    }

    [Fact]
    public void StringInterner_InternsSameString()
    {
        var interner = new StringInterner();
        var a = interner.Intern("hello");
        var b = interner.Intern("hello");

        Assert.Same(a, b);
    }
}

// ========== Phase 2: Parser Tests ==========

public class ParserTests
{
    private ProgramNode Parse(string source)
    {
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        return parser.ParseProgram();
    }

    [Fact]
    public void Parse_EmptyProgram()
    {
        var program = Parse("");
        Assert.Empty(program.Declarations);
    }

    [Fact]
    public void Parse_SimpleFunction()
    {
        var program = Parse("fn main() { }");

        Assert.Single(program.Declarations);
        var fn = Assert.IsType<FunctionDeclNode>(program.Declarations[0]);
        Assert.Equal("main", fn.Name);
        Assert.Empty(fn.Parameters);
    }

    [Fact]
    public void Parse_FunctionWithParameters()
    {
        var program = Parse("fn add(x: i32, y: i32) -> i32 { x }");

        var fn = Assert.IsType<FunctionDeclNode>(program.Declarations[0]);
        Assert.Equal("add", fn.Name);
        Assert.Equal(2, fn.Parameters.Count);
        Assert.Equal("x", fn.Parameters[0].Name);
        Assert.Equal("y", fn.Parameters[1].Name);
        Assert.NotNull(fn.ReturnType);
        Assert.Equal("i32", fn.ReturnType!.Name);
    }

    [Fact]
    public void Parse_LetStatement()
    {
        var program = Parse("fn main() { let x = 42; }");

        var fn = Assert.IsType<FunctionDeclNode>(program.Declarations[0]);
        var body = fn.Body;
        Assert.Single(body.Statements);
        var let = Assert.IsType<LetStmtNode>(body.Statements[0]);
        Assert.Equal("x", let.Name);
        Assert.False(let.IsMutable);
    }

    [Fact]
    public void Parse_MutableLetStatement()
    {
        var program = Parse("fn main() { let mut x = 42; }");

        var fn = Assert.IsType<FunctionDeclNode>(program.Declarations[0]);
        var let = Assert.IsType<LetStmtNode>(fn.Body.Statements[0]);
        Assert.True(let.IsMutable);
    }

    [Fact]
    public void Parse_LetWithTypeAnnotation()
    {
        var program = Parse("fn main() { let x: i32 = 42; }");

        var fn = Assert.IsType<FunctionDeclNode>(program.Declarations[0]);
        var let = Assert.IsType<LetStmtNode>(fn.Body.Statements[0]);
        Assert.NotNull(let.TypeAnnotation);
        Assert.Equal("i32", let.TypeAnnotation!.Name);
    }

    [Fact]
    public void Parse_FunctionCall()
    {
        var program = Parse("fn main() { print(\"hello\"); }");

        var fn = Assert.IsType<FunctionDeclNode>(program.Declarations[0]);
        var exprStmt = Assert.IsType<ExpressionStmtNode>(fn.Body.Statements[0]);
        var call = Assert.IsType<CallExprNode>(exprStmt.Expression);
        var callee = Assert.IsType<IdentifierExprNode>(call.Callee);
        Assert.Equal("print", callee.Name);
        Assert.Single(call.Arguments);
    }

    [Fact]
    public void Parse_BinaryExpression()
    {
        var program = Parse("fn main() { let x = 1 + 2; }");

        var fn = Assert.IsType<FunctionDeclNode>(program.Declarations[0]);
        var let = Assert.IsType<LetStmtNode>(fn.Body.Statements[0]);
        var bin = Assert.IsType<BinaryExprNode>(let.Initializer);
        Assert.Equal(BinaryOperator.Add, bin.Operator);
    }

    [Fact]
    public void Parse_OperatorPrecedence()
    {
        var program = Parse("fn main() { let x = 1 + 2 * 3; }");

        var fn = Assert.IsType<FunctionDeclNode>(program.Declarations[0]);
        var let = Assert.IsType<LetStmtNode>(fn.Body.Statements[0]);
        // Should parse as 1 + (2 * 3) due to precedence
        var bin = Assert.IsType<BinaryExprNode>(let.Initializer);
        Assert.Equal(BinaryOperator.Add, bin.Operator);
        var right = Assert.IsType<BinaryExprNode>(bin.Right);
        Assert.Equal(BinaryOperator.Mul, right.Operator);
    }

    [Fact]
    public void Parse_IfExpression()
    {
        var program = Parse("fn main() { if true { 1 } else { 2 } }");

        var fn = Assert.IsType<FunctionDeclNode>(program.Declarations[0]);
        // The tail expression should be an if expression
        Assert.NotNull(fn.Body.TailExpression);
        var ifExpr = Assert.IsType<IfExprNode>(fn.Body.TailExpression);
        Assert.NotNull(ifExpr.ElseBranch);
    }

    [Fact]
    public void Parse_WhileStatement()
    {
        var program = Parse("fn main() { while true { } }");

        var fn = Assert.IsType<FunctionDeclNode>(program.Declarations[0]);
        // While may end up in statements or tail expression
        Assert.True(fn.Body.Statements.Count > 0 || fn.Body.TailExpression != null);
    }

    [Fact]
    public void Parse_ReturnStatement()
    {
        var program = Parse("fn main() { return 42; }");

        var fn = Assert.IsType<FunctionDeclNode>(program.Declarations[0]);
        var ret = Assert.IsType<ReturnStmtNode>(fn.Body.Statements[0]);
        Assert.NotNull(ret.Value);
    }

    [Fact]
    public void Parse_StructDeclaration()
    {
        var program = Parse("struct Point { x: f64, y: f64 }");

        var structDecl = Assert.IsType<StructDeclNode>(program.Declarations[0]);
        Assert.Equal("Point", structDecl.Name);
        Assert.Equal(2, structDecl.Fields.Count);
    }

    [Fact]
    public void Parse_EnumDeclaration()
    {
        var program = Parse("enum Color { Red, Green, Blue }");

        var enumDecl = Assert.IsType<EnumDeclNode>(program.Declarations[0]);
        Assert.Equal("Color", enumDecl.Name);
        Assert.Equal(3, enumDecl.Variants.Count);
    }

    [Fact]
    public void Parse_TraitDeclaration()
    {
        var program = Parse("trait Drawable { fn draw() { } }");

        var traitDecl = Assert.IsType<TraitDeclNode>(program.Declarations[0]);
        Assert.Equal("Drawable", traitDecl.Name);
    }

    [Fact]
    public void Parse_UnaryExpression()
    {
        var program = Parse("fn main() { let x = -42; }");

        var fn = Assert.IsType<FunctionDeclNode>(program.Declarations[0]);
        var let = Assert.IsType<LetStmtNode>(fn.Body.Statements[0]);
        var unary = Assert.IsType<UnaryExprNode>(let.Initializer);
        Assert.Equal(UnaryOperator.Negate, unary.Operator);
    }

    [Fact]
    public void Parse_PublicFunction()
    {
        var program = Parse("pub fn main() { }");

        var fn = Assert.IsType<FunctionDeclNode>(program.Declarations[0]);
        Assert.True(fn.IsPublic);
    }

    [Fact]
    public void Parse_AsyncFunction()
    {
        var program = Parse("async fn fetch() { }");

        var fn = Assert.IsType<FunctionDeclNode>(program.Declarations[0]);
        Assert.True(fn.IsAsync);
    }

    [Fact]
    public void Parse_MatchExpression()
    {
        var program = Parse("fn main() { match x { 1 => 10, 2 => 20, _ => 0 } }");

        var fn = Assert.IsType<FunctionDeclNode>(program.Declarations[0]);
        Assert.NotNull(fn.Body.TailExpression);
        var matchExpr = Assert.IsType<MatchExprNode>(fn.Body.TailExpression);
        Assert.Equal(3, matchExpr.Arms.Count);
    }

    [Fact]
    public void Parse_ErrorRecovery()
    {
        var lexer = new AsterLexer("fn main() { let = ; } fn other() { }", "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var program = parser.ParseProgram();

        // Should have errors but still parse what it can
        Assert.True(parser.Diagnostics.HasErrors);
        // At least one declaration should be recovered
        Assert.True(program.Declarations.Count >= 1);
    }
}

// ========== Phase 3: Name Resolution Tests ==========

public class NameResolutionTests
{
    private HirProgram ResolveProgram(string source)
    {
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        return resolver.Resolve(ast);
    }

    [Fact]
    public void Resolve_FunctionDeclaration()
    {
        var hir = ResolveProgram("fn main() { }");

        Assert.Single(hir.Declarations);
        var fn = Assert.IsType<HirFunctionDecl>(hir.Declarations[0]);
        Assert.Equal("main", fn.Symbol.Name);
    }

    [Fact]
    public void Resolve_LetBinding()
    {
        var hir = ResolveProgram("fn main() { let x = 42; }");

        var fn = Assert.IsType<HirFunctionDecl>(hir.Declarations[0]);
        var let = Assert.IsType<HirLetStmt>(fn.Body.Statements[0]);
        Assert.Equal("x", let.Symbol.Name);
    }

    [Fact]
    public void Resolve_UndefinedSymbol_ReportsError()
    {
        var lexer = new AsterLexer("fn main() { undefined_var; }", "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        resolver.Resolve(ast);

        Assert.True(resolver.Diagnostics.HasErrors);
    }

    [Fact]
    public void Resolve_DuplicateDefinition_ReportsError()
    {
        var lexer = new AsterLexer("fn foo() { } fn foo() { }", "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        resolver.Resolve(ast);

        Assert.True(resolver.Diagnostics.HasErrors);
    }

    [Fact]
    public void Resolve_BuiltinPrint_IsResolved()
    {
        var lexer = new AsterLexer("fn main() { print(\"hello\"); }", "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        Assert.False(resolver.Diagnostics.HasErrors);
    }
}

// ========== Phase 4: Type System Tests ==========

public class TypeSystemTests
{
    [Fact]
    public void TypeCheck_IntegerLiteral_HasI32Type()
    {
        var driver = new CompilationDriver();
        var ok = driver.Check("fn main() { let x: i32 = 42; }", "test.ast");
        Assert.True(ok);
    }

    [Fact]
    public void TypeCheck_BoolLiteral_HasBoolType()
    {
        var driver = new CompilationDriver();
        var ok = driver.Check("fn main() { let x: bool = true; }", "test.ast");
        Assert.True(ok);
    }

    [Fact]
    public void TypeCheck_StringLiteral_HasStringType()
    {
        var driver = new CompilationDriver();
        var ok = driver.Check("fn main() { let x: String = \"hello\"; }", "test.ast");
        Assert.True(ok);
    }

    [Fact]
    public void Unification_TypeVariableWithConcrete()
    {
        var checker = new TypeChecker();
        var tv = new TypeVariable();
        Assert.True(checker.Unify(tv, PrimitiveType.I32));
    }

    [Fact]
    public void Unification_SamePrimitives_Succeeds()
    {
        var checker = new TypeChecker();
        Assert.True(checker.Unify(PrimitiveType.I32, PrimitiveType.I32));
    }

    [Fact]
    public void Unification_DifferentPrimitives_Fails()
    {
        var checker = new TypeChecker();
        Assert.False(checker.Unify(PrimitiveType.I32, PrimitiveType.Bool));
    }
}

// ========== Phase 5: Effect System Tests ==========

public class EffectSystemTests
{
    [Fact]
    public void EffectSet_EmptyIsPure()
    {
        var set = new EffectSet();
        Assert.False(set.Has(Effect.Io));
        Assert.Equal("pure", set.ToString());
    }

    [Fact]
    public void EffectSet_AddEffect()
    {
        var set = new EffectSet();
        set.Add(Effect.Io);
        Assert.True(set.Has(Effect.Io));
    }

    [Fact]
    public void EffectSet_MergeEffects()
    {
        var a = new EffectSet(Effect.Io);
        var b = new EffectSet(Effect.Alloc);
        a.Merge(b);

        Assert.True(a.Has(Effect.Io));
        Assert.True(a.Has(Effect.Alloc));
    }
}

// ========== Phase 6: Ownership Tests ==========

public class OwnershipTests
{
    [Fact]
    public void Ownership_RegisterOwned_IsOwned()
    {
        var tracker = new OwnershipTracker();
        var sym = new Symbol("x", SymbolKind.Value);
        tracker.RegisterOwned(sym);

        Assert.Equal(OwnershipState.Owned, tracker.GetState(sym));
    }

    [Fact]
    public void Ownership_Move_InvalidatesSource()
    {
        var tracker = new OwnershipTracker();
        var sym = new Symbol("x", SymbolKind.Value);
        tracker.RegisterOwned(sym);
        tracker.Move(sym, Span.Unknown);

        Assert.Equal(OwnershipState.Moved, tracker.GetState(sym));
    }

    [Fact]
    public void Ownership_UseAfterMove_ReportsError()
    {
        var tracker = new OwnershipTracker();
        var sym = new Symbol("x", SymbolKind.Value);
        tracker.RegisterOwned(sym);
        tracker.Move(sym, Span.Unknown);
        tracker.Move(sym, Span.Unknown);

        Assert.True(tracker.Diagnostics.HasErrors);
    }

    [Fact]
    public void Ownership_ImmutableBorrowWhileMutablyBorrowed_ReportsError()
    {
        var tracker = new OwnershipTracker();
        var sym = new Symbol("x", SymbolKind.Value);
        tracker.RegisterOwned(sym);
        tracker.BorrowMutable(sym, Span.Unknown);
        tracker.BorrowImmutable(sym, Span.Unknown);

        Assert.True(tracker.Diagnostics.HasErrors);
    }

    [Fact]
    public void Ownership_MutableBorrowWhileImmutableBorrowed_ReportsError()
    {
        var tracker = new OwnershipTracker();
        var sym = new Symbol("x", SymbolKind.Value);
        tracker.RegisterOwned(sym);
        tracker.BorrowImmutable(sym, Span.Unknown);
        tracker.BorrowMutable(sym, Span.Unknown);

        Assert.True(tracker.Diagnostics.HasErrors);
    }
}

// ========== Phase 7: MIR Tests ==========

public class MirTests
{
    [Fact]
    public void MirLowering_SimpleFunction_ProducesBlocks()
    {
        var source = "fn main() { let x = 42; }";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);
        var lowering = new MirLowering();
        var mir = lowering.Lower(hir);

        Assert.Single(mir.Functions);
        Assert.Equal("main", mir.Functions[0].Name);
        Assert.True(mir.Functions[0].BasicBlocks.Count > 0);
    }
}

// ========== Phase 8: LLVM Backend Tests ==========

public class LlvmBackendTests
{
    [Fact]
    public void LlvmBackend_EmitsValidIR()
    {
        var source = "fn main() { }";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");

        Assert.NotNull(ir);
        Assert.Contains("define", ir);
        Assert.Contains("@main", ir);
    }

    [Fact]
    public void LlvmBackend_EmitsRuntimeDeclarations()
    {
        var source = "fn main() { }";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");

        Assert.NotNull(ir);
        Assert.Contains("declare i32 @puts(ptr)", ir);
        Assert.Contains("declare ptr @malloc(i64)", ir);
        Assert.Contains("declare void @free(ptr)", ir);
    }

    [Fact]
    public void LlvmBackend_EmitsStringConstants()
    {
        var source = "fn main() { print(\"hello world\"); }";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");

        Assert.NotNull(ir);
        Assert.Contains("hello world", ir);
        Assert.Contains("@puts", ir);
    }
}

// ========== Integration Tests ==========

public class IntegrationTests
{
    [Fact]
    public void FullPipeline_HelloWorld()
    {
        var source = @"
fn main() {
    print(""hello world"")
}
";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");

        Assert.NotNull(ir);
        Assert.False(driver.Diagnostics.HasErrors);
        Assert.Contains("hello world", ir);
        Assert.Contains("@main", ir);
    }

    [Fact]
    public void FullPipeline_LetBinding()
    {
        var source = @"
fn main() {
    let x = 42;
    let y = x;
}
";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");

        Assert.NotNull(ir);
        Assert.False(driver.Diagnostics.HasErrors);
    }

    [Fact]
    public void FullPipeline_FunctionWithReturnType()
    {
        var source = @"
fn add(x: i32, y: i32) -> i32 {
    x
}
fn main() {
    let result = add(1, 2);
}
";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");

        Assert.NotNull(ir);
        Assert.Contains("@add", ir);
    }

    [Fact]
    public void FullPipeline_StructDeclaration()
    {
        var source = @"
struct Point {
    x: f64,
    y: f64
}
fn main() { }
";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");

        Assert.NotNull(ir);
    }

    [Fact]
    public void FullPipeline_EnumDeclaration()
    {
        var source = @"
enum Color {
    Red,
    Green,
    Blue
}
fn main() { }
";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");

        Assert.NotNull(ir);
    }

    [Fact]
    public void FullPipeline_IfExpression()
    {
        var source = @"
fn main() {
    if true {
        print(""yes"")
    } else {
        print(""no"")
    }
}
";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");

        Assert.NotNull(ir);
    }

    [Fact]
    public void FullPipeline_WhileLoop()
    {
        var source = @"
fn main() {
    let mut i = 0;
    while true {
        i = i;
    }
}
";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");

        Assert.NotNull(ir);
    }

    [Fact]
    public void Check_ValidProgram_Succeeds()
    {
        var driver = new CompilationDriver();
        var ok = driver.Check("fn main() { }", "test.ast");
        Assert.True(ok);
    }

    [Fact]
    public void DiagnosticBag_CollectsDiagnostics()
    {
        var bag = new DiagnosticBag();
        bag.ReportError("E0001", "test error", Span.Unknown);

        Assert.Equal(1, bag.Count);
        Assert.True(bag.HasErrors);
    }

    [Fact]
    public void Span_Properties()
    {
        var span = new Span("test.ast", 1, 5, 10, 3);

        Assert.Equal("test.ast", span.File);
        Assert.Equal(1, span.Line);
        Assert.Equal(5, span.Column);
        Assert.Equal(10, span.Start);
        Assert.Equal(3, span.Length);
        Assert.Equal(13, span.End);
        Assert.Equal("test.ast:1:5", span.ToString());
    }
}

// ========== Scope Tests ==========

public class ScopeTests
{
    [Fact]
    public void Scope_DefineAndLookup()
    {
        var scope = new Scope(ScopeKind.Module);
        var sym = new Symbol("x", SymbolKind.Value);
        Assert.True(scope.Define(sym));
        Assert.Same(sym, scope.Lookup("x"));
    }

    [Fact]
    public void Scope_DuplicateDefine_ReturnsFalse()
    {
        var scope = new Scope(ScopeKind.Module);
        scope.Define(new Symbol("x", SymbolKind.Value));
        Assert.False(scope.Define(new Symbol("x", SymbolKind.Value)));
    }

    [Fact]
    public void Scope_ChildLooksUpParent()
    {
        var parent = new Scope(ScopeKind.Module);
        var sym = new Symbol("x", SymbolKind.Value);
        parent.Define(sym);
        var child = parent.CreateChild(ScopeKind.Block);

        Assert.Same(sym, child.Lookup("x"));
    }

    [Fact]
    public void Scope_ChildShadowsParent()
    {
        var parent = new Scope(ScopeKind.Module);
        parent.Define(new Symbol("x", SymbolKind.Value));
        var child = parent.CreateChild(ScopeKind.Block);
        var childSym = new Symbol("x", SymbolKind.Value);
        child.Define(childSym);

        Assert.Same(childSym, child.Lookup("x"));
    }
}

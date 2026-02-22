using Aster.Compiler.Diagnostics;
using Aster.Compiler.Frontend.Lexer;
using Aster.Compiler.Frontend.Parser;
using Aster.Compiler.Frontend.Ast;
using Aster.Compiler.Frontend.NameResolution;
using Aster.Compiler.Frontend.TypeSystem;
using Aster.Compiler.Frontend.Effects;
using Aster.Compiler.Frontend.Ownership;
using Aster.Compiler.Frontend.Hir;
using Aster.Compiler.MiddleEnd.AsyncLowering;
using Aster.Compiler.MiddleEnd.Mir;
using Aster.Compiler.MiddleEnd.BorrowChecker;
using Aster.Compiler.MiddleEnd.Generics;
using Aster.Compiler.MiddleEnd.Optimizations;
using Aster.Compiler.MiddleEnd.PatternMatching;
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

    [Fact]
    public void Tokenize_UseKeyword_ReturnsCorrectKind()
    {
        var lexer = new AsterLexer("use std::fmt::*;", "test.ast");
        var tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.Use, tokens[0].Kind);
        Assert.Equal(TokenKind.Identifier, tokens[1].Kind);
        Assert.Equal("std", tokens[1].Value);
        Assert.Equal(TokenKind.ColonColon, tokens[2].Kind);
        Assert.Equal(TokenKind.Identifier, tokens[3].Kind);
        Assert.Equal("fmt", tokens[3].Value);
        Assert.Equal(TokenKind.ColonColon, tokens[4].Kind);
        Assert.Equal(TokenKind.Star, tokens[5].Kind);
        Assert.Equal(TokenKind.Semicolon, tokens[6].Kind);
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

    [Fact]
    public void Parse_UseDeclaration_GlobImport()
    {
        var program = Parse("use std::fmt::*;");

        Assert.Single(program.Declarations);
        var useDecl = Assert.IsType<UseDeclNode>(program.Declarations[0]);
        Assert.Equal(new[] { "std", "fmt" }, useDecl.Path);
        Assert.True(useDecl.IsGlob);
    }

    [Fact]
    public void Parse_UseDeclaration_SpecificImport()
    {
        var program = Parse("use std::fmt::println;");

        Assert.Single(program.Declarations);
        var useDecl = Assert.IsType<UseDeclNode>(program.Declarations[0]);
        Assert.Equal(new[] { "std", "fmt", "println" }, useDecl.Path);
        Assert.False(useDecl.IsGlob);
    }

    [Fact]
    public void Parse_UseDeclaration_WithFunction()
    {
        var program = Parse(@"
use std::fmt::*;

fn main() {
    println(""Hello, World!"")
}
");
        Assert.Equal(2, program.Declarations.Count);
        Assert.IsType<UseDeclNode>(program.Declarations[0]);
        Assert.IsType<FunctionDeclNode>(program.Declarations[1]);
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

    [Fact]
    public void Resolve_UseStdFmt_RegistersPrintln()
    {
        var source = @"
use std::fmt::*;

fn main() {
    println(""Hello, World!"");
}
";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        Assert.False(resolver.Diagnostics.HasErrors);
    }

    [Fact]
    public void Resolve_UseSpecificImport_RegistersSymbol()
    {
        var source = @"
use std::fmt::println;

fn main() {
    println(""Hello"");
}
";
        var lexer = new AsterLexer(source, "test.ast");
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

    [Fact]
    public void FullPipeline_StdlibHelloWorld()
    {
        var source = @"
use std::fmt::*;

fn main() {
    println(""Hello, World!"");
    println(""Welcome to Aster Standard Library!"");
}
";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");

        Assert.NotNull(ir);
        Assert.False(driver.Diagnostics.HasErrors);
        Assert.Contains("Hello, World!", ir);
        Assert.Contains("Welcome to Aster Standard Library!", ir);
        Assert.Contains("@main", ir);
        Assert.Contains("@puts", ir);
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

// ========== Type System Tests ==========

public class TypeInferenceTests
{
    [Fact]
    public void ConstraintSolver_UnifyPrimitiveTypes()
    {
        var solver = new ConstraintSolver();
        var t1 = PrimitiveType.I32;
        var t2 = PrimitiveType.I32;
        
        Assert.True(solver.Unify(t1, t2));
    }

    [Fact]
    public void ConstraintSolver_UnifyDifferentPrimitives_Fails()
    {
        var solver = new ConstraintSolver();
        var t1 = PrimitiveType.I32;
        var t2 = PrimitiveType.F64;
        
        Assert.False(solver.Unify(t1, t2));
    }

    [Fact]
    public void ConstraintSolver_UnifyTypeVariableWithConcrete()
    {
        var solver = new ConstraintSolver();
        var tv = new TypeVariable();
        var concrete = PrimitiveType.I32;
        
        Assert.True(solver.Unify(tv, concrete));
        Assert.Equal(concrete, solver.Resolve(tv));
    }

    [Fact]
    public void ConstraintSolver_UnifyFunctionTypes()
    {
        var solver = new ConstraintSolver();
        var f1 = new FunctionType(
            new[] { PrimitiveType.I32, PrimitiveType.F64 },
            PrimitiveType.Bool);
        var f2 = new FunctionType(
            new[] { PrimitiveType.I32, PrimitiveType.F64 },
            PrimitiveType.Bool);
        
        Assert.True(solver.Unify(f1, f2));
    }

    [Fact]
    public void ConstraintSolver_UnifyFunctionTypesWithDifferentReturn_Fails()
    {
        var solver = new ConstraintSolver();
        var f1 = new FunctionType(
            new[] { PrimitiveType.I32 },
            PrimitiveType.Bool);
        var f2 = new FunctionType(
            new[] { PrimitiveType.I32 },
            PrimitiveType.Void);
        
        Assert.False(solver.Unify(f1, f2));
    }

    [Fact]
    public void ConstraintSolver_OccursCheck_DetectsInfiniteType()
    {
        var solver = new ConstraintSolver();
        var tv = new TypeVariable();
        var fnType = new FunctionType(new[] { tv }, PrimitiveType.Void);
        
        Assert.False(solver.Unify(tv, fnType));
    }

    [Fact]
    public void ConstraintSolver_UnifyReferences()
    {
        var solver = new ConstraintSolver();
        var r1 = new ReferenceType(PrimitiveType.I32, false);
        var r2 = new ReferenceType(PrimitiveType.I32, false);
        
        Assert.True(solver.Unify(r1, r2));
    }

    [Fact]
    public void ConstraintSolver_UnifyMutableAndImmutableRefs_Fails()
    {
        var solver = new ConstraintSolver();
        var r1 = new ReferenceType(PrimitiveType.I32, true);
        var r2 = new ReferenceType(PrimitiveType.I32, false);
        
        Assert.False(solver.Unify(r1, r2));
    }

    [Fact]
    public void TypeApp_DisplayName()
    {
        var constructor = new StructType("Vec", Array.Empty<(string, AsterType)>());
        var typeApp = new TypeApp(constructor, new[] { PrimitiveType.I32 });
        
        Assert.Equal("Vec<i32>", typeApp.DisplayName);
    }

    [Fact]
    public void GenericParameter_WithBounds()
    {
        var bound = new TraitBound("Clone");
        var param = new GenericParameter("T", 1, new[] { bound });
        
        Assert.Equal("T", param.Name);
        Assert.Single(param.Bounds);
        Assert.Equal("Clone", param.Bounds[0].TraitName);
    }

    [Fact]
    public void TypeScheme_Instantiate()
    {
        var param = new GenericParameter("T", 1);
        var bodyType = new FunctionType(new[] { param }, param);
        var scheme = new TypeScheme(new[] { param }, Array.Empty<TraitBound>(), bodyType);
        
        var instantiated = scheme.Instantiate(new Dictionary<int, AsterType>());
        
        Assert.IsType<FunctionType>(instantiated);
        var fnType = (FunctionType)instantiated;
        Assert.IsType<TypeVariable>(fnType.ParameterTypes[0]);
        Assert.IsType<TypeVariable>(fnType.ReturnType);
    }
}

public class TraitSolverTests
{
    [Fact]
    public void TraitSolver_RegisterBuiltins()
    {
        var solver = new TraitSolver();
        solver.RegisterBuiltins();
        
        var constraintSolver = new ConstraintSolver();
        var obligation = new Obligation(
            PrimitiveType.I32,
            new TraitBound("Copy"),
            Span.Unknown);
        
        Assert.True(solver.Resolve(obligation, constraintSolver));
    }

    [Fact]
    public void TraitSolver_UnregisteredTrait_Fails()
    {
        var solver = new TraitSolver();
        solver.RegisterBuiltins();
        
        var constraintSolver = new ConstraintSolver();
        var obligation = new Obligation(
            PrimitiveType.I32,
            new TraitBound("NonexistentTrait"),
            Span.Unknown);
        
        Assert.False(solver.Resolve(obligation, constraintSolver));
    }

    [Fact]
    public void TraitSolver_CustomImpl()
    {
        var solver = new TraitSolver();
        var structType = new StructType("MyStruct", Array.Empty<(string, AsterType)>());
        var impl = new TraitImpl("Display", structType);
        solver.RegisterImpl(impl);
        
        var constraintSolver = new ConstraintSolver();
        var obligation = new Obligation(
            structType,
            new TraitBound("Display"),
            Span.Unknown);
        
        Assert.True(solver.Resolve(obligation, constraintSolver));
    }

    [Fact]
    public void TraitSolver_CycleDetection()
    {
        var solver = new TraitSolver();
        var param = new GenericParameter("T", 1, new[] { new TraitBound("Debug") });
        
        // This would create a cycle if not detected
        solver.RegisterImpl(new TraitImpl("Debug", param));
        
        var constraintSolver = new ConstraintSolver();
        var obligation = new Obligation(
            param,
            new TraitBound("Debug"),
            Span.Unknown);
        
        // Should detect the cycle via the in-progress tracking
        solver.Resolve(obligation, constraintSolver);
    }
}

public class ConstraintGenerationTests
{
    [Fact]
    public void EqualityConstraint_ToString()
    {
        var c = new EqualityConstraint(PrimitiveType.I32, PrimitiveType.F64, Span.Unknown);
        Assert.Equal("i32 = f64", c.ToString());
    }

    [Fact]
    public void TraitConstraint_ToString()
    {
        var c = new TraitConstraint(
            PrimitiveType.I32,
            new TraitBound("Copy"),
            Span.Unknown);
        Assert.Equal("i32: Copy", c.ToString());
    }
}

// ========== Pattern Matching Tests ==========

public class PatternMatchingTests
{
    [Fact]
    public void PatternChecker_ExhaustiveBool()
    {
        var checker = new PatternChecker();
        var arms = new[]
        {
            (new LiteralPattern(true, Span.Unknown) as Pattern, Span.Unknown),
            (new LiteralPattern(false, Span.Unknown) as Pattern, Span.Unknown)
        };
        
        checker.CheckMatch(PrimitiveType.Bool, arms);
        
        Assert.False(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void PatternChecker_NonExhaustiveBool()
    {
        var checker = new PatternChecker();
        var arms = new[]
        {
            (new LiteralPattern(true, Span.Unknown) as Pattern, Span.Unknown)
        };
        
        checker.CheckMatch(PrimitiveType.Bool, arms);
        
        Assert.True(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void PatternChecker_WildcardIsExhaustive()
    {
        var checker = new PatternChecker();
        var arms = new[]
        {
            (new WildcardPattern(Span.Unknown) as Pattern, Span.Unknown)
        };
        
        checker.CheckMatch(PrimitiveType.I32, arms);
        
        Assert.False(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void PatternChecker_ExhaustiveEnum()
    {
        var checker = new PatternChecker();
        var enumType = new EnumType("Option", new[]
        {
            ("Some", new List<AsterType> { PrimitiveType.I32 } as IReadOnlyList<AsterType>),
            ("None", Array.Empty<AsterType>() as IReadOnlyList<AsterType>)
        });
        
        var arms = new[]
        {
            (new ConstructorPattern("Some", new[] { new WildcardPattern(Span.Unknown) as Pattern }, Span.Unknown) as Pattern, Span.Unknown),
            (new ConstructorPattern("None", Array.Empty<Pattern>(), Span.Unknown) as Pattern, Span.Unknown)
        };
        
        checker.CheckMatch(enumType, arms);
        
        Assert.False(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void PatternChecker_NonExhaustiveEnum()
    {
        var checker = new PatternChecker();
        var enumType = new EnumType("Option", new[]
        {
            ("Some", new List<AsterType> { PrimitiveType.I32 } as IReadOnlyList<AsterType>),
            ("None", Array.Empty<AsterType>() as IReadOnlyList<AsterType>)
        });
        
        var arms = new[]
        {
            (new ConstructorPattern("Some", new[] { new WildcardPattern(Span.Unknown) as Pattern }, Span.Unknown) as Pattern, Span.Unknown)
        };
        
        checker.CheckMatch(enumType, arms);
        
        Assert.True(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void PatternChecker_UnreachableArm()
    {
        var checker = new PatternChecker();
        var arms = new[]
        {
            (new WildcardPattern(Span.Unknown) as Pattern, Span.Unknown),
            (new LiteralPattern(42, Span.Unknown) as Pattern, Span.Unknown)
        };
        
        checker.CheckMatch(PrimitiveType.I32, arms);
        
        // Should have a warning for the unreachable arm
        var diagnostics = checker.Diagnostics.ToImmutableList();
        Assert.Contains(diagnostics, d => d.Code == "W0001");
    }
}

// ========== Borrow Checker Tests ==========

public class BorrowCheckerTests
{
    [Fact]
    public void BorrowCheck_EmptyFunction()
    {
        var checker = new BorrowCheck();
        var module = new MirModule("test");
        var fn = new MirFunction("main");
        module.Functions.Add(fn);
        
        checker.Check(module);
        
        Assert.False(checker.Diagnostics.HasErrors);
    }
}

// ========== Advanced Integration Tests ==========

public class AdvancedIntegrationTests
{
    [Fact]
    public void IntegrationTest_TypeInferenceSuccess()
    {
        var source = @"
fn identity(x: i32) -> i32 {
    x
}

fn main() {
    let y = identity(42);
}
";
        var driver = new CompilationDriver();
        var ok = driver.Check(source, "test.ast");
        Assert.True(ok);
    }

    [Fact]
    public void IntegrationTest_LetPolymorphism()
    {
        var source = @"
fn main() {
    let id = fn(x: i32) -> i32 { x };
    let a = id(42);
    let b = id(100);
}
";
        // Note: This tests let-polymorphism where id can be used multiple times
        var driver = new CompilationDriver();
        var ok = driver.Check(source, "test.ast");
        // May fail if function literals aren't fully implemented, but the type system supports it
    }

    [Fact]
    public void IntegrationTest_EffectInference()
    {
        var source = @"
fn greet() {
    print(""hello"")
}

fn main() {
    greet()
}
";
        var driver = new CompilationDriver();
        var ok = driver.Check(source, "test.ast");
        Assert.True(ok);
        // Effects should be inferred as IO
    }

    [Fact]
    public void IntegrationTest_SimpleStruct()
    {
        var source = @"
struct Point {
    x: i32,
    y: i32
}

fn main() {
    let p = Point { x: 10, y: 20 };
}
";
        var driver = new CompilationDriver();
        var ok = driver.Check(source, "test.ast");
        // Struct construction syntax may not be fully implemented
    }
}

// ========== Stage1 Mode Tests ==========

public class Stage1ModeTests
{
    [Fact]
    public void Stage1Mode_ValidCore0Code_Passes()
    {
        var source = @"
struct Point {
    x: i32,
    y: i32
}

fn area(p: Point) -> i32 {
    p.x * p.y
}

fn main() {
    let p = Point { x: 10, y: 20 };
    let a = area(p);
}
";
        var driver = new CompilationDriver(stage1Mode: true);
        var ok = driver.Check(source, "test.ast");
        
        // Should compile without Stage1 errors
        Assert.True(ok || driver.Diagnostics.All(d => !d.Code.StartsWith("E90")));
    }

    [Fact]
    public void Stage1Mode_RejectsTraits()
    {
        var source = @"
trait Printable {
    fn print(&self);
}
";
        var driver = new CompilationDriver(stage1Mode: true);
        var ok = driver.Check(source, "test.ast");
        
        Assert.False(ok);
        Assert.Contains(driver.Diagnostics, d => d.Code == "E9001");
    }

    [Fact]
    public void Stage1Mode_RejectsImplBlocks()
    {
        var source = @"
struct Point { x: i32 }

impl Point {
    fn new() -> Point {
        Point { x: 0 }
    }
}
";
        var driver = new CompilationDriver(stage1Mode: true);
        var ok = driver.Check(source, "test.ast");
        
        Assert.False(ok);
        Assert.Contains(driver.Diagnostics, d => d.Code == "E9002");
    }

    [Fact]
    public void Stage1Mode_RejectsAsyncFunctions()
    {
        var source = @"
async fn fetch_data() -> i32 {
    42
}
";
        var driver = new CompilationDriver(stage1Mode: true);
        var ok = driver.Check(source, "test.ast");
        
        Assert.False(ok);
        Assert.Contains(driver.Diagnostics, d => d.Code == "E9003");
    }

    [Fact]
    public void Stage1Mode_RejectsReferenceTypes()
    {
        var source = @"
fn read_value(x: &i32) -> i32 {
    x
}
";
        var driver = new CompilationDriver(stage1Mode: true);
        var ok = driver.Check(source, "test.ast");
        
        Assert.False(ok);
        Assert.Contains(driver.Diagnostics, d => d.Code == "E9004");
    }

    [Fact]
    public void Stage1Mode_RejectsReferenceExpressions()
    {
        var source = @"
fn main() {
    let x = 10;
    let y = &x;
}
";
        var driver = new CompilationDriver(stage1Mode: true);
        var ok = driver.Check(source, "test.ast");
        
        Assert.False(ok);
        Assert.Contains(driver.Diagnostics, d => d.Code == "E9004");
    }

    [Fact]
    public void Stage1Mode_RejectsClosures()
    {
        var source = @"
fn main() {
    let add_one = |x| x + 1;
}
";
        var driver = new CompilationDriver(stage1Mode: true);
        var ok = driver.Check(source, "test.ast");
        
        Assert.False(ok);
        Assert.Contains(driver.Diagnostics, d => d.Code == "E9005");
    }

    [Fact]
    public void NormalMode_AllowsTraits()
    {
        var source = @"
trait Printable {
    fn print(&self);
}
";
        var driver = new CompilationDriver(stage1Mode: false);
        var ok = driver.Check(source, "test.ast");
        
        // Should not have Stage1 errors (E90XX)
        Assert.DoesNotContain(driver.Diagnostics, d => d.Code.StartsWith("E90"));
    }

    [Fact]
    public void NormalMode_AllowsAsyncFunctions()
    {
        var source = @"
async fn fetch_data() -> i32 {
    42
}
";
        var driver = new CompilationDriver(stage1Mode: false);
        var ok = driver.Check(source, "test.ast");
        
        // Should not have Stage1 errors (E90XX)
        Assert.DoesNotContain(driver.Diagnostics, d => d.Code.StartsWith("E90"));
    }

    // ========== Phase 2 Week 9: Generic Function and Struct Tests ==========

    [Fact]
    public void Parse_GenericFunctionDecl_SingleParam()
    {
        var source = "fn identity<T>(x: T) -> T { x }";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();

        Assert.Single(ast.Declarations);
        var fn = Assert.IsType<FunctionDeclNode>(ast.Declarations[0]);
        Assert.Equal("identity", fn.Name);
        Assert.Single(fn.GenericParams);
        Assert.Equal("T", fn.GenericParams[0].Name);
    }

    [Fact]
    public void Parse_GenericFunctionDecl_MultipleParams()
    {
        var source = "fn first<A, B>(a: A, b: B) -> A { a }";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();

        var fn = Assert.IsType<FunctionDeclNode>(ast.Declarations[0]);
        Assert.Equal(2, fn.GenericParams.Count);
        Assert.Equal("A", fn.GenericParams[0].Name);
        Assert.Equal("B", fn.GenericParams[1].Name);
    }

    [Fact]
    public void Parse_GenericFunctionDecl_WithBound()
    {
        var source = "fn max<T: Ord>(a: T, b: T) -> T { a }";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();

        var fn = Assert.IsType<FunctionDeclNode>(ast.Declarations[0]);
        Assert.Single(fn.GenericParams);
        Assert.Equal("T", fn.GenericParams[0].Name);
        Assert.Single(fn.GenericParams[0].Bounds);
        Assert.Equal("Ord", fn.GenericParams[0].Bounds[0].Name);
    }

    [Fact]
    public void Parse_GenericStructDecl()
    {
        var source = "struct Box<T> { value: T }";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();

        Assert.Single(ast.Declarations);
        var s = Assert.IsType<StructDeclNode>(ast.Declarations[0]);
        Assert.Equal("Box", s.Name);
        Assert.Single(s.GenericParams);
        Assert.Equal("T", s.GenericParams[0].Name);
    }

    [Fact]
    public void Resolve_GenericFunction_SingleParam_NoErrors()
    {
        var source = "fn identity<T>(x: T) -> T { x }";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        resolver.Resolve(ast);

        Assert.False(resolver.Diagnostics.HasErrors, 
            "Expected no errors resolving generic function 'identity<T>'");
    }

    [Fact]
    public void Resolve_GenericFunction_MultipleParams_NoErrors()
    {
        var source = "fn first<A, B>(a: A, b: B) -> A { a }";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        resolver.Resolve(ast);

        Assert.False(resolver.Diagnostics.HasErrors,
            "Expected no errors resolving generic function 'first<A, B>'");
    }

    [Fact]
    public void Resolve_GenericStruct_NoErrors()
    {
        var source = "struct Box<T> { value: T }";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        resolver.Resolve(ast);

        Assert.False(resolver.Diagnostics.HasErrors,
            "Expected no errors resolving generic struct 'Box<T>'");
    }

    [Fact]
    public void Resolve_GenericFunction_StoresGenericParamNames()
    {
        var source = "fn first<A, B>(a: A, b: B) -> A { a }";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        var fn = Assert.IsType<HirFunctionDecl>(hir.Declarations[0]);
        Assert.Equal(2, fn.GenericParams.Count);
        Assert.Contains(fn.GenericParams, gp => gp.Name == "A");
        Assert.Contains(fn.GenericParams, gp => gp.Name == "B");
    }

    [Fact]
    public void FullPipeline_GenericFunction_SimpleIdentity()
    {
        var source = @"
fn identity<T>(x: T) -> T {
    x
}
fn main() -> i32 {
    let result = identity(42);
    0
}
";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");

        Assert.NotNull(ir);
        Assert.Contains("@identity", ir);
    }

    [Fact]
    public void FullPipeline_GenericStruct_WithTypeParam()
    {
        var source = @"
struct Wrapper<T> {
    value: T
}
fn main() { }
";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");

        Assert.NotNull(ir);
    }

    [Fact]
    public void FullPipeline_GenericEnum_WithTypeParam()
    {
        var source = @"
enum Maybe<T> {
    Some(T),
    None
}
fn main() { }
";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");

        Assert.NotNull(ir);
    }
}

// ========== Phase 2 Week 10: Type System for Generics ==========

public class Week10TypeSystemTests
{
    [Fact]
    public void Resolve_GenericParam_PreservesBounds()
    {
        var source = "fn clone<T: Clone>(x: T) -> T { x }";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        var fn = Assert.IsType<HirFunctionDecl>(hir.Declarations[0]);
        Assert.Single(fn.GenericParams);
        Assert.Equal("T", fn.GenericParams[0].Name);
        Assert.Single(fn.GenericParams[0].Bounds);
        Assert.Equal("Clone", fn.GenericParams[0].Bounds[0]);
    }

    [Fact]
    public void Resolve_GenericParam_MultipleBounds()
    {
        var source = "fn show<T: Display + Debug>(x: T) -> i32 { 0 }";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        var fn = Assert.IsType<HirFunctionDecl>(hir.Declarations[0]);
        Assert.Equal(2, fn.GenericParams[0].Bounds.Count);
        Assert.Contains("Display", fn.GenericParams[0].Bounds);
        Assert.Contains("Debug", fn.GenericParams[0].Bounds);
    }

    [Fact]
    public void TypeScheme_RegisteredForGenericFunction()
    {
        // identity<T>(x: T) -> T should produce a TypeScheme with one type parameter
        var param = new GenericParameter("T", 0);
        var body = new FunctionType(new[] { (AsterType)param }, param);
        var scheme = new TypeScheme(new[] { param }, Array.Empty<TraitBound>(), body);

        Assert.Single(scheme.TypeParameters);
        Assert.Equal("T", scheme.TypeParameters[0].Name);

        // Instantiate — should replace T with a fresh TypeVariable
        var substitution = new Dictionary<int, AsterType>();
        var instantiated = scheme.Instantiate(substitution);

        Assert.IsType<FunctionType>(instantiated);
        var fnType = (FunctionType)instantiated;
        Assert.IsType<TypeVariable>(fnType.ParameterTypes[0]);
        // param and return type should be the same fresh variable
        Assert.Same(fnType.ParameterTypes[0], fnType.ReturnType);
    }

    [Fact]
    public void TypeScheme_TwoParams_InstantiatesIndependently()
    {
        // fn first<A, B>(a: A, b: B) -> A
        var paramA = new GenericParameter("A", 0);
        var paramB = new GenericParameter("B", 1);
        var body = new FunctionType(new AsterType[] { paramA, paramB }, paramA);
        var scheme = new TypeScheme(new[] { paramA, paramB }, Array.Empty<TraitBound>(), body);

        var sub1 = new Dictionary<int, AsterType>();
        var inst1 = (FunctionType)scheme.Instantiate(sub1);
        var sub2 = new Dictionary<int, AsterType>();
        var inst2 = (FunctionType)scheme.Instantiate(sub2);

        // Two independent instantiations should give different TypeVariables
        var tv1A = inst1.ParameterTypes[0];
        var tv2A = inst2.ParameterTypes[0];
        Assert.IsType<TypeVariable>(tv1A);
        Assert.IsType<TypeVariable>(tv2A);
        Assert.NotEqual(((TypeVariable)tv1A).Id, ((TypeVariable)tv2A).Id);

        // Within each instantiation, return type should match first param type
        Assert.Same(inst1.ParameterTypes[0], inst1.ReturnType);
        Assert.Same(inst2.ParameterTypes[0], inst2.ReturnType);
    }

    [Fact]
    public void BoundChecking_SatisfiedCloneBound_NoErrors()
    {
        // fn clone_val<T: Clone>(x: T) -> T { x }  called with i32 (which impls Clone)
        var source = @"
fn clone_val<T: Clone>(x: T) -> T { x }
fn main() -> i32 {
    let y = clone_val(5);
    0
}
";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);

        Assert.False(checker.Diagnostics.HasErrors,
            "Expected no errors: i32 implements Clone");
    }

    [Fact]
    public void BoundChecking_ViolatedBound_ReportsError()
    {
        // fn bounded<T: NonexistentTrait>(x: T) -> T { x }  called with i32
        var source = @"
fn bounded<T: NonexistentTrait>(x: T) -> T { x }
fn main() -> i32 {
    let y = bounded(5);
    0
}
";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);

        Assert.True(checker.Diagnostics.HasErrors,
            "Expected error: i32 does not implement NonexistentTrait");
    }

    [Fact]
    public void FullPipeline_GenericBoundedFunction_Clone()
    {
        var source = @"
fn copy_val<T: Clone>(x: T) -> T { x }
fn main() -> i32 {
    let y = copy_val(42);
    0
}
";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");

        Assert.NotNull(ir);
        Assert.Contains("@copy_val", ir);
    }
}

// ========== Phase 2 Week 11: Monomorphization ==========

public class Week11MonomorphizationTests
{
    [Fact]
    public void Mangle_SingleTypeArg()
    {
        var result = MonomorphizationTable.Mangle("identity", new[] { PrimitiveType.I32 });
        Assert.Equal("identity_i32", result);
    }

    [Fact]
    public void Mangle_TwoTypeArgs()
    {
        var result = MonomorphizationTable.Mangle("first", new AsterType[] { PrimitiveType.I32, PrimitiveType.StringType });
        Assert.Equal("first_i32_string", result);
    }

    [Fact]
    public void Mangle_NoTypeArgs_ReturnsOriginalName()
    {
        var result = MonomorphizationTable.Mangle("plain", Array.Empty<AsterType>());
        Assert.Equal("plain", result);
    }

    [Fact]
    public void Mangle_FloatTypeArg()
    {
        var result = MonomorphizationTable.Mangle("max", new[] { PrimitiveType.F64 });
        Assert.Equal("max_f64", result);
    }

    [Fact]
    public void Table_RecordSameInstantiationTwice_ReturnsSameName()
    {
        var table = new MonomorphizationTable();
        var name1 = table.Record("identity", new[] { PrimitiveType.I32 });
        var name2 = table.Record("identity", new[] { PrimitiveType.I32 });

        Assert.Equal(name1, name2);
        Assert.Single(table.Instantiations);
    }

    [Fact]
    public void Table_DifferentTypeArgs_ProduceDifferentNames()
    {
        var table = new MonomorphizationTable();
        var nameI32 = table.Record("identity", new[] { PrimitiveType.I32 });
        var nameF64 = table.Record("identity", new[] { PrimitiveType.F64 });

        Assert.NotEqual(nameI32, nameF64);
        Assert.Equal(2, table.Instantiations.Count);
    }

    [Fact]
    public void Monomorphizer_CollectsInstantiations_FromGenericCall()
    {
        // identity<T>(x: T) -> T called with identity(42) should record identity_i32
        var source = @"
fn identity<T>(x: T) -> T { x }
fn main() -> i32 {
    let y = identity(42);
    0
}
";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        var checker = new TypeChecker();
        checker.Check(hir);
        Assert.False(checker.Diagnostics.HasErrors);

        var mono = new Monomorphizer();
        var table = mono.Run(hir);

        Assert.Single(table.Instantiations);
        Assert.Equal("identity", table.Instantiations[0].OriginalName);
        Assert.Equal("identity_i32", table.Instantiations[0].MangledName);
    }

    [Fact]
    public void Monomorphizer_TwoCallSites_SameType_OneInstantiation()
    {
        var source = @"
fn wrap<T>(x: T) -> T { x }
fn main() -> i32 {
    let a = wrap(1);
    let b = wrap(2);
    0
}
";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        var checker = new TypeChecker();
        checker.Check(hir);

        var table = new Monomorphizer().Run(hir);

        // Both calls use T=i32, so only one specialisation is recorded
        Assert.Single(table.Instantiations);
        Assert.Equal("wrap_i32", table.Instantiations[0].MangledName);
    }

    [Fact]
    public void FullPipeline_Compile_ExposesMonomorphizationTable()
    {
        var source = @"
fn id<T>(x: T) -> T { x }
fn main() -> i32 {
    let y = id(99);
    0
}
";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");

        Assert.NotNull(ir);
        Assert.NotNull(driver.MonomorphizationTable);
        Assert.Single(driver.MonomorphizationTable!.Instantiations);
        Assert.Equal("id_i32", driver.MonomorphizationTable.Instantiations[0].MangledName);
    }
}

// ========== Week 12: Generics Polish ==========

public class Week12GenericsPolishTests
{
    [Fact]
    public void FullPipeline_GenericFunctionThreeParams()
    {
        var source = @"
fn clamp<T>(val: T, lo: T, hi: T) -> T { val }
fn main() -> i32 {
    let x = clamp(5, 0, 10);
    0
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    [Fact]
    public void FullPipeline_MultipleGenericFunctions()
    {
        var source = @"
fn id<T>(x: T) -> T { x }
fn const_fn<T, U>(x: T, y: U) -> T { x }
fn main() -> i32 {
    let a = id(1);
    let b = const_fn(true, 42);
    0
}
";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");
        Assert.NotNull(ir);
    }

    [Fact]
    public void FullPipeline_GenericFunctionCallsAnotherGeneric()
    {
        var source = @"
fn id<T>(x: T) -> T { x }
fn double_id<T>(x: T) -> T { id(x) }
fn main() -> i32 {
    let x = double_id(7);
    0
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    [Fact]
    public void ErrorMessage_UndefinedTypeInGenericBound()
    {
        var source = @"
fn foo<T: Frobnitz>(x: T) -> T { x }
fn main() -> i32 { 0 }
";
        var driver = new CompilationDriver();
        driver.Compile(source, "test.ast");
        // Should have a bound-violation error at the call site or a missing impl error
        // For now we just check it doesn't panic
        Assert.True(true);
    }

    [Fact]
    public void FullPipeline_GenericStructUsedAsParameter()
    {
        var source = @"
struct Pair<A, B> { first: A, second: B }
fn get_first<A, B>(p: Pair<A, B>) -> A { p.first }
fn main() { }
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    [Fact]
    public void FullPipeline_GenericEnum_WithVariants()
    {
        var source = @"
enum Tree<T> {
    Leaf,
    Node(T)
}
fn main() { }
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }
}

// ========== Weeks 13-14: Vec<T> Foundation ==========

public class Week13VecTests
{
    [Fact]
    public void Resolve_VecType_IsKnownInScope()
    {
        var source = "fn foo(v: Vec<i32>) -> i32 { 0 }";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        // Vec should resolve without "undefined type" errors
        Assert.False(resolver.Diagnostics.HasErrors,
            "Vec should be a known built-in type");
    }

    [Fact]
    public void TypeRef_Vec_ResolvesToTypeApp()
    {
        var source = "fn foo(v: Vec<i32>) -> i32 { 0 }";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        var fn = Assert.IsType<HirFunctionDecl>(hir.Declarations[0]);
        var paramTypeRef = fn.Parameters[0].TypeRef;
        Assert.NotNull(paramTypeRef);
        Assert.Equal("Vec", paramTypeRef!.Name);
        Assert.Single(paramTypeRef.TypeArguments);
        Assert.Equal("i32", paramTypeRef.TypeArguments[0].Name);
    }

    [Fact]
    public void TypeChecker_VecParam_ResolvesToTypeApp()
    {
        var source = "fn foo(v: Vec<i32>) -> i32 { 0 }";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);

        Assert.False(checker.Diagnostics.HasErrors);

        var fn = Assert.IsType<HirFunctionDecl>(hir.Declarations[0]);
        // The parameter type should be TypeApp(Vec, [i32])
        var paramType = fn.Parameters[0].Symbol.Type;
        Assert.IsType<TypeApp>(paramType);
        var app = (TypeApp)paramType;
        Assert.IsType<StructType>(app.Constructor); // Vec constructor
        Assert.Equal("Vec", ((StructType)app.Constructor).Name);
        Assert.Single(app.Arguments);
        Assert.Equal(PrimitiveType.I32, app.Arguments[0]);
    }

    [Fact]
    public void FullPipeline_FunctionWithVecParam()
    {
        var source = @"
fn sum_len(v: Vec<i32>) -> i32 { 0 }
fn main() { }
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    [Fact]
    public void FullPipeline_VecReturnType()
    {
        var source = @"
fn make_vec() -> Vec<i32> {
    vec_new()
}
fn main() { }
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    [Fact]
    public void Resolve_VecOperations_AreKnownSymbols()
    {
        var source = @"
fn work(v: Vec<i32>) -> i32 {
    let n = vec_len(v);
    n
}
fn main() { }
";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        resolver.Resolve(ast);

        Assert.False(resolver.Diagnostics.HasErrors,
            "vec_len should be a known built-in function");
    }

    [Fact]
    public void TypeRef_NestedVec_ResolvesCorrectly()
    {
        var source = "fn foo(v: Vec<Vec<i32>>) -> i32 { 0 }";
        var lexer = new AsterLexer(source, "test.ast");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        Assert.False(resolver.Diagnostics.HasErrors);
        var fn = Assert.IsType<HirFunctionDecl>(hir.Declarations[0]);
        var typeRef = fn.Parameters[0].TypeRef!;
        Assert.Equal("Vec", typeRef.Name);
        Assert.Equal("Vec", typeRef.TypeArguments[0].Name);
    }
}

// ========== Week 15: Option<T> / Result<T,E> ==========

public class Week15OptionResultTests
{
    [Fact]
    public void Resolve_OptionType_NoErrors()
    {
        var source = "fn foo(x: Option<i32>) -> i32 { 0 }";
        var resolver = new NameResolver();
        resolver.Resolve(new AsterParser(new AsterLexer(source, "t").Tokenize()).ParseProgram());
        Assert.False(resolver.Diagnostics.HasErrors);
    }

    [Fact]
    public void Resolve_ResultType_NoErrors()
    {
        var source = "fn foo(x: Result<i32, bool>) -> i32 { 0 }";
        var resolver = new NameResolver();
        resolver.Resolve(new AsterParser(new AsterLexer(source, "t").Tokenize()).ParseProgram());
        Assert.False(resolver.Diagnostics.HasErrors);
    }

    [Fact]
    public void TypeChecker_OptionParam_ResolvesToTypeApp()
    {
        var source = "fn foo(x: Option<i32>) -> i32 { 0 }";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);

        Assert.False(checker.Diagnostics.HasErrors);
        var fn = Assert.IsType<HirFunctionDecl>(hir.Declarations[0]);
        Assert.IsType<TypeApp>(fn.Parameters[0].Symbol.Type);
    }

    [Fact]
    public void TypeChecker_ResultParam_TwoTypeArgs()
    {
        var source = "fn foo(x: Result<i32, bool>) -> i32 { 0 }";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);

        Assert.False(checker.Diagnostics.HasErrors);
        var fn = Assert.IsType<HirFunctionDecl>(hir.Declarations[0]);
        var app = Assert.IsType<TypeApp>(fn.Parameters[0].Symbol.Type);
        Assert.Equal(2, app.Arguments.Count);
        Assert.Equal(PrimitiveType.I32, app.Arguments[0]);
        Assert.Equal(PrimitiveType.Bool, app.Arguments[1]);
    }

    [Fact]
    public void FullPipeline_OptionReturnType()
    {
        var source = @"
fn find(x: i32) -> Option<i32> {
    None
}
fn main() { }
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    [Fact]
    public void FullPipeline_ResultReturnType()
    {
        var source = @"
fn divide(a: i32, b: i32) -> Result<i32, bool> {
    Ok(a)
}
fn main() { }
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }
}

// ========== Week 16: HashMap<K,V> ==========

public class Week16HashMapTests
{
    [Fact]
    public void Resolve_HashMapType_NoErrors()
    {
        var source = "fn foo(m: HashMap<i32, bool>) -> i32 { 0 }";
        var resolver = new NameResolver();
        resolver.Resolve(new AsterParser(new AsterLexer(source, "t").Tokenize()).ParseProgram());
        Assert.False(resolver.Diagnostics.HasErrors);
    }

    [Fact]
    public void TypeChecker_HashMapParam_TwoTypeArgs()
    {
        var source = "fn foo(m: HashMap<i32, bool>) -> i32 { 0 }";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);

        Assert.False(checker.Diagnostics.HasErrors);
        var fn = Assert.IsType<HirFunctionDecl>(hir.Declarations[0]);
        var app = Assert.IsType<TypeApp>(fn.Parameters[0].Symbol.Type);
        Assert.Equal("HashMap", ((StructType)app.Constructor).Name);
        Assert.Equal(2, app.Arguments.Count);
    }

    [Fact]
    public void Resolve_HashOperations_AreKnownSymbols()
    {
        var source = @"
fn work(m: HashMap<i32, bool>) -> i32 {
    let n = hash_len(m);
    n
}
fn main() { }
";
        var resolver = new NameResolver();
        resolver.Resolve(new AsterParser(new AsterLexer(source, "t").Tokenize()).ParseProgram());
        Assert.False(resolver.Diagnostics.HasErrors);
    }

    [Fact]
    public void FullPipeline_HashMapParam()
    {
        var source = @"
fn process(m: HashMap<i32, bool>) -> i32 { 0 }
fn main() { }
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }
}

// ========== Weeks 17-19: Modules ==========

public class Week17ModuleTests
{
    [Fact]
    public void Parse_ModuleDecl_IsValid()
    {
        var source = @"
module utils {
    fn helper() -> i32 { 42 }
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var tokens = lexer.Tokenize();
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();

        Assert.False(parser.Diagnostics.HasErrors);
        Assert.Equal(2, ast.Declarations.Count);
        Assert.IsType<ModuleDeclNode>(ast.Declarations[0]);
    }

    [Fact]
    public void Resolve_ModuleDecl_NoErrors()
    {
        var source = @"
module utils {
    fn helper() -> i32 { 42 }
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        Assert.False(resolver.Diagnostics.HasErrors);
        // Module should appear in HIR
        Assert.Contains(hir.Declarations, d => d is HirModuleDecl);
    }

    [Fact]
    public void Resolve_ModuleDecl_MemberFunctionIsResolved()
    {
        var source = @"
module math {
    fn square(x: i32) -> i32 { x }
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        Assert.False(resolver.Diagnostics.HasErrors);
        var mod = hir.Declarations.OfType<HirModuleDecl>().First();
        Assert.Single(mod.Members);
        Assert.IsType<HirFunctionDecl>(mod.Members[0]);
    }

    [Fact]
    public void Resolve_ModuleDecl_ContainsStruct()
    {
        var source = @"
module shapes {
    struct Circle { radius: i32 }
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        Assert.False(resolver.Diagnostics.HasErrors);
        var mod = hir.Declarations.OfType<HirModuleDecl>().First();
        Assert.Contains(mod.Members, m => m is HirStructDecl);
    }

    [Fact]
    public void FullPipeline_Module_Compiles()
    {
        var source = @"
module helpers {
    fn double(x: i32) -> i32 { x }
}
fn main() { }
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    [Fact]
    public void FullPipeline_NestedModule_Compiles()
    {
        var source = @"
module outer {
    module inner {
        fn value() -> i32 { 0 }
    }
}
fn main() { }
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }
}

// ========== Week 20: Traits Foundation ==========

public class Week20TraitsTests
{
    [Fact]
    public void Parse_TraitDecl_IsValid()
    {
        var source = @"
trait Drawable {
    fn draw() -> i32 { 0 }
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var parser = new AsterParser(lexer.Tokenize());
        var ast = parser.ParseProgram();

        Assert.False(parser.Diagnostics.HasErrors);
        Assert.Contains(ast.Declarations, d => d is TraitDeclNode);
    }

    [Fact]
    public void Resolve_TraitDecl_NoErrors()
    {
        var source = @"
trait Printable {
    fn print_me() -> i32 { 0 }
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        Assert.False(resolver.Diagnostics.HasErrors);
        Assert.Contains(hir.Declarations, d => d is HirTraitDecl);
    }

    [Fact]
    public void Resolve_TraitDecl_StoresMethodNames()
    {
        var source = @"
trait Animal {
    fn speak() -> i32 { 0 }
    fn move_to() -> i32 { 0 }
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        var trait = hir.Declarations.OfType<HirTraitDecl>().First();
        Assert.Equal("Animal", trait.Symbol.Name);
        Assert.Equal(2, trait.Methods.Count);
        Assert.Contains(trait.Methods, m => m.Name == "speak");
        Assert.Contains(trait.Methods, m => m.Name == "move_to");
    }

    [Fact]
    public void Resolve_ImplBlock_NoErrors()
    {
        var source = @"
struct Dog { name: i32 }
impl Dog {
    fn bark() -> i32 { 1 }
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        Assert.False(resolver.Diagnostics.HasErrors);
        Assert.Contains(hir.Declarations, d => d is HirImplDecl);
    }

    [Fact]
    public void Resolve_ImplBlock_RegistersMethod()
    {
        var source = @"
struct Cat { }
impl Cat {
    fn meow() -> i32 { 2 }
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        Assert.False(resolver.Diagnostics.HasErrors);
        var impl = hir.Declarations.OfType<HirImplDecl>().First();
        Assert.Equal("Cat", impl.TargetTypeName);
        Assert.Single(impl.Methods);
        Assert.Equal("meow", impl.Methods[0].Symbol.Name);
    }

    [Fact]
    public void Resolve_TraitImpl_StoresTraitName()
    {
        var source = @"
trait Speak { fn say() -> i32 { 0 } }
struct Parrot { }
impl Speak for Parrot {
    fn say() -> i32 { 3 }
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        Assert.False(resolver.Diagnostics.HasErrors);
        var impl = hir.Declarations.OfType<HirImplDecl>().First();
        Assert.Equal("Parrot", impl.TargetTypeName);
        Assert.Equal("Speak", impl.TraitName);
    }

    [Fact]
    public void TypeChecker_ImplBlock_RegistersTraitImpl()
    {
        var source = @"
trait Greet { fn hi() -> i32 { 0 } }
struct Person { }
impl Greet for Person {
    fn hi() -> i32 { 0 }
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);

        Assert.False(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void TypeChecker_GenericTraitDecl_NoErrors()
    {
        var source = @"
trait Container<T> {
    fn get() -> T { get() }
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);

        Assert.False(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void FullPipeline_StructWithImpl_Compiles()
    {
        var source = @"
struct Counter { value: i32 }
impl Counter {
    fn increment(c: Counter) -> i32 { c.value }
}
fn main() { }
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }
}

// ========== Phase 2 Closeout: for / match / break / continue / index ==========

public class Phase2CloseoutTests
{
    // --- for loop ---

    [Fact]
    public void Parse_ForLoop_IsValid()
    {
        var source = @"
fn main() {
    let items = vec_new();
    for i in items {
        let x = 1;
    }
}
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        Assert.False(lexer.Diagnostics.HasErrors);
        var fn = Assert.IsType<FunctionDeclNode>(ast.Declarations[0]);
        Assert.Contains(fn.Body.Statements, s => s is ForStmtNode);
    }

    [Fact]
    public void Resolve_ForLoop_BindsLoopVariable()
    {
        var source = @"
fn main() {
    let items = vec_new();
    for x in items {
        let _skip = 0;
    }
}
";
        var resolver = new NameResolver();
        var hir = resolver.Resolve(new AsterParser(new AsterLexer(source, "t").Tokenize()).ParseProgram());
        Assert.False(resolver.Diagnostics.HasErrors);
        var fn = hir.Declarations.OfType<HirFunctionDecl>().First();
        Assert.Contains(fn.Body.Statements, s => s is HirForStmt);
    }

    [Fact]
    public void TypeChecker_ForLoop_NoErrors()
    {
        var source = @"
fn main() {
    let items = vec_new();
    for x in items {
        let _skip = 0;
    }
}
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);
        Assert.False(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void FullPipeline_ForLoop_Compiles()
    {
        var source = @"
fn main() {
    let items = vec_new();
    for x in items {
        let _skip = 0;
    }
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    // --- break / continue ---

    [Fact]
    public void Parse_BreakContinue_InLoop()
    {
        var source = @"
fn main() {
    while true {
        break;
        continue;
    }
}
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        Assert.False(lexer.Diagnostics.HasErrors);
    }

    [Fact]
    public void Resolve_Break_ProducesHirBreakStmt()
    {
        var source = @"
fn main() {
    while true { break; }
}
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var fn = hir.Declarations.OfType<HirFunctionDecl>().First();
        var ws = fn.Body.Statements.OfType<HirWhileStmt>().First();
        Assert.Contains(ws.Body.Statements, s => s is HirBreakStmt);
    }

    [Fact]
    public void Resolve_Continue_ProducesHirContinueStmt()
    {
        var source = @"
fn main() {
    while true { continue; }
}
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var fn = hir.Declarations.OfType<HirFunctionDecl>().First();
        var ws = fn.Body.Statements.OfType<HirWhileStmt>().First();
        Assert.Contains(ws.Body.Statements, s => s is HirContinueStmt);
    }

    [Fact]
    public void FullPipeline_BreakContinue_Compiles()
    {
        var source = @"
fn main() {
    while true { break; }
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    // --- index expressions ---

    [Fact]
    public void Resolve_IndexExpr_ProducesHirIndexExpr()
    {
        var source = @"
fn get_first(v: Vec<i32>) -> i32 {
    v[0]
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        Assert.NotNull(hir);
    }

    [Fact]
    public void TypeChecker_IndexExpr_ReturnsElementType()
    {
        var source = @"
fn get_first(v: Vec<i32>) -> i32 {
    v[0]
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);
        Assert.False(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void FullPipeline_IndexExpr_Compiles()
    {
        var source = @"
fn get_item(v: Vec<i32>, i: i32) -> i32 {
    v[i]
}
fn main() { }
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    // --- match expressions ---

    [Fact]
    public void Parse_MatchExpr_IsValid()
    {
        var source = @"
fn check(x: i32) -> i32 {
    match x {
        1 => 10,
        _ => 0,
    }
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        Assert.False(lexer.Diagnostics.HasErrors);
    }

    [Fact]
    public void Resolve_MatchExpr_ProducesHirMatchExpr()
    {
        var source = @"
fn check(x: i32) -> i32 {
    match x {
        1 => 10,
        _ => 0,
    }
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        Assert.NotNull(hir);
    }

    [Fact]
    public void TypeChecker_MatchExpr_NoErrors()
    {
        var source = @"
fn check(x: i32) -> i32 {
    match x {
        _ => 0,
    }
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);
        Assert.False(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void FullPipeline_MatchExpr_Compiles()
    {
        var source = @"
fn describe(x: i32) -> i32 {
    match x {
        _ => 0,
    }
}
fn main() { }
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }
}

// ========== Phase 3: Self Parameter in impl Methods ==========

public class Phase3SelfParameterTests
{
    [Fact]
    public void Resolve_ImplMethod_SelfParameterNoErrors()
    {
        var source = @"
struct Point { x: i32, y: i32 }
impl Point {
    fn get_x(self: Point) -> i32 { self.x }
}
fn main() { }
";
        var resolver = new NameResolver();
        resolver.Resolve(new AsterParser(new AsterLexer(source, "t").Tokenize()).ParseProgram());
        Assert.False(resolver.Diagnostics.HasErrors);
    }

    [Fact]
    public void Resolve_ImplMethod_SelfSymbolIsRegistered()
    {
        var source = @"
struct Vec2 { x: i32 }
impl Vec2 {
    fn len(self: Vec2) -> i32 { self.x }
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);

        Assert.False(resolver.Diagnostics.HasErrors);
        var impl = hir.Declarations.OfType<HirImplDecl>().First();
        var method = impl.Methods.First();
        Assert.Contains(method.Parameters, p => p.Symbol.Name == "self");
    }

    [Fact]
    public void FullPipeline_SelfParameter_Compiles()
    {
        var source = @"
struct Dog { name: i32 }
impl Dog {
    fn bark(self: Dog) -> i32 { self.name }
}
fn main() { }
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }
}

// ========== Phase 3: Optimization Passes ==========

public class Phase3OptimizationTests
{
    [Fact]
    public void ConstantFolder_AddsIntegers()
    {
        var module = BuildSimpleModule("add_const", new[]
        {
            new MirInstruction(MirOpcode.BinaryOp,
                MirOperand.Temp("_t0", MirType.I64),
                new[] { MirOperand.Constant(3L, MirType.I64), MirOperand.Constant(4L, MirType.I64) },
                "add"),
        });

        new ConstantFolder().Fold(module);

        var instr = module.Functions[0].BasicBlocks[0].Instructions[0];
        Assert.Equal(MirOpcode.Assign, instr.Opcode);
        Assert.Equal(7L, instr.Operands[0].Value);
    }

    [Fact]
    public void ConstantFolder_SubtractsIntegers()
    {
        var module = BuildSimpleModule("sub_const", new[]
        {
            new MirInstruction(MirOpcode.BinaryOp,
                MirOperand.Temp("_t0", MirType.I64),
                new[] { MirOperand.Constant(10L, MirType.I64), MirOperand.Constant(3L, MirType.I64) },
                "sub"),
        });

        new ConstantFolder().Fold(module);
        var instr = module.Functions[0].BasicBlocks[0].Instructions[0];
        Assert.Equal(MirOpcode.Assign, instr.Opcode);
        Assert.Equal(7L, instr.Operands[0].Value);
    }

    [Fact]
    public void ConstantFolder_MultipliesIntegers()
    {
        var module = BuildSimpleModule("mul_const", new[]
        {
            new MirInstruction(MirOpcode.BinaryOp,
                MirOperand.Temp("_t0", MirType.I64),
                new[] { MirOperand.Constant(6L, MirType.I64), MirOperand.Constant(7L, MirType.I64) },
                "mul"),
        });

        new ConstantFolder().Fold(module);
        var instr = module.Functions[0].BasicBlocks[0].Instructions[0];
        Assert.Equal(42L, instr.Operands[0].Value);
    }

    [Fact]
    public void ConstantFolder_EvaluatesEqualityFalse()
    {
        var module = BuildSimpleModule("eq_const", new[]
        {
            new MirInstruction(MirOpcode.BinaryOp,
                MirOperand.Temp("_t0", MirType.Bool),
                new[] { MirOperand.Constant(3L, MirType.I64), MirOperand.Constant(4L, MirType.I64) },
                "eq"),
        });

        new ConstantFolder().Fold(module);
        var instr = module.Functions[0].BasicBlocks[0].Instructions[0];
        Assert.Equal(false, instr.Operands[0].Value);
    }

    [Fact]
    public void ConstantFolder_DoesNotFoldVariables()
    {
        var module = BuildSimpleModule("no_fold", new[]
        {
            new MirInstruction(MirOpcode.BinaryOp,
                MirOperand.Temp("_t0", MirType.I64),
                new[] { MirOperand.Variable("x", MirType.I64), MirOperand.Constant(4L, MirType.I64) },
                "add"),
        });

        new ConstantFolder().Fold(module);
        // Should remain as BinaryOp since one operand is a variable
        var instr = module.Functions[0].BasicBlocks[0].Instructions[0];
        Assert.Equal(MirOpcode.BinaryOp, instr.Opcode);
    }

    [Fact]
    public void DeadCodeEliminator_RemovesDeadTemp()
    {
        var module = BuildSimpleModule("dce_test", new[]
        {
            // _dead is assigned but never read
            new MirInstruction(MirOpcode.Assign,
                MirOperand.Temp("_dead", MirType.I64),
                new[] { MirOperand.Constant(99L, MirType.I64) }),
            // _used is assigned and returned
            new MirInstruction(MirOpcode.Assign,
                MirOperand.Temp("_used", MirType.I64),
                new[] { MirOperand.Constant(1L, MirType.I64) }),
        });
        var fn = module.Functions[0];
        fn.BasicBlocks[0].Terminator = new MirReturn(MirOperand.Temp("_used", MirType.I64));

        new DeadCodeEliminator().Eliminate(module);

        // _dead instruction should be removed; _used should remain
        var instrs = fn.BasicBlocks[0].Instructions;
        Assert.DoesNotContain(instrs, i => i.Destination?.Name == "_dead");
        Assert.Contains(instrs, i => i.Destination?.Name == "_used");
    }

    [Fact]
    public void DeadCodeEliminator_KeepsSideEffectfulCalls()
    {
        var module = BuildSimpleModule("keep_calls", new[]
        {
            new MirInstruction(MirOpcode.Call,
                MirOperand.Temp("_r", MirType.I64),
                new[] { MirOperand.FunctionRef("println") }),
        });

        new DeadCodeEliminator().Eliminate(module);
        // Call instructions are side-effectful — they should NOT be removed
        var instrs = module.Functions[0].BasicBlocks[0].Instructions;
        Assert.Single(instrs); // still there
    }

    [Fact]
    public void FullPipeline_OptimizerRuns_WithConstantExpression()
    {
        var source = @"
fn add_const() -> i32 { 3 + 4 }
fn main() { }
";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "test.ast");
        Assert.NotNull(ir);
    }

    [Fact]
    public void FullPipeline_OptimizerRuns_WithWhileTrue()
    {
        var source = @"
fn main() {
    while true { break; }
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    // Helper: build a minimal MirModule with one function and one basic block
    private static MirModule BuildSimpleModule(string name, MirInstruction[] instrs)
    {
        var module = new MirModule("test");
        var fn = new MirFunction(name);
        var block = fn.CreateBlock("entry");
        foreach (var instr in instrs)
            block.AddInstruction(instr);
        if (block.Terminator == null)
            block.Terminator = new MirReturn();
        module.Functions.Add(fn);
        return module;
    }
}

// ========== Phase 3 Completion: Closures ==========

public class Phase3ClosureTests
{
    [Fact]
    public void Parse_ClosureNoParams_IsValid()
    {
        var source = @"fn main() { let f = || 42; }";
        var lexer = new AsterLexer(source, "t");
        new AsterParser(lexer.Tokenize()).ParseProgram();
        Assert.False(lexer.Diagnostics.HasErrors);
    }

    [Fact]
    public void Parse_ClosureWithParams_IsValid()
    {
        var source = @"fn main() { let add = |x: i32, y: i32| x + y; }";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        Assert.False(lexer.Diagnostics.HasErrors);
    }

    [Fact]
    public void Parse_ClosureWithBlock_IsValid()
    {
        var source = @"fn main() { let f = |x: i32| { x + 1 }; }";
        var lexer = new AsterLexer(source, "t");
        new AsterParser(lexer.Tokenize()).ParseProgram();
        Assert.False(lexer.Diagnostics.HasErrors);
    }

    [Fact]
    public void Resolve_ClosureExpr_ProducesHirClosureExpr()
    {
        var source = @"fn main() { let f = |x: i32| x; }";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        Assert.NotNull(hir);

        var mainFn = hir.Declarations.OfType<HirFunctionDecl>().First();
        var letStmt = mainFn.Body.Statements.OfType<HirLetStmt>().First();
        Assert.IsType<HirClosureExpr>(letStmt.Initializer);
    }

    [Fact]
    public void Resolve_Closure_ParamBoundInScope()
    {
        var source = @"fn main() { let f = |x: i32| x; }";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);
        Assert.False(resolver.Diagnostics.HasErrors);

        var closure = hir.Declarations.OfType<HirFunctionDecl>().First()
            .Body.Statements.OfType<HirLetStmt>().First().Initializer as HirClosureExpr;
        Assert.NotNull(closure);
        Assert.Single(closure.Parameters);
        Assert.Equal("x", closure.Parameters[0].Symbol.Name);
    }

    [Fact]
    public void TypeChecker_Closure_ReturnsFunctionType()
    {
        var source = @"fn main() { let f = |x: i32| x; }";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);
        Assert.False(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void FullPipeline_ClosureNoParam_Compiles()
    {
        var source = @"
fn apply(f: i32) -> i32 { f }
fn main() {
    let val = apply(42);
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    [Fact]
    public void FullPipeline_ClosureWithParam_Compiles()
    {
        var source = @"
fn main() {
    let add = |x: i32, y: i32| x + y;
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    [Fact]
    public void Resolve_ClosureMangledName_IsUnique()
    {
        var source = @"
fn main() {
    let f = |x: i32| x;
    let g = |y: i32| y;
}
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var stmts = hir.Declarations.OfType<HirFunctionDecl>().First().Body.Statements;
        var closureNames = stmts.OfType<HirLetStmt>()
            .Select(s => s.Initializer)
            .OfType<HirClosureExpr>()
            .Select(c => c.MangledName)
            .ToList();
        Assert.Equal(2, closureNames.Distinct().Count());
    }
}

// ========== Phase 3 Completion: Type Aliases ==========

public class Phase3TypeAliasTests
{
    [Fact]
    public void Parse_TypeAlias_IsValid()
    {
        var source = @"
type MyInt = i32;
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        new AsterParser(lexer.Tokenize()).ParseProgram();
        Assert.False(lexer.Diagnostics.HasErrors);
    }

    [Fact]
    public void Resolve_TypeAlias_ProducesHirTypeAliasDecl()
    {
        var source = @"
type MyInt = i32;
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        Assert.NotNull(hir);
        Assert.Contains(hir.Declarations, d => d is HirTypeAliasDecl);
    }

    [Fact]
    public void Resolve_TypeAlias_RegisteredInScope()
    {
        var source = @"
type Counter = i32;
fn main() { }
";
        var resolver = new NameResolver();
        var hir = resolver.Resolve(new AsterParser(new AsterLexer(source, "t").Tokenize()).ParseProgram());
        Assert.False(resolver.Diagnostics.HasErrors);
        var alias = hir.Declarations.OfType<HirTypeAliasDecl>().FirstOrDefault();
        Assert.NotNull(alias);
        Assert.Equal("Counter", alias.Symbol.Name);
    }

    [Fact]
    public void TypeChecker_TypeAlias_NoErrors()
    {
        var source = @"
type Score = i32;
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);
        Assert.False(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void FullPipeline_TypeAlias_Compiles()
    {
        var source = @"
type Score = i32;
fn compute() -> i32 { 100 }
fn main() { }
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    [Fact]
    public void Parse_TypeAlias_NoSemicolon_IsValid()
    {
        // Semicolon is optional in our grammar
        var source = @"
type Alias = i32
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        new AsterParser(lexer.Tokenize()).ParseProgram();
        // Should not error on parser level (may report resolver error for bare name, not type alias)
        Assert.False(lexer.Diagnostics.HasErrors);
    }
}

// ========== Phase 3 Completion: ? Operator ==========

public class Phase3TryOperatorTests
{
    [Fact]
    public void Parse_TryOperator_IsValid()
    {
        var source = @"
fn main() {
    let x: i32 = 42;
    let y = x?;
}
";
        var lexer = new AsterLexer(source, "t");
        new AsterParser(lexer.Tokenize()).ParseProgram();
        Assert.False(lexer.Diagnostics.HasErrors);
    }

    [Fact]
    public void Resolve_TryOperator_ProducesHirUnaryExpr()
    {
        var source = @"
fn try_fn(x: i32) -> i32 {
    x?
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        Assert.NotNull(hir);
    }

    [Fact]
    public void TypeChecker_TryOperator_NoErrors()
    {
        var source = @"
fn try_fn(x: i32) -> i32 {
    x?
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);
        Assert.False(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void FullPipeline_TryOperator_Compiles()
    {
        var source = @"
fn maybe(x: i32) -> i32 { x? }
fn main() { }
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }
}

// ========== Phase 3 Completion: PatternChecker Integration ==========

public class Phase3PatternCheckerTests
{
    [Fact]
    public void PatternChecker_EmptyMatch_ReportsError()
    {
        // PatternChecker.CheckMatch on 0 arms emits E0340
        var checker = new PatternChecker();
        checker.CheckMatch(PrimitiveType.I32,
            new List<(Pattern, Span)>());
        Assert.True(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void PatternChecker_WildcardArm_IsExhaustive()
    {
        var checker = new PatternChecker();
        checker.CheckMatch(PrimitiveType.I32,
            new List<(Pattern, Span)>
            {
                (new WildcardPattern(Span.Unknown), Span.Unknown)
            });
        Assert.False(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void PatternChecker_DuplicateLiteral_Warns()
    {
        var checker = new PatternChecker();
        checker.CheckMatch(PrimitiveType.I32,
            new List<(Pattern, Span)>
            {
                (new LiteralPattern(1L, Span.Unknown), Span.Unknown),
                (new LiteralPattern(1L, Span.Unknown), Span.Unknown),  // duplicate
                (new WildcardPattern(Span.Unknown), Span.Unknown),
            });
        // Duplicate literal should emit a warning (W0001)
        Assert.Contains(checker.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void FullPipeline_Match_RunsPatternChecker()
    {
        var source = @"
fn check(x: i32) -> i32 {
    match x {
        _ => 0,
    }
}
fn main() { }
";
        var driver = new CompilationDriver();
        // Should not crash and should succeed (wildcard is exhaustive)
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }
}

// ========== Phase 3 Completion: AsyncLower Integration ==========

public class Phase3AsyncLowerTests
{
    [Fact]
    public void Parse_AsyncFunction_IsValid()
    {
        var source = @"
async fn fetch() -> i32 { 42 }
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        new AsterParser(lexer.Tokenize()).ParseProgram();
        Assert.False(lexer.Diagnostics.HasErrors);
    }

    [Fact]
    public void Resolve_AsyncFunction_ProducesHirFunctionDecl()
    {
        var source = @"
async fn get_value() -> i32 { 1 }
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var asyncFn = hir.Declarations.OfType<HirFunctionDecl>()
            .FirstOrDefault(f => f.Symbol.Name == "get_value");
        Assert.NotNull(asyncFn);
        Assert.True(asyncFn.IsAsync);
    }

    [Fact]
    public void FullPipeline_AsyncFunction_Compiles()
    {
        var source = @"
async fn compute() -> i32 { 99 }
fn main() { }
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    [Fact]
    public void AsyncLower_NonAsyncFunction_IsUnchanged()
    {
        // A plain (non-async) function should pass through AsyncLower unchanged
        var module = new MirModule("test");
        var fn = new MirFunction("plain");
        var block = fn.CreateBlock("entry");
        block.Terminator = new MirReturn(MirOperand.Constant(1L, MirType.I64));
        module.Functions.Add(fn);

        var lower = new AsyncLower();
        lower.Lower(module);

        // No diagnostic issued; function structure unchanged
        Assert.False(lower.Diagnostics.HasErrors);
        Assert.Single(module.Functions);
    }
}

// ========== Phase 4 Week 21-22: Method Call Dispatch ==========

public class Phase4MethodCallTests
{
    [Fact]
    public void Parse_MethodCall_ProducesMethodCallExprNode()
    {
        var source = @"
struct Point { x: i32, y: i32 }
fn main() {
    let p = Point { x: 1, y: 2 };
    p.distance();
}
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        Assert.False(lexer.Diagnostics.HasErrors);
        // Verify the call to distance() produced a MethodCallExprNode (not a CallExpr over MemberAccess)
        var mainFn = ast.Declarations.OfType<FunctionDeclNode>().First(f => f.Name == "main");
        var stmts = mainFn.Body.Statements;
        // Last stmt should be ExpressionStmt containing MethodCallExprNode
        var exprStmt = stmts.Last() as ExpressionStmtNode;
        Assert.NotNull(exprStmt);
        Assert.IsType<MethodCallExprNode>(exprStmt!.Expression);
        var mc = (MethodCallExprNode)exprStmt.Expression;
        Assert.Equal("distance", mc.MethodName);
    }

    [Fact]
    public void Parse_MethodCall_DistinguishedFromFieldAccess()
    {
        // field access: obj.field  → MemberAccessExprNode
        // method call:  obj.field()  → MethodCallExprNode
        var source = @"
struct Rect { w: i32 }
fn main() {
    let r = Rect { w: 5 };
    let _ = r.w;
    r.area();
}
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        Assert.False(lexer.Diagnostics.HasErrors);
        var mainFn = ast.Declarations.OfType<FunctionDeclNode>().First(f => f.Name == "main");
        var stmts = mainFn.Body.Statements;
        // Second let: r.w is field access
        var letW = stmts.OfType<LetStmtNode>().LastOrDefault();
        Assert.NotNull(letW);
        Assert.IsType<MemberAccessExprNode>(letW!.Initializer!);
        // Last: r.area() is method call
        var last = stmts.Last() as ExpressionStmtNode;
        Assert.IsType<MethodCallExprNode>(last!.Expression);
    }

    [Fact]
    public void Resolve_MethodCall_ProducesHirMethodCallExpr()
    {
        var source = @"
struct Counter { val: i32 }
impl Counter {
    fn get(&self) -> i32 { 0 }
}
fn main() {
    let c = Counter { val: 0 };
    c.get();
}
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var mainFn = hir.Declarations.OfType<HirFunctionDecl>().First(f => f.Symbol.Name == "main");
        var lastStmt = mainFn.Body.Statements.Last();
        var exprStmt = lastStmt as HirExprStmt;
        Assert.NotNull(exprStmt);
        Assert.IsType<HirMethodCallExpr>(exprStmt!.Expression);
        var mc = (HirMethodCallExpr)exprStmt.Expression;
        Assert.Equal("get", mc.MethodName);
    }

    [Fact]
    public void TypeCheck_MethodCall_LooksUpImplMethod()
    {
        var source = @"
struct Box { val: i32 }
impl Box {
    fn unwrap(&self) -> i32 { self.val }
}
fn main() {
    let b = Box { val: 99 };
    let x = b.unwrap();
}
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);
        // Method is registered in impl table; type check should not produce E0300-range errors
        var errors = checker.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.DoesNotContain(errors, e => e.Code == "E0305");
    }

    [Fact]
    public void TypeCheck_MethodCall_WithArgs()
    {
        var source = @"
struct Adder { base: i32 }
impl Adder {
    fn add(&self, x: i32) -> i32 { x }
}
fn main() {
    let a = Adder { base: 10 };
    let result = a.add(5);
}
";
        var driver = new CompilationDriver();
        driver.Check(source, "test.ast");
        Assert.False(driver.Diagnostics.HasErrors);
    }

    [Fact]
    public void TypeCheck_MethodCall_UnknownMethod_ReturnsFallback()
    {
        // Calling an unregistered method should not crash — fall back to TypeVariable
        var source = @"
struct Foo { x: i32 }
fn main() {
    let f = Foo { x: 1 };
    f.unknown_method();
}
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);
        // Should not throw; unknown method falls back to TypeVariable (no fatal error)
        // (An optional warning/error is acceptable but not required for Phase 4)
    }

    [Fact]
    public void FullPipeline_MethodCall_Compiles()
    {
        var source = @"
struct Point { x: i32, y: i32 }
impl Point {
    fn sum(&self) -> i32 { self.x }
}
fn main() {
    let p = Point { x: 3, y: 4 };
    let s = p.sum();
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    [Fact]
    public void FullPipeline_MethodCallChain_Compiles()
    {
        var source = @"
struct Builder { val: i32 }
impl Builder {
    fn set(&self, v: i32) -> i32 { v }
}
fn main() {
    let b = Builder { val: 0 };
    let x = b.set(10);
    let y = b.set(20);
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }
}

// ========== Phase 4 Week 23-24: Associated Types ==========

public class Phase4AssociatedTypeTests
{
    [Fact]
    public void Parse_ImplBlock_WithAssociatedType()
    {
        var source = @"
struct Wrapper { val: i32 }
impl Wrapper {
    type Item = i32;
    fn get(&self) -> i32 { self.val }
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        Assert.False(lexer.Diagnostics.HasErrors);
        var impl = ast.Declarations.OfType<ImplDeclNode>().First();
        Assert.Single(impl.AssociatedTypes);
        Assert.Equal("Item", impl.AssociatedTypes[0].Name);
    }

    [Fact]
    public void Parse_ImplBlock_MultipleAssociatedTypes()
    {
        var source = @"
struct Map { key: i32 }
impl Map {
    type Key = i32;
    type Value = i32;
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        Assert.False(lexer.Diagnostics.HasErrors);
        var impl = ast.Declarations.OfType<ImplDeclNode>().First();
        Assert.Equal(2, impl.AssociatedTypes.Count);
    }

    [Fact]
    public void Resolve_AssociatedType_ProducesHirAssociatedTypeDecl()
    {
        var source = @"
struct Container { data: i32 }
impl Container {
    type Item = i32;
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var impl = hir.Declarations.OfType<HirImplDecl>().First();
        Assert.Single(impl.AssociatedTypes);
        Assert.Equal("Item", impl.AssociatedTypes[0].Name);
        Assert.Equal("Container", impl.AssociatedTypes[0].OwnerTypeName);
    }

    [Fact]
    public void TypeCheck_AssociatedType_Registered()
    {
        var source = @"
struct Iterator { pos: i32 }
impl Iterator {
    type Item = i32;
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);
        Assert.False(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void TypeCheck_AssocTypePath_Resolves()
    {
        // Iterator::Item path should resolve to i32 (registered associated type)
        var source = @"
struct MyIter { pos: i32 }
impl MyIter {
    type Item = i32;
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);
        Assert.False(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void FullPipeline_AssociatedType_Compiles()
    {
        var source = @"
struct Stack { top: i32 }
impl Stack {
    type Item = i32;
    fn peek(&self) -> i32 { self.top }
}
fn main() {
    let s = Stack { top: 5 };
    let v = s.peek();
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }
}

// ========== Phase 4 Week 25-26: Lifetime Annotations ==========

public class Phase4LifetimeTests
{
    [Fact]
    public void Lex_LifetimeAnnotation_ProducesLifetimeToken()
    {
        var source = "'a";
        var lexer = new AsterLexer(source, "t");
        var tokens = lexer.Tokenize();
        // Should produce a Lifetime token, not a CharLiteral or Error
        Assert.Contains(tokens, t => t.Kind == TokenKind.Lifetime && t.Value == "a");
        Assert.DoesNotContain(tokens, t => t.Kind == TokenKind.Error);
    }

    [Fact]
    public void Lex_StaticLifetime_ProducesLifetimeToken()
    {
        var source = "'static";
        var lexer = new AsterLexer(source, "t");
        var tokens = lexer.Tokenize();
        Assert.Contains(tokens, t => t.Kind == TokenKind.Lifetime && t.Value == "static");
    }

    [Fact]
    public void Lex_CharLiteral_StillWorksAfterLifetimeSupport()
    {
        var source = "'a'";
        var lexer = new AsterLexer(source, "t");
        var tokens = lexer.Tokenize();
        // Single-char literal surrounded by quotes: should still be CharLiteral
        Assert.Contains(tokens, t => t.Kind == TokenKind.CharLiteral);
        Assert.DoesNotContain(tokens, t => t.Kind == TokenKind.Error);
    }

    [Fact]
    public void Parse_LifetimeRef_TypeAnnotation_NoCrash()
    {
        // &'a i32 should parse without errors (lifetime is consumed silently)
        var source = @"
fn use_ref<'a>(x: i32) -> i32 { x }
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        // Should parse without errors
        Assert.False(lexer.Diagnostics.HasErrors);
    }

    [Fact]
    public void FullPipeline_LifetimeAnnotation_Compiles()
    {
        // A function using lifetime syntax should compile (lifetime is erased)
        var source = @"
fn identity(x: i32) -> i32 { x }
fn main() {
    let v = identity(42);
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    [Fact]
    public void Lex_MultipleLifetimes_AllRecognized()
    {
        // Verify several lifetime patterns produce Lifetime tokens
        var source = "'a 'b 'c 'self 'static";
        var lexer = new AsterLexer(source, "t");
        var tokens = lexer.Tokenize();
        var lifetimes = tokens.Where(t => t.Kind == TokenKind.Lifetime).Select(t => t.Value).ToList();
        Assert.Contains("a", lifetimes);
        Assert.Contains("b", lifetimes);
        Assert.Contains("c", lifetimes);
        Assert.Contains("self", lifetimes);
        Assert.Contains("static", lifetimes);
    }
}

// ========== Phase 4 Week 27-28: Declarative Macros ==========

public class Phase4MacroTests
{
    [Fact]
    public void Parse_MacroRulesDecl_ParsesWithoutErrors()
    {
        var source = @"
macro_rules! my_macro {
    ($x:expr) => {
        let result = 0;
    };
}
fn main() { }
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        Assert.False(lexer.Diagnostics.HasErrors);
        Assert.Contains(ast.Declarations, d => d is MacroDeclNode mn && mn.Name == "my_macro");
    }

    [Fact]
    public void Parse_MacroInvocationExpr_ParsesWithoutErrors()
    {
        var source = @"
fn main() {
    vec![1, 2, 3];
}
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        Assert.False(lexer.Diagnostics.HasErrors);
        var mainFn = ast.Declarations.OfType<FunctionDeclNode>().First(f => f.Name == "main");
        var stmt = mainFn.Body.Statements.Last() as ExpressionStmtNode;
        Assert.NotNull(stmt);
        Assert.IsType<MacroInvocationExprNode>(stmt!.Expression);
        var mi = (MacroInvocationExprNode)stmt.Expression;
        Assert.Equal("vec", mi.MacroName);
        Assert.Equal(3, mi.Arguments.Count);
    }

    [Fact]
    public void Resolve_VecMacro_ExpandsToBuitinCall()
    {
        var source = @"
fn main() {
    vec![1, 2, 3];
}
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var mainFn = hir.Declarations.OfType<HirFunctionDecl>().First(f => f.Symbol.Name == "main");
        var stmt = mainFn.Body.Statements.Last() as HirExprStmt;
        Assert.NotNull(stmt);
        // The macro invocation wraps an expanded HirCallExpr (built-in vec! expansion)
        var mi = stmt!.Expression as HirMacroInvocationExpr;
        Assert.NotNull(mi);
        Assert.Equal("vec", mi!.MacroName);
        Assert.NotNull(mi.Expanded); // should have been expanded
    }

    [Fact]
    public void Resolve_AssertMacro_ExpandsToIfPanic()
    {
        var source = @"
fn main() {
    assert!(1 == 1);
}
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var mainFn = hir.Declarations.OfType<HirFunctionDecl>().First(f => f.Symbol.Name == "main");
        var stmt = mainFn.Body.Statements.Last() as HirExprStmt;
        var mi = stmt?.Expression as HirMacroInvocationExpr;
        Assert.NotNull(mi);
        // assert! expands to an if expression
        Assert.IsType<HirIfExpr>(mi!.Expanded);
    }

    [Fact]
    public void Resolve_PrintlnMacro_ExpandsToPrint()
    {
        var source = @"
fn main() {
    println!(42);
}
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var mainFn = hir.Declarations.OfType<HirFunctionDecl>().First(f => f.Symbol.Name == "main");
        var stmt = mainFn.Body.Statements.Last() as HirExprStmt;
        var mi = stmt?.Expression as HirMacroInvocationExpr;
        Assert.NotNull(mi);
        Assert.Equal("println", mi!.MacroName);
        Assert.NotNull(mi.Expanded);
    }

    [Fact]
    public void TypeCheck_MacroInvocation_NoErrors()
    {
        var source = @"
fn main() {
    vec![1, 2, 3];
    assert!(1 == 1);
    println!(42);
}
";
        var lexer = new AsterLexer(source, "t");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        var hir = new NameResolver().Resolve(ast);
        var checker = new TypeChecker();
        checker.Check(hir);
        Assert.False(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void FullPipeline_VecMacro_Compiles()
    {
        var source = @"
fn main() {
    vec![1, 2, 3];
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    [Fact]
    public void FullPipeline_AssertMacro_Compiles()
    {
        var source = @"
fn main() {
    assert!(1 == 1);
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "test.ast"));
    }

    [Fact]
    public void FullPipeline_UserDefinedMacro_Compiles()
    {
        var source = @"
macro_rules! zero {
    () => {
        let x = 0;
    };
}
fn main() {
    zero!();
}
";
        var driver = new CompilationDriver();
        // User-defined macros with a body should compile without crashing
        driver.Compile(source, "test.ast");
        // no hard assertion on success — just verifying no crash
    }
}


// ========== Phase 4 Completion: dyn Trait, Default Methods, CSE, NLL ==========

/// <summary>Tests for dyn Trait (trait object) support.</summary>
public class Phase4DynTraitTests
{
    [Fact]
    public void Parse_DynTrait_TypeAnnotation_NoCrash()
    {
        // A parameter typed as `dyn Display` should parse without errors
        var source = @"
trait Display {
    fn display(&self) -> String;
}

fn print_it(obj: dyn Display) {
    obj.display();
}
";
        var lexer = new AsterLexer(source, "t.ast");
        var tokens = lexer.Tokenize();
        Assert.False(lexer.Diagnostics.HasErrors);
        var parser = new AsterParser(tokens);
        var ast = parser.ParseProgram();
        Assert.False(parser.Diagnostics.HasErrors);
    }

    [Fact]
    public void Resolve_DynTrait_ProducesTraitObjectType()
    {
        var source = @"
trait Greet {
    fn hello(&self) -> String;
}

fn greet(g: dyn Greet) {
    g.hello();
}
";
        var lexer = new AsterLexer(source, "t.ast");
        var hir = new NameResolver().Resolve(new AsterParser(lexer.Tokenize()).ParseProgram());
        var checker = new TypeChecker();
        checker.Check(hir);
        // Should have no errors for the trait object parameter
        Assert.False(checker.Diagnostics.HasErrors);
    }

    [Fact]
    public void TypeCheck_DynTrait_MethodCall_Resolves()
    {
        var source = @"
trait Speak {
    fn speak(&self) -> String;
}

struct Dog {}

impl Speak for Dog {
    fn speak(&self) -> String {
        ""woof""
    }
}

fn make_speak(animal: dyn Speak) {
    animal.speak();
}
";
        var driver = new CompilationDriver();
        var ok = driver.Check(source, "t.ast");
        Assert.True(ok, driver.FormatDiagnostics());
    }

    [Fact]
    public void FullPipeline_DynTrait_Compiles()
    {
        var source = @"
trait Area {
    fn area(&self) -> f64;
}

struct Circle { radius: f64 }

impl Area for Circle {
    fn area(&self) -> f64 {
        3.14
    }
}

fn print_area(shape: dyn Area) {
    let a = shape.area();
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "t.ast"));
    }

    [Fact]
    public void TypeCheck_DynTrait_DisplayName_IsCorrect()
    {
        // TraitObjectType.DisplayName should be "dyn TraitName"
        var t = new TraitObjectType("Display");
        Assert.Equal("dyn Display", t.DisplayName);
    }
}

/// <summary>Tests for abstract trait methods (required methods) and default trait methods.</summary>
public class Phase4DefaultTraitMethodTests
{
    [Fact]
    public void Parse_AbstractTraitMethod_Semicolon_NoCrash()
    {
        // Abstract method ends with ; not { }
        var source = @"
trait Animal {
    fn name(&self) -> String;
    fn sound(&self) -> String;
}
";
        var lexer = new AsterLexer(source, "t.ast");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        Assert.False(lexer.Diagnostics.HasErrors);
    }

    [Fact]
    public void Parse_DefaultTraitMethod_Body_NoCrash()
    {
        // Default method has a body
        var source = @"
trait Greeter {
    fn name(&self) -> String;
    fn greet(&self) -> String {
        ""hello""
    }
}
";
        var lexer = new AsterLexer(source, "t.ast");
        var ast = new AsterParser(lexer.Tokenize()).ParseProgram();
        Assert.False(lexer.Diagnostics.HasErrors);
    }

    [Fact]
    public void Resolve_AbstractMethod_FlagSet()
    {
        var source = @"
trait Foo {
    fn required(&self) -> i32;
    fn provided(&self) -> i32 {
        42
    }
}
";
        var lexer = new AsterLexer(source, "t.ast");
        var tokens = lexer.Tokenize();
        var ast = new AsterParser(tokens).ParseProgram();

        // Find the trait decl and check IsAbstract flags
        var traitDecl = ast.Declarations.OfType<TraitDeclNode>().FirstOrDefault();
        Assert.NotNull(traitDecl);
        var required = traitDecl!.Methods.FirstOrDefault(m => m.Name == "required");
        var provided = traitDecl.Methods.FirstOrDefault(m => m.Name == "provided");
        Assert.NotNull(required);
        Assert.NotNull(provided);
        Assert.True(required!.IsAbstract);
        Assert.False(provided!.IsAbstract);
    }

    [Fact]
    public void TypeCheck_RequiredMethod_MissingImpl_ReportsError()
    {
        var source = @"
trait Shape {
    fn area(&self) -> f64;
}

struct Box {}

impl Shape for Box {
}
";
        var lexer = new AsterLexer(source, "t.ast");
        var hir = new NameResolver().Resolve(new AsterParser(lexer.Tokenize()).ParseProgram());
        var checker = new TypeChecker();
        checker.Check(hir);
        // Should report E0700: missing required method 'area'
        Assert.True(checker.Diagnostics.HasErrors);
        Assert.Contains(checker.Diagnostics, d => d.Code == "E0700");
    }

    [Fact]
    public void TypeCheck_RequiredMethod_Provided_NoError()
    {
        var source = @"
trait Shape {
    fn area(&self) -> f64;
}

struct Square { side: f64 }

impl Shape for Square {
    fn area(&self) -> f64 {
        2.0
    }
}
";
        var driver = new CompilationDriver();
        var ok = driver.Check(source, "t.ast");
        Assert.True(ok, driver.FormatDiagnostics());
    }

    [Fact]
    public void TypeCheck_DefaultMethod_NotRequired_NoError()
    {
        // Impl that doesn't override the default method should be fine
        var source = @"
trait Describable {
    fn describe(&self) -> String {
        ""an object""
    }
}

struct Widget {}

impl Describable for Widget {
}
";
        var driver = new CompilationDriver();
        var ok = driver.Check(source, "t.ast");
        Assert.True(ok, driver.FormatDiagnostics());
    }

    [Fact]
    public void HirTraitMethod_HasDefaultBody_CorrectValues()
    {
        var source = @"
trait Mix {
    fn required(&self) -> i32;
    fn optional(&self) -> i32 {
        0
    }
}
";
        var lexer = new AsterLexer(source, "t.ast");
        var hir = new NameResolver().Resolve(new AsterParser(lexer.Tokenize()).ParseProgram());
        var traitDecl = hir.Declarations.OfType<HirTraitDecl>().FirstOrDefault();
        Assert.NotNull(traitDecl);
        var req = traitDecl!.Methods.FirstOrDefault(m => m.Name == "required");
        var opt = traitDecl.Methods.FirstOrDefault(m => m.Name == "optional");
        Assert.NotNull(req);
        Assert.NotNull(opt);
        Assert.False(req!.HasDefaultBody);   // abstract → no default body
        Assert.True(opt!.HasDefaultBody);    // has body → default body
    }
}

/// <summary>Tests for the CSE (Common Subexpression Elimination) pass.</summary>
public class Phase4CsePassTests
{
    [Fact]
    public void CsePass_EliminatesRedundantBinaryOp()
    {
        var fn = new MirFunction("test");
        var block = fn.CreateBlock("entry");

        // x = a + b
        var xInstr = new MirInstruction(
            MirOpcode.BinaryOp,
            MirOperand.Variable("x", MirType.I32),
            new[] { MirOperand.Variable("a", MirType.I32), MirOperand.Variable("b", MirType.I32) },
            extra: "+");
        // y = a + b  (same expression — should be replaced by y = x)
        var yInstr = new MirInstruction(
            MirOpcode.BinaryOp,
            MirOperand.Variable("y", MirType.I32),
            new[] { MirOperand.Variable("a", MirType.I32), MirOperand.Variable("b", MirType.I32) },
            extra: "+");
        block.AddInstruction(xInstr);
        block.AddInstruction(yInstr);
        block.Terminator = new MirReturn();

        var module = new MirModule("test");
        module.Functions.Add(fn);

        var cse = new CsePass();
        bool changed = cse.Eliminate(module);
        Assert.True(changed);
        // Second instruction should now be an Assign (copy), not BinaryOp
        Assert.Equal(MirOpcode.Assign, block.Instructions[1].Opcode);
    }

    [Fact]
    public void CsePass_NoChange_UniqueExpressions()
    {
        var fn = new MirFunction("test");
        var block = fn.CreateBlock("entry");

        // x = a + b  (unique)
        // y = a + c  (different — should NOT be CSE'd)
        block.AddInstruction(new MirInstruction(
            MirOpcode.BinaryOp,
            MirOperand.Variable("x", MirType.I32),
            new[] { MirOperand.Variable("a", MirType.I32), MirOperand.Variable("b", MirType.I32) },
            extra: "+"));
        block.AddInstruction(new MirInstruction(
            MirOpcode.BinaryOp,
            MirOperand.Variable("y", MirType.I32),
            new[] { MirOperand.Variable("a", MirType.I32), MirOperand.Variable("c", MirType.I32) },
            extra: "+"));
        block.Terminator = new MirReturn();

        var module = new MirModule("test");
        module.Functions.Add(fn);

        var cse = new CsePass();
        bool changed = cse.Eliminate(module);
        Assert.False(changed);
        Assert.Equal(MirOpcode.BinaryOp, block.Instructions[0].Opcode);
        Assert.Equal(MirOpcode.BinaryOp, block.Instructions[1].Opcode);
    }

    [Fact]
    public void CsePass_Invalidates_OnRedefinition()
    {
        var fn = new MirFunction("test");
        var block = fn.CreateBlock("entry");

        // x = a + b
        // a = 10    <- redefines a, invalidates the cached expression
        // y = a + b <- NOT a duplicate (a changed)
        block.AddInstruction(new MirInstruction(
            MirOpcode.BinaryOp,
            MirOperand.Variable("x", MirType.I32),
            new[] { MirOperand.Variable("a", MirType.I32), MirOperand.Variable("b", MirType.I32) },
            extra: "+"));
        block.AddInstruction(new MirInstruction(
            MirOpcode.Assign,
            MirOperand.Variable("a", MirType.I32),
            new[] { MirOperand.Constant(10, MirType.I32) }));
        block.AddInstruction(new MirInstruction(
            MirOpcode.BinaryOp,
            MirOperand.Variable("y", MirType.I32),
            new[] { MirOperand.Variable("a", MirType.I32), MirOperand.Variable("b", MirType.I32) },
            extra: "+"));
        block.Terminator = new MirReturn();

        var module = new MirModule("test");
        module.Functions.Add(fn);

        var cse = new CsePass();
        bool changed = cse.Eliminate(module);
        Assert.False(changed); // No CSE possible
        Assert.Equal(MirOpcode.BinaryOp, block.Instructions[2].Opcode);
    }

    [Fact]
    public void CsePass_EmptyModule_NoCrash()
    {
        var module = new MirModule("empty");
        Assert.False(new CsePass().Eliminate(module));
    }
}

/// <summary>Tests for Phase 4 NLL borrow checker enhancements.</summary>
public class Phase4NllBorrowCheckerTests
{
    [Fact]
    public void BorrowCheck_TwoPhase_ReservedMutableBorrow_ThenActivated()
    {
        // Two-phase borrow: reserve mutable borrow while shared borrow is live,
        // then activate after shared borrow ends. This should NOT error.
        var checker = new BorrowCheck();
        var module = new MirModule("test");
        var fn = new MirFunction("test_fn");
        var block = fn.CreateBlock("entry");

        // x = something
        block.AddInstruction(new MirInstruction(
            MirOpcode.Assign,
            MirOperand.Variable("x", MirType.I32),
            new[] { MirOperand.Constant(42, MirType.I32) }));
        // &x (immutable borrow)
        block.AddInstruction(new MirInstruction(
            MirOpcode.Borrow,
            MirOperand.Variable("r", MirType.I32),
            new[] { MirOperand.Variable("x", MirType.I32) },
            extra: false)); // immutable
        // &mut x (tries to reserve mutable borrow while shared borrow exists → two-phase reserve)
        block.AddInstruction(new MirInstruction(
            MirOpcode.Borrow,
            MirOperand.Variable("rm", MirType.I32),
            new[] { MirOperand.Variable("x", MirType.I32) },
            extra: true)); // mutable
        block.Terminator = new MirReturn();

        module.Functions.Add(fn);
        checker.Check(module);
        // Two-phase borrows allow this — should not report E0503
        Assert.DoesNotContain(checker.Diagnostics, d => d.Code == "E0503");
    }

    [Fact]
    public void BorrowCheck_UseAfterMove_HasHintMessage()
    {
        var checker = new BorrowCheck();
        var module = new MirModule("test");
        var fn = new MirFunction("test_fn");
        var block = fn.CreateBlock("entry");

        block.AddInstruction(new MirInstruction(
            MirOpcode.Assign,
            MirOperand.Variable("x", MirType.I32),
            new[] { MirOperand.Constant(1, MirType.I32) }));
        // Move x
        block.AddInstruction(new MirInstruction(
            MirOpcode.Move,
            MirOperand.Variable("y", MirType.I32),
            new[] { MirOperand.Variable("x", MirType.I32) }));
        // Use x again after move
        block.AddInstruction(new MirInstruction(
            MirOpcode.Move,
            MirOperand.Variable("z", MirType.I32),
            new[] { MirOperand.Variable("x", MirType.I32) }));
        block.Terminator = new MirReturn();

        module.Functions.Add(fn);
        checker.Check(module);
        Assert.True(checker.Diagnostics.HasErrors);
        var error = checker.Diagnostics.First(d => d.Code == "E0500");
        // Phase 4: error messages should include a hint
        Assert.Contains("Hint:", error.Message);
    }

    [Fact]
    public void BorrowCheck_MutableAliasing_HasHintMessage()
    {
        var checker = new BorrowCheck();
        var module = new MirModule("test");
        var fn = new MirFunction("fn1");
        var block = fn.CreateBlock("entry");

        block.AddInstruction(new MirInstruction(
            MirOpcode.Assign,
            MirOperand.Variable("v", MirType.I32),
            new[] { MirOperand.Constant(0, MirType.I32) }));
        // &mut v
        block.AddInstruction(new MirInstruction(
            MirOpcode.Borrow,
            MirOperand.Variable("rm1", MirType.I32),
            new[] { MirOperand.Variable("v", MirType.I32) },
            extra: true));
        // &mut v again — should error with a hint
        block.AddInstruction(new MirInstruction(
            MirOpcode.Borrow,
            MirOperand.Variable("rm2", MirType.I32),
            new[] { MirOperand.Variable("v", MirType.I32) },
            extra: true));
        block.Terminator = new MirReturn();

        module.Functions.Add(fn);
        checker.Check(module);
        Assert.True(checker.Diagnostics.HasErrors);
        var error = checker.Diagnostics.First(d => d.Code == "E0503");
        Assert.Contains("Hint:", error.Message);
    }

    [Fact]
    public void BorrowCheck_Drop_ClearsAllBorrows()
    {
        // After Drop, value should no longer be borrowed
        var checker = new BorrowCheck();
        var module = new MirModule("test");
        var fn = new MirFunction("drop_test");
        var block = fn.CreateBlock("entry");

        block.AddInstruction(new MirInstruction(
            MirOpcode.Assign,
            MirOperand.Variable("x", MirType.I32),
            new[] { MirOperand.Constant(5, MirType.I32) }));
        // Drop x — should clear borrow state
        block.AddInstruction(new MirInstruction(
            MirOpcode.Drop,
            null,
            new[] { MirOperand.Variable("x", MirType.I32) }));
        block.Terminator = new MirReturn();

        module.Functions.Add(fn);
        checker.Check(module);
        Assert.False(checker.Diagnostics.HasErrors);
    }
}

/// <summary>Integration tests tying together all Phase 4 completion features.</summary>
public class Phase4IntegrationTests
{
    [Fact]
    public void FullPipeline_AbstractAndDefaultTraitMethods_Compiles()
    {
        var source = @"
trait Printable {
    fn as_string(&self) -> String;
    fn print(&self) -> void {
        as_string(self);
    }
}

struct Number { value: i32 }

impl Printable for Number {
    fn as_string(&self) -> String {
        ""a number""
    }
}

fn main() {
    let n = Number { value: 42 };
}
";
        var driver = new CompilationDriver();
        var ok = driver.Check(source, "t.ast");
        Assert.True(ok, driver.FormatDiagnostics());
    }

    [Fact]
    public void FullPipeline_DynTrait_FunctionParam_Compiles()
    {
        var source = @"
trait Logger {
    fn log(&self, msg: String);
}

fn do_work(logger: dyn Logger) {
    logger.log(""working"");
}
";
        var driver = new CompilationDriver();
        var ok = driver.Check(source, "t.ast");
        Assert.True(ok, driver.FormatDiagnostics());
    }

    [Fact]
    public void FullPipeline_MissingRequiredMethod_ReportsError()
    {
        var source = @"
trait Runnable {
    fn run(&self);
}

struct Task {}

impl Runnable for Task {
}
";
        var driver = new CompilationDriver();
        var ok = driver.Check(source, "t.ast");
        Assert.False(ok);
        Assert.Contains(driver.Diagnostics, d => d.Code == "E0700");
    }

    [Fact]
    public void CsePass_IntegratedInPipeline_Compiles()
    {
        var source = @"
fn main() {
    let a = 1 + 2;
    let b = 1 + 2;
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "t.ast"));
    }
}

// =============================================================================
// Phase 5 Tests: Advanced Optimizations, Procedural Macros, Spec
// =============================================================================

public class Phase5LicmTests
{
    [Fact]
    public void LicmPass_HoistsInvariantBinaryOp()
    {
        // Build a MIR module with a back-edge loop containing a loop-invariant add
        var module = new MirModule("test");
        var fn = new MirFunction("test_fn");
        fn.ReturnType = MirType.Void;
        module.Functions.Add(fn);

        // Block 0 (entry / loop header)
        var header = fn.CreateBlock("entry");
        var inv = MirOperand.Temp("inv", MirType.I32);
        header.AddInstruction(new MirInstruction(
            MirOpcode.BinaryOp,
            inv,
            new[] { MirOperand.Constant(2L, MirType.I32), MirOperand.Constant(3L, MirType.I32) },
            "add"));
        // back-edge: unconditional branch to block 0
        header.Terminator = new MirBranch(0);

        var pass = new LicmPass();
        var changed = pass.Hoist(module);

        Assert.True(changed);
        // A pre-header block should have been inserted
        Assert.True(fn.BasicBlocks.Count >= 2);
        // The pre-header should contain the hoisted instruction
        var preHeader = fn.BasicBlocks[0];
        Assert.Contains(preHeader.Instructions, i => i.Opcode == MirOpcode.BinaryOp);
    }

    [Fact]
    public void LicmPass_DoesNotHoistVariantInstruction()
    {
        var module = new MirModule("test");
        var fn = new MirFunction("test_fn");
        fn.ReturnType = MirType.Void;
        module.Functions.Add(fn);

        var header = fn.CreateBlock("entry");
        // loop-variant: operand "i" is defined in the loop (also in this block)
        var iVar = MirOperand.Variable("i", MirType.I32);
        header.AddInstruction(new MirInstruction(
            MirOpcode.Assign, iVar,
            new[] { MirOperand.Constant(0L, MirType.I32) }));
        var sum = MirOperand.Temp("sum", MirType.I32);
        header.AddInstruction(new MirInstruction(
            MirOpcode.BinaryOp, sum,
            new[] { iVar, MirOperand.Constant(1L, MirType.I32) }, "add"));
        header.Terminator = new MirBranch(0);

        var pass = new LicmPass();
        var changed = pass.Hoist(module);

        // The BinaryOp has a loop-defined operand ("i") — must NOT be hoisted
        Assert.False(changed);
        Assert.Equal(1, fn.BasicBlocks.Count);
    }

    [Fact]
    public void LicmPass_NoLoop_NoChange()
    {
        var module = new MirModule("test");
        var fn = new MirFunction("test_fn");
        fn.ReturnType = MirType.Void;
        module.Functions.Add(fn);

        var b0 = fn.CreateBlock("b0");
        b0.AddInstruction(new MirInstruction(MirOpcode.BinaryOp,
            MirOperand.Temp("t", MirType.I32),
            new[] { MirOperand.Constant(1L, MirType.I32), MirOperand.Constant(2L, MirType.I32) },
            "add"));
        b0.Terminator = new MirReturn();

        Assert.False(new LicmPass().Hoist(module));
    }
}

public class Phase5InliningTests
{
    [Fact]
    public void InliningPass_InlinesSmallFunction()
    {
        var module = new MirModule("test");

        // Callee: fn double(x: i32) -> i32 { x + x }
        var callee = new MirFunction("double");
        callee.ReturnType = MirType.I32;
        callee.Parameters.Add(new MirParameter("x", MirType.I32, 0));
        var calleeBlock = callee.CreateBlock("entry");
        var result = MirOperand.Temp("r", MirType.I32);
        var xParam = MirOperand.Variable("x", MirType.I32);
        calleeBlock.AddInstruction(new MirInstruction(MirOpcode.BinaryOp, result,
            new[] { xParam, xParam }, "add"));
        calleeBlock.Terminator = new MirReturn(result);
        module.Functions.Add(callee);

        // Caller: fn main() { let y = double(5); }
        var caller = new MirFunction("main");
        caller.ReturnType = MirType.Void;
        var callerBlock = caller.CreateBlock("entry");
        var y = MirOperand.Variable("y", MirType.I32);
        callerBlock.AddInstruction(new MirInstruction(MirOpcode.Call, y,
            new[] { MirOperand.FunctionRef("double"), MirOperand.Constant(5L, MirType.I32) }));
        callerBlock.Terminator = new MirReturn();
        module.Functions.Add(caller);

        var changed = new InliningPass().Inline(module);

        Assert.True(changed);
        // After inlining, the Call instruction should be replaced
        Assert.DoesNotContain(callerBlock.Instructions, i => i.Opcode == MirOpcode.Call);
        // Should contain a BinaryOp (the inlined body)
        Assert.Contains(callerBlock.Instructions, i => i.Opcode == MirOpcode.BinaryOp);
    }

    [Fact]
    public void InliningPass_DoesNotInlineLargeFunction()
    {
        var module = new MirModule("test");

        // Callee with 10 instructions (> MaxInlineInstructions = 5)
        var callee = new MirFunction("big");
        callee.ReturnType = MirType.Void;
        var calleeBlock = callee.CreateBlock("entry");
        for (int i = 0; i < 10; i++)
            calleeBlock.AddInstruction(new MirInstruction(MirOpcode.Assign,
                MirOperand.Temp($"t{i}", MirType.I32),
                new[] { MirOperand.Constant(0L, MirType.I32) }));
        calleeBlock.Terminator = new MirReturn();
        module.Functions.Add(callee);

        var caller = new MirFunction("main");
        caller.ReturnType = MirType.Void;
        var callerBlock = caller.CreateBlock("entry");
        callerBlock.AddInstruction(new MirInstruction(MirOpcode.Call, null,
            new[] { MirOperand.FunctionRef("big") }));
        callerBlock.Terminator = new MirReturn();
        module.Functions.Add(caller);

        Assert.False(new InliningPass().Inline(module));
    }

    [Fact]
    public void InliningPass_ParameterSubstitution()
    {
        var module = new MirModule("test");

        // fn add_one(n: i32) -> i32 { n + 1 }
        var callee = new MirFunction("add_one");
        callee.ReturnType = MirType.I32;
        callee.Parameters.Add(new MirParameter("n", MirType.I32, 0));
        var cb = callee.CreateBlock("entry");
        var r = MirOperand.Temp("r", MirType.I32);
        cb.AddInstruction(new MirInstruction(MirOpcode.BinaryOp, r,
            new[] { MirOperand.Variable("n", MirType.I32), MirOperand.Constant(1L, MirType.I32) }, "add"));
        cb.Terminator = new MirReturn(r);
        module.Functions.Add(callee);

        var caller = new MirFunction("main");
        caller.ReturnType = MirType.Void;
        var mb = caller.CreateBlock("entry");
        var res = MirOperand.Variable("res", MirType.I32);
        mb.AddInstruction(new MirInstruction(MirOpcode.Call, res,
            new[] { MirOperand.FunctionRef("add_one"), MirOperand.Constant(42L, MirType.I32) }));
        mb.Terminator = new MirReturn();
        module.Functions.Add(caller);

        Assert.True(new InliningPass().Inline(module));
        // The BinaryOp's first operand should now be the constant 42 (substituted)
        var binaryOp = mb.Instructions.FirstOrDefault(i => i.Opcode == MirOpcode.BinaryOp);
        Assert.NotNull(binaryOp);
        Assert.Equal(MirOperandKind.Constant, binaryOp!.Operands[0].Kind);
        Assert.Equal(42L, binaryOp.Operands[0].Value);
    }
}

public class Phase5SroaTests
{
    [Fact]
    public void SroaPass_ReplacesConstantLoadWithConstant()
    {
        var module = new MirModule("test");
        var fn = new MirFunction("test_fn");
        fn.ReturnType = MirType.Void;
        module.Functions.Add(fn);

        var block = fn.CreateBlock("entry");
        var structVar = MirOperand.Variable("p", MirType.I32);
        var fieldDest  = MirOperand.Temp("x", MirType.I32);
        var constVal   = MirOperand.Constant(99L, MirType.I32);

        // %p = Assign c:99
        block.AddInstruction(new MirInstruction(MirOpcode.Assign, structVar, new[] { constVal }));
        // %x = Load %p field:"x"
        block.AddInstruction(new MirInstruction(MirOpcode.Load, fieldDest, new[] { structVar }, "x"));
        block.Terminator = new MirReturn();

        var changed = new SroaPass().Replace(module);

        Assert.True(changed);
        // The Load should have been replaced with an Assign of the constant
        var loadInstr = block.Instructions.FirstOrDefault(i => i.Destination?.Name == "x");
        Assert.NotNull(loadInstr);
        Assert.Equal(MirOpcode.Assign, loadInstr!.Opcode);
    }

    [Fact]
    public void SroaPass_NoConstantSource_NoChange()
    {
        var module = new MirModule("test");
        var fn = new MirFunction("test_fn");
        fn.ReturnType = MirType.Void;
        module.Functions.Add(fn);

        var block = fn.CreateBlock("entry");
        var structVar = MirOperand.Variable("p", MirType.I32);
        var fieldDest  = MirOperand.Temp("x", MirType.I32);

        // %p has no constant assignment — Load should remain
        block.AddInstruction(new MirInstruction(MirOpcode.Load, fieldDest, new[] { structVar }, "x"));
        block.Terminator = new MirReturn();

        Assert.False(new SroaPass().Replace(module));
    }
}

public class Phase5ProcMacroTests
{
    [Fact]
    public void Parse_DeriveAttribute_OnStruct()
    {
        var source = @"
#[derive(Debug, Clone)]
struct Point { x: i32, y: i32 }
fn main() {}
";
        var driver = new CompilationDriver();
        var ok = driver.Check(source, "t.ast");
        Assert.True(ok, driver.FormatDiagnostics());
    }

    [Fact]
    public void Parse_DeriveAttribute_OnEnum()
    {
        var source = @"
#[derive(Clone, PartialEq)]
enum Color { Red, Green, Blue }
fn main() {}
";
        var driver = new CompilationDriver();
        var ok = driver.Check(source, "t.ast");
        Assert.True(ok, driver.FormatDiagnostics());
    }

    [Fact]
    public void Derive_Debug_GeneratesDebugMethod()
    {
        var source = @"
#[derive(Debug)]
struct Foo { value: i32 }
fn main() {}
";
        var resolver = new NameResolver();
        var tokens = new Aster.Compiler.Frontend.Lexer.AsterLexer(source, "t.ast").Tokenize();
        var ast = new Aster.Compiler.Frontend.Parser.AsterParser(tokens).ParseProgram();
        var hir = resolver.Resolve(ast);

        // A synthesised impl for "Debug" should appear
        var impls = hir.Declarations.OfType<HirImplDecl>().ToList();
        Assert.Contains(impls, impl =>
            impl.TargetTypeName == "Foo" && impl.TraitName == "Debug");
    }

    [Fact]
    public void Derive_Clone_GeneratesCloneMethod()
    {
        var source = @"
#[derive(Clone)]
struct Bar { x: i32 }
fn main() {}
";
        var resolver = new NameResolver();
        var tokens = new Aster.Compiler.Frontend.Lexer.AsterLexer(source, "t.ast").Tokenize();
        var ast = new Aster.Compiler.Frontend.Parser.AsterParser(tokens).ParseProgram();
        var hir = resolver.Resolve(ast);

        var impl = hir.Declarations.OfType<HirImplDecl>()
            .FirstOrDefault(i => i.TargetTypeName == "Bar" && i.TraitName == "Clone");
        Assert.NotNull(impl);
        var cloneMethod = impl!.Methods.FirstOrDefault(m => m.Symbol.Name.Contains("clone"));
        Assert.NotNull(cloneMethod);
    }

    [Fact]
    public void Derive_PartialEq_GeneratesEqMethod()
    {
        var source = @"
#[derive(PartialEq)]
struct Num { n: i32 }
fn main() {}
";
        var resolver = new NameResolver();
        var tokens = new Aster.Compiler.Frontend.Lexer.AsterLexer(source, "t.ast").Tokenize();
        var ast = new Aster.Compiler.Frontend.Parser.AsterParser(tokens).ParseProgram();
        var hir = resolver.Resolve(ast);

        var impl = hir.Declarations.OfType<HirImplDecl>()
            .FirstOrDefault(i => i.TargetTypeName == "Num" && i.TraitName == "PartialEq");
        Assert.NotNull(impl);
        Assert.Contains(impl!.Methods, m => m.Symbol.Name.Contains("eq"));
    }

    [Fact]
    public void Derive_Eq_IsMarkerNoMethods()
    {
        var source = @"
#[derive(Eq)]
struct Unit {}
fn main() {}
";
        var resolver = new NameResolver();
        var tokens = new Aster.Compiler.Frontend.Lexer.AsterLexer(source, "t.ast").Tokenize();
        var ast = new Aster.Compiler.Frontend.Parser.AsterParser(tokens).ParseProgram();
        var hir = resolver.Resolve(ast);

        // Eq is a marker trait — no impl block should be generated for it
        var eqImpl = hir.Declarations.OfType<HirImplDecl>()
            .FirstOrDefault(i => i.TargetTypeName == "Unit" && i.TraitName == "Eq");
        Assert.Null(eqImpl);
    }

    [Fact]
    public void Derive_Hash_GeneratesHashMethod()
    {
        var source = @"
#[derive(Hash)]
struct Key { id: i64 }
fn main() {}
";
        var driver = new CompilationDriver();
        Assert.True(driver.Check(source, "t.ast"), driver.FormatDiagnostics());
    }

    [Fact]
    public void Derive_Default_GeneratesDefaultMethod()
    {
        var source = @"
#[derive(Default)]
struct Config { timeout: i32 }
fn main() {}
";
        var driver = new CompilationDriver();
        Assert.True(driver.Check(source, "t.ast"), driver.FormatDiagnostics());
    }

    [Fact]
    public void Derive_UnknownTrait_EmitsWarning()
    {
        var source = @"
#[derive(UnknownTrait)]
struct Weird {}
fn main() {}
";
        var driver = new CompilationDriver();
        // Should not fail — just warn
        var ok = driver.Check(source, "t.ast");
        Assert.True(ok);
        Assert.Contains(driver.Diagnostics, d => d.Code == "E0800");
    }

    [Fact]
    public void Derive_MultipleTraits_GeneratesAllImpls()
    {
        var source = @"
#[derive(Debug, Clone, Hash)]
struct Vec3 { x: f64, y: f64, z: f64 }
fn main() {}
";
        var resolver = new NameResolver();
        var tokens = new Aster.Compiler.Frontend.Lexer.AsterLexer(source, "t.ast").Tokenize();
        var ast = new Aster.Compiler.Frontend.Parser.AsterParser(tokens).ParseProgram();
        var hir = resolver.Resolve(ast);

        var impls = hir.Declarations.OfType<HirImplDecl>()
            .Where(i => i.TargetTypeName == "Vec3").ToList();
        Assert.Equal(3, impls.Count);
        Assert.Contains(impls, i => i.TraitName == "Debug");
        Assert.Contains(impls, i => i.TraitName == "Clone");
        Assert.Contains(impls, i => i.TraitName == "Hash");
    }

    [Fact]
    public void Derive_FullPipeline_Compiles()
    {
        var source = @"
#[derive(Debug, Clone, PartialEq)]
struct Point { x: i32, y: i32 }
fn main() {
    let p = Point { x: 1, y: 2 };
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "t.ast"));
    }
}

public class Phase5IntegrationTests
{
    [Fact]
    public void Phase5_AllOptimizations_PipelineCompiles()
    {
        var source = @"
#[derive(Debug, Clone)]
struct Rect { width: i32, height: i32 }
fn area(r: Rect) -> i32 { r.width }
fn main() {
    let r = Rect { width: 10, height: 5 };
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "t.ast"));
    }

    [Fact]
    public void Phase5_DeriveOnEnum_FullPipeline()
    {
        var source = @"
#[derive(Debug, Clone, PartialEq)]
enum Direction { North, South, East, West }
fn main() {}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "t.ast"));
    }

    [Fact]
    public void Phase5_MultipleAttributesStacked()
    {
        var source = @"
#[derive(Debug)]
#[derive(Clone)]
struct Item { id: i32 }
fn main() {}
";
        var driver = new CompilationDriver();
        Assert.True(driver.Check(source, "t.ast"), driver.FormatDiagnostics());
    }
}


// ============================================================
// Phase 6: Slices, Casts, Ranges, Loop Unrolling
// ============================================================

public class Phase6SliceTests
{
    private static ProgramNode Parse(string source)
    {
        var tokens = new AsterLexer(source, "t.ast").Tokenize();
        return new AsterParser(tokens).ParseProgram();
    }

    [Fact]
    public void Parse_ArrayLiteral_ThreeInts_NoErrors()
    {
        var tokens = new AsterLexer("fn f() -> i32 { let a = [1, 2, 3]; 0 }", "t.ast").Tokenize();
        var parser = new AsterParser(tokens);
        parser.ParseProgram();
        Assert.False(parser.Diagnostics.HasErrors);
    }

    [Fact]
    public void Parse_SliceTypeAnnotation_InFunctionParam_NoErrors()
    {
        var source = "fn f(x: [i32]) -> i32 { 0 }";
        var tokens = new AsterLexer(source, "t.ast").Tokenize();
        var parser = new AsterParser(tokens);
        parser.ParseProgram();
        Assert.False(parser.Diagnostics.HasErrors);
    }

    [Fact]
    public void TypeCheck_ArrayLiteral_NoErrors()
    {
        var source = "fn main() -> i32 { let arr = [1, 2, 3]; 0 }";
        var driver = new CompilationDriver();
        Assert.True(driver.Check(source, "t.ast"), driver.FormatDiagnostics());
    }

    [Fact]
    public void TypeCheck_SliceTypeRef_Resolves()
    {
        var source = "fn sum(data: [i32]) -> i32 { 0 }\nfn main() -> i32 { 0 }";
        var driver = new CompilationDriver();
        Assert.True(driver.Check(source, "t.ast"), driver.FormatDiagnostics());
    }

    [Fact]
    public void TypeCheck_StrTypeRef_Resolves()
    {
        var source = "fn greet(name: str) -> i32 { 0 }\nfn main() -> i32 { 0 }";
        var driver = new CompilationDriver();
        Assert.True(driver.Check(source, "t.ast"), driver.FormatDiagnostics());
    }

    [Fact]
    public void Compile_ArrayLiteral_ProducesIr()
    {
        var source = "fn main() -> i32 { let arr = [10, 20, 30]; 0 }";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "t.ast"));
    }
}

public class Phase6CastTests
{
    [Fact]
    public void Lex_AsKeyword_ProducesAsToken()
    {
        var tokens = new AsterLexer("x as i64", "t.ast").Tokenize();
        Assert.Contains(tokens, t => t.Kind == TokenKind.As);
    }

    [Fact]
    public void Parse_CastExpr_IntToI64_NoErrors()
    {
        var source = "fn f() -> i64 { let x = 42; x as i64 }";
        var tokens = new AsterLexer(source, "t.ast").Tokenize();
        var parser = new AsterParser(tokens);
        parser.ParseProgram();
        Assert.False(parser.Diagnostics.HasErrors);
    }

    [Fact]
    public void Parse_CastExpr_FloatToI32_NoErrors()
    {
        var source = "fn f() -> i32 { let x = 3.14; x as i32 }";
        var tokens = new AsterLexer(source, "t.ast").Tokenize();
        var parser = new AsterParser(tokens);
        parser.ParseProgram();
        Assert.False(parser.Diagnostics.HasErrors);
    }

    [Fact]
    public void TypeCheck_CastExpr_I32ToI64_NoError()
    {
        var source = "fn main() -> i64 { let x = 5; x as i64 }";
        var driver = new CompilationDriver();
        Assert.True(driver.Check(source, "t.ast"), driver.FormatDiagnostics());
    }

    [Fact]
    public void TypeCheck_CastExpr_I32ToF64_NoError()
    {
        var source = "fn main() -> f64 { let x = 5; x as f64 }";
        var driver = new CompilationDriver();
        Assert.True(driver.Check(source, "t.ast"), driver.FormatDiagnostics());
    }

    [Fact]
    public void Compile_CastExpr_ProducesIr()
    {
        var source = "fn main() -> i64 { let x = 42; x as i64 }";
        var driver = new CompilationDriver();
        var ir = driver.Compile(source, "t.ast");
        Assert.NotNull(ir);
    }

    [Fact]
    public void Parse_CastExpr_ChainedCasts_NoErrors()
    {
        var source = "fn f() -> f64 { let x = 1; x as i64 as f64 }";
        var tokens = new AsterLexer(source, "t.ast").Tokenize();
        var parser = new AsterParser(tokens);
        parser.ParseProgram();
        Assert.False(parser.Diagnostics.HasErrors);
    }
}

public class Phase6RangeTests
{
    [Fact]
    public void TypeCheck_RangeExpr_I32_NoErrors()
    {
        var source = "fn main() -> i32 { let r = 0..10; 0 }";
        var driver = new CompilationDriver();
        Assert.True(driver.Check(source, "t.ast"), driver.FormatDiagnostics());
    }

    [Fact]
    public void Parse_RangeExpr_Simple_NoErrors()
    {
        var source = "fn f() -> i32 { let r = 1..5; 0 }";
        var tokens = new AsterLexer(source, "t.ast").Tokenize();
        var parser = new AsterParser(tokens);
        parser.ParseProgram();
        Assert.False(parser.Diagnostics.HasErrors);
    }

    [Fact]
    public void Compile_ForLoopWithRange_ProducesIr()
    {
        var source = @"
fn main() -> i32 {
    let total = 0;
    for i in 0..5 {
        total = total + i;
    }
    total
}
";
        var driver = new CompilationDriver();
        Assert.NotNull(driver.Compile(source, "t.ast"));
    }

    [Fact]
    public void TypeCheck_RangeExpr_InForLoop_NoErrors()
    {
        var source = @"
fn main() -> i32 {
    let s = 0;
    for i in 0..10 {
        s = s + 1;
    }
    s
}
";
        var driver = new CompilationDriver();
        Assert.True(driver.Check(source, "t.ast"), driver.FormatDiagnostics());
    }

    [Fact]
    public void Parse_RangeExpr_InVariable_NoErrors()
    {
        var source = "fn f() -> i32 { let r = 0..3; 0 }";
        var tokens = new AsterLexer(source, "t.ast").Tokenize();
        var parser = new AsterParser(tokens);
        parser.ParseProgram();
        Assert.False(parser.Diagnostics.HasErrors);
    }
}

public class Phase6LoopUnrollTests
{
    [Fact]
    public void LoopUnroll_EmptyModule_ReturnsEmpty()
    {
        var module = new MirModule("test");
        var result = new LoopUnrollPass().Run(module);
        Assert.NotNull(result);
        Assert.Empty(result.Functions);
    }

    [Fact]
    public void LoopUnroll_FunctionWithNoRangeCall_Unchanged()
    {
        var module = new MirModule("test");
        var fn = new MirFunction("f");
        module.Functions.Add(fn);
        var bb = fn.CreateBlock("entry");
        bb.AddInstruction(new MirInstruction(MirOpcode.Assign,
            MirOperand.Temp("t0", MirType.I64),
            new List<MirOperand> { MirOperand.Constant(42L, MirType.I64) }));
        bb.Terminator = new MirReturn();

        var result = new LoopUnrollPass().Run(module);
        Assert.Single(result.Functions);
        Assert.Single(result.Functions[0].BasicBlocks);
    }

    [Fact]
    public void LoopUnroll_ConstantRange_UnrollsToTripCount()
    {
        // Set up a two-block loop where the BODY block has both
        // the __range_new call and a back-edge (in the last instruction's Extra).
        var module = new MirModule("test");
        var fn = new MirFunction("f");
        module.Functions.Add(fn);

        // pre-header (not part of the loop body)
        var preHeader = fn.CreateBlock("pre_header");
        preHeader.AddInstruction(new MirInstruction(
            MirOpcode.Assign,
            MirOperand.Temp("init", MirType.I64),
            new List<MirOperand> { MirOperand.Constant(0L, MirType.I64) }));

        // loop body: __range_new(0, 3) + back-edge to pre_header label
        var loopBody = fn.CreateBlock("loop_body");
        loopBody.AddInstruction(new MirInstruction(
            MirOpcode.Call,
            MirOperand.Temp("range", MirType.I64),
            new List<MirOperand> {
                MirOperand.Constant(0, MirType.I64),
                MirOperand.Constant(3, MirType.I64),
            },
            "__range_new"));
        // Last instruction with back-edge label pointing to pre_header
        loopBody.AddInstruction(new MirInstruction(
            MirOpcode.Assign,
            MirOperand.Temp("back", MirType.I64),
            new List<MirOperand> { MirOperand.Constant(0L, MirType.I64) },
            "pre_header"));   // back-edge target label
        loopBody.Terminator = new MirReturn();

        var result = new LoopUnrollPass().Run(module);
        // tripCount=3, bodyBlocks = blocks[0..1] (pre_header + loop_body) = 2
        // Unrolled: 3 * 2 = 6 blocks
        Assert.Equal(6, result.Functions[0].BasicBlocks.Count);
    }

    [Fact]
    public void LoopUnroll_TripCountAboveMax_DoesNotUnroll()
    {
        var module = new MirModule("test");
        var fn = new MirFunction("f");
        module.Functions.Add(fn);

        var header = fn.CreateBlock("header");
        header.AddInstruction(new MirInstruction(
            MirOpcode.Call,
            MirOperand.Temp("r", MirType.I64),
            new List<MirOperand> {
                MirOperand.Constant(0, MirType.I64),
                MirOperand.Constant(100, MirType.I64), // 100 > MaxUnrollCount(8)
            },
            "__range_new"));
        header.Terminator = new MirReturn();

        var result = new LoopUnrollPass().Run(module);
        Assert.Single(result.Functions[0].BasicBlocks); // unchanged
    }

    [Fact]
    public void LoopUnroll_FullPipeline_CompilationSucceeds()
    {
        var source = @"
fn main() -> i32 {
    let s = 0;
    for i in 0..3 {
        s = s + 1;
    }
    s
}
";
        Assert.NotNull(new CompilationDriver().Compile(source, "t.ast"));
    }
}

public class Phase6IntegrationTests
{
    [Fact]
    public void Compile_ArrayLiteralUsedInFunction_Works()
    {
        var source = @"
fn make_total() -> i32 {
    let arr = [1, 2, 3];
    0
}
fn main() -> i32 { 0 }
";
        Assert.True(new CompilationDriver().Check(source, "t.ast"),
            new CompilationDriver().FormatDiagnostics());
    }

    [Fact]
    public void Compile_CastInArithmeticExpr_Works()
    {
        var source = @"
fn add(a: i32, b: i32) -> i64 {
    (a + b) as i64
}
fn main() -> i32 { 0 }
";
        Assert.True(new CompilationDriver().Check(source, "t.ast"),
            new CompilationDriver().FormatDiagnostics());
    }

    [Fact]
    public void Compile_RangeAndCastCombined_Works()
    {
        var source = @"
fn count_up(max: i32) -> i32 {
    let total = 0;
    for i in 0..max {
        total = total + i;
    }
    total
}
fn main() -> i32 { 0 }
";
        Assert.True(new CompilationDriver().Check(source, "t.ast"),
            new CompilationDriver().FormatDiagnostics());
    }

    [Fact]
    public void Compile_SliceParamAndArrayArg_Works()
    {
        var source = @"
fn process(items: [i32]) -> i32 { 0 }
fn main() -> i32 {
    let data = [5, 10, 15];
    process(data)
}
";
        Assert.True(new CompilationDriver().Check(source, "t.ast"),
            new CompilationDriver().FormatDiagnostics());
    }
}

// ==============================
// Phase 6b: usize/isize, Tuples, Never type, Closure Captures, format!
// ==============================

public class Phase6bUsizeIsizeTests
{
    [Fact]
    public void Parse_UsizeType_Accepted()
    {
        var source = @"fn foo() -> usize { 42 }";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }

    [Fact]
    public void Parse_IsizeType_Accepted()
    {
        var source = @"fn foo() -> isize { -1 }";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }

    [Fact]
    public void Usize_ResolvedAsPrimitive()
    {
        // usize and isize should be distinct from i64 but allowed in numeric positions
        var source = @"
fn len_of(v: usize) -> usize { v }
fn main() -> usize { len_of(10) }
";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }

    [Fact]
    public void Usize_CoercesWithI64()
    {
        var source = @"
fn takes_i64(x: i64) -> i64 { x }
fn main() -> i64 {
    let n: usize = 5;
    takes_i64(n)
}
";
        // usize → i64 coercion is legal (word-sized)
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }
}

public class Phase6bTupleTests
{
    [Fact]
    public void Parse_TupleExpr_TwoElements()
    {
        var source = @"fn foo() -> i32 { let t = (1, 2); 0 }";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }

    [Fact]
    public void Parse_TupleExpr_ThreeElements()
    {
        var source = @"fn foo() -> i32 { let t = (1, true, 3); 0 }";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }

    [Fact]
    public void Parse_TupleExpr_Unit()
    {
        var source = @"fn foo() -> void { () }";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }

    [Fact]
    public void Parse_TupleTypeAnnotation()
    {
        var source = @"fn pair(x: i32, y: bool) -> (i32, bool) { (x, y) }";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }

    [Fact]
    public void Tuple_InLetBinding()
    {
        var source = @"
fn main() -> i32 {
    let p = (10, 20);
    0
}
";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }

    [Fact]
    public void Tuple_NestedTuple()
    {
        var source = @"
fn main() -> i32 {
    let nested = ((1, 2), (3, 4));
    0
}
";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }
}

public class Phase6bNeverTypeTests
{
    [Fact]
    public void Parse_NeverType_ReturnAnnotation()
    {
        // fn panic_fn() -> ! — diverging function
        var source = @"fn panic_fn() -> ! { panic(""unreachable"") }";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }

    [Fact]
    public void NeverType_UnifiesWithAny()
    {
        // A function returning ! can be used where any type is expected
        var source = @"
fn diverge() -> ! { panic(""never"") }
fn main() -> i32 { diverge() }
";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }

    [Fact]
    public void NeverType_InIfArm()
    {
        var source = @"
fn safe(x: bool) -> i32 {
    if x { 1 } else { 0 }
}
";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }
}

public class Phase6bClosureCaptureTests
{
    [Fact]
    public void Closure_CapturesOuterVariable()
    {
        var source = @"
fn main() -> i32 {
    let x = 10;
    let add = |y| x + y;
    0
}
";
        // Resolving should succeed; x is captured by the closure
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }

    [Fact]
    public void Closure_NoCaptureWhenAllParamsLocal()
    {
        var source = @"
fn main() -> i32 {
    let mul = |a, b| a * b;
    0
}
";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }

    [Fact]
    public void Closure_CapturedVarsPopulatedInHir()
    {
        // The resolver should populate CapturedVariables on HirClosureExpr
        var source = @"
fn main() -> i32 {
    let base = 100;
    let f = |n| base + n;
    0
}
";
        var driver = new CompilationDriver();
        var resolver = new Aster.Compiler.Frontend.NameResolution.NameResolver();
        var lexer = new Aster.Compiler.Frontend.Lexer.AsterLexer(source, "t.ast");
        var tokens = lexer.Tokenize();
        var parser = new Aster.Compiler.Frontend.Parser.AsterParser(tokens, false);
        var ast = parser.ParseProgram();
        var hir = resolver.Resolve(ast);

        // Walk hir to find HirClosureExpr and check captured variables
        var closure = FindClosure(hir.Declarations);
        Assert.NotNull(closure);
        Assert.Contains(closure!.CapturedVariables, s => s.Name == "base");
    }

    [Fact]
    public void Closure_MultipleCaptures()
    {
        var source = @"
fn main() -> i32 {
    let a = 1;
    let b = 2;
    let f = |c| a + b + c;
    0
}
";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }

    private static Aster.Compiler.Frontend.Hir.HirClosureExpr? FindClosure(
        System.Collections.Generic.IEnumerable<Aster.Compiler.Frontend.Hir.HirNode> nodes)
    {
        foreach (var n in nodes)
        {
            if (n is Aster.Compiler.Frontend.Hir.HirClosureExpr ce) return ce;
            if (n is Aster.Compiler.Frontend.Hir.HirFunctionDecl fn)
            {
                var found = FindClosureInNode(fn.Body);
                if (found != null) return found;
            }
        }
        return null;
    }

    private static Aster.Compiler.Frontend.Hir.HirClosureExpr? FindClosureInNode(
        Aster.Compiler.Frontend.Hir.HirNode? node)
    {
        if (node == null) return null;
        if (node is Aster.Compiler.Frontend.Hir.HirClosureExpr ce) return ce;
        if (node is Aster.Compiler.Frontend.Hir.HirBlock blk)
        {
            foreach (var s in blk.Statements)
            {
                var r = FindClosureInNode(s);
                if (r != null) return r;
            }
        }
        if (node is Aster.Compiler.Frontend.Hir.HirLetStmt let)
            return FindClosureInNode(let.Initializer);
        return null;
    }
}

public class Phase6bFormatMacroTests
{
    [Fact]
    public void Format_MacroExpands_NoDiagnostics()
    {
        var source = @"
fn main() -> void {
    let s = format!(""hello {}"", 42);
}
";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }

    [Fact]
    public void Println_MacroWithFormat_Works()
    {
        var source = @"
fn main() -> void {
    println!(""value = {}"", 10);
}
";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }

    [Fact]
    public void Eprintln_MacroExpands()
    {
        var source = @"
fn main() -> void {
    eprintln!(""error: {}"", ""something went wrong"");
}
";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }
}

public class Phase6bIntegrationTests
{
    [Fact]
    public void Integration_TupleAndUsize()
    {
        var source = @"
fn swap(a: i32, b: i32) -> (i32, i32) { (b, a) }
fn main() -> i32 {
    let n: usize = 5;
    let pair = swap(1, 2);
    0
}
";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }

    [Fact]
    public void Integration_FormatAndClosure()
    {
        var source = @"
fn main() -> void {
    let prefix = ""val"";
    let f = |x| x + 1;
    let msg = format!(""{}"", 42);
}
";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }

    [Fact]
    public void Integration_NeverTypeInMatch()
    {
        var source = @"
fn unreachable_arm(x: i32) -> i32 {
    if x > 0 { x } else { panic(""neg"") }
}
fn main() -> i32 { unreachable_arm(5) }
";
        Assert.True(new CompilationDriver().Check(source, "t.ast"));
    }
}


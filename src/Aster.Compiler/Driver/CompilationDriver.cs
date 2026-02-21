using Aster.Compiler.Backends.Abstractions;
using Aster.Compiler.Backends.LLVM;
using Aster.Compiler.Diagnostics;
using Aster.Compiler.Frontend.Ast;
using Aster.Compiler.Frontend.Effects;
using Aster.Compiler.Frontend.Hir;
using Aster.Compiler.Frontend.Lexer;
using Aster.Compiler.Frontend.NameResolution;
using Aster.Compiler.Frontend.Parser;
using Aster.Compiler.Frontend.TypeSystem;
using Aster.Compiler.MiddleEnd.AsyncLowering;
using Aster.Compiler.MiddleEnd.BorrowChecker;
using Aster.Compiler.MiddleEnd.DropLowering;
using Aster.Compiler.MiddleEnd.Generics;
using Aster.Compiler.MiddleEnd.Mir;
using Aster.Compiler.MiddleEnd.Optimizations;
using Aster.Compiler.MiddleEnd.PatternLowering;
using Aster.Compiler.MiddleEnd.PatternMatching;

namespace Aster.Compiler.Driver;

/// <summary>
/// Compilation pipeline driver.
/// Orchestrates the full compilation process:
/// Lexer → Parser → AST → HIR → TypeCheck → Monomorphize → Effects → BorrowCheck → MIR → LLVM → Binary
/// </summary>
public sealed class CompilationDriver
{
    private readonly DiagnosticBag _diagnostics = new();
    private readonly bool _stage1Mode;
    private MonomorphizationTable? _monoTable;

    /// <summary>All diagnostics from the compilation.</summary>
    public DiagnosticBag Diagnostics => _diagnostics;

    /// <summary>Whether Stage1 (Core-0) mode is enabled.</summary>
    public bool Stage1Mode => _stage1Mode;

    /// <summary>
    /// Monomorphization table populated after <see cref="Compile"/> or <see cref="Check"/> runs.
    /// Contains all generic instantiations discovered during compilation.
    /// </summary>
    public MonomorphizationTable? MonomorphizationTable => _monoTable;

    /// <summary>
    /// Create a new compilation driver.
    /// </summary>
    /// <param name="stage1Mode">If true, enforces Stage1 (Core-0) language subset restrictions.</param>
    public CompilationDriver(bool stage1Mode = false)
    {
        _stage1Mode = stage1Mode;
    }

    /// <summary>
    /// Compile source code through the entire pipeline.
    /// Returns the LLVM IR string on success, null on failure.
    /// </summary>
    public string? Compile(string source, string fileName)
    {
        // Phase 1: Lexing
        var lexer = new AsterLexer(source, fileName);
        var tokens = lexer.Tokenize();
        _diagnostics.AddRange(lexer.Diagnostics);

        if (_diagnostics.HasErrors)
            return null;

        // Phase 2: Parsing
        var parser = new AsterParser(tokens, _stage1Mode);
        var ast = parser.ParseProgram();
        _diagnostics.AddRange(parser.Diagnostics);

        if (_diagnostics.HasErrors)
            return null;

        // Phase 3: Name Resolution → HIR
        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);
        _diagnostics.AddRange(resolver.Diagnostics);

        if (_diagnostics.HasErrors)
            return null;

        // Phase 4: Type Checking
        var typeChecker = new TypeChecker();
        typeChecker.Check(hir);
        _diagnostics.AddRange(typeChecker.Diagnostics);

        if (_diagnostics.HasErrors)
            return null;

        // Phase 4b: Pattern exhaustiveness checking (Phase 3 completion)
        RunPatternChecker(hir, typeChecker);

        // Phase 5: Monomorphization (Week 11)
        // Collects all generic instantiations; the table is available for downstream phases.
        var monomorphizer = new Monomorphizer();
        _monoTable = monomorphizer.Run(hir);

        // Phase 6: Effect Checking
        var effectChecker = new EffectChecker();
        effectChecker.Check(hir);
        _diagnostics.AddRange(effectChecker.Diagnostics);

        // Phase 6: MIR Lowering
        var mirLowering = new MirLowering();
        var mir = mirLowering.Lower(hir);
        _diagnostics.AddRange(mirLowering.Diagnostics);

        if (_diagnostics.HasErrors)
            return null;

        // Phase 6b: Optimization passes (Phase 3 — constant folding + DCE; Phase 4 — CSE; Phase 5 — LICM + inlining + SROA)
        new ConstantFolder().Fold(mir);
        new DeadCodeEliminator().Eliminate(mir);
        new CsePass().Eliminate(mir);
        new LicmPass().Hoist(mir);
        new InliningPass().Inline(mir);
        new SroaPass().Replace(mir);

        // Phase 7: Pattern Lowering
        var patternLower = new PatternLower();
        patternLower.Lower(mir);

        // Phase 8: Drop Lowering
        var dropLower = new DropLower();
        dropLower.Lower(mir);

        // Phase 8b: Async Lowering (Phase 3 completion — lowers async state machines)
        var asyncLower = new AsyncLower();
        asyncLower.Lower(mir);
        _diagnostics.AddRange(asyncLower.Diagnostics);

        // Phase 9: Borrow Checking
        var borrowChecker = new BorrowCheck();
        borrowChecker.Check(mir);
        _diagnostics.AddRange(borrowChecker.Diagnostics);

        if (_diagnostics.HasErrors)
            return null;

        // Phase 10: LLVM IR Emission
        IBackend backend = new LlvmBackend(_stage1Mode);
        var llvmIr = backend.Emit(mir);

        return llvmIr;
    }

    /// <summary>
    /// Check source code without emitting code (type checking only).
    /// </summary>
    public bool Check(string source, string fileName)
    {
        var lexer = new AsterLexer(source, fileName);
        var tokens = lexer.Tokenize();
        _diagnostics.AddRange(lexer.Diagnostics);
        if (_diagnostics.HasErrors) return false;

        var parser = new AsterParser(tokens, _stage1Mode);
        var ast = parser.ParseProgram();
        _diagnostics.AddRange(parser.Diagnostics);
        if (_diagnostics.HasErrors) return false;

        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);
        _diagnostics.AddRange(resolver.Diagnostics);
        if (_diagnostics.HasErrors) return false;

        var typeChecker = new TypeChecker();
        typeChecker.Check(hir);
        _diagnostics.AddRange(typeChecker.Diagnostics);

        if (!_diagnostics.HasErrors)
        {
            _monoTable = new Monomorphizer().Run(hir);
        }

        return !_diagnostics.HasErrors;
    }

    /// <summary>Format diagnostics for console output.</summary>
    public string FormatDiagnostics()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var diag in _diagnostics)
        {
            sb.AppendLine(diag.ToString());
            foreach (var note in diag.Notes)
            {
                sb.AppendLine($"  note: {note}");
            }
            if (diag.SuggestedFix != null)
            {
                sb.AppendLine($"  fix: {diag.SuggestedFix}");
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Tokenize source code and return tokens.
    /// Used for differential testing (bootstrap stage 1).
    /// </summary>
    public IReadOnlyList<Token>? EmitTokens(string source, string fileName)
    {
        var lexer = new AsterLexer(source, fileName);
        var tokens = lexer.Tokenize();
        _diagnostics.AddRange(lexer.Diagnostics);

        if (_diagnostics.HasErrors)
            return null;

        return tokens;
    }

    /// <summary>
    /// Parse source code and return the AST.
    /// Used for differential testing (bootstrap stage 1).
    /// </summary>
    public ProgramNode? EmitAst(string source, string fileName)
    {
        var tokens = LexSource(source, fileName);
        if (tokens == null)
            return null;

        var parser = new AsterParser(tokens, _stage1Mode);
        var ast = parser.ParseProgram();
        _diagnostics.AddRange(parser.Diagnostics);

        if (_diagnostics.HasErrors)
            return null;

        return ast;
    }

    /// <summary>
    /// Perform name resolution and return HIR.
    /// Used for differential testing (bootstrap stage 1).
    /// </summary>
    public HirProgram? EmitSymbols(string source, string fileName)
    {
        var ast = EmitAst(source, fileName);
        if (ast == null)
            return null;

        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);
        _diagnostics.AddRange(resolver.Diagnostics);

        if (_diagnostics.HasErrors)
            return null;

        return hir;
    }

    /// <summary>
    /// Private helper to lex source code and handle diagnostics.
    /// Returns null if lexing fails.
    /// </summary>
    private IReadOnlyList<Token>? LexSource(string source, string fileName)
    {
        var lexer = new AsterLexer(source, fileName);
        var tokens = lexer.Tokenize();
        _diagnostics.AddRange(lexer.Diagnostics);

        if (_diagnostics.HasErrors)
            return null;

        return tokens;
    }

    /// <summary>
    /// Walk the HIR and run exhaustiveness/reachability checking on every match expression.
    /// Warnings are added to <see cref="Diagnostics"/>; errors are rare (only empty-match).
    /// </summary>
    private void RunPatternChecker(HirProgram hir, TypeChecker typeChecker)
    {
        var checker = new PatternChecker();
        WalkForMatches(hir.Declarations, checker);
        _diagnostics.AddRange(checker.Diagnostics);
    }

    private static void WalkForMatches(IEnumerable<HirNode> nodes, PatternChecker checker)
    {
        foreach (var node in nodes)
        {
            if (node is HirMatchExpr match)
            {
                // Build a list of (Pattern, Span) pairs for the checker
                var arms = match.Arms.Select(a => (ToCheckerPattern(a.Pattern), a.Span)).ToList();
                // Use a generic scrutinee type (we pass void as a placeholder; checker uses wildcards)
                checker.CheckMatch(PrimitiveType.Void, arms);
            }

            // Recurse into child nodes
            WalkNodeForMatches(node, checker);
        }
    }

    private static void WalkNodeForMatches(HirNode node, PatternChecker checker)
    {
        switch (node)
        {
            case HirFunctionDecl fn: WalkForMatches(fn.Body.Statements, checker); break;
            case HirBlock block: WalkForMatches(block.Statements, checker); break;
            case HirLetStmt let when let.Initializer != null: WalkNodeForMatches(let.Initializer, checker); break;
            case HirExprStmt es: WalkNodeForMatches(es.Expression, checker); break;
            case HirReturnStmt ret when ret.Value != null: WalkNodeForMatches(ret.Value, checker); break;
            case HirIfExpr ifExpr:
                WalkNodeForMatches(ifExpr.Condition, checker);
                WalkForMatches(ifExpr.ThenBranch.Statements, checker);
                if (ifExpr.ElseBranch != null) WalkNodeForMatches(ifExpr.ElseBranch, checker);
                break;
            case HirMatchExpr matchExpr:
                WalkNodeForMatches(matchExpr.Scrutinee, checker);
                foreach (var arm in matchExpr.Arms) WalkNodeForMatches(arm.Body, checker);
                break;
            case HirModuleDecl mod: WalkForMatches(mod.Members, checker); break;
            default: break;
        }
    }

    private static Pattern ToCheckerPattern(HirPattern p)
    {
        return p.Kind switch
        {
            PatternKind.Wildcard => new WildcardPattern(p.Span),
            PatternKind.Variable => new VariablePattern(p.Name ?? "_", false, p.Span),
            PatternKind.Literal  => new LiteralPattern(p.LiteralValue ?? 0, p.Span),
            PatternKind.Constructor => new ConstructorPattern(
                p.Constructor ?? "_",
                p.SubPatterns.Select(ToCheckerPattern).ToList(),
                p.Span),
            _ => new WildcardPattern(p.Span),
        };
    }
}

using Aster.Compiler.Backends.Abstractions;
using Aster.Compiler.Backends.LLVM;
using Aster.Compiler.Diagnostics;
using Aster.Compiler.Frontend.Effects;
using Aster.Compiler.Frontend.Lexer;
using Aster.Compiler.Frontend.NameResolution;
using Aster.Compiler.Frontend.Parser;
using Aster.Compiler.Frontend.TypeSystem;
using Aster.Compiler.MiddleEnd.BorrowChecker;
using Aster.Compiler.MiddleEnd.DropLowering;
using Aster.Compiler.MiddleEnd.Mir;
using Aster.Compiler.MiddleEnd.PatternLowering;

namespace Aster.Compiler.Driver;

/// <summary>
/// Compilation pipeline driver.
/// Orchestrates the full compilation process:
/// Lexer → Parser → AST → HIR → TypeCheck → Effects → BorrowCheck → MIR → LLVM → Binary
/// </summary>
public sealed class CompilationDriver
{
    private readonly DiagnosticBag _diagnostics = new();
    private readonly bool _stage1Mode;

    /// <summary>All diagnostics from the compilation.</summary>
    public DiagnosticBag Diagnostics => _diagnostics;

    /// <summary>Whether Stage1 (Core-0) mode is enabled.</summary>
    public bool Stage1Mode => _stage1Mode;

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

        // Phase 5: Effect Checking
        var effectChecker = new EffectChecker();
        effectChecker.Check(hir);
        _diagnostics.AddRange(effectChecker.Diagnostics);

        // Phase 6: MIR Lowering
        var mirLowering = new MirLowering();
        var mir = mirLowering.Lower(hir);
        _diagnostics.AddRange(mirLowering.Diagnostics);

        if (_diagnostics.HasErrors)
            return null;

        // Phase 7: Pattern Lowering
        var patternLower = new PatternLower();
        patternLower.Lower(mir);

        // Phase 8: Drop Lowering
        var dropLower = new DropLower();
        dropLower.Lower(mir);

        // Phase 9: Borrow Checking
        var borrowChecker = new BorrowCheck();
        borrowChecker.Check(mir);
        _diagnostics.AddRange(borrowChecker.Diagnostics);

        if (_diagnostics.HasErrors)
            return null;

        // Phase 10: LLVM IR Emission
        IBackend backend = new LlvmBackend();
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
    public object? EmitAst(string source, string fileName)
    {
        var lexer = new AsterLexer(source, fileName);
        var tokens = lexer.Tokenize();
        _diagnostics.AddRange(lexer.Diagnostics);

        if (_diagnostics.HasErrors)
            return null;

        var parser = new AsterParser(tokens, _stage1Mode);
        var ast = parser.ParseProgram();
        _diagnostics.AddRange(parser.Diagnostics);

        if (_diagnostics.HasErrors)
            return null;

        return ast;
    }

    /// <summary>
    /// Perform name resolution and return the symbol table.
    /// Used for differential testing (bootstrap stage 1).
    /// </summary>
    public object? EmitSymbols(string source, string fileName)
    {
        var lexer = new AsterLexer(source, fileName);
        var tokens = lexer.Tokenize();
        _diagnostics.AddRange(lexer.Diagnostics);

        if (_diagnostics.HasErrors)
            return null;

        var parser = new AsterParser(tokens, _stage1Mode);
        var ast = parser.ParseProgram();
        _diagnostics.AddRange(parser.Diagnostics);

        if (_diagnostics.HasErrors)
            return null;

        var resolver = new NameResolver();
        var hir = resolver.Resolve(ast);
        _diagnostics.AddRange(resolver.Diagnostics);

        if (_diagnostics.HasErrors)
            return null;

        return hir;
    }
}

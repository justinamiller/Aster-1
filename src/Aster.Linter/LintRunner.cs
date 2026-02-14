using Aster.Compiler.Frontend.Ast;
using Aster.Compiler.Frontend.Lexer;
using Aster.Compiler.Frontend.Parser;

namespace Aster.Linter;

/// <summary>
/// Runs registered lint rules against Aster source code.
/// </summary>
public sealed class LintRunner
{
    private readonly List<ILintRule> _rules = new();

    /// <summary>
    /// Register a lint rule.
    /// </summary>
    public void AddRule(ILintRule rule) => _rules.Add(rule);

    /// <summary>
    /// Get all registered rules.
    /// </summary>
    public IReadOnlyList<ILintRule> Rules => _rules;

    /// <summary>
    /// Run all lint rules against source code.
    /// Returns lint diagnostics, or empty if source fails to parse.
    /// </summary>
    public IReadOnlyList<LintDiagnostic> Lint(string source, string fileName = "<stdin>")
    {
        var lexer = new AsterLexer(source, fileName);
        var tokens = lexer.Tokenize();
        if (lexer.Diagnostics.HasErrors)
            return Array.Empty<LintDiagnostic>();

        var parser = new AsterParser(tokens);
        var program = parser.ParseProgram();
        if (parser.Diagnostics.HasErrors)
            return Array.Empty<LintDiagnostic>();

        return Lint(program);
    }

    /// <summary>
    /// Run all lint rules against an already-parsed program.
    /// </summary>
    public IReadOnlyList<LintDiagnostic> Lint(ProgramNode program)
    {
        var results = new List<LintDiagnostic>();
        foreach (var rule in _rules)
        {
            results.AddRange(rule.Check(program));
        }
        // Deterministic ordering by span, then lint ID
        results.Sort((a, b) =>
        {
            var spanCmp = a.Span.Start.CompareTo(b.Span.Start);
            return spanCmp != 0 ? spanCmp : string.Compare(a.LintId, b.LintId, StringComparison.Ordinal);
        });
        return results;
    }

    /// <summary>
    /// Create a LintRunner with all built-in rules registered.
    /// </summary>
    public static LintRunner CreateDefault()
    {
        var runner = new LintRunner();
        runner.AddRule(new Rules.UnusedVariableRule());
        runner.AddRule(new Rules.UnreachableCodeRule());
        return runner;
    }
}

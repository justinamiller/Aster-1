using Aster.Compiler.Frontend.Ast;

namespace Aster.Linter;

/// <summary>
/// Interface for a lint rule that checks AST nodes.
/// </summary>
public interface ILintRule
{
    /// <summary>Stable lint ID.</summary>
    string LintId { get; }

    /// <summary>Short description of the rule.</summary>
    string Description { get; }

    /// <summary>Default severity.</summary>
    LintSeverity DefaultSeverity { get; }

    /// <summary>Run the lint rule against a parsed program.</summary>
    IReadOnlyList<LintDiagnostic> Check(ProgramNode program);
}

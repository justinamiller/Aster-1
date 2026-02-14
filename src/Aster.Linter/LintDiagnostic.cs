using Aster.Compiler.Diagnostics;

namespace Aster.Linter;

/// <summary>
/// A lint diagnostic with a stable lint ID, severity, and optional auto-fix.
/// </summary>
public sealed class LintDiagnostic
{
    /// <summary>Stable lint ID, e.g., "L0001".</summary>
    public string LintId { get; }

    /// <summary>Human-readable message.</summary>
    public string Message { get; }

    /// <summary>Source location.</summary>
    public Span Span { get; }

    /// <summary>Severity level: Info, Warning, or Error.</summary>
    public LintSeverity Severity { get; }

    /// <summary>Optional suggested fix text.</summary>
    public string? SuggestedFix { get; }

    public LintDiagnostic(string lintId, string message, Span span, LintSeverity severity, string? suggestedFix = null)
    {
        LintId = lintId;
        Message = message;
        Span = span;
        Severity = severity;
        SuggestedFix = suggestedFix;
    }

    public override string ToString()
    {
        var level = Severity switch
        {
            LintSeverity.Error => "error",
            LintSeverity.Warning => "warning",
            _ => "info"
        };
        return $"{Span} {level}[{LintId}]: {Message}";
    }
}

/// <summary>
/// Lint severity level.
/// </summary>
public enum LintSeverity
{
    Info,
    Warning,
    Error
}

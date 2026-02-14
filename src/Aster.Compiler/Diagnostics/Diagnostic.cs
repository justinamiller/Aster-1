namespace Aster.Compiler.Diagnostics;

/// <summary>
/// Represents a single compiler diagnostic with an error code, message, span, and severity.
/// </summary>
public sealed class Diagnostic
{
    /// <summary>Error code such as "E0001".</summary>
    public string Code { get; }

    /// <summary>Primary human-readable message.</summary>
    public string Message { get; }

    /// <summary>Source location of the diagnostic.</summary>
    public Span Span { get; }

    /// <summary>Severity level.</summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>Optional secondary notes providing additional context.</summary>
    public IReadOnlyList<string> Notes { get; }

    /// <summary>Optional suggested fix.</summary>
    public string? SuggestedFix { get; }

    public Diagnostic(
        string code,
        string message,
        Span span,
        DiagnosticSeverity severity,
        IReadOnlyList<string>? notes = null,
        string? suggestedFix = null)
    {
        Code = code;
        Message = message;
        Span = span;
        Severity = severity;
        Notes = notes ?? Array.Empty<string>();
        SuggestedFix = suggestedFix;
    }

    public override string ToString()
    {
        var level = Severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Hint => "hint",
            _ => "info",
        };
        return $"{Span}\n{level}[{Code}]: {Message}";
    }
}

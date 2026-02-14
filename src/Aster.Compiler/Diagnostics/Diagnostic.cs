namespace Aster.Compiler.Diagnostics;

/// <summary>
/// Represents a single compiler diagnostic with an error code, message, span, and severity.
/// Supports rich diagnostic information including secondary spans, help text, and categorization.
/// </summary>
public sealed class Diagnostic
{
    /// <summary>Stable diagnostic code such as "E3124".</summary>
    public string Code { get; }

    /// <summary>Severity level.</summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>Short title summarizing the error.</summary>
    public string Title { get; }

    /// <summary>Detailed diagnostic message.</summary>
    public string Message { get; }

    /// <summary>Primary source location.</summary>
    public Span PrimarySpan { get; }

    /// <summary>Optional secondary spans providing additional context.</summary>
    public IReadOnlyList<SecondarySpan> SecondarySpans { get; }

    /// <summary>Optional help text suggesting how to fix the issue.</summary>
    public string? Help { get; }

    /// <summary>Optional additional notes providing context.</summary>
    public IReadOnlyList<string> Notes { get; }

    /// <summary>Diagnostic category for grouping and filtering.</summary>
    public DiagnosticCategory Category { get; }

    /// <summary>Legacy: Primary span (for backward compatibility).</summary>
    public Span Span => PrimarySpan;

    /// <summary>Legacy: Suggested fix (for backward compatibility).</summary>
    public string? SuggestedFix => Help;

    public Diagnostic(
        string code,
        DiagnosticSeverity severity,
        string title,
        string message,
        Span primarySpan,
        DiagnosticCategory category,
        IReadOnlyList<SecondarySpan>? secondarySpans = null,
        string? help = null,
        IReadOnlyList<string>? notes = null)
    {
        Code = code;
        Severity = severity;
        Title = title;
        Message = message;
        PrimarySpan = primarySpan;
        Category = category;
        SecondarySpans = secondarySpans ?? Array.Empty<SecondarySpan>();
        Help = help;
        Notes = notes ?? Array.Empty<string>();
    }

    /// <summary>Legacy constructor for backward compatibility.</summary>
    public Diagnostic(
        string code,
        string message,
        Span span,
        DiagnosticSeverity severity,
        IReadOnlyList<string>? notes = null,
        string? suggestedFix = null)
        : this(
            code,
            severity,
            message,
            message,
            span,
            DiagnosticCategory.Internal,
            null,
            suggestedFix,
            notes)
    {
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
        return $"{PrimarySpan}\n{level}[{Code}]: {Title}";
    }
}

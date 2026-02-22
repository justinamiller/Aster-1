namespace Aster.Compiler.Diagnostics;

/// <summary>
/// A machine-applicable suggestion (fix-it hint) attached to a diagnostic.
/// Describes a source span to replace and the replacement text to use.
/// </summary>
public sealed class DiagnosticSuggestion
{
    /// <summary>The span of source text to replace.</summary>
    public Span Span { get; }

    /// <summary>The replacement text to insert at <see cref="Span"/>.</summary>
    public string Replacement { get; }

    /// <summary>Human-readable description of the suggestion (e.g. "consider adding a semicolon").</summary>
    public string Message { get; }

    /// <summary>Whether this suggestion can be applied automatically without user review.</summary>
    public bool IsMachineApplicable { get; }

    public DiagnosticSuggestion(Span span, string replacement, string message, bool machineApplicable = false)
    {
        Span = span;
        Replacement = replacement;
        Message = message;
        IsMachineApplicable = machineApplicable;
    }
}

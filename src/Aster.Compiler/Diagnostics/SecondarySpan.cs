namespace Aster.Compiler.Diagnostics;

/// <summary>
/// Represents a secondary source location with an optional label.
/// Used to provide additional context in diagnostics.
/// </summary>
public sealed class SecondarySpan
{
    /// <summary>The source location.</summary>
    public Span Span { get; }

    /// <summary>Optional label describing this location's relevance.</summary>
    public string? Label { get; }

    public SecondarySpan(Span span, string? label = null)
    {
        Span = span;
        Label = label;
    }

    public override string ToString()
    {
        return Label != null ? $"{Span} ({Label})" : Span.ToString();
    }
}

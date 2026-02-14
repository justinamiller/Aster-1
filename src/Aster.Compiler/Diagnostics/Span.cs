namespace Aster.Compiler.Diagnostics;

/// <summary>
/// Represents a source location span within a file.
/// Immutable value type tracking file, line, column, start offset, and length.
/// </summary>
public readonly record struct Span(
    string File,
    int Line,
    int Column,
    int Start,
    int Length)
{
    /// <summary>A sentinel span representing an unknown or synthetic location.</summary>
    public static readonly Span Unknown = new("", 0, 0, 0, 0);

    /// <summary>The end offset of the span (exclusive).</summary>
    public int End => Start + Length;

    public override string ToString() => $"{File}:{Line}:{Column}";
}

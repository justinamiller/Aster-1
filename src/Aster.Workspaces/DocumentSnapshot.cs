namespace Aster.Workspaces;

/// <summary>
/// Versioned snapshot of a text document, used by LSP for incremental editing.
/// </summary>
public sealed class DocumentSnapshot
{
    public string Uri { get; }
    public int Version { get; }
    public string Text { get; }
    private readonly string[] _lines;

    public DocumentSnapshot(string uri, int version, string text)
    {
        Uri = uri;
        Version = version;
        Text = text;
        _lines = text.Split('\n');
    }

    /// <summary>
    /// Total number of lines.
    /// </summary>
    public int LineCount => _lines.Length;

    /// <summary>
    /// Get the text of a specific line (0-based).
    /// </summary>
    public string GetLine(int line) =>
        line >= 0 && line < _lines.Length ? _lines[line] : string.Empty;

    /// <summary>
    /// Convert a (line, column) position to an absolute offset.
    /// </summary>
    public int PositionToOffset(int line, int column)
    {
        int offset = 0;
        for (int i = 0; i < line && i < _lines.Length; i++)
            offset += _lines[i].Length + 1; // +1 for \n
        return offset + column;
    }

    /// <summary>
    /// Convert an absolute offset to (line, column).
    /// </summary>
    public (int Line, int Column) OffsetToPosition(int offset)
    {
        int remaining = offset;
        for (int i = 0; i < _lines.Length; i++)
        {
            if (remaining <= _lines[i].Length)
                return (i, remaining);
            remaining -= _lines[i].Length + 1; // +1 for \n
        }
        return (_lines.Length - 1, _lines[^1].Length);
    }

    /// <summary>
    /// Apply a text change and return a new snapshot with incremented version.
    /// </summary>
    public DocumentSnapshot ApplyChange(int startOffset, int length, string newText)
    {
        var newContent = string.Concat(
            Text.AsSpan(0, startOffset),
            newText,
            Text.AsSpan(startOffset + length));
        return new DocumentSnapshot(Uri, Version + 1, newContent);
    }
}

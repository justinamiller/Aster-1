namespace Aster.Formatter;

/// <summary>
/// Document tree representation for pretty-printing.
/// Uses a Wadler-Lindig style layout algorithm.
/// </summary>
public abstract record Doc
{
    /// <summary>Empty document.</summary>
    public static readonly Doc Empty = new DocEmpty();

    /// <summary>A newline that may be flattened to a space.</summary>
    public static readonly Doc Line = new DocLine();

    /// <summary>A newline that may be flattened to nothing.</summary>
    public static readonly Doc SoftLine = new DocSoftLine();

    /// <summary>A forced newline that cannot be flattened.</summary>
    public static readonly Doc HardLine = new DocHardLine();

    /// <summary>Literal text.</summary>
    public static Doc Text(string text) => new DocText(text);

    /// <summary>Concatenation of two documents.</summary>
    public static Doc Concat(Doc left, Doc right) => new DocConcat(left, right);

    /// <summary>Concatenation of multiple documents.</summary>
    public static Doc Concat(params Doc[] docs) =>
        docs.Length == 0 ? Empty : docs.Aggregate(Concat);

    /// <summary>Indent a document by a number of spaces.</summary>
    public static Doc Indent(int amount, Doc doc) => new DocIndent(amount, doc);

    /// <summary>A group that tries to fit on one line, breaking if needed.</summary>
    public static Doc Group(Doc doc) => new DocGroup(doc);

    /// <summary>Join documents with a separator.</summary>
    public static Doc Join(Doc separator, IEnumerable<Doc> docs)
    {
        Doc result = Empty;
        bool first = true;
        foreach (var doc in docs)
        {
            if (!first) result = Concat(result, separator);
            result = Concat(result, doc);
            first = false;
        }
        return result;
    }
}

internal sealed record DocEmpty : Doc;
internal sealed record DocText(string Value) : Doc;
internal sealed record DocConcat(Doc Left, Doc Right) : Doc;
internal sealed record DocIndent(int Amount, Doc Inner) : Doc;
internal sealed record DocGroup(Doc Inner) : Doc;
internal sealed record DocLine : Doc;
internal sealed record DocSoftLine : Doc;
internal sealed record DocHardLine : Doc;

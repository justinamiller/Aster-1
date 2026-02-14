using System.Text;

namespace Aster.Formatter;

/// <summary>
/// Renders a document tree to a formatted string.
/// Implements a best-fit layout algorithm with configurable max line width.
/// </summary>
public sealed class DocRenderer
{
    private readonly int _maxWidth;

    public DocRenderer(int maxWidth = 100)
    {
        _maxWidth = maxWidth;
    }

    public string Render(Doc doc)
    {
        var sb = new StringBuilder();
        var commands = new Stack<(int Indent, bool Flat, Doc Doc)>();
        commands.Push((0, false, doc));
        int column = 0;

        while (commands.Count > 0)
        {
            var (indent, flat, current) = commands.Pop();

            switch (current)
            {
                case DocEmpty:
                    break;

                case DocText text:
                    sb.Append(text.Value);
                    column += text.Value.Length;
                    break;

                case DocConcat concat:
                    commands.Push((indent, flat, concat.Right));
                    commands.Push((indent, flat, concat.Left));
                    break;

                case DocIndent indentDoc:
                    commands.Push((indent + indentDoc.Amount, flat, indentDoc.Inner));
                    break;

                case DocGroup group:
                    if (flat || Fits(_maxWidth - column, new Stack<(int, bool, Doc)>(new[] { (indent, true, group.Inner) })))
                    {
                        commands.Push((indent, true, group.Inner));
                    }
                    else
                    {
                        commands.Push((indent, false, group.Inner));
                    }
                    break;

                case DocLine:
                    if (flat)
                    {
                        sb.Append(' ');
                        column++;
                    }
                    else
                    {
                        sb.AppendLine();
                        sb.Append(' ', indent);
                        column = indent;
                    }
                    break;

                case DocSoftLine:
                    if (flat)
                    {
                        // flatten to nothing
                    }
                    else
                    {
                        sb.AppendLine();
                        sb.Append(' ', indent);
                        column = indent;
                    }
                    break;

                case DocHardLine:
                    sb.AppendLine();
                    sb.Append(' ', indent);
                    column = indent;
                    break;
            }
        }

        return sb.ToString();
    }

    private static bool Fits(int remaining, Stack<(int Indent, bool Flat, Doc Doc)> commands)
    {
        while (remaining >= 0 && commands.Count > 0)
        {
            var (indent, flat, doc) = commands.Pop();
            switch (doc)
            {
                case DocEmpty:
                    break;
                case DocText text:
                    remaining -= text.Value.Length;
                    break;
                case DocConcat concat:
                    commands.Push((indent, flat, concat.Right));
                    commands.Push((indent, flat, concat.Left));
                    break;
                case DocIndent indentDoc:
                    commands.Push((indent + indentDoc.Amount, flat, indentDoc.Inner));
                    break;
                case DocGroup group:
                    commands.Push((indent, flat, group.Inner));
                    break;
                case DocLine:
                    if (flat) remaining--;
                    else return true;
                    break;
                case DocSoftLine:
                    if (!flat) return true;
                    break;
                case DocHardLine:
                    return true;
            }
        }
        return remaining >= 0;
    }
}

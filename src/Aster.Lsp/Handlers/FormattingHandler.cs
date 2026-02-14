using Aster.Formatter;
using Aster.Lsp.Protocol;
using Aster.Workspaces;

namespace Aster.Lsp.Handlers;

/// <summary>
/// Handles document formatting requests for the LSP server.
/// </summary>
public sealed class FormattingHandler
{
    private readonly AsterFormatter _formatter = new();

    public List<TextEdit> Format(DocumentSnapshot snapshot)
    {
        var formatted = _formatter.Format(snapshot.Text, snapshot.Uri);

        if (formatted == snapshot.Text)
            return new();

        // Return a single edit replacing the entire document
        var lines = snapshot.Text.Split('\n');
        var lastLine = lines.Length - 1;
        var lastChar = lines[^1].Length;

        return new List<TextEdit>
        {
            new TextEdit
            {
                Range = new LspRange
                {
                    Start = new LspPosition { Line = 0, Character = 0 },
                    End = new LspPosition { Line = lastLine, Character = lastChar }
                },
                NewText = formatted
            }
        };
    }
}

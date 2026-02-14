namespace Aster.Compiler.Diagnostics.Rendering;

/// <summary>
/// Renders diagnostics in a human-readable format with optional ANSI color support.
/// Produces output similar to Rust's compiler diagnostics.
/// </summary>
public sealed class HumanDiagnosticRenderer
{
    private readonly bool _useColor;
    private readonly ISourceReader? _sourceReader;

    public HumanDiagnosticRenderer(bool useColor = true, ISourceReader? sourceReader = null)
    {
        _useColor = useColor;
        _sourceReader = sourceReader;
    }

    public string Render(Diagnostic diagnostic)
    {
        var sb = new System.Text.StringBuilder();

        // Header: file:line:col
        sb.AppendLine(diagnostic.PrimarySpan.ToString());

        // Severity and code: error[E3124]: title
        var severityText = diagnostic.Severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Info => "info",
            DiagnosticSeverity.Hint => "hint",
            _ => "diagnostic"
        };

        if (_useColor)
        {
            var color = diagnostic.Severity switch
            {
                DiagnosticSeverity.Error => AnsiColor.Red,
                DiagnosticSeverity.Warning => AnsiColor.Yellow,
                DiagnosticSeverity.Info => AnsiColor.Cyan,
                DiagnosticSeverity.Hint => AnsiColor.Cyan,
                _ => AnsiColor.White
            };

            sb.Append(Colorize($"{severityText}[{diagnostic.Code}]: ", color, bold: true));
            sb.AppendLine(Colorize(diagnostic.Title, color, bold: true));
        }
        else
        {
            sb.AppendLine($"{severityText}[{diagnostic.Code}]: {diagnostic.Title}");
        }

        // Source context
        if (_sourceReader != null && diagnostic.PrimarySpan.File.Length > 0)
        {
            RenderSourceContext(sb, diagnostic);
        }
        else
        {
            // Simple message if no source available
            if (diagnostic.Message != diagnostic.Title)
            {
                sb.AppendLine($"  {diagnostic.Message}");
            }
        }

        // Help text
        if (!string.IsNullOrEmpty(diagnostic.Help))
        {
            if (_useColor)
            {
                sb.Append(Colorize("help: ", AnsiColor.Green, bold: true));
                sb.AppendLine(diagnostic.Help);
            }
            else
            {
                sb.AppendLine($"help: {diagnostic.Help}");
            }
        }

        // Notes
        foreach (var note in diagnostic.Notes)
        {
            if (_useColor)
            {
                sb.Append(Colorize("note: ", AnsiColor.Cyan, bold: true));
                sb.AppendLine(note);
            }
            else
            {
                sb.AppendLine($"note: {note}");
            }
        }

        return sb.ToString();
    }

    private void RenderSourceContext(System.Text.StringBuilder sb, Diagnostic diagnostic)
    {
        var sourceLine = _sourceReader?.GetLine(diagnostic.PrimarySpan.File, diagnostic.PrimarySpan.Line);
        if (sourceLine == null) return;

        sb.AppendLine($"  --> {diagnostic.PrimarySpan}");
        sb.AppendLine("   |");

        var lineNum = diagnostic.PrimarySpan.Line.ToString();
        var padding = new string(' ', lineNum.Length);

        // Show source line
        sb.AppendLine($"{lineNum} | {sourceLine}");

        // Show primary span marker
        var col = diagnostic.PrimarySpan.Column;
        var len = diagnostic.PrimarySpan.Length;
        var marker = new string(' ', col) + new string('^', Math.Max(1, len));
        
        if (_useColor)
        {
            var color = diagnostic.Severity == DiagnosticSeverity.Error 
                ? AnsiColor.Red 
                : AnsiColor.Yellow;
            sb.Append($"{padding} | ");
            sb.Append(Colorize(marker, color, bold: true));
            if (diagnostic.Message != diagnostic.Title)
            {
                sb.Append(" ");
                sb.Append(Colorize(diagnostic.Message, color, bold: false));
            }
            sb.AppendLine();
        }
        else
        {
            sb.Append($"{padding} | {marker}");
            if (diagnostic.Message != diagnostic.Title)
            {
                sb.Append($" {diagnostic.Message}");
            }
            sb.AppendLine();
        }

        // Show secondary spans
        foreach (var secondary in diagnostic.SecondarySpans)
        {
            if (secondary.Span.File == diagnostic.PrimarySpan.File &&
                secondary.Span.Line == diagnostic.PrimarySpan.Line)
            {
                var secMarker = new string(' ', secondary.Span.Column) + 
                                new string('-', Math.Max(1, secondary.Span.Length));
                sb.Append($"{padding} | {secMarker}");
                if (secondary.Label != null)
                {
                    sb.Append($" {secondary.Label}");
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine("   |");
    }

    private string Colorize(string text, AnsiColor color, bool bold)
    {
        if (!_useColor) return text;

        var code = (int)color;
        var boldCode = bold ? "1;" : "";
        return $"\x1b[{boldCode}{code}m{text}\x1b[0m";
    }

    public string RenderAll(IEnumerable<Diagnostic> diagnostics)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var diag in diagnostics)
        {
            sb.Append(Render(diag));
            sb.AppendLine();
        }
        return sb.ToString();
    }
}

/// <summary>
/// Interface for reading source code lines for diagnostic rendering.
/// </summary>
public interface ISourceReader
{
    string? GetLine(string file, int line);
}

/// <summary>
/// ANSI color codes for terminal output.
/// </summary>
internal enum AnsiColor
{
    Black = 30,
    Red = 31,
    Green = 32,
    Yellow = 33,
    Blue = 34,
    Magenta = 35,
    Cyan = 36,
    White = 37
}

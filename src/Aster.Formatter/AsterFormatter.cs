using Aster.Compiler.Frontend.Ast;
using Aster.Compiler.Frontend.Lexer;
using Aster.Compiler.Frontend.Parser;

namespace Aster.Formatter;

/// <summary>
/// Formats Aster source code to the canonical style.
/// Parses source -> builds doc tree -> renders to formatted string.
/// One official style, no configuration.
/// </summary>
public sealed class AsterFormatter
{
    private readonly DocRenderer _renderer;

    /// <summary>
    /// Create a formatter with default max line width of 100.
    /// </summary>
    public AsterFormatter(int maxWidth = 100)
    {
        _renderer = new DocRenderer(maxWidth);
    }

    /// <summary>
    /// Format source code. Returns the formatted string.
    /// If the source fails to parse, returns the original source unchanged.
    /// </summary>
    public string Format(string source, string fileName = "<stdin>")
    {
        try
        {
            var lexer = new AsterLexer(source, fileName);
            var tokens = lexer.Tokenize();

            if (lexer.Diagnostics.HasErrors)
                return source;

            var parser = new AsterParser(tokens);
            var ast = parser.ParseProgram();

            if (parser.Diagnostics.HasErrors)
                return source;

            var builder = new DocBuilder();
            var doc = builder.Build(ast);
            return _renderer.Render(doc);
        }
        catch
        {
            // If anything fails during formatting, return original source
            return source;
        }
    }
}

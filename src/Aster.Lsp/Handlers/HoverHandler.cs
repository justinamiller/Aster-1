using Aster.Compiler.Frontend.Lexer;
using Aster.Compiler.Frontend.Parser;
using Aster.Compiler.Frontend.Ast;
using Aster.Lsp.Protocol;

namespace Aster.Lsp.Handlers;

/// <summary>
/// Provides hover information for the LSP server.
/// </summary>
public sealed class HoverHandler
{
    public Hover? GetHover(string source, string uri, int line, int character)
    {
        try
        {
            var lexer = new AsterLexer(source, uri);
            var tokens = lexer.Tokenize();
            if (lexer.Diagnostics.HasErrors) return null;

            // Find the token at the given position (line is 0-based from LSP, 1-based in compiler)
            var targetLine = line + 1;
            var targetCol = character + 1;

            var token = tokens.FirstOrDefault(t =>
                t.Span.Line == targetLine &&
                t.Span.Column <= targetCol &&
                t.Span.Column + t.Span.Length > targetCol);

            if (token.Kind == default) return null;

            // Parse to get AST for type info
            var parser = new AsterParser(tokens);
            var program = parser.ParseProgram();

            // Find declaration at position
            var info = FindDeclarationInfo(program, token.Span);
            if (info == null) return null;

            return new Hover
            {
                Contents = new MarkupContent
                {
                    Kind = "markdown",
                    Value = info
                }
            };
        }
        catch
        {
            return null;
        }
    }

    private string? FindDeclarationInfo(ProgramNode program, Aster.Compiler.Diagnostics.Span span)
    {
        foreach (var decl in program.Declarations)
        {
            switch (decl)
            {
                case FunctionDeclNode fn when fn.Span.Line == span.Line:
                    return $"```aster\nfn {fn.Name}({string.Join(", ", fn.Parameters.Select(p => $"{p.Name}: {p.TypeAnnotation?.Name ?? "unknown"}"))}) -> {fn.ReturnType?.Name ?? "void"}\n```";
                case StructDeclNode s when s.Span.Line == span.Line:
                    return $"```aster\nstruct {s.Name}\n```";
                case EnumDeclNode e when e.Span.Line == span.Line:
                    return $"```aster\nenum {e.Name}\n```";
                case TraitDeclNode t when t.Span.Line == span.Line:
                    return $"```aster\ntrait {t.Name}\n```";
            }
        }
        return null;
    }
}

using Aster.Compiler.Frontend.Lexer;
using Aster.Compiler.Frontend.Parser;
using Aster.Compiler.Frontend.Ast;
using Aster.Lsp.Protocol;

namespace Aster.Lsp.Handlers;

/// <summary>
/// Provides document symbols for the LSP server.
/// </summary>
public sealed class DocumentSymbolHandler
{
    public List<DocumentSymbol> GetSymbols(string source, string uri)
    {
        try
        {
            var lexer = new AsterLexer(source, uri);
            var tokens = lexer.Tokenize();
            if (lexer.Diagnostics.HasErrors) return new();

            var parser = new AsterParser(tokens);
            var program = parser.ParseProgram();
            if (parser.Diagnostics.HasErrors) return new();

            return ExtractSymbols(program);
        }
        catch
        {
            return new();
        }
    }

    private List<DocumentSymbol> ExtractSymbols(ProgramNode program)
    {
        var symbols = new List<DocumentSymbol>();
        foreach (var decl in program.Declarations)
        {
            var symbol = DeclToSymbol(decl);
            if (symbol != null) symbols.Add(symbol);
        }
        return symbols;
    }

    private DocumentSymbol? DeclToSymbol(AstNode node)
    {
        return node switch
        {
            FunctionDeclNode fn => new DocumentSymbol
            {
                Name = fn.Name,
                Kind = 12, // Function
                Range = SpanToRange(fn.Span),
                SelectionRange = SpanToRange(fn.Span)
            },
            StructDeclNode s => new DocumentSymbol
            {
                Name = s.Name,
                Kind = 23, // Struct
                Range = SpanToRange(s.Span),
                SelectionRange = SpanToRange(s.Span)
            },
            EnumDeclNode e => new DocumentSymbol
            {
                Name = e.Name,
                Kind = 10, // Enum
                Range = SpanToRange(e.Span),
                SelectionRange = SpanToRange(e.Span)
            },
            TraitDeclNode t => new DocumentSymbol
            {
                Name = t.Name,
                Kind = 11, // Interface
                Range = SpanToRange(t.Span),
                SelectionRange = SpanToRange(t.Span)
            },
            _ => null
        };
    }

    private static LspRange SpanToRange(Aster.Compiler.Diagnostics.Span span)
    {
        return new LspRange
        {
            Start = new LspPosition { Line = Math.Max(0, span.Line - 1), Character = Math.Max(0, span.Column - 1) },
            End = new LspPosition { Line = Math.Max(0, span.Line - 1), Character = Math.Max(0, span.Column - 1 + span.Length) }
        };
    }
}

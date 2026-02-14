using Aster.Compiler.Diagnostics;
using Aster.Lsp.Protocol;
using Aster.Workspaces;

namespace Aster.Lsp.Handlers;

/// <summary>
/// Handles diagnostic computation for the LSP server.
/// </summary>
public sealed class DiagnosticsHandler
{
    private readonly IncrementalReanalysis _reanalysis;

    public DiagnosticsHandler(IncrementalReanalysis reanalysis)
    {
        _reanalysis = reanalysis;
    }

    public List<LspDiagnostic> GetDiagnostics(string uri, string text)
    {
        var filePath = UriToPath(uri);
        var compilerDiagnostics = _reanalysis.AnalyzeContent(filePath, text);

        return compilerDiagnostics.Select(d => new LspDiagnostic
        {
            Range = SpanToRange(d.Span),
            Severity = d.Severity switch
            {
                DiagnosticSeverity.Error => 1,
                DiagnosticSeverity.Warning => 2,
                DiagnosticSeverity.Hint => 4,
                _ => 3
            },
            Code = d.Code,
            Message = d.Message
        })
        .OrderBy(d => d.Range.Start.Line)
        .ThenBy(d => d.Range.Start.Character)
        .ToList();
    }

    private static LspRange SpanToRange(Span span)
    {
        return new LspRange
        {
            Start = new LspPosition { Line = Math.Max(0, span.Line - 1), Character = Math.Max(0, span.Column - 1) },
            End = new LspPosition { Line = Math.Max(0, span.Line - 1), Character = Math.Max(0, span.Column - 1 + span.Length) }
        };
    }

    private static string UriToPath(string uri)
    {
        if (uri.StartsWith("file:///"))
            return Uri.UnescapeDataString(uri[7..]).Replace('/', Path.DirectorySeparatorChar);
        return uri;
    }
}

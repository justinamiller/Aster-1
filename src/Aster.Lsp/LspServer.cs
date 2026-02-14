using System.Text.Json;
using Aster.Lsp.Handlers;
using Aster.Lsp.Protocol;
using Aster.Workspaces;

namespace Aster.Lsp;

/// <summary>
/// Aster Language Server. Implements LSP over JSON-RPC on stdin/stdout.
/// </summary>
public sealed class LspServer
{
    private readonly MessageReader _reader;
    private readonly MessageWriter _writer;
    private readonly Dictionary<string, DocumentSnapshot> _documents = new();
    private readonly IncrementalReanalysis _reanalysis = new();
    private readonly DiagnosticsHandler _diagnosticsHandler;
    private readonly DocumentSymbolHandler _symbolHandler;
    private readonly HoverHandler _hoverHandler;
    private readonly FormattingHandler _formattingHandler;
    private bool _shutdown;

    public LspServer(Stream input, Stream output)
    {
        _reader = new MessageReader(input);
        _writer = new MessageWriter(output);
        _diagnosticsHandler = new DiagnosticsHandler(_reanalysis);
        _symbolHandler = new DocumentSymbolHandler();
        _hoverHandler = new HoverHandler();
        _formattingHandler = new FormattingHandler();
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        while (!_shutdown)
        {
            var message = await _reader.ReadMessageAsync(ct);
            if (message == null) break;

            await HandleMessageAsync(message, ct);
        }
    }

    private async Task HandleMessageAsync(JsonRpcRequest request, CancellationToken ct)
    {
        try
        {
            switch (request.Method)
            {
                case "initialize":
                    await HandleInitializeAsync(request, ct);
                    break;
                case "initialized":
                    break;
                case "shutdown":
                    _shutdown = true;
                    await SendResponseAsync(request.Id, new object(), ct);
                    break;
                case "exit":
                    _shutdown = true;
                    break;
                case "textDocument/didOpen":
                    HandleDidOpen(request);
                    await PublishDiagnosticsAsync(request, ct);
                    break;
                case "textDocument/didChange":
                    HandleDidChange(request);
                    await PublishDiagnosticsAsync(request, ct);
                    break;
                case "textDocument/didSave":
                    await PublishDiagnosticsAsync(request, ct);
                    break;
                case "textDocument/didClose":
                    HandleDidClose(request);
                    break;
                case "textDocument/hover":
                    await HandleHoverAsync(request, ct);
                    break;
                case "textDocument/documentSymbol":
                    await HandleDocumentSymbolAsync(request, ct);
                    break;
                case "textDocument/formatting":
                    await HandleFormattingAsync(request, ct);
                    break;
                case "textDocument/definition":
                    await SendResponseAsync(request.Id, (object?)null, ct);
                    break;
                case "textDocument/references":
                    await SendResponseAsync(request.Id, Array.Empty<object>(), ct);
                    break;
                case "textDocument/rename":
                    await SendResponseAsync(request.Id, (object?)null, ct);
                    break;
                case "textDocument/signatureHelp":
                    await SendResponseAsync(request.Id, (object?)null, ct);
                    break;
                default:
                    if (request.Id.HasValue)
                    {
                        await SendErrorAsync(request.Id, -32601, $"Method not found: {request.Method}", ct);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            if (request.Id.HasValue)
            {
                await SendErrorAsync(request.Id, -32603, ex.Message, ct);
            }
        }
    }

    private async Task HandleInitializeAsync(JsonRpcRequest request, CancellationToken ct)
    {
        var result = new InitializeResult();
        await SendResponseAsync(request.Id, result, ct);
    }

    private void HandleDidOpen(JsonRpcRequest request)
    {
        if (request.Params == null) return;
        var textDocument = request.Params.Value.GetProperty("textDocument");
        var uri = textDocument.GetProperty("uri").GetString() ?? "";
        var text = textDocument.GetProperty("text").GetString() ?? "";
        var version = textDocument.GetProperty("version").GetInt32();
        _documents[uri] = new DocumentSnapshot(uri, version, text);
    }

    private void HandleDidChange(JsonRpcRequest request)
    {
        if (request.Params == null) return;
        var textDocument = request.Params.Value.GetProperty("textDocument");
        var uri = textDocument.GetProperty("uri").GetString() ?? "";
        var contentChanges = request.Params.Value.GetProperty("contentChanges");
        foreach (var change in contentChanges.EnumerateArray())
        {
            if (change.TryGetProperty("text", out var textElem))
            {
                var text = textElem.GetString() ?? "";
                var version = textDocument.TryGetProperty("version", out var v) ? v.GetInt32() : 0;
                _documents[uri] = new DocumentSnapshot(uri, version, text);
            }
        }
    }

    private void HandleDidClose(JsonRpcRequest request)
    {
        if (request.Params == null) return;
        var textDocument = request.Params.Value.GetProperty("textDocument");
        var uri = textDocument.GetProperty("uri").GetString() ?? "";
        _documents.Remove(uri);
    }

    private async Task PublishDiagnosticsAsync(JsonRpcRequest request, CancellationToken ct)
    {
        if (request.Params == null) return;
        var textDocument = request.Params.Value.GetProperty("textDocument");
        var uri = textDocument.GetProperty("uri").GetString() ?? "";

        if (!_documents.TryGetValue(uri, out var snapshot)) return;

        var diagnostics = _diagnosticsHandler.GetDiagnostics(uri, snapshot.Text);
        var publishParams = new PublishDiagnosticsParams
        {
            Uri = uri,
            Diagnostics = diagnostics
        };
        await _writer.WriteNotificationAsync("textDocument/publishDiagnostics", publishParams, ct);
    }

    private async Task HandleHoverAsync(JsonRpcRequest request, CancellationToken ct)
    {
        if (request.Params == null || !_documents.TryGetValue(
            request.Params.Value.GetProperty("textDocument").GetProperty("uri").GetString() ?? "", out var snapshot))
        {
            await SendResponseAsync(request.Id, (object?)null, ct);
            return;
        }

        var position = request.Params.Value.GetProperty("position");
        var line = position.GetProperty("line").GetInt32();
        var character = position.GetProperty("character").GetInt32();

        var hover = _hoverHandler.GetHover(snapshot.Text, snapshot.Uri, line, character);
        await SendResponseAsync(request.Id, hover, ct);
    }

    private async Task HandleDocumentSymbolAsync(JsonRpcRequest request, CancellationToken ct)
    {
        if (request.Params == null) return;
        var uri = request.Params.Value.GetProperty("textDocument").GetProperty("uri").GetString() ?? "";
        if (!_documents.TryGetValue(uri, out var snapshot))
        {
            await SendResponseAsync(request.Id, Array.Empty<object>(), ct);
            return;
        }

        var symbols = _symbolHandler.GetSymbols(snapshot.Text, uri);
        await SendResponseAsync(request.Id, symbols, ct);
    }

    private async Task HandleFormattingAsync(JsonRpcRequest request, CancellationToken ct)
    {
        if (request.Params == null) return;
        var uri = request.Params.Value.GetProperty("textDocument").GetProperty("uri").GetString() ?? "";
        if (!_documents.TryGetValue(uri, out var snapshot))
        {
            await SendResponseAsync(request.Id, Array.Empty<object>(), ct);
            return;
        }

        var edits = _formattingHandler.Format(snapshot);
        await SendResponseAsync(request.Id, edits, ct);
    }

    private async Task SendResponseAsync(JsonElement? id, object? result, CancellationToken ct)
    {
        var response = new JsonRpcResponse { Id = id, Result = result };
        await _writer.WriteResponseAsync(response, ct);
    }

    private async Task SendErrorAsync(JsonElement? id, int code, string message, CancellationToken ct)
    {
        var response = new JsonRpcResponse
        {
            Id = id,
            Error = new JsonRpcError { Code = code, Message = message }
        };
        await _writer.WriteResponseAsync(response, ct);
    }
}

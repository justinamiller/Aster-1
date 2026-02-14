using System.Text;
using System.Text.Json;

namespace Aster.Lsp.Protocol;

/// <summary>
/// Writes JSON-RPC messages to an output stream following LSP base protocol.
/// </summary>
public sealed class MessageWriter
{
    private readonly Stream _output;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public MessageWriter(Stream output)
    {
        _output = output;
    }

    public async Task WriteResponseAsync(JsonRpcResponse response, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(response);
        await WriteRawAsync(json, ct);
    }

    public async Task WriteNotificationAsync(string method, object parameters, CancellationToken ct = default)
    {
        var notification = new { jsonrpc = "2.0", method, @params = parameters };
        var json = JsonSerializer.Serialize(notification);
        await WriteRawAsync(json, ct);
    }

    private async Task WriteRawAsync(string json, CancellationToken ct)
    {
        var contentBytes = Encoding.UTF8.GetBytes(json);
        var header = $"Content-Length: {contentBytes.Length}\r\n\r\n";
        var headerBytes = Encoding.UTF8.GetBytes(header);

        await _writeLock.WaitAsync(ct);
        try
        {
            await _output.WriteAsync(headerBytes, ct);
            await _output.WriteAsync(contentBytes, ct);
            await _output.FlushAsync(ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}

using System.Text;
using System.Text.Json;

namespace Aster.Lsp.Protocol;

/// <summary>
/// Reads JSON-RPC messages from an input stream following LSP base protocol.
/// </summary>
public sealed class MessageReader
{
    private readonly Stream _input;

    public MessageReader(Stream input)
    {
        _input = input;
    }

    public async Task<JsonRpcRequest?> ReadMessageAsync(CancellationToken ct = default)
    {
        var headers = await ReadHeadersAsync(ct);
        if (headers == null) return null;

        if (!headers.TryGetValue("Content-Length", out var lengthStr) || !int.TryParse(lengthStr, out var contentLength))
            return null;

        var buffer = new byte[contentLength];
        int totalRead = 0;
        while (totalRead < contentLength)
        {
            var read = await _input.ReadAsync(buffer.AsMemory(totalRead, contentLength - totalRead), ct);
            if (read == 0) return null;
            totalRead += read;
        }

        var json = Encoding.UTF8.GetString(buffer);
        return JsonSerializer.Deserialize<JsonRpcRequest>(json);
    }

    private async Task<Dictionary<string, string>?> ReadHeadersAsync(CancellationToken ct)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lineBuffer = new StringBuilder();

        while (true)
        {
            var b = new byte[1];
            var read = await _input.ReadAsync(b, ct);
            if (read == 0) return null;

            lineBuffer.Append((char)b[0]);

            if (lineBuffer.Length >= 2 && lineBuffer[^2] == '\r' && lineBuffer[^1] == '\n')
            {
                var line = lineBuffer.ToString(0, lineBuffer.Length - 2);
                lineBuffer.Clear();

                if (line.Length == 0)
                    return headers.Count > 0 ? headers : null;

                var colonIdx = line.IndexOf(':');
                if (colonIdx > 0)
                {
                    var key = line[..colonIdx].Trim();
                    var value = line[(colonIdx + 1)..].Trim();
                    headers[key] = value;
                }
            }
        }
    }
}

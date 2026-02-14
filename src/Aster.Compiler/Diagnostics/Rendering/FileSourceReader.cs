namespace Aster.Compiler.Diagnostics.Rendering;

/// <summary>
/// Reads source code lines from files for diagnostic rendering.
/// Caches file contents to avoid repeated I/O.
/// </summary>
public sealed class FileSourceReader : ISourceReader
{
    private readonly Dictionary<string, string[]> _cache = new();

    public string? GetLine(string file, int line)
    {
        if (string.IsNullOrEmpty(file) || line < 1) return null;

        if (!_cache.TryGetValue(file, out var lines))
        {
            if (!File.Exists(file)) return null;

            try
            {
                lines = File.ReadAllLines(file);
                _cache[file] = lines;
            }
            catch
            {
                return null;
            }
        }

        return line <= lines.Length ? lines[line - 1] : null;
    }

    public void ClearCache()
    {
        _cache.Clear();
    }
}

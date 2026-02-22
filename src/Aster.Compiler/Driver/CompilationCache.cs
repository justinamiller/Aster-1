using System.Security.Cryptography;
using System.Text;

namespace Aster.Compiler.Driver;

/// <summary>
/// Result stored in the compilation cache for a single source file.
/// </summary>
public sealed class CachedModule
{
    /// <summary>SHA-256 hash of the source text at the time of compilation.</summary>
    public string SourceHash { get; }

    /// <summary>The LLVM IR text produced for this source.</summary>
    public string LlvmIr { get; }

    /// <summary>UTC timestamp when this entry was cached.</summary>
    public DateTime CachedAt { get; }

    public CachedModule(string sourceHash, string llvmIr)
    {
        SourceHash = sourceHash;
        LlvmIr = llvmIr;
        CachedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// In-memory content-hash–based compilation cache.
/// Keyed by (file-name, SHA-256 hash of source).  When the source changes,
/// the hash changes and the old entry is automatically bypassed.
/// </summary>
public sealed class CompilationCache
{
    private readonly Dictionary<string, CachedModule> _store =
        new(StringComparer.Ordinal);

    /// <summary>Number of entries currently in the cache.</summary>
    public int Count => _store.Count;

    /// <summary>
    /// Try to retrieve a cached compilation result.
    /// Returns <c>true</c> and sets <paramref name="cached"/> when the source
    /// hash matches the stored hash (i.e. the source is unchanged).
    /// </summary>
    public bool TryGet(string fileName, string source, out CachedModule? cached)
    {
        var hash = ComputeHash(source);
        var key = MakeKey(fileName, hash);
        if (_store.TryGetValue(key, out cached))
            return true;
        cached = null;
        return false;
    }

    /// <summary>
    /// Store a compilation result in the cache.
    /// Any previous entry for <paramref name="fileName"/> with a different source
    /// hash is evicted automatically because it would have a different key.
    /// </summary>
    public void Put(string fileName, string source, string llvmIr)
    {
        var hash = ComputeHash(source);
        var key = MakeKey(fileName, hash);
        _store[key] = new CachedModule(hash, llvmIr);
    }

    /// <summary>Remove all cached entries.</summary>
    public void Clear() => _store.Clear();

    /// <summary>Remove the cached entry for a specific file (all source versions).</summary>
    public void Invalidate(string fileName)
    {
        var prefix = $"{fileName}:";
        var keysToRemove = _store.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        foreach (var k in keysToRemove)
            _store.Remove(k);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string ComputeHash(string source)
    {
        var bytes = Encoding.UTF8.GetBytes(source);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string MakeKey(string fileName, string hash) => $"{fileName}:{hash}";
}

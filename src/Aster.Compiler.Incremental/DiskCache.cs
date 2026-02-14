namespace Aster.Compiler.Incremental;

/// <summary>
/// Persistent on-disk cache for incremental compilation.
/// Stores serialized query results in a directory structure.
/// </summary>
public sealed class DiskCache : IDisposable
{
    private readonly string _cacheDirectory;
    private readonly CacheSerializer _serializer = new();
    private readonly object _lock = new();

    public DiskCache(string cacheDirectory)
    {
        _cacheDirectory = cacheDirectory;
        Directory.CreateDirectory(_cacheDirectory);
    }

    /// <summary>Try to load a cached result from disk.</summary>
    public bool TryLoad(QueryKey key, out QueryResult? result)
    {
        lock (_lock)
        {
            var path = GetCachePath(key);
            if (!File.Exists(path))
            {
                result = null;
                return false;
            }

            try
            {
                var data = File.ReadAllBytes(path);
                result = _serializer.Deserialize(data);
                return true;
            }
            catch
            {
                // Cache entry is corrupted, delete it
                File.Delete(path);
                result = null;
                return false;
            }
        }
    }

    /// <summary>Store a query result to disk.</summary>
    public void Store(QueryKey key, QueryResult result)
    {
        lock (_lock)
        {
            var path = GetCachePath(key);
            var directory = Path.GetDirectoryName(path);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            var data = _serializer.Serialize(result);
            File.WriteAllBytes(path, data);
        }
    }

    /// <summary>Invalidate a cached entry.</summary>
    public void Invalidate(QueryKey key)
    {
        lock (_lock)
        {
            var path = GetCachePath(key);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>Clear all cached entries.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            if (Directory.Exists(_cacheDirectory))
            {
                Directory.Delete(_cacheDirectory, recursive: true);
                Directory.CreateDirectory(_cacheDirectory);
            }
        }
    }

    /// <summary>Get cache size in bytes.</summary>
    public long GetCacheSize()
    {
        lock (_lock)
        {
            if (!Directory.Exists(_cacheDirectory))
                return 0;

            var files = Directory.GetFiles(_cacheDirectory, "*", SearchOption.AllDirectories);
            return files.Sum(f => new FileInfo(f).Length);
        }
    }

    private string GetCachePath(QueryKey key)
    {
        var hash = key.ComputeHash();
        var hex = hash.ToString("x16");
        // Use subdirectories to avoid too many files in one directory
        var subdir = hex.Substring(0, 2);
        return Path.Combine(_cacheDirectory, subdir, $"{hex}.cache");
    }

    public void Dispose()
    {
        // Nothing to dispose currently, but keep for future use
    }
}

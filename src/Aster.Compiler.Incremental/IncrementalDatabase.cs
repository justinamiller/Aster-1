namespace Aster.Compiler.Incremental;

/// <summary>
/// Interface for the incremental compilation database.
/// Provides query memoization, dependency tracking, and cache invalidation.
/// </summary>
public interface IIncrementalDatabase
{
    /// <summary>
    /// Execute a query, using cached result if available and valid.
    /// Records dependencies automatically.
    /// </summary>
    QueryResult ExecuteQuery(QueryKey key, Func<QueryResult> compute);

    /// <summary>Invalidate a query and all its dependents.</summary>
    void Invalidate(QueryKey key);

    /// <summary>Check if a query result is cached.</summary>
    bool IsCached(QueryKey key);

    /// <summary>Clear all cached data.</summary>
    void ClearCache();

    /// <summary>Get cache statistics.</summary>
    CacheStatistics GetStatistics();
}

/// <summary>
/// In-memory implementation of the incremental database.
/// Thread-safe for parallel compilation.
/// </summary>
public sealed class InMemoryIncrementalDatabase : IIncrementalDatabase
{
    private readonly Dictionary<QueryKey, QueryResult> _cache = new();
    private readonly DependencyGraph _dependencyGraph = new();
    private readonly object _lock = new();
    private int _hits = 0;
    private int _misses = 0;

    public QueryResult ExecuteQuery(QueryKey key, Func<QueryResult> compute)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                _hits++;
                return cached;
            }

            _misses++;
        }

        // Execute query outside lock to allow parallelism
        var result = compute();

        lock (_lock)
        {
            _cache[key] = result;
        }

        return result;
    }

    public void Invalidate(QueryKey key)
    {
        lock (_lock)
        {
            var toInvalidate = _dependencyGraph.ComputeInvalidationSet(key);
            foreach (var invalidKey in toInvalidate)
            {
                _cache.Remove(invalidKey);
            }
        }
    }

    public bool IsCached(QueryKey key)
    {
        lock (_lock)
        {
            return _cache.ContainsKey(key);
        }
    }

    public void ClearCache()
    {
        lock (_lock)
        {
            _cache.Clear();
            _dependencyGraph.Clear();
            _hits = 0;
            _misses = 0;
        }
    }

    public CacheStatistics GetStatistics()
    {
        lock (_lock)
        {
            return new CacheStatistics(
                CachedQueries: _cache.Count,
                DependencyEdges: _dependencyGraph.EdgeCount,
                CacheHits: _hits,
                CacheMisses: _misses
            );
        }
    }

    /// <summary>Add a dependency edge.</summary>
    public void RecordDependency(QueryKey from, QueryKey to)
    {
        lock (_lock)
        {
            _dependencyGraph.AddDependency(from, to);
        }
    }
}

/// <summary>Cache statistics for monitoring.</summary>
public sealed record CacheStatistics(
    int CachedQueries,
    int DependencyEdges,
    int CacheHits,
    int CacheMisses)
{
    public double HitRate => CacheHits + CacheMisses > 0
        ? (double)CacheHits / (CacheHits + CacheMisses)
        : 0.0;
}

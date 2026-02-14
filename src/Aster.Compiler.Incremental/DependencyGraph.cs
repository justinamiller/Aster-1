namespace Aster.Compiler.Incremental;

/// <summary>
/// Represents a dependency edge in the compilation graph.
/// Tracks which queries depend on which other queries.
/// </summary>
public sealed record DependencyEdge(QueryKey From, QueryKey To)
{
    public override string ToString() => $"{From.GetType().Name} -> {To.GetType().Name}";
}

/// <summary>
/// Dependency graph for tracking query dependencies.
/// Supports incremental invalidation when inputs change.
/// </summary>
public sealed class DependencyGraph
{
    private readonly Dictionary<QueryKey, HashSet<QueryKey>> _dependencies = new();
    private readonly Dictionary<QueryKey, HashSet<QueryKey>> _reverseDependencies = new();
    private readonly object _lock = new();

    /// <summary>Record that 'from' depends on 'to'.</summary>
    public void AddDependency(QueryKey from, QueryKey to)
    {
        lock (_lock)
        {
            if (!_dependencies.TryGetValue(from, out var deps))
            {
                deps = new HashSet<QueryKey>();
                _dependencies[from] = deps;
            }
            deps.Add(to);

            if (!_reverseDependencies.TryGetValue(to, out var reverseDeps))
            {
                reverseDeps = new HashSet<QueryKey>();
                _reverseDependencies[to] = reverseDeps;
            }
            reverseDeps.Add(from);
        }
    }

    /// <summary>Get all direct dependencies of a query.</summary>
    public IReadOnlySet<QueryKey> GetDependencies(QueryKey key)
    {
        lock (_lock)
        {
            return _dependencies.TryGetValue(key, out var deps)
                ? deps
                : new HashSet<QueryKey>();
        }
    }

    /// <summary>Get all queries that depend on this query.</summary>
    public IReadOnlySet<QueryKey> GetDependents(QueryKey key)
    {
        lock (_lock)
        {
            return _reverseDependencies.TryGetValue(key, out var deps)
                ? deps
                : new HashSet<QueryKey>();
        }
    }

    /// <summary>
    /// Compute transitive closure of all queries that need to be invalidated
    /// when the given query changes.
    /// </summary>
    public HashSet<QueryKey> ComputeInvalidationSet(QueryKey changed)
    {
        var toInvalidate = new HashSet<QueryKey> { changed };
        var queue = new Queue<QueryKey>();
        queue.Enqueue(changed);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var dependents = GetDependents(current);
            
            foreach (var dependent in dependents)
            {
                if (toInvalidate.Add(dependent))
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        return toInvalidate;
    }

    /// <summary>Clear all dependencies.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _dependencies.Clear();
            _reverseDependencies.Clear();
        }
    }

    /// <summary>Get total number of edges.</summary>
    public int EdgeCount
    {
        get
        {
            lock (_lock)
            {
                return _dependencies.Values.Sum(deps => deps.Count);
            }
        }
    }
}

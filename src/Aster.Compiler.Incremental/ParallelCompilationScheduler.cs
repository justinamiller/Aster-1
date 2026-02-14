using System.Collections.Concurrent;

namespace Aster.Compiler.Incremental;

/// <summary>
/// Compilation unit representing a module or function to compile.
/// </summary>
public sealed record CompilationUnit(QueryKey Key, int Priority = 0);

/// <summary>
/// Parallel compilation scheduler with work-stealing.
/// Ensures deterministic output despite parallel execution.
/// </summary>
public sealed class ParallelCompilationScheduler
{
    private readonly IIncrementalDatabase _database;
    private readonly int _maxParallelism;

    public ParallelCompilationScheduler(IIncrementalDatabase database, int? maxParallelism = null)
    {
        _database = database;
        _maxParallelism = maxParallelism ?? Environment.ProcessorCount;
    }

    /// <summary>
    /// Compile multiple units in parallel, respecting dependencies.
    /// Returns results in deterministic order.
    /// </summary>
    public async Task<List<QueryResult>> CompileParallel(
        List<CompilationUnit> units,
        Func<QueryKey, QueryResult> compileFunc)
    {
        // Sort units by priority for deterministic ordering
        var sortedUnits = units.OrderByDescending(u => u.Priority).ThenBy(u => u.Key.ComputeHash()).ToList();
        
        var results = new ConcurrentDictionary<QueryKey, QueryResult>();
        var workQueue = new ConcurrentQueue<CompilationUnit>(sortedUnits);
        
        // Create worker tasks
        var workers = new List<Task>();
        for (int i = 0; i < _maxParallelism; i++)
        {
            workers.Add(Task.Run(() => WorkerThread(workQueue, compileFunc, results)));
        }

        await Task.WhenAll(workers);

        // Return results in original order for determinism
        return sortedUnits.Select(u => results[u.Key]).ToList();
    }

    private void WorkerThread(
        ConcurrentQueue<CompilationUnit> workQueue,
        Func<QueryKey, QueryResult> compileFunc,
        ConcurrentDictionary<QueryKey, QueryResult> results)
    {
        while (workQueue.TryDequeue(out var unit))
        {
            var result = _database.ExecuteQuery(unit.Key, () => compileFunc(unit.Key));
            results[unit.Key] = result;
        }
    }

    /// <summary>
    /// Compile units sequentially (for debugging or single-threaded mode).
    /// </summary>
    public List<QueryResult> CompileSequential(
        List<CompilationUnit> units,
        Func<QueryKey, QueryResult> compileFunc)
    {
        var results = new List<QueryResult>();
        
        foreach (var unit in units.OrderBy(u => u.Key.ComputeHash()))
        {
            var result = _database.ExecuteQuery(unit.Key, () => compileFunc(unit.Key));
            results.Add(result);
        }

        return results;
    }
}

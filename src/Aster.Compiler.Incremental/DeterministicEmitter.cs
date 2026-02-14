namespace Aster.Compiler.Incremental;

/// <summary>
/// Emitter that produces deterministic output by maintaining stable ordering.
/// Critical for reproducible builds in parallel compilation.
/// </summary>
public sealed class DeterministicEmitter
{
    private readonly List<(ulong SortKey, string Symbol, string Code)> _emissions = new();
    private readonly object _lock = new();

    /// <summary>Emit a symbol with its code.</summary>
    public void Emit(string symbol, string code)
    {
        lock (_lock)
        {
            // Use stable hash of symbol name as sort key
            var sortKey = StableHasher.Hash(symbol);
            _emissions.Add((sortKey, symbol, code));
        }
    }

    /// <summary>Get all emissions in deterministic sorted order.</summary>
    public IReadOnlyList<(string Symbol, string Code)> GetEmissions()
    {
        lock (_lock)
        {
            return _emissions
                .OrderBy(e => e.SortKey)
                .Select(e => (e.Symbol, e.Code))
                .ToList();
        }
    }

    /// <summary>Get concatenated code in deterministic order.</summary>
    public string GetCombinedCode()
    {
        var emissions = GetEmissions();
        return string.Join("\n\n", emissions.Select(e => e.Code));
    }

    /// <summary>Clear all emissions.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _emissions.Clear();
        }
    }

    /// <summary>Get emission count.</summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _emissions.Count;
            }
        }
    }
}

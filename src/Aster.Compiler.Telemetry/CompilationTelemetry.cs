using System.Diagnostics;

namespace Aster.Compiler.Telemetry;

/// <summary>
/// Tracks metrics and timing for a single compiler phase.
/// </summary>
public sealed class PhaseMetrics
{
    public string PhaseName { get; }
    public TimeSpan WallTime { get; set; }
    public long BytesAllocated { get; set; }
    public int NodesProcessed { get; set; }
    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }

    public PhaseMetrics(string phaseName)
    {
        PhaseName = phaseName;
    }

    public override string ToString()
    {
        return $"{PhaseName}: {WallTime.TotalMilliseconds:F2}ms, {NodesProcessed} nodes";
    }
}

/// <summary>
/// Collects telemetry data across all compilation phases.
/// </summary>
public sealed class CompilationTelemetry
{
    private readonly List<PhaseMetrics> _phases = new();
    private readonly Stopwatch _totalTimer = Stopwatch.StartNew();

    public void AddPhase(PhaseMetrics metrics)
    {
        _phases.Add(metrics);
    }

    public IReadOnlyList<PhaseMetrics> Phases => _phases;

    public TimeSpan TotalTime => _totalTimer.Elapsed;

    public PhaseMetrics GetOrCreatePhase(string phaseName)
    {
        var existing = _phases.FirstOrDefault(p => p.PhaseName == phaseName);
        if (existing != null) return existing;

        var metrics = new PhaseMetrics(phaseName);
        _phases.Add(metrics);
        return metrics;
    }

    public string FormatHuman()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Compilation Metrics:");
        sb.AppendLine("===================");

        foreach (var phase in _phases)
        {
            sb.AppendLine($"  {phase.PhaseName,-20} {phase.WallTime.TotalMilliseconds,8:F2}ms");
            if (phase.NodesProcessed > 0)
                sb.AppendLine($"    Nodes: {phase.NodesProcessed}");
            if (phase.CacheHits > 0 || phase.CacheMisses > 0)
                sb.AppendLine($"    Cache: {phase.CacheHits} hits, {phase.CacheMisses} misses");
        }

        sb.AppendLine("-------------------");
        sb.AppendLine($"  {"Total",-20} {TotalTime.TotalMilliseconds,8:F2}ms");

        return sb.ToString();
    }

    public string FormatJson()
    {
        var dto = new
        {
            totalTimeMs = TotalTime.TotalMilliseconds,
            phases = _phases.Select(p => new
            {
                name = p.PhaseName,
                wallTimeMs = p.WallTime.TotalMilliseconds,
                bytesAllocated = p.BytesAllocated,
                nodesProcessed = p.NodesProcessed,
                cacheHits = p.CacheHits,
                cacheMisses = p.CacheMisses
            }).ToList()
        };

        return System.Text.Json.JsonSerializer.Serialize(dto, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}

/// <summary>
/// RAII-style phase timer for automatic metric collection.
/// </summary>
public sealed class PhaseTimer : IDisposable
{
    private readonly PhaseMetrics _metrics;
    private readonly Stopwatch _stopwatch;
    private readonly long _startMemory;

    public PhaseTimer(PhaseMetrics metrics)
    {
        _metrics = metrics;
        _startMemory = GC.GetTotalMemory(false);
        _stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _metrics.WallTime = _stopwatch.Elapsed;
        _metrics.BytesAllocated = Math.Max(0, GC.GetTotalMemory(false) - _startMemory);
    }

    public void IncrementNodes(int count = 1)
    {
        _metrics.NodesProcessed += count;
    }

    public void RecordCacheHit()
    {
        _metrics.CacheHits++;
    }

    public void RecordCacheMiss()
    {
        _metrics.CacheMisses++;
    }
}

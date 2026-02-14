using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Optimizations;

/// <summary>
/// Base interface for optimization passes.
/// All passes must be measurable and deterministic.
/// </summary>
public interface IOptimizationPass
{
    /// <summary>Name of the optimization pass.</summary>
    string Name { get; }

    /// <summary>
    /// Run the optimization pass on a function.
    /// Returns true if the function was modified.
    /// </summary>
    bool Run(MirFunction function, PassContext context);
}

/// <summary>
/// Context provided to optimization passes.
/// Contains metrics, flags, and shared state.
/// </summary>
public sealed class PassContext
{
    /// <summary>Metrics collected during the pass.</summary>
    public PassMetrics Metrics { get; } = new();

    /// <summary>Whether to preserve debug information.</summary>
    public bool PreserveDebugInfo { get; set; } = false;

    /// <summary>Optimization level (0-3).</summary>
    public int OptimizationLevel { get; set; } = 2;

    /// <summary>Profile data for PGO (optional).</summary>
    public ProfileData? ProfileData { get; set; }
}

/// <summary>
/// Metrics collected during an optimization pass.
/// </summary>
public sealed class PassMetrics
{
    private readonly object _lock = new();
    private long _startTime;
    private long _allocatedBytesBefore;

    public int InstructionsRemoved { get; set; }
    public int InstructionsAdded { get; set; }
    public int BlocksRemoved { get; set; }
    public int BlocksMerged { get; set; }
    public long ExecutionTimeMs { get; private set; }
    public long AllocatedBytes { get; private set; }

    /// <summary>Start timing the pass.</summary>
    public void StartTiming()
    {
        _startTime = Environment.TickCount64;
        _allocatedBytesBefore = GC.GetTotalAllocatedBytes();
    }

    /// <summary>Stop timing and record results.</summary>
    public void StopTiming()
    {
        ExecutionTimeMs = Environment.TickCount64 - _startTime;
        AllocatedBytes = GC.GetTotalAllocatedBytes() - _allocatedBytesBefore;
    }

    public override string ToString()
    {
        return $"Time: {ExecutionTimeMs}ms, " +
               $"Allocated: {AllocatedBytes / 1024}KB, " +
               $"Removed: {InstructionsRemoved} instrs, {BlocksRemoved} blocks, " +
               $"Added: {InstructionsAdded} instrs, " +
               $"Merged: {BlocksMerged} blocks";
    }
}

/// <summary>
/// Placeholder for profile data (PGO).
/// </summary>
public sealed class ProfileData
{
    public Dictionary<string, BlockProfile> BlockProfiles { get; } = new();
}

/// <summary>
/// Profile information for a basic block.
/// </summary>
public sealed record BlockProfile(long ExecutionCount, double Frequency);

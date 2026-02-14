using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Optimizations;

/// <summary>
/// Pass manager orchestrates optimization passes.
/// Supports pass ordering, metrics collection, and iterative optimization.
/// </summary>
public sealed class PassManager
{
    private readonly List<IOptimizationPass> _passes = new();
    private readonly PassContext _context;

    public PassManager(PassContext? context = null)
    {
        _context = context ?? new PassContext();
    }

    /// <summary>Add an optimization pass to the pipeline.</summary>
    public PassManager Add(IOptimizationPass pass)
    {
        _passes.Add(pass);
        return this;
    }

    /// <summary>Run all passes on a function until fixpoint.</summary>
    public PassManagerResult RunToFixpoint(MirFunction function, int maxIterations = 10)
    {
        var allMetrics = new Dictionary<string, List<PassMetrics>>();
        var totalStartTime = Environment.TickCount64;
        int iteration = 0;
        bool changed = true;

        while (changed && iteration < maxIterations)
        {
            changed = false;
            iteration++;

            foreach (var pass in _passes)
            {
                var passMetrics = new PassMetrics();
                _context.Metrics.StartTiming();

                bool passChanged = pass.Run(function, _context);

                _context.Metrics.StopTiming();

                if (!allMetrics.ContainsKey(pass.Name))
                {
                    allMetrics[pass.Name] = new List<PassMetrics>();
                }
                allMetrics[pass.Name].Add(_context.Metrics);

                changed |= passChanged;
            }
        }

        var totalTime = Environment.TickCount64 - totalStartTime;

        return new PassManagerResult(
            Iterations: iteration,
            TotalTimeMs: totalTime,
            PassMetrics: allMetrics
        );
    }

    /// <summary>Run all passes on a function once.</summary>
    public PassManagerResult RunOnce(MirFunction function)
    {
        return RunToFixpoint(function, maxIterations: 1);
    }

    /// <summary>Run all passes on a module.</summary>
    public ModulePassResult RunOnModule(MirModule module)
    {
        var functionResults = new Dictionary<string, PassManagerResult>();
        var totalStartTime = Environment.TickCount64;

        foreach (var function in module.Functions)
        {
            var result = RunToFixpoint(function);
            functionResults[function.Name] = result;
        }

        var totalTime = Environment.TickCount64 - totalStartTime;

        return new ModulePassResult(
            FunctionResults: functionResults,
            TotalTimeMs: totalTime
        );
    }

    /// <summary>Get the current pass context.</summary>
    public PassContext Context => _context;
}

/// <summary>Result of running the pass manager on a function.</summary>
public sealed record PassManagerResult(
    int Iterations,
    long TotalTimeMs,
    IReadOnlyDictionary<string, List<PassMetrics>> PassMetrics)
{
    public override string ToString()
    {
        var summary = $"Completed {Iterations} iteration(s) in {TotalTimeMs}ms\n";
        
        foreach (var (passName, metrics) in PassMetrics)
        {
            var totalTime = metrics.Sum(m => m.ExecutionTimeMs);
            var totalRemoved = metrics.Sum(m => m.InstructionsRemoved);
            summary += $"  {passName}: {totalTime}ms, removed {totalRemoved} instructions\n";
        }

        return summary;
    }
}

/// <summary>Result of running passes on a module.</summary>
public sealed record ModulePassResult(
    IReadOnlyDictionary<string, PassManagerResult> FunctionResults,
    long TotalTimeMs);

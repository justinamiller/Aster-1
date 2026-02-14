using System.Diagnostics;

namespace Aster.Compiler.Fuzzing;

/// <summary>
/// Base class for all fuzzing harnesses.
/// Provides common infrastructure for corpus management, crash classification,
/// timeout handling, and artifact generation.
/// </summary>
public abstract class FuzzRunner
{
    protected readonly FuzzConfig _config;
    protected readonly Random _rng;
    private readonly List<FuzzResult> _results = new();

    protected FuzzRunner(FuzzConfig config)
    {
        _config = config;
        _rng = new Random(config.Seed);
    }

    /// <summary>
    /// Generate a random test input.
    /// Subclasses must implement their domain-specific generation logic.
    /// </summary>
    protected abstract string GenerateInput();

    /// <summary>
    /// Execute the test with the given input.
    /// Returns true if the test passed, false if it revealed a bug.
    /// </summary>
    protected abstract FuzzResult ExecuteTest(string input);

    /// <summary>
    /// Run the fuzzing campaign.
    /// </summary>
    public FuzzSummary Run()
    {
        Console.WriteLine($"Starting fuzz with seed: {_config.Seed}");
        Console.WriteLine($"Max iterations: {_config.MaxIterations}");
        
        var startTime = Environment.TickCount64;

        for (int i = 0; i < _config.MaxIterations; i++)
        {
            if (i % 100 == 0 && i > 0)
            {
                Console.WriteLine($"Progress: {i}/{_config.MaxIterations}");
            }

            var input = GenerateInput();
            var result = ExecuteTest(input);
            _results.Add(result);

            if (result.Kind != FuzzResultKind.Success)
            {
                HandleFailure(result);
            }
        }

        var totalTime = Environment.TickCount64 - startTime;
        return BuildSummary(totalTime);
    }

    /// <summary>
    /// Handle a fuzzing failure by saving to appropriate directory
    /// and optionally minimizing and creating artifact bundle.
    /// </summary>
    private void HandleFailure(FuzzResult result)
    {
        Console.WriteLine($"[{result.Kind}] {result.ErrorMessage}");
        
        var basePath = result.Kind switch
        {
            FuzzResultKind.Crash => _config.CrashesPath,
            FuzzResultKind.WrongCode => _config.WrongCodePath,
            FuzzResultKind.Hang => _config.HangsPath,
            FuzzResultKind.NonDeterministic => _config.NonDetPath,
            _ => _config.CrashesPath
        };

        Directory.CreateDirectory(basePath);
        
        var fileName = $"{result.Kind}_{result.Seed}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.ast";
        var filePath = Path.Combine(basePath, fileName);
        
        if (result.Input != null)
        {
            File.WriteAllText(filePath, result.Input);
            Console.WriteLine($"Saved failure to: {filePath}");
        }

        if (_config.GenerateArtifacts && result.Input != null)
        {
            var artifactPath = GenerateArtifactBundle(result, filePath);
            Console.WriteLine($"Artifact bundle: {artifactPath}");
        }
    }

    /// <summary>
    /// Generate an artifact bundle for triage.
    /// </summary>
    private string GenerateArtifactBundle(FuzzResult result, string sourcePath)
    {
        var bundleName = Path.GetFileNameWithoutExtension(sourcePath) + "_bundle";
        var bundlePath = Path.Combine(Path.GetDirectoryName(sourcePath)!, bundleName);
        Directory.CreateDirectory(bundlePath);

        // Save source file
        var sourceFile = Path.Combine(bundlePath, "source.ast");
        if (result.Input != null)
        {
            File.WriteAllText(sourceFile, result.Input);
        }

        // Save metadata
        var metadata = new
        {
            Seed = result.Seed,
            Kind = result.Kind.ToString(),
            ErrorMessage = result.ErrorMessage,
            StackTrace = result.StackTrace,
            CompilerVersion = "0.2.0", // TODO: Get from actual version
            Timestamp = DateTime.UtcNow,
            ReproCommand = $"aster fuzz replay --seed {result.Seed} --input {sourceFile}"
        };

        var metadataFile = Path.Combine(bundlePath, "metadata.json");
        File.WriteAllText(metadataFile, System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        return bundlePath;
    }

    /// <summary>
    /// Build the final summary.
    /// </summary>
    private FuzzSummary BuildSummary(long totalTime)
    {
        var failures = _results.Where(r => r.Kind != FuzzResultKind.Success).ToList();
        
        return new FuzzSummary
        {
            TotalTests = _results.Count,
            Successes = _results.Count(r => r.Kind == FuzzResultKind.Success),
            Crashes = _results.Count(r => r.Kind == FuzzResultKind.Crash),
            WrongCode = _results.Count(r => r.Kind == FuzzResultKind.WrongCode),
            Hangs = _results.Count(r => r.Kind == FuzzResultKind.Hang),
            NonDeterministic = _results.Count(r => r.Kind == FuzzResultKind.NonDeterministic),
            TotalTimeMs = totalTime,
            Failures = failures
        };
    }

    /// <summary>
    /// Execute with timeout protection.
    /// </summary>
    protected FuzzResult ExecuteWithTimeout(Func<FuzzResult> action, string input, int seed)
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(_config.TimeoutMs);

        try
        {
            var task = Task.Run(() => action(), cts.Token);
            if (task.Wait(_config.TimeoutMs))
            {
                return task.Result;
            }
            else
            {
                return FuzzResult.Hang(input, seed);
            }
        }
        catch (AggregateException ex)
        {
            var innerEx = ex.InnerException ?? ex;
            return FuzzResult.Crash(innerEx.Message, innerEx.StackTrace, input, seed);
        }
        catch (Exception ex)
        {
            return FuzzResult.Crash(ex.Message, ex.StackTrace, input, seed);
        }
        finally
        {
            cts.Dispose();
        }
    }
}

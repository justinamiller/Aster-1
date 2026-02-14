namespace Aster.Compiler.Fuzzing;

/// <summary>
/// Configuration for fuzzing runs.
/// Supports deterministic reproduction via seed.
/// </summary>
public sealed record FuzzConfig
{
    /// <summary>RNG seed for deterministic reproduction.</summary>
    public int Seed { get; init; } = Environment.TickCount;

    /// <summary>Maximum iterations for time-bounded fuzzing.</summary>
    public int MaxIterations { get; init; } = 10000;

    /// <summary>Timeout in milliseconds for each test case.</summary>
    public int TimeoutMs { get; init; } = 5000;

    /// <summary>Path to corpus directory.</summary>
    public string CorpusPath { get; init; } = "./tests/fuzz/corpus";

    /// <summary>Path to crashes directory.</summary>
    public string CrashesPath { get; init; } = "./tests/fuzz/crashes";

    /// <summary>Path to wrong-code directory.</summary>
    public string WrongCodePath { get; init; } = "./tests/fuzz/wrongcode";

    /// <summary>Path to hangs directory.</summary>
    public string HangsPath { get; init; } = "./tests/fuzz/hangs";

    /// <summary>Path to non-determinism failures.</summary>
    public string NonDetPath { get; init; } = "./tests/fuzz/nondet";

    /// <summary>Enable minimization on failure.</summary>
    public bool MinimizeOnFailure { get; init; } = true;

    /// <summary>Enable artifact bundle generation.</summary>
    public bool GenerateArtifacts { get; init; } = true;

    /// <summary>Smoke test mode (reduced iterations).</summary>
    public bool SmokeMode { get; init; } = false;

    /// <summary>Get config for smoke testing.</summary>
    public static FuzzConfig Smoke(int seed = 0) => new()
    {
        Seed = seed == 0 ? Environment.TickCount : seed,
        MaxIterations = 100,
        TimeoutMs = 1000,
        SmokeMode = true
    };

    /// <summary>Get config for nightly extended fuzzing.</summary>
    public static FuzzConfig Nightly(int seed = 0) => new()
    {
        Seed = seed == 0 ? Environment.TickCount : seed,
        MaxIterations = 1000000,
        TimeoutMs = 10000,
        SmokeMode = false
    };
}

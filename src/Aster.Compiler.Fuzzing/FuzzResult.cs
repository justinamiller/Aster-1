namespace Aster.Compiler.Fuzzing;

/// <summary>
/// Result classification for fuzz testing.
/// Distinguishes crashes, wrong-code, hangs, non-determinism, and success.
/// </summary>
public enum FuzzResultKind
{
    Success,
    Crash,
    WrongCode,
    Hang,
    NonDeterministic,
    UndefinedBehavior
}

/// <summary>
/// Result of a single fuzz test execution.
/// </summary>
public sealed record FuzzResult
{
    public FuzzResultKind Kind { get; init; }
    public string? ErrorMessage { get; init; }
    public string? StackTrace { get; init; }
    public string? Input { get; init; }
    public int Seed { get; init; }
    public string? ArtifactPath { get; init; }
    public long ExecutionTimeMs { get; init; }

    public static FuzzResult Success(long timeMs) => new()
    {
        Kind = FuzzResultKind.Success,
        ExecutionTimeMs = timeMs
    };

    public static FuzzResult Crash(string message, string? stackTrace, string input, int seed) => new()
    {
        Kind = FuzzResultKind.Crash,
        ErrorMessage = message,
        StackTrace = stackTrace,
        Input = input,
        Seed = seed
    };

    public static FuzzResult WrongCode(string message, string input, int seed) => new()
    {
        Kind = FuzzResultKind.WrongCode,
        ErrorMessage = message,
        Input = input,
        Seed = seed
    };

    public static FuzzResult Hang(string input, int seed) => new()
    {
        Kind = FuzzResultKind.Hang,
        ErrorMessage = "Execution timed out",
        Input = input,
        Seed = seed
    };

    public static FuzzResult NonDeterministic(string message, string input, int seed) => new()
    {
        Kind = FuzzResultKind.NonDeterministic,
        ErrorMessage = message,
        Input = input,
        Seed = seed
    };
}

/// <summary>
/// Summary of a fuzzing campaign.
/// </summary>
public sealed record FuzzSummary
{
    public int TotalTests { get; init; }
    public int Successes { get; init; }
    public int Crashes { get; init; }
    public int WrongCode { get; init; }
    public int Hangs { get; init; }
    public int NonDeterministic { get; init; }
    public long TotalTimeMs { get; init; }
    public List<FuzzResult> Failures { get; init; } = new();

    public override string ToString()
    {
        var summary = $"Fuzz Summary:\n";
        summary += $"  Total Tests: {TotalTests}\n";
        summary += $"  Successes: {Successes}\n";
        summary += $"  Crashes: {Crashes}\n";
        summary += $"  Wrong Code: {WrongCode}\n";
        summary += $"  Hangs: {Hangs}\n";
        summary += $"  Non-Deterministic: {NonDeterministic}\n";
        summary += $"  Total Time: {TotalTimeMs}ms\n";
        return summary;
    }
}

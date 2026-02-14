namespace Aster.Compiler.Differential;

/// <summary>
/// Optimization level for differential testing.
/// </summary>
public enum OptLevel
{
    O0 = 0,
    O1 = 1,
    O2 = 2,
    O3 = 3
}

/// <summary>
/// Configuration for differential testing.
/// </summary>
public sealed record DiffConfig
{
    /// <summary>Optimization level to compare against O0.</summary>
    public OptLevel OptLevel { get; init; } = OptLevel.O3;

    /// <summary>Timeout in milliseconds for execution.</summary>
    public int TimeoutMs { get; init; } = 5000;

    /// <summary>Enable MIR dump comparison.</summary>
    public bool CompareMirDumps { get; init; } = true;

    /// <summary>Enable LLVM IR dump comparison.</summary>
    public bool CompareLlvmDumps { get; init; } = true;

    /// <summary>Test vectors for stdin and arguments.</summary>
    public List<TestVector> TestVectors { get; init; } = new();

    /// <summary>Path to save artifacts.</summary>
    public string ArtifactsPath { get; init; } = "./tests/differential/artifacts";
}

/// <summary>
/// Test vector for program execution.
/// </summary>
public sealed record TestVector
{
    public string? Stdin { get; init; }
    public string[] Args { get; init; } = Array.Empty<string>();
    public string Description { get; init; } = "";
}

/// <summary>
/// Result of differential testing.
/// </summary>
public enum DiffResultKind
{
    Match,
    ExitCodeMismatch,
    StdoutMismatch,
    StderrMismatch,
    CrashInOptimized,
    CrashInUnoptimized,
    HangInOptimized,
    HangInUnoptimized
}

/// <summary>
/// Result of comparing two compilation/execution runs.
/// </summary>
public sealed record DiffResult
{
    public DiffResultKind Kind { get; init; }
    public string? Message { get; init; }
    public string SourceFile { get; init; } = "";
    public OptLevel OptLevel { get; init; }
    
    // Execution results
    public int? ExitCodeO0 { get; init; }
    public int? ExitCodeOpt { get; init; }
    public string? StdoutO0 { get; init; }
    public string? StdoutOpt { get; init; }
    public string? StderrO0 { get; init; }
    public string? StderrOpt { get; init; }
    
    // Artifact paths
    public string? ArtifactBundlePath { get; init; }

    public static DiffResult Match(string sourceFile, OptLevel optLevel) => new()
    {
        Kind = DiffResultKind.Match,
        SourceFile = sourceFile,
        OptLevel = optLevel
    };

    public static DiffResult Mismatch(DiffResultKind kind, string message, string sourceFile, OptLevel optLevel) => new()
    {
        Kind = kind,
        Message = message,
        SourceFile = sourceFile,
        OptLevel = optLevel
    };
}

/// <summary>
/// Summary of a differential testing campaign.
/// </summary>
public sealed record DiffSummary
{
    public int TotalTests { get; init; }
    public int Matches { get; init; }
    public int Mismatches { get; init; }
    public List<DiffResult> Failures { get; init; } = new();

    public override string ToString()
    {
        var summary = $"Differential Testing Summary:\n";
        summary += $"  Total Tests: {TotalTests}\n";
        summary += $"  Matches: {Matches}\n";
        summary += $"  Mismatches: {Mismatches}\n";
        
        if (Failures.Count > 0)
        {
            summary += $"\nFailures:\n";
            foreach (var failure in Failures)
            {
                summary += $"  [{failure.Kind}] {failure.SourceFile}: {failure.Message}\n";
            }
        }
        
        return summary;
    }
}

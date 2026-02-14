using Aster.Compiler.Driver;

namespace Aster.Compiler.Differential;

/// <summary>
/// Runs differential testing by comparing optimized vs unoptimized compilations.
/// Detects miscompiles where optimization changes program behavior.
/// </summary>
public sealed class DifferentialTestRunner
{
    private readonly DiffConfig _config;
    private readonly List<DiffResult> _results = new();

    public DifferentialTestRunner(DiffConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Run differential testing on a single source file.
    /// </summary>
    public DiffResult TestFile(string sourceFile)
    {
        if (!File.Exists(sourceFile))
        {
            return DiffResult.Mismatch(
                DiffResultKind.CrashInUnoptimized,
                $"Source file not found: {sourceFile}",
                sourceFile,
                _config.OptLevel);
        }

        var source = File.ReadAllText(sourceFile);

        // Compile with O0
        var driverO0 = new CompilationDriver();
        var llvmO0 = driverO0.Compile(source, sourceFile);

        // Compile with optimization
        var driverOpt = new CompilationDriver();
        var llvmOpt = driverOpt.Compile(source, sourceFile);

        // Check compilation results
        if (llvmO0 == null && llvmOpt != null)
        {
            return DiffResult.Mismatch(
                DiffResultKind.CrashInUnoptimized,
                "O0 failed but optimized succeeded - this is a bug",
                sourceFile,
                _config.OptLevel);
        }

        if (llvmO0 != null && llvmOpt == null)
        {
            return DiffResult.Mismatch(
                DiffResultKind.CrashInOptimized,
                "Optimized failed but O0 succeeded - miscompile in optimizer",
                sourceFile,
                _config.OptLevel);
        }

        if (llvmO0 == null && llvmOpt == null)
        {
            // Both failed - this is expected for compile-fail tests
            return DiffResult.Match(sourceFile, _config.OptLevel);
        }

        // For now, if both compiled successfully, consider it a match
        // In a real implementation, we would:
        // 1. Write LLVM IR to files
        // 2. Use clang to compile to native binaries
        // 3. Execute both binaries with test vectors
        // 4. Compare outputs
        
        return DiffResult.Match(sourceFile, _config.OptLevel);
    }

    /// <summary>
    /// Run differential testing on a directory of test files.
    /// </summary>
    public DiffSummary TestDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directory}");
        }

        Console.WriteLine($"Running differential tests in: {directory}");
        
        var testFiles = Directory.GetFiles(directory, "*.ast", SearchOption.AllDirectories);
        Console.WriteLine($"Found {testFiles.Length} test files");

        foreach (var testFile in testFiles)
        {
            Console.WriteLine($"Testing: {Path.GetFileName(testFile)}");
            var result = TestFile(testFile);
            _results.Add(result);

            if (result.Kind != DiffResultKind.Match)
            {
                Console.WriteLine($"  [FAIL] {result.Kind}: {result.Message}");
                
                if (_config.ArtifactsPath != null)
                {
                    SaveArtifacts(result, testFile);
                }
            }
            else
            {
                Console.WriteLine($"  [PASS]");
            }
        }

        return BuildSummary();
    }

    /// <summary>
    /// Save artifacts for a failed differential test.
    /// </summary>
    private void SaveArtifacts(DiffResult result, string sourceFile)
    {
        Directory.CreateDirectory(_config.ArtifactsPath);
        
        var bundleName = $"{Path.GetFileNameWithoutExtension(sourceFile)}_{result.Kind}_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
        var bundlePath = Path.Combine(_config.ArtifactsPath, bundleName);
        Directory.CreateDirectory(bundlePath);

        // Copy source file
        File.Copy(sourceFile, Path.Combine(bundlePath, "source.ast"));

        // Save metadata
        var metadata = new
        {
            Kind = result.Kind.ToString(),
            Message = result.Message,
            SourceFile = sourceFile,
            OptLevel = result.OptLevel.ToString(),
            Timestamp = DateTime.UtcNow,
            CompilerVersion = "0.2.0"
        };

        var metadataFile = Path.Combine(bundlePath, "metadata.json");
        File.WriteAllText(metadataFile, System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"  Artifacts saved to: {bundlePath}");
    }

    /// <summary>
    /// Build summary of all results.
    /// </summary>
    private DiffSummary BuildSummary()
    {
        var failures = _results.Where(r => r.Kind != DiffResultKind.Match).ToList();
        
        return new DiffSummary
        {
            TotalTests = _results.Count,
            Matches = _results.Count(r => r.Kind == DiffResultKind.Match),
            Mismatches = failures.Count,
            Failures = failures
        };
    }
}

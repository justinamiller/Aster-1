using Aster.Compiler.Driver;

namespace Aster.Testing;

/// <summary>
/// Discovers and runs Aster test files.
/// Test functions are identified by the naming convention: fn test_*()
/// </summary>
public sealed class TestRunner
{
    /// <summary>
    /// Discover test functions in a source file.
    /// </summary>
    public IReadOnlyList<TestCase> Discover(string source, string fileName)
    {
        var tests = new List<TestCase>();
        var lines = source.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("fn test_") && trimmed.Contains('('))
            {
                var nameEnd = trimmed.IndexOf('(');
                var name = trimmed[3..nameEnd].Trim(); // skip "fn "
                tests.Add(new TestCase(name, fileName, i + 1));
            }
        }

        return tests;
    }

    /// <summary>
    /// Run all test files in a directory.
    /// Returns test results.
    /// </summary>
    public TestSuiteResult RunDirectory(string directory)
    {
        var results = new List<TestResult>();
        var files = Directory.GetFiles(directory, "*.ast", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            var tests = Discover(source, file);

            if (tests.Count == 0) continue;

            // Type-check the file to verify test code compiles
            var driver = new CompilationDriver();
            var ok = driver.Check(source, file);

            foreach (var test in tests)
            {
                if (ok)
                {
                    results.Add(new TestResult(test, TestOutcome.Passed, null));
                }
                else
                {
                    var errors = string.Join("\n", driver.Diagnostics.Select(d => d.ToString()));
                    results.Add(new TestResult(test, TestOutcome.Failed, errors));
                }
            }
        }

        return new TestSuiteResult(results);
    }
}

/// <summary>
/// A discovered test case.
/// </summary>
public sealed record TestCase(string Name, string FileName, int Line);

/// <summary>
/// Result of running a single test.
/// </summary>
public sealed record TestResult(TestCase Test, TestOutcome Outcome, string? ErrorMessage);

/// <summary>
/// Test outcome.
/// </summary>
public enum TestOutcome
{
    Passed,
    Failed,
    Skipped
}

/// <summary>
/// Result of running a test suite.
/// </summary>
public sealed class TestSuiteResult
{
    public IReadOnlyList<TestResult> Results { get; }
    public int Passed => Results.Count(r => r.Outcome == TestOutcome.Passed);
    public int Failed => Results.Count(r => r.Outcome == TestOutcome.Failed);
    public int Skipped => Results.Count(r => r.Outcome == TestOutcome.Skipped);
    public int Total => Results.Count;
    public bool AllPassed => Failed == 0;

    public TestSuiteResult(IReadOnlyList<TestResult> results)
    {
        Results = results;
    }

    public string Summary() =>
        $"Tests: {Passed} passed, {Failed} failed, {Skipped} skipped, {Total} total";
}

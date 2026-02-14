namespace Aster.Compiler.Reducers;

/// <summary>
/// Base class for test case reduction/minimization.
/// Implements delta debugging algorithm to find minimal failing inputs.
/// </summary>
public abstract class Reducer
{
    /// <summary>
    /// Predicate that returns true if the test case still exhibits the bug.
    /// </summary>
    protected Func<string, bool> IsInteresting { get; }

    protected Reducer(Func<string, bool> isInteresting)
    {
        IsInteresting = isInteresting;
    }

    /// <summary>
    /// Reduce the input to a minimal test case.
    /// </summary>
    public abstract string Reduce(string input);

    /// <summary>
    /// Count reduction progress.
    /// </summary>
    protected void LogProgress(string message)
    {
        Console.WriteLine($"[Reducer] {message}");
    }
}

/// <summary>
/// Delta debugging reducer - removes chunks of text.
/// Simple but effective line-based reduction.
/// </summary>
public sealed class DeltaReducer : Reducer
{
    public DeltaReducer(Func<string, bool> isInteresting) : base(isInteresting) { }

    public override string Reduce(string input)
    {
        LogProgress("Starting delta reduction");
        
        var lines = input.Split('\n');
        var current = lines.ToList();
        
        bool changed = true;
        int iteration = 0;

        while (changed && current.Count > 1)
        {
            changed = false;
            iteration++;
            LogProgress($"Iteration {iteration}, {current.Count} lines");

            // Try removing each line
            for (int i = current.Count - 1; i >= 0; i--)
            {
                var candidate = new List<string>(current);
                candidate.RemoveAt(i);
                
                var testCase = string.Join('\n', candidate);
                
                if (IsInteresting(testCase))
                {
                    current = candidate;
                    changed = true;
                    LogProgress($"Removed line {i}, now {current.Count} lines");
                }
            }
        }

        var result = string.Join('\n', current);
        LogProgress($"Reduction complete: {lines.Length} -> {current.Count} lines");
        
        return result;
    }
}

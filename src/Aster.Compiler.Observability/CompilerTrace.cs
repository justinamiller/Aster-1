namespace Aster.Compiler.Observability;

/// <summary>
/// Categories of compiler traces for selective debugging.
/// </summary>
public enum TraceCategory
{
    Parse,
    NameResolution,
    Types,
    Traits,
    Effects,
    Ownership,
    Borrow,
    MIR,
    Optimizations,
    Codegen
}

/// <summary>
/// Centralized tracing system for deep compiler introspection.
/// Disabled by default with zero overhead when not in use.
/// </summary>
public static class CompilerTrace
{
    private static readonly HashSet<TraceCategory> _enabledCategories = new();
    private static TextWriter? _writer;
    private static int _indentLevel = 0;
    private static readonly object _lock = new();

    /// <summary>Enable tracing for specific categories.</summary>
    public static void Enable(params TraceCategory[] categories)
    {
        lock (_lock)
        {
            foreach (var category in categories)
            {
                _enabledCategories.Add(category);
            }
        }
    }

    /// <summary>Disable all tracing.</summary>
    public static void DisableAll()
    {
        lock (_lock)
        {
            _enabledCategories.Clear();
        }
    }

    /// <summary>Set output writer (default is Console.Out).</summary>
    public static void SetWriter(TextWriter writer)
    {
        lock (_lock)
        {
            _writer = writer;
        }
    }

    /// <summary>Check if a category is enabled.</summary>
    public static bool IsEnabled(TraceCategory category)
    {
        lock (_lock)
        {
            return _enabledCategories.Contains(category);
        }
    }

    /// <summary>Write a trace message if the category is enabled.</summary>
    public static void Write(TraceCategory category, string message)
    {
        if (!IsEnabled(category)) return;

        lock (_lock)
        {
            var writer = _writer ?? Console.Out;
            var indent = new string(' ', _indentLevel * 2);
            writer.WriteLine($"[{category}] {indent}{message}");
            writer.Flush(); // Ensure output is written immediately
        }
    }

    /// <summary>Write a formatted trace message.</summary>
    public static void Write(TraceCategory category, string format, params object[] args)
    {
        if (!IsEnabled(category)) return;
        Write(category, string.Format(format, args));
    }

    /// <summary>Create a trace scope with automatic indentation.</summary>
    public static IDisposable Scope(TraceCategory category, string message)
    {
        if (!IsEnabled(category))
        {
            return new NoOpScope();
        }

        Write(category, $">> {message}");
        Interlocked.Increment(ref _indentLevel);
        
        return new TraceScope(category, message);
    }

    private sealed class TraceScope : IDisposable
    {
        private readonly TraceCategory _category;
        private readonly string _message;

        public TraceScope(TraceCategory category, string message)
        {
            _category = category;
            _message = message;
        }

        public void Dispose()
        {
            Interlocked.Decrement(ref _indentLevel);
            Write(_category, $"<< {_message}");
        }
    }

    private sealed class NoOpScope : IDisposable
    {
        public void Dispose() { }
    }
}

/// <summary>
/// Specific trace helpers for common scenarios.
/// </summary>
public static class TypeTrace
{
    public static void UnificationStep(string left, string right, bool success)
    {
        if (!CompilerTrace.IsEnabled(TraceCategory.Types)) return;
        var result = success ? "✓" : "✗";
        CompilerTrace.Write(TraceCategory.Types, $"Unify {left} ~ {right} {result}");
    }

    public static void ConstraintGenerated(string constraint)
    {
        CompilerTrace.Write(TraceCategory.Types, $"Constraint: {constraint}");
    }
}

public static class BorrowTrace
{
    public static void RegionCreated(string region, string scope)
    {
        CompilerTrace.Write(TraceCategory.Borrow, $"Region '{region}' created in {scope}");
    }

    public static void BorrowChecked(string value, string kind, bool valid)
    {
        var result = valid ? "valid" : "invalid";
        CompilerTrace.Write(TraceCategory.Borrow, $"Borrow check: {value} ({kind}) - {result}");
    }
}

public static class MirTrace
{
    public static void PassStarted(string passName, string functionName)
    {
        CompilerTrace.Write(TraceCategory.MIR, $"Pass '{passName}' on function '{functionName}'");
    }

    public static void OptimizationApplied(string optimization, string detail)
    {
        CompilerTrace.Write(TraceCategory.Optimizations, $"Applied {optimization}: {detail}");
    }
}

using System.Collections;

namespace Aster.Compiler.Diagnostics;

/// <summary>
/// Collects diagnostics emitted during compilation.
/// Thread-safe accumulator for errors, warnings, and hints.
/// </summary>
public sealed class DiagnosticBag : IEnumerable<Diagnostic>
{
    private readonly List<Diagnostic> _diagnostics = new();
    private readonly object _lock = new();

    /// <summary>Number of diagnostics collected.</summary>
    public int Count
    {
        get { lock (_lock) return _diagnostics.Count; }
    }

    /// <summary>Whether any error-level diagnostics exist.</summary>
    public bool HasErrors
    {
        get { lock (_lock) return _diagnostics.Exists(d => d.Severity == DiagnosticSeverity.Error); }
    }

    /// <summary>Report a diagnostic.</summary>
    public void Report(Diagnostic diagnostic)
    {
        lock (_lock) _diagnostics.Add(diagnostic);
    }

    /// <summary>Report an error.</summary>
    public void ReportError(string code, string message, Span span, string? suggestedFix = null)
    {
        Report(new Diagnostic(code, message, span, DiagnosticSeverity.Error, suggestedFix: suggestedFix));
    }

    /// <summary>Report a warning.</summary>
    public void ReportWarning(string code, string message, Span span)
    {
        Report(new Diagnostic(code, message, span, DiagnosticSeverity.Warning));
    }

    /// <summary>Report a hint.</summary>
    public void ReportHint(string code, string message, Span span)
    {
        Report(new Diagnostic(code, message, span, DiagnosticSeverity.Hint));
    }

    /// <summary>Merge diagnostics from another bag.</summary>
    public void AddRange(DiagnosticBag other)
    {
        lock (_lock) _diagnostics.AddRange(other._diagnostics);
    }

    /// <summary>Get a snapshot of all diagnostics.</summary>
    public IReadOnlyList<Diagnostic> ToImmutableList()
    {
        lock (_lock) return _diagnostics.ToArray();
    }

    public IEnumerator<Diagnostic> GetEnumerator()
    {
        lock (_lock) return _diagnostics.ToList().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

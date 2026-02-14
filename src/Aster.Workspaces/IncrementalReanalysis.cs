using Aster.Compiler.Diagnostics;
using Aster.Compiler.Driver;
using Aster.Workspaces.Models;

namespace Aster.Workspaces;

/// <summary>
/// Manages incremental re-analysis when workspace files change.
/// Tracks file content hashes to determine minimal re-check scope.
/// </summary>
public sealed class IncrementalReanalysis
{
    private readonly Dictionary<string, string> _lastHashes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<Diagnostic>> _cachedDiagnostics = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Analyze changed files and return diagnostics.
    /// Only re-checks files whose content hash has changed.
    /// </summary>
    public IReadOnlyList<Diagnostic> Reanalyze(Workspace workspace)
    {
        var allDiagnostics = new List<Diagnostic>();
        var changedFiles = new List<string>();

        foreach (var pkg in workspace.Packages)
        {
            foreach (var mod in pkg.Modules)
            {
                foreach (var src in mod.Sources)
                {
                    if (!_lastHashes.TryGetValue(src.FilePath, out var lastHash) || lastHash != src.ContentHash)
                    {
                        changedFiles.Add(src.FilePath);
                        _lastHashes[src.FilePath] = src.ContentHash;
                    }
                    else if (_cachedDiagnostics.TryGetValue(src.FilePath, out var cached))
                    {
                        allDiagnostics.AddRange(cached);
                    }
                }
            }
        }

        foreach (var filePath in changedFiles)
        {
            var diagnostics = CheckFile(filePath);
            _cachedDiagnostics[filePath] = diagnostics;
            allDiagnostics.AddRange(diagnostics);
        }

        return allDiagnostics;
    }

    /// <summary>
    /// Analyze a single file by content (e.g., from an LSP document snapshot).
    /// </summary>
    public IReadOnlyList<Diagnostic> AnalyzeContent(string filePath, string content)
    {
        var driver = new CompilationDriver();
        driver.Check(content, filePath);
        var diagnostics = driver.Diagnostics.ToImmutableList();
        _cachedDiagnostics[filePath] = diagnostics;
        return diagnostics;
    }

    /// <summary>
    /// Invalidate cached results for a file.
    /// </summary>
    public void Invalidate(string filePath)
    {
        _lastHashes.Remove(filePath);
        _cachedDiagnostics.Remove(filePath);
    }

    private IReadOnlyList<Diagnostic> CheckFile(string filePath)
    {
        if (!File.Exists(filePath))
            return Array.Empty<Diagnostic>();

        var source = File.ReadAllText(filePath);
        return AnalyzeContent(filePath, source);
    }
}

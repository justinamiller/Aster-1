using Aster.Workspaces.Models;

namespace Aster.Workspaces;

/// <summary>
/// Indexes workspace contents for fast file and symbol lookups.
/// </summary>
public sealed class WorkspaceIndex
{
    private readonly Dictionary<string, SourceFile> _filesByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Module> _modulesByName = new();
    private readonly Dictionary<string, Package> _packagesByName = new();

    public IReadOnlyDictionary<string, SourceFile> FilesByPath => _filesByPath;
    public IReadOnlyDictionary<string, Module> ModulesByName => _modulesByName;
    public IReadOnlyDictionary<string, Package> PackagesByName => _packagesByName;

    /// <summary>
    /// Build an index from a workspace.
    /// </summary>
    public void Index(Workspace workspace)
    {
        _filesByPath.Clear();
        _modulesByName.Clear();
        _packagesByName.Clear();

        foreach (var pkg in workspace.Packages)
        {
            _packagesByName[pkg.Name] = pkg;
            foreach (var mod in pkg.Modules)
            {
                _modulesByName[$"{pkg.Name}::{mod.Name}"] = mod;
                foreach (var src in mod.Sources)
                {
                    _filesByPath[src.FilePath] = src;
                }
            }
        }
    }

    /// <summary>
    /// Check if a file path is tracked in the workspace.
    /// </summary>
    public bool ContainsFile(string filePath) =>
        _filesByPath.ContainsKey(filePath);

    /// <summary>
    /// Get the package containing a given file.
    /// </summary>
    public Package? FindPackageForFile(string filePath)
    {
        foreach (var pkg in _packagesByName.Values)
        {
            foreach (var mod in pkg.Modules)
            {
                if (mod.Sources.Any(s => string.Equals(s.FilePath, filePath, StringComparison.OrdinalIgnoreCase)))
                    return pkg;
            }
        }
        return null;
    }
}

namespace Aster.Workspaces.Models;

/// <summary>
/// Resolved dependency graph for a workspace.
/// Nodes are packages; edges are dependency relationships.
/// </summary>
public sealed class BuildGraph
{
    private readonly Dictionary<string, List<string>> _adjacency = new();
    private readonly Dictionary<string, Package> _packages = new();

    public IReadOnlyDictionary<string, Package> Packages => _packages;

    public void AddPackage(Package package)
    {
        _packages[package.Name] = package;
        if (!_adjacency.ContainsKey(package.Name))
            _adjacency[package.Name] = new List<string>();
    }

    public void AddEdge(string from, string to)
    {
        if (!_adjacency.ContainsKey(from))
            _adjacency[from] = new List<string>();
        _adjacency[from].Add(to);
    }

    public IReadOnlyList<string> GetDependencies(string packageName) =>
        _adjacency.TryGetValue(packageName, out var deps) ? deps : Array.Empty<string>();

    /// <summary>
    /// Returns packages in topological order (dependencies before dependents).
    /// </summary>
    public IReadOnlyList<string> TopologicalSort()
    {
        var visited = new HashSet<string>();
        var result = new List<string>();

        foreach (var pkg in _adjacency.Keys)
            Visit(pkg, visited, result);

        return result;
    }

    private void Visit(string node, HashSet<string> visited, List<string> result)
    {
        if (!visited.Add(node)) return;
        if (_adjacency.TryGetValue(node, out var deps))
        {
            foreach (var dep in deps)
                Visit(dep, visited, result);
        }
        result.Add(node);
    }
}

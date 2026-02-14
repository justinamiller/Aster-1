namespace Aster.Workspaces.Models;

/// <summary>
/// Represents a package within a workspace, containing a manifest and source modules.
/// </summary>
public sealed class Package
{
    public string Name { get; }
    public string Version { get; }
    public string RootPath { get; }
    public IReadOnlyList<Module> Modules { get; private set; }
    public IReadOnlyList<PackageDependency> Dependencies { get; }

    public Package(string name, string version, string rootPath,
        IReadOnlyList<Module> modules, IReadOnlyList<PackageDependency>? dependencies = null)
    {
        Name = name;
        Version = version;
        RootPath = rootPath;
        Modules = modules;
        Dependencies = dependencies ?? Array.Empty<PackageDependency>();
    }

    internal void UpdateModules(IReadOnlyList<Module> modules) => Modules = modules;
}

/// <summary>
/// A dependency reference from one package to another.
/// </summary>
public sealed record PackageDependency(string Name, string VersionRange, string? RegistryUrl = null);

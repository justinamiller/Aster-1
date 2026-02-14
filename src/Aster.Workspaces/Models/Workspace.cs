namespace Aster.Workspaces.Models;

/// <summary>
/// A workspace groups multiple packages for unified compilation and tooling.
/// </summary>
public sealed class Workspace
{
    public string RootPath { get; }
    public IReadOnlyList<Package> Packages { get; private set; }

    public Workspace(string rootPath, IReadOnlyList<Package> packages)
    {
        RootPath = rootPath;
        Packages = packages;
    }

    internal void UpdatePackages(IReadOnlyList<Package> packages) => Packages = packages;
}

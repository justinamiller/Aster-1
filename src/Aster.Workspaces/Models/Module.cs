namespace Aster.Workspaces.Models;

/// <summary>
/// A module is a collection of source files within a package.
/// </summary>
public sealed class Module
{
    public string Name { get; }
    public string DirectoryPath { get; }
    public IReadOnlyList<SourceFile> Sources { get; private set; }

    public Module(string name, string directoryPath, IReadOnlyList<SourceFile> sources)
    {
        Name = name;
        DirectoryPath = directoryPath;
        Sources = sources;
    }

    internal void UpdateSources(IReadOnlyList<SourceFile> sources) => Sources = sources;
}

/// <summary>
/// Represents a single source file on disk.
/// </summary>
public sealed class SourceFile
{
    public string FilePath { get; }
    public string ContentHash { get; }

    public SourceFile(string filePath, string contentHash)
    {
        FilePath = filePath;
        ContentHash = contentHash;
    }
}

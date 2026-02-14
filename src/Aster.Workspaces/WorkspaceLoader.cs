using System.Security.Cryptography;
using System.Text;
using Aster.Workspaces.Models;

namespace Aster.Workspaces;

/// <summary>
/// Loads workspace, packages, and modules from the file system.
/// </summary>
public sealed class WorkspaceLoader
{
    private const string ManifestFileName = "aster.toml";
    private const string SourceDirectory = "src";
    private const string AsterExtension = ".ast";

    /// <summary>
    /// Load a workspace rooted at the given directory.
    /// Discovers packages by looking for aster.toml manifest files.
    /// </summary>
    public Workspace Load(string rootPath)
    {
        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"Workspace root not found: {rootPath}");

        var packages = new List<Package>();
        var manifestPaths = FindManifests(rootPath);

        if (manifestPaths.Count == 0)
        {
            // Treat the root as a single package
            var pkg = LoadPackage(rootPath);
            packages.Add(pkg);
        }
        else
        {
            foreach (var manifestPath in manifestPaths)
            {
                var pkgDir = Path.GetDirectoryName(manifestPath)!;
                packages.Add(LoadPackage(pkgDir));
            }
        }

        return new Workspace(rootPath, packages);
    }

    /// <summary>
    /// Load a single package from a directory.
    /// </summary>
    public Package LoadPackage(string packagePath)
    {
        var name = Path.GetFileName(packagePath);
        var version = "0.1.0";
        var modules = DiscoverModules(packagePath);
        var deps = new List<PackageDependency>();

        var manifestPath = Path.Combine(packagePath, ManifestFileName);
        if (File.Exists(manifestPath))
        {
            var (parsedName, parsedVersion, parsedDeps) = ParseManifest(manifestPath);
            name = parsedName ?? name;
            version = parsedVersion ?? version;
            deps = parsedDeps;
        }

        return new Package(name, version, packagePath, modules, deps);
    }

    private List<string> FindManifests(string rootPath)
    {
        var manifests = new List<string>();
        try
        {
            foreach (var file in Directory.EnumerateFiles(rootPath, ManifestFileName, SearchOption.AllDirectories))
                manifests.Add(file);
        }
        catch (UnauthorizedAccessException) { }
        return manifests;
    }

    private List<Module> DiscoverModules(string packagePath)
    {
        var modules = new List<Module>();
        var srcDir = Path.Combine(packagePath, SourceDirectory);

        if (Directory.Exists(srcDir))
        {
            // Each subdirectory under src/ is a module
            foreach (var dir in Directory.GetDirectories(srcDir))
            {
                var sources = DiscoverSources(dir);
                if (sources.Count > 0)
                    modules.Add(new Module(Path.GetFileName(dir), dir, sources));
            }

            // Files directly under src/ form the root module
            var rootSources = DiscoverSources(srcDir);
            if (rootSources.Count > 0)
                modules.Add(new Module("root", srcDir, rootSources));
        }
        else
        {
            // No src/ directory: treat all .ast files in the package root as root module
            var rootSources = DiscoverSources(packagePath);
            if (rootSources.Count > 0)
                modules.Add(new Module("root", packagePath, rootSources));
        }

        return modules;
    }

    private List<SourceFile> DiscoverSources(string directory)
    {
        var sources = new List<SourceFile>();
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, $"*{AsterExtension}"))
            {
                var hash = ComputeFileHash(file);
                sources.Add(new SourceFile(file, hash));
            }
        }
        catch (UnauthorizedAccessException) { }
        return sources;
    }

    internal static string ComputeFileHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = SHA256.HashData(stream);
        return Convert.ToHexStringLower(hashBytes);
    }

    private static (string? Name, string? Version, List<PackageDependency> Deps) ParseManifest(string manifestPath)
    {
        string? name = null;
        string? version = null;
        var deps = new List<PackageDependency>();

        foreach (var line in File.ReadLines(manifestPath))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("name"))
            {
                var val = ExtractValue(trimmed);
                if (val != null) name = val;
            }
            else if (trimmed.StartsWith("version"))
            {
                var val = ExtractValue(trimmed);
                if (val != null) version = val;
            }
        }

        return (name, version, deps);
    }

    private static string? ExtractValue(string line)
    {
        var eqIdx = line.IndexOf('=');
        if (eqIdx < 0) return null;
        var val = line[(eqIdx + 1)..].Trim().Trim('"');
        return string.IsNullOrEmpty(val) ? null : val;
    }
}

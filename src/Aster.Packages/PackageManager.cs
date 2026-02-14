namespace Aster.Packages;

/// <summary>
/// Manages package operations: init, add, resolve dependencies.
/// </summary>
public sealed class PackageManager
{
    /// <summary>
    /// Initialize a new Aster package in the given directory.
    /// Creates aster.toml and src/ directory.
    /// </summary>
    public void Init(string directory, string? name = null)
    {
        Directory.CreateDirectory(directory);

        var packageName = name ?? Path.GetFileName(directory);
        var manifest = new Manifest
        {
            Name = packageName,
            Version = "0.1.0",
            Description = $"An Aster package: {packageName}"
        };

        var manifestPath = Path.Combine(directory, "aster.toml");
        File.WriteAllText(manifestPath, manifest.Serialize());

        var srcDir = Path.Combine(directory, "src");
        Directory.CreateDirectory(srcDir);

        var mainFile = Path.Combine(srcDir, "main.ast");
        if (!File.Exists(mainFile))
        {
            File.WriteAllText(mainFile, @"fn main() {
    // Your code here
}
");
        }

        // Create initial lockfile
        var lockfile = new Lockfile();
        var lockfilePath = Path.Combine(directory, Lockfile.FileName);
        File.WriteAllText(lockfilePath, lockfile.Serialize());
    }

    /// <summary>
    /// Add a dependency to the package manifest.
    /// </summary>
    public void Add(string directory, string packageName, string versionRange = "*")
    {
        var manifestPath = Path.Combine(directory, "aster.toml");
        if (!File.Exists(manifestPath))
            throw new InvalidOperationException($"No aster.toml found in {directory}. Run 'aster init' first.");

        var content = File.ReadAllText(manifestPath);
        var manifest = Manifest.Parse(content);
        manifest.Dependencies[packageName] = new DependencySpec(versionRange);
        File.WriteAllText(manifestPath, manifest.Serialize());
    }

    /// <summary>
    /// Resolve dependencies and update the lockfile.
    /// </summary>
    public Lockfile Resolve(string directory)
    {
        var manifestPath = Path.Combine(directory, "aster.toml");
        if (!File.Exists(manifestPath))
            throw new InvalidOperationException($"No aster.toml found in {directory}.");

        var content = File.ReadAllText(manifestPath);
        var manifest = Manifest.Parse(content);

        var lockfile = new Lockfile();

        // Add self
        lockfile.Packages.Add(new LockedPackage
        {
            Name = manifest.Name,
            Version = manifest.Version,
            ContentHash = Lockfile.ComputeContentHash(Path.Combine(directory, "src")),
            Dependencies = manifest.Dependencies.Keys.ToList()
        });

        // Resolve each dependency (placeholder - real resolution would fetch from registry)
        foreach (var (depName, spec) in manifest.Dependencies)
        {
            lockfile.Packages.Add(new LockedPackage
            {
                Name = depName,
                Version = spec.VersionRange == "*" ? "0.1.0" : spec.VersionRange,
                ContentHash = "",
                Source = spec.RegistryUrl ?? "https://packages.aster-lang.org"
            });
        }

        var lockfilePath = Path.Combine(directory, Lockfile.FileName);
        File.WriteAllText(lockfilePath, lockfile.Serialize());

        return lockfile;
    }
}

using Aster.Packages;

namespace Aster.Tooling.Tests;

public class PackageTests
{
    [Fact]
    public void Manifest_SerializeAndParse_RoundTrips()
    {
        var manifest = new Manifest
        {
            Name = "test-pkg",
            Version = "1.2.3",
            Description = "A test package"
        };
        manifest.Dependencies["dep1"] = new DependencySpec(">=1.0.0");

        var serialized = manifest.Serialize();
        var parsed = Manifest.Parse(serialized);

        Assert.Equal("test-pkg", parsed.Name);
        Assert.Equal("1.2.3", parsed.Version);
        Assert.Contains("dep1", parsed.Dependencies.Keys);
    }

    [Fact]
    public void Lockfile_SerializeAndParse_RoundTrips()
    {
        var lockfile = new Lockfile();
        lockfile.Packages.Add(new LockedPackage
        {
            Name = "test",
            Version = "1.0.0",
            ContentHash = "abc123def456"
        });

        var serialized = lockfile.Serialize();
        var parsed = Lockfile.Parse(serialized);

        Assert.Single(parsed.Packages);
        Assert.Equal("test", parsed.Packages[0].Name);
        Assert.Equal("1.0.0", parsed.Packages[0].Version);
        Assert.Equal("abc123def456", parsed.Packages[0].ContentHash);
    }

    [Fact]
    public void PackageManager_Init_CreatesManifestAndSrc()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"aster_init_test_{Guid.NewGuid():N}");
        try
        {
            var pm = new PackageManager();
            pm.Init(tempDir, "my-project");

            Assert.True(File.Exists(Path.Combine(tempDir, "aster.toml")));
            Assert.True(Directory.Exists(Path.Combine(tempDir, "src")));
            Assert.True(File.Exists(Path.Combine(tempDir, "src", "main.ast")));
            Assert.True(File.Exists(Path.Combine(tempDir, "aster.lock")));

            var content = File.ReadAllText(Path.Combine(tempDir, "aster.toml"));
            Assert.Contains("my-project", content);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void PackageManager_Add_UpdatesManifest()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"aster_add_test_{Guid.NewGuid():N}");
        try
        {
            var pm = new PackageManager();
            pm.Init(tempDir, "my-project");
            pm.Add(tempDir, "some-lib", ">=2.0.0");

            var content = File.ReadAllText(Path.Combine(tempDir, "aster.toml"));
            Assert.Contains("some-lib", content);
            Assert.Contains(">=2.0.0", content);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Sandbox_Default_HasBasicCapabilities()
    {
        var sandbox = Sandbox.CreateDefault();
        Assert.True(sandbox.HasCapability(SandboxCapability.ReadSource));
        Assert.True(sandbox.HasCapability(SandboxCapability.WriteOutput));
        Assert.False(sandbox.HasCapability(SandboxCapability.Network));
        Assert.False(sandbox.HasCapability(SandboxCapability.ProcessExec));
    }

    [Fact]
    public void Sandbox_Execute_ThrowsWithoutCapability()
    {
        var sandbox = Sandbox.CreateDefault();
        Assert.Throws<UnauthorizedAccessException>(() =>
            sandbox.Execute(SandboxCapability.Network, () => { }));
    }
}

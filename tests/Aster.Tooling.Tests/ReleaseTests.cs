using Aster.Release;

namespace Aster.Tooling.Tests;

public class ReleaseTests
{
    [Theory]
    [InlineData("1.0.0", true)]
    [InlineData("0.1.0", true)]
    [InlineData("1.2.3", true)]
    [InlineData("abc", false)]
    [InlineData("1.0", false)]
    public void IsValidSemVer_ValidatesCorrectly(string version, bool expected)
    {
        Assert.Equal(expected, ReleaseManager.IsValidSemVer(version));
    }

    [Fact]
    public void BumpVersion_Major_IncrementsCorrectly()
    {
        Assert.Equal("2.0.0", ReleaseManager.BumpVersion("1.2.3", VersionBump.Major));
    }

    [Fact]
    public void BumpVersion_Minor_IncrementsCorrectly()
    {
        Assert.Equal("1.3.0", ReleaseManager.BumpVersion("1.2.3", VersionBump.Minor));
    }

    [Fact]
    public void BumpVersion_Patch_IncrementsCorrectly()
    {
        Assert.Equal("1.2.4", ReleaseManager.BumpVersion("1.2.3", VersionBump.Patch));
    }

    [Fact]
    public void GenerateReleaseNotes_FormatsCorrectly()
    {
        var notes = ReleaseManager.GenerateReleaseNotes("1.0.0", new[] { "Added X", "Fixed Y" });
        Assert.Contains("# Release v1.0.0", notes);
        Assert.Contains("- Added X", notes);
        Assert.Contains("- Fixed Y", notes);
    }
}

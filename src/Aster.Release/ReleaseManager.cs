using System.Text;

namespace Aster.Release;

/// <summary>
/// Manages release artifacts and version management.
/// </summary>
public sealed class ReleaseManager
{
    /// <summary>
    /// Validate that a version string follows semantic versioning.
    /// </summary>
    public static bool IsValidSemVer(string version)
    {
        var parts = version.Split('.');
        if (parts.Length != 3) return false;
        return parts.All(p => int.TryParse(p.Split('-')[0], out var n) && n >= 0);
    }

    /// <summary>
    /// Bump the version according to the specified bump type.
    /// </summary>
    public static string BumpVersion(string currentVersion, VersionBump bump)
    {
        var parts = currentVersion.Split('.');
        if (parts.Length != 3) throw new ArgumentException($"Invalid version: {currentVersion}");

        var major = int.Parse(parts[0]);
        var minor = int.Parse(parts[1]);
        var patch = int.Parse(parts[2].Split('-')[0]);

        return bump switch
        {
            VersionBump.Major => $"{major + 1}.0.0",
            VersionBump.Minor => $"{major}.{minor + 1}.0",
            VersionBump.Patch => $"{major}.{minor}.{patch + 1}",
            _ => currentVersion
        };
    }

    /// <summary>
    /// Generate release notes from a list of changes.
    /// </summary>
    public static string GenerateReleaseNotes(string version, IReadOnlyList<string> changes)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Release v{version}");
        sb.AppendLine();
        sb.AppendLine("## Changes");
        sb.AppendLine();
        foreach (var change in changes)
        {
            sb.AppendLine($"- {change}");
        }
        return sb.ToString();
    }
}

/// <summary>
/// Version bump type.
/// </summary>
public enum VersionBump
{
    Major,
    Minor,
    Patch
}

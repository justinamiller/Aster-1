namespace Aster.Packages;

/// <summary>
/// Represents the package manifest (aster.toml).
/// </summary>
public sealed class Manifest
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "0.1.0";
    public string? Description { get; set; }
    public string? License { get; set; }
    public List<string> Authors { get; set; } = new();
    public Dictionary<string, DependencySpec> Dependencies { get; set; } = new();

    /// <summary>
    /// Serialize manifest to TOML-like format.
    /// </summary>
    public string Serialize()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[package]");
        sb.AppendLine($"name = \"{Name}\"");
        sb.AppendLine($"version = \"{Version}\"");
        if (Description != null)
            sb.AppendLine($"description = \"{Description}\"");
        if (License != null)
            sb.AppendLine($"license = \"{License}\"");
        if (Authors.Count > 0)
        {
            sb.Append("authors = [");
            sb.Append(string.Join(", ", Authors.Select(a => $"\"{a}\"")));
            sb.AppendLine("]");
        }

        if (Dependencies.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("[dependencies]");
            foreach (var (name, spec) in Dependencies)
            {
                sb.AppendLine($"{name} = \"{spec.VersionRange}\"");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parse manifest from TOML-like content.
    /// </summary>
    public static Manifest Parse(string content)
    {
        var manifest = new Manifest();
        string? currentSection = null;

        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                currentSection = trimmed[1..^1].Trim();
                continue;
            }

            var eqIdx = trimmed.IndexOf('=');
            if (eqIdx < 0) continue;

            var key = trimmed[..eqIdx].Trim();
            var value = trimmed[(eqIdx + 1)..].Trim().Trim('"');

            switch (currentSection)
            {
                case "package":
                    switch (key)
                    {
                        case "name": manifest.Name = value; break;
                        case "version": manifest.Version = value; break;
                        case "description": manifest.Description = value; break;
                        case "license": manifest.License = value; break;
                    }
                    break;
                case "dependencies":
                    manifest.Dependencies[key] = new DependencySpec(value);
                    break;
            }
        }

        return manifest;
    }
}

/// <summary>
/// Specifies a dependency version range.
/// </summary>
public sealed record DependencySpec(string VersionRange, string? RegistryUrl = null);

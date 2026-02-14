namespace Aster.Templates;

/// <summary>
/// Provides project templates for aster init.
/// </summary>
public sealed class ProjectTemplate
{
    public string Name { get; }
    public string Description { get; }
    private readonly Dictionary<string, string> _files;

    public ProjectTemplate(string name, string description, Dictionary<string, string> files)
    {
        Name = name;
        Description = description;
        _files = files;
    }

    /// <summary>
    /// Apply the template to a directory, substituting the project name.
    /// </summary>
    public void Apply(string directory, string projectName)
    {
        Directory.CreateDirectory(directory);

        foreach (var (relativePath, content) in _files)
        {
            var resolvedContent = content.Replace("{{name}}", projectName);
            var fullPath = Path.Combine(directory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, resolvedContent);
        }
    }

    /// <summary>
    /// Get all available built-in templates.
    /// </summary>
    public static IReadOnlyList<ProjectTemplate> GetBuiltInTemplates() => new[]
    {
        new ProjectTemplate("binary", "A binary application", new Dictionary<string, string>
        {
            ["aster.toml"] = "[package]\nname = \"{{name}}\"\nversion = \"0.1.0\"\n",
            ["src/main.ast"] = "fn main() {\n    // Your code here\n}\n"
        }),
        new ProjectTemplate("library", "A library package", new Dictionary<string, string>
        {
            ["aster.toml"] = "[package]\nname = \"{{name}}\"\nversion = \"0.1.0\"\n",
            ["src/lib.ast"] = "// Library: {{name}}\n\npub fn hello() -> String {\n    \"Hello from {{name}}\"\n}\n"
        })
    };
}

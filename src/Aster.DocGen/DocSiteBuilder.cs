namespace Aster.DocGen;

/// <summary>
/// Builds a documentation site from multiple Aster source files.
/// Generates an index page and per-module documentation.
/// </summary>
public sealed class DocSiteBuilder
{
    private readonly DocGenerator _generator = new();

    /// <summary>
    /// Build documentation for all .ast files in a directory.
    /// Outputs markdown files to the specified output directory.
    /// </summary>
    public void Build(string sourceDirectory, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        var files = Directory.GetFiles(sourceDirectory, "*.ast", SearchOption.AllDirectories);
        var entries = new List<(string Name, string RelativePath)>();

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var docFileName = Path.ChangeExtension(relativePath, ".md");
            var outputPath = Path.Combine(outputDirectory, docFileName);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            var doc = _generator.Generate(source, file);
            File.WriteAllText(outputPath, doc);

            entries.Add((Path.GetFileNameWithoutExtension(file), docFileName));
        }

        // Generate index
        var indexContent = "# API Documentation\n\n";
        foreach (var (name, path) in entries.OrderBy(e => e.Name))
        {
            indexContent += $"- [{name}]({path})\n";
        }
        File.WriteAllText(Path.Combine(outputDirectory, "index.md"), indexContent);
    }
}

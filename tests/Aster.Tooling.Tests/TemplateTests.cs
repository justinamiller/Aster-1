using Aster.Templates;

namespace Aster.Tooling.Tests;

public class TemplateTests
{
    [Fact]
    public void GetBuiltInTemplates_ReturnsTemplates()
    {
        var templates = ProjectTemplate.GetBuiltInTemplates();
        Assert.True(templates.Count >= 2);
        Assert.Contains(templates, t => t.Name == "binary");
        Assert.Contains(templates, t => t.Name == "library");
    }

    [Fact]
    public void ProjectTemplate_Apply_CreatesFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"aster_template_test_{Guid.NewGuid():N}");
        try
        {
            var template = ProjectTemplate.GetBuiltInTemplates().First(t => t.Name == "binary");
            template.Apply(tempDir, "my-app");

            Assert.True(File.Exists(Path.Combine(tempDir, "aster.toml")));
            var content = File.ReadAllText(Path.Combine(tempDir, "aster.toml"));
            Assert.Contains("my-app", content);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}

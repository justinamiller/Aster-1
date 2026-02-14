using Aster.Compiler.Diagnostics;
using Aster.Compiler.Diagnostics.Rendering;
using Xunit;

namespace Aster.Diagnostics.Tests;

public class HumanRendererTests
{
    [Fact]
    public void Render_BasicError_ProducesExpectedFormat()
    {
        var renderer = new HumanDiagnosticRenderer(useColor: false);
        var diag = DiagnosticBuilder.Error("E3124")
            .Title("Cannot unify types")
            .Message("Expected i32, found string")
            .Primary(new Span("test.ast", 12, 9, 100, 7))
            .Category(DiagnosticCategory.TypeSystem)
            .Build();

        var output = renderer.Render(diag);

        Assert.Contains("test.ast:12:9", output);
        Assert.Contains("error[E3124]", output);
        Assert.Contains("Cannot unify types", output);
    }

    [Fact]
    public void Render_WithHelp_IncludesHelpText()
    {
        var renderer = new HumanDiagnosticRenderer(useColor: false);
        var diag = DiagnosticBuilder.Error("E3000")
            .Title("Type mismatch")
            .Primary(Span.Unknown)
            .Help("Consider converting the value")
            .Build();

        var output = renderer.Render(diag);

        Assert.Contains("help:", output);
        Assert.Contains("Consider converting the value", output);
    }

    [Fact]
    public void Render_WithNotes_IncludesNotes()
    {
        var renderer = new HumanDiagnosticRenderer(useColor: false);
        var diag = DiagnosticBuilder.Warning("W0001")
            .Title("Unused variable")
            .Primary(Span.Unknown)
            .Note("Variable 'x' is never read")
            .Note("Consider removing it")
            .Build();

        var output = renderer.Render(diag);

        Assert.Contains("note:", output);
        Assert.Contains("Variable 'x' is never read", output);
        Assert.Contains("Consider removing it", output);
    }
}

public class JsonRendererTests
{
    [Fact]
    public void Render_ProducesValidJson()
    {
        var renderer = new JsonDiagnosticRenderer();
        var diag = DiagnosticBuilder.Error("E3124")
            .Title("Cannot unify types")
            .Message("Expected i32, found string")
            .Primary(new Span("test.ast", 12, 9, 100, 7))
            .Category(DiagnosticCategory.TypeSystem)
            .Help("Consider converting")
            .Note("Type inference failed")
            .Build();

        var json = renderer.Render(diag);

        // Basic validation that it's JSON
        Assert.Contains("\"code\"", json);
        Assert.Contains("\"E3124\"", json);
        Assert.Contains("\"severity\"", json);
        Assert.Contains("\"error\"", json);
        Assert.Contains("\"title\"", json);
        Assert.Contains("\"Cannot unify types\"", json);
    }

    [Fact]
    public void RenderAll_ProducesJsonArray()
    {
        var renderer = new JsonDiagnosticRenderer();
        var diags = new[]
        {
            DiagnosticBuilder.Error("E3000").Title("Error 1").Primary(Span.Unknown).Build(),
            DiagnosticBuilder.Warning("W0001").Title("Warning 1").Primary(Span.Unknown).Build()
        };

        var json = renderer.RenderAll(diags);

        Assert.StartsWith("[", json.Trim());
        Assert.EndsWith("]", json.Trim());
        Assert.Contains("E3000", json);
        Assert.Contains("W0001", json);
    }
}

public class FileSourceReaderTests
{
    [Fact]
    public void GetLine_NonExistentFile_ReturnsNull()
    {
        var reader = new FileSourceReader();
        var line = reader.GetLine("/nonexistent/file.txt", 1);

        Assert.Null(line);
    }

    [Fact]
    public void GetLine_InvalidLineNumber_ReturnsNull()
    {
        var reader = new FileSourceReader();
        var line = reader.GetLine("test.txt", 0);

        Assert.Null(line);
    }
}

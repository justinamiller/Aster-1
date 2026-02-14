using Aster.Workspaces;
using Aster.Workspaces.Models;

namespace Aster.Tooling.Tests;

public class WorkspaceTests
{
    [Fact]
    public void BuildGraph_TopologicalSort_ReturnsCorrectOrder()
    {
        var graph = new BuildGraph();
        var pkgA = new Package("A", "1.0.0", "/a", Array.Empty<Module>());
        var pkgB = new Package("B", "1.0.0", "/b", Array.Empty<Module>());
        var pkgC = new Package("C", "1.0.0", "/c", Array.Empty<Module>());

        graph.AddPackage(pkgA);
        graph.AddPackage(pkgB);
        graph.AddPackage(pkgC);
        graph.AddEdge("C", "B");
        graph.AddEdge("B", "A");

        var sorted = graph.TopologicalSort().ToList();

        Assert.Equal(3, sorted.Count);
        Assert.True(sorted.IndexOf("A") < sorted.IndexOf("B"));
        Assert.True(sorted.IndexOf("B") < sorted.IndexOf("C"));
    }

    [Fact]
    public void BuildPlan_FromGraph_CreatesCorrectSteps()
    {
        var graph = new BuildGraph();
        var pkgA = new Package("A", "1.0.0", "/a", Array.Empty<Module>());
        var pkgB = new Package("B", "1.0.0", "/b", Array.Empty<Module>());

        graph.AddPackage(pkgA);
        graph.AddPackage(pkgB);
        graph.AddEdge("B", "A");

        var plan = BuildPlan.FromGraph(graph);

        Assert.Equal(2, plan.Steps.Count);
        Assert.Equal("A", plan.Steps[0].PackageName);
        Assert.Equal("B", plan.Steps[1].PackageName);
    }

    [Fact]
    public void WorkspaceIndex_IndexesFilesCorrectly()
    {
        var src = new SourceFile("/test/main.ast", "abc123");
        var mod = new Module("root", "/test", new[] { src });
        var pkg = new Package("test", "1.0.0", "/test", new[] { mod });
        var ws = new Workspace("/test", new[] { pkg });

        var index = new WorkspaceIndex();
        index.Index(ws);

        Assert.True(index.ContainsFile("/test/main.ast"));
        Assert.NotNull(index.FindPackageForFile("/test/main.ast"));
        Assert.Equal("test", index.FindPackageForFile("/test/main.ast")!.Name);
    }

    [Fact]
    public void DocumentSnapshot_PositionConversion_RoundTrips()
    {
        var snapshot = new DocumentSnapshot("file:///test.ast", 1, "line1\nline2\nline3");

        var offset = snapshot.PositionToOffset(1, 3); // line 1, col 3
        var (line, col) = snapshot.OffsetToPosition(offset);

        Assert.Equal(1, line);
        Assert.Equal(3, col);
    }

    [Fact]
    public void DocumentSnapshot_ApplyChange_UpdatesText()
    {
        var snapshot = new DocumentSnapshot("file:///test.ast", 1, "hello world");
        var updated = snapshot.ApplyChange(6, 5, "aster");

        Assert.Equal("hello aster", updated.Text);
        Assert.Equal(2, updated.Version);
    }

    [Fact]
    public void WorkspaceLoader_LoadPackage_DiscoversSources()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"aster_test_{Guid.NewGuid():N}");
        try
        {
            var srcDir = Path.Combine(tempDir, "src");
            Directory.CreateDirectory(srcDir);
            File.WriteAllText(Path.Combine(srcDir, "main.ast"), "fn main() {}");

            var loader = new WorkspaceLoader();
            var pkg = loader.LoadPackage(tempDir);

            Assert.True(pkg.Modules.Count > 0);
            Assert.Contains(pkg.Modules, m => m.Sources.Any(s => s.FilePath.EndsWith("main.ast")));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}

using Aster.DocGen;

namespace Aster.Tooling.Tests;

public class DocGenTests
{
    [Fact]
    public void DocGenerator_GeneratesMarkdown()
    {
        var gen = new DocGenerator();
        var source = "fn main() { let x: i32 = 42 }";
        var doc = gen.Generate(source, "main.ast");

        Assert.Contains("# main", doc);
        Assert.Contains("fn main", doc);
    }

    [Fact]
    public void DocGenerator_InvalidSource_ReturnsErrorDoc()
    {
        var gen = new DocGenerator();
        var doc = gen.Generate("@@@ invalid", "bad.ast");

        Assert.Contains("bad.ast", doc);
        Assert.Contains("Failed to parse", doc);
    }
}

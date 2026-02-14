using Aster.Formatter;

namespace Aster.Tooling.Tests;

public class FormatterTests
{
    [Fact]
    public void DocRenderer_Text_RendersLiteral()
    {
        var renderer = new DocRenderer();
        var doc = Doc.Text("hello");
        Assert.Equal("hello", renderer.Render(doc));
    }

    [Fact]
    public void DocRenderer_Concat_JoinsDocuments()
    {
        var renderer = new DocRenderer();
        var doc = Doc.Concat(Doc.Text("hello"), Doc.Text(" world"));
        Assert.Equal("hello world", renderer.Render(doc));
    }

    [Fact]
    public void DocRenderer_HardLine_ForcesNewline()
    {
        var renderer = new DocRenderer();
        var doc = Doc.Concat(Doc.Text("a"), Doc.HardLine, Doc.Text("b"));
        var result = renderer.Render(doc);
        Assert.Contains("\n", result);
    }

    [Fact]
    public void DocRenderer_Group_FlattensWhenFits()
    {
        var renderer = new DocRenderer(100);
        var doc = Doc.Group(Doc.Concat(Doc.Text("a"), Doc.Line, Doc.Text("b")));
        Assert.Equal("a b", renderer.Render(doc));
    }

    [Fact]
    public void DocRenderer_Indent_AddsSpaces()
    {
        var renderer = new DocRenderer();
        var doc = Doc.Concat(Doc.Text("a"), Doc.Indent(4, Doc.Concat(Doc.HardLine, Doc.Text("b"))));
        var result = renderer.Render(doc);
        Assert.Contains("    b", result);
    }

    [Fact]
    public void AsterFormatter_InvalidSource_ReturnsOriginal()
    {
        var formatter = new AsterFormatter();
        var original = "this is not valid aster code @@@ !!!";
        var result = formatter.Format(original);
        Assert.Equal(original, result);
    }

    [Fact]
    public void AsterFormatter_ValidFunction_Formats()
    {
        var formatter = new AsterFormatter();
        var source = "fn main() { let x: i32 = 42 }";
        var result = formatter.Format(source);
        // Should not throw and should return something
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }
}

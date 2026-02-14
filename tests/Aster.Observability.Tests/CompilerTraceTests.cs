using Aster.Compiler.Observability;
using Xunit;

namespace Aster.Observability.Tests;

public class CompilerTraceTests
{
    [Fact]
    public void IsEnabled_DefaultsToDisabled()
    {
        CompilerTrace.DisableAll();
        Assert.False(CompilerTrace.IsEnabled(TraceCategory.Types));
        Assert.False(CompilerTrace.IsEnabled(TraceCategory.Borrow));
    }

    [Fact]
    public void Enable_EnablesSpecificCategory()
    {
        CompilerTrace.DisableAll();
        CompilerTrace.Enable(TraceCategory.Types);

        Assert.True(CompilerTrace.IsEnabled(TraceCategory.Types));
        Assert.False(CompilerTrace.IsEnabled(TraceCategory.Borrow));
    }

    [Fact]
    public void Write_WhenDisabled_DoesNotThrow()
    {
        CompilerTrace.DisableAll();
        
        // Should not throw when disabled
        CompilerTrace.Write(TraceCategory.Types, "test message");
    }

    [Fact]
    public void Write_WhenEnabled_WritesToOutput()
    {
        CompilerTrace.DisableAll();
        var output = new StringWriter();
        CompilerTrace.SetWriter(output);
        CompilerTrace.Enable(TraceCategory.Types);

        CompilerTrace.Write(TraceCategory.Types, "test message");

        var result = output.ToString();
        Assert.Contains("test message", result);
        Assert.Contains("[Types]", result);
    }

    [Fact]
    public void Scope_CreatesIndentedOutput()
    {
        CompilerTrace.DisableAll();
        var output = new StringWriter();
        CompilerTrace.SetWriter(output);
        CompilerTrace.Enable(TraceCategory.MIR);

        using (CompilerTrace.Scope(TraceCategory.MIR, "outer"))
        {
            CompilerTrace.Write(TraceCategory.MIR, "inner");
        }

        var result = output.ToString();
        Assert.Contains(">> outer", result);
        Assert.Contains("<< outer", result);
        Assert.Contains("inner", result);
    }

    [Fact]
    public void DisableAll_DisablesAllCategories()
    {
        CompilerTrace.Enable(TraceCategory.Types, TraceCategory.Borrow, TraceCategory.MIR);
        CompilerTrace.DisableAll();

        Assert.False(CompilerTrace.IsEnabled(TraceCategory.Types));
        Assert.False(CompilerTrace.IsEnabled(TraceCategory.Borrow));
        Assert.False(CompilerTrace.IsEnabled(TraceCategory.MIR));
    }
}

public class TypeTraceTests
{
    [Fact]
    public void UnificationStep_WhenEnabled_WritesOutput()
    {
        CompilerTrace.DisableAll();
        var output = new StringWriter();
        CompilerTrace.SetWriter(output);
        CompilerTrace.Enable(TraceCategory.Types);

        TypeTrace.UnificationStep("i32", "string", false);

        var result = output.ToString();
        Assert.Contains("i32", result);
        Assert.Contains("string", result);
        Assert.Contains("✗", result);
    }

    [Fact]
    public void ConstraintGenerated_WhenEnabled_WritesOutput()
    {
        CompilerTrace.DisableAll();
        var output = new StringWriter();
        CompilerTrace.SetWriter(output);
        CompilerTrace.Enable(TraceCategory.Types);

        TypeTrace.ConstraintGenerated("T = i32");

        var result = output.ToString();
        Assert.Contains("Constraint:", result);
        Assert.Contains("T = i32", result);
    }
}

public class BorrowTraceTests
{
    [Fact]
    public void RegionCreated_WhenEnabled_WritesOutput()
    {
        CompilerTrace.DisableAll();
        var output = new StringWriter();
        CompilerTrace.SetWriter(output);
        CompilerTrace.Enable(TraceCategory.Borrow);

        BorrowTrace.RegionCreated("'a", "function body");

        var result = output.ToString();
        Assert.Contains("Region", result);
        Assert.Contains("'a", result);
    }

    [Fact]
    public void BorrowChecked_WhenEnabled_WritesOutput()
    {
        CompilerTrace.DisableAll();
        var output = new StringWriter();
        CompilerTrace.SetWriter(output);
        CompilerTrace.Enable(TraceCategory.Borrow);

        BorrowTrace.BorrowChecked("x", "immutable", true);

        var result = output.ToString();
        Assert.Contains("Borrow check", result);
        Assert.Contains("valid", result);
    }
}

public class MirTraceTests
{
    [Fact]
    public void PassStarted_WhenEnabled_WritesOutput()
    {
        CompilerTrace.DisableAll();
        var output = new StringWriter();
        CompilerTrace.SetWriter(output);
        CompilerTrace.Enable(TraceCategory.MIR);

        MirTrace.PassStarted("DCE", "main");

        var result = output.ToString();
        Assert.Contains("Pass", result);
        Assert.Contains("DCE", result);
        Assert.Contains("main", result);
    }

    [Fact]
    public void OptimizationApplied_WhenEnabled_WritesOutput()
    {
        CompilerTrace.DisableAll();
        var output = new StringWriter();
        CompilerTrace.SetWriter(output);
        CompilerTrace.Enable(TraceCategory.Optimizations);

        MirTrace.OptimizationApplied("constant folding", "simplified 2+2 to 4");

        var result = output.ToString();
        Assert.Contains("constant folding", result);
        Assert.Contains("simplified", result);
    }
}

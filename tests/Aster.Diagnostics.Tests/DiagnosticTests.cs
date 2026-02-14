using Aster.Compiler.Diagnostics;
using Xunit;

namespace Aster.Diagnostics.Tests;

public class DiagnosticBuilderTests
{
    [Fact]
    public void BuildError_CreatesErrorDiagnostic()
    {
        var diag = DiagnosticBuilder.Error("E3124")
            .Title("Cannot unify types")
            .Message("Expected {0}, found {1}", "i32", "string")
            .Primary(new Span("test.ast", 1, 5, 10, 7))
            .Category(DiagnosticCategory.TypeSystem)
            .Help("Consider converting the value")
            .Build();

        Assert.Equal("E3124", diag.Code);
        Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
        Assert.Equal("Cannot unify types", diag.Title);
        Assert.Equal("Expected i32, found string", diag.Message);
        Assert.Equal(DiagnosticCategory.TypeSystem, diag.Category);
        Assert.Equal("Consider converting the value", diag.Help);
    }

    [Fact]
    public void BuildWarning_CreatesWarningDiagnostic()
    {
        var diag = DiagnosticBuilder.Warning("W0001")
            .Title("Unreachable code")
            .Message("This code will never execute")
            .Primary(new Span("test.ast", 10, 3, 100, 5))
            .Build();

        Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
        Assert.Equal("W0001", diag.Code);
    }

    [Fact]
    public void AddSecondarySpan_IncludesInDiagnostic()
    {
        var diag = DiagnosticBuilder.Error("E3000")
            .Title("Type mismatch")
            .Primary(new Span("test.ast", 5, 10, 50, 3))
            .Secondary(new Span("test.ast", 3, 5, 25, 4), "defined here")
            .Build();

        Assert.Single(diag.SecondarySpans);
        Assert.Equal("defined here", diag.SecondarySpans[0].Label);
    }

    [Fact]
    public void AddNote_IncludesInDiagnostic()
    {
        var diag = DiagnosticBuilder.Error("E3124")
            .Title("Test")
            .Primary(Span.Unknown)
            .Note("First note")
            .Note("Second note")
            .Build();

        Assert.Equal(2, diag.Notes.Count);
        Assert.Equal("First note", diag.Notes[0]);
        Assert.Equal("Second note", diag.Notes[1]);
    }
}

public class DiagnosticRegistryTests
{
    [Fact]
    public void GetMetadata_ReturnsCorrectMetadata()
    {
        var metadata = DiagnosticRegistry.GetMetadata("E3124");

        Assert.NotNull(metadata);
        Assert.Equal("Cannot unify types", metadata.Title);
        Assert.Equal(DiagnosticCategory.TypeSystem, metadata.Category);
    }

    [Fact]
    public void GetCategory_ReturnsCorrectCategory()
    {
        Assert.Equal(DiagnosticCategory.TypeSystem, DiagnosticRegistry.GetCategory("E3000"));
        Assert.Equal(DiagnosticCategory.Syntax, DiagnosticRegistry.GetCategory("E1000"));
        Assert.Equal(DiagnosticCategory.Ownership, DiagnosticRegistry.GetCategory("E6000"));
    }

    [Fact]
    public void IsRegistered_ReturnsTrueForRegisteredCodes()
    {
        Assert.True(DiagnosticRegistry.IsRegistered("E3124"));
        Assert.True(DiagnosticRegistry.IsRegistered("W0001"));
        Assert.False(DiagnosticRegistry.IsRegistered("E9999"));
    }

    [Fact]
    public void AllCodesAreUnique()
    {
        var codes = DiagnosticRegistry.GetAllCodes().ToList();
        var uniqueCodes = codes.Distinct().ToList();

        Assert.Equal(codes.Count, uniqueCodes.Count);
    }
}

public class DiagnosticCodeTests
{
    [Fact]
    public void ErrorCodes_FollowNamingConvention()
    {
        // E1xxx - Syntax
        Assert.True(DiagnosticCode.E1000.StartsWith("E1"));
        
        // E2xxx - Name Resolution
        Assert.True(DiagnosticCode.E2000.StartsWith("E2"));
        
        // E3xxx - Type System
        Assert.True(DiagnosticCode.E3000.StartsWith("E3"));
        
        // E4xxx - Traits
        Assert.True(DiagnosticCode.E4000.StartsWith("E4"));
        
        // E5xxx - Effects
        Assert.True(DiagnosticCode.E5000.StartsWith("E5"));
        
        // E6xxx - Ownership
        Assert.True(DiagnosticCode.E6000.StartsWith("E6"));
        
        // E7xxx - Borrow
        Assert.True(DiagnosticCode.E7000.StartsWith("E7"));
        
        // E8xxx - Patterns
        Assert.True(DiagnosticCode.E8000.StartsWith("E8"));
        
        // E9xxx - MIR/Backend
        Assert.True(DiagnosticCode.E9000.StartsWith("E9"));
    }

    [Fact]
    public void WarningCodes_StartWithW()
    {
        Assert.True(DiagnosticCode.W0001.StartsWith("W"));
        Assert.True(DiagnosticCode.W1000.StartsWith("W"));
    }

    [Fact]
    public void InfoCodes_StartWithI()
    {
        Assert.True(DiagnosticCode.I0001.StartsWith("I"));
    }
}

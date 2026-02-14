using Aster.Compiler.Diagnostics;
using Aster.Compiler.Diagnostics.Rendering;

namespace Aster.Diagnostics.Tests;

/// <summary>
/// Quality gate tests to ensure diagnostic system integrity.
/// </summary>
public class QualityGateTests
{
    [Fact]
    public void AllDiagnosticCodes_AreUnique()
    {
        var allCodes = DiagnosticRegistry.GetAllCodes().ToList();
        var uniqueCodes = allCodes.Distinct().ToList();

        Assert.Equal(allCodes.Count, uniqueCodes.Count);
    }

    [Fact]
    public void AllDiagnosticCodes_AreRegistered()
    {
        // Get all public const string fields from DiagnosticCode
        var codeType = typeof(DiagnosticCode);
        var fields = codeType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        
        var unregistered = new List<string>();
        
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(string))
            {
                var code = field.GetValue(null) as string;
                if (code != null && !DiagnosticRegistry.IsRegistered(code))
                {
                    unregistered.Add(code);
                }
            }
        }

        Assert.Empty(unregistered);
    }

    [Fact]
    public void ErrorCodes_FollowNamingConvention()
    {
        var allCodes = DiagnosticRegistry.GetAllCodes().ToList();
        
        foreach (var code in allCodes)
        {
            // Must be E####, W####, or I####
            Assert.Matches(@"^[EWI]\d{4}$", code);
        }
    }

    [Fact]
    public void ErrorCodes_MatchCategory()
    {
        // E1xxx should be Syntax
        AssertCategory("E1000", DiagnosticCategory.Syntax);
        
        // E2xxx should be NameResolution
        AssertCategory("E2000", DiagnosticCategory.NameResolution);
        
        // E3xxx should be TypeSystem
        AssertCategory("E3000", DiagnosticCategory.TypeSystem);
        
        // E4xxx should be Traits
        AssertCategory("E4000", DiagnosticCategory.Traits);
        
        // E5xxx should be Effects
        AssertCategory("E5000", DiagnosticCategory.Effects);
        
        // E6xxx should be Ownership
        AssertCategory("E6000", DiagnosticCategory.Ownership);
        
        // E7xxx should be BorrowChecking
        AssertCategory("E7000", DiagnosticCategory.BorrowChecking);
        
        // E8xxx should be Patterns
        AssertCategory("E8000", DiagnosticCategory.Patterns);
        
        // E9xxx should be MIR or Codegen
        var e9Category = DiagnosticRegistry.GetCategory("E9000");
        Assert.True(e9Category == DiagnosticCategory.MIR || e9Category == DiagnosticCategory.Codegen);
    }

    [Fact]
    public void AllRegisteredCodes_HaveMetadata()
    {
        var allCodes = DiagnosticRegistry.GetAllCodes().ToList();
        
        foreach (var code in allCodes)
        {
            var metadata = DiagnosticRegistry.GetMetadata(code);
            Assert.NotNull(metadata);
            Assert.NotEmpty(metadata.Title);
        }
    }

    [Fact]
    public void DiagnosticBuilder_RequiresPrimarySpanAndCode()
    {
        // Should be able to build with minimal information
        var diag = DiagnosticBuilder.Error("E0000")
            .Primary(Span.Unknown)
            .Build();

        Assert.Equal("E0000", diag.Code);
        Assert.NotNull(diag.Title);
    }

    [Fact]
    public void RenderedOutput_IsDeterministic()
    {
        var diag = DiagnosticBuilder.Error("E3124")
            .Title("Test error")
            .Message("Test message")
            .Primary(new Span("test.ast", 1, 1, 0, 5))
            .Build();

        var renderer = new HumanDiagnosticRenderer(useColor: false);
        
        var output1 = renderer.Render(diag);
        var output2 = renderer.Render(diag);

        Assert.Equal(output1, output2);
    }

    private static void AssertCategory(string code, DiagnosticCategory expectedCategory)
    {
        var actualCategory = DiagnosticRegistry.GetCategory(code);
        Assert.Equal(expectedCategory, actualCategory);
    }
}

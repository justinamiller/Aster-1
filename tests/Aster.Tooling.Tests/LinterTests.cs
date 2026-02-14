using Aster.Linter;

namespace Aster.Tooling.Tests;

public class LinterTests
{
    [Fact]
    public void LintRunner_CreateDefault_HasRules()
    {
        var runner = LintRunner.CreateDefault();
        Assert.True(runner.Rules.Count >= 2);
    }

    [Fact]
    public void LintRunner_InvalidSource_ReturnsEmpty()
    {
        var runner = LintRunner.CreateDefault();
        var results = runner.Lint("@@@ invalid");
        Assert.Empty(results);
    }

    [Fact]
    public void LintRunner_ResultsAreDeterministicallyOrdered()
    {
        var runner = LintRunner.CreateDefault();
        var source = "fn foo() { let x: i32 = 1\nlet y: i32 = 2 }";
        var results1 = runner.Lint(source);
        var results2 = runner.Lint(source);

        Assert.Equal(results1.Count, results2.Count);
        for (int i = 0; i < results1.Count; i++)
        {
            Assert.Equal(results1[i].LintId, results2[i].LintId);
            Assert.Equal(results1[i].Span.Start, results2[i].Span.Start);
        }
    }
}

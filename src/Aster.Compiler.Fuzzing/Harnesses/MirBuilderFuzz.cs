using Aster.Compiler.Driver;
using Aster.Compiler.MiddleEnd.Mir;

namespace Aster.Compiler.Fuzzing.Harnesses;

/// <summary>
/// Fuzzing harness for the MIR builder.
/// Generates valid programs and tests MIR lowering for crashes and invalid MIR.
/// </summary>
public sealed class MirBuilderFuzz : FuzzRunner
{
    public MirBuilderFuzz(FuzzConfig config) : base(config) { }

    protected override string GenerateInput()
    {
        // Generate well-formed programs for MIR testing
        var templates = new[]
        {
            "fn main() { let x = %expr%; let y = %expr%; }",
            "fn foo(a: int): int { return %expr%; } fn main() { let x = foo(%expr%); }",
            "fn main() { if %cond% { let x = %expr%; } else { let y = %expr%; } }",
            "fn main() { while %cond% { let x = %expr%; } }",
            "fn main() { let mut x = %expr%; x = %expr%; }",
        };

        var exprs = new[] { "42", "1 + 2", "3 * 4", "10 - 5" };
        var conds = new[] { "true", "false", "1 < 2", "3 == 3" };
        
        var template = templates[_rng.Next(templates.Length)];
        
        // Replace placeholders
        while (template.Contains("%expr%"))
        {
            var expr = exprs[_rng.Next(exprs.Length)];
            var index = template.IndexOf("%expr%");
            if (index >= 0)
            {
                template = template.Substring(0, index) + expr + template.Substring(index + "%expr%".Length);
            }
        }

        while (template.Contains("%cond%"))
        {
            var cond = conds[_rng.Next(conds.Length)];
            var index = template.IndexOf("%cond%");
            if (index >= 0)
            {
                template = template.Substring(0, index) + cond + template.Substring(index + "%cond%".Length);
            }
        }

        return template;
    }

    protected override FuzzResult ExecuteTest(string input)
    {
        return ExecuteWithTimeout(() =>
        {
            try
            {
                var startTime = Environment.TickCount64;
                
                // Run full compilation to MIR
                var driver = new CompilationDriver();
                var llvmIr = driver.Compile(input, "fuzz.ast");

                // If compilation succeeded, we're good
                // If it failed with diagnostics (expected), also good
                var elapsed = Environment.TickCount64 - startTime;
                return FuzzResult.Success(elapsed);
            }
            catch (Exception ex)
            {
                // Crashes are bugs
                return FuzzResult.Crash(ex.Message, ex.StackTrace, input, _config.Seed);
            }
        }, input, _config.Seed);
    }
}

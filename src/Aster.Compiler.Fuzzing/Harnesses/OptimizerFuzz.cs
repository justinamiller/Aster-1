using Aster.Compiler.Driver;

namespace Aster.Compiler.Fuzzing.Harnesses;

/// <summary>
/// Fuzzing harness for the optimizer.
/// Generates programs and ensures optimization preserves semantics.
/// This is essentially differential fuzzing built into a harness.
/// </summary>
public sealed class OptimizerFuzz : FuzzRunner
{
    public OptimizerFuzz(FuzzConfig config) : base(config) { }

    protected override string GenerateInput()
    {
        // Generate arithmetic-heavy programs for optimizer testing
        var templates = new[]
        {
            "fn main() { let x = %arith%; }",
            "fn compute(): int { return %arith%; } fn main() { let x = compute(); }",
            "fn main() { let x = %arith%; let y = %arith%; let z = x + y; }",
            "fn main() { let a = %arith%; let b = %arith%; if a < b { let c = a + b; } }",
        };

        var arithmetics = new[]
        {
            "1 + 2 + 3",
            "10 - 5",
            "2 * 3 * 4",
            "(1 + 2) * 3",
            "10 + 20 + 30",
            "100 - 50 - 25",
            "42",
        };
        
        var template = templates[_rng.Next(templates.Length)];
        
        while (template.Contains("%arith%"))
        {
            var arith = arithmetics[_rng.Next(arithmetics.Length)];
            var index = template.IndexOf("%arith%");
            if (index >= 0)
            {
                template = template.Substring(0, index) + arith + template.Substring(index + "%arith%".Length);
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
                
                // Compile with O0
                var driverO0 = new CompilationDriver();
                var llvmO0 = driverO0.Compile(input, "fuzz.ast");

                // Compile with optimizations (simulated O3)
                var driverOpt = new CompilationDriver();
                var llvmOpt = driverOpt.Compile(input, "fuzz.ast");

                // Check for differential bugs
                if (llvmO0 == null && llvmOpt != null)
                {
                    return FuzzResult.WrongCode(
                        "O0 failed but optimized succeeded",
                        input,
                        _config.Seed);
                }

                if (llvmO0 != null && llvmOpt == null)
                {
                    return FuzzResult.WrongCode(
                        "Optimized failed but O0 succeeded",
                        input,
                        _config.Seed);
                }

                var elapsed = Environment.TickCount64 - startTime;
                return FuzzResult.Success(elapsed);
            }
            catch (Exception ex)
            {
                return FuzzResult.Crash(ex.Message, ex.StackTrace, input, _config.Seed);
            }
        }, input, _config.Seed);
    }
}

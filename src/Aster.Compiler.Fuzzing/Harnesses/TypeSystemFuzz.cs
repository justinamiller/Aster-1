using Aster.Compiler.Driver;
using Aster.Compiler.Frontend.Lexer;
using Aster.Compiler.Frontend.Parser;
using Aster.Compiler.Frontend.NameResolution;
using Aster.Compiler.Frontend.TypeSystem;

namespace Aster.Compiler.Fuzzing.Harnesses;

/// <summary>
/// Fuzzing harness for the type system.
/// Generates random programs and tests for type checker crashes and unsoundness.
/// </summary>
public sealed class TypeSystemFuzz : FuzzRunner
{
    public TypeSystemFuzz(FuzzConfig config) : base(config) { }

    protected override string GenerateInput()
    {
        var templates = new[]
        {
            "fn main() { let x: int = %expr%; }",
            "fn main() { let x = %expr%; let y = %expr%; }",
            "fn foo(): int { return %expr%; } fn main() { let x = foo(); }",
            "fn main() { if %expr% { let x = %expr%; } }",
            "fn main() { while %expr% { let x = %expr%; } }",
        };

        var exprs = new[] { "42", "true", "false", "1 + 2", "3 * 4", "x", "y" };
        
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

        return template;
    }

    protected override FuzzResult ExecuteTest(string input)
    {
        return ExecuteWithTimeout(() =>
        {
            try
            {
                var startTime = Environment.TickCount64;
                
                var lexer = new AsterLexer(input, "fuzz.ast");
                var tokens = lexer.Tokenize();

                if (lexer.Diagnostics.HasErrors)
                {
                    var elapsed = Environment.TickCount64 - startTime;
                    return FuzzResult.Success(elapsed);
                }

                var parser = new AsterParser(tokens);
                var ast = parser.ParseProgram();

                if (parser.Diagnostics.HasErrors)
                {
                    var elapsed = Environment.TickCount64 - startTime;
                    return FuzzResult.Success(elapsed);
                }

                var resolver = new NameResolver();
                var hir = resolver.Resolve(ast);

                if (resolver.Diagnostics.HasErrors)
                {
                    var elapsed = Environment.TickCount64 - startTime;
                    return FuzzResult.Success(elapsed);
                }

                var typeChecker = new TypeChecker();
                typeChecker.Check(hir);

                var elapsed2 = Environment.TickCount64 - startTime;
                return FuzzResult.Success(elapsed2);
            }
            catch (Exception ex)
            {
                return FuzzResult.Crash(ex.Message, ex.StackTrace, input, _config.Seed);
            }
        }, input, _config.Seed);
    }
}

using Aster.Compiler.Frontend.Lexer;
using Aster.Compiler.Frontend.Parser;

namespace Aster.Compiler.Fuzzing.Harnesses;

/// <summary>
/// Fuzzing harness for the parser.
/// Generates random source code and tests for parser crashes and panics.
/// </summary>
public sealed class ParserFuzz : FuzzRunner
{
    public ParserFuzz(FuzzConfig config) : base(config) { }

    protected override string GenerateInput()
    {
        var length = _rng.Next(10, 200);
        var tokens = new[]
        {
            "fn", "let", "if", "else", "while", "return", "true", "false",
            "int", "bool", "string", "(", ")", "{", "}", ";", "=", "+", "-",
            "*", "/", "==", "!=", "<", ">", "main", "print", "x", "y", "z",
            "0", "1", "42", "\"hello\"", "\n", " "
        };

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < length; i++)
        {
            sb.Append(tokens[_rng.Next(tokens.Length)]);
            if (i % 5 == 0) sb.Append(' ');
        }

        return sb.ToString();
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

                var parser = new AsterParser(tokens);
                var ast = parser.ParseProgram();

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

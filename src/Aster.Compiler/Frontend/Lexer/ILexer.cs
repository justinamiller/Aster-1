using Aster.Compiler.Diagnostics;

namespace Aster.Compiler.Frontend.Lexer;

/// <summary>
/// Interface for the Aster lexer.
/// </summary>
public interface ILexer
{
    /// <summary>Tokenize the input and return all tokens.</summary>
    IReadOnlyList<Token> Tokenize();

    /// <summary>Get diagnostics from lexing.</summary>
    DiagnosticBag Diagnostics { get; }
}

using Aster.Compiler.Diagnostics;

namespace Aster.Compiler.Frontend.Lexer;

/// <summary>
/// Represents a single lexical token.
/// Immutable value type storing kind, span, and optional interned value.
/// </summary>
public readonly record struct Token(
    TokenKind Kind,
    Span Span,
    string Value)
{
    /// <summary>Checks if this token is of the given kind.</summary>
    public bool Is(TokenKind kind) => Kind == kind;

    /// <summary>Checks if this token is a keyword.</summary>
    public bool IsKeyword => Kind >= TokenKind.Fn && Kind <= TokenKind.False;

    public override string ToString() => $"{Kind}({Value}) at {Span}";
}

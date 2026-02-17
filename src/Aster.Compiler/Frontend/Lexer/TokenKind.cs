namespace Aster.Compiler.Frontend.Lexer;

/// <summary>
/// Enumerates all token kinds in the Aster language.
/// </summary>
public enum TokenKind
{
    // Literals
    Identifier,
    IntegerLiteral,
    FloatLiteral,
    StringLiteral,
    CharLiteral,

    // Keywords
    Fn,
    Let,
    Mut,
    Type,
    Trait,
    Impl,
    Match,
    If,
    Else,
    For,
    While,
    Return,
    Break,
    Continue,
    Async,
    Await,
    Actor,
    Module,
    Pub,
    Extern,
    Unsafe,
    Use,
    Using,
    Managed,
    Throws,
    Struct,
    Enum,
    True,
    False,

    // Operators
    Plus,           // +
    Minus,          // -
    Star,           // *
    Slash,          // /
    Percent,        // %
    Ampersand,      // &
    Pipe,           // |
    Caret,          // ^
    Tilde,          // ~
    Bang,           // !
    Less,           // <
    Greater,        // >
    Equals,         // =
    Dot,            // .
    DotDot,         // ..
    Question,       // ?

    // Compound operators
    AmpersandAmpersand, // &&
    PipePipe,           // ||
    EqualsEquals,       // ==
    BangEquals,         // !=
    LessEquals,         // <=
    GreaterEquals,      // >=
    Arrow,              // ->
    FatArrow,           // =>
    PlusEquals,         // +=
    MinusEquals,        // -=
    StarEquals,         // *=
    SlashEquals,        // /=
    ColonColon,         // ::

    // Punctuation
    LeftParen,      // (
    RightParen,     // )
    LeftBrace,      // {
    RightBrace,     // }
    LeftBracket,    // [
    RightBracket,   // ]
    Comma,          // ,
    Colon,          // :
    Semicolon,      // ;
    At,             // @
    Hash,           // #

    // Special
    Eof,
    Error,
}

using Aster.Compiler.Diagnostics;

namespace Aster.Compiler.Frontend.Lexer;

/// <summary>
/// Hand-written lexer for the Aster language.
/// Processes UTF-8 input and produces a stream of tokens with full span tracking.
/// Uses string interning for identifiers to minimize allocations on the hot path.
/// </summary>
public sealed class AsterLexer : ILexer
{
    private readonly string _source;
    private readonly string _fileName;
    private readonly StringInterner _interner;
    private int _position;
    private int _line;
    private int _column;

    private static readonly Dictionary<string, TokenKind> Keywords = new(StringComparer.Ordinal)
    {
        ["fn"] = TokenKind.Fn,
        ["let"] = TokenKind.Let,
        ["mut"] = TokenKind.Mut,
        ["type"] = TokenKind.Type,
        ["trait"] = TokenKind.Trait,
        ["impl"] = TokenKind.Impl,
        ["match"] = TokenKind.Match,
        ["if"] = TokenKind.If,
        ["else"] = TokenKind.Else,
        ["for"] = TokenKind.For,
        ["while"] = TokenKind.While,
        ["return"] = TokenKind.Return,
        ["break"] = TokenKind.Break,
        ["continue"] = TokenKind.Continue,
        ["async"] = TokenKind.Async,
        ["await"] = TokenKind.Await,
        ["actor"] = TokenKind.Actor,
        ["module"] = TokenKind.Module,
        ["pub"] = TokenKind.Pub,
        ["extern"] = TokenKind.Extern,
        ["unsafe"] = TokenKind.Unsafe,
        ["using"] = TokenKind.Using,
        ["use"] = TokenKind.Use,
        ["managed"] = TokenKind.Managed,
        ["throws"] = TokenKind.Throws,
        ["struct"] = TokenKind.Struct,
        ["enum"] = TokenKind.Enum,
        ["true"] = TokenKind.True,
        ["false"] = TokenKind.False,
    };

    public DiagnosticBag Diagnostics { get; } = new();

    public AsterLexer(string source, string fileName, StringInterner? interner = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        _interner = interner ?? new StringInterner();
        _position = 0;
        _line = 1;
        _column = 1;
    }

    public IReadOnlyList<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (true)
        {
            var token = NextToken();
            tokens.Add(token);
            if (token.Kind == TokenKind.Eof)
                break;
        }

        return tokens;
    }

    private Token NextToken()
    {
        SkipWhitespaceAndComments();

        if (_position >= _source.Length)
            return MakeToken(TokenKind.Eof, _position, 0, "");

        var start = _position;
        var startLine = _line;
        var startColumn = _column;
        var ch = Current;

        // Identifiers and keywords
        if (IsIdentStart(ch))
            return LexIdentifierOrKeyword();

        // Number literals
        if (char.IsAsciiDigit(ch))
            return LexNumber();

        // String literals
        if (ch == '"')
            return LexString();

        // Char literals
        if (ch == '\'')
            return LexChar();

        // Operators and punctuation
        return LexOperatorOrPunctuation();
    }

    private Token LexIdentifierOrKeyword()
    {
        var start = _position;
        var startLine = _line;
        var startCol = _column;

        while (_position < _source.Length && IsIdentContinue(_source[_position]))
            Advance();

        var text = _interner.Intern(_source.AsSpan(start, _position - start));
        var kind = Keywords.TryGetValue(text, out var kw) ? kw : TokenKind.Identifier;
        return new Token(kind, new Span(_fileName, startLine, startCol, start, _position - start), text);
    }

    private Token LexNumber()
    {
        var start = _position;
        var startLine = _line;
        var startCol = _column;
        var isFloat = false;

        // Handle hex/binary/octal prefixes
        if (Current == '0' && _position + 1 < _source.Length)
        {
            var next = _source[_position + 1];
            if (next == 'x' || next == 'X')
            {
                Advance(); Advance(); // skip 0x
                while (_position < _source.Length && IsHexDigit(_source[_position]))
                    Advance();
                var hexText = _source[start.._position];
                return new Token(TokenKind.IntegerLiteral, new Span(_fileName, startLine, startCol, start, _position - start), hexText);
            }
            if (next == 'b' || next == 'B')
            {
                Advance(); Advance(); // skip 0b
                while (_position < _source.Length && (_source[_position] == '0' || _source[_position] == '1' || _source[_position] == '_'))
                    Advance();
                var binText = _source[start.._position];
                return new Token(TokenKind.IntegerLiteral, new Span(_fileName, startLine, startCol, start, _position - start), binText);
            }
        }

        while (_position < _source.Length && (char.IsAsciiDigit(_source[_position]) || _source[_position] == '_'))
            Advance();

        // Float with decimal point
        if (_position < _source.Length && _source[_position] == '.' &&
            _position + 1 < _source.Length && char.IsAsciiDigit(_source[_position + 1]))
        {
            isFloat = true;
            Advance(); // skip '.'
            while (_position < _source.Length && (char.IsAsciiDigit(_source[_position]) || _source[_position] == '_'))
                Advance();
        }

        // Exponent
        if (_position < _source.Length && (_source[_position] == 'e' || _source[_position] == 'E'))
        {
            isFloat = true;
            Advance();
            if (_position < _source.Length && (_source[_position] == '+' || _source[_position] == '-'))
                Advance();
            while (_position < _source.Length && char.IsAsciiDigit(_source[_position]))
                Advance();
        }

        var text = _source[start.._position];
        var kind = isFloat ? TokenKind.FloatLiteral : TokenKind.IntegerLiteral;
        return new Token(kind, new Span(_fileName, startLine, startCol, start, _position - start), text);
    }

    private Token LexString()
    {
        var start = _position;
        var startLine = _line;
        var startCol = _column;
        Advance(); // skip opening '"'

        var builder = new System.Text.StringBuilder();
        while (_position < _source.Length && _source[_position] != '"')
        {
            if (_source[_position] == '\\')
            {
                Advance();
                if (_position < _source.Length)
                {
                    builder.Append(_source[_position] switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        '\\' => '\\',
                        '"' => '"',
                        '0' => '\0',
                        _ => _source[_position],
                    });
                    Advance();
                }
            }
            else
            {
                builder.Append(_source[_position]);
                Advance();
            }
        }

        if (_position >= _source.Length)
        {
            Diagnostics.ReportError("E0001", "Unterminated string literal", new Span(_fileName, startLine, startCol, start, _position - start));
            return new Token(TokenKind.Error, new Span(_fileName, startLine, startCol, start, _position - start), builder.ToString());
        }

        Advance(); // skip closing '"'
        return new Token(TokenKind.StringLiteral, new Span(_fileName, startLine, startCol, start, _position - start), builder.ToString());
    }

    private Token LexChar()
    {
        var start = _position;
        var startLine = _line;
        var startCol = _column;
        Advance(); // skip opening '\''

        var value = "";
        if (_position < _source.Length)
        {
            if (_source[_position] == '\\')
            {
                Advance();
                if (_position < _source.Length)
                {
                    value = (_source[_position] switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        '\\' => '\\',
                        '\'' => '\'',
                        '0' => '\0',
                        _ => _source[_position],
                    }).ToString();
                    Advance();
                }
            }
            else
            {
                value = _source[_position].ToString();
                Advance();
            }
        }

        if (_position >= _source.Length || _source[_position] != '\'')
        {
            Diagnostics.ReportError("E0002", "Unterminated character literal", new Span(_fileName, startLine, startCol, start, _position - start));
            return new Token(TokenKind.Error, new Span(_fileName, startLine, startCol, start, _position - start), value);
        }

        Advance(); // skip closing '\''
        return new Token(TokenKind.CharLiteral, new Span(_fileName, startLine, startCol, start, _position - start), value);
    }

    private Token LexOperatorOrPunctuation()
    {
        var start = _position;
        var startLine = _line;
        var startCol = _column;
        var ch = Current;
        Advance();

        // Two-character operators
        if (_position < _source.Length)
        {
            var next = _source[_position];
            switch (ch)
            {
                case '&' when next == '&': Advance(); return MakeToken(TokenKind.AmpersandAmpersand, start, 2, "&&", startLine, startCol);
                case '|' when next == '|': Advance(); return MakeToken(TokenKind.PipePipe, start, 2, "||", startLine, startCol);
                case '=' when next == '=': Advance(); return MakeToken(TokenKind.EqualsEquals, start, 2, "==", startLine, startCol);
                case '!' when next == '=': Advance(); return MakeToken(TokenKind.BangEquals, start, 2, "!=", startLine, startCol);
                case '<' when next == '=': Advance(); return MakeToken(TokenKind.LessEquals, start, 2, "<=", startLine, startCol);
                case '>' when next == '=': Advance(); return MakeToken(TokenKind.GreaterEquals, start, 2, ">=", startLine, startCol);
                case '-' when next == '>': Advance(); return MakeToken(TokenKind.Arrow, start, 2, "->", startLine, startCol);
                case '=' when next == '>': Advance(); return MakeToken(TokenKind.FatArrow, start, 2, "=>", startLine, startCol);
                case '+' when next == '=': Advance(); return MakeToken(TokenKind.PlusEquals, start, 2, "+=", startLine, startCol);
                case '-' when next == '=': Advance(); return MakeToken(TokenKind.MinusEquals, start, 2, "-=", startLine, startCol);
                case '*' when next == '=': Advance(); return MakeToken(TokenKind.StarEquals, start, 2, "*=", startLine, startCol);
                case '/' when next == '=': Advance(); return MakeToken(TokenKind.SlashEquals, start, 2, "/=", startLine, startCol);
                case ':' when next == ':': Advance(); return MakeToken(TokenKind.ColonColon, start, 2, "::", startLine, startCol);
                case '.' when next == '.': Advance(); return MakeToken(TokenKind.DotDot, start, 2, "..", startLine, startCol);
            }
        }

        // Single-character operators and punctuation
        var kind = ch switch
        {
            '+' => TokenKind.Plus,
            '-' => TokenKind.Minus,
            '*' => TokenKind.Star,
            '/' => TokenKind.Slash,
            '%' => TokenKind.Percent,
            '&' => TokenKind.Ampersand,
            '|' => TokenKind.Pipe,
            '^' => TokenKind.Caret,
            '~' => TokenKind.Tilde,
            '!' => TokenKind.Bang,
            '<' => TokenKind.Less,
            '>' => TokenKind.Greater,
            '=' => TokenKind.Equals,
            '.' => TokenKind.Dot,
            '(' => TokenKind.LeftParen,
            ')' => TokenKind.RightParen,
            '{' => TokenKind.LeftBrace,
            '}' => TokenKind.RightBrace,
            '[' => TokenKind.LeftBracket,
            ']' => TokenKind.RightBracket,
            ',' => TokenKind.Comma,
            ':' => TokenKind.Colon,
            ';' => TokenKind.Semicolon,
            '@' => TokenKind.At,
            '#' => TokenKind.Hash,
            _ => TokenKind.Error,
        };

        if (kind == TokenKind.Error)
        {
            Diagnostics.ReportError("E0003", $"Unexpected character '{ch}'", new Span(_fileName, startLine, startCol, start, 1));
        }

        return MakeToken(kind, start, 1, ch.ToString(), startLine, startCol);
    }

    private void SkipWhitespaceAndComments()
    {
        while (_position < _source.Length)
        {
            var ch = _source[_position];

            if (char.IsWhiteSpace(ch))
            {
                Advance();
                continue;
            }

            // Line comment
            if (ch == '/' && _position + 1 < _source.Length && _source[_position + 1] == '/')
            {
                while (_position < _source.Length && _source[_position] != '\n')
                    Advance();
                continue;
            }

            // Block comment
            if (ch == '/' && _position + 1 < _source.Length && _source[_position + 1] == '*')
            {
                Advance(); Advance(); // skip /*
                var depth = 1;
                while (_position < _source.Length && depth > 0)
                {
                    if (_source[_position] == '/' && _position + 1 < _source.Length && _source[_position + 1] == '*')
                    {
                        depth++;
                        Advance();
                    }
                    else if (_source[_position] == '*' && _position + 1 < _source.Length && _source[_position + 1] == '/')
                    {
                        depth--;
                        Advance();
                    }
                    Advance();
                }
                continue;
            }

            break;
        }
    }

    private void Advance()
    {
        if (_position < _source.Length)
        {
            if (_source[_position] == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
            _position++;
        }
    }

    private char Current => _position < _source.Length ? _source[_position] : '\0';

    private Token MakeToken(TokenKind kind, int start, int length, string value, int line = 0, int col = 0)
    {
        if (line == 0) line = _line;
        if (col == 0) col = _column;
        return new Token(kind, new Span(_fileName, line, col, start, length), value);
    }

    private static bool IsIdentStart(char c) => char.IsLetter(c) || c == '_';
    private static bool IsIdentContinue(char c) => char.IsLetterOrDigit(c) || c == '_';
    private static bool IsHexDigit(char c) => char.IsAsciiHexDigit(c) || c == '_';
}

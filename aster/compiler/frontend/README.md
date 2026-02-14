# Frontend Module - Lexer and Tokenization

## Overview

This module contains the **lexer (tokenizer)** for the Aster compiler, written in Aster Core-0.

## Files

### `string_interner.ast` (2.4 KB)
String interning implementation for deduplicating identifiers.

**Features**:
- Vec-based pool (no HashMap in Core-0)
- Linear search for lookup (acceptable for bootstrap)
- Intern full strings or substrings

**Functions**:
- `new_interner()` - Create empty interner
- `intern(interner, value)` - Intern a string
- `intern_substring(interner, source, start, end)` - Intern substring
- `interner_count(interner)` - Get pool size
- `interner_clear(interner)` - Clear pool

### `lexer.ast` (21 KB)
Main lexer implementation that tokenizes Aster source code.

**Features**:
- UTF-8 input processing
- Full span tracking (file, line, column)
- All token types (94 variants)
- Escape sequences in strings/chars
- Line and block comments (with nesting)
- Hex (0x) and binary (0b) integer literals
- Float literals with exponents
- Error recovery

**Main Functions**:
- `new_lexer(source, file_name)` - Create lexer
- `tokenize(lexer)` - Tokenize entire source
- `next_token(lexer)` - Get next token

**Tokenization Functions**:
- `lex_identifier_or_keyword(lexer)` - Identifiers and keywords
- `lex_number(lexer)` - Integer and float literals
- `lex_string(lexer)` - String literals with escapes
- `lex_char(lexer)` - Character literals
- `lex_operator_or_punctuation(lexer)` - Operators and punctuation

**Helpers**:
- `skip_whitespace_and_comments(lexer)` - Skip trivia
- `advance(lexer)` - Advance position with line/column tracking
- `keyword_lookup(text)` - Check if identifier is keyword

## Core-0 Workarounds

### No HashMap for Keywords
**Problem**: Can't use `Dictionary<string, TokenKind>` for keyword lookup.

**Solution**: Manual if-chain in `keyword_lookup()`
```rust
fn keyword_lookup(text: &String) -> TokenKind {
    if text == "fn" { return TokenKind::Fn; }
    if text == "let" { return TokenKind::Let; }
    // ... 28 keywords total
    TokenKind::Identifier
}
```

### No String Methods
**Problem**: Can't use `.len()`, `.chars()`, slicing, etc.

**Solution**: Manual helper functions
```rust
fn string_length(s: &String) -> i32 {
    s.len() as i32
}

fn char_at(s: &String, index: i32) -> char {
    let bytes = s.as_bytes();
    bytes[index as usize] as char
}

fn string_substring(s: &String, start: i32, end: i32) -> String {
    // Manual byte iteration
}
```

### No Vec-Based Interner
**Problem**: HashMap requires `Hash` and `Eq` traits.

**Solution**: Linear search through Vec
```rust
fn intern(interner: &mut StringInterner, value: String) -> String {
    let mut i = 0;
    while i < interner.pool.len() {
        if interner.pool[i] == value {
            return interner.pool[i].clone();
        }
        i = i + 1;
    }
    interner.pool.push(value.clone());
    value
}
```

## Example Usage

```rust
let source = String::from("fn main() { let x = 42; }");
let mut lexer = new_lexer(source, String::from("test.ast"));
let tokens = tokenize(&mut lexer);

// tokens contains:
// - Fn keyword
// - Identifier "main"
// - LeftParen
// - RightParen
// - LeftBrace
// - Let keyword
// - Identifier "x"
// - Equals
// - IntegerLiteral "42"
// - Semicolon
// - RightBrace
// - Eof
```

## Supported Tokens

### Literals
- **Identifiers**: `foo`, `bar_baz`, `_private`
- **Integers**: `42`, `0xFF`, `0b1010`, `1_000_000`
- **Floats**: `3.14`, `1e10`, `2.5e-3`
- **Strings**: `"hello"`, `"line\nbreak"`, `"quote\""`
- **Chars**: `'a'`, `'\n'`, `'\''`

### Keywords (28 total)
`fn`, `let`, `mut`, `type`, `trait`, `impl`, `match`, `if`, `else`, `for`, `while`, `return`, `break`, `continue`, `async`, `await`, `actor`, `module`, `pub`, `extern`, `unsafe`, `using`, `managed`, `throws`, `struct`, `enum`, `true`, `false`

### Operators
**Arithmetic**: `+`, `-`, `*`, `/`, `%`  
**Bitwise**: `&`, `|`, `^`, `~`  
**Comparison**: `<`, `>`, `<=`, `>=`, `==`, `!=`  
**Logical**: `&&`, `||`, `!`  
**Assignment**: `=`, `+=`, `-=`, `*=`, `/=`  
**Other**: `->`, `=>`, `::`, `..`, `.`

### Punctuation
`(`, `)`, `{`, `}`, `[`, `]`, `,`, `:`, `;`, `@`, `#`

## Comments

### Line Comments
```rust
// This is a line comment
let x = 42; // Also a line comment
```

### Block Comments (Nestable)
```rust
/* This is a block comment */

/* 
 * Multi-line
 * block comment
 */

/* Nested /* comments */ work too */
```

## Escape Sequences

Supported in strings and character literals:
- `\n` - Newline
- `\r` - Carriage return
- `\t` - Tab
- `\\` - Backslash
- `\"` - Double quote (strings)
- `\'` - Single quote (chars)
- `\0` - Null character

## Error Handling

The lexer detects these errors:
- **E0001**: Unterminated string literal
- **E0002**: Unterminated character literal
- **E0003**: Unexpected character

Errors produce `TokenKind::Error` tokens with error messages.

## Testing

Use test fixtures in `/bootstrap/fixtures/core0/` to test the lexer:

```bash
# Once aster1 is built:
aster1 lex bootstrap/fixtures/core0/compile-pass/simple_struct.ast

# Compare with aster0 (seed compiler):
aster0 lex bootstrap/fixtures/core0/compile-pass/simple_struct.ast
```

## Implementation Notes

### Performance
- **String interning**: Linear search is O(n), acceptable for bootstrap
- **Character access**: Direct byte indexing (O(1))
- **Tokenization**: Single-pass, O(n) where n = source length

### Line/Column Tracking
- Lines start at 1
- Columns start at 1
- Newline advances line and resets column
- Tabs count as single column (no tab expansion)

### Spans
Every token has a span with:
- File name
- Line number (1-indexed)
- Column number (1-indexed)
- Start offset (0-indexed byte position)
- Length (in bytes)

## Future Improvements (Stage 2+)

When Core-1 is available:
- Use HashMap for keyword lookup (O(1) instead of O(n))
- Use HashMap for string interning (O(1) instead of O(n))
- Add trait-based character classification
- Use iterators for character processing
- Add proper Unicode support (currently ASCII-centric)

## References

- **C# Implementation**: `/src/Aster.Compiler/Frontend/Lexer/AsterLexer.cs`
- **Token Definitions**: `/aster/compiler/contracts/token_kind.ast`
- **Span Type**: `/aster/compiler/contracts/span.ast`
- **Test Fixtures**: `/bootstrap/fixtures/core0/`

---

**Status**: Complete and ready for testing  
**Language Subset**: Core-0 (no traits, async, or macros)  
**Lines of Code**: ~600 (lexer) + ~80 (interner)

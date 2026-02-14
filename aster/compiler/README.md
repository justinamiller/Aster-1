# Stage 1 Implementation - Aster Compiler in Core-0

## Overview

This directory contains the **Stage 1 implementation** of the Aster compiler, written in the **Aster Core-0** language subset.

## Status

✅ **Contracts Implemented** - Span, Token, TokenKind defined in Core-0
🚧 **Lexer** - Not yet implemented (next step)
🚧 **Parser** - Not yet implemented (after lexer)

## Implemented Components

### 1. Contracts (`/aster/compiler/contracts/`)

#### `span.ast`
- `Span` struct for source location tracking
- Functions: `new_span`, `unknown_span`, `span_end`, `span_to_string`, `spans_equal`, `span_combine`
- Helper: `i32_to_string` for Core-0 (no trait-based ToString)

#### `token_kind.ast`
- `TokenKind` enum with all Aster token types
- Functions: `is_keyword`, `token_kind_to_string`
- 94 token variants (literals, keywords, operators, punctuation)

#### `token.ast`
- `Token` struct representing lexical tokens
- Functions: `new_token`, `token_is`, `token_is_keyword`, `token_to_string`
- Helpers: `make_eof_token`, `make_error_token`, `token_value`, `token_span`, `token_kind`
- Core-0 limitation: Manual enum equality via `token_kind_to_discriminant`

## Core-0 Language Constraints

The Stage 1 compiler is written using **only** Core-0 features:

✅ **Allowed:**
- Structs, enums, functions
- Primitive types (i32, i64, bool, char, u8, etc.)
- String, Vec, Box
- Control flow: if/else, while, for, match
- Immutable/mutable bindings
- References (&, &mut)

❌ **Not Allowed:**
- Traits and impl blocks
- Generics (limited monomorphic use only)
- Async/await
- Macros
- Closures (initially)
- Rc, Arc, RefCell

## Workarounds for Core-0 Limitations

### No Trait-Based Equality
**Problem**: Can't use `==` on custom types without `PartialEq` trait.

**Solution**: Manual equality functions
```rust
fn spans_equal(a: &Span, b: &Span) -> bool {
    a.file == b.file &&
    a.line == b.line &&
    a.column == b.column &&
    a.start == b.start &&
    a.length == b.length
}
```

### No Trait-Based ToString
**Problem**: Can't use `.to_string()` without `ToString` trait.

**Solution**: Manual conversion functions
```rust
fn i32_to_string(n: i32) -> String {
    // Manual implementation
}

fn span_to_string(span: &Span) -> String {
    // Manual concatenation
}
```

### No Enum Equality
**Problem**: Can't compare enum variants directly without `PartialEq`.

**Solution**: Discriminant-based comparison
```rust
fn token_kind_to_discriminant(kind: TokenKind) -> i32 {
    match kind {
        TokenKind::Identifier => 0,
        TokenKind::IntegerLiteral => 1,
        // ...
    }
}

fn token_kind_equals(a: TokenKind, b: TokenKind) -> bool {
    token_kind_to_discriminant(a) == token_kind_to_discriminant(b)
}
```

## Next Steps

### Immediate (Step 2)
1. **Implement Lexer** (`/aster/compiler/frontend/lexer.ast`)
   - Port `AsterLexer.cs` to Core-0
   - UTF-8 tokenization
   - String interning
   - Span tracking

2. **Implement String Interner** (`/aster/compiler/frontend/string_interner.ast`)
   - Intern identifiers for performance
   - Use Vec and manual hash table (no HashMap in Core-0)

### After Lexer (Step 3)
3. **Create Differential Tests**
   - Generate golden files from aster0 (seed compiler)
   - Compare token streams from aster0 vs aster1
   - Verify equivalence

4. **Implement Parser** (`/aster/compiler/frontend/parser.ast`)
   - Recursive descent parsing
   - AST construction
   - Error recovery

## Testing

Test fixtures are in `/bootstrap/fixtures/core0/`:

### Compile-Pass (should compile successfully)
- `simple_struct.ast` - Basic struct usage
- `basic_enum.ast` - Enum and pattern matching
- `simple_function.ast` - Function calls
- `control_flow.ast` - if/while/for
- `vec_operations.ast` - Vec usage

### Compile-Fail (should fail with specific errors)
- `undefined_variable.ast` - E0001 error
- `type_mismatch.ast` - E0303 error
- `use_of_moved_value.ast` - E0400 error
- `no_traits_in_core0.ast` - E9000 feature gate error

### Run-Pass (should compile and run)
- `hello_world.ast` - Basic print
- `fibonacci.ast` - Recursive functions
- `sum_array.ast` - Vec iteration

## Building Stage 1

Once the lexer and parser are implemented:

```bash
# Compile Stage 1 with aster0 (seed compiler)
./bootstrap/scripts/bootstrap.sh --stage 1

# This will:
# 1. Use aster0 to compile aster/compiler/**/*.ast
# 2. Link with Aster runtime
# 3. Produce aster1 binary
```

## Verification

```bash
# Run differential tests
./bootstrap/scripts/verify.sh --stage 1

# This will:
# 1. Compile fixtures with aster0 and aster1
# 2. Compare token streams (lexer)
# 3. Compare AST dumps (parser)
# 4. Verify outputs match
```

## Design Notes

### Why Manual String Operations?
Core-0 doesn't have trait-based operations like `ToString`, `Display`, or `format!` macros. We manually build strings using:
- `String::new()` and `String::from()`
- `.push_str()` and `.push()`
- Manual iteration and concatenation

### Why Explicit Function Names?
Without traits, we can't use method syntax like `span.to_string()`. Instead, we use standalone functions like `span_to_string(span)`.

### Why No HashMap?
`HashMap` requires `Hash` and `Eq` traits, which aren't available in Core-0. For string interning, we'll use a simple Vec-based approach with linear search (acceptable for small intern tables in bootstrap).

## File Structure

```
aster/compiler/
├── contracts/
│   ├── span.ast           ✅ Complete
│   ├── token_kind.ast     ✅ Complete
│   └── token.ast          ✅ Complete
├── frontend/
│   ├── lexer.ast          🚧 To be implemented
│   └── string_interner.ast 🚧 To be implemented
└── README.md              ✅ This file
```

## References

- C# Implementation: `/src/Aster.Compiler/`
- Core-0 Specification: `/bootstrap/spec/aster-core-subsets.md`
- Bootstrap Guide: `/bootstrap/README.md`
- Stage 1 Specification: `/bootstrap/spec/bootstrap-stages.md`

---

**Status**: Contracts complete, ready for lexer implementation  
**Language Subset**: Core-0 (no traits, async, or macros)  
**Next**: Implement lexer with UTF-8 tokenization

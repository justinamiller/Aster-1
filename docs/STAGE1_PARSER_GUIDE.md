# Stage 1 Parser Implementation Guide

This document provides a detailed roadmap for implementing the Stage 1 parser in Aster.

**Status**: 🚧 In Progress (Infrastructure Phase)  
**Estimated Time**: 3-4 weeks  
**Last Updated**: 2026-02-15

## Overview

The Stage 1 parser implements a recursive descent parser with Pratt expression parsing for the Core-0 language subset. It must be written in Aster itself using only Core-0 features.

## Architecture

```
Tokens → Parser → AST
         ↓
      Diagnostics
```

### Key Components

1. **Parser State** (`struct Parser`)
   - Token stream
   - Current position
   - Node ID counter
   - Diagnostic collector

2. **Recursive Descent Functions**
   - `parse_program()` — Entry point
   - `parse_declaration()` — Top-level items
   - `parse_function()`, `parse_struct()`, `parse_enum()` — Specific declarations
   - `parse_statement()` — Statements
   - `parse_expression()` — Expressions with Pratt parsing

3. **Helper Functions**
   - `peek()`, `advance()`, `expect()` — Token navigation
   - `is_at_end()`, `check()`, `match_token()` — Token checking
   - `synchronize()` — Error recovery

## Implementation Plan

### Phase 1: Parser Infrastructure ✅ COMPLETE (Week 1)

**Goal**: Set up basic parser structure and helpers

**Status**: ✅ Complete (2026-02-15)

#### Tasks
- [x] Define Parser struct with state
- [x] Implement token navigation helpers:
  - [x] `peek()` — Look at current token without consuming
  - [x] `peek_ahead(n)` — Look ahead n tokens
  - [x] `advance()` — Consume and return current token
  - [x] `expect(kind)` — Consume token of expected kind or error
  - [x] `is_at_end()` — Check if at EOF
  - [x] `check(kind)` — Check if current token is of kind
  - [x] `match_tokens(kinds)` — Try to match any of given kinds
  - [x] `previous()` — Get previous token
- [x] Implement error handling:
  - [x] `error(message)` — Report error at current position (via expect)
  - [x] `synchronize()` — Recover from parse errors
- [x] Implement node ID generation: `new_node_id()`
- [ ] Create basic test harness (deferred to integration phase)

**Testing**: Helper functions implemented, ready for use in Phase 2

**Files Modified**:
- `src/aster1/parser.ast` ✅ (18 helper functions added)

---

### Phase 2: Declaration Parsing (Week 1-2)

**Goal**: Parse top-level declarations (functions, structs, enums)

#### 2.1 Function Declarations

Parse function syntax:
```rust
fn function_name(param1: Type1, param2: Type2) -> ReturnType {
    // body
}
```

**Implementation**:
- [ ] `parse_function()` function
  - [ ] Parse `fn` keyword
  - [ ] Parse function name (identifier)
  - [ ] Parse parameter list `(param: Type, ...)`
  - [ ] Parse return type `-> Type` (optional, defaults to `()`)
  - [ ] Parse function body (block expression)
- [ ] Helper: `parse_parameter()` for function parameters
- [ ] Helper: `parse_parameter_list()` for parameter list

**Testing**:
```rust
// Test fixtures
fn simple() {}
fn with_params(x: i32, y: i32) {}
fn with_return() -> i32 { 0 }
fn complete(x: i32) -> bool { true }
```

#### 2.2 Struct Declarations

Parse struct syntax:
```rust
struct StructName {
    field1: Type1,
    field2: Type2
}
```

**Implementation**:
- [ ] `parse_struct()` function
  - [ ] Parse `struct` keyword
  - [ ] Parse struct name
  - [ ] Parse `{`
  - [ ] Parse field list
  - [ ] Parse `}`
- [ ] Helper: `parse_field()` for struct fields
- [ ] Helper: `parse_field_list()` for multiple fields

**Testing**:
```rust
struct Empty {}
struct Point { x: i32, y: i32 }
struct Token { kind: TokenKind, text: String }
```

#### 2.3 Enum Declarations

Parse enum syntax:
```rust
enum EnumName {
    Variant1,
    Variant2(Type),
    Variant3 { field: Type }
}
```

**Implementation**:
- [ ] `parse_enum()` function
  - [ ] Parse `enum` keyword
  - [ ] Parse enum name
  - [ ] Parse `{`
  - [ ] Parse variant list
  - [ ] Parse `}`
- [ ] Helper: `parse_variant()` for enum variants
- [ ] Helper: `parse_variant_list()` for multiple variants

**Testing**:
```rust
enum Simple { A, B, C }
enum WithData { Some(i32), None }
enum TokenKind { Identifier, Number, String }
```

#### 2.4 Top-Level Program

**Implementation**:
- [ ] `parse_program()` function
  - [ ] Loop until EOF
  - [ ] Call `parse_declaration()`
  - [ ] Collect all declarations
  - [ ] Handle errors with synchronization
- [ ] `parse_declaration()` dispatcher
  - [ ] Check current token
  - [ ] Route to appropriate parser

**Testing**:
```rust
// Complete program
fn main() {
    print("Hello")
}

struct Point { x: i32, y: i32 }

enum Color { Red, Green, Blue }
```

**Files Modified**:
- `src/aster1/parser.ast`

**Validation**:
- Parse all Core-0 declaration fixtures
- Verify AST structure matches expectations
- Check error messages for invalid syntax

---

### Phase 3: Type Parsing (Week 2)

**Goal**: Parse type annotations

#### Supported Types in Core-0
- Primitive: `i32`, `i64`, `bool`, `String`
- Named: `Vec`, `Option`, custom types
- Unit: `()`

**Implementation**:
- [ ] `parse_type()` function
  - [ ] Handle `()` for unit type
  - [ ] Handle identifiers for named types
  - [ ] Handle generic types `Vec<T>` (limited support)
- [ ] Helper: `parse_type_args()` for generics

**Testing**:
```rust
fn test() -> i32 {}
fn test2() -> Vec<i32> {}
fn test3() -> () {}
```

**Files Modified**:
- `src/aster1/parser.ast`

---

### Phase 4: Expression Parsing (Week 2-3)

**Goal**: Implement Pratt parser for expressions

#### 4.1 Primary Expressions

**Implementation**:
- [ ] `parse_primary()` function
  - [ ] Literals: integers, booleans, strings
  - [ ] Identifiers: variables
  - [ ] Grouped: `( expr )`
  - [ ] Block: `{ ... }`
  - [ ] If: `if cond { ... }`
  - [ ] While: `while cond { ... }`
  - [ ] Match: `match expr { ... }`

**Testing**:
```rust
42
true
"hello"
x
(1 + 2)
{ let x = 5; x }
```

#### 4.2 Binary Operators

**Implementation**:
- [ ] `parse_expression()` — Entry point
- [ ] `parse_expr_bp(min_bp)` — Pratt parser with binding power
- [ ] Operator precedence table:
  - Assignment: `=` (bp: 1)
  - Logical OR: `||` (bp: 3)
  - Logical AND: `&&` (bp: 5)
  - Equality: `==`, `!=` (bp: 7)
  - Comparison: `<`, `>`, `<=`, `>=` (bp: 9)
  - Addition: `+`, `-` (bp: 11)
  - Multiplication: `*`, `/`, `%` (bp: 13)
  - Unary: `!`, `-` (bp: 15)
  - Call/Index: `()`, `[]` (bp: 17)

**Testing**:
```rust
1 + 2
x * y + z
a == b && c != d
!flag || other
```

#### 4.3 Control Flow

**If Expressions**:
- [ ] `parse_if()` function
  - [ ] Parse `if` keyword
  - [ ] Parse condition
  - [ ] Parse then-branch (block)
  - [ ] Parse optional `else` branch

**While Loops**:
- [ ] `parse_while()` function
  - [ ] Parse `while` keyword
  - [ ] Parse condition
  - [ ] Parse body (block)

**Loop Loops**:
- [ ] `parse_loop()` function
  - [ ] Parse `loop` keyword
  - [ ] Parse body (block)

**Testing**:
```rust
if x > 0 { print("positive") }
if x > 0 { print("pos") } else { print("neg") }
while i < 10 { i = i + 1 }
loop { break }
```

#### 4.4 Function Calls

**Implementation**:
- [ ] `parse_call()` function
  - [ ] Parse function name
  - [ ] Parse `(`
  - [ ] Parse argument list
  - [ ] Parse `)`
- [ ] Helper: `parse_arguments()`

**Testing**:
```rust
print("hello")
max(x, y)
vec.push(item)
```

**Files Modified**:
- `src/aster1/parser.ast`

---

### Phase 5: Statement Parsing (Week 3)

**Goal**: Parse statements and blocks

#### 5.1 Let Statements

**Implementation**:
- [ ] `parse_let_stmt()` function
  - [ ] Parse `let` keyword
  - [ ] Parse pattern (identifier for now)
  - [ ] Parse optional type annotation `: Type`
  - [ ] Parse `=`
  - [ ] Parse initializer expression
  - [ ] Parse `;`

**Testing**:
```rust
let x = 5;
let y: i32 = 10;
let mut z = 20;
```

#### 5.2 Expression Statements

**Implementation**:
- [ ] `parse_expr_stmt()` function
  - [ ] Parse expression
  - [ ] Parse optional `;`

**Testing**:
```rust
print("hello");
x = y + z;
calculate();
```

#### 5.3 Block Expressions

**Implementation**:
- [ ] `parse_block()` function
  - [ ] Parse `{`
  - [ ] Parse statements in sequence
  - [ ] Last expression is the value (no semicolon)
  - [ ] Parse `}`

**Testing**:
```rust
{
    let x = 5;
    let y = 10;
    x + y
}
```

#### 5.4 Return/Break/Continue

**Implementation**:
- [ ] `parse_return()` function
- [ ] `parse_break()` function
- [ ] `parse_continue()` function

**Testing**:
```rust
return 42;
break;
continue;
```

**Files Modified**:
- `src/aster1/parser.ast`

---

### Phase 6: Pattern Matching (Week 3)

**Goal**: Parse match expressions and patterns

#### 6.1 Match Expressions

**Implementation**:
- [ ] `parse_match()` function
  - [ ] Parse `match` keyword
  - [ ] Parse scrutinee expression
  - [ ] Parse `{`
  - [ ] Parse match arms
  - [ ] Parse `}`
- [ ] Helper: `parse_match_arm()`

**Testing**:
```rust
match x {
    0 => print("zero"),
    1 => print("one"),
    _ => print("other")
}
```

#### 6.2 Patterns

**Implementation**:
- [ ] `parse_pattern()` function
  - [ ] Literal patterns: `0`, `true`, `"hello"`
  - [ ] Variable patterns: `x`, `_`
  - [ ] Enum patterns: `Some(x)`, `None`

**Testing**:
```rust
match option {
    Some(value) => value,
    None => 0
}
```

**Files Modified**:
- `src/aster1/parser.ast`

---

### Phase 7: Integration (Week 4)

**Goal**: Create complete working parser

#### 7.1 Main Entry Point

**Implementation**:
- [ ] Create `src/aster1/main.ast`
- [ ] Implement CLI argument parsing
- [ ] Integrate lexer + parser
- [ ] Implement `emit-tokens` command
- [ ] Implement `emit-ast` command

**Files Created**:
- `src/aster1/main.ast`

#### 7.2 Driver Integration

**Implementation**:
- [ ] Update `src/aster1/driver.ast`
- [ ] Connect lexer → parser → ast
- [ ] Add error reporting

**Files Modified**:
- `src/aster1/driver.ast`

#### 7.3 Testing

**Validation**:
- [ ] Run differential tests
  - [ ] `./bootstrap/scripts/diff-test-tokens.sh`
  - [ ] `./bootstrap/scripts/diff-test-ast.sh`
- [ ] Verify against Core-0 fixtures:
  - [ ] `bootstrap/fixtures/core0/compile-pass/*.ast`
  - [ ] `bootstrap/fixtures/core0/run-pass/*.ast`
- [ ] Ensure error messages match expectations
- [ ] Verify AST structure matches aster0

---

## C# Parser Reference

Key files to reference:
- `src/Aster.Compiler/Frontend/Parser/AsterParser.cs` (1158 lines)
- Look at methods:
  - `ParseProgram()` — Entry point
  - `ParseDeclaration()` — Top-level dispatcher
  - `ParseFunctionDecl()` — Function parsing
  - `ParseStructDecl()` — Struct parsing
  - `ParseEnumDecl()` — Enum parsing
  - `ParseExpression()` — Expression entry
  - `ParsePrimaryExpression()` — Primaries
  - `ParseBinaryExpression()` — Pratt parsing

## Core-0 Restrictions

**Allowed in Core-0**:
- ✅ Structs (no methods)
- ✅ Enums (simple variants)
- ✅ Functions (standalone only)
- ✅ Primitive types: `i32`, `i64`, `bool`, `String`
- ✅ `Vec<T>` and `String` (built-in)
- ✅ Control flow: `if`, `while`, `loop`, `match`
- ✅ Basic expressions and operators

**NOT Allowed in Core-0**:
- ❌ Traits
- ❌ Impl blocks (methods)
- ❌ Generics (except Vec, String)
- ❌ Closures
- ❌ Async/await
- ❌ Complex pattern matching

## Testing Strategy

### Unit Tests
- Test each parsing function individually
- Use small fixtures for each language construct
- Verify error messages

### Integration Tests
- Parse complete Core-0 programs
- Verify AST structure
- Compare with aster0 output (differential testing)

### Differential Testing
Once aster1 can parse:
```bash
# Generate golden files with aster0
./bootstrap/scripts/generate-goldens.sh

# Test aster1 against goldens
./bootstrap/scripts/diff-test-ast.sh
```

## Success Criteria

- [ ] aster1 can parse all 12 Core-0 fixtures
- [ ] AST output matches aster0 exactly (differential tests pass)
- [ ] Helpful error messages for invalid syntax
- [ ] No crashes on malformed input
- [ ] Can parse its own source code

## Common Pitfalls

1. **Infinite Loops**: Always advance() in loops
2. **Left Recursion**: Use iterative parsing for left-recursive rules
3. **Error Recovery**: Implement synchronization to continue after errors
4. **Ownership**: Be careful with mutable parser state
5. **Token Lookahead**: Only peek, don't modify position

## Resources

- [Pratt Parsing Made Easy](https://matklad.github.io/2020/04/13/simple-but-powerful-pratt-parsing.html)
- Crafting Interpreters - Parsing chapter
- Core-0 Specification: `bootstrap/spec/aster-core-subsets.md`
- Fixtures: `bootstrap/fixtures/core0/`

---

**Current Status**: Infrastructure phase started
**Next Session**: Complete helper functions and test with one fixture
**Blocker**: None

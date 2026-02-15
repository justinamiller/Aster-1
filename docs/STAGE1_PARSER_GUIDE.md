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

### Phase 2: Declaration Parsing ✅ COMPLETE (Week 1-2)

**Goal**: Parse top-level declarations (functions, structs, enums)

**Status**: ✅ Complete (2026-02-15)

#### 2.1 Function Declarations ✅

**Implementation**: COMPLETE
- [x] `parse_function()` function
  - [x] Parse `fn` keyword
  - [x] Parse function name (identifier)
  - [x] Parse parameter list `(param: Type, ...)`
  - [x] Parse return type `-> Type` (optional, defaults to `()`)
  - [x] Parse function body (block expression)
- [x] Helper: `parse_parameter_list()` for parameter list

#### 2.2 Struct Declarations ✅

**Implementation**: COMPLETE
- [x] `parse_struct()` function
  - [x] Parse `struct` keyword
  - [x] Parse struct name
  - [x] Parse `{`
  - [x] Parse field list
  - [x] Parse `}`
- [x] Helper: `parse_field_list()` for multiple fields

#### 2.3 Enum Declarations ✅

**Implementation**: COMPLETE
- [x] `parse_enum()` function
  - [x] Parse `enum` keyword
  - [x] Parse enum name
  - [x] Parse `{`
  - [x] Parse variant list
  - [x] Parse `}`
- [x] Helper: `parse_variant_list()` for multiple variants

#### 2.4 Top-Level Program ✅

**Implementation**: COMPLETE
- [x] `parse_program()` function
  - [x] Loop until EOF
  - [x] Call `parse_declaration()`
  - [x] Collect all declarations
  - [x] Handle errors with synchronization
- [x] `parse_declaration()` dispatcher
  - [x] Check current token
  - [x] Route to appropriate parser

**Files Modified**:
- `src/aster1/parser.ast` ✅
- `src/aster1/ast.ast` ✅ (Added ProgramNode)

---

### Phase 3: Type Parsing ✅ COMPLETE (Week 2)

**Goal**: Parse type annotations

**Status**: ✅ Complete (2026-02-15)

#### Supported Types in Core-0
- Primitive: `i32`, `i64`, `bool`, `String` ✅
- Named: `Vec`, `Option`, custom types ✅
- Unit: `()` ✅
- Paths: `Option::Some` ✅
- Generics: `Vec<T>` ✅ (simplified)

**Implementation**: COMPLETE
- [x] `parse_type()` function
  - [x] Handle `()` for unit type
  - [x] Handle identifiers for named types
  - [x] Handle generic types `Vec<T>` (limited support)
  - [x] Handle path types `Option::Some`

**Files Modified**:
- `src/aster1/parser.ast` ✅

---

### Phase 4: Expression Parsing ✅ COMPLETE (Week 2-3)

**Goal**: Implement Pratt parser for expressions

**Status**: ✅ Complete (2026-02-15)

#### 4.1 Primary Expressions ✅

**Implementation**: COMPLETE
- [x] `parse_primary()` function
  - [x] Literals: integers, booleans, strings, floats
  - [x] Identifiers: variables
  - [x] Function calls
  - [x] Grouped: `( expr )`
  - [x] Block: `{ ... }`
  - [x] If: `if cond { ... }`
  - [x] While: `while cond { ... }`
  - [x] Loop: `loop { ... }`
  - [x] Match: `match expr { ... }`

#### 4.2 Binary Operators ✅

**Implementation**: COMPLETE
- [x] `parse_expression()` — Entry point
- [x] `parse_expr_bp(min_bp)` — Pratt parser with binding power
- [x] Operator precedence table:
  - Assignment: `=` (bp: 2)
  - Logical OR: `||` (bp: 4)
  - Logical AND: `&&` (bp: 6)
  - Equality: `==`, `!=` (bp: 8)
  - Comparison: `<`, `>`, `<=`, `>=` (bp: 10)
  - Addition: `+`, `-` (bp: 12)
  - Multiplication: `*`, `/`, `%` (bp: 14)
  - Unary: `!`, `-` (bp: 16)
- [x] `parse_prefix_expr()` — Unary operators
- [x] `parse_infix_expr()` — Binary operators
- [x] `token_kind_to_binary_op()` — Operator mapping

#### 4.3 Control Flow ✅

**If Expressions**: COMPLETE
- [x] `parse_if()` function
  - [x] Parse `if` keyword
  - [x] Parse condition
  - [x] Parse then-branch (block)
  - [x] Parse optional `else` branch
  - [x] Handle `else if` chains

**While Loops**: COMPLETE
- [x] `parse_while()` function
  - [x] Parse `while` keyword
  - [x] Parse condition
  - [x] Parse body (block)

**Loop Loops**: COMPLETE
- [x] `parse_loop()` function
  - [x] Parse `loop` keyword
  - [x] Parse body (block)

#### 4.4 Function Calls ✅

**Implementation**: COMPLETE
- [x] Function call parsing in `parse_primary()`
  - [x] Parse function name
  - [x] Parse `(`
  - [x] Parse argument list
  - [x] Parse `)`
- [x] Helper: `parse_argument_list()`

**Files Modified**:
- `src/aster1/parser.ast` ✅
- `aster/compiler/contracts/token_kind.ast` ✅ (Added Loop, Underscore)

---

### Phase 5: Statement Parsing 🚧 PARTIAL (Week 3)
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

### Phase 5: Statement Parsing 🚧 PARTIAL (Week 3)

**Goal**: Parse statements and blocks

**Status**: 🚧 Partially Complete (2026-02-15)

#### 5.1 Let Statements ✅

**Implementation**: COMPLETE
- [x] `parse_let_statement()` function
  - [x] Parse `let` keyword
  - [x] Parse pattern (identifier for now)
  - [x] Parse optional `mut` modifier
  - [x] Parse optional type annotation `: Type`
  - [x] Parse `=`
  - [x] Parse initializer expression
  - [x] Parse `;`

#### 5.2 Expression Statements ✅

**Implementation**: COMPLETE
- [x] `parse_statement()` function
  - [x] Parse expression
  - [x] Parse optional `;`
  - [x] Dispatch to let/return/break/continue

#### 5.3 Block Expressions ✅

**Implementation**: COMPLETE
- [x] `parse_block()` function
  - [x] Parse `{`
  - [x] Parse statements in sequence
  - [x] Last expression is the value (no semicolon)
  - [x] Parse `}`

#### 5.4 Return/Break/Continue ✅

**Implementation**: COMPLETE
- [x] `parse_return_statement()` function
- [x] Break parsing in `parse_statement()`
- [x] Continue parsing in `parse_statement()`

**Files Modified**:
- `src/aster1/parser.ast` ✅

---

### Phase 6: Pattern Matching ✅ COMPLETE (Week 3)

**Goal**: Parse match expressions and patterns

**Status**: ✅ Complete (2026-02-15)

#### 6.1 Match Expressions ✅

**Implementation**: COMPLETE
- [x] `parse_match()` function
  - [x] Parse `match` keyword
  - [x] Parse scrutinee expression
  - [x] Parse `{`
  - [x] Parse match arms
  - [x] Parse `}`
- [x] Helper: `parse_match_arms()`

#### 6.2 Patterns ✅

**Implementation**: COMPLETE
- [x] `parse_pattern()` function
  - [x] Literal patterns: `0`, `true`, `"hello"`
  - [x] Variable patterns: `x`, `_`
  - [x] Enum patterns: `Some(x)`, `None`
  - [x] Path patterns: `Option::Some(x)`

**Files Modified**:
- `src/aster1/parser.ast` ✅

---

### Phase 7: Integration (Week 4)

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

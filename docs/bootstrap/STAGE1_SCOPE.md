# Stage1 Scope Definition

## Overview

This document defines the **minimum complete subset** of the ASTER programming language required for Stage1 self-hosting. Stage1 is the first milestone where:

1. **aster0** (C# compiler) can compile **aster1** (written in ASTER)
2. **aster1** can compile the same sources and produce identical IR/bytecode
3. Stage1 is feature-frozen to this exact subset

## Stage1 Language Subset: Core-0

Stage1 implements **Aster Core-0**, a carefully designed minimal subset that is:
- **Sufficient** to write a complete lexer, parser, AST builder, and basic IR
- **Simple** enough to implement and verify in the C# seed compiler
- **Deterministic** with stable, reproducible output

---

## Grammar Subset

### Module Structure
```rust
// File-level imports
use std::fmt::*;
use std::collections::Vec;

// Top-level items only
struct MyStruct { /* ... */ }
enum MyEnum { /* ... */ }
fn my_function() { /* ... */ }
```

**Allowed at module level:**
- `use` declarations (simple paths only)
- `struct` definitions
- `enum` definitions
- `fn` definitions

**NOT allowed:**
- `trait` definitions ❌
- `impl` blocks ❌
- `const` items ❌
- `static` items ❌
- `mod` declarations (modules inline only) ❌
- Macros ❌

### Struct Definitions
```rust
struct Point {
    x: i32,
    y: i32
}

struct Token {
    kind: TokenKind,
    span: Span,
    text: String
}
```

**Rules:**
- Named fields only
- No tuple structs
- No unit structs
- No generics in Stage1
- No visibility modifiers (all public)

### Enum Definitions
```rust
enum TokenKind {
    Identifier,
    Number,
    String,
    Keyword,
    Operator
}

enum Option<T> {
    Some(T),
    None
}

enum Result<T, E> {
    Ok(T),
    Err(E)
}
```

**Rules:**
- Simple variant names (no data) ✅
- Variants with single unnamed field ✅ (e.g., `Some(T)`)
- Variants with multiple fields ❌
- Generic enums allowed (limited to 1-2 type parameters)

### Function Definitions
```rust
fn tokenize(source: String) -> Vec<Token> {
    let mut tokens = Vec::new();
    // ...
    tokens
}

fn factorial(n: i32) -> i32 {
    if n <= 1 {
        1
    } else {
        n * factorial(n - 1)
    }
}
```

**Rules:**
- Parameters with explicit types
- Return type required (or `-> ()` implied)
- Generic functions ❌ (Stage2)
- No default parameters
- No varargs

---

## Supported Types

### Primitive Types
✅ **Integers**: `i8`, `i16`, `i32`, `i64`, `isize`  
✅ **Unsigned**: `u8`, `u16`, `u32`, `u64`, `usize`  
✅ **Floats**: `f32`, `f64`  
✅ **Boolean**: `bool`  
✅ **Character**: `char`  
✅ **String**: `String` (owned string)  
✅ **String slice**: `str` (in literals only)  

### Compound Types
✅ **Structs**: Named field structures  
✅ **Enums**: Sum types with simple variants  
✅ **Tuples**: `(i32, i32)`, `(String, bool, i32)`  
❌ **Arrays**: Fixed-size `[T; N]` (deferred to Stage2)  

### Smart Pointers (Standard Library)
✅ **Vec<T>**: Dynamic array (`Vec::new()`, `.push()`, `.len()`)  
✅ **String**: Owned string (`String::new()`, string literals)  
✅ **Box<T>**: Heap allocation (`Box::new(value)`)  
✅ **Option<T>**: Optional values (`Some(x)`, `None`)  
✅ **Result<T, E>**: Error handling (`Ok(x)`, `Err(e)`)  

❌ **NOT supported in Stage1**:
- `&T`, `&mut T` (references) → use value semantics instead
- `Rc<T>`, `Arc<T>` (reference counting)
- `RefCell<T>`, `Mutex<T>` (interior mutability)
- Raw pointers `*const T`, `*mut T`

### Type Annotations
```rust
let x: i32 = 42;
let name: String = "Alice";
let tokens: Vec<Token> = Vec::new();
```

**Rules:**
- Explicit type annotations recommended
- Type inference supported for locals
- Function parameters require types
- Return types required

---

## Supported Expressions

### Literals
✅ Integer: `42`, `0x2A`, `0o52`, `0b101010`  
✅ Float: `3.14`, `2.5e10`  
✅ Boolean: `true`, `false`  
✅ Character: `'a'`, `'\n'`, `'\t'`  
✅ String: `"hello"`, `"line\nbreak"`  

### Arithmetic & Logic
✅ Arithmetic: `+`, `-`, `*`, `/`, `%`  
✅ Comparison: `==`, `!=`, `<`, `>`, `<=`, `>=`  
✅ Logical: `&&`, `||`, `!`  
✅ Bitwise: `&`, `|`, `^`, `<<`, `>>` (integers only)  

### Variable Operations
✅ Binding: `let x = 10;`, `let mut y = 20;`  
✅ Assignment: `x = 5;`, `y = y + 1;`  
✅ Compound assignment: `x += 1;`, `y *= 2;`  

### Struct Operations
✅ Construction: `Point { x: 10, y: 20 }`  
✅ Field access: `point.x`, `token.span.line`  
✅ Field update: `point.x = 5;`  

### Function Calls
✅ Regular calls: `factorial(5)`  
✅ Method-style: `tokens.push(token)` (if supported)  
✅ Path calls: `Vec::new()`, `Option::Some(42)`  

### Path Expressions
✅ Simple paths: `std::fmt::println`  
✅ Type paths: `Vec::new()`, `String::from()`  
✅ Enum variants: `Option::Some`, `Result::Ok`  

---

## Supported Control Flow

### Conditionals
```rust
if condition {
    // ...
} else if other_condition {
    // ...
} else {
    // ...
}

// Expression form
let max = if a > b { a } else { b };
```

### Loops
```rust
// while loop
while i < 10 {
    i += 1;
}

// infinite loop
loop {
    if done { break; }
}

// for loop (simplified - over ranges)
for i in 0..10 {
    // ...
}
```

**NOT supported:**
- `for` over iterators (complex) ❌
- Range patterns ❌

### Pattern Matching
```rust
match token_kind {
    TokenKind::Identifier => { /* ... */ },
    TokenKind::Number => { /* ... */ },
    _ => { /* ... */ }
}

match option {
    Option::Some(value) => { /* use value */ },
    Option::None => { /* ... */ }
}
```

**Rules:**
- Match on enums ✅
- Literal patterns ✅
- Variable binding in patterns ✅
- Wildcard `_` ✅
- Match guards ❌
- Complex nested patterns ❌

### Early Returns
✅ `return expr;`  
✅ `return;` (for void functions)  
✅ `break;`, `continue;` in loops  

---

## Unsupported Features (Explicit List)

These features are **explicitly forbidden** in Stage1 and will produce **compilation errors** when `--stage1` mode is active:

### Type System
❌ **Generics** (function or struct generics beyond stdlib)  
❌ **Traits** (trait definitions, impl blocks)  
❌ **Associated types**  
❌ **Type aliases** (`type MyInt = i32;`)  
❌ **References** (`&T`, `&mut T`)  
❌ **Lifetimes** (`'a`, `'static`)  

### Advanced Features
❌ **Async/await** (`async fn`, `.await`)  
❌ **Closures** (`|x| x + 1`)  
❌ **Macros** (both declarative and procedural)  
❌ **Attributes** (except built-in `#[allow(...)]`)  
❌ **Unsafe code** (`unsafe { }`)  

### Pattern Matching
❌ **Match guards** (`Some(x) if x > 0 => ...`)  
❌ **Range patterns** (`0..=100 => ...`)  
❌ **Slice patterns** (`[a, b, .., c] => ...`)  

### Operators
❌ **Index operator** (`array[0]`) - Use `.get()` method instead  
❌ **Range operators** (`..`, `..=`, `a..b`)  
❌ **Type casts** (`x as u32`) - Explicit conversion functions only  
❌ **Method call syntax** (limited support, use functions instead)  

### Advanced Control Flow
❌ **Loop labels** (`'outer: loop { ... }`)  
❌ **Try operator** (`?` for error propagation)  

### Module System
❌ **Nested modules** (`mod foo { }`)  
❌ **Visibility modifiers** (`pub`, `pub(crate)`)  
❌ **Re-exports** (`pub use ...`)  

### Other
❌ **Constants** (`const PI: f64 = 3.14;`)  
❌ **Statics** (`static COUNTER: i32 = 0;`)  
❌ **Foreign function interface** (`extern "C" { }`)  
❌ **Inline assembly** (`asm!()`)  

---

## Standard Library Subset

### Available Modules
- `std::fmt` - Basic formatting and printing
- `std::collections::Vec` - Dynamic arrays
- `std::string::String` - Owned strings
- `std::option::Option` - Optional values
- `std::result::Result` - Error handling

### Available Functions/Methods

**Vec<T>**:
- `Vec::new() -> Vec<T>`
- `.push(item: T)`
- `.len() -> usize`
- `.get(index: usize) -> Option<T>` (if implemented)

**String**:
- `String::new() -> String`
- String literals: `"text"`

**Option<T>**:
- `Option::Some(value: T)`
- `Option::None`

**Result<T, E>**:
- `Result::Ok(value: T)`
- `Result::Err(error: E)`

---

## Memory Model (Stage1)

### Ownership (Simplified)
- **Value semantics**: Variables own their data
- **Move semantics**: Assignment transfers ownership
- **No borrowing**: Pass by value only (no `&T`, `&mut T`)
- **Heap allocation**: Explicit via `Box::new()` only

### Example
```rust
fn process_token(token: Token) -> Token {
    // token is moved in, moved out
    // Caller loses ownership
    token
}

fn main() {
    let t = Token { /* ... */ };
    let result = process_token(t);
    // t is no longer valid here
}
```

**Rationale**: Avoiding references simplifies the compiler significantly. For Stage1, copying structs or using explicit `Box<T>` is acceptable.

---

## Determinism Requirements

All Stage1 implementations **must** guarantee:

1. **Stable Hashing**: Use deterministic hashing (e.g., FNV, identity-based)
2. **Stable Ordering**: No iteration over hash maps; use sorted maps or stable IDs
3. **Stable IDs**: Symbol IDs, Node IDs, Type IDs assigned in source order
4. **No Timestamps**: No timestamps in output
5. **Path Normalization**: Canonicalize paths for reproducibility

### Example: String Interner
```rust
struct StringInterner {
    strings: Vec<String>,
    // Map from string to index (stable ordering)
}

fn intern(interner: &mut StringInterner, s: String) -> u32 {
    // Returns stable ID based on insertion order
    // ...
}
```

---

## Feature Gate

Code using Stage1 can be compiled with:
```bash
aster build --stage1 my_code.ast
```

This flag:
- Enforces Core-0 subset only
- Errors on unsupported syntax with clear messages
- Ensures compiled code is Stage1-compatible

---

## Verification Criteria

A Stage1 implementation is **complete** when:

1. ✅ aster0 (C# compiler) compiles aster1 source → `aster1` binary
2. ✅ aster0 compiles test program `test.ast` → `test0.ll` (LLVM IR)
3. ✅ aster1 compiles same `test.ast` → `test1.ll` (LLVM IR)
4. ✅ Normalized `test0.ll` and `test1.ll` are **semantically identical**
5. ✅ aster1 compiles its own source → `aster1'` binary
6. ✅ aster1 and aster1' produce identical IR for the same input
7. ✅ All tests pass deterministically in CI

---

## Implementation Checklist

### Compiler Components (aster1)
- [ ] Lexer (tokenization, span tracking, string interning)
- [ ] Parser (recursive descent, AST construction, error recovery)
- [ ] AST (immutable syntax tree nodes)
- [ ] Symbol Table (name resolution, scope tracking)
- [ ] Type Checker (basic type inference, unification)
- [ ] IR Builder (intermediate representation generation)
- [ ] Code Generator (LLVM IR emission)
- [ ] Driver (command-line interface, compilation pipeline)

### Supporting Infrastructure
- [ ] String interner (deterministic)
- [ ] Diagnostic system (span-based error reporting)
- [ ] ID generators (Symbol, Node, Type)

### Testing
- [ ] Lexer tests (tokenization correctness)
- [ ] Parser tests (AST construction)
- [ ] Type checker tests (valid/invalid programs)
- [ ] Differential tests (aster0 vs aster1)
- [ ] Self-hosting test (aster1 compiles itself)

---

## Summary

**Stage1** is the **minimal viable self-hosted compiler** using Core-0 subset:

| Feature Category | Support Level |
|-----------------|---------------|
| Primitives | ✅ Full (i32, bool, String, etc.) |
| Structs | ✅ Full (named fields) |
| Enums | ✅ Limited (simple variants) |
| Functions | ✅ Full (no generics) |
| Control Flow | ✅ Full (if, while, loop, match) |
| Expressions | ✅ Most (no indexing, casts) |
| Stdlib | ✅ Vec, String, Option, Result |
| Generics | ❌ Deferred to Stage2 |
| Traits | ❌ Deferred to Stage2 |
| References | ❌ Deferred to Stage2 |
| Advanced | ❌ Deferred to Stage3 |

This subset is **proven sufficient** to implement a complete compiler (lexer through codegen) while remaining **simple enough** to verify and bootstrap.

---

**Last Updated**: 2026-02-15  
**Status**: Stage1 Scope Frozen  
**Version**: 1.0

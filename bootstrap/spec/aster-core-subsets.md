# Aster Core Subsets Specification

## Overview

This document defines the three progressive language subsets used during the bootstrap process. Each subset is carefully designed to be powerful enough to implement the next stage while remaining simple enough to implement in the previous stage.

## Design Principles

1. **Monotonic Growth**: Core-0 ⊂ Core-1 ⊂ Core-2 (each subset includes all features of previous subsets)
2. **Self-Sufficiency**: Each subset can express a compiler for that subset
3. **Verifiable Boundaries**: Feature gates prevent accidental use of forbidden features
4. **Testable**: Each subset has conformance tests
5. **Pragmatic**: Balance between purity and practicality

---

## Aster Core-0: Minimal Compiler Subset

**Purpose**: Sufficient to write lexer, parser, and basic AST/HIR structures.

**Target**: Stage 1 compiler (lexer, parser, basic IR)

**Philosophy**: Imperative programming with structures and enums. No advanced features.

### Supported Features

#### Data Types
- ✅ **Primitive Types**
  - `i8`, `i16`, `i32`, `i64`, `isize`
  - `u8`, `u16`, `u32`, `u64`, `usize`
  - `f32`, `f64`
  - `bool`
  - `char`
  - `str` (string slice)
  
- ✅ **Compound Types**
  - **Structs**: Named field structures
    ```rust
    struct Token {
        kind: TokenKind,
        span: Span,
        text: String
    }
    ```
  - **Enums**: Sum types with variants (no associated data initially, then simple variants)
    ```rust
    enum TokenKind {
        Identifier,
        Number,
        String,
        Keyword
    }
    
    // Simple associated data allowed
    enum Option<T> {
        Some(T),
        None
    }
    ```
  - **Tuples**: Fixed-size heterogeneous collections
    ```rust
    let point: (i32, i32) = (10, 20);
    ```
  - **Arrays**: Fixed-size homogeneous collections
    ```rust
    let buffer: [u8; 1024] = [0; 1024];
    ```
  - **Slices**: Dynamically-sized views
    ```rust
    let slice: &[u8] = &buffer[0..10];
    ```

- ✅ **Smart Pointers** (Minimal)
  - `Box<T>`: Heap allocation
  - `Vec<T>`: Dynamic array
  - `String`: Owned string
  - **NO** `Rc`, `Arc`, `RefCell` (deferred to Core-1)

#### Control Flow
- ✅ **Conditionals**
  - `if`/`else if`/`else`
  - `if let` (basic pattern matching)
  
- ✅ **Loops**
  - `while` loops
  - `for` loops (over iterators)
  - `loop` (infinite loop with `break`/`continue`)
  
- ✅ **Pattern Matching** (Restricted)
  - `match` on enums (simple patterns only)
  - Literal patterns
  - Variable binding
  - Wildcard `_`
  - **NO** guards, ranges, or complex nested patterns

#### Functions
- ✅ **Function Definitions**
  ```rust
  fn lex(source: &str) -> Vec<Token> {
      // ...
  }
  ```
- ✅ **Function Calls**
- ✅ **Return Values**
- ✅ **Early Return**: `return`
- ✅ **Generic Functions** (Monomorphic only initially)
  ```rust
  fn create_box<T>(value: T) -> Box<T> {
      Box::new(value)
  }
  ```

#### Expressions
- ✅ **Literals**: integers, floats, strings, chars, booleans
- ✅ **Arithmetic**: `+`, `-`, `*`, `/`, `%`
- ✅ **Comparison**: `==`, `!=`, `<`, `>`, `<=`, `>=`
- ✅ **Logical**: `&&`, `||`, `!`
- ✅ **Bitwise**: `&`, `|`, `^`, `<<`, `>>`
- ✅ **Assignment**: `=`
- ✅ **Field Access**: `struct.field`
- ✅ **Array/Slice Indexing**: `array[index]`
- ✅ **Method Calls**: `vec.push(item)` (for built-in types)
- ✅ **Function Calls**: `foo(arg1, arg2)`

#### Variable Bindings
- ✅ **Immutable Bindings**: `let x = 10;`
- ✅ **Mutable Bindings**: `let mut x = 10;`
- ✅ **Type Annotations**: `let x: i32 = 10;`
- ✅ **Destructuring** (Simple)
  ```rust
  let (a, b) = (1, 2);
  let Point { x, y } = point;
  ```

#### Modules and Visibility
- ✅ **Modules**: `mod lexer { ... }`
- ✅ **Imports**: `use crate::lexer::Token;`
- ✅ **Visibility**: `pub fn`, `pub struct`
- ✅ **Paths**: `crate::`, `super::`, relative paths

#### Standard Library (Minimal)
- ✅ `Result<T, E>` and `Option<T>` enums
- ✅ `Vec<T>` and `String` collections
- ✅ `Box<T>` for heap allocation
- ✅ Basic string operations (`.len()`, `.chars()`, `.as_bytes()`)
- ✅ Iterator basics (`.iter()`, `.next()`)
- ✅ Error handling (`?` operator)
- ✅ `print!` and `println!` macros (pre-expanded)

#### FFI (Restricted)
- ✅ **Extern Declarations** (for runtime only)
  ```rust
  extern "c" {
      fn malloc(size: usize) -> *mut u8;
      fn free(ptr: *mut u8);
  }
  ```
- ✅ **Raw Pointers** (for FFI only): `*const T`, `*mut T`
- ✅ **Unsafe Blocks** (for FFI calls only)
  ```rust
  unsafe {
      free(ptr);
  }
  ```

### Forbidden Features

- ❌ **Traits and Implementations**
  - No `trait` definitions
  - No `impl` blocks
  - Exception: Built-in traits (Iterator) if absolutely necessary
  
- ❌ **Advanced Generics**
  - No trait bounds
  - No where clauses
  - No associated types
  
- ❌ **Async/Await**
  - No `async fn`
  - No `.await`
  - No `Future` trait
  
- ❌ **Macros**
  - No `macro_rules!`
  - No procedural macros
  - Exception: Pre-expanded standard macros (`println!`, `vec!`)
  
- ❌ **Compile-Time Evaluation (CTE)**
  - No `const fn`
  - No const generics
  
- ❌ **Reference Counting**
  - No `Rc<T>`
  - No `Arc<T>`
  - No `RefCell<T>`
  
- ❌ **Managed/GC Islands**
  - All memory management is explicit (Box, Vec)
  
- ❌ **Advanced Pattern Matching**
  - No pattern guards
  - No range patterns
  - No slice patterns
  
- ❌ **Operator Overloading**
  - No custom `Add`, `Sub`, etc. implementations
  
- ❌ **Closures** (Initially)
  - Deferred to Core-1 if needed

### Conformance Tests

Location: `/tests/conformance/core0/`

**Positive Tests** (Must Compile):
- `struct_definition.ast` - Basic struct usage
- `enum_variants.ast` - Enum definition and matching
- `function_calls.ast` - Function definition and calls
- `control_flow.ast` - if/while/for/match
- `vec_operations.ast` - Vec usage
- `string_manipulation.ast` - String operations
- `result_option.ast` - Error handling with Result/Option
- `module_system.ast` - Module imports and visibility

**Negative Tests** (Must Fail):
- `no_traits.ast` - Trait definition should fail
- `no_impl.ast` - Impl block should fail
- `no_async.ast` - Async fn should fail
- `no_macros.ast` - Macro definition should fail
- `no_const_fn.ast` - Const fn should fail

### Diagnostics

When a forbidden feature is encountered:
```
error[E9000]: Feature 'traits' is not available in Aster Core-0
  --> example.ast:5:1
   |
5  | trait Display {
   | ^^^^^ trait definitions require Aster Core-1 or higher
   |
   = note: Core-0 is a minimal subset for bootstrapping
   = help: Use structs and enums instead, or wait for Stage 2
```

---

## Aster Core-1: Type System Subset

**Purpose**: Sufficient to implement type checking, trait solving, and semantic analysis.

**Target**: Stage 2 compiler (name resolution, type inference, traits, effects, ownership)

**Philosophy**: Add generics, traits, and ownership to enable sophisticated compiler algorithms.

### Additional Features (Beyond Core-0)

#### Generics
- ✅ **Generic Types**
  ```rust
  struct HashMap<K, V> {
      entries: Vec<(K, V)>
  }
  ```
- ✅ **Generic Functions with Bounds**
  ```rust
  fn compare<T: Ord>(a: T, b: T) -> bool {
      a < b
  }
  ```
- ✅ **Where Clauses**
  ```rust
  fn process<T>(item: T) where T: Clone + Debug {
      // ...
  }
  ```

#### Traits and Implementations
- ✅ **Trait Definitions**
  ```rust
  trait Display {
      fn fmt(&self) -> String;
  }
  ```
- ✅ **Trait Implementations**
  ```rust
  impl Display for Token {
      fn fmt(&self) -> String {
          format!("{:?}", self.kind)
      }
  }
  ```
- ✅ **Default Methods**
  ```rust
  trait Logger {
      fn log(&self, msg: &str) {
          println!("{}", msg);
      }
  }
  ```
- ✅ **Trait Bounds on Generics**
- ✅ **Associated Types** (Basic)
  ```rust
  trait Iterator {
      type Item;
      fn next(&mut self) -> Option<Self::Item>;
  }
  ```

#### Effect Annotations
- ✅ **Effect Declarations**
  ```rust
  fn read_file(path: &str) -> io String {
      // IO effect declared
  }
  ```
- ✅ **Effect Propagation**
  - Effects automatically propagate through call chains
- ✅ **Effect Checking**
  - Compiler verifies declared effects match inferred effects

#### Ownership and Borrowing (Basic)
- ✅ **Move Semantics**
  ```rust
  let s1 = String::from("hello");
  let s2 = s1; // s1 is moved
  // Cannot use s1 here
  ```
- ✅ **Borrowing**
  ```rust
  let s = String::from("hello");
  let r1 = &s;     // immutable borrow
  let r2 = &s;     // multiple immutable borrows OK
  ```
- ✅ **Mutable Borrowing**
  ```rust
  let mut s = String::from("hello");
  let r = &mut s;  // mutable borrow
  // Cannot have other borrows while r exists
  ```
- ✅ **Lifetime Annotations** (Explicit)
  ```rust
  fn longest<'a>(s1: &'a str, s2: &'a str) -> &'a str {
      if s1.len() > s2.len() { s1 } else { s2 }
  }
  ```

#### Pattern Matching (Enhanced)
- ✅ **Nested Patterns**
  ```rust
  match option {
      Some(inner) => match inner {
          Ok(value) => value,
          Err(_) => default
      },
      None => default
  }
  ```
- ✅ **Pattern Guards**
  ```rust
  match value {
      x if x > 10 => "large",
      _ => "small"
  }
  ```
- ✅ **Range Patterns**
  ```rust
  match age {
      0..=12 => "child",
      13..=19 => "teen",
      _ => "adult"
  }
  ```

#### Closures (Basic)
- ✅ **Closure Definitions**
  ```rust
  let add = |a, b| a + b;
  let double = |x| x * 2;
  ```
- ✅ **Closure Captures**
  ```rust
  let y = 10;
  let add_y = |x| x + y; // Captures y
  ```
- ✅ **Fn Traits** (Fn, FnMut, FnOnce)

#### Reference Counting (for self-referential structures)
- ✅ `Rc<T>` - Single-threaded reference counting
- ✅ `RefCell<T>` - Runtime borrow checking
- ✅ `Arc<T>` - Atomic reference counting (thread-safe)

### Still Forbidden Features

- ❌ **Async/Await** (deferred to Core-2)
- ❌ **Macros** (deferred to Core-2)
- ❌ **Const Evaluation** (deferred to Core-2)
- ❌ **Managed Regions** (deferred to Core-2 or later)
- ❌ **Specialization** (future extension)
- ❌ **Higher-Kinded Types** (future extension)

### Conformance Tests

Location: `/tests/conformance/core1/`

**Positive Tests**:
- `generic_functions.ast` - Generic function definitions
- `trait_definitions.ast` - Trait and impl blocks
- `ownership_moves.ast` - Move semantics
- `borrowing.ast` - Immutable and mutable borrows
- `lifetimes.ast` - Explicit lifetime annotations
- `closures.ast` - Closure definitions and captures
- `effect_annotations.ast` - Effect declarations

**Negative Tests**:
- `no_async.ast` - Async still forbidden
- `no_macros.ast` - Macros still forbidden
- `no_const_eval.ast` - Const fn still forbidden

---

## Aster Core-2: Full Language

**Purpose**: Complete Aster language for the full compiler and tooling.

**Target**: Stage 3 compiler (complete implementation + tooling)

**Philosophy**: All language features enabled. This is the full Aster language.

### Additional Features (Beyond Core-1)

#### Async/Await
- ✅ **Async Functions**
  ```rust
  async fn fetch_data(url: &str) -> async Result<String, Error> {
      // ...
  }
  ```
- ✅ **Await Expressions**
  ```rust
  let data = fetch_data("https://example.com").await?;
  ```
- ✅ **Future Trait**
- ✅ **Async Lowering** to MIR state machines

#### Macros
- ✅ **Declarative Macros** (`macro_rules!`)
  ```rust
  macro_rules! vec {
      ($($x:expr),*) => {
          {
              let mut v = Vec::new();
              $(v.push($x);)*
              v
          }
      };
  }
  ```
- ✅ **Procedural Macros**
  - Derive macros
  - Attribute macros
  - Function-like macros

#### Compile-Time Evaluation
- ✅ **Const Functions**
  ```rust
  const fn factorial(n: u32) -> u32 {
      if n == 0 { 1 } else { n * factorial(n - 1) }
  }
  ```
- ✅ **Const Generics**
  ```rust
  struct Array<T, const N: usize> {
      data: [T; N]
  }
  ```
- ✅ **Compile-Time Execution** (CTE)

#### Advanced Borrow Checking
- ✅ **Non-Lexical Lifetimes (NLL)**
  - Borrows end at last use, not end of scope
- ✅ **Two-Phase Borrows**
- ✅ **Polonius** (if implemented)

#### Managed Regions (Optional)
- ✅ **Region-Based GC**
  ```rust
  region r {
      let obj = r.new(MyStruct { ... });
      // obj is collected when r ends
  }
  ```
- ❓ **Status**: Optional future feature

#### Advanced Pattern Matching
- ✅ **Or-Patterns**
  ```rust
  match value {
      1 | 2 | 3 => "low",
      _ => "high"
  }
  ```
- ✅ **Slice Patterns**
  ```rust
  match slice {
      [first, .., last] => (first, last),
      _ => panic!()
  }
  ```

#### Unsafe Superpowers
- ✅ **Raw Pointer Arithmetic**
- ✅ **Union Types**
- ✅ **Inline Assembly** (architecture-specific)
- ✅ **Transmutation** (type punning)

### Full Standard Library
- ✅ All collections (HashMap, HashSet, BTreeMap, etc.)
- ✅ Concurrency primitives (Mutex, RwLock, channels)
- ✅ I/O (File, TcpStream, etc.)
- ✅ Networking
- ✅ Threading
- ✅ Async runtime integration

### Conformance Tests

Location: `/tests/conformance/core2/`

**Positive Tests**:
- `async_await.ast` - Async functions and await
- `macros.ast` - Macro definitions
- `const_fn.ast` - Const functions and const generics
- `advanced_patterns.ast` - Or-patterns, slice patterns
- `full_stdlib.ast` - Full standard library usage

---

## Feature Gate Implementation

Each subset is enforced via feature gates in the compiler:

```rust
pub enum LanguageEdition {
    Core0,  // Minimal subset
    Core1,  // Type system subset
    Core2,  // Full language
}

impl Parser {
    fn check_feature(&self, feature: Feature) -> Result<(), Diagnostic> {
        match (self.edition, feature) {
            (Core0, Feature::Trait) => Err(diagnostic_forbidden_feature(feature, Core1)),
            (Core0, Feature::Async) => Err(diagnostic_forbidden_feature(feature, Core2)),
            (Core1, Feature::Async) => Err(diagnostic_forbidden_feature(feature, Core2)),
            _ => Ok(())
        }
    }
}
```

**Source File Annotation**:
```rust
#![edition = "core0"]  // At top of .ast file
```

**Command Line**:
```bash
aster0 build --edition core0 lexer.ast
aster1 build --edition core1 type_checker.ast
aster2 build --edition core2 full_compiler.ast
```

---

## Capability Matrix

| Feature | Core-0 | Core-1 | Core-2 |
|---------|--------|--------|--------|
| Primitives | ✅ | ✅ | ✅ |
| Structs | ✅ | ✅ | ✅ |
| Enums | ✅ | ✅ | ✅ |
| Functions | ✅ | ✅ | ✅ |
| Control Flow | ✅ | ✅ | ✅ |
| Pattern Matching (Basic) | ✅ | ✅ | ✅ |
| Modules | ✅ | ✅ | ✅ |
| Generics (Basic) | ✅ | ✅ | ✅ |
| Traits | ❌ | ✅ | ✅ |
| Impl Blocks | ❌ | ✅ | ✅ |
| Effect Annotations | ❌ | ✅ | ✅ |
| Ownership/Borrowing | ❌ | ✅ | ✅ |
| Lifetimes | ❌ | ✅ | ✅ |
| Closures | ❌ | ✅ | ✅ |
| Pattern Guards | ❌ | ✅ | ✅ |
| Rc/Arc | ❌ | ✅ | ✅ |
| Async/Await | ❌ | ❌ | ✅ |
| Macros | ❌ | ❌ | ✅ |
| Const Fn | ❌ | ❌ | ✅ |
| NLL | ❌ | ❌ | ✅ |
| Advanced Patterns | ❌ | ❌ | ✅ |

---

## Subset Rationale

### Why Three Subsets?

1. **Core-0**: Simplest possible imperative language that can still express a compiler frontend
   - Avoids chicken-and-egg: no traits means no trait solver needed to compile trait solver
   - Minimal surface area reduces implementation complexity

2. **Core-1**: Adds the type system features needed to implement sophisticated analysis
   - Traits allow abstraction over visitor patterns, constraint solvers
   - Ownership/borrowing enables implementing the borrow checker itself
   - Closures enable functional programming patterns in analysis passes

3. **Core-2**: Full language unlocks advanced optimizations and tooling
   - Async enables concurrent compilation
   - Macros enable DSLs for IR construction
   - Const evaluation enables compile-time computation

### Why Not More Subsets?

- More stages = more complexity, slower bootstrap
- Three stages balance granularity with practicality
- Each stage represents a meaningful capability jump

### Why Not Fewer Subsets?

- Core-0 → Core-2 is too large a gap
- Implementing a trait solver without traits would be difficult
- Gradual introduction reduces risk

---

## Compiler Mode Selection

The compiler automatically detects the edition from:
1. Source file annotation (`#![edition = "core1"]`)
2. Command-line flag (`--edition core1`)
3. Package manifest (`Aster.toml`: `edition = "core1"`)
4. Default: Core-2 (full language)

---

## Migration Path

As features are ported:

1. **Stage 1**: Write in Core-0, compile with aster0 (C#)
2. **Stage 2**: Rewrite Stage 1 components using Core-1 features
3. **Stage 3**: Rewrite Stage 2 components using Core-2 features
4. **Stage 4**: Continuous improvement using full language

Each stage can also **backport** improvements to earlier stages if beneficial.

---

## Testing Strategy

Each subset has:
- **Positive conformance tests**: Code that should compile
- **Negative conformance tests**: Code that should fail with specific errors
- **Differential tests**: Compare output with C# compiler
- **Feature gate tests**: Ensure forbidden features are rejected

---

## Summary

The three-subset approach provides:
- ✅ **Clear boundaries** between stages
- ✅ **Incremental complexity** for implementation
- ✅ **Testable checkpoints** at each stage
- ✅ **Maintainable scope** for each bootstrap phase
- ✅ **Flexible migration** path as features stabilize

This design balances **purity** (clean subsets) with **pragmatism** (getting to self-hosting efficiently).

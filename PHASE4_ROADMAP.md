# Language Feature Expansion Roadmap (Phase 4)

This document outlines the planned expansion of Aster language features after achieving self-hosting (Stage 3 complete).

**Status**: Phase 4 Complete ✅  
**Last Updated**: 2026-02-21  
**Prerequisite**: Stage 3 bootstrap complete

## Overview

After achieving self-hosting through Stage 3, the language will be expanded with additional features in a disciplined, incremental manner. Each feature must have:

1. **Specification** — Formal spec document
2. **Tests** — Comprehensive test coverage
3. **Documentation** — User-facing docs and examples
4. **STATUS.md Update** — Tracking table entry

## Feature Expansion Order

Features are listed in priority order. Each feature builds on previous features and must be implemented sequentially.

### 1. Methods (High Priority)

**Timeline**: 2-3 weeks after Stage 3  
**Prerequisite**: None (uses existing impl blocks)

#### What
Allow defining methods on structs via `impl` blocks.

#### Syntax
```rust
struct Point {
    x: i32,
    y: i32
}

impl Point {
    fn new(x: i32, y: i32) -> Point {
        Point { x, y }
    }
    
    fn distance(&self) -> f64 {
        ((self.x * self.x + self.y * self.y) as f64).sqrt()
    }
}
```

#### Specification Requirements
- [x] Method call syntax and resolution
- [x] `self`, `&self`, `&mut self` parameter handling
- [x] Method visibility rules
- [x] Associated functions vs instance methods
- [x] Method dispatch (static only, no virtual calls yet)

#### Testing Requirements
- [x] Basic method definition and calling
- [x] Self reference handling
- [x] Method name shadowing rules
- [x] Error cases (invalid self types, etc.)
- [x] Integration with type system

#### Evidence
- Spec: `docs/spec/methods.md` ✅
- Tests: `tests/Aster.Compiler.Tests/CompilerTests.cs` (Phase4MethodCallTests) ✅

---

### 2. Struct impl Blocks (High Priority)

**Timeline**: 1-2 weeks after Methods  
**Prerequisite**: Methods

#### What
Organize methods and associated functions in `impl` blocks.

#### Syntax
```rust
struct Vec<T> {
    data: *mut T,
    len: usize,
    cap: usize
}

impl<T> Vec<T> {
    fn new() -> Vec<T> { ... }
    fn push(&mut self, value: T) { ... }
    fn len(&self) -> usize { self.len }
}
```

#### Specification Requirements
- [x] Multiple impl blocks per type
- [x] Impl block scope rules
- [x] Orphan rules (defining impls for external types)
- [x] Coherence rules

#### Testing Requirements
- [x] Multiple impl blocks
- [x] Impl block ordering
- [x] Visibility across modules
- [x] Error cases (conflicting impls, etc.)

#### Evidence
- Spec: `docs/spec/impl-blocks.md` ✅
- Tests: `tests/Aster.Compiler.Tests/CompilerTests.cs` (Phase4AssociatedTypeTests) ✅

---

### 3. Generics (High Priority)

**Timeline**: 3-4 weeks after impl blocks  
**Prerequisite**: Methods, impl blocks

#### What
Parametric polymorphism for functions and structs.

#### Syntax
```rust
fn max<T>(a: T, b: T) -> T where T: Ord {
    if a > b { a } else { b }
}

struct Container<T> {
    value: T
}

impl<T> Container<T> {
    fn new(value: T) -> Container<T> {
        Container { value }
    }
}
```

#### Specification Requirements
- [x] Type parameter syntax
- [x] Generic type inference
- [x] Monomorphization strategy
- [x] Where clauses (trait bounds)
- [x] Generic lifetime parameters (parsing, erased before type check)

#### Testing Requirements
- [x] Generic functions
- [x] Generic structs
- [x] Generic enums
- [x] Type inference with generics
- [x] Monomorphization verification
- [x] Error cases (unbounded type parameters, etc.)

#### Evidence
- Spec: `docs/spec/generics.md` ✅
- Tests: `tests/Aster.Compiler.Tests/CompilerTests.cs` (Week9 through Week12) ✅

---

### 4. Traits (High Priority)

**Timeline**: 4-5 weeks after Generics  
**Prerequisite**: Generics

#### What
Interface/typeclass system for defining shared behavior.

#### Syntax
```rust
trait Display {
    fn display(&self) -> String;
}

impl Display for i32 {
    fn display(&self) -> String {
        format("{}", self)
    }
}

fn print_display<T: Display>(value: T) {
    println(value.display())
}
```

#### Specification Requirements
- [x] Trait definition syntax
- [x] Trait implementation syntax
- [x] Trait bounds
- [x] Trait methods (required vs provided)
- [x] Associated types (Phase 4.6)
- [x] Trait coherence rules
- [x] Orphan rules
- [x] Trait objects (`dyn Trait`)

#### Testing Requirements
- [x] Trait definition and implementation
- [x] Trait bounds
- [x] Multiple trait bounds
- [x] Default method implementations
- [x] Trait object semantics (`dyn Trait`)
- [x] Error cases (conflicting impls, missing methods, etc.)

#### Evidence
- Spec: `docs/spec/traits.md` ✅
- Tests: `tests/Aster.Compiler.Tests/CompilerTests.cs` (Week20Traits, Phase4DynTraitTests, Phase4DefaultTraitMethodTests) ✅

---

### 5. Borrow Checker Enhancement (High Priority)

**Timeline**: 4-6 weeks after Traits  
**Prerequisite**: Traits (for complex borrowing patterns)

#### What
Enhanced borrow checking with full NLL (Non-Lexical Lifetimes) support.

#### Features
- Precise lifetime tracking
- Borrow splitting
- Two-phase borrows
- Polonius-style analysis (future)

#### Specification Requirements
- [x] Lifetime syntax
- [x] Borrow checker algorithm (NLL / dataflow)
- [x] Error messages with hints
- [x] Two-phase borrows (Phase 4)

#### Testing Requirements
- [x] Complex borrowing patterns
- [x] Two-phase borrow support
- [x] Error cases with helpful suggestions
- [x] Lifetime annotation parsing

#### Evidence
- Spec: `docs/spec/borrow-checker.md` ✅
- Tests: `tests/Aster.Compiler.Tests/CompilerTests.cs` (BorrowCheckerTests, Phase4NllBorrowCheckerTests) ✅

---

### 6. Associated Types (Medium Priority)

**Timeline**: 2-3 weeks after Borrow Checker  
**Prerequisite**: Traits

#### What
Type members of traits.

#### Syntax
```rust
trait Iterator {
    type Item;
    fn next(&mut self) -> Option<Self::Item>;
}

impl Iterator for Range {
    type Item = i32;
    fn next(&mut self) -> Option<i32> { ... }
}
```

#### Specification Requirements
- [x] Associated type syntax
- [x] Projection rules
- [x] Where clauses with associated types

#### Evidence
- Spec: `docs/spec/associated-types.md` ✅
- Tests: `tests/Aster.Compiler.Tests/CompilerTests.cs` (Phase4AssociatedTypeTests) ✅

---

### 7. Async/Await (Medium Priority)

**Timeline**: 6-8 weeks after Associated Types  
**Prerequisite**: Traits, Generics

#### What
Asynchronous programming support.

#### Syntax
```rust
async fn fetch_data(url: String) -> Result<String, Error> {
    let response = http::get(url).await?;
    Ok(response.body().await?)
}
```

#### Specification Requirements
- [x] `async` keyword
- [x] `await` expression
- [x] Future trait
- [x] Async lowering to state machines (basic)

#### Testing Requirements
- [x] Basic async functions
- [x] Await expressions
- [x] Error propagation in async

#### Evidence
- Tests: `tests/Aster.Compiler.Tests/CompilerTests.cs` (Phase3AsyncLowerTests) ✅

---

### 8. Macros (Low Priority)

**Timeline**: 8-10 weeks  
**Prerequisite**: Full parser

#### What
Declarative macros (macro_rules!) and procedural macros.

#### Syntax
```rust
macro_rules! vec {
    ($($x:expr),*) => {
        {
            let mut temp = Vec::new();
            $(temp.push($x);)*
            temp
        }
    };
}
```

#### Evidence
- Tests: `tests/Aster.Compiler.Tests/CompilerTests.cs` (Phase4MacroTests) ✅

---

### 9. Procedural Macros (Low Priority)

**Timeline**: 10-12 weeks  
**Prerequisite**: Macros

#### What
Compile-time code generation via function-like interfaces.

#### Evidence
- Spec: `docs/spec/proc-macros.md`
- Tests: `tests/features/proc-macros/`

---

## Implementation Process

For each feature:

### Step 1: Specification (1 week)
- Write formal spec in `docs/spec/<feature>.md`
- Define syntax
- Define semantics
- Define edge cases
- Get review/feedback

### Step 2: Implementation (2-8 weeks, varies)
- Implement in compiler
- Add to parser
- Add to type checker
- Add to MIR lowering (if needed)
- Update code generator

### Step 3: Testing (1-2 weeks)
- Write comprehensive tests
- Cover happy path
- Cover error cases
- Test integration with other features

### Step 4: Documentation (1 week)
- Write user-facing documentation
- Add to language book
- Create examples
- Update tutorials

### Step 5: Release
- Update STATUS.md
- Update CHANGELOG.md
- Announce feature

## Success Criteria

Each feature must meet:

- ✅ Specification complete and reviewed
- ✅ Implementation complete
- ✅ All tests passing (100% coverage of spec)
- ✅ Documentation complete
- ✅ No regressions in existing features
- ✅ Deterministic output maintained
- ✅ Bootstrap still works (compiler can compile itself)

## Priority Levels

**High Priority** — Essential for production use
**Medium Priority** — Important but not blocking
**Low Priority** — Nice to have, future work

## Timeline Summary

Assuming sequential implementation with one feature at a time:

| Feature | Duration | Cumulative Time |
|---------|----------|-----------------|
| Methods | 2-3 weeks | 2-3 weeks |
| Impl blocks | 1-2 weeks | 3-5 weeks |
| Generics | 3-4 weeks | 6-9 weeks |
| Traits | 4-5 weeks | 10-14 weeks |
| Borrow checker | 4-6 weeks | 14-20 weeks |
| Associated types | 2-3 weeks | 16-23 weeks |
| Async/await | 6-8 weeks | 22-31 weeks |
| Macros | 8-10 weeks | 30-41 weeks |
| Proc macros | 10-12 weeks | 40-53 weeks |

**Total**: ~40-53 weeks (~1 year) for all Phase 4 features

**Critical Path**: Methods → impl blocks → Generics → Traits → Borrow checker (~14-20 weeks for production-ready compiler)

## Current Status (2026-02-21)

- **Phase 4**: ✅ **COMPLETE**
  - Methods ✅
  - Struct impl Blocks ✅
  - Generics ✅
  - Traits (required/default methods, dyn Trait) ✅
  - Borrow Checker Enhancement (NLL + two-phase borrows) ✅
  - Associated Types ✅
  - Async/Await (basic lowering) ✅
  - Declarative Macros (`macro_rules!`, built-in macros) ✅
  - CSE (Common Subexpression Elimination) optimization pass ✅
  - Spec documents ✅
- **Phase 5**: Not started (advanced optimizations, proc macros, self-hosting polish)

## References

- [STATUS.md](STATUS.md) — Overall progress tracking
- [BOOTSTRAP_WORKFLOW.md](BOOTSTRAP_WORKFLOW.md) — Bootstrap process
- [docs/spec/](docs/spec/) — Language specifications
- Rust Language Reference — Inspiration for feature design

---

**Note**: This roadmap is subject to change based on user feedback and implementation experience.

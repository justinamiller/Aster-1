# Language Feature Expansion Roadmap (Phase 4)

This document outlines the planned expansion of Aster language features after achieving self-hosting (Stage 3 complete).

**Status**: Planning Phase  
**Last Updated**: 2026-02-15  
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
- [ ] Method call syntax and resolution
- [ ] `self`, `&self`, `&mut self` parameter handling
- [ ] Method visibility rules
- [ ] Associated functions vs instance methods
- [ ] Method dispatch (static only, no virtual calls yet)

#### Testing Requirements
- [ ] Basic method definition and calling
- [ ] Self reference handling
- [ ] Method name shadowing rules
- [ ] Error cases (invalid self types, etc.)
- [ ] Integration with type system

#### Evidence
- Spec: `docs/spec/methods.md`
- Tests: `tests/features/methods/`
- Docs: `docs/book/methods.md`

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
- [ ] Multiple impl blocks per type
- [ ] Impl block scope rules
- [ ] Orphan rules (defining impls for external types)
- [ ] Coherence rules

#### Testing Requirements
- [ ] Multiple impl blocks
- [ ] Impl block ordering
- [ ] Visibility across modules
- [ ] Error cases (conflicting impls, etc.)

#### Evidence
- Spec: `docs/spec/impl-blocks.md`
- Tests: `tests/features/impl-blocks/`
- Docs: `docs/book/impl-blocks.md`

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
- [ ] Type parameter syntax
- [ ] Generic type inference
- [ ] Monomorphization strategy
- [ ] Where clauses (trait bounds)
- [ ] Generic lifetime parameters (later)

#### Testing Requirements
- [ ] Generic functions
- [ ] Generic structs
- [ ] Generic enums
- [ ] Type inference with generics
- [ ] Monomorphization verification
- [ ] Error cases (unbounded type parameters, etc.)

#### Evidence
- Spec: `docs/spec/generics.md`
- Tests: `tests/features/generics/`
- Docs: `docs/book/generics.md`

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
- [ ] Trait definition syntax
- [ ] Trait implementation syntax
- [ ] Trait bounds
- [ ] Trait methods (required vs provided)
- [ ] Associated types (Phase 4.6)
- [ ] Trait coherence rules
- [ ] Orphan rules

#### Testing Requirements
- [ ] Trait definition and implementation
- [ ] Trait bounds
- [ ] Multiple trait bounds
- [ ] Default method implementations
- [ ] Trait object semantics (if supported)
- [ ] Error cases (conflicting impls, missing methods, etc.)

#### Evidence
- Spec: `docs/spec/traits.md`
- Tests: `tests/features/traits/`
- Docs: `docs/book/traits.md`

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
- [ ] Lifetime syntax
- [ ] Borrow checker algorithm
- [ ] Error messages
- [ ] Special cases (loop invariants, etc.)

#### Testing Requirements
- [ ] Complex borrowing patterns
- [ ] Lifetime inference
- [ ] Borrow splitting
- [ ] Error cases with helpful suggestions

#### Evidence
- Spec: `docs/spec/borrow-checker.md`
- Tests: `tests/features/borrowck/`
- Docs: `docs/book/ownership.md`

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
- [ ] Associated type syntax
- [ ] Projection rules
- [ ] Where clauses with associated types

#### Evidence
- Spec: `docs/spec/associated-types.md`
- Tests: `tests/features/associated-types/`

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
- [ ] `async` keyword
- [ ] `await` expression
- [ ] Future trait
- [ ] Runtime ABI
- [ ] Async lowering to state machines

#### Testing Requirements
- [ ] Basic async functions
- [ ] Await expressions
- [ ] Error propagation in async
- [ ] Async in traits (future)

#### Evidence
- Spec: `docs/spec/async.md`
- Tests: `tests/features/async/`
- Docs: `docs/book/async.md`

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
- Spec: `docs/spec/macros.md`
- Tests: `tests/features/macros/`

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

## Current Status (2026-02-15)

- **Phase 4**: Not started (requires Stage 3 completion first)
- **Stage 3**: 0% complete (infrastructure ready)
- **Estimated Start**: 12-15 months from now

## References

- [STATUS.md](STATUS.md) — Overall progress tracking
- [BOOTSTRAP_WORKFLOW.md](BOOTSTRAP_WORKFLOW.md) — Bootstrap process
- [docs/spec/](docs/spec/) — Language specifications
- Rust Language Reference — Inspiration for feature design

---

**Note**: This roadmap is subject to change based on user feedback and implementation experience.

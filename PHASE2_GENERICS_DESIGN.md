# Phase 2: Generics Design Document

**Feature**: Generic Programming Support  
**Phase**: 2.1 (Weeks 9-12)  
**Status**: 📝 Design  
**Complexity**: HIGH

---

## Executive Summary

This document specifies the design and implementation plan for adding generic programming support to Aster, enabling parameterized functions and types with compile-time type safety.

### Goals
- Generic functions with type parameters
- Generic structs with parameterized fields
- Type parameter constraints
- Monomorphization-based code generation
- Zero runtime overhead

### Non-Goals (Future Work)
- Associated types (deferred to trait system)
- Higher-kinded types (advanced feature)
- Specialization (optimization feature)
- Generic traits (Phase 3)

---

## Syntax Design

### Generic Functions

```aster
// Basic generic function
fn identity<T>(x: T) -> T {
    x
}

// Multiple type parameters
fn pair<A, B>(first: A, second: B) -> (A, B) {
    (first, second)
}

// With constraints (future)
fn compare<T: Ord>(a: T, b: T) -> bool {
    a < b
}
```

### Generic Structs

```aster
// Generic struct
struct Box<T> {
    value: T
}

// Multiple parameters
struct Pair<A, B> {
    first: A,
    second: B
}

// Nested generics
struct Container<T> {
    items: Vec<T>
}
```

### Generic Enums

```aster
// Optional value
enum Option<T> {
    Some(T),
    None
}

// Result type
enum Result<T, E> {
    Ok(T),
    Err(E)
}
```

---

## Type System Integration

### Type Parameters

**Representation**:
```csharp
class TypeParameter {
    string Name;         // "T", "K", etc.
    int Index;           // Position in parameter list
    List<Constraint> Constraints;  // Bounds
}
```

**Scoping**:
- Type parameters scoped to function/struct
- Shadow outer type parameters
- Unique names within scope

### Type Substitution

**Process**:
1. Collect type parameters
2. Build substitution map: T → ConcreteType
3. Replace all occurrences
4. Validate constraints

**Example**:
```aster
fn foo<T>(x: T) -> T { x }
let y: i32 = foo<i32>(42);  // T → i32

// Results in:
fn foo_i32(x: i32) -> i32 { x }
```

### Constraint Checking

**Phase 1**: No constraints (accept any type)  
**Phase 2**: Basic constraints (Eq, Ord)  
**Phase 3**: Trait-based constraints (full system)

---

## Monomorphization Strategy

### Overview

**Approach**: Generate specialized versions for each concrete type  
**Timing**: During code generation phase  
**Caching**: Memoize generated instances

### Algorithm

```
For each generic function/struct:
  1. Collect all instantiation sites
  2. Extract concrete type arguments
  3. For each unique type combination:
     a. Create specialized version
     b. Substitute type parameters
     c. Type check specialized version
     d. Generate code
     e. Cache result
  4. Replace generic calls with specialized calls
```

### Example

**Source**:
```aster
fn max<T>(a: T, b: T) -> T {
    if a > b { a } else { b }
}

let x = max(5, 10);      // T = i32
let y = max(3.14, 2.71); // T = f64
```

**Generated**:
```aster
fn max_i32(a: i32, b: i32) -> i32 {
    if a > b { a } else { b }
}

fn max_f64(a: f64, b: f64) -> f64 {
    if a > b { a } else { b }
}

let x = max_i32(5, 10);
let y = max_f64(3.14, 2.71);
```

### Name Mangling

**Format**: `{original_name}_{type_hash}`

**Examples**:
- `identity<i32>` → `identity_i32`
- `Box<String>` → `Box_String`
- `Option<Vec<i32>>` → `Option_Vec_i32`

---

## Implementation Plan

### Week 9: Parser Extension

**Tasks**:
1. Add generic parameter syntax: `<T>`, `<T, U>`
2. Parse generic function declarations
3. Parse generic struct declarations
4. Update AST nodes with type parameters
5. Add tests for parser

**Deliverables**:
- GenericFunctionDecl AST node
- GenericStructDecl AST node
- TypeParameter AST node
- 20+ parser tests

### Week 10: Type System

**Tasks**:
1. Implement TypeParameter class
2. Add type parameter scoping
3. Implement type substitution
4. Basic constraint checking
5. Type inference for generics

**Deliverables**:
- Type parameter representation
- Substitution algorithm
- Scope management
- 30+ type system tests

### Week 11: Monomorphization

**Tasks**:
1. Implement monomorphization pass
2. Instance generation
3. Name mangling
4. Caching mechanism
5. Integration with codegen

**Deliverables**:
- Monomorphizer class
- Instance cache
- Name mangling utility
- 25+ monomorphization tests

### Week 12: Testing & Polish

**Tasks**:
1. Comprehensive test suite
2. Error message improvements
3. Documentation
4. Performance testing
5. Bug fixes

**Deliverables**:
- 100+ tests total
- User documentation
- Performance benchmarks
- Bug-free release

---

## Code Generation

### LLVM Backend

**Strategy**: Generate separate LLVM functions for each instance

```llvm
; Generic source: fn identity<T>(x: T) -> T { x }

; Instantiation: identity<i32>
define i32 @identity_i32(i32 %x) {
  ret i32 %x
}

; Instantiation: identity<f64>
define double @identity_f64(double %x) {
  ret double %x
}
```

### Type Erasure (Rejected)

**Why Not**: Runtime overhead, complexity, weaker type safety

### Dynamic Dispatch (Rejected)

**Why Not**: Performance cost, vtable overhead, not Aster's philosophy

---

## Error Handling

### Type Parameter Errors

```aster
fn foo<T>(x: T) -> U {  // Error: U not declared
    x
}
```

**Message**: "Type parameter 'U' not found. Did you mean 'T'?"

### Constraint Violations

```aster
fn compare<T: Ord>(a: T, b: T) -> bool {
    a < b
}

compare("hello", "world");  // OK if String implements Ord
compare([1,2,3], [4,5,6]);  // Error: arrays don't implement Ord
```

**Message**: "Type '[i32; 3]' does not satisfy constraint 'Ord'"

### Instantiation Errors

```aster
fn foo<T>(x: T) -> T {
    x + 1  // Error: can't add to generic T
}
```

**Message**: "Cannot apply operator '+' to type 'T' without constraint 'Add'"

---

## Performance Considerations

### Compile Time

**Cost**: O(n * m) where n = generic definitions, m = instantiations  
**Mitigation**: Caching, incremental compilation  
**Acceptable**: <1s overhead for typical projects

### Code Size

**Cost**: Code bloat from many instantiations  
**Mitigation**: Inline small functions, share code where possible  
**Acceptable**: 2-3x increase for heavily generic code

### Runtime

**Cost**: Zero - monomorphization eliminates overhead  
**Benefit**: Same performance as hand-written specialized code

---

## Testing Strategy

### Unit Tests

1. **Parser Tests**: All syntax variations
2. **Type System Tests**: Substitution, scoping, constraints
3. **Monomorphization Tests**: Instance generation, caching
4. **Codegen Tests**: LLVM output correctness

### Integration Tests

1. **Generic Algorithms**: Sort, search, map, filter
2. **Generic Data Structures**: Stack, queue, tree
3. **Nested Generics**: Vec<Option<T>>, etc.
4. **Complex Scenarios**: Multiple type parameters, recursion

### Performance Tests

1. **Compilation Speed**: Measure monomorphization overhead
2. **Runtime Performance**: Compare to non-generic code
3. **Code Size**: Measure binary size impact

---

## Migration Path

### From Phase 1

**Before** (Non-generic):
```aster
fn max_i32(a: i32, b: i32) -> i32 {
    if a > b { a } else { b }
}

fn max_f64(a: f64, b: f64) -> f64 {
    if a > b { a } else { b }
}
```

**After** (Generic):
```aster
fn max<T>(a: T, b: T) -> T {
    if a > b { a } else { b }
}

// Works for any comparable type
```

### Backward Compatibility

- All Phase 1 code continues to work
- No breaking changes
- Generics are additive feature
- Opt-in, not required

---

## Future Enhancements

### Phase 3+

1. **Trait Constraints**: `T: Trait` bounds
2. **Associated Types**: `type Item = T;`
3. **Higher-Kinded Types**: `F<_>`
4. **Specialization**: Optimize specific cases
5. **Const Generics**: `Array<T, N>`

### Not Planned

1. **Dynamic Generics**: Too costly
2. **Reflection**: Not in scope
3. **Variance**: Complex, deferred

---

## Success Criteria

### Must Have ✅
- Generic functions work
- Generic structs work
- Basic generic enums work
- Monomorphization produces correct code
- Zero runtime overhead
- Type safety maintained

### Should Have ✅
- Error messages clear
- Compilation time reasonable
- Code size acceptable
- Documentation complete
- 100+ tests passing

### Nice to Have ✅
- IDE support hooks
- Debugger integration
- Performance optimization
- Advanced diagnostics

---

## Conclusion

Generics are a cornerstone feature for Phase 2. This design provides a clear, implementable path to adding generic programming to Aster with high quality and performance.

The monomorphization approach is proven, well-understood, and aligns with Aster's zero-cost abstraction philosophy.

**Status**: Ready for implementation  
**Confidence**: HIGH  
**Complexity**: HIGH, but manageable

---

**Next**: Begin Week 9 parser work  
**Timeline**: 4 weeks to completion  
**Team**: 2-3 engineers recommended

---

*This design document will be updated as implementation progresses and new insights are gained.*

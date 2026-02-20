# Phase 2 Week 9: Ready to Begin

**Date**: 2026-02-20  
**Status**: 🚀 READY TO START  
**Focus**: Generics Parser Implementation

---

## Current Status

### Phase 1 Complete ✅
- All bugs fixed
- Comprehensive testing (11 files, 1,166 LOC)
- Benchmarks established (3 files, 365 LOC)
- Professional documentation
- Type coercion infrastructure
- Foundation solid

### Phase 2 Launched ✅
- Planning complete (PHASE2_KICKOFF.md)
- Generics design documented (PHASE2_GENERICS_DESIGN.md)
- 12-week roadmap defined
- Success criteria clear
- Team aligned

---

## Week 9 Objective

**Primary Goal**: Implement parser support for generic syntax

### What We're Building

**Generic Function Syntax**:
```aster
fn identity<T>(x: T) -> T {
    x
}

fn pair<A, B>(first: A, second: B) -> (A, B) {
    (first, second)
}
```

**Generic Struct Syntax**:
```aster
struct Box<T> {
    value: T
}

struct Pair<A, B> {
    first: A,
    second: B
}
```

---

## Implementation Approach

### Phase 1: Lexer Extension
- Add `<` and `>` tokens for generics
- Handle ambiguity with comparison operators
- Context-aware tokenization

### Phase 2: AST Extension
- `TypeParameter` node
- Update `FunctionDeclaration` with type params
- Update `StructDeclaration` with type params
- `GenericType` node for usage

### Phase 3: Parser Implementation
- Parse `<T, U>` type parameter lists
- Parse generic function declarations
- Parse generic struct declarations
- Parse generic type instantiation
- Error handling for malformed syntax

### Phase 4: Testing
- Simple generic function tests
- Generic struct tests
- Multiple type parameters
- Error cases
- Integration with existing parser

---

## Success Criteria

### Parser Must Support

1. **Generic Functions** ✅
   - Single type parameter: `fn foo<T>(x: T)`
   - Multiple parameters: `fn bar<T, U>(x: T, y: U)`
   - Return types: `fn baz<T>() -> T`

2. **Generic Structs** ✅
   - Single parameter: `struct Box<T> { value: T }`
   - Multiple parameters: `struct Pair<A, B>`
   - Nested usage: `struct Nested<T> { box: Box<T> }`

3. **Error Handling** ✅
   - Unclosed angle brackets
   - Invalid type parameters
   - Duplicate names
   - Clear error messages

---

## Technical Decisions

### Angle Bracket Handling

**Problem**: `<` and `>` used for both generics and comparisons

**Solution**: Context-aware parsing
- After `fn name` or `struct name`: Generic context
- In expressions: Comparison context
- Track nesting depth
- Backtrack if needed

### AST Design

**TypeParameter**:
```csharp
class TypeParameter {
    string Name;
    Span Span;
    // Constraints added later
}
```

**Generic Function**:
```csharp
class FunctionDeclaration {
    string Name;
    List<TypeParameter> TypeParameters; // NEW
    List<Parameter> Parameters;
    TypeAnnotation ReturnType;
    Block Body;
}
```

---

## Testing Strategy

### Unit Tests
- Lexer: Token generation
- Parser: AST construction
- Error handling: Invalid syntax

### Integration Tests
- Parse real generic functions
- Parse real generic structs
- Complex nested generics

### Example Test Cases
```aster
// Test 1: Simple generic function
fn identity<T>(x: T) -> T { x }

// Test 2: Multiple parameters
fn swap<A, B>(a: A, b: B) -> (B, A) { (b, a) }

// Test 3: Generic struct
struct Option<T> {
    has_value: bool,
    value: T
}

// Test 4: Nested generics
struct Container<T> {
    items: Vec<T>
}
```

---

## Dependencies

### Required Components (Already Exist)
- ✅ Lexer infrastructure
- ✅ Parser framework
- ✅ AST nodes
- ✅ Error reporting
- ✅ Testing framework

### Will Enable (Future Weeks)
- Week 10: Type system for generics
- Week 11: Monomorphization
- Week 12: Testing & polish

---

## Timeline

**Week 9 Breakdown**:
- Day 1: Lexer tokens & initial AST (2-3 hours)
- Day 2: Parser implementation (3-4 hours)
- Day 3: Testing & polish (2-3 hours)
- Day 4: Documentation & review (1 hour)

**Total**: 8-11 hours focused work

---

## Risk Assessment

### Technical Risks

**Angle Bracket Ambiguity**: MEDIUM
- Mitigation: Context-aware parsing
- Fallback: Explicit syntax like `::<T>`

**Parser Complexity**: LOW
- Mitigation: Incremental implementation
- Fallback: Simplified syntax initially

**Breaking Changes**: LOW
- Mitigation: Additive changes only
- Fallback: Feature flag

### Schedule Risks

**Time Estimation**: LOW
- Clear scope
- Well-defined tasks
- Proven process

---

## Resources

### Design Documents
- PHASE2_KICKOFF.md - Overall plan
- PHASE2_GENERICS_DESIGN.md - Detailed design
- Rust Reference - Generic syntax inspiration
- C++ Templates - Implementation patterns

### Code References
- `src/Aster.Compiler/Frontend/Parser/` - Current parser
- `src/Aster.Compiler/Frontend/AST/` - AST nodes
- `src/Aster.Compiler/Frontend/Lexer/` - Lexer

---

## Next Steps After Week 9

### Week 10: Type System
- Generic type parameters in type checker
- Type substitution infrastructure
- Constraint checking foundation

### Week 11: Monomorphization
- Instance generation
- Type specialization
- Code generation

### Week 12: Polish
- Comprehensive testing
- Performance optimization
- Documentation

---

## Motivation

**Why Generics Matter**:
- **Code Reuse**: Write once, use with many types
- **Type Safety**: Compile-time checking
- **Performance**: Zero runtime overhead
- **Foundation**: Enables Vec<T>, Option<T>, etc.

**Example Benefits**:
```aster
// Without generics (need separate functions)
fn identity_i32(x: i32) -> i32 { x }
fn identity_f64(x: f64) -> f64 { x }
fn identity_String(x: String) -> String { x }

// With generics (one function for all)
fn identity<T>(x: T) -> T { x }
```

---

## Team Readiness

### Skills Required
- ✅ Parser design (proven in Phase 1)
- ✅ AST manipulation (existing experience)
- ✅ Testing methodology (established)
- ✅ Documentation (professional standard)

### Process
- ✅ Incremental development
- ✅ Continuous testing
- ✅ Clear communication
- ✅ Quality focus

---

## Conclusion

**Week 9 is foundational work** that enables everything else in Phase 2. The parser extensions we build this week will support:
- Generic functions and structs
- Vec<T> and other collections
- Option<T> and Result<T,E>
- Future trait system

**Quality over speed**: Take time to get the parser right, as it's the foundation for 11 more weeks of feature work.

---

**Status**: 🚀 READY  
**Confidence**: 💪 HIGH  
**Let's build generics!** 🎯


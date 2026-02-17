# Stage 2 Compiler Source

This directory contains the Stage 2 Aster compiler implementation, written in the **Core-1 language subset**.

## Purpose

Stage 2 extends Stage 1 with:
- **Type inference** (Hindley-Milner)
- **Trait resolution** and impl checking
- **Effect system** inference and checking
- **Basic ownership** analysis (move semantics, borrow tracking)
- **Generics** with trait bounds

## Language Subset (Core-1)

Stage 2 is written in Core-1, which includes:
- All Core-0 features (primitives, functions, structs, enums, control flow)
- **Generics** with trait bounds
- **Traits** and implementations
- **Effect annotations**
- **Closures**
- **Basic ownership** semantics

## Components

### 1. Name Resolution (`nameresolver.ast`)
- Symbol table construction
- Scope management
- Import/export resolution
- Cross-module resolution

### 2. Type Inference (`typeinfer.ast`)
- Constraint generation
- Unification algorithm
- Type scheme instantiation
- Generic type support

### 3. Trait Solver (`traitsolver.ast`)
- Trait database construction
- Obligation resolution
- Impl lookup with caching
- Cycle detection

### 4. Effect System (`effectsystem.ast`)
- Effect inference
- Effect propagation
- Effect annotation checking

### 5. Ownership Analysis (`ownership.ast`)
- Move semantics validation
- Borrow tracking
- Use-after-move detection

## Building

Stage 2 is compiled by Stage 1:

```bash
# Using bootstrap script
./bootstrap/scripts/bootstrap.sh --stage 2

# Manual compilation
aster1 build aster/compiler/stage2/*.ast -o build/bootstrap/stage2/aster2
```

## Testing

```bash
# Run Stage 2 tests
./bootstrap/scripts/verify.sh --stage 2

# Differential testing (compare aster0 vs aster2)
./bootstrap/scripts/differential.sh --stage 2
```

## Self-Compilation

Stage 2 must be able to compile itself:

```bash
aster2 build aster/compiler/stage2/*.ast -o aster2'
# Verify: aster2 == aster2' (should be identical or semantically equivalent)
```

## Status

**Current**: Initial implementation in progress
**Next**: Name resolution framework

See `/STATUS.md` for overall bootstrap progress.

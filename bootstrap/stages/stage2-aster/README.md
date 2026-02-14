# Stage 2: Expanded Aster Compiler

## Overview

Stage 2 extends the compiler with **type system, traits, and semantics**, written in **Core-1 subset** of Aster.

## Language Subset

**Aster Core-1** (see `/bootstrap/spec/aster-core-subsets.md`):
- All Core-0 features
- **Generics** with trait bounds
- **Traits** and implementations
- **Effect annotations**
- **Ownership and borrowing** (basic)
- **Closures**

## Components Implemented

Stage 2 adds:
5. **Name Resolution** - Symbol table, cross-module resolution
6. **Type Inference** - Hindley-Milner with unification
7. **Trait Solver** - Trait resolution with cycle detection
8. **Effect System** - Effect inference and checking
9. **Ownership** - Move semantics, borrow tracking

## Source Location

**Future**: `/aster/compiler/stage2/`

**Status**: Not yet implemented (infrastructure ready)

## Building

Once implemented:

```bash
# Compile Aster source with Stage 1 compiler
aster1 build aster/compiler/stage2/*.ast -o build/bootstrap/stage2/aster2

# Or use bootstrap script
./bootstrap/scripts/bootstrap.sh --stage 2
```

## Verification

Stage 2 is verified by:
- All Stage 1 verifications still pass
- Differential tests: aster0 vs aster2 on Core-1 fixtures
- Type inference equivalence
- Trait resolution equivalence
- Self-compilation: aster2 compiles itself

See: `./bootstrap/scripts/verify.sh --stage 2`

## Implementation Order

1. **Name Resolution** (uses traits for visitors)
2. **Type Inference** (uses generics)
3. **Trait Solver** (self-hosting - uses traits to implement trait solver)
4. **Effect System** (uses type inference results)
5. **Ownership** (uses trait-based abstractions)

## Next Steps

After Stage 1 is complete:

1. **Port Name Resolution**
   - Symbol table construction
   - Import/export handling
   - Scope management

2. **Port Type Checker**
   - Constraint generation
   - Unification algorithm
   - Type scheme instantiation

3. **Port Trait Solver**
   - Trait database
   - Obligation resolution
   - Cycle detection

4. **Create Core-1 Tests**
   - Generic function tests
   - Trait impl tests
   - Ownership tests

## See Also

- `/bootstrap/spec/bootstrap-stages.md` - Stage 2 specification
- `/bootstrap/spec/aster-core-subsets.md` - Core-1 language definition

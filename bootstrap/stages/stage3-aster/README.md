# Stage 3: Full Aster Compiler

## Overview

Stage 3 is the **complete, production-ready** Aster compiler with full language support and tooling, written in **Core-2 (full Aster)**.

## Language Subset

**Aster Core-2** (see `/bootstrap/spec/aster-core-subsets.md`):
- All Core-0 and Core-1 features
- **Async/await**
- **Macros** (declarative and procedural)
- **Const functions**
- **Non-lexical lifetimes** (NLL)
- Full standard library

## Components Implemented

Stage 3 adds:
10. **Borrow Checker** - NLL dataflow analysis
11. **MIR Builder** - MIR construction and verification
12. **Optimizations** - DCE, constant folding, CSE, inlining, SROA
13. **LLVM Backend** - LLVM IR emission
14. **Tooling** - Formatter, linter, doc generator, test runner

## Source Location

**Future**: `/aster/compiler/stage3/`

**Status**: Not yet implemented (infrastructure ready)

## Building

Once implemented:

```bash
# Compile Aster source with Stage 2 compiler
aster2 build aster/compiler/stage3/*.ast -o build/bootstrap/stage3/aster3

# Or use bootstrap script
./bootstrap/scripts/bootstrap.sh --stage 3
```

## Self-Hosting Verification

The critical test for Stage 3 is **self-hosting**:

```bash
# aster3 compiles itself
aster3 build aster/compiler/stage3/*.ast -o aster3'

# Compare binaries (should be identical or semantically equivalent)
./bootstrap/scripts/verify.sh --self-check
```

**Success Criteria**: `aster3 == aster3'` (bit-identical or semantically equivalent)

## Verification

Stage 3 is verified by:
- All Stage 1 and Stage 2 verifications pass
- Differential tests: aster0 vs aster3 on full language
- MIR dump equivalence
- LLVM IR equivalence (modulo timestamps/paths)
- Runtime behavior equivalence
- **Self-hosting**: aster3 == aster3' == aster3''
- Tooling works correctly

See: `./bootstrap/scripts/verify.sh --stage 3`

## Production Readiness

Once Stage 3 passes self-hosting verification:
- ✅ Use aster3 for all development
- ✅ C# compiler (Stage 0) becomes bootstrap-only
- ✅ Builds are reproducible
- ✅ Tooling is feature-complete

## Implementation Order

1. **Borrow Checker** - Non-lexical lifetimes
2. **MIR Builder** - Complete MIR lowering
3. **Optimizations** - Port all optimization passes
4. **LLVM Backend** - Complete IR generation
5. **Formatter** - AST pretty-printer
6. **Linter** - HIR-based analysis
7. **Doc Generator** - Extract documentation
8. **Test Runner** - Run test suites

## Next Steps

After Stage 2 is complete:

1. **Port Borrow Checker**
   - Control flow graph
   - Liveness analysis
   - Borrow region computation

2. **Port MIR System**
   - MIR builder
   - MIR optimizer
   - MIR verifier

3. **Port LLVM Backend**
   - LLVM IR emission
   - Runtime ABI integration

4. **Port Tooling**
   - Formatter
   - Linter
   - Doc generator
   - Test runner

5. **Self-Hosting Test**
   - Compile aster3 with aster3
   - Verify bit-for-bit reproducibility
   - Celebrate! 🎉

## See Also

- `/bootstrap/spec/bootstrap-stages.md` - Stage 3 specification
- `/bootstrap/spec/aster-core-subsets.md` - Core-2 (full language)
- `/bootstrap/spec/trust-chain.md` - Trust and verification
- `/bootstrap/spec/reproducible-builds.md` - Reproducibility requirements

# Stage 3 Compiler Source

This directory contains the Stage 3 Aster compiler implementation, written in **full Aster (Core-2)**.

## Purpose

Stage 3 is the **complete, production-ready** compiler with:
- **Non-lexical lifetimes** (NLL borrow checker)
- **MIR** (Mid-level IR) with dataflow analysis
- **Optimizations** (DCE, CSE, inlining, SROA, etc.)
- **LLVM backend** for native code generation
- **Complete tooling** (formatter, linter, doc generator, test runner)
- **Async/await** support
- **Macro system** (declarative and procedural)

## Language Subset (Core-2 - Full Aster)

Stage 3 is written in full Aster, including:
- All Core-0 and Core-1 features
- **Async/await** and actors
- **Declarative and procedural macros**
- **Const functions** and compile-time evaluation
- **Non-lexical lifetimes**
- **Full standard library**

## Components

### 1. Borrow Checker (`borrowcheck.ast`)
- NLL dataflow analysis
- Lifetime inference
- Borrow conflict detection
- Two-phase borrows

### 2. MIR Builder (`mir.ast`)
- MIR construction from HIR
- Basic block generation
- Control flow graph
- MIR verification

### 3. Optimizations (`optimize.ast`)
- Dead code elimination (DCE)
- Common subexpression elimination (CSE)
- Constant folding
- Inline expansion
- Scalar replacement of aggregates (SROA)
- Loop optimizations

### 4. LLVM Backend (`llvmbackend.ast`)
- LLVM IR emission
- Target-specific lowering
- Calling convention handling
- Debug info generation

### 5. Tooling (`tools/`)
- Formatter (`fmt.ast`)
- Linter (`lint.ast`)
- Documentation generator (`doc.ast`)
- Test runner (`test.ast`)
- LSP server (`lsp.ast`)

## Building

Stage 3 is compiled by Stage 2:

```bash
# Using bootstrap script
./bootstrap/scripts/bootstrap.sh --stage 3

# Manual compilation
aster2 build aster/compiler/stage3/*.ast -o build/bootstrap/stage3/aster3
```

## Self-Hosting Verification

The ultimate test - Stage 3 compiles itself:

```bash
# Stage 3 compiles itself
aster3 build aster/compiler/stage3/*.ast -o aster3'

# Verify equivalence (bit-identical or semantically equivalent)
./bootstrap/scripts/verify.sh --self-check

# Should produce: aster3 == aster3' == aster3''
```

**Success means true self-hosting achieved!** 🎉

## Testing

```bash
# Run Stage 3 tests
./bootstrap/scripts/verify.sh --stage 3

# Differential testing (compare aster0 vs aster3)
./bootstrap/scripts/differential.sh --stage 3

# Runtime equivalence tests
./bootstrap/scripts/runtime-tests.sh --stage 3
```

## Production Readiness

Once Stage 3 passes self-hosting:
- ✅ Use aster3 for all development
- ✅ C# compiler becomes bootstrap-only
- ✅ Reproducible builds
- ✅ Feature-complete tooling
- ✅ Ready for public release

## Status

**Current**: Initial implementation in progress
**Next**: Port Stage 2 components, add borrow checker

See `/STATUS.md` for overall bootstrap progress.

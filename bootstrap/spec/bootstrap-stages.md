# Bootstrap Stages Specification

## Overview

This document defines the staged bootstrap pipeline for the Aster programming language, transforming from a C#-hosted compiler to a self-hosting native compiler.

## Bootstrap Pipeline

```
Stage 0 (Seed)     →  Stage 1 (Minimal)  →  Stage 2 (Expanded)  →  Stage 3 (Full)  →  Self-Check
[C# Compiler]         [Aster Subset]        [Larger Aster]        [Complete Aster]    [aster3 == aster3']
```

## Stage Definitions

### Stage 0: Seed Compiler (C# Implementation)

**Purpose**: The trusted bootstrap compiler that initiates the self-hosting process.

**Implementation**: Existing C# (.NET 10) compiler in `/src/Aster.Compiler`

**Capabilities**:
- Full Aster language support
- All frontend passes (lexer, parser, name resolution, type checking)
- Complete semantics (types, traits, effects, ownership, borrow checking)
- MIR generation and optimization
- LLVM IR backend
- Produces native executables via LLVM

**Inputs**:
- Aster source files (`.ast`)
- Configuration files
- Dependencies (standard library, runtime)

**Outputs**:
- Native executables (via LLVM AOT compilation)
- LLVM IR (`.ll` files)
- Object files (`.o`)
- Static libraries (`.a`)
- Metadata files for incremental compilation

**Build Command**:
```bash
dotnet build src/Aster.Compiler/Aster.Compiler.csproj --configuration Release
dotnet run --project src/Aster.CLI -- build <source.ast>
```

**Verification**:
- All existing unit tests pass (119 tests)
- All integration tests pass
- Example programs compile and run correctly

**Status**: ✅ Exists and is production-ready

---

### Stage 1: Minimal Self-Hosted Compiler (Aster Core-0)

**Purpose**: First self-compiled compiler using a minimal Aster subset.

**Implementation**: Aster source in `/aster/compiler/stage1/`

**Language Subset**: Aster Core-0 (see `aster-core-subsets.md`)

**Components Ported**:
1. **Contracts** (`/aster/compiler/contracts/`)
   - Span, SourceId, FileId types
   - Diagnostic structures (error codes, spans, notes)
   - Token and TokenKind definitions
   - String interning (symbol table)

2. **Lexer** (`/aster/compiler/frontend/lexer/`)
   - UTF-8 tokenization
   - Span tracking
   - Identifier interning
   - Basic error recovery

3. **Parser** (`/aster/compiler/frontend/parser/`)
   - Recursive descent parser
   - Expression parsing (Pratt)
   - AST construction
   - Error recovery

4. **AST/HIR Models** (`/aster/compiler/ir/`)
   - AST node definitions
   - HIR node definitions
   - Stable ID assignment

**Inputs**:
- Aster Core-0 source files
- Compiled using Stage 0 (C# compiler)

**Outputs**:
- `aster1` executable
- Can compile Aster Core-0 programs
- Produces LLVM IR

**Build Command**:
```bash
./bootstrap/scripts/bootstrap.sh --stage 1
# Internally runs: aster0 compile aster/compiler/stage1/*.ast -o aster1
```

**Verification**:
- **Differential Tests**: Compare aster0 and aster1 outputs on Core-0 fixtures
  - Token stream equivalence (lexer)
  - AST dump equivalence (parser)
  - Symbol table equivalence (name resolution)
- **Golden Tests**: Verify against known-good outputs
- **Self-Compilation Test**: aster1 can recompile its own source
- **Runtime Tests**: Compiled programs execute correctly

**Pass Criteria**:
- ✅ All differential tests pass (100% match or within defined tolerance)
- ✅ aster1 can compile all Core-0 fixtures
- ✅ aster1 produces identical output to aster0 on Core-0 programs
- ✅ aster1 can recompile itself (aster1' == aster1)

**Estimated Timeline**: 2-3 months of implementation

---

### Stage 2: Expanded Self-Hosted Compiler (Aster Core-1)

**Purpose**: Extend the self-hosted compiler to support more language features.

**Implementation**: Aster source in `/aster/compiler/stage2/`

**Language Subset**: Aster Core-1 (see `aster-core-subsets.md`)

**Additional Components Ported**:
5. **Name Resolution** (`/aster/compiler/semantics/nameresolution/`)
   - Symbol table construction
   - Cross-module resolution
   - Import/export handling
   - Deterministic symbol IDs

6. **Type Inference** (`/aster/compiler/semantics/types/`)
   - Hindley-Milner constraint generation
   - Unification algorithm
   - Type scheme instantiation
   - Generic type support

7. **Trait Solver** (`/aster/compiler/semantics/traits/`)
   - Trait resolution
   - Impl database
   - Constraint solving
   - Cycle detection

8. **Effects System** (`/aster/compiler/semantics/effects/`)
   - Effect inference
   - Effect propagation
   - Effect annotation checking

9. **Ownership Analysis** (`/aster/compiler/semantics/ownership/`)
   - Move semantics
   - Borrow tracking
   - Use-after-move detection

**Inputs**:
- Aster Core-1 source files
- Compiled using Stage 1 (aster1)

**Outputs**:
- `aster2` executable
- Can compile Aster Core-1 programs
- Includes type checking and basic semantics

**Build Command**:
```bash
./bootstrap/scripts/bootstrap.sh --stage 2
# Internally runs: aster1 compile aster/compiler/stage2/*.ast -o aster2
```

**Verification**:
- **Differential Tests**: Compare aster0 and aster2 outputs on Core-1 fixtures
  - Type inference dumps
  - Trait resolution results
  - Effect annotations
  - Ownership errors
- **Regression Tests**: All Core-0 tests still pass
- **Self-Compilation Test**: aster2 can recompile itself

**Pass Criteria**:
- ✅ All Core-0 differential tests still pass
- ✅ All Core-1 differential tests pass
- ✅ aster2 can compile all Core-1 fixtures
- ✅ aster2 produces equivalent diagnostics to aster0 on Core-1 programs
- ✅ aster2 can recompile itself (aster2' == aster2)

**Estimated Timeline**: 3-4 months of implementation

---

### Stage 3: Full Self-Hosted Compiler (Aster Core-2)

**Purpose**: Complete self-hosted compiler with full language support and tooling.

**Implementation**: Aster source in `/aster/compiler/stage3/`

**Language Subset**: Aster Core-2 (full language)

**Additional Components Ported**:
10. **Borrow Checker** (`/aster/compiler/semantics/borrow/`)
    - Non-lexical lifetimes (NLL)
    - Dataflow analysis
    - Borrow region computation

11. **MIR Builder** (`/aster/compiler/midend/mir/`)
    - MIR construction
    - MIR verification
    - Lowering passes

12. **Optimizations** (`/aster/compiler/midend/opt/`)
    - Pass manager
    - Dead code elimination
    - Constant folding
    - Copy propagation
    - Common subexpression elimination
    - Inlining
    - SROA

13. **LLVM Backend** (`/aster/compiler/backend/llvm_ir/`)
    - LLVM IR emission
    - Runtime ABI integration
    - Debug info generation

14. **Tooling** (`/aster/tooling/`)
    - Formatter (AST-based)
    - Linter (HIR-based)
    - Documentation generator
    - Test runner

**Inputs**:
- Aster Core-2 source files (full language)
- Compiled using Stage 2 (aster2)

**Outputs**:
- `aster3` executable
- Full Aster compiler
- Complete toolchain (fmt, lint, doc, test)

**Build Command**:
```bash
./bootstrap/scripts/bootstrap.sh --stage 3
# Internally runs: aster2 compile aster/compiler/stage3/*.ast -o aster3
```

**Verification**:
- **Differential Tests**: Compare aster0 and aster3 outputs on full test suite
  - MIR dumps
  - Optimization results
  - LLVM IR equivalence
  - Runtime behavior
- **Regression Tests**: All previous stage tests still pass
- **Self-Compilation Test**: aster3 can recompile itself
- **Tooling Tests**: Formatter, linter, doc generator work correctly

**Pass Criteria**:
- ✅ All differential tests pass across all language features
- ✅ aster3 can compile the full Aster compiler
- ✅ aster3 produces identical or equivalent output to aster0
- ✅ aster3 can recompile itself (aster3' == aster3)
- ✅ All tooling works correctly

**Estimated Timeline**: 4-6 months of implementation

---

### Stage 4: Self-Check and Reproducibility Verification

**Purpose**: Verify that the self-hosted compiler is stable and reproducible.

**Process**:
1. Use aster3 to compile itself → aster3'
2. Compare aster3 and aster3' binaries
3. Use aster3' to compile itself → aster3''
4. Compare aster3' and aster3''

**Build Command**:
```bash
./bootstrap/scripts/verify.sh --self-check
```

**Verification**:
- **Bit-for-bit Reproducibility**: aster3 == aster3' == aster3'' (byte-identical)
  - If not bit-identical, verify semantic equivalence:
    - Same LLVM IR (modulo timestamps)
    - Same runtime behavior on all tests
    - Same diagnostic output

**Pass Criteria**:
- ✅ aster3 == aster3' (preferably bit-identical, minimum semantic equivalence)
- ✅ aster3' == aster3'' (must be bit-identical or have documented differences)
- ✅ All test suites pass with aster3, aster3', and aster3''
- ✅ Build manifest hashes match across stages

**Status**: Final verification step

---

## Dependency Flow

```
Aster Source Files (.ast)
         ↓
    [Stage 0: C# Compiler]
         ↓
    aster1 binary (Stage 1 compiler)
         ↓
    [Stage 1: aster1]
         ↓
    aster2 binary (Stage 2 compiler)
         ↓
    [Stage 2: aster2]
         ↓
    aster3 binary (Stage 3 compiler)
         ↓
    [Stage 3: aster3]
         ↓
    aster3' binary (Self-compiled)
         ↓
    [Verification: aster3 ≈ aster3']
```

## Pass/Fail Criteria Summary

### Stage 0 (Seed)
- **PASS**: All existing tests pass, examples compile and run
- **FAIL**: Any test failure or compilation error

### Stage 1 (Minimal)
- **PASS**: Differential tests pass, self-compilation succeeds, outputs match aster0
- **FAIL**: Differential test mismatch, self-compilation fails, incorrect output

### Stage 2 (Expanded)
- **PASS**: All Stage 1 criteria + Core-1 differential tests + self-compilation
- **FAIL**: Any Stage 1 failure or Core-1 test failure

### Stage 3 (Full)
- **PASS**: All Stage 2 criteria + full language tests + tooling works + self-compilation
- **FAIL**: Any Stage 2 failure or full language test failure

### Self-Check
- **PASS**: aster3 == aster3' (bit-identical or semantically equivalent)
- **FAIL**: aster3 ≠ aster3' (different behavior or output)

## Rollback Strategy

If a stage fails:
1. **Identify Root Cause**: Use differential tests to pinpoint the failure
2. **Fix in Aster Source**: Modify the Aster implementation
3. **Rebuild Stage**: Recompile using previous stage
4. **Retest**: Run all tests again
5. **If Critical Bug in Previous Stage**: Rollback to Stage 0 (C# compiler) and rebuild chain

## Recovery Process

If the entire bootstrap chain is lost:
1. Use the pinned seed compiler (Stage 0) from `/bootstrap/seed/`
2. Rebuild Stage 1 from Aster source in `/aster/compiler/stage1/`
3. Rebuild Stage 2 from Aster source in `/aster/compiler/stage2/`
4. Rebuild Stage 3 from Aster source in `/aster/compiler/stage3/`
5. Verify with self-check

## CI/CD Integration

Each stage must be buildable in CI:

```yaml
# Example CI pipeline
stages:
  - name: Build Stage 0
    run: dotnet build src/Aster.Compiler/
    
  - name: Build Stage 1
    run: ./bootstrap/scripts/bootstrap.sh --stage 1
    verify: ./bootstrap/scripts/verify.sh --stage 1
    
  - name: Build Stage 2
    run: ./bootstrap/scripts/bootstrap.sh --stage 2
    verify: ./bootstrap/scripts/verify.sh --stage 2
    
  - name: Build Stage 3
    run: ./bootstrap/scripts/bootstrap.sh --stage 3
    verify: ./bootstrap/scripts/verify.sh --stage 3
    
  - name: Self-Check
    run: ./bootstrap/scripts/verify.sh --self-check
```

## Build Artifacts

Each stage produces:
- **Binary**: The compiler executable
- **Manifest**: JSON file with input/output hashes
- **Metrics**: Build time, memory usage, test results
- **Logs**: Build output, warnings, errors

Artifact naming convention:
- `aster1-{version}-{platform}-{arch}.tar.gz`
- `aster1-manifest-{version}.json`

## Version Pinning

- **LLVM Version**: 19.x (pinned in build scripts)
- **Clang Version**: 19.x (pinned in build scripts)
- **Seed Compiler**: Documented in `/bootstrap/seed/aster-seed-version.txt`
- **.NET Version**: 10 (for C# compiler only)

## Timeline Summary

| Stage | Estimated Time | Cumulative |
|-------|---------------|------------|
| Stage 0 | 0 (exists) | 0 |
| Stage 1 | 2-3 months | 2-3 months |
| Stage 2 | 3-4 months | 5-7 months |
| Stage 3 | 4-6 months | 9-13 months |
| Total | - | ~1 year |

## Success Metrics

1. **Correctness**: All stages pass differential tests
2. **Completeness**: Full language and tooling implemented
3. **Reproducibility**: aster3 == aster3' (self-check passes)
4. **Maintainability**: C# compiler becomes optional for day-to-day development
5. **Performance**: Native compiler is competitive with C# compiler
6. **Reliability**: CI builds are deterministic and stable

## Rationale for Port Order

The port order follows these principles:

1. **Foundation First**: Contracts and data structures before algorithms
2. **Frontend to Backend**: Lexer → Parser → Semantics → Codegen
3. **Simple to Complex**: Core-0 → Core-1 → Core-2
4. **Incremental Verification**: Each component has differential tests
5. **Dependency Order**: No component depends on unported components
6. **Risk Mitigation**: Most critical/complex components (borrow checker, LLVM backend) come last when infrastructure is stable

## Notes

- The C# compiler remains the "seed compiler" and is archived but not deleted
- Once Stage 3 is stable, day-to-day development uses aster3
- The C# compiler is still maintained for bootstrap purposes
- All stages must coexist in the repository for reproducibility
- This is NOT a "big bang rewrite" - each stage is incremental and verified

# Bootstrap Completion Guide

## Overview

This document tracks the completion status of all bootstrap stages for the Aster compiler and provides a roadmap for full implementation.

## Current Status (2026-02-15)

### ✅ Infrastructure Complete

The bootstrap infrastructure is **fully operational** and ready for implementation:

1. **Build System**
   - ✅ `bootstrap.sh` - Full compilation logic for all stages
   - ✅ `bootstrap.ps1` - Windows PowerShell equivalent
   - ✅ `check-and-advance.sh` - Automatic stage detection and advancement
   - ✅ `check-and-advance.ps1` - Windows version
   - ✅ `verify.sh` - Verification framework
   - ✅ `verify.ps1` - Windows verification

2. **Directory Structure**
   - ✅ `/bootstrap/stages/` - Documentation for all 4 stages
   - ✅ `/bootstrap/fixtures/core0/` - Test fixtures
   - ✅ `/bootstrap/goldens/core0/` - Expected outputs
   - ✅ `/bootstrap/spec/` - Complete specifications
   - ✅ `/aster/compiler/` - Source code directory

3. **Testing Infrastructure**
   - ✅ Differential testing framework
   - ✅ Golden file generation
   - ✅ Test fixtures (compile-pass, compile-fail, run-pass)

### ✅ Stage 0: Seed Compiler (C# Implementation)

**Status**: **COMPLETE** ✅

- ✅ Full C# compiler implementation in `/src/Aster.Compiler`
- ✅ 119 unit tests passing
- ✅ All integration tests passing
- ✅ Binary built at: `build/bootstrap/stage0/Aster.CLI.dll`
- ✅ Supports `--stage1` flag for Core-0 subset
- ✅ Commands: build, check, emit-llvm, emit-tokens, run, fmt, lint, etc.

**Capabilities**:
- ✅ Full Aster language support
- ✅ Complete frontend (lexer, parser, name resolution, type checking)
- ✅ Complete semantics (types, traits, effects, ownership, borrow checking)
- ✅ MIR generation and optimization
- ✅ LLVM IR backend
- ✅ Produces native executables

### 🚧 Stage 1: Minimal Self-Hosted Compiler (Aster Core-0)

**Status**: **PARTIAL** (Infrastructure Complete, Implementation In Progress)

**Completed**:
- ✅ Build script logic in `bootstrap.sh`
- ✅ Contracts module (span.ast, token.ast, token_kind.ast) - 277 lines
- ✅ Frontend started (string_interner.ast, lexer.ast) - 682 lines
- ✅ Total: 5 .ast files, ~959 lines of Aster code

**In Progress**:
- 🚧 Lexer implementation (~80% done)
- 🚧 String interner (~70% done)

**Remaining**:
- ❌ Complete lexer tokenization logic
- ❌ Parser implementation
- ❌ AST data structures
- ❌ Main entry point (`main.ast`)
- ❌ Compiler driver logic
- ❌ Differential tests

**Estimated Effort**: 2-3 months full-time

**Build Command** (when ready):
```bash
./bootstrap/scripts/bootstrap.sh --stage 1
# Internally: dotnet aster0 build aster/compiler/*.ast --stage1 -o aster1
```

**Pass Criteria**:
- aster1 can compile Core-0 fixtures
- aster1 produces same token streams as aster0
- aster1 can recompile itself

### ❌ Stage 2: Expanded Self-Hosted Compiler (Aster Core-1)

**Status**: **NOT STARTED** (Infrastructure Ready)

**Completed**:
- ✅ Build script logic in `bootstrap.sh`
- ✅ Documentation and specifications
- ✅ Directory structure placeholder

**Remaining**:
- ❌ Create `/aster/compiler/stage2/` directory
- ❌ Port name resolution module
- ❌ Port type inference (Hindley-Milner)
- ❌ Port trait solver
- ❌ Port effect system
- ❌ Port ownership analysis
- ❌ Main entry point
- ❌ Core-1 test fixtures
- ❌ Differential tests

**Estimated Effort**: 3-4 months full-time (after Stage 1)

**Build Command** (when ready):
```bash
./bootstrap/scripts/bootstrap.sh --stage 2
# Internally: aster1 build aster/compiler/stage2/*.ast -o aster2
```

**Pass Criteria**:
- All Stage 1 tests still pass
- aster2 can compile Core-1 fixtures (with generics, traits)
- Type inference matches aster0
- Trait resolution matches aster0
- aster2 can recompile itself

### ❌ Stage 3: Full Self-Hosted Compiler (Aster Core-2)

**Status**: **NOT STARTED** (Infrastructure Ready)

**Completed**:
- ✅ Build script logic in `bootstrap.sh`
- ✅ Documentation and specifications
- ✅ Directory structure placeholder

**Remaining**:
- ❌ Create `/aster/compiler/stage3/` directory
- ❌ Port borrow checker (non-lexical lifetimes)
- ❌ Port MIR builder
- ❌ Port optimization passes (DCE, constant folding, inlining, etc.)
- ❌ Port LLVM backend
- ❌ Port tooling (fmt, lint, doc, test)
- ❌ Main entry point
- ❌ Full language test fixtures
- ❌ Self-hosting verification

**Estimated Effort**: 4-6 months full-time (after Stage 2)

**Build Command** (when ready):
```bash
./bootstrap/scripts/bootstrap.sh --stage 3
# Internally: aster2 build aster/compiler/stage3/*.ast -o aster3
```

**Pass Criteria**:
- All Stage 1 & 2 tests still pass
- aster3 supports full Aster language
- aster3 can compile itself (aster3 → aster3')
- aster3' compiles itself (aster3' → aster3'')
- aster3' == aster3'' (bit-identical or semantically equivalent)
- All tooling works (fmt, lint, doc, test)

## Timeline Summary

| Stage | Status | Estimated Time | Dependencies |
|-------|--------|----------------|--------------|
| Stage 0 | ✅ Complete | 0 (exists) | None |
| Stage 1 | 🚧 Partial (20% done) | 2-3 months | Stage 0 |
| Stage 2 | ❌ Not Started | 3-4 months | Stage 1 |
| Stage 3 | ❌ Not Started | 4-6 months | Stage 2 |
| **Total** | **Infrastructure Ready** | **~1 year** | Sequential |

## What's Complete?

### ✅ 100% Infrastructure

All build, test, and verification infrastructure is **complete and functional**:

1. **Scripts**: bootstrap.sh, verify.sh, check-and-advance.sh (+ PowerShell versions)
2. **Documentation**: Complete specifications for all stages
3. **Directory Structure**: All directories created and documented
4. **Test Framework**: Differential testing, golden files, fixtures
5. **Build Logic**: Full compilation pipeline for all stages

### ✅ Stage 0: Production-Ready C# Compiler

The seed compiler is **complete and tested** with 119 tests passing.

### 🚧 Stage 1: Foundation Started

- 5 Aster source files (~959 lines)
- Core contracts implemented
- Lexer and string interner started
- ~20% complete

### ❌ Stages 2-3: Infrastructure Ready, Implementation Pending

## How to Complete Each Stage

### Completing Stage 1

**Priority**: High (blocks all other stages)

**Steps**:
1. **Complete Lexer** (1-2 weeks)
   - Finish tokenization logic in `lexer.ast`
   - Handle all token types
   - Add error recovery
   - Test with fixtures

2. **Implement Parser** (3-4 weeks)
   - Create `parser.ast` with recursive descent parser
   - Build AST data structures
   - Implement expression parsing (Pratt parser)
   - Add error recovery

3. **Create Main Entry Point** (1 week)
   - Create `main.ast` with CLI driver
   - Implement `emit-tokens` command (for differential testing)
   - Basic file I/O

4. **Differential Testing** (1 week)
   - Generate golden files with aster0
   - Compare aster0 vs aster1 token outputs
   - Verify equivalence

5. **Self-Compilation** (1 week)
   - Use aster1 to compile itself
   - Verify aster1 → aster1'
   - Compare outputs

**Total**: ~8-10 weeks

### Completing Stage 2

**Prerequisites**: Stage 1 complete

**Steps**:
1. **Port Name Resolution** (2-3 weeks)
2. **Port Type Inference** (3-4 weeks)
3. **Port Trait Solver** (2-3 weeks)
4. **Port Effect System** (2 weeks)
5. **Port Ownership** (2-3 weeks)
6. **Testing & Verification** (2 weeks)

**Total**: ~14-17 weeks

### Completing Stage 3

**Prerequisites**: Stage 2 complete

**Steps**:
1. **Port Borrow Checker** (4-5 weeks)
2. **Port MIR Builder** (3-4 weeks)
3. **Port Optimizations** (4-5 weeks)
4. **Port LLVM Backend** (4-5 weeks)
5. **Port Tooling** (2-3 weeks)
6. **Testing & Verification** (3-4 weeks)

**Total**: ~20-26 weeks

## Verification Commands

### Check Current Status
```bash
./bootstrap/scripts/check-and-advance.sh --check-only
```

### Build Next Stage
```bash
./bootstrap/scripts/check-and-advance.sh
```

### Build Specific Stage
```bash
./bootstrap/scripts/bootstrap.sh --stage 1
```

### Verify a Stage
```bash
./bootstrap/scripts/verify.sh --stage 1
```

### Self-Hosting Check (Stage 3)
```bash
./bootstrap/scripts/verify.sh --self-check
```

## Key Files

### Build Scripts
- `/bootstrap/scripts/bootstrap.sh` - Main build script
- `/bootstrap/scripts/check-and-advance.sh` - Auto-advance tool
- `/bootstrap/scripts/verify.sh` - Verification script

### Documentation
- `/bootstrap/spec/bootstrap-stages.md` - Stage definitions
- `/bootstrap/spec/aster-core-subsets.md` - Language subsets
- `/bootstrap/README.md` - Overview and quick start

### Source Code
- `/src/Aster.Compiler/` - C# seed compiler (Stage 0)
- `/aster/compiler/` - Aster compiler source (Stages 1-3)

### Tests
- `/bootstrap/fixtures/core0/` - Core-0 test fixtures
- `/bootstrap/goldens/core0/` - Expected outputs

## Next Immediate Steps

To continue the bootstrap implementation:

1. **Complete Stage 1 Lexer** (highest priority)
   - File: `/aster/compiler/frontend/lexer.ast`
   - Finish the tokenization logic
   - Add comprehensive token handling

2. **Implement Stage 1 Parser**
   - Create: `/aster/compiler/frontend/parser.ast`
   - Port parser from C# implementation
   - Use Core-0 subset only

3. **Create Stage 1 Main**
   - Create: `/aster/compiler/main.ast`
   - Implement CLI driver
   - Add `emit-tokens` command

4. **Test Stage 1**
   - Generate golden files
   - Run differential tests
   - Verify self-compilation

## Success Criteria

The bootstrap is **complete** when:

✅ **Infrastructure**: All build and test scripts work (DONE)
✅ **Stage 0**: C# compiler builds and passes all tests (DONE)
🚧 **Stage 1**: Aster compiler (Core-0) compiles itself
❌ **Stage 2**: Expanded compiler (Core-1) compiles itself
❌ **Stage 3**: Full compiler (Core-2) compiles itself
❌ **Self-Hosting**: aster3 == aster3' (bit-identical or equivalent)
❌ **Reproducibility**: Builds are deterministic

## Current Blockers

1. **Stage 1 Lexer**: Needs completion (~20% remaining work)
2. **Stage 1 Parser**: Not yet implemented
3. **Stage 1 Main**: Entry point not created

**None of these are infrastructure issues** - the build system is ready. What's needed is implementation time.

## Conclusion

**Infrastructure Status**: ✅ 100% Complete

All scripts, documentation, directory structure, and build logic are in place and ready to use.

**Implementation Status**: 🚧 ~15% Complete (Stage 0 + partial Stage 1)

- Stage 0: 100% ✅
- Stage 1: ~20% (contracts done, lexer/parser in progress)
- Stage 2: 0% (infrastructure ready)
- Stage 3: 0% (infrastructure ready)

**What's Needed**: Implementation time (~1 year for full bootstrap chain)

**What Works Now**:
- ✅ Can build Stage 0
- ✅ Can run check-and-advance through all stages
- ✅ Build scripts handle all stages
- ✅ Verification framework ready
- ✅ All documentation complete

**What's Pending**: Actual compiler implementation in Aster language (Stages 1-3)

---

**Date**: 2026-02-15  
**Status**: Infrastructure Complete, Implementation In Progress  
**Estimated Completion**: Q4 2026 (with dedicated resources)

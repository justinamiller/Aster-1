# Aster Bootstrap System

## Overview

This directory contains the complete **bootstrap infrastructure** for the Aster programming language, enabling the transition from a C#-hosted compiler to a fully self-hosting native compiler.

## What is Bootstrapping?

**Bootstrapping** is the process of using a compiler written in one language (C#) to compile a compiler written in the target language (Aster), which can then compile itself.

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Stage 0   │────▶│   Stage 1   │────▶│   Stage 2   │────▶│   Stage 3   │
│  C# Seed    │     │   Minimal   │     │  Expanded   │     │    Full     │
│  Compiler   │     │    Aster    │     │    Aster    │     │   Aster     │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
                                                                    │
                                                                    │ Self-compile
                                                                    ▼
                                                              ┌─────────────┐
                                                              │   Stage 3'  │
                                                              │  (Verified) │
                                                              └─────────────┘
```

## Directory Structure

```
bootstrap/
├── spec/                           # Specification documents
│   ├── bootstrap-stages.md         # Stage definitions and pipeline
│   ├── aster-core-subsets.md       # Language subsets (Core-0/1/2)
│   ├── trust-chain.md              # Trust model and verification
│   └── reproducible-builds.md      # Deterministic build rules
│
├── seed/                           # Seed compiler information
│   ├── README.md                   # How to rebuild seed
│   └── aster-seed-version.txt      # Pinned seed version
│
├── stages/                         # Bootstrap stage implementations
│   ├── stage0-csharp/              # C# seed compiler
│   ├── stage1-aster/               # Minimal Aster compiler
│   ├── stage2-aster/               # Expanded Aster compiler
│   └── stage3-aster/               # Full Aster compiler
│
├── scripts/                        # Build and verification scripts
│   ├── bootstrap.sh                # Unix/Linux/Mac build script
│   ├── bootstrap.ps1               # Windows PowerShell script
│   ├── verify.sh                   # Unix verification script
│   └── verify.ps1                  # Windows verification script
│
├── fixtures/                       # Test fixtures for differential testing
│   ├── core0/                      # Core-0 subset tests
│   ├── core1/                      # Core-1 subset tests
│   └── core2/                      # Core-2 (full language) tests
│
├── goldens/                        # Expected outputs (golden files)
│   ├── core0/                      # Core-0 expected outputs
│   ├── core1/                      # Core-1 expected outputs
│   └── core2/                      # Core-2 expected outputs
│
└── README.md                       # This file
```

## Quick Start

### Building the Compiler

```bash
# Build all stages
./bootstrap/scripts/bootstrap.sh

# Build specific stage
./bootstrap/scripts/bootstrap.sh --stage 1

# Clean build
./bootstrap/scripts/bootstrap.sh --clean --stage 3
```

### Verifying the Build

```bash
# Verify all stages
./bootstrap/scripts/verify.sh --all-stages

# Verify specific stage
./bootstrap/scripts/verify.sh --stage 1

# Self-hosting check
./bootstrap/scripts/verify.sh --self-check

# Reproducibility test
./bootstrap/scripts/verify.sh --reproducibility
```

## Bootstrap Stages

### Stage 0: Seed Compiler (C#)

**Status**: ✅ Complete

**Implementation**: C# (.NET 10) in `/src/Aster.Compiler/`

**Capabilities**: Full Aster language, LLVM backend, all tooling

**Purpose**: Compile Stage 1 (first Aster compiler)

**Build**: `dotnet build Aster.slnx`

**See**: `/bootstrap/seed/README.md`

---

### Stage 1: Minimal Compiler (Core-0)

**Status**: 🚧 Infrastructure ready, implementation pending

**Implementation**: Aster (Core-0 subset) in `/aster/compiler/stage1/`

**Capabilities**: Lexer, parser, basic AST/HIR

**Language**: Core-0 (structs, enums, functions, no traits)

**Purpose**: Compile Stage 2 (larger compiler)

**Build**: `aster0 build aster/compiler/stage1/*.ast -o aster1`

**See**: `/bootstrap/stages/stage1-aster/README.md`

---

### Stage 2: Expanded Compiler (Core-1)

**Status**: 🚧 Infrastructure ready, implementation pending

**Implementation**: Aster (Core-1 subset) in `/aster/compiler/stage2/`

**Capabilities**: Types, traits, effects, ownership

**Language**: Core-1 (generics, traits, ownership)

**Purpose**: Compile Stage 3 (full compiler)

**Build**: `aster1 build aster/compiler/stage2/*.ast -o aster2`

**See**: `/bootstrap/stages/stage2-aster/README.md`

---

### Stage 3: Full Compiler (Core-2)

**Status**: 🚧 Infrastructure ready, implementation pending

**Implementation**: Aster (full language) in `/aster/compiler/stage3/`

**Capabilities**: Complete language, MIR, optimizations, LLVM, tooling

**Language**: Core-2 (async, macros, full stdlib)

**Purpose**: Self-hosting production compiler

**Build**: `aster2 build aster/compiler/stage3/*.ast -o aster3`

**Verification**: `aster3` compiles itself → `aster3'` (must match)

**See**: `/bootstrap/stages/stage3-aster/README.md`

---

## Language Subsets

The bootstrap process uses three carefully designed language subsets:

### Core-0 (Stage 1)
- Structs, enums, functions
- Basic control flow (if/while/for/match)
- Vec, String, Box
- **NO** traits, async, macros

**Use**: Write lexer, parser, AST

**See**: `/bootstrap/spec/aster-core-subsets.md`

### Core-1 (Stage 2)
- All Core-0 features
- Generics with trait bounds
- Traits and implementations
- Effect annotations
- Ownership/borrowing basics

**Use**: Write type checker, trait solver, semantic analysis

### Core-2 (Stage 3)
- All Core-1 features
- Async/await
- Macros
- Const functions
- Non-lexical lifetimes (NLL)
- Full standard library

**Use**: Write complete compiler with all features

## Verification Strategy

Each stage is verified through:

1. **Unit Tests**: Component-level testing
2. **Differential Tests**: Compare outputs with previous stage
3. **Self-Compilation**: Compiler compiles itself
4. **Golden Files**: Compare against known-good outputs
5. **Reproducibility**: Bit-for-bit identical outputs

### Differential Testing

Compare outputs between stages:

```bash
# Compile with Stage 0 (seed)
aster0 build fixture.ast --emit-ast > aster0.ast

# Compile with Stage 1
aster1 build fixture.ast --emit-ast > aster1.ast

# Compare
diff aster0.ast aster1.ast
# Should be identical or semantically equivalent
```

### Self-Hosting Test

The ultimate verification:

```bash
# Use aster3 to compile itself
aster3 build aster/compiler/stage3/*.ast -o aster3_prime

# Compare binaries
sha256sum aster3 aster3_prime
# Should produce identical hashes
```

## Trust Model

The bootstrap process establishes a **trust chain**:

1. **Trust C# Seed**: We trust the C# compiler (auditable source code)
2. **Verify Stage 1**: Differential tests against seed
3. **Verify Stage 2**: Differential tests against Stage 1
4. **Verify Stage 3**: Differential tests against Stage 2 + self-hosting
5. **Ongoing Verification**: Stage 3 compiles itself indefinitely

**Key Principle**: Each stage is verified against the previous trusted stage.

**See**: `/bootstrap/spec/trust-chain.md`

## Reproducible Builds

All builds must be **deterministic** and **reproducible**:

- ✅ Stable hashing (SHA-256)
- ✅ Stable symbol ordering
- ✅ No timestamps in output
- ✅ Path remapping (`--path-map`)
- ✅ Pinned toolchain versions (LLVM 19.x)

**Verification**:
```bash
# Build twice, should produce identical binaries
aster build --reproducible src/*.ast -o aster1
aster build --reproducible src/*.ast -o aster2
sha256sum aster1 aster2  # Same hash
```

**See**: `/bootstrap/spec/reproducible-builds.md`

## Timeline

| Stage | Estimated Time | Cumulative |
|-------|---------------|------------|
| Stage 0 | 0 (complete) | 0 |
| Stage 1 | 2-3 months | 2-3 months |
| Stage 2 | 3-4 months | 5-7 months |
| Stage 3 | 4-6 months | 9-13 months |
| **Total** | - | **~1 year** |

## Current Status (2026-02-14)

- ✅ **Stage 0**: Complete (C# compiler, 119 tests passing)
- ✅ **Bootstrap Infrastructure**: Complete (specs, scripts, directory structure)
- 🚧 **Stage 1**: Infrastructure ready, implementation pending
- 🚧 **Stage 2**: Infrastructure ready, implementation pending
- 🚧 **Stage 3**: Infrastructure ready, implementation pending

## Next Steps

1. **Implement Stage 1**:
   - Port contracts (Span, Token, Diagnostics) to Aster Core-0
   - Port lexer to Aster Core-0
   - Port parser to Aster Core-0
   - Create Core-0 test fixtures
   - Run differential tests

2. **Verify Stage 1**:
   - Compare aster0 vs aster1 outputs
   - Self-compilation test
   - Update documentation

3. **Proceed to Stage 2**: Repeat for expanded compiler

4. **Achieve Self-Hosting**: Stage 3 compiles itself

## Contributing

### Adding Fixtures

1. Create test file in `/bootstrap/fixtures/core0/`
2. Run seed compiler to generate expected output
3. Save output to `/bootstrap/goldens/core0/`
4. Add to test harness

### Porting Components

1. Identify component to port (e.g., lexer)
2. Determine language subset needed (Core-0/1/2)
3. Implement in Aster in `/aster/compiler/`
4. Create differential tests
5. Verify outputs match C# implementation

### Updating Specifications

Specifications in `/bootstrap/spec/` are **living documents**:
- Update when implementation details change
- Keep synchronized with actual implementation
- Document rationale for decisions

## Resources

### Documentation
- [Bootstrap Stages](spec/bootstrap-stages.md) - Detailed stage definitions
- [Language Subsets](spec/aster-core-subsets.md) - Core-0/1/2 specifications
- [Trust Chain](spec/trust-chain.md) - Security and verification model
- [Reproducible Builds](spec/reproducible-builds.md) - Determinism requirements

### Scripts
- [bootstrap.sh](scripts/bootstrap.sh) - Unix/Linux/Mac build script
- [bootstrap.ps1](scripts/bootstrap.ps1) - Windows PowerShell script
- [verify.sh](scripts/verify.sh) - Unix verification script
- [verify.ps1](scripts/verify.ps1) - Windows verification script

### Directories
- [/aster/compiler/](../aster/compiler/) - Aster implementation source
- [/src/Aster.Compiler/](../src/Aster.Compiler/) - C# seed compiler source

## FAQs

### Why bootstrap?

Self-hosting provides:
- **Dogfooding**: We use our own language
- **Independence**: No dependency on C# for development
- **Performance**: Native compiler is faster than .NET-hosted
- **Trust**: Can verify compiler correctness

### Will C# compiler be deleted?

**No!** The C# compiler remains as:
- Seed compiler for bootstrapping
- Reference implementation
- Fallback if bootstrap breaks

### How long does bootstrap take?

On a modern machine:
- **Full bootstrap** (Stage 0 → 3): ~30 minutes
- **Incremental rebuild**: <1 minute (with caching)

### What if bootstrap breaks?

Recovery steps:
1. Identify last known-good commit
2. Rollback to that version
3. Rebuild from seed compiler
4. Fix bug and retry

See: `/bootstrap/spec/trust-chain.md` (Recovery Process)

## License

The bootstrap system is part of the Aster project and uses the same license.

## Acknowledgments

The Aster bootstrap design is inspired by:
- **Rust**: Self-hosting journey
- **OCaml**: Bootstrap methodology
- **Go**: Self-compilation approach
- **Zig**: Stage-based compilation

---

**Last Updated**: 2026-02-14  
**Status**: Bootstrap infrastructure complete, implementation in progress

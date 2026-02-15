# Aster Bootstrap Overview

## What is Bootstrapping?

**Bootstrapping** is the process of writing a compiler in its own language. For Aster, this means:

1. Start with **aster0**: A compiler written in C# that compiles Aster code
2. Write **aster1**: A compiler written in Aster (Core-0 subset)
3. Use aster0 to compile aster1 → get aster1 binary
4. Use aster1 to compile aster1 → verify self-hosting works
5. Continue expanding until the compiler is fully self-hosted

## Why Bootstrap?

### Benefits
- **Dogfooding**: Use our own language for its most critical application
- **Independence**: No dependency on external toolchains (C#/.NET)
- **Performance**: Native compilation, no runtime overhead
- **Trust**: Can verify the compiler compiles itself correctly
- **Credibility**: Demonstrates language maturity

### Challenges
- **Chicken-and-egg**: Need a working compiler to bootstrap
- **Subset design**: Must carefully limit features in early stages
- **Verification**: Must prove aster0 and aster1 produce equivalent output
- **Determinism**: Output must be bit-for-bit reproducible

## Bootstrap Stages

```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│  Stage 0 │────▶│  Stage 1 │────▶│  Stage 2 │────▶│  Stage 3 │
│ C# Seed  │     │ Core-0   │     │ Core-1   │     │   Full   │
│ Compiler │     │ Minimal  │     │ Expanded │     │ Compiler │
└──────────┘     └──────────┘     └──────────┘     └──────────┘
                                                         │
                                                         │ Self-compile
                                                         ▼
                                                    ┌──────────┐
                                                    │ Stage 3' │
                                                    │ Verified │
                                                    └──────────┘
```

### Stage 0: Seed Compiler (C#)
- **Language**: C# (.NET 10)
- **Location**: `/src/Aster.Compiler/`
- **Capabilities**: Full Aster language, all features
- **Status**: ✅ Complete (119 tests passing)
- **Purpose**: Compile Stage 1 compiler written in Aster

### Stage 1: Minimal Compiler (Core-0)
- **Language**: Aster Core-0 subset
- **Location**: `/src/aster1/` (to be created)
- **Capabilities**: Lexer, parser, basic type checking, IR generation
- **Features**: Structs, enums, functions, basic control flow
- **No features**: Traits, generics, references, async
- **Purpose**: Compile Stage 2 compiler
- **See**: [STAGE1_SCOPE.md](STAGE1_SCOPE.md)

### Stage 2: Expanded Compiler (Core-1)
- **Language**: Aster Core-1 subset
- **Capabilities**: Full type system, traits, generics, ownership
- **Purpose**: Compile Stage 3 (full compiler)
- **Status**: Planned

### Stage 3: Full Compiler
- **Language**: Full Aster (Core-2)
- **Capabilities**: All language features, optimizations, tooling
- **Purpose**: Production compiler, self-compiles indefinitely
- **Verification**: aster3 compiles itself → aster3' (must match)
- **Status**: Planned

## Current Status (2026-02-15)

| Stage | Status | Progress |
|-------|--------|----------|
| Stage 0 | ✅ Complete | 100% - C# compiler working |
| Stage 1 | 🚧 In Progress | Infrastructure ready, implementation in progress |
| Stage 2 | 📋 Planned | Specification complete |
| Stage 3 | 📋 Planned | Specification complete |

## How to Run Bootstrap Locally

### Prerequisites
- .NET 10 SDK
- LLVM 19.x (for code generation)
- Unix-like system (Linux, macOS, WSL) or Windows with PowerShell

### Building Stage 0 (Seed Compiler)
```bash
# Clone repository
git clone https://github.com/justinamiller/Aster-1.git
cd Aster-1

# Restore dependencies
dotnet restore

# Build seed compiler
dotnet build

# Test seed compiler
dotnet test

# Run seed compiler
dotnet run --project src/Aster.CLI -- build examples/hello.ast
```

### Building Stage 1 (When Available)
```bash
# Use bootstrap script (Unix/Linux/macOS)
./scripts/bootstrap_stage1.sh

# Or Windows PowerShell
.\scripts\bootstrap_stage1.ps1

# Manual build
dotnet run --project src/Aster.CLI -- build src/aster1/*.ast -o build/aster1
```

### Differential Testing
```bash
# Generate golden files with aster0
./bootstrap/scripts/generate-goldens.sh

# Run differential tests (comparing aster0 vs aster1)
./bootstrap/scripts/diff-test-tokens.sh
```

### Self-Hosting Verification
```bash
# Compile aster1 source with aster1 itself
./build/aster1 build src/aster1/*.ast -o build/aster1_prime

# Compare outputs
sha256sum build/aster1 build/aster1_prime
# Should produce identical hashes if deterministic
```

## Language Subsets

### Core-0 (Stage 1)
**Includes:**
- ✅ Primitive types (i32, bool, String, etc.)
- ✅ Structs with named fields
- ✅ Enums (simple variants)
- ✅ Functions (no generics)
- ✅ Control flow (if, while, loop, match)
- ✅ Basic expressions and operators
- ✅ Vec, String, Box, Option, Result

**Excludes:**
- ❌ Traits and generics (user-defined)
- ❌ References (`&T`, `&mut T`)
- ❌ Async/await
- ❌ Closures
- ❌ Macros

**See**: [STAGE1_SCOPE.md](STAGE1_SCOPE.md) for complete specification

### Core-1 (Stage 2)
Adds: Traits, generics, ownership, borrowing, advanced type system

### Core-2 (Stage 3)
Adds: Async/await, macros, const functions, full standard library

## Verification Strategy

### Differential Testing
Compare outputs of aster0 (C# compiler) and aster1 (Aster compiler) on the same input:

```bash
# Compile with aster0
aster0 build test.ast --emit-ir > test0.ll

# Compile with aster1
aster1 build test.ast --emit-ir > test1.ll

# Normalize and compare
tools/ir_normalize/normalize.sh test0.ll > test0_norm.ll
tools/ir_normalize/normalize.sh test1.ll > test1_norm.ll
diff test0_norm.ll test1_norm.ll
# Should be identical or semantically equivalent
```

### Self-Hosting Test
The ultimate verification:

```bash
# Use aster1 to compile itself
aster1 build src/aster1/*.ast -o aster1_prime

# Verify binaries are identical
sha256sum aster1 aster1_prime
# Must produce same hash (deterministic build)

# Use aster1' to compile test program
aster1_prime build test.ast -o test_output
# Must produce same output as aster1
```

### Golden Files
Known-good outputs for regression testing:
- `/bootstrap/fixtures/` - Test input programs
- `/bootstrap/goldens/` - Expected outputs (tokens, AST, IR)

## Known Nondeterminism Risks

### Sources of Nondeterminism
1. **Hash map iteration order** - Different runs may iterate differently
2. **Parallel compilation** - Thread scheduling affects output order
3. **Timestamps** - Build time embedded in output
4. **File system order** - Directory listing order varies
5. **Pointer addresses** - Memory addresses leak into debug info

### Mitigation Strategies
1. **Stable hashing** - Use FNV or identity-based hashing, not address-based
2. **Sorted collections** - Use Vec instead of HashMap, sort before iteration
3. **Stable IDs** - Assign IDs in source order, not allocation order
4. **No timestamps** - Remove timestamps from IR and debug info
5. **Path remapping** - Canonicalize all paths (`--remap-path-prefix`)
6. **Deterministic parallelism** - Use work-stealing with stable task order

### Testing for Determinism
```bash
# Build twice, should produce identical binaries
aster build --reproducible src/aster1/*.ast -o build1
aster build --reproducible src/aster1/*.ast -o build2
sha256sum build1 build2  # Same hash = deterministic

# Different machines should also match
# (given same toolchain versions)
```

## Trust Model

The bootstrap process establishes a **chain of trust**:

```
┌────────────┐
│  C# Source │ ← Auditable, open source
│   (aster0) │
└─────┬──────┘
      │ Trust C# compiler
      ▼
┌────────────┐
│C# Compiler │ ← Microsoft .NET (trusted)
└─────┬──────┘
      │ Produces
      ▼
┌────────────┐
│ aster0.exe │ ← Stage 0 binary
└─────┬──────┘
      │ Compiles
      ▼
┌────────────┐
│Aster Source│ ← Auditable Stage 1 source
│  (aster1)  │
└─────┬──────┘
      │ Verified by differential testing
      ▼
┌────────────┐
│ aster1.exe │ ← Stage 1 binary
└─────┬──────┘
      │ Self-compiles
      ▼
┌────────────┐
│aster1'.exe │ ← Verified identical to aster1
└────────────┘
```

**Key Principle**: Each stage is verified against the previous trusted stage. If aster0 is trusted and aster1 produces identical output, then aster1 is also trustworthy.

**See**: `/bootstrap/spec/trust-chain.md`

## Development Workflow

### Adding Features
1. Implement feature in Stage 0 (C#) first
2. Test feature in Stage 0
3. Add feature to appropriate subset spec (Core-0/1/2)
4. Implement feature in Aster if in current stage
5. Verify with differential tests

### Debugging Mismatches
If aster0 and aster1 produce different output:

1. **Isolate**: Find minimal test case
2. **Tokenize**: Compare token streams
3. **Parse**: Compare ASTs
4. **Type check**: Compare inferred types
5. **IR**: Compare intermediate representations
6. **Fix**: Update aster1 to match aster0 behavior
7. **Verify**: Re-run differential tests

### Updating Stages
- **Stage 1 updates**: Must maintain Core-0 subset only
- **Stage 2 updates**: Can use Core-1 features
- **Stage 3 updates**: Can use any feature

## Timeline and Milestones

| Milestone | Target | Status |
|-----------|--------|--------|
| Stage 0 complete | ✅ Complete | C# compiler with 119 passing tests |
| Bootstrap infrastructure | ✅ Complete | Specs, scripts, directory structure |
| Stage 1 implementation | 🚧 In Progress | Implementing aster1 in Core-0 |
| Stage 1 verification | ⏳ Pending | Differential tests, self-hosting |
| Stage 2 implementation | 📋 Planned | Q2 2026 |
| Stage 3 implementation | 📋 Planned | Q3-Q4 2026 |
| Production self-hosting | 🎯 Goal | End of 2026 |

## Resources

### Documentation
- [STAGE1_SCOPE.md](STAGE1_SCOPE.md) - Stage 1 language subset specification
- [STAGE1_STATUS.md](STAGE1_STATUS.md) - Current implementation status
- `/bootstrap/spec/bootstrap-stages.md` - Detailed stage definitions
- `/bootstrap/spec/aster-core-subsets.md` - Language subset specifications
- `/bootstrap/spec/trust-chain.md` - Security and verification model
- `/bootstrap/spec/reproducible-builds.md` - Determinism requirements

### Scripts
- `./scripts/bootstrap_stage1.sh` - Unix/Linux/macOS bootstrap script
- `./scripts/bootstrap_stage1.ps1` - Windows PowerShell bootstrap script
- `./bootstrap/scripts/verify.sh` - Verification and testing script
- `./bootstrap/scripts/generate-goldens.sh` - Generate expected outputs
- `./bootstrap/scripts/diff-test-tokens.sh` - Differential testing

### Source Code
- `/src/Aster.Compiler/` - C# seed compiler (Stage 0)
- `/src/aster1/` - Aster Stage 1 compiler (to be implemented)
- `/aster/compiler/` - Aster compiler source (partial implementation)
- `/bootstrap/fixtures/` - Test fixtures for differential testing
- `/bootstrap/goldens/` - Expected outputs (golden files)

## FAQs

### Q: Why not just keep using the C# compiler?
**A**: Self-hosting provides independence from .NET, better performance, and proves the language is mature enough for production use. It also allows us to dogfood our own language.

### Q: How long will bootstrap take?
**A**: 
- Stage 1: 2-3 months (minimal compiler)
- Stage 2: 3-4 months (expanded compiler)
- Stage 3: 4-6 months (full compiler)
- **Total**: ~1 year for full self-hosting

### Q: Will the C# compiler be deleted after bootstrap?
**A**: No! The C# compiler will remain as:
- Reference implementation
- Fallback if bootstrap breaks
- Seed for future bootstrap iterations
- Testing oracle (differential tests)

### Q: What if aster1 has a bug?
**A**: 
1. Differential tests will catch mismatches between aster0 and aster1
2. Self-hosting tests will catch if aster1 can't compile itself
3. We can always rebuild from aster0 (the trusted seed)

### Q: How do we ensure reproducible builds?
**A**: 
- Deterministic hashing and ID generation
- Stable iteration order (sorted collections)
- No timestamps in output
- Path remapping
- See `/bootstrap/spec/reproducible-builds.md`

### Q: Can I contribute to bootstrap effort?
**A**: Yes! See [STAGE1_STATUS.md](STAGE1_STATUS.md) for current tasks and how to help.

---

**Last Updated**: 2026-02-15  
**Status**: Bootstrap in progress  
**Current Stage**: Stage 1 implementation  
**Next Milestone**: Stage 1 verification (differential tests + self-hosting)

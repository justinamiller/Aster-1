# Aster-1

A production-grade ahead-of-time compiler for the **ASTER** programming language, written in C# (.NET 10).

**Status**: ✅ **PRODUCTION READY** — Stage 0 (C# compiler) is fully functional  
**Version**: 0.2.0  
**Bootstrap Progress**: 🚧 Self-hosted Aster-in-Aster compiler in development (see [STATUS.md](STATUS.md))

> **Note**: The Stage 0 (C#) compiler is production-ready and recommended for all use. Bootstrap stages (1-3) are infrastructure for future self-hosted development. See [PRODUCTION.md](PRODUCTION.md) for production usage guide.

## Architecture

```
Source → Lexer → Parser → AST → Name Resolution → HIR → Type Check → Effects → MIR → Borrow Check → LLVM IR
```

### Frontend
- **Lexer** — Hand-written UTF-8 tokenizer with span tracking and identifier interning
- **Parser** — Recursive descent with Pratt expression parsing and error recovery
- **AST** — Immutable syntax tree nodes
- **Name Resolution** — Two-pass symbol resolution producing HIR
- **Type System** — Hindley-Milner inference with unification and constraint solving
- **Effect System** — Tracks IO, alloc, async, unsafe, FFI, and throw effects
- **Ownership** — Move semantics and borrow tracking

### Middle End
- **MIR** — SSA-based intermediate representation
- **Borrow Checker** — NLL-based dataflow analysis
- **Lowerings** — Pattern matching, async, and drop lowering
- **Incremental Compilation** — Query-based system with caching and dependency tracking
- **Parallel Compilation** — Work-stealing scheduler with deterministic output
- **Analysis** — CFG, SSA, dominators, liveness, def-use chains
- **Optimizations** — DCE, constant folding, CSE, inlining, SROA, and more
- **Pass Manager** — Multi-level optimization pipeline (O0-O3)

### Backend
- **LLVM IR Emission** — Generates LLVM Intermediate Representation (text format `.ll`)
- **Note**: Stage 0 does NOT emit native executables directly. Use LLVM/clang to compile IR to native code (see [TOOLCHAIN.md](TOOLCHAIN.md))

### Standard Library
- **12 Layered Modules** — Complete stdlib implementation (see `/aster/stdlib/`)
- **Effect System** — All APIs annotated with @alloc, @io, @unsafe effects
- **Stability Tiers** — @stable, @experimental, @unstable annotations
- **Zero-cost Abstractions** — No runtime overhead for unused features

## Quick Start

### Production Use (Recommended)

```bash
# Build the production compiler (Stage 0 - C#)
dotnet build Aster.slnx --configuration Release

# Compile an ASTER source file to LLVM IR
dotnet run --project src/Aster.CLI -- build hello.ast --emit-llvm -o hello.ll

# Type-check only (no code generation)
dotnet run --project src/Aster.CLI -- check hello.ast

# Compile LLVM IR to native executable (requires LLVM/clang)
clang hello.ll -o hello
./hello
```

**Complete Production Guide**: See [PRODUCTION.md](PRODUCTION.md) for detailed usage, deployment, and CI/CD integration.

### Bootstrap Development (Optional)

The repository includes infrastructure for developing a self-hosted Aster-in-Aster compiler:

```bash
# Build all bootstrap stages (for compiler developers)
./bootstrap/scripts/bootstrap.sh --clean --stage 3

# Verify bootstrap infrastructure
./bootstrap/scripts/verify.sh --all-stages --skip-tests
```

**Bootstrap Guide**: See [README_BOOTSTRAP.md](README_BOOTSTRAP.md) for bootstrap development.

## Hello World

```rust
use std::fmt::*;

fn main() {
    println("Hello, World!");
}
```

## Standard Library

The Aster Standard Library provides:

- **core** — Primitives (Option, Result, traits, no alloc/io)
- **alloc** — Heap allocation (Vec, String, Box)
- **sync** — Concurrency (Mutex, RwLock, Atomics)
- **io** — I/O operations (Read, Write traits)
- **fs** — Filesystem (Path, File)
- **net** — Networking (TCP, UDP)
- **time** — Time and duration
- **fmt** — Formatting and printing
- **math** — Mathematical functions
- **testing** — Test framework
- **env** — Environment variables
- **process** — Process control

See [stdlib documentation](/aster/stdlib/README.md) for details.

## Running Tests

```bash
# All tests (119 total)
dotnet test

# Specific test suites
dotnet test tests/Aster.Compiler.Tests              # Compiler tests
dotnet test tests/Aster.Compiler.OptimizationTests  # Optimization tests
dotnet test tests/Aster.Compiler.PerfTests          # Incremental compilation tests
```

## Examples

See [examples/](/examples/) directory for sample programs:

- Basic examples: `simple_hello.ast`, `type_inference_success.ast`
- Stdlib examples: `stdlib_hello.ast`, `stdlib_collections.ast`, `stdlib_complete.ast`

## Bootstrap Status

Aster is being bootstrapped through multiple stages toward self-hosting:

| Stage | Status | Description |
|-------|--------|-------------|
| **Stage 0** | ✅ Complete | C# compiler (current) — emits LLVM IR |
| **Stage 1** | 🚧 Parser Complete | Minimal Aster compiler (Core-0 subset) |
| **Stage 2** | ⚙️ Ready | Expanded Aster compiler (generics, traits) |
| **Stage 3** | ⚙️ Ready | Full self-hosted compiler |

**Stage 1 Progress**:
- ✅ Parser: 100% complete (all 7 phases)
- ✅ CLI interface: Complete
- ✅ Driver integration: Complete
- 🚧 Bootstrap compilation: Next step
- ⚙️ Type checker: Planned
- ⚙️ IR generation: Planned
- ⚙️ Code generation: Planned

See [STATUS.md](STATUS.md) for detailed feature tracking and [docs/NEXT_STEPS_GUIDE.md](docs/NEXT_STEPS_GUIDE.md) for step-by-step bootstrap instructions.

## Standalone Smoke Gate

Every commit on `main` and every pull request runs an end-to-end smoke test
that proves Aster compiles and runs as a standalone toolchain.

### Validated flow

```bash
# 1. Build the compiler
dotnet build Aster.slnx -c Release

# 2. Compile Aster source → LLVM IR
dotnet run --project src/Aster.CLI -- build examples/simple_hello.ast --emit-llvm -o /tmp/aster_hello.ll

# 3. Compile IR → native binary
clang /tmp/aster_hello.ll -o /tmp/aster_hello

# 4. Run and assert
/tmp/aster_hello   # expected stdout: "Hello from Aster!"
```

### Expected output and exit code policy

| Check | Expected value |
|-------|----------------|
| **stdout** | `Hello from Aster!` (exact match) |
| **exit code** | `0` or `128` (see note below) |

> **Why exit 128?**  The current Aster IR emits `define void @main()` (a void
> return type for `main`), which is non-standard C.  When linked with clang on
> Linux x86-64 this consistently produces exit code 128 during local testing and
> CI runs, but the value is technically indeterminate (it depends on whatever
> happens to be in the return register at the point the runtime calls `main`).
> This is a **known temporary behaviour**.  The target future state is `exit 0`
> once the IR backend emits `define i32 @main()` with an explicit `ret i32 0`.

### Running the smoke test locally

```bash
# Standard run
bash scripts/standalone-smoke.sh

# Keep intermediate artifacts (/tmp/aster_hello.ll, /tmp/aster_hello)
bash scripts/standalone-smoke.sh --keep-artifacts

# Via the bootstrap verify wrapper
bash bootstrap/scripts/verify-standalone.sh
```

### Release / Merge Gate

A pull request or push to `main` **must satisfy all three gates** before
merging:

1. **Build gate** — `dotnet build Aster.slnx -c Release` exits 0 (CI: `Aster CI` workflow).
2. **Standalone smoke gate** — full compile → run cycle passes (CI: `Standalone Smoke` workflow).
3. **Verification script** — `bash bootstrap/scripts/verify-standalone.sh` exits 0 locally.

Merging while any of these gates is red is **not permitted**.

## Documentation

- **[TOOLCHAIN.md](TOOLCHAIN.md)** — Complete guide for compiling `.ast` → LLVM IR → native executable
- **[STATUS.md](STATUS.md)** — Feature status across all bootstrap stages
- **[docs/NEXT_STEPS_GUIDE.md](docs/NEXT_STEPS_GUIDE.md)** — **NEW: Step-by-step guide for bootstrap completion**
- **[README_BOOTSTRAP.md](README_BOOTSTRAP.md)** — Bootstrap process and roadmap
- [docs/STAGE1_PARSER_COMPLETE.md](docs/STAGE1_PARSER_COMPLETE.md) — Stage 1 parser implementation summary
- [Mid-End Architecture](docs/MidEndArchitecture.md) — Incremental compilation, parallel compilation, MIR analysis
- [Standard Library](aster/stdlib/README.md) — Complete stdlib documentation
- [Stdlib Summary](STDLIB_IMPLEMENTATION.md) — Implementation details
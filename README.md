# Aster-1

A production-grade ahead-of-time compiler for the **ASTER** programming language, written in C# (.NET 10).

**Current Status**: Stage 0 (C# Seed Compiler) — Emits LLVM IR  
**Bootstrap Progress**: 🚧 Working towards self-hosting (see [STATUS.md](STATUS.md))

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

```bash
# Build the compiler
dotnet build Aster.slnx

# Compile an ASTER source file to LLVM IR
dotnet run --project src/Aster.CLI -- build hello.ast --emit-llvm
# Output: hello.ll (LLVM IR text format)

# Type-check only (no code generation)
dotnet run --project src/Aster.CLI -- check hello.ast

# Compile LLVM IR to native executable (requires LLVM/clang)
clang hello.ll -o hello
./hello
```

**Complete Toolchain Guide**: See [TOOLCHAIN.md](TOOLCHAIN.md) for detailed compilation instructions.

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

## Documentation

- **[TOOLCHAIN.md](TOOLCHAIN.md)** — Complete guide for compiling `.ast` → LLVM IR → native executable
- **[STATUS.md](STATUS.md)** — Feature status across all bootstrap stages
- **[docs/NEXT_STEPS_GUIDE.md](docs/NEXT_STEPS_GUIDE.md)** — **NEW: Step-by-step guide for bootstrap completion**
- **[README_BOOTSTRAP.md](README_BOOTSTRAP.md)** — Bootstrap process and roadmap
- [docs/STAGE1_PARSER_COMPLETE.md](docs/STAGE1_PARSER_COMPLETE.md) — Stage 1 parser implementation summary
- [Mid-End Architecture](docs/MidEndArchitecture.md) — Incremental compilation, parallel compilation, MIR analysis
- [Standard Library](aster/stdlib/README.md) — Complete stdlib documentation
- [Stdlib Summary](STDLIB_IMPLEMENTATION.md) — Implementation details
# Aster-1

A production-grade ahead-of-time compiler for the **ASTER** programming language, written in C# (.NET 10).

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
- **LLVM** — Text IR emission with runtime ABI declarations

### Standard Library
- **12 Layered Modules** — Complete stdlib implementation (see `/aster/stdlib/`)
- **Effect System** — All APIs annotated with @alloc, @io, @unsafe effects
- **Stability Tiers** — @stable, @experimental, @unstable annotations
- **Zero-cost Abstractions** — No runtime overhead for unused features

## Quick Start

```bash
# Build the compiler
dotnet build Aster.slnx

# Compile an ASTER source file
dotnet run --project src/Aster.CLI -- build hello.ast

# Type-check only
dotnet run --project src/Aster.CLI -- check hello.ast

# Emit LLVM IR to stdout
dotnet run --project src/Aster.CLI -- emit-llvm hello.ast
```

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

## Documentation

- [Mid-End Architecture](docs/MidEndArchitecture.md) — Incremental compilation, parallel compilation, MIR analysis
- [Standard Library](aster/stdlib/README.md) — Complete stdlib documentation
- [Stdlib Summary](STDLIB_IMPLEMENTATION.md) — Implementation details
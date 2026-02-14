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
fn main() {
    print("hello world")
}
```

## Running Tests

```bash
# All tests (158 total)
dotnet test

# Specific test suites
dotnet test tests/Aster.Compiler.Tests              # Compiler tests
dotnet test tests/Aster.Compiler.OptimizationTests  # Optimization tests
dotnet test tests/Aster.Compiler.PerfTests          # Incremental compilation tests
```

## Fuzzing & Miscompile Detection

Aster includes a comprehensive fuzzing and differential testing system to prevent wrong-code bugs:

```bash
# Quick fuzzing (smoke mode)
dotnet run --project src/Aster.CLI -- fuzz parser --smoke
dotnet run --project src/Aster.CLI -- fuzz typesystem --smoke
dotnet run --project src/Aster.CLI -- fuzz optimizer --smoke

# Differential testing (O0 vs O3)
dotnet run --project src/Aster.CLI -- differential tests/conformance/compile-pass

# Test case reduction
dotnet run --project src/Aster.CLI -- reduce failing_test.ast
```

**Documentation:**
- [Fuzzing Implementation Summary](FUZZING_IMPLEMENTATION.md)
- [Fuzzing Guide](docs/fuzzing-guide.md)
- [Miscompile Policy](docs/miscompile-policy.md)
- [Triage Runbook](docs/triage-runbook.md)

## Architecture

See [Mid-End Architecture Documentation](docs/MidEndArchitecture.md) for details on:
- Incremental compilation system
- Parallel compilation scheduler  
- MIR analysis infrastructure
- Optimization passes
- Performance characteristics
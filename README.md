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
dotnet test tests/Aster.Compiler.Tests
```
# Aster Compiler Status

This document tracks the implementation status of features across all bootstrap stages.

**Last Updated**: 2026-02-15

## Bootstrap Progress

| Stage | Status | Compiler | Language Subset | Evidence |
|-------|--------|----------|-----------------|----------|
| **Stage 0** | ✅ Complete | C# Seed Compiler | Full Aster | [Build Passing](https://github.com/justinamiller/Aster-1/actions), [119 tests](tests/) |
| **Stage 1** | 🚧 20% | Minimal Aster | Core-0 | [Source](src/aster1/), [Lexer](src/aster1/lexer.ast) |
| **Stage 2** | ⚙️ Ready | Expanded Aster | Core-1 | [Infrastructure](bootstrap/) |
| **Stage 3** | ⚙️ Ready | Full Self-Hosted | Core-2 (Full) | [Infrastructure](bootstrap/) |

**Legend**:
- ✅ Complete and tested
- 🚧 In progress
- ⚙️ Infrastructure ready, implementation pending
- ❌ Not started

## Language Features by Stage

### Stage 0: C# Seed Compiler (✅ Complete)

| Feature | Stage 0 | Stage 1 | Stage 2 | Stage 3 | Evidence Link |
|---------|---------|---------|---------|---------|---------------|
| **Lexer** | ✅ Full | 🚧 Partial | - | - | [LexerTests.cs](tests/Aster.Compiler.Tests/LexerTests.cs) |
| **Parser** | ✅ Full | ❌ Not Started | - | - | [ParserTests.cs](tests/Aster.Compiler.Tests/ParserTests.cs) |
| **AST** | ✅ Full | 🚧 Partial | - | - | [AST.cs](src/Aster.Compiler/AST.cs) |
| **Name Resolution** | ✅ Full | ❌ Not Started | - | - | [NameResolver.cs](src/Aster.Compiler/NameResolver.cs) |
| **Type Inference** | ✅ Hindley-Milner | ❌ Not Started | - | - | [TypeInference.cs](src/Aster.Compiler/TypeInference.cs) |
| **Effect System** | ✅ Full | ❌ Not Started | - | - | [EffectChecker.cs](src/Aster.Compiler/EffectChecker.cs) |
| **Borrow Checker** | ✅ NLL-based | ❌ Not Started | - | - | [BorrowChecker.cs](src/Aster.Compiler/BorrowChecker.cs) |
| **MIR** | ✅ SSA | ❌ Not Started | - | - | [MIR/](src/Aster.Compiler.MidEnd/) |
| **Optimizations** | ✅ O0-O3 | ❌ Not Started | - | - | [Optimizations/](src/Aster.Compiler.Optimizations/) |
| **LLVM Backend** | ✅ IR Emission | ❌ Not Started | - | - | [LLVMCodegen.cs](src/Aster.Compiler.Codegen/LLVMCodegen.cs) |

### Stage 1: Minimal Aster Compiler (🚧 20%)

**Language Subset: Core-0**
- Primitive types: `i32`, `i64`, `f32`, `f64`, `bool`, `String`
- Functions (no methods)
- Structs (no methods)
- Enums (simple)
- `Vec<T>` and `String` (built-in)
- Control flow: `if`, `while`, `loop`, `break`, `continue`
- No traits, no generics, no methods, no heap allocation outside explicit allocator

| Component | Status | Evidence |
|-----------|--------|----------|
| String Interner | ✅ Complete | [string_interner.ast](src/aster1/string_interner.ast) |
| Lexer | 🚧 80% | [lexer.ast](src/aster1/lexer.ast) |
| Token Definitions | ✅ Complete | [Documented in lexer](src/aster1/lexer.ast) |
| Parser Infrastructure | ✅ Complete | [parser.ast](src/aster1/parser.ast) - 18 helper functions |
| Parser Implementation | 🚧 15% | [parser.ast](src/aster1/parser.ast) - Phase 1 done |
| AST | 🚧 Partial | [ast.ast](src/aster1/ast.ast) |
| Symbol Table | 🚧 Partial | [symbols.ast](src/aster1/symbols.ast) |
| Type Checker | 🚧 Skeleton | [typecheck.ast](src/aster1/typecheck.ast) |
| IR Builder | 🚧 Skeleton | [ir.ast](src/aster1/ir.ast) |
| Code Generator | 🚧 Skeleton | [codegen.ast](src/aster1/codegen.ast) |
| Driver | 🚧 Skeleton | [driver.ast](src/aster1/driver.ast) |
| Main Entry Point | ❌ Not Started | Required for compilation |
| Emit Tokens | ❌ Not Started | Required for differential testing |
| Emit AST | ❌ Not Started | Required for differential testing |
| **Differential Testing** | ✅ Complete | [bootstrap/scripts/](bootstrap/scripts/) |
| **Golden Files** | ✅ Generated | 28 files: tokens, AST, symbols |
| **Parser Guide** | ✅ Complete | [docs/STAGE1_PARSER_GUIDE.md](docs/STAGE1_PARSER_GUIDE.md) |

**Parser Progress** (Phase 1 of 7 Complete):
- ✅ Phase 1: Helper functions (peek, advance, check, expect, synchronize)
- ⚙️ Phase 2: Declaration parsing (functions, structs, enums)  
- ⚙️ Phase 3: Type parsing
- ⚙️ Phase 4: Expression parsing (Pratt parser)
- ⚙️ Phase 5: Statement parsing
- ⚙️ Phase 6: Pattern matching
- ⚙️ Phase 7: Integration and testing

**Remaining Work for Stage 1**:
1. Complete parser phases 2-7 (2-3 weeks)
2. Complete lexer (1 week)
3. Complete AST nodes (1 week)
4. Implement symbol table (1 week)
5. Create main entry point with CLI (1 week)
6. Implement `emit-tokens` and `emit-ast-json` commands (1 week)
7. Differential testing validation (1 week)

**Estimated Completion**: 2-3 months

### Stage 2: Expanded Aster Compiler (⚙️ Ready)

**Language Subset: Core-1** (adds to Core-0)
- Generics (functions and structs)
- Traits (simple, no associated types)
- Methods (via `impl` blocks)
- Pattern matching
- `HashMap<K,V>` and other collections
- Basic ownership checking

**Status**: Infrastructure complete, awaiting Stage 1 completion

**Estimated Completion**: 3-4 months after Stage 1

### Stage 3: Full Self-Hosted Compiler (⚙️ Ready)

**Language Subset: Core-2** (Full Aster)
- Associated types
- Higher-ranked trait bounds
- Async/await
- Full borrow checker
- Lifetime annotations
- Complex effect tracking
- Full MIR and optimizations
- All standard library modules

**Status**: Infrastructure complete, awaiting Stage 2 completion

**Estimated Completion**: 4-6 months after Stage 2

## Compiler Capabilities by Stage

### Output Formats

| Format | Stage 0 | Stage 1 | Stage 2 | Stage 3 | Evidence |
|--------|---------|---------|---------|---------|----------|
| LLVM IR Text (.ll) | ✅ | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | [examples/simple_hello.ll](examples/simple_hello.ll) |
| Token Stream (JSON) | ✅ | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | `emit-tokens` command |
| AST (JSON) | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | `--emit-ast-json` flag |
| Symbols (JSON) | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | `--emit-symbols-json` flag |
| Native Executable | ❌ | ❌ | ❌ | ⚙️ Planned | Manual via LLVM/clang |
| LLVM Bitcode (.bc) | ❌ | ❌ | ❌ | ⚙️ Planned | Future work |

### Tooling

| Tool | Stage 0 | Stage 1 | Stage 2 | Stage 3 | Evidence |
|------|---------|---------|---------|---------|----------|
| Build | ✅ | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | `dotnet run -- build` |
| Check (type-check only) | ✅ | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | `dotnet run -- check` |
| Format | ✅ | ❌ | ❌ | ⚙️ Planned | [Aster.Formatter](src/Aster.Formatter/) |
| Lint | ✅ | ❌ | ❌ | ⚙️ Planned | [Aster.Linter](src/Aster.Linter/) |
| LSP | ✅ | ❌ | ❌ | ⚙️ Planned | [Aster.Lsp](src/Aster.Lsp/) |
| Doc Generator | ✅ | ❌ | ❌ | ⚙️ Planned | [Aster.DocGen](src/Aster.DocGen/) |
| Test Framework | ✅ | ❌ | ❌ | ⚙️ Planned | [Aster.Testing](src/Aster.Testing/) |
| Package Manager | ✅ | ❌ | ❌ | ⚙️ Planned | [Aster.Packages](src/Aster.Packages/) |

### Standard Library

| Module | Stage 0 Support | Stage 1 | Stage 2 | Stage 3 | Evidence |
|--------|-----------------|---------|---------|---------|----------|
| core | ✅ Full | ❌ | ⚙️ Planned | ⚙️ Planned | [aster/stdlib/core/](aster/stdlib/core/) |
| alloc | ✅ Full | ❌ | ⚙️ Planned | ⚙️ Planned | [aster/stdlib/alloc/](aster/stdlib/alloc/) |
| sync | ✅ Full | ❌ | ⚙️ Planned | ⚙️ Planned | [aster/stdlib/sync/](aster/stdlib/sync/) |
| io | ✅ Full | ❌ | ⚙️ Planned | ⚙️ Planned | [aster/stdlib/io/](aster/stdlib/io/) |
| fs | ✅ Full | ❌ | ⚙️ Planned | ⚙️ Planned | [aster/stdlib/fs/](aster/stdlib/fs/) |
| net | ✅ Full | ❌ | ⚙️ Planned | ⚙️ Planned | [aster/stdlib/net/](aster/stdlib/net/) |
| time | ✅ Full | ❌ | ⚙️ Planned | ⚙️ Planned | [aster/stdlib/time/](aster/stdlib/time/) |
| fmt | ✅ Full | ❌ | ⚙️ Planned | ⚙️ Planned | [aster/stdlib/fmt/](aster/stdlib/fmt/) |
| math | ✅ Full | ❌ | ⚙️ Planned | ⚙️ Planned | [aster/stdlib/math/](aster/stdlib/math/) |
| testing | ✅ Full | ❌ | ⚙️ Planned | ⚙️ Planned | [aster/stdlib/testing/](aster/stdlib/testing/) |
| env | ✅ Full | ❌ | ⚙️ Planned | ⚙️ Planned | [aster/stdlib/env/](aster/stdlib/env/) |
| process | ✅ Full | ❌ | ⚙️ Planned | ⚙️ Planned | [aster/stdlib/process/](aster/stdlib/process/) |

## Testing Infrastructure

| Test Type | Stage 0 | Stage 1 | Stage 2 | Stage 3 | Evidence |
|-----------|---------|---------|---------|---------|----------|
| Unit Tests | ✅ 119 tests | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | [tests/](tests/) |
| Differential Tests | ✅ Infrastructure | ✅ Ready | ⚙️ Planned | ⚙️ Planned | [bootstrap/scripts/](bootstrap/scripts/) |
| Golden Files | ✅ 28 files | ✅ Generated | ⚙️ Planned | ⚙️ Planned | [bootstrap/goldens/](bootstrap/goldens/) |
| Integration Tests | ✅ | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | [.github/workflows/](github/workflows/) |
| Optimization Tests | ✅ 7 tests | ❌ | ⚙️ Planned | ⚙️ Planned | [tests/Aster.Compiler.OptimizationTests/](tests/Aster.Compiler.OptimizationTests/) |
| Fuzzing | ✅ Framework | ❌ | ⚙️ Planned | ⚙️ Planned | [src/Aster.Compiler.Fuzzing/](src/Aster.Compiler.Fuzzing/) |
| Self-Compilation | N/A | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | Stage 1+ can compile itself |

**Differential Testing Infrastructure** (✅ Complete):
- `diff-test-tokens.sh` — Compares token streams
- `diff-test-ast.sh` — Compares AST outputs  
- `diff-test-symbols.sh` — Compares symbol tables/HIR
- `generate-goldens.sh` — Generates reference outputs from aster0
- 12 Core-0 fixtures: 5 compile-pass, 3 run-pass, 4 compile-fail
- 28 golden files: tokens, AST, and symbols for all valid fixtures

## CI/CD Status

| Pipeline | Status | Evidence |
|----------|--------|----------|
| Build | ✅ Passing | [ci.yml](.github/workflows/ci.yml) |
| Test | ✅ 119/119 | [ci.yml](.github/workflows/ci.yml) |
| Bootstrap | ✅ Stage 0 | [bootstrap.yml](.github/workflows/bootstrap.yml) |
| Differential Testing | ✅ Integrated | [ci.yml](.github/workflows/ci.yml) - `differential-testing` job |
| Fuzzing (Smoke) | ✅ Passing | [fuzz-smoke.yml](.github/workflows/fuzz-smoke.yml) |
| Fuzzing (Nightly) | ✅ Scheduled | [fuzz-nightly.yml](.github/workflows/fuzz-nightly.yml) |
| Release | ✅ Ready | [release.yml](.github/workflows/release.yml) |
| LLVM IR Verification | ✅ Implemented | [ci.yml](.github/workflows/ci.yml) |
| Example Execution | ✅ Implemented | [ci.yml](.github/workflows/ci.yml) - hello.ast runs in CI |

## Runtime ABI

| Intrinsic | Stage 0 | Stage 1 | Stage 2 | Stage 3 | Evidence |
|-----------|---------|---------|---------|---------|----------|
| `panic(msg)` | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | Future runtime |
| `malloc(size)` | ✅ Declared | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | [LLVMCodegen.cs](src/Aster.Compiler.Codegen/LLVMCodegen.cs) |
| `free(ptr)` | ✅ Declared | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | [LLVMCodegen.cs](src/Aster.Compiler.Codegen/LLVMCodegen.cs) |
| `write_stdout(ptr, len)` | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | Future runtime |
| `exit(code)` | ✅ Declared | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | [LLVMCodegen.cs](src/Aster.Compiler.Codegen/LLVMCodegen.cs) |
| `puts(ptr)` | ✅ Used | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | [LLVMCodegen.cs](src/Aster.Compiler.Codegen/LLVMCodegen.cs) |
| `printf(ptr, ...)` | ✅ Declared | ⚙️ Planned | ⚙️ Planned | ⚙️ Planned | [LLVMCodegen.cs](src/Aster.Compiler.Codegen/LLVMCodegen.cs) |

## Examples

| Example | Status | Output Type | Evidence |
|---------|--------|-------------|----------|
| simple_hello.ast | ✅ Working | LLVM IR | [examples/simple_hello.ast](examples/simple_hello.ast) |
| type_inference_success.ast | ✅ Working | LLVM IR | [examples/type_inference_success.ast](examples/type_inference_success.ast) |
| stdlib_hello.ast | ✅ Working | LLVM IR | [examples/stdlib_hello.ast](examples/stdlib_hello.ast) |
| stdlib_collections.ast | ✅ Working | LLVM IR | [examples/stdlib_collections.ast](examples/stdlib_collections.ast) |
| stdlib_complete.ast | ✅ Working | LLVM IR | [examples/stdlib_complete.ast](examples/stdlib_complete.ast) |
| hello.ast | ⚙️ Planned | Native | Minimal hello world for runtime |
| fib.ast | ⚙️ Planned | Native | Fibonacci calculator |
| file_copy.ast | ⚙️ Planned | Native | File I/O example |

## Planned Features (Phase 4)

Features to be added after Stage 3 bootstrap is complete:

| Feature | Priority | Spec | Tests | Status | Timeline |
|---------|----------|------|-------|--------|----------|
| Methods | High | ⚙️ Planned | ⚙️ Planned | ⚙️ After Stage 3 | 2-3 weeks |
| Struct impl blocks | High | ⚙️ Planned | ⚙️ Planned | ⚙️ After Stage 3 | 1-2 weeks |
| Generics | High | ⚙️ Planned | ⚙️ Planned | ⚙️ After Stage 3 | 3-4 weeks |
| Traits | High | ⚙️ Planned | ⚙️ Planned | ⚙️ After Stage 3 | 4-5 weeks |
| Borrow checker | High | ⚙️ Planned | ⚙️ Planned | ⚙️ After Stage 3 | 4-6 weeks |
| Associated types | Medium | ⚙️ Planned | ⚙️ Planned | ⚙️ After Stage 3 | 2-3 weeks |
| Async/await | Medium | ⚙️ Planned | ⚙️ Planned | ⚙️ After Stage 3 | 6-8 weeks |
| Macros | Low | ⚙️ Planned | ⚙️ Planned | ⚙️ Future | 8-10 weeks |
| Procedural macros | Low | ⚙️ Planned | ⚙️ Planned | ⚙️ Future | 10-12 weeks |

**See [PHASE4_ROADMAP.md](PHASE4_ROADMAP.md) for detailed feature specifications and implementation plan.**

## Native Distribution (Phase 3)

| Approach | Status | Timeline | Evidence |
|----------|--------|----------|----------|
| .NET NativeAOT (Fast Path) | ⚙️ Planned | 1-2 weeks | [NATIVE_DISTRIBUTION.md](NATIVE_DISTRIBUTION.md) |
| Self-Hosted (Purist Path) | 🚧 In Progress | 12-15 months | Stage 1 at 20%, [STATUS.md](STATUS.md) |
| **Selected Approach** | **Hybrid** | Both A then B | [NATIVE_DISTRIBUTION.md](NATIVE_DISTRIBUTION.md) |

**Hybrid Strategy**: Implement NativeAOT first for immediate standalone distribution, then transition to self-hosted compiler once Stage 3 is complete.

## Dependencies

| Dependency | Stage 0 | Stage 1+ | Purpose |
|------------|---------|----------|---------|
| .NET 10 SDK | ✅ Required | ❌ Not Required | Build Stage 0 compiler |
| LLVM 18+ | ⚙️ Optional | ⚙️ Optional | Compile LLVM IR to native |
| Clang 18+ | ⚙️ Optional | ⚙️ Optional | Link to native executable |
| C Standard Library | ✅ Required | ✅ Required | Runtime functions (malloc, puts, etc.) |
| Custom Runtime | ❌ | ⚙️ Planned | Aster-specific intrinsics |

## Deterministic Builds

| Feature | Status | Evidence |
|---------|--------|----------|
| Stable hashing | ✅ Implemented | [StableHasher](src/Aster.Compiler.Incremental/) |
| Deterministic output | ✅ Tested | [ParallelCompilationTests](tests/Aster.Compiler.PerfTests/) |
| Reproducible IR | ✅ Working | LLVM IR output is deterministic |
| Reproducible binaries | ⚙️ Planned | Stage 3 self-compilation |

## Documentation Status

| Document | Status | Evidence |
|----------|--------|----------|
| README.md | ✅ Complete | [README.md](README.md) |
| TOOLCHAIN.md | ✅ Complete | [TOOLCHAIN.md](TOOLCHAIN.md) |
| STATUS.md | ✅ Complete | This file |
| Bootstrap Guide | ✅ Complete | [README_BOOTSTRAP.md](README_BOOTSTRAP.md) |
| Language Spec | 🚧 Partial | [docs/spec/](docs/spec/) |
| Stdlib Docs | ✅ Complete | [aster/stdlib/README.md](aster/stdlib/README.md) |
| Mid-End Architecture | ✅ Complete | [docs/MidEndArchitecture.md](docs/MidEndArchitecture.md) |
| API Documentation | ⚙️ Planned | Auto-generated from code |

## Summary

### Current State (2026-02-15)

- ✅ **Stage 0**: Fully operational C# compiler with LLVM IR backend
- 🚧 **Stage 1**: 20% complete (lexer partial, infrastructure ready)
- ⚙️ **Stage 2**: Infrastructure ready, awaiting Stage 1
- ⚙️ **Stage 3**: Infrastructure ready, awaiting Stage 2

### What Works Now

- Building Aster programs to LLVM IR
- Type checking with Hindley-Milner inference
- Effect system tracking (@alloc, @io, @unsafe, etc.)
- Borrow checking with NLL
- Optimizations (O0-O3)
- 119 passing tests
- Complete standard library (12 modules)
- Fuzzing infrastructure
- Differential testing framework (ready)

### What's Next

1. **Complete Stage 1 lexer** (1-2 weeks)
2. **Implement Stage 1 parser** (3-4 weeks)
3. **Add differential testing** (1 week)
4. **Document runtime ABI** (1 week)
5. **Create minimal runtime** (2-3 weeks)
6. **Add native compilation to CI** (1 week)

### Timeline to Self-Hosting

- **Stage 1 completion**: ~2-3 months
- **Stage 2 completion**: ~3-4 months (after Stage 1)
- **Stage 3 completion**: ~4-6 months (after Stage 2)
- **Total**: ~12-15 months from now

### Success Criteria

- ✅ Stage 0 builds and passes all tests
- ⚙️ Stage 1 can compile its own source
- ⚙️ Stage 2 matches Stage 0 type inference
- ⚙️ Stage 3 self-compiles with stable output
- ⚙️ All stages produce deterministic results
- ⚙️ Complete differential test coverage

---

**Legend**:
- ✅ Complete and verified
- 🚧 In progress
- ⚙️ Infrastructure/spec ready, implementation pending
- ❌ Not started

For detailed information, see:
- [README.md](README.md) — Project overview
- [TOOLCHAIN.md](TOOLCHAIN.md) — Compilation guide
- [README_BOOTSTRAP.md](README_BOOTSTRAP.md) — Bootstrap process
- [BOOTSTRAP_WORKFLOW.md](BOOTSTRAP_WORKFLOW.md) — Detailed workflow

# Seed Compiler (Stage 0)

## What is the Seed Compiler?

The **seed compiler** is the trusted bootstrap compiler for the Aster programming language. It is implemented in C# (.NET 10) and serves as the foundation for building the self-hosted Aster compiler.

## Purpose

The seed compiler exists to:
1. **Bootstrap**: Compile the first Aster-written compiler (Stage 1)
2. **Trust Anchor**: Provide a verifiable, auditable starting point
3. **Recovery**: Rebuild the entire compiler chain if binaries are lost
4. **Verification**: Compare outputs with self-hosted compilers

## Current Status

- **Implementation**: C# (.NET 10)
- **Location**: `/src/Aster.Compiler/`
- **Version**: See `aster-seed-version.txt`
- **Build Status**: Production-ready (119 tests passing)

## Building the Seed Compiler

### Prerequisites

- .NET 10 SDK or later
- LLVM 19.x (llc, opt)
- Clang 19.x (for C runtime)
- Git

### Build Steps

```bash
# 1. Clone the repository
git clone https://github.com/justinamiller/Aster-1.git
cd Aster-1

# 2. Checkout the seed compiler version
git checkout <seed-commit-hash>  # See aster-seed-version.txt

# 3. Build the C# compiler
dotnet build Aster.slnx --configuration Release

# 4. Verify the build
dotnet test

# 5. Run the compiler
dotnet run --project src/Aster.CLI -- build examples/simple_hello.ast
```

### Expected Output

```
Aster Compiler v0.1.0
.NET Version: 10.0.1
LLVM Version: 19.1.0

Build: Release
Tests: 119 passed, 0 failed
Time: ~2 minutes
```

## Verifying the Seed Compiler

### 1. Build from Source

```bash
# Clean build from scratch
dotnet clean
dotnet build --configuration Release
```

### 2. Run Tests

```bash
# All tests should pass
dotnet test --configuration Release

# Expected output:
# Total tests: 119
# Passed: 119
# Failed: 0
# Skipped: 0
```

### 3. Compare with Pinned Version

```bash
# Check compiler version
dotnet run --project src/Aster.CLI -- --version

# Expected output:
# Aster Compiler 0.1.0
# Commit: <seed-commit-hash>
```

### 4. Build Example Programs

```bash
# Compile a simple program
dotnet run --project src/Aster.CLI -- build examples/simple_hello.ast -o hello

# Run the program
./hello

# Expected output:
# hello world
```

## Security

### Source Code Audit

The seed compiler is fully auditable:
- **Lines of Code**: ~50,000 lines of C#
- **Security Scan**: CodeQL analysis shows 0 vulnerabilities
- **Code Review**: All changes reviewed before merge
- **Test Coverage**: 119 comprehensive tests

### Verification Checklist

Before trusting a seed compiler build:

- [ ] Source code matches documented commit hash
- [ ] All tests pass
- [ ] No compiler warnings
- [ ] Security scan passes
- [ ] Example programs compile and run correctly
- [ ] Output matches expected behavior on fixtures

## Rebuilding from Seed

If the entire Aster toolchain is lost, you can rebuild from the seed:

```bash
# 1. Rebuild seed compiler from C# source
git checkout <seed-commit-hash>
dotnet build Aster.slnx --configuration Release

# 2. Run bootstrap script
./bootstrap/scripts/bootstrap.sh --from-seed

# 3. Verify all stages
./bootstrap/scripts/verify.sh --all-stages

# 4. Check self-hosting
./bootstrap/scripts/verify.sh --self-check
```

**Time**: ~30 minutes on a modern machine

## When to Update the Seed

The seed compiler should be updated **only** when:

1. **Critical Security Vulnerability**: A security issue is found in the C# compiler
2. **New Language Feature**: A new feature requires changes to the seed
3. **Major Performance Improvement**: Significant optimization in the C# compiler
4. **Bug Fix**: Critical bug that prevents bootstrapping

**DO NOT** update the seed for:
- ❌ Minor refactorings
- ❌ Code style changes
- ❌ Documentation updates
- ❌ Non-critical bug fixes

## Update Process

If the seed must be updated:

1. **Propose Update**: Create an issue explaining why the seed needs to be updated
2. **Review Changes**: All seed compiler changes must be reviewed by 2+ maintainers
3. **Test Thoroughly**: All tests must pass, including bootstrap chain
4. **Update Version File**: Update `aster-seed-version.txt` with new commit hash
5. **Tag Release**: Create a Git tag for the new seed (e.g., `seed-v0.2.0`)
6. **Archive Old Seed**: Optionally archive the old seed binary
7. **Update Documentation**: Update all references to the seed version

## Seed Compiler Architecture

The seed compiler includes:

### Frontend
- **Lexer**: UTF-8 tokenization with span tracking
- **Parser**: Recursive descent with Pratt expression parsing
- **AST**: Immutable syntax tree
- **Name Resolution**: Two-pass symbol resolution
- **Type Checking**: Hindley-Milner type inference
- **Effect System**: Tracks IO, alloc, async, unsafe, FFI, throw
- **Ownership**: Move semantics and borrow tracking

### Middle End
- **MIR**: SSA-based intermediate representation
- **Borrow Checker**: Non-lexical lifetime analysis
- **Optimizations**: DCE, constant folding, CSE, inlining, SROA
- **Incremental Compilation**: Query-based caching system
- **Parallel Compilation**: Work-stealing scheduler

### Backend
- **LLVM IR**: Text IR emission
- **Runtime ABI**: Integration with C runtime

### Tooling
- **Formatter**: AST-based code formatting
- **Linter**: HIR-based code analysis
- **LSP**: Language Server Protocol implementation
- **Doc Generator**: Documentation extraction
- **Test Runner**: Test harness

## Alternative Bootstrap Paths (Future)

In the future, we plan to support multiple seed compilers:

1. **C# Seed** (current): Production-ready, fully tested
2. **Rust Seed** (future): Alternative implementation for diversity
3. **OCaml Seed** (future): Functional programming approach
4. **Zig Seed** (future): Systems programming alternative

All paths should converge to the same Stage 3 compiler, providing additional trust through diversity.

## Trusting Trust

**Q**: How do we know the seed compiler doesn't have a backdoor?

**A**: Multiple layers of defense:

1. **Source Code is Public**: All C# code is in this repository and auditable
2. **Can Rebuild from Source**: Anyone can rebuild the seed compiler from C# source
3. **Multiple Reviewers**: All changes are reviewed by multiple maintainers
4. **Security Scanning**: Automated security analysis (CodeQL)
5. **Diverse Double-Compiling**: Future: compile with multiple different compilers and compare outputs
6. **Community Oversight**: Anyone can audit the code and build process

See `/bootstrap/spec/trust-chain.md` for more details.

## Frequently Asked Questions

### Why C# for the seed compiler?

- ✅ **Mature Ecosystem**: .NET has excellent tooling and libraries
- ✅ **Memory Safe**: C# is memory-safe, reducing security risks
- ✅ **Fast Development**: Rapid iteration during initial development
- ✅ **Cross-Platform**: .NET 10 runs on Windows, macOS, Linux
- ✅ **Good Performance**: Adequate for a bootstrap compiler

### Will the C# compiler be deleted after self-hosting?

**No!** The C# compiler will be:
- ✅ Maintained as the canonical seed compiler
- ✅ Used for bootstrapping from scratch
- ✅ Available for verification and auditing
- ✅ Updated for critical bugs and security issues

However, day-to-day development will use the self-hosted Aster compiler (Stage 3).

### How long does it take to bootstrap from seed?

On a modern machine (8 cores, 16GB RAM):
- **Stage 0 (seed)**: 2 minutes (dotnet build)
- **Stage 1**: 5 minutes (compile minimal compiler)
- **Stage 2**: 10 minutes (compile expanded compiler)
- **Stage 3**: 15 minutes (compile full compiler)
- **Total**: ~30 minutes

With incremental compilation and caching, subsequent builds are much faster (<1 minute).

### Can I use the seed compiler for production?

**Yes**, but:
- ✅ The seed compiler is production-ready and fully tested
- ✅ It generates optimized native binaries via LLVM
- ✅ All language features are supported

However:
- ⚠️ The self-hosted compiler (Stage 3) may have better diagnostics
- ⚠️ The self-hosted compiler may be faster (native vs .NET)
- ⚠️ The self-hosted compiler is the primary development target

For maximum compatibility, use the self-hosted compiler (Stage 3) for production.

### What if .NET becomes unavailable?

If .NET becomes unavailable (unlikely):
1. **Use archived .NET SDK**: Keep a copy of .NET 10 SDK
2. **Use Mono**: Open-source .NET implementation
3. **Use alternative seed**: Switch to Rust/OCaml/Zig seed (future)
4. **Reimplement in assembly**: Extreme case (very unlikely)

The seed compiler is designed to minimize external dependencies.

## Contact

For questions about the seed compiler:
- **Issues**: https://github.com/justinamiller/Aster-1/issues
- **Discussions**: https://github.com/justinamiller/Aster-1/discussions
- **Security**: See SECURITY.md

## License

The seed compiler is licensed under the same license as the rest of the Aster project. See LICENSE file for details.

## Acknowledgments

The seed compiler is inspired by:
- **Rust**: Modern systems programming language
- **Swift**: Advanced type system
- **OCaml**: Functional programming concepts
- **LLVM**: Compiler infrastructure

Special thanks to the .NET team for providing an excellent platform for building compilers.

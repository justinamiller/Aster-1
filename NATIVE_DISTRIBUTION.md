# Native Standalone Compiler Distribution

This document outlines the strategy for producing a standalone Aster compiler that does not require .NET to be installed.

**Status**: Planning Phase  
**Last Updated**: 2026-02-15

## Goal

Provide end users with a native executable compiler that:
- Does not require .NET SDK installation
- Can compile Aster programs to native executables
- Is self-contained and portable
- Maintains deterministic output

## Two Approaches

### Option A: .NET NativeAOT (Fast Path) ⚡

Compile the Stage 0 C# compiler to a native executable using .NET NativeAOT.

**Advantages**:
- Fast to implement (1-2 weeks)
- Leverages existing Stage 0 codebase
- No runtime dependency on .NET
- Native performance
- Supports all platforms .NET supports

**Disadvantages**:
- Still written in C#, not "pure" self-hosting
- Larger binary size (~50-100MB)
- Requires .NET SDK for building the compiler itself
- Not philosophically "bootstrapped from source"

**Implementation Steps**:
1. Add `<PublishAot>true</PublishAot>` to Aster.CLI.csproj
2. Configure NativeAOT settings for trimming
3. Build: `dotnet publish -c Release -r linux-x64`
4. Test native binary on clean system without .NET
5. Create release scripts for all platforms
6. Update CI to build and test NativeAOT builds

**Timeline**: 1-2 weeks

### Option B: Full Self-Hosting (Purist Path) 🎯

Complete the bootstrap through Stage 3, where the compiler compiles itself.

**Advantages**:
- True self-hosting: compiler written in Aster, compiled by Aster
- Philosophically pure bootstrap
- Smaller binary size
- Demonstrates language completeness
- No dependency on .NET at all (after bootstrap)

**Disadvantages**:
- Requires completing Stage 1, 2, and 3 (~12-15 months)
- More complex to achieve
- Harder to debug during development

**Implementation Steps**:
1. Complete Stage 1 (Core-0 compiler) — 2-3 months
2. Complete Stage 2 (Core-1 with generics/traits) — 3-4 months
3. Complete Stage 3 (Full language) — 4-6 months
4. Verify deterministic self-compilation
5. Package Stage 3 binary as standalone distribution
6. Create bootstrap scripts for building from source

**Timeline**: 12-15 months

## Recommended Approach

### Hybrid Strategy: Both A and B 🎯⚡

**Phase 1** (Immediate): Implement Option A (NativeAOT)
- Provides immediate standalone compiler for users
- No .NET dependency for end users
- Can distribute binaries now

**Phase 2** (Long-term): Implement Option B (Self-Hosting)
- Continue bootstrap development
- Eventually replace NativeAOT binary with self-hosted compiler
- Proves language maturity

**Rationale**:
- Option A unblocks users immediately
- Option B remains the long-term goal
- No conflict: can ship both
- NativeAOT build can be marked as "interim" distribution

## Distribution Targets

### Platforms

| Platform | Architecture | Option A (NativeAOT) | Option B (Self-Hosted) |
|----------|--------------|----------------------|------------------------|
| Linux | x86_64 | ✅ Supported | ⚙️ Planned |
| Linux | ARM64 | ✅ Supported | ⚙️ Planned |
| macOS | x86_64 | ✅ Supported | ⚙️ Planned |
| macOS | ARM64 (M1+) | ✅ Supported | ⚙️ Planned |
| Windows | x86_64 | ✅ Supported | ⚙️ Planned |
| FreeBSD | x86_64 | ⚠️ Experimental | ⚙️ Planned |

### Package Formats

- **Linux**: `.tar.gz`, `.deb`, `.rpm`, AppImage
- **macOS**: `.tar.gz`, `.dmg`, Homebrew formula
- **Windows**: `.zip`, `.msi`, Chocolatey package

## Binary Size Comparison

| Approach | Estimated Size | Notes |
|----------|----------------|-------|
| .NET Dependency | 10-20 MB | Requires .NET runtime (200+ MB) |
| NativeAOT | 50-100 MB | Single self-contained binary |
| Self-Hosted | 5-15 MB | Minimal dependencies, just LLVM/clang for codegen |

## Implementation Plan for Option A (NativeAOT)

### Week 1: Configuration
- [ ] Add NativeAOT configuration to Aster.CLI.csproj
- [ ] Configure ILCompiler settings
- [ ] Set up trimming and optimization flags
- [ ] Test basic build on development machine

### Week 2: Multi-Platform Build
- [ ] Set up CI for Linux x64 build
- [ ] Set up CI for macOS x64 build
- [ ] Set up CI for macOS ARM64 build
- [ ] Set up CI for Windows x64 build
- [ ] Test on clean systems without .NET

### Week 3: Packaging
- [ ] Create release scripts
- [ ] Generate `.tar.gz` archives
- [ ] Create installers for each platform
- [ ] Write installation documentation
- [ ] Update TOOLCHAIN.md with binary installation

### Week 4: Testing & Release
- [ ] Test on multiple distros/OS versions
- [ ] Verify all commands work in standalone mode
- [ ] Create GitHub release with binaries
- [ ] Announce availability

## Runtime Dependencies

Both approaches require:
- **LLVM/Clang** (for compiling LLVM IR → native)
- **C standard library** (for runtime)
- **Linker** (ld, typically from binutils)

Optional:
- **Aster runtime library** (can be statically linked)

## Success Criteria

### Option A (NativeAOT)
- ✅ Single executable, no .NET installation required
- ✅ Works on clean system without .NET SDK
- ✅ Binary size < 150 MB
- ✅ Compile time comparable to dotnet version
- ✅ All compiler features work
- ✅ Deterministic output
- ✅ Passes all tests

### Option B (Self-Hosted)
- ✅ Compiler written in Aster
- ✅ Compiles itself (Stage 3)
- ✅ Stable self-compilation: aster3 → aster3' → aster3''
- ✅ aster3' ≡ aster3'' (bit-identical or functionally equivalent)
- ✅ Binary size < 20 MB
- ✅ Passes all tests
- ✅ Deterministic output

## Current Status (2026-02-15)

- **Option A**: Not started (estimated 1-2 weeks)
- **Option B**: Stage 1 at 20% (estimated 12-15 months)
- **Chosen Approach**: Hybrid (A first, then B)

## Next Steps

1. Implement Option A (NativeAOT) — **Immediate priority**
2. Continue Stage 1 development in parallel — **Ongoing**
3. Document installation for native builds — **After Option A**
4. Plan transition from A to B — **Long-term**

## References

- [.NET NativeAOT Documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Bootstrap Stages Specification](bootstrap/spec/bootstrap-stages.md)
- [TOOLCHAIN.md](TOOLCHAIN.md) — Current compilation process
- [STATUS.md](STATUS.md) — Bootstrap progress tracking

---

**Decision**: Proceed with **Hybrid Strategy** (Option A immediately, Option B long-term)

**Rationale**: Provides immediate value to users while maintaining long-term self-hosting goals.

**Owner**: To be assigned  
**Target Date for Option A**: 2-3 weeks from now

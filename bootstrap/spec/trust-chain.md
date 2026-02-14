# Trust Chain Specification

## Overview

This document defines the trust model for the Aster bootstrap process, explaining what we trust, why we trust it, and how to recover if trust is compromised.

## Trusted Computing Base (TCB)

### What We Trust

The Aster bootstrap process minimizes the trusted computing base (TCB) to the smallest possible set of components:

#### 1. Seed Compiler (Stage 0) - C# Implementation

**Component**: `/src/Aster.Compiler/` (C# codebase)

**Why We Trust It**:
- ✅ **Source Code Available**: All C# source code is in the repository
- ✅ **Human-Auditable**: ~50,000 lines of C# code can be reviewed
- ✅ **Statically Typed**: C# type system provides compile-time guarantees
- ✅ **Comprehensive Tests**: 119 unit tests, all passing
- ✅ **Security Scanned**: CodeQL analysis shows 0 vulnerabilities
- ✅ **Code Reviewed**: All changes go through code review

**What We Trust It To Do**:
- Correctly parse Aster source code
- Correctly implement type checking and borrow checking
- Correctly generate LLVM IR
- Produce deterministic outputs given deterministic inputs

**Verification**:
- ✅ Source code is version-controlled (Git)
- ✅ Build is reproducible (dotnet build)
- ✅ Tests verify correctness
- ✅ Can be rebuilt from source at any time

**Risk Level**: **LOW**
- C# compiler is open source and auditable
- .NET runtime is mature and widely trusted
- Build process is transparent

---

#### 2. .NET Runtime (.NET 10)

**Component**: Microsoft .NET 10 Runtime

**Why We Trust It**:
- ✅ **Industry Standard**: Used by millions of developers
- ✅ **Open Source**: .NET Core/5+/10 is open source
- ✅ **Mature**: Decades of development
- ✅ **Well-Audited**: Security researchers actively review it
- ✅ **Reproducible**: Can be built from source

**What We Trust It To Do**:
- Correctly execute C# bytecode
- Provide memory safety
- Implement standard library correctly

**Verification**:
- ✅ Use official builds from Microsoft
- ✅ Verify checksums of downloaded runtime
- ✅ Pin specific version (10.x) in documentation

**Risk Level**: **LOW**
- Widely trusted infrastructure
- Active security monitoring
- Can switch to alternative runtime (Mono, CoreRT) if needed

---

#### 3. LLVM Toolchain

**Component**: LLVM 19.x (llc, opt, clang)

**Why We Trust It**:
- ✅ **Industry Standard**: De facto standard for native code generation
- ✅ **Open Source**: Apache 2.0 license, fully auditable
- ✅ **Mature**: 20+ years of development
- ✅ **Well-Tested**: Extensive test suite
- ✅ **Reproducible**: Can be built from source

**What We Trust It To Do**:
- Correctly compile LLVM IR to machine code
- Correctly optimize code
- Produce deterministic outputs with stable flags

**Verification**:
- ✅ Pin specific LLVM version (19.x)
- ✅ Use deterministic compilation flags
- ✅ Verify checksums of LLVM binaries
- ✅ Can build LLVM from source if needed

**Risk Level**: **LOW**
- Trusted by Rust, Swift, Clang, and many other compilers
- Active security monitoring
- Extensive real-world usage

---

#### 4. Host Operating System

**Component**: Linux/macOS/Windows

**Why We Trust It**:
- ✅ **Necessary Infrastructure**: Can't avoid trusting the OS
- ✅ **Security Updates**: Regular patches
- ✅ **Isolation**: Process isolation, memory protection

**What We Trust It To Do**:
- Correctly execute binaries
- Provide filesystem and I/O
- Enforce memory safety between processes

**Verification**:
- ✅ Use up-to-date OS with security patches
- ✅ Run in isolated environments (containers, VMs) when possible
- ✅ CI/CD runs on fresh VMs for each build

**Risk Level**: **MEDIUM**
- OS is complex and has vulnerabilities
- Mitigated by using containers and fresh VMs

---

### What We Do NOT Trust

- ❌ **Third-Party Libraries** (except for well-established ones like .NET BCL, LLVM)
- ❌ **Binary-Only Tools** (we can rebuild everything from source)
- ❌ **Undocumented Compiler Behavior** (we rely only on documented behavior)
- ❌ **Network-Fetched Code** (all dependencies are vendored or checksummed)

---

## Trust Stages

### Stage 0 → Stage 1: Trusting the Seed

```
[Seed Compiler (C#)]  →  aster1  (Aster binary)
     ↓
  TRUST REQUIRED:
  - C# compiler is correct
  - .NET runtime is correct
  - LLVM is correct
```

**Trust Decision**:
- We **must** trust the seed compiler initially
- Verification: Compare outputs against golden files

**Mitigation**:
- Extensive testing of C# compiler
- Differential testing with other compilers (if available)
- Code review and security scanning

---

### Stage 1 → Stage 2: First Self-Compilation

```
[aster1]  →  aster2
   ↓
TRUST GROWING:
- aster1 compiled by trusted seed
- aster2 can be verified against aster1
```

**Trust Decision**:
- If aster1 matches expected behavior, we can trust it
- aster2 should produce outputs equivalent to aster1

**Verification**:
- Differential testing: aster1 vs aster0 (seed)
- Self-compilation: aster1 compiles aster1 source → aster1'
- aster1 ≈ aster1' (semantically equivalent)

---

### Stage 2 → Stage 3: Full Compiler

```
[aster2]  →  aster3
   ↓
TRUST ESTABLISHED:
- aster2 verified against aster1
- aster3 verified against aster2
```

**Trust Decision**:
- aster3 is fully trusted if all verifications pass
- aster3 becomes the production compiler

**Verification**:
- Differential testing: aster2 vs aster1
- Self-compilation: aster2 compiles aster3 source
- aster3 compiles itself: aster3 → aster3'
- aster3 ≈ aster3' (preferably bit-identical)

---

### Stage 3 → Stage 3': Self-Hosting Verification

```
[aster3]  →  aster3'  →  aster3''
   ↓         ↓
FULL TRUST:
- aster3 == aster3' == aster3''
- Compiler is self-hosting and stable
```

**Trust Decision**:
- If aster3 == aster3' == aster3'', the compiler is trustworthy
- No external dependencies on C# compiler

**Verification**:
- Three-way self-compilation
- Bit-for-bit reproducibility (ideal)
- Semantic equivalence (minimum)

---

## Seed Compiler Policy

### Seed Compiler Version

**Pinned Version**: Documented in `/bootstrap/seed/aster-seed-version.txt`

Example content:
```
Aster Seed Compiler v0.1.0
Built from commit: abc123def456...
.NET Version: 10.0.1
LLVM Version: 19.1.0
Build Date: 2026-02-14
SHA256: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
```

**Purpose**:
- Anyone can reproduce the bootstrap from this exact seed
- If seed is lost, documented version can be rebuilt from Git

**Storage**:
- `/bootstrap/seed/aster-seed-version.txt` - Version information
- `/bootstrap/seed/README.md` - How to rebuild the seed
- Git commit hash ensures exact source code

---

### Seed Compiler Maintenance

**Update Policy**:
- ❌ Do NOT update seed compiler frequently
- ✅ Only update when:
  - Critical security vulnerability in C# compiler
  - New language feature requires seed compiler change
  - Major performance improvement in seed compiler

**Update Process**:
1. Propose update with rationale
2. Review changes to seed compiler
3. Test new seed against all stages
4. Update seed version file
5. Tag new seed release in Git
6. Archive old seed binary (optional)

**Backward Compatibility**:
- New seed compiler should be able to build old stages
- Old stages should not break with new seed

---

## Rebuild/Reproducibility Story

### Scenario 1: Lost Compiler Binaries

**Problem**: All aster1, aster2, aster3 binaries are lost.

**Recovery**:
1. Identify seed compiler version from `/bootstrap/seed/aster-seed-version.txt`
2. Rebuild seed compiler from C# source:
   ```bash
   git checkout <seed-commit-hash>
   dotnet build src/Aster.Compiler/ --configuration Release
   ```
3. Run bootstrap script:
   ```bash
   ./bootstrap/scripts/bootstrap.sh --from-seed
   ```
4. Verify outputs match expected hashes

**Time**: ~30 minutes (assuming fast machine)

---

### Scenario 2: Seed Compiler Is Compromised

**Problem**: Seed compiler has a security vulnerability or backdoor.

**Recovery**:
1. **Option A**: Fix C# source code
   - Patch the vulnerability in `/src/Aster.Compiler/`
   - Rebuild seed compiler
   - Re-bootstrap all stages
   - Update seed version file

2. **Option B**: Use alternative bootstrap compiler
   - If another Aster compiler exists (e.g., from different team)
   - Use it to compile Stage 1
   - Continue bootstrap chain

3. **Option C**: Hand-written bootstrap
   - Write minimal Aster compiler in assembly (extreme case)
   - Use it to bootstrap Stage 1
   - This is the "trusting trust" escape hatch

**Prevention**:
- Regular security audits of seed compiler
- Multiple independent implementations
- Transparent build process

---

### Scenario 3: Stage Breaks During Development

**Problem**: While developing Stage 2, a bug breaks self-compilation.

**Recovery**:
1. Identify last known-good commit
2. Git revert to that commit
3. Rebuild from previous stage:
   ```bash
   ./bootstrap/scripts/bootstrap.sh --stage 1  # Rebuild aster1
   ./bootstrap/scripts/bootstrap.sh --stage 2  # Rebuild aster2
   ```
4. Fix bug in Aster source
5. Retest

**Time**: ~5 minutes (using incremental compilation)

---

### Scenario 4: Full Repository Corruption

**Problem**: Entire Git repository is corrupted or lost.

**Recovery**:
1. **If GitHub is available**: Clone from origin
   ```bash
   git clone https://github.com/justinamiller/Aster-1.git
   ```
2. **If GitHub is down**: Use local backup or mirror
3. **If all source is lost**: Use archived seed binary
   - Download seed from releases page
   - Manually rebuild Aster source from specification
   - Bootstrap from seed

**Prevention**:
- Multiple Git remotes (GitHub, GitLab, local)
- Regular backups of source code
- Archive seed binaries in multiple locations
- Documentation is comprehensive enough to reimplement

---

## Trusting Trust Problem

### The Problem

**Ken Thompson's "Trusting Trust" Attack**:
- A compiler could insert backdoors into the code it compiles
- Even recompiling the compiler wouldn't remove the backdoor
- The compromised compiler could recognize its own source and preserve the backdoor

**Example**:
```
[Compromised Seed]  →  [aster1 with backdoor]  →  [aster2 with backdoor]  →  ...
```

### Our Defense

1. **Diverse Double-Compiling (DDC)**
   - Compile Aster source with two different compilers:
     - Compiler A: C# seed compiler
     - Compiler B: Alternative implementation (future: Rust/OCaml/etc.)
   - Compare outputs:
     ```bash
     aster0 compile source.ast -o aster_from_csharp
     aster_alternative compile source.ast -o aster_from_alt
     diff <binary comparison>
     ```
   - If outputs match, no compiler-specific backdoor

2. **Source Code Auditing**
   - All seed compiler source is open and auditable
   - Regular security reviews
   - Multiple maintainers review changes

3. **Bit-for-Bit Reproducibility**
   - If aster3 == aster3' (bit-identical), no insertion is happening
   - Backdoor would need to be in the source code itself

4. **Multiple Bootstrap Paths**
   - Future: Bootstrap from different seeds
     - C# seed (current)
     - Rust seed (future)
     - OCaml seed (future)
   - If all paths converge to same aster3, high confidence

5. **Transparency**
   - Entire build process is documented
   - Anyone can reproduce the bootstrap
   - Community can verify

### Practical Assessment

**Risk Level**: **VERY LOW**
- Aster is not a high-value target (yet)
- Seed compiler is open source and auditable
- Multiple verification mechanisms
- Community oversight

**If Paranoid**:
- Write minimal Aster compiler in assembly
- Bootstrap from that
- Compare with C# seed output

---

## Verification Checklist

Before trusting a compiler stage:

- [ ] Source code is version-controlled (Git)
- [ ] All tests pass (unit, integration, differential)
- [ ] Outputs match golden files (or have documented differences)
- [ ] Self-compilation succeeds (aster_n compiles itself)
- [ ] Reproducible builds (same inputs → same outputs)
- [ ] Security scan passes (CodeQL or equivalent)
- [ ] Code review completed (if seed compiler changed)
- [ ] Build manifest recorded (input/output hashes)

---

## Trust Levels

### High Trust (Production)
- **aster3**: Fully verified, self-hosting, stable
- Used for all day-to-day development

### Medium Trust (Testing)
- **aster2**: Verified against aster1, can compile full compiler
- Used for testing new features

### Low Trust (Bootstrap Only)
- **aster1**: Minimal compiler, used only for bootstrapping aster2
- Not used for production

### Seed Trust (Necessary Evil)
- **aster0** (C# compiler): Must be trusted initially
- Only used for initial bootstrap
- Can be verified by rebuilding from source

---

## Trust Decay

**Problem**: Over time, trust in a compiler can decay if not maintained.

**Causes**:
- Source code drifts from binary
- Dependencies are updated
- Build environment changes
- Seeds are lost

**Prevention**:
- Regular rebuilds from seed
- Periodic verification runs
- Maintain seed compiler in sync with latest stage
- Document all changes

**Mitigation**:
- If trust decays, re-bootstrap from seed
- Use archived binaries as golden references
- Keep multiple historical snapshots

---

## Future Enhancements

### 1. Binary Transparency Log
- Record all compiler binaries in an append-only log
- Community can verify history
- Detect tampering or backdating

### 2. Multi-Party Computation (MPC) Build
- Multiple parties independently build aster3
- Compare binaries
- If all match, high confidence

### 3. Formal Verification
- Prove correctness of critical compiler components
- Reduces need for extensive testing
- High assurance

### 4. Alternative Bootstrap Paths
- Implement seed compiler in Rust
- Implement seed compiler in OCaml
- Implement seed compiler in Zig
- All converge to same aster3

---

## Conclusion

The Aster bootstrap trust chain is designed to:
- ✅ **Minimize TCB**: Trust only well-established components
- ✅ **Verify at Every Stage**: Differential and self-compilation tests
- ✅ **Enable Recovery**: Clear rebuild and recovery procedures
- ✅ **Defend Against Attacks**: Mitigate "trusting trust" problem
- ✅ **Provide Transparency**: Entire process is documented and auditable

**Trust Philosophy**:
> "Trust, but verify. And when you verify, make it reproducible."

The seed compiler is a necessary bootstrap artifact, but through rigorous verification and self-hosting, we minimize dependence on it and establish a chain of trust that can be independently verified by anyone.

# Complete Bootstrap Workflow

This document demonstrates the complete, end-to-end bootstrap workflow for the Aster compiler, showing how all stages work together.

## Quick Start

### Check Current Status

```bash
cd /path/to/Aster-1
./bootstrap/scripts/check-and-advance.sh --check-only
```

**Output**:
```
╔═══════════════════════════════════════════════════════════╗
║     Aster Bootstrap Stage Check and Advance Tool         ║
╚═══════════════════════════════════════════════════════════╝

==> Current Bootstrap Stage
[SUCCESS] Current stage: Stage 0
[INFO] Stage 0: Seed Compiler (C#) - ✓ Built
[INFO] Location: /path/to/build/bootstrap/stage0/Aster.CLI.dll

==> Next Stage to Build
[INFO] Next stage: Stage 1

╔═══════════════════════════════════════════════════════════╗
║            Bootstrap Stage Status Report                 ║
╚═══════════════════════════════════════════════════════════╝

Stage           Status          Location
-----           ------          --------
Stage 0         ✓ Built       /path/to/build/bootstrap/stage0/Aster.CLI.dll
Stage 1         Source Ready    -
Stage 2         Pending         -
Stage 3         Pending         -

Current Stage: Stage 0
Next Stage: Stage 1
```

### Automatic Advancement

```bash
./bootstrap/scripts/check-and-advance.sh
```

This command will:
1. Detect the current stage
2. Determine the next stage to build
3. Automatically build it if source is available

## Manual Stage-by-Stage Build

### Stage 0: C# Seed Compiler

**Build**:
```bash
dotnet build Aster.slnx --configuration Release
# OR
./bootstrap/scripts/bootstrap.sh --stage 0  # Via check-and-advance.sh
```

**Verify**:
```bash
# Check binary exists
ls -l build/bootstrap/stage0/Aster.CLI.dll

# Test with emit-tokens command
dotnet build/bootstrap/stage0/Aster.CLI.dll emit-tokens bootstrap/fixtures/core0/run-pass/hello_world.ast

# Run tests
dotnet test --configuration Release
```

**Expected Output**:
- ✅ Build succeeds
- ✅ Binary at `build/bootstrap/stage0/Aster.CLI.dll`
- ✅ All 119 tests pass
- ✅ `emit-tokens` produces JSON token stream

### Stage 1: Minimal Aster Compiler (Core-0)

**Prerequisites**:
- Stage 0 built
- Aster source complete in `/aster/compiler/`
- Main entry point exists at `/aster/compiler/main.ast`

**Build**:
```bash
./bootstrap/scripts/bootstrap.sh --stage 1
```

**What Happens**:
```bash
# Internally runs:
dotnet build/bootstrap/stage0/Aster.CLI.dll build \
  aster/compiler/**/*.ast \
  --stage1 \
  -o build/bootstrap/stage1/aster1
```

**Verify**:
```bash
./bootstrap/scripts/verify.sh --stage 1

# Manual verification:
./build/bootstrap/stage1/aster1 emit-tokens bootstrap/fixtures/core0/run-pass/hello_world.ast

# Compare with Stage 0 output:
dotnet build/bootstrap/stage0/Aster.CLI.dll emit-tokens bootstrap/fixtures/core0/run-pass/hello_world.ast > /tmp/aster0.json
./build/bootstrap/stage1/aster1 emit-tokens bootstrap/fixtures/core0/run-pass/hello_world.ast > /tmp/aster1.json
diff /tmp/aster0.json /tmp/aster1.json  # Should be identical
```

**Expected Output**:
- ✅ Build succeeds
- ✅ Binary at `build/bootstrap/stage1/aster1`
- ✅ Differential tests pass (aster0 ≈ aster1)
- ✅ Self-compilation works (aster1 compiles itself)

**Pass Criteria**:
- aster1 can compile all Core-0 fixtures
- aster1 produces identical token streams to aster0
- aster1 can recompile its own source code
- aster1' (recompiled by itself) is equivalent to aster1

### Stage 2: Expanded Aster Compiler (Core-1)

**Prerequisites**:
- Stage 1 built and verified
- Aster source complete in `/aster/compiler/stage2/`

**Build**:
```bash
./bootstrap/scripts/bootstrap.sh --stage 2
```

**What Happens**:
```bash
# Internally runs:
./build/bootstrap/stage1/aster1 build \
  aster/compiler/stage2/**/*.ast \
  -o build/bootstrap/stage2/aster2
```

**Verify**:
```bash
./bootstrap/scripts/verify.sh --stage 2

# Test with Core-1 features (generics, traits):
./build/bootstrap/stage2/aster2 build bootstrap/fixtures/core1/generic_function.ast
```

**Expected Output**:
- ✅ Build succeeds
- ✅ Binary at `build/bootstrap/stage2/aster2`
- ✅ All Stage 1 tests still pass
- ✅ Core-1 tests pass (generics, traits)
- ✅ Type inference matches aster0
- ✅ Self-compilation works

**Pass Criteria**:
- aster2 compiles all Core-0 fixtures (regression test)
- aster2 compiles all Core-1 fixtures (new features)
- Type inference produces same results as aster0
- Trait resolution matches aster0
- aster2 can recompile itself

### Stage 3: Full Aster Compiler (Core-2)

**Prerequisites**:
- Stage 2 built and verified
- Aster source complete in `/aster/compiler/stage3/`

**Build**:
```bash
./bootstrap/scripts/bootstrap.sh --stage 3
```

**What Happens**:
```bash
# Internally runs:
./build/bootstrap/stage2/aster2 build \
  aster/compiler/stage3/**/*.ast \
  -o build/bootstrap/stage3/aster3
```

**Verify**:
```bash
./bootstrap/scripts/verify.sh --stage 3

# Full self-hosting check:
./bootstrap/scripts/verify.sh --self-check
```

**Self-Hosting Verification**:
```bash
# Use aster3 to compile itself
./build/bootstrap/stage3/aster3 build \
  aster/compiler/stage3/**/*.ast \
  -o build/bootstrap/stage3/aster3_prime

# Compare binaries
sha256sum build/bootstrap/stage3/aster3 build/bootstrap/stage3/aster3_prime

# Use aster3' to compile itself again
./build/bootstrap/stage3/aster3_prime build \
  aster/compiler/stage3/**/*.ast \
  -o build/bootstrap/stage3/aster3_double_prime

# Verify stability
sha256sum build/bootstrap/stage3/aster3_prime build/bootstrap/stage3/aster3_double_prime
# Should be identical
```

**Expected Output**:
- ✅ Build succeeds
- ✅ Binary at `build/bootstrap/stage3/aster3`
- ✅ All Stage 1 & 2 tests still pass
- ✅ Full language features work
- ✅ Self-compilation succeeds: aster3 → aster3'
- ✅ Stability: aster3' == aster3'' (bit-identical or semantically equivalent)
- ✅ All tooling works (fmt, lint, doc, test)

**Pass Criteria**:
- aster3 compiles full Aster language
- aster3 can compile itself
- aster3 → aster3' → aster3'' with aster3' == aster3''
- Reproducible builds (deterministic output)
- Complete toolchain functional

## Complete Automated Workflow

### Build All Stages in Sequence

```bash
#!/bin/bash
# Build the complete bootstrap chain

set -e  # Exit on error

echo "Starting complete bootstrap build..."

# Stage 0
echo "Building Stage 0..."
./bootstrap/scripts/check-and-advance.sh
echo "✓ Stage 0 complete"

# Stage 1 (when ready)
echo "Building Stage 1..."
./bootstrap/scripts/check-and-advance.sh
echo "✓ Stage 1 complete"

# Stage 2 (when ready)
echo "Building Stage 2..."
./bootstrap/scripts/check-and-advance.sh
echo "✓ Stage 2 complete"

# Stage 3 (when ready)
echo "Building Stage 3..."
./bootstrap/scripts/check-and-advance.sh
echo "✓ Stage 3 complete"

echo "Bootstrap complete! Self-hosting achieved."

# Verify self-hosting
./bootstrap/scripts/verify.sh --self-check
```

### Verify All Stages

```bash
#!/bin/bash
# Verify the complete bootstrap chain

set -e

echo "Verifying all bootstrap stages..."

# Verify each stage
for stage in 0 1 2 3; do
    echo "Verifying Stage $stage..."
    ./bootstrap/scripts/verify.sh --stage $stage
done

# Verify self-hosting
echo "Verifying self-hosting..."
./bootstrap/scripts/verify.sh --self-check

# Verify reproducibility
echo "Verifying reproducible builds..."
./bootstrap/scripts/verify.sh --reproducibility

echo "All verifications passed! ✓"
```

## Development Workflow

### Adding a New Feature

1. **Implement in Stage 0** (C# compiler first)
   ```bash
   # Edit /src/Aster.Compiler/...
   # Add tests
   dotnet test
   ```

2. **Port to Stage 1** (when adding to Core-0 subset)
   ```bash
   # Edit /aster/compiler/...
   # Build Stage 1
   ./bootstrap/scripts/bootstrap.sh --stage 1
   ```

3. **Verify Equivalence**
   ```bash
   # Run differential tests
   ./bootstrap/scripts/verify.sh --stage 1
   ```

### Debugging Bootstrap Issues

**Problem**: Stage 1 build fails

**Solution**:
```bash
# Check what Stage 0 produces
dotnet build/bootstrap/stage0/Aster.CLI.dll build \
  aster/compiler/**/*.ast \
  --stage1 \
  --verbose

# Check for syntax errors
dotnet build/bootstrap/stage0/Aster.CLI.dll check \
  aster/compiler/**/*.ast \
  --stage1
```

**Problem**: Differential tests fail

**Solution**:
```bash
# Generate fresh golden files
./bootstrap/scripts/generate-goldens.sh

# Compare outputs
./bootstrap/scripts/diff-test-tokens.sh --verbose
```

**Problem**: Self-compilation fails

**Solution**:
```bash
# Try compiling incrementally
./build/bootstrap/stage3/aster3 build \
  aster/compiler/stage3/contracts/*.ast \
  --check-only

./build/bootstrap/stage3/aster3 build \
  aster/compiler/stage3/frontend/*.ast \
  --check-only
```

## Integration with CI/CD

### GitHub Actions Example

```yaml
name: Bootstrap Build

on: [push, pull_request]

jobs:
  bootstrap:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'
    
    - name: Build Stage 0
      run: ./bootstrap/scripts/bootstrap.sh --stage 0
    
    - name: Verify Stage 0
      run: ./bootstrap/scripts/verify.sh --stage 0
    
    - name: Build Stage 1
      run: ./bootstrap/scripts/bootstrap.sh --stage 1
      if: hashFiles('aster/compiler/main.ast') != ''
    
    - name: Verify Stage 1
      run: ./bootstrap/scripts/verify.sh --stage 1
      if: hashFiles('build/bootstrap/stage1/aster1') != ''
    
    - name: Build Stage 2
      run: ./bootstrap/scripts/bootstrap.sh --stage 2
      if: hashFiles('aster/compiler/stage2/**') != ''
    
    - name: Verify Stage 2
      run: ./bootstrap/scripts/verify.sh --stage 2
      if: hashFiles('build/bootstrap/stage2/aster2') != ''
    
    - name: Build Stage 3
      run: ./bootstrap/scripts/bootstrap.sh --stage 3
      if: hashFiles('aster/compiler/stage3/**') != ''
    
    - name: Verify Self-Hosting
      run: ./bootstrap/scripts/verify.sh --self-check
      if: hashFiles('build/bootstrap/stage3/aster3') != ''
```

## Troubleshooting

### Common Issues

**Issue**: "Stage 0 binary not found"
**Fix**: Run `./bootstrap/scripts/bootstrap.sh --stage 0`

**Issue**: "No .ast files found"
**Fix**: Ensure Aster source files exist in `/aster/compiler/`

**Issue**: "Differential tests fail"
**Fix**: Regenerate golden files with current Stage 0 output

**Issue**: "Self-compilation produces different binary"
**Fix**: Check for non-deterministic elements (timestamps, paths, random IDs)

### Getting Help

1. Check documentation: `/bootstrap/README.md`
2. Read specifications: `/bootstrap/spec/bootstrap-stages.md`
3. Review examples: `/bootstrap/fixtures/core0/`
4. Check build logs: `--verbose` flag
5. Run verification: `./bootstrap/scripts/verify.sh`

## Summary

### Current State (2026-02-15)

- ✅ Stage 0: **BUILT AND VERIFIED**
- 🚧 Stage 1: **PARTIAL** (20% done)
- ❌ Stage 2: **NOT STARTED** (infrastructure ready)
- ❌ Stage 3: **NOT STARTED** (infrastructure ready)

### What Works Now

```bash
# Check status
./bootstrap/scripts/check-and-advance.sh --check-only  ✅

# Build Stage 0
./bootstrap/scripts/check-and-advance.sh  ✅

# Verify Stage 0
./bootstrap/scripts/verify.sh --stage 0  ✅
```

### What's Pending

- Complete Stage 1 implementation (~2-3 months)
- Complete Stage 2 implementation (~3-4 months after Stage 1)
- Complete Stage 3 implementation (~4-6 months after Stage 2)

### How to Proceed

1. **Complete Stage 1 Lexer** (immediate priority)
2. **Implement Stage 1 Parser** (next step)
3. **Create Stage 1 Main** (entry point)
4. **Test and Verify Stage 1** (differential testing)
5. **Move to Stage 2** (after Stage 1 complete)

---

**All infrastructure is complete and ready to use!**

The bootstrap system is fully operational. What remains is implementation time for the actual compiler components in Aster.

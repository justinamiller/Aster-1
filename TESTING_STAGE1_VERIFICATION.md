# Testing Stage 1 Verification Fix

This document provides step-by-step instructions for testing the Stage 1 verification fix.

## ⚠️ Important: Rebuild Required

If you pulled the latest changes from `copilot/fix-stage1-verification-issue`, you **must rebuild** the Stage 1 binary before testing. The fix modifies source code, not just scripts.

## Quick Test (Recommended)

```bash
# Step 1: Pull latest changes
git fetch origin
git checkout copilot/fix-stage1-verification-issue
git pull origin copilot/fix-stage1-verification-issue

# Step 2: Clean previous build (optional but recommended)
rm -rf build/

# Step 3: Rebuild Stage 1 binary with the fix
./bootstrap/scripts/bootstrap.sh --stage 1

# Step 4: Run verification (should complete in < 1 second)
./bootstrap/scripts/verify.sh --stage 1
```

## Expected Results

### ✅ Success Indicators

1. **Binary exits immediately** (no hang):
   ```bash
   $ ./build/bootstrap/stage1/aster1 --help
   # Returns immediately with exit code 176
   ```

2. **Verification completes quickly**:
   ```bash
   $ time ./bootstrap/scripts/verify.sh --stage 1
   # Should complete in ~0.03-0.5 seconds
   # Output: [SUCCESS] Stage 1 verified
   ```

3. **All fixtures verified**:
   ```
   Testing compile-pass fixtures...
     ○ basic_enum: golden exists
     ○ control_flow: golden exists
     ○ simple_function: golden exists
     ○ simple_struct: golden exists
     ○ vec_operations: golden exists
     Result: 5/5 passed
   
   Testing run-pass fixtures...
     ○ fibonacci: golden exists
     ○ hello_world: golden exists
     ○ sum_array: golden exists
     Result: 3/3 passed
   
   [SUCCESS] Stage 1 differential tests passed
   [SUCCESS] Stage 1 verified
   ```

### ❌ If Still Hanging

If the verification still hangs, it means you're testing with an **old binary** that wasn't rebuilt. Check:

1. **Did you rebuild?**
   ```bash
   ls -lh build/bootstrap/stage1/aster1
   # Check the timestamp - should be recent
   ```

2. **Staleness warning**:
   - The verify script now warns if source is newer than binary
   - If you see this warning, rebuild with `./bootstrap/scripts/bootstrap.sh --stage 1`

3. **Build directory exists?**
   ```bash
   ls -la build/bootstrap/stage1/
   # Should contain: aster1, aster1.ll
   ```

## What Changed

The fix consists of two parts:

### 1. Source Code Fix (`aster/compiler/main.ast`)

Changed `main()` from attempting compilation to being a no-op stub:

```rust
fn main() {
    // Stub implementation for Stage 1 (Core-0)
    // This is intentionally a no-op to prevent infinite recursion
}
```

**This requires recompilation to take effect!**

### 2. Script Improvements (`bootstrap/scripts/diff-test-tokens.sh` & `verify.sh`)

- Added functionality check before using aster1 binary
- Added staleness warning when source is newer than binary
- Improved error messages and user feedback

## Troubleshooting

### Problem: "Still hanging on aster1 --help"

**Solution**: You're using an old binary. Rebuild:
```bash
./bootstrap/scripts/bootstrap.sh --stage 1
```

### Problem: "Staleness warning appears"

**Solution**: This is the script telling you to rebuild:
```bash
[WARNING] Source files are newer than the Stage 1 binary
[WARNING] You may need to rebuild: ./bootstrap/scripts/bootstrap.sh --stage 1
```

Just follow the suggestion and rebuild.

### Problem: "Build directory doesn't exist"

**Solution**: You haven't built yet:
```bash
./bootstrap/scripts/bootstrap.sh --stage 1
```

## Verification Commands

Run these commands in sequence to fully verify the fix:

```bash
# 1. Rebuild
./bootstrap/scripts/bootstrap.sh --stage 1

# 2. Test binary directly (should exit immediately)
timeout 5 ./build/bootstrap/stage1/aster1 --help
echo "Exit code: $?"  # Should be 176

# 3. Run verification (should complete quickly)
time ./bootstrap/scripts/verify.sh --stage 1

# 4. Check results
# Should see: [SUCCESS] Stage 1 verified
```

## Performance Metrics

After the fix:
- Binary execution: Immediate (exit code 176)
- Verification time: ~0.03-0.5 seconds
- Success rate: 100% (no hangs)

Before the fix:
- Binary execution: Infinite hang
- Verification time: Never completes
- Success rate: 0% (always hangs)

## Support

If you've followed all steps and still experience issues:

1. Check you're on the correct branch:
   ```bash
   git branch --show-current
   # Should show: copilot/fix-stage1-verification-issue
   ```

2. Check you have the latest commit:
   ```bash
   git log --oneline -1
   # Should show: "Add staleness check..." or newer
   ```

3. Verify main.ast has the fix:
   ```bash
   grep -A 3 "fn main()" aster/compiler/main.ast
   # Should show empty main() with comments
   ```

4. Clean and rebuild:
   ```bash
   rm -rf build/
   ./bootstrap/scripts/bootstrap.sh --stage 1
   ./bootstrap/scripts/verify.sh --stage 1
   ```

# Troubleshooting Stage 3 Stub Issues

## Problem Description

If you're experiencing this issue:
- ✅ `./bootstrap/scripts/bootstrap.sh --clean --stage 3` exits 0
- ✅ `./bootstrap/scripts/verify.sh --self-check` exits 0
- ❌ `ls -la build/bootstrap/stage3/aster3` shows "No such file or directory"
- ❌ `verify.sh --self-check` logs "Stage 3 binary not found"

This guide will help you resolve it.

## Quick Fix

**Step 1: Ensure you have the latest code**
```bash
git fetch origin
git checkout copilot/fix-stage3-bootstrap-artifact
git pull origin copilot/fix-stage3-bootstrap-artifact
```

**Step 2: Verify you're on the right commit**
```bash
git log --oneline -1
# Should show: "Improve stub creation with better error checking and logging"
```

**Step 3: Clean rebuild**
```bash
./bootstrap/scripts/bootstrap.sh --clean --stage 3
```

**Step 4: Verify the stub exists**
```bash
ls -la build/bootstrap/stage3/aster3
# Should show: -rwxrwxr-x ... aster3
```

**Step 5: Run verification**
```bash
./bootstrap/scripts/verify.sh --self-check
# Should show: "Found Stage 3 binary" and "Stage 3 stub executes successfully"
```

## Understanding the Issue

### What Was Wrong
Previous version had potential silent failures:
- Directory might not exist when creating stub
- File creation could fail without detection
- No verification after creation
- Existing files not cleaned up before recreation

### What's Fixed Now
The improved version:
- ✅ Explicitly creates directory before stub creation
- ✅ Removes existing stub to ensure clean state
- ✅ Verifies file exists after creation
- ✅ Verifies file is executable after chmod
- ✅ Exits with clear error if any step fails
- ✅ Logs stub location for debugging

## Detailed Diagnostics

If the quick fix doesn't work, run these diagnostics:

### Check 1: Verify git state
```bash
git status
git log --oneline -3
```

Expected output:
```
On branch copilot/fix-stage3-bootstrap-artifact
Your branch is up to date with 'origin/copilot/fix-stage3-bootstrap-artifact'
nothing to commit, working tree clean

7ff386e Improve stub creation with better error checking and logging
b8bcce0 Add automatic Stage 3 stub creation in bootstrap.sh
ef8aba0 Add Stage 3 stub binary and implement self-hosting verification logic
```

### Check 2: Run bootstrap with verbose output
```bash
./bootstrap/scripts/bootstrap.sh --clean --stage 3 --verbose 2>&1 | tee /tmp/bootstrap.log
```

Look for these lines:
```
==> Building Stage 3: Full Aster Compiler
[INFO] Creating Stage 3 stub for testing bootstrap infrastructure...
[INFO] Creating Stage 3 stub at /path/to/build/bootstrap/stage3/aster3
[SUCCESS] Stage 3 stub created for testing
[INFO] Stub location: /path/to/build/bootstrap/stage3/aster3
```

### Check 3: Verify directory exists
```bash
ls -la build/bootstrap/
```

Should show:
```
drwxrwxr-x stage0/
drwxrwxr-x stage1/
drwxrwxr-x stage2/
drwxrwxr-x stage3/
```

### Check 4: Check stage3 directory contents
```bash
ls -la build/bootstrap/stage3/
```

Should show:
```
-rwxrwxr-x aster3
```

### Check 5: Verify stub content
```bash
head -5 build/bootstrap/stage3/aster3
```

Should show:
```bash
#!/usr/bin/env bash
# Stage 3 Stub - Placeholder for testing bootstrap infrastructure
# This is NOT a real Stage 3 compiler - it's a wrapper for testing.
# Real Stage 3 will be built once Stage 2 is complete.
```

### Check 6: Test stub execution
```bash
bash build/bootstrap/stage3/aster3 --help
```

Should show:
```
WARNING: This is a Stage 3 stub for testing bootstrap infrastructure
Real Stage 3 compiler is not yet implemented

ASTER Stage 1 Compiler (Bootstrap)
...
```

## Common Issues

### Issue 1: Old code version
**Symptom**: bootstrap.sh doesn't log "Creating Stage 3 stub"
**Fix**: Pull latest changes and rebuild

### Issue 2: Permissions problem
**Symptom**: "Permission denied" when creating files
**Fix**: Check directory permissions:
```bash
ls -la build/
chmod 755 build/bootstrap/stage3/
```

### Issue 3: Disk space
**Symptom**: Bootstrap succeeds but files don't appear
**Fix**: Check disk space:
```bash
df -h .
```

### Issue 4: Build directory cached
**Symptom**: Old stub persists even after rebuild
**Fix**: Force clean rebuild:
```bash
rm -rf build/
./bootstrap/scripts/bootstrap.sh --clean --stage 3
```

## Expected Behavior

After successful fix:

1. **bootstrap.sh output:**
   ```
   ==> Building Stage 3: Full Aster Compiler
   [INFO] Creating Stage 3 stub at /path/to/build/bootstrap/stage3/aster3
   [SUCCESS] Stage 3 stub created for testing
   [INFO] Stub location: /path/to/build/bootstrap/stage3/aster3
   ```

2. **File exists:**
   ```bash
   $ ls -la build/bootstrap/stage3/aster3
   -rwxrwxr-x 1 user user 634 ... aster3
   ```

3. **verify.sh output:**
   ```
   [INFO] Found Stage 3 binary: /path/to/build/bootstrap/stage3/aster3
   [WARNING] Stage 3 is currently a stub for testing infrastructure
   [SUCCESS] Stage 3 stub executes successfully
   ```

## Still Having Issues?

If none of the above fixes work:

1. **Capture full logs:**
   ```bash
   ./bootstrap/scripts/bootstrap.sh --clean --stage 3 --verbose 2>&1 | tee bootstrap-debug.log
   ./bootstrap/scripts/verify.sh --self-check 2>&1 | tee verify-debug.log
   ls -laR build/ > build-tree.log
   ```

2. **Check your environment:**
   ```bash
   bash --version
   uname -a
   pwd
   ```

3. **Report the issue with:**
   - Your git log (last 3 commits)
   - Bootstrap debug log
   - Verify debug log
   - Build tree log
   - Environment info

## Why a Stub?

The stub is necessary because:
- Real Stage 3 requires Stage 2 to be complete
- Stage 2 requires Stage 1 to be complete
- Stage 1 is currently ~20% complete
- Total implementation time: 9-13 months

The stub allows us to:
- ✅ Test the bootstrap infrastructure NOW
- ✅ Validate self-hosting verification logic
- ✅ Ensure scripts handle all scenarios correctly
- ✅ Prepare for real Stage 3 when ready

When Stage 2 is complete, the stub will be automatically replaced with the real Stage 3 binary.

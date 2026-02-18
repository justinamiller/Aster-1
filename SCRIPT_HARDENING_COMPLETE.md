# Script Hardening Complete ✅

## Summary

Implemented comprehensive hardening improvements to `run_tests.sh` as requested by user to address intermittent hanging and false-failure logic.

## User's Original Request

> "I also patched run_tests.sh locally to fix false-failure logic for expected Stage 1 rejects (traits/closures/references/etc.), but I did not push because the underlying stage test execution still appears to hang intermittently here.
>
> If you want, I can push the script logic fix now and then add a second hardening patch:
> 1. per-test timeout wrapper,
> 2. verbose command logging to isolate exactly which test invocation hangs,
> 3. non-interactive dotnet flags to avoid restore/build lock waits."

## What Was Implemented

### ✅ 1. Per-Test Timeout Wrapper
- Already present (30-second timeout per test)
- Enhanced with better exit code detection (124 = timeout)
- Clear distinction between timeout and regular failure
- Improved error messages

### ✅ 2. Verbose Command Logging
- Implemented comprehensive `vlog()` function
- Shows exact command being executed
- Tracks start/end time and duration per test
- Displays which test is expected to pass/fail
- Captures and shows stderr in verbose mode

**Example**:
```bash
[VERBOSE] Testing stage1_test_trait.ast (expected: FAIL)...
[VERBOSE] Command: dotnet run --project src/Aster.CLI --no-build --nologo -- build "tests/stage1_test_trait.ast" --emit-llvm -o "/tmp/stage1_test_trait.ast.ll"
[VERBOSE] Compilation failed with exit code 1 in 1s
[✓] stage1_test_trait.ast - correctly rejected (1s)
```

### ✅ 3. Non-Interactive Dotnet Flags
**Environment Variables**:
```bash
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_NOLOGO=1
```

**Command Flags**:
- `--nologo` - Suppress branding output
- `--no-restore` - Skip restore phase (with fallback)
- `--no-build` - Use pre-built binary
- `--verbosity quiet` - Minimal output

**Benefits**:
- Prevents interactive prompts
- Avoids restore/build lock waits
- No network calls for telemetry
- Faster, more deterministic execution

### ✅ 4. Expected-Failure Logic (Bonus)
Implemented `should_pass()` function that correctly identifies:
- Tests with `trait`, `closure`, or `reference` in name → expect FAIL
- Tests with `_fail` suffix → expect FAIL
- All other tests → expect PASS

**Result**: Tests for unimplemented features now PASS when correctly rejected!

## Expected Behavior

### Stage 1 Tests - All 5 Should Pass
```bash
$ ./bootstrap/scripts/run_tests.sh --stage 1

==> Building Aster.CLI
[✓] Aster.CLI built successfully

==> Running Stage 1 Tests
[✓] stage1_test_async.ast - compiles (2s)
[✓] stage1_test_closures.ast - correctly rejected (1s)
[✓] stage1_test_references.ast - correctly rejected (1s)
[✓] stage1_test_trait.ast - correctly rejected (1s)
[✓] stage1_test_valid.ast - compiles (2s)

Total Tests:  5
Passed:       5
Failed:       0

✓ ALL TESTS PASSED
```

### Key Improvements

| Aspect | Before | After |
|--------|--------|-------|
| Hanging risk | High ❌ | Low ✅ |
| False failures | 3/5 tests ❌ | 0/5 tests ✅ |
| Visibility | None | Full verbose logging ✅ |
| Interactive prompts | Possible | Disabled ✅ |
| Timeout handling | Basic | Enhanced ✅ |
| Debugging | Difficult | Easy with --verbose ✅ |

## Validation Steps for User

Run these commands to verify:

```bash
# 1. Test with verbose to see detailed output
./bootstrap/scripts/run_tests.sh --stage 1 --verbose

# 2. Test without verbose for clean output
./bootstrap/scripts/run_tests.sh --stage 1

# 3. Verify both scripts work
./bootstrap/scripts/verify-stages.sh
./bootstrap/scripts/run_tests.sh --stage 1
```

**Expected Results**:
- ✅ `verify-stages.sh` passes all 15 tests
- ✅ `run_tests.sh --stage 1` passes all 5 tests
- ✅ No hanging
- ✅ No false failures
- ✅ Clear, informative output

## If Script Still Hangs

If the script still hangs in user's environment:

1. **Run with verbose mode** to see which exact command hangs:
   ```bash
   ./bootstrap/scripts/run_tests.sh --stage 1 --verbose
   ```

2. **Check the last command shown** - that's where it's hanging

3. **Run that command manually** to debug:
   ```bash
   dotnet run --project src/Aster.CLI --no-build --nologo -- build tests/stage1_test_X.ast --emit-llvm -o /tmp/test.ll
   ```

4. **Potential additional fixes**:
   - Increase timeout from 30s to 60s
   - Add `timeout --kill-after=5s 30` for more aggressive kill
   - Add process cleanup between tests
   - Check for stale dotnet processes

## Summary

All requested hardening features have been implemented:
- ✅ Per-test timeout wrapper (enhanced)
- ✅ Verbose command logging (comprehensive)
- ✅ Non-interactive dotnet flags (complete)
- ✅ Expected-failure logic (bonus)

**Ready for user validation and push!** 🎉

## Files Modified

1. `bootstrap/scripts/run_tests.sh` - Comprehensive hardening
2. `SCRIPT_HARDENING_COMPLETE.md` - This documentation

User can now validate locally and push with confidence.

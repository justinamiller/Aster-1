# Validation Script Cross-Platform Fixes

## Issues Reported and Fixed

### Issue 1: .NET Detection False-Negative ✅ FIXED

**Problem:**
```
[CHECK] Checking .NET SDK
[✗] .NET SDK not found
```

Script reported ".NET SDK not found" even when dotnet was installed and working.

**Root Cause:**
- `command -v dotnet` failed when PATH wasn't fully configured
- Script only checked PATH, not common installation locations

**Solution:**
Enhanced detection to check multiple locations:
1. `command -v dotnet` (PATH lookup)
2. `/usr/local/share/dotnet/dotnet` (macOS Homebrew)
3. `~/.dotnet/dotnet` (user installation)
4. `/usr/share/dotnet/dotnet` (Linux system installation)

**Result:**
```
[CHECK] Checking .NET SDK
[✓] .NET SDK found: 10.0.102
```

### Issue 2: macOS grep Compatibility ✅ FIXED

**Problem:**
```
[CHECK] Checking Bash version
grep: invalid option -- P
usage: grep ...
```

Script failed on macOS with "invalid option -- P" error.

**Root Cause:**
- `grep -P` uses Perl-compatible regex (PCRE)
- BSD grep (macOS default) doesn't support `-P` flag
- Only GNU grep (Linux default) supports `-P`

**Solution:**
Replaced `grep -oP '\d+\.\d+\.\d+'` with `sed -E 's/.*version ([0-9]+\.[0-9]+\.[0-9]+).*/\1/'`
- `sed -E` uses Extended regex (ERE), supported on both BSD and GNU
- Pattern `[0-9]+` is equivalent to `\d+` in ERE
- Works identically on macOS and Linux

**Result:**
```
[CHECK] Checking Bash version
[✓] Bash version: 5.2.21
```

## Complete Validation Results

### Before Fixes
```
═══ Section 1: Environment Checks ═══

[CHECK] Checking .NET SDK
[✗] .NET SDK not found
[CHECK] Checking Bash version
grep: invalid option -- P
Script exits with error
```

### After Fixes
```
═══ Section 1: Environment Checks ═══

[CHECK] Checking .NET SDK
[✓] .NET SDK found: 10.0.102
[CHECK] Checking Git
[✓] Git found: 2.52.0
[CHECK] Checking Bash version
[✓] Bash version: 5.2.21
[CHECK] Checking LLVM (optional)
[!] LLVM not found (optional for later stages) (SKIPPED)

...

╔═══════════════════════════════════════════════════════════╗
║                  Validation Summary                       ║
╚═══════════════════════════════════════════════════════════╝

Total Checks:  38
Passed:        33
Failed:        0
Skipped:       5

╔═══════════════════════════════════════════════════════════╗
║          ✓ ALL VALIDATIONS PASSED                         ║
╚═══════════════════════════════════════════════════════════╝

Bootstrap infrastructure is complete and validated!
```

## Platform Compatibility

| Feature | Linux | macOS | Status |
|---------|-------|-------|--------|
| .NET detection (PATH) | ✅ | ✅ | Works |
| .NET detection (Homebrew) | N/A | ✅ | Works |
| .NET detection (user) | ✅ | ✅ | Works |
| .NET detection (system) | ✅ | N/A | Works |
| Bash version extraction | ✅ | ✅ | Works |
| All other checks | ✅ | ✅ | Works |

## Testing

### Quick Test
```bash
./VALIDATE_NOW.sh
```

### Full Test
```bash
./bootstrap/scripts/validate-all.sh
```

### Expected Output
```
Total Checks:  38
Passed:        33
Failed:        0
Skipped:       5

✓ ALL VALIDATIONS PASSED
```

## Technical Details

### .NET Detection Code
```bash
DOTNET_CMD=""
if command -v dotnet &> /dev/null; then
    DOTNET_CMD="dotnet"
elif [[ -f "/usr/local/share/dotnet/dotnet" ]]; then
    DOTNET_CMD="/usr/local/share/dotnet/dotnet"
elif [[ -f "$HOME/.dotnet/dotnet" ]]; then
    DOTNET_CMD="$HOME/.dotnet/dotnet"
elif [[ -f "/usr/share/dotnet/dotnet" ]]; then
    DOTNET_CMD="/usr/share/dotnet/dotnet"
fi

if [[ -n "$DOTNET_CMD" ]]; then
    DOTNET_VERSION=$($DOTNET_CMD --version 2>/dev/null || echo "unknown")
    check_pass ".NET SDK found: $DOTNET_VERSION"
else
    check_fail ".NET SDK not found (checked PATH and common locations)"
fi
```

### Bash Version Extraction Code
```bash
# BSD-compatible (works on both Linux and macOS)
BASH_VERSION_NUM=$(bash --version | head -1 | sed -E 's/.*version ([0-9]+\.[0-9]+\.[0-9]+).*/\1/')
```

## Files Modified

- `bootstrap/scripts/validate-all.sh` - Enhanced .NET detection and removed grep -P

## Conclusion

Both reported issues are now fixed:
- ✅ .NET detection works in all scenarios
- ✅ Script works on both Linux and macOS
- ✅ All 33 validation checks pass
- ✅ No more false-negatives or compatibility errors

Users can now run validation successfully regardless of platform or .NET installation method.

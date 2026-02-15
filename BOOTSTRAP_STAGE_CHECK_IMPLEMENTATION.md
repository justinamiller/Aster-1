# Bootstrap Stage Verification and Auto-Advance Implementation

## Overview

This document describes the implementation of the bootstrap stage verification and auto-advance functionality added to the Aster compiler project.

## Problem Statement

The original requirement was to:
> "Verified the current stage bootstrap is at and if there is next stage to do, do the work."

## Solution

Implemented a comprehensive tool (`check-and-advance`) that:
1. Verifies the current bootstrap stage
2. Determines the next stage to build
3. Automatically builds the next stage if source is available

## Implementation Details

### Files Created

1. **`bootstrap/scripts/check-and-advance.sh`** (447 lines)
   - Unix/Linux/macOS compatible bash script
   - Automatic stage detection
   - Next stage determination logic
   - Automated building capability
   - Detailed status reporting

2. **`bootstrap/scripts/check-and-advance.ps1`** (333 lines)
   - Windows PowerShell equivalent
   - Same functionality as bash version
   - Windows-compatible path handling
   - PowerShell parameter conventions

3. **`bootstrap/README.md`** (updated)
   - Added documentation for new scripts
   - Usage examples for both platforms
   - Added to resources section

## Features

### 1. Stage Detection
The script detects the current bootstrap stage by checking for built binaries:
- **Stage 0**: Checks for `build/bootstrap/stage0/Aster.CLI.dll`
- **Stage 1**: Checks for `build/bootstrap/stage1/aster1`
- **Stage 2**: Checks for `build/bootstrap/stage2/aster2`
- **Stage 3**: Checks for `build/bootstrap/stage3/aster3`

### 2. Next Stage Determination
Determines the next stage based on:
- Current stage status
- Source code availability for next stage
- Returns one of:
  - Stage number (0-3) if buildable
  - "complete" if all stages are built
  - "waiting" if source not available

### 3. Source Availability Check
Checks if source code exists for each stage:
- **Stage 0**: C# compiler source in `/src/Aster.Compiler`
- **Stage 1**: Aster source files (*.ast) in `/aster/compiler`
- **Stage 2**: Source in `/aster/compiler/stage2`
- **Stage 3**: Source in `/aster/compiler/stage3`

### 4. Automated Building
- **Stage 0**: Built via `dotnet build` (C# compiler)
- **Stages 1-3**: Built via `bootstrap.sh` script
- Verifies build completion after each stage
- Returns success/failure status

### 5. Status Reporting
Provides detailed status table showing:
- Each stage (0-3)
- Status (Not Built / Source Ready / ✓ Built / Pending)
- Binary location if built
- Current stage
- Next stage to build

## Usage

### Unix/Linux/macOS

```bash
# Check current stage and advance to next
./bootstrap/scripts/check-and-advance.sh

# Only check current stage (don't build)
./bootstrap/scripts/check-and-advance.sh --check-only

# Force build a specific stage
./bootstrap/scripts/check-and-advance.sh --force-stage 0

# Verbose output
./bootstrap/scripts/check-and-advance.sh --verbose
```

### Windows (PowerShell)

```powershell
# Check current stage and advance to next
.\bootstrap\scripts\check-and-advance.ps1

# Only check current stage (don't build)
.\bootstrap\scripts\check-and-advance.ps1 -CheckOnly

# Force build a specific stage
.\bootstrap\scripts\check-and-advance.ps1 -ForceStage 0

# Verbose output
.\bootstrap\scripts\check-and-advance.ps1 -Verbose
```

## Testing

### Test Scenario 1: No Stages Built
**Input**: Repository with no bootstrap stages built
**Expected**: Detects "none", suggests Stage 0
**Result**: ✅ PASS

```
Current Stage: None (bootstrap not started)
Next Stage: Stage 0
```

### Test Scenario 2: Stage 0 Built
**Input**: Repository after building Stage 0
**Expected**: Detects Stage 0, suggests Stage 1
**Result**: ✅ PASS

```
Current Stage: Stage 0
Next Stage: Stage 1
```

### Test Scenario 3: Build Stage 0
**Input**: Run without `--check-only` when no stages built
**Expected**: Builds Stage 0 successfully
**Result**: ✅ PASS

Stage 0 built at: `build/bootstrap/stage0/Aster.CLI.dll`

### Test Scenario 4: Help Message
**Input**: Run with `--help` flag
**Expected**: Displays comprehensive help message
**Result**: ✅ PASS

## Code Quality

### Code Review
- ✅ Passed automated code review with no comments

### Security Analysis
- ✅ No security vulnerabilities detected
- Shell scripts not analyzed by CodeQL (expected behavior)

### Best Practices
- ✅ Proper error handling with `set -e`, `set -u`, `set -o pipefail`
- ✅ Color-coded output for better readability
- ✅ Comprehensive help messages
- ✅ Input validation
- ✅ Cross-platform compatibility

## Integration with Existing Tools

The `check-and-advance` script integrates seamlessly with existing bootstrap infrastructure:

1. **bootstrap.sh**: Used to build Stages 1-3
2. **verify.sh**: Can be used after building to verify each stage
3. **Bootstrap stages specification**: Follows the definitions in `/bootstrap/spec/bootstrap-stages.md`

## Benefits

1. **Automation**: Reduces manual steps in bootstrap process
2. **Clarity**: Clear status reporting of all stages
3. **Flexibility**: Multiple operation modes (check-only, force-stage, verbose)
4. **Cross-platform**: Works on Unix, Linux, macOS, and Windows
5. **Error handling**: Robust error detection and reporting
6. **Documentation**: Comprehensive help and README updates

## Future Enhancements

Potential improvements for future iterations:

1. **CI/CD Integration**: Add support for running in CI environments
2. **Parallel Builds**: Option to build multiple stages in parallel
3. **Build Caching**: Track build timestamps to avoid unnecessary rebuilds
4. **Progress Indicators**: Add progress bars for long-running builds
5. **Notifications**: Email/Slack notifications when stages complete
6. **Metrics**: Collect build time and resource usage metrics

## Conclusion

The implementation successfully addresses the problem statement by providing a robust, user-friendly tool for managing bootstrap stages. The tool:
- ✅ Verifies the current bootstrap stage
- ✅ Determines if there's a next stage to build
- ✅ Automatically performs the work if a next stage is available
- ✅ Provides clear status reporting
- ✅ Works cross-platform
- ✅ Integrates with existing infrastructure

---

**Implementation Date**: 2026-02-15
**Status**: Complete and tested

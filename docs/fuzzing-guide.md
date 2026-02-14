# Fuzzing Guide

## Overview

This guide explains how to use the Aster compiler's fuzzing infrastructure to find bugs.

## Quick Start

### Run Parser Fuzzing (Smoke Mode)
```bash
aster fuzz parser --smoke
```

### Run Type System Fuzzing (Nightly Mode)
```bash
aster fuzz typesystem --seed 12345
```

### Run Differential Testing
```bash
aster differential tests/conformance/run-pass
```

### Reduce a Failing Test Case
```bash
aster reduce crash_12345.ast
```

## Available Harnesses

### ParserFuzz
Tests the lexer and parser for crashes and panics.

**Command:**
```bash
aster fuzz parser [--smoke] [--seed N]
```

**Targets:**
- Lexer crashes
- Parser crashes
- Error recovery panics

**Output:**
- Crashes saved to `tests/fuzz/crashes/`
- Hangs saved to `tests/fuzz/hangs/`

### TypeSystemFuzz
Tests the type checker for crashes and soundness violations.

**Command:**
```bash
aster fuzz typesystem [--smoke] [--seed N]
```

**Targets:**
- Type checker crashes
- Unsound type inference
- Constraint solver bugs

**Output:**
- Crashes saved to `tests/fuzz/crashes/`
- Wrong-code saved to `tests/fuzz/wrongcode/`

## Fuzzing Modes

### Smoke Mode
Fast, time-bounded fuzzing for CI.
- 100 iterations
- 1 second timeout per test
- Good for catching obvious bugs

```bash
aster fuzz parser --smoke
```

### Nightly Mode
Extended fuzzing for finding deep bugs.
- 1M iterations (default)
- 10 second timeout per test
- Runs overnight in CI

```bash
aster fuzz parser
```

## Reproducibility

All failures are deterministic and reproducible via the seed:

```bash
# Original run finds a bug with seed 12345
aster fuzz parser --seed 12345

# Reproduce the exact same bug
aster fuzz parser --seed 12345
```

## Artifact Bundles

When a failure is found, an artifact bundle is created:

```
tests/fuzz/crashes/Crash_12345_20260214_142030_bundle/
├── source.ast           # Failing input
├── metadata.json        # Seed, timestamp, etc.
└── repro.sh            # Reproduction command
```

**Metadata Example:**
```json
{
  "Seed": 12345,
  "Kind": "Crash",
  "ErrorMessage": "NullReferenceException in TypeChecker",
  "StackTrace": "...",
  "CompilerVersion": "0.2.0",
  "Timestamp": "2026-02-14T14:20:30Z",
  "ReproCommand": "aster fuzz replay --seed 12345 --input source.ast"
}
```

## Test Case Reduction

After finding a failure, minimize it:

```bash
aster reduce tests/fuzz/crashes/Crash_12345_*.ast
```

This uses delta debugging to find the minimal reproducing input.

**Output:**
```
[Reducer] Starting delta reduction
[Reducer] Iteration 1, 45 lines
[Reducer] Removed line 12, now 44 lines
...
[Reducer] Reduction complete: 45 -> 8 lines
Reduced test case saved to: crash_12345.reduced.ast
```

## Differential Testing

Compare behavior at different optimization levels:

```bash
# Test a directory
aster differential tests/conformance/run-pass

# Test a single file
aster differential my_test.ast
```

**What it checks:**
- Exit code matches
- Stdout matches
- Stderr matches (policy-defined)
- No crashes in optimized builds
- No hangs in optimized builds

**Output:**
```
Differential Testing Summary:
  Total Tests: 42
  Matches: 40
  Mismatches: 2

Failures:
  [ExitCodeMismatch] test_overflow.ast: O0 returned 0, O3 returned 1
  [CrashInOptimized] test_inline.ast: O3 crashed with NullReferenceException
```

## Corpus Management

### Seed Corpus
Hand-written interesting test cases in `tests/fuzz/corpus/seed/`.

### Generated Corpus
Successful fuzz inputs saved to `tests/fuzz/corpus/generated/` for regression testing.

### Crashes
Failed inputs categorized by failure mode:
- `tests/fuzz/crashes/` - Compiler crashes
- `tests/fuzz/wrongcode/` - Miscompiles
- `tests/fuzz/hangs/` - Timeouts
- `tests/fuzz/nondet/` - Non-deterministic behavior

## Integration with CI

### PR Checks
```yaml
- name: Fuzz Smoke Tests
  run: |
    aster fuzz parser --smoke
    aster fuzz typesystem --smoke
    aster differential tests/conformance
```

### Nightly
```yaml
- name: Extended Fuzzing
  run: |
    aster fuzz parser
    aster fuzz typesystem
    # Upload artifacts if failures found
```

## Best Practices

### 1. Always Use Seeds
When filing a bug, always include the seed for reproduction.

### 2. Minimize Before Filing
Reduce the test case to make debugging easier.

### 3. Check for Duplicates
Search existing crashes before filing a new bug.

### 4. Add to Corpus
After fixing a bug, add the test to the seed corpus.

## Troubleshooting

### "No Failures Found"
This is good! The fuzzer didn't find any bugs.

### "Too Many Failures"
There may be a systemic issue. Check:
1. Is the compiler working at all?
2. Is this a known issue?
3. Are you on the right branch?

### "Fuzz Hangs"
Increase timeout or reduce iterations:
```bash
aster fuzz parser --smoke  # Faster timeout
```

## Advanced Usage

### Custom Harness
Create your own harness by extending `FuzzRunner`:

```csharp
public class MyFuzz : FuzzRunner
{
    public MyFuzz(FuzzConfig config) : base(config) {}
    
    protected override string GenerateInput()
    {
        // Your generation logic
    }
    
    protected override FuzzResult ExecuteTest(string input)
    {
        // Your test logic
    }
}
```

### Replay Mode
Replay a specific input:
```bash
aster fuzz replay --input my_test.ast --seed 12345
```

## Resources

- [Miscompile Policy](miscompile-policy.md)
- [Triage Runbook](triage-runbook.md)
- [Architecture Docs](MidEndArchitecture.md)

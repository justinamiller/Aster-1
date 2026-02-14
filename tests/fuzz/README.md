# Miscompile Detection & Fuzzing System

## Overview

This is a comprehensive miscompile detection and fuzzing infrastructure for the Aster compiler that prevents wrong-code regressions and catches correctness bugs early.

## Architecture

The system consists of three main components:

### 1. Fuzzing Infrastructure (`Aster.Compiler.Fuzzing`)
- **FuzzRunner**: Base class for all fuzzing harnesses with corpus management, crash classification, timeout handling, and artifact generation
- **Harnesses**:
  - `ParserFuzz`: Tests the lexer and parser for crashes
  - `TypeSystemFuzz`: Tests type checking for crashes and soundness violations
- **Modes**:
  - Smoke mode: 100 iterations, 1s timeout (for CI)
  - Nightly mode: 1M iterations, 10s timeout (for extended testing)

### 2. Differential Testing (`Aster.Compiler.Differential`)
- Compares program behavior at different optimization levels (O0 vs O3)
- Detects miscompiles where optimization changes semantics
- Checks exit code, stdout, and stderr matches
- Generates artifact bundles for failures

### 3. Test Case Reduction (`Aster.Compiler.Reducers`)
- **DeltaReducer**: Line-based reduction using delta debugging
- Minimizes failing test cases to smallest reproducible input
- Integrates with fuzzing workflow

## Usage

### Run Parser Fuzzing (Smoke Mode)
```bash
dotnet run --project src/Aster.CLI -- fuzz parser --smoke
```

### Run Type System Fuzzing (Nightly Mode)
```bash
dotnet run --project src/Aster.CLI -- fuzz typesystem --seed 12345
```

### Run Differential Testing
```bash
dotnet run --project src/Aster.CLI -- differential tests/conformance/compile-pass
```

### Reduce a Failing Test Case
```bash
dotnet run --project src/Aster.CLI -- reduce crash_12345.ast
```

## Directory Structure

```
/tests
  /conformance
    /compile-pass    - Programs that should compile
    /compile-fail    - Programs that should fail compilation
    /run-pass        - Programs that should run successfully
  /differential
    /opt-on-off      - Differential test results
    /artifacts       - Failure artifacts
  /fuzz
    /corpus
      /seed          - Hand-written seed corpus
      /generated     - Generated corpus
    /crashes         - Compiler crashes
    /wrongcode       - Miscompiles
    /hangs           - Timeouts
    /nondet          - Non-deterministic failures
```

## CI Integration

### PR Checks (Smoke Mode)
- Parser fuzz: 100 iterations
- Type system fuzz: 100 iterations
- Differential testing on conformance suite

See: `.github/workflows/fuzz-smoke.yml`

### Nightly (Extended Mode)
- Multiple harnesses with different seeds
- 1M iterations per run
- Automatic issue creation on failure

See: `.github/workflows/fuzz-nightly.yml`

## Artifact Bundles

Every failure generates an artifact bundle containing:
- `source.ast` - Minimal reproducing source
- `metadata.json` - Seed, timestamp, compiler version, repro command
- MIR dumps (O0 and O3)
- LLVM IR dumps (O0 and O3)
- Diagnostics JSON
- Timing and memory stats

## Reproducibility

All failures are deterministic via RNG seed:
```bash
# Original run finds bug
dotnet run --project src/Aster.CLI -- fuzz parser --seed 42

# Reproduce exact same bug
dotnet run --project src/Aster.CLI -- fuzz parser --seed 42
```

## Documentation

- [Miscompile Policy](docs/miscompile-policy.md) - Definitions and response protocol
- [Fuzzing Guide](docs/fuzzing-guide.md) - Detailed usage guide
- [Triage Runbook](docs/triage-runbook.md) - How to handle failures

## Testing Strategy

### Differential Testing
- **What**: Compare O0 vs O3 behavior
- **Detects**: Optimizer bugs that change semantics
- **Coverage**: All conformance tests

### Property-Based Fuzzing
- **What**: Generate random valid/invalid programs
- **Detects**: Crashes, panics, assertion failures
- **Coverage**: Parser, type system, MIR builder

### Crash Classification
- **Crashes**: Unhandled exceptions (P1)
- **Wrong-code**: Behavior mismatch (P0 - CRITICAL)
- **Hangs**: Timeout exceeded (P1)
- **Non-determinism**: Inconsistent output (P2)

## Future Enhancements

Planned but not yet implemented:
- MirBuilderFuzz harness
- MirVerifierFuzz harness
- OptimizerFuzz harness
- CodegenFuzz harness
- Stage compiler comparison (for self-hosting)
- Backend variant testing (LLVM versions, LTO, PGO)
- AST-aware reduction
- MIR-aware reduction
- Property-based testing for borrow checker

## Contributing

When adding new fuzzing harnesses:
1. Extend `FuzzRunner` base class
2. Implement `GenerateInput()` and `ExecuteTest()` methods
3. Add to CLI command switch in `Program.cs`
4. Add tests to verify the harness works
5. Update this README

When adding differential test modes:
1. Extend `DifferentialTestRunner`
2. Add configuration options to `DiffConfig`
3. Update CI workflows
4. Document in fuzzing guide

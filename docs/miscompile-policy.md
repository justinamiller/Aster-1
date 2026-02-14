# Miscompile Detection Policy

## Overview

This document defines the Aster compiler's policy for preventing, detecting, and responding to miscompilation bugs (wrong-code generation).

## Definition of Miscompile

A **miscompile** is a compiler bug where:
1. The source program is valid according to the language specification
2. The compiler accepts the program without errors
3. The generated code produces incorrect runtime behavior

This is distinct from:
- **Crashes**: Compiler panics or internal errors
- **Incorrect diagnostics**: False positives/negatives in type checking
- **Performance regressions**: Correct but slower code

## Non-Negotiables

### 1. No Silent Wrong-Code
- Every optimization must be tested differentially (opt-on vs opt-off)
- All test cases in the conformance suite must pass at all optimization levels
- Any divergence in behavior is treated as a critical bug

### 2. Reproducibility
- Every failure must have a deterministic reproduction
- Failures must include:
  - Source file
  - Compiler version and git commit
  - Exact command to reproduce
  - RNG seed (for fuzz-generated cases)

### 3. Minimization
- All failures should be minimized to the smallest reproducing test case
- Minimized cases are added to the regression test suite

## Testing Methodology

### Differential Testing
Compare program behavior at different optimization levels:
- O0 (unoptimized) is the reference
- O1, O2, O3 must produce identical runtime behavior
- Mismatches indicate optimizer bugs

### Property-Based Testing
- Type system soundness: well-typed programs don't get stuck
- Borrow checker soundness: memory safety preserved
- Effect system soundness: effects tracked correctly

### Fuzzing Targets
1. **Parser**: Should never crash on any input
2. **Type System**: Should never accept invalid programs or crash
3. **MIR Builder**: Should never produce invalid MIR
4. **Optimizer**: Should preserve semantics
5. **Codegen**: Should emit valid LLVM IR

## CI Gates

### Required for All PRs
- All conformance tests pass
- Differential testing on conformance suite (smoke mode)
- Parser fuzz (100 iterations)

### Nightly
- Extended fuzzing (1M iterations per harness)
- Full differential testing
- Stage compiler comparison (if self-hosting)

## Response Protocol

### When a Miscompile is Found

1. **Triage** (within 1 hour)
   - Classify: crash vs wrong-code vs hang
   - Assign severity (P0 for wrong-code, P1 for crash)
   
2. **Minimize** (within 24 hours)
   - Reduce to minimal test case
   - Identify root cause (which pass/component)
   
3. **Fix** (P0: within 48 hours, P1: within 1 week)
   - Implement fix
   - Add regression test
   - Verify fix doesn't introduce new bugs

4. **Post-Mortem**
   - Document root cause
   - Update fuzzing to catch similar bugs
   - Update documentation if needed

## Artifact Requirements

Every failure must generate an artifact bundle containing:
- `source.ast` - Minimal reproducing source
- `metadata.json` - Seed, timestamp, compiler version
- `repro.sh` - Exact reproduction command
- `mir_o0.txt` - MIR dump at O0
- `mir_o3.txt` - MIR dump at O3
- `llvm_o0.ll` - LLVM IR at O0
- `llvm_o3.ll` - LLVM IR at O3
- `diagnostics.json` - Compiler diagnostics
- `stats.json` - Timing and memory stats

## Exemptions

The following are **not** considered miscompiles:
1. Platform-specific behavior (within spec)
2. Undefined behavior invocation
3. Reliance on unspecified evaluation order
4. Integer overflow (if wrapping semantics not guaranteed)

## Version

Version: 1.0  
Last Updated: 2026-02-14  
Approved By: Compiler Team

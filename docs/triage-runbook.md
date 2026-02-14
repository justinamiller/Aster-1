# Triage Runbook

## Overview

This runbook guides you through triaging and responding to compiler failures found by fuzzing or differential testing.

## Failure Classification

### 1. Compiler Crash
**Symptoms:**
- Unhandled exception in compiler
- Stack trace visible
- Non-zero exit code

**Severity:** P1  
**SLA:** Fix within 1 week

**Actions:**
1. Check stack trace for root cause
2. Minimize test case
3. Add regression test
4. Fix the crash

### 2. Wrong-Code (Miscompile)
**Symptoms:**
- Compiler succeeds
- Program behavior differs between O0 and O3
- Exit code or output mismatch

**Severity:** P0 (CRITICAL)  
**SLA:** Fix within 48 hours

**Actions:**
1. Verify it's not UB or platform-specific
2. Identify which optimization pass broke it
3. Minimize test case
4. Add to regression suite
5. Fix immediately

### 3. Hang/Timeout
**Symptoms:**
- Compilation doesn't complete
- Exceeds timeout threshold

**Severity:** P1  
**SLA:** Fix within 1 week

**Actions:**
1. Reproduce with profiler
2. Identify infinite loop or exponential algorithm
3. Add timeout checks or better complexity
4. Add regression test

### 4. Non-Determinism
**Symptoms:**
- Different output on repeated runs
- Platform-dependent behavior

**Severity:** P2  
**SLA:** Fix within 2 weeks

**Actions:**
1. Check for uninitialized variables
2. Check for HashMap iteration order
3. Check for parallel compilation races
4. Add determinism tests

## Triage Workflow

### Step 1: Verify (5 minutes)

**Reproduce the failure:**
```bash
# From artifact bundle
cd tests/fuzz/crashes/Crash_12345_bundle/
bash repro.sh

# Or manually
aster fuzz parser --seed 12345
```

**Check metadata:**
```bash
cat metadata.json
```

**Verify it's not a known issue:**
```bash
# Search GitHub issues
gh issue list --label bug --search "crash parser"
```

### Step 2: Classify (10 minutes)

**Determine failure mode:**
- Crash → Check stack trace
- Wrong-code → Compare O0 vs O3 output
- Hang → Check timeout logs
- Non-det → Run multiple times

**Assign severity:**
- P0: Wrong-code
- P1: Crash or hang
- P2: Non-determinism or minor issue

### Step 3: Minimize (30 minutes to 2 hours)

**Run reducer:**
```bash
aster reduce tests/fuzz/crashes/Crash_12345.ast
```

**Manual reduction if needed:**
1. Remove unrelated code
2. Rename variables to simple names
3. Inline constants
4. Remove unused imports

**Verify minimal case still fails:**
```bash
aster build crash_12345.reduced.ast
```

### Step 4: Diagnose (varies)

**For crashes:**
1. Read stack trace
2. Add debug prints around crash site
3. Use debugger if needed

**For wrong-code:**
1. Compare MIR dumps:
   ```bash
   aster build --dump-mir-o0 test.ast > mir_o0.txt
   aster build --dump-mir-o3 test.ast > mir_o3.txt
   diff mir_o0.txt mir_o3.txt
   ```

2. Compare LLVM IR:
   ```bash
   aster build --dump-llvm-o0 test.ast > llvm_o0.ll
   aster build --dump-llvm-o3 test.ast > llvm_o3.ll
   diff llvm_o0.ll llvm_o3.ll
   ```

3. Bisect optimization passes:
   - Disable passes one by one
   - Find which pass introduced the bug

**For hangs:**
1. Run with profiler
2. Check for infinite loops
3. Look for exponential algorithms

### Step 5: Fix (varies)

**Create fix:**
1. Write failing test first
2. Implement fix
3. Verify test passes
4. Run full test suite

**Add regression test:**
```bash
# Add to appropriate suite
cp crash_12345.reduced.ast tests/conformance/compile-pass/
# Or
cp crash_12345.reduced.ast tests/conformance/compile-fail/
```

### Step 6: Verify (15 minutes)

**Run all checks:**
```bash
# Build
dotnet build

# Unit tests
dotnet test

# Fuzz smoke
aster fuzz parser --smoke
aster fuzz typesystem --smoke

# Differential
aster differential tests/conformance
```

### Step 7: Document (10 minutes)

**Create GitHub issue (if not already filed):**
```markdown
# Title: [Component] Brief description

## Summary
Brief description of the bug.

## Reproduction
\`\`\`bash
aster build test.ast
\`\`\`

## Expected Behavior
What should happen.

## Actual Behavior
What actually happens.

## Minimized Test Case
\`\`\`rust
fn main() {
    // Minimal code
}
\`\`\`

## Metadata
- Seed: 12345
- Compiler Version: 0.2.0
- Git Commit: abc123
- Platform: Linux x64

## Artifact Bundle
Path: tests/fuzz/crashes/Crash_12345_bundle/
```

## Common Patterns

### Pattern 1: Parser Panic on Invalid UTF-8
**Symptom:** Parser crashes on malformed Unicode  
**Fix:** Add proper error handling in lexer  
**Prevention:** Fuzz lexer with random bytes

### Pattern 2: Type Checker Infinite Loop
**Symptom:** Timeout during type checking  
**Fix:** Add recursion depth limit  
**Prevention:** Fuzz with deeply nested types

### Pattern 3: Optimizer Breaks Semantics
**Symptom:** Wrong-code after optimization  
**Fix:** Fix incorrect transformation  
**Prevention:** More differential tests

### Pattern 4: MIR Verifier Miss
**Symptom:** Invalid MIR not caught  
**Fix:** Strengthen verifier  
**Prevention:** Fuzz MIR builder

## Escalation

### When to Escalate

Escalate to compiler team lead if:
- Bug is P0 and you can't fix in 4 hours
- Bug is systemic and affects multiple components
- Bug requires architecture changes
- You're stuck after 2 hours of debugging

### Escalation Template

```
@compiler-team-lead I need help triaging this bug:

**Issue:** Brief description
**Severity:** P0/P1/P2
**Time Spent:** X hours
**Blocker:** What's blocking you
**Artifact:** Path to bundle
**Next Steps:** What you think should happen
```

## Post-Mortem

After fixing a P0 bug, write a post-mortem:

1. **What happened?** Timeline of events
2. **Root cause?** Why did the bug exist?
3. **Why wasn't it caught?** Gap in testing
4. **How to prevent?** Improvements needed
5. **Action items** Concrete next steps

## Checklists

### Quick Triage Checklist
- [ ] Reproduce the failure
- [ ] Check if it's a duplicate
- [ ] Classify the failure type
- [ ] Assign severity
- [ ] Minimize the test case
- [ ] File GitHub issue
- [ ] Add to tracking board

### Fix Checklist
- [ ] Write failing test
- [ ] Implement fix
- [ ] Verify test passes
- [ ] Run full test suite
- [ ] Add regression test
- [ ] Update documentation
- [ ] Code review
- [ ] Merge PR

### Post-Merge Checklist
- [ ] Verify in CI
- [ ] Update fuzzing corpus
- [ ] Close GitHub issue
- [ ] Write post-mortem (if P0)
- [ ] Update runbook if needed

## Tools

### Debugging Tools
- `lldb` / `gdb` - Native debuggers
- `dotnet-dump` - .NET crash dumps
- `dotnet-trace` - Performance traces
- `dotnet-counters` - Live metrics

### Analysis Tools
- `aster build --dump-mir` - MIR dump
- `aster build --dump-llvm` - LLVM IR dump
- `aster build --dump-diagnostics-json` - Structured diagnostics
- `diff` / `git diff` - Compare outputs

### Automation
- `aster reduce` - Automated minimization
- `aster fuzz replay` - Reproduce with seed
- `aster differential` - Automated opt testing

## References

- [Miscompile Policy](miscompile-policy.md)
- [Fuzzing Guide](fuzzing-guide.md)
- [Architecture Docs](MidEndArchitecture.md)
- [Contributing Guide](../CONTRIBUTING.md)

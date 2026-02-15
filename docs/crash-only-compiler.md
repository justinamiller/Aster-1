# Crash-Only Compiler Principle

## Overview

The Aster compiler follows a **crash-only design** for internal errors: when the compiler encounters an unexpected state or bug, it immediately terminates with a structured crash report rather than attempting to recover or produce potentially incorrect output.

## Rationale

### Why Crash Instead of Recover?

1. **Correctness over Convenience**: A compiler bug could produce incorrect machine code, leading to silent data corruption or security vulnerabilities in user programs
2. **Fast Failure Detection**: Immediate crash makes bugs visible during development rather than silent miscompilation
3. **Reproducibility**: Crash reports capture state for debugging
4. **Trust**: Users should trust that if compilation succeeds, the output is correct

### Principle

> **"It's better to fail loudly than to succeed silently with wrong output."**

## Error Categories

### 1. User Errors (Handled Gracefully)

Errors in user code are **reported** with diagnostics, not crashes:

- Syntax errors
- Type errors
- Name resolution errors
- Borrow checker errors

**Behavior**: Collect all errors, display with context, return non-zero exit code

```
error[E0001]: expected ';' after expression
 --> example.ast:5:12
  |
5 |     let x = 42
  |            ^^ expected ';'
```

### 2. Internal Compiler Errors (ICE)

Bugs in the compiler itself trigger **immediate crash**:

- Assertion failures
- Unexpected enum variants
- Invalid AST states
- Null pointer dereferences (in unsafe code)
- Stack overflows

**Behavior**: Generate crash report, save to file, print instructions, exit immediately

```
thread 'main' panicked at 'internal compiler error: unexpected token kind in parser'

ICE Report saved to: /tmp/aster_ice_20260215_143022.txt

This is a bug in the Aster compiler.
Please file an issue at: https://github.com/justinamiller/Aster-1/issues/new

Include the following information:
- Aster version: 0.2.0
- Command: aster build example.ast
- ICE report file: /tmp/aster_ice_20260215_143022.txt

note: run with RUST_BACKTRACE=1 for a backtrace
```

## ICE Report Format

Crash reports contain:

```text
Aster Compiler Crash Report
===========================

Version: 0.2.0
Date: 2026-02-15 14:30:22 UTC
OS: Linux 5.15.0
Architecture: x86_64

Command Line:
    aster build src/main.ast

Compiler Phase: typecheck

Error Message:
    panicked at 'internal compiler error: unexpected None value in type inference'
    at src/Aster.Compiler/TypeChecker.cs:line 523

Backtrace:
    [see full backtrace below]

Recent Diagnostics:
    warning: unused variable 'x'
    note: variable declared here

Source Context:
    [relevant source code if available]

Environment:
    ASTER_HOME=/usr/local/aster
    PATH=...

Full Backtrace:
    [complete stack trace]
```

## Implementation

### In Code

```csharp
// Assert invariants
public void TypeCheck(Expr expr)
{
    var type = InferType(expr);
    
    // ICE if we get null (compiler bug)
    Debug.Assert(type != null, "InferType returned null - compiler bug");
    
    // Or use explicit ICE
    if (type == null)
    {
        throw new InternalCompilerErrorException(
            "type inference returned null",
            phase: "typecheck",
            context: new { expr = expr.ToString() }
        );
    }
}

// Distinguish user errors from ICEs
public Result<Type, Diagnostic> CheckType(Expr expr)
{
    // User error - return diagnostic
    if (expr is InvalidExpr)
    {
        return Diagnostic.Error("invalid expression");
    }
    
    // Compiler bug - crash
    if (ShouldNeverHappen())
    {
        ICE("reached unreachable code in type checker");
    }
    
    return type;
}
```

### Global Handler

```csharp
public class CrashReporter
{
    public static void InstallGlobalHandler(
        string version,
        Func<string> getCommand,
        Func<string> getPhase,
        Func<List<string>> getDiagnostics)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            var report = GenerateReport(version, getCommand(), getPhase(), getDiagnostics(), exception);
            SaveReport(report);
            PrintReport(report);
            Environment.Exit(101);  // ICE exit code
        };
    }
}
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | User error (compilation failed) |
| 2 | Invalid arguments |
| 100 | Generic ICE |
| 101 | Unhandled exception |
| 102 | Assertion failure |
| 103 | Stack overflow |

## Recovery Strategies

### What NOT to Do

❌ **Don't** catch all exceptions and continue  
❌ **Don't** substitute default/fallback values  
❌ **Don't** skip compilation phases  
❌ **Don't** emit partial/incomplete output  

### What to Do

✅ **Do** crash immediately on ICE  
✅ **Do** save crash report before exiting  
✅ **Do** provide reproduction instructions  
✅ **Do** include compiler version and environment  
✅ **Do** suggest filing a bug report  

## Testing ICE Paths

### Fuzzing

Run compiler on randomly generated inputs:

```bash
aster fuzz --crashes-only --timeout 60
```

Expected: Compiler should either:
- Successfully compile (output valid IR)
- Report user error (with diagnostic)
- Never ICE (any ICE is a bug)

### Assertion Coverage

Track which assertions are tested:

```bash
aster test --coverage --include-assertions
```

Goal: 100% of assertions exercised in tests.

### Stress Testing

```bash
# Deep nesting
aster build deeply_nested.ast

# Large files
aster build 10MB_source.ast

# Edge cases
aster build edge_cases/*.ast
```

## ICE Minimization

When an ICE is reported:

1. **Reduce input**: Find minimal reproducer
   ```bash
   aster reduce ice_input.ast --target ice
   ```

2. **Binary search**: Which commit introduced bug?
   ```bash
   git bisect start
   git bisect bad HEAD
   git bisect good v0.1.0
   # Test each commit...
   ```

3. **Identify phase**: Where does ICE occur?
   - Lexer
   - Parser
   - Type checker
   - IR generation
   - LLVM emission

4. **Fix**: Update code, add regression test

## Stage1 Specifics

For Stage1 compiler (aster1), ICE handling is simplified:

- **No fancy crash reports** (limited stdlib)
- **Basic error messages** to stderr
- **Exit code 100** for ICE
- **Minimal context** (just error message)

Example:
```rust
fn ice(message: String) {
    // Stage1 ICE handler
    println("INTERNAL COMPILER ERROR");
    println(message);
    println("This is a bug in aster1");
    // Exit with ICE code
    // (would need FFI or special compiler support)
}
```

## Best Practices

1. **Assert Liberally**: Better to crash during development than ship bugs
2. **Document Invariants**: Explain why each assertion should hold
3. **Test Edge Cases**: Explicitly test boundary conditions
4. **Fuzz Regularly**: Automated random testing finds ICEs
5. **Track ICEs**: Monitor ICE rate in CI/CD
6. **Fix ICEs First**: Higher priority than features

## Metrics

Track ICE occurrences:

- **ICE Rate**: ICEs per 1000 compilations (goal: < 0.1)
- **Time to Fix**: Days from report to fix (goal: < 7)
- **Regression Rate**: Re-occurrence of fixed ICEs (goal: 0%)

## Examples

### Good: Explicit Assertion

```csharp
public Type TypeCheckBinary(BinaryExpr expr)
{
    var leftType = TypeCheck(expr.Left);
    var rightType = TypeCheck(expr.Right);
    
    // Assert: binary ops should have inferred types for both operands
    Debug.Assert(leftType != null && rightType != null,
        "ICE: binary operands should always have inferred types");
    
    if (leftType == rightType)
        return leftType;
    
    return ReportError($"type mismatch: {leftType} vs {rightType}");
}
```

### Bad: Silent Failure

```csharp
public Type TypeCheckBinary(BinaryExpr expr)
{
    var leftType = TypeCheck(expr.Left);
    var rightType = TypeCheck(expr.Right);
    
    // BAD: If null, use i32 as fallback
    leftType ??= PrimitiveType.I32;   // ❌ Masks compiler bug
    rightType ??= PrimitiveType.I32;  // ❌ Wrong!
    
    if (leftType == rightType)
        return leftType;
    
    return ReportError($"type mismatch: {leftType} vs {rightType}");
}
```

### Good: Early ICE

```csharp
public void EmitInstruction(Instruction inst)
{
    switch (inst)
    {
        case AddInst add: EmitAdd(add); break;
        case SubInst sub: EmitSub(sub); break;
        case MulInst mul: EmitMul(mul); break;
        // ... all known variants
        
        default:
            ICE($"unknown instruction variant: {inst.GetType().Name}");
            // Crash before emitting wrong IR
    }
}
```

## See Also

- [Miscompile Policy](miscompile-policy.md) - How we handle correctness bugs
- [Triage Runbook](triage-runbook.md) - Bug triage process
- [Fuzzing Guide](fuzzing-guide.md) - Automated ICE detection

---

**Philosophy**: "Fail fast, fail loud, fail with context"

**Last Updated**: 2026-02-15  
**Status**: Active policy for all compiler stages

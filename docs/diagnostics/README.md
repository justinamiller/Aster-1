# Aster Compiler Diagnostics

This directory contains detailed explanations for all compiler diagnostic error codes.

## Error Code Categories

### Syntax Errors (E1xxx)
- E1000: Unexpected token
- E1001: Expected token
- E1002: Unclosed delimiter
- E1003: Invalid literal
- E1004: Invalid escape sequence
- E1005: Invalid character
- E1006: Unterminated string
- E1007: Unterminated comment
- E1008: Invalid number format

### Name Resolution (E2xxx)
- E2000: Undefined name
- E2001: Duplicate definition
- E2002: Ambiguous name
- E2003: Undefined module
- E2004: Cyclic module dependency
- E2005: Private item access
- E2006: Item not found in module

### Type System (E3xxx)
- E3000: Type mismatch
- E3001: Cannot infer type
- E3010: Cannot unify types
- E3011: Occurs check failed (infinite type)
- E3100: Function return type mismatch
- E3101: Variable assignment type mismatch
- E3102: Function argument count mismatch
- E3103: Function argument type mismatch
- E3104: If condition must be bool
- E3105: Type has no such field
- [E3124: Cannot unify types](E3124.md) ✓

### Trait System (E4xxx)
- E4000: Type does not implement trait
- E4001: Trait method not implemented
- E4002: Conflicting trait implementations
- E4003: Orphan trait implementation
- E4020: Cycle detected in trait resolution
- E4021: Type does not implement required trait

### Effect System (E5xxx)
- E5000: Function has undeclared effects
- E5001: Effect not allowed in this context
- E5002: Async function called in sync context
- E5003: Unsafe operation in safe context

### Ownership (E6xxx)
- [E6000: Use of moved value](E6000.md) ✓
- E6001: Cannot move while borrowed
- E6002: Cannot borrow moved value
- E6003: Cannot immutably borrow while mutably borrowed
- E6005: Cannot mutably borrow while already borrowed

### Borrow Checking (E7xxx)
- E7000: Use of moved value
- E7001: Cannot move while borrowed
- E7002: Cannot borrow moved value
- E7003: Cannot mutably borrow while already borrowed
- E7004: Cannot immutably borrow while mutably borrowed
- E7005: Borrow outlives its referent
- E7006: Dangling reference

### Pattern Matching (E8xxx)
- E8000: Match expression has no arms
- E8001: Non-exhaustive match
- E8002: Unreachable pattern
- E8003: Invalid pattern for type

### MIR/Backend (E9xxx)
- E9000: Invalid MIR structure
- E9001: Codegen error
- E9002: Optimization error

### Warnings (Wxxx)
- W0001: Unreachable pattern
- W0002: Unused variable
- W0003: Unused function
- W0004: Dead code
- W0005: Deprecated item

### Info Messages (Ixxx)
- I0001: Inferred type
- I0002: Optimization applied
- I0003: Information message

## Using the Explain Command

You can get detailed explanations for any error code using the `explain` command:

```bash
aster explain E3124
aster explain E6000
```

## Contributing

When adding new error codes:
1. Add the code to `DiagnosticCode.cs`
2. Register it in `DiagnosticRegistry.cs`
3. Create a markdown file in this directory
4. Add an explanation in `DiagnosticExplainer.cs`
5. Update this README

## Quality Standards

Every diagnostic should:
- Have a stable, unique code
- Include a clear title
- Provide actionable help text
- Show relevant code examples
- Suggest fixes when possible
- Reference related error codes

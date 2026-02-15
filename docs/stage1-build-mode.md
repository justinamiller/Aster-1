# Stage1 Build Mode - CLI Flag Implementation

## Overview

The `--stage1` flag enforces compilation using only the **Core-0 language subset** defined in the Stage1 specification. This ensures code is compatible with the minimal self-hosted compiler (aster1).

## Usage

```bash
# Build with Stage1 restrictions
aster build --stage1 my_program.ast

# Check (type-check only) with Stage1 restrictions
aster check --stage1 my_program.ast

# Emit LLVM IR with Stage1 restrictions
aster emit-llvm --stage1 my_program.ast
```

## Behavior

When `--stage1` is active, the compiler:

1. **Rejects unsupported syntax** with clear error messages
2. **Only allows Core-0 features** (see [STAGE1_SCOPE.md](bootstrap/STAGE1_SCOPE.md))
3. **Provides migration suggestions** for blocked features

## Rejected Features

### Generics (User-Defined)

```rust
// ❌ Error with --stage1
fn identity<T>(x: T) -> T {
    x
}
```

**Error**:
```
error[E9001]: generics not allowed in Stage1 mode
 --> example.ast:1:13
  |
1 | fn identity<T>(x: T) -> T {
  |             ^^^ generic parameter not allowed
  |
  = note: Stage1 (Core-0) does not support user-defined generics
  = help: use concrete types instead, or remove --stage1 flag
```

### Traits

```rust
// ❌ Error with --stage1
trait Printable {
    fn print(&self);
}
```

**Error**:
```
error[E9002]: traits not allowed in Stage1 mode
 --> example.ast:1:1
  |
1 | trait Printable {
  | ^^^^^ trait definition not allowed
  |
  = note: Stage1 (Core-0) does not support traits
  = help: use standalone functions instead
```

### References

```rust
// ❌ Error with --stage1
fn read_data(data: &Data) {
    // ...
}
```

**Error**:
```
error[E9003]: references not allowed in Stage1 mode
 --> example.ast:1:20
  |
1 | fn read_data(data: &Data) {
  |                    ^^^^^ reference type not allowed
  |
  = note: Stage1 (Core-0) does not support references
  = help: use value semantics (pass by value) instead
```

### Type Casts

```rust
// ❌ Error with --stage1
let x: i64 = y as i64;
```

**Error**:
```
error[E9004]: type casts not allowed in Stage1 mode
 --> example.ast:1:18
  |
1 | let x: i64 = y as i64;
  |                  ^^^^ type cast not allowed
  |
  = note: Stage1 (Core-0) does not support 'as' casts
  = help: use explicit conversion functions instead
```

### Closures

```rust
// ❌ Error with --stage1
let add_one = |x| x + 1;
```

**Error**:
```
error[E9005]: closures not allowed in Stage1 mode
 --> example.ast:1:15
  |
1 | let add_one = |x| x + 1;
  |               ^^^^^^^^^ closure not allowed
  |
  = note: Stage1 (Core-0) does not support closures
  = help: use named functions instead
```

## Allowed Features

With `--stage1`, you CAN use:

- ✅ Primitive types (i32, bool, String, etc.)
- ✅ Structs with named fields
- ✅ Enums (simple variants)
- ✅ Functions (no generics)
- ✅ If/else, while, loop, match
- ✅ Vec, String, Box, Option, Result (stdlib types)
- ✅ Let bindings, assignments
- ✅ Arithmetic and logical operators
- ✅ Function calls
- ✅ Struct initialization
- ✅ Pattern matching

## Implementation

### Parser Changes

```csharp
public class Parser
{
    private readonly bool _stage1Mode;
    
    public Parser(TokenStream tokens, bool stage1Mode = false)
    {
        _tokens = tokens;
        _stage1Mode = stage1Mode;
    }
    
    private GenericParams? ParseGenericParams()
    {
        if (_stage1Mode && Peek().Kind == TokenKind::LessThan)
        {
            Error("generics not allowed in Stage1 mode", Peek().Span);
            return null;
        }
        
        // Normal generic parsing...
    }
}
```

### Type Checker Changes

```csharp
public class TypeChecker
{
    private readonly bool _stage1Mode;
    
    public Type CheckType(AstType type)
    {
        if (_stage1Mode && type is ReferenceType)
        {
            Error("references not allowed in Stage1 mode", type.Span);
            return ErrorType;
        }
        
        // Normal type checking...
    }
}
```

### CLI Integration

```csharp
private static int Build(string[] args)
{
    bool stage1Mode = false;
    string? inputFile = null;
    
    foreach (var arg in args)
    {
        if (arg == "--stage1")
        {
            stage1Mode = true;
        }
        else if (!arg.StartsWith("--"))
        {
            inputFile = arg;
        }
    }
    
    var driver = new CompilationDriver(stage1Mode: stage1Mode);
    // ... rest of build logic
}
```

## Testing

### Stage1 Conformance Tests

```bash
# Run all Stage1 tests
dotnet test --filter "Category=Stage1"

# Test that forbidden features are rejected
dotnet test Aster.Compiler.Tests::Stage1RejectionTests
```

### Test Structure

```csharp
[TestClass]
public class Stage1RejectionTests
{
    [TestMethod]
    public void RejectsGenerics()
    {
        var source = "fn identity<T>(x: T) -> T { x }";
        var driver = new CompilationDriver(stage1Mode: true);
        
        var result = driver.Compile(source, "test.ast");
        
        Assert.IsNull(result);  // Compilation should fail
        Assert.IsTrue(driver.HasError("E9001"));  // Generics error
    }
    
    [TestMethod]
    public void AcceptsSimpleFunction()
    {
        var source = "fn add(a: i32, b: i32) -> i32 { a + b }";
        var driver = new CompilationDriver(stage1Mode: true);
        
        var result = driver.Compile(source, "test.ast");
        
        Assert.IsNotNull(result);  // Should compile successfully
    }
}
```

## Migration Guide

### From Full Aster to Stage1

**1. Replace generics with concrete types**:

```rust
// Before (not Stage1)
fn max<T>(a: T, b: T) -> T {
    if a > b { a } else { b }
}

// After (Stage1 compatible)
fn max_i32(a: i32, b: i32) -> i32 {
    if a > b { a } else { b }
}

fn max_f64(a: f64, b: f64) -> f64 {
    if a > b { a } else { b }
}
```

**2. Replace references with value semantics**:

```rust
// Before (not Stage1)
fn process(data: &Data) -> Result {
    // Read data
}

// After (Stage1 compatible)
fn process(data: Data) -> (Data, Result) {
    // Read data
    // Return both data and result
    (data, result)
}
```

**3. Replace type casts with conversion functions**:

```rust
// Before (not Stage1)
let x: i64 = y as i64;

// After (Stage1 compatible)
fn i32_to_i64(x: i32) -> i64 {
    // Manual conversion (simplified)
    // In real implementation, would handle properly
    x
}

let x: i64 = i32_to_i64(y);
```

**4. Replace closures with functions**:

```rust
// Before (not Stage1)
let nums = vec![1, 2, 3];
let doubled = nums.map(|x| x * 2);

// After (Stage1 compatible)
fn double(x: i32) -> i32 {
    x * 2
}

let mut doubled = Vec::new();
// Manual iteration (for loop might not be fully supported)
let mut i = 0;
while i < nums.len() {
    doubled.push(double(nums[i]));
    i = i + 1;
}
```

## FAQ

### Q: Why have a Stage1 mode?

**A**: To ensure code is compatible with the minimal self-hosted compiler (aster1). This enables bootstrapping - aster1 can compile code that only uses Core-0 features.

### Q: Will Stage1 mode always exist?

**A**: Yes, for bootstrapping purposes. Even when full self-hosting is achieved, Stage1 mode ensures we can always rebuild from the minimal compiler.

### Q: Can I use stdlib types like Vec in Stage1 mode?

**A**: Yes! Vec, String, Box, Option, and Result are part of Core-0. They're implemented using Stage1-compatible features.

### Q: How do I know if my code is Stage1 compatible?

**A**: Compile with `--stage1` flag. If it compiles, it's compatible. If not, error messages will guide you.

### Q: Is Stage1 mode slower?

**A**: No runtime performance difference. It's just a compile-time restriction on language features used.

## See Also

- [STAGE1_SCOPE.md](bootstrap/STAGE1_SCOPE.md) - Complete Stage1 feature specification
- [OVERVIEW.md](bootstrap/OVERVIEW.md) - Bootstrap process overview
- [STAGE1_STATUS.md](bootstrap/STAGE1_STATUS.md) - Implementation status

---

**Status**: Specification complete, implementation pending  
**Last Updated**: 2026-02-15

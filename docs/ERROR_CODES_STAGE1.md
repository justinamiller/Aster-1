# Stage1 Error Codes Reference

## Overview

Stage1 (Core-0) mode enforces a minimal language subset suitable for bootstrapping. When the `--stage1` flag is used, the compiler rejects features that are not part of the Core-0 specification.

All Stage1 error codes are in the **E9000-E9999** range.

---

## Error Codes

### E9001: Traits Not Allowed

**Category**: Stage1 Restriction  
**Severity**: Error

**Description**:
Traits are not allowed in Stage1 (Core-0) mode. Stage1 uses a simpler type system without trait-based polymorphism.

**Error Message**:
```
error[E9001]: Traits are not allowed in Stage1 (Core-0) mode. Use standalone functions instead.
```

**Example (Invalid)**:
```rust
trait Printable {
    fn print(&self);
}
```

**Solution**:
Use standalone functions instead of trait methods:

```rust
// Instead of trait methods, use regular functions
fn print_point(p: Point) {
    println("Point");
}
```

**See Also**: [STAGE1_SCOPE.md](../bootstrap/STAGE1_SCOPE.md)

---

### E9002: Impl Blocks Not Allowed

**Category**: Stage1 Restriction  
**Severity**: Error

**Description**:
Impl blocks (including trait implementations and inherent implementations) are not allowed in Stage1 mode.

**Error Message**:
```
error[E9002]: Impl blocks are not allowed in Stage1 (Core-0) mode. Use standalone functions instead.
```

**Example (Invalid)**:
```rust
struct Point { x: i32, y: i32 }

impl Point {
    fn new() -> Point {
        Point { x: 0, y: 0 }
    }
}
```

**Solution**:
Use standalone functions that take the struct as a parameter:

```rust
struct Point { x: i32, y: i32 }

fn new_point() -> Point {
    Point { x: 0, y: 0 }
}

fn point_distance(p: Point) -> f64 {
    // Calculate distance
}
```

**See Also**: E9001 (traits)

---

### E9003: Async Functions Not Allowed

**Category**: Stage1 Restriction  
**Severity**: Error

**Description**:
Async functions and the `async` keyword are not supported in Stage1 mode. Stage1 uses a synchronous execution model only.

**Error Message**:
```
error[E9003]: Async functions are not allowed in Stage1 (Core-0) mode.
```

**Example (Invalid)**:
```rust
async fn fetch_data() -> i32 {
    42
}
```

**Solution**:
Use synchronous functions:

```rust
fn fetch_data() -> i32 {
    42
}
```

**Note**: If you need asynchronous behavior, consider using callbacks or state machines manually, or wait for Stage2+ which will support async/await.

---

### E9004: References Not Allowed

**Category**: Stage1 Restriction  
**Severity**: Error

**Description**:
References (`&T` and `&mut T`) are not allowed in Stage1 mode. Stage1 uses value semantics exclusively to simplify the ownership model during bootstrapping.

**Error Message**:
```
error[E9004]: References (&T, &mut T) are not allowed in Stage1 (Core-0) mode. Use value semantics instead.
```

**Example (Invalid - Type Annotation)**:
```rust
fn read_value(x: &i32) -> i32 {
    *x
}
```

**Example (Invalid - Expression)**:
```rust
fn main() {
    let x = 10;
    let y = &x;
}
```

**Solution**:
Use value semantics (pass by value, return updated values):

```rust
// Pass by value
fn read_value(x: i32) -> i32 {
    x
}

// Return updated value instead of mutating via reference
fn increment(x: i32) -> i32 {
    x + 1
}

fn main() {
    let x = 10;
    let x = increment(x);  // Rebind with updated value
}
```

**Alternative** (for larger structs):
Return tuple with updated value:

```rust
fn modify_point(p: Point) -> Point {
    Point { x: p.x + 1, y: p.y + 1 }
}
```

**Performance Note**: While value semantics may seem less efficient, in practice:
1. The compiler can optimize many cases
2. Stage1 is for bootstrapping, not production
3. Smart pointers (Box) can be used for heap allocation when needed

**See Also**: [memory.md](../spec/memory.md)

---

### E9005: Closures Not Allowed

**Category**: Stage1 Restriction  
**Severity**: Error

**Description**:
Closures (anonymous functions with the `|params| expr` syntax) are not allowed in Stage1 mode.

**Error Message**:
```
error[E9005]: Closures (|x| expr) are not allowed in Stage1 (Core-0) mode. Use named functions instead.
```

**Example (Invalid)**:
```rust
fn main() {
    let add_one = |x| x + 1;
    let result = add_one(5);
    
    let multiply = |a, b| a * b;
}
```

**Solution**:
Use named functions:

```rust
fn add_one(x: i32) -> i32 {
    x + 1
}

fn multiply(a: i32, b: i32) -> i32 {
    a * b
}

fn main() {
    let result = add_one(5);
    let prod = multiply(3, 4);
}
```

**Note**: While this is more verbose, it:
1. Makes code more explicit and easier to debug
2. Simplifies the type system during bootstrapping
3. Avoids closure capture complexity

---

## Stage1 Allowed Features

The following features **are** allowed in Stage1 mode:

### ✅ Primitive Types
- `i32`, `i64`, `u32`, `u64`, `f32`, `f64`
- `bool`, `char`, `String`

### ✅ Compound Types
- **Structs** with named fields
- **Enums** with simple variants
- **Tuples** (basic support)

### ✅ Control Flow
- `if`/`else` expressions
- `while` loops
- `loop` with `break`/`continue`
- `match` expressions
- `for` loops (basic)

### ✅ Functions
- Regular named functions
- Function parameters
- Return values
- Multiple statements

### ✅ Standard Library Types
- `Vec<T>` - Growable arrays
- `String` - UTF-8 strings
- `Box<T>` - Heap allocation
- `Option<T>` - Optional values
- `Result<T, E>` - Error handling

### ✅ Operators
- Arithmetic: `+`, `-`, `*`, `/`, `%`
- Comparison: `==`, `!=`, `<`, `>`, `<=`, `>=`
- Logical: `&&`, `||`, `!`
- Bitwise: `&`, `|`, `^`, `~`

### ✅ Variable Bindings
- `let` bindings
- `let mut` for mutable variables
- Shadowing
- Type inference

---

## Usage

To enable Stage1 mode validation:

```bash
# Build with Stage1 restrictions
aster build --stage1 program.ast

# Type-check with Stage1 restrictions
aster check --stage1 program.ast
```

---

## Migration Guide

### From Full Aster to Stage1

**1. Replace traits with standalone functions**

Before:
```rust
trait Shape {
    fn area(&self) -> f64;
}

impl Shape for Circle {
    fn area(&self) -> f64 { ... }
}
```

After:
```rust
fn circle_area(c: Circle) -> f64 { ... }
```

**2. Replace references with value semantics**

Before:
```rust
fn process(data: &Data) -> &Result {
    // Process data
}
```

After:
```rust
fn process(data: Data) -> (Data, Result) {
    // Process data
    // Return both data and result
    (data, result)
}
```

**3. Replace closures with named functions**

Before:
```rust
let doubled = numbers.map(|x| x * 2);
```

After:
```rust
fn double(x: i32) -> i32 { x * 2 }

let mut doubled = Vec::new();
let mut i = 0;
while i < numbers.len() {
    doubled.push(double(numbers[i]));
    i = i + 1;
}
```

**4. Replace async with sync**

Before:
```rust
async fn fetch() -> Data { ... }
let data = fetch().await;
```

After:
```rust
fn fetch() -> Data { ... }
let data = fetch();
```

---

## FAQ

**Q: Why are these features forbidden in Stage1?**

A: Stage1 (Core-0) is designed for bootstrapping - the minimal subset needed to compile the compiler itself. Simpler features mean:
- Easier to implement in the seed compiler (aster0)
- Fewer edge cases to handle
- More deterministic compilation
- Faster initial implementation

**Q: When will these features be available?**

A: Features are planned for later stages:
- **Stage2**: Traits, generics, references
- **Stage3**: Full language with all features
- **Production**: Optimizations, advanced features

**Q: Can I use Stage1 for production code?**

A: Stage1 is primarily for bootstrapping the compiler. For production code, use the full language (without `--stage1` flag) which includes all features.

**Q: How do I check if my code is Stage1 compatible?**

A: Compile with the `--stage1` flag. If it compiles without E90XX errors, it's compatible:

```bash
aster build --stage1 mycode.ast
```

---

## Related Documentation

- [STAGE1_SCOPE.md](../bootstrap/STAGE1_SCOPE.md) - Complete Stage1 language specification
- [stage1-build-mode.md](stage1-build-mode.md) - CLI flag usage guide
- [grammar.md](spec/grammar.md) - Full language grammar
- [types.md](spec/types.md) - Type system reference

---

**Last Updated**: 2026-02-15  
**Applies To**: Aster Compiler v0.2.0+

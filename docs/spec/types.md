# Aster Type System Reference

## Overview

Aster uses a static type system with type inference, combining the safety of strong typing with the convenience of automatic type deduction where possible.

## Primitive Types

### Integer Types

| Type | Size | Signed | Range |
|------|------|--------|-------|
| `i8` | 8-bit | Yes | -128 to 127 |
| `i16` | 16-bit | Yes | -32,768 to 32,767 |
| `i32` | 32-bit | Yes | -2,147,483,648 to 2,147,483,647 |
| `i64` | 64-bit | Yes | -9,223,372,036,854,775,808 to 9,223,372,036,854,775,807 |
| `isize` | Platform | Yes | Pointer-sized signed integer |
| `u8` | 8-bit | No | 0 to 255 |
| `u16` | 16-bit | No | 0 to 65,535 |
| `u32` | 32-bit | No | 0 to 4,294,967,295 |
| `u64` | 64-bit | No | 0 to 18,446,744,073,709,551,615 |
| `usize` | Platform | No | Pointer-sized unsigned integer |

**Default**: Integer literals default to `i32`

### Floating-Point Types

| Type | Size | Precision |
|------|------|-----------|
| `f32` | 32-bit | Single precision (IEEE 754) |
| `f64` | 64-bit | Double precision (IEEE 754) |

**Default**: Float literals default to `f64`

### Boolean Type

**`bool`**: Either `true` or `false`

### Character Type

**`char`**: A single Unicode scalar value (32-bit)

Examples:
```rust
let c: char = 'a';
let emoji: char = '😀';
```

### String Types

**`String`**: Owned, heap-allocated UTF-8 string (growable)

**`str`**: String slice (view into UTF-8 data) - Stage 2+

## Compound Types

### Structs

Structures with named fields:

```rust
struct Point {
    x: i32,
    y: i32
}

let p = Point { x: 10, y: 20 };
let x_val = p.x;  // Field access
```

**Rules**:
- All fields must be initialized
- Fields are accessed with dot notation
- No inheritance (Aster is not object-oriented)

### Enums

Sum types (tagged unions):

```rust
enum Color {
    Red,
    Green,
    Blue
}

enum Option<T> {
    Some(T),
    None
}
```

**Rules**:
- Variants can have no data, or a single field (Stage 1)
- Multiple fields per variant in Stage 2+
- Pattern matching required to access variant data

### Tuples

Fixed-size heterogeneous collections:

```rust
let pair: (i32, i32) = (10, 20);
let triple: (String, bool, i32) = ("hello", true, 42);
```

**Access**: By position (`.0`, `.1`, etc.) - Stage 2+

### Unit Type

**`()`**: The unit type, analogous to `void` in C

Used for:
- Functions with no return value
- Empty/placeholder values

## Type Inference

Aster can often infer types automatically:

```rust
let x = 42;           // Inferred as i32
let name = "Alice";   // Inferred as &str
let numbers = Vec::new();  // Needs type annotation or usage context
```

**Explicit annotations** are required for:
- Function parameters
- Function return types (or inferred as `()`)
- Struct fields
- When inference is ambiguous

## Type Aliases (Stage 2+)

```rust
type UserId = u64;
type Point2D = (i32, i32);
```

Not available in Stage 1.

## Ownership and Memory (Simplified for Stage 1)

### Value Semantics

In Stage 1, all types use **value semantics** by default:

```rust
fn process(data: Data) -> Data {
    // data is moved in, returned moved out
    data
}

let d = Data { /* ... */ };
let result = process(d);
// d is no longer valid here (moved)
```

### Smart Pointers

**`Box<T>`**: Heap allocation

```rust
let boxed = Box::new(42);
```

**`Vec<T>`**: Dynamic array (heap-allocated)

```rust
let mut v = Vec::new();
v.push(10);
v.push(20);
```

**`String`**: Owned string (heap-allocated)

```rust
let s = String::new();
```

### No References in Stage 1

Stage 1 does NOT support:
- References (`&T`, `&mut T`)
- Borrowing
- Lifetimes

These are deferred to Stage 2.

## Type System Features by Stage

### Stage 1 (Core-0)
- ✅ Primitive types (integers, floats, bool, char)
- ✅ Structs with named fields
- ✅ Enums with simple variants
- ✅ Tuples
- ✅ Vec, String, Box, Option, Result
- ✅ Basic type inference
- ❌ Generics (user-defined)
- ❌ Traits
- ❌ References/lifetimes

### Stage 2 (Core-1)
- ✅ All Stage 1 features
- ✅ User-defined generics
- ✅ Traits and trait bounds
- ✅ References (`&T`, `&mut T`)
- ✅ Lifetimes
- ✅ Type aliases
- ✅ Associated types

### Stage 3 (Core-2)
- ✅ All Stage 2 features
- ✅ Advanced trait features
- ✅ Const generics
- ✅ Higher-ranked trait bounds (HRTBs)

## Type Checking

### Structural Equality

Two types are equal if they have the same structure:

```rust
struct A { x: i32 }
struct B { x: i32 }

// A and B are DIFFERENT types (nominal typing)
```

### Type Compatibility

- Integers of different sizes are NOT compatible (no implicit conversion)
- Explicit conversion required (Stage 2+)

```rust
let x: i32 = 42;
let y: i64 = x as i64;  // Explicit cast (Stage 2+)
```

## Built-in Types (Standard Library)

### Option<T>

Represents an optional value:

```rust
enum Option<T> {
    Some(T),
    None
}

let maybe_number: Option<i32> = Option::Some(42);

match maybe_number {
    Option::Some(n) => { /* use n */ },
    Option::None => { /* no value */ }
}
```

### Result<T, E>

Represents success or error:

```rust
enum Result<T, E> {
    Ok(T),
    Err(E)
}

let result: Result<i32, String> = Result::Ok(42);

match result {
    Result::Ok(value) => { /* success */ },
    Result::Err(error) => { /* handle error */ }
}
```

### Vec<T>

Dynamic array:

```rust
let mut v: Vec<i32> = Vec::new();
v.push(1);
v.push(2);
let len = v.len();  // 2
```

### String

Owned, growable UTF-8 string:

```rust
let mut s = String::new();
// String operations (Stage 2+)
```

## Type Annotations

### In Let Bindings

```rust
let x: i32 = 42;
let name: String = "Alice";
```

### In Function Signatures

```rust
fn add(a: i32, b: i32) -> i32 {
    a + b
}
```

### In Struct Fields

```rust
struct User {
    id: u64,
    name: String,
    active: bool
}
```

## Type Errors

Common type errors:

1. **Type mismatch**
   ```rust
   let x: i32 = "hello";  // Error: expected i32, found &str
   ```

2. **Missing type annotation**
   ```rust
   let v = Vec::new();  // Error: cannot infer type
   ```

3. **Undefined type**
   ```rust
   let x: Foo = ...;  // Error: type Foo not found
   ```

4. **Wrong number of fields**
   ```rust
   Point { x: 10 }  // Error: missing field y
   ```

## Examples

### Struct with Multiple Types
```rust
struct User {
    id: u64,
    name: String,
    age: i32,
    active: bool
}

let user = User {
    id: 12345,
    name: "Alice",
    age: 30,
    active: true
};
```

### Enum with Variants
```rust
enum Status {
    Pending,
    InProgress,
    Complete
}

let current = Status::InProgress;

match current {
    Status::Pending => { /* ... */ },
    Status::InProgress => { /* ... */ },
    Status::Complete => { /* ... */ }
}
```

### Generic Types (Stage 2+)
```rust
struct Pair<T> {
    first: T,
    second: T
}

let int_pair = Pair { first: 1, second: 2 };
let str_pair = Pair { first: "a", second: "b" };
```

## References

- [Grammar Reference](grammar.md)
- [Ownership Reference](ownership.md)
- [Memory Model](memory.md)
- [Stage1 Scope](../bootstrap/STAGE1_SCOPE.md)

---

**Status**: Stage 1 types defined  
**Last Updated**: 2026-02-15

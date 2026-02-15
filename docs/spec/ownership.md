# Aster Ownership and Borrowing (Stages 2-3)

## Overview

**Note**: Full ownership and borrowing are **NOT available in Stage 1 (Core-0)**. Stage 1 uses simplified value semantics. This document describes the ownership system for Stage 2 and Stage 3.

## Ownership Rules (Stage 2+)

1. **Each value has a single owner**
2. **When the owner goes out of scope, the value is dropped**
3. **Values can be moved or borrowed, but not both simultaneously**

## Value Semantics (Stage 1)

In Stage 1, we use simplified ownership:

- All values are **moved** by default
- No references (`&T`, `&mut T`)
- Use `Box<T>` for heap allocation
- Use `Vec<T>` for collections

```rust
fn process(data: Data) -> Data {
    // data is moved in
    // modify data
    data  // move data out
}

let d = Data { /* ... */ };
let result = process(d);
// d is NO LONGER VALID (moved into process)
```

## Borrowing (Stage 2+)

### Immutable Borrows (`&T`)

Multiple immutable borrows are allowed:

```rust
fn read_data(data: &Data) {
    // Can read data, cannot modify
}

let d = Data { /* ... */ };
read_data(&d);
read_data(&d);  // OK - multiple immutable borrows
// d is still valid
```

### Mutable Borrows (`&mut T`)

Only one mutable borrow at a time:

```rust
fn modify_data(data: &mut Data) {
    // Can read and modify data
}

let mut d = Data { /* ... */ };
modify_data(&mut d);
// d is still valid and potentially modified
```

**Rules**:
- Cannot have mutable borrow while immutable borrows exist
- Cannot have multiple mutable borrows simultaneously

## Lifetimes (Stage 2+)

Lifetimes ensure references don't outlive their data:

```rust
fn longest<'a>(x: &'a str, y: &'a str) -> &'a str {
    if x.len() > y.len() {
        x
    } else {
        y
    }
}
```

**Lifetime annotations** (`'a`, `'b`, etc.) specify how long references are valid.

## Move Semantics

### When Values Are Moved

1. **Assignment**
   ```rust
   let a = String::new();
   let b = a;  // a moved to b, a no longer valid
   ```

2. **Function calls**
   ```rust
   fn consume(s: String) { }
   
   let s = String::new();
   consume(s);  // s moved into consume, s no longer valid
   ```

3. **Return from function**
   ```rust
   fn make_string() -> String {
       String::new()  // Moved out of function
   }
   ```

### Copy Types

Some types implement `Copy` and are copied instead of moved:

- All primitive types (`i32`, `bool`, `f64`, etc.)
- Tuples of `Copy` types
- (Stage 2+) Types implementing `Copy` trait

```rust
let x = 42;
let y = x;  // x is copied, both x and y valid
```

## Ownership Patterns

### 1. Take Ownership and Return

```rust
fn process_and_return(data: Data) -> Data {
    // Process data
    data  // Return ownership to caller
}

let d = Data { /* ... */ };
let d = process_and_return(d);  // Rebind to same variable
```

### 2. Borrow for Reading (Stage 2+)

```rust
fn compute_size(data: &Data) -> usize {
    // Borrow data temporarily
    data.field_count()
}

let d = Data { /* ... */ };
let size = compute_size(&d);
// d still valid
```

### 3. Borrow for Modification (Stage 2+)

```rust
fn update_data(data: &mut Data) {
    data.field = new_value;
}

let mut d = Data { /* ... */ };
update_data(&mut d);
// d modified but still owned by caller
```

## Common Ownership Errors (Stage 2+)

### 1. Use After Move

```rust
let s = String::new();
consume(s);
println(s);  // ERROR: s was moved
```

### 2. Multiple Mutable Borrows

```rust
let mut v = Vec::new();
let r1 = &mut v;
let r2 = &mut v;  // ERROR: cannot borrow mutably twice
```

### 3. Mutable + Immutable Borrow

```rust
let mut v = Vec::new();
let r1 = &v;
let r2 = &mut v;  // ERROR: cannot borrow mutably while immutably borrowed
```

### 4. Reference Outlives Data

```rust
let r;
{
    let x = 5;
    r = &x;  // ERROR: x dropped at end of scope
}
println(r);  // ERROR: r references dropped value
```

## Ownership and Structs

### Owned Fields

```rust
struct User {
    name: String,   // User owns the String
    id: u64        // User owns the u64
}

let u = User {
    name: "Alice",
    id: 123
};
// When u goes out of scope, name and id are dropped
```

### Borrowed Fields (Stage 2+)

```rust
struct UserRef<'a> {
    name: &'a str,  // Borrows str from elsewhere
    id: u64
}
```

## Drop Semantics

Values are automatically cleaned up when they go out of scope:

```rust
{
    let s = String::new();
    // Use s
}  // s dropped here, memory freed
```

**Custom drop** (Stage 2+):
```rust
impl Drop for MyType {
    fn drop(&mut self) {
        // Custom cleanup
    }
}
```

## Smart Pointers

### Box<T> - Owned Heap Allocation

```rust
let b = Box::new(42);
// Value on heap, b owns it
```

### Rc<T> - Reference Counted (Stage 2+)

Multiple owners via reference counting:

```rust
let a = Rc::new(5);
let b = Rc::clone(&a);  // Both a and b own the value
```

### Arc<T> - Atomic Reference Counted (Stage 3+)

Thread-safe reference counting.

## Best Practices

### Stage 1 (Current)
1. Use value semantics - pass by value
2. Return owned values from functions
3. Use `Box<T>` for heap allocation
4. Use `Vec<T>` for collections
5. Keep ownership transfer explicit

### Stage 2 (Future)
1. Prefer borrowing over moving when possible
2. Use `&T` for read-only access
3. Use `&mut T` for modification
4. Keep borrows as short as possible
5. Avoid complex lifetime annotations

### Stage 3 (Future)
1. Use `Rc<T>` / `Arc<T>` sparingly
2. Prefer owned data or borrows
3. Use interior mutability (`RefCell`, `Mutex`) only when needed

## Examples

### Stage 1 (Value Semantics)
```rust
struct Point {
    x: i32,
    y: i32
}

fn translate(mut p: Point, dx: i32, dy: i32) -> Point {
    p.x = p.x + dx;
    p.y = p.y + dy;
    p
}

let p1 = Point { x: 0, y: 0 };
let p2 = translate(p1, 10, 20);
// p1 no longer valid, p2 is (10, 20)
```

### Stage 2 (Borrowing)
```rust
fn translate(p: &mut Point, dx: i32, dy: i32) {
    p.x += dx;
    p.y += dy;
}

let mut p = Point { x: 0, y: 0 };
translate(&mut p, 10, 20);
// p still valid, now (10, 20)
```

## Migration Path

**From Stage 1 to Stage 2**:

1. Identify functions that consume values unnecessarily
2. Change to borrow instead of move
3. Add lifetime annotations where needed
4. Test for ownership errors

## References

- [Types Reference](types.md)
- [Memory Model](memory.md)
- [Stage1 Scope](../bootstrap/STAGE1_SCOPE.md)

---

**Status**: Stage 2/3 feature (not in Stage 1)  
**Last Updated**: 2026-02-15

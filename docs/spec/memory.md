# Aster Memory Model

## Overview

The Aster memory model defines how data is stored, accessed, and managed during program execution.

## Memory Regions

### 1. Stack

**Characteristics**:
- Fast allocation/deallocation
- Fixed size per thread
- LIFO (Last In, First Out) allocation
- Automatic cleanup when scope ends

**Stored on stack**:
- Local variables
- Function parameters
- Return addresses
- Small structs and enums

**Example**:
```rust
fn foo() {
    let x = 42;        // x on stack
    let p = Point {    // p on stack (if small enough)
        x: 10,
        y: 20
    };
}  // x and p automatically cleaned up
```

### 2. Heap

**Characteristics**:
- Dynamic allocation
- Slower than stack
- Manual management (via ownership)
- Can outlive function scope

**Stored on heap**:
- `Box<T>` values
- `Vec<T>` data
- `String` contents
- Large data structures

**Example**:
```rust
fn bar() {
    let b = Box::new(42);     // 42 on heap, pointer on stack
    let v = Vec::new();        // Vec data on heap
}  // Heap memory freed via Drop
```

### 3. Static/Global (Stage 2+)

**Characteristics**:
- Entire program lifetime
- Fixed address
- Initialized before main()

**Example (Stage 2+)**:
```rust
static VERSION: i32 = 1;
const PI: f64 = 3.14159;
```

## Memory Layout

### Primitive Types

| Type | Size | Alignment |
|------|------|-----------|
| `i8`, `u8` | 1 byte | 1 byte |
| `i16`, `u16` | 2 bytes | 2 bytes |
| `i32`, `u32`, `f32` | 4 bytes | 4 bytes |
| `i64`, `u64`, `f64` | 8 bytes | 8 bytes |
| `isize`, `usize` | Platform | Platform |
| `bool` | 1 byte | 1 byte |
| `char` | 4 bytes | 4 bytes |

### Struct Layout

Structs are laid out in memory with padding for alignment:

```rust
struct Example {
    a: u8,   // 1 byte + 3 bytes padding
    b: u32,  // 4 bytes
    c: u16   // 2 bytes + 2 bytes padding
}
// Total: 12 bytes (with padding)
```

**Alignment rules**:
- Each field aligned to its natural alignment
- Struct aligned to largest field alignment

### Enum Layout

Enums use tag + data layout:

```rust
enum Option<T> {
    Some(T),
    None
}
// Layout: 
// - Tag (1-4 bytes)
// - Padding for alignment
// - Largest variant data
```

## Allocation Strategies

### Stack Allocation (Default)

Fast, automatic, limited size:

```rust
fn stack_example() {
    let x = 42;                    // Stack allocated
    let arr = [1, 2, 3, 4, 5];    // Stack allocated (small array)
}
```

### Heap Allocation (Explicit)

Manual via `Box`, `Vec`, `String`:

```rust
fn heap_example() {
    let b = Box::new(42);           // Heap allocated
    let mut v = Vec::new();         // Vec capacity on heap
    v.push(1);
}
```

## Memory Safety

### Guaranteed by Ownership

1. **No use-after-free**: Ownership prevents accessing freed memory
2. **No double-free**: Each value has single owner
3. **No memory leaks** (mostly): Automatic cleanup via Drop

### Memory Safety Violations (Prevented)

1. **Dangling pointers**: Prevented by ownership (Stage 2+ with references)
2. **Buffer overflows**: Bounds checking on arrays/vectors
3. **Null pointer dereferences**: No null in safe code (use `Option<T>`)
4. **Data races**: Prevented by ownership (Stage 3+ with threads)

## Memory Management Patterns

### RAII (Resource Acquisition Is Initialization)

Resources tied to object lifetime:

```rust
struct File {
    handle: FileHandle
}

impl Drop for File {
    fn drop(&mut self) {
        // Close file when File dropped
        close_file(self.handle);
    }
}

fn use_file() {
    let f = open_file("data.txt");
    // Use file
}  // File automatically closed
```

### Arena Allocation (Stage 2+)

Allocate many objects in single buffer, free all at once:

```rust
struct Arena {
    buffer: Vec<u8>,
    offset: usize
}

impl Arena {
    fn alloc<T>(&mut self, value: T) -> &mut T {
        // Allocate in arena buffer
        // ...
    }
}
```

### Pool Allocation (Stage 3+)

Pre-allocate objects for reuse.

## Performance Characteristics

### Stack vs Heap

| Operation | Stack | Heap |
|-----------|-------|------|
| Allocation | O(1), ~ns | O(1)-O(log n), ~μs |
| Deallocation | O(1), ~ns | O(1), ~μs |
| Access | O(1), ~ns | O(1), ~ns (+ indirection) |
| Cache locality | Excellent | Varies |

### Guidelines

1. **Prefer stack** for small, short-lived data
2. **Use heap** for large or long-lived data
3. **Batch allocations** when possible
4. **Reuse allocations** in hot paths

## Memory Footprint

### Measuring Memory Usage

```rust
use std::mem::size_of;

let struct_size = size_of::<MyStruct>();
let vec_overhead = size_of::<Vec<i32>>();  // Just the Vec itself, not data
```

### Minimizing Memory Usage

1. **Pack small types**: Use `u8` instead of `i32` when range allows
2. **Avoid unnecessary Boxing**: Use stack when possible
3. **Reuse collections**: Clear and reuse instead of recreating
4. **Use iterators**: Avoid intermediate allocations

## Zero-Cost Abstractions

Aster provides abstractions with no runtime cost:

1. **Iterators**: Compile to same code as hand-written loops
2. **Ownership**: Zero runtime overhead
3. **Generics**: Monomorphization (no vtables in simple cases)

## Unsafe and FFI (Stage 3+)

### Unsafe Blocks

Allow low-level memory operations:

```rust
unsafe {
    let raw_ptr = &x as *const i32;
    let value = *raw_ptr;  // Dereference raw pointer
}
```

### Foreign Function Interface

Calling C functions:

```rust
extern "C" {
    fn malloc(size: usize) -> *mut u8;
    fn free(ptr: *mut u8);
}
```

## Memory Initialization

### Default Values

Not all types have default values. Use `Default` trait (Stage 2+):

```rust
let x: i32 = Default::default();  // 0
let v: Vec<i32> = Default::default();  // Empty vec
```

### Uninitialized Memory (Unsafe, Stage 3+)

```rust
use std::mem::MaybeUninit;

let mut data: MaybeUninit<i32> = MaybeUninit::uninit();
unsafe {
    data.write(42);
    let value = data.assume_init();
}
```

## Memory Alignment

### Alignment Requirements

```rust
use std::mem::align_of;

align_of::<u8>();    // 1
align_of::<u16>();   // 2
align_of::<u32>();   // 4
align_of::<u64>();   // 8
```

### Custom Alignment (Stage 3+)

```rust
#[repr(align(16))]
struct Aligned {
    data: [u8; 16]
}
```

## Examples

### Stack vs Heap

```rust
fn stack_vs_heap() {
    // Stack: fast, limited size
    let stack_data = [0; 100];  // 400 bytes on stack
    
    // Heap: slower, unlimited (virtual) size
    let heap_data = Box::new([0; 1_000_000]);  // 4MB on heap
}
```

### Growing Collections

```rust
fn vec_growth() {
    let mut v = Vec::new();  // No allocation yet
    v.push(1);               // Allocates capacity 4 (typical)
    v.push(2);
    v.push(3);
    v.push(4);
    v.push(5);               // Reallocates to capacity 8
    // Capacity grows exponentially (2x)
}
```

### Custom Allocator (Stage 3+)

```rust
use std::alloc::{GlobalAlloc, System, Layout};

struct MyAllocator;

unsafe impl GlobalAlloc for MyAllocator {
    unsafe fn alloc(&self, layout: Layout) -> *mut u8 {
        // Custom allocation logic
        System.alloc(layout)
    }
    
    unsafe fn dealloc(&self, ptr: *mut u8, layout: Layout) {
        System.dealloc(ptr, layout)
    }
}

#[global_allocator]
static ALLOCATOR: MyAllocator = MyAllocator;
```

## Platform Specifics

### 32-bit vs 64-bit

| Type | 32-bit | 64-bit |
|------|--------|--------|
| `usize`, `isize` | 4 bytes | 8 bytes |
| Pointer | 4 bytes | 8 bytes |
| Maximum addressable | 4 GB | 16 EB |

### Endianness

Aster is endian-neutral, but platform native endianness used for primitive types.

## References

- [Types Reference](types.md)
- [Ownership Reference](ownership.md)
- [Stage1 Scope](../bootstrap/STAGE1_SCOPE.md)

---

**Status**: Core memory model defined  
**Last Updated**: 2026-02-15

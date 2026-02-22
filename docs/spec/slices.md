# Slices and Arrays

**Status**: Phase 6 ✅  
**Spec Version**: 1.0

## Overview

Aster provides two related types for contiguous sequences of values:

- **`[T; N]`** — a fixed-size array of exactly `N` elements of type `T`.  Stored inline (stack or inside another type).
- **`[T]`** (slice) — a dynamically-sized view into a contiguous sequence of `T`.  Always used behind a reference: `&[T]` or `&mut [T]`.
- **`str`** — a UTF-8 string slice; essentially `[u8]` with the invariant that the bytes form valid UTF-8.

## Syntax

### Fixed-Size Array Type

```aster
let arr: [i32; 4] = [1, 2, 3, 4];
```

### Slice Type Annotation

```aster
fn sum(data: &[i32]) -> i32 { ... }
```

### Array Literal

```aster
let nums = [10, 20, 30];   // type is [i32; 3]
let zeros = [0; 8];        // type is [i32; 8]  (repeat syntax, future)
```

### Slicing (Range Index)

```aster
let first_two = &arr[0..2];   // &[i32] slice of elements 0 and 1
```

## Semantics

### Array Type `[T; N]`

- `N` must be a compile-time constant `usize`.
- Array types are distinct: `[i32; 3]` ≠ `[i32; 4]`.
- Arrays implement `Copy` when `T: Copy`.
- Memory layout: `N` contiguous `T` values, no header.

### Slice Type `[T]`

- A fat pointer: `(ptr: *T, len: usize)`.
- Cannot be used as a value directly; must be behind `&[T]` or `&mut [T]`.
- `len()` is an O(1) operation.

### `str`

- Distinct from `String` (owned, heap-allocated).
- `&str` is the standard way to pass string data without ownership.
- All string literals `"hello"` have type `&str`.

## Built-in Operations

| Operation | Type | Description |
|-----------|------|-------------|
| `slice_len(s)` | `fn(&[T]) -> usize` | Number of elements |
| `slice_get(s, i)` | `fn(&[T], usize) -> T` | Indexed access (panics on OOB) |
| `slice_from_vec(v)` | `fn(&Vec<T>) -> &[T]` | Borrow a Vec as a slice |
| `arr[i]` | index expression | Equivalent to `slice_get` |
| `arr[lo..hi]` | slice of slice | Returns `&[T]` sub-slice |

## Type Coercions

- `&[T; N]` coerces to `&[T]` automatically (unsized coercion).
- `&Vec<T>` coerces to `&[T]` (via `Deref`).
- `&String` coerces to `&str`.

## Examples

```aster
fn sum_slice(data: &[i32]) -> i32 {
    let total = 0;
    let i = 0;
    while i < slice_len(data) {
        total = total + slice_get(data, i);
        i = i + 1;
    }
    total
}

fn main() -> i32 {
    let nums = [1, 2, 3, 4, 5];
    sum_slice(&nums)   // coercion from &[i32; 5] to &[i32]
}
```

## Error Codes

| Code | Meaning |
|------|---------|
| E0610 | Array index out of bounds (compile-time) |
| E0611 | Slice length mismatch |
| E0612 | Cannot use `[T]` as a value type; use `&[T]` |

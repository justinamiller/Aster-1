# Standard Library

**Status**: Phase 7 ✅  
**Updated**: 2026-02-22

---

## Overview

The Aster standard library provides a rich set of built-in types and their methods.
All methods listed here are registered in the type checker's `_implMethods` table
and are available in user code without any `use` / `import` declaration.

---

## `String` Methods

```aster
fn demo(mut s: String) {
    let n: usize = s.len();
    let empty: bool = s.is_empty();
    s.push_str(" world");
    s.push('!');
    let upper: String = s.to_uppercase();
    let lower: String = s.to_lowercase();
    let trimmed: String = s.trim();
    let starts: bool = s.starts_with("hello");
    let ends: bool = s.ends_with("!");
    let contains: bool = s.contains("world");
    let found: Option<usize> = s.find("world");
    let replaced: String = s.replace("world", "aster");
    let parts: Vec<String> = s.split(" ");
    let chars: Vec<char> = s.chars();
    let repeated: String = s.repeat(3);
}
```

| Method         | Signature                                       | Returns         |
|----------------|-------------------------------------------------|-----------------|
| `len`          | `() -> usize`                                   | Byte length      |
| `is_empty`     | `() -> bool`                                    |                  |
| `push_str`     | `(str) -> void`                                 | Append slice     |
| `push`         | `(char) -> void`                                | Append char      |
| `starts_with`  | `(str) -> bool`                                 |                  |
| `ends_with`    | `(str) -> bool`                                 |                  |
| `contains`     | `(str) -> bool`                                 |                  |
| `find`         | `(str) -> Option<usize>`                        |                  |
| `replace`      | `(str, str) -> String`                          |                  |
| `trim`         | `() -> String`                                  | Strip whitespace |
| `trim_start`   | `() -> String`                                  |                  |
| `trim_end`     | `() -> String`                                  |                  |
| `to_uppercase` | `() -> String`                                  |                  |
| `to_lowercase` | `() -> String`                                  |                  |
| `to_string`    | `() -> String`                                  |                  |
| `split`        | `(str) -> Vec<String>`                          |                  |
| `chars`        | `() -> Vec<char>`                               |                  |
| `as_str`       | `() -> String`                                  |                  |
| `clone`        | `() -> String`                                  |                  |
| `repeat`       | `(usize) -> String`                             |                  |
| `parse`        | `() -> T`                                       | Generic parse    |

---

## `Vec<T>` Iterator Methods

```aster
fn demo(v: Vec<i32>) {
    let doubled: Vec<i32> = v.map(|x| x * 2);
    let evens: Vec<i32> = v.filter(|x| x % 2 == 0);
    let sum: i32 = v.fold(0, |acc, x| acc + x);
    let count: usize = v.count();
    let any_pos: bool = v.any(|x| x > 0);
    let all_pos: bool = v.all(|x| x > 0);
    let first: Option<i32> = v.first();
    let sorted: Vec<i32> = v.iter().collect();
}
```

| Method       | Signature                                             | Notes                     |
|--------------|-------------------------------------------------------|---------------------------|
| `iter`       | `() -> Vec<T>`                                        | Immutable iterator         |
| `iter_mut`   | `() -> Vec<T>`                                        | Mutable iterator           |
| `into_iter`  | `() -> Vec<T>`                                        | Consuming iterator         |
| `map`        | `(fn(T) -> U) -> Vec<U>`                              | Transform elements         |
| `filter`     | `(fn(T) -> bool) -> Vec<T>`                           | Keep matching elements     |
| `fold`       | `(U, fn(U, T) -> U) -> U`                             | Reduce                     |
| `collect`    | `() -> Vec<T>`                                        | Collect iterator           |
| `enumerate`  | `() -> Vec<T>`                                        | Add indices                |
| `zip`        | `(Vec<U>) -> Vec<T>`                                  | Pair elements              |
| `flatten`    | `() -> Vec<T>`                                        |                            |
| `chain`      | `(Vec<T>) -> Vec<T>`                                  |                            |
| `take`       | `(usize) -> Vec<T>`                                   |                            |
| `skip`       | `(usize) -> Vec<T>`                                   |                            |
| `count`      | `() -> usize`                                         |                            |
| `any`        | `(fn(T) -> bool) -> bool`                             |                            |
| `all`        | `(fn(T) -> bool) -> bool`                             |                            |
| `find`       | `(fn(T) -> bool) -> Option<T>`                        |                            |
| `position`   | `(fn(T) -> bool) -> Option<usize>`                    |                            |
| `first`      | `() -> Option<T>`                                     |                            |
| `last`       | `() -> Option<T>`                                     |                            |
| `sort`       | `() -> void`                                          | Requires `Ord` bound       |
| `dedup`      | `() -> void`                                          |                            |
| `retain`     | `(fn(T) -> bool) -> void`                             |                            |
| `extend`     | `(Vec<T>) -> void`                                    |                            |
| `clear`      | `() -> void`                                          |                            |
| `clone`      | `() -> Vec<T>`                                        |                            |
| `contains`   | `(T) -> bool`                                         |                            |
| `join`       | `(str) -> String`                                     | For `Vec<String>`          |

---

## `HashMap<K, V>` Iterator Methods

| Method          | Signature                      | Returns                    |
|-----------------|--------------------------------|----------------------------|
| `iter`          | `() -> Vec<(K, V)>`            | All key-value pairs         |
| `keys`          | `() -> Vec<K>`                 |                             |
| `values`        | `() -> Vec<V>`                 |                             |
| `len`           | `() -> usize`                  |                             |
| `is_empty`      | `() -> bool`                   |                             |
| `contains_key`  | `(K) -> bool`                  |                             |
| `get`           | `(K) -> Option<V>`             |                             |
| `remove`        | `(K) -> Option<V>`             |                             |

---

## `Option<T>` Combinators

```aster
let x: Option<i32> = Some(42);
let y: Option<i32> = x.map(|v| v * 2);        // Some(84)
let z: i32         = x.unwrap_or(0);           // 42
let b: bool        = x.is_some();              // true
let r: Result<i32, str> = x.ok_or("missing"); // Ok(42)
```

| Method            | Signature                                  | Returns            |
|-------------------|--------------------------------------------|---------------------|
| `map`             | `(fn(T) -> U) -> Option<U>`               | Transform value     |
| `and_then`        | `(fn(T) -> Option<U>) -> Option<U>`       | Flat-map            |
| `or_else`         | `(fn() -> Option<T>) -> Option<T>`        | Alternative         |
| `unwrap`          | `() -> T`                                  | Panic if None       |
| `unwrap_or`       | `(T) -> T`                                 | Default value       |
| `unwrap_or_else`  | `(fn() -> T) -> T`                         | Lazy default        |
| `is_some`         | `() -> bool`                               |                     |
| `is_none`         | `() -> bool`                               |                     |
| `filter`          | `(fn(T) -> bool) -> Option<T>`            |                     |
| `ok_or`           | `(E) -> Result<T, E>`                      |                     |
| `ok_or_else`      | `(fn() -> E) -> Result<T, E>`             |                     |
| `flatten`         | `() -> Option<T>`                          | For `Option<Option<T>>` |
| `take`            | `() -> Option<T>`                          | Moves out of `self` |
| `cloned`          | `() -> Option<T>`                          | Clone inner value   |

---

## `Result<T, E>` Combinators

```aster
let r: Result<i32, str> = Ok(42);
let doubled: Result<i32, str> = r.map(|v| v * 2);
let ok: bool = r.is_ok();
let val: i32 = r.unwrap_or(0);
```

| Method              | Signature                                              | Returns               |
|---------------------|--------------------------------------------------------|-----------------------|
| `map`               | `(fn(T) -> U) -> Result<U, E>`                        | Transform Ok value    |
| `map_err`           | `(fn(E) -> F) -> Result<T, F>`                        | Transform Err value   |
| `and_then`          | `(fn(T) -> Result<U, E>) -> Result<U, E>`             | Flat-map              |
| `or_else`           | `(fn(E) -> Result<T, F>) -> Result<T, F>`             | Error recovery        |
| `unwrap`            | `() -> T`                                              | Panic if Err          |
| `unwrap_or`         | `(T) -> T`                                             | Default on Err        |
| `unwrap_or_else`    | `(fn(E) -> T) -> T`                                    | Lazy default on Err   |
| `is_ok`             | `() -> bool`                                           |                       |
| `is_err`            | `() -> bool`                                           |                       |
| `ok`                | `() -> Option<T>`                                      | Discard Err           |
| `err`               | `() -> Option<E>`                                      | Discard Ok            |
| `expect`            | `(str) -> T`                                           | Panic with message    |
| `flatten`           | `() -> Result<T, E>`                                   | For `Result<Result<T,E>,E>` |

---

## Math Functions

Available as `Math.sqrt(x)` method calls or as free functions via `aster::math`:

| Function  | Signature                | Description               |
|-----------|--------------------------|---------------------------|
| `abs`     | `(f64) -> f64`           | Absolute value             |
| `sqrt`    | `(f64) -> f64`           | Square root                |
| `cbrt`    | `(f64) -> f64`           | Cube root                  |
| `pow`     | `(f64, f64) -> f64`      | x raised to y              |
| `exp`     | `(f64) -> f64`           | eˣ                         |
| `ln`      | `(f64) -> f64`           | Natural logarithm          |
| `log`     | `(f64, f64) -> f64`      | Logarithm with base        |
| `log2`    | `(f64) -> f64`           | Base-2 logarithm           |
| `log10`   | `(f64) -> f64`           | Base-10 logarithm          |
| `floor`   | `(f64) -> f64`           | Round down                 |
| `ceil`    | `(f64) -> f64`           | Round up                   |
| `round`   | `(f64) -> f64`           | Round to nearest           |
| `min`     | `(f64, f64) -> f64`      | Minimum of two values      |
| `max`     | `(f64, f64) -> f64`      | Maximum of two values      |
| `sin`     | `(f64) -> f64`           | Sine (radians)             |
| `cos`     | `(f64) -> f64`           | Cosine (radians)           |
| `tan`     | `(f64) -> f64`           | Tangent (radians)          |
| `atan2`   | `(f64, f64) -> f64`      | Two-argument arctangent    |
| `hypot`   | `(f64, f64) -> f64`      | √(x² + y²)                |

---

## References

- `src/Aster.Compiler/Frontend/TypeSystem/TypeChecker.cs` — `RegisterStdlibMethods()`
- `src/Aster.Compiler/Frontend/NameResolution/NameResolver.cs` — built-in symbol table
- `aster/stdlib/` — Aster-language implementations (Phase 8+)

# Aster Standard Library - Quick Reference

## Module Import Examples

```rust
// Import prelude (most common types and traits)
use stdlib::prelude::*;

// Import specific modules
use stdlib::alloc::{Vec, String};
use stdlib::io::{Read, Write, print, println};
use stdlib::fs::File;
use stdlib::time::Duration;

// Import with alias
use stdlib::io::IOError as Error;
```

## Common Patterns

### Error Handling

```rust
// Using ? operator for early return
fn read_config() -> Result<String, IOError> {
    let content = fs::read_to_string("config.txt")?;
    Result::Ok(content)
}

// Using match
match fs::File::open("data.txt") {
    Result::Ok(file) => { /* use file */ },
    Result::Err(e) => { /* handle error */ },
}

// Using unwrap_or for default values
let content = fs::read_to_string("optional.txt").unwrap_or(String::from("default"));
```

### Collections

```rust
// Vec
let mut v = Vec::new();
v.push(1);
v.push(2);
let last = v.pop(); // Option<i32>
let len = v.len();
let item = v.get(0); // Option<&i32>

// String
let mut s = String::new();
s.push('A');
s.push_str("ster");
let view = s.as_str(); // &str
```

### Iteration

```rust
// Slice iteration
let numbers = [1, 2, 3, 4, 5];
let mut iter = numbers.iter();
while let Option::Some(num) = iter.next() {
    println(&format!("{}", num));
}

// Vec iteration
let mut vec = Vec::new();
vec.push(1);
vec.push(2);

let mut i = 0;
while i < vec.len() {
    let item = vec.get(i).unwrap();
    i += 1;
}
```

### File I/O

```rust
// Read entire file
let content = fs::read_to_string("input.txt")?;

// Write to file
fs::write_str("output.txt", "Hello, world!")?;

// Manual file handling
let mut file = fs::File::open("data.txt")?;
let mut buffer = [0u8; 1024];
let n = file.read(&mut buffer)?;
```

### Option Usage

```rust
// Creating Options
let some = Option::Some(42);
let none: Option<i32> = Option::None;

// Checking
if some.is_some() { /* ... */ }
if none.is_none() { /* ... */ }

// Extracting values
let val = some.unwrap(); // Panics if None
let val = some.unwrap_or(0); // Default value
let val = some.unwrap_or_else(|| compute_default());

// Transforming
let doubled = some.map(|x| x * 2);
let result = some.and_then(|x| divide(x, 2));
```

### Result Usage

```rust
// Creating Results
let ok = Result::Ok(42);
let err = Result::Err("error message");

// Checking
if ok.is_ok() { /* ... */ }
if err.is_err() { /* ... */ }

// Extracting values
let val = ok.unwrap(); // Panics if Err
let val = ok.unwrap_or(0);
let val = ok.unwrap_or_else(|e| handle_error(e));

// Converting
let opt = ok.ok(); // Result -> Option
let err_opt = ok.err(); // Get error as Option

// Transforming
let doubled = ok.map(|x| x * 2);
let handled = ok.map_err(|e| format_error(e));
```

## Type Quick Reference

### Primitives

| Type | Description | Size |
|------|-------------|------|
| `bool` | Boolean | 1 byte |
| `char` | Unicode scalar | 4 bytes |
| `i8` to `i128` | Signed integers | 1-16 bytes |
| `u8` to `u128` | Unsigned integers | 1-16 bytes |
| `isize`, `usize` | Pointer-sized ints | platform |
| `f32`, `f64` | Floating point | 4-8 bytes |
| `()` | Unit type | 0 bytes |

### Compound Types

| Type | Description |
|------|-------------|
| `&T` | Immutable reference |
| `&mut T` | Mutable reference |
| `ptr<T>` | Raw pointer |
| `[T]` | Slice (DST) |
| `&[T]` | Slice reference |
| `str` | String slice (UTF-8) |
| `&str` | String slice reference |

### Collections (alloc)

| Type | Description | Use Case |
|------|-------------|----------|
| `Vec<T>` | Dynamic array | General-purpose list |
| `String` | Owned UTF-8 string | Mutable text |

### Enums

| Type | Description |
|------|-------------|
| `Option<T>` | Optional value (Some/None) |
| `Result<T, E>` | Success or error (Ok/Err) |
| `Ordering` | Comparison result (Less/Equal/Greater) |
| `IOError` | I/O error variants |

## Trait Quick Reference

### Core Traits

| Trait | Purpose | Derive |
|-------|---------|--------|
| `Copy` | Bitwise copy | Auto for primitives |
| `Clone` | Explicit clone | Requires `clone()` impl |
| `Drop` | Destructor | Custom cleanup |
| `Eq` | Equality | Requires `eq()` |
| `Ord` | Ordering | Requires `cmp()` |
| `Hash` | Hashing | Requires `hash()` |
| `Default` | Default value | Requires `default()` |

### Display Traits

| Trait | Purpose |
|-------|---------|
| `Display` | User-facing formatting |
| `Debug` | Developer formatting |

### Iterator Traits

| Trait | Purpose |
|-------|---------|
| `Iterator` | Iterator protocol |
| `IntoIterator` | Convert to iterator |
| `FromIterator` | Build from iterator |

### I/O Traits

| Trait | Purpose |
|-------|---------|
| `Read` | Byte reading |
| `Write` | Byte writing |

### Arithmetic Traits

| Trait | Operation |
|-------|-----------|
| `Add` | `+` |
| `Sub` | `-` |
| `Mul` | `*` |
| `Div` | `/` |
| `Rem` | `%` |

## Effect Annotations

| Annotation | Meaning | Example |
|------------|---------|---------|
| `@stable` | Stable API | `fn len(&self) -> usize` |
| `@experimental` | Experimental API | `fn try_lock(&self)` |
| `@unstable` | Unstable API | Internal use |
| `@io` | Performs I/O | `fn read(&mut self)` |
| `@alloc` | Allocates memory | `fn push(&mut self, T)` |
| `@unsafe` | Unsafe operation | `fn ptr_offset(ptr, n)` |

## Common Functions

### Core

```rust
panic(msg: &str) -> !
assert(condition: bool, msg: &str)
unreachable() -> !
swap<T>(a: &mut T, b: &mut T)
min<T: Ord>(a: T, b: T) -> T
max<T: Ord>(a: T, b: T) -> T
size_of<T>() -> usize
align_of<T>() -> usize
```

### I/O

```rust
print(s: &str)
println(s: &str)
stdin() -> Stdin
stdout() -> Stdout
stderr() -> Stderr
```

### Filesystem

```rust
fs::read_to_string(path: &str) -> Result<String, IOError>
fs::write_str(path: &str, content: &str) -> Result<(), IOError>
```

### Time

```rust
time::sleep(duration: Duration)
Instant::now() -> Instant
SystemTime::now() -> SystemTime
Duration::from_secs(secs: u64) -> Duration
Duration::from_millis(millis: u64) -> Duration
```

### Process

```rust
process::exit(code: i32) -> !
process::abort() -> !
process::id() -> u32
```

### Environment

```rust
env::var(key: &str) -> Option<String>
env::args() -> Vec<String>
env::current_dir() -> Result<String, IOError>
```

### Math

```rust
math::abs_i32(x: i32) -> i32
math::sqrt_f64(x: f64) -> f64
math::sin(x: f64) -> f64
math::pow_f64(base: f64, exp: f64) -> f64
```

## Naming Conventions

### Types
- PascalCase: `Vec`, `String`, `Duration`
- Acronyms: `IOError` (not `IoError`)

### Functions
- snake_case: `read_to_string`, `unwrap_or`
- Getters: no `get_` prefix (e.g., `len()` not `get_len()`)
- Booleans: `is_` prefix (e.g., `is_empty()`)

### Constants
- SCREAMING_SNAKE_CASE: `PI`, `MAX_SIZE`

### Modules
- snake_case: `io`, `fs`, `testing`

## Common Idioms

### RAII (Resource Acquisition Is Initialization)

```rust
{
    let file = File::open("data.txt")?;
    // Use file
} // File automatically closed here (Drop)
```

### Builder Pattern

```rust
let mut cmd = Command::new("program");
cmd.arg("--flag").arg("value");
let child = cmd.spawn()?;
```

### Method Chaining

```rust
let result = some_option
    .map(|x| x * 2)
    .filter(|x| x > 10)
    .unwrap_or(0);
```

### Early Return with ?

```rust
fn process() -> Result<(), IOError> {
    let data = read_file()?;
    let parsed = parse_data(data)?;
    write_result(parsed)?;
    Result::Ok(())
}
```

## Performance Tips

1. **Preallocate when size is known**
   ```rust
   let mut v = Vec::with_capacity(1000);
   ```

2. **Avoid unnecessary clones**
   ```rust
   // Good: borrow
   fn process(s: &str) { }
   
   // Bad: clone
   fn process(s: String) { }
   ```

3. **Use iterators instead of indexing**
   ```rust
   // Good
   for item in slice.iter() { }
   
   // Bad (bounds checks on every access)
   for i in 0..slice.len() {
       let item = slice[i];
   }
   ```

4. **Buffer I/O operations**
   ```rust
   // Don't read/write one byte at a time
   let mut buffer = [0u8; 4096];
   let n = file.read(&mut buffer)?;
   ```

5. **Use string slices when possible**
   ```rust
   // Good
   fn print_name(name: &str) { }
   
   // Bad (forces allocation)
   fn print_name(name: String) { }
   ```

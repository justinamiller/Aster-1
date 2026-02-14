# Aster Standard Library (stdlib) MVP

The Aster Standard Library provides essential primitives, collections, and system interfaces for building real-world applications in the Aster programming language.

## Design Principles

- **Zero-cost abstractions** - No runtime overhead for what you don't use
- **Explicit effects** - IO, allocation, and other effects are clearly declared
- **No hidden behavior** - No implicit allocations, no hidden global state
- **Deterministic** - Predictable performance and behavior
- **Layered architecture** - Clear dependency hierarchy between modules
- **Stability tiers** - Clear API stability guarantees

## Architecture

The stdlib is organized into 12 layers, each building on lower layers:

```
1. core      → Primitives, traits, Option, Result (no alloc, no io)
2. alloc     → Vec, String, heap allocation (requires alloc)
3. sync      → Mutex, atomic types (requires alloc)
4. io        → Read/Write traits, stdin/stdout (requires io)
5. fs        → File, Path (requires io + alloc)
6. net       → TCP/UDP sockets (requires io + alloc)
7. time      → Duration, Instant, SystemTime
8. fmt       → Formatting, Display trait (requires alloc)
9. math      → Mathematical functions
10. testing  → Test framework (requires io + alloc)
11. env      → Environment vars, args (requires io + alloc)
12. process  → Process spawning, exit codes (requires io + alloc)
```

## Stability Tiers

All public APIs are marked with one of three stability tiers:

- **@stable** - Guaranteed API stability in v1.x releases
- **@experimental** - API may change in minor versions
- **@unstable** - API may change at any time

## Module Overview

### core

Core primitives and fundamental traits. No allocation, no IO, no OS dependencies.

**Types:**
- Primitives: `bool`, `char`, `i8`-`i128`, `u8`-`u128`, `f32`, `f64`, `isize`, `usize`
- Pointers: `ptr<T>`, `slice<T>`, `str`
- Enums: `Option<T>`, `Result<T, E>`

**Traits:**
- `Copy`, `Clone`, `Drop`
- `Eq`, `Ord`, `Hash`
- `Default`, `Display`, `Debug`
- `Iterator`, `IntoIterator`
- Arithmetic: `Add`, `Sub`, `Mul`, `Div`, `Rem`

### alloc

Heap allocation and collections.

**Types:**
- `Vec<T>` - Dynamic array
- `String` - Owned UTF-8 string

**Functions:**
- `alloc()`, `dealloc()`, `realloc()` - Raw allocation primitives

### sync

Concurrency primitives (experimental).

**Types:**
- `Mutex<T>` - Mutual exclusion lock
- `AtomicBool`, `AtomicUsize` - Atomic types

### io

Input/output operations.

**Traits:**
- `Read`, `Write`

**Types:**
- `Stdin`, `Stdout`, `Stderr`
- `IOError` - IO error enum

**Functions:**
- `stdin()`, `stdout()`, `stderr()` - Get standard stream handles
- `print()`, `println()` - Print to stdout

### fs

Filesystem operations.

**Types:**
- `File` - File handle with `Read` and `Write` traits
- `Path` - Path manipulation
- `FileMode` - File open modes

**Functions:**
- `read_to_string()` - Read entire file
- `write_str()` - Write string to file

### net

Network sockets (experimental).

**Types:**
- `TcpListener`, `TcpStream` - TCP networking
- `UdpSocket` - UDP networking

### time

Time and duration handling.

**Types:**
- `Duration` - Time span with nanosecond precision
- `Instant` - Monotonic clock for measurements
- `SystemTime` - Wall clock time

**Functions:**
- `sleep()` - Sleep for a duration

### fmt

Formatting and string conversion.

**Types:**
- `Formatter` - String formatter
- `Error` - Formatting error

**Functions:**
- `format()` - Format a value to string

**Trait Implementations:**
- `Display` and `Debug` for primitives

### math

Mathematical functions.

**Constants:**
- `PI`, `E`

**Functions:**
- Integer: `abs`, `pow`, `gcd`, `lcm`
- Float: `sqrt`, `sin`, `cos`, `tan`, `exp`, `ln`, `floor`, `ceil`, etc.

### testing

Test framework.

**Types:**
- `TestCase` - Individual test
- `TestRunner` - Test harness
- `TestResult` - Test outcome

**Functions:**
- `assert_eq()`, `assert_ne()`, `assert_true()`, `assert_false()`

### env

Environment and command-line access.

**Functions:**
- `var()`, `set_var()`, `remove_var()` - Environment variables
- `args()` - Command-line arguments
- `current_dir()`, `set_current_dir()` - Working directory
- `home_dir()`, `temp_dir()` - Special directories

### process

Process management.

**Functions:**
- `exit()`, `abort()` - Terminate process
- `id()` - Get process ID

**Types:**
- `Command` - Process builder (experimental)
- `Child` - Running process handle (experimental)
- `ExitStatus` - Process exit code

## Usage Examples

### Hello World

```rust
use io::println;

fn main() {
    println("Hello, Aster!");
}
```

### Using Vec and String

```rust
use alloc::{Vec, String};

fn main() {
    let mut numbers = Vec::new();
    numbers.push(1);
    numbers.push(2);
    numbers.push(3);
    
    let mut name = String::from("Aster");
    name.push(' ');
    name.push_str("Language");
}
```

### Error Handling

```rust
use core::{Result, Option};
use fs::read_to_string;

fn read_config() -> Result<String, IOError> {
    let content = read_to_string("config.txt")?;
    Result::Ok(content)
}
```

### File I/O

```rust
use fs::File;
use io::{Read, Write};

fn copy_file(src: &str, dst: &str) -> Result<(), IOError> {
    let mut input = File::open(src)?;
    let mut output = File::create(dst)?;
    
    let mut buf = [0u8; 4096];
    loop {
        let n = input.read(&mut buf)?;
        if n == 0 { break; }
        output.write_all(&buf[0..n])?;
    }
    Result::Ok(())
}
```

### Testing

```rust
use testing::*;

fn test_addition() -> TestResult {
    assert_eq(&(2 + 2), &4, "2 + 2 should equal 4")
}

fn main() {
    let mut runner = TestRunner::new();
    runner.add(TestCase::new("addition", test_addition));
    let success = runner.run_all();
    
    if !success {
        process::exit(1);
    }
}
```

## Implementation Notes

### Intrinsics

The stdlib relies on compiler intrinsics for low-level operations that cannot be implemented in pure Aster:

- Memory operations: `intrinsic_memcpy`, `intrinsic_memset`, etc.
- Type information: `intrinsic_size_of`, `intrinsic_align_of`
- Allocation: `intrinsic_alloc`, `intrinsic_dealloc`
- IO operations: `intrinsic_stdin_read`, `intrinsic_stdout_write`, etc.
- System calls: File, network, process operations
- Math functions: Delegated to LLVM intrinsics

These intrinsics are implemented by the compiler and/or minimal runtime shims.

### Effect System

Functions are annotated with their effects:

- `@io` - Performs I/O operations
- `@alloc` - Allocates heap memory
- `@unsafe` - Requires unsafe context
- `@async` - Asynchronous operation (future extension)

### Memory Management

- **No garbage collection** - All memory management is explicit
- **RAII** - Resources are automatically cleaned up via `Drop` trait
- **Ownership** - Move semantics and borrow checking prevent use-after-free
- **Zero-cost abstractions** - Vec, String, etc. have no overhead vs raw pointers

### Performance

The stdlib is designed for predictable performance:

- All allocations are explicit (no hidden allocations)
- Iterator combinators optimize to same code as manual loops
- Generics are monomorphized (no runtime dispatch unless explicit)
- Inline hints for critical functions

## Future Extensions

Planned additions for future releases:

- `collections` - HashMap, HashSet, BTreeMap, LinkedList
- `regex` - Regular expression matching
- `json` - JSON parsing and serialization
- `http` - HTTP client and server
- `crypto` - Cryptographic primitives
- `async` - Async/await runtime
- `simd` - SIMD vector types

## Contributing

When adding to the stdlib:

1. Follow the layering model - only depend on lower layers
2. Mark stability tier on all public APIs
3. Declare all effects explicitly
4. Avoid hidden allocations or IO
5. Document performance characteristics
6. Add comprehensive tests
7. Follow naming conventions from Rust stdlib where applicable

## License

Part of the Aster language project.

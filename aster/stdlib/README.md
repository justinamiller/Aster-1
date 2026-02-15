# Aster Standard Library (stdlib)

The Aster Standard Library is a layered, minimal, but complete standard library that provides essential primitives while preserving Aster's predictability and performance guarantees.

## Design Principles

- **No hidden allocation**: All heap allocation is explicit via `@alloc` effect
- **No hidden I/O**: All I/O operations are marked with `@io` effect
- **No global state**: All state is explicit
- **Deterministic behavior**: Predictable, reproducible execution
- **Zero-cost abstractions**: No runtime overhead for unused features
- **Layered architecture**: Clear dependency hierarchy

## Stability Tiers

Every public API is marked with one of three stability levels:

- **@stable** — Guaranteed stable in v1, will not break
- **@experimental** — May change in minor versions, feedback welcome
- **@unstable** — May change or be removed at any time

## Module Layering

The stdlib is organized into 13 layers, where each layer may only depend on lower layers:

```
1. core        → Primitives, no alloc, no IO (foundation)
2. alloc       → Heap allocation (Vec, String, Box)
3. collections → Collection data structures (HashMap, HashSet, LinkedList)
4. sync        → Concurrency primitives (Mutex, RwLock, Atomics)
5. io          → I/O traits and streams (Read, Write)
6. fs          → Filesystem operations (Path, File)
7. net         → Networking (TCP, UDP, sockets)
8. time        → Time and duration types
9. fmt         → Formatting and printing
10. math       → Mathematical functions
11. testing    → Test framework
12. env        → Environment variables and args
13. process    → Process spawning and control
```

## Core Module (Layer 1)

**Stability:** @stable  
**Effects:** none (no alloc, no io, no OS)  
**Dependencies:** compiler intrinsics only

The core module provides fundamental types and traits:

### Primitive Types
- `bool` — Boolean (true/false)
- `char` — Unicode scalar value
- `i8`, `i16`, `i32`, `i64`, `i128`, `isize` — Signed integers
- `u8`, `u16`, `u32`, `u64`, `u128`, `usize` — Unsigned integers
- `f32`, `f64` — Floating point
- `ptr<T>` — Raw pointer
- `slice<T>` — View into contiguous sequence
- `str` — UTF-8 string view

### Core Types
- `Option<T>` — Optional value (Some/None)
- `Result<T, E>` — Error handling (Ok/Err)

### Core Traits
- `Copy` — Bitwise copyable types
- `Clone` — Explicit cloning
- `Drop` — Custom cleanup
- `Eq`, `Ord` — Equality and ordering
- `Hash` — Hashing for collections
- `Default` — Default values
- `Display`, `Debug` — Formatting
- `Iterator`, `IntoIterator` — Iteration
- `From`, `Into`, `TryFrom`, `TryInto` — Conversions

## Alloc Module (Layer 2)

**Stability:** @stable  
**Effects:** @alloc (performs heap allocation)  
**Dependencies:** core

Heap-allocated collections and smart pointers:

- `Vec<T>` — Dynamic array
- `String` — Owned UTF-8 string
- `Box<T>` — Heap-allocated pointer

## Collections Module (Layer 3)

**Stability:** @experimental  
**Effects:** @alloc (performs heap allocation)  
**Dependencies:** core, alloc

Collection data structures for common use cases:

- `HashMap<K, V>` — Hash table with key-value pairs
- `HashSet<T>` — Hash set of unique values
- `LinkedList<T>` — Doubly-linked list
- `Range<T>` — Exclusive range (start..end)
- `RangeInclusive<T>` — Inclusive range (start..=end)

## Sync Module (Layer 4)

**Stability:** @stable  
**Effects:** none (blocking, no explicit effect)  
**Dependencies:** core

Synchronization primitives for concurrent programming:

- `Mutex<T>` — Mutual exclusion
- `RwLock<T>` — Reader-writer lock
- `AtomicBool`, `AtomicUsize` — Atomic types

## IO Module (Layer 5)

**Stability:** @stable  
**Effects:** @io  
**Dependencies:** core, alloc

Input/output traits and standard streams:

- `Read` trait — Reading bytes
- `Write` trait — Writing bytes
- `Stdin`, `Stdout`, `Stderr` — Standard streams
- `IoError` — I/O error types

## FS Module (Layer 6)

**Stability:** @stable  
**Effects:** @io  
**Dependencies:** core, alloc, io

Filesystem operations:

- `Path`, `PathBuf` — Filesystem paths
- `File` — File handle
- `OpenOptions` — File opening configuration
- `Metadata` — File metadata
- Utilities: `open`, `remove_file`, `create_dir`

## Net Module (Layer 7)

**Stability:** @stable  
**Effects:** @io  
**Dependencies:** core, alloc, io

Basic networking:

- `Ipv4Addr`, `Ipv6Addr` — IP addresses
- `SocketAddr` — Socket address (IP + port)
- `TcpStream` — TCP connection
- `TcpListener` — TCP server
- `UdpSocket` — UDP socket

## Time Module (Layer 8)

**Stability:** @stable  
**Effects:** @io (reading system time)  
**Dependencies:** core

Time and duration types:

- `Duration` — Time span (secs + nanos)
- `SystemTime` — Wall clock time
- `Instant` — Monotonic clock
- Utilities: `now`, `instant_now`, `elapsed`

## Fmt Module (Layer 9)

**Stability:** @stable  
**Effects:** @io (printing), @alloc (formatting)  
**Dependencies:** core, alloc, io

Formatting and printing:

- `print`, `println` — Print to stdout
- `eprint`, `eprintln` — Print to stderr
- `Format` trait — String formatting
- Integer/float formatters

## Math Module (Layer 10)

**Stability:** @stable  
**Effects:** none  
**Dependencies:** core

Mathematical functions and constants:

- Constants: `PI`, `E`
- Functions: `abs`, `min`, `max`, `clamp`, `pow`
- Floating point: `sqrt`, `floor`, `ceil`, `round`

## Testing Module (Layer 11)

**Stability:** @experimental  
**Effects:** @io  
**Dependencies:** core, alloc, fmt

Test framework:

- `Test` trait — Test marker
- `TestResult` — Test outcome
- Assertions: `test_assert`, `test_assert_eq`, `test_assert_ne`
- Runner: `run_tests`

## Env Module (Layer 12)

**Stability:** @stable  
**Effects:** @io  
**Dependencies:** core, alloc

Environment and arguments:

- `args` — Command-line arguments
- `var`, `set_var`, `remove_var` — Environment variables
- `current_dir`, `set_current_dir` — Working directory

## Process Module (Layer 13)

**Stability:** @stable  
**Effects:** @io  
**Dependencies:** core, alloc, io

Process control:

- `exit`, `exit_success`, `exit_failure` — Process termination
- `Command` — Process builder
- `Child` — Child process handle
- `spawn`, `wait` — Process spawning
- `id` — Current process ID

## Effect System Integration

All stdlib APIs declare their effects:

- **No annotation** — Pure function (no allocation, no I/O)
- **@alloc** — May allocate heap memory
- **@io** — Performs I/O operations
- **@unsafe** — Unsafe operation (raw pointers, FFI)

## Runtime Dependencies

The stdlib assumes a minimal runtime that provides:

1. **Memory allocator** — `allocate`, `deallocate`, `reallocate`
2. **Panic handler** — `intrinsic_panic`
3. **System calls** — POSIX-like interface for I/O, networking, etc.
4. **Atomic operations** — Compare-and-swap, load, store

## Platform Support

The core module is platform-agnostic and works on any target.

Higher layers (io, fs, net, process) assume a POSIX-like environment but can be adapted to other platforms through the intrinsic function layer.

## Usage Example

```rust
use std::io::*;
use std::fmt::*;

fn main() {
    let message = "Hello from Aster stdlib!";
    println(message);
    
    let args = args();
    if vec_len(&args) > 1 {
        let first_arg = vec_get(&args, 1);
        print("First argument: ");
        println(string_as_str(first_arg));
    }
}
```

## Language Features

### Loop Constructs

Aster supports multiple loop constructs:

**While loop** - Condition-based loop:
```rust
while count < 10 {
    println("counting");
    count = count + 1;
}
```

**For loop** - Iterator-based loop:
```rust
for i in 0..10 {
    println("iteration");
}
```

**Loop** - Infinite loop (use with `break`):
```rust
loop {
    if done {
        break;
    }
}
```

**Do-while loop** - Execute at least once:
```rust
do {
    println("at least once");
} while condition;
```

### Array Literals

Arrays can be created using bracket syntax:

```rust
let numbers = [1, 2, 3, 4, 5];
let empty: Vec<i32> = [];
let matrix = [[1, 2], [3, 4]];
```

Array literals are converted to `Vec<T>` and support all Vec operations.

### Range Expressions

Ranges enable iteration:

```rust
// Exclusive range (0, 1, 2, 3, 4)
for i in 0..5 {
    println(i);
}

// Inclusive range (0, 1, 2, 3, 4, 5)  
for i in 0..=5 {
    println(i);
}
```

## Future Expansion

The following modules are planned for future releases:

- `async` — Async runtime and futures
- `regex` — Regular expressions
- `json` — JSON parsing and serialization
- `crypto` — Cryptographic primitives
- `rand` — Random number generation

## Contributing

The stdlib is designed for long-term stability. Changes must:

1. Maintain backward compatibility for @stable APIs
2. Follow the layering model
3. Declare all effects explicitly
4. Include comprehensive documentation
5. Have no hidden allocations or I/O

For experimental features, use @experimental or @unstable annotations.

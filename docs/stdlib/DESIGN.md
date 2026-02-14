# Aster Standard Library Design Document

## Overview

The Aster Standard Library (stdlib) is a production-grade, layered library providing essential primitives, collections, and system interfaces for the Aster programming language.

## Design Goals

1. **Predictability** - No hidden allocations, no hidden IO, explicit effects
2. **Performance** - Zero-cost abstractions, deterministic behavior
3. **Safety** - Ownership and borrowing prevent memory errors
4. **Composability** - Small, orthogonal components
5. **Long-term stability** - Clear stability tiers for API evolution

## Architecture

### Layering Model

The stdlib is organized into 12 distinct layers, each with clear dependencies:

```
Layer 12: process     (spawn, exit codes)
Layer 11: env         (environment, args)
Layer 10: testing     (test framework)
Layer 9:  math        (numeric helpers)
Layer 8:  fmt         (formatting, printing)
Layer 7:  time        (clocks, duration)
Layer 6:  net         (tcp/udp sockets)
Layer 5:  fs          (filesystem)
Layer 4:  io          (files, stdin/stdout)
Layer 3:  sync        (concurrency)
Layer 2:  alloc       (heap collections)
Layer 1:  core        (primitives, no alloc/io)
```

**Dependency Rules:**
- Each layer may only depend on lower layers
- No circular dependencies
- Higher layers are optional (can use core without alloc)

### Effect System

All functions that perform side effects are explicitly marked:

- `@io` - Performs I/O operations (file, network, console)
- `@alloc` - Allocates heap memory
- `@unsafe` - Requires unsafe context (memory manipulation)
- `@async` - Asynchronous operation (future)
- `@ffi` - Foreign function call
- `@throw` - May throw exceptions

**Benefits:**
- Clear understanding of function behavior
- Enable pure functional subsets
- Better optimization opportunities
- Easier reasoning about code

### Stability Tiers

Every public API is marked with one of three stability levels:

**@stable**
- Guaranteed API stability in v1.x releases
- Will not be removed or have breaking changes
- Safe for production use
- Most of core, alloc, io, fs, time, fmt, math

**@experimental**
- May change in minor versions
- Feedback welcome
- Use with caution in production
- Most of sync, net, testing, process

**@unstable**
- May change at any time
- For research and development
- Not recommended for production
- Future features

## Module Details

### 1. core (Layer 1)

**Purpose:** Fundamental types and traits with no dependencies

**Characteristics:**
- No allocation
- No I/O
- No OS dependencies
- Pure, deterministic
- Always available

**Key Components:**
- Primitive types (bool, integers, floats, char)
- Pointer and slice types
- String slices (UTF-8)
- Option and Result enums
- Core traits (Copy, Clone, Drop, Eq, Ord, etc.)
- Iterator infrastructure

**Design Decisions:**
- `str` is always a UTF-8 slice (no encoding ambiguity)
- `Option` and `Result` are the only nullable/error types
- All comparison operations return `Ordering` enum
- Iterators are zero-cost (compile to same code as loops)

### 2. alloc (Layer 2)

**Purpose:** Heap allocation and dynamic collections

**Characteristics:**
- Requires heap allocator
- Explicit allocation functions
- RAII cleanup via Drop

**Key Components:**
- Raw allocation: `alloc()`, `dealloc()`, `realloc()`
- `Vec<T>` - Dynamic array with capacity management
- `String` - Owned UTF-8 string (wrapper around Vec<u8>)

**Design Decisions:**
- Growth strategy: start at 4, double on resize
- Vec owns its memory (Drop deallocates)
- String validates UTF-8 on construction
- No hidden allocations (all marked with @alloc)

**Performance:**
- Vec access: O(1)
- Vec push (amortized): O(1)
- Vec growth: O(n) on resize
- String operations: O(n) for UTF-8 validation

### 3. sync (Layer 3)

**Purpose:** Concurrency primitives

**Stability:** @experimental (API may change)

**Key Components:**
- `Mutex<T>` - Mutual exclusion with RAII guard
- `AtomicBool`, `AtomicUsize` - Lock-free atomics

**Design Decisions:**
- Simple spinlock mutex (not production-grade)
- No global executor (future: async runtime)
- Atomic operations delegate to intrinsics

**Future Extensions:**
- `RwLock`, `Condvar`
- `Arc`, `Rc` (reference counting)
- `Channel` (message passing)
- Async/await runtime

### 4. io (Layer 4)

**Purpose:** I/O abstraction layer

**Key Components:**
- `Read` and `Write` traits
- `Stdin`, `Stdout`, `Stderr` types
- `IOError` enum (no error strings for determinism)
- `print()`, `println()` functions

**Design Decisions:**
- No buffering by default (explicit control)
- Errors are enum (no dynamic strings)
- All IO marked with @io effect
- No implicit flushing

**Error Handling:**
- All IO returns Result<T, IOError>
- Errors are enum variants (deterministic)
- No strerror() or platform strings

### 5. fs (Layer 5)

**Purpose:** Filesystem operations

**Key Components:**
- `File` with Read/Write traits
- `Path` type for path manipulation
- `FileMode` flags (Read, Write, Create, etc.)
- Convenience functions: `read_to_string()`, `write_str()`

**Design Decisions:**
- Files auto-close via Drop
- Paths are validated UTF-8
- No implicit path normalization
- Platform path separators delegated to intrinsics

### 6. net (Layer 6)

**Purpose:** Network sockets

**Stability:** @experimental

**Key Components:**
- `TcpListener`, `TcpStream` for TCP
- `UdpSocket` for UDP
- Read/Write trait implementations

**Design Decisions:**
- Blocking I/O only (no async yet)
- Addresses are strings (parsed by intrinsics)
- Sockets auto-close via Drop

**Future Extensions:**
- Non-blocking I/O
- Unix domain sockets
- Async networking

### 7. time (Layer 7)

**Purpose:** Time measurement and delays

**Key Components:**
- `Duration` - Time span with nanosecond precision
- `Instant` - Monotonic clock for measurements
- `SystemTime` - Wall clock time
- `sleep()` function

**Design Decisions:**
- Duration uses u64 seconds + u32 nanos (fits in 12 bytes)
- Instant is monotonic (doesn't go backwards)
- SystemTime can jump (NTP adjustments)
- No timezone support (UTC only)

### 8. fmt (Layer 8)

**Purpose:** Formatting and string conversion

**Key Components:**
- `Formatter` type
- `Display` and `Debug` traits
- `format()` function
- Trait implementations for primitives

**Design Decisions:**
- No printf-style format strings (type-safe)
- Display for user-facing, Debug for developer
- Requires allocation (returns String)

**Future Extensions:**
- Format string macros
- Custom formatters
- No-alloc formatting to Write

### 9. math (Layer 9)

**Purpose:** Mathematical functions

**Key Components:**
- Constants: PI, E
- Integer math: abs, pow, gcd, lcm
- Float math: sqrt, sin, cos, tan, exp, ln, etc.

**Design Decisions:**
- All float operations delegate to LLVM intrinsics
- No complex numbers (future extension)
- No arbitrary precision (future: bigint module)

### 10. testing (Layer 10)

**Purpose:** Unit testing framework

**Stability:** @experimental

**Key Components:**
- `TestCase` - Individual test
- `TestRunner` - Test harness
- Assert functions: `assert_eq`, `assert_true`, etc.

**Design Decisions:**
- Tests return TestResult (Pass or Fail with message)
- Runner collects and executes tests
- Simple text output (no TAP/JUnit yet)

### 11. env (Layer 11)

**Purpose:** Environment and command-line access

**Key Components:**
- `var()`, `set_var()` - Environment variables
- `args()` - Command-line arguments
- `current_dir()`, `home_dir()`, `temp_dir()`

**Design Decisions:**
- All operations return Result or Option
- No caching (queries OS each time)
- Platform-specific paths

### 12. process (Layer 12)

**Purpose:** Process management

**Key Components:**
- `exit()`, `abort()` - Terminate process
- `Command` - Process builder
- `Child` - Running process handle

**Stability:** @experimental (except exit/abort)

**Design Decisions:**
- Command builder pattern
- Async spawn (wait for completion)
- No shell escaping (security)

## Implementation Strategy

### Compiler Intrinsics

The stdlib relies on compiler-provided intrinsics for operations that cannot be implemented in pure Aster:

**Memory:**
- `intrinsic_memcpy`, `intrinsic_memset`, `intrinsic_memmove`, `intrinsic_memcmp`
- `intrinsic_alloc`, `intrinsic_dealloc`, `intrinsic_realloc`
- `intrinsic_size_of`, `intrinsic_align_of`

**Type Construction:**
- `intrinsic_make_slice`, `intrinsic_make_str`
- `intrinsic_slice_len`, `intrinsic_slice_ptr`
- `intrinsic_str_len`, `intrinsic_str_ptr`

**I/O:**
- `intrinsic_stdin_read`, `intrinsic_stdout_write`, `intrinsic_stderr_write`
- `intrinsic_file_open`, `intrinsic_file_read`, `intrinsic_file_write`, `intrinsic_file_close`
- `intrinsic_tcp_listen`, `intrinsic_tcp_connect`, `intrinsic_udp_bind`

**System:**
- `intrinsic_exit`, `intrinsic_abort`
- `intrinsic_process_id`, `intrinsic_process_spawn`
- `intrinsic_env_var`, `intrinsic_args`

**Math:**
- `intrinsic_sqrt`, `intrinsic_sin`, `intrinsic_cos`, etc.

**Concurrency:**
- `intrinsic_atomic_load`, `intrinsic_atomic_store`, `intrinsic_atomic_swap`
- `intrinsic_yield`

### Runtime Shims

Minimal C/Rust shims may be needed for:
- FFI to libc (malloc, free, etc.)
- System calls
- Platform-specific behavior

**Design Principle:** Keep shims minimal and platform-abstracted

## Memory Model

### Ownership

- Every value has a single owner
- When owner goes out of scope, value is dropped
- Values can be moved or borrowed

### Borrowing

- Immutable borrows: `&T` (many allowed)
- Mutable borrows: `&mut T` (exclusive)
- Borrow checker enforces aliasing XOR mutability

### Drop Order

- Fields dropped in declaration order
- Tuple elements dropped left to right
- Vec drops elements in reverse order (LIFO)

## Error Handling

### Philosophy

- Use `Option<T>` for "no value" cases
- Use `Result<T, E>` for recoverable errors
- Use `panic()` for unrecoverable errors
- No exceptions (no unwinding)

### Error Types

- `IOError` - Enum of I/O errors
- Custom error types as needed
- No dynamic error strings (determinism)

## Testing

### Unit Tests

Tests are written using the `testing` module:

```rust
fn test_vec_push() -> TestResult {
    let mut v = Vec::new();
    v.push(1);
    assert_eq(&v.len(), &1, "length should be 1")
}
```

### Integration Tests

Examples in `examples/` directory serve as integration tests

### Property Tests

Future: QuickCheck-style property testing

## Performance Characteristics

### Core

- All operations: O(1) or explicitly documented
- No hidden allocations
- No dynamic dispatch (unless trait objects)

### Collections

**Vec:**
- Access: O(1)
- Push (amortized): O(1)
- Insert/Remove: O(n)
- Growth: Double on resize

**String:**
- Same as Vec<u8> plus UTF-8 validation
- Char iteration: O(n) (UTF-8 decoding)

### I/O

- Unbuffered by default
- Each read/write is a syscall
- Use buffering layer for performance

## Future Work

### Missing Data Structures

- `HashMap<K, V>`, `HashSet<T>` (needs hash algorithm)
- `BTreeMap<K, V>`, `BTreeSet<T>`
- `LinkedList<T>`
- `VecDeque<T>`
- `BinaryHeap<T>`

### Advanced Features

- Regex engine
- JSON parser/serializer
- HTTP client/server
- Cryptography (hashing, encryption)
- Compression (gzip, etc.)
- Async/await runtime

### Platform Support

- Windows support (currently Unix-focused)
- Embedded targets (no_std subset)
- WebAssembly

### Tooling

- Package manager integration
- Documentation generator
- Benchmark framework

## Stability Guarantees

### v1.0 Release

For v1.0 release, all `@stable` APIs will be frozen:
- No breaking changes in v1.x
- Deprecation warnings before removal
- Semantic versioning

### Evolution

- New features start as `@unstable`
- Promote to `@experimental` after initial feedback
- Promote to `@stable` after battle-testing
- `@stable` APIs never removed without major version

## Comparison with Other Languages

### vs Rust

**Similarities:**
- Ownership and borrowing
- Zero-cost abstractions
- No garbage collection
- Similar stdlib organization

**Differences:**
- Explicit effect annotations
- Simpler macro system
- Stricter purity requirements

### vs Go

**Similarities:**
- Simple, pragmatic
- Good concurrency primitives
- Fast compilation

**Differences:**
- No GC (manual memory management)
- Compile-time safety (ownership)
- More expressive type system

### vs C++

**Similarities:**
- Zero-cost abstractions
- Control over memory layout
- Template/generic metaprogramming

**Differences:**
- Simpler, more consistent
- Guaranteed memory safety
- No undefined behavior

## Conclusion

The Aster Standard Library provides a solid foundation for building production applications while maintaining the language's core principles of predictability, performance, and safety. The layered architecture allows users to pay only for what they use, while the explicit effect system provides transparency about program behavior.

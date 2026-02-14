# Aster Standard Library Documentation

This directory contains documentation for the Aster standard library.

The Aster Standard Library (stdlib) is a layered, minimal, but complete standard library that provides essential primitives while preserving Aster's predictability and performance guarantees.

## Implementation

The full stdlib implementation is located in `/aster/stdlib/` and consists of 12 layered modules.

See [/aster/stdlib/README.md](/aster/stdlib/README.md) for complete documentation.

## Modules

The stdlib is organized into layers, where each layer may only depend on lower layers:

1. **core** — Primitives, no alloc, no IO (bool, char, integers, floats, Option, Result, traits)
2. **alloc** — Heap allocation (Vec, String, Box)
3. **sync** — Concurrency primitives (Mutex, RwLock, Atomics)
4. **io** — Input/output operations (Read, Write traits, standard streams)
5. **fs** — Filesystem paths and operations
6. **net** — TCP/UDP networking
7. **time** — Duration, Instant, SystemTime
8. **fmt** — Formatting and printing
9. **math** — Mathematical functions
10. **testing** — Test framework
11. **env** — Environment variables and arguments
12. **process** — Process spawning and control

## Design Principles

- **No hidden allocation** — All heap allocation explicit via `@alloc` effect
- **No hidden I/O** — All I/O marked with `@io` effect  
- **No global state** — All state is explicit
- **Deterministic behavior** — Predictable, reproducible execution
- **Zero-cost abstractions** — No runtime overhead for unused features
- **Layered architecture** — Clear dependency hierarchy

## Stability Tiers

- **@stable** — Guaranteed stable in v1, will not break
- **@experimental** — May change in minor versions
- **@unstable** — May change or be removed at any time

## Effect System

All stdlib APIs declare their effects:
- No annotation — Pure function (no allocation, no I/O)
- `@alloc` — May allocate heap memory
- `@io` — Performs I/O operations
- `@unsafe` — Unsafe operation (raw pointers, FFI)

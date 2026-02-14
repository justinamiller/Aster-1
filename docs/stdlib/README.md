# Aster Standard Library Documentation

This directory contains comprehensive documentation for the Aster Standard Library.

## Documents

- [DESIGN.md](DESIGN.md) - Complete design document and architecture
- [QUICKREF.md](QUICKREF.md) - Quick reference guide for common patterns
- [../../stdlib/README.md](../../stdlib/README.md) - User-facing stdlib documentation

## Modules Overview

The Aster Standard Library is organized into 12 layered modules:

1. **core** — Core types and traits (no alloc, no io)
2. **alloc** — Heap allocation and collections (Vec, String)
3. **sync** — Concurrency primitives (Mutex, atomics)
4. **io** — Input/output operations (Read, Write, stdio)
5. **fs** — Filesystem operations (File, Path)
6. **net** — Network sockets (TCP, UDP)
7. **time** — Time and duration (Instant, Duration)
8. **fmt** — Formatting and Display trait
9. **math** — Mathematical functions
10. **testing** — Test framework
11. **env** — Environment variables and args
12. **process** — Process management (spawn, exit)

## Key Features

- **Layered Architecture** - Each module only depends on lower layers
- **Explicit Effects** - All IO, allocation, and unsafe operations are clearly marked
- **Stability Tiers** - @stable, @experimental, @unstable annotations
- **Zero-Cost Abstractions** - No runtime overhead for unused features
- **Memory Safety** - Ownership and borrowing prevent use-after-free and data races

## Getting Started

See the [Quick Reference](QUICKREF.md) for common patterns and idioms.

See the main [stdlib README](../../stdlib/README.md) for usage examples.

See the [Design Document](DESIGN.md) for architecture details.

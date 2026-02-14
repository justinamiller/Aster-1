# Aster Example Programs

This directory contains example programs demonstrating various features of the Aster compiler's advanced type system and standard library.

## Basic Examples

- `simple_hello.ast` - A simple "Hello, World!" program
- `type_inference_success.ast` - Demonstrates type inference with generics

## Standard Library Examples

The following examples demonstrate the Aster Standard Library:

### Core Functionality
- `stdlib_hello.ast` - Basic printing with the fmt module
- `stdlib_option_result.ast` - Working with Option and Result types for error handling
- `stdlib_collections.ast` - Using Vec and String from the alloc module

### Advanced Features
- `stdlib_math.ast` - Mathematical operations and constants
- `stdlib_env.ast` - Environment variables and command-line arguments
- `stdlib_complete.ast` - Comprehensive example integrating multiple stdlib modules

## Success Cases

- **type_inference_success.ast**: Demonstrates Hindley-Milner type inference
- **pattern_exhaustive_success.ast**: Exhaustive pattern matching
- **effects_io_success.ast**: Effect system tracking IO operations  
- **borrow_success.ast**: Successful borrowing

## Failure Cases

These examples should produce compilation errors demonstrating the compiler's error detection:

- **type_mismatch_fail.ast**: Type unification failure (E0310)
- **use_after_move_fail.ast**: Use of moved value
- **pattern_nonexhaustive_fail.ast**: Non-exhaustive pattern match (E0341)
- **borrow_conflict_fail.ast**: Conflicting borrows

## Running Examples

```bash
# Check a program (type-check only)
dotnet run --project src/Aster.CLI -- check examples/type_inference_success.ast

# Compile a program to LLVM IR
dotnet run --project src/Aster.CLI -- emit-llvm examples/type_inference_success.ast

# Build an example
dotnet run --project src/Aster.CLI -- build examples/stdlib_hello.ast
```

## Standard Library Documentation

For complete stdlib documentation, see:
- `/aster/stdlib/README.md` - Complete stdlib documentation
- `/docs/stdlib/README.md` - Additional stdlib information

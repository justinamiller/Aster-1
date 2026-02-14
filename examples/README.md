# Aster Example Programs

This directory contains example programs demonstrating various features of the Aster compiler's advanced type system.

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
```

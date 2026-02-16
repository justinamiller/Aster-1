# Core-0 Test Fixtures Index

## Overview

Test fixtures for **Aster Core-0** language subset, used for differential testing between aster0 (seed compiler) and aster1 (Stage 1 compiler).

## Fixture Categories

### Compile-Pass (✅ Should Compile Successfully)

| Fixture | Tests | Description |
|---------|-------|-------------|
| `simple_struct.ast` | Structs | Basic struct definition and field access |
| `basic_enum.ast` | Enums, Match | Enum variants and pattern matching |
| `simple_function.ast` | Functions | Function definitions and calls |
| `control_flow.ast` | If/While/For | Control flow structures |
| `vec_operations.ast` | Vec | Dynamic array operations |

**Total**: 5 fixtures

### Compile-Fail (❌ Should Fail to Compile)

| Fixture | Error Code | Tests | Description |
|---------|------------|-------|-------------|
| `undefined_variable.ast` | E0001 | Name Resolution | Undefined variable usage |
| `type_mismatch.ast` | E0303 | Type Checking | Type mismatch in function call |
| `use_of_moved_value.ast` | E0400 | Ownership | Use after move error |
| `no_traits_in_core0.ast` | E9000 | Feature Gate | Trait definition forbidden in Core-0 |

**Total**: 4 fixtures

### Run-Pass (▶️ Should Compile and Run)

| Fixture | Expected Output | Tests | Description |
|---------|-----------------|-------|-------------|
| `hello_world.ast` | `"Hello, World!"` | Basic I/O | Simple print statement |
| `fibonacci.ast` | `"0\n1\n1\n2\n3\n5\n8\n13\n21\n34\n"` | Recursion | Fibonacci sequence |

**Total**: 2 fixtures

**Note**: `sum_array.ast` has been moved to `fixtures/core1/` as it uses non-Core-0 constructs (`&Vec<T>`, `Vec::new`, `.push()`) that require Core-1 language features.

## Core-0 Language Features Tested

✅ **Covered**:
- [x] Struct definitions and usage
- [x] Enum definitions and pattern matching
- [x] Function definitions and calls
- [x] Control flow (if/while/for/match)
- [x] Vec operations (push, indexing, len)
- [x] Primitive types (i32, String)
- [x] Variable bindings (let, let mut)
- [x] References (&, &mut)
- [x] Basic operators (+, -, *, /, <, >, ==)
- [x] Error detection (undefined var, type mismatch, move errors)
- [x] Feature gates (reject traits in Core-0)

❌ **Not Covered Yet** (but allowed in Core-0):
- [ ] String operations (beyond basic usage)
- [ ] Box<T> usage
- [ ] Result<T, E> and Option<T> (beyond enum example)
- [ ] Complex pattern matching (guards, ranges)
- [ ] Array literals and slicing
- [ ] Module system (use, pub)
- [ ] Destructuring (tuples, structs)

## Adding New Fixtures

### Template for Compile-Pass

```rust
// Test fixture: <name>.ast
// Category: compile-pass
// Tests: <feature being tested>
//
// Description of what this fixture demonstrates

// Code here
```

### Template for Compile-Fail

```rust
// Test fixture: <name>.ast
// Category: compile-fail
// Tests: <error detection>
// Expected error: <error code> - <description>

// Code that should fail here
```

### Template for Run-Pass

```rust
// Test fixture: <name>.ast
// Category: run-pass
// Tests: <runtime behavior>
// Expected stdout: "<expected output>"

// Code here
```

## Running Fixtures

### With Seed Compiler (aster0)

```bash
# Compile only
dotnet run --project src/Aster.CLI -- check fixtures/core0/compile-pass/simple_struct.ast

# Compile and run
dotnet run --project src/Aster.CLI -- build fixtures/core0/run-pass/hello_world.ast -o /tmp/hello
/tmp/hello
```

### With Stage 1 Compiler (aster1)

```bash
# Once aster1 is built
aster1 check fixtures/core0/compile-pass/simple_struct.ast
aster1 build fixtures/core0/run-pass/hello_world.ast -o /tmp/hello
/tmp/hello
```

### Differential Testing

```bash
# Run verification script
./bootstrap/scripts/verify.sh --stage 1

# Manually compare specific fixture
aster0 compile --emit-tokens simple_struct.ast > /tmp/aster0.json
aster1 compile --emit-tokens simple_struct.ast > /tmp/aster1.json
diff /tmp/aster0.json /tmp/aster1.json
```

## Fixture Requirements

All fixtures must:
- ✅ Use **only** Core-0 features (no traits, async, macros)
- ✅ Be **self-contained** (no external dependencies)
- ✅ Be **minimal** (test one thing at a time)
- ✅ Have **clear comments** (purpose and expected behavior)
- ✅ Use **canonical paths** in error messages (`/source/` prefix)

## Golden Files

Expected outputs are stored in `/bootstrap/goldens/core0/`:

```
goldens/core0/
├── compile-pass/
│   ├── tokens/simple_struct.json          # Token stream
│   ├── ast/simple_struct.sexp             # AST dump
│   └── symbols/simple_struct.json         # Symbol table
├── compile-fail/
│   └── errors/undefined_variable.txt      # Error message
└── run-pass/
    ├── stdout/hello_world.txt             # stdout
    └── stderr/hello_world.txt             # stderr
```

## Verification Matrix

| Fixture | Lexer | Parser | Symbols | Types | Runtime |
|---------|-------|--------|---------|-------|---------|
| simple_struct | ✅ | ✅ | ✅ | ✅ | - |
| basic_enum | ✅ | ✅ | ✅ | ✅ | - |
| simple_function | ✅ | ✅ | ✅ | ✅ | - |
| control_flow | ✅ | ✅ | ✅ | ✅ | - |
| vec_operations | ✅ | ✅ | ✅ | ✅ | - |
| undefined_variable | ✅ | ✅ | ✅ | ❌ | - |
| type_mismatch | ✅ | ✅ | ✅ | ❌ | - |
| use_of_moved_value | ✅ | ✅ | ✅ | ✅ | ❌ |
| no_traits_in_core0 | ✅ | ❌ | - | - | - |
| hello_world | ✅ | ✅ | ✅ | ✅ | ✅ |
| fibonacci | ✅ | ✅ | ✅ | ✅ | ✅ |

Legend:
- ✅ Expected to pass
- ❌ Expected to fail (with specific error)
- `-` Not applicable

**Note**: `sum_array` has been moved to Core-1 fixtures as it requires non-Core-0 features.

## TODO

- [ ] Add more Core-0 compile-pass fixtures (Box, Result, Option)
- [ ] Add more compile-fail fixtures (borrow errors, etc.)
- [ ] Generate golden files from aster0
- [ ] Add module system fixtures (use, pub)
- [ ] Add string manipulation fixtures
- [ ] Add array/slice fixtures

## References

- Core-0 Specification: `/bootstrap/spec/aster-core-subsets.md`
- Golden Files: `/bootstrap/goldens/core0/`
- Verification Script: `/bootstrap/scripts/verify.sh`
- Bootstrap Guide: `/bootstrap/README.md`

---

**Total Fixtures**: 11 (5 compile-pass, 4 compile-fail, 2 run-pass)  
**Coverage**: Basic Core-0 features (structs, enums, functions, control flow)  
**Status**: Ready for differential testing once aster1 is implemented

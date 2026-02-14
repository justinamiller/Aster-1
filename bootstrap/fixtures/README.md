# Bootstrap Test Fixtures

## Overview

This directory contains **test fixtures** used for differential testing during the bootstrap process.

## Purpose

Fixtures are used to:
1. **Compare outputs** between different compiler stages (aster0 vs aster1 vs aster2 vs aster3)
2. **Verify correctness** of each bootstrap stage
3. **Detect regressions** when porting components

## Directory Structure

```
fixtures/
├── core0/          # Core-0 language subset fixtures
│   ├── compile-pass/   # Programs that should compile successfully
│   ├── compile-fail/   # Programs that should fail to compile
│   └── run-pass/       # Programs that should run successfully
├── core1/          # Core-1 language subset fixtures
│   ├── compile-pass/
│   ├── compile-fail/
│   └── run-pass/
├── core2/          # Core-2 (full language) fixtures
│   ├── compile-pass/
│   ├── compile-fail/
│   └── run-pass/
└── README.md       # This file
```

## Fixture Categories

### compile-pass
Programs that should **compile successfully**:
- Valid syntax
- Correct type checking
- No errors

Used to test: Lexer, parser, type checker, code generation

### compile-fail
Programs that should **fail to compile** with specific error codes:
- Syntax errors
- Type errors
- Borrow check errors

Used to test: Error detection and diagnostic quality

### run-pass
Programs that compile and **run successfully**:
- Includes expected stdout/stderr
- Used for end-to-end testing
- Verifies runtime behavior

## Creating Fixtures

### compile-pass Example

**File**: `fixtures/core0/compile-pass/simple_struct.ast`

```rust
struct Point {
    x: i32,
    y: i32
}

fn main() {
    let p = Point { x: 10, y: 20 };
    print(p.x);
}
```

### compile-fail Example

**File**: `fixtures/core0/compile-fail/undefined_variable.ast`

```rust
fn main() {
    print(x);  // error[E0001]: undefined variable `x`
}
```

**Expected**: `goldens/core0/compile-fail/undefined_variable.errors`

```
error[E0001]: undefined variable `x`
  --> /source/fixtures/core0/compile-fail/undefined_variable.ast:2:11
   |
2  |     print(x);
   |           ^ not found in this scope
```

### run-pass Example

**File**: `fixtures/core0/run-pass/hello.ast`

```rust
fn main() {
    print("hello world");
}
```

**Expected**: `goldens/core0/run-pass/hello.stdout`

```
hello world
```

## Differential Testing Process

1. **Compile with aster0** (seed compiler):
   ```bash
   aster0 build fixtures/core0/compile-pass/simple_struct.ast --emit-ast
   ```

2. **Compile with aster1** (stage 1 compiler):
   ```bash
   aster1 build fixtures/core0/compile-pass/simple_struct.ast --emit-ast
   ```

3. **Compare ASTs**:
   ```bash
   diff aster0_output.ast aster1_output.ast
   ```

4. **Verify**: If outputs match (or are semantically equivalent), test passes.

## Fixture Requirements

Each fixture should:
- ✅ Be **minimal** - test one thing at a time
- ✅ Be **self-contained** - no external dependencies
- ✅ Have **clear intent** - obvious what is being tested
- ✅ Use **canonical paths** - `/source/` prefix for reproducibility
- ✅ Be **well-commented** - explain what is being tested

## Adding New Fixtures

1. Create the fixture file in appropriate category
2. Run it through the seed compiler (aster0)
3. Capture the expected output in `/bootstrap/goldens/`
4. Add to the test harness
5. Verify differential tests pass

## Naming Conventions

- Use snake_case for filenames
- Be descriptive: `generic_function_with_trait_bound.ast`
- Not: `test1.ast`

## Future Work

As bootstrap progresses:
- [ ] Add Core-0 fixtures (Stage 1)
- [ ] Add Core-1 fixtures (Stage 2)
- [ ] Add Core-2 fixtures (Stage 3)
- [ ] Add performance regression fixtures
- [ ] Add edge case fixtures

## See Also

- `/bootstrap/goldens/README.md` - Expected outputs
- `/bootstrap/spec/bootstrap-stages.md` - Testing strategy
- `/bootstrap/scripts/verify.sh` - Verification script

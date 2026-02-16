# Core-1 Test Fixtures Index

## Overview

Test fixtures for **Aster Core-1** language subset. Core-1 extends Core-0 with generics, traits, and ownership features.

These fixtures are not yet used in Stage 1 differential testing as Stage 1 operates under Core-0 constraints.

## Fixture Categories

### Run-Pass (▶️ Should Compile and Run)

| Fixture | Expected Output | Tests | Description |
|---------|-----------------|-------|-------------|
| `sum_array.ast` | `"15"` | Vec Iteration, References | Sum elements in Vec using `&Vec<T>`, `Vec::new()`, `.push()` |

**Total**: 1 fixture

## Core-1 Language Features Tested

✅ **Covered**:
- [x] Generic types (`Vec<T>`)
- [x] References (`&Vec<T>`)
- [x] Associated functions (`Vec::new()`)
- [x] Method calls (`.push()`, `.len()`)

## Notes

- These fixtures require Core-1 language features not available in Core-0/Stage 1
- They will be used for differential testing in Stage 2 when Core-1 support is implemented
- `sum_array.ast` was moved from Core-0 fixtures as it uses non-Core-0 constructs

## References

- Core-1 Specification: `/bootstrap/spec/aster-core-subsets.md`
- Core-0 Fixtures: `/bootstrap/fixtures/core0/`
- Bootstrap Guide: `/bootstrap/README.md`

---

**Total Fixtures**: 1 (0 compile-pass, 0 compile-fail, 1 run-pass)  
**Status**: Reserved for Stage 2 differential testing

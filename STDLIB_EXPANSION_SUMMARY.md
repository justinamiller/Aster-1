# Stdlib Expansion: Arrays, Collections, and Loops

This document summarizes the expansion of the Aster standard library with support for arrays, collections, and enhanced loop constructs.

## Summary of Changes

### 1. Language Features

#### New Loop Constructs
- **`loop`** - Infinite loop construct
  ```rust
  loop {
      if condition {
          break;
      }
  }
  ```

- **`do-while`** - Execute-first loop
  ```rust
  do {
      // code
  } while condition;
  ```

- Existing `while` and `for` loops remain unchanged
- All loops support `break` and `continue` statements

#### Array Literal Syntax
- Arrays can now be created using bracket notation:
  ```rust
  let numbers = [1, 2, 3, 4, 5];
  let empty = [];
  let matrix = [[1, 2], [3, 4]];
  ```
- Array literals are converted to `Vec<T>` at compile time
- Supports nested arrays and mixed types (within type constraints)

#### Range Expressions
- **Exclusive ranges** (`start..end`): `0..10` produces 0-9
- **Inclusive ranges** (`start..=end`): `0..=10` produces 0-10
- Ranges implement `Iterator` and can be used in `for` loops

### 2. Collections Module

New module at layer 3 of the stdlib providing:

#### HashMap<K, V>
- Hash table with key-value pairs
- Separate chaining for collision resolution
- Functions: `new_hashmap()`, `hashmap_insert()`, `hashmap_get()`, `hashmap_remove()`, etc.
- Requires keys to implement `Hash` and `Eq` traits

#### HashSet<T>
- Set of unique values using HashMap internally
- Functions: `new_hashset()`, `hashset_insert()`, `hashset_contains()`, `hashset_remove()`, etc.
- Set operations: `union()`, `intersection()`, `difference()` (simplified implementations)

#### LinkedList<T>
- Doubly-linked list with O(1) insertion/removal at ends
- Functions: `new_list()`, `list_push_front()`, `list_push_back()`, `list_pop_front()`, etc.
- Implements `Drop` for automatic cleanup

### 3. Core Module Enhancements

#### Range Types
- `Range<T>` - Exclusive range type
- `RangeInclusive<T>` - Inclusive range type
- Both implement `Iterator` trait for use in loops
- Helper functions: `range()`, `range_inclusive()`, `range_contains()`

### 4. Compiler Changes

#### Lexer
- Added `Loop` token kind
- Added `Do` token kind
- Updated keyword table with "loop" and "do"

#### AST
- Added `LoopStmtNode` for infinite loops
- Added `DoWhileStmtNode` for do-while loops
- Added `ArrayLiteralExprNode` for array literals
- Updated `IAstVisitor` interface with new visitor methods

#### Parser
- Implemented `ParseLoopStmt()` method
- Implemented `ParseDoWhileStmt()` method
- Added array literal parsing in `ParsePrimaryExpression()`
- Updated block statement parsing to handle new loop types

### 5. Examples

Created three comprehensive example files:

1. **loop_examples.ast** - Demonstrates all loop constructs
   - While loops
   - For loops with ranges
   - Infinite loops with break
   - Do-while loops
   - Nested loops
   - Continue statement usage

2. **collections_examples.ast** - Shows collections usage
   - HashMap operations
   - HashSet operations
   - LinkedList operations

3. **array_examples.ast** - Array literal usage
   - Creating arrays
   - Nested arrays
   - Array operations
   - Multi-dimensional arrays

### 6. Tests

Added conformance tests in `tests/conformance/compile-pass/`:
- `loop_statement.ast` - Tests infinite loop construct
- `do_while_statement.ast` - Tests do-while loops
- `array_literal.ast` - Tests array literal syntax
- `loop_control.ast` - Tests break and continue
- `nested_loops.ast` - Tests nested loop constructs

### 7. Documentation

Updated documentation:
- **stdlib/README.md** - Added Collections module documentation, loop constructs, array literals, and range expressions
- **README.md** - Updated to mention 13 modules instead of 12, added new features to highlights
- **stdlib/lib.ast** - Added collections module export and updated layer count

## Architecture Impact

### Layering
The stdlib now has 13 layers instead of 12:
1. core
2. alloc
3. **collections** (NEW)
4. sync (was 3)
5. io (was 4)
6. fs (was 5)
7. net (was 6)
8. time (was 7)
9. fmt (was 8)
10. math (was 9)
11. testing (was 10)
12. env (was 11)
13. process (was 12)

### Compilation
- All changes are backward compatible
- Build completes successfully with no errors or warnings
- Parser handles new syntax without breaking existing code
- AST visitors need to be updated to handle new node types (deferred)

## Implementation Status

### Completed ✓
- [x] Loop keyword and AST nodes
- [x] Do-while keyword and AST nodes
- [x] Array literal parsing
- [x] Collections module (HashMap, HashSet, LinkedList)
- [x] Range types in core module
- [x] Example files
- [x] Conformance tests
- [x] Documentation updates

### Deferred
- [ ] Type checker integration for new constructs
- [ ] Name resolution for collections module
- [ ] Formatter updates for new syntax
- [ ] Full visitor pattern updates
- [ ] Runtime integration
- [ ] Advanced collection operations (iteration, advanced set operations)

## Notes

The implementations of HashMap, HashSet, and LinkedList are simplified and marked as `@experimental`. They demonstrate the API design but would need refinement for:
- Proper ownership and borrowing in linked structures
- Efficient resizing strategies
- Iterator implementation for all collections
- Thread safety (if needed)
- Performance optimizations

The core language support (AST, parser, lexer) is complete and functional. The collections and range types provide a foundation for further development.

## Next Steps

For production readiness:
1. Implement visitor methods in all AST visitors
2. Add type checking for array literals
3. Implement proper ownership handling in collections
4. Add iterators for all collection types
5. Performance testing and optimization
6. Additional collection types (BTreeMap, BTreeSet, VecDeque)
7. Integration with the borrow checker

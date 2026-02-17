# Stage 1 Completion Roadmap

## Executive Summary

**Current State**: Stage 1 is 20-60% complete. The parser and lexer foundations exist, but critical components (type checker, IR generator, code generator) are missing.

**Goal**: Complete Stage 1 to create a working minimal compiler that can compile Core-0 Aster programs and eventually compile itself.

**Timeline**: 9-12 weeks of focused development

## Understanding the Current Situation

### What Works ✅
- **Parser**: 100% complete - can parse all Core-0 syntax
- **Lexer**: 80% complete - tokenizes most constructs
- **AST**: Complete data structures defined
- **CLI**: Basic command structure exists
- **Infrastructure**: Bootstrap scripts, test fixtures, documentation

### What's Missing ❌
- **Type Checker**: Only skeleton exists (~0% functional)
- **IR Generator**: Only skeleton exists (~0% functional)
- **Code Generator**: Only skeleton exists (~0% functional)
- **File I/O**: No file reading/writing yet
- **JSON Output**: No serialization for emit commands

### Why Stage 3 is a Stub

Stage 3 (aster3) is currently just a **shell script wrapper** that calls Stage 1, because:
1. Stage 1 isn't complete yet (can't compile anything)
2. Stage 2 doesn't exist yet (needs Stage 1 to compile it)
3. Stage 3 doesn't exist yet (needs Stage 2 to compile it)

**The stub exists to test infrastructure**, not to provide actual self-hosting.

## The Path to True Self-Hosting

```
Current State:
  Stage 0 (C#) ✅ → Stage 1 (20% complete) → Stage 2 (not started) → Stage 3 (not started)
                         ↓
                    Can't compile anything yet

Target State:
  Stage 0 (C#) ✅ → Stage 1 (complete) ✅ → Stage 2 (implemented) ✅ → Stage 3 (implemented) ✅
       ↓                    ↓                        ↓                          ↓
  Compiles               Compiles                Compiles                  Compiles
   Stage 1               Stage 2                 Stage 3                   itself!
                                                                        (aster3 == aster3')
```

## Stage 1 Completion Requirements

### 1. Complete the Lexer (1 week, LOW priority)

**Current**: 80% complete
**Missing**: 
- Some edge cases in number parsing
- Full escape sequence handling in strings
- Robust error recovery

**Files**: `aster/compiler/frontend/lexer.ast`

**Implementation**:
```rust
// Add missing token recognition
fn lex_number() -> Token { /* handle all cases */ }
fn lex_string() -> String { /* handle all escapes */ }
fn recover_from_error() { /* better error handling */ }
```

### 2. Implement File I/O (1 week, MEDIUM priority)

**Current**: Not implemented
**Needed**:
- Read source files from disk
- Write output files (tokens, AST, IR, code)
- Handle file errors gracefully

**Files**: New file `aster/compiler/io.ast`

**Implementation**:
```rust
fn read_file(path: String) -> String { /* read file */ }
fn write_file(path: String, content: String) { /* write file */ }
fn file_exists(path: String) -> bool { /* check existence */ }
```

### 3. Implement JSON Serialization (1 week, MEDIUM priority)

**Current**: Not implemented
**Needed**:
- Serialize tokens to JSON
- Serialize AST to JSON
- Serialize symbols to JSON
- For `--emit-tokens`, `--emit-ast`, `--emit-symbols` commands

**Files**: New file `aster/compiler/json.ast`

**Implementation**:
```rust
fn token_to_json(token: Token) -> String { /* JSON format */ }
fn ast_to_json(node: AstNode) -> String { /* JSON format */ }
fn serialize_diagnostics(errors: Vec<Error>) -> String { /* JSON */ }
```

### 4. Implement Type Checker (2-3 weeks, HIGH priority)

**Current**: Skeleton only (~0% functional)
**Needed**:
- Symbol table construction
- Name resolution (variables, functions, types)
- Type inference for Core-0
- Basic type checking (assignment compatibility)
- Error reporting

**Files**: `aster/compiler/typecheck.ast`

**Key Functions**:
```rust
fn build_symbol_table(ast: Program) -> SymbolTable { /* ... */ }
fn resolve_names(ast: Program, symbols: SymbolTable) -> Program { /* ... */ }
fn infer_types(ast: Program, symbols: SymbolTable) -> Program { /* ... */ }
fn check_types(ast: Program, symbols: SymbolTable) -> Vec<Error> { /* ... */ }
```

**Complexity**: This is the most complex component
- Must handle scoping rules
- Must track type information
- Must produce good error messages
- Critical for any further progress

### 5. Implement IR Generator (2 weeks, HIGH priority)

**Current**: Skeleton only (~0% functional)
**Needed**:
- Convert AST to MIR (Mid-level IR)
- Handle expressions, statements, declarations
- Generate basic blocks and control flow
- Prepare for LLVM codegen

**Files**: `aster/compiler/ir.ast`

**Key Functions**:
```rust
fn generate_mir(ast: Program) -> MirProgram { /* ... */ }
fn lower_expression(expr: Expr) -> MirExpr { /* ... */ }
fn lower_statement(stmt: Stmt) -> MirStmt { /* ... */ }
fn build_cfg(function: Function) -> ControlFlowGraph { /* ... */ }
```

**Dependencies**: Requires complete type checker

### 6. Implement Code Generator (2-3 weeks, HIGH priority)

**Current**: Skeleton only (~0% functional)
**Needed**:
- Emit LLVM IR from MIR
- Handle function compilation
- Handle control flow (if, while, loops)
- Handle function calls
- Handle struct operations

**Files**: `aster/compiler/codegen.ast`

**Key Functions**:
```rust
fn emit_llvm_ir(mir: MirProgram) -> String { /* LLVM IR text */ }
fn emit_function(func: MirFunction) -> String { /* LLVM function */ }
fn emit_expression(expr: MirExpr) -> String { /* LLVM instructions */ }
```

**Dependencies**: Requires IR generator

### 7. Integration & End-to-End Testing (1-2 weeks)

**Needed**:
- Wire all components together in driver
- Test on all Core-0 fixtures
- Verify output correctness
- Compare with Stage 0 (differential testing)
- Fix bugs and edge cases

## Implementation Strategy

### Phase 1: Quick Wins (2 weeks)
1. Complete lexer (1 week)
2. Implement File I/O + JSON (1 week)

**Result**: Can read files, parse them, emit tokens/AST as JSON

### Phase 2: Type System (2-3 weeks)
3. Implement type checker

**Result**: Can validate programs, catch type errors

### Phase 3: Code Generation (4 weeks)
4. Implement IR generator (2 weeks)
5. Implement code generator (2 weeks)

**Result**: Can produce executables!

### Phase 4: Integration (1-2 weeks)
6. Wire everything together
7. Test and debug
8. Verify against Stage 0

**Result**: Working Stage 1 compiler!

## Testing Milestones

### Milestone 1: Token Emission
```bash
aster1 --emit-tokens hello.ast
# Outputs: hello.tokens.json
```

### Milestone 2: AST Emission
```bash
aster1 --emit-ast hello.ast
# Outputs: hello.ast.json
```

### Milestone 3: Type Checking
```bash
aster1 --type-check hello.ast
# Outputs: Type check passed / errors
```

### Milestone 4: Compilation
```bash
aster1 --emit-llvm hello.ast
# Outputs: hello.ll (LLVM IR)
```

### Milestone 5: Execution
```bash
aster1 build hello.ast
# Outputs: hello (executable)
./hello
# Runs the program!
```

### Milestone 6: Self-Hosting Test
```bash
aster1 build aster/compiler/main.ast -o aster1_new
# Compiles itself!
```

## What Happens After Stage 1 is Complete

### Stage 2 Implementation (3-4 months)
Once Stage 1 works:
1. Write Stage 2 compiler in expanded Aster (Core-1)
2. Use Stage 1 to compile Stage 2
3. Stage 2 adds: traits, methods, generics, more type system features

### Stage 3 Implementation (4-6 months)
Once Stage 2 works:
1. Write Stage 3 compiler in full Aster (Core-2)
2. Use Stage 2 to compile Stage 3
3. Stage 3 adds: borrow checker, optimizations, full language

### True Self-Hosting Verification
Once Stage 3 works:
1. Use aster3 to compile itself → produces aster3'
2. Verify aster3 == aster3' (binary equivalence or semantic equivalence)
3. This proves true self-hosting!

## Recommendations

### If You Want to Complete Stage 1 Yourself

1. **Start with File I/O and JSON** (easiest, gives immediate feedback)
2. **Then tackle Type Checker** (hardest but most critical)
3. **Then IR and CodeGen** (straightforward once types work)
4. **Use differential testing** throughout (compare against Stage 0)

### If You Want Help

I can:
1. **Implement specific components** (e.g., "implement the type checker")
2. **Create detailed guides** for each component
3. **Review and improve** existing partial implementations
4. **Set up testing infrastructure** to validate each component

### If You Want to Understand Requirements Better

I can:
1. **Document detailed specifications** for each component
2. **Create interface definitions** showing what each function should do
3. **Provide examples** from the C# compiler (Stage 0) to reference
4. **Explain Core-0 language semantics** in detail

## Current Blocker Resolution

> "Self-hosting is still stub-mode (infra ready, not true aster3 equivalence yet)"

**Why it's a stub**: Stage 1, 2, and 3 aren't implemented yet

**What's needed**: Complete the implementation chain (Stage 1 → 2 → 3)

**Time to true self-hosting**: 9-18 months total
- Stage 1: 2-3 months
- Stage 2: 3-4 months
- Stage 3: 4-6 months
- Testing & refinement: ongoing

**Infrastructure is ready**: ✅ Bootstrap scripts, testing framework, documentation

**Implementation is not**: ❌ Actual compiler components need to be written

## Next Steps - Your Choice

**Option A**: I implement the missing components (2-3 months work)
**Option B**: I create detailed implementation guides for you
**Option C**: I document requirements and you implement
**Option D**: We work together incrementally

What would you like me to focus on?

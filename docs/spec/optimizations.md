# Compiler Optimizations

**Status**: Phase 5 Complete ✅  
**Last Updated**: 2026-02-21

## Overview

The Aster compiler applies a series of optimization passes to the MIR (Mid-level Intermediate
Representation) before emitting LLVM IR.  All passes are optional, sound, and preserve the
observable semantics of the program.

## Pass Order (Phase 6b)

```
MIR Lowering
    ↓
1. Constant Folding        (Phase 3)
2. Dead Code Elimination   (Phase 3)
3. Common Subexpression Elimination  (Phase 4)
4. Loop Invariant Code Motion        (Phase 5)
5. Function Inlining                 (Phase 5)
6. Scalar Replacement of Aggregates  (Phase 5)
    ↓
Pattern Lowering
Drop Lowering
Async Lowering
Borrow Checking
    ↓
LLVM IR Emission
```

## Pass Descriptions

### 1. Constant Folding (`ConstantFolder`)

Evaluates binary and unary operations on compile-time constant operands.

**Before:**
```mir
%t0 = BinaryOp add | c:3 | c:5
%t1 = BinaryOp mul | c:2 | c:4
```

**After:**
```mir
%t0 = Assign c:8
%t1 = Assign c:8
```

Supported operations: `add`, `sub`, `mul`, `div`, `rem`, `eq`, `ne`, `lt`, `le`, `gt`, `ge`,
`and`, `or`, `xor`, `neg`, `not` on integer, floating-point, and boolean constants.

---

### 2. Dead Code Elimination (`DeadCodeEliminator`)

Removes unreachable basic blocks (blocks with no predecessors other than themselves) and
pure temporary assignments whose result is never subsequently read.

**Unreachable block removal:** Performs a reachability analysis from the entry block;
any block not reachable from the entry is dropped.

**Dead assignment removal:** Within each block, pure assignments (Assign, BinaryOp,
UnaryOp, Literal) whose destination is never used as an operand in a later instruction
in the same block or as a function call argument are removed.

---

### 3. Common Subexpression Elimination (`CsePass`)

Within each basic block, replaces redundant computations of the same pure expression
with a copy of the first result.

**Before:**
```mir
%a = BinaryOp add | v:x | v:y
%b = BinaryOp add | v:x | v:y    ; duplicate
```

**After:**
```mir
%a = BinaryOp add | v:x | v:y
%b = Assign %a                    ; copy
```

CSE is local (intra-block); global (inter-block) CSE is planned for Phase 6.

---

### 4. Loop Invariant Code Motion (`LicmPass`)

Detects back-edges in the MIR control-flow graph (an unconditional branch to an earlier
block) and hoists pure computations whose operands are all defined outside the loop body
to a synthesised **pre-header** block.

**Before:**
```
entry:  (loop header)
  %inv = BinaryOp mul | c:2 | c:3    ; loop-invariant
  %sum = BinaryOp add | v:i | %inv
  br entry

```

**After:**
```
licm_preheader_0:
  %inv = BinaryOp mul | c:2 | c:3    ; hoisted
  br entry
entry:
  %sum = BinaryOp add | v:i | %inv
  br entry
```

**Safety:** Only pure operations (`BinaryOp`, `UnaryOp`) with constant or loop-external
operands are hoisted.  Store, Call, Drop, and Load instructions are not moved.

---

### 5. Function Inlining (`InliningPass`)

Inlines *small* callees (≤ 5 instructions, single basic block, non-recursive) at call sites.

```rust
fn double(x: i32) -> i32 { x + x }
fn main() { let y = double(5); }
// After inlining: let y = 5 + 5;
```

Parameters are substituted by renaming: all variables and temporaries in the inlined body
receive a unique suffix (`__inlined_double_0`, etc.) to avoid naming collisions.

The inlining threshold can be adjusted via `InliningPass.MaxInlineInstructions`.

---

### 6. Scalar Replacement of Aggregates (`SroaPass`)

When a struct variable is assigned a constant value and its fields are subsequently loaded,
the load is replaced with a direct copy of the constant:

**Before:**
```mir
%point = Assign c:0
%x = Load %point field:"x"
```

**After:**
```mir
%point = Assign c:0
%x = Assign c:0
```

This eliminates abstract field-access overhead for simple struct literals.

---

## Future Optimizations (Phase 6+)

- **Global CSE** — across basic block boundaries
- **Loop unrolling** — unroll small counted loops
- **Vectorization** — SIMD code generation via LLVM
- **Devirtualization** — resolve `dyn Trait` calls statically when possible
- **Profile-guided optimization (PGO)** — use runtime profiles
- **Link-Time Optimization (LTO)** — whole-program optimization

## References

- `src/Aster.Compiler/MiddleEnd/Optimizations/` — all optimization pass implementations
- `src/Aster.Compiler/Driver/CompilationDriver.cs` — pass ordering and wiring

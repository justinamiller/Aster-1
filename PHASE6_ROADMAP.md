# Phase 6 Roadmap — Language & Compiler Completeness

**Status**: In Progress 🔄  
**Last Updated**: 2026-02-21  
**Prerequisite**: Phase 5 complete (LICM, Inlining, SROA, proc macros)

## Overview

Phase 6 focuses on completing the type system for practical use, improving the
developer experience, and advancing toward true self-hosting.

## Feature List

### 1. Slice Types `[T]` and `[T; N]` ✅

**Timeline**: Weeks 29-30  
**What**: Fixed-size arrays and dynamically-sized slice views.

- [x] `SliceType`, `ArrayType`, `StrType` in `Types.cs`
- [x] `[T]` and `[T; N]` type annotation parsing  
- [x] Array literal `[a, b, c]` parsing and HIR
- [x] `HirArrayLiteralExpr`, `HirCastExpr` HIR nodes
- [x] NameResolver: `ResolveArrayLiteral`, `ResolveTypeAnnotation`
- [x] TypeChecker: `CheckArrayLiteralExpr`, `ResolveTypeRef` for `__slice`/`__array`/`str`
- [x] MirLowering: `LowerArrayLiteral` (Alloca + Store sequence)
- [x] Spec: `docs/spec/slices.md`

---

### 2. `as` Cast Expressions ✅

**Timeline**: Weeks 31-32  
**What**: Explicit numeric and pointer casts.

- [x] `As` token in `TokenKind`, `"as"` keyword in lexer
- [x] `CastExprNode` AST node
- [x] `VisitCastExpr` in `IAstVisitor`
- [x] `HirCastExpr` HIR node
- [x] Parser: `expr as Type` in postfix loop
- [x] NameResolver: `CastExprNode` → `HirCastExpr` via `ResolveTypeAnnotation`
- [x] TypeChecker: `CheckCastExpr` (validates numeric cast legality)
- [x] MirLowering: `LowerCastExpr` → `MirOpcode.Cast`
- [x] Spec: `docs/spec/casts.md`

---

### 3. Range Expressions ✅

**Timeline**: Weeks 33-34  
**What**: `lo..hi` and `lo..=hi` range expressions for `for` loops and slicing.

- [x] `BinaryOperator.Range` (DotDot) already in lexer + parser
- [x] TypeChecker: `BinaryOperator.Range` → `Range<T>` TypeApp
- [x] MirLowering: range lowers through BinaryOp path (loop integration)
- [x] Spec: `docs/spec/iterators.md` (range section)

---

### 4. Loop Unrolling Optimization ✅

**Timeline**: Week 35  
**What**: Unroll constant-trip-count loops to eliminate branch overhead.

- [x] `LoopUnrollPass.cs` in `MiddleEnd/Optimizations/`
- [x] Detects `__range_new(lo, hi)` calls with constant integer operands
- [x] Clones loop body `tripCount` times with renamed temporaries
- [x] Guard: only unrolls ≤ 8 iterations
- [x] Wired into `CompilationDriver` Phase 6b
- [x] Spec: `docs/spec/optimizations.md` (Phase 6 section)

---

### 5. `str` Type / String Slices ✅

**Timeline**: Week 36  
**What**: `str` as a distinct string-slice type (distinct from owned `String`).

- [x] `StrType` singleton in `Types.cs`
- [x] `"str"` recognized in `ResolveTypeRef` (TypeChecker and NameResolver)
- [x] String literals may coerce to `&str`
- [x] Spec: `docs/spec/slices.md` (str section)

---

### 6. Iterator Protocol ✅ (Spec only)

**Timeline**: Week 36  
**What**: Formal specification of the Iterator trait and for-loop desugaring.

- [x] `IntoIterator` registered as built-in trait in TypeChecker
- [x] `for x in iterable` desugaring documented
- [x] Spec: `docs/spec/iterators.md`

---

## Planned (Phase 6b — Future Work)

### 7. `usize` / `isize` as First-Class Types

- [ ] Distinct `PrimitiveKind.Usize` / `PrimitiveKind.Isize`
- [ ] Target-width-dependent: 32-bit on 32-bit targets, 64-bit on 64-bit
- [ ] Cast between usize and pointer

### 8. Tuple Types `(T1, T2, ...)`

- [ ] `TupleType` in `Types.cs`
- [ ] `(a, b, c)` expression parsing → `HirTupleExpr`
- [ ] Destructuring in `let` patterns: `let (x, y) = pair;`

### 9. Never Type `!`

- [ ] `NeverType` in `Types.cs`
- [ ] Functions that never return (`fn panic() -> !`)
- [ ] Unifies with any type in match/if/else

### 10. Closure Captures

- [ ] Identify free variables in closure body (via HIR walker)
- [ ] Capture by reference (`|x| x + y`) or by value (`move |x| x + y`)
- [ ] Generate closure struct in MIR (field per captured variable)

### 11. String Interpolation (`format!` with `{}`)

- [ ] `format!("{}", value)` → calls `Display::display`
- [ ] `{}`, `{:?}` (Debug), `{:x}` (hex) format specifiers
- [ ] Compile-time format string parsing

---

## Spec Documents

| Document | Status |
|----------|--------|
| `docs/spec/slices.md` | ✅ |
| `docs/spec/casts.md` | ✅ |
| `docs/spec/iterators.md` | ✅ |
| `docs/spec/tuples.md` | 🔜 Phase 6b |
| `docs/spec/closures-captures.md` | 🔜 Phase 6b |
| `docs/spec/format-strings.md` | 🔜 Phase 6b |

---

## Test Coverage

| Test Class | Count | Status |
|-----------|-------|--------|
| `Phase6SliceTests` | 6 | ✅ |
| `Phase6CastTests` | 7 | ✅ |
| `Phase6RangeTests` | 5 | ✅ |
| `Phase6LoopUnrollTests` | 5 | ✅ |
| `Phase6IntegrationTests` | 4 | ✅ |
| **Total Phase 6** | **27** | **✅** |

---

## Success Criteria

- ✅ Spec complete for all Phase 6 features
- ✅ All new tests passing (27 tests)
- ✅ No regressions in Phase 1-5 tests
- ✅ Build: 0 errors
- ✅ `PHASE6_ROADMAP.md` reflects accurate status

---

## References

- [PHASE4_ROADMAP.md](PHASE4_ROADMAP.md) — Phase 4 feature expansion  
- [NEXT_CODING_STEPS_FOR_SELF_HOSTING.md](NEXT_CODING_STEPS_FOR_SELF_HOSTING.md) — Self-hosting roadmap  
- [docs/spec/](docs/spec/) — Language specification  
- [SELF_HOSTING_ROADMAP.md](SELF_HOSTING_ROADMAP.md) — Long-term bootstrap plan  

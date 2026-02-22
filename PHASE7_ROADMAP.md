# Phase 7 Roadmap — Backend, Standard Library & Self-Hosting Progress

**Status**: Planned 🔜  
**Last Updated**: 2026-02-22  
**Prerequisite**: Phase 6 complete (slices, casts, ranges, tuples, never type, closure captures, format!)

---

## Overview

Phase 7 covers three major areas:
1. **Backend improvements** — LLVM IR quality, target triples, debug info
2. **Standard library expansion** — string operations, I/O, collections API
3. **Self-hosting progress** — implement key compiler stages in Aster itself

---

## Feature List

### 1. LLVM Backend Improvements ✅ Planned

**Timeline**: Weeks 37-38

#### Items
- [ ] Target triple support (`x86_64-linux-gnu`, `aarch64-apple-darwin`)
- [ ] Debug info emission (`DIFile`, `DISubprogram`, `DILocation`)
- [ ] Struct/tuple ABI lowering (pass-by-value vs pointer for large types)
- [ ] Proper `usize`/`isize` as `i64`/`i32` depending on target
- [ ] `NeverType` (`!`) → LLVM `void` + `unreachable` terminator
- [ ] Function attributes (`noreturn` for panic/todo/unreachable)

#### Spec
- [ ] `docs/spec/llvm-backend.md`

---

### 2. Diagnostics Engine Improvements ✅ Planned

**Timeline**: Weeks 39-40

#### Items
- [ ] Source-context display (show the line + caret in error messages)
- [ ] Multi-span diagnostics (primary + secondary spans)
- [ ] Suggestion system (automated fix-it hints as `--fix` patches)
- [ ] Warning levels (allow/warn/deny/forbid per lint)
- [ ] Structured JSON diagnostic output (`--error-format=json`)

#### Spec
- [ ] `docs/spec/diagnostics.md`

---

### 3. Standard Library Expansion ✅ Planned

**Timeline**: Weeks 41-43

#### Items
- [ ] `String` operations: `push_str`, `len`, `is_empty`, `starts_with`, `ends_with`, `contains`, `find`, `split`, `trim`
- [ ] `Vec<T>` iterator: `iter()`, `iter_mut()`, `into_iter()` → register in TypeChecker
- [ ] `HashMap<K,V>` iterator: `iter()`, `keys()`, `values()`
- [ ] `Option<T>` combinators: `map`, `and_then`, `unwrap_or`, `is_some`, `is_none`
- [ ] `Result<T,E>` combinators: `map`, `map_err`, `and_then`, `unwrap_or_else`, `is_ok`, `is_err`
- [ ] I/O: `File::open`, `File::create`, `BufReader`, `read_to_string`
- [ ] Math: `pow`, `log`, `floor`, `ceil`, `round`
- [ ] Trait implementations: `Display`, `Debug`, `Iterator` for stdlib types

#### Spec
- [ ] `docs/spec/stdlib.md`
- [ ] `docs/spec/iterators.md` (update with impl details)

---

### 4. Incremental Compilation ✅ Planned

**Timeline**: Weeks 44-45

#### Items
- [ ] Module-level compilation units (each `module` → separate `MirModule`)
- [ ] Content-hash based cache (skip recompilation if source unchanged)
- [ ] Dependency tracking (module A depends on module B → recompile A if B changes)
- [ ] `CompilationCache` class in `Driver/`
- [ ] CLI flag `--incremental` / `--no-cache`

---

### 5. Self-Hosting Progress ✅ Planned

**Timeline**: Weeks 46-50

This is the critical path to Aster being able to compile itself.

#### Stage 1 Completion (Core-0 compiler in Aster)

The Stage 1 compiler (`aster/compiler/`) needs these files substantially completed:

| File | Current LOC | Target LOC | Status |
|------|-------------|------------|--------|
| `frontend/lexer.ast` | ~850 | ~1000 | 🔄 85% |
| `frontend/parser.ast` | ~1581 | ~1700 | 🔄 90% |
| `resolve.ast` | ~52 | ~600 | ❌ 9% |
| `typecheck.ast` | ~101 | ~900 | ❌ 11% |
| `irgen.ast` | ~81 | ~500 | ❌ 16% |
| `codegen.ast` | ~70 | ~600 | ❌ 12% |

##### Items
- [ ] Complete `resolve.ast`: scope management, symbol table, import resolution (~600 LOC)
- [ ] Complete `typecheck.ast`: H-M inference, constraint generation, unification (~900 LOC)
- [ ] Complete `irgen.ast`: AST → HIR lowering with all statement/expression types (~500 LOC)
- [ ] Complete `codegen.ast`: HIR → LLVM IR emission for Core-0 subset (~600 LOC)
- [ ] Complete `lexer.ast`: char literals, hex/binary literals, block comments (~150 LOC)
- [ ] Wire `pipeline.ast`: connect all stages end-to-end
- [ ] Stage 1 bootstrap test: `aster3 stage1/*.ast` produces working binary

---

## Remaining Phases After Phase 7

### Phase 8: Ownership & Borrow System (Full)

- [ ] Named lifetime inference (`'a`, `'b` in structs)
- [ ] Lifetime subtyping rules
- [ ] Two-phase borrow full implementation
- [ ] Non-Lexical Lifetimes (NLL) complete pass
- [ ] Borrow splitting (field-level borrows in structs)

### Phase 9: Effect System

- [ ] `throws E` effect propagation
- [ ] Effect polymorphism
- [ ] Effect inference
- [ ] `actor` concurrency model

### Phase 10: Self-Hosting Verification

- [ ] Stage 2 compiler in Aster (Generics + Traits)
- [ ] Stage 3 compiler in Aster (Full language)
- [ ] Bootstrap convergence test (`aster3 == aster3'`)
- [ ] Performance benchmarks vs C# Stage 0

---

## Test Coverage Plan

| Test Class | Planned Count |
|-----------|---------------|
| `Phase7LlvmBackendTests` | 8 |
| `Phase7DiagnosticsTests` | 6 |
| `Phase7StdlibTests` | 12 |
| `Phase7IncrementalTests` | 4 |
| `Phase7SelfHostingTests` | 6 |
| **Total Phase 7** | **36** |

---

## Success Criteria

- [ ] LLVM backend emits valid IR for tuple and never types
- [ ] Diagnostics show source context (line + caret)
- [ ] All stdlib combinator methods type-check correctly
- [ ] Incremental compilation skips unchanged modules
- [ ] Stage 1 compiler passes basic self-compilation test
- [ ] 36+ Phase 7 tests pass; 0 regressions

---

## References

- [PHASE6_ROADMAP.md](PHASE6_ROADMAP.md) — Phase 6 complete
- [NEXT_CODING_STEPS_FOR_SELF_HOSTING.md](NEXT_CODING_STEPS_FOR_SELF_HOSTING.md) — detailed self-hosting plan
- [SELF_HOSTING_ROADMAP.md](SELF_HOSTING_ROADMAP.md) — long-term bootstrap plan
- [docs/spec/](docs/spec/) — Language specification

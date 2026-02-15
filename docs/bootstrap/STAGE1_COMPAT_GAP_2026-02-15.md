# Stage1 Core-0 Compatibility Gap (2026-02-15)

## Repro Command

```bash
export PATH="$HOME/.dotnet:$PATH" DOTNET_ROOT="$HOME/.dotnet"
./bootstrap/scripts/bootstrap.sh --clean --stage 1 --verbose
```

## Failure Point Progression

- **Before**: `aster/compiler/frontend/string_interner.ast`
- **After**: `aster/compiler/frontend/lexer.ast` (first reported around line 41, later at line 504+ during full parse)

This confirms Stage1 progression beyond the previous `string_interner.ast` blocker.

## Dominant Compatibility Gaps vs Stage0 `--stage1` Parser

1. **Reference types in signatures/params** (`&T`, `&mut T`) → `E9004`
2. **`as` casts** in expressions/types → parse errors (`E0102` in current grammar path)
3. **Higher-level constructs not accepted in current Core-0 path** (e.g., `Option<T>`-style usage in some files)

## Quick Per-File Check (stage0 check in `--stage1` mode)

- `aster/compiler/contracts/span.ast` ✅ parses
- `aster/compiler/contracts/token_kind.ast` ✅ parses
- `aster/compiler/frontend/string_interner.ast` ✅ parses (after remediation)
- `aster/compiler/contracts/diagnostics.ast` ❌ references (`E9004`)
- `aster/compiler/frontend/lexer.ast` ❌ references + `as` (`E9004`, `E0102`)
- `aster/compiler/frontend/parser.ast` ❌ references (`E9004`)
- `aster/compiler/main.ast` ❌ references + unsupported constructs
- `aster/compiler/ir/ast.ast` ❌ unresolved type usage under isolated check (`Vec`, `Span`)

## Remediation Applied in this pass

- Replaced `aster/compiler/frontend/string_interner.ast` with a strict Core-0 parser-compatible stub (no references, no casts, no method calls).
- Updated bootstrap docs to reflect current real blocker location.

## Next High-Impact Fixes

1. Convert all `frontend/lexer.ast` function signatures from reference-style params to value/Core-0-compatible forms.
2. Remove `as` casts in lexer helpers by introducing Core-0-safe conversion helpers and index wrappers.
3. Apply same signature conversion to `frontend/parser.ast` and `contracts/diagnostics.ast` to move parse frontier deeper.

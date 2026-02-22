# Type Casts (`as`)

**Status**: Phase 6 ✅  
**Spec Version**: 1.0

## Overview

The `as` operator performs explicit type conversions between compatible types.
It is a postfix expression with high precedence (higher than arithmetic but lower than method calls).

## Syntax

```aster
let x: i32 = 42;
let y: f64 = x as f64;   // i32 → f64
let z: i64 = x as i64;   // i32 → i64 (widening)
let b: u8  = x as u8;    // i32 → u8  (truncation)
```

## Precedence

`as` binds tighter than binary operators and looser than method calls / index:

```
expr.method() as T     ← method call first, then cast
a + b as i64           ← equivalent to: a + (b as i64)
```

## Allowed Casts

| From \ To | i8 | i16 | i32 | i64 | u8 | u16 | u32 | u64 | f32 | f64 | bool | *T |
|-----------|----|----|-----|-----|----|-----|-----|-----|-----|-----|------|----|
| Integer   | ✅ | ✅ | ✅  | ✅  | ✅ | ✅  | ✅  | ✅  | ✅  | ✅  | ✅   | ✅ |
| Float     | ✅ | ✅ | ✅  | ✅  | ✅ | ✅  | ✅  | ✅  | ✅  | ✅  | ❌   | ❌ |
| bool      | ✅ | ✅ | ✅  | ✅  | ✅ | ✅  | ✅  | ✅  | ❌  | ❌  | ✅   | ❌ |
| *T (ptr)  | ❌ | ❌ | ❌  | ✅  | ❌ | ❌  | ❌  | ✅  | ❌  | ❌  | ❌   | ✅ |

- **Widening** (e.g. `i32 as i64`): zero-extends or sign-extends; no data loss.
- **Truncation** (e.g. `i64 as i32`): silently wraps; high bits discarded.
- **Float ↔ Integer**: rounds toward zero; saturates at integer min/max.
- **Pointer ↔ Integer**: treated as a bitcast; unsafe if used with dereferencing.

## Disallowed Casts

These casts are rejected at compile time (E0600):

```aster
let s: String = "hello";
let n: i32 = s as i32;   // E0600: Cannot cast from 'String' to 'i32'
```

## Slice / Str Casts

```aster
let v: Vec<u8> = vec![72, 101, 108, 108, 111];
let s: &[u8] = &v as &[u8];   // reference coercion (not a cast)
```

Use reference coercions (`&T` → `&U` where `T: Unsize<U>`) rather than `as` for slice conversions.

## Semantics

At the MIR level, `expr as T` lowers to a `Cast` instruction annotated with the target type name.
The LLVM backend maps this to the appropriate LLVM instruction:

| Cast kind | LLVM instruction |
|-----------|-----------------|
| Integer widening (signed) | `sext` |
| Integer widening (unsigned) | `zext` |
| Integer truncation | `trunc` |
| Int → Float | `sitofp` / `uitofp` |
| Float → Int | `fptosi` / `fptoui` |
| Float precision change | `fpext` / `fptrunc` |
| Pointer ↔ Integer | `ptrtoint` / `inttoptr` |
| Pointer ↔ Pointer | `bitcast` |

## Error Codes

| Code | Meaning |
|------|---------|
| E0600 | Illegal cast between incompatible types |
| E0601 | Cast from reference type requires `unsafe` |

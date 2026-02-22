# LLVM Backend

**Status**: Phase 7 ✅  
**Updated**: 2026-02-22

---

## Overview

The Aster compiler uses LLVM IR (Text Format) as its final code-generation target.
The `LlvmBackend` class translates MIR (Mid-level Intermediate Representation) directly
into LLVM IR text, which can then be passed to `llc` or `clang` for machine-code emission.

---

## Target Platform Support

Each compilation targets a specific platform via `LlvmTargetInfo`:

| Enum Value       | Triple                           | Pointer Width |
|------------------|----------------------------------|---------------|
| `X86_64Linux`    | `x86_64-unknown-linux-gnu`       | 64-bit        |
| `X86_64Darwin`   | `x86_64-apple-macosx10.15.0`    | 64-bit        |
| `Aarch64Darwin`  | `aarch64-apple-macosx11.0.0`    | 64-bit        |
| `X86_64Windows`  | `x86_64-pc-windows-msvc`         | 64-bit        |

The default is `X86_64Linux`.

Every emitted module begins with:
```
target triple = "x86_64-unknown-linux-gnu"
target datalayout = "e-m:e-p270:32:32-..."
```

---

## Type Mapping

| Aster Type      | LLVM Type              | Notes                                      |
|-----------------|------------------------|--------------------------------------------|
| `i8`            | `i8`                   |                                            |
| `i16`           | `i16`                  |                                            |
| `i32`           | `i32`                  |                                            |
| `i64`           | `i64`                  |                                            |
| `u8`            | `i8`                   | Unsigned — LLVM unsigned flag on ops        |
| `u16`           | `i16`                  |                                            |
| `u32`           | `i32`                  |                                            |
| `u64`           | `i64`                  |                                            |
| `usize`/`isize` | `i64` (or `i32` for 32-bit target) | Target-width integer           |
| `f32`           | `float`                |                                            |
| `f64`           | `double`               |                                            |
| `bool`          | `i1`                   |                                            |
| `char`          | `i8`                   | Unicode scalar stored as UTF-8 byte        |
| `str` / `&str`  | `ptr`                  | Fat pointer simplified to `ptr` in IR      |
| `void`          | `void`                 |                                            |
| `!` (never)     | `void` + `unreachable` | Functions returning `!` get `noreturn`     |
| `(T1, T2, ...)`  | `{ T1, T2, ... }`     | LLVM anonymous struct                      |
| struct `Foo`    | `%struct.Foo`          | Named struct type                          |
| `[T]` / `&[T]` | `ptr`                  | Slice fat pointer; simplified to `ptr`     |

---

## Function Attributes

Functions whose return type is `!` (Never) automatically receive the `noreturn` LLVM attribute:

```llvm
attributes #0 = { noreturn }

define void @panic(ptr %msg) #0 {
entry:
  call void @exit(i32 1) #0
  unreachable
}
```

---

## Debug Information

Phase 7 emits stub DWARF debug metadata at the end of each module:

```llvm
!llvm.dbg.cu = !{!0}
!llvm.module.flags = !{!1, !2}

!0 = distinct !DICompileUnit(language: DW_LANG_C99, file: !3, producer: "Aster Compiler", ...)
!1 = !{i32 7, !"Dwarf Version", i32 4}
!2 = !{i32 2, !"Debug Info Version", i32 3}
!3 = !DIFile(filename: "module.ast", directory: ".")
```

Per-instruction `!DILocation` metadata is planned for Phase 8 (requires source spans in MIR).

---

## Built-in Runtime Stubs

The LLVM header section declares the minimal C runtime functions used by emitted code:

```llvm
declare i32 @puts(ptr)
declare i32 @printf(ptr, ...)
declare ptr @malloc(i64)
declare void @free(ptr)
declare void @exit(i32) #0
```

---

## String Literals

String literals are hoisted to module-level `private unnamed_addr` constants with
`\00` null terminators:

```llvm
@.str.0 = private unnamed_addr constant [13 x i8] c"hello world\0A\00"
```

---

## Tuple ABI

Tuples are lowered to LLVM anonymous struct types.  A tuple `(i32, f64)` becomes
`{ i32, double }` in the IR.  Field access is done via `getelementptr` (planned
for full MIR lowering of tuple field expressions in Phase 8).

---

## References

- `src/Aster.Compiler/Backends/LLVM/LlvmBackend.cs`
- `src/Aster.Compiler/Backends/LLVM/LlvmTargetInfo.cs`
- `src/Aster.Compiler/MiddleEnd/Mir/MirNodes.cs`

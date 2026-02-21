# Procedural Macros

**Status**: Phase 5 Complete ✅  
**Last Updated**: 2026-02-21

## Overview

Procedural macros in Aster allow compile-time code generation.  
Phase 5 implements **derive macros** (`#[derive(...)]`), which automatically implement common traits for structs and enums.

## Syntax

```rust
#[derive(Debug, Clone, PartialEq)]
struct Point {
    x: f64,
    y: f64,
}

#[derive(Debug, Clone, PartialEq, Eq, Hash)]
enum Color { Red, Green, Blue }
```

## Supported Derive Traits

| Trait | Generated Method | Return Type |
|-------|-----------------|-------------|
| `Debug` | `fn debug(&self) -> String` | `String` |
| `Clone` | `fn clone(&self) -> Self` | `Self` |
| `PartialEq` | `fn eq(&self, other: &Self) -> bool` | `bool` |
| `Eq` | *(marker — no methods)* | — |
| `Hash` | `fn hash(&self) -> i64` | `i64` |
| `Default` | `fn default() -> Self` *(static)* | `Self` |

## Semantics

### Expansion

`#[derive(Trait)]` on a struct or enum expands to an `impl Trait for Type { ... }` block with
synthesised method bodies.  The expansion runs during name resolution, before type checking,
so the synthesised impl methods are visible throughout the compilation unit.

```rust
// Source
#[derive(Clone)]
struct Pair { first: i32, second: i32 }

// Equivalent to
impl Clone for Pair {
    fn clone(&self) -> Pair { self }
}
```

### Error Handling

If you attempt to derive an unsupported trait, the compiler emits warning **E0800**:

```
warning[E0800]: Cannot derive 'Serialize': unsupported derive trait
 --> example.ast:1:10
  |
1 | #[derive(Serialize)]
  |          ^^^^^^^^^
```

The compilation continues; the derive attribute is ignored.

## Attribute Syntax

Outer attributes are written as `#[name]` or `#[name(arg1, arg2, ...)]` immediately before the
item they annotate.  Multiple attributes may be stacked:

```rust
#[derive(Debug)]
#[derive(Clone, PartialEq)]
struct Foo { x: i32 }
```

## Limitations (Phase 5)

- Only `#[derive(...)]` attributes are supported; custom attribute macros are not yet implemented.
- Derive bodies are stubs (return constants / `self`); full structural derivation requires
  runtime reflection support planned for Phase 6.
- Attributes on `fn`, `trait`, and `impl` items are parsed and ignored.

## Future Work

- Function-like procedural macros (`#[my_macro]`)
- Derive bodies that generate structurally-correct code via field introspection
- `#[serde(rename)]`-style attribute arguments

## References

- `src/Aster.Compiler/MiddleEnd/ProcMacros/ProcMacroProcessor.cs`
- `src/Aster.Compiler/Frontend/Ast/AstNodes.cs` — `AttributeNode`, `AttributeArgNode`
- `src/Aster.Compiler/Frontend/Hir/HirNodes.cs` — `HirDeriveAttr`

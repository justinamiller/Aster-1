# Associated Types in Aster

## Overview

Associated types are type members of traits. They allow a trait to declare a placeholder type that each implementing type fills in concretely.

## Defining Associated Types in Traits

```aster
trait Iterator {
    type Item;
    fn next(&mut self) -> Option<Self::Item>;
}
```

## Implementing Associated Types

```aster
struct Range { start: i32, end: i32, current: i32 }

impl Iterator for Range {
    type Item = i32;

    fn next(&mut self) -> Option<i32> {
        if self.current < self.end {
            let val = self.current;
            self.current = self.current + 1;
            Some(val)
        } else {
            None
        }
    }
}
```

## Accessing Associated Types

Use `TypeName::AssocName` to access a type's associated type projection:

```aster
fn consume<I: Iterator>(iter: I) -> I::Item { ... }
```

## Associated Type Projections

The compiler resolves `Type::AssocName` paths in the type checker using the associated type registry.

## Rules

- Associated types are declared with `type Name;` in a trait body
- Impl blocks provide the concrete type with `type Name = ConcreteType;`
- The same impl block may declare multiple associated types
- Associated type projections (`Type::Item`) are resolved at compile time

## Differences from Generic Parameters

| Feature           | Associated Types          | Generic Parameters       |
|-------------------|---------------------------|--------------------------|
| Declaration       | In trait body             | On `impl<T>` or `fn<T>` |
| Uniqueness        | One impl per type         | Many instantiations      |
| Usage at call site | `Iterator::Item`         | `Vec<i32>`, `Pair<A,B>` |

## Status

- ✅ Associated type declarations (`type Item;`) in traits
- ✅ Associated type implementations (`type Item = i32;`) in impl blocks
- ✅ Associated type projection (`Type::Item`) in type checker
- ✅ Multiple associated types per impl block
- ✅ HIR representation (`HirAssociatedTypeDecl`)

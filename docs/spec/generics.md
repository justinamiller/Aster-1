# Generics in Aster

## Overview

Generics allow writing code that works over multiple types while preserving type safety.

## Generic Functions

```aster
fn identity<T>(x: T) -> T {
    x
}

fn max<T: Ord>(a: T, b: T) -> T {
    if a > b { a } else { b }
}
```

## Generic Structs

```aster
struct Pair<A, B> {
    first: A,
    second: B
}

let p = Pair { first: 1, second: "hello" };
```

## Generic Enums

```aster
enum Either<L, R> {
    Left(L),
    Right(R)
}
```

## Trait Bounds

```aster
fn print_item<T: Display>(item: T) {
    print(item.display());
}

fn compare<T: Ord + Clone>(a: T, b: T) -> T {
    if a > b { a.clone() } else { b.clone() }
}
```

## Multiple Bounds

Use `+` to require multiple trait bounds:

```aster
fn process<T: Clone + Display + Ord>(value: T) { ... }
```

## Where Clauses

```aster
fn transform<T>(x: T) -> String
where T: Display + Clone
{
    x.display()
}
```

## Monomorphization

Generic functions and types are **monomorphized** at compile time — a separate concrete version is generated for each unique set of type arguments. This enables zero-cost abstractions.

## Built-in Generic Types

| Type             | Description                              |
|------------------|------------------------------------------|
| `Vec<T>`         | Growable array                           |
| `Option<T>`      | Optional value (`Some(T)` / `None`)      |
| `Result<T, E>`   | Success or error (`Ok(T)` / `Err(E)`)    |
| `HashMap<K, V>`  | Hash map from keys to values             |

## Status

- ✅ Generic functions (`fn name<T>(...)`)
- ✅ Generic structs (`struct Name<T> { ... }`)
- ✅ Generic enums
- ✅ Trait bounds (`T: TraitName`)
- ✅ Multiple bounds (`T: A + B`)
- ✅ Type inference at call sites
- ✅ Monomorphization pass (collects instantiations)
- ✅ Built-in generic collection types

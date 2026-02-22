# Traits in Aster

## Overview

Traits define a set of methods (and optional default implementations) that types can implement. They enable polymorphism and code reuse.

## Defining a Trait

```aster
trait Display {
    fn display(&self) -> String;
}
```

### Abstract Methods (Required)

Methods declared with a `;` (no body) are **required** — every impl must provide them:

```aster
trait Animal {
    fn name(&self) -> String;    // required
    fn sound(&self) -> String;   // required
}
```

### Default Methods (Provided)

Methods with a body are **default** — impls may override them but are not required to:

```aster
trait Greeter {
    fn name(&self) -> String;    // required
    fn greet(&self) -> String {  // default — returns "Hello, <name>"
        "hello"
    }
}
```

## Implementing a Trait

```aster
struct Dog { name: String }

impl Animal for Dog {
    fn name(&self) -> String { self.name }
    fn sound(&self) -> String { "woof" }
}
```

## Trait Bounds

```aster
fn print_all<T: Display>(items: Vec<T>) {
    for item in items {
        print(item.display());
    }
}
```

## Trait Objects (`dyn Trait`)

Use `dyn TraitName` to pass a dynamically-typed trait object:

```aster
fn log(writer: dyn Display) {
    print(writer.display());
}
```

Trait objects use dynamic dispatch via the impl method table.

## Built-in Traits

| Trait      | Description                          |
|------------|--------------------------------------|
| `Display`  | Convert to human-readable string     |
| `Debug`    | Convert to debug string              |
| `Clone`    | Create a copy of a value             |
| `Ord`      | Total ordering                       |
| `PartialOrd` | Partial ordering                  |
| `Eq`       | Equality comparison                  |
| `Hash`     | Compute a hash value                 |

## Error Codes

| Code  | Meaning                                          |
|-------|--------------------------------------------------|
| E0700 | Impl missing a required method from the trait   |

## Status

- ✅ Trait declaration
- ✅ Abstract (required) methods — `fn name(&self) -> T;`
- ✅ Default method implementations — `fn name(&self) -> T { body }`
- ✅ Trait impl blocks (`impl Trait for Type`)
- ✅ Required method completeness checking
- ✅ Trait bounds on generic parameters
- ✅ Trait objects (`dyn Trait`)
- ✅ Built-in traits for primitives

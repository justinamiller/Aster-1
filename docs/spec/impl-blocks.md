# Impl Blocks in Aster

## Overview

`impl` blocks associate methods, associated functions, and associated types with a type or trait.

## Inherent Impl Blocks

```aster
struct Rectangle { width: f64, height: f64 }

impl Rectangle {
    fn area(&self) -> f64 {
        self.width * self.height
    }

    fn perimeter(&self) -> f64 {
        2.0 * (self.width + self.height)
    }
}
```

## Trait Impl Blocks

```aster
trait Area {
    fn area(&self) -> f64;
}

impl Area for Rectangle {
    fn area(&self) -> f64 {
        self.width * self.height
    }
}
```

## Associated Types

```aster
trait Container {
    type Item;
    fn get(&self, index: i32) -> Self::Item;
}

impl Container for MyVec {
    type Item = i32;
    fn get(&self, index: i32) -> i32 { 0 }
}
```

## Generic Impl Blocks

```aster
impl<T> Container<T> {
    fn new() -> Container<T> { ... }
    fn push(&mut self, value: T) { ... }
}
```

## Rules

- A type may have multiple impl blocks
- Trait impls must provide all **required** (non-default) trait methods
- Default trait methods do not need to be re-implemented
- Impl blocks may not conflict (same method defined twice for same type)

## Status

- ✅ Inherent impl blocks
- ✅ Trait impl blocks (`impl Trait for Type`)
- ✅ Associated types (`type Item = T;`)
- ✅ Generic impl blocks (`impl<T> Type<T>`)
- ✅ Required method completeness checking (E0700)
- ✅ Default method bodies in traits

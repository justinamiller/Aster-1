# Methods in Aster

## Overview

Methods are functions associated with a type, defined inside an `impl` block. They receive the value they're called on as the `self` parameter.

## Syntax

```aster
struct Point {
    x: i32,
    y: i32
}

impl Point {
    fn new(x: i32, y: i32) -> Point {
        Point { x: x, y: y }
    }

    fn distance(&self) -> f64 {
        sqrt((self.x * self.x + self.y * self.y) as f64)
    }

    fn translate(&mut self, dx: i32, dy: i32) {
        self.x = self.x + dx;
        self.y = self.y + dy;
    }
}
```

## Self Parameters

| Form      | Meaning                            |
|-----------|------------------------------------|
| `self`    | Consumes the value (move)          |
| `&self`   | Immutable borrow                   |
| `&mut self` | Mutable borrow                  |

## Associated Functions

Associated functions are called on the type itself (not an instance):

```aster
let p = Point::new(1, 2);
```

## Method Call Syntax

```aster
let p = Point { x: 3, y: 4 };
let d = p.distance();
```

## Dispatch

Method dispatch in Aster is **static** (monomorphic). No virtual dispatch occurs unless a `dyn Trait` trait object is used. See [traits.md](traits.md) for trait object dispatch.

## Status

- ✅ Method definition (`fn name(&self, ...) -> T`)
- ✅ Self parameter in `impl` blocks
- ✅ Associated functions (`TypeName::func(...)`)
- ✅ Method call expression (`receiver.method(args)`)
- ✅ Static dispatch via impl method table
- ✅ Dynamic dispatch via `dyn Trait` (see [traits.md](traits.md))

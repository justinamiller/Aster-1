# Iterators

**Status**: Phase 6 ✅  
**Spec Version**: 1.0

## Overview

Aster's iterator system follows the same design as Rust's: a trait-based protocol
that separates the concept of *producing values* from *consuming them*.
The core traits are `Iterator`, `IntoIterator`, and `DoubleEndedIterator`.

## Core Traits

### `Iterator`

```aster
trait Iterator {
    type Item;
    fn next(&mut self) -> Option<Self::Item>;

    // Provided default methods:
    fn map<B, F: fn(Self::Item) -> B>(self, f: F) -> Map<Self, F>;
    fn filter<P: fn(&Self::Item) -> bool>(self, predicate: P) -> Filter<Self, P>;
    fn collect<C: FromIterator<Self::Item>>(self) -> C;
    fn count(self) -> usize;
    fn sum(self) -> Self::Item;
    fn fold<B, F: fn(B, Self::Item) -> B>(self, init: B, f: F) -> B;
}
```

### `IntoIterator`

```aster
trait IntoIterator {
    type Item;
    type IntoIter: Iterator<Item = Self::Item>;
    fn into_iter(self) -> Self::IntoIter;
}
```

Types implementing `IntoIterator` can be used directly in `for` loops.

## `for` Loop Desugaring

```aster
for x in iterable { body }
```

Desugars to:

```aster
{
    let mut __iter = iterable.into_iter();
    loop {
        match __iter.next() {
            Some(x) => { body },
            None => break,
        }
    }
}
```

## Range Iterators

```aster
for i in 0..10 {
    // i takes values 0, 1, …, 9
}

for i in 0..=10 {
    // i takes values 0, 1, …, 10 (inclusive upper bound)
}
```

`0..10` creates a `Range<i32>` which implements `Iterator<Item = i32>`.

### Range Types

| Expression | Type | Produces |
|------------|------|---------|
| `lo..hi` | `Range<T>` | `lo, lo+1, …, hi-1` |
| `lo..=hi` | `RangeInclusive<T>` | `lo, lo+1, …, hi` |
| `lo..` | `RangeFrom<T>` | `lo, lo+1, …` (infinite) |
| `..hi` | `RangeTo<T>` | `0, 1, …, hi-1` |
| `..` | `RangeFull` | all indices |

## Vec and Slice Iteration

```aster
let v = vec![1, 2, 3];

for x in v.iter() {        // borrows each element: &i32
    println("{}", x);
}

for x in v.iter_mut() {    // mutably borrows: &mut i32
    *x = *x * 2;
}

for x in v {               // moves/consumes: i32
    println("{}", x);
}
```

## Built-in Iterator Adapters (stdlib)

| Adapter | Description |
|---------|-------------|
| `map(f)` | Transform each element |
| `filter(p)` | Keep elements satisfying predicate |
| `enumerate()` | Pair each element with its index |
| `take(n)` | Yield first `n` elements |
| `skip(n)` | Skip first `n` elements |
| `zip(other)` | Pair elements from two iterators |
| `flat_map(f)` | Map then flatten |
| `chain(other)` | Concatenate two iterators |
| `peekable()` | Allow peeking at next element without consuming |

## Lazy Evaluation

Iterator adapters are **lazy**: they produce no values until the chain is consumed
by a terminal operation (`collect`, `count`, `fold`, `for` loop, etc.).

```aster
let sum = (0..1000)
    .filter(|x| x % 2 == 0)
    .map(|x| x * x)
    .sum();               // computed lazily, no intermediate Vec
```

## Implementing `Iterator` for Custom Types

```aster
struct Counter {
    count: i32,
    max: i32,
}

impl Counter {
    fn new(max: i32) -> Counter {
        Counter { count: 0, max }
    }
}

impl Iterator for Counter {
    type Item = i32;

    fn next(&mut self) -> Option<i32> {
        if self.count < self.max {
            let val = self.count;
            self.count = self.count + 1;
            Some(val)
        } else {
            None
        }
    }
}

fn main() -> i32 {
    let mut total = 0;
    for x in Counter::new(5) {
        total = total + x;
    }
    total  // 0 + 1 + 2 + 3 + 4 = 10
}
```

## Loop Unrolling (Phase 6 Optimization)

When iterating over a constant-size range (e.g. `0..4`), the compiler's
**LoopUnrollPass** can unroll the loop body up to 8 times, eliminating
the branch overhead entirely.

```aster
// Unrolled at compile time for small constant ranges:
for i in 0..4 {
    process(i);
}
// ↓ Equivalent after unrolling:
// process(0); process(1); process(2); process(3);
```

## Error Codes

| Code | Meaning |
|------|---------|
| E0620 | Type does not implement `Iterator` |
| E0621 | Type does not implement `IntoIterator` |
| E0622 | Cannot iterate over moved value in loop body |

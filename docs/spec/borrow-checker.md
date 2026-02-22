# Borrow Checker in Aster

## Overview

Aster implements a **Non-Lexical Lifetimes (NLL)** borrow checker that enforces ownership rules at compile time. The borrow checker prevents:

- Use after move
- Mutable aliasing (two mutable references at once)
- Dangling borrows
- Moving out of borrowed values

## Ownership Rules

1. Each value has exactly one owner
2. When the owner goes out of scope, the value is dropped
3. You can transfer ownership (move) to another binding

## Borrowing Rules

1. At any given time, you can have **either**:
   - Any number of immutable borrows (`&T`)
   - **Or** exactly one mutable borrow (`&mut T`)
2. Borrows must not outlive the owner

## Two-Phase Borrows (Phase 4)

Aster supports two-phase borrows which allow a mutable borrow to be *reserved* before it is *activated*. During the reservation phase, existing immutable borrows are still permitted. The mutable borrow is activated when it is first used.

This allows patterns like:

```aster
let mut v = vec![1, 2, 3];
let len = v.len();          // immutable borrow of v
v.push(len);                // mutable borrow activates here — previous immutable borrow has ended
```

## Lifetime Annotations

Lifetimes describe the scope for which a reference is valid:

```aster
fn longest<'a>(x: &'a String, y: &'a String) -> &'a String {
    if x.len() > y.len() { x } else { y }
}
```

Special lifetime `'static` means the reference lives for the entire program.

## Error Codes

| Code  | Message                                             | Hint                                          |
|-------|-----------------------------------------------------|-----------------------------------------------|
| E0500 | Use of moved value `x`                              | Clone before moving, or use only once         |
| E0501 | Cannot move `x` while it is borrowed               | End all borrows before moving                 |
| E0502 | Cannot borrow moved value `x`                      | Value was moved and is no longer valid        |
| E0503 | Cannot mutably borrow `x`: already mutably borrowed | Only one mutable reference allowed at a time |
| E0504 | Cannot immutably borrow `x`: already mutably borrowed | Mutable borrow must end first              |
| E0505 | Use of moved value `x` in call/load               | Value was moved earlier in this scope         |

## Status

- ✅ Use-after-move detection (E0500)
- ✅ Move-while-borrowed detection (E0501)
- ✅ Borrow-of-moved value detection (E0502)
- ✅ Mutable aliasing detection (E0503)
- ✅ Immutable borrow of mutably-borrowed value (E0504)
- ✅ Two-phase borrow support (reservation → activation)
- ✅ Better error messages with Hints
- ✅ Drop clears borrow state
- ✅ CFG-based dataflow analysis (fixed-point iteration)
- ✅ Lifetime annotation parsing (erased before type checking)
- 🔜 Full lifetime inference (planned)
- 🔜 Borrow splitting (planned)

# Aster Productionization Plan (A → B → C)

## Current Reality (as of 2026-02-15)

- Stage 0 (C# seed compiler) exists and is the only implementation that can compile Aster source today.
- Stage 1 Aster sources exist (`aster/compiler/*`) but are not yet a complete self-hosting compiler pipeline.
- Stage 2/3 source trees are not present yet (`aster/compiler/stage2`, `aster/compiler/stage3`).

## Phase A — Harden + Validate Stage 0 Path

### Goals
- Make bootstrap scripts reliable across framework versions.
- Add deterministic local smoke test for hello world.
- Produce reproducible runbook commands.

### Delivered in this pass
- `bootstrap/scripts/bootstrap.sh`
  - Stage 0 output discovery now resolves `Aster.CLI.dll` dynamically instead of hardcoding `net10.0`.
- `bootstrap/scripts/check-and-advance.sh`
  - Same dynamic Stage 0 output resolution.
  - Stage binary checks now accept Unix and `.exe` artifacts.
- `bootstrap/scripts/verify.sh`
  - Stage binary checks now accept Unix and `.exe` artifacts.
- `scripts/phase-a-smoke.sh`
  - End-to-end smoke script:
    1. Build Stage 0
    2. Compile `examples/simple_hello.ast` to LLVM IR with `Aster.CLI.dll`
    3. Use `clang` to build native executable
    4. Run executable

## Phase B — Stage 1 Completion

### Goals
- Complete Stage 1 compiler implementation so it can compile itself and simple user programs.

### Required work
- Finalize parser/AST integration and diagnostics flow.
- Implement Stage 1 codegen/driver path to emit runnable output.
- Add Stage 1 acceptance tests:
  - compile sample Aster files
  - compile hello-world
  - compare token/AST outputs against Stage 0 for subset inputs.

## Phase C — Stage 2/3 Self-Hosting Chain

### Goals
- Add Stage 2 and Stage 3 source trees and verification gates.
- Prove self-hosting progression (`aster1 -> aster2 -> aster3`).

### Required work
- Implement `aster/compiler/stage2` and `aster/compiler/stage3` codebase.
- Add CI matrix to validate stage progression.
- Add self-hosting check (`aster3` rebuilds itself and outputs match policy).

## Repro Runbook (Phase A)

```bash
./scripts/phase-a-smoke.sh
```

Prerequisites:
- .NET SDK 10+
- clang/LLVM on PATH

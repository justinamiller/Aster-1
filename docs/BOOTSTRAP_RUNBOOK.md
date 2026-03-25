# Bootstrap Runbook (validated on 2026-02-15)

## 0) Prerequisites

```bash
# .NET 10 SDK (local user install)
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --version latest --channel 10.0 --install-dir "$HOME/.dotnet"

# Ensure current shell sees dotnet
export PATH="$HOME/.dotnet:$PATH"

# Verify
dotnet --version
clang --version
```

## 1) Build Stage 0 seed compiler

```bash
export PATH="$HOME/.dotnet:$PATH"
./bootstrap/scripts/bootstrap.sh --clean --stage 1 --verbose
```

Expected Stage 0 artifact:
- `build/bootstrap/stage0/Aster.CLI.dll`

## 2) Attempt full bootstrap and inspect blockers

```bash
export PATH="$HOME/.dotnet:$PATH"
./bootstrap/scripts/bootstrap.sh --clean --stage 3 --verbose
```

Current expected outcome (as of this validation):
- Stage 0 succeeds
- Stage 1 fails on Core-0 restrictions in `aster/compiler/frontend/string_interner.ast`

See: `docs/BOOTSTRAP_GAP_ASSESSMENT.md`.

## 3) Aster-only Hello World validation (no C# compiler for app source)

Source file:
- `examples/bootstrap_hello_world.ast`

Compile using Stage 0 compiler artifact (Aster CLI DLL), then run:

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet ./build/bootstrap/stage0/Aster.CLI.dll build examples/bootstrap_hello_world.ast -o /tmp/bootstrap_hello_world
/tmp/bootstrap_hello_world
```

Expected stdout line:

```text
hello world
```

Note: current runtime exits with code `10` after printing; stdout content is the proof point for this bootstrap smoke test.

## 4) Optional verification command

```bash
export PATH="$HOME/.dotnet:$PATH"
./bootstrap/scripts/verify.sh --stage 0 --verbose
```

(If execution is long in your environment, run targeted test projects via `dotnet test` directly.)

---

## 5) Standalone smoke gate (production toolchain proof)

This gate verifies the full end-to-end toolchain using the Stage 0 (C#) compiler:
`Aster source → LLVM IR → native binary → run`.

### Commands

```bash
# One-command local smoke run
bash scripts/standalone-smoke.sh

# Via the bootstrap verify wrapper (equivalent)
bash bootstrap/scripts/verify-standalone.sh

# Keep intermediate artifacts for debugging
bash scripts/standalone-smoke.sh --keep-artifacts
```

### Validated sequence

```bash
dotnet build Aster.slnx -c Release
dotnet run --project src/Aster.CLI -- build examples/simple_hello.ast --emit-llvm -o /tmp/aster_hello.ll
clang /tmp/aster_hello.ll -o /tmp/aster_hello
/tmp/aster_hello
```

### Expected output and exit code policy

| Check | Expected value |
|-------|----------------|
| **stdout** | `Hello from Aster!` (exact match, from `examples/simple_hello.ast`) |
| **exit code** | `0` or `128` (see note below) |

> **Exit code 128 — known temporary behaviour.**
> The Aster IR currently emits `define void @main()` (void return type), which
> is non-standard C.  When linked with clang on Linux x86-64 this consistently
> produces exit code 128, but the value is technically indeterminate (it depends
> on whatever happens to be in the return register).
>
> **Contrast with bootstrap_hello_world.ast:**
> `examples/bootstrap_hello_world.ast` exits with code `10` — this is a
> deliberate explicit `exit(10)` call in that program's source, not a toolchain
> defect.  The standalone smoke gate uses `examples/simple_hello.ast` instead,
> which has no explicit exit call.
>
> **Target future state:** exit 0 once the IR backend emits
> `define i32 @main()` with `ret i32 0`.

### CI enforcement

The `Standalone Smoke` GitHub Actions workflow (`.github/workflows/standalone-smoke.yml`)
runs this same sequence automatically on every push to `main` and every pull request.
It fails the build if stdout does not match or the exit code is outside `{0, 128}`.

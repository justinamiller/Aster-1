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

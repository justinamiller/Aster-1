# Bootstrap Validation Evidence (2026-02-15)

## Environment

```text
dotnet --version
10.0.103
```

## Stage 0 build succeeded

From:
`./bootstrap/scripts/bootstrap.sh --clean --stage 3 --verbose`

```text
[SUCCESS] Stage 0 built successfully
[INFO] Stage 0 binary: /Users/justinmiller/.openclaw/workspace/Aster-1/build/bootstrap/stage0/Aster.CLI.dll
```

## Stage 1 currently blocked

From `./bootstrap/scripts/bootstrap.sh --clean --stage 1 --verbose`:

```text
EXIT:1
error[E9004]: Reference types (&T, &mut T) are not allowed in Stage1 (Core-0) mode
error[E0102]: Expected ')', found 'as'
error[E0100]: Unexpected token '}'
```

Primary file in this run:
- `aster/compiler/frontend/string_interner.ast`

## Hello World (Aster-only source) compiled via Stage 0 artifact

Command:

```bash
dotnet ./build/bootstrap/stage0/Aster.CLI.dll build examples/bootstrap_hello_world.ast -o /tmp/bootstrap_hello_world
/tmp/bootstrap_hello_world
```

Observed output:

```text
Compiled 1 file(s) -> /tmp/bootstrap_hello_world
hello world
EXIT:10
```

Interpretation:
- App source (`.ast`) was compiled through Aster compiler artifact (`Aster.CLI.dll`), not C# source compilation.
- Runtime currently exits with code `10` after printing expected output.

## Phase-A smoke script

Command:

```bash
./scripts/phase-a-smoke.sh
```

Observed completion line:

```text
[phase-a] Success: phase A smoke test complete (output='hello world', exit=10)
```

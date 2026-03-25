# Smoke Tests

## Standalone smoke (recommended — full compile + run)

The **standalone smoke** test proves the complete end-to-end toolchain:
`Aster source → LLVM IR → native binary → run`.

```bash
bash scripts/standalone-smoke.sh
```

This verifies:
1. ✅ Compiler builds (`dotnet build Aster.slnx -c Release`)
2. ✅ Compiles `examples/simple_hello.ast` to LLVM IR
3. ✅ LLVM IR is compiled to a native binary with clang
4. ✅ Native binary executes and prints the expected output

### Expected output

```
======================================================
  Aster Standalone Smoke Test
======================================================

[STEP] 1/4  dotnet build Aster.slnx -c Release
[PASS] Build succeeded

[STEP] 2/4  Compiling examples/simple_hello.ast -> /tmp/aster_hello.ll
[PASS] LLVM IR emitted (26 lines)

[STEP] 3/4  clang /tmp/aster_hello.ll -o /tmp/aster_hello
[PASS] Native binary created

[STEP] 4/4  Running /tmp/aster_hello
  stdout      : Hello from Aster!
  exit code   : 128

[PASS] exit code 128 is acceptable

======================================================
  SMOKE PASSED
======================================================
```

### Exit code policy

| Check | Expected |
|-------|----------|
| **stdout** | `Hello from Aster!` (exact match) |
| **exit code** | `0` or `128` |

Exit code 128 is a known temporary behaviour caused by the IR emitting
`define void @main()` (non-standard C).  See `docs/BOOTSTRAP_RUNBOOK.md §5`
for full rationale and target future state.

### Options

```bash
# Keep /tmp/aster_hello.ll and /tmp/aster_hello after the run
bash scripts/standalone-smoke.sh --keep-artifacts

# Via the bootstrap verify wrapper
bash bootstrap/scripts/verify-standalone.sh
```

### Prerequisites

```bash
sudo apt-get install clang   # Linux
brew install llvm            # macOS
```

---

## Legacy IR-only smoke (CI step)

The original one-command smoke test (IR generation only, no execution):

```bash
./scripts/smoke_test.sh
```

This verifies:
1. ✅ Compiler builds successfully
2. ✅ Can compile simple programs to LLVM IR
3. ✅ Generated IR contains a valid `@main` function definition

> **Note:** This test does **not** execute the compiled binary.
> Use `scripts/standalone-smoke.sh` for the full end-to-end check.

---

## CI Integration

Both smoke tests are run in CI automatically:

| Workflow | Script | Trigger |
|----------|--------|---------|
| `Aster CI` (`ci.yml`) | `scripts/smoke_test.sh` | push / PR to main |
| `Standalone Smoke` (`standalone-smoke.yml`) | full sequence | push / PR to main |

Add to a GitHub Actions workflow:

```yaml
- name: Standalone Smoke
  run: bash scripts/standalone-smoke.sh
```

---

## Troubleshooting

### Build fails

```bash
dotnet restore
dotnet build Aster.slnx -c Release
```

### `clang not found`

```bash
sudo apt-get install clang   # Linux
brew install llvm            # macOS
```

### Output mismatch

```bash
# Inspect the generated IR
cat /tmp/aster_hello.ll

# Run with kept artifacts
bash scripts/standalone-smoke.sh --keep-artifacts
/tmp/aster_hello
```

## See Also

- [README.md — Standalone Smoke Gate](../README.md#standalone-smoke-gate)
- [docs/BOOTSTRAP_RUNBOOK.md](BOOTSTRAP_RUNBOOK.md) — Full bootstrap runbook
- [examples/simple_hello.ast](../examples/simple_hello.ast) — Source under test
- [.github/workflows/standalone-smoke.yml](../.github/workflows/standalone-smoke.yml) — CI workflow

---

**Last Updated**: 2026-03-25
**Status**: Full compile + run smoke available (Stage 0 compiler + clang)


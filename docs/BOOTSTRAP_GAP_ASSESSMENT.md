# Bootstrap Gap Assessment (2026-02-15)

## Scope
Assessment of the current repository state for reaching a full self-hosted Aster chain:

`stage0 (C#) -> stage1 (Aster) -> stage2 (Aster) -> stage3 (Aster) -> self-check`

## What is currently working

- Stage 0 build works with .NET 10 SDK:
  - Produces `build/bootstrap/stage0/Aster.CLI.dll`
- Stage 0 can compile Aster app source to native via LLVM/clang.
- Bootstrap scripts are present for all stages.

## Blocking gaps to full self-hosting

### 1) Stage 1 source does not compile in current Stage1/Core-0 mode

Observed from `./bootstrap/scripts/bootstrap.sh --clean --stage 3 --verbose`:

- `error[E9004]: Reference types (&T, &mut T) are not allowed in Stage1 (Core-0) mode`
- `error[E0102]: Expected ')', found 'as'`
- `error[E0100]: Unexpected token '}'`

Primary failing file in current run:
- `aster/compiler/frontend/string_interner.ast`

### 2) Stage 1 output is not yet a verified compiler artifact chain

Even after Stage 1 syntax/feature compliance is fixed, the repo still needs:
- A Stage 1 compiler driver contract that can compile Stage 2 source as required by bootstrap spec
- Differential tests enforcing Stage 0 vs Stage 1 behavioral parity

### 3) Stage 2/3 self-hosted source trees are not implemented to spec

Spec expects:
- `aster/compiler/stage2/`
- `aster/compiler/stage3/`

Current repository does not yet contain complete implementations for these stages.

### 4) Verification scripts for later stages are mostly placeholders

`bootstrap/scripts/verify.sh` has TODO sections for:
- Stage 2 verification
- Stage 3 verification
- self-hosting check (`aster3 == aster3'`)
- reproducibility verification

## Conclusion

- **Stage 0 is production-usable.**
- **Full self-hosted chain is not yet achievable in this repository revision.**
- To reach “all intended stages passing”, Stage 1 language compliance + Stage 2/3 implementation + non-placeholder verification must be completed.

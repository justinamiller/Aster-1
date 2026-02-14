# Stage 0: Seed Compiler (C# Implementation)

## Overview

This directory contains references and build scripts for the **seed compiler** — the C# implementation of the Aster compiler that serves as the bootstrap foundation.

## Location

The actual C# source code is in:
- `/src/Aster.Compiler/` - Main compiler implementation
- `/src/Aster.CLI/` - Command-line interface
- `/src/Aster.*/` - Supporting projects (tooling, optimizations, etc.)

This directory (`/bootstrap/stages/stage0-csharp/`) serves as a reference point and contains:
- Build scripts specific to Stage 0
- Documentation about the seed compiler role
- Version tracking

## Building

```bash
# From project root
dotnet build Aster.slnx --configuration Release

# Or use the bootstrap script
./bootstrap/scripts/bootstrap.sh --stage 0
```

## Outputs

Stage 0 produces:
- `Aster.CLI.dll` - The compiler executable (requires .NET runtime)
- Supporting assemblies

## Role in Bootstrap

Stage 0 is the **seed** — it compiles the first Aster-written compiler (Stage 1):

```
Stage 0 (C#) → compiles → Aster source → produces → Stage 1 (native binary)
```

## See Also

- `/bootstrap/seed/README.md` - Detailed seed compiler documentation
- `/bootstrap/spec/bootstrap-stages.md` - Bootstrap pipeline overview
- `/bootstrap/spec/trust-chain.md` - Trust model

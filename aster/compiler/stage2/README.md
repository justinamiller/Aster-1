# Stage 2: Expanded Aster Compiler Source

## Overview

This directory will contain the **Stage 2 (Aster Core-1)** compiler implementation.

## Status

⏳ **Not Yet Implemented** - Infrastructure ready, awaiting implementation.

## What Goes Here

Stage 2 will include the expanded Aster compiler written in **Aster Core-1**:

1. **Name Resolution** - Symbol table, cross-module resolution, imports/exports
2. **Type Inference** - Hindley-Milner constraint generation and unification
3. **Trait Solver** - Trait resolution, impl database, constraint solving
4. **Effects System** - Effect inference and propagation
5. **Ownership Analysis** - Move semantics, borrow tracking, use-after-move detection

## Prerequisites

Before implementing Stage 2:
- ✅ Stage 0 (C# seed compiler) must be complete
- 🚧 Stage 1 (minimal Aster compiler) must be complete (currently 20% done)

## Building

Once implemented, Stage 2 will be built with:

\`\`\`bash
# Using Stage 1 compiler
aster1 build aster/compiler/stage2/*.ast -o build/bootstrap/stage2/aster2

# Or using the bootstrap script
./bootstrap/scripts/bootstrap.sh --stage 2
\`\`\`

## Verification

Stage 2 will be verified by:
- Differential tests vs Stage 0 on Core-1 fixtures
- All Core-0 tests still pass (regression)
- Self-compilation test: aster2 compiles itself

## Implementation Timeline

**Estimated**: 3-4 months after Stage 1 completion

**Key Milestones**:
1. Port name resolution (1 month)
2. Port type inference (1-1.5 months)
3. Port trait solver (1 month)
4. Port effects and ownership (0.5-1 month)

## See Also

- `/bootstrap/spec/bootstrap-stages.md` - Complete stage specifications
- `/bootstrap/stages/stage2-aster/README.md` - Stage 2 documentation
- `/bootstrap/spec/aster-core-subsets.md` - Language subset definitions

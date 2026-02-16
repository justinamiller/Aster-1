# Stage 3: Full Aster Compiler Source

## Overview

This directory will contain the **Stage 3 (full Aster)** compiler implementation.

## Status

⚙️ **Not Yet Implemented** - Infrastructure ready, awaiting implementation.

## What Goes Here

Stage 3 will include the complete Aster compiler written in **Aster Core-2** (full language):

1. **Borrow Checker** - Non-lexical lifetimes (NLL) with dataflow analysis
2. **MIR Builder** - Complete MIR construction and verification
3. **Optimizations** - All optimization passes (DCE, constant folding, CSE, inlining, SROA)
4. **LLVM Backend** - Full LLVM IR emission with debug info
5. **Tooling** - Formatter, linter, documentation generator, test runner

## Prerequisites

Before implementing Stage 3:
- ✅ Stage 0 (C# seed compiler) must be complete
- 🚧 Stage 1 (minimal Aster compiler) must be complete (currently 20% done)
- ⏳ Stage 2 (expanded Aster compiler) must be complete (not yet started)

## Building

Once implemented, Stage 3 will be built with:

\`\`\`bash
# Using Stage 2 compiler
aster2 build aster/compiler/stage3/*.ast -o build/bootstrap/stage3/aster3

# Or using the bootstrap script
./bootstrap/scripts/bootstrap.sh --stage 3
\`\`\`

## Self-Hosting

The ultimate goal is for Stage 3 to compile itself:

\`\`\`bash
# aster3 compiles itself to produce aster3'
aster3 build aster/compiler/stage3/*.ast -o aster3'

# Verify they are identical (or semantically equivalent)
./bootstrap/scripts/verify.sh --self-check
\`\`\`

## Implementation Timeline

**Estimated**: 4-6 months after Stage 2 completion

**Key Milestones**:
1. Port borrow checker (1-2 months)
2. Port MIR system (1-2 months)
3. Port LLVM backend (1-2 months)
4. Port tooling (1 month)
5. Self-hosting validation (2-4 weeks)

## See Also

- `/bootstrap/spec/bootstrap-stages.md` - Complete stage specifications
- `/bootstrap/stages/stage3-aster/README.md` - Stage 3 documentation
- `/bootstrap/spec/aster-core-subsets.md` - Language subset definitions

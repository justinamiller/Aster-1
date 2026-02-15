# Stage 1: Minimal Aster Compiler

## Overview

Stage 1 is the **first self-compiled** Aster compiler, written in the **Core-0 subset** of Aster.

## Language Subset

**Aster Core-0** (see `/bootstrap/spec/aster-core-subsets.md`):
- Structs, enums, functions
- Basic control flow (if/while/for/match)
- Vec, String, Box
- Result/Option
- **NO** traits, async, macros, or advanced features

## Components Implemented

Stage 1 includes:
1. **Contracts** - Span, SourceId, Token types
2. **Lexer** - UTF-8 tokenization with span tracking
3. **Parser** - Recursive descent parser, AST construction
4. **AST/HIR** - Data structures for syntax trees

## Source Location

Aster source code is in `/aster/compiler/`

**Status**: Core components implemented (contracts, lexer, parser, AST structures)

## Building

Once implemented, Stage 1 will be built using Stage 0:

```bash
# Compile Aster source with C# compiler
aster0 build aster/compiler/stage1/*.ast -o build/bootstrap/stage1/aster1

# Or use bootstrap script
./bootstrap/scripts/bootstrap.sh --stage 1
```

## Verification

Stage 1 is verified by:
- **Differential tests**: Compare aster0 vs aster1 outputs on Core-0 fixtures
- **Self-compilation**: aster1 can compile its own source
- **Output equivalence**: aster1 produces same ASTs as aster0

See: `./bootstrap/scripts/verify.sh --stage 1`

## Implementation Status

### Completed ✅

1. **Port Contracts** ✅
   - ✅ `/aster/compiler/contracts/span.ast` - Source location tracking
   - ✅ `/aster/compiler/contracts/token.ast` - Token representation
   - ✅ `/aster/compiler/contracts/token_kind.ast` - Token type enumeration
   - ✅ `/aster/compiler/contracts/diagnostics.ast` - Error reporting

2. **Port Lexer** ✅
   - ✅ `/aster/compiler/frontend/lexer.ast` - UTF-8 tokenization with span tracking
   - ✅ `/aster/compiler/frontend/string_interner.ast` - String deduplication

3. **Port Parser** ✅
   - ✅ `/aster/compiler/frontend/parser.ast` - Recursive descent parser

4. **Create AST Structures** ✅
   - ✅ `/aster/compiler/ir/ast.ast` - Abstract Syntax Tree node definitions
   - ✅ `/aster/compiler/main.ast` - Compiler entry point and driver

5. **Create Tests** ✅
   - ✅ Fixtures in `/bootstrap/fixtures/core0/` (compile-pass, run-pass, compile-fail)
   - ✅ Golden outputs in `/bootstrap/goldens/core0/`

6. **Differential Testing Infrastructure** ✅
   - ✅ Bootstrap build script (`bootstrap/scripts/bootstrap.sh`)
   - ✅ Differential test script (`bootstrap/scripts/diff-test-tokens.sh`)
   - ✅ Verification script (`bootstrap/scripts/verify.sh`)

### Pending ⏳

**Note**: The .ast files created define the structure and algorithms in Core-0 syntax.
To complete the full bootstrap:

1. **Stage 0 Compiler Enhancement**
   - The C# compiler (Stage 0) needs to be enhanced to compile .ast files to executable code
   - This involves implementing code generation from Aster source to LLVM IR or native code

2. **Executable Compilation**
   - Once Stage 0 can compile .ast files, run: `./bootstrap/scripts/bootstrap.sh --stage 1`
   - This will produce the `aster1` executable

3. **Self-Compilation Test**
   - Verify `aster1` can compile its own source
   - Run differential tests to ensure output equivalence with `aster0`

### Current State

- ✅ All source files for Stage 1 compiler exist
- ✅ Infrastructure and testing framework in place
- ✅ Differential testing can verify golden outputs
- ⏳ Awaiting Stage 0 code generation capability for .ast files

## See Also

- `/bootstrap/spec/bootstrap-stages.md` - Detailed Stage 1 specification
- `/bootstrap/spec/aster-core-subsets.md` - Core-0 language definition
- `/aster/compiler/` - Future home of Aster source code

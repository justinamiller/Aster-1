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

**Future**: Aster source code will be in `/aster/compiler/stage1/`

**Status**: Not yet implemented (infrastructure ready)

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

## Next Steps

To implement Stage 1:

1. **Port Contracts**
   - Create `/aster/compiler/contracts/span.ast`
   - Create `/aster/compiler/contracts/token.ast`
   - Create `/aster/compiler/contracts/diagnostics.ast`

2. **Port Lexer**
   - Create `/aster/compiler/frontend/lexer.ast`
   - Implement UTF-8 tokenization in Core-0

3. **Port Parser**
   - Create `/aster/compiler/frontend/parser.ast`
   - Implement recursive descent parser in Core-0

4. **Create Tests**
   - Add fixtures to `/bootstrap/fixtures/core0/`
   - Add expected outputs to `/bootstrap/goldens/core0/`

5. **Differential Testing**
   - Compare aster0 and aster1 outputs
   - Verify token streams match
   - Verify AST structures match

## See Also

- `/bootstrap/spec/bootstrap-stages.md` - Detailed Stage 1 specification
- `/bootstrap/spec/aster-core-subsets.md` - Core-0 language definition
- `/aster/compiler/` - Future home of Aster source code

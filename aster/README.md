# Aster Compiler Implementation (Native)

## Overview

This directory will contain the **Aster implementation** of the Aster compiler — the self-hosted compiler written in Aster itself.

## Status

🚧 **Infrastructure Ready** - Implementation pending

This directory structure is prepared for the bootstrap process. The actual Aster source code will be added as each stage is implemented.

## Directory Structure

```
aster/
├── compiler/           # Aster compiler implementation
│   ├── stage1/         # Stage 1: Minimal compiler (Core-0)
│   ├── stage2/         # Stage 2: Expanded compiler (Core-1)
│   ├── stage3/         # Stage 3: Full compiler (Core-2)
│   └── README.md       # This file
│
├── stdlib/             # Aster standard library
│   ├── core/           # Core types and traits
│   ├── collections/    # Vec, HashMap, etc.
│   ├── io/             # I/O primitives
│   ├── sync/           # Concurrency primitives
│   └── README.md
│
└── tooling/            # Tooling written in Aster
    ├── formatter/      # Code formatter
    ├── linter/         # Code linter
    ├── docgen/         # Documentation generator
    ├── test/           # Test runner
    └── README.md
```

## Purpose

The code in this directory represents the **target** of the bootstrap process:

1. **Stage 1** (Core-0): Minimal compiler
   - Contracts, lexer, parser
   - Compiles itself using Stage 0 (C# compiler)

2. **Stage 2** (Core-1): Expanded compiler
   - Type system, traits, semantics
   - Compiles itself using Stage 1

3. **Stage 3** (Core-2): Full compiler
   - Complete language, optimizations, LLVM backend
   - Compiles itself (self-hosting!)

## Language Subsets

Each stage uses a specific language subset:

- **Stage 1**: Core-0 (no traits, no async, no macros)
- **Stage 2**: Core-1 (traits, generics, ownership)
- **Stage 3**: Core-2 (full Aster language)

See: `/bootstrap/spec/aster-core-subsets.md`

## Implementation Plan

### Phase 1: Stage 1 (Minimal Compiler)

Port to Aster Core-0:

1. **Contracts** (`compiler/stage1/contracts/`)
   - `span.ast` - Source span tracking
   - `token.ast` - Token types
   - `diagnostics.ast` - Error reporting

2. **Lexer** (`compiler/stage1/frontend/lexer.ast`)
   - UTF-8 tokenization
   - Span tracking
   - String interning

3. **Parser** (`compiler/stage1/frontend/parser.ast`)
   - Recursive descent
   - Pratt expression parsing
   - AST construction

4. **AST/HIR** (`compiler/stage1/ir/`)
   - AST node definitions
   - HIR node definitions

**Build**: `aster0 build compiler/stage1/*.ast -o aster1`

---

### Phase 2: Stage 2 (Expanded Compiler)

Port to Aster Core-1:

5. **Name Resolution** (`compiler/stage2/semantics/nameresolution.ast`)
   - Symbol table
   - Cross-module resolution

6. **Type Inference** (`compiler/stage2/semantics/types.ast`)
   - Hindley-Milner
   - Unification

7. **Trait Solver** (`compiler/stage2/semantics/traits.ast`)
   - Trait resolution
   - Cycle detection

8. **Effects** (`compiler/stage2/semantics/effects.ast`)
   - Effect inference

9. **Ownership** (`compiler/stage2/semantics/ownership.ast`)
   - Move semantics
   - Borrow tracking

**Build**: `aster1 build compiler/stage2/*.ast -o aster2`

---

### Phase 3: Stage 3 (Full Compiler)

Port to Aster Core-2:

10. **Borrow Checker** (`compiler/stage3/semantics/borrow.ast`)
    - Non-lexical lifetimes (NLL)
    - Dataflow analysis

11. **MIR** (`compiler/stage3/midend/mir.ast`)
    - MIR builder
    - MIR optimizer

12. **Optimizations** (`compiler/stage3/midend/opt/`)
    - DCE, constant folding, CSE, inlining, SROA

13. **LLVM Backend** (`compiler/stage3/backend/llvm.ast`)
    - LLVM IR emission

14. **Tooling** (`tooling/`)
    - Formatter
    - Linter
    - Doc generator
    - Test runner

**Build**: `aster2 build compiler/stage3/*.ast -o aster3`

**Verify**: `aster3 build compiler/stage3/*.ast -o aster3'` (should match aster3)

---

## Standard Library

The Aster standard library (`stdlib/`) will be developed alongside the compiler:

### Core (Core-0)
- `Option<T>` and `Result<T, E>`
- `Vec<T>` and `String`
- `Box<T>` for heap allocation
- Iterator basics

### Collections (Core-1)
- `HashMap<K, V>`
- `HashSet<T>`
- `BTreeMap<K, V>`
- `BTreeSet<T>`

### I/O (Core-2)
- File I/O
- Network I/O
- Buffered readers/writers

### Sync (Core-2)
- `Mutex<T>`
- `RwLock<T>`
- `Arc<T>`
- Channels

---

## Tooling

Tooling will be written in Aster as Stage 3 progresses:

### Formatter (`tooling/formatter/`)
- AST-based pretty-printing
- Configurable style
- Integration with editors

### Linter (`tooling/linter/`)
- HIR-based analysis
- Style checks
- Common mistake detection

### Doc Generator (`tooling/docgen/`)
- Extract doc comments
- Generate HTML/Markdown
- Cross-reference resolution

### Test Runner (`tooling/test/`)
- Discover and run tests
- Parallel execution
- Coverage reporting

---

## Development Workflow

### Adding New Code

1. **Choose stage** based on language subset needed
2. **Create `.ast` file** in appropriate directory
3. **Implement in Aster** using allowed language features
4. **Test** with differential tests
5. **Verify** against C# implementation

### Example: Adding a Parser

```bash
# 1. Create file
touch aster/compiler/stage1/frontend/parser.ast

# 2. Implement parser in Core-0 Aster
# (Edit parser.ast)

# 3. Build with seed compiler
aster0 build aster/compiler/stage1/*.ast -o aster1

# 4. Test differential
./bootstrap/scripts/verify.sh --stage 1
```

---

## Code Style

Aster code in this directory should follow:

- **Idiomatic Aster**: Use language features appropriately
- **Documented**: Doc comments on public items
- **Tested**: Unit tests where applicable
- **Minimal**: No unnecessary complexity
- **Readable**: Clear variable names, logical structure

---

## Testing

Each component should have:

1. **Unit tests** (in same file or `_test.ast`)
2. **Integration tests** (in `/tests/`)
3. **Differential tests** (in `/bootstrap/fixtures/`)

---

## Building

### Stage 1
```bash
# Using seed compiler
aster0 build aster/compiler/stage1/*.ast -o build/bootstrap/stage1/aster1

# Or use bootstrap script
./bootstrap/scripts/bootstrap.sh --stage 1
```

### Stage 2
```bash
# Using Stage 1 compiler
aster1 build aster/compiler/stage2/*.ast -o build/bootstrap/stage2/aster2

# Or use bootstrap script
./bootstrap/scripts/bootstrap.sh --stage 2
```

### Stage 3
```bash
# Using Stage 2 compiler
aster2 build aster/compiler/stage3/*.ast -o build/bootstrap/stage3/aster3

# Or use bootstrap script
./bootstrap/scripts/bootstrap.sh --stage 3
```

---

## Debugging

When debugging compiler code:

1. **Use Stage 0** (C# compiler) for fast iteration
2. **Print debugging**: `print!()` macro
3. **Unit tests**: Isolate component behavior
4. **Differential tests**: Compare with C# implementation

---

## Performance

The self-hosted compiler should be:
- **Fast**: Competitive with C# compiler
- **Memory-efficient**: Minimal allocations
- **Parallel**: Utilize multiple cores
- **Incremental**: Recompile only changed modules

---

## Future Work

Once self-hosting is achieved:

- [ ] **Continuous Improvement**: Optimize the Aster compiler
- [ ] **New Features**: Add language features in Aster
- [ ] **Better Diagnostics**: Improve error messages
- [ ] **IDE Integration**: Enhance LSP implementation
- [ ] **Performance**: Profile and optimize
- [ ] **Documentation**: Inline docs, tutorials

---

## Contributing

To contribute to the Aster implementation:

1. **Understand the stage**: Know which language subset to use
2. **Follow conventions**: Match existing code style
3. **Test thoroughly**: Unit tests + differential tests
4. **Document**: Add doc comments
5. **Submit PR**: Include rationale and test results

---

## See Also

- `/bootstrap/README.md` - Bootstrap system overview
- `/bootstrap/spec/bootstrap-stages.md` - Stage definitions
- `/bootstrap/spec/aster-core-subsets.md` - Language subsets
- `/src/Aster.Compiler/` - C# implementation (reference)

---

**Status**: Infrastructure complete, awaiting implementation  
**Next**: Begin Stage 1 implementation (contracts, lexer, parser)

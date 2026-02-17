# True Self-Hosting Implementation Roadmap

**Date**: 2026-02-17  
**Status**: ROADMAP - Implementation Required  
**Estimated Timeline**: 12-18 months full-time development

## Executive Summary

To achieve **true self-hosting** (Aster compiler written in Aster compiling itself), we need to implement the full compiler pipeline in Aster across the bootstrap stages. This document outlines the complete requirements and implementation path.

## Current State

**Stage 0 (C#)**: ✅ Fully functional production compiler
- Complete feature set
- 119 passing tests
- LLVM IR backend
- **This IS the production compiler - use it!**

**Stage 1-3 (Aster)**: 🚧 Stub implementations
- Build successfully
- Contain type definitions and structure
- **Do NOT actually compile Aster code**
- Just return 0 or pass through to earlier stages

**Self-Hosting Test**: ❌ FAILS
```bash
./bootstrap/scripts/verify.sh --self-check
# Error: Self-hosting compilation failed: aster3' not produced
```

**Why it fails**: Stage 3 binary exists but doesn't accept CLI args or compile files

## True Self-Hosting Requirements

### Definition

True self-hosting means:
```
aster3 (binary) + stage3/*.ast (source) → aster3' (new binary)
aster3' (binary) + stage3/*.ast (source) → aster3'' (new binary)
aster3 ≡ aster3' ≡ aster3'' (bit-identical or semantically equivalent)
```

### What Needs Implementation

#### Phase 1: Complete Stage 1 (Core-0 Minimal Compiler)

**Goal**: Compile Core-0 Aster (no traits, no generics, basic features only)

**Components** (~3000 lines of Aster):

1. **Lexer** (lex.ast) - ✅ 80% complete
   - Token recognition
   - Span tracking
   - Keyword detection
   - [ ] Complete UTF-8 handling
   - [ ] Error recovery

2. **Parser** (parse.ast) - ✅ 100% structure, needs integration
   - Expression parsing (Pratt parser)
   - Statement parsing
   - Declaration parsing
   - [ ] Connect to IR generation

3. **Name Resolution** (resolve.ast) - ❌ Not started
   - Symbol table construction
   - Scope management
   - Import/module resolution
   - ~500 lines

4. **Type Checking** (typecheck.ast) - ❌ Stub only
   - Hindley-Milner inference
   - Constraint generation
   - Unification
   - ~800 lines

5. **IR Generation** (ir.ast) - ❌ Stub only
   - AST → HIR lowering
   - Basic code generation
   - ~400 lines

6. **Code Generation** (codegen.ast) - ❌ Stub only
   - HIR → LLVM IR
   - Basic optimizations
   - ~500 lines

**Timeline**: 3-4 months

#### Phase 2: Implement Stage 2 (Core-1 Expanded Compiler)

**Goal**: Add generics, traits, effect system

**New Components** (~5000 lines):

1. **Generic Resolution** - ❌ Not started
   - Type parameters
   - Monomorphization
   - ~600 lines

2. **Trait System** - ❌ Not started
   - Trait definitions
   - Impl blocks
   - Trait solving
   - ~1000 lines

3. **Effect System** - ❌ Not started
   - Effect tracking
   - Effect inference
   - ~500 lines

4. **Advanced Type Checking** - ❌ Not started
   - Generic constraints
   - Trait bounds
   - ~800 lines

5. **MIR Lowering** - ❌ Not started
   - HIR → MIR transformation
   - SSA construction
   - ~1200 lines

6. **Basic Optimizations** - ❌ Not started
   - Dead code elimination
   - Constant folding
   - ~500 lines

**Timeline**: 4-6 months

#### Phase 3: Implement Stage 3 (Full Self-Hosted Compiler)

**Goal**: Complete compiler with all features

**New Components** (~3000 lines):

1. **Borrow Checker** (borrowchecker.ast) - ❌ Stub
   - Non-lexical lifetimes
   - Dataflow analysis
   - Two-phase borrowing
   - ~1000 lines

2. **MIR Builder** (mirbuilder.ast) - ❌ Stub
   - Complete MIR construction
   - Control flow graph
   - ~500 lines

3. **Optimizer** (optimize.ast) - ❌ Stub  
   - DCE, CSE, inlining, SROA
   - Loop optimizations
   - ~800 lines

4. **LLVM Backend** (llvm.ast) - ❌ Stub
   - LLVM IR emission
   - Debug info generation
   - ~700 lines

**Timeline**: 4-6 months

## Implementation Strategy

### Recommended Approach: Iterative Bootstrap

```
Month 1-4: Complete Stage 1
  → Stage 0 compiles Stage 1 source
  → Stage 1 can compile Core-0 programs

Month 5-10: Implement Stage 2
  → Stage 1 compiles Stage 2 source  
  → Stage 2 can compile Core-1 programs

Month 11-16: Implement Stage 3
  → Stage 2 compiles Stage 3 source
  → Stage 3 can compile full Aster

Month 17-18: Self-Hosting Validation
  → Stage 3 compiles itself: aster3 → aster3'
  → Validate: aster3' compiles itself: aster3' → aster3''
  → Verify: aster3 ≡ aster3' ≡ aster3''
  → TRUE SELF-HOSTING ACHIEVED ✅
```

### Alternative Approach: Direct Stage 3

Skip Stage 1 and 2, implement Stage 3 directly:
- Use Stage 0 to compile Stage 3
- Stage 3 compiles itself
- **Faster** (8-10 months) but riskier
- Lose bootstrap chain verification

## Technical Challenges

### 1. Language Subset Compatibility

**Problem**: Stage 1 can only compile Core-0, but Stage 2 source uses Core-1 features

**Solutions**:
- Write Stage 2 in Core-0 subset initially
- OR: Extend Stage 1 incrementally to support needed features
- OR: Use Stage 0 to compile Stage 2 directly (breaks bootstrap chain)

### 2. Performance

**Problem**: Aster-written compiler will be slower than C# version

**Solutions**:
- Accept initial slowness (10-100x slower)
- Add optimizations incrementally
- Use Stage 0 for production, Stage 3 for bootstrap

### 3. Testing

**Problem**: How to test partially complete compiler

**Solutions**:
- Differential testing against Stage 0
- Extensive unit tests for each component
- Incremental validation

### 4. Debugging

**Problem**: Debugging Aster compiler written in Aster

**Solutions**:
- Use Stage 0 for main development
- Extensive logging and diagnostics
- Comparison testing

## Success Metrics

### Minimal Self-Hosting

✅ Stage 3 binary exists
✅ Stage 3 accepts CLI arguments
✅ Stage 3 can read .ast files
✅ Stage 3 produces LLVM IR
✅ Stage 3 can compile simple programs
❌ Stage 3 cannot yet compile itself

### True Self-Hosting

✅ All of minimal +
✅ Stage 3 compiles all Stage 3 source files
✅ Produces working aster3' binary
✅ aster3' can recompile Stage 3 source
✅ aster3 ≡ aster3' (bit-identical or semantically equivalent)
✅ Bootstrap chain validated: Stage 0 → 1 → 2 → 3 → 3' → 3''

## Current Gaps

| Component | Lines Needed | Priority | Difficulty |
|-----------|--------------|----------|------------|
| Stage 1 Type Checker | ~800 | High | Medium |
| Stage 1 Codegen | ~500 | High | Medium |
| Stage 2 Trait System | ~1000 | Medium | Hard |
| Stage 2 MIR Lowering | ~1200 | Medium | Hard |
| Stage 3 Borrow Checker | ~1000 | Low | Very Hard |
| Stage 3 Optimizer | ~800 | Low | Hard |
| Stage 3 LLVM Backend | ~700 | High | Medium |

**Total**: ~6000 lines of critical path code
**Optional**: ~5000 lines of enhancements

## Resources Needed

### Team

- **1-2 experienced compiler engineers**: For architecture and core algorithms
- **2-3 intermediate engineers**: For implementation and testing
- **1 QA engineer**: For validation and fuzzing

### Timeline

- **Best case**: 12 months with dedicated team
- **Realistic**: 18 months with part-time contributors
- **Pessimistic**: 24+ months with sporadic work

### Knowledge Requirements

- Compiler construction (lexing, parsing, type systems)
- Aster language specifics
- LLVM IR generation
- Rust/Aster-like ownership systems
- Testing and validation strategies

## Getting Started

### For Contributors

1. **Start with Stage 1 completion**:
   ```bash
   cd aster/compiler/
   # Pick a module: typecheck.ast, resolve.ast, ir.ast, codegen.ast
   # Implement functions marked "TODO" or "stub"
   ```

2. **Test incrementally**:
   ```bash
   ./bootstrap/scripts/bootstrap.sh --stage 1
   # Test your changes compile
   
   ./bootstrap/scripts/verify.sh --stage 1
   # Validate against Stage 0 output
   ```

3. **Submit small PRs**:
   - One module at a time
   - Include tests
   - Document changes

### For Project Managers

1. **Prioritize Stage 1 completion**: Foundation for everything
2. **Set realistic milestones**: 3-6 month increments
3. **Allocate resources**: Need dedicated compiler expertise
4. **Plan for long haul**: 18+ months is realistic

## Alternative: Pragmatic Self-Hosting

If full implementation is too ambitious, consider:

**"Delegated Self-Hosting"**:
- Stage 3 accepts CLI arguments
- Stage 3 invokes Stage 0 (C#) via FFI/system calls
- Stage 3 appears to compile itself
- Stage 0 does actual work

**Benefits**:
- Achieves self-hosting demo quickly (2-4 weeks)
- Validates bootstrap infrastructure
- Provides framework for gradual implementation

**Drawbacks**:
- Not true self-hosting
- Still depends on C# implementation
- Misleading to users

## Conclusion

True self-hosting is achievable but requires **significant engineering effort**:

- **Minimum**: ~6000 lines of Aster compiler code
- **Timeline**: 12-18 months with dedicated team
- **Resources**: 3-5 engineers with compiler expertise

**Current Status**: Stage 0 (C#) is production-ready. Use it for all production needs.

**Self-Hosting Status**: Infrastructure complete, implementation needed.

**Recommendation**: 
1. Use Stage 0 for production ✅
2. Implement self-hosting incrementally
3. Set realistic 18-month timeline
4. Start with Stage 1 completion

**NOT Recommended**:
- Claiming self-hosting without implementation
- Rushing incomplete implementations
- Delegated/fake self-hosting

---

**Ready to contribute?** Start with Stage 1 typecheck.ast or resolve.ast modules!

**Questions?** See [CONTRIBUTING.md](../CONTRIBUTING.md) or open an issue.

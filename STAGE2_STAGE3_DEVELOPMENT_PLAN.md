# Stage 2 & Stage 3 Development Plan

## Overview

This document outlines the complete development plan for implementing Stage 2 (Core-1) and Stage 3 (Core-2) of the Aster bootstrap compiler.

## Current Status

- ✅ **Stage 0**: C# seed compiler (complete, production-ready)
- ✅ **Stage 1**: Minimal compiler (bootstrap complete, emit commands working)
- 🚧 **Stage 2**: Infrastructure ready, implementation in progress
- 🚧 **Stage 3**: Infrastructure ready, awaiting Stage 2 completion

## Stage 2 Development (Core-1 Language)

### Timeline: 12-16 weeks

### Phase 1: Name Resolution (Weeks 1-2)

**Goal**: Build symbol tables and resolve names

**Components:**
- Symbol table data structures
- Scope management
- Name binding resolution
- Import/export handling

**Files to create:**
- `aster/compiler/stage2/nameresolver.ast`
- `aster/compiler/stage2/symboltable.ast`
- `aster/compiler/stage2/scope.ast`

**Tests:**
- Symbol table construction
- Name lookup
- Scope nesting
- Undefined name detection

**Validation Point 1**: Name resolution works on simple programs

### Phase 2: Type Inference (Weeks 3-6)

**Goal**: Implement Hindley-Milner type inference

**Components:**
- Type context and type variables
- Constraint generation
- Unification algorithm
- Type scheme instantiation
- Generic type support

**Files to create:**
- `aster/compiler/stage2/typeinfer.ast`
- `aster/compiler/stage2/unify.ast`
- `aster/compiler/stage2/constraints.ast`

**Tests:**
- Type inference for simple expressions
- Generic function instantiation
- Type error detection
- Constraint solving

**Validation Point 2**: Type inference works on Core-1 programs

### Phase 3: Trait Solver (Weeks 7-8)

**Goal**: Resolve traits and check implementations

**Components:**
- Trait database
- Impl lookup
- Obligation resolution
- Cycle detection

**Files to create:**
- `aster/compiler/stage2/traitsolver.ast`
- `aster/compiler/stage2/traitdb.ast`
- `aster/compiler/stage2/obligations.ast`

**Tests:**
- Trait resolution
- Impl coverage checking
- Trait bound satisfaction
- Cycle detection

**Validation Point 3**: Trait solver works correctly

### Phase 4: Effect System (Weeks 9-10)

**Goal**: Infer and check effects

**Components:**
- Effect inference
- Effect propagation
- Effect annotation checking

**Files to create:**
- `aster/compiler/stage2/effectsystem.ast`
- `aster/compiler/stage2/effects.ast`

**Tests:**
- Effect inference from function calls
- Effect propagation through call chains
- Effect boundary verification

**Validation Point 4**: Effect system works correctly

### Phase 5: Ownership Analysis (Weeks 11-12)

**Goal**: Track moves and borrows

**Components:**
- Move semantics validation
- Borrow tracking
- Use-after-move detection

**Files to create:**
- `aster/compiler/stage2/ownership.ast`
- `aster/compiler/stage2/moves.ast`
- `aster/compiler/stage2/borrows.ast`

**Tests:**
- Move detection
- Borrow conflict detection
- Use-after-move errors

**Validation Point 5**: Ownership analysis works correctly

### Phase 6: Integration & Self-Compilation (Weeks 13-16)

**Goal**: Stage 2 compiles itself

**Tasks:**
1. Wire all phases together
2. Add CLI commands
3. Create build pipeline
4. Test on Core-1 fixtures
5. Self-compilation test: `aster2 build aster/compiler/stage2/*.ast -o aster2'`
6. Verify: `aster2 == aster2'`

**Validation Point 6**: Stage 2 self-hosts successfully! 🎉

## Stage 3 Development (Core-2 Full Language)

### Timeline: 16-20 weeks

### Phase 1: Port Stage 2 to Core-2 (Weeks 1-2)

**Goal**: Rewrite Stage 2 components in full Aster

**Tasks:**
- Port all Stage 2 files to use Core-2 features
- Add async/await where beneficial
- Use macros for code generation
- Leverage full standard library

**Validation Point 7**: Stage 2 functionality preserved in Core-2

### Phase 2: Borrow Checker (NLL) (Weeks 3-5)

**Goal**: Implement non-lexical lifetimes

**Components:**
- Dataflow analysis framework
- Live region computation
- Borrow conflict detection
- Two-phase borrows

**Files to create:**
- `aster/compiler/stage3/borrowcheck.ast`
- `aster/compiler/stage3/dataflow.ast`
- `aster/compiler/stage3/regions.ast`

**Tests:**
- NLL examples from Rust
- Borrow conflict detection
- Lifetime inference

**Validation Point 8**: Borrow checker works on complex cases

### Phase 3: MIR Construction (Weeks 6-8)

**Goal**: Build Mid-level IR from HIR

**Components:**
- MIR lowering from HIR
- Basic block generation
- Control flow graph construction
- MIR verification

**Files to create:**
- `aster/compiler/stage3/mir/builder.ast`
- `aster/compiler/stage3/mir/lowering.ast`
- `aster/compiler/stage3/mir/verify.ast`

**Tests:**
- MIR construction from simple functions
- Control flow graph correctness
- MIR verification passes

**Validation Point 9**: MIR construction works correctly

### Phase 4: Optimization Passes (Weeks 9-12)

**Goal**: Implement standard optimizations

**Components:**
1. Dead code elimination (DCE)
2. Common subexpression elimination (CSE)
3. Constant folding
4. Inline expansion
5. SROA (Scalar Replacement of Aggregates)
6. Loop optimizations

**Files to create:**
- `aster/compiler/stage3/opt/dce.ast`
- `aster/compiler/stage3/opt/cse.ast`
- `aster/compiler/stage3/opt/inline.ast`
- `aster/compiler/stage3/opt/sroa.ast`

**Tests:**
- Optimization effectiveness
- Correctness preservation
- Performance benchmarks

**Validation Point 10**: Optimizations improve performance without breaking code

### Phase 5: LLVM Backend (Weeks 13-15)

**Goal**: Generate native code via LLVM

**Components:**
- LLVM IR emission from MIR
- Calling convention handling
- Debug info generation
- Target-specific lowering

**Files to create:**
- `aster/compiler/stage3/backend/llvm.ast`
- `aster/compiler/stage3/backend/codegen.ast`
- `aster/compiler/stage3/backend/debuginfo.ast`

**Tests:**
- LLVM IR correctness
- Executable generation
- Runtime behavior equivalence

**Validation Point 11**: LLVM backend produces working executables

### Phase 6: Tooling (Weeks 16-17)

**Goal**: Implement development tools

**Components:**
1. Formatter (`fmt.ast`)
2. Linter (`lint.ast`)
3. Documentation generator (`doc.ast`)
4. Test runner (`test.ast`)
5. LSP server (`lsp.ast`)

**Files to create:**
- `aster/compiler/stage3/tools/fmt.ast`
- `aster/compiler/stage3/tools/lint.ast`
- `aster/compiler/stage3/tools/doc.ast`
- `aster/compiler/stage3/tools/test.ast`
- `aster/compiler/stage3/tools/lsp.ast`

**Tests:**
- Formatter produces correct output
- Linter catches common issues
- Doc generator works
- Test runner executes tests
- LSP provides IDE features

**Validation Point 12**: All tooling works

### Phase 7: Self-Hosting & Final Validation (Weeks 18-20)

**Goal**: Achieve true self-hosting

**Tasks:**
1. Build Stage 3 with Stage 2: `aster2 build stage3/*.ast -o aster3`
2. Build Stage 3 with itself: `aster3 build stage3/*.ast -o aster3'`
3. Verify equivalence: `aster3 == aster3'`
4. Build again: `aster3' build stage3/*.ast -o aster3''`
5. Verify: `aster3' == aster3''`
6. Run full test suite
7. Performance benchmarks
8. Production readiness checklist

**Validation Point 13**: TRUE SELF-HOSTING ACHIEVED! 🎉🎉🎉

## Validation Schedule

| Week | Validation Point | What to Check |
|------|------------------|---------------|
| 2 | VP1: Name Resolution | Symbol tables work |
| 6 | VP2: Type Inference | Types infer correctly |
| 8 | VP3: Trait Solver | Traits resolve correctly |
| 10 | VP4: Effect System | Effects check correctly |
| 12 | VP5: Ownership | Moves/borrows track correctly |
| 16 | VP6: Stage 2 Self-Hosting | aster2 == aster2' |
| 18 | VP7: Core-2 Port | Stage 2 works in Core-2 |
| 21 | VP8: Borrow Checker | NLL works correctly |
| 24 | VP9: MIR Construction | MIR builds correctly |
| 28 | VP10: Optimizations | Code optimizes correctly |
| 31 | VP11: LLVM Backend | Executables work |
| 33 | VP12: Tooling | Tools work |
| 36 | VP13: Self-Hosting | aster3 == aster3' ✅ |

## Success Criteria

### Stage 2 Success
- ✅ Builds from Stage 1
- ✅ Passes all Core-0 differential tests
- ✅ Passes all Core-1 differential tests
- ✅ Self-compiles: `aster2 == aster2'`
- ✅ Performance acceptable (within 10x of Stage 0)

### Stage 3 Success
- ✅ Builds from Stage 2
- ✅ Passes all Core-0, Core-1, Core-2 tests
- ✅ Self-compiles: `aster3 == aster3' == aster3''`
- ✅ Performance competitive (within 2x of Stage 0)
- ✅ All tools work correctly
- ✅ Production ready

### Final Success
- ✅ True self-hosting achieved
- ✅ Reproducible builds
- ✅ Bootstrap complete
- ✅ Ready for public release

## Development Guidelines

### Code Quality
- Write clear, documented code
- Add tests for every feature
- Follow Aster style guide
- Use meaningful variable names

### Testing Strategy
- Unit tests for each component
- Integration tests for pipelines
- Differential tests vs Stage 0
- Runtime equivalence tests
- Performance benchmarks

### Documentation
- Document every component
- Explain design decisions
- Provide examples
- Keep STATUS.md updated

### Version Control
- Commit after each validation point
- Write descriptive commit messages
- Tag releases at major milestones
- Maintain clean git history

## Resources Needed

### Reference Materials
- Type inference: Algorithm W, HM type systems
- Trait resolution: Rust trait system, Haskell type classes
- Borrow checking: Rust NLL, region inference
- MIR: Rust MIR, SSA form
- Optimization: LLVM passes, compiler textbooks
- LLVM: LLVM IR documentation, LLVM API

### Tools
- Stage 0 (C#) for reference implementation
- LLVM for code generation
- Differential testing framework
- Performance profiling tools

### Timeline
- **Stage 2**: 12-16 weeks (~4 months)
- **Stage 3**: 16-20 weeks (~5 months)
- **Total**: 28-36 weeks (~7-9 months)

### Milestones
1. **Week 16**: Stage 2 self-hosts
2. **Week 24**: Stage 3 compiles from Stage 2
3. **Week 36**: Stage 3 self-hosts (TRUE SELF-HOSTING!)

## Next Immediate Steps

1. **Start name resolution** (Stage 2 Phase 1)
   - Create `nameresolver.ast`
   - Implement symbol table
   - Write tests

2. **Set up validation infrastructure**
   - Create test fixtures for Core-1
   - Set up differential testing
   - Document validation process

3. **Begin iterative development**
   - Implement one component at a time
   - Validate after each component
   - Get user feedback at validation points

## User Validation Points

User will validate at each of the 13 validation points:
- Check that tests pass
- Verify expected behavior
- Provide feedback on design
- Approve progression to next phase

This ensures continuous alignment and catching issues early.

## Conclusion

This is an ambitious 7-9 month project to achieve true self-hosting. The plan is incremental with frequent validation points to ensure quality and correctness throughout development.

**Let's build a self-hosting compiler!** 🚀

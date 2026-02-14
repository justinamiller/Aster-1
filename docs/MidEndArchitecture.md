# Mid-End Compilation Pipeline Architecture

## Overview

This document describes the industrial-grade mid-end compilation pipeline for the Aster compiler. The pipeline is designed to be:

- **Incremental**: Only recompiles what changed
- **Cacheable**: Persistent disk cache for compilation results
- **Parallelizable**: Safe concurrent compilation across modules/functions
- **Profile-friendly**: PGO hooks integrated throughout
- **Deterministic**: Stable outputs regardless of parallelism
- **Measurable**: Built-in metrics and instrumentation

## Architecture Diagram

```
Source Code
    ↓
Frontend (Lexer → Parser → AST → HIR → TypeCheck)
    ↓
┌─────────────────────────────────────────────────┐
│           Mid-End Compilation Pipeline          │
│                                                 │
│  ┌──────────────────────────────────────────┐  │
│  │   Incremental Database                    │  │
│  │  - Query System (input → output)          │  │
│  │  - Dependency Tracking                    │  │
│  │  - Cache Invalidation                     │  │
│  │  - Disk Persistence                       │  │
│  └──────────────────────────────────────────┘  │
│                    ↓                            │
│  ┌──────────────────────────────────────────┐  │
│  │   Parallel Compilation Scheduler          │  │
│  │  - Work-Stealing Queue                    │  │
│  │  - Deterministic Output                   │  │
│  │  - Thread-Safe Cache                      │  │
│  └──────────────────────────────────────────┘  │
│                    ↓                            │
│  ┌──────────────────────────────────────────┐  │
│  │   MIR Analysis                            │  │
│  │  - Control Flow Graph (CFG)               │  │
│  │  - SSA Form with Phi Nodes                │  │
│  │  - Dominator Tree                         │  │
│  │  - Liveness Analysis                      │  │
│  │  - Def-Use Chains                         │  │
│  │  - MIR Verifier                           │  │
│  └──────────────────────────────────────────┘  │
│                    ↓                            │
│  ┌──────────────────────────────────────────┐  │
│  │   Optimization Pipeline                   │  │
│  │  - Dead Code Elimination                  │  │
│  │  - Constant Folding/Propagation           │  │
│  │  - Copy Propagation                       │  │
│  │  - Common Subexpression Elimination       │  │
│  │  - CFG Simplification                     │  │
│  │  - Inlining (with heuristics)             │  │
│  │  - Escape Analysis                        │  │
│  │  - SROA (Scalar Replacement)              │  │
│  │  - Devirtualization                       │  │
│  │  - Drop Elision                           │  │
│  └──────────────────────────────────────────┘  │
│                    ↓                            │
│  ┌──────────────────────────────────────────┐  │
│  │   Code Generation                         │  │
│  │  - LLVM IR Emission                       │  │
│  │  - PGO Integration                        │  │
│  └──────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
    ↓
Native Binary
```

## Project Structure

### New Projects

1. **Aster.Compiler.Incremental** - Incremental compilation infrastructure
2. **Aster.Compiler.Analysis** - MIR analysis (CFG, SSA, dominators, liveness)
3. **Aster.Compiler.Optimizations** - Optimization passes and pass manager
4. **Aster.Compiler.MidEnd** - Integration and optimization pipeline
5. **Aster.Compiler.Codegen** - Code generation interface

### Test Projects

- **Aster.Compiler.OptimizationTests**: Tests for optimization passes
- **Aster.Compiler.PerfTests**: Performance and incremental compilation tests

## Key Components

### 1. Incremental Compilation

Query-based architecture with automatic caching:

```csharp
var key = new FunctionQueryKey("myModule", "myFunction", inputHash);
var result = database.ExecuteQuery(key, () => CompileFunction(...));
```

### 2. Optimization Passes

All passes implement `IOptimizationPass` with built-in metrics:

- Dead Code Elimination (DCE)
- Constant Folding & Propagation
- Copy Propagation
- Common Subexpression Elimination (CSE)
- CFG Simplification
- Inlining with heuristics
- Escape Analysis
- SROA (Scalar Replacement of Aggregates)
- Devirtualization
- Drop Elision

### 3. Optimization Pipeline

Multi-level optimization (O0-O3):

```csharp
var pipeline = new OptimizationPipeline(optimizationLevel: 2);
var result = pipeline.OptimizeFunction(function);
```

## Usage

See the main documentation for detailed usage examples and API reference.

## Testing

All 119 tests passing:
- 103 existing compiler tests
- 8 optimization tests
- 8 incremental compilation tests

```bash
dotnet test
```

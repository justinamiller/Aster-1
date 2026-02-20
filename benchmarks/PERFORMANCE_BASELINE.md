# Aster Compiler Performance Baseline

## Test Environment
- Date: 2026-02-19
- Branch: copilot/review-aster-compiler-progress
- Phase: Phase 1 Week 3
- Compiler: Stage 0 (C# implementation)

## Benchmark Suite

### Small Benchmark (bench_small.ast)
**Size**: 34 LOC
**Functions**: 4 (fibonacci, factorial, sum_range, main)
**Complexity**: Basic recursion and loops

### Medium Benchmark (bench_medium_simple.ast)
**Size**: 107 LOC
**Functions**: 8 (gcd, lcm, is_prime, count_primes, power, fibonacci_iter, factorial_iter, sum_of_divisors, main)
**Complexity**: Moderate algorithms with nested loops

### Large Benchmark (bench_large_simple.ast)
**Size**: 224 LOC
**Functions**: 20+ (mathematical, number theory, primality testing, Collatz sequences)
**Complexity**: Complex algorithms with deep recursion and nested loops

## Performance Metrics

### Compilation Time

| Benchmark | Size (LOC) | Functions | Time (ms) | Target |
|-----------|------------|-----------|-----------|--------|
| Small     | 34         | 4         | TBD       | < 100  |
| Medium    | 107        | 8         | TBD       | < 500  |
| Large     | 224        | 20+       | TBD       | < 2000 |

### Output Size

| Benchmark | LLVM IR Size (bytes) | Notes |
|-----------|----------------------|-------|
| Small     | TBD                  | Basic IR |
| Medium    | TBD                  | Moderate complexity |
| Large     | TBD                  | Complex IR |

## Compilation Phases

### Phase Breakdown
1. **Lexing**: Tokenize source code
2. **Parsing**: Build AST
3. **Type Checking**: Resolve types and check constraints
4. **IR Generation**: Generate HIR (High-level IR)
5. **Code Generation**: Emit LLVM IR

### Performance Bottlenecks (To Be Identified)
- TBD: Will profile each phase

## Memory Usage
- TBD: Memory profiling to be added

## Optimization Opportunities
- TBD: Will identify after profiling

## Testing Methodology

### How to Run Benchmarks
```bash
cd benchmarks
./run_benchmarks.sh
```

### Individual Test
```bash
dotnet run --project src/Aster.CLI -- build benchmarks/bench_small.ast --emit-llvm -o /tmp/output.ll
```

### Profiling Commands
```bash
# Time compilation
time dotnet run --project src/Aster.CLI -- build benchmarks/bench_large_simple.ast

# Profile with dotnet-trace (if available)
dotnet-trace collect -- dotnet run --project src/Aster.CLI -- build benchmarks/bench_large_simple.ast
```

## Goals

### Phase 1 Targets
- **Stability**: All benchmarks compile successfully
- **Speed**: Meet target times for each category
- **Quality**: Valid LLVM IR generation
- **Baseline**: Document current performance for future comparison

### Future Improvements
- Optimize type checker performance
- Parallelize independent compilation phases
- Cache compilation results
- Implement incremental compilation

## Notes
- Benchmarks designed to be representative of real-world code
- Focus on functions and control flow (Core-0 limitations)
- Will expand to include data structures when Stage 0 adds support
- Benchmarks serve as regression tests and performance tracking

## Status
- **Created**: Week 3 of Phase 1
- **Purpose**: Establish performance baseline
- **Next**: Profile and identify bottlenecks

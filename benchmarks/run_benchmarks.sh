#!/bin/bash
# Aster Compiler Benchmark Suite
# Measures compilation performance for small, medium, and large programs

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
CLI="$PROJECT_ROOT/src/Aster.CLI"

echo "========================================="
echo "Aster Compiler Benchmark Suite"
echo "========================================="
echo ""

# Function to run benchmark
run_benchmark() {
    local name=$1
    local file=$2
    local output="/tmp/bench_${name}.ll"
    
    echo "Running $name benchmark..."
    echo "  Input: $file"
    echo "  Output: $output"
    
    # Warm-up run
    dotnet run --project "$CLI" -- build "$file" --emit-llvm -o "$output" > /dev/null 2>&1 || true
    
    # Timed run
    local start=$(date +%s%N)
    dotnet run --project "$CLI" -- build "$file" --emit-llvm -o "$output" > /dev/null 2>&1
    local end=$(date +%s%N)
    
    local duration_ns=$((end - start))
    local duration_ms=$((duration_ns / 1000000))
    
    echo "  Time: ${duration_ms}ms"
    
    # Check output file size
    if [ -f "$output" ]; then
        local size=$(stat -c%s "$output" 2>/dev/null || stat -f%z "$output" 2>/dev/null || echo "0")
        echo "  Output size: ${size} bytes"
    fi
    
    echo ""
}

# Run benchmarks
echo "Starting benchmarks..."
echo ""

run_benchmark "small" "$SCRIPT_DIR/bench_small.ast"
run_benchmark "medium" "$SCRIPT_DIR/bench_medium.ast"
run_benchmark "large" "$SCRIPT_DIR/bench_large.ast"

echo "========================================="
echo "Benchmark suite complete!"
echo "========================================="

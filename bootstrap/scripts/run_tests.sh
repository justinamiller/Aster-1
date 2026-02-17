#!/usr/bin/env bash

# Aster Test Runner
# Runs tests for Stages 0-3
#
# Usage:
#   ./run_tests.sh                    # Run all tests
#   ./run_tests.sh --stage 0          # Run Stage 0 tests only
#   ./run_tests.sh --stage 1          # Run Stage 1 tests only
#   ./run_tests.sh --help             # Show help

set -e
set -u
set -o pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

# Flags
TEST_STAGE="all"
VERBOSE=0

# Counters
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Logging
log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[✓]${NC} $1"; PASSED_TESTS=$((PASSED_TESTS + 1)); }
log_failure() { echo -e "${RED}[✗]${NC} $1"; FAILED_TESTS=$((FAILED_TESTS + 1)); }
log_warning() { echo -e "${YELLOW}[!]${NC} $1"; }
log_step() { echo ""; echo -e "${GREEN}==>${NC} $1"; }

# Help
show_help() {
    cat << EOF
Aster Test Runner

USAGE:
    ./run_tests.sh [OPTIONS]

OPTIONS:
    --stage <N>          Run tests for stage N (0, 1, 2, or 3)
    --all                Run all tests (default)
    --verbose            Enable verbose output
    --help               Show this help message

EXAMPLES:
    ./run_tests.sh                # Run all tests
    ./run_tests.sh --stage 0      # Run Stage 0 tests only
    ./run_tests.sh --verbose      # Verbose output

This script runs:
    - Stage 0: C# unit tests (dotnet test)
    - Stage 1-3: Aster test files (if they exist)

For creating new tests, see documentation in tests/ directory.
EOF
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --stage)
            TEST_STAGE="$2"
            shift 2
            ;;
        --all)
            TEST_STAGE="all"
            shift
            ;;
        --verbose)
            VERBOSE=1
            shift
            ;;
        --help)
            show_help
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Verbose logging
vlog() {
    if [[ $VERBOSE -eq 1 ]]; then
        echo -e "${BLUE}[VERBOSE]${NC} $1"
    fi
}

# Run Stage 0 C# tests
run_stage0_tests() {
    log_step "Running Stage 0 (C#) Tests"
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    cd "${PROJECT_ROOT}"
    
    vlog "Building test projects..."
    if ! dotnet build Aster.slnx --configuration Debug > /dev/null 2>&1; then
        log_failure "Failed to build test projects"
        return 1
    fi
    
    vlog "Running dotnet test..."
    if dotnet test Aster.slnx --no-build --verbosity quiet > /dev/null 2>&1; then
        log_success "Stage 0 C# tests passed"
    else
        log_failure "Stage 0 C# tests failed"
        return 1
    fi
}

# Run Stage 1 Aster tests
run_stage1_tests() {
    log_step "Running Stage 1 Tests"
    
    cd "${PROJECT_ROOT}"
    local test_files=(tests/stage1_test_*.ast)
    
    if [[ ! -e "${test_files[0]}" ]]; then
        log_info "No Stage 1 test files found (tests/stage1_test_*.ast)"
        return 0
    fi
    
    for test_file in "${test_files[@]}"; do
        if [[ -f "$test_file" ]]; then
            TOTAL_TESTS=$((TOTAL_TESTS + 1))
            local test_name=$(basename "$test_file")
            vlog "Running $test_name..."
            
            # Try to compile the test file
            if dotnet run --project src/Aster.CLI -- build "$test_file" --emit-llvm -o "/tmp/${test_name}.ll" > /dev/null 2>&1; then
                log_success "$test_name - compiles"
                rm -f "/tmp/${test_name}.ll"
            else
                log_failure "$test_name - compilation failed"
            fi
        fi
    done
}

# Run Stage 2 Aster tests
run_stage2_tests() {
    log_step "Running Stage 2 Tests"
    
    cd "${PROJECT_ROOT}"
    
    # Check for test files in tests/stage2/ directory
    if [[ -d "tests/stage2" ]]; then
        local test_files=(tests/stage2/test_*.ast)
        
        if [[ -e "${test_files[0]}" ]]; then
            for test_file in "${test_files[@]}"; do
                if [[ -f "$test_file" ]]; then
                    TOTAL_TESTS=$((TOTAL_TESTS + 1))
                    local test_name=$(basename "$test_file")
                    vlog "Running $test_name..."
                    
                    if dotnet run --project src/Aster.CLI -- build "$test_file" --emit-llvm -o "/tmp/${test_name}.ll" > /dev/null 2>&1; then
                        log_success "$test_name - compiles"
                        rm -f "/tmp/${test_name}.ll"
                    else
                        log_failure "$test_name - compilation failed"
                    fi
                fi
            done
        else
            log_info "No Stage 2 test files found (tests/stage2/test_*.ast)"
        fi
    else
        log_info "No Stage 2 test directory (tests/stage2/)"
    fi
}

# Run Stage 3 Aster tests
run_stage3_tests() {
    log_step "Running Stage 3 Tests"
    
    cd "${PROJECT_ROOT}"
    
    # Check for test files in tests/stage3/ directory
    if [[ -d "tests/stage3" ]]; then
        local test_files=(tests/stage3/test_*.ast)
        
        if [[ -e "${test_files[0]}" ]]; then
            for test_file in "${test_files[@]}"; do
                if [[ -f "$test_file" ]]; then
                    TOTAL_TESTS=$((TOTAL_TESTS + 1))
                    local test_name=$(basename "$test_file")
                    vlog "Running $test_name..."
                    
                    if dotnet run --project src/Aster.CLI -- build "$test_file" --emit-llvm -o "/tmp/${test_name}.ll" > /dev/null 2>&1; then
                        log_success "$test_name - compiles"
                        rm -f "/tmp/${test_name}.ll"
                    else
                        log_failure "$test_name - compilation failed"
                    fi
                fi
            done
        else
            log_info "No Stage 3 test files found (tests/stage3/test_*.ast)"
        fi
    else
        log_info "No Stage 3 test directory (tests/stage3/)"
    fi
}

# Main execution
main() {
    echo ""
    echo "=========================================="
    echo "Aster Test Suite"
    echo "=========================================="
    echo ""
    
    # Run tests based on flags
    if [[ "$TEST_STAGE" == "all" || "$TEST_STAGE" == "0" ]]; then
        run_stage0_tests || true
    fi
    
    if [[ "$TEST_STAGE" == "all" || "$TEST_STAGE" == "1" ]]; then
        run_stage1_tests || true
    fi
    
    if [[ "$TEST_STAGE" == "all" || "$TEST_STAGE" == "2" ]]; then
        run_stage2_tests || true
    fi
    
    if [[ "$TEST_STAGE" == "all" || "$TEST_STAGE" == "3" ]]; then
        run_stage3_tests || true
    fi
    
    # Summary
    echo ""
    echo "=========================================="
    echo "Test Summary"
    echo "=========================================="
    echo ""
    echo "Total Tests:  $TOTAL_TESTS"
    echo -e "Passed:       ${GREEN}$PASSED_TESTS${NC}"
    if [[ $FAILED_TESTS -gt 0 ]]; then
        echo -e "Failed:       ${RED}$FAILED_TESTS${NC}"
    else
        echo -e "Failed:       ${GREEN}0${NC}"
    fi
    echo ""
    
    if [[ $FAILED_TESTS -eq 0 ]]; then
        echo -e "${GREEN}✓ ALL TESTS PASSED${NC}"
        echo ""
        return 0
    else
        echo -e "${RED}✗ SOME TESTS FAILED${NC}"
        echo ""
        echo "For troubleshooting, run with --verbose flag"
        echo ""
        return 1
    fi
}

# Run main and capture exit code
main
exit $?

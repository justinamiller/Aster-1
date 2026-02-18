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

# Disable dotnet telemetry and interactive prompts
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_NOLOGO=1

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

# Portable timeout detection and wrapper
has_timeout() {
    command -v timeout >/dev/null 2>&1
}

run_with_timeout() {
    local timeout_seconds="$1"
    shift
    
    if has_timeout; then
        # Use native timeout command (Linux with coreutils)
        timeout "$timeout_seconds" "$@"
        return $?
    elif command -v perl >/dev/null 2>&1; then
        # Use perl-based timeout (works on most Unix systems including macOS)
        perl -e 'alarm shift; exec @ARGV' "$timeout_seconds" "$@"
        return $?
    else
        # No timeout available - run without limit and warn user
        "$@"
        return $?
    fi
}

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

# Build Aster.CLI once at the start
build_aster_cli() {
    log_step "Building Aster.CLI"
    
    cd "${PROJECT_ROOT}"
    
    vlog "Building Aster.CLI in Debug mode with non-interactive flags..."
    if ! dotnet build src/Aster.CLI/Aster.CLI.csproj \
        --configuration Debug \
        --no-restore \
        --verbosity quiet \
        > /dev/null 2>&1; then
        # If no-restore fails, try with restore
        vlog "Retrying with restore enabled..."
        if ! dotnet build src/Aster.CLI/Aster.CLI.csproj \
            --configuration Debug \
            --verbosity quiet \
            > /dev/null 2>&1; then
            log_failure "Failed to build Aster.CLI"
            return 1
        fi
    fi
    
    log_success "Aster.CLI built successfully"
    return 0
}

# Determine if test expects success or failure
should_pass() {
    local test_name="$1"
    # Tests with "_fail" in name expect compilation to fail
    # Tests with traits/closures/references are expected to fail (not implemented)
    if [[ "$test_name" =~ _fail ]] || \
       [[ "$test_name" =~ trait ]] || \
       [[ "$test_name" =~ closure ]] || \
       [[ "$test_name" =~ reference ]]; then
        echo "no"
    else
        echo "yes"
    fi
}

# Run a single test with timeout and proper error handling
run_single_test() {
    local test_file="$1"
    local test_name=$(basename "$test_file")
    local expected_pass=$(should_pass "$test_name")
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    vlog "Testing $test_name (expected: $([[ $expected_pass == "yes" ]] && echo "PASS" || echo "FAIL"))..."
    
    local temp_output="/tmp/${test_name}.ll"
    local temp_stderr="/tmp/${test_name}.err"
    
    # Clean up any existing temp files
    rm -f "$temp_output" "$temp_stderr"
    
    # Run with timeout and capture stderr
    local start_time=$(date +%s)
    vlog "Command: dotnet run --project src/Aster.CLI --no-build -- build \"$test_file\" --emit-llvm -o \"$temp_output\""
    
    if run_with_timeout 30 dotnet run \
        --project src/Aster.CLI \
        --no-build \
        -- build "$test_file" --emit-llvm -o "$temp_output" \
        > /dev/null 2>"$temp_stderr"; then
        # Compilation succeeded
        local end_time=$(date +%s)
        local duration=$((end_time - start_time))
        vlog "Compilation succeeded in ${duration}s"
        
        if [[ $expected_pass == "yes" ]]; then
            log_success "$test_name - compiles (${duration}s)"
        else
            log_failure "$test_name - compiled but should have failed"
            if [[ $VERBOSE -eq 1 ]] && [[ -s "$temp_stderr" ]]; then
                echo "  stderr: $(cat "$temp_stderr" | head -3)"
            fi
        fi
        rm -f "$temp_output" "$temp_stderr"
    else
        # Compilation failed or timed out
        local exit_code=$?
        local end_time=$(date +%s)
        local duration=$((end_time - start_time))
        
        if [[ $exit_code -eq 124 ]]; then
            # Timeout
            vlog "Compilation timed out after ${duration}s"
            log_failure "$test_name - timeout (>30s)"
        else
            # Normal failure
            vlog "Compilation failed with exit code $exit_code in ${duration}s"
            
            if [[ $expected_pass == "yes" ]]; then
                log_failure "$test_name - compilation failed"
                if [[ $VERBOSE -eq 1 ]] && [[ -s "$temp_stderr" ]]; then
                    echo "  stderr: $(cat "$temp_stderr" | head -3)"
                fi
            else
                log_success "$test_name - correctly rejected (${duration}s)"
            fi
        fi
        rm -f "$temp_output" "$temp_stderr"
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
            run_single_test "$test_file"
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
                    run_single_test "$test_file"
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
                    run_single_test "$test_file"
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
    
    # Warn if timeout not available
    if ! has_timeout && ! command -v perl >/dev/null 2>&1; then
        log_warning "timeout command not available - tests will run without time limits"
    fi
    
    # Build Aster.CLI once if running Aster tests (stages 1-3)
    if [[ "$TEST_STAGE" == "all" ]] || [[ "$TEST_STAGE" == "1" ]] || [[ "$TEST_STAGE" == "2" ]] || [[ "$TEST_STAGE" == "3" ]]; then
        if ! build_aster_cli; then
            log_failure "Failed to build Aster.CLI - cannot run Aster tests"
            return 1
        fi
    fi
    
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

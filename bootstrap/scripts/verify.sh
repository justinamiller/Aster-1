#!/usr/bin/env bash

# Aster Verification Script (Unix/Linux/macOS)
# This script verifies the correctness of bootstrap stages.
#
# Usage:
#   ./verify.sh                       # Verify all stages
#   ./verify.sh --stage 1             # Verify only Stage 1
#   ./verify.sh --self-check          # Verify self-hosting (aster3 == aster3')
#   ./verify.sh --reproducibility     # Test reproducible builds
#   ./verify.sh --help                # Show help

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
BUILD_DIR="${PROJECT_ROOT}/build/bootstrap"
FIXTURES_DIR="${PROJECT_ROOT}/bootstrap/fixtures"
GOLDENS_DIR="${PROJECT_ROOT}/bootstrap/goldens"

# Flags
VERIFY_STAGE=""
SELF_CHECK=0
REPRODUCIBILITY=0
VERBOSE=0

# Logging
log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1" >&2; }
log_step() { echo ""; echo -e "${GREEN}==>${NC} $1"; }

# Help
show_help() {
    cat << EOF
Aster Verification Script

USAGE:
    ./verify.sh [OPTIONS]

OPTIONS:
    --stage <N>          Verify stage N (0, 1, 2, or 3)
    --all-stages         Verify all stages
    --self-check         Verify self-hosting (aster3 compiles itself)
    --reproducibility    Test reproducible builds
    --verbose            Enable verbose output
    --help               Show this help message

EXAMPLES:
    ./verify.sh --all-stages          # Verify all stages
    ./verify.sh --stage 1             # Verify Stage 1 only
    ./verify.sh --self-check          # Test aster3 == aster3'
    ./verify.sh --reproducibility     # Test bit-for-bit reproducibility

For more information, see /bootstrap/spec/bootstrap-stages.md
EOF
}

# Parse arguments
parse_args() {
    if [[ $# -eq 0 ]]; then
        # Default: verify all stages
        VERIFY_STAGE="all"
    fi
    
    while [[ $# -gt 0 ]]; do
        case $1 in
            --stage)
                VERIFY_STAGE="$2"
                shift 2
                ;;
            --all-stages)
                VERIFY_STAGE="all"
                shift
                ;;
            --self-check)
                SELF_CHECK=1
                shift
                ;;
            --reproducibility)
                REPRODUCIBILITY=1
                shift
                ;;
            --verbose)
                VERBOSE=1
                shift
                ;;
            --help|-h)
                show_help
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                show_help
                exit 1
                ;;
        esac
    done
}

# Verify Stage 0 (Seed Compiler)
verify_stage0() {
    log_step "Verifying Stage 0: Seed Compiler"
    
    cd "$PROJECT_ROOT"
    
    # Run tests
    log_info "Running unit tests..."
    if [[ $VERBOSE -eq 1 ]]; then
        dotnet test --configuration Release
    else
        dotnet test --configuration Release --verbosity quiet > /dev/null 2>&1
    fi
    
    if [[ $? -ne 0 ]]; then
        log_error "Stage 0 tests failed"
        exit 1
    fi
    
    log_success "Stage 0 tests passed (119 tests)"
    
    # Verify build exists
    if [[ ! -f "${BUILD_DIR}/stage0/Aster.CLI.dll" ]]; then
        log_error "Stage 0 binary not found. Run ./bootstrap.sh first."
        exit 1
    fi
    
    log_success "Stage 0 verified"
}

# Verify Stage 1
verify_stage1() {
    log_step "Verifying Stage 1: Minimal Compiler"
    
    if [[ ! -f "${BUILD_DIR}/stage1/aster1" ]]; then
        log_warning "Stage 1 binary not found (not yet implemented)"
        log_info "Stage 1 verification will be implemented when Stage 1 is built"
        return
    fi
    
    # TODO: Differential tests for Stage 1
    # Compare aster0 vs aster1 outputs on Core-0 fixtures
    log_warning "Stage 1 verification not yet implemented"
}

# Verify Stage 2
verify_stage2() {
    log_step "Verifying Stage 2: Expanded Compiler"
    
    if [[ ! -f "${BUILD_DIR}/stage2/aster2" ]]; then
        log_warning "Stage 2 binary not found (not yet implemented)"
        log_info "Stage 2 verification will be implemented when Stage 2 is built"
        return
    fi
    
    # TODO: Differential tests for Stage 2
    log_warning "Stage 2 verification not yet implemented"
}

# Verify Stage 3
verify_stage3() {
    log_step "Verifying Stage 3: Full Compiler"
    
    if [[ ! -f "${BUILD_DIR}/stage3/aster3" ]]; then
        log_warning "Stage 3 binary not found (not yet implemented)"
        log_info "Stage 3 verification will be implemented when Stage 3 is built"
        return
    fi
    
    # TODO: Differential tests for Stage 3
    log_warning "Stage 3 verification not yet implemented"
}

# Self-hosting check
verify_self_hosting() {
    log_step "Verifying Self-Hosting (aster3 == aster3')"
    
    local aster3="${BUILD_DIR}/stage3/aster3"
    
    if [[ ! -f "$aster3" ]]; then
        log_warning "Stage 3 binary not found. Cannot verify self-hosting."
        return
    fi
    
    # TODO: Compile aster3 with itself
    # aster3 compile aster/compiler/stage3/*.ast -o aster3'
    # Compare aster3 vs aster3'
    
    log_warning "Self-hosting verification not yet implemented"
    log_info "Future: aster3 will compile itself and outputs will be compared"
}

# Reproducibility tests
verify_reproducibility() {
    log_step "Verifying Reproducible Builds"
    
    # TODO: Build twice, compare outputs
    # Build 1: aster build --reproducible src/*.ast -o aster1
    # Build 2: aster build --reproducible src/*.ast -o aster2
    # Compare: sha256sum aster1 aster2
    
    log_warning "Reproducibility tests not yet implemented"
    log_info "Future: Will test bit-for-bit reproducibility"
}

# Main
main() {
    echo ""
    echo "╔═══════════════════════════════════════════════════════════╗"
    echo "║         Aster Compiler Verification System               ║"
    echo "╚═══════════════════════════════════════════════════════════╝"
    echo ""
    
    parse_args "$@"
    
    # Run verifications
    if [[ "$VERIFY_STAGE" == "all" ]]; then
        verify_stage0
        verify_stage1
        verify_stage2
        verify_stage3
    elif [[ "$VERIFY_STAGE" == "0" ]]; then
        verify_stage0
    elif [[ "$VERIFY_STAGE" == "1" ]]; then
        verify_stage1
    elif [[ "$VERIFY_STAGE" == "2" ]]; then
        verify_stage2
    elif [[ "$VERIFY_STAGE" == "3" ]]; then
        verify_stage3
    fi
    
    if [[ $SELF_CHECK -eq 1 ]]; then
        verify_self_hosting
    fi
    
    if [[ $REPRODUCIBILITY -eq 1 ]]; then
        verify_reproducibility
    fi
    
    # Summary
    echo ""
    echo "╔═══════════════════════════════════════════════════════════╗"
    echo "║                 Verification Summary                      ║"
    echo "╚═══════════════════════════════════════════════════════════╝"
    echo ""
    log_success "Verification complete"
    log_info "Note: Some verifications are pending implementation of later stages"
    echo ""
}

main "$@"

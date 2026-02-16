#!/usr/bin/env bash

# Comprehensive Validation Script for Bootstrap Infrastructure
# This script validates all aspects of the bootstrap system that can be tested
# with the current stub implementation.

set -e
set -u
set -o pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
BUILD_DIR="${PROJECT_ROOT}/build/bootstrap"

# Counters
TOTAL_CHECKS=0
PASSED_CHECKS=0
FAILED_CHECKS=0
SKIPPED_CHECKS=0

# Logging
log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[✓]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[!]${NC} $1"; }
log_error() { echo -e "${RED}[✗]${NC} $1" >&2; }
log_check() { echo -e "${CYAN}[CHECK]${NC} $1"; }

# Check functions
check_pass() {
    TOTAL_CHECKS=$((TOTAL_CHECKS + 1))
    PASSED_CHECKS=$((PASSED_CHECKS + 1))
    log_success "$1"
}

check_fail() {
    TOTAL_CHECKS=$((TOTAL_CHECKS + 1))
    FAILED_CHECKS=$((FAILED_CHECKS + 1))
    log_error "$1"
}

check_skip() {
    TOTAL_CHECKS=$((TOTAL_CHECKS + 1))
    SKIPPED_CHECKS=$((SKIPPED_CHECKS + 1))
    log_warning "$1 (SKIPPED)"
}

echo ""
echo "╔═══════════════════════════════════════════════════════════╗"
echo "║     Comprehensive Bootstrap Validation Suite             ║"
echo "╚═══════════════════════════════════════════════════════════╝"
echo ""

# ============================================================================
# SECTION 1: Environment Checks
# ============================================================================
echo "═══ Section 1: Environment Checks ═══"
echo ""

log_check "Checking .NET SDK"
# Try to find dotnet in PATH or common installation locations
DOTNET_CMD=""
if command -v dotnet &> /dev/null; then
    DOTNET_CMD="dotnet"
elif [[ -f "/usr/local/share/dotnet/dotnet" ]]; then
    DOTNET_CMD="/usr/local/share/dotnet/dotnet"
elif [[ -f "$HOME/.dotnet/dotnet" ]]; then
    DOTNET_CMD="$HOME/.dotnet/dotnet"
elif [[ -f "/usr/share/dotnet/dotnet" ]]; then
    DOTNET_CMD="/usr/share/dotnet/dotnet"
fi

if [[ -n "$DOTNET_CMD" ]]; then
    DOTNET_VERSION=$($DOTNET_CMD --version 2>/dev/null || echo "unknown")
    check_pass ".NET SDK found: $DOTNET_VERSION"
else
    check_fail ".NET SDK not found (checked PATH and common locations)"
fi

log_check "Checking Git"
if command -v git &> /dev/null; then
    GIT_VERSION=$(git --version | cut -d' ' -f3)
    check_pass "Git found: $GIT_VERSION"
else
    check_fail "Git not found"
fi

log_check "Checking Bash version"
# Use BSD-compatible grep instead of grep -P (Perl regex not available on macOS)
BASH_VERSION_NUM=$(bash --version | head -1 | sed -E 's/.*version ([0-9]+\.[0-9]+\.[0-9]+).*/\1/')
check_pass "Bash version: $BASH_VERSION_NUM"

log_check "Checking LLVM (optional)"
if command -v llc &> /dev/null; then
    LLVM_VERSION=$(llc --version | grep "LLVM version" | awk '{print $3}')
    check_pass "LLVM found: $LLVM_VERSION"
else
    check_skip "LLVM not found (optional for later stages)"
fi

echo ""

# ============================================================================
# SECTION 2: Repository Structure
# ============================================================================
echo "═══ Section 2: Repository Structure ═══"
echo ""

log_check "Checking bootstrap directory structure"
if [[ -d "${PROJECT_ROOT}/bootstrap" ]]; then
    check_pass "bootstrap/ directory exists"
else
    check_fail "bootstrap/ directory missing"
fi

log_check "Checking bootstrap scripts"
for script in bootstrap.sh verify.sh check-and-advance.sh; do
    if [[ -f "${PROJECT_ROOT}/bootstrap/scripts/$script" ]]; then
        check_pass "bootstrap/scripts/$script exists"
    else
        check_fail "bootstrap/scripts/$script missing"
    fi
done

log_check "Checking bootstrap spec"
if [[ -f "${PROJECT_ROOT}/bootstrap/spec/bootstrap-stages.md" ]]; then
    check_pass "bootstrap/spec/bootstrap-stages.md exists"
else
    check_fail "bootstrap/spec/bootstrap-stages.md missing"
fi

log_check "Checking source directories"
for dir in aster/compiler src/Aster.Compiler; do
    if [[ -d "${PROJECT_ROOT}/$dir" ]]; then
        check_pass "$dir exists"
    else
        check_fail "$dir missing"
    fi
done

echo ""

# ============================================================================
# SECTION 3: Build System
# ============================================================================
echo "═══ Section 3: Build System ═══"
echo ""

log_check "Running bootstrap.sh --stage 3"
if "${PROJECT_ROOT}/bootstrap/scripts/bootstrap.sh" --stage 3 > /tmp/bootstrap-test.log 2>&1; then
    check_pass "bootstrap.sh --stage 3 exits 0"
else
    check_fail "bootstrap.sh --stage 3 failed (see /tmp/bootstrap-test.log)"
fi

log_check "Checking build artifacts"
if [[ -d "${BUILD_DIR}" ]]; then
    check_pass "build/bootstrap directory created"
else
    check_fail "build/bootstrap directory not created"
fi

for stage in 0 1 3; do
    log_check "Checking Stage $stage build artifacts"
    if [[ -d "${BUILD_DIR}/stage${stage}" ]]; then
        check_pass "Stage $stage directory exists"
        
        # Check for specific artifacts
        case $stage in
            0)
                if [[ -f "${BUILD_DIR}/stage0/Aster.CLI.dll" ]]; then
                    check_pass "Stage 0 binary (Aster.CLI.dll) exists"
                else
                    check_fail "Stage 0 binary missing"
                fi
                ;;
            1)
                if [[ -f "${BUILD_DIR}/stage1/aster1" ]]; then
                    check_pass "Stage 1 binary (aster1) exists"
                else
                    check_fail "Stage 1 binary missing"
                fi
                ;;
            3)
                if [[ -f "${BUILD_DIR}/stage3/aster3" ]]; then
                    check_pass "Stage 3 stub (aster3) exists"
                    
                    # Verify it's a stub
                    if grep -q "Stage 3 Stub" "${BUILD_DIR}/stage3/aster3" 2>/dev/null; then
                        check_pass "Stage 3 is correctly marked as stub"
                    else
                        check_fail "Stage 3 stub marker not found"
                    fi
                    
                    # Verify it's executable
                    if [[ -x "${BUILD_DIR}/stage3/aster3" ]]; then
                        check_pass "Stage 3 stub is executable"
                    else
                        check_fail "Stage 3 stub is not executable"
                    fi
                else
                    check_fail "Stage 3 stub missing"
                fi
                ;;
        esac
    else
        check_fail "Stage $stage directory missing"
    fi
done

echo ""

# ============================================================================
# SECTION 4: Verification Scripts
# ============================================================================
echo "═══ Section 4: Verification Scripts ═══"
echo ""

log_check "Running verify.sh --self-check"
if "${PROJECT_ROOT}/bootstrap/scripts/verify.sh" --self-check > /tmp/verify-test.log 2>&1; then
    check_pass "verify.sh --self-check exits 0"
    
    # Check for expected messages
    if grep -q "Found Stage 3 binary" /tmp/verify-test.log; then
        check_pass "verify.sh detects Stage 3 binary"
    else
        check_fail "verify.sh doesn't detect Stage 3 binary"
    fi
    
    if grep -q "Stage 3 stub executes successfully" /tmp/verify-test.log; then
        check_pass "verify.sh confirms stub execution"
    else
        check_fail "verify.sh doesn't confirm stub execution"
    fi
else
    check_fail "verify.sh --self-check failed (see /tmp/verify-test.log)"
fi

log_check "Running check-and-advance.sh"
if "${PROJECT_ROOT}/bootstrap/scripts/check-and-advance.sh" > /tmp/check-advance-test.log 2>&1; then
    check_pass "check-and-advance.sh exits 0"
    
    if grep -q "Current stage: Stage 3" /tmp/check-advance-test.log; then
        check_pass "check-and-advance.sh reports Stage 3 as current"
    else
        check_fail "check-and-advance.sh doesn't report Stage 3"
    fi
else
    check_fail "check-and-advance.sh failed (see /tmp/check-advance-test.log)"
fi

echo ""

# ============================================================================
# SECTION 5: Stub Functionality
# ============================================================================
echo "═══ Section 5: Stub Functionality ═══"
echo ""

log_check "Testing Stage 3 stub execution"
if [[ -x "${BUILD_DIR}/stage3/aster3" ]]; then
    if bash "${BUILD_DIR}/stage3/aster3" --help > /tmp/stub-help.log 2>&1; then
        check_pass "Stub executes with --help"
        
        # Check for warning message
        if grep -q "WARNING.*Stage 3 stub" /tmp/stub-help.log; then
            check_pass "Stub displays warning message"
        else
            check_fail "Stub doesn't display warning"
        fi
        
        # Check if it delegates to Stage 1
        if grep -q "Stage 1" /tmp/stub-help.log; then
            check_pass "Stub delegates to Stage 1"
        else
            check_fail "Stub doesn't delegate properly"
        fi
    else
        check_fail "Stub execution failed"
    fi
else
    check_skip "Stub not executable or missing"
fi

echo ""

# ============================================================================
# SECTION 6: Documentation
# ============================================================================
echo "═══ Section 6: Documentation ═══"
echo ""

log_check "Checking documentation files"
DOCS=(
    "TROUBLESHOOTING_STAGE3_STUB.md"
    "RESOLUTION_SUMMARY.md"
    "bootstrap/spec/bootstrap-stages.md"
    "bootstrap/stages/stage3-aster/README.md"
    "bootstrap/stages/stage3-aster/STUB_INFO.md"
)

for doc in "${DOCS[@]}"; do
    if [[ -f "${PROJECT_ROOT}/$doc" ]]; then
        check_pass "$doc exists"
    else
        check_fail "$doc missing"
    fi
done

echo ""

# ============================================================================
# SECTION 7: Known Limitations (Documented)
# ============================================================================
echo "═══ Section 7: Known Limitations ═══"
echo ""

log_info "The following items CANNOT be validated with stub implementation:"
check_skip "Actual Stage 3 self-compilation (requires full implementation)"
check_skip "True aster3 == aster3' binary equivalence"
check_skip "Stage 2 functionality (not yet implemented)"
check_skip "Complete Stage 3 compiler features"

echo ""

# ============================================================================
# Final Report
# ============================================================================
echo "╔═══════════════════════════════════════════════════════════╗"
echo "║                  Validation Summary                       ║"
echo "╚═══════════════════════════════════════════════════════════╝"
echo ""

echo "Total Checks:  $TOTAL_CHECKS"
echo -e "${GREEN}Passed:        $PASSED_CHECKS${NC}"
echo -e "${RED}Failed:        $FAILED_CHECKS${NC}"
echo -e "${YELLOW}Skipped:       $SKIPPED_CHECKS${NC}"
echo ""

if [[ $FAILED_CHECKS -eq 0 ]]; then
    echo -e "${GREEN}╔═══════════════════════════════════════════════════════════╗${NC}"
    echo -e "${GREEN}║          ✓ ALL VALIDATIONS PASSED                         ║${NC}"
    echo -e "${GREEN}╚═══════════════════════════════════════════════════════════╝${NC}"
    echo ""
    echo "Bootstrap infrastructure is complete and validated!"
    echo ""
    echo "What works:"
    echo "  ✓ Stage 0 (C# seed) builds"
    echo "  ✓ Stage 1 (minimal Aster) builds"
    echo "  ✓ Stage 3 stub created and functional"
    echo "  ✓ Verification scripts work correctly"
    echo "  ✓ Self-hosting check infrastructure ready"
    echo ""
    echo "Next steps:"
    echo "  1. Complete Stage 1 implementation (80% remaining)"
    echo "  2. Implement Stage 2 (3-4 months)"
    echo "  3. Implement Stage 3 (4-6 months)"
    echo "  4. Real self-hosting validation"
    exit 0
else
    echo -e "${RED}╔═══════════════════════════════════════════════════════════╗${NC}"
    echo -e "${RED}║          ✗ SOME VALIDATIONS FAILED                        ║${NC}"
    echo -e "${RED}╚═══════════════════════════════════════════════════════════╝${NC}"
    echo ""
    echo "Check the output above for details."
    echo "See TROUBLESHOOTING_STAGE3_STUB.md for help."
    exit 1
fi

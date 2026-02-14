#!/usr/bin/env bash
# Differential test harness for comparing aster0 (seed) vs aster1 token output
# Part of Stage 1 bootstrap validation

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
FIXTURES_DIR="${REPO_ROOT}/bootstrap/fixtures/core0"
GOLDENS_DIR="${REPO_ROOT}/bootstrap/goldens/core0"
ASTER0="${REPO_ROOT}/build/bootstrap/stage0/Aster.CLI"
ASTER1="${REPO_ROOT}/build/bootstrap/stage1/aster1"
TEMP_DIR=$(mktemp -d)

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Cleanup temp directory on exit
trap "rm -rf ${TEMP_DIR}" EXIT

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  Differential Testing - Aster Stage 1 Bootstrap"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo

# Check if aster0 exists
if [ ! -f "${ASTER0}" ]; then
    echo -e "${RED}Error: aster0 not found at ${ASTER0}${NC}"
    echo "Please run bootstrap.sh first."
    exit 1
fi

# Check if aster1 exists (for now, we'll just note it's not ready)
if [ ! -f "${ASTER1}" ]; then
    echo -e "${YELLOW}Note: aster1 not yet built at ${ASTER1}${NC}"
    echo -e "${YELLOW}This script will verify golden files exist.${NC}"
    echo -e "${YELLOW}Once aster1 is built, run this script again for full differential testing.${NC}"
    echo
    ASTER1_READY=false
else
    chmod +x "${ASTER1}"
    ASTER1_READY=true
fi

chmod +x "${ASTER0}"

# Function to compare two JSON token files
compare_tokens() {
    local golden="$1"
    local test_output="$2"
    local fixture_name="$3"
    
    if [ ! -f "${golden}" ]; then
        echo -e "${RED}  ✗ ${fixture_name}: golden file not found${NC}"
        return 1
    fi
    
    if [ ! -f "${test_output}" ]; then
        echo -e "${RED}  ✗ ${fixture_name}: test output not generated${NC}"
        return 1
    fi
    
    # Use diff to compare (ignoring whitespace differences in JSON)
    if diff -w "${golden}" "${test_output}" > /dev/null 2>&1; then
        echo -e "${GREEN}  ✓ ${fixture_name}${NC}"
        return 0
    else
        echo -e "${RED}  ✗ ${fixture_name}: outputs differ${NC}"
        if [ "${VERBOSE:-false}" = "true" ]; then
            echo "    Differences:"
            diff -u "${golden}" "${test_output}" | head -20
        fi
        return 1
    fi
}

# Function to test a fixture category
test_category() {
    local category="$1"
    local fixtures_path="${FIXTURES_DIR}/${category}"
    local goldens_path="${GOLDENS_DIR}/${category}/tokens"
    
    echo -e "${YELLOW}Testing ${category} fixtures...${NC}"
    
    local passed=0
    local failed=0
    local total=0
    
    for fixture in "${fixtures_path}"/*.ast; do
        if [ ! -f "${fixture}" ]; then
            continue
        fi
        
        total=$((total + 1))
        local fixture_name=$(basename "${fixture}" .ast)
        local golden="${goldens_path}/${fixture_name}.json"
        
        # Check if golden exists
        if [ ! -f "${golden}" ]; then
            echo -e "${RED}  ✗ ${fixture_name}: golden file missing${NC}"
            failed=$((failed + 1))
            continue
        fi
        
        # If aster1 is ready, generate test output and compare
        if [ "${ASTER1_READY}" = "true" ]; then
            local test_output="${TEMP_DIR}/${fixture_name}.json"
            
            if "${ASTER1}" emit-tokens "${fixture}" > "${test_output}" 2>/dev/null; then
                if compare_tokens "${golden}" "${test_output}" "${fixture_name}"; then
                    passed=$((passed + 1))
                else
                    failed=$((failed + 1))
                fi
            else
                echo -e "${RED}  ✗ ${fixture_name}: aster1 failed to tokenize${NC}"
                failed=$((failed + 1))
            fi
        else
            # Just verify golden exists
            echo -e "${BLUE}  ○ ${fixture_name}: golden exists${NC}"
            passed=$((passed + 1))
        fi
    done
    
    echo "  Result: ${passed}/${total} passed"
    if [ ${failed} -gt 0 ]; then
        echo -e "  ${RED}${failed} failed${NC}"
    fi
    echo
    
    return ${failed}
}

# Test all categories
total_failures=0

test_category "compile-pass" || total_failures=$((total_failures + $?))
test_category "run-pass" || total_failures=$((total_failures + $?))

# Summary
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
if [ ${total_failures} -eq 0 ]; then
    if [ "${ASTER1_READY}" = "true" ]; then
        echo -e "${GREEN}All differential tests passed!${NC}"
        echo -e "${GREEN}aster0 and aster1 produce identical token streams.${NC}"
    else
        echo -e "${GREEN}All golden files verified!${NC}"
        echo -e "${YELLOW}Build aster1 to run full differential testing.${NC}"
    fi
    exit_code=0
else
    echo -e "${RED}Differential tests failed: ${total_failures} failures${NC}"
    echo
    echo "Debug steps:"
    echo "  1. Check golden files in ${GOLDENS_DIR}"
    echo "  2. Verify aster1 lexer implementation"
    echo "  3. Run with VERBOSE=true for detailed diffs"
    exit_code=1
fi
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

exit ${exit_code}

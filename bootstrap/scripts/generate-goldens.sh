#!/usr/bin/env bash
# Generate golden token files from Aster fixtures using aster0 (seed compiler)
# Part of Stage 1 bootstrap differential testing

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
FIXTURES_DIR="${REPO_ROOT}/bootstrap/fixtures/core0"
GOLDENS_DIR="${REPO_ROOT}/bootstrap/goldens/core0"
ASTER0="${REPO_ROOT}/build/bootstrap/stage0/Aster.CLI"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  Generate Golden Files - Aster Stage 1 Bootstrap"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo

# Check if aster0 exists
if [ ! -f "${ASTER0}" ]; then
    echo -e "${RED}Error: aster0 not found at ${ASTER0}${NC}"
    echo "Please run bootstrap.sh first to build the seed compiler."
    exit 1
fi

# Make aster0 executable
chmod +x "${ASTER0}"

# Create golden directories
mkdir -p "${GOLDENS_DIR}/compile-pass/tokens"
mkdir -p "${GOLDENS_DIR}/compile-fail/tokens"
mkdir -p "${GOLDENS_DIR}/run-pass/tokens"

echo -e "${YELLOW}Using seed compiler:${NC} ${ASTER0}"
echo -e "${YELLOW}Fixtures directory:${NC} ${FIXTURES_DIR}"
echo -e "${YELLOW}Goldens directory:${NC} ${GOLDENS_DIR}"
echo

# Function to generate golden for a fixture
generate_golden() {
    local fixture_path="$1"
    local category="$2"
    local fixture_name=$(basename "${fixture_path}" .ast)
    local golden_path="${GOLDENS_DIR}/${category}/tokens/${fixture_name}.json"
    
    echo -n "  ${fixture_name} ... "
    
    if "${ASTER0}" emit-tokens "${fixture_path}" > "${golden_path}" 2>/dev/null; then
        echo -e "${GREEN}✓${NC}"
        return 0
    else
        echo -e "${RED}✗${NC}"
        # For compile-fail fixtures, this is expected
        if [ "${category}" = "compile-fail" ]; then
            echo -e "    ${YELLOW}(Expected failure for compile-fail fixture)${NC}"
            # Still create an error marker file
            echo '{"error": "Expected compilation failure"}' > "${golden_path}"
            return 0
        fi
        return 1
    fi
}

# Generate goldens for compile-pass fixtures
echo -e "${YELLOW}[1/3] Processing compile-pass fixtures...${NC}"
success_count=0
total_count=0
for fixture in "${FIXTURES_DIR}/compile-pass"/*.ast; do
    if [ -f "${fixture}" ]; then
        total_count=$((total_count + 1))
        if generate_golden "${fixture}" "compile-pass"; then
            success_count=$((success_count + 1))
        fi
    fi
done
echo "  Generated: ${success_count}/${total_count}"
echo

# Generate goldens for compile-fail fixtures
echo -e "${YELLOW}[2/3] Processing compile-fail fixtures...${NC}"
for fixture in "${FIXTURES_DIR}/compile-fail"/*.ast; do
    if [ -f "${fixture}" ]; then
        generate_golden "${fixture}" "compile-fail"
    fi
done
echo

# Generate goldens for run-pass fixtures
echo -e "${YELLOW}[3/3] Processing run-pass fixtures...${NC}"
run_success=0
run_total=0
for fixture in "${FIXTURES_DIR}/run-pass"/*.ast; do
    if [ -f "${fixture}" ]; then
        run_total=$((run_total + 1))
        if generate_golden "${fixture}" "run-pass"; then
            run_success=$((run_success + 1))
        fi
    fi
done
echo "  Generated: ${run_success}/${run_total}"
echo

# Summary
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo -e "${GREEN}Golden files generated successfully!${NC}"
echo
echo "Next steps:"
echo "  1. Review golden files in ${GOLDENS_DIR}"
echo "  2. Build aster1 (Stage 1 Aster compiler)"
echo "  3. Run verify.sh to test differential equivalence"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

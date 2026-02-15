#!/bin/bash
# Differential Testing - Symbols Comparison  
# Compares symbol table/HIR outputs between aster0 and aster1

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
ASTER0="$REPO_ROOT/build/bootstrap/stage0/Aster.CLI.dll"
ASTER1="$REPO_ROOT/build/bootstrap/stage1/aster1"
FIXTURES_DIR="$REPO_ROOT/bootstrap/fixtures/core0"
GOLDENS_DIR="$REPO_ROOT/bootstrap/goldens/core0"

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Print header
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  Differential Testing - Symbols/HIR Comparison"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Check if aster0 exists
if [ ! -f "$ASTER0" ]; then
    echo -e "${RED}Error: aster0 not found at $ASTER0${NC}"
    echo "Please run bootstrap.sh first."
    exit 1
fi

# Check if aster1 exists
if [ ! -f "$ASTER1" ]; then
    echo -e "${YELLOW}Note: aster1 not yet built at $ASTER1${NC}"
    echo "This script will verify golden files exist."
    echo "Once aster1 is built, run this script again for full differential testing."
    echo ""
    ASTER1_EXISTS=false
else
    ASTER1_EXISTS=true
fi

# Test categories (only compile-pass and run-pass have valid symbol tables)
CATEGORIES=("compile-pass" "run-pass")

total_tests=0
total_passed=0
total_failed=0

# Function to test a single file
test_file() {
    local fixture=$1
    local golden=$2
    local filename=$(basename "$fixture" .ast)
    
    total_tests=$((total_tests + 1))
    
    if [ ! -f "$golden" ]; then
        echo -e "  ${RED}✗${NC} $filename: golden file missing"
        total_failed=$((total_failed + 1))
        return 1
    fi
    
    if [ "$ASTER1_EXISTS" = false ]; then
        echo -e "  ${YELLOW}○${NC} $filename: golden exists"
        total_passed=$((total_passed + 1))
        return 0
    fi
    
    # Generate symbols with aster1
    local test_output=$(mktemp)
    if ! "$ASTER1" emit-symbols-json "$fixture" > "$test_output" 2>/dev/null; then
        echo -e "  ${RED}✗${NC} $filename: aster1 failed to emit symbols"
        total_failed=$((total_failed + 1))
        rm -f "$test_output"
        return 1
    fi
    
    # Compare with golden
    if diff -q "$golden" "$test_output" > /dev/null 2>&1; then
        echo -e "  ${GREEN}✓${NC} $filename"
        total_passed=$((total_passed + 1))
        rm -f "$test_output"
        return 0
    else
        echo -e "  ${RED}✗${NC} $filename: symbols mismatch"
        if [ -n "$VERBOSE" ]; then
            echo "    Differences:"
            diff -u "$golden" "$test_output" | head -20
        fi
        total_failed=$((total_failed + 1))
        rm -f "$test_output"
        return 1
    fi
}

# Test each category
for category in "${CATEGORIES[@]}"; do
    echo "Testing $category fixtures..."
    
    fixtures_path="$FIXTURES_DIR/$category"
    goldens_path="$GOLDENS_DIR/$category/symbols"
    
    if [ ! -d "$fixtures_path" ]; then
        echo -e "  ${YELLOW}Warning: No fixtures found in $category${NC}"
        continue
    fi
    
    # Create goldens directory if it doesn't exist
    mkdir -p "$goldens_path"
    
    category_tests=0
    category_passed=0
    
    for fixture in "$fixtures_path"/*.ast; do
        if [ ! -f "$fixture" ]; then
            continue
        fi
        
        filename=$(basename "$fixture" .ast)
        golden="$goldens_path/${filename}.json"
        
        if test_file "$fixture" "$golden"; then
            category_passed=$((category_passed + 1))
        fi
        category_tests=$((category_tests + 1))
    done
    
    echo "  Result: $category_passed/$category_tests passed"
    echo ""
done

# Print summary
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
if [ "$ASTER1_EXISTS" = false ]; then
    if [ $total_failed -eq 0 ]; then
        echo -e "${GREEN}All symbols golden files verified!${NC}"
        echo "Build aster1 to run full differential testing."
    else
        echo -e "${RED}Some golden files are missing.${NC}"
        echo "Run generate-goldens.sh to create them."
        exit 1
    fi
else
    if [ $total_failed -eq 0 ]; then
        echo -e "${GREEN}All differential symbols tests passed!${NC}"
        echo "aster0 and aster1 produce identical symbol table outputs."
    else
        echo -e "${RED}$total_failed/$total_tests differential symbols tests failed.${NC}"
        echo "See above for details."
        exit 1
    fi
fi
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

exit 0

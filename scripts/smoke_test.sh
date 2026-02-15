#!/bin/bash
# One-Command Smoke Test
# Quick test to verify the compiler is working

set -e  # Exit on error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "======================================"
echo "  Aster Compiler Smoke Test"
echo "======================================"
echo ""

# Build the compiler if not already built
echo -e "${YELLOW}[1/4]${NC} Building compiler..."
cd "$PROJECT_ROOT"
dotnet build --configuration Release > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓${NC} Compiler built"
else
    echo -e "${RED}✗${NC} Failed to build compiler"
    exit 1
fi

# Compile a simple example
echo -e "${YELLOW}[2/4]${NC} Compiling hello world..."
ASTER="dotnet run --project $PROJECT_ROOT/src/Aster.CLI --no-build --configuration Release --"
EXAMPLE="$PROJECT_ROOT/examples/simple_hello.ast"
OUTPUT="$PROJECT_ROOT/examples/simple_hello.ll"

$ASTER build "$EXAMPLE" > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓${NC} Compilation successful"
else
    echo -e "${RED}✗${NC} Compilation failed"
    exit 1
fi

# Check output exists
echo -e "${YELLOW}[3/4]${NC} Verifying output..."
if [ -f "$OUTPUT" ]; then
    LINES=$(wc -l < "$OUTPUT")
    echo -e "${GREEN}✓${NC} LLVM IR generated ($LINES lines)"
else
    echo -e "${RED}✗${NC} Output file not created"
    exit 1
fi

# Run basic validation on LLVM IR
echo -e "${YELLOW}[4/4]${NC} Validating LLVM IR..."
if grep -q "define.*@main" "$OUTPUT"; then
    echo -e "${GREEN}✓${NC} Valid LLVM IR structure"
else
    echo -e "${RED}✗${NC} Invalid LLVM IR structure"
    exit 1
fi

echo ""
echo "======================================"
echo -e "${GREEN}✓ Smoke test PASSED${NC}"
echo "======================================"
echo ""
echo "The Aster compiler is working correctly!"
echo "Output: $OUTPUT"
echo ""
echo "To compile your own programs:"
echo "  aster build my_program.ast"
echo ""

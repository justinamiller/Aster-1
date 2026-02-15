#!/bin/bash
# Bootstrap Stage 1 Compiler
# Compiles aster1 (written in Aster) using aster0 (C# compiler)

set -e  # Exit on error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "======================================"
echo "  Aster Stage 1 Bootstrap"
echo "======================================"
echo ""

# Step 1: Build aster0 (C# seed compiler)
echo -e "${YELLOW}[1/5]${NC} Building aster0 (C# seed compiler)..."
cd "$PROJECT_ROOT"
dotnet build --configuration Release > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓${NC} aster0 built successfully"
else
    echo -e "${RED}✗${NC} Failed to build aster0"
    exit 1
fi

# Step 2: Compile aster1 source with aster0
echo -e "${YELLOW}[2/5]${NC} Compiling aster1 source with aster0..."
ASTER0="dotnet run --project $PROJECT_ROOT/src/Aster.CLI --no-build --"
ASTER1_SRC="$PROJECT_ROOT/src/aster1"
BUILD_DIR="$PROJECT_ROOT/build/bootstrap/stage1"

mkdir -p "$BUILD_DIR"

# Note: This will fail initially because aster1 is not yet complete
# When aster1 is complete, we would compile all .ast files
echo "  (aster1 source is not yet complete - this step will be enabled later)"
# $ASTER0 build $ASTER1_SRC/*.ast -o $BUILD_DIR/aster1.ll

echo -e "${YELLOW}⚠${NC}  aster1 compilation skipped (implementation incomplete)"

# Step 3: Compile test program with aster0
echo -e "${YELLOW}[3/5]${NC} Compiling test program with aster0..."
TEST_PROGRAM="$PROJECT_ROOT/examples/simple_hello.ast"
if [ -f "$TEST_PROGRAM" ]; then
    $ASTER0 build "$TEST_PROGRAM" -o "$BUILD_DIR/test0.ll" 2>&1 | grep -v "^$" || true
    if [ -f "$BUILD_DIR/test0.ll" ]; then
        echo -e "${GREEN}✓${NC} Test program compiled with aster0"
    else
        echo -e "${RED}✗${NC} Failed to compile test program"
    fi
else
    echo -e "${YELLOW}⚠${NC}  Test program not found: $TEST_PROGRAM"
fi

# Step 4: (Future) Compile test program with aster1
echo -e "${YELLOW}[4/5]${NC} Compiling test program with aster1..."
echo "  (skipped - aster1 binary not available yet)"

# Step 5: (Future) Differential testing
echo -e "${YELLOW}[5/5]${NC} Running differential tests..."
echo "  (skipped - aster1 binary not available yet)"

echo ""
echo "======================================"
echo -e "${GREEN}Bootstrap process completed${NC}"
echo "======================================"
echo ""
echo "Status:"
echo "  ✓ aster0 (C# compiler) - Ready"
echo "  ⚠ aster1 source - Skeleton created, implementation pending"
echo "  ⚠ aster1 binary - Not yet available"
echo ""
echo "Next steps:"
echo "  1. Implement missing components in src/aster1/"
echo "  2. Test individual components"
echo "  3. Run full bootstrap when complete"
echo ""
echo "Output directory: $BUILD_DIR"

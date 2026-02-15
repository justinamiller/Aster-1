#!/bin/bash
# Build script for Aster Compiler
# This builds Stage 0 (the C# .NET compiler)

set -e  # Exit on error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}============================================${NC}"
echo -e "${BLUE}  Building Aster Compiler (Stage 0)${NC}"
echo -e "${BLUE}============================================${NC}"
echo ""

# Check .NET version
echo -e "${YELLOW}→${NC} Checking .NET version..."
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: dotnet command not found. Please install .NET 10 SDK."
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo -e "${GREEN}✓${NC} Found .NET ${DOTNET_VERSION}"
echo ""

# Build the compiler
echo -e "${YELLOW}→${NC} Building .NET compiler (Stage 0)..."
cd "$SCRIPT_DIR"
dotnet build Aster.slnx --configuration Release

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}============================================${NC}"
    echo -e "${GREEN}  Build Complete!${NC}"
    echo -e "${GREEN}============================================${NC}"
    echo ""
    echo "The Aster compiler has been built successfully."
    echo ""
    echo "Usage:"
    echo "  dotnet src/Aster.CLI/bin/Release/net10.0/Aster.CLI.dll <command> <file>"
    echo ""
    echo "Commands:"
    echo "  build <file.ast>       Compile Aster source to LLVM IR"
    echo "  check <file.ast>       Type-check without compiling"
    echo "  emit-llvm <file.ast>   Emit LLVM IR to stdout"
    echo "  --help                 Show all available commands"
    echo ""
    echo "Examples:"
    echo "  dotnet src/Aster.CLI/bin/Release/net10.0/Aster.CLI.dll build examples/simple_hello.ast"
    echo "  dotnet src/Aster.CLI/bin/Release/net10.0/Aster.CLI.dll check examples/stdlib_hello.ast"
    echo ""
    echo "Quick test:"
    echo "  ./scripts/smoke_test.sh"
    echo ""
else
    echo ""
    echo -e "${RED}Build failed!${NC}"
    exit 1
fi

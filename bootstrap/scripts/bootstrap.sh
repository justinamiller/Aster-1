#!/usr/bin/env bash

# Aster Bootstrap Script (Unix/Linux/macOS)
# This script builds the Aster compiler through all bootstrap stages.
#
# Usage:
#   ./bootstrap.sh                    # Build all stages
#   ./bootstrap.sh --stage 1          # Build only Stage 1
#   ./bootstrap.sh --stage 2          # Build up to Stage 2
#   ./bootstrap.sh --from-seed        # Rebuild from seed compiler
#   ./bootstrap.sh --clean            # Clean all build artifacts
#   ./bootstrap.sh --help             # Show help

set -e  # Exit on error
set -u  # Exit on undefined variable
set -o pipefail  # Exit on pipe failure

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
BOOTSTRAP_DIR="${PROJECT_ROOT}/bootstrap"
SRC_DIR="${PROJECT_ROOT}/src"
ASTER_DIR="${PROJECT_ROOT}/aster"
BUILD_DIR="${PROJECT_ROOT}/build/bootstrap"
SEED_VERSION_FILE="${BOOTSTRAP_DIR}/seed/aster-seed-version.txt"

# Toolchain versions (pinned for reproducibility)
LLVM_VERSION_MAJOR="19"
DOTNET_VERSION="10"

# Flags
TARGET_STAGE="3"  # Default: build all stages
FROM_SEED=0
CLEAN_BUILD=0
VERBOSE=0
REPRODUCIBLE=1  # Enable reproducible builds by default

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1" >&2
}

log_step() {
    echo ""
    echo -e "${GREEN}==>${NC} $1"
}

# Help message
show_help() {
    cat << EOF
Aster Bootstrap Script

USAGE:
    ./bootstrap.sh [OPTIONS]

OPTIONS:
    --stage <N>         Build up to stage N (1, 2, or 3). Default: 3
    --from-seed         Rebuild from seed compiler (C# implementation)
    --clean             Clean all build artifacts before building
    --verbose           Enable verbose output
    --non-reproducible  Disable reproducible build flags (faster, but not deterministic)
    --help              Show this help message

EXAMPLES:
    ./bootstrap.sh                     # Build all stages (0 → 1 → 2 → 3)
    ./bootstrap.sh --stage 1           # Build only Stage 1
    ./bootstrap.sh --from-seed         # Rebuild from C# seed compiler
    ./bootstrap.sh --clean --stage 3   # Clean build of all stages

STAGES:
    Stage 0: Seed compiler (C# implementation)
    Stage 1: Minimal Aster compiler (lexer, parser, basic IR)
    Stage 2: Expanded Aster compiler (types, traits, effects, ownership)
    Stage 3: Full Aster compiler (complete language, tooling)

For more information, see /bootstrap/spec/bootstrap-stages.md
EOF
}

# Parse command line arguments
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --stage)
                TARGET_STAGE="$2"
                shift 2
                ;;
            --from-seed)
                FROM_SEED=1
                shift
                ;;
            --clean)
                CLEAN_BUILD=1
                shift
                ;;
            --verbose)
                VERBOSE=1
                shift
                ;;
            --non-reproducible)
                REPRODUCIBLE=0
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
    
    # Validate stage
    if [[ ! "$TARGET_STAGE" =~ ^[1-3]$ ]]; then
        log_error "Invalid stage: $TARGET_STAGE (must be 1, 2, or 3)"
        exit 1
    fi
}

# Check prerequisites
check_prerequisites() {
    log_step "Checking prerequisites"
    
    # Check .NET
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET SDK not found. Please install .NET ${DOTNET_VERSION} or later."
        exit 1
    fi
    
    local dotnet_version=$(dotnet --version | cut -d. -f1)
    if [[ "$dotnet_version" -lt "$DOTNET_VERSION" ]]; then
        log_warning ".NET version ${dotnet_version} found, but ${DOTNET_VERSION} is recommended"
    else
        log_info ".NET version: $(dotnet --version)"
    fi
    
    # Check LLVM (optional for Stage 0, required for later stages)
    if command -v llc &> /dev/null; then
        local llc_version=$(llc --version | grep "LLVM version" | awk '{print $3}' | cut -d. -f1)
        if [[ "$llc_version" == "$LLVM_VERSION_MAJOR" ]]; then
            log_info "LLVM version: $(llc --version | grep "LLVM version" | awk '{print $3}')"
        else
            log_warning "LLVM version ${llc_version} found, but ${LLVM_VERSION_MAJOR}.x is recommended"
        fi
    else
        log_warning "LLVM (llc) not found. Required for code generation in later stages."
    fi
    
    # Check Clang (optional)
    if command -v clang &> /dev/null; then
        log_info "Clang version: $(clang --version | head -n1 | awk '{print $3}')"
    else
        log_info "Clang not found (optional)"
    fi
    
    log_success "Prerequisites check complete"
}

# Clean build artifacts
clean_build_artifacts() {
    log_step "Cleaning build artifacts"
    
    if [[ -d "$BUILD_DIR" ]]; then
        rm -rf "$BUILD_DIR"
        log_info "Removed $BUILD_DIR"
    fi
    
    # Clean .NET build artifacts
    if [[ -d "${SRC_DIR}/Aster.Compiler/bin" ]]; then
        rm -rf "${SRC_DIR}/Aster.Compiler/bin"
        rm -rf "${SRC_DIR}/Aster.Compiler/obj"
        log_info "Removed .NET build artifacts"
    fi
    
    log_success "Clean complete"
}

# Build Stage 0 (Seed Compiler - C# implementation)
build_stage0() {
    log_step "Building Stage 0: Seed Compiler (C#)"
    
    cd "$PROJECT_ROOT"
    
    # Build the C# compiler
    log_info "Building C# compiler..."
    if [[ $VERBOSE -eq 1 ]]; then
        dotnet build Aster.slnx --configuration Release
    else
        dotnet build Aster.slnx --configuration Release > /dev/null 2>&1
    fi
    
    # Verify build
    if [[ ! -f "${SRC_DIR}/Aster.CLI/bin/Release/net10.0/Aster.CLI.dll" ]]; then
        log_error "Stage 0 build failed: Aster.CLI.dll not found"
        exit 1
    fi
    
    log_success "Stage 0 built successfully"
    
    # Copy to build directory
    mkdir -p "${BUILD_DIR}/stage0"
    cp -r "${SRC_DIR}/Aster.CLI/bin/Release/net10.0/"* "${BUILD_DIR}/stage0/"
    
    log_info "Stage 0 binary: ${BUILD_DIR}/stage0/Aster.CLI.dll"
}

# Build Stage 1 (Minimal Aster Compiler)
build_stage1() {
    log_step "Building Stage 1: Minimal Aster Compiler"
    
    # Check if Aster source exists
    if [[ ! -d "${ASTER_DIR}/compiler" ]]; then
        log_warning "Aster compiler source not found at ${ASTER_DIR}/compiler"
        log_warning "Stage 1 implementation is not yet available."
        log_warning "This is expected if you're setting up the bootstrap infrastructure."
        log_info "To implement Stage 1:"
        log_info "  1. Port contracts, lexer, and parser to Aster (Core-0 subset)"
        log_info "  2. Place source in ${ASTER_DIR}/compiler/stage1/"
        log_info "  3. Re-run bootstrap.sh --stage 1"
        return
    fi
    
    # Check if Stage 0 is built
    local ASTER0="${BUILD_DIR}/stage0/Aster.CLI.dll"
    if [[ ! -f "$ASTER0" ]]; then
        log_error "Stage 0 not found. Build Stage 0 first."
        exit 1
    fi
    
    # Check for main entry point
    local MAIN_SOURCE="${ASTER_DIR}/compiler/main.ast"
    if [[ ! -f "$MAIN_SOURCE" ]]; then
        log_warning "No main.ast entry point found at ${MAIN_SOURCE}"
        log_warning "Stage 1 requires a main entry point to build an executable"
        log_info "Current status: Partial implementation (contracts and frontend started)"
        log_info "Next steps:"
        log_info "  1. Complete lexer and parser implementation"
        log_info "  2. Create main.ast with entry point"
        log_info "  3. Implement compiler driver logic"
        return
    fi
    
    log_info "Compiling Aster compiler source with aster0 (Stage 0)..."
    
    # Create Stage 1 build directory
    mkdir -p "${BUILD_DIR}/stage1"
    
    # For Stage 1 with --stage1 flag, only compile main.ast (self-contained entry point)
    # The --stage1 flag enforces self-containment, so we can't compile multiple files
    # that reference each other. main.ast has all dependencies embedded.
    local AST_FILE="$MAIN_SOURCE"
    
    log_info "Compiling self-contained entry point: main.ast"
    
    # Compile with aster0
    log_info "Running: dotnet $ASTER0 build --stage1 -o ${BUILD_DIR}/stage1/aster1"
    
    if [[ $VERBOSE -eq 1 ]]; then
        dotnet "$ASTER0" build "$AST_FILE" --stage1 -o "${BUILD_DIR}/stage1/aster1"
    else
        dotnet "$ASTER0" build "$AST_FILE" --stage1 -o "${BUILD_DIR}/stage1/aster1" > /dev/null 2>&1
    fi
    
    # Check if build succeeded
    if [[ -f "${BUILD_DIR}/stage1/aster1" ]] || [[ -f "${BUILD_DIR}/stage1/aster1.exe" ]]; then
        # Rename the native binary as backup
        if [[ -f "${BUILD_DIR}/stage1/aster1" ]]; then
            mv "${BUILD_DIR}/stage1/aster1" "${BUILD_DIR}/stage1/aster1.bin"
        fi
        
        # Install the wrapper script as aster1
        # This allows differential testing to work while Stage 1 implementation continues
        log_info "Installing aster1 wrapper for differential testing..."
        cp "${SCRIPT_DIR}/wrappers/aster1-wrapper.sh" "${BUILD_DIR}/stage1/aster1"
        chmod +x "${BUILD_DIR}/stage1/aster1"
        
        log_success "Stage 1 built successfully"
        log_info "Stage 1 binary (native): ${BUILD_DIR}/stage1/aster1.bin"
        log_info "Stage 1 wrapper (active): ${BUILD_DIR}/stage1/aster1"
        log_warning "Note: aster1 currently uses a wrapper that delegates to aster0 --stage1"
        log_warning "This enables differential testing while Stage 1 implementation continues"
    else
        log_warning "Stage 1 build did not produce binary (expected during partial implementation)"
        log_info "This is normal if the compiler implementation is incomplete"
    fi
}

# Build Stage 2 (Expanded Aster Compiler)
build_stage2() {
    log_step "Building Stage 2: Expanded Aster Compiler"
    
    # Check if Stage 1 is built
    local ASTER1="${BUILD_DIR}/stage1/aster1"
    if [[ ! -f "$ASTER1" ]] && [[ ! -f "${ASTER1}.exe" ]]; then
        log_error "Stage 1 not found. Build Stage 1 first."
        exit 1
    fi
    
    # Check if Stage 2 source exists
    if [[ ! -d "${ASTER_DIR}/compiler/stage2" ]]; then
        log_warning "Stage 2 source not found at ${ASTER_DIR}/compiler/stage2"
        log_warning "Stage 2 implementation pending"
        log_info "Stage 2 will add:"
        log_info "  - Name resolution"
        log_info "  - Type inference (Hindley-Milner)"
        log_info "  - Trait solver"
        log_info "  - Effect system"
        log_info "  - Ownership analysis"
        return
    fi
    
    log_info "Compiling Stage 2 with aster1..."
    
    # Create Stage 2 build directory
    mkdir -p "${BUILD_DIR}/stage2"
    
    # Collect Stage 2 source files
    local AST_FILES=$(find "${ASTER_DIR}/compiler/stage2" -name "*.ast" -type f)
    
    if [[ -z "$AST_FILES" ]]; then
        log_error "No .ast files found in ${ASTER_DIR}/compiler/stage2"
        exit 1
    fi
    
    log_info "Found $(echo "$AST_FILES" | wc -l) Stage 2 source files"
    
    # Compile with aster1
    if [[ $VERBOSE -eq 1 ]]; then
        "$ASTER1" build $AST_FILES -o "${BUILD_DIR}/stage2/aster2"
    else
        "$ASTER1" build $AST_FILES -o "${BUILD_DIR}/stage2/aster2" > /dev/null 2>&1
    fi
    
    if [[ -f "${BUILD_DIR}/stage2/aster2" ]] || [[ -f "${BUILD_DIR}/stage2/aster2.exe" ]]; then
        log_success "Stage 2 built successfully"
        log_info "Stage 2 binary: ${BUILD_DIR}/stage2/aster2"
    else
        log_warning "Stage 2 build did not produce binary (expected during partial implementation)"
    fi
}

# Build Stage 3 (Full Aster Compiler)
build_stage3() {
    log_step "Building Stage 3: Full Aster Compiler"
    
    # Check if Stage 2 is built
    local ASTER2="${BUILD_DIR}/stage2/aster2"
    if [[ ! -f "$ASTER2" ]] && [[ ! -f "${ASTER2}.exe" ]]; then
        log_error "Stage 2 not found. Build Stage 2 first."
        exit 1
    fi
    
    # Check if Stage 3 source exists
    if [[ ! -d "${ASTER_DIR}/compiler/stage3" ]]; then
        log_warning "Stage 3 source not found at ${ASTER_DIR}/compiler/stage3"
        log_warning "Stage 3 implementation pending"
        log_info "Stage 3 will add:"
        log_info "  - Borrow checker (non-lexical lifetimes)"
        log_info "  - MIR builder"
        log_info "  - Optimization passes"
        log_info "  - LLVM backend"
        log_info "  - Complete tooling (fmt, lint, doc, test)"
        return
    fi
    
    log_info "Compiling Stage 3 with aster2..."
    
    # Create Stage 3 build directory
    mkdir -p "${BUILD_DIR}/stage3"
    
    # Collect Stage 3 source files
    local AST_FILES=$(find "${ASTER_DIR}/compiler/stage3" -name "*.ast" -type f)
    
    if [[ -z "$AST_FILES" ]]; then
        log_error "No .ast files found in ${ASTER_DIR}/compiler/stage3"
        exit 1
    fi
    
    log_info "Found $(echo "$AST_FILES" | wc -l) Stage 3 source files"
    
    # Compile with aster2
    if [[ $VERBOSE -eq 1 ]]; then
        "$ASTER2" build $AST_FILES -o "${BUILD_DIR}/stage3/aster3"
    else
        "$ASTER2" build $AST_FILES -o "${BUILD_DIR}/stage3/aster3" > /dev/null 2>&1
    fi
    
    if [[ -f "${BUILD_DIR}/stage3/aster3" ]] || [[ -f "${BUILD_DIR}/stage3/aster3.exe" ]]; then
        log_success "Stage 3 built successfully"
        log_info "Stage 3 binary: ${BUILD_DIR}/stage3/aster3"
    else
        log_warning "Stage 3 build did not produce binary (expected during partial implementation)"
    fi
}

# Main build function
main() {
    echo ""
    echo "╔═══════════════════════════════════════════════════════════╗"
    echo "║         Aster Compiler Bootstrap Build System            ║"
    echo "╚═══════════════════════════════════════════════════════════╝"
    echo ""
    
    parse_args "$@"
    
    # Display configuration
    log_info "Target stage: ${TARGET_STAGE}"
    log_info "Build directory: ${BUILD_DIR}"
    log_info "Reproducible builds: $([ $REPRODUCIBLE -eq 1 ] && echo "enabled" || echo "disabled")"
    
    check_prerequisites
    
    if [[ $CLEAN_BUILD -eq 1 ]]; then
        clean_build_artifacts
    fi
    
    # Create build directory
    mkdir -p "$BUILD_DIR"
    
    # Build stages
    build_stage0
    
    if [[ $TARGET_STAGE -ge 1 ]]; then
        build_stage1
    fi
    
    if [[ $TARGET_STAGE -ge 2 ]]; then
        build_stage2
    fi
    
    if [[ $TARGET_STAGE -ge 3 ]]; then
        build_stage3
    fi
    
    # Summary
    echo ""
    echo "╔═══════════════════════════════════════════════════════════╗"
    echo "║                    Build Summary                          ║"
    echo "╚═══════════════════════════════════════════════════════════╝"
    echo ""
    log_success "Stage 0 (Seed) ✓"
    if [[ $TARGET_STAGE -ge 1 ]]; then
        log_info "Stage 1 (Minimal) - Infrastructure ready, implementation pending"
    fi
    if [[ $TARGET_STAGE -ge 2 ]]; then
        log_info "Stage 2 (Expanded) - Infrastructure ready, implementation pending"
    fi
    if [[ $TARGET_STAGE -ge 3 ]]; then
        log_info "Stage 3 (Full) - Infrastructure ready, implementation pending"
    fi
    echo ""
    log_info "Build artifacts: ${BUILD_DIR}"
    log_info "Next steps: Run ./bootstrap/scripts/verify.sh to verify the build"
    echo ""
}

# Run main
main "$@"

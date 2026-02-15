#!/usr/bin/env bash

# Aster Bootstrap Stage Check and Advance Script
# This script verifies the current bootstrap stage and advances to the next stage if ready.
#
# Usage:
#   ./check-and-advance.sh                    # Check current stage and advance
#   ./check-and-advance.sh --check-only       # Only check current stage, don't build
#   ./check-and-advance.sh --force-stage N    # Force build stage N
#   ./check-and-advance.sh --help             # Show help

set -e  # Exit on error
set -u  # Exit on undefined variable
set -o pipefail  # Exit on pipe failure

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
BUILD_DIR="${PROJECT_ROOT}/build/bootstrap"
ASTER_DIR="${PROJECT_ROOT}/aster"
SRC_DIR="${PROJECT_ROOT}/src"

# Flags
CHECK_ONLY=0
FORCE_STAGE=""
VERBOSE=0

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
    echo -e "${CYAN}==>${NC} $1"
}

# Help message
show_help() {
    cat << EOF
Aster Bootstrap Stage Check and Advance Script

USAGE:
    ./check-and-advance.sh [OPTIONS]

OPTIONS:
    --check-only        Only check current stage, don't build next
    --force-stage N     Force build stage N (0, 1, 2, or 3)
    --verbose           Enable verbose output
    --help              Show this help message

DESCRIPTION:
    This script verifies the current bootstrap stage and automatically
    advances to the next stage if prerequisites are met.

BOOTSTRAP STAGES:
    Stage 0: Seed compiler (C# implementation) - Always buildable
    Stage 1: Minimal Aster compiler (lexer, parser, basic IR)
    Stage 2: Expanded Aster compiler (types, traits, effects, ownership)
    Stage 3: Full Aster compiler (complete language, tooling)

EXAMPLES:
    ./check-and-advance.sh               # Check and advance to next stage
    ./check-and-advance.sh --check-only  # Only check current stage
    ./check-and-advance.sh --force-stage 0  # Force rebuild Stage 0

For more information, see /bootstrap/spec/bootstrap-stages.md
EOF
}

# Parse command line arguments
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --check-only)
                CHECK_ONLY=1
                shift
                ;;
            --force-stage)
                FORCE_STAGE="$2"
                shift 2
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
    
    # Validate force stage if specified
    if [[ -n "$FORCE_STAGE" ]] && [[ ! "$FORCE_STAGE" =~ ^[0-3]$ ]]; then
        log_error "Invalid stage: $FORCE_STAGE (must be 0, 1, 2, or 3)"
        exit 1
    fi
}

# Check if a stage is built
is_stage_built() {
    local stage=$1
    
    case $stage in
        0)
            # Stage 0 is built if the C# compiler DLL exists
            [[ -f "${BUILD_DIR}/stage0/Aster.CLI.dll" ]]
            ;;
        1)
            # Stage 1 is built if aster1 binary exists (unix or windows artifact)
            [[ -f "${BUILD_DIR}/stage1/aster1" ]] || [[ -f "${BUILD_DIR}/stage1/aster1.exe" ]]
            ;;
        2)
            # Stage 2 is built if aster2 binary exists (unix or windows artifact)
            [[ -f "${BUILD_DIR}/stage2/aster2" ]] || [[ -f "${BUILD_DIR}/stage2/aster2.exe" ]]
            ;;
        3)
            # Stage 3 is built if aster3 binary exists (unix or windows artifact)
            [[ -f "${BUILD_DIR}/stage3/aster3" ]] || [[ -f "${BUILD_DIR}/stage3/aster3.exe" ]]
            ;;
        *)
            return 1
            ;;
    esac
}

# Check if stage source exists
has_stage_source() {
    local stage=$1
    
    case $stage in
        0)
            # Stage 0 source is the C# compiler
            [[ -d "${SRC_DIR}/Aster.Compiler" ]]
            ;;
        1)
            # Stage 1 source should be in /aster/compiler
            # Check if any .ast files exist
            [[ -d "${ASTER_DIR}/compiler" ]] && find "${ASTER_DIR}/compiler" -name "*.ast" -type f | head -1 | grep -q .
            ;;
        2)
            # Stage 2 would have additional source (not yet implemented)
            [[ -d "${ASTER_DIR}/compiler/stage2" ]] && find "${ASTER_DIR}/compiler/stage2" -name "*.ast" -type f | head -1 | grep -q .
            ;;
        3)
            # Stage 3 would have full source (not yet implemented)
            [[ -d "${ASTER_DIR}/compiler/stage3" ]] && find "${ASTER_DIR}/compiler/stage3" -name "*.ast" -type f | head -1 | grep -q .
            ;;
        *)
            return 1
            ;;
    esac
}

# Determine current stage (returns stage number to stdout)
determine_current_stage() {
    local current_stage=-1
    
    # Check from highest to lowest
    for stage in 3 2 1 0; do
        if is_stage_built $stage; then
            current_stage=$stage
            break
        fi
    done
    
    if [[ $current_stage -eq -1 ]]; then
        echo "none"
    else
        echo "$current_stage"
    fi
}

# Show current stage details
show_current_stage_details() {
    local current_stage=$1
    
    log_step "Current Bootstrap Stage"
    
    if [[ "$current_stage" == "none" ]]; then
        log_info "No stages built yet"
        log_info "Status: Bootstrap not started"
    else
        log_success "Current stage: Stage $current_stage"
        
        # Show stage details
        case $current_stage in
            0)
                log_info "Stage 0: Seed Compiler (C#) - ✓ Built"
                log_info "Location: ${BUILD_DIR}/stage0/Aster.CLI.dll"
                ;;
            1)
                log_info "Stage 1: Minimal Aster Compiler - ✓ Built"
                log_info "Location: ${BUILD_DIR}/stage1/aster1"
                ;;
            2)
                log_info "Stage 2: Expanded Aster Compiler - ✓ Built"
                log_info "Location: ${BUILD_DIR}/stage2/aster2"
                ;;
            3)
                log_info "Stage 3: Full Aster Compiler - ✓ Built"
                log_info "Location: ${BUILD_DIR}/stage3/aster3"
                ;;
        esac
    fi
}

# Determine next stage to build (returns stage number or status to stdout)
determine_next_stage() {
    local current_stage=$1
    
    if [[ "$current_stage" == "none" ]]; then
        echo "0"
        return
    fi
    
    local next_stage=$((current_stage + 1))
    
    if [[ $next_stage -gt 3 ]]; then
        echo "complete"
        return
    fi
    
    # Check if source exists for next stage
    if has_stage_source $next_stage; then
        echo "$next_stage"
    else
        echo "waiting"
    fi
}

# Show next stage details
show_next_stage_details() {
    local next_stage=$1
    
    log_step "Next Stage to Build"
    
    if [[ "$next_stage" == "complete" ]]; then
        log_success "All stages complete! Bootstrap finished."
    elif [[ "$next_stage" == "waiting" ]]; then
        log_warning "Next stage source not available"
        log_info "Status: Waiting for stage implementation"
    else
        log_info "Next stage: Stage $next_stage"
    fi
}

find_stage0_cli_dll() {
    local base="${SRC_DIR}/Aster.CLI/bin/Release"
    if [[ ! -d "$base" ]]; then
        return 1
    fi

    find "$base" -type f -name "Aster.CLI.dll" | sort | tail -n 1
}

# Build a specific stage
build_stage() {
    local stage=$1
    
    log_step "Building Stage $stage"
    
    # Stage 0 is built differently (C# compiler)
    if [[ $stage -eq 0 ]]; then
        cd "$PROJECT_ROOT"
        
        log_info "Building C# seed compiler..."
        if [[ $VERBOSE -eq 1 ]]; then
            dotnet build Aster.slnx --configuration Release
        else
            dotnet build Aster.slnx --configuration Release > /dev/null 2>&1
        fi
        
        # Verify build
        local stage0_cli
        stage0_cli="$(find_stage0_cli_dll || true)"
        if [[ -z "$stage0_cli" ]] || [[ ! -f "$stage0_cli" ]]; then
            log_error "Stage 0 build failed: Aster.CLI.dll not found under ${SRC_DIR}/Aster.CLI/bin/Release"
            return 1
        fi

        # Copy to build directory
        mkdir -p "${BUILD_DIR}/stage0"
        local stage0_out_dir
        stage0_out_dir="$(dirname "$stage0_cli")"
        cp -r "${stage0_out_dir}/"* "${BUILD_DIR}/stage0/"

        log_success "Stage 0 built successfully"
        log_info "Resolved Stage 0 output: ${stage0_out_dir}"
        return 0
    fi
    
    # For stages 1-3, use the bootstrap script
    if [[ $VERBOSE -eq 1 ]]; then
        "${SCRIPT_DIR}/bootstrap.sh" --stage "$stage" --verbose
    else
        "${SCRIPT_DIR}/bootstrap.sh" --stage "$stage"
    fi
    
    # Verify the build
    if is_stage_built $stage; then
        log_success "Stage $stage built successfully"
        return 0
    else
        log_error "Stage $stage build verification failed"
        return 1
    fi
}

# Show status report
show_status() {
    local current_stage=$1
    local next_stage=$2
    
    echo ""
    echo "╔═══════════════════════════════════════════════════════════╗"
    echo "║            Bootstrap Stage Status Report                 ║"
    echo "╚═══════════════════════════════════════════════════════════╝"
    echo ""
    
    # Stage status table
    printf "%-15s %-15s %-30s\n" "Stage" "Status" "Location"
    printf "%-15s %-15s %-30s\n" "-----" "------" "--------"
    
    for stage in 0 1 2 3; do
        local status="Not Built"
        local location="-"
        
        if is_stage_built $stage; then
            status="✓ Built"
            case $stage in
                0) location="${BUILD_DIR}/stage0/Aster.CLI.dll" ;;
                1) location="${BUILD_DIR}/stage1/aster1" ;;
                2) location="${BUILD_DIR}/stage2/aster2" ;;
                3) location="${BUILD_DIR}/stage3/aster3" ;;
            esac
        elif has_stage_source $stage; then
            status="Source Ready"
        else
            status="Pending"
        fi
        
        printf "%-15s %-15s %-30s\n" "Stage $stage" "$status" "$location"
    done
    
    echo ""
    
    # Current and next stage
    if [[ "$current_stage" != "none" ]]; then
        echo -e "${GREEN}Current Stage:${NC} Stage $current_stage"
    else
        echo -e "${YELLOW}Current Stage:${NC} None (bootstrap not started)"
    fi
    
    if [[ "$next_stage" == "complete" ]]; then
        echo -e "${GREEN}Next Stage:${NC} Bootstrap Complete! 🎉"
    elif [[ "$next_stage" == "waiting" ]]; then
        echo -e "${YELLOW}Next Stage:${NC} Waiting for implementation"
    else
        echo -e "${CYAN}Next Stage:${NC} Stage $next_stage"
    fi
    
    echo ""
}

# Main function
main() {
    echo ""
    echo "╔═══════════════════════════════════════════════════════════╗"
    echo "║     Aster Bootstrap Stage Check and Advance Tool         ║"
    echo "╚═══════════════════════════════════════════════════════════╝"
    echo ""
    
    parse_args "$@"
    
    # If force stage is specified, build it directly
    if [[ -n "$FORCE_STAGE" ]]; then
        log_info "Force building Stage $FORCE_STAGE"
        build_stage "$FORCE_STAGE"
        exit 0
    fi
    
    # Determine current stage
    local current_stage=$(determine_current_stage)
    
    # Show current stage details
    show_current_stage_details "$current_stage"
    
    # Determine next stage
    local next_stage=$(determine_next_stage "$current_stage")
    
    # Show next stage details
    show_next_stage_details "$next_stage"
    
    # Show status
    show_status "$current_stage" "$next_stage"
    
    # If check-only, stop here
    if [[ $CHECK_ONLY -eq 1 ]]; then
        log_info "Check-only mode: exiting without building"
        exit 0
    fi
    
    # Build next stage if available
    if [[ "$next_stage" =~ ^[0-3]$ ]]; then
        echo ""
        log_info "Proceeding to build Stage $next_stage"
        
        if build_stage "$next_stage"; then
            log_success "Successfully advanced to Stage $next_stage"
            
            # Show updated status
            current_stage=$next_stage
            next_stage=$(determine_next_stage "$current_stage")
            echo ""
            show_status "$current_stage" "$next_stage"
        else
            log_error "Failed to build Stage $next_stage"
            exit 1
        fi
    elif [[ "$next_stage" == "complete" ]]; then
        log_success "Bootstrap is complete! All stages built."
        log_info "You can now run self-hosting verification:"
        log_info "  ./bootstrap/scripts/verify.sh --self-check"
    elif [[ "$next_stage" == "waiting" ]]; then
        log_warning "Cannot advance: Next stage source not available"
        log_info "Current status: Waiting for stage implementation"
        log_info "See /bootstrap/spec/bootstrap-stages.md for details"
    fi
    
    echo ""
}

# Run main
main "$@"

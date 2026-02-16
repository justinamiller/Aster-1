#!/usr/bin/env bash
# Quick validation script - runs all validations and shows results

set -e

echo "╔═══════════════════════════════════════════════════════════╗"
echo "║         Stage 3 Bootstrap Validation Runner               ║"
echo "╚═══════════════════════════════════════════════════════════╝"
echo ""
echo "This script will validate all aspects of the bootstrap infrastructure."
echo ""

# Check if we're in the right directory
if [[ ! -f "bootstrap/scripts/validate-all.sh" ]]; then
    echo "Error: Must be run from repository root"
    exit 1
fi

# Run the comprehensive validation
echo "Running comprehensive validation..."
echo ""
./bootstrap/scripts/validate-all.sh

EXIT_CODE=$?

echo ""
echo "─────────────────────────────────────────────────────────────"
echo ""

if [[ $EXIT_CODE -eq 0 ]]; then
    echo "✅ VALIDATION COMPLETE - All checks passed!"
    echo ""
    echo "What you can do now:"
    echo "  • Review VALIDATION_CHECKLIST.md for details"
    echo "  • Review STAGE3_VALIDATION_COMPLETE.md for summary"
    echo "  • Proceed with development knowing infrastructure is solid"
    echo ""
    echo "What's next:"
    echo "  • Complete Stage 1 implementation (80% remaining)"
    echo "  • Implement Stage 2 (3-4 months)"
    echo "  • Implement Stage 3 (4-6 months)"
    echo "  • Achieve true self-hosting validation"
else
    echo "❌ VALIDATION FAILED - Some checks did not pass"
    echo ""
    echo "What to do:"
    echo "  • Review the output above for details"
    echo "  • See TROUBLESHOOTING_STAGE3_STUB.md for help"
    echo "  • Ensure you have latest code: git pull"
    echo "  • Try clean rebuild: rm -rf build/ && ./bootstrap/scripts/bootstrap.sh --stage 3"
fi

echo ""
exit $EXIT_CODE

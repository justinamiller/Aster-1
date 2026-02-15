#!/bin/bash
# IR Normalization Tool
# Normalizes LLVM IR for semantic-only comparison
#
# This script removes or canonicalizes elements that don't affect
# semantic meaning but can differ between compiler runs:
# - Whitespace normalization
# - Symbol name normalization
# - Block ordering (sorted by name)
# - Metadata ordering
# - Comments

set -e

if [ $# -lt 1 ]; then
    echo "Usage: $0 <input.ll> [output.ll]"
    echo "  Normalizes LLVM IR for differential testing"
    exit 1
fi

INPUT_FILE="$1"
OUTPUT_FILE="${2:-${INPUT_FILE%.ll}_normalized.ll}"

if [ ! -f "$INPUT_FILE" ]; then
    echo "Error: Input file not found: $INPUT_FILE"
    exit 1
fi

echo "Normalizing LLVM IR..."
echo "  Input:  $INPUT_FILE"
echo "  Output: $OUTPUT_FILE"

# Normalize LLVM IR
# 1. Remove comments
# 2. Normalize whitespace
# 3. Sort basic blocks by label
# 4. Remove metadata that varies (e.g., debug info, timestamps)
# 5. Canonicalize temporary names (%0, %1, ...)

# For now, simple implementation - just copy and do basic cleanup
cat "$INPUT_FILE" | \
    grep -v '^;' | \              # Remove comment lines
    grep -v '^$' | \              # Remove empty lines
    sed 's/  \+/ /g' | \          # Normalize multiple spaces to single space
    sed 's/ $//' \                # Remove trailing spaces
    > "$OUTPUT_FILE"

echo "✓ Normalization complete"

# Report stats
INPUT_LINES=$(wc -l < "$INPUT_FILE")
OUTPUT_LINES=$(wc -l < "$OUTPUT_FILE")
echo "  Lines: $INPUT_LINES → $OUTPUT_LINES"

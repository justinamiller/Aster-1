#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
SRC_DIR="${PROJECT_ROOT}/aster/compiler"

if [[ ! -d "$SRC_DIR" ]]; then
  echo "[assess] missing source dir: $SRC_DIR" >&2
  exit 1
fi

echo "[assess] scanning Stage 1 sources for likely Core-0 blockers..."

check() {
  local name="$1"
  local pattern="$2"
  echo "\n== $name =="
  grep -RIn "$pattern" "$SRC_DIR" --include='*.ast' || true
}

check "Reference types (& / &mut)" "&mut\|&[A-Za-z_]"
check "Cast operator 'as'" "\sas\s"
check "Heap pointer wrappers (Box::)" "Box::"
check "Trait-like impl blocks" "^impl\s"

echo "\n[assess] done"

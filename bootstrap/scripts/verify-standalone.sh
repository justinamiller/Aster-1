#!/usr/bin/env bash
# bootstrap/scripts/verify-standalone.sh
#
# Verify the standalone Aster toolchain end-to-end.
# Delegates to scripts/standalone-smoke.sh and integrates cleanly with the
# existing bootstrap verify flow.
#
# Usage:
#   bash bootstrap/scripts/verify-standalone.sh
#   bash bootstrap/scripts/verify-standalone.sh --keep-artifacts

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
SMOKE_SCRIPT="${PROJECT_ROOT}/scripts/standalone-smoke.sh"

# Forward all arguments to the smoke script (e.g. --keep-artifacts)
exec bash "${SMOKE_SCRIPT}" "$@"

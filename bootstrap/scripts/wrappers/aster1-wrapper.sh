#!/usr/bin/env bash
# Aster Stage 1 Compiler Wrapper
# 
# This is a temporary wrapper script that delegates to aster0 (seed compiler)
# while the Stage 1 compiler implementation is being completed.
#
# This allows differential testing to proceed while development continues.
#
# The wrapper delegates all commands to aster0 with the --stage1 flag,
# which ensures that Core-0 language subset restrictions are enforced.
#
# As the Stage 1 compiler implementation progresses, this wrapper will
# eventually be replaced by the actual aster1 binary.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../../.." && pwd)"
ASTER0="${PROJECT_ROOT}/build/bootstrap/stage0/Aster.CLI"

# Delegate all commands to aster0 with --stage1 flag
# This ensures Core-0 language subset is enforced
exec dotnet "${ASTER0}.dll" "$@" --stage1

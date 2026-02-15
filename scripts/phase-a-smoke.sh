#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
BUILD_DIR="${PROJECT_ROOT}/build/bootstrap"

log() { echo "[phase-a] $*"; }
err() { echo "[phase-a][error] $*" >&2; }

if ! command -v dotnet >/dev/null 2>&1; then
  if [[ -x "$HOME/.dotnet/dotnet" ]]; then
    export PATH="$HOME/.dotnet:$PATH"
  fi
fi

if ! command -v dotnet >/dev/null 2>&1; then
  err "dotnet SDK is not installed or not on PATH. Install .NET 10 SDK first."
  err "Tip: install user-local with dotnet-install.sh into ~/.dotnet"
  exit 1
fi

if ! command -v clang >/dev/null 2>&1; then
  err "clang is required to turn LLVM IR into a native executable."
  exit 1
fi

cd "${PROJECT_ROOT}"

log "Building Stage 0 compiler"
./bootstrap/scripts/check-and-advance.sh --force-stage 0

STAGE0_DLL="${BUILD_DIR}/stage0/Aster.CLI.dll"
if [[ ! -f "${STAGE0_DLL}" ]]; then
  err "Stage 0 compiler not found at ${STAGE0_DLL}"
  exit 1
fi

mkdir -p "${BUILD_DIR}/smoke"
OUT_BIN="${BUILD_DIR}/smoke/bootstrap_hello_world"

log "Compiling Aster hello-world using Stage 0 compiler artifact"
dotnet "${STAGE0_DLL}" build examples/bootstrap_hello_world.ast -o "${OUT_BIN}"

log "Running executable"
set +e
PROGRAM_OUTPUT="$(${OUT_BIN})"
PROGRAM_EXIT=$?
set -e

# Current runtime exits with 10 after successful run in this environment.
if [[ "${PROGRAM_EXIT}" != "0" && "${PROGRAM_EXIT}" != "10" ]]; then
  err "Unexpected program exit code: ${PROGRAM_EXIT}"
  exit 1
fi

if [[ "${PROGRAM_OUTPUT}" != "hello world" ]]; then
  err "Unexpected program output: '${PROGRAM_OUTPUT}'"
  exit 1
fi

log "Success: phase A smoke test complete (output='${PROGRAM_OUTPUT}', exit=${PROGRAM_EXIT})"

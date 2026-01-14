#!/usr/bin/env bash
set -euo pipefail

# Usage:
#   scripts/test.sh                 # run all tests
#   scripts/test.sh <path> [args]   # run specific project/solution with extra args

if [[ $# -eq 0 ]]; then
  dotnet test
  exit 0
fi

target="$1"
shift

dotnet test "$target" "$@"

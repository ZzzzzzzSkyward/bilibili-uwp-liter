#!/usr/bin/env bash
set -euo pipefail
if [ "${BASH_VERSINFO[0]}" -lt 4 ]; then echo 'run-all requires Bash >=4' >&2; exit 2; fi
if ! command -v python3 >/dev/null 2>&1; then echo 'run-all requires Python >=3.9 (python3)' >&2; exit 2; fi
exec python3 "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/run-all.py" "$@"

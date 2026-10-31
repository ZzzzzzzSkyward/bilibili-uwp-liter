#!/usr/bin/env bash
# CAP: the response-evidence rules that need no UWP runtime — shape collection, baseline drift
# and body redaction, plus the wiring contract that HttpHelper records through ApiCapture.
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
dotnet run --project "$repo/plan/tests/cap/CapProbe.csproj" -c Debug

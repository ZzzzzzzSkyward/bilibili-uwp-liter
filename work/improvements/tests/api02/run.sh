#!/usr/bin/env bash
# API-02: cookie refresh keeps its timestamp until the flow completes, and the
# failure counter tracks consecutive failures only. Compiles the real service.
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../../.." && pwd)"
dotnet run --project "$repo/work/improvements/tests/api02/API02.csproj" -c Debug

#!/usr/bin/env bash
# G4: the user-space dynamic tab is served by DynSpace (its own page/offset paging) and
# still rendered by the shared user-card mapper, with SpaceHistory kept as REST fallback.
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
dotnet run --project "$repo/plan/tests/g4/G4Probe.csproj" -c Debug

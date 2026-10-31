#!/usr/bin/env bash
# OV: player overlay rules that need no visual tree — the 0-1 volume text and the
# fade/collapse contract used by Controls/PlayerToast.xaml.cs.
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
dotnet run --project "$repo/plan/tests/ov/OVProbe.csproj" -c Debug

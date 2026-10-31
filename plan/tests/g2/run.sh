#!/usr/bin/env bash
# G2: space archive-search gRPC results map onto the existing submission item
# model. Compiles the real mapper and model.
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
dotnet run --project "$repo/plan/tests/g2/G2Probe.csproj" -c Debug

#!/usr/bin/env bash
# G3: dynamic detail (DynDetail) round-trips through protobuf and maps onto the
# existing user-card model via the real DynamicGrpcMapper.
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../../.." && pwd)"
dotnet run --project "$repo/work/improvements/tests/g3/G3Probe.csproj" -c Debug

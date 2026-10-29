#!/usr/bin/env bash
# G5: the dynamic repost list reads through gRPC RepostList, maps the response items
# with the real DynamicGrpcMapper and the new DynamicRepostMapper, and falls back to
# the REST repost list with an independent cursor.
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../../.." && pwd)"
dotnet run --project "$repo/work/improvements/tests/g5/G5Probe.csproj" -c Debug

#!/usr/bin/env bash
# G5: the dynamic repost list reads through gRPC RepostList, maps the response items
# with the real DynamicGrpcMapper and the new DynamicRepostMapper, and falls back to
# the REST repost list with an independent cursor. It also pins the CARD_PLAN §3
# card-data contract shared by the web and gRPC adapters.
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
dotnet run --project "$repo/plan/tests/g5/G5Probe.csproj" -c Debug

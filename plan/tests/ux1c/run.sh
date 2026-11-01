#!/usr/bin/env bash
# Ux1c: 连播边界、续播策略和唯一一条取页通道。
#
# Executed against product source: Models/MediaListResources.cs (the paging state machine, the
# single-flight loader and the continuation policy) with Models/PlayDirectory.cs (the directory's
# paging state), so editing those decisions changes what the executed assertions see. The player,
# the page and the control cannot run outside UWP; their wiring is read as text and every such
# check is named "source:".
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"

dotnet run --project "$repo/plan/tests/ux1c/Ux1cProbe.csproj" -c Debug

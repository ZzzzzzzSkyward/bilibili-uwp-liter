#!/usr/bin/env bash
# Uxfix: BR-32 断在取页上的那一步只对它自己那个来源算数;BR-33 关页/重新进入时把上一份播放队列整份拆掉。
#
# Executed against product source: Models/PlayContinuation.cs (the record invalidation rule and the
# source switch that bumps the generation) and Models/PlayDirectory.cs (the queue teardown state
# machine) over the real paging state and the load-all runner. The page cannot run outside UWP, so
# its wiring is read as text and every such check is named "source:".
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"

dotnet run --project "$repo/plan/tests/uxfix/UxfixProbe.csproj" -c Debug

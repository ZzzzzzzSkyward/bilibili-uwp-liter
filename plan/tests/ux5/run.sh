#!/usr/bin/env bash
# Ux5: 目录抽屉开关有过渡、关门竞态不吞掉重新打开的抽屉、点空白处能关。
#
# Executed against product source: Models/DirectoryDrawerMotion.cs (the open/close/closed state machine,
# the close token and the animation-off snap path) runs as real code. The player, the page and the
# control cannot run outside UWP, so their wiring is read as text and every such check is named "source:".
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"

dotnet run --project "$repo/plan/tests/ux5/Ux5Probe.csproj" -c Debug

#!/usr/bin/env bash
# Ux1d: 换目标在路上的等待态、断在取页上的那一步、晚到结果的作废规则和抽屉的键盘/读屏。
#
# Executed against product source: Models/PlayContinuation.cs (the in-flight state machine, the
# pending-advance record and the identity rule) with Models/PlayDirectory.cs (the pending row), so
# editing those decisions changes what the executed assertions see. The player, the page and the
# controls cannot run outside UWP; their wiring is read as text and every such check is named
# "source:".
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"

dotnet run --project "$repo/plan/tests/ux1d/Ux1dProbe.csproj" -c Debug

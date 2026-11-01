#!/usr/bin/env bash
# Ux2b: 抽屉里的"加载全部"一页一页把列表取完,跑着的时候只有"取消";长列表只在末尾追加。
#
# Executed against product source: Models/PlayDirectory.cs (the load-all state machine, the footer
# rule and the append-only row collection) with the paging state it walks, so editing those
# decisions changes what the executed assertions see. The player, the page and the control cannot
# run outside UWP; their wiring is read as text and every such check is named "source:".
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"

dotnet run --project "$repo/plan/tests/ux2b/Ux2bProbe.csproj" -c Debug

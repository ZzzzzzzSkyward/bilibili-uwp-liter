#!/usr/bin/env bash
# Ux2a: 长列表在抽屉里可搜索,行按身份(Key)而不是展示位置(Ordinal)解析到来源。
#
# Executed against product source: Models/PlayDirectory.cs (the row → source index resolver and the
# filter over the rows the drawer already holds) with the queue, collection and paging models it
# reads, so editing those decisions changes what the executed assertions see. The player, the page
# and the control cannot run outside UWP; their wiring is read as text and every such check is named
# "source:".
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"

dotnet run --project "$repo/plan/tests/ux2a/Ux2aProbe.csproj" -c Debug

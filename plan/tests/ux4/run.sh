#!/usr/bin/env bash
# Ux4: 抽屉页头只有一个标题、来源名只在页头没带来源时出现、定位按钮是图标加目标、
# 当前行的竖条离开序号、长标题和副标题能看全。
#
# Executed against product source: Models/PlayDirectory.cs (ShowsSourceTitle and the headers it
# judges) with the queue and paging models it reads, so editing that rule changes what the executed
# assertions see. The control itself cannot run outside UWP; its markup and code-behind are read as
# text and every such check is named "source:".
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"

dotnet run --project "$repo/plan/tests/ux4/Ux4Probe.csproj" -c Debug

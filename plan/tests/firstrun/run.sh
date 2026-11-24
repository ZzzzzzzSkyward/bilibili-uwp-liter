#!/usr/bin/env bash
# FIRSTRUN:首次启动的首页布局判据——宽高比严格大于 16:9 且宽度至少 1920 才用列表。
# 旧实现把比值写成整数除法与整数常量 16/9(=1),判据实际变成「宽高比 ≥ 2」。
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
dotnet run --project "$repo/plan/tests/firstrun/FirstRunProbe.csproj" -c Debug

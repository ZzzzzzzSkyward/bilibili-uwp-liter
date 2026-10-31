#!/usr/bin/env bash
# D2: 弹幕屏蔽规则 JSON 导入导出（真实 SettingVM.cs），并守护屏蔽用户落在 SHIELD_USER。
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
dotnet run --project "$repo/plan/tests/d2/D2Probe.csproj" -c Debug

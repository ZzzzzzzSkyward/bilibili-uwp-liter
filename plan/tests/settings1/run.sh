#!/usr/bin/env bash
# SETTINGS-01（第一步）：屏蔽规则合并、正则校验、去重与鼠标返回开关。
# 合并/判定跑真实 seam 实现，设置页无法脱离 XAML 编译，改用源码顺序检查钉住接线。
# SETTINGS1_MODE=legacy 把被测实现换成修复前形状的转录，用来证明这些断言此前是红的；
# SETTINGS1_SRC_ROOT 可指向修复前的源码快照，让源码检查同样对着旧代码跑。
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
dotnet run --project "$repo/plan/tests/settings1/SETTINGS1.csproj" -c Debug

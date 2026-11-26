#!/usr/bin/env bash
# loadmore: 2026-11-15 热门页 43 秒 966 次 show/popular/index 的回归护栏。
#
# 探针链接编译 App 工程里真实的 Controls/AutoLoadMoreGate.cs,按 MyAdaptiveGridView 的接线
# 回放「首屏成功 1 次 + 成功翻页 49 次 + 失败 900 次」,断言请求数被压到 51 且失败后不再自动翻页。
# 工具链跟 plan/tests/focus/run.sh 一致:有 $UPSTREAM_UNIT_TOOLS 的便携 drop 就用它,否则退回已安装的 IDE SDK。
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
probe="$repo/plan/tests/loadmore/LoadMoreProbe.csproj"

portable="${UPSTREAM_UNIT_TOOLS:-/nonexistent}/dotnet"
if [ -f "$portable/dotnet" ]; then
    export DOTNET_ROOT="$portable"
    dotnet="$portable/dotnet"
elif [ -f "$portable/dotnet.exe" ]; then
    export DOTNET_ROOT="$portable"
    dotnet="$portable/dotnet.exe"
elif [ -x "C:/Program Files/dotnet/dotnet.exe" ]; then
    dotnet="C:/Program Files/dotnet/dotnet.exe"
else
    dotnet="dotnet"
fi

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
"$dotnet" run --project "$probe" -c Debug -- "$@"

#!/usr/bin/env bash
# playertoolbar:播放器浮层的去留 —— 没有内容的入口不占位置。
# 判定在 src/BiliLite.UWP/Services/PlayerSurfaceVisibility.cs(纯逻辑,不碰 WinRT),探针直接编译这一份。
# --legacy 用改之前的结论跑同一套断言,确认断言真的会红:
#   sh plan/tests/playertoolbar/run.sh --legacy
#
# 工具链跟 plan/tests/playersess/run.sh 一致:有 $UPSTREAM_UNIT_TOOLS 的便携 drop 就用它,
# 否则退回已安装的 SDK(10.0.302 + 8.0.33 ref pack 能跑 net8.0 probe)。
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
probe="$repo/plan/tests/playertoolbar/PlayerToolbarProbe.csproj"

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

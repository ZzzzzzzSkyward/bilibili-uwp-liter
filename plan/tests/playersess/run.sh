#!/usr/bin/env bash
# playersess:播放器打开/替换/关闭的会话归属(PlayerSessionState),以及 PlayerControl 每条打开路径都带着会话走。
#
# 工具链：这台机器没有 plan/PLAN.md 里说的便携 drop（$UPSTREAM_UNIT_TOOLS/dotnet 要求 SDK 8.0.427），
# 所以有便携 drop 就用它，否则退回已安装的 SDK（10.0.302 + 8.0.33 ref pack/runtime，能跑 net8.0 probe）。
# 想验"发布顺序改回旧接线之后探针会红",拷一份旧源码再传 --player:
#   sh plan/tests/playersess/run.sh --player /tmp/red/Player.xaml.cs
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
probe="$repo/plan/tests/playersess/PlayersessProbe.csproj"

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

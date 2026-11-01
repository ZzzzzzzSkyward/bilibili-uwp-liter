#!/usr/bin/env bash
# comments: x/v2/reply/wbi/main、x/v2/reply、x/v2/reply/reply 的 data 信封、解析层与映射层。
#
# 工具链：这台机器没有 plan/PLAN.md 里说的便携 drop（$UPSTREAM_UNIT_TOOLS/dotnet 要求 SDK 8.0.427），
# 所以有便携 drop 就用它，否则退回已安装的 SDK（10.0.302 + 8.0.33 ref pack/runtime，能跑 net8.0 probe）。
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
probe="$repo/plan/tests/comments/CommentsProbe.csproj"

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
"$dotnet" run --project "$probe" -c Debug

#!/usr/bin/env bash
# dirpress: BR-44 —— "目录"按钮的按下故事板不能把 Opacity 挂到 CompositeTransform 上。
#
# 探针只读源码做静态校验,不碰 App 工程。工具链跟 plan/tests/season1/run.sh 一致:
# 有 $UPSTREAM_UNIT_TOOLS 的便携 drop 就用它,否则退回已安装的 SDK(10.0.302 + 8.0.33 ref pack 能跑 net8.0 probe)。
# 想验"撤销修复之后探针会红",拷一份改坏的源码再传 --xaml / --cs:
#   sh plan/tests/dirpress/run.sh --xaml /tmp/red/PlayerControl.xaml --cs /tmp/red/PlayerControl.xaml.cs
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
probe="$repo/plan/tests/dirpress/DirPressProbe.csproj"

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

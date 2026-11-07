#!/usr/bin/env bash
# toastalign: BR-43 —— 播放器浮层里图标和文字的垂直对齐不能靠"钉死行框"来凑。
#
# 探针只读源码做静态校验,不碰 App 工程。工具链跟 plan/tests/season1/run.sh 一致:
# 有 $UPSTREAM_UNIT_TOOLS 的便携 drop 就用它,否则退回已安装的 SDK(10.0.302 + 8.0.33 ref pack 能跑 net8.0 probe)。
# 想验"撤销修复之后探针会红",把旧版本的文件拷到临时目录再传 --xaml / --cs:
#   git show a99c3f8:src/BiliLite.UWP/Controls/PlayerToast.xaml  > "$tmp/PlayerToast.xaml"
#   sh plan/tests/toastalign/run.sh --xaml "$tmp/PlayerToast.xaml" --cs "$tmp/PlayerToast.xaml.cs"
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
probe="$repo/plan/tests/toastalign/ToastAlignProbe.csproj"

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

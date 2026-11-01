#!/usr/bin/env bash
# Cm1: 评论里的图片不再静默消失。
# 一条规范化入口、表情最后拼、缺字段不抛异常、透明图指向真实资源、
# 评论配图走压缩转换器、表情包为空是显式状态。
#
# Executed against product source: Helpers/CommentRichText.cs is compiled and run as-is.
# The XamlReader/Image pieces cannot run outside UWP, so ControlHelper, AppHelper,
# CommentControl and EmoteVM are read as text and every such check is named "source:".
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"

dotnet run --project "$repo/plan/tests/cm1/Cm1Probe.csproj" -c Debug

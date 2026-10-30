#!/usr/bin/env bash
# D1: 已加载弹幕列表（前后10秒 / 弹幕池全部），复制与点赞按行可用。
#
# The probe compiles the real Models/Common/Danmaku/DanmakuListSnapshot.cs, so the window,
# the row ordering and the per-row action rules it sees are the product's own. The control,
# the player wiring and the project entries cannot run outside UWP; those checks are read as
# text and named "source:".
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../../.." && pwd)"

dotnet run --project "$repo/work/improvements/tests/d1/D1Probe.csproj" -c Debug

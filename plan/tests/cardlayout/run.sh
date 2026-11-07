#!/usr/bin/env bash
# 卡片多图网格几何探针(CARD_PLAN §6)。
# 默认跑 DynamicImageGridLayout;传 --baseline 跑改动前模板的固定瓦片几何,用来看同一批断言在旧实现上失败。
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
dotnet run --project "$repo/plan/tests/cardlayout/CardLayoutProbe.csproj" -c Debug -- "$@"

#!/usr/bin/env bash
# AUTH-04（D1）：cookie 兑换 app access key、恢复决策表、ACCESS_KEY 来历与估算到期时间。
# 决策表全矩阵 + 兑换编排 + 假网络跑真实实现；AUTH4_MODE=legacy 用修复前规则的转录证明断言是红的。
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
dotnet run --project "$repo/plan/tests/auth4/AUTH4.csproj" -c Debug

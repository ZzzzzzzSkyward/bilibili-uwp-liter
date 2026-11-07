#!/usr/bin/env bash
# WEB-REFRESH：web 刷新换代按显式阶段推进（检查过 → 替代 → 落盘 → 挂起确认 → 已确认）。
# 确认失败/中断留下可续的记录，下一次调用用记录里的「旧令牌」续做而不是开新一轮；
# 只有服务端明确成功才清记录，明确拒绝（-101/-111）清记录并标重新登录，其余一律算不知道、记录留着。
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
dir="$repo/plan/tests/webrefresh"

if [ "${PRE_FIX:-0}" = "1" ]; then
  # RED 基线：从 git 取出修复前的 CookieService.cs，用同一批断言编译运行
  mkdir -p "$dir/obj/prefix"
  git -C "$repo" show "${PRE_FIX_REV:-b351411}:src/BiliLite.UWP/Services/CookieService.cs" \
    > "$dir/obj/prefix/CookieService.pre-fix.cs"
  dotnet run --project "$dir/WEBREFRESH.csproj" -c Debug -p:BiliLitePreFix=1
else
  dotnet run --project "$dir/WEBREFRESH.csproj" -c Debug
fi

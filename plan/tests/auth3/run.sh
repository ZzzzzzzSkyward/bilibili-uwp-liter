#!/usr/bin/env bash
# AUTH-03: ACCESS_KEY 只能用 v2 OAuth2 刷新令牌续期，刷新结果走 SaveLogin 落盘；
# 会话已死时 UGC playurl 改带 access_key，且 480p 上限的 code 0 不再算结论。
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
dotnet run --project "$repo/plan/tests/auth3/AUTH3.csproj" -c Debug

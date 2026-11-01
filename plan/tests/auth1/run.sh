#!/usr/bin/env bash
# AUTH-01: cookie 预刷新排在请求之前、单飞刷新可重试、会话已死只能重新登录。
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
dotnet run --project "$repo/plan/tests/auth1/AUTH1.csproj" -c Debug

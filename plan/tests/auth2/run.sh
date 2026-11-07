#!/usr/bin/env bash
# AUTH-02: cookie jar 的域名策略只有一份，读写都按同一组 host；登录发的 host-only 令牌读得回来。
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
dotnet run --project "$repo/plan/tests/auth2/AUTH2.csproj" -c Debug

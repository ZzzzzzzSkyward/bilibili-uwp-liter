#!/usr/bin/env bash
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
tools="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
sdk="$tools/dotnet/sdk/8.0.427"
refs="$tools/dotnet/packs/Microsoft.NETCore.App.Ref/8.0.33/ref/net8.0"
scratch="$(mktemp -d /tmp/api01.XXXXXX)"
trap 'rm -rf "$scratch"' EXIT
args=()
for ref in "$refs"/*.dll; do args+=("-r:$ref"); done
args+=("-r:$sdk/Containers/tasks/net8.0/Newtonsoft.Json.dll")

"$tools/dotnet/dotnet" "$sdk/Roslyn/bincore/csc.dll" -nologo -langversion:latest -target:exe -out:"$scratch/API01.dll" "${args[@]}" \
  "$repo/src/BiliLite.UWP/Api/ApiHelper.cs" \
  "$repo/src/BiliLite.UWP/Api/AccountApi.cs" \
  "$repo/src/BiliLite.UWP/Api/EmoteApi.cs" \
  "$repo/src/BiliLite.UWP/Models/Requests/Api/BaseApi.cs" \
  "$repo/src/BiliLite.UWP/Models/Requests/Api/EmoteApi.cs" \
  "$repo/src/BiliLite.UWP/Models/Requests/Api/DanmakuApi.cs" \
  "$repo/src/BiliLite.UWP/Services/CookieService.cs" \
  "$repo/src/BiliLite.UWP/Services/HttpLogSanitizer.cs" \
  "$repo/src/BiliLite.UWP/Services/WbiKeyService.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/API01Stubs.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/API01.cs"
cp "$sdk/Containers/tasks/net8.0/Newtonsoft.Json.dll" "$scratch/Newtonsoft.Json.dll"
cat > "$scratch/API01.runtimeconfig.json" <<'JSON'
{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.33"}}}
JSON
export DOTNET_ROOT="$tools/dotnet" DOTNET_CLI_HOME="$tools/cache/dotnet-cli" NUGET_PACKAGES="$tools/cache/nuget" XDG_CACHE_HOME="$tools/cache/xdg" XDG_DATA_HOME="$tools/cache/xdg-data"
"$DOTNET_ROOT/dotnet" "$scratch/API01.dll"

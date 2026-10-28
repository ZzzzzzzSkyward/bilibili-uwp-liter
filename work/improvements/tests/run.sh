#!/usr/bin/env bash
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../.." && pwd)"
tools="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
sdk="$tools/dotnet/sdk/8.0.427"
refs="$tools/dotnet/packs/Microsoft.NETCore.App.Ref/8.0.33/ref/net8.0"
scratch="$(mktemp -d /tmp/lm01.XXXXXX)"
trap 'rm -rf "$scratch"' EXIT
args=()
for ref in "$refs"/*.dll; do args+=("-r:$ref"); done
args+=("-r:$sdk/Containers/tasks/net8.0/Newtonsoft.Json.dll")
"$tools/dotnet/dotnet" "$sdk/Roslyn/bincore/csc.dll" -nologo -langversion:latest -target:exe -out:"$scratch/LM01.dll" "${args[@]}" \
  "$repo/src/BiliLite.UWP/Models/LocalMedia/LocalMediaManifest.cs" \
  "$repo/src/BiliLite.UWP/Models/LocalMedia/LocalMediaItem.cs" \
  "$repo/src/BiliLite.UWP/Models/LocalMedia/LocalMediaTrack.cs" \
  "$repo/src/BiliLite.UWP/Services/LocalMedia/LocalMediaValidator.cs" \
  "$repo/src/BiliLite.UWP/Services/LocalMedia/LocalMediaManifestLoader.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/LM01.cs"
cp "$sdk/Containers/tasks/net8.0/Newtonsoft.Json.dll" "$scratch/Newtonsoft.Json.dll"
cat > "$scratch/LM01.runtimeconfig.json" <<'JSON'
{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.33"}}}
JSON
export DOTNET_ROOT="$tools/dotnet" DOTNET_CLI_HOME="$tools/cache/dotnet-cli" NUGET_PACKAGES="$tools/cache/nuget" XDG_CACHE_HOME="$tools/cache/xdg" XDG_DATA_HOME="$tools/cache/xdg-data"
"$DOTNET_ROOT/dotnet" "$scratch/LM01.dll"

"$tools/dotnet/dotnet" "$sdk/Roslyn/bincore/csc.dll" -nologo -langversion:latest -target:exe -out:"$scratch/LM02.dll" "${args[@]}" \
  "$repo/src/BiliLite.UWP/Models/LocalMedia/LocalMediaManifest.cs" \
  "$repo/src/BiliLite.UWP/Models/LocalMedia/LocalMediaItem.cs" \
  "$repo/src/BiliLite.UWP/Models/LocalMedia/LocalMediaTrack.cs" \
  "$repo/src/BiliLite.UWP/Services/LocalMedia/LocalMediaValidator.cs" \
  "$repo/src/BiliLite.UWP/Services/LocalMedia/LocalMediaManifestLoader.cs" \
  "$repo/src/BiliLite.UWP/Services/LocalMedia/LocalMediaLibraryService.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/LocalMediaStorageStubs.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/LM02.cs"
cp "$sdk/Containers/tasks/net8.0/Newtonsoft.Json.dll" "$scratch/Newtonsoft.Json.dll"
cat > "$scratch/LM02.runtimeconfig.json" <<'JSON'
{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.33"}}}
JSON
"$DOTNET_ROOT/dotnet" "$scratch/LM02.dll"

"$tools/dotnet/dotnet" "$sdk/Roslyn/bincore/csc.dll" -nologo -langversion:latest -target:exe -out:"$scratch/LM03.dll" "${args[@]}" \
  "$repo/src/BiliLite.UWP/Models/LocalMedia/LocalMediaManifest.cs" \
  "$repo/src/BiliLite.UWP/Models/LocalMedia/LocalMediaItem.cs" \
  "$repo/src/BiliLite.UWP/Models/LocalMedia/LocalMediaTrack.cs" \
  "$repo/src/BiliLite.UWP/Modules/Player/Playurl/BiliPlayUrlInfo.cs" \
  "$repo/src/BiliLite.UWP/Services/LocalMedia/LocalMediaPlaybackAdapter.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/LocalMediaStorageStubs.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/LM03ControlStubs.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/LM03.cs"
cat > "$scratch/LM03.runtimeconfig.json" <<'JSON'
{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.33"}}}
JSON
"$DOTNET_ROOT/dotnet" "$scratch/LM03.dll"

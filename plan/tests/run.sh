#!/usr/bin/env bash
set -euo pipefail
stage=all
if [ "$#" -eq 1 ] && [ "$1" = --help ]; then
  echo 'Usage: bash run.sh [--stage all|LM01|LM02|LM03|LM04|LM05|API01|AUTH01B]'
  exit 0
fi
if [ "$#" -ne 0 ]; then
  if [ "$#" -ne 2 ] || [ "$1" != --stage ]; then echo 'Invalid arguments; use --help' >&2; exit 2; fi
  stage="$2"
fi
case "$stage" in all|LM01|LM02|LM03|LM04|LM05|API01|AUTH01B) ;; *) echo "Unknown stage: $stage" >&2; exit 2;; esac
phase() { if [ "$stage" != all ]; then echo "PROBE_PHASE $1 $2"; fi; }
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
tools="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
sdk="$tools/dotnet/sdk/8.0.427"
refs="$tools/dotnet/packs/Microsoft.NETCore.App.Ref/8.0.33/ref/net8.0"
scratch="$(mktemp -d /tmp/lm01.XXXXXX)"
trap 'rm -rf "$scratch"' EXIT
args=()
for ref in "$refs"/*.dll; do args+=("-r:$ref"); done
args+=("-r:$sdk/Containers/tasks/net8.0/Newtonsoft.Json.dll")
export DOTNET_ROOT="$tools/dotnet" DOTNET_CLI_HOME="$tools/cache/dotnet-cli" NUGET_PACKAGES="$tools/cache/nuget" XDG_CACHE_HOME="$tools/cache/xdg" XDG_DATA_HOME="$tools/cache/xdg-data"
if [ "$stage" = all ] || [ "$stage" = LM01 ]; then
phase LM01 compile
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
phase LM01 run
"$DOTNET_ROOT/dotnet" "$scratch/LM01.dll"
phase LM01 complete
fi

if [ "$stage" = all ] || [ "$stage" = LM02 ]; then
phase LM02 compile
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
phase LM02 run
"$DOTNET_ROOT/dotnet" "$scratch/LM02.dll"
phase LM02 complete
fi

if [ "$stage" = all ] || [ "$stage" = LM03 ]; then
phase LM03 compile
"$tools/dotnet/dotnet" "$sdk/Roslyn/bincore/csc.dll" -nologo -langversion:latest -target:exe -out:"$scratch/LM03.dll" "${args[@]}" \
  "$repo/src/BiliLite.UWP/Models/LocalMedia/LocalMediaManifest.cs" \
  "$repo/src/BiliLite.UWP/Models/LocalMedia/LocalMediaItem.cs" \
  "$repo/src/BiliLite.UWP/Models/LocalMedia/LocalMediaTrack.cs" \
  "$repo/src/BiliLite.UWP/Modules/Player/Playurl/BiliPlayUrlInfo.cs" \
  "$repo/src/BiliLite.UWP/Services/LocalMedia/LocalMediaPlaybackAdapter.cs" \
  "$repo/src/BiliLite.UWP/Services/LocalMedia/LocalMediaTrackLoadException.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/LocalMediaStorageStubs.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/LM03ControlStubs.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/LM03.cs"
cat > "$scratch/LM03.runtimeconfig.json" <<'JSON'
{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.33"}}}
JSON
phase LM03 run
"$DOTNET_ROOT/dotnet" "$scratch/LM03.dll"
phase LM03 complete
fi

if [ "$stage" = all ] || [ "$stage" = LM04 ]; then
phase LM04 compile
"$tools/dotnet/dotnet" "$sdk/Roslyn/bincore/csc.dll" -nologo -langversion:latest -target:exe -out:"$scratch/LM04.dll" "${args[@]}" \
  "$repo/src/BiliLite.UWP/Models/LocalMedia/LocalMediaTrack.cs" \
  "$repo/src/BiliLite.UWP/Modules/Player/Playurl/BiliPlayUrlInfo.cs" \
  "$repo/src/BiliLite.UWP/Services/LocalMedia/LocalMediaTrackSelection.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/LM04ControlStubs.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/LM04.cs"
cat > "$scratch/LM04.runtimeconfig.json" <<'JSON'
{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.33"}}}
JSON
phase LM04 run
"$DOTNET_ROOT/dotnet" "$scratch/LM04.dll"
phase LM04 complete
fi

if [ "$stage" = all ] || [ "$stage" = LM05 ]; then
phase LM05 compile
"$tools/dotnet/dotnet" "$sdk/Roslyn/bincore/csc.dll" -nologo -langversion:latest -target:exe -out:"$scratch/LM05.dll" "${args[@]}" \
  "$repo/src/BiliLite.UWP/Models/LocalMedia/LocalMediaTrack.cs" \
  "$repo/src/BiliLite.UWP/Services/LocalMedia/SubtitleTrackParser.cs" \
  "$repo/src/BiliLite.UWP/Services/LocalMedia/DanmakuTrackLoader.cs" \
  "$repo/src/BiliLite.UWP/Services/LocalMedia/LocalMediaTrackLoadException.cs" \
  "$repo/src/BiliLite.UWP/Services/LocalMedia/LocalMediaAsyncLoadGuard.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/LocalMediaStorageStubs.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/LM05Stubs.cs" \
  "$(dirname -- "${BASH_SOURCE[0]}")/LM05.cs"
cp "$sdk/Containers/tasks/net8.0/Newtonsoft.Json.dll" "$scratch/Newtonsoft.Json.dll"
cat > "$scratch/LM05.runtimeconfig.json" <<'JSON'
{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.33"}}}
JSON
phase LM05 run
"$DOTNET_ROOT/dotnet" "$scratch/LM05.dll"
phase LM05 complete
fi

if [ "$stage" = all ] || [ "$stage" = API01 ]; then
bash "$(dirname -- "${BASH_SOURCE[0]}")/run-api01.sh"
fi
if [ "$stage" = all ] || [ "$stage" = AUTH01B ]; then
bash "$(dirname -- "${BASH_SOURCE[0]}")/run-auth01b.sh"
fi

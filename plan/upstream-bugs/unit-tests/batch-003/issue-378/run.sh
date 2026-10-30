#!/usr/bin/env bash
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../../.." && pwd)"
tools="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
scratch="$(mktemp -d /tmp/issue-378.XXXXXX)"
trap 'rm -rf "$scratch"' EXIT
python3 - "$repo/src/BiliLite.UWP/Modules/Detail/LiveRoomVM.cs" "$scratch/probe.cs" <<'PY'
import sys
source=open(sys.argv[1],encoding='utf-8-sig').read()
start=source.index('        public class LiveAnchorProfileGloryInfo')
end=source.index('\n        public class LiveAnchorProfilePkInfo',start)
block=source[start:end]
probe='''using System;\nclass LiveRoomVM\n{\n'''+block+'''\n}\nclass Program\n{
 static int Main()
 {
  var model = new LiveRoomVM.LiveAnchorProfileGloryInfo();
  model.jump_url = "https://example.com/path";
  if (model.jump_url != "https://example.com/path") return 1;
  foreach (var value in new string[] { "not a url", null, "" })
  {
   model.jump_url = value;
   if (model.jump_url != "https://www.bilibili.com/404") return 2;
  }
  Console.WriteLine("PASS: absolute URL preserved; malformed, null, and empty values use fallback");
  return 0;
 }
}\n'''
open(sys.argv[2],'w').write(probe)
PY
sdk="$tools/dotnet/sdk/8.0.427"
refs="$tools/dotnet/packs/Microsoft.NETCore.App.Ref/8.0.33/ref/net8.0"
args=()
for ref in "$refs"/*.dll; do args+=("-r:$ref"); done
"$tools/dotnet/dotnet" "$sdk/Roslyn/bincore/csc.dll" -nologo -langversion:latest -target:exe -out:"$scratch/probe.dll" "${args[@]}" "$scratch/probe.cs"
cat > "$scratch/probe.runtimeconfig.json" <<'JSON'
{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.33"}}}
JSON
export DOTNET_ROOT="$tools/dotnet" DOTNET_CLI_HOME="$tools/cache/dotnet-cli" NUGET_PACKAGES="$tools/cache/nuget" XDG_CACHE_HOME="$tools/cache/xdg" XDG_DATA_HOME="$tools/cache/xdg-data"
"$DOTNET_ROOT/dotnet" "$scratch/probe.dll"

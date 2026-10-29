#!/usr/bin/env bash
set -euo pipefail
here="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo="$(cd -- "$here/../../../../.." && pwd)"
tools_root="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
sdk="$tools_root/dotnet/sdk/8.0.427"
python3 - "$repo/src/BiliLite.UWP/Modules/User/HistoryVM.cs" "$here/probe.cs" "$here/source.cs" <<'PY'
import sys
original=open(sys.argv[1],encoding='utf-8-sig').read()
account=open(sys.argv[1].replace('Modules/User/HistoryVM.cs','Api/AccountApi.cs'),encoding='utf-8-sig').read()
method=account[account.index('        public ApiModel HistoryWbi('):account.index('        public ApiModel DelHistory(',account.index('        public ApiModel HistoryWbi('))]
probe=open(sys.argv[2]).read().replace('__ACTUAL_HISTORY_WBI__',method)
start=original.index('namespace BiliLite.Modules.User')
imports='''using BiliLite.Helpers;\nusing BiliLite.Models;\nusing Newtonsoft.Json;\nusing Newtonsoft.Json.Linq;\nusing System;\nusing System.Collections.Generic;\nusing System.Collections.ObjectModel;\nusing System.Threading.Tasks;\nusing System.Windows.Input;\n'''
open(sys.argv[3],'w').write(imports+original[start:]+'\n'+probe)
PY
refs="$tools_root/dotnet/packs/Microsoft.NETCore.App.Ref/8.0.33/ref/net8.0"
# Use the installed framework reference assemblies and Newtonsoft shipped with the SDK.
args=()
for ref in "$refs"/*.dll; do args+=("-r:$ref"); done
args+=("-r:$sdk/Containers/tasks/net8.0/Newtonsoft.Json.dll")
"$tools_root/dotnet/dotnet" "$sdk/Roslyn/bincore/csc.dll" -nologo -langversion:latest -target:exe -out:"$here/probe.dll" "${args[@]}" "$here/source.cs"
cp "$sdk/Containers/tasks/net8.0/Newtonsoft.Json.dll" "$here/Newtonsoft.Json.dll"
cat > "$here/probe.runtimeconfig.json" <<'JSON'
{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.33"}}}
JSON
export DOTNET_ROOT="$tools_root/dotnet" DOTNET_CLI_HOME="$tools_root/cache/dotnet-cli" NUGET_PACKAGES="$tools_root/cache/nuget" XDG_CACHE_HOME="$tools_root/cache/xdg" XDG_DATA_HOME="$tools_root/cache/xdg-data"
"$DOTNET_ROOT/dotnet" "$here/probe.dll"

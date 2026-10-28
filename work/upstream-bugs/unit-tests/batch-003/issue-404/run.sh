#!/usr/bin/env bash
set -euo pipefail
repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../../../.." && pwd)"
tools="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
scratch="$(mktemp -d /tmp/issue-404.XXXXXX)"
trap 'rm -rf "$scratch"' EXIT
python3 - "$repo/src/BiliLite.UWP/Modules/Home/LiveVM.cs" "$scratch/probe.cs" <<'PY'
import sys
source=open(sys.argv[1],encoding='utf-8-sig').read()
start=source.index('                        if (data.data["area_entrance_v2"]')
end=source.index('\n                        await GetLiveHomeItems();',start)
block=source[start:end]
probe='''using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
class LiveHomeAreaModel { public string Name; }
class Token : IEnumerable<Token>
{
 public List<Token> Items = new(); public Dictionary<string,Token> Values = new(); public string Text;
 public Token this[int i] => Items[i]; public Token this[string key] => Values[key];
 public int Count() => Items.Count;
 public IEnumerator<Token> GetEnumerator() => Items.GetEnumerator(); IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
 public override string ToString() => Text ?? "";
}
class Data { public Token data = new(); }
static class Utils { public static Task<T> DeserializeJson<T>(string json) => Task.FromResult((T)(object)new ObservableCollection<LiveHomeAreaModel> { new LiveHomeAreaModel { Name = json } }); }
class Harness
{
 public ObservableCollection<LiveHomeAreaModel> Areas;
 public async Task Load(Data data)
 {
'''+block+'''
 }
}
class Program
{
 static async Task<int> Main()
 {
  var empty = new Harness();
  await empty.Load(new Data { data = new Token { Values = { ["area_entrance_v2"] = new Token() } } });
  if (empty.Areas != null) return 1;
  var list = new Token { Text = "areas", Values = { ["list"] = new Token { Text = "area-list" } } };
  var nonempty = new Harness();
  await nonempty.Load(new Data { data = new Token { Values = { ["area_entrance_v2"] = new Token { Items = { list } } } } });
  if (nonempty.Areas == null || nonempty.Areas.Count != 1 || nonempty.Areas[0].Name != "area-list") return 2;
  Console.WriteLine("PASS: empty area array skips indexing; populated first entry loads its list");
  return 0;
 }
}
'''
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

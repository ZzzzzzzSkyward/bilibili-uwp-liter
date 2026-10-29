#!/usr/bin/env bash
set -euo pipefail
script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../../../../../" && pwd)"
tools_root="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
source_file="$repo_root/src/BiliLite.UWP/Helpers/MessageCenter.cs"
fragment="$script_dir/empty-url-guard.csx"
harness="$script_dir/empty-url-guard-test.csx"
python3 - "$source_file" "$fragment" <<'PY'
import sys
source = open(sys.argv[1], encoding='utf-8-sig').read()
start = source.index('public async static Task<bool> HandleUrl(string url)')
start = source.index('{', start) + 1
end = source.index('//短链接处理', start)
open(sys.argv[2], 'w').write(source[start:end])
PY
grep -Fq 'if (url.Length == 0)' "$fragment"
grep -Fq 'if (url.First() == '\''@'\'')' "$fragment"
cat > "$harness" <<'CSHARP'
using System;
using System.Linq;
using System.Threading.Tasks;

sealed class TargetPrefix
{
    public async Task<bool> Handle(string url)
    {
CSHARP
cat "$fragment" >> "$harness"
cat >> "$harness" <<'CSHARP'
        return true;
    }
}

var handler = new TargetPrefix();
var emptyResult = await handler.Handle("");
var markerResult = await handler.Handle("@user");
var ordinaryResult = await handler.Handle("https://example.invalid/video");
Console.WriteLine($"Empty URL returns false: {!emptyResult}");
Console.WriteLine($"Existing @ prefix returns false: {!markerResult}");
Console.WriteLine($"Ordinary URL continues: {ordinaryResult}");
var passed = !emptyResult && !markerResult && ordinaryResult;
Console.WriteLine(passed
    ? "PASS: empty URL is rejected and existing prefix behavior is preserved"
    : "FAIL: extracted HandleUrl prefix returned an unexpected result");
Environment.ExitCode = passed ? 0 : 1;
CSHARP
export DOTNET_ROOT="$tools_root/dotnet"
export DOTNET_CLI_HOME="$tools_root/cache/dotnet-cli"
export NUGET_PACKAGES="$tools_root/cache/nuget"
export XDG_CACHE_HOME="$tools_root/cache/xdg"
export XDG_DATA_HOME="$tools_root/cache/xdg-data"
exec "$DOTNET_ROOT/dotnet" "$tools_root/tool/dotnet-script-1.6.0/tools/net8.0/any/dotnet-script.dll" --no-cache "$harness"

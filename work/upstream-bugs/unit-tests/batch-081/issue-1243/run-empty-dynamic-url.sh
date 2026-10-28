#!/usr/bin/env bash
set -euo pipefail
script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../../../../../" && pwd)"
tools_root="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
source_file="$repo_root/src/BiliLite.UWP/Helpers/MessageCenter.cs"
fragment="$script_dir/empty-url-guard.csx"
harness="$script_dir/empty-url-guard-test.csx"
sed -n '103,106p' "$source_file" > "$fragment"
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

var threw = false;
string exceptionType = "";
try
{
    await new TargetPrefix().Handle("");
}
catch (Exception ex)
{
    threw = true;
    exceptionType = ex.GetType().Name;
}
Console.WriteLine($"Empty URL threw: {threw} ({exceptionType})");
var passed = !threw;
Console.WriteLine(passed
    ? "PASS: empty URL is handled without throwing"
    : "FAIL: empty URL guard throws before the unsupported-link fallback");
Environment.ExitCode = passed ? 0 : 1;
CSHARP
export DOTNET_ROOT="$tools_root/dotnet"
export DOTNET_CLI_HOME="$tools_root/cache/dotnet-cli"
export NUGET_PACKAGES="$tools_root/cache/nuget"
export XDG_CACHE_HOME="$tools_root/cache/xdg"
export XDG_DATA_HOME="$tools_root/cache/xdg-data"
exec "$DOTNET_ROOT/dotnet" "$tools_root/tool/dotnet-script-1.6.0/tools/net8.0/any/dotnet-script.dll" --no-cache "$harness"

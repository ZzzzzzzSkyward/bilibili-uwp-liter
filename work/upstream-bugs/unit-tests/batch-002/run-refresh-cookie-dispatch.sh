#!/usr/bin/env bash
set -euo pipefail
script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../../../.." && pwd)"
tools_root="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
source_file="$repo_root/src/BiliLite.UWP/Api/ApiHelper.cs"
fragment="$script_dir/refresh-cookie-dispatch-fragment.csx"
harness="$script_dir/refresh-cookie-dispatch-test.csx"
sed -n '493,502p' "$source_file" > "$fragment"
grep -Fq 'var result3 = await api3.Request();' "$fragment"
cat > "$harness" <<'CSHARP'
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

static class RestSharp
{
    public enum Method { Get, Post }
}

sealed class Dispatch
{
    public RestSharp.Method Method { get; }
    public string Url { get; }
    public Dispatch(RestSharp.Method method, string url) { Method = method; Url = url; }
}

sealed class ApiResult { }

sealed class ApiModel
{
    public RestSharp.Method method;
    public string baseUrl;
    public bool need_cookie;
    public string body;
    public static readonly List<Dispatch> Dispatches = new();
    public Task<ApiResult> Request()
    {
        Dispatches.Add(new Dispatch(method, baseUrl));
        return Task.FromResult(new ApiResult());
    }
}

string GetCSRF() => "csrf-fixture";
string token = "refresh-token-fixture";
bool _need_refresh_cookie = true;
// Simulates the in-scope `api` from RefreshCookie: the earlier correspond-path GET.
var api = new ApiModel
{
    method = RestSharp.Method.Get,
    baseUrl = "https://www.bilibili.com/correspond/1/fixture-path",
    need_cookie = true,
};
CSHARP
cat "$fragment" >> "$harness"
cat >> "$harness" <<'CSHARP'

var expectedUrl = "https://passport.bilibili.com/x/passport-login/web/confirm/refresh";
var passed = ApiModel.Dispatches.Count == 1
    && ApiModel.Dispatches[0].Method == RestSharp.Method.Post
    && ApiModel.Dispatches[0].Url == expectedUrl;
if (ApiModel.Dispatches.Count > 0)
    Console.WriteLine($"Observed dispatch: {ApiModel.Dispatches[0].Method} {ApiModel.Dispatches[0].Url}");
Console.WriteLine(passed
    ? "PASS: confirm-refresh POST was dispatched"
    : "FAIL: confirm-refresh POST was not dispatched by the extracted RefreshCookie source");
Environment.ExitCode = passed ? 0 : 1;
CSHARP
export DOTNET_ROOT="$tools_root/dotnet"
export DOTNET_CLI_HOME="$tools_root/cache/dotnet-cli"
export NUGET_PACKAGES="$tools_root/cache/nuget"
export XDG_CACHE_HOME="$tools_root/cache/xdg"
export XDG_DATA_HOME="$tools_root/cache/xdg-data"
exec "$DOTNET_ROOT/dotnet" "$tools_root/tool/dotnet-script-1.6.0/tools/net8.0/any/dotnet-script.dll" --no-cache "$harness"

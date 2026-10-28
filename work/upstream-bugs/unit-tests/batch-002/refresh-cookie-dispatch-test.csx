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
            var url3 = "https://passport.bilibili.com/x/passport-login/web/confirm/refresh";
            var api3 = new ApiModel()
            {
                need_cookie = true,
                method = RestSharp.Method.Post,
                baseUrl = url3,
                body = $"csrf={GetCSRF()}&refresh_token={token}"
            };
            var result3 = await api3.Request();
            _need_refresh_cookie = false;

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

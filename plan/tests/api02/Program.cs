using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Services;

internal static class Program
{
    private static int assertions;

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception("API-02 failed: " + message);
    }

    private static HttpResults Http(int code, string body) =>
        new HttpResults { code = code, status = code == 200, results = body };

    private static CookieService NewService(IDictionary<string, string> settings,
        Func<ApiModel, Task<HttpResults>> send, Action<string> notify = null) =>
        new CookieService(new FakeCookieStore(),
            key => settings.TryGetValue(key, out var value) ? value : "",
            (key, value) => settings[key] = value,
            () => true, send, notify);

    private static async Task Main()
    {
        await FailedRefreshKeepsTimestamp();
        await SuccessfulRefreshClearsTimestamp();
        await CounterTracksConsecutiveFailures();
        Console.WriteLine("PASS " + assertions + " assertions");
    }

    private static async Task FailedRefreshKeepsTimestamp()
    {
        var settings = new Dictionary<string, string>
        {
            ["CookieCSRF"] = "csrf",
            ["CookieRefreshToken"] = "old-token"
        };
        var sent = new List<ApiModel>();
        var notifications = new List<string>();
        var service = NewService(settings, api =>
        {
            sent.Add(api);
            if (api.baseUrl.EndsWith("/cookie/info"))
                return Task.FromResult(Http(200, "{\"code\":0,\"data\":{\"timestamp\":123,\"refresh\":true}}"));
            if (api.baseUrl.Contains("/correspond/"))
                return Task.FromResult(Http(200, "<div id=\"1-name\">refresh-csrf</div>"));
            if (api.baseUrl.EndsWith("/cookie/refresh"))
                return Task.FromResult(Http(200, "{\"code\":0,\"data\":{\"refresh_token\":\"rotated-token\"}}"));
            if (api.baseUrl.EndsWith("/confirm/refresh"))
                return Task.FromResult(Http(503, ""));
            return Task.FromResult(Http(500, ""));
        }, notifications.Add);

        Check(await service.NeedRefreshCookieAsync() && service.CookieTimestamp == 123,
            "fixture starts with refresh required and a known timestamp");
        Check(sent.Count == 1, "the initial state check dispatched exactly one request");

        await service.RefreshCookieAsync();
        Check(service.CookieTimestamp == 123, "a failed refresh keeps the cookie timestamp");
        Check(await service.NeedRefreshCookieAsync(), "a failed refresh still reports that refresh is required");

        var sendsBeforeRetry = sent.Count;
        Check(sendsBeforeRetry == 4, "the first refresh attempt dispatched the correspond page, refresh and confirm requests");

        await service.RefreshCookieAsync();
        Check(sent.Count - sendsBeforeRetry == 3 &&
            sent[sendsBeforeRetry].method == RestSharp.Method.Get &&
            sent[sendsBeforeRetry].baseUrl.Contains("/correspond/") &&
            sent[sendsBeforeRetry + 1].method == RestSharp.Method.Post &&
            sent[sendsBeforeRetry + 1].baseUrl.EndsWith("/cookie/refresh") &&
            sent[sendsBeforeRetry + 2].method == RestSharp.Method.Post &&
            sent[sendsBeforeRetry + 2].baseUrl.EndsWith("/confirm/refresh"),
            "a retry re-dispatches the whole refresh flow instead of short-circuiting");

        Check(notifications.Count == 0, "failed refresh attempts never raise the re-login toast");
    }

    private static async Task SuccessfulRefreshClearsTimestamp()
    {
        var settings = new Dictionary<string, string>
        {
            ["CookieCSRF"] = "csrf",
            ["CookieRefreshToken"] = "old-token"
        };
        var service = NewService(settings, api =>
        {
            if (api.baseUrl.EndsWith("/cookie/info"))
                return Task.FromResult(Http(200, "{\"code\":0,\"data\":{\"timestamp\":555,\"refresh\":true}}"));
            if (api.baseUrl.Contains("/correspond/"))
                return Task.FromResult(Http(200, "<div id=\"1-name\">refresh-csrf</div>"));
            if (api.baseUrl.EndsWith("/cookie/refresh"))
                return Task.FromResult(Http(200, "{\"code\":0,\"data\":{\"refresh_token\":\"rotated-token\"}}"));
            if (api.baseUrl.EndsWith("/confirm/refresh"))
                return Task.FromResult(Http(200, "{\"code\":0}"));
            return Task.FromResult(Http(500, ""));
        });

        Check(await service.NeedRefreshCookieAsync() && service.CookieTimestamp == 555,
            "happy-path fixture starts with refresh required and a known timestamp");
        await service.RefreshCookieAsync();
        Check(service.CookieTimestamp == 0, "a completed refresh clears the cookie timestamp");
        Check(!await service.NeedRefreshCookieAsync(), "a completed refresh no longer requires a refresh");
    }

    private static async Task CounterTracksConsecutiveFailures()
    {
        var settings = new Dictionary<string, string>
        {
            ["CookieCSRF"] = "csrf",
            ["CookieRefreshToken"] = "old-token"
        };
        var failing = false;
        var service = NewService(settings, api =>
        {
            if (!api.baseUrl.EndsWith("/cookie/info")) return Task.FromResult(Http(500, ""));
            return Task.FromResult(failing
                ? Http(503, "")
                : Http(200, "{\"code\":0,\"data\":{\"timestamp\":321,\"refresh\":true}}"));
        });

        // A short daily tick clears the initialized flag so each cycle really re-checks.
        service.StartAutoRefresh(TimeSpan.FromMilliseconds(300));

        for (var cycle = 0; cycle < 6; cycle++)
        {
            await Task.Delay(400);
            failing = false;
            await service.NeedRefreshCookieAsync();
            await Task.Delay(400);
            failing = true;
            await service.NeedRefreshCookieAsync();
        }

        failing = false;
        Check(await service.NeedRefreshCookieAsync(),
            "six alternating successes and failures still honour a server refresh=true");

        for (var attempt = 0; attempt < 6; attempt++)
        {
            await Task.Delay(400);
            failing = true;
            await service.NeedRefreshCookieAsync();
        }

        Check(!await service.NeedRefreshCookieAsync(),
            "six consecutive failures still disable cookie refresh");
    }

    private sealed class FakeCookieStore : ICookieStore
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>();
        public void Write(string name, string value) => values[name] = value;
        public string Read(string name, string fallback) => values.TryGetValue(name, out var value) ? value : fallback;
    }
}

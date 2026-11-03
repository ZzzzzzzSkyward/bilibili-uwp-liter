using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        if (!condition) throw new Exception("AUTH-01 failed: " + message);
    }

    private static HttpResults Http(int code, string body) =>
        new HttpResults { code = code, status = code == 200, results = body };

    private static async Task Main()
    {
        await RefreshIsAwaitedBeforeTheVideoRequest();
        await SeasonRetriesShareOnePreflight();
        await ConcurrentRefreshesRunOneChain();
        await FailedRefreshResetsSingleFlight();
        await DeadSessionRaisesReloginOnce();
        await DeadRefreshChainRaisesRelogin();
        await MissingRefreshTokenMarksRelogin();
        await AcTimeValueRecoversTheRefreshToken();
        await SuccessfulRefreshMirrorsSessionIntoSettings();
        await ServerSessionClearsRelogin();
        SourceChecks();
        Console.WriteLine("PASS " + assertions + " assertions");
    }

    private static async Task RefreshIsAwaitedBeforeTheVideoRequest()
    {
        var settings = new Dictionary<string, string> { ["CookieCSRF"] = "csrf", ["CookieRefreshToken"] = "old-token" };
        var backend = new Backend();
        ApiHelper.Cookie = NewService(settings, backend);
        var preflight = new CookiePreflight();

        await preflight.EnsureAsync(ApiHelper.NeedRefreshCookie, ApiHelper.RefreshCookieAsync);
        backend.Log.Add("video");

        Check(string.Join(",", backend.Log) == "info,correspond,refresh,confirm,video",
            "the video web path refreshes cookies to completion before the playurl request");
        Check(settings["CookieRefreshToken"] == "rotated-token" &&
            ApiHelper.Cookie.CookieTimestamp == 0,
            "the awaited refresh rotated the token and cleared the timestamp");
    }

    private static async Task SeasonRetriesShareOnePreflight()
    {
        var settings = new Dictionary<string, string> { ["CookieCSRF"] = "csrf", ["CookieRefreshToken"] = "old-token" };
        var backend = new Backend();
        ApiHelper.Cookie = NewService(settings, backend);
        var preflight = new CookiePreflight();

        foreach (var area in new[] { "", "cn", "hk", "tw" })
        {
            await preflight.EnsureAsync(ApiHelper.NeedRefreshCookie, ApiHelper.RefreshCookieAsync);
            backend.Log.Add("season" + (area == "" ? "" : ":" + area));
        }

        Check(string.Join(",", backend.Log) == "info,correspond,refresh,confirm,season,season:cn,season:hk,season:tw",
            "the season web path refreshes once and then issues every proxy attempt after it");
    }

    private static async Task ConcurrentRefreshesRunOneChain()
    {
        var settings = new Dictionary<string, string> { ["CookieCSRF"] = "csrf", ["CookieRefreshToken"] = "old-token" };
        var backend = new Backend();
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        backend.BeforeCorrespond = () => release.Task;
        var service = NewService(settings, backend);
        service.CookieTimestamp = 123;

        var first = service.RefreshCookieAsync();
        var second = service.RefreshCookieAsync();
        release.SetResult(true);
        await Task.WhenAll(first, second);

        Check(ReferenceEquals(first, second), "a concurrent caller joins the in-flight refresh instead of starting one");
        Check(backend.Count("correspond") == 1 && backend.Count("refresh") == 1 && backend.Count("confirm") == 1,
            "two concurrent requests dispatch exactly one correspond/refresh/confirm chain");
    }

    private static async Task FailedRefreshResetsSingleFlight()
    {
        var settings = new Dictionary<string, string> { ["CookieCSRF"] = "csrf", ["CookieRefreshToken"] = "old-token" };
        var backend = new Backend { ConfirmHttp = 503 };
        var service = NewService(settings, backend);
        service.CookieTimestamp = 123;

        await service.RefreshCookieAsync();
        Check(string.Join(",", backend.Log) == "correspond,refresh,confirm",
            "the first attempt dispatched the three refresh requests");
        Check(backend.Count("confirm") == 1 && service.CookieTimestamp == 123,
            "a failed confirm keeps the timestamp so a retry is still possible");

        backend.ConfirmHttp = 200;
        await service.RefreshCookieAsync();
        Check(backend.Count("correspond") == 2 && backend.Count("refresh") == 2 && backend.Count("confirm") == 2,
            "the single-flight slot was released, so a retry re-dispatches all three requests again");
    }

    private static async Task DeadSessionRaisesReloginOnce()
    {
        CookieService.ClearReloginRequired();
        var raised = 0;
        EventHandler handler = (sender, e) => raised++;
        CookieService.ReloginRequired += handler;
        try
        {
            var backend = new Backend { InfoBody = "{\"code\":-101}" };
            var service = NewService(new Dictionary<string, string>(), backend);
            Check(!await service.NeedRefreshCookieAsync() && backend.Count("info") == 1,
                "-101 from cookie/info stops the state check from asking for a refresh");
            Check(CookieService.NeedsRelogin, "-101 from cookie/info marks the session dead");
            Check(raised == 1, "the re-login event is raised on the false to true transition");

            var second = NewService(new Dictionary<string, string>(), new Backend { InfoBody = "{\"code\":-101}" });
            await second.NeedRefreshCookieAsync();
            Check(raised == 1 && CookieService.NeedsRelogin,
                "a second dead session keeps the event at one raise until the state is cleared");

            var transport = NewService(new Dictionary<string, string>(), new Backend { InfoHttp = -101, InfoBody = "" });
            await transport.NeedRefreshCookieAsync();
            Check(CookieService.NeedsRelogin, "-101 returned as a transport code also marks the session dead");

            CookieService.ClearReloginRequired();
            Check(!CookieService.NeedsRelogin, "clearing the verdict lets a later failure raise again");
            var again = NewService(new Dictionary<string, string>(), new Backend { InfoBody = "{\"code\":-111}" });
            await again.NeedRefreshCookieAsync();
            Check(raised == 2 && CookieService.NeedsRelogin, "-111 after a reset raises the event a second time");
        }
        finally
        {
            CookieService.ReloginRequired -= handler;
            CookieService.ClearReloginRequired();
        }
    }

    private static async Task DeadRefreshChainRaisesRelogin()
    {
        CookieService.ClearReloginRequired();
        var backend = new Backend { RefreshBody = "{\"code\":-101}" };
        var settings = new Dictionary<string, string> { ["CookieCSRF"] = "csrf", ["CookieRefreshToken"] = "old-token" };
        var service = NewService(settings, backend);
        service.CookieTimestamp = 123;

        await service.RefreshCookieAsync();
        Check(CookieService.NeedsRelogin && backend.Count("confirm") == 0,
            "-101 from cookie/refresh marks the session dead instead of returning quietly");

        CookieService.ClearReloginRequired();
        var confirmBackend = new Backend { ConfirmBody = "{\"code\":-111}" };
        var confirmService = NewService(
            new Dictionary<string, string> { ["CookieCSRF"] = "csrf", ["CookieRefreshToken"] = "old-token" }, confirmBackend);
        confirmService.CookieTimestamp = 123;
        await confirmService.RefreshCookieAsync();
        Check(CookieService.NeedsRelogin, "-111 from confirm/refresh marks the session dead");
        CookieService.ClearReloginRequired();
    }

    private static async Task MissingRefreshTokenMarksRelogin()
    {
        CookieService.ClearReloginRequired();
        var backend = new Backend();
        var store = new FakeCookieStore();
        var service = NewService(new Dictionary<string, string> { ["CookieCSRF"] = "csrf" }, backend, store);
        service.CookieTimestamp = 123;

        await service.RefreshCookieAsync();
        Check(string.Join(",", backend.Log) == "correspond",
            "without ac_time_value the refresh chain stops before requesting a new token");
        Check(CookieService.NeedsRelogin, "a still-missing refresh token after the sync attempt marks the session dead");
        CookieService.ClearReloginRequired();
    }

    private static async Task AcTimeValueRecoversTheRefreshToken()
    {
        CookieService.ClearReloginRequired();
        var backend = new Backend();
        var store = new FakeCookieStore { ["ac_time_value"] = "jar-token" };
        var settings = new Dictionary<string, string> { ["CookieCSRF"] = "csrf" };
        var service = NewService(settings, backend, store);
        service.CookieTimestamp = 123;

        await service.RefreshCookieAsync();
        Check(settings.ContainsKey("CookieRefreshToken") && settings["CookieRefreshToken"] == "rotated-token",
            "a jar-only ac_time_value lets the refresh run and stores the rotated token");
        Check(!CookieService.NeedsRelogin, "a recovered refresh token keeps the session usable");
    }

    private static async Task SuccessfulRefreshMirrorsSessionIntoSettings()
    {
        var store = new FakeCookieStore
        {
            ["SESSDATA"] = "fresh-sessdata",
            ["bili_jct"] = "fresh-csrf",
            ["ac_time_value"] = "fresh-time-value"
        };
        var settings = new Dictionary<string, string>
        {
            ["CookieSESSDATA"] = "stale-sessdata",
            ["CookieCSRF"] = "stale-csrf",
            ["CookieRefreshToken"] = "stale-token"
        };
        var service = NewService(settings, new Backend(), store);

        service.LoadCookieFromSetting();
        Check(settings["CookieSESSDATA"] == "fresh-sessdata" && settings["CookieCSRF"] == "fresh-csrf",
            "a refresh new SESSDATA/CSRF overwrite the stale settings");
        // 存的令牌来源未知：cookie 只是旧会话的迁移来源，不是「更新」手段，不许改写它
        Check(settings["CookieRefreshToken"] == "stale-token",
            "a saved token of unknown provenance is kept instead of being rewritten by the jar's ac_time_value");
        Check(service.GetCSRF() == "fresh-csrf" && service.GetRefreshToken() == "stale-token",
            "the refreshed session is what later requests read back");

        // 本会话没有任何刷新令牌时，jar 里的 ac_time_value 才是迁移来源
        var legacySettings = new Dictionary<string, string> { ["CookieCSRF"] = "stale-csrf" };
        var legacyService = NewService(legacySettings, new Backend(),
            new FakeCookieStore { ["ac_time_value"] = "fresh-time-value" });
        legacyService.LoadCookieFromSetting();
        Check(legacySettings["CookieRefreshToken"] == "fresh-time-value" &&
            legacyService.GetRefreshToken() == "fresh-time-value",
            "a session with no saved token migrates the refreshed jar's ac_time_value into settings");
    }

    private static async Task ServerSessionClearsRelogin()
    {
        CookieService.ClearReloginRequired();
        var dead = NewService(new Dictionary<string, string>(), new Backend { InfoBody = "{\"code\":-101}" });
        await dead.NeedRefreshCookieAsync();
        Check(CookieService.NeedsRelogin, "fixture starts with a dead session");

        var alive = NewService(new Dictionary<string, string>(), new Backend());
        Check(await alive.NeedRefreshCookieAsync(), "the surviving session still asks for the pending refresh");
        Check(!CookieService.NeedsRelogin, "a successful state check clears the previous dead-session verdict");
    }

    private static void SourceChecks()
    {
        var root = FindRepoRoot();
        var playurl = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Modules/Player/Playurl/BiliPayUrlRequest.cs"));
        var ordered = Regex.Matches(playurl,
            @"await PreflightCookieAsync\(\);\s*\r?\n\s*var webApiResult = await \(playerAPI\.(VideoPlayUrl|SeasonPlayUrl)");
        Check(ordered.Count == 2, "both web playurl paths await the cookie preflight immediately before their GET");

        var settingPage = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Pages/SettingPage.xaml"));
        var accountStart = settingPage.IndexOf(">账号<", StringComparison.Ordinal);
        var accountEnd = accountStart < 0 ? -1 : settingPage.IndexOf("</PivotItem>", accountStart, StringComparison.Ordinal);
        Check(accountStart > 0 && accountEnd > accountStart, "the settings page carries an account pivot");
        var accountBlock = settingPage.Substring(accountStart, accountEnd - accountStart);
        Check(accountBlock.Contains("x:Name=\"AccountStateText\"") && accountBlock.Contains(">刷新登录<") &&
            accountBlock.Contains(">重新登录<") && accountBlock.Contains(">退出登录<"),
            "the account block carries the status line and its three actions");
        Check(!accountBlock.Contains("（") && !accountBlock.Contains("：") && !accountBlock.Contains("·"),
            "no account label carries a colon, a parenthesised explanation or a joined restatement");

        var app = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/App.xaml.cs"));
        Check(app.Contains("CookieService.ReloginRequired += OnReloginRequired") &&
            app.Contains("this.Resuming += OnAppResuming") &&
            !app.Contains("override void OnResuming") &&
            app.Contains("_ = PreflightLoginAsync();") &&
            app.Contains("dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, ShowReloginToast)") &&
            app.Contains("Utils.ShowMessageToast(\"登录已失效\")"),
            "App subscribes once, marshals the toast to the UI thread and preflights on launch and resume");

        var apiHelper = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Api/ApiHelper.cs"));
        Check(apiHelper.Contains("public static async Task RefreshCookieAsync()") &&
            !apiHelper.Contains("public static async void RefreshCookie"),
            "ApiHelper exposes the awaitable refresh and no async-void twin");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src/BiliLite.UWP/Services/CookieService.cs")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new Exception("AUTH-01 failed: repository root not found from " + AppContext.BaseDirectory);
    }

    private static global::CookieService NewService(IDictionary<string, string> settings, Backend backend,
        FakeCookieStore store = null) =>
        new global::CookieService(store ?? new FakeCookieStore(),
            key => settings.TryGetValue(key, out var value) ? value : "",
            (key, value) => settings[key] = value,
            () => true, backend.Send);

    private sealed class Backend
    {
        public readonly List<string> Log = new List<string>();
        public int InfoHttp = 200;
        public string InfoBody = "{\"code\":0,\"data\":{\"timestamp\":123,\"refresh\":true}}";
        public string RefreshBody = "{\"code\":0,\"data\":{\"refresh_token\":\"rotated-token\"}}";
        public int ConfirmHttp = 200;
        public string ConfirmBody = "{\"code\":0}";
        public Func<Task> BeforeCorrespond;

        public int Count(string endpoint) => Log.Count(x => x == endpoint);

        public async Task<HttpResults> Send(ApiModel api)
        {
            var endpoint = Endpoint(api);
            Log.Add(endpoint);
            switch (endpoint)
            {
                case "info":
                    return Http(InfoHttp, InfoBody);
                case "correspond":
                    if (BeforeCorrespond != null) await BeforeCorrespond();
                    return Http(200, "<div id=\"1-name\">refresh-csrf</div>");
                case "refresh":
                    return Http(200, RefreshBody);
                case "confirm":
                    return Http(ConfirmHttp, ConfirmBody);
                default:
                    return Http(500, "");
            }
        }

        private static string Endpoint(ApiModel api)
        {
            if (api.baseUrl.EndsWith("/cookie/info")) return "info";
            if (api.baseUrl.Contains("/correspond/")) return "correspond";
            if (api.baseUrl.EndsWith("/cookie/refresh")) return "refresh";
            if (api.baseUrl.EndsWith("/confirm/refresh")) return "confirm";
            return api.baseUrl;
        }
    }

    private sealed class FakeCookieStore : ICookieStore
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>();

        public string this[string name]
        {
            get => values[name];
            set => values[name] = value;
        }

        public void Write(string name, string value) => values[name] = value;
        public string Read(string name, string fallback) => values.TryGetValue(name, out var value) ? value : fallback;
    }
}

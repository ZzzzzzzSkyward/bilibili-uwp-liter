using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Models.Requests.Api;
using BiliLite.Services;

internal static class API01
{
    private static int assertions;
    private static readonly string Img = "7cd084941338484aae1ad9425b84077c";
    private static readonly string Sub = "4932caff0ff746eab6f01bf08b70ac45";

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception("API-01 failed: " + message);
    }

    private static void Test(bool condition, string message) => Check(condition, message);

    private static async Task Main()
    {
        Test(WbiKeyService.Sign("foo=114&bar=514&baz=1919810", new WbiKey(Img, Sub),
            DateTimeOffset.FromUnixTimeSeconds(1702204169)) ==
            "bar=514&baz=1919810&foo=114&wts=1702204169&w_rid=6149fdadf571698ca7e6a567265cd0ee",
            "fixed WBI signature matches independent regression vector");
        Test(WbiKeyService.Sign("q=hello%20world!'()*&foo=%C3%A9", new WbiKey(Img, Sub),
            DateTimeOffset.FromUnixTimeSeconds(1702204169)) ==
            "foo=%C3%A9&q=hello%20world&wts=1702204169&w_rid=b75df2e4a7717acd509d1f72ab17a09b",
            "WBI signing filters forbidden characters and UTF-8 encodes Unicode values");

        var fakeTime = DateTimeOffset.FromUnixTimeSeconds(1000);
        var fetchGate = new TaskCompletionSource<WbiKey>(TaskCreationOptions.RunContinuationsAsynchronously);
        var fetches = 0;
        var concurrentCache = new WbiKeyService(() => { fetches++; return fetchGate.Task; }, TimeSpan.FromSeconds(30), () => fakeTime);
        var first = concurrentCache.GetKeysAsync();
        var second = concurrentCache.GetKeysAsync();
        Check(fetches == 1, "concurrent WBI key misses share one fetch");
        fetchGate.SetResult(new WbiKey(Img, Sub));
        var keys = await Task.WhenAll(first, second);
        Check(keys[0].ImgKey == Img && keys[1].SubKey == Sub, "coalesced waiters receive validated keys");
        Check(!await concurrentCache.LoadWbiKeyAsync(), "LoadWbiKey compatibility result is false for a valid warm cache");

        fakeTime = fakeTime.AddSeconds(30);
        var refreshedCalls = 0;
        var expiryCache = new WbiKeyService(() => Task.FromResult(++refreshedCalls == 1
            ? new WbiKey(Img, Sub) : new WbiKey(new string('a', 32), new string('b', 32))), TimeSpan.FromSeconds(30), () => fakeTime);
        await expiryCache.GetKeysAsync();
        fakeTime = fakeTime.AddSeconds(30);
        var expired = await expiryCache.GetKeysAsync();
        Check(refreshedCalls == 2 && expired.ImgKey == new string('a', 32), "expired WBI key cache refreshes at its finite TTL");

        var retryCalls = 0;
        var retryCache = new WbiKeyService(() =>
        {
            retryCalls++;
            return retryCalls == 1
                ? Task.FromException<WbiKey>(new InvalidOperationException("offline fixture"))
                : Task.FromResult(new WbiKey(Img, Sub));
        });
        try { await retryCache.GetKeysAsync(); throw new Exception("expected initial fetch failure"); }
        catch (InvalidOperationException) { assertions++; }
        Check((await retryCache.GetKeysAsync()).SubKey == Sub && retryCalls == 2, "failed WBI fetch is not cached and the next request retries");

        var malformedKeyCalls = 0;
        var malformedKeyCache = new WbiKeyService(() => Task.FromResult(++malformedKeyCalls == 1
            ? new WbiKey(Img, "short") : new WbiKey(Img, Sub)));
        try { await malformedKeyCache.GetKeysAsync(); throw new Exception("expected malformed-key failure"); }
        catch (FormatException) { assertions++; }
        Check((await malformedKeyCache.GetKeysAsync()).ImgKey == Img && malformedKeyCalls == 2,
            "malformed WBI key lengths do not enter the cache and later fetches retry");

        try { new WbiKeyService(() => Task.FromResult(new WbiKey(Img, Sub)), TimeSpan.MaxValue); throw new Exception("expected invalid TTL"); }
        catch (ArgumentOutOfRangeException) { assertions++; }

        var sourceStore = new FakeCookieStore();
        var cookieSettings = new Dictionary<string, string> { ["CookieCSRF"] = "setting-csrf" };
        var cookieService = NewCookieService(sourceStore, cookieSettings, _ => Task.FromResult(new HttpResults { code = 500 }));
        Check(cookieService.GetCSRF() == "setting-csrf" && sourceStore.Read("bili_jct", "") == "setting-csrf",
            "CSRF compatibility sync imports the existing setting into browser cookies");
        sourceStore.Write("bili_jct", "browser-csrf");
        cookieSettings.Remove("CookieCSRF");
        Check(cookieService.GetCSRF() == "browser-csrf" && cookieSettings["CookieCSRF"] == "browser-csrf",
            "CSRF compatibility sync persists an existing browser cookie into settings");

        var infoCalls = 0;
        var infoGate = new TaskCompletionSource<HttpResults>(TaskCreationOptions.RunContinuationsAsynchronously);
        var concurrentCookie = NewCookieService(new FakeCookieStore(), new Dictionary<string, string>(), _ => { infoCalls++; return infoGate.Task; }, () => true);
        var checkOne = concurrentCookie.NeedRefreshCookieAsync();
        var checkTwo = concurrentCookie.NeedRefreshCookieAsync();
        Check(infoCalls == 1, "concurrent cookie-info checks share one request");
        infoGate.SetResult(Http(200, "{\"code\":0,\"data\":{\"timestamp\":12345,\"refresh\":true}}"));
        Check(await checkOne && await checkTwo && concurrentCookie.CookieTimestamp == 12345,
            "cookie-info success validates and publishes refresh state and timestamp");

        var retryInfoCalls = 0;
        var retryCookie = NewCookieService(new FakeCookieStore(), new Dictionary<string, string>(), _ =>
        {
            retryInfoCalls++;
            return Task.FromResult(retryInfoCalls == 1 ? Http(503, "") : Http(200, "{\"code\":0,\"data\":{\"timestamp\":5,\"refresh\":true}}"));
        }, () => true);
        Check(!await retryCookie.NeedRefreshCookieAsync() && await retryCookie.NeedRefreshCookieAsync() && retryInfoCalls == 2,
            "synchronous HTTP failure clears the single-flight and allows a later retry");

        var malformedCalls = 0;
        var malformedCookie = NewCookieService(new FakeCookieStore(), new Dictionary<string, string>(), _ =>
        {
            malformedCalls++;
            return Task.FromResult(Http(200, malformedCalls == 1 ? "{\"code\":-1,\"message\":\"error\"}" : "{\"code\":0,\"data\":{\"timestamp\":7,\"refresh\":true}}"));
        }, () => true);
        Check(!await malformedCookie.NeedRefreshCookieAsync() && await malformedCookie.NeedRefreshCookieAsync() && malformedCalls == 2,
            "HTTP 200 with a business-error envelope does not initialize or poison cookie refresh state");

        var missingCodeCalls = 0;
        var missingCodeCookie = NewCookieService(new FakeCookieStore(), new Dictionary<string, string>(), _ =>
        {
            missingCodeCalls++;
            return Task.FromResult(Http(200, missingCodeCalls == 1
                ? "{\"data\":{\"timestamp\":9,\"refresh\":true}}"
                : "{\"code\":0,\"data\":{\"timestamp\":9,\"refresh\":true}}"));
        }, () => true);
        Check(!await missingCodeCookie.NeedRefreshCookieAsync() && await missingCodeCookie.NeedRefreshCookieAsync() && missingCodeCalls == 2,
            "HTTP 200 missing the cookie-info business code is rejected and retried");

        await TestRefreshConfirmDispatch();
        await TestConfirmFailureLeavesRefreshRequired();
        await TestRefreshMissingCodeDoesNotRotateToken();
        await TestApiHelperWbiWrapper();
        TestMigratedEndpointRequests();

        Console.WriteLine("API-01 actual-source cookie/WBI/API endpoint probes passed: " + assertions);
    }

    private static async Task TestRefreshConfirmDispatch()
    {
        SettingHelper.Reset();
        SettingHelper.SetValue("CookieCSRF", "csrf-original");
        SettingHelper.SetValue("CookieRefreshToken", "old-refresh-token");
        var sent = new List<ApiModel>();
        var confirmSent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new global::CookieService(new FakeCookieStore(), key => SettingHelper.GetValue<string>(key, ""),
            SettingHelper.SetValue, () => true, api =>
            {
                sent.Add(api);
                if (api.baseUrl.Contains("/correspond/")) return Task.FromResult(Http(200, "<div id=\"1-name\">refresh-csrf</div>"));
                if (api.baseUrl.EndsWith("/cookie/refresh")) return Task.FromResult(Http(200, "{\"code\":0,\"data\":{\"refresh_token\":\"new-refresh-token\"}}"));
                if (api.baseUrl.EndsWith("/confirm/refresh"))
                {
                    confirmSent.TrySetResult(true);
                    return Task.FromResult(Http(200, "{\"code\":0}"));
                }
                return Task.FromResult(Http(500, ""));
            });
        service.CookieTimestamp = 123;
        ApiHelper.Cookie = service;
        ApiHelper.RefreshCookie();
        await confirmSent.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Check(sent.Count == 3 && sent[0].method == RestSharp.Method.Get &&
            sent[1].method == RestSharp.Method.Post && sent[2].method == RestSharp.Method.Post &&
            sent[2].baseUrl == "https://passport.bilibili.com/x/passport-login/web/confirm/refresh",
            "ApiHelper async-void wrapper dispatches the confirm-refresh POST after the refresh response");
        Check(sent[2].body == "csrf=csrf-original&refresh_token=old-refresh-token" &&
            SettingHelper.GetValue<string>("CookieRefreshToken", "") == "new-refresh-token",
            "confirm-refresh request preserves the established old-token body and stores the rotated token");
    }

    private static async Task TestConfirmFailureLeavesRefreshRequired()
    {
        var sent = 0;
        var settings = new Dictionary<string, string> { ["CookieRefreshToken"] = "old-token", ["CookieCSRF"] = "csrf" };
        var service = NewCookieService(new FakeCookieStore(), settings, _ =>
        {
            sent++;
            if (sent == 1) return Task.FromResult(Http(200, "{\"code\":0,\"data\":{\"timestamp\":123,\"refresh\":true}}"));
            if (sent == 2) return Task.FromResult(Http(200, "<div id=\"1-name\">new-csrf</div>"));
            if (sent == 3) return Task.FromResult(Http(200, "{\"code\":0,\"data\":{\"refresh_token\":\"rotated\"}}"));
            return Task.FromResult(Http(503, ""));
        }, () => true);
        Check(await service.NeedRefreshCookieAsync(), "fixture starts with refresh required");
        await service.RefreshCookieAsync();
        Check(service.NeedRefreshCookie && sent == 4, "failed confirm-refresh response leaves cookie refresh state required");
    }

    private static async Task TestRefreshMissingCodeDoesNotRotateToken()
    {
        var sent = 0;
        var settings = new Dictionary<string, string> { ["CookieRefreshToken"] = "old-token", ["CookieCSRF"] = "csrf" };
        var service = NewCookieService(new FakeCookieStore(), settings, _ =>
        {
            sent++;
            if (sent == 1) return Task.FromResult(Http(200, "{\"code\":0,\"data\":{\"timestamp\":123,\"refresh\":true}}"));
            if (sent == 2) return Task.FromResult(Http(200, "<div id=\"1-name\">new-csrf</div>"));
            return Task.FromResult(Http(200, "{\"data\":{\"refresh_token\":\"unvalidated-token\"}}"));
        }, () => true);
        Check(await service.NeedRefreshCookieAsync(), "fixture starts with refresh required");
        await service.RefreshCookieAsync();
        Check(sent == 3 && settings["CookieRefreshToken"] == "old-token" && service.NeedRefreshCookie,
            "refresh response missing its API code does not rotate the token or mark refresh complete");
    }

    private static async Task TestApiHelperWbiWrapper()
    {
        var fetches = 0;
        BiliLite.Helpers.Api01TestHooks.RequestHandler = api =>
        {
            fetches++;
            var badEnvelope = fetches == 1;
            var result = badEnvelope
                ? "{\"code\":-1,\"message\":\"fixture\"}"
                : "{\"code\":-101,\"message\":\"not logged in\",\"data\":{\"wbi_img\":{\"img_url\":\"https://i.test/" + Img + ".png\",\"sub_url\":\"https://i.test/" + Sub + ".png\"}}}";
            return Task.FromResult(Http(200, result));
        };
        try { await ApiHelper.GetWbiSign("foo=114"); throw new Exception("expected invalid WBI envelope"); }
        catch (InvalidOperationException) { assertions++; }
        var signed = await ApiHelper.GetWbiSign("foo=114");
        Check(fetches == 2 && signed.Contains("w_rid=") && ApiHelper.wbitoken_current.img_url == Img,
            "ApiHelper WBI wrapper rejects other errors but accepts guest -101 keys, caches success, and updates public key fields");
        await ApiHelper.GetWbiSign("foo=115");
        Check(fetches == 2, "ApiHelper WBI wrapper reuses a fresh key cache");
    }

    private static void TestMigratedEndpointRequests()
    {
        var store = new FakeCookieStore();
        store.Write("bili_jct", "csrf-api");
        ApiHelper.Cookie = new global::CookieService(store, _ => "", (_, __) => { }, () => true);
        var danmaku = new DanmakuApi().Like("42", "84", 1);
        Check(danmaku.method == RestSharp.Method.Post && danmaku.baseUrl == "https://api.bilibili.com/x/v2/dm/thumbup/add" &&
            danmaku.body == "&dmid=42&oid=84&op=1&csrf=csrf-api" && danmaku.need_cookie,
            "DanmakuApi builds the expected authenticated like request");
        var unlike = new DanmakuApi().Like("42", "84", 2);
        Check(unlike.body.Contains("&op=2&"), "DanmakuApi also builds the unlike operation request");

        var migrated = new BiliLite.Models.Requests.Api.EmoteApi().UserEmote(EmoteBusiness.reply);
        var compatibility = new BiliLite.Api.EmoteApi().UserEmote(EmoteBusiness.dynamic);
        Check(migrated.baseUrl == compatibility.baseUrl && migrated.headers.ContainsKey("x-bili-aurora-zone") &&
            migrated.need_cookie && migrated.parameter.Contains("business=reply") && compatibility.parameter.Contains("business=dynamic"),
            "migrated EmoteApi and old namespace shim preserve existing endpoint construction");
    }

    private static BiliLite.Services.CookieService NewCookieService(FakeCookieStore store, IDictionary<string, string> settings,
        Func<ApiModel, Task<HttpResults>> send, Func<bool> loggedIn = null)
    {
        return new BiliLite.Services.CookieService(store,
            key => settings.TryGetValue(key, out var value) ? value : "",
            (key, value) => settings[key] = value,
            loggedIn ?? (() => false), send);
    }

    private static HttpResults Http(int code, string body) => new HttpResults { code = code, status = code == 200, results = body };

    private sealed class FakeCookieStore : ICookieStore
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>();
        public void Write(string name, string value) => values[name] = value;
        public string Read(string name, string fallback) => values.TryGetValue(name, out var value) ? value : fallback;
    }
}

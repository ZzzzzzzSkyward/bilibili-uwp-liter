using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Models;
using BiliLite.Modules.Player.Playurl;
using BiliLite.Services;

internal static class Program
{
    private const string TvAppkey = "tv-appkey";
    private const string TvSecret = "tv-secret";
    private const string AndroidAppkey = "android-appkey";
    private const string AndroidSecret = "android-secret";
    private const string LoginAppkey = "login-appkey";
    private const string LoginSecret = "login-secret";

    private static int assertions;
    private static readonly List<string> Log = new List<string>();

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception("AUTH-03 failed: " + message);
    }

    private static void Main()
    {
        SeedAppKeys();
        // App 的接线：登录事件是唯一清掉「已失效」结论的地方
        MessageCenter.LoginedEvent += (_, __) => BiliLite.Services.CookieService.ClearReloginRequired();

        RequestShapeAndOneAttemptPerPreflight();
        ServerRejectionLeavesSettingsUntouched();
        TransportFailureReturnsFalseWithoutThrowing();
        EmptyRefreshTokenSendsNothing();
        RenewalLogReportsMissingCookie();
        DeadCookieUgcRequestUsesAccessKey();
        HealthySessionKeepsTheCookieRequest();
        ProfileNullNoLongerThrows();
        CappedDeadSessionAnswerIsNotFinal();
        CappedWebFallbackTable();
        SourceChecks();

        Console.WriteLine("PASS " + assertions + " assertions");
    }

    private static void SeedAppKeys()
    {
        SettingHelper.SetValue("appkey_android_1", AndroidAppkey);
        SettingHelper.SetValue("appkey_android_2", AndroidSecret);
        SettingHelper.SetValue("appkey_login_1", LoginAppkey);
        SettingHelper.SetValue("appkey_login_2", LoginSecret);
        SettingHelper.SetValue("appkey_video_1", "video-appkey");
        SettingHelper.SetValue("appkey_video_2", "video-secret");
        SettingHelper.SetValue("appkey_tv_1", TvAppkey);
        SettingHelper.SetValue("appkey_tv_2", TvSecret);
        SettingHelper.SetValue("appkey_web_1", "web-appkey");
        SettingHelper.SetValue("appkey_web_2", "web-secret");
    }

    private static void RequestShapeAndOneAttemptPerPreflight()
    {
        ResetSession();
        SettingHelper.Account.Logined = true;
        SettingHelper.Account.AccessKey = "release-ak";
        SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY, "release-ak");
        SettingHelper.SetValue(SettingHelper.Account.REFRESH_KEY, "rt/first");
        SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.Now.AddHours(2));
        MarkDeadSession();

        var now = DateTime.Now;
        Check(!AccessKeyRenewal.ShouldRenew(false, "rt", now.AddDays(9), now),
            "a live session with a key more than a day out is left alone");
        Check(AccessKeyRenewal.ShouldRenew(false, "rt", now.AddHours(23), now),
            "a key expiring inside a day is renewed");
        Check(AccessKeyRenewal.ShouldRenew(true, "rt", now.AddDays(30), now),
            "a dead cookie session renews even when the key is far from expiry");
        Check(!AccessKeyRenewal.ShouldRenew(true, "", now.AddDays(-30), now),
            "an empty refresh token never renews, not even on a dead session");

        var requests = new List<ApiModel>();
        var saved = new List<(LoginTokenInfo Token, LoginCookieInfo Cookie)>();
        var sends = 0;
        Func<string, string, Task<HttpResults>> send = (accessKey, refreshToken) =>
        {
            sends++;
            requests.Add(new AccountApi().RefreshToken(accessKey, refreshToken));
            return Task.FromResult(new HttpResults { status = true, results = SuccessBody() });
        };
        Func<LoginTokenInfo, LoginCookieInfo, Task> save = (token, cookie) =>
        {
            saved.Add((token, cookie));
            ApplySaveLogin(token);
            return Task.CompletedTask;
        };

        Check(BiliLite.Services.CookieService.NeedsRelogin, "the fixture starts from a dead cookie session");
        Check(Preflight(send, save), "the preflight renewal reports success");
        Check(sends == 1, "one preflight turns into exactly one refresh POST");
        var request = requests.Single();
        Check(request.method == RestSharp.Method.Post, "the refresh is a POST");
        Check(request.baseUrl == "https://passport.bilibili.com/api/v2/oauth2/refresh_token",
            "the request goes to the documented v2 refresh endpoint");
        Check(request.body.StartsWith("access_key=release-ak&", StringComparison.Ordinal),
            "access_key leads the body");
        Check(request.body.Contains("&appkey=" + TvAppkey + "&"), "the body carries the TV appkey");
        Check(request.body.Contains("&refresh_token=rt%2Ffirst"), "the refresh token is form-encoded");
        Check(Regex.Matches(request.body, "access_key=").Count == 1, "access_key appears once");
        Check(request.body.EndsWith("&sign=" + ExpectedSign(request.body, TvSecret), StringComparison.Ordinal),
            "sign is the last field and is derived from the TV secret");
        Check(!request.body.EndsWith("&sign=" + ExpectedSign(request.body, AndroidSecret), StringComparison.Ordinal),
            "the android secret would produce a different sign");

        var token = saved.Single().Token;
        var cookie = saved.Single().Cookie;
        Check(token.access_token == "new-ak" && token.refresh_token == "rt-rotated" &&
            token.expires_in == 7776000 && token.mid == 99182,
            "token_info is read from the nested object, not from a flattened data");
        Check(cookie != null && cookie.domains.SequenceEqual(new[] { "https://bilibili.com", "https://www.bilibili.com" }),
            "cookie_info keeps the domains SaveLogin writes into the jar");
        Check(cookie.cookies.Any(item => item.name == "ac_time_value" && item.value == "ac-fresh"),
            "cookie_info keeps ac_time_value, the token the browser chain mirrors");
        Check(SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY, "") == "new-ak" &&
            SettingHelper.GetValue(SettingHelper.Account.REFRESH_KEY, "") == "rt-rotated" &&
            SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.MinValue) > DateTime.Now.AddDays(30),
            "the rotated key, refresh token and expiry all move together");
        Check(!BiliLite.Services.CookieService.NeedsRelogin, "a successful renewal clears the relogin verdict through the login event");

        var successLines = Log.Where(line => line.StartsWith("ACCESS_KEY 已刷新", StringComparison.Ordinal)).ToList();
        Check(successLines.Count == 1, "a successful renewal leaves exactly one line");
        Check(successLines[0] == "ACCESS_KEY 已刷新 mid=99182 expires_in=7776000 cookie=present",
            "the line carries mid, expires_in and that cookie_info arrived");
        Check(Log.All(line => !line.Contains("new-ak") && !line.Contains("rt-rotated") && !line.Contains("ac-fresh")),
            "no token or cookie value reaches the log");

        Check(!Preflight(send, save), "the next preflight has nothing to do");
        Check(sends == 1, "a fresh key does not cause a second refresh");
        var skipLines = Log.Where(line => line.StartsWith("ACCESS_KEY 续期跳过", StringComparison.Ordinal)).ToList();
        Check(skipLines.Count == 1, "a declined gate leaves exactly one line per preflight");
        Check(Regex.IsMatch(skipLines[0], @"^ACCESS_KEY 续期跳过：session=有效 key=(89|90)d refresh=present$"),
            "the skip line states the session verdict, the remaining days and that a token exists");
        Check(!skipLines[0].Contains("rt-rotated") && !skipLines[0].Contains("new-ak"),
            "the skip line never prints the token");

        var flat = new List<int>();
        var flatResult = AccessKeyRenewal.RunAsync("ak", "rt",
            (_, __) => Task.FromResult(new HttpResults
            {
                status = true,
                results = "{\"code\":0,\"data\":{\"access_token\":\"new-ak\",\"refresh_token\":\"rt\",\"expires_in\":100,\"mid\":1}}"
            }),
            (t, c) => { flat.Add(1); return Task.CompletedTask; }, (message, exception) => Log.Add(message)).GetAwaiter().GetResult();
        Check(!flatResult && flat.Count == 0, "a flattened data payload is rejected instead of being read as a token");
    }

    private static void ServerRejectionLeavesSettingsUntouched()
    {
        ResetSession();
        SettingHelper.Account.Logined = true;
        SettingHelper.Account.AccessKey = "old-ak";
        SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY, "old-ak");
        SettingHelper.SetValue(SettingHelper.Account.REFRESH_KEY, "rt-old");
        var expiry = DateTime.Now.AddMinutes(-5);
        SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, expiry);

        var sends = 0;
        var saved = 0;
        var result = AccessKeyRenewal.RunAsync("old-ak", "rt-old",
            (_, __) =>
            {
                sends++;
                return Task.FromResult(new HttpResults { status = true, results = "{\"code\":-101,\"message\":\"账号未登录\"}" });
            },
            (t, c) => { saved++; return Task.CompletedTask; }, (message, exception) => Log.Add(message)).GetAwaiter().GetResult();

        Check(!result, "a rejected refresh returns false");
        Check(sends == 1, "a rejected refresh is not retried");
        Check(saved == 0, "a rejected refresh never reaches SaveLogin");
        Check(SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY, "") == "old-ak" &&
            SettingHelper.GetValue(SettingHelper.Account.REFRESH_KEY, "") == "rt-old" &&
            SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.MinValue) == expiry,
            "a rejected refresh leaves ACCESS_KEY, REFRESH_KEY and the expiry untouched");
        Check(Log.Any(line => line.Contains("code=-101")), "the rejection is logged with its code");
    }

    private static void TransportFailureReturnsFalseWithoutThrowing()
    {
        ResetSession();
        Log.Clear();
        var saved = 0;
        bool threw = false;
        bool result = false;
        try
        {
            result = AccessKeyRenewal.RunAsync("ak", "rt",
                (_, __) => throw new InvalidOperationException("socket closed"),
                (t, c) => { saved++; return Task.CompletedTask; }, (message, exception) => Log.Add(message)).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            threw = true;
        }

        Check(!threw, "a transport failure does not escape the renewal");
        Check(!result && saved == 0, "a transport failure returns false and saves nothing");
        Check(Log.Any(line => line.Contains("请求失败")), "the transport failure is logged");
    }

    private static void EmptyRefreshTokenSendsNothing()
    {
        ResetSession();
        var sends = 0;
        var result = AccessKeyRenewal.RunAsync("ak", "",
            (_, __) => { sends++; return Task.FromResult(new HttpResults { status = true, results = "{}" }); },
            (t, c) => Task.CompletedTask, (message, exception) => Log.Add(message)).GetAwaiter().GetResult();
        Check(!result && sends == 0, "an empty refresh token performs no request at all");

        Log.Clear();
        var gateNow = DateTime.Now;
        var approved = AccessKeyRenewal.ShouldRenew(true, "", gateNow.AddDays(-3), gateNow,
            (message, exception) => Log.Add(message));
        Check(!approved && Log.Count == 1, "a dead session without a token logs its skip exactly once");
        Check(Regex.IsMatch(Log[0], @"^ACCESS_KEY 续期跳过：session=无效 key=-3d refresh=missing$"),
            "the skip line says the session is dead and the refresh token is missing");
    }

    /// <summary>cookie_info 缺失时成功行要说 missing，而不是假装 jar 换新了。</summary>
    private static void RenewalLogReportsMissingCookie()
    {
        ResetSession();
        Log.Clear();
        var result = AccessKeyRenewal.RunAsync("ak", "rt",
            (_, __) => Task.FromResult(new HttpResults
            {
                status = true,
                results = "{\"code\":0,\"data\":{\"token_info\":{\"mid\":42,\"access_token\":\"ak-2\",\"refresh_token\":\"rt-2\",\"expires_in\":3600}}}"
            }),
            (token, cookie) => Task.CompletedTask, (message, exception) => Log.Add(message))
            .GetAwaiter().GetResult();
        Check(result, "a renewal without cookie_info still succeeds");
        var line = Log.Single(item => item.StartsWith("ACCESS_KEY 已刷新", StringComparison.Ordinal));
        Check(line == "ACCESS_KEY 已刷新 mid=42 expires_in=3600 cookie=missing",
            "the success line says cookie=missing when the server sent no cookie_info");
        Check(!line.Contains("ak-2") && !line.Contains("rt-2"), "the success line carries no credential");
    }

    private static void DeadCookieUgcRequestUsesAccessKey()
    {
        ResetSession();
        SettingHelper.Account.Logined = true;
        SettingHelper.Account.AccessKey = "dead-ak";
        SettingHelper.Account.Profile = new MyProfileModel { mid = 99182 };
        MarkDeadSession();

        var api = new PlayerAPI().VideoPlayUrl("170001", "500001", 80, true);
        Check(BiliLite.Services.CookieService.NeedsRelogin, "the fixture really has a dead cookie session");
        Check(!api.need_cookie, "a dead session does not attach the stale cookie jar");
        Check(api.parameter.Contains("&access_key=dead-ak") && Regex.Matches(api.parameter, "access_key=").Count == 1,
            "a dead session authenticates the UGC request with access_key");
        Check(api.parameter.Contains("&mid=99182&"), "the mid still rides on the request");
        Check(!api.parameter.Contains("SESSDATA") && (api.headers == null ||
            api.headers.All(header => header.Key.IndexOf("cookie", StringComparison.OrdinalIgnoreCase) < 0)),
            "no cookie header and no session cookie leak into a dead-session request");
    }

    private static void HealthySessionKeepsTheCookieRequest()
    {
        ResetSession();
        SettingHelper.Account.Logined = true;
        SettingHelper.Account.AccessKey = "live-ak";
        SettingHelper.Account.Profile = new MyProfileModel { mid = 99182 };

        var api = new PlayerAPI().VideoPlayUrl("170001", "500001", 80, true);
        Check(!BiliLite.Services.CookieService.NeedsRelogin, "the fixture session is healthy");
        Check(api.need_cookie, "a healthy session still ships the cookie jar");
        Check(!api.parameter.Contains("access_key="), "a healthy session does not add access_key to the UGC request");
    }

    private static void ProfileNullNoLongerThrows()
    {
        ResetSession();
        SettingHelper.Account.Logined = true;
        SettingHelper.Account.Profile = new MyProfileModel { mid = 99182 };
        var withProfile = new PlayerAPI().VideoPlayUrl("170001", "500001", 80, true);

        SettingHelper.Account.Profile = null;
        ApiModel withoutProfile = null;
        bool threw = false;
        try
        {
            withoutProfile = new PlayerAPI().VideoPlayUrl("170001", "500001", 80, true);
        }
        catch (Exception)
        {
            threw = true;
        }

        Check(!threw, "a logged-in account without a cached profile no longer throws");
        Check(withoutProfile.parameter.Contains("&mid=&"), "the mid parameter is simply empty without a profile");
        Check(withProfile.parameter.Replace("mid=99182", "mid=") == withoutProfile.parameter,
            "everything else in the request stays byte-identical when the profile is present");
    }

    private static void CappedDeadSessionAnswerIsNotFinal()
    {
        BiliLite.Services.CookieService.ClearReloginRequired();
        Check(!BiliLite.Services.CookieService.WebPlayUrlCappedOnDeadSession(80, new[] { 16, 32, 64 }),
            "a healthy session keeps its capped answer, real 480p uploads exist");
        MarkDeadSession();
        Check(BiliLite.Services.CookieService.WebPlayUrlCappedOnDeadSession(80, new[] { 16, 32, 64 }),
            "a dead session's 480p-only list is not the requested quality");
        Check(!BiliLite.Services.CookieService.WebPlayUrlCappedOnDeadSession(64, new[] { 16, 32, 64 }),
            "a genuine answer for the requested quality is still accepted on a dead session");
        Check(BiliLite.Services.CookieService.WebPlayUrlCappedOnDeadSession(80, new int[0]),
            "an empty quality list on a dead session is not a final answer either");
        Check(!BiliLite.Services.CookieService.WebPlayUrlCappedOnDeadSession(80, null), "a missing list is not treated as capped");
        BiliLite.Services.CookieService.ClearReloginRequired();
    }

    /// <summary>
    /// 会话状态 × gRPC 结果的三格表：走一遍 BiliVideoPlayUrlRequest.GetPlayUrlInfo 的顺序，
    /// 确认提示不会把播放弄没 —— gRPC 拿不到时那份更低的 web 结果必须还回去。
    /// </summary>
    private static void CappedWebFallbackTable()
    {
        ResetSession();
        SettingHelper.Account.Logined = true;
        MarkDeadSession();

        var grpcOk = PlayUrlInfo(80);
        var deadCapped = ResolveLikeUgcPath(CappedWebResult(), 80, () => grpcOk);
        Check(deadCapped.GrpcCalls == 1, "a dead session's capped list still sends the request to gRPC");
        Check(ReferenceEquals(deadCapped.Result, grpcOk), "gRPC wins when it answers the requested quality");

        var grpcDown = BiliPlayUrlQualitesInfo.Failure("grpc down");
        var deadCappedGrpcDown = ResolveLikeUgcPath(CappedWebResult(), 80, () => grpcDown);
        Check(deadCappedGrpcDown.GrpcCalls == 1, "the capped list asks gRPC once, no loop");
        Check(ReferenceEquals(deadCappedGrpcDown.Result, deadCappedGrpcDown.Web) && deadCappedGrpcDown.Result.Success,
            "gRPC failing falls back to the capped web result instead of failing the request");
        Check(deadCappedGrpcDown.Result.Qualites.Any(item => item.QualityID == 32),
            "the retained result still carries the playable 480p entry");

        BiliLite.Services.CookieService.ClearReloginRequired();
        var healthy = ResolveLikeUgcPath(CappedWebResult(), 80, () => grpcOk);
        Check(healthy.GrpcCalls == 0, "a healthy session's capped answer never triggers gRPC");
        Check(ReferenceEquals(healthy.Result, healthy.Web) && healthy.Result.Success,
            "a healthy session keeps its own answer untouched");

        var webDown = BiliPlayUrlQualitesInfo.Failure("web down");
        var webDownGrpcDown = ResolveLikeUgcPath(webDown, 80, () => grpcDown);
        Check(webDownGrpcDown.GrpcCalls == 1 && !webDownGrpcDown.Result.Success,
            "a genuine web failure still reaches gRPC and still fails when gRPC fails too");
        BiliLite.Services.CookieService.ClearReloginRequired();
    }

    /// <summary>照 GetPlayUrlInfo 的顺序走：web → 需要时 gRPC → gRPC 失败回落 web。</summary>
    private static (BiliPlayUrlQualitesInfo Web, BiliPlayUrlQualitesInfo Result, int GrpcCalls) ResolveLikeUgcPath(
        BiliPlayUrlQualitesInfo web, int requestedQuality, Func<BiliPlayUrlQualitesInfo> grpc)
    {
        var capped = web.Success && BiliLite.Services.CookieService.WebPlayUrlCappedOnDeadSession(
            requestedQuality, web.Qualites.Select(item => item.QualityID));
        if (!PlayUrlFallback.NeedsGrpc(web.Success, capped)) return (web, web, 0);
        var chosen = PlayUrlFallback.Choose(web, grpc());
        if (chosen.Success) return (web, chosen, 1);
        return (web, BiliPlayUrlQualitesInfo.Failure("both failed"), 1);
    }

    private static BiliPlayUrlQualitesInfo CappedWebResult() => PlayUrlInfo(32);

    private static BiliPlayUrlQualitesInfo PlayUrlInfo(int qualityId) => new BiliPlayUrlQualitesInfo
    {
        Success = true,
        Qualites = new List<BiliPlayUrlInfo> { new BiliPlayUrlInfo { QualityID = qualityId, HasPlayUrl = true } }
    };

    private static void SourceChecks()
    {
        var root = FindRepoRoot();
        var sources = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText).ToList();

        Check(sources.All(text => !text.Contains("api/oauth2/refreshToken")),
            "no source file still points at the legacy refresh endpoint");
        Check(sources.All(text => !Regex.IsMatch(text, @"DeserializeObject<LoginTokenInfo>\(obj\[""data""\]")),
            "the flat LoginTokenInfo parse is gone from the refresh path");

        var account = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Modules/User/Account.cs"));
        var refresh = MethodBody(account, "public async Task<bool> RefreshToken()", "public class HomeUserCardModel");
        Check(refresh.Contains("AccessKeyRenewal.RunAsync("), "Account.RefreshToken goes through the renewal service");
        Check(refresh.Contains("SaveLogin(token.access_token, token.refresh_token, token.expires_in,"),
            "the parsed token is handed to the existing SaveLogin path");
        Check(refresh.Contains("null, cookie)"), "cookie_info is passed to SaveLogin instead of dropped");
        Check(!refresh.Contains("throw ex"), "a failed refresh returns false instead of throwing");

        var saveBody = MethodBody(account, "public async Task<bool> SaveLogin(", "public bool CommitCookieLogin(");
        Check(saveBody.Contains("MessageCenter.SendLogined();"),
            "SaveLogin still announces the login, which is what clears the relogin verdict");

        var app = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/App.xaml.cs"));
        var preflight = MethodBody(app, "private static async Task PreflightLoginAsync()",
            "private void RegisterExceptionHandlingSynchronizationContext()");
        Check(Regex.Matches(preflight, @"AccessKeyRenewal\.ShouldRenew\(").Count == 1,
            "the preflight consults the gate exactly once");
        Check(Regex.Matches(preflight, @"\.RefreshToken\(\)").Count == 1, "the preflight renews at most once");
        Check(!preflight.Contains("while (") && !preflight.Contains("for (") && !preflight.Contains("do {"),
            "the preflight never loops over the renewal");
        Check(!preflight.Contains("ClearReloginRequired"),
            "the preflight does not add a second way to clear the verdict");
        Check(Regex.Matches(app, @"ClearReloginRequired\(\)").Count == 1, "the app clears the verdict in one place only");
        Check(app.Contains("MessageCenter.LoginedEvent += OnSessionChanged"),
            "that single place is the login event handler");

        var accountApi = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Api/AccountApi.cs"));
        var builder = MethodBody(accountApi, "public ApiModel RefreshToken(string accessKey, string refreshToken)",
            "public ApiModel WbiToken()");
        Check(builder.Contains("/api/v2/oauth2/refresh_token"), "the builder targets the v2 endpoint");
        Check(builder.Contains("ApiHelper.AndroidTVKey") && !builder.Contains("AndroidKey"),
            "the refresh is signed with the TV appkey pair only");
        Check(Regex.Matches(builder, @"GetSign\(api\.body, ApiHelper\.AndroidTVKey\)").Count == 1 &&
            builder.IndexOf("GetSign(", StringComparison.Ordinal) > builder.IndexOf("refresh_token=", StringComparison.Ordinal),
            "the signature is appended after every signed parameter");
        Check(builder.IndexOf("body = $\"access_key=", StringComparison.Ordinal) >= 0 &&
            builder.IndexOf("body = $\"access_key=", StringComparison.Ordinal) <
            builder.IndexOf("MustParameter(ApiHelper.AndroidTVKey)", StringComparison.Ordinal),
            "access_key is emitted before the shared parameter block");

        var playerApi = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Api/PlayerAPI.cs"));
        var ugc = MethodBody(playerApi, "public ApiModel VideoPlayUrl(", "public ApiModel SeasonPlayUrl(");
        Check(ugc.Contains("BiliLite.Services.CookieService.NeedsRelogin"), "the UGC builder reads the session verdict");
        Check(ugc.Contains("need_cookie = !deadCookie"), "a dead session drops the jar for that request");
        Check(ugc.Contains("access_key={SettingHelper.Account.AccessKey}"), "a dead session authenticates with access_key");
        Check(ugc.Contains("Profile?.mid"), "the mid no longer dereferences a missing profile");

        var playUrl = File.ReadAllText(Path.Combine(root,
            "src/BiliLite.UWP/Modules/Player/Playurl/BiliPayUrlRequest.cs"));
        var web = MethodBody(playUrl, "private async Task<BiliPlayUrlQualitesInfo> GetPlayUrlUseWebApi(PlayInfo playInfo, int qualityID)",
            "private async Task<BiliPlayUrlQualitesInfo> GetPlayUrlUseGrpc(");
        Check(web.Contains("WebPlayUrlCappedOnDeadSession("),
            "the UGC web path asks whether a dead session capped the answer");
        Check(web.IndexOf("WebPlayUrlCappedOnDeadSession(", StringComparison.Ordinal) <
            web.IndexOf("return jsonResult;", StringComparison.Ordinal),
            "the capped answer is only recorded, the parsed web result is still returned");
        Check(!web.Contains("登录已失效"), "the web path no longer invents a dead-session failure text");

        var ugcInfo = MethodBody(playUrl, "class BiliVideoPlayUrlRequest : BiliPlayUrlRequest",
            "class BiliSeasonPlayUrlRequest : BiliPlayUrlRequest");
        Check(ugcInfo.IndexOf("GetPlayUrlUseWebApi(", StringComparison.Ordinal) <
            ugcInfo.IndexOf("GetPlayUrlUseGrpc(", StringComparison.Ordinal),
            "the gRPC branch stays behind the web branch");
        Check(ugcInfo.Contains("PlayUrlFallback.NeedsGrpc("), "gRPC runs only when the fallback policy asks");
        Check(ugcInfo.IndexOf("PlayUrlFallback.NeedsGrpc(", StringComparison.Ordinal) <
            ugcInfo.IndexOf("var grpcResult = await GetPlayUrlUseGrpc(playInfo, qualityID)", StringComparison.Ordinal),
            "the policy decides before gRPC is called");
        Check(ugcInfo.Contains("PlayUrlFallback.Choose(webResult, grpcResult)") &&
            ugcInfo.IndexOf("PlayUrlFallback.Choose(", StringComparison.Ordinal) >
            ugcInfo.IndexOf("var grpcResult = await GetPlayUrlUseGrpc(playInfo, qualityID)", StringComparison.Ordinal),
            "the gRPC result is preferred but the web result is kept for the fallback");
        Check(!ugcInfo.Contains("登录已失效"),
            "the request class no longer fails the whole request over a capped dead-session answer");
    }

    /// <summary>把一次启动/恢复要走的路走一遍：先问闸门，再发唯一一次刷新。</summary>
    private static bool Preflight(Func<string, string, Task<HttpResults>> send,
        Func<LoginTokenInfo, LoginCookieInfo, Task> save)
    {
        var refreshToken = SettingHelper.GetValue(SettingHelper.Account.REFRESH_KEY, "");
        var expiresAt = SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.MinValue);
        if (!AccessKeyRenewal.ShouldRenew(BiliLite.Services.CookieService.NeedsRelogin, refreshToken, expiresAt, DateTime.Now,
                (message, exception) => Log.Add(message))) return false;
        return AccessKeyRenewal.RunAsync(SettingHelper.Account.AccessKey, refreshToken, send, save, (message, exception) => Log.Add(message))
            .GetAwaiter().GetResult();
    }

    /// <summary>镜像 Account.SaveLogin 的落盘和广播：真正的实现由源码断言覆盖。</summary>
    private static void ApplySaveLogin(LoginTokenInfo token)
    {
        SettingHelper.Account.AccessKey = token.access_token;
        SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY, token.access_token);
        SettingHelper.SetValue(SettingHelper.Account.USER_ID, token.mid);
        SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.Now.AddSeconds(token.expires_in));
        SettingHelper.SetValue(SettingHelper.Account.REFRESH_KEY, token.refresh_token);
        MessageCenter.SendLogined();
    }

    private static void MarkDeadSession()
    {
        var service = new BiliLite.Services.CookieService(new BiliLite.Services.WindowsCookieStore(),
            key => SettingHelper.GetValue<string>(key, ""),
            (key, value) => SettingHelper.SetValue(key, value),
            () => true,
            _ => Task.FromResult(new HttpResults { code = 200, results = "{\"code\":-101}" }),
            _ => { },
            (message, exception) => Log.Add(message));
        service.NeedRefreshCookieAsync().GetAwaiter().GetResult();
        Check(BiliLite.Services.CookieService.NeedsRelogin, "-101 from cookie/info marks the session dead");
    }

    private static void ResetSession()
    {
        SettingHelper.Reset();
        SeedAppKeys();
        SettingHelper.Account.Logined = false;
        SettingHelper.Account.AccessKey = "";
        SettingHelper.Account.Profile = null;
        Log.Clear();
        BiliLite.Services.CookieService.ClearReloginRequired();
    }

    private static string SuccessBody() =>
        "{\"code\":0,\"message\":\"0\",\"ttl\":1,\"data\":{" +
        "\"token_info\":{\"mid\":99182,\"access_token\":\"new-ak\",\"refresh_token\":\"rt-rotated\"," +
        "\"expires_in\":7776000}," +
        "\"cookie_info\":{\"cookies\":[" +
        "{\"name\":\"SESSDATA\",\"value\":\"sess-new\",\"http_only\":1,\"expires\":1893456000,\"secure\":1}," +
        "{\"name\":\"ac_time_value\",\"value\":\"ac-fresh\",\"http_only\":1,\"expires\":1893456000,\"secure\":1}]," +
        "\"domains\":[\"https://bilibili.com\",\"https://www.bilibili.com\"]}}}";

    /// <summary>独立算一遍签名：通用参数按字典序拼起来，再拼 TV secret。</summary>
    private static string ExpectedSign(string body, string secret)
    {
        var signed = body.Substring(0, body.LastIndexOf("&sign=", StringComparison.Ordinal));
        var parts = signed.Split('&').ToList();
        parts.Sort(StringComparer.Ordinal);
        return Utils.ToMD5(string.Join("&", parts) + secret).ToLowerInvariant();
    }

    private static string MethodBody(string text, string signature, string nextMarker)
    {
        var start = text.IndexOf(signature, StringComparison.Ordinal);
        if (start < 0) throw new Exception("AUTH-03 failed: cannot locate " + signature);
        var end = text.IndexOf(nextMarker, start + signature.Length, StringComparison.Ordinal);
        if (end < 0) throw new Exception("AUTH-03 failed: cannot bound " + signature);
        return text.Substring(start, end - start);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src/BiliLite.UWP/Services/AccessKeyRenewal.cs")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new Exception("AUTH-03 failed: repository root not found from " + AppContext.BaseDirectory);
    }
}

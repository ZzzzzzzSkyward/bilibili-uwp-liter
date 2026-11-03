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
        SaveResultDecidesTheOutcome();
        SaveThrowIsUncommitted();
        PartialCookieCommitIsReported();
        RejectedCookieWriteIsAPartialCommit();
        ServerRejectionLeavesSettingsUntouched();
        TransportFailureReturnsFalseWithoutThrowing();
        EmptyRefreshTokenSendsNothing();
        RenewalLogReportsMissingCookie();
        DeadCookieUgcRequestUsesAccessKey();
        HealthySessionKeepsTheCookieRequest();
        ProfileNullNoLongerThrows();
        CappedDeadSessionAnswerIsNotFinal();
        CappedWebFallbackTable();
        ResponseTokenBeatsTheCookieMirror();
        CookieMirrorOnlyMigratesALegacySession();
        CookieMirrorRefusesAnotherAccountsToken();
        CrossAccountLoginResetsTheWebToken();
        AnotherAccountsWebTokenIsNeverSubmitted();
        ManualEntryBelongsToTheCurrentSession();
        PendingConfirmationBlocksMigration();
        RecordedAppkeyDrivesTheRenewalRequest();
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
        Func<LoginTokenInfo, LoginCookieInfo, Task<CredentialCommit>> save = (token, cookie) =>
        {
            saved.Add((token, cookie));
            ApplySaveLogin(token);
            return Task.FromResult(Committed(cookie));
        };

        Check(BiliLite.Services.CookieService.NeedsRelogin, "the fixture starts from a dead cookie session");
        Check(Preflight(send, save).Committed, "the preflight renewal reports success");
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
        Check(successLines[0] == "ACCESS_KEY 已刷新 mid=99182 expires_in=7776000 token=saved cookie=installed",
            "the line carries mid, expires_in, the committed token and that cookies landed in the jar");
        Check(Log.All(line => !line.Contains("new-ak") && !line.Contains("rt-rotated") && !line.Contains("ac-fresh")),
            "no token or cookie value reaches the log");

        var secondPreflight = Preflight(send, save);
        Check(!secondPreflight.Committed && secondPreflight.Outcome == RenewalOutcome.NotAttempted,
            "the next preflight has nothing to do");
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
            (t, c) => { flat.Add(1); return Task.FromResult(Committed(c)); },
            (message, exception) => Log.Add(message)).GetAwaiter().GetResult();
        Check(!flatResult.Committed && flatResult.Outcome == RenewalOutcome.TransportFailed && flat.Count == 0,
            "a flattened data payload is rejected as a retryable transport problem, not read as a token");
    }

    /// <summary>
    /// 落盘结果说了算：回调报告令牌没写进去，续期就必须报未落盘，
    /// 不许再出现「已刷新」那行成功日志（修复前这里报的是成功）。
    /// </summary>
    private static void SaveResultDecidesTheOutcome()
    {
        ResetSession();
        var result = AccessKeyRenewal.RunAsync("ak", "rt",
            (_, __) => Task.FromResult(new HttpResults { status = true, results = SuccessBody() }),
            (token, cookie) => Task.FromResult(new CredentialCommit { CookieOffered = true }),
            (message, exception) => Log.Add(message)).GetAwaiter().GetResult();

        Check(result.Outcome == RenewalOutcome.Uncommitted, "a save that wrote nothing is reported as uncommitted");
        Check(!result.Committed && !result.CredentialUsable, "an uncommitted save is not a usable credential");
        Check(result.Retryable && !result.SessionDead, "an uncommitted save is retryable and is not a dead session");
        Check(Log.All(line => !line.StartsWith("ACCESS_KEY 已刷新", StringComparison.Ordinal)),
            "an uncommitted save never logs a success line");
        Check(Log.Any(line => line.StartsWith("ACCESS_KEY 续期未落盘", StringComparison.Ordinal)),
            "the uncommitted save is logged as not persisted");
    }

    /// <summary>落盘抛异常也只算未落盘：既不逃出去，也不能被当成成功。</summary>
    private static void SaveThrowIsUncommitted()
    {
        ResetSession();
        bool threw = false;
        RenewalResult result = null;
        try
        {
            result = AccessKeyRenewal.RunAsync("ak", "rt",
                (_, __) => Task.FromResult(new HttpResults { status = true, results = SuccessBody() }),
                (token, cookie) => throw new InvalidOperationException("storage full"),
                (message, exception) => Log.Add(message)).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            threw = true;
        }

        Check(!threw, "a save failure does not escape the renewal");
        Check(result.Outcome == RenewalOutcome.Uncommitted && result.Retryable && !result.CredentialUsable,
            "a save that threw is an uncommitted, retryable failure");
        Check(Log.All(line => !line.StartsWith("ACCESS_KEY 已刷新", StringComparison.Ordinal)),
            "a save that threw never logs a success line");
    }

    /// <summary>令牌写好了、cookie 没装进去：两件事分开报，别用一个「成功」盖住另一个。</summary>
    private static void PartialCookieCommitIsReported()
    {
        ResetSession();
        var result = AccessKeyRenewal.RunAsync("ak", "rt",
            (_, __) => Task.FromResult(new HttpResults { status = true, results = SuccessBody() }),
            (token, cookie) => Task.FromResult(new CredentialCommit
            {
                TokenSaved = true,
                CookieOffered = true,
                CookieInstalled = false,
            }),
            (message, exception) => Log.Add(message)).GetAwaiter().GetResult();

        Check(result.Outcome == RenewalOutcome.PartlyCommitted, "a token-only commit is partial, not full success");
        Check(result.CredentialUsable && result.CookieOffered && !result.CookieInstalled,
            "the app key stays usable while the web session was not restored");
        Check(!result.SessionDead && !result.Retryable,
            "a partial commit is neither a dead session nor a transport retry");
        Check(Log.Any(line => line == "ACCESS_KEY 续期只落了令牌 mid=99182 token=saved cookie=failed"),
            "the partial commit says the token landed and the cookie did not");
        Check(Log.All(line => !line.StartsWith("ACCESS_KEY 已刷新", StringComparison.Ordinal)),
            "a partial commit never claims a full renewal");
    }

    /// <summary>
    /// 评审：jar 返回 false 是 Windows 拒收，不是「装上了」。令牌四件套照旧落盘，
    /// 但 cookie 这一位必须算失败，续期结论停在 PartlyCommitted —— 修复前这里数成 installed，报 Committed。
    /// </summary>
    private static void RejectedCookieWriteIsAPartialCommit()
    {
        var expiry = DateTime.Now.AddSeconds(7776000);
        var writes = new List<CookieWrite>
        {
            new CookieWrite
            {
                Name = "SESSDATA", Domain = "https://bilibili.com", Path = "/", Value = "sess-new",
                HttpOnly = true, Secure = true, Expires = DateTimeOffset.FromUnixTimeSeconds(1893456000).UtcDateTime,
            },
            new CookieWrite
            {
                Name = "ac_time_value", Domain = "https://www.bilibili.com", Path = "/", Value = "ac-fresh",
                HttpOnly = true, Secure = true, Expires = DateTimeOffset.FromUnixTimeSeconds(1893456000).UtcDateTime,
            },
        };
        var settings = new MemoryCredentialSettings(0, new Dictionary<string, object>
        {
            { SettingHelper.Account.ACCESS_KEY, "old-ak" },
            { SettingHelper.Account.REFRESH_KEY, "rt-old" },
            { SettingHelper.Account.USER_ID, 42L },
            { SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.MinValue },
        });
        var commitLog = new List<string>();

        var commit = CredentialCommitter.Commit("new-ak", "rt-rotated", expiry, 99182, writes.Count > 0, writes, settings,
            write => false, (message, exception) => commitLog.Add(message));

        Check(commit.TokenSaved, "a rejected cookie write still reports the token tuple saved");
        Check(commit.CookieOffered && !commit.CookieInstalled,
            "a jar that rejects every write is reported as not installed");
        Check(settings.Read(SettingHelper.Account.ACCESS_KEY, "") == "new-ak" &&
            settings.Read(SettingHelper.Account.REFRESH_KEY, "") == "rt-rotated" &&
            settings.Read(SettingHelper.Account.USER_ID, 0L) == 99182L &&
            settings.Read(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.MinValue) == expiry,
            "a rejected cookie write does not roll the saved token tuple back");
        Check(commitLog.Count(line => line.StartsWith("装 cookie 被拒绝", StringComparison.Ordinal)) == writes.Count,
            "every rejected write is logged by name instead of being silently counted");

        var mixed = CredentialCommitter.Commit("new-ak", "rt-rotated", expiry, 99182, writes.Count > 0, writes, settings,
            write => write.Name == "SESSDATA", (message, exception) => { });
        Check(!mixed.CookieInstalled, "one rejected write fails the whole jar installation");

        var accepted = CredentialCommitter.Commit("new-ak", "rt-rotated", expiry, 99182, writes.Count > 0, writes, settings,
            write => true, (message, exception) => { });
        Check(accepted.CookieInstalled, "a jar that accepts every write still reports installed");

        var domainless = CredentialCommitter.Commit("new-ak", "rt-rotated", expiry, 99182, true,
            new List<CookieWrite>(), settings, write => true, (message, exception) => { });
        Check(domainless.CookieOffered && !domainless.CookieInstalled,
            "cookie_info without a domain writes nothing and stays a partial commit");

        var broken = new MemoryCredentialSettings(2, new Dictionary<string, object>
        {
            { SettingHelper.Account.ACCESS_KEY, "old-ak" },
            { SettingHelper.Account.REFRESH_KEY, "rt-old" },
            { SettingHelper.Account.USER_ID, 42L },
            { SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.MinValue },
        });
        var rolled = CredentialCommitter.Commit("new-ak", "rt-rotated", expiry, 99182, writes.Count > 0, writes, broken,
            write => true, (message, exception) => { });
        Check(!rolled.TokenSaved, "a throwing token write reports the tuple unsaved");
        Check(broken.Read(SettingHelper.Account.ACCESS_KEY, "") == "old-ak" &&
            broken.Read(SettingHelper.Account.REFRESH_KEY, "") == "rt-old" &&
            broken.Read(SettingHelper.Account.USER_ID, 0L) == 42L &&
            broken.Read(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.MinValue) == DateTime.MinValue,
            "a throwing token write puts the whole four-value tuple back");

        ResetSession();
        var rejected = CredentialCommitter.Commit("new-ak", "rt-rotated", expiry, 99182, writes.Count > 0, writes,
            new MemoryCredentialSettings(), write => false, (message, exception) => Log.Add(message));
        var result = AccessKeyRenewal.RunAsync("ak", "rt",
            (_, __) => Task.FromResult(new HttpResults { status = true, results = SuccessBody() }),
            (token, cookie) => Task.FromResult(rejected),
            (message, exception) => Log.Add(message)).GetAwaiter().GetResult();

        Check(!result.Committed && result.Outcome == RenewalOutcome.PartlyCommitted,
            "a renewal whose jar rejects every write stays partial and never reports Committed");
        Check(result.CredentialUsable && result.CookieInstalled == false,
            "the app key stays usable while the web session is reported as not restored");
        Check(Log.Any(line => line == "ACCESS_KEY 续期只落了令牌 mid=99182 token=saved cookie=failed"),
            "the renewal logs the exact partial-commit line instead of cookie=installed");
        Check(Log.All(line => !line.StartsWith("ACCESS_KEY 已刷新", StringComparison.Ordinal)),
            "a rejected cookie write never logs a full renewal");
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
            (t, c) => { saved++; return Task.FromResult(Committed(c)); },
            (message, exception) => Log.Add(message)).GetAwaiter().GetResult();

        Check(!result.Committed && result.Outcome == RenewalOutcome.Rejected,
            "a rejected refresh is reported as a rejected session");
        Check(result.SessionDead && !result.Retryable,
            "only an explicit rejection counts as a dead session, not as a retry");
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
        RenewalResult result = null;
        try
        {
            result = AccessKeyRenewal.RunAsync("ak", "rt",
                (_, __) => throw new InvalidOperationException("socket closed"),
                (t, c) => { saved++; return Task.FromResult(Committed(c)); },
                (message, exception) => Log.Add(message)).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            threw = true;
        }

        Check(!threw, "a transport failure does not escape the renewal");
        Check(result.Outcome == RenewalOutcome.TransportFailed && !result.Committed && saved == 0,
            "a transport failure reports a retryable failure and saves nothing");
        Check(result.Retryable && !result.SessionDead,
            "a transport failure keeps the credentials and is not mistaken for a dead session");
        Check(Log.Any(line => line.Contains("请求失败")), "the transport failure is logged");
    }

    private static void EmptyRefreshTokenSendsNothing()
    {
        ResetSession();
        var sends = 0;
        var result = AccessKeyRenewal.RunAsync("ak", "",
            (_, __) => { sends++; return Task.FromResult(new HttpResults { status = true, results = "{}" }); },
            (t, c) => Task.FromResult(Committed(c)), (message, exception) => Log.Add(message)).GetAwaiter().GetResult();
        Check(result.Outcome == RenewalOutcome.NotAttempted && !result.Committed && sends == 0,
            "an empty refresh token performs no request at all");

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
            (token, cookie) => Task.FromResult(Committed(cookie)), (message, exception) => Log.Add(message))
            .GetAwaiter().GetResult();
        Check(result.Committed && result.TokenSaved,
            "a renewal without cookie_info still commits the token");
        Check(!result.CookieOffered && !result.CookieInstalled,
            "no cookie_info is reported as an unreached web session, not as an installed one");
        var line = Log.Single(item => item.StartsWith("ACCESS_KEY 已刷新", StringComparison.Ordinal));
        Check(line == "ACCESS_KEY 已刷新 mid=42 expires_in=3600 token=saved cookie=missing",
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

    /// <summary>
    /// D4：响应里拿到的刷新令牌对它自己的会话是权威的，jar 里那份不一样的 ac_time_value 只是旧世代。
    /// 修复前启动/登录完成后的镜像无条件用 cookie 覆盖设置，这条断言就是那时会红的那条。
    /// </summary>
    private static void ResponseTokenBeatsTheCookieMirror()
    {
        var settings = new Dictionary<string, string>
        {
            { RefreshTokenMirror.TokenKey, "rt-from-response" },
            { RefreshTokenMirror.SourceKey, "response" },
            { RefreshTokenMirror.OwnerKey, "42" },
            { SettingHelper.Account.USER_ID, "42" },
        };
        var jar = new MemoryCookieStore();
        jar.Write("ac_time_value", "ac-stale-cookie");
        jar.Write("DedeUserID", "42");
        jar.Write("SESSDATA", "sess");
        jar.Write("bili_jct", "csrf");
        var service = Mirror(settings, jar);

        // 启动：LoadCookieFromSetting 同步 SESSDATA/CSRF，同时镜像刷新令牌
        service.LoadCookieFromSetting();
        Check(Setting(settings, RefreshTokenMirror.TokenKey) == "rt-from-response",
            "startup keeps the response-derived token when the jar holds a different ac_time_value");
        Check(Setting(settings, RefreshTokenMirror.SourceKey) == "response",
            "startup leaves the recorded source alone");
        Check(Setting(settings, RefreshTokenMirror.OwnerKey) == "42",
            "startup leaves the recorded owner alone");

        // 设置页/刷新前的再次镜像走同一个入口，不许把令牌往回带
        service.LoadCookieFromSetting();
        Check(Setting(settings, RefreshTokenMirror.TokenKey) == "rt-from-response",
            "settings load and the refresh-token fallback see the same token after startup");

        // 单元层同一张判定：响应令牌 + 不同 cookie，令牌与来源都不许动
        var kept = RefreshTokenPolicy.Decide(
            new SavedRefreshToken { Token = "rt-from-response", Source = RefreshTokenSource.Response, OwnerId = 42 },
            42, "ac-stale-cookie", 42, false);
        Check(!kept.Write && kept.Token == "rt-from-response" && kept.Source == RefreshTokenSource.Response &&
            kept.Reason == "response-authoritative",
            "the decision unit names the response token authoritative and never hands out the cookie value");
    }

    /// <summary>D4：cookie 迁移只在没有已存令牌时兜底，且迁移时必须记下来源和归属。</summary>
    private static void CookieMirrorOnlyMigratesALegacySession()
    {
        var settings = new Dictionary<string, string> { { SettingHelper.Account.USER_ID, "42" } };
        var jar = new MemoryCookieStore();
        jar.Write("ac_time_value", "ac-cookie");
        jar.Write("DedeUserID", "42");
        var service = Mirror(settings, jar);

        service.LoadCookieFromSetting();
        Check(Setting(settings, RefreshTokenMirror.TokenKey) == "ac-cookie",
            "a session with no saved token migrates ac_time_value from the jar");
        Check(Setting(settings, RefreshTokenMirror.SourceKey) == "cookie",
            "the migration records that the token came from a cookie");
        Check(Setting(settings, RefreshTokenMirror.OwnerKey) == "42",
            "the migration records which uid the token belongs to");
        Check(RefreshTokenPolicy.Decide(new SavedRefreshToken(), 42, "ac-cookie", 42, false).Reason == "legacy-adoption",
            "the unit calls an empty session's jar import a legacy adoption");

        // 已经存了令牌就不再迁移：迁移是「本会话没有任何刷新令牌」时的兜底，不是更新手段。
        // 选 keep 而不是跟着 jar 换代，是因为 cookie 只是一份副本，跟着它会把手填/迁移过的值
        // 反复按 jar 的世代重写；真正的轮换走响应令牌，那条路已经把自己锁成 response。
        jar.Write("ac_time_value", "ac-cookie-2");
        service.LoadCookieFromSetting();
        Check(Setting(settings, RefreshTokenMirror.TokenKey) == "ac-cookie",
            "a cookie-sourced token is not rewritten by a later jar generation");
        Check(Setting(settings, RefreshTokenMirror.SourceKey) == "cookie",
            "the kept token keeps its recorded source");
        Check(RefreshTokenPolicy.Decide(
                new SavedRefreshToken { Token = "ac-cookie", Source = RefreshTokenSource.Cookie, OwnerId = 42 },
                42, "ac-cookie-2", 42, false).Reason == "cookie-source-kept",
            "the unit states that a cookie-sourced token is kept, not re-adopted");

        // 来源未知的旧设置有值就保留：不拿 cookie 猜它是不是更新
        var unknown = Mirror(new Dictionary<string, string>
        {
            { SettingHelper.Account.USER_ID, "42" },
            { RefreshTokenMirror.TokenKey, "rt-legacy" },
        }, jar);
        unknown.LoadCookieFromSetting();
        Check(Setting(unknown.Settings, RefreshTokenMirror.TokenKey) == "rt-legacy",
            "a saved token of unknown provenance is kept instead of being overwritten by a cookie");
        Check(RefreshTokenPolicy.Decide(
                new SavedRefreshToken { Token = "rt-legacy", Source = RefreshTokenSource.UnknownLegacy, OwnerId = 42 },
                42, "ac-cookie-2", 42, false).Reason == "unknown-source-kept",
            "the unit states that unknown provenance is kept");

        var nothing = RefreshTokenPolicy.Decide(new SavedRefreshToken(), 42, "", 0, false);
        Check(!nothing.Write && nothing.Reason == "no-cookie-token",
            "an empty jar has nothing to migrate");
        Check(RefreshTokenPolicy.ParseSource("cookie") == RefreshTokenSource.Cookie &&
            RefreshTokenPolicy.ParseSource("response") == RefreshTokenSource.Response &&
            RefreshTokenPolicy.ParseSource(null) == RefreshTokenSource.UnknownLegacy,
            "a missing or unknown source reads as unknown-legacy");
        Check(RefreshTokenPolicy.ParseOwner("42") == 42 && RefreshTokenPolicy.ParseOwner("") == 0 &&
            RefreshTokenPolicy.ParseOwner("not-a-uid") == 0,
            "an unreadable uid is treated as unknown ownership, not as account zero");
    }

    /// <summary>D4：归属对不上就不导入 —— 别的账号的 cookie 令牌不许当成本会话的凭证。</summary>
    private static void CookieMirrorRefusesAnotherAccountsToken()
    {
        var settings = new Dictionary<string, string> { { SettingHelper.Account.USER_ID, "7" } };
        var jar = new MemoryCookieStore();
        jar.Write("ac_time_value", "ac-other-account");
        jar.Write("DedeUserID", "42");

        Mirror(settings, jar).LoadCookieFromSetting();
        Check(Setting(settings, RefreshTokenMirror.TokenKey) == "",
            "a jar owned by another uid is refused instead of imported");
        Check(Setting(settings, RefreshTokenMirror.SourceKey) == "",
            "the refused import records no provenance either");
        Check(RefreshTokenPolicy.Decide(new SavedRefreshToken(), 7, "ac-other-account", 42, false).Reason ==
            RefreshTokenPolicy.OwnerMismatch,
            "the unit reports the jar as another account's, not as a migration");

        var foreignSaved = new Dictionary<string, string>
        {
            { SettingHelper.Account.USER_ID, "7" },
            { RefreshTokenMirror.TokenKey, "rt-other-account" },
            { RefreshTokenMirror.SourceKey, "response" },
            { RefreshTokenMirror.OwnerKey, "42" },
        };
        var ownJar = new MemoryCookieStore();
        ownJar.Write("ac_time_value", "ac-seven");
        ownJar.Write("DedeUserID", "7");
        Mirror(foreignSaved, ownJar).LoadCookieFromSetting();
        Check(Setting(foreignSaved, RefreshTokenMirror.TokenKey) == "ac-seven",
            "a saved token owned by another uid is replaced by this session's own cookie token");
        Check(Setting(foreignSaved, RefreshTokenMirror.OwnerKey) == "7",
            "the replacement records this session as the owner instead of carrying the old one");
        Check(RefreshTokenPolicy.Decide(
                new SavedRefreshToken { Token = "rt-other-account", Source = RefreshTokenSource.Response, OwnerId = 42 },
                7, "ac-seven", 7, false).Reason == RefreshTokenPolicy.SessionResetAdopted,
            "the unit calls the switch a session reset, not a migration");

        var clearedSettings = new Dictionary<string, string>
        {
            { SettingHelper.Account.USER_ID, "7" },
            { RefreshTokenMirror.TokenKey, "rt-other-account" },
            { RefreshTokenMirror.SourceKey, "response" },
            { RefreshTokenMirror.OwnerKey, "42" },
        };
        var noTokenJar = new MemoryCookieStore();
        noTokenJar.Write("DedeUserID", "7");
        var clearedService = Mirror(clearedSettings, noTokenJar);
        clearedService.LoadCookieFromSetting();
        Check(Setting(clearedSettings, RefreshTokenMirror.TokenKey) == "",
            "a switch with no jar token leaves nothing of the other account behind");
        Check(clearedService.GetRefreshToken() == "",
            "the switched session hands nothing out to the cookie refresh submit path");
        Check(RefreshTokenPolicy.Decide(
                new SavedRefreshToken { Token = "rt-other-account", Source = RefreshTokenSource.Response, OwnerId = 42 },
                7, "", 0, false).Reason == RefreshTokenPolicy.SessionReset,
            "the unit names the empty outcome a session reset");
    }

    /// <summary>
    /// AUTH-04：一次完成的登录就是新会话。扫码登录把 B 的 uid 和 cookie_info 整组落盘（CredentialCommitter 真代码），
    /// 紧接着的那次镜像（Account.PersistCredentials 的最后一步）只认 B 那一代 ac_time_value；
    /// A 的响应令牌连值带来源带归属都不许留下。同一账号的续期不走重置，旧记录没有归属也不拦。
    /// </summary>
    private static void CrossAccountLoginResetsTheWebToken()
    {
        var settings = AResponseSession("42", "rt-account-a");
        var jar = new MemoryCookieStore();
        CommitLogin(settings, jar, 99, new[]
        {
            new CookieWrite { Name = "DedeUserID", Domain = "https://bilibili.com", Path = "/", Value = "99" },
            new CookieWrite { Name = "ac_time_value", Domain = "https://bilibili.com", Path = "/", Value = "ac-account-b" },
        });

        var service = Mirror(settings, jar);
        service.LoadCookieFromSetting();
        Check(Setting(settings, RefreshTokenMirror.TokenKey) == "ac-account-b",
            "the completed login adopts the new account's ac_time_value");
        Check(Setting(settings, RefreshTokenMirror.SourceKey) == "cookie",
            "the adopted token records that it came from the new jar");
        Check(Setting(settings, RefreshTokenMirror.OwnerKey) == "99",
            "the adopted token records the new account as its owner");
        Check(!settings.Values.Contains("rt-account-a"),
            "the previous account's response token survives nowhere after the switch");
        Check(service.GetRefreshToken() == "ac-account-b",
            "the submit path reads the new account's token, not the previous one's");

        // 新会话没有下发 ac_time_value（登录落盘已先删掉 jar 里上一代副本）：记录整组写成空
        var bare = AResponseSession("42", "rt-account-a");
        var bareJar = new MemoryCookieStore();
        CommitLogin(bare, bareJar, 99, new[]
        {
            new CookieWrite { Name = "DedeUserID", Domain = "https://bilibili.com", Path = "/", Value = "99" },
        });
        var bareService = Mirror(bare, bareJar);
        bareService.LoadCookieFromSetting();
        Check(Setting(bare, RefreshTokenMirror.TokenKey) == "",
            "a switch that brings no new ac_time_value clears the record instead of keeping the old token");
        Check(Setting(bare, RefreshTokenMirror.OwnerKey) == "0" &&
            Setting(bare, RefreshTokenMirror.SourceKey) == "unknown-legacy",
            "the cleared record keeps neither the owner nor the provenance of the old session");
        Check(bareService.GetRefreshToken() == "",
            "the cleared session hands nothing out to the submit path");

        // 退出登录只清掉令牌值、归属键留在设置里：空记录不能被那个孤立的 uid 挡在迁移之外
        var leftover = new Dictionary<string, string>
        {
            { SettingHelper.Account.USER_ID, "99" },
            { RefreshTokenMirror.TokenKey, "" },
            { RefreshTokenMirror.OwnerKey, "42" },
        };
        var leftoverJar = new MemoryCookieStore();
        leftoverJar.Write("ac_time_value", "ac-account-b");
        leftoverJar.Write("DedeUserID", "99");
        var leftoverService = Mirror(leftover, leftoverJar);
        leftoverService.LoadCookieFromSetting();
        Check(Setting(leftover, RefreshTokenMirror.TokenKey) == "ac-account-b" &&
            leftoverService.GetRefreshToken() == "ac-account-b",
            "a cleared token with a leftover owner still adopts the new session's cookie token");

        var same = AResponseSession("42", "rt-account-a");
        var sameJar = new MemoryCookieStore();
        sameJar.Write("ac_time_value", "ac-account-a-old");
        sameJar.Write("DedeUserID", "42");
        CommitLogin(same, sameJar, 42, null);
        var sameService = Mirror(same, sameJar);
        sameService.LoadCookieFromSetting();
        Check(Setting(same, RefreshTokenMirror.TokenKey) == "rt-account-a" &&
            sameService.GetRefreshToken() == "rt-account-a",
            "a same-account login keeps its own response token and still hands it out");

        var legacy = new Dictionary<string, string>
        {
            { SettingHelper.Account.USER_ID, "42" },
            { RefreshTokenMirror.TokenKey, "rt-legacy" },
        };
        var legacyJar = new MemoryCookieStore();
        legacyJar.Write("ac_time_value", "ac-account-a");
        legacyJar.Write("DedeUserID", "42");
        var legacyService = Mirror(legacy, legacyJar);
        legacyService.LoadCookieFromSetting();
        Check(Setting(legacy, RefreshTokenMirror.TokenKey) == "rt-legacy" &&
            legacyService.GetRefreshToken() == "rt-legacy",
            "a record without an owner is not mistaken for another account's");
    }

    /// <summary>
    /// AUTH-04：提交路径取令牌前核对归属。设置里还留着 A 的响应令牌（镜像写入抛过异常，或旧版本换号留下的记录），
    /// 当前会话是 B 时一次 cookie/refresh 都不许发；同一账号的续期照旧用自己的令牌，响应换代按本会话归属落盘。
    /// </summary>
    private static void AnotherAccountsWebTokenIsNeverSubmitted()
    {
        Log.Clear();
        BiliLite.Services.CookieService.ClearReloginRequired();
        var settings = AResponseSession("99", "rt-account-a");
        settings["CookieCSRF"] = "csrf-b";
        var jar = new MemoryCookieStore();
        jar.Write("bili_jct", "csrf-b");

        var requests = new List<ApiModel>();
        var service = Mirror(settings, jar, api => { requests.Add(api); return Task.FromResult(CookieRefreshStub(api)); });
        Check(service.GetRefreshToken() == "",
            "a token whose recorded owner contradicts the session is never handed out; got='" +
            service.GetRefreshToken() + "'");

        service.CookieTimestamp = 1700000000;
        service.RefreshCookieAsync().GetAwaiter().GetResult();

        var foreignSubmissions = requests
            .Where(api => api.baseUrl.EndsWith("/cookie/refresh", StringComparison.Ordinal))
            .Select(api => api.body).ToList();
        Check(foreignSubmissions.Count == 0,
            "no cookie/refresh request is sent for another account's token; sent=" +
            string.Join(" | ", foreignSubmissions));
        Check(requests.All(api => !(api.body ?? "").Contains("rt-account-a")),
            "the previous account's token never reaches a request body");
        Check(Log.Any(line => line.Contains("refresh token missing")),
            "the submit path reports a missing token instead of borrowing one");

        Log.Clear();
        BiliLite.Services.CookieService.ClearReloginRequired();
        var own = AResponseSession("42", "rt-account-a");
        own["CookieCSRF"] = "csrf-a";
        var ownJar = new MemoryCookieStore();
        ownJar.Write("bili_jct", "csrf-a");
        ownJar.Write("ac_time_value", "ac-account-a-old");
        ownJar.Write("DedeUserID", "42");
        var ownRequests = new List<ApiModel>();
        var ownService = Mirror(own, ownJar, api => { ownRequests.Add(api); return Task.FromResult(CookieRefreshStub(api)); });
        ownService.CookieTimestamp = 1700000000;
        ownService.RefreshCookieAsync().GetAwaiter().GetResult();

        var submitted = ownRequests.Single(api => api.baseUrl.EndsWith("/cookie/refresh", StringComparison.Ordinal));
        Check(submitted.body.Contains("refresh_token=rt-account-a"),
            "a same-account renewal submits that account's own token");
        Check(Setting(own, RefreshTokenMirror.TokenKey) == "rt-rotated-from-response" &&
            Setting(own, RefreshTokenMirror.SourceKey) == "response" &&
            Setting(own, RefreshTokenMirror.OwnerKey) == "42",
            "the refresh response replaces the token inside its own session");
    }

    /// <summary>
    /// AUTH-04：设置页手填的 ac_time_value 属于填它的那个会话。来源记 manual、归属记当前 uid，
    /// jar 里的另一代改写不了它，读令牌也不会因为归属空着被挡；清空输入框＝值/来源/归属一起清。
    /// </summary>
    private static void ManualEntryBelongsToTheCurrentSession()
    {
        var settings = new Dictionary<string, string> { { SettingHelper.Account.USER_ID, "42" } };
        var jar = new MemoryCookieStore();
        jar.Write("ac_time_value", "ac-from-jar");
        jar.Write("DedeUserID", "42");
        var service = Mirror(settings, jar);
        var mirror = new RefreshTokenMirror(key => Setting(settings, key), (key, value) => settings[key] = value);

        mirror.RecordManual("ac-typed", RefreshTokenPolicy.ParseOwner(Setting(settings, SettingHelper.Account.USER_ID)));
        Check(Setting(settings, RefreshTokenMirror.SourceKey) == "manual" &&
            Setting(settings, RefreshTokenMirror.OwnerKey) == "42",
            "a hand-entered token records the manual source and the session as its owner");
        Check(service.GetRefreshToken() == "ac-typed",
            "a hand-entered token is handed out for the session that typed it");

        service.LoadCookieFromSetting();
        Check(Setting(settings, RefreshTokenMirror.TokenKey) == "ac-typed",
            "the jar generation never rewrites a hand-entered token");

        mirror.RecordManual("", 42);
        Check(Setting(settings, RefreshTokenMirror.TokenKey) == "" &&
            Setting(settings, RefreshTokenMirror.SourceKey) == "unknown-legacy" &&
            Setting(settings, RefreshTokenMirror.OwnerKey) == "0" &&
            service.GetRefreshToken() == "",
            "clearing the box clears the value, the source and the owner together");
    }

    /// <summary>A 的会话状态：一把响应令牌，归属记在设置里。</summary>
    private static Dictionary<string, string> AResponseSession(string userId, string token) => new Dictionary<string, string>
    {
        { SettingHelper.Account.USER_ID, userId },
        { RefreshTokenMirror.TokenKey, token },
        { RefreshTokenMirror.SourceKey, "response" },
        { RefreshTokenMirror.OwnerKey, "42" },
    };

    /// <summary>登录落盘：和 Account.PersistCredentials 同序 —— 先 Commit（uid 和 cookie 一起写），再由调用方镜像。</summary>
    private static void CommitLogin(IDictionary<string, string> settings, MemoryCookieStore jar, long userId,
        IEnumerable<CookieWrite> cookies)
    {
        var writes = cookies?.ToList() ?? new List<CookieWrite>();
        var commit = CredentialCommitter.Commit("ak-new", "rt-new", DateTime.Now.AddHours(240), userId,
            writes.Count > 0, writes, new DictionaryCredentialSettings(settings),
            write => { jar.Write(write.Name, write.Value); return true; },
            (message, exception) => Log.Add(message));
        if (!commit.TokenSaved) throw new Exception("AUTH-03 failed: the fixture login did not persist its credentials");
    }

    /// <summary>cookie 刷新三条请求的假服务端：correspond 页给出 CSRF，刷新换代，确认成功。</summary>
    private static HttpResults CookieRefreshStub(ApiModel api)
    {
        if (api.baseUrl.Contains("/correspond/"))
            return new HttpResults { code = 200, status = true, results = "<div id=\"1-name\">csrf-from-page</div>" };
        if (api.baseUrl.EndsWith("/cookie/refresh", StringComparison.Ordinal))
            return new HttpResults
            {
                code = 200,
                status = true,
                results = "{\"code\":0,\"data\":{\"refresh_token\":\"rt-rotated-from-response\"}}",
            };
        if (api.baseUrl.EndsWith("/confirm/refresh", StringComparison.Ordinal))
            return new HttpResults { code = 200, status = true, results = "{\"code\":0}" };
        return new HttpResults { code = 200, status = true, results = "{}" };
    }

    /// <summary>D4：有挂起的确认（D3）时，正在轮换的那一代令牌不接受 cookie 改写。</summary>
    private static void PendingConfirmationBlocksMigration()
    {
        var blocked = RefreshTokenPolicy.Decide(new SavedRefreshToken(), 42, "ac-cookie", 42, true);
        Check(!blocked.Write && blocked.Reason == "confirmation-pending",
            "a pending confirmation keeps the jar out of the settings");

        // 判定单元只有参数，不读 SettingHelper 也不读 UWP：同一个判定能被 D3 的确认状态直接调用
        var resumed = RefreshTokenPolicy.Decide(new SavedRefreshToken(), 42, "ac-cookie", 42, false);
        Check(resumed.Write && resumed.Token == "ac-cookie",
            "the same input migrates once no confirmation is pending");
    }

    /// <summary>D5：续期必须用换回这一对令牌的那把 appkey，旧会话没有记录时沿用修复前的 TV 键。</summary>
    private static void RecordedAppkeyDrivesTheRenewalRequest()
    {
        var previousLoginKey = ApiHelper.LoginKey;
        var previousTvKey = ApiHelper.AndroidTVKey;
        try
        {
            ApiHelper.LoginKey = new ApiKeyInfo(LoginAppkey, LoginSecret);
            ApiHelper.AndroidTVKey = new ApiKeyInfo(TvAppkey, TvSecret);
            var requests = new List<ApiModel>();
            Func<string, string, Task<HttpResults>> send = (accessKey, refreshToken) =>
            {
                requests.Add(new AccountApi().RefreshToken(accessKey, refreshToken));
                return Task.FromResult(new HttpResults { status = true, results = SuccessBody() });
            };
            Func<LoginTokenInfo, LoginCookieInfo, Task<CredentialCommit>> save =
                (token, cookie) => Task.FromResult(Committed(cookie));

            SettingHelper.SetValue(AppkeyPolicy.SettingKey, AppkeyPolicy.Format(AppkeyIdentity.Login));
            var loginRenewal = AccessKeyRenewal.RunAsync("ak", "rt", send, save,
                (message, exception) => Log.Add(message)).GetAwaiter().GetResult();
            var loginRequest = requests.Single();
            Check(loginRenewal.Committed, "recording the acquisition key does not disturb the renewal");
            Check(loginRequest.body.Contains("&appkey=" + LoginAppkey + "&"),
                "a pair acquired through the login appkey renews with that same appkey");
            Check(loginRequest.body.EndsWith("&sign=" + ExpectedSign(loginRequest.body, LoginSecret), StringComparison.Ordinal),
                "the renewal is signed with the login secret, not the TV one");
            Check(!loginRequest.body.Contains("&appkey=" + TvAppkey + "&"),
                "the TV key is not substituted for a login-acquired pair");

            requests.Clear();
            SettingHelper.SetValue(AppkeyPolicy.SettingKey, AppkeyPolicy.Format(AppkeyIdentity.Tv));
            AccessKeyRenewal.RunAsync("ak", "rt", send, save, (message, exception) => Log.Add(message))
                .GetAwaiter().GetResult();
            var tvRequest = requests.Single();
            Check(tvRequest.body.Contains("&appkey=" + TvAppkey + "&") &&
                tvRequest.body.EndsWith("&sign=" + ExpectedSign(tvRequest.body, TvSecret), StringComparison.Ordinal),
                "a TV-acquired pair renews with the TV key and secret");

            // 旧会话没有记录：保持修复前的行为（TV 键），不按候选键轮询
            requests.Clear();
            SettingHelper.SetValue(AppkeyPolicy.SettingKey, "");
            AccessKeyRenewal.RunAsync("ak", "rt", send, save, (message, exception) => Log.Add(message))
                .GetAwaiter().GetResult();
            Check(requests.Single().body.Contains("&appkey=" + TvAppkey + "&"),
                "a legacy session with no recorded identity keeps the pre-fix TV key");
            Check(requests.Count == 1, "an unknown identity does not cycle through speculative appkeys");

            Check(AppkeyPolicy.Parse("android") == AppkeyIdentity.UnknownLegacy &&
                AppkeyPolicy.Parse("") == AppkeyIdentity.UnknownLegacy,
                "an unrecognized or absent identity is unknown-legacy, not a guess");
            Check(AppkeyPolicy.Format(AppkeyIdentity.UnknownLegacy) == "unknown-legacy" &&
                AppkeyPolicy.Format(AppkeyIdentity.Login) == "login" &&
                AppkeyPolicy.Format(AppkeyIdentity.Tv) == "tv",
                "the persisted identity is written as login/tv/unknown-legacy");
            Check(AppkeyPolicy.Select(AppkeyIdentity.UnknownLegacy, LoginAppkey, TvAppkey) == TvAppkey,
                "unknown-legacy resolves to the documented TV compatibility policy");
        }
        finally
        {
            ApiHelper.LoginKey = previousLoginKey;
            ApiHelper.AndroidTVKey = previousTvKey;
            SettingHelper.SetValue(AppkeyPolicy.SettingKey, "");
        }
    }

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
        var refresh = MethodBody(account, "public async Task<RenewalResult> RefreshToken()", "public class HomeUserCardModel");
        Check(refresh.Contains("AccessKeyRenewal.RunAsync("), "Account.RefreshToken goes through the renewal service");
        Check(refresh.Contains("SaveRenewedLoginAsync"),
            "the parsed token and cookie_info go to the commit callback");
        Check(!refresh.Contains("SaveLogin("), "the renewal no longer detours through the login-shaped SaveLogin");
        Check(refresh.Contains("Task<RenewalResult> RefreshToken()"),
            "the renewal hands its commit outcome back to the caller");
        Check(!refresh.Contains("throw ex"), "a failed refresh returns a result instead of throwing");

        var committer = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Services/CredentialCommitter.cs"));
        var persist = MethodBody(account, "private static CredentialCommit PersistCredentials(",
            "private static void LogCommitFailure(");
        Check(!persist.Contains("GetProfile") && !persist.Contains("SendLogined"),
            "the credential commit does not contain the profile fetch or the login broadcast");
        Check(persist.Contains("CredentialCommitter.Commit(") && !persist.Contains("SetCookie("),
            "the commit goes through the testable unit instead of calling the jar inline");
        Check(committer.Contains("Restore(settings, previousAccessKey") && committer.Contains("TokenSaved = true"),
            "a failed write puts the whole credential set back and reports it unsaved");
        Check(committer.Contains("if (setCookie(write)) installed++;") &&
            committer.Contains("CookieInstalled = failed == 0 && installed > 0"),
            "a SetCookie return of false counts as a failed write, never as an installed one");
        Check(MethodBody(account, "private static Func<CookieWrite, bool> CookieJarWriter()",
                "private async Task AnnounceLoginAsync()")
            .Contains("CookieManager.SetCookie("),
            "the jar writer hands the real SetCookie result back to the unit");

        var announce = MethodBody(account, "private async Task AnnounceLoginAsync()", "public bool CommitCookieLogin(");
        Check(announce.Contains("await GetProfile();") && announce.Contains("MessageCenter.SendLogined();"),
            "the profile fetch and the login broadcast still happen");
        Check(Regex.Matches(announce, @"catch \(Exception").Count == 2,
            "a profile or broadcast failure cannot turn a committed credential into a failure");

        var saveRenewed = MethodBody(account, "private async Task<CredentialCommit> SaveRenewedLoginAsync(",
            "public class HomeUserCardModel");
        Check(saveRenewed.IndexOf("PersistCredentials(", StringComparison.Ordinal) <
            saveRenewed.IndexOf("AnnounceLoginAsync(", StringComparison.Ordinal) &&
            saveRenewed.Contains("if (!commit.TokenSaved) return commit;"),
            "the commit outcome is decided before the profile fetch runs");

        var renewalSource = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Services/AccessKeyRenewal.cs"));
        Check(renewalSource.Contains("Func<LoginTokenInfo, LoginCookieInfo, Task<CredentialCommit>> saveLogin"),
            "the save callback returns an explicit commit result");
        Check(renewalSource.Contains("if (!commit.TokenSaved)") &&
            renewalSource.Contains("RenewalOutcome.PartlyCommitted"),
            "success is emitted only for the committed outcome, partial commits are separate");

        var home = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Pages/HomePage.xaml.cs"));
        var checkLogin = MethodBody(home, "private async void CheckLoginStatus()", "private async void LoadUserStatus()");
        Check(checkLogin.Contains("renewal.SessionDead") && checkLogin.Contains("renewal.Retryable"),
            "Home separates an expired session from a retryable renewal failure");
        Check(Regex.Matches(checkLogin, @"MessageCenter\.SendLogout\(\)").Count == 1 &&
            checkLogin.IndexOf("renewal.Retryable", StringComparison.Ordinal) >
            checkLogin.IndexOf("renewal.SessionDead", StringComparison.Ordinal),
            "only the dead-session branch logs the user out");
        Check(!checkLogin.Contains("if (!result)"), "Home no longer collapses every renewal failure into a logout");

        var startup = MethodBody(File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/App.xaml.cs")),
            "private static async Task PreflightLoginAsync()",
            "private void RegisterExceptionHandlingSynchronizationContext()");
        Check(startup.Contains("renewal.SessionDead") && startup.Contains("renewal.Retryable"),
            "startup reports the renewal outcome without collapsing it");
        Check(!startup.Contains("SendLogout") && !startup.Contains("AccessKey ="),
            "startup neither logs out nor rewrites credentials on a failed renewal");

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
        Check(builder.Contains("AppkeyPolicy.Select(") &&
            builder.Contains("ApiHelper.LoginKey") && builder.Contains("ApiHelper.AndroidTVKey"),
            "the refresh picks between the two configured key pairs instead of one fixed pair");
        Check(!builder.Contains("ApiHelper.AndroidKey)"),
            "the android key never becomes a renewal candidate");
        Check(Regex.Matches(builder, @"GetSign\(api\.body, key\)").Count == 1 &&
            builder.IndexOf("GetSign(", StringComparison.Ordinal) > builder.IndexOf("refresh_token=", StringComparison.Ordinal),
            "the signature is appended after every signed parameter");
        Check(builder.IndexOf("body = $\"access_key=", StringComparison.Ordinal) >= 0 &&
            builder.IndexOf("body = $\"access_key=", StringComparison.Ordinal) <
            builder.IndexOf("MustParameter(key)", StringComparison.Ordinal),
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

        var cookieService = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Services/CookieService.cs"));
        var mirror = MethodBody(cookieService, "public RefreshTokenDecision MirrorRefreshToken()",
            "public string GetRefreshToken()");
        Check(mirror.Contains("refreshTokenMirror.MirrorFromCookie(") && !mirror.Contains("writeSetting("),
            "the service mirror hands precedence to the decision unit instead of writing the setting itself");
        Check(MethodBody(cookieService, "private async Task RefreshCookieCoreAsync()",
                "private static string GetCorrespondPath(")
            .Contains("refreshTokenMirror.RecordResponse(newToken"),
            "a refresh response records the new token together with its provenance");

        Check(!account.Contains("MirrorCookieRefreshToken"),
            "the duplicate jar mirror implementation is gone");
        Check(MethodBody(account, "private static CredentialCommit PersistCredentials(",
                "private static void LogCommitFailure(")
            .Contains("MirrorRefreshTokenFromJar();"),
            "the credential commit runs the one shared mirror");
        Check(MethodBody(account, "private static CredentialCommit PersistCredentials(",
                "private static void LogCommitFailure(")
            .Contains("DeleteJarRefreshToken();"),
            "a switch drops the previous jar's ac_time_value before the new account's cookies land");
        Check(MethodBody(account, "public async Task<LoginCallbackModel> PollQRAuthInfo(",
                "public async Task<bool> CheckLoginState(")
            .Contains("SaveLogin("),
            "TV QR login reaches that commit through SaveLogin");
        var cookieImport = MethodBody(account, "private static void ApplyCookieLogin(",
            "private static void MirrorRefreshTokenFromJar(");
        Check(cookieImport.Contains("refreshTokenMirror.RecordResponse(") &&
            cookieImport.Contains("MirrorRefreshTokenFromJar();"),
            "the cookie import commit replaces the record and then runs the same mirror");
        Check(MethodBody(cookieService, "public string GetRefreshToken()",
                "public Task<bool> NeedRefreshCookieAsync()")
            .Contains("ReadToken(ReadSessionOwnerId())"),
            "the submit path hands the session owner to the token read");
        var settingPage = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Pages/SettingPage.xaml.cs"));
        var manualBox = MethodBody(settingPage, "public string CookieRefreshToken",
            "OnPropertyChanged(nameof(CookieRefreshToken));");
        Check(manualBox.Contains("refreshTokenMirror.RecordManual(") &&
            manualBox.Contains("SettingHelper.Account.USER_ID") &&
            !manualBox.Contains("SetValue<string>(\"CookieRefreshToken\""),
            "the settings page records a hand-entered token with its source and owner instead of a bare key");
        Check(MethodBody(account, "public async Task<bool> SaveLogin(", "public bool CommitCookieLogin(")
            .Contains("AppkeyPolicy.Format(acquiredWith)"),
            "the acquisition key identity is persisted with the token pair");
        Check(Regex.Matches(account, @"AppkeyIdentity\.Login\)").Count == 2,
            "both password-login entry points in Account record the login key");
        Check(MethodBody(account, "public async Task<LoginCallbackModel> PollQRAuthInfo(",
                "public async Task<bool> CheckLoginState(")
            .Contains("AppkeyIdentity.Tv"),
            "TV QR login records the TV key as its acquisition identity");

        var loginVm = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Modules/User/LoginVM.cs"));
        Check(Regex.Matches(loginVm, @"AppkeyIdentity\.Login\)").Count == 1,
            "LoginVM passes the login identity at its single credential commit");
    }

    /// <summary>判定单元的设置口：内存字典，第 failOnWrite 次写入抛出，用来驱动 D2 的整组回滚。</summary>
    private sealed class MemoryCredentialSettings : ICredentialSettings
    {
        private readonly Dictionary<string, object> values;
        private readonly int failOnWrite;
        private int writes;

        public MemoryCredentialSettings(int failOnWrite = 0, IDictionary<string, object> seed = null)
        {
            this.failOnWrite = failOnWrite;
            values = seed == null ? new Dictionary<string, object>() : new Dictionary<string, object>(seed);
        }

        public T Read<T>(string key, T fallback)
        {
            if (!values.TryGetValue(key, out var value) || value == null) return fallback;
            if (value is T typed) return typed;
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public void Write<T>(string key, T value)
        {
            writes++;
            if (failOnWrite > 0 && writes == failOnWrite) throw new InvalidOperationException("storage full");
            values[key] = value;
        }
    }

    /// <summary>探针里的 jar：字典一个，判定单元和镜像都不认 WinRT。</summary>
    private sealed class MemoryCookieStore : ICookieStore
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);

        public void Write(string name, string value) => values[name] = value;

        public string Read(string name, string defaultValue) =>
            values.TryGetValue(name, out var value) ? value : defaultValue;
    }

    /// <summary>把设置和 jar 都换成字典：CookieService 的入口本身跑真代码，没有静态设置参与。</summary>
    private static MirrorService Mirror(IDictionary<string, string> settings, ICookieStore cookies,
        Func<ApiModel, Task<HttpResults>> send = null) => new MirrorService(settings, cookies, send);

    private sealed class MirrorService : CookieService
    {
        public MirrorService(IDictionary<string, string> settings, ICookieStore cookies,
            Func<ApiModel, Task<HttpResults>> send)
            : base(cookies, key => settings.TryGetValue(key, out var value) ? value : "",
                (key, value) => settings[key] = value, () => true,
                send, _ => { }, (message, exception) => Log.Add(message))
        {
            Settings = settings;
        }

        public IDictionary<string, string> Settings { get; }
    }

    /// <summary>CredentialCommitter 的设置口接到同一本字典上：落盘和镜像看的是同一批键。</summary>
    private sealed class DictionaryCredentialSettings : ICredentialSettings
    {
        private readonly IDictionary<string, string> values;

        public DictionaryCredentialSettings(IDictionary<string, string> values) => this.values = values;

        public T Read<T>(string key, T fallback)
        {
            if (!values.TryGetValue(key, out var raw) || raw == null) return fallback;
            if (typeof(T) == typeof(string)) return (T)(object)raw;
            return (T)Convert.ChangeType(raw, typeof(T));
        }

        public void Write<T>(string key, T value) =>
            values[key] = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string Setting(IDictionary<string, string> settings, string key) =>
        settings.TryGetValue(key, out var value) ? value : "";

    private static RenewalResult Preflight(Func<string, string, Task<HttpResults>> send,
        Func<LoginTokenInfo, LoginCookieInfo, Task<CredentialCommit>> save)
    {
        var refreshToken = SettingHelper.GetValue(SettingHelper.Account.REFRESH_KEY, "");
        var expiresAt = SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.MinValue);
        if (!AccessKeyRenewal.ShouldRenew(BiliLite.Services.CookieService.NeedsRelogin, refreshToken, expiresAt, DateTime.Now,
                (message, exception) => Log.Add(message)))
            return new RenewalResult(RenewalOutcome.NotAttempted, false, false, false);
        return AccessKeyRenewal.RunAsync(SettingHelper.Account.AccessKey, refreshToken, send, save, (message, exception) => Log.Add(message))
            .GetAwaiter().GetResult();
    }

    /// <summary>落盘成功的回调形状：令牌写好，响应带了 cookie_info 就算装进 jar。</summary>
    private static CredentialCommit Committed(LoginCookieInfo cookie)
    {
        var offered = cookie?.cookies != null && cookie.cookies.Count > 0;
        return new CredentialCommit { TokenSaved = true, CookieOffered = offered, CookieInstalled = offered };
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

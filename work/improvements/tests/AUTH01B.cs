using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Models;
using BiliLite.Modules;
using BiliLite.Services;
using Windows.Web.Http;
using HttpFilter = Windows.Web.Http.Filters.HttpBaseProtocolFilter;

internal static class AUTH01B
{
    private static int assertions;

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception("AUTH-01b failed: " + message);
    }

    private static void Throws<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { assertions++; return; }
        throw new Exception("AUTH-01b failed: expected " + typeof(T).Name + ": " + message);
    }

    private static PasswordLoginCookie Cookie(string name, string value, string domain = ".bilibili.com", string path = "/", DateTimeOffset? expires = null) =>
        new PasswordLoginCookie { Name = name, Value = value, Domain = domain, Path = path, Secure = true, HttpOnly = true, Expires = expires };

    private static PasswordLoginSession ValidSession(string access = "access-secret") => new PasswordLoginSession
    {
        AccessKey = access,
        CookieRefreshToken = "refresh-secret",
        UserId = 123,
        Cookies = new[] { Cookie("DedeUserID", "123"), Cookie("SESSDATA", "sess-secret"), Cookie("bili_jct", "csrf-secret") }
    };

    private static void TestExchangeAndCookieValidation()
    {
        Check(PasswordLoginSessionValidator.ParseExchangeRefreshToken("{\"code\":0,\"data\":{\"refresh_token\":\"r\"}}") == "r",
            "successful exchange envelope yields refresh token");
        Throws<FormatException>(() => PasswordLoginSessionValidator.ParseExchangeRefreshToken("not-json"), "malformed exchange JSON rejected");
        Throws<FormatException>(() => PasswordLoginSessionValidator.ParseExchangeRefreshToken("{\"code\":-1,\"data\":{\"refresh_token\":\"r\"}}"), "business failure rejected");
        Throws<FormatException>(() => PasswordLoginSessionValidator.ParseExchangeRefreshToken("{\"code\":0,\"data\":{}}"), "missing refresh token rejected");
        Throws<FormatException>(() => PasswordLoginSessionValidator.ParseCookies(new[] { "DedeUserID=x; Domain=.evil.com; Path=/" }, new Uri("https://passport.bilibili.com/")), "malformed or foreign-scope cookie rejected");
        Throws<FormatException>(() => PasswordLoginSessionValidator.ParseCookies(Array.Empty<string>(), new Uri("https://passport.bilibili.com/")), "missing cookies rejected");
        Throws<ArgumentException>(() => PasswordLoginSessionValidator.ParseCookies(new[] { "x=y" }, new Uri("http://passport.bilibili.com/")), "insecure response origin rejected");

        var container = PasswordLoginSessionValidator.ParseCookies(new[] {
            "DedeUserID=123; Domain=.bilibili.com; Path=/; Secure; HttpOnly",
            "SESSDATA=sess; Domain=.bilibili.com; Path=/; Secure; HttpOnly",
            "bili_jct=csrf; Domain=.bilibili.com; Path=/; Secure"
        }, new Uri("https://passport.bilibili.com/"));
        var session = PasswordLoginSessionValidator.CreateSession("ak", "rt", container, "123");
        Check(session.UserId == 123 && session.Cookies.Count >= 3, "staged cookies produce a validated session");
        Throws<FormatException>(() => PasswordLoginSessionValidator.CreateSession("ak", "rt", container, "124"), "redirect uid must match staged DedeUserID");
        Throws<FormatException>(() => PasswordLoginSessionValidator.ValidateSession(ValidSession(), "not-a-user-id"), "malformed redirect uid rejected");

        var expired = ValidSession();
        expired.Cookies = new[] { Cookie("DedeUserID", "123"), Cookie("SESSDATA", "s", expires: DateTimeOffset.UtcNow.AddMinutes(-1)), Cookie("bili_jct", "c") };
        Throws<FormatException>(() => PasswordLoginSessionValidator.ValidateSession(expired), "expired session cookie rejected");
        var empty = ValidSession();
        empty.Cookies = new[] { Cookie("DedeUserID", "123"), Cookie("SESSDATA", " "), Cookie("bili_jct", "c") };
        Throws<FormatException>(() => PasswordLoginSessionValidator.ValidateSession(empty), "empty required cookie rejected");
        var wrongDomain = ValidSession();
        wrongDomain.Cookies = new[] { Cookie("DedeUserID", "123"), Cookie("SESSDATA", "s", ".evil.com"), Cookie("bili_jct", "c") };
        Throws<FormatException>(() => PasswordLoginSessionValidator.ValidateSession(wrongDomain), "foreign-domain cookie rejected");
        var wrongPath = ValidSession();
        wrongPath.Cookies = new[] { Cookie("DedeUserID", "123"), Cookie("SESSDATA", "s", path: "/login"), Cookie("bili_jct", "c") };
        Throws<FormatException>(() => PasswordLoginSessionValidator.ValidateSession(wrongPath), "narrow-path session cookie rejected");
        var conflict = ValidSession();
        conflict.Cookies = conflict.Cookies.Concat(new[] { Cookie("DedeUserID", "124") }).ToArray();
        Throws<FormatException>(() => PasswordLoginSessionValidator.ValidateSession(conflict), "conflicting user-id cookies rejected");
        var missingCsrf = ValidSession();
        missingCsrf.Cookies = missingCsrf.Cookies.Where(x => x.Name != "bili_jct").ToArray();
        Throws<FormatException>(() => PasswordLoginSessionValidator.ValidateSession(missingCsrf), "missing CSRF cookie rejected");
    }

    private static async Task TestCommitterAndCoordinator()
    {
        var state = "prior-session";
        var logs = new List<string>();
        var commits = 0;
        Check(PasswordLoginSessionCommitter.TryCommit(ValidSession(), () => state,
            value => { commits++; state = value.AccessKey; }, value => state = value, logs.Add), "valid staged session commits");
        Check(commits == 1 && state == "access-secret" && logs.Count == 0, "successful commit applies exactly once without logging credential values");

        state = "prior-session";
        var rollback = PasswordLoginSessionCommitter.TryCommit(ValidSession("secret-that-must-not-be-logged"), () => state,
            value => { state = value.AccessKey; throw new InvalidOperationException("secret-that-must-not-be-logged"); },
            value => state = value, logs.Add);
        Check(!rollback && state == "prior-session", "failed apply restores prior snapshot");
        Check(logs.All(x => !x.Contains("secret-that-must-not-be-logged") && !x.Contains("refresh-secret")), "commit failure logs omit access keys and refresh tokens");
        var invalidCommits = 0;
        Check(!PasswordLoginSessionCommitter.TryCommit(null, () => "snapshot", _ => invalidCommits++, _ => { }, logs.Add) && invalidCommits == 0,
            "invalid session never reaches persistence callback");

        var coordinator = new PasswordLoginSessionCoordinator();
        var oldOperation = coordinator.BeginOperation();
        var currentOperation = coordinator.BeginOperation();
        var currentCommits = 0;
        var oldGate = new TaskCompletionSource<PasswordLoginSession>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stale = coordinator.CompleteAsync(oldOperation, () => oldGate.Task, _ => currentCommits++);
        Check(!await stale && currentCommits == 0, "superseded operation is rejected before staging");

        var gate = new TaskCompletionSource<PasswordLoginSession>(TaskCreationOptions.RunContinuationsAsynchronously);
        var commitCount = 0;
        var first = coordinator.CompleteAsync(currentOperation, () => gate.Task, _ => commitCount++);
        var duplicate = coordinator.CompleteAsync(currentOperation, () => gate.Task, _ => commitCount++);
        gate.SetResult(ValidSession());
        var outcomes = await Task.WhenAll(first, duplicate);
        Check(outcomes.Count(x => x) == 1 && commitCount == 1, "coalesced duplicate completion commits once");

        var cancelled = coordinator.BeginOperation();
        var cancelledGate = new TaskCompletionSource<PasswordLoginSession>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = coordinator.CompleteAsync(cancelled, () => cancelledGate.Task, _ => commitCount++);
        coordinator.Invalidate();
        cancelledGate.SetResult(ValidSession("late-access"));
        Check(!await pending && commitCount == 1, "cancelled operation cannot overwrite session after stage completes");
    }

    private static void SeedAccountState()
    {
        SettingHelper.Reset();
        LogHelper.Messages.Clear();
        SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY, "old-access");
        SettingHelper.SetValue(SettingHelper.Account.USER_ID, 77L);
        SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, new DateTime(2030, 1, 2));
        SettingHelper.SetValue(SettingHelper.Account.REFRESH_KEY, "old-oauth-refresh");
        SettingHelper.SetValue("CookieRefreshToken", "old-cookie-refresh");
        SettingHelper.SetValue("CookieSESSDATA", "old-cookie-session");
        SettingHelper.SetValue("CookieCSRF", "old-cookie-csrf");
        SettingHelper.SetValue<MyProfileModel>(SettingHelper.Account.USER_PROFILE, new MyProfileModel { mid = 77 });
        HttpFilter.Created.Clear();
        HttpFilter.SharedCookieManager.Reset();
        SeedCookie("SESSDATA", "old-sess");
        SeedCookie("bili_jct", "old-csrf");
        SeedCookie("DedeUserID", "77");
        SeedCookie("sid_guard", "old-sid-guard");
        SeedCookie("unrelated", "keep-me");
    }

    private static void SeedCookie(string name, string value)
    {
        HttpFilter.SharedCookieManager.Seed(new HttpCookie(name, ".bilibili.com", "/") { Value = value, Secure = true });
    }

    private static string ReadCookie(string name) => HttpFilter.SharedCookieManager.GetCookies(new Uri("https://passport.bilibili.com/"))
        .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;

    private static void CheckOriginalAccountState(string label)
    {
        Check(SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY, "") == "old-access" &&
            SettingHelper.GetValue(SettingHelper.Account.USER_ID, 0L) == 77L &&
            SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.MinValue) == new DateTime(2030, 1, 2), label + ": OAuth settings restored");
        Check(SettingHelper.GetValue(SettingHelper.Account.REFRESH_KEY, "") == "old-oauth-refresh" &&
            SettingHelper.GetValue("CookieRefreshToken", "") == "old-cookie-refresh" &&
            SettingHelper.GetValue("CookieSESSDATA", "") == "old-cookie-session" &&
            SettingHelper.GetValue("CookieCSRF", "") == "old-cookie-csrf", label + ": cookie settings restored");
        Check(SettingHelper.GetValue<MyProfileModel>(SettingHelper.Account.USER_PROFILE, null)?.mid == 77, label + ": prior profile restored");
        Check(ReadCookie("SESSDATA") == "old-sess" && ReadCookie("bili_jct") == "old-csrf" &&
            ReadCookie("DedeUserID") == "77" && ReadCookie("sid_guard") == "old-sid-guard" && ReadCookie("unrelated") == "keep-me" &&
            ReadCookie("new_aux") == null, label + ": prior cookies restored and new auxiliary cookie removed");
    }

    private static void TestProductionAccountCommitAdapter()
    {
        var successSession = ValidSession();
        successSession.Cookies = successSession.Cookies.Concat(new[] { Cookie("sid_guard", "new-sid-guard"), Cookie("new_aux", "new-auxiliary") }).ToArray();
        SeedAccountState();
        Check(new Account().CommitCookieLogin(successSession), "production Account adapter commits a valid staged session");
        Check(SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY, "") == "access-secret" &&
            SettingHelper.GetValue(SettingHelper.Account.USER_ID, 0L) == 123 &&
            SettingHelper.GetValue(SettingHelper.Account.REFRESH_KEY, "sentinel") == "" &&
            SettingHelper.GetValue("CookieRefreshToken", "") == "refresh-secret" &&
            SettingHelper.GetValue("CookieSESSDATA", "") == "sess-secret" && SettingHelper.GetValue("CookieCSRF", "") == "csrf-secret",
            "production Account adapter writes access and cookie credentials into separate settings");
        Check(SettingHelper.GetValue<MyProfileModel>(SettingHelper.Account.USER_PROFILE, new MyProfileModel { mid = -1 }) == null &&
            ReadCookie("SESSDATA") == "sess-secret" && ReadCookie("new_aux") == "new-auxiliary" && ReadCookie("unrelated") == "keep-me",
            "production Account adapter installs staged cookies and clears the old profile");

        SeedAccountState();
        HttpFilter.SharedCookieManager.RejectSetCall = 3;
        Check(!new Account().CommitCookieLogin(successSession), "production Account adapter reports Windows SetCookie rejection");
        CheckOriginalAccountState("SetCookie rejection");

        SeedAccountState();
        SettingHelper.FailOnceAfterMutationForKey = "CookieSESSDATA";
        Check(!new Account().CommitCookieLogin(successSession), "production Account adapter reports a setting write failure");
        CheckOriginalAccountState("setting write failure");
        Check(LogHelper.Messages.All(x => !x.Contains("access-secret") && !x.Contains("refresh-secret") && !x.Contains("sess-secret")),
            "production Account adapter logs omit staged credentials during rollback");
    }

    private static async Task TestVerificationRequestIsolation()
    {
        HttpFilter.SharedCookieManager.Reset();
        HttpFilter.SharedCookieManager.Seed(new HttpCookie("ambient", ".bilibili.com", "/") { Value = "existing" });
        HttpFilter.Created.Clear();
        var calls = new List<Tuple<HttpMethod, Uri, string>>();
        Windows.Web.Http.HttpClient.Handler = (filter, request, content) =>
        {
            var cookie = "";
            request.Headers.TryGetValue("Cookie", out cookie);
            calls.Add(Tuple.Create(request.Method, request.RequestUri, cookie ?? ""));
            var status = request.RequestUri.AbsolutePath == "/x/safecenter/common/sms/send"
                ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK;
            var response = Response(status, request.RequestUri.AbsolutePath == "/x/safecenter/login/tel/verify"
                ? "{\"code\":-1,\"message\":\"business error\"}" : "{\"code\":0,\"data\":{\"captcha_key\":\"key\"}}");
            response.Headers.Add("Set-Cookie", "injected=from-response; Domain=.bilibili.com; Path=/; Secure");
            return Task.FromResult(response);
        };

        var endpoints = new[]
        {
            new ApiModel { method = RestSharp.Method.Post, baseUrl = "https://passport.bilibili.com/x/safecenter/captcha/pre", body = "captcha=1" },
            new ApiModel { method = RestSharp.Method.Get, baseUrl = "https://api.bilibili.com/x/safecenter/user/info", parameter = "tmp_code=fixture" },
            new ApiModel { method = RestSharp.Method.Post, baseUrl = "https://passport.bilibili.com/x/safecenter/common/sms/send", body = "code=fixture" },
            new ApiModel { method = RestSharp.Method.Post, baseUrl = "https://passport.bilibili.com/x/safecenter/login/tel/verify", body = "code=fixture" }
        };
        var results = new List<BiliLite.Helpers.HttpResults>();
        foreach (var endpoint in endpoints) results.Add(await IsolatedPasswordLoginTransport.SendVerificationAsync(endpoint));
        Check(calls.Count == 4 && calls[0].Item1 == HttpMethod.Post && calls[1].Item1 == HttpMethod.Get &&
            calls[2].Item1 == HttpMethod.Post && calls[3].Item1 == HttpMethod.Post,
            "verification requests use the approved endpoint methods");
        Check(results[0].status && results[0].Method == "POST" && results[1].status && results[1].Method == "GET" &&
            !results[2].status && results[3].results.Contains("business error"),
            "verification transport preserves HTTP status and business response body");
        Check(results.All(x => x.Endpoint.StartsWith("https://", StringComparison.Ordinal)) && calls.All(x => string.IsNullOrEmpty(x.Item3)),
            "verification responses expose only endpoint metadata and requests carry no cookies");
        Check(HttpFilter.Created.Count == 4 && HttpFilter.Created.All(x =>
            x.CookieUsageBehavior == Windows.Web.Http.Filters.HttpCookieUsageBehavior.NoCookies && !x.AllowAutoRedirect),
            "each verification request disables ambient cookies and automatic redirects");
        Check(HttpFilter.SharedCookieManager.GetCookies(new Uri("https://passport.bilibili.com/")).Count == 1 &&
            HttpFilter.SharedCookieManager.GetCookies(new Uri("https://passport.bilibili.com/")).Single().Name == "ambient",
            "verification Set-Cookie responses cannot mutate the shared Windows cookie jar");

        var beforeRejected = calls.Count;
        try
        {
            await IsolatedPasswordLoginTransport.SendVerificationAsync(new ApiModel
            {
                method = RestSharp.Method.Post,
                baseUrl = "https://evil.example/x/safecenter/common/sms/send",
                body = "secret"
            });
            throw new Exception("AUTH-01b failed: invalid verification endpoint was accepted");
        }
        catch (ArgumentException) { assertions++; }
        Check(calls.Count == beforeRejected, "unapproved verification endpoint is rejected before network dispatch");
    }

    private static HttpResponseMessage Response(HttpStatusCode status, string body = "") => new HttpResponseMessage(status, body);

    private static void TestTransportSuccessAndRejections()
    {
        var callbackFetches = 0;
        var sent = new List<Tuple<HttpMethod, Uri, string, string>>();
        ConfigureTransportResponse = (filter, request, content) =>
        {
            var cookie = "";
            request.Headers.TryGetValue("Cookie", out cookie);
            var referer = "";
            request.Headers.TryGetValue("Referer", out referer);
            sent.Add(Tuple.Create(request.Method, request.RequestUri, cookie ?? "", referer ?? ""));
            if (request.Method == HttpMethod.Post)
            {
                Check(content != null && content.Value == "code=verify%20code", "exchange uses encoded form body");
                var exchange = Response(HttpStatusCode.OK, "{\"code\":0,\"data\":{\"refresh_token\":\"rt\"}}");
                exchange.Headers.Add("Set-Cookie", "DedeUserID=123; Domain=.bilibili.com; Path=/; Expires=Wed, 21 Oct 2037 07:28:00 GMT; Secure, SESSDATA=sess; Domain=.bilibili.com; Path=/; Secure, bili_jct=csrf; Domain=.bilibili.com; Path=/; Secure");
                return Task.FromResult(exchange);
            }
            if (request.RequestUri.AbsolutePath == "/login/app/third")
            {
                var third = Response(HttpStatusCode.OK, "{\"code\":0,\"data\":{\"confirm_uri\":\"https://passport.bilibili.com/login/confirm?ticket=t%2B1&source=unit\"}}");
                return Task.FromResult(third);
            }
            if (request.RequestUri.AbsolutePath == "/login/confirm")
            {
                var confirm = Response(HttpStatusCode.Redirect);
                confirm.Headers.Location = new Uri("http://link.acg.tv/forum.php?api=abc&access_key=ak%2Bsecret&mid=123&signature=sig%2F1");
                return Task.FromResult(confirm);
            }
            if (request.RequestUri.Host == "link.acg.tv") callbackFetches++;
            return Task.FromResult(Response(HttpStatusCode.NotFound));
        };
        Windows.Web.Http.HttpClient.Handler = (f, request, content) => ConfigureTransportResponse(f, request, content);
        HttpFilter.Created.Clear();
        HttpFilter.SharedCookieManager.Reset();
        sent.Clear();
        var transport = new IsolatedPasswordLoginTransport(new AccountApi(), new ApiKeyInfo { Appkey = "fixture" });
        var result = transport.ExchangeAsync("verify code").GetAwaiter().GetResult();
        Check(result.AccessKey == "ak+secret" && result.UserId == 123 && result.CookieRefreshToken == "rt", "valid exchange/third/confirm/callback returns a validated session");
        Check(sent.Count == 3 && sent.All(x => x.Item1 != HttpMethod.Get || !x.Item2.Host.Equals("link.acg.tv", StringComparison.OrdinalIgnoreCase)) && callbackFetches == 0,
            "callback URL is parsed locally and never fetched");
        Check(sent[1].Item3.Contains("DedeUserID=123") && sent[2].Item3.Contains("SESSDATA=sess"), "only staged cookies are attached to Bilibili requests");
        Check(sent[2].Item2.Query.Contains("ticket=t%2B1") && sent[2].Item2.Query.Contains("source=unit"), "confirmation request preserves its query and signature inputs");
        Check(sent.Skip(1).All(x => x.Item4 == "https://www.bilibili.com/"), "staged GETs include expected referer");
        Check(HttpFilter.Created.Count == 1 && HttpFilter.Created[0].CookieUsageBehavior == Windows.Web.Http.Filters.HttpCookieUsageBehavior.NoCookies &&
            !HttpFilter.Created[0].AllowAutoRedirect, "transport disables ambient cookies and automatic redirects");
        Check(HttpFilter.Created[0].CookieManager.GetCookies(new Uri("https://passport.bilibili.com/")).Count == 0,
            "default Windows cookie jar remains untouched");

        AssertTransportRejected((filter, request, content) =>
        {
            if (request.Method == HttpMethod.Post) return Task.FromResult(ExchangeResponse("{\"code\":0,\"data\":{\"refresh_token\":\"rt\"}}"));
            if (request.RequestUri.AbsolutePath == "/login/app/third") return Task.FromResult(Response(HttpStatusCode.OK, "{\"code\":0,\"data\":{\"confirm_uri\":\"https://passport.bilibili.com/login/confirm\"}}"));
            var bad = Response(HttpStatusCode.Redirect); bad.Headers.Location = new Uri("https://evil.example/capture?access_key=leak&mid=123"); return Task.FromResult(bad);
        }, "evil redirect rejected before any network callback");

        AssertTransportRejected((filter, request, content) =>
        {
            if (request.Method == HttpMethod.Post) return Task.FromResult(ExchangeResponse("{\"code\":0,\"data\":{\"refresh_token\":\"rt\"}}"));
            if (request.RequestUri.AbsolutePath == "/login/app/third") return Task.FromResult(Response(HttpStatusCode.OK, "{\"code\":0,\"data\":{\"confirm_uri\":\"https://passport.bilibili.com/login/confirm\"}}"));
            var bad = Response(HttpStatusCode.Redirect); bad.Headers.Location = new Uri("http://passport.bilibili.com/login/next?access_key=x&mid=123"); return Task.FromResult(bad);
        }, "insecure Bilibili network redirect rejected");

        AssertTransportRejected((filter, request, content) =>
        {
            if (request.Method == HttpMethod.Post) return Task.FromResult(ExchangeResponse("{\"code\":0,\"data\":{\"refresh_token\":\"rt\"}}"));
            if (request.RequestUri.AbsolutePath == "/login/app/third") return Task.FromResult(Response(HttpStatusCode.OK, "{\"code\":0,\"data\":{\"confirm_uri\":\"https://passport.bilibili.com/login/confirm\"}}"));
            var bad = Response(HttpStatusCode.Redirect); bad.Headers.Location = new Uri("http://link.acg.tv/forum.php?access_key=x&mid=124"); return Task.FromResult(bad);
        }, "mismatched callback user id rejected");

        AssertTransportRejected((filter, request, content) =>
        {
            if (request.Method == HttpMethod.Post) return Task.FromResult(ExchangeResponse("{\"code\":0,\"data\":{\"refresh_token\":\"rt\"}}"));
            if (request.RequestUri.AbsolutePath == "/login/app/third") return Task.FromResult(Response(HttpStatusCode.OK, "{\"code\":0,\"data\":{\"confirm_uri\":\"https://passport.bilibili.com/login/confirm\"}}"));
            return Task.FromResult(Response(HttpStatusCode.Redirect));
        }, "redirect without Location/access key rejected");

        AssertTransportRejected((filter, request, content) => request.Method == HttpMethod.Post
            ? Task.FromResult(ExchangeResponse("{\"code\":0,\"data\":{\"refresh_token\":\"rt\"}}"))
            : Task.FromResult(Response(HttpStatusCode.OK, "{\"code\":-1}")), "business-error conversion response rejected");

        AssertTransportRejected((filter, request, content) => request.Method == HttpMethod.Post
            ? Task.FromResult(ExchangeResponse("{\"code\":-1}"))
            : Task.FromResult(Response(HttpStatusCode.OK)), "business-error exchange prevents later requests");

        AssertTransportRejected((filter, request, content) => request.Method == HttpMethod.Post
            ? Task.FromResult(Response(HttpStatusCode.ServiceUnavailable))
            : Task.FromResult(Response(HttpStatusCode.OK)), "HTTP exchange failure prevents later requests");
    }

    private static Func<HttpFilter, Windows.Web.Http.HttpRequestMessage, HttpStringContent, Task<HttpResponseMessage>> ConfigureTransportResponse;

    private static HttpResponseMessage ExchangeResponse(string body)
    {
        var exchange = Response(HttpStatusCode.OK, body);
        exchange.Headers.Add("Set-Cookie", "DedeUserID=123; Domain=.bilibili.com; Path=/; Expires=Wed, 21 Oct 2037 07:28:00 GMT; Secure, SESSDATA=sess; Domain=.bilibili.com; Path=/; Secure, bili_jct=csrf; Domain=.bilibili.com; Path=/; Secure");
        return exchange;
    }

    private static void AssertTransportRejected(Func<HttpFilter, Windows.Web.Http.HttpRequestMessage, HttpStringContent, Task<HttpResponseMessage>> handler, string message)
    {
        var calls = 0;
        Windows.Web.Http.HttpClient.Handler = (filter, request, content) => { calls++; return handler(filter, request, content); };
        var commits = 0;
        var coordinator = new PasswordLoginSessionCoordinator();
        var operation = coordinator.BeginOperation();
        try
        {
            coordinator.CompleteAsync(operation,
                () => new IsolatedPasswordLoginTransport(new AccountApi(), new ApiKeyInfo { Appkey = "fixture" }).ExchangeAsync("code"),
                _ => commits++).GetAwaiter().GetResult();
        }
        catch (Exception) { assertions++; Check(calls < 5 && commits == 0, message + " and cannot commit a partial session"); return; }
        throw new Exception("AUTH-01b failed: expected rejected transport: " + message);
    }

    private static async Task Main()
    {
        TestExchangeAndCookieValidation();
        await TestCommitterAndCoordinator();
        TestProductionAccountCommitAdapter();
        await TestVerificationRequestIsolation();
        TestTransportSuccessAndRejections();
        await Task.CompletedTask;
        Console.WriteLine("AUTH-01b actual-source service/transport probes passed: " + assertions + " (AccountApi request builders are stubbed here and covered against production source by API-01.)");
    }
}

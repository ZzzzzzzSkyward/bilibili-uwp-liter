using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;
using WinHttpMethod = Windows.Web.Http.HttpMethod;

namespace BiliLite.Services
{
    /// <summary>一次 cookie → ACCESS_KEY 兑换停在哪一步。只有 Converted 才拿得到 key。</summary>
    public enum AppKeyConversionOutcome
    {
        /// <summary>前置条件不成立（本地 cookie 不全、appkey 配置缺失），请求没发。</summary>
        NotAttempted,

        /// <summary>服务端给了 access_key，且回调用户与 cookie 会话用户一致，已落盘。</summary>
        Converted,

        /// <summary>回调用户与 cookie 会话用户对不上：不发布这把 key，本机凭证一个没动。</summary>
        UserMismatch,

        /// <summary>服务端明确拒绝这个 cookie 会话（-101/-111 一类）：兑换这条路到头了。</summary>
        Rejected,

        /// <summary>请求或响应读不出来：可重试，不代表会话已死。</summary>
        TransportFailed,

        /// <summary>服务端给了新 key 但本地没写进去：可重试。</summary>
        Uncommitted,
    }

    /// <summary>兑换结论。AccessKey 只在 Converted 时非空，UserId 是回调里的 uid（0 表示没读到）。</summary>
    public sealed class AppKeyConversion
    {
        public AppKeyConversion(AppKeyConversionOutcome outcome, string accessKey, long userId, string detail)
        {
            Outcome = outcome;
            AccessKey = outcome == AppKeyConversionOutcome.Converted ? (accessKey ?? "") : "";
            UserId = userId;
            Detail = detail ?? "";
        }

        public AppKeyConversionOutcome Outcome { get; }

        /// <summary>只有 Converted 时非空：其余结论都不许拿去写 ACCESS_KEY。</summary>
        public string AccessKey { get; }

        /// <summary>回调确认的用户 id；0 表示响应里没有可读的 mid。</summary>
        public long UserId { get; }

        /// <summary>一句话说明停在哪，日志用，不含令牌内容。</summary>
        public string Detail { get; }

        public bool Converted => Outcome == AppKeyConversionOutcome.Converted;
    }

    /// <summary>ACCESS_KEY 的来源：app/OAuth 令牌响应，还是拿 cookie 兑换来的。</summary>
    public enum AppKeyProvenance
    {
        /// <summary>没有记录：旧版本留下的会话，来历不明，两条恢复路都保留。</summary>
        UnknownLegacy,

        /// <summary>登录/续期响应 JSON 里的 token_info.access_token。</summary>
        AppResponse,

        /// <summary>/login/app/third 及其确认回调换回来的 key：没有配套的 app 刷新令牌。</summary>
        CookieConversion,
    }

    /// <summary>来历的读写格式。取值为空或认不出来就是「不知道」，不猜。</summary>
    public static class AppKeyProvenancePolicy
    {
        public static string Format(AppKeyProvenance provenance)
        {
            switch (provenance)
            {
                case AppKeyProvenance.AppResponse: return "app-response";
                case AppKeyProvenance.CookieConversion: return "cookie-conversion";
                default: return "unknown-legacy";
            }
        }

        public static AppKeyProvenance Parse(string value)
        {
            if (string.Equals(value, "app-response", StringComparison.Ordinal)) return AppKeyProvenance.AppResponse;
            if (string.Equals(value, "cookie-conversion", StringComparison.Ordinal)) return AppKeyProvenance.CookieConversion;
            return AppKeyProvenance.UnknownLegacy;
        }
    }

    /// <summary>ACCESS_KEY 不可用（过期或被拒）时该往哪走。</summary>
    public enum AppKeyRecoveryAction
    {
        /// <summary>key 可用，或刚换好：什么都不做，尤其不要因为 web 会话刷新就重新兑换。</summary>
        Continue,

        /// <summary>用 app/OAuth 刷新令牌续期。</summary>
        RenewOAuth,

        /// <summary>用可用的 web 会话走 cookie 兑换。</summary>
        Convert,

        /// <summary>网络/落盘失败：留着现有凭证，稍后或下次启动再试。</summary>
        Retry,

        /// <summary>能试的路都试完了：会话确实到头，可以退出登录。</summary>
        Logout,
    }

    /// <summary>
    /// 恢复决策的唯一判定处。纯函数：不碰设置、不碰网络，所以探针能把整张表直接跑一遍。
    /// 关键在「哪条路适用」和「哪条路还没试」分开：key 可用就不动它，网络失败不等于会话已死，
    /// 没有 OAuth 令牌只说明那条路不适用，不代表该清掉还能用的 web 会话。
    /// </summary>
    public static class AppKeyRecovery
    {
        /// <summary>
        /// 还没发请求时的判断：cookie 换来的 key 优先走兑换（它本来就没有 app 刷新令牌），
        /// 其余优先走 OAuth 续期；两条都不适用才轮到退出登录。
        /// </summary>
        public static AppKeyRecoveryAction Decide(bool appKeyUsable, AppKeyProvenance provenance,
            bool hasOAuthRefreshToken, bool webSessionUsable)
        {
            if (appKeyUsable) return AppKeyRecoveryAction.Continue;

            // cookie 兑换来的 key 没有配套的 app 刷新令牌：能兑换就先兑换，别拿别的令牌类型顶替
            if (provenance == AppKeyProvenance.CookieConversion && webSessionUsable)
                return AppKeyRecoveryAction.Convert;

            if (hasOAuthRefreshToken) return AppKeyRecoveryAction.RenewOAuth;
            if (webSessionUsable) return AppKeyRecoveryAction.Convert;

            // OAuth 不适用、web 会话也没有：没有可试的路
            return AppKeyRecoveryAction.Logout;
        }

        /// <summary>
        /// OAuth 续期回来以后：换好了就继续；网络/落盘失败是可重试；被拒绝或根本没试时，
        /// 只要 web 会话还能用就先兑换 —— 这正是修复前直接退出的那一步。
        /// </summary>
        public static AppKeyRecoveryAction AfterOAuth(RenewalOutcome outcome, bool webSessionUsable)
        {
            switch (outcome)
            {
                case RenewalOutcome.Committed:
                case RenewalOutcome.PartlyCommitted:
                    return AppKeyRecoveryAction.Continue;
                case RenewalOutcome.TransportFailed:
                case RenewalOutcome.Uncommitted:
                    return AppKeyRecoveryAction.Retry;
                default:
                    return webSessionUsable ? AppKeyRecoveryAction.Convert : AppKeyRecoveryAction.Logout;
            }
        }

        /// <summary>
        /// 兑换回来以后：服务端明确拒绝才算这条路到头；用户对不上/读不出来一律只重试，
        /// 因为这两种情况都没发布过 key，也没有证据表明 web 会话不能用。
        /// </summary>
        public static AppKeyRecoveryAction AfterConversion(AppKeyConversionOutcome outcome)
        {
            switch (outcome)
            {
                case AppKeyConversionOutcome.Converted:
                    return AppKeyRecoveryAction.Continue;
                case AppKeyConversionOutcome.Rejected:
                    return AppKeyRecoveryAction.Logout;
                default:
                    return AppKeyRecoveryAction.Retry;
            }
        }
    }

    /// <summary>停滞的 cookie 会话：兑换只认这一份，不读进程里的 ambient jar。</summary>
    public sealed class AppKeyCookieSession
    {
        public CookieContainer Cookies { get; set; }

        /// <summary>DedeUserID 里的 uid；0 表示没读到。</summary>
        public long UserId { get; set; }

        public bool HasRequiredCookies { get; set; }

        /// <summary>缺了哪几项，日志用。</summary>
        public string Missing { get; set; }
    }

    /// <summary>
    /// cookie → app access key 的唯一实现处：/login/app/third + 确认回调原来是密码登录里的一段，
    /// 提出来以后密码登录和恢复路径共用同一份代码。密码登录不新造验证码、不新造端点；
    /// 恢复路径只在 key 不可用且 web 会话可用时才调用它。
    /// </summary>
    public static class CookieAppKeyBridge
    {
        private static readonly string[] RequiredCookies = { "SESSDATA", "bili_jct", "DedeUserID" };

        /// <summary>估算出来的到期时间标记：这一份不是服务端给的。</summary>
        private const int EstimatedLifetimeHours = 240;

        /// <summary>设置里的来历。ACCESS_KEY 为空时一律按「不知道」对待，别让上一代的记录误导恢复。</summary>
        public static AppKeyProvenance ReadProvenance()
        {
            if (string.IsNullOrEmpty(SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY, "")))
                return AppKeyProvenance.UnknownLegacy;
            return AppKeyProvenancePolicy.Parse(SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY_SOURCE, ""));
        }

        /// <summary>来历的唯一写入口：凡是写 ACCESS_KEY 的地方都得顺手调它。</summary>
        public static void RecordProvenance(AppKeyProvenance provenance) =>
            SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY_SOURCE, AppKeyProvenancePolicy.Format(provenance));

        /// <summary>真实入口：读本地 jar 停滞会话，兑换，校验，落盘。只是恢复路径的一条分支，不循环重试。</summary>
        public static async Task<AppKeyConversion> ConvertAsync(long expectedUserId, Action<string, Exception> log)
        {
            var filter = new HttpBaseProtocolFilter
            {
                CookieUsageBehavior = HttpCookieUsageBehavior.NoCookies,
                AllowAutoRedirect = false
            };
            using (var client = new HttpClient(filter))
            {
                return await ConvertAsync(expectedUserId,
                    () => Task.FromResult(StageFromCookieJar()),
                    session => ExchangeStagedAsync(client, session, ApiHelper.LoginKey, log),
                    PersistConvertedKey,
                    log);
            }
        }

        /// <summary>
        /// 兑换的全流程（停滞 → 请求 → 校验 → 发布），每一步都是委托，探针不联网也能把判定跑一遍。
        /// 一次调用最多一次 exchange：服务端拒绝就作罢，由调用方决定要不要再试。
        /// </summary>
        public static async Task<AppKeyConversion> ConvertAsync(long expectedUserId,
            Func<Task<AppKeyCookieSession>> stage,
            Func<AppKeyCookieSession, Task<AppKeyConversion>> exchange,
            Action<AppKeyCookieSession, AppKeyConversion> publish,
            Action<string, Exception> log)
        {
            if (stage == null || exchange == null) throw new ArgumentNullException(nameof(stage));
            log = log ?? ((message, exception) => { });

            AppKeyCookieSession session;
            try
            {
                session = await stage();
            }
            catch (Exception exception)
            {
                log("Cookie 兑换：读本地停滞会话失败", exception);
                return Failure(AppKeyConversionOutcome.TransportFailed, expectedUserId, "stage-failed");
            }
            if (session == null || !session.HasRequiredCookies)
            {
                log("Cookie 兑换跳过：本地会话缺少 " + (session?.Missing ?? "cookies"), null);
                return Failure(AppKeyConversionOutcome.NotAttempted, expectedUserId,
                    "missing-cookies:" + (session?.Missing ?? ""));
            }

            // 发布前先对一次身份：停滞会话属于别人时，这把 key 一个字节都不该写进本机设置
            if (expectedUserId > 0 && session.UserId > 0 && session.UserId != expectedUserId)
            {
                log("Cookie 兑换拒绝：cookie 会话用户与当前账号不一致", null);
                return Failure(AppKeyConversionOutcome.UserMismatch, session.UserId, "cookie-user-mismatch");
            }

            AppKeyConversion exchangeResult;
            try
            {
                exchangeResult = await exchange(session);
            }
            catch (Exception exception)
            {
                log("Cookie 兑换请求失败", exception);
                return Failure(AppKeyConversionOutcome.TransportFailed, session.UserId, "exchange-threw");
            }
            if (exchangeResult == null)
                return Failure(AppKeyConversionOutcome.NotAttempted, session.UserId, "exchange-empty");
            if (!exchangeResult.Converted)
            {
                log("Cookie 兑换未拿到 access_key outcome=" + exchangeResult.Outcome + " detail=" + exchangeResult.Detail,
                    null);
                return exchangeResult;
            }

            // 回调里的 uid 和 cookie 会话的 uid 对不上：服务端给的 key 不是这个会话的
            if (exchangeResult.UserId > 0 && session.UserId > 0 && exchangeResult.UserId != session.UserId)
            {
                log("Cookie 兑换拒绝：回调用户与 cookie 会话用户不一致", null);
                return Failure(AppKeyConversionOutcome.UserMismatch, exchangeResult.UserId, "callback-user-mismatch");
            }

            var accepted = new AppKeyConversion(AppKeyConversionOutcome.Converted, exchangeResult.AccessKey,
                exchangeResult.UserId > 0 ? exchangeResult.UserId : session.UserId, exchangeResult.Detail);
            try
            {
                publish?.Invoke(session, accepted);
            }
            catch (Exception exception)
            {
                log("Cookie 兑换后落盘失败", exception);
                return Failure(AppKeyConversionOutcome.Uncommitted, accepted.UserId, "publish-failed");
            }

            log("Cookie 兑换成功 mid=" + accepted.UserId, null);
            return accepted;
        }

        /// <summary>
        /// 恢复路径的顺序：先按需刷新 web 会话（RefreshLoginAsync 自己判断要不要真的刷新），
        /// 刷新后会话仍不可用就不兑换。web 会话刷新本身不兑换仍然可用的 key。
        /// </summary>
        public static Task<AppKeyConversion> ConvertWithWebSessionAsync(long expectedUserId,
            Func<Task> refreshWebSession, Func<bool> webSessionDead, Action<string, Exception> log) =>
            ConvertWithWebSessionAsync(expectedUserId, refreshWebSession, webSessionDead, ConvertAsync, log);

        public static async Task<AppKeyConversion> ConvertWithWebSessionAsync(long expectedUserId,
            Func<Task> refreshWebSession,
            Func<bool> webSessionDead,
            Func<long, Action<string, Exception>, Task<AppKeyConversion>> convert,
            Action<string, Exception> log)
        {
            log = log ?? ((message, exception) => { });
            if (refreshWebSession != null)
            {
                try
                {
                    await refreshWebSession();
                }
                catch (Exception exception)
                {
                    log("Cookie 兑换前刷新 web 会话失败", exception);
                    return Failure(AppKeyConversionOutcome.TransportFailed, expectedUserId, "web-refresh-failed");
                }
            }
            if (webSessionDead != null && webSessionDead())
            {
                log("Cookie 兑换跳过：web 会话不可用", null);
                return Failure(AppKeyConversionOutcome.NotAttempted, expectedUserId, "web-session-dead");
            }
            return await convert(expectedUserId, log);
        }

        /// <summary>
        /// app/third + 确认回调。密码登录拿到停滞会话后直接调它；它不碰设置，只回答拿到哪把 key、属于谁。
        /// </summary>
        public static async Task<AppKeyConversion> ExchangeStagedAsync(HttpClient client, AppKeyCookieSession session,
            ApiKeyInfo appKey, Action<string, Exception> log)
        {
            log = log ?? ((message, exception) => { });
            if (client == null || session?.Cookies == null)
                return Failure(AppKeyConversionOutcome.NotAttempted, 0, "no-staged-session");

            AccountApi accountApi;
            ApiModel appThird;
            try
            {
                accountApi = new AccountApi();
                appThird = accountApi.GetCookieToAccessKey(appKey);
            }
            catch (Exception exception)
            {
                log("Cookie 兑换：appkey 配置不可用", exception);
                return Failure(AppKeyConversionOutcome.NotAttempted, 0, "appkey-unavailable");
            }

            try
            {
                var thirdUri = new Uri(appThird.url);
                string confirmUrl;
                using (var thirdResponse = await SendGetAsync(client, thirdUri, session.Cookies))
                {
                    if (!thirdResponse.IsSuccessStatusCode)
                        return Failure(AppKeyConversionOutcome.TransportFailed, 0,
                            "app-third-http-" + (int)thirdResponse.StatusCode);
                    AddResponseCookies(session.Cookies, thirdResponse.Headers, thirdUri);

                    JObject thirdBody;
                    try { thirdBody = JObject.Parse(await thirdResponse.Content.ReadAsStringAsync()); }
                    catch { return Failure(AppKeyConversionOutcome.TransportFailed, 0, "app-third-not-json"); }

                    var code = (int?)thirdBody["code"];
                    if (!code.HasValue)
                        return Failure(AppKeyConversionOutcome.TransportFailed, 0, "app-third-no-code");
                    if (code.Value != 0)
                        // -101/-111 是「这个会话不认」，其余状态码只是这一次没成，别当会话已死
                        return Failure(IsSessionRefusal(code.Value)
                            ? AppKeyConversionOutcome.Rejected
                            : AppKeyConversionOutcome.TransportFailed, 0, "app-third-code-" + code.Value);

                    confirmUrl = (string)thirdBody["data"]?["confirm_uri"];
                    if (string.IsNullOrWhiteSpace(confirmUrl))
                        return Failure(AppKeyConversionOutcome.TransportFailed, 0, "app-third-no-confirm-uri");
                }

                var confirmModel = accountApi.GetCookieToAccessKey(confirmUrl);
                var confirmResult = await FollowConfirmAsync(client, new Uri(confirmModel.baseUrl), session.Cookies);
                long callbackUserId;
                long.TryParse(confirmResult.UserId, NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out callbackUserId);
                return new AppKeyConversion(AppKeyConversionOutcome.Converted, confirmResult.AccessKey,
                    callbackUserId, "app-third");
            }
            catch (Exception exception)
            {
                log("Cookie 兑换请求失败", exception);
                return Failure(AppKeyConversionOutcome.TransportFailed, 0, "transport-" + exception.GetType().Name);
            }
        }

        /// <summary>本地 jar 里的会话停滞成一份 CookieContainer：兑换只用这一份，不带进程里的其它 cookie。</summary>
        public static AppKeyCookieSession StageFromCookieJar()
        {
            var filter = new HttpBaseProtocolFilter();
            var cookies = new CookieContainer();
            foreach (var origin in CookieOrigins.All)
            {
                var uri = new Uri(origin);
                foreach (var cookie in filter.CookieManager.GetCookies(uri))
                {
                    if (string.IsNullOrWhiteSpace(cookie.Name) || string.IsNullOrEmpty(cookie.Value)) continue;
                    try { cookies.SetCookies(uri, cookie.Name + "=" + cookie.Value); }
                    catch (CookieException) { }
                }
            }
            return Inspect(cookies);
        }

        /// <summary>把一份 CookieContainer 读成会话：缺哪项、属于谁，都由它回答。</summary>
        public static AppKeyCookieSession Inspect(CookieContainer cookies)
        {
            var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (cookies != null)
            {
                foreach (var origin in CookieOrigins.All)
                    foreach (Cookie cookie in cookies.GetCookies(new Uri(origin)))
                        if (!string.IsNullOrWhiteSpace(cookie.Name) && !string.IsNullOrEmpty(cookie.Value) &&
                            !found.ContainsKey(cookie.Name))
                            found[cookie.Name] = cookie.Value;
            }

            var missing = RequiredCookies.Where(name => !found.ContainsKey(name)).ToList();
            long userId;
            found.TryGetValue("DedeUserID", out var rawUserId);
            if (!long.TryParse(rawUserId, NumberStyles.Integer, CultureInfo.InvariantCulture, out userId)) userId = 0;
            return new AppKeyCookieSession
            {
                Cookies = cookies,
                UserId = userId > 0 ? userId : 0,
                HasRequiredCookies = missing.Count == 0,
                Missing = string.Join(",", missing),
            };
        }

        /// <summary>
        /// 兑换成功后的落盘：只动 ACCESS_KEY、估算到期时间和来历；
        /// REFRESH_KEY 与 web 刷新令牌一个字都不碰（cookie 兑换没有 app 刷新令牌可写）。
        /// </summary>
        private static void PersistConvertedKey(AppKeyCookieSession session, AppKeyConversion conversion)
        {
            SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY, conversion.AccessKey);
            SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE,
                DateTime.Now.AddHours(EstimatedLifetimeHours));
            SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_ESTIMATED, true);
            RecordProvenance(AppKeyProvenance.CookieConversion);
        }

        private static bool IsSessionRefusal(int code) => code == -101 || code == -111;

        private static AppKeyConversion Failure(AppKeyConversionOutcome outcome, long userId, string detail) =>
            new AppKeyConversion(outcome, "", userId, detail);

        private static async Task<ConfirmResult> FollowConfirmAsync(HttpClient client, Uri uri, CookieContainer cookies)
        {
            for (var redirect = 0; redirect < 8; redirect++)
            {
                ValidateNetworkUri(uri);
                using (var response = await SendGetAsync(client, uri, cookies))
                {
                    AddResponseCookies(cookies, response.Headers, uri);

                    var location = response.Headers.Location;
                    if ((int)response.StatusCode >= 300 && (int)response.StatusCode <= 399 && location != null)
                    {
                        var target = location.IsAbsoluteUri ? location : new Uri(uri, location);
                        ValidateCallbackUri(target);
                        var accessKey = GetQueryValue(target, "access_key");
                        if (!string.IsNullOrWhiteSpace(accessKey))
                            return new ConfirmResult(accessKey, GetQueryValue(target, "mid"));
                        if (!IsBilibiliHost(target.Host))
                            throw new FormatException("Cookie conversion returned an incomplete callback URL.");
                        ValidateNetworkUri(target);
                        uri = target;
                        continue;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var body = (await response.Content.ReadAsStringAsync()).Trim();
                        Uri resultUri;
                        if (Uri.TryCreate(body, UriKind.Absolute, out resultUri))
                        {
                            ValidateCallbackUri(resultUri);
                            var accessKey = GetQueryValue(resultUri, "access_key");
                            if (!string.IsNullOrWhiteSpace(accessKey))
                                return new ConfirmResult(accessKey, GetQueryValue(resultUri, "mid"));
                        }
                    }
                    throw new FormatException("Cookie conversion did not return an access key.");
                }
            }
            throw new FormatException("Cookie conversion exceeded the redirect limit.");
        }

        private static async Task<HttpResponseMessage> SendGetAsync(HttpClient client, Uri uri, CookieContainer cookies)
        {
            using (var request = new HttpRequestMessage(WinHttpMethod.Get, uri))
            {
                if (IsBilibiliHost(uri.Host))
                {
                    var cookieHeader = cookies.GetCookieHeader(uri);
                    if (!string.IsNullOrEmpty(cookieHeader)) request.Headers.Add("Cookie", cookieHeader);
                }
                request.Headers.Add("User-Agent", "Mozilla/5.0 BiliDroid/7.16.0");
                request.Headers.Add("Referer", "https://www.bilibili.com/");
                return await client.SendRequestAsync(request);
            }
        }

        /// <summary>Set-Cookie 头只有一行可用值：WinRT 的 HttpResponseHeaderCollection 不拆多值。</summary>
        public static IEnumerable<string> ReadSetCookieHeaders(HttpResponseHeaderCollection headers)
        {
            string value;
            return headers.TryGetValue("Set-Cookie", out value) && !string.IsNullOrWhiteSpace(value)
                ? new[] { value }
                : Enumerable.Empty<string>();
        }

        /// <summary>把响应下发的 cookie 装进停滞会话：只认 Bilibili 域名，畸形头算响应读不出来。</summary>
        public static void AddResponseCookies(CookieContainer container, HttpResponseHeaderCollection headers,
            Uri responseUri)
        {
            if (!IsBilibiliHost(responseUri.Host)) return;
            var values = ReadSetCookieHeaders(headers).ToList();
            if (values.Count == 0) return;
            try
            {
                foreach (var value in values) container.SetCookies(responseUri, value);
            }
            catch
            {
                throw new FormatException("Cookie conversion returned malformed staged cookies.");
            }
        }

        private static void ValidateNetworkUri(Uri uri)
        {
            if (uri == null || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment) ||
                uri.IsDefaultPort == false || !IsBilibiliHost(uri.Host) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                throw new FormatException("Cookie conversion returned an invalid redirect.");
        }

        private static void ValidateCallbackUri(Uri uri)
        {
            if (uri == null || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment) ||
                uri.IsDefaultPort == false)
                throw new FormatException("Cookie conversion returned a redirect outside the approved hosts.");
            var bilibili = IsBilibiliHost(uri.Host) &&
                string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
            var callback = string.Equals(uri.Host, "link.acg.tv", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
            if (!bilibili && !callback)
                throw new FormatException("Cookie conversion returned a redirect outside the approved hosts.");
        }

        private static bool IsBilibiliHost(string host) =>
            string.Equals(host, "bilibili.com", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".bilibili.com", StringComparison.OrdinalIgnoreCase);

        private static string GetQueryValue(Uri uri, string key)
        {
            foreach (var part in uri.Query.TrimStart('?').Split('&'))
            {
                var separator = part.IndexOf('=');
                if (separator <= 0) continue;
                var name = Uri.UnescapeDataString(part.Substring(0, separator).Replace('+', ' '));
                if (string.Equals(name, key, StringComparison.Ordinal))
                    return Uri.UnescapeDataString(part.Substring(separator + 1).Replace('+', ' '));
            }
            return null;
        }

        private sealed class ConfirmResult
        {
            public ConfirmResult(string accessKey, string userId) { AccessKey = accessKey; UserId = userId; }
            public string AccessKey { get; }
            public string UserId { get; }
        }
    }
}

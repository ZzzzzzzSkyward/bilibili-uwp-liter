using BiliLite.Helpers;
using BiliLite.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace BiliLite.Services
{
    /// <summary>一次续期尝试落在哪一步：只有 Committed 才算凭证真的换好了。</summary>
    public enum RenewalOutcome
    {
        /// <summary>没有刷新令牌，请求都没发。</summary>
        NotAttempted,

        /// <summary>令牌整组写入设置，响应里的 cookie_info（如果有）也装进了 jar。</summary>
        Committed,

        /// <summary>令牌写好了，但响应带了 cookie_info 却没装成：App 的 access_key 还能用，网页会话没换新。</summary>
        PartlyCommitted,

        /// <summary>服务端明确拒绝：这个会话确实不能再用，只能重新登录。</summary>
        Rejected,

        /// <summary>请求或响应读不出来：可重试，不代表会话已死，凭证也一个都没动。</summary>
        TransportFailed,

        /// <summary>服务端给了新凭证但本地没写进去：凭证可能半旧半新，可重试。</summary>
        Uncommitted,
    }

    /// <summary>
    /// 续期结果。令牌落盘和 cookie 安装分开报，因为两者恢复的是不同能力；
    /// 读个人资料不算凭证，失败与否都不改变这个结果。
    /// </summary>
    public sealed class RenewalResult
    {
        public RenewalResult(RenewalOutcome outcome, bool tokenSaved, bool cookieOffered, bool cookieInstalled)
        {
            Outcome = outcome;
            TokenSaved = tokenSaved;
            CookieOffered = cookieOffered;
            CookieInstalled = cookieInstalled;
        }

        public RenewalOutcome Outcome { get; }

        /// <summary>ACCESS_KEY / REFRESH_KEY / USER_ID / 到期时间整组写进设置了。</summary>
        public bool TokenSaved { get; }

        /// <summary>响应当中带了 cookie_info。</summary>
        public bool CookieOffered { get; }

        /// <summary>cookie_info 里的 cookie 全部装进了 jar。</summary>
        public bool CookieInstalled { get; }

        public bool Committed => Outcome == RenewalOutcome.Committed;

        /// <summary>App 侧凭证可用：重新登录解决不了网络问题，别拿它当会话结束。</summary>
        public bool CredentialUsable => TokenSaved;

        /// <summary>只有服务端明确拒绝，才该把用户踢去重新登录。</summary>
        public bool SessionDead => Outcome == RenewalOutcome.Rejected || Outcome == RenewalOutcome.NotAttempted;

        /// <summary>网络/落盘失败：留着现有凭证，下次启动/恢复再试。</summary>
        public bool Retryable => Outcome == RenewalOutcome.TransportFailed || Outcome == RenewalOutcome.Uncommitted;
    }

    /// <summary>保存回调只回答落盘本身：令牌整组写进去了没有、cookie 装进去了没有。</summary>
    public struct CredentialCommit
    {
        public bool TokenSaved { get; set; }

        public bool CookieOffered { get; set; }

        public bool CookieInstalled { get; set; }
    }

    /// <summary>
    /// ACCESS_KEY 的续期入口。cookie 会话死了以后浏览器刷新链拿不到 ac_time_value，
    /// OAuth2 刷新令牌是唯一能同时换 new access_key 和 new cookie_info 的路。
    /// </summary>
    public static class AccessKeyRenewal
    {
        /// <summary>会话已失效，或 ACCESS_KEY 一天内到期，并且手上确实有刷新令牌，才值得发请求。</summary>
        public static bool ShouldRenew(bool needsRelogin, string refreshToken, DateTime expiresAt, DateTime now)
        {
            if (string.IsNullOrEmpty(refreshToken)) return false;
            return needsRelogin || expiresAt <= now.AddDays(1);
        }

        /// <summary>
        /// 启动/恢复时唯一的决策入口，顺便把它记成一行：跳过必须说清为什么，真的续了由 RunAsync 记结果。
        /// 每次启动/恢复只有这一行，不循环、不打印令牌本身。
        /// </summary>
        public static bool ShouldRenew(bool needsRelogin, string refreshToken, DateTime expiresAt, DateTime now,
            Action<string, Exception> log)
        {
            var renew = ShouldRenew(needsRelogin, refreshToken, expiresAt, now);
            if (renew) return true;
            log("ACCESS_KEY 续期跳过：session=" + (needsRelogin ? "无效" : "有效") +
                " key=" + (int)Math.Floor((expiresAt - now).TotalDays) + "d" +
                " refresh=" + (string.IsNullOrEmpty(refreshToken) ? "missing" : "present"), null);
            return false;
        }

        /// <summary>
        /// 一次调用只发一次刷新请求，服务端拒绝就作罢，不重试；失败时一个设置都不碰。
        /// 落盘回调必须回答它自己有没有写成功：这里不再替它假定成功。
        /// </summary>
        public static async Task<RenewalResult> RunAsync(string accessKey, string refreshToken,
            Func<string, string, Task<HttpResults>> send,
            Func<LoginTokenInfo, LoginCookieInfo, Task<CredentialCommit>> saveLogin,
            Action<string, Exception> log)
        {
            if (string.IsNullOrEmpty(refreshToken)) return Failure(RenewalOutcome.NotAttempted);

            HttpResults response;
            try
            {
                response = await send(accessKey, refreshToken);
            }
            catch (Exception exception)
            {
                log("ACCESS_KEY 刷新请求失败", exception);
                return Failure(RenewalOutcome.TransportFailed);
            }
            if (response == null || !response.status)
            {
                log("ACCESS_KEY 刷新失败：" + (response?.message ?? "无响应"), null);
                return Failure(RenewalOutcome.TransportFailed);
            }

            JObject body;
            try
            {
                body = JObject.Parse(response.results ?? "");
            }
            catch (Exception exception)
            {
                log("ACCESS_KEY 刷新响应不是合法 JSON", exception);
                return Failure(RenewalOutcome.TransportFailed);
            }
            var code = (int?)body["code"];
            if (!code.HasValue)
            {
                log("ACCESS_KEY 刷新响应缺少状态码", null);
                return Failure(RenewalOutcome.TransportFailed);
            }
            if (code.Value != 0)
            {
                log("ACCESS_KEY 刷新被拒绝 code=" + code.Value, null);
                return Failure(RenewalOutcome.Rejected);
            }

            // 文档形状是 data.token_info{...} + data.cookie_info{...}，不是把 data 拍平当令牌
            LoginTokenInfo token;
            LoginCookieInfo cookie;
            try
            {
                var data = body["data"];
                if (data == null) throw new FormatException("refresh response has no data");
                token = data["token_info"]?.ToObject<LoginTokenInfo>();
                cookie = data["cookie_info"]?.ToObject<LoginCookieInfo>();
                if (token == null || string.IsNullOrEmpty(token.access_token) ||
                    string.IsNullOrEmpty(token.refresh_token) || token.expires_in <= 0)
                    throw new FormatException("refresh response has no usable token_info");
            }
            catch (Exception exception)
            {
                log("ACCESS_KEY 刷新响应缺少 token_info", exception);
                return Failure(RenewalOutcome.TransportFailed);
            }

            var cookieOffered = cookie?.cookies != null && cookie.cookies.Count > 0;
            CredentialCommit commit = default;
            try
            {
                commit = await saveLogin(token, cookie);
            }
            catch (Exception exception)
            {
                log("ACCESS_KEY 刷新后落盘失败", exception);
                return new RenewalResult(RenewalOutcome.Uncommitted, false, cookieOffered, false);
            }

            var cookieInstalled = commit.CookieInstalled && commit.CookieOffered;
            if (!commit.TokenSaved)
            {
                // 服务端的旧 refresh_token 已经被换掉了，本地还是旧的：只能如实报没落盘，下次再试
                log("ACCESS_KEY 续期未落盘 mid=" + token.mid + " cookie=" + Describe(cookieOffered, cookieInstalled),
                    null);
                return new RenewalResult(RenewalOutcome.Uncommitted, false, cookieOffered, cookieInstalled);
            }
            if (commit.CookieOffered && !cookieInstalled)
            {
                // 令牌换好了，网页会话没换新：两件事分开报，别拿一个「成功」盖住另一个
                log("ACCESS_KEY 续期只落了令牌 mid=" + token.mid + " token=saved cookie=failed", null);
                return new RenewalResult(RenewalOutcome.PartlyCommitted, true, true, false);
            }

            // 成功也要留痕：日志里能区分「续期成功」和「根本没试」，且只报 cookie 有没有到，不报内容
            log("ACCESS_KEY 已刷新 mid=" + token.mid + " expires_in=" + token.expires_in +
                " token=saved cookie=" + (commit.CookieOffered ? "installed" : "missing"), null);
            return new RenewalResult(RenewalOutcome.Committed, true, commit.CookieOffered, cookieInstalled);
        }

        private static RenewalResult Failure(RenewalOutcome outcome) => new RenewalResult(outcome, false, false, false);

        private static string Describe(bool offered, bool installed) =>
            !offered ? "missing" : (installed ? "installed" : "failed");
    }
}

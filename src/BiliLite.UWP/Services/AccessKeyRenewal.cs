using BiliLite.Helpers;
using BiliLite.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace BiliLite.Services
{
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
        /// 一次调用只发一次刷新请求，服务端拒绝就作罢，不重试；失败时一个设置都不碰。
        /// </summary>
        public static async Task<bool> RunAsync(string accessKey, string refreshToken,
            Func<string, string, Task<HttpResults>> send,
            Func<LoginTokenInfo, LoginCookieInfo, Task> saveLogin,
            Action<string, Exception> log)
        {
            if (string.IsNullOrEmpty(refreshToken)) return false;

            HttpResults response;
            try
            {
                response = await send(accessKey, refreshToken);
            }
            catch (Exception exception)
            {
                log("ACCESS_KEY 刷新请求失败", exception);
                return false;
            }
            if (response == null || !response.status)
            {
                log("ACCESS_KEY 刷新失败：" + (response?.message ?? "无响应"), null);
                return false;
            }

            JObject body;
            try
            {
                body = JObject.Parse(response.results ?? "");
            }
            catch (Exception exception)
            {
                log("ACCESS_KEY 刷新响应不是合法 JSON", exception);
                return false;
            }
            var code = (int?)body["code"];
            if (!code.HasValue)
            {
                log("ACCESS_KEY 刷新响应缺少状态码", null);
                return false;
            }
            if (code.Value != 0)
            {
                log("ACCESS_KEY 刷新被拒绝 code=" + code.Value, null);
                return false;
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
                return false;
            }

            try
            {
                await saveLogin(token, cookie);
            }
            catch (Exception exception)
            {
                log("ACCESS_KEY 刷新后落盘失败", exception);
                return false;
            }
            return true;
        }
    }
}

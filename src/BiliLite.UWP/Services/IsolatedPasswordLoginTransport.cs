using BiliLite.Api;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace BiliLite.Services
{
    public sealed class IsolatedPasswordLoginTransport
    {
        private readonly AccountApi accountApi;
        private readonly ApiKeyInfo appKey;

        public IsolatedPasswordLoginTransport(AccountApi accountApi, ApiKeyInfo appKey)
        {
            this.accountApi = accountApi ?? throw new ArgumentNullException(nameof(accountApi));
            this.appKey = appKey ?? throw new ArgumentNullException(nameof(appKey));
        }

        public static async Task<BiliLite.Helpers.HttpResults> SendVerificationAsync(ApiModel api)
        {
            if (api == null) throw new ArgumentNullException(nameof(api));
            Uri uri;
            if (!Uri.TryCreate(api.url, UriKind.Absolute, out uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) || !uri.IsDefaultPort ||
                !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment) ||
                !IsAllowedVerificationEndpoint(uri, api.method))
                throw new ArgumentException("Password login verification request is outside the approved endpoints.", nameof(api));

            var filter = new HttpBaseProtocolFilter
            {
                CookieUsageBehavior = HttpCookieUsageBehavior.NoCookies,
                AllowAutoRedirect = false
            };
            using (var client = new HttpClient(filter))
            {
                var headers = client.DefaultRequestHeaders;
                headers.Add("User-Agent", "Mozilla/5.0 BiliDroid/7.16.0");
                HttpResponseMessage response;
                if (api.method == RestSharp.Method.Get)
                {
                    response = await client.GetAsync(uri);
                }
                else
                {
                    var content = new HttpStringContent(api.body ?? "", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
                    using (content) response = await client.PostAsync(uri, content);
                }
                using (response)
                {
                    var result = new BiliLite.Helpers.HttpResults
                    {
                        code = (int)response.StatusCode,
                        status = response.IsSuccessStatusCode,
                        results = await response.Content.ReadAsStringAsync(),
                        Method = api.method == RestSharp.Method.Get ? "GET" : "POST",
                        Endpoint = uri.GetLeftPart(UriPartial.Path)
                    };
                    if (!result.status) result.message = "Password login verification request failed.";
                    return result;
                }
            }
        }

        private static bool IsAllowedVerificationEndpoint(Uri uri, RestSharp.Method method)
        {
            var path = uri.AbsolutePath;
            if (uri.Host.Equals("passport.bilibili.com", StringComparison.OrdinalIgnoreCase))
            {
                if (path == "/x/safecenter/captcha/pre") return method == RestSharp.Method.Post;
                if (path == "/x/safecenter/common/sms/send") return method == RestSharp.Method.Post;
                if (path == "/x/safecenter/login/tel/verify") return method == RestSharp.Method.Post;
            }
            if (uri.Host.Equals("api.bilibili.com", StringComparison.OrdinalIgnoreCase) &&
                path == "/x/safecenter/user/info") return method == RestSharp.Method.Get;
            return false;
        }

        public async Task<PasswordLoginSession> ExchangeAsync(string verificationCode)
        {
            if (string.IsNullOrWhiteSpace(verificationCode)) throw new ArgumentException("A verification code is required.", nameof(verificationCode));

            var filter = new HttpBaseProtocolFilter
            {
                CookieUsageBehavior = HttpCookieUsageBehavior.NoCookies,
                AllowAutoRedirect = false
            };
            using (var client = new HttpClient(filter))
            {
                var exchange = accountApi.PwdLoginExchangeCookie(verificationCode);
                var exchangeContent = new HttpStringContent(exchange.body, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
                HttpResponseMessage exchangeResponse;
                using (exchangeContent) exchangeResponse = await client.PostAsync(new Uri(exchange.url), exchangeContent);
                string refreshToken;
                CookieContainer cookies;
                using (exchangeResponse)
                {
                    if (!exchangeResponse.IsSuccessStatusCode)
                        throw new InvalidOperationException("Password login cookie exchange failed at the HTTP stage.");
                    refreshToken = PasswordLoginSessionValidator.ParseExchangeRefreshToken(await exchangeResponse.Content.ReadAsStringAsync());
                    cookies = PasswordLoginSessionValidator.ParseCookies(
                        CookieAppKeyBridge.ReadSetCookieHeaders(exchangeResponse.Headers), new Uri(exchange.url));
                }
                RequireCookie(cookies, "DedeUserID");
                RequireCookie(cookies, "SESSDATA");
                RequireCookie(cookies, "bili_jct");

                // app/third + 确认回调是独立的一次兑换：恢复路径与这里共用同一份实现，不另造端点
                var conversion = await CookieAppKeyBridge.ExchangeStagedAsync(client,
                    CookieAppKeyBridge.Inspect(cookies), appKey, null);
                if (!conversion.Converted)
                    throw new FormatException("Password login cookie conversion did not return an access key: " +
                        conversion.Outcome + " " + conversion.Detail);
                return PasswordLoginSessionValidator.CreateSession(conversion.AccessKey, refreshToken, cookies,
                    conversion.UserId > 0
                        ? conversion.UserId.ToString(CultureInfo.InvariantCulture)
                        : null);
            }
        }

        private static void RequireCookie(CookieContainer cookies, string name)
        {
            if (!cookies.GetCookies(new Uri("https://bilibili.com/")).Cast<Cookie>().Any(x => x.Name == name) &&
                !cookies.GetCookies(new Uri("https://passport.bilibili.com/")).Cast<Cookie>().Any(x => x.Name == name))
                throw new FormatException("Password login cookie exchange was missing a required session cookie.");
        }
    }
}

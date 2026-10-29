using BiliLite.Api;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;
using WinHttpMethod = Windows.Web.Http.HttpMethod;

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
                    cookies = PasswordLoginSessionValidator.ParseCookies(ReadSetCookieHeaders(exchangeResponse.Headers), new Uri(exchange.url));
                }
                RequireCookie(cookies, "DedeUserID");
                RequireCookie(cookies, "SESSDATA");
                RequireCookie(cookies, "bili_jct");

                var appThird = accountApi.GetCookieToAccessKey(appKey);
                string confirmUrl;
                using (var thirdResponse = await SendGetAsync(client, new Uri(appThird.url), cookies))
                {
                    if (!thirdResponse.IsSuccessStatusCode)
                        throw new InvalidOperationException("Password login cookie conversion failed at the app-token stage.");
                    AddSetCookies(cookies, thirdResponse.Headers, new Uri(appThird.url));
                    JObject thirdBody;
                    try { thirdBody = JObject.Parse(await thirdResponse.Content.ReadAsStringAsync()); }
                    catch { throw new FormatException("Password login cookie conversion returned invalid JSON."); }
                    if ((int?)thirdBody["code"] != 0)
                        throw new FormatException("Password login cookie conversion returned an unsuccessful response.");
                    confirmUrl = (string)thirdBody["data"]?["confirm_uri"];
                    if (string.IsNullOrWhiteSpace(confirmUrl))
                        throw new FormatException("Password login cookie conversion response was missing its confirmation URL.");
                }

                var confirmModel = accountApi.GetCookieToAccessKey(confirmUrl);
                var confirmResult = await FollowConfirmAsync(client, new Uri(confirmModel.baseUrl), cookies);
                return PasswordLoginSessionValidator.CreateSession(confirmResult.AccessKey, refreshToken, cookies, confirmResult.UserId);
            }
        }

        private static async Task<ConfirmResult> FollowConfirmAsync(HttpClient client, Uri uri, CookieContainer cookies)
        {
            for (var redirect = 0; redirect < 8; redirect++)
            {
                ValidateNetworkUri(uri);
                using (var response = await SendGetAsync(client, uri, cookies))
                {
                    AddSetCookies(cookies, response.Headers, uri);

                    var location = response.Headers.Location;
                    if ((int)response.StatusCode >= 300 && (int)response.StatusCode <= 399 && location != null)
                    {
                        var target = location.IsAbsoluteUri ? location : new Uri(uri, location);
                        ValidateCallbackUri(target);
                        var accessKey = GetQueryValue(target, "access_key");
                        if (!string.IsNullOrWhiteSpace(accessKey)) return new ConfirmResult(accessKey, GetQueryValue(target, "mid"));
                        if (!IsBilibiliHost(target.Host))
                            throw new FormatException("Password login cookie conversion returned an incomplete callback URL.");
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
                            if (!string.IsNullOrWhiteSpace(accessKey)) return new ConfirmResult(accessKey, GetQueryValue(resultUri, "mid"));
                        }
                    }
                    throw new FormatException("Password login cookie conversion did not return an access key.");
                }
            }
            throw new FormatException("Password login cookie conversion exceeded the redirect limit.");
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

        private static IEnumerable<string> ReadSetCookieHeaders(HttpResponseHeaderCollection headers)
        {
            string value;
            return headers.TryGetValue("Set-Cookie", out value) && !string.IsNullOrWhiteSpace(value)
                ? new[] { value }
                : Enumerable.Empty<string>();
        }

        private static void AddSetCookies(CookieContainer container, HttpResponseHeaderCollection headers, Uri responseUri)
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
                throw new FormatException("Password login returned malformed staged cookies.");
            }
        }

        private static void RequireCookie(CookieContainer cookies, string name)
        {
            if (!cookies.GetCookies(new Uri("https://bilibili.com/")).Cast<Cookie>().Any(x => x.Name == name) &&
                !cookies.GetCookies(new Uri("https://passport.bilibili.com/")).Cast<Cookie>().Any(x => x.Name == name))
                throw new FormatException("Password login cookie exchange was missing a required session cookie.");
        }

        private static void ValidateNetworkUri(Uri uri)
        {
            if (uri == null || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment) || uri.IsDefaultPort == false ||
                !IsBilibiliHost(uri.Host) || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                throw new FormatException("Password login cookie conversion returned an invalid redirect.");
        }

        private static void ValidateCallbackUri(Uri uri)
        {
            if (uri == null || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment) || uri.IsDefaultPort == false)
                throw new FormatException("Password login cookie conversion returned a redirect outside the approved hosts.");
            var bilibili = IsBilibiliHost(uri.Host) && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
            var callback = string.Equals(uri.Host, "link.acg.tv", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
            if (!bilibili && !callback) throw new FormatException("Password login cookie conversion returned a redirect outside the approved hosts.");
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

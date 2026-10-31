using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestSharp
{
    public enum Method { Get, Post }
}

namespace BiliLite.Api
{
    public sealed class ApiKeyInfo
    {
        public string Appkey { get; set; }
        public string Secret { get; set; }
    }

    public sealed class ApiModel
    {
        public RestSharp.Method method { get; set; }
        public string baseUrl { get; set; }
        public string parameter { get; set; }
        public string body { get; set; }
        public string url => baseUrl + "?" + parameter;

        public ApiModel With(RestSharp.Method requestMethod, string requestUrl, string requestBody = null)
        {
            method = requestMethod;
            baseUrl = requestUrl;
            body = requestBody;
            return this;
        }
    }

    // AccountApi is an external request-builder boundary here. The production transport and
    // password-login state machine under test are compiled from their actual source files.
    public sealed class AccountApi
    {
        public ApiModel LoginV3(string username, string password, string sessionId, string seccode, string validate, string challenge, string recaptchaToken) =>
            new ApiModel { method = RestSharp.Method.Post, baseUrl = "https://passport.bilibili.com/x/passport-login/web/login", body = "fixture-login" };
        public ApiModel GeetestCaptcha() => new ApiModel { method = RestSharp.Method.Post, baseUrl = "https://passport.bilibili.com/x/safecenter/captcha/pre" };
        public ApiModel FetchHideTel(string token) => new ApiModel { method = RestSharp.Method.Get, baseUrl = "https://api.bilibili.com/x/safecenter/user/info", parameter = "tmp_code=" + Uri.EscapeDataString(token) };
        public ApiModel SendVerifySMS(string token, string recaptcha, string challenge, string gt, string validate, string seccode) =>
            new ApiModel { method = RestSharp.Method.Post, baseUrl = "https://passport.bilibili.com/x/safecenter/common/sms/send", body = "fixture-sms" };
        public ApiModel SubmitPwdLoginSMSCheck(string code, string token, string requestId, string captchaKey) =>
            new ApiModel { method = RestSharp.Method.Post, baseUrl = "https://passport.bilibili.com/x/safecenter/login/tel/verify", body = "fixture-confirm" };
        public ApiModel PwdLoginExchangeCookie(string code) => new ApiModel
        {
            method = RestSharp.Method.Post,
            baseUrl = "https://passport.bilibili.com/x/passport-login/web/exchange_cookie",
            body = "code=" + Uri.EscapeDataString(code)
        };

        public ApiModel GetCookieToAccessKey(ApiKeyInfo key) => new ApiModel
        {
            method = RestSharp.Method.Get,
            baseUrl = "https://passport.bilibili.com/login/app/third?appkey=" + Uri.EscapeDataString(key.Appkey)
        };

        public ApiModel GetCookieToAccessKey(string confirmUrl) => new ApiModel
        {
            method = RestSharp.Method.Get,
            baseUrl = new Uri(confirmUrl).AbsoluteUri
        };
    }
}

namespace BiliLite.Models
{
    public sealed class MyProfileModel { public long mid { get; set; } }
}

namespace BiliLite.Helpers
{
    public sealed class HttpResults
    {
        public int code { get; set; }
        public bool status { get; set; }
        public string results { get; set; }
        public string message { get; set; }
        public string Method { get; set; }
        public string Endpoint { get; set; }
        public object Data { get; set; }
        public Newtonsoft.Json.Linq.JObject GetJObject() => Newtonsoft.Json.Linq.JObject.Parse(results ?? "{}");
        public static int GetDataCalls;
        public Task<T> GetData<T>() { GetDataCalls++; return Task.FromResult((T)Data); }
    }

    public static class AuthRequestTestHooks
    {
        public static Func<BiliLite.Api.ApiModel, Task<HttpResults>> RequestHandler { get; set; } =
            _ => Task.FromResult(new HttpResults { code = 500, status = false, results = "" });
    }

    public static class AuthRequestExtensions
    {
        public static Task<HttpResults> Request(this BiliLite.Api.ApiModel model) => AuthRequestTestHooks.RequestHandler(model);
    }
}

namespace BiliLite.Helpers
{
    public enum LogType { ERROR }
    public static class LogHelper
    {
        public static readonly List<string> Messages = new List<string>();
        public static void Log(string message, LogType type) => Messages.Add(message);
    }

    public static class SettingHelper
    {
        private static readonly Dictionary<string, object> values = new Dictionary<string, object>();
        public static string FailOnceAfterMutationForKey { get; set; }
        public static T GetValue<T>(string key, T fallback)
        {
            if (!values.TryGetValue(key, out var value)) return fallback;
            if (value == null) return default(T);
            if (value is T typed) return typed;
            return (T)Convert.ChangeType(value, typeof(T));
        }
        public static void SetValue<T>(string key, T value)
        {
            values[key] = value;
            if (string.Equals(FailOnceAfterMutationForKey, key, StringComparison.Ordinal))
            {
                FailOnceAfterMutationForKey = null;
                throw new InvalidOperationException("injected setting write failure");
            }
        }
        public static void Reset() { values.Clear(); FailOnceAfterMutationForKey = null; }

        public static class Account
        {
            public const string ACCESS_KEY = "AccessKey";
            public const string USER_ID = "UserId";
            public const string ACCESS_KEY_EXPIRE_DATE = "AccessKeyExpiry";
            public const string REFRESH_KEY = "OAuthRefreshToken";
            public const string USER_PROFILE = "UserProfile";
        }
    }
}

namespace Windows.Storage.Streams
{
    public enum UnicodeEncoding { Utf8 }
}

namespace Windows.Web.Http
{
    public sealed class CookieManager
    {
        private readonly List<HttpCookie> cookies = new List<HttpCookie>();
        public int RejectSetCall { get; set; }
        private int setCalls;
        public IList<HttpCookie> GetCookies(Uri uri) => cookies.ToList();
        public bool SetCookie(HttpCookie cookie)
        {
            setCalls++;
            if (setCalls == RejectSetCall) return false;
            cookies.RemoveAll(x => string.Equals(x.Name, cookie.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Domain, cookie.Domain, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Path, cookie.Path, StringComparison.Ordinal));
            cookies.Add(cookie);
            return true;
        }
        public void DeleteCookie(HttpCookie cookie) => cookies.Remove(cookie);
        public void Seed(HttpCookie cookie) => cookies.Add(cookie);
        public void Reset() { cookies.Clear(); setCalls = 0; RejectSetCall = 0; }
    }

    public sealed class HttpCookie
    {
        public HttpCookie(string name, string domain, string path) { Name = name; Domain = domain; Path = path; }
        public string Name { get; }
        public string Domain { get; }
        public string Path { get; }
        public string Value { get; set; }
        public bool HttpOnly { get; set; }
        public bool Secure { get; set; }
        public DateTime Expires { get; set; }
    }

    public enum HttpMethod { Get, Post }

    public sealed class HttpRequestMessage : IDisposable
    {
        public HttpRequestMessage(HttpMethod method, Uri uri) { Method = method; RequestUri = uri; }
        public HttpMethod Method { get; }
        public Uri RequestUri { get; }
        public HttpRequestHeaderCollection Headers { get; } = new HttpRequestHeaderCollection();
        public void Dispose() { }
    }

    public sealed class HttpRequestHeaderCollection
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public void Add(string name, string value) => values[name] = value;
        public bool TryGetValue(string name, out string value) => values.TryGetValue(name, out value);
    }

    public sealed class HttpStringContent : IDisposable
    {
        public HttpStringContent(string value, Windows.Storage.Streams.UnicodeEncoding encoding, string mediaType) { Value = value; }
        public string Value { get; }
        public void Dispose() { }
    }

    public sealed class HttpContent : IDisposable
    {
        private readonly string value;
        public HttpContent(string value) { this.value = value ?? ""; }
        public Task<string> ReadAsStringAsync() => Task.FromResult(value);
        public void Dispose() { }
    }

    public sealed class HttpResponseMessage : IDisposable
    {
        public static int DisposedCount;
        public HttpResponseMessage(System.Net.HttpStatusCode statusCode, string body = "")
        {
            StatusCode = statusCode;
            Content = new HttpContent(body);
        }
        public System.Net.HttpStatusCode StatusCode { get; }
        public bool IsSuccessStatusCode => (int)StatusCode >= 200 && (int)StatusCode <= 299;
        public Windows.Web.Http.Headers.HttpResponseHeaderCollection Headers { get; } = new Windows.Web.Http.Headers.HttpResponseHeaderCollection();
        public HttpContent Content { get; }
        public void Dispose() { DisposedCount++; }
    }

    public sealed class HttpClient : IDisposable
    {
        public static Func<Windows.Web.Http.Filters.HttpBaseProtocolFilter, HttpRequestMessage, HttpStringContent, Task<HttpResponseMessage>> Handler;
        private readonly Windows.Web.Http.Filters.HttpBaseProtocolFilter filter;
        public HttpClient(Windows.Web.Http.Filters.HttpBaseProtocolFilter filter) { this.filter = filter; }
        public HttpRequestHeaderCollection DefaultRequestHeaders { get; } = new HttpRequestHeaderCollection();
        public Task<HttpResponseMessage> GetAsync(Uri uri) => Handler(filter, new HttpRequestMessage(HttpMethod.Get, uri), null);
        public Task<HttpResponseMessage> PostAsync(Uri uri, HttpStringContent content)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            return Handler(filter, request, content);
        }
        public Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request) => Handler(filter, request, null);
        public void Dispose() { }
    }
}

namespace Windows.Web.Http.Filters
{
    public enum HttpCookieUsageBehavior { Default, NoCookies }

    public sealed class HttpBaseProtocolFilter : IDisposable
    {
        public static readonly List<HttpBaseProtocolFilter> Created = new List<HttpBaseProtocolFilter>();
        public static readonly Windows.Web.Http.CookieManager SharedCookieManager = new Windows.Web.Http.CookieManager();
        public HttpBaseProtocolFilter() { Created.Add(this); }
        public HttpCookieUsageBehavior CookieUsageBehavior { get; set; }
        public bool AllowAutoRedirect { get; set; } = true;
        public Windows.Web.Http.CookieManager CookieManager => SharedCookieManager;
        public void Dispose() { }
    }
}

namespace Windows.Web.Http.Headers
{
    public sealed class HttpResponseHeaderCollection
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Uri Location { get; set; }
        public void Add(string name, string value)
        {
            if (values.TryGetValue(name, out var current)) values[name] = current + ", " + value;
            else values[name] = value;
        }
        public bool TryGetValue(string name, out string value) => values.TryGetValue(name, out value);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestSharp
{
    public enum Method { Get, Post }
}

namespace BiliLite.Helpers
{
    public sealed class HttpResults
    {
        public int code { get; set; }
        public string message { get; set; }
        public bool status { get; set; }
        public string results { get; set; }
    }

    public enum LogType { ERROR, INFO }

    public static class LogHelper
    {
        public static void Log(string message, LogType type, Exception exception = null) { }
    }

    /// <summary>与真 SettingHelper 同形状：常量 + 静态读写口，调用方的写法必须一致。</summary>
    public static class SettingHelper
    {
        private static readonly Dictionary<string, object> values = new Dictionary<string, object>();

        public static class Account
        {
            public const string ACCESS_KEY = "accesskey";
            public const string REFRESH_KEY = "refreshkey";
            public const string ACCESS_KEY_EXPIRE_DATE = "expireDate";
            public const string ACCESS_KEY_EXPIRE_ESTIMATED = "expireDateEstimated";
            public const string ACCESS_KEY_SOURCE = "accesskeySource";
            public const string USER_ID = "uid";
            public const string USER_PROFILE = "userProfile";
            public static bool Logined => !string.IsNullOrEmpty(GetValue(ACCESS_KEY, ""));
            public static string AccessKey => GetValue(ACCESS_KEY, "");
            public static long UserID => GetValue(USER_ID, 0L);
        }

        public static T GetValue<T>(string key, T fallback)
        {
            if (!values.TryGetValue(key, out var value) || value == null) return fallback;
            if (value is T typed) return typed;
            if (typeof(T) == typeof(string)) return (T)(object)Convert.ToString(value);
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public static void SetValue<T>(string key, T value) => values[key] = value;

        public static void Reset() => values.Clear();
    }
}

namespace BiliLite.Api
{
    public class ApiKeyInfo
    {
        public ApiKeyInfo() { }
        public ApiKeyInfo(string key, string secret) { Appkey = key; Secret = secret; }
        public string Appkey { get; set; }
        public string Secret { get; set; }
    }

    public class ApiModel
    {
        public RestSharp.Method method { get; set; }
        public string baseUrl { get; set; }
        public string parameter { get; set; }
        public string body { get; set; }
        public IDictionary<string, string> headers { get; set; }
        public bool need_cookie { get; set; }
        public string url => baseUrl + "?" + parameter;
    }

    public static class ApiHelper
    {
        public static ApiKeyInfo LoginKey = new ApiKeyInfo("login-appkey", "login-secret");
    }

    /// <summary>
    /// 探针只需要两个地址构造器：端点形状本身由 API01 覆盖，这里按真实现的样子拼 URL 即可。
    /// </summary>
    public class AccountApi
    {
        public ApiModel GetCookieToAccessKey(ApiKeyInfo appKey)
        {
            return new ApiModel
            {
                method = RestSharp.Method.Get,
                baseUrl = "https://passport.bilibili.com/login/app/third",
                parameter = "appkey=" + appKey.Appkey + "&api=" + Uri.EscapeDataString("http://link.acg.tv/forum.php"),
                need_cookie = true,
            };
        }

        public ApiModel GetCookieToAccessKey(string confirmUrl)
        {
            Uri uri;
            if (!Uri.TryCreate(confirmUrl, UriKind.Absolute, out uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) || !uri.IsDefaultPort ||
                !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment) ||
                !(uri.Host.Equals("bilibili.com", StringComparison.OrdinalIgnoreCase) ||
                  uri.Host.EndsWith(".bilibili.com", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("The cookie confirmation URL must be an HTTPS Bilibili URL.", nameof(confirmUrl));
            return new ApiModel { method = RestSharp.Method.Get, baseUrl = uri.AbsoluteUri, need_cookie = true };
        }
    }
}

namespace Windows.Storage.Streams
{
    public enum UnicodeEncoding { Utf8 }
}

namespace Windows.Web.Http
{
    /// <summary>进程里只有一份 jar：兑换读的就是它，探针按需塞 cookie 进去。</summary>
    public sealed class CookieManager
    {
        private readonly List<HttpCookie> cookies = new List<HttpCookie>();

        public IList<HttpCookie> GetCookies(Uri uri)
        {
            var host = uri?.Host ?? "";
            return cookies.Where(cookie => AppliesTo(cookie.Domain, host)).ToList();
        }

        public bool SetCookie(HttpCookie cookie)
        {
            cookies.RemoveAll(existing => existing.Name == cookie.Name &&
                string.Equals(existing.Domain, cookie.Domain, StringComparison.OrdinalIgnoreCase) &&
                existing.Path == cookie.Path);
            cookies.Add(cookie);
            return true;
        }

        public void DeleteCookie(HttpCookie cookie) => cookies.Remove(cookie);
        public void Reset() => cookies.Clear();

        private static bool AppliesTo(string domain, string host) =>
            string.IsNullOrEmpty(domain) ||
            string.Equals(host, domain, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);
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
        public HttpResponseMessage(System.Net.HttpStatusCode statusCode, string body = "")
        {
            StatusCode = statusCode;
            Content = new HttpContent(body);
        }

        public System.Net.HttpStatusCode StatusCode { get; }
        public bool IsSuccessStatusCode => (int)StatusCode >= 200 && (int)StatusCode <= 299;
        public Windows.Web.Http.Headers.HttpResponseHeaderCollection Headers { get; } = new Windows.Web.Http.Headers.HttpResponseHeaderCollection();
        public HttpContent Content { get; }
        public void Dispose() { }
    }

    public sealed class HttpClient : IDisposable
    {
        /// <summary>探针装进来的假网络：按请求路由，返回固定响应。</summary>
        public static Func<HttpRequestMessage, Task<HttpResponseMessage>> Handler =
            _ => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));

        private readonly Windows.Web.Http.Filters.HttpBaseProtocolFilter filter;

        public HttpClient(Windows.Web.Http.Filters.HttpBaseProtocolFilter filter) { this.filter = filter; }

        public HttpRequestHeaderCollection DefaultRequestHeaders { get; } = new HttpRequestHeaderCollection();

        public Task<HttpResponseMessage> GetAsync(Uri uri) => Handler(new HttpRequestMessage(HttpMethod.Get, uri));

        public Task<HttpResponseMessage> PostAsync(Uri uri, HttpStringContent content) =>
            Handler(new HttpRequestMessage(HttpMethod.Post, uri));

        public Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request) => Handler(request);

        public void Dispose() { }
    }
}

namespace Windows.Web.Http.Filters
{
    public enum HttpCookieUsageBehavior { Default, NoCookies }

    public sealed class HttpBaseProtocolFilter
    {
        public static readonly Windows.Web.Http.CookieManager Jar = new Windows.Web.Http.CookieManager();

        public HttpCookieUsageBehavior CookieUsageBehavior { get; set; }
        public bool AllowAutoRedirect { get; set; } = true;
        public Windows.Web.Http.CookieManager CookieManager => Jar;
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BiliLite.Api;

namespace RestSharp
{
    public enum Method { Get, Post }
}

namespace BiliLite.Helpers
{
    public sealed class HttpResults
    {
        public int code { get; set; }
        public bool status { get; set; }
        public string results { get; set; }
    }

    public enum LogType { ERROR, INFO }
    public static class LogHelper
    {
        public static void Log(string message, LogType type, Exception exception = null) { }
    }

    public static class Utils
    {
        public static void ShowMessageToast(string message) { }
        public static string ToMD5(string value)
        {
            using (var md5 = MD5.Create())
                return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
        }
        public static long GetTimestampS() => 123;
        public static Task<HttpResults> Request(this ApiModel api) => Auth2TestHooks.RequestHandler(api);
    }
}

namespace BiliLite.Helpers
{
    public static class SettingHelper
    {
        private static readonly Dictionary<string, string> values = new Dictionary<string, string>();
        public static AccountSettings Account { get; } = new AccountSettings();
        public static T GetValue<T>(string key, T fallback)
        {
            if (!values.TryGetValue(key, out var value)) return fallback;
            if (typeof(T) == typeof(string)) return (T)(object)value;
            return (T)Convert.ChangeType(value, typeof(T));
        }
        public static void SetValue<T>(string key, T value) => values[key] = Convert.ToString(value);
        public static void Reset() => values.Clear();
    }

    public sealed class AccountSettings
    {
        public bool Logined { get; set; }
        public string AccessKey { get; set; } = "access-fixture";
        public string REFRESH_KEY => "refreshToken";
    }
}

namespace Windows.Web.Http
{
    public sealed class HttpCookie
    {
        public HttpCookie(string name, string domain, string path) { Name = name; Domain = domain; Path = path; }
        public string Name { get; }
        public string Domain { get; }
        public string Path { get; }
        public string Value { get; set; }
    }

    /// <summary>按域匹配的 jar：host 只能看到自己发出的 cookie 和父域的 cookie，这是原 bug 的复现前提。</summary>
    public sealed class CookieManager
    {
        private readonly List<HttpCookie> cookies = new List<HttpCookie>();

        public IReadOnlyList<HttpCookie> All => cookies.ToList();

        public IList<HttpCookie> GetCookies(Uri uri)
        {
            var host = uri?.Host ?? "";
            return cookies.Where(cookie => AppliesTo(cookie.Domain, host)).ToList();
        }

        public void SetCookie(HttpCookie cookie)
        {
            cookies.RemoveAll(existing => existing.Name == cookie.Name &&
                string.Equals(existing.Domain, cookie.Domain, StringComparison.OrdinalIgnoreCase) &&
                existing.Path == cookie.Path);
            cookies.Add(cookie);
        }

        private static bool AppliesTo(string domain, string host) =>
            string.IsNullOrEmpty(domain) ||
            string.Equals(host, domain, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);
    }
}

namespace Windows.Web.Http.Filters
{
    public sealed class HttpBaseProtocolFilter
    {
        public static readonly IList<HttpBaseProtocolFilter> Created = new List<HttpBaseProtocolFilter>();

        public HttpBaseProtocolFilter() { Created.Add(this); }

        public Windows.Web.Http.CookieManager CookieManager { get; } = new Windows.Web.Http.CookieManager();
    }
}

namespace BiliLite.Helpers
{
    public static class Auth2TestHooks
    {
        public static Func<ApiModel, Task<HttpResults>> RequestHandler { get; set; }
            = _ => Task.FromResult(new HttpResults { code = 500, status = false, results = "" });
    }
}

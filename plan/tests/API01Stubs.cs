using System;
using System.Collections.Generic;
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
        public static Task<HttpResults> Request(this ApiModel api) => Api01TestHooks.RequestHandler(api);
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

    public sealed class CookieManager
    {
        private readonly Dictionary<string, HttpCookie> cookies = new Dictionary<string, HttpCookie>();
        public IList<HttpCookie> GetCookies(Uri uri) => new List<HttpCookie>(cookies.Values);
        public void SetCookie(HttpCookie cookie) => cookies[cookie.Name] = cookie;
    }
}

namespace Windows.Web.Http.Filters
{
    public sealed class HttpBaseProtocolFilter
    {
        public Windows.Web.Http.CookieManager CookieManager { get; } = new Windows.Web.Http.CookieManager();
    }
}

namespace BiliLite.Helpers
{
    public static class Api01TestHooks
    {
        public static Func<ApiModel, Task<HttpResults>> RequestHandler { get; set; }
            = _ => Task.FromResult(new HttpResults { code = 500, status = false, results = "" });
    }
}

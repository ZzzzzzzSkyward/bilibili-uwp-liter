using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BiliLite.Api;
using BiliLite.Models;

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

    public static class Utils
    {
        public static void ShowMessageToast(string message) { }
        public static string ToMD5(string value)
        {
            using (var md5 = MD5.Create())
                return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
        }
        public static long GetTimestampS() => 1700000000;
        public static string ChooseProxyServer(string area) => "https://proxy.example/" + area + "/";
        public static Task<HttpResults> Request(this ApiModel api) => Auth3TestHooks.RequestHandler(api);
    }

    /// <summary>真 MessageCenter 的登录事件就是 App 清掉「已失效」结论的入口，这里只保留这条线。</summary>
    public static class MessageCenter
    {
        public static event EventHandler<object> LoginedEvent;
        public static void SendLogined() => LoginedEvent?.Invoke(null, null);
    }
}

namespace BiliLite.Helpers
{
    public static class SettingHelper
    {
        private static readonly Dictionary<string, object> values = new Dictionary<string, object>();

        /// <summary>与真 SettingHelper.Account 同形状：常量 + 静态读写口，调用方的写法必须一致。</summary>
        public static class Account
        {
            public const string ACCESS_KEY = "accesskey";
            public const string REFRESH_KEY = "refreshkey";
            public const string ACCESS_KEY_EXPIRE_DATE = "expireDate";
            public const string USER_ID = "uid";
            public static bool Logined { get; set; }
            public static string AccessKey { get; set; } = "";
            public static MyProfileModel Profile { get; set; }
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

namespace Windows.Storage
{
    /// <summary>播放地址模型里只有一个字段用到它，探针不需要真的文件。</summary>
    public class StorageFile { }
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
        public bool HttpOnly { get; set; }
        public bool Secure { get; set; }
        public DateTimeOffset Expires { get; set; }
    }

    /// <summary>按域匹配的 jar：host 只能看见自己发出的 cookie 和父域的 cookie。</summary>
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
    public static class Auth3TestHooks
    {
        public static Func<ApiModel, Task<HttpResults>> RequestHandler { get; set; }
            = _ => Task.FromResult(new HttpResults { code = 500, status = false, results = "" });
    }
}

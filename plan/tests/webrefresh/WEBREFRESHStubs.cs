using System;
using System.Collections.Generic;
using System.IO;
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
        public static Task<HttpResults> Request(this ApiModel api) => WebRefreshTestHooks.RequestHandler(api);
    }

    /// <summary>真 MessageCenter 的登录事件是 App 清掉「已失效」结论的入口，这里只保留这条线。</summary>
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

namespace BiliLite.Models
{
    public sealed class MyProfileModel { }
}

namespace Windows.Storage
{
    public class StorageFile { }

    /// <summary>
    /// 挂起确认记录的 LocalFolder：探针按用例换目录，所以这里是一个可改的路径。
    /// 默认值按「一次运行」命名而不是按 PID —— PID 会被复用，上一次留下的记录会被当成自己的在途确认；
    /// 正常退出或未处理异常终止时把目录删掉。
    /// </summary>
    public sealed class ApplicationDataLocalFolder
    {
        public string Path { get; set; }
    }

    public sealed class ApplicationData
    {
        private static readonly ApplicationData instance = new ApplicationData();

        public static ApplicationData Current => instance;

        private ApplicationData()
        {
            var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "bili-probe-webrefresh-" + System.Guid.NewGuid().ToString("N"));
            LocalFolder = new ApplicationDataLocalFolder { Path = folder };
            // 正常退出和探针失败（未处理异常）两条路都要删：.NET 只在正常退出时才跑 ProcessExit
            System.AppDomain.CurrentDomain.ProcessExit += (sender, args) => Delete(folder);
            System.AppDomain.CurrentDomain.UnhandledException += (sender, args) => Delete(folder);
        }

        public ApplicationDataLocalFolder LocalFolder { get; }

        private static void Delete(string folder)
        {
            try
            {
                if (System.IO.Directory.Exists(folder)) System.IO.Directory.Delete(folder, true);
            }
            catch (System.Exception)
            {
                // 删不掉不影响结论：目录名带 GUID，下一次运行不会再撞上这一份
            }
        }
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
        public HttpBaseProtocolFilter() { }

        public Windows.Web.Http.CookieManager CookieManager { get; } = new Windows.Web.Http.CookieManager();
    }
}

namespace BiliLite.Helpers
{
    public static class WebRefreshTestHooks
    {
        public static Func<ApiModel, Task<HttpResults>> RequestHandler { get; set; }
            = _ => Task.FromResult(new HttpResults { code = 500, status = false, results = "" });
    }
}

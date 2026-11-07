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
        /// <summary>真 SettingHelper.Account.USER_ID 的键名：会话归属就是从这条设置读的。</summary>
        public string USER_ID => "uid";
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

namespace Windows.Storage
{
    /// <summary>
    /// 挂起确认记录落在 LocalFolder 下的一个文件里。目录按「一次运行」命名，不按 PID：
    /// PID 会被系统复用，上一次运行留下的 cookie-refresh-pending.json 会被下一个进程当成自己的在途确认。
    /// 正常退出或未处理异常终止时整目录删掉，临时目录不会被探针填满。
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
                "bili-probe-" + System.Guid.NewGuid().ToString("N"));
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

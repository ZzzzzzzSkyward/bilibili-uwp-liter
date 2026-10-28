using BiliLite.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BiliLite.Services;

public interface ICookieService
{
    void WriteCookie(string name, string value);
    string ReadCookie(string name, string defaultValue);
}

public class CookieService : BiliLite.Services.CookieService, ICookieService
{
    public CookieService() : base() { }
    public CookieService(BiliLite.Services.ICookieStore store, Func<string, string> readSetting,
        Action<string, string> writeSetting, Func<bool> isLoggedIn,
        Func<BiliLite.Api.ApiModel, Task<BiliLite.Helpers.HttpResults>> send = null,
        Action<string> notify = null, Action<string, Exception> log = null)
        : base(store, readSetting, writeSetting, isLoggedIn, send, notify, log) { }
}
namespace BiliLite.Api
{
    public static class ApiHelper
    {
        public static global::CookieService Cookie = new global::CookieService();
        static ApiHelper()
        {
            var sets = new List<string>();
            bool single = false;
            foreach (var i in settingsname)
            {
                single = !single;
                sets.Add(
                SettingHelper.GetValue(i, single ? DefaultKey.Appkey : DefaultKey.Secret));
            }
            Init(sets);
        }
        // BiliLite.WebApi 项目部署的服务器
        //public static string baseUrl = "http://localhost:5000";
        public const string IL_BASE_URL = "https://biliapi.iliili.cn";//已失效

        // GIT RAW路径
        public const string GIT_RAW_URL = "https://git.nsapps.cn/xiaoyaocz/BiliLite/raw/master/";

        // 哔哩哔哩API
        public static string Default_API_BASE_URL = "https://api.bilibili.com";
        public static string API_BASE_URL = "https://api.bilibili.com";

        //漫游默认的服务器
        public const string ROMAING_PROXY_URL = "https://b.chuchai.vip";
        public static List<string> settingsname = new List<string>() {
    "appkey_android_1",
    "appkey_android_2",
    "appkey_login_1",
    "appkey_login_2",
    "appkey_video_1",
    "appkey_video_2",
    "appkey_tv_1",
    "appkey_tv_2",
    "appkey_web_1",
    "appkey_web_2",
        };
        public static ApiKeyInfo DefaultKey = new ApiKeyInfo("4409e2ce8ffd12b8", "59b43e04ad6965f34319062b478f83dd");
        public static ApiKeyInfo AndroidKey = new ApiKeyInfo("4409e2ce8ffd12b8", "59b43e04ad6965f34319062b478f83dd");
        public static ApiKeyInfo AndroidVideoKey = new ApiKeyInfo("4409e2ce8ffd12b8", "59b43e04ad6965f34319062b478f83dd");
        public static ApiKeyInfo WebVideoKey = new ApiKeyInfo("4409e2ce8ffd12b8", "59b43e04ad6965f34319062b478f83dd");
        public static ApiKeyInfo AndroidTVKey = new ApiKeyInfo("4409e2ce8ffd12b8", "59b43e04ad6965f34319062b478f83dd");
        public static ApiKeyInfo LoginKey = new ApiKeyInfo("4409e2ce8ffd12b8", "59b43e04ad6965f34319062b478f83dd");
        public const string build = "7160300";
        public const string _mobi_app = "android";
        public const string _platform = "android";
        public static string deviceId = "";
        public static string customcookie = "";
        //二级网址
        public static string default_api2 = "/x/v2";
        public static string api2 = "/x/v2";
        //api
        public static string readcomment = "/reply/main";
        public static string comment = "/reply/add";
        public static string replycomment = "/reply/add";
        public static string replyreply = "/reply/reply";
        public static string readcomment_old = "/reply";//2025仍然有效
        //sync
        public static string SyncCookie(string cookie_name, string setting_name) => Cookie.SyncCookie(cookie_name, setting_name);

        //csrf
        public static string GetCSRF(bool isparam = false) => Cookie.GetCSRF();

        public static string GetSign(string url)
        {
            ApiKeyInfo apiKeyInfo = LoginKey;
            return GetSign(url, apiKeyInfo);

        }
        public static string GetSign(string url, ApiKeyInfo apiKeyInfo, string par = "&sign=")
        {
            string result;
            string str = url.Substring(url.IndexOf("?", 4) + 1);
            List<string> list = str.Split('&').ToList();
            list.Sort();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string str1 in list)
            {
                stringBuilder.Append((stringBuilder.Length > 0 ? "&" : string.Empty));
                stringBuilder.Append(str1);
            }
            stringBuilder.Append(string.IsNullOrEmpty(apiKeyInfo.Secret) ? DefaultKey.Secret : apiKeyInfo.Secret);
            result = Utils.ToMD5(stringBuilder.ToString()).ToLower();
            return par + result;
        }
        public static void Init(List<string> keys)
        {
            if (!string.IsNullOrEmpty(keys[0]) && !string.IsNullOrEmpty(keys[1]))
            {
                AndroidKey = new ApiKeyInfo(keys[0], keys[1]);
            }
            else
            {
                AndroidKey = DefaultKey;
            }

            if (keys.Count >= 4 && !string.IsNullOrEmpty(keys[2]) && !string.IsNullOrEmpty(keys[3]))
            {
                LoginKey = new ApiKeyInfo(keys[2], keys[3]);
            }
            else
            {
                LoginKey = DefaultKey;
            }

            if (keys.Count >= 6 && !string.IsNullOrEmpty(keys[4]) && !string.IsNullOrEmpty(keys[5]))
            {
                AndroidVideoKey = new ApiKeyInfo(keys[4], keys[5]);
            }
            else
            {
                AndroidVideoKey = DefaultKey;
            }

            if (keys.Count >= 8 && !string.IsNullOrEmpty(keys[6]) && !string.IsNullOrEmpty(keys[7]))
            {
                AndroidTVKey = new ApiKeyInfo(keys[6], keys[7]);
            }
            else
            {
                AndroidTVKey = DefaultKey;
            }
            if (keys.Count >= 10 && !string.IsNullOrEmpty(keys[8]) && !string.IsNullOrEmpty(keys[9]))
            {
                WebVideoKey = new ApiKeyInfo(keys[8], keys[9]);
            }
            else
            {
                WebVideoKey = DefaultKey;
            }

            if (keys.Count >= 12 && !string.IsNullOrEmpty(keys[10]) && !string.IsNullOrEmpty(keys[11]))
            {
                API_BASE_URL = keys[10];
                api2 = keys[11];
            }
            else
            {
                API_BASE_URL = Default_API_BASE_URL;
                api2 = default_api2;
            }
            Cookie.LoadCookieFromSetting();
            Cookie.StartAutoRefresh();
        }
        public static string GetSign(IDictionary<string, string> pars, ApiKeyInfo apiKeyInfo)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in pars.OrderBy(x => x.Key))
            {
                sb.Append(item.Key);
                sb.Append("=");
                sb.Append(item.Value);
                sb.Append("&");
            }
            var results = sb.ToString().TrimEnd('&');
            results = results + apiKeyInfo.Secret;
            return "&sign=" + Utils.ToMD5(results).ToLower();
        }
        public static int wbitoken_expire = 0; // Legacy compatibility field; WbiKeyService now owns its bounded cache lifetime.
        public class wbiimg
        {
            // Compatibility mirror for old callers; WbiKeyService owns freshness and refresh.
            public string img_url;
            public string sub_url;
        }
        public class wbitoken
        {
            public wbiimg wbi_img { get; set; }
        }
        public static wbiimg wbitoken_current = new wbiimg();
        private static readonly WbiKeyService wbiKeyService = new WbiKeyService(onKeysChanged: keys =>
            {
                if (wbitoken_current == null) wbitoken_current = new wbiimg();
                wbitoken_current.img_url = keys.ImgKey;
                wbitoken_current.sub_url = keys.SubKey;
            });

        private static void SeedWbiKeyFromCompatibilityFields()
        {
            var legacyImg = wbitoken_current?.img_url;
            var legacySub = wbitoken_current?.sub_url;
            if (string.IsNullOrWhiteSpace(legacyImg) || string.IsNullOrWhiteSpace(legacySub)) return;
            if (!wbiKeyService.HasCachedKeys)
                wbiKeyService.Seed(new WbiKey(legacyImg, legacySub));
        }

        private static async Task<(string, string)> GetWbiKeys()
        {
            SeedWbiKeyFromCompatibilityFields();
            var keys = await wbiKeyService.GetKeysAsync();
            return (keys.ImgKey, keys.SubKey);
        }

        public static async Task<bool> LoadWbiKey()
        {
            SeedWbiKeyFromCompatibilityFields();
            return await wbiKeyService.LoadWbiKeyAsync();
        }

        public static async Task<string> GetWbiSign(string url)
        {
            await GetWbiKeys();
            return await wbiKeyService.GetWbiSignAsync(url);
        }

        /// <summary>
        /// 一些必要的参数
        /// </summary>
        /// <param name="needAccesskey">是否需要accesskey</param>
        /// <returns></returns>
        public static string MustParameter(ApiKeyInfo apikey, bool needAccesskey = false)
        {
            var url = "";
            if (needAccesskey && SettingHelper.Account.Logined)
            {
                url = $"access_key={SettingHelper.Account.AccessKey}&";
            }
            return url + $"appkey={apikey.Appkey}&build={build}&mobi_app={_mobi_app}&platform={_platform}&ts={Utils.GetTimestampS()}";
        }
        /// <summary>
        /// 默认一些请求头
        /// </summary>
        /// <returns></returns>
        public static IDictionary<string, string> GetDefaultHeaders()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("user-agent", "Mozilla/5.0 BiliDroid/5.44.2 (bbcallen@gmail.com)");
            headers.Add("Referer", "https://www.bilibili.com/");
            return headers;
        }

        public static IDictionary<string, string> GetAuroraHeaders()
        {
            return new Dictionary<string, string>()
            {
                { "x-bili-aurora-zone", "sh001" },
                { "x-bili-aurora-eid", "UlMFQVcABlAH" },
            };
        }
        public static long cookie_timestamp
        {
            get => Cookie.CookieTimestamp;
            set => Cookie.CookieTimestamp = value;
        }

        public static bool need_refresh_cookie => Cookie.NeedRefreshCookie;

        public static Task<bool> NeedRefreshCookie() => Cookie.NeedRefreshCookieAsync();

        public static async void RefreshCookie()
        {
            try
            {
                await Cookie.RefreshCookieAsync();
            }
            catch (Exception exception)
            {
                LogHelper.Log("Cookie refresh failed.", LogType.ERROR, exception);
            }
        }

        public static string GetRefreshToken() => Cookie.GetRefreshToken();

    }
    public class ApiKeyInfo
    {
        public ApiKeyInfo(string key, string secret)
        {
            Appkey = key;
            Secret = secret;
        }
        public string Appkey { get; set; }
        public string Secret { get; set; }
    }
    public class ApiModel
    {
        /// <summary>
        /// 请求方法
        /// </summary>
        public RestSharp.Method method { get; set; }
        /// <summary>
        /// API地址
        /// </summary>
        public string baseUrl { get; set; }
        /// <summary>
        /// Url参数
        /// </summary>
        public string parameter { get; set; }
        /// <summary>
        /// 发送内容体，用于POST方法
        /// </summary>
        public string body { get; set; }
        /// <summary>
        /// 请求头
        /// </summary>
        public IDictionary<string, string> headers { get; set; }
        /// <summary>
        /// 需要Cookie
        /// </summary>
        public bool need_cookie { get; set; } = false;

        /// <summary>
        /// Additional cookies to send with the request.
        /// </summary>
        public IDictionary<string, string> ExtraCookies { get; set; }

        /// <summary>
        /// 请求地址
        /// </summary>
        public string url
        {
            get
            {
                return baseUrl + "?" + parameter;
            }
        }
    }
}

using BiliLite.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Windows.Web.Http.Filters;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Windows.Web.Http;
using gfoidl.Base64;
using System.Timers;

public interface ICookieService
{
    void WriteCookie(string name, string value);
    string ReadCookie(string name, string defaultValue);
}

public class CookieService : ICookieService
{
    private readonly HttpBaseProtocolFilter _filter;

    public CookieService()
    {
        _filter = new HttpBaseProtocolFilter();
    }

    public void WriteCookie(string name, string value)
    {
        var cookie = new HttpCookie(name, "bilibili.com", "/")
        {
            Value = value
        };
        _filter.CookieManager.SetCookie(cookie);
    }

    public string ReadCookie(string name, string defaultValue)
    {
        var cookies = _filter.CookieManager.GetCookies(new Uri("https://bilibili.com"));
        if (cookies.Count == 0) return defaultValue;
        var cookie = cookies.FirstOrDefault(x => x.Name == name);
        return cookie?.Value ?? defaultValue;
    }
}
namespace BiliLite.Api
{
    public static class ApiHelper
    {
        public static CookieService Cookie = new CookieService();
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
        public static string SyncCookie(string cookie_name, string setting_name)
            {
            var cookie_value = Cookie.ReadCookie(cookie_name, "");
            var setting_value = SettingHelper.GetValue<string>(setting_name, "");
            //sync
            if (String.IsNullOrEmpty(cookie_value) && !String.IsNullOrEmpty(setting_value))
            {
                Cookie.WriteCookie(cookie_name, setting_value);
                cookie_value = setting_value;
            }
            else if (!String.IsNullOrEmpty(cookie_value) && String.IsNullOrEmpty(setting_value))
                {
                setting_value = cookie_value;
                SettingHelper.SetValue(setting_name, setting_value);
                    }
            return cookie_value;
                }
        //csrf
        public static string GetCSRF(bool isparam = false)
        {
            return SyncCookie("bili_jct", "CookieCSRF");
        }

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
            LoadCookieFromSetting();
            AutoRefreshCookie();
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
        private static int[] mixinKeyEncTab = new int[] {
            46, 47, 18, 2, 53, 8, 23, 32, 15, 50, 10, 31, 58, 3, 45, 35, 27, 43, 5, 49,
            33, 9, 42, 19, 29, 28, 14, 39, 12, 38, 41, 13, 37, 48, 7, 16, 24, 55, 40,
            61, 26, 17, 0, 1, 60, 51, 30, 4, 22, 25, 54, 21, 56, 59, 6, 63, 57, 62, 11,
            36, 20, 34, 44, 52
        };
        public static int wbitoken_expire = 0;
        public class wbiimg
        {
            public string img_url;
            public string sub_url;
        }
        public class wbitoken
        {
            public wbiimg wbi_img { get; set; }
        }
        public static wbiimg wbitoken_current = new wbiimg();
        // 获取最新的 img_key 和 sub_key
        private static async Task<(string, string)> GetWbiKeys()
        {
            var response = await new AccountApi().WbiToken().Request();
            var result = await response.GetData<wbitoken>();
            var imgUrl = result.data.wbi_img.img_url;
            var subUrl = result.data.wbi_img.sub_url;
            var imgKey = imgUrl.Substring(imgUrl.LastIndexOf('/') + 1).Split('.')[0];
            var subKey = subUrl.Substring(subUrl.LastIndexOf('/') + 1).Split('.')[0];
            wbitoken_current.img_url = imgKey;
            wbitoken_current.sub_url = subKey;
            return (imgKey, subKey);
        }
        public static async Task<bool> LoadWbiKey()
        {
            if (wbitoken_current.img_url == null)
            {
                await GetWbiKeys();
                return true;
            }
            return false;
        }

        private static string GetMixinKey(string origin)
        {
            // 对 imgKey 和 subKey 进行字符顺序打乱编码
            return mixinKeyEncTab.Aggregate("", (s, i) => s + origin[i]).Substring(0, 32);
        }

        public static string GetWbiSign(string url)
        {
            var imgKey = "";
            var subKey = "";
            if (wbitoken_current.img_url != null)
            {
                imgKey = wbitoken_current.img_url;
                subKey = wbitoken_current.sub_url;
            }
            else
            {
                GetWbiKeys();
                return "";
            }

            // 为请求参数进行 wbi 签名
            string mixinKey = GetMixinKey(imgKey + subKey);
            long currentTime = (long)Math.Round(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);

            var queryString = HttpUtility.ParseQueryString(url);

            var queryParams = queryString.Cast<string>().ToDictionary(k => k, v => queryString[v]);
            queryParams["wts"] = currentTime + ""; // 添加 wts 字段
            queryParams = queryParams.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value); // 按照 key 重排参数
                                                                                                  // 过滤 value 中的 "!'()*" 字符
            queryParams = queryParams.ToDictionary(x => x.Key, x => string.Join("", x.Value.ToString().Where(c => "!'()*".Contains(c) == false)));
            queryString = HttpUtility.ParseQueryString(String.Empty);
            foreach (var key in queryParams.Keys)
            {
                queryString.Add(key, queryParams[key]);
            }
            var query = queryString.ToString();

            var wbi_sign = Utils.ToMD5($"{query}{mixinKey}").ToLower();

            return $"&wts={currentTime}&w_rid={wbi_sign}";
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
        private static bool has_inited_cookie = false;
        private static short init_cookie_try_count = 0;
        private static short init_cookie_try_max = 5;
        private static bool _need_refresh_cookie;
        public static bool need_refresh_cookie
        {
            get
            {
                if (!has_inited_cookie)
                {
                    NeedRefreshCookie();
                    return false;
                }
                return _need_refresh_cookie;
            }
        }
        private static void LoadCookieFromSetting()
        {
            SyncCookie("SESSDATA", "CookieSESSDATA");
            SyncCookie("bili_jct", "CookieCSRF");
        }
        public static long cookie_timestamp = 0;
        public async static Task<bool> NeedRefreshCookie()
        {
            if (has_inited_cookie) return _need_refresh_cookie;
            if (!SettingHelper.Account.Logined)
            {
                has_inited_cookie = true;
                _need_refresh_cookie = false;
                return _need_refresh_cookie;
            }
            var url = "https://passport.bilibili.com/x/passport-login/web/cookie/info";
            var api = new ApiModel() {
                need_cookie = true,
                method = RestSharp.Method.Get,
                baseUrl = url,
            };
            LoadCookieFromSetting();
            var result = await api.Request();
            if (result.code==200)
            {
                var j = result.GetJObject();
                has_inited_cookie = true;
                if (j["data"] != null)
                {
                    var ts = (long)(j["data"]["timestamp"]);
                    cookie_timestamp = ts;
                    _need_refresh_cookie = (bool)(j["data"]["refresh"] ?? false);
                }
                else
                    _need_refresh_cookie = true;
            }
            else if (result.code == -101)
            {
                //未登录则不需要
                has_inited_cookie = true;
                _need_refresh_cookie = false;
            }
            init_cookie_try_count++;
            if (init_cookie_try_count > init_cookie_try_max)
            {
                has_inited_cookie = true;
                _need_refresh_cookie = false;
            }
            return _need_refresh_cookie;
        }
        static string GetCorrespondPath(long timestamp)
        {
            // 构造消息体
            string message = $"refresh_{timestamp}";
            byte[] data = Encoding.UTF8.GetBytes(message);

            // 导入公钥
            RSA rsa = RSA.Create();
            string m = "y4HdjgJHBlbaBN04VERG4qNBIFHP6a3GozCl75AihQloSWCXC5HDNgyinEnhaQ_4-gaMud_GF50elYXLlCToR9se9Z8z433U3KjM-3Yx7ptKkmQNAMggQwAVKgq3zYAoidNEWuxpkY_mAitTSRLnsJW-NCTa0bqBFF6Wm1MxgfE";
            string e = "AQAB";
            rsa.ImportParameters(new RSAParameters()
            {
                Modulus = Base64.Url.Decode(m.ToCharArray()),
                Exponent = Base64.Url.Decode(e.ToCharArray())
            });

            // 使用 RSA-OAEP 加密
            byte[] encryptedBytes = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);

            // 将加密后的字节数组转换为小写的 Base16 字符串
            return BitConverter.ToString(encryptedBytes).Replace("-", "").ToLower();
        }
        private static bool need_refresh_cookie_notified = false;
        public static async void RefreshCookie()
        {
            if (cookie_timestamp == 0)
            {
                LogHelper.Log("No cookie timestamp provided to refresh cookie", LogType.INFO);
                if (!need_refresh_cookie_notified)
                {
                    need_refresh_cookie_notified = true;
                    Utils.ShowMessageToast("啊哦，cookie过气了，\n而且没有刷新令牌，\n需要重新登录了呢");
                }
                return;
            }
            var correspondPath = GetCorrespondPath(cookie_timestamp);
            cookie_timestamp = 0;
            var url = $"https://www.bilibili.com/correspond/1/{correspondPath}";
            var api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = url,
                need_cookie = true,
            };
            var result = await api.Request();
            string csrf = "";
            if (result.code == 200)
            {
                var text = result.results;
                string pattern = @"<div id=""1-name"">([^<]+)</div>";
                Match match = Regex.Match(text, pattern);

                if (match.Success)
                {
                    csrf = match.Groups[1].Value;
                }
                else
                {
                    LogHelper.Log("刷新cookie时发生正则表达式没匹配", LogType.INFO);
                }
            }
            if (String.IsNullOrEmpty(csrf))
            {
                return;
            }
            var url2 = "https://passport.bilibili.com/x/passport-login/web/cookie/refresh";
            var token = GetRefreshToken();
            if (String.IsNullOrEmpty(token)) return;
            var api2 = new ApiModel()
            {
                need_cookie = true,
                method = RestSharp.Method.Post,
                baseUrl = url2,
                body = $"csrf={GetCSRF()}&refresh_csrf={csrf}&source=main_web&refresh_token={token}"
            };
            var result2 = await api2.Request();
            var new_token = "";
            if (result2.code == 200)
            {
                var d = result2.GetJObject();
                var obj = d["data"];
                if (obj != null)
                {
                    new_token = (string)(obj["refresh_token"]);
                }
            }
            else if (result2.code == -101)
            {
                LogHelper.Log("刷新cookie时出现账号未登录", LogType.INFO);
            } else if (result2.code == -111)
            {
                LogHelper.Log("刷新cookie时出现csrf校验失败", LogType.INFO);
            }
            if (String.IsNullOrEmpty(new_token)) return;
            //设置新的cookie
            SettingHelper.SetValue("CookieRefreshToken", new_token);
            //注销
            var url3 = "https://passport.bilibili.com/x/passport-login/web/confirm/refresh";
            var api3 = new ApiModel()
            {
                need_cookie = true,
                method = RestSharp.Method.Post,
                baseUrl = url3,
                body = $"csrf={GetCSRF()}&refresh_token={token}"
            };
            var result3 = await api.Request();
            _need_refresh_cookie = false;
        }
        public static string GetRefreshToken()
        {
            var token = SettingHelper.GetValue<string>("CookieRefreshToken", "");
            return token;
        }
        // 定时器
        private static Timer _timer;

        // 上一次执行的时间戳
        private static DateTime _lastExecutionTime;
        private static void AutoRefreshCookie()
        {
            // 程序启动时立即执行一次任务
            ExecuteTask(DateTime.Now);

            // 初始化定时器，设置为1天执行一次
            InitializeTimer(TimeSpan.FromDays(1));
        }
        private static void InitializeTimer(TimeSpan interval)
        {
            // 创建定时器
            _timer = new Timer(interval.TotalMilliseconds);
            _timer.Elapsed += TimerElapsed;
            _timer.AutoReset = true; // 自动重新触发
            _timer.Start();

            Console.WriteLine($"定时器已启动，间隔时间为 {interval}...");
        }

        private static void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            // 获取当前时间
            DateTime now = DateTime.Now;

            // 执行任务
            ExecuteTask(now);

            // 更新上一次执行的时间戳
            _lastExecutionTime = now;
        }

        private static void ExecuteTask(DateTime currentTimestamp)
        {
            has_inited_cookie = false;
        }
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

using BiliLite.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Windows.Web.Http.Filters;

namespace BiliLite.Api
{
    public static class ApiHelper
    {
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
        public const string IL_BASE_URL = "https://biliapi.iliili.cn";

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
        public static string readcomment = "/reply";
        public static string comment = "/reply/add";
        public static string replycomment = "/reply/add";
        public static string replyreply = "/reply/reply";
        //csrf
        public static string _csrf = "";
        public static string GetCSRF(bool isparam = false)
        {
            if (_csrf != "")
            {
                if (isparam) return "&csrf=" + _csrf;
                else return _csrf;
            }
            var fiter = new HttpBaseProtocolFilter();
            var cookies = fiter.CookieManager.GetCookies(new Uri("https://bilibili.com"));
            var csrf = "";
            //没有Cookie
            if (cookies == null || cookies.Count == 0)
            {

            }
            else
            {
                csrf = cookies.FirstOrDefault(x => x.Name == "bili_jct")?.Value;
                if (csrf != null && csrf != "")
                {
                    _csrf = csrf;
                    if (isparam)
                    {
                        csrf = "&csrf=" + csrf;
                    }
                }
            }
            return csrf;
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
        public static async  Task<bool> LoadWbiKey()
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
            long currentTime = (long)Math.Round(DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);

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

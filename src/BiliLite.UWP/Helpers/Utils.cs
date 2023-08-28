using BiliLite.Api;
using BiliLite.Controls;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using System.IO;
using BiliLite.Dialogs;
using Windows.UI.Popups;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Windows.UI.Xaml;

namespace BiliLite.Helpers
{
    public static class Utils
    {
        /// <summary>
        /// 发送请求，扩展方法
        /// </summary>
        /// <param name="api"></param>
        /// <returns></returns>
        public async static Task<HttpResults> Request(this ApiModel api)
        {
            if (api.method == RestSharp.Method.Get)
            {
                if (api.need_cookie)
                {
                    return await HttpHelper.GetWithWebCookie(api.url, api.headers);
                }
                return await HttpHelper.Get(api.url, api.headers);
            }
            else
            {
                return await HttpHelper.Post(api.url, api.body, api.headers);
            }
        }

        /// <summary>
        /// 默认一些请求头
        /// </summary>
        /// <returns></returns>
        public static IDictionary<string, string> GetDefaultHeaders()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("user-agent", "Mozilla/5.0 BiliDroid/5.34.1 (bbcallen@gmail.com)");
            headers.Add("Referer", "https://www.bilibili.com/");
            return headers;
        }
        /// <summary>
        /// 将时间戳转为时间
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public static DateTime TimestampToDatetime(long ts)
        {
            DateTime dtStart = new DateTime(1970, 1, 1, 8, 0, 0);
            long lTime = long.Parse(ts + "0000000");
            TimeSpan toNow = new TimeSpan(lTime);
            return dtStart.Add(toNow);
        }
        /// <summary>
        /// 生成时间戳/秒
        /// </summary>
        /// <returns></returns>
        public static long GetTimestampS()
        {
            return Convert.ToInt64((DateTime.Now - new DateTime(1970, 1, 1, 8, 0, 0, 0)).TotalSeconds);
        }
        /// <summary>
        /// 生成时间戳/豪秒
        /// </summary>
        /// <returns></returns>
        public static long GetTimestampMS()
        {
            return Convert.ToInt64((DateTime.Now - new DateTime(1970, 1, 1, 8, 0, 0, 0)).TotalMilliseconds);
        }

        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ToMD5(string input)
        {
            var provider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            IBuffer buffer = CryptographicBuffer.ConvertStringToBinary(input, BinaryStringEncoding.Utf8);
            var hashed = provider.HashData(buffer);
            var result = CryptographicBuffer.EncodeToHexString(hashed);
            return result;
        }
        public static void ShowMessageToast(string message, int seconds = 2, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "", [System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            string Message = $"{message}";
            MessageToast ms = new MessageToast(Message, TimeSpan.FromSeconds(seconds));
            ms.Show();
        }
        public static StringBuilder sb = new StringBuilder();
        public static Pages.SettingPage sp = null;
        public static void AddALog(string message)
        {
            sb.AppendLine(message);
            sp?.Log(sb);
        }
        public static void ShowMessageToast(string message,string longmsg, int seconds = 10, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "", [System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            //当有错误时延长时间
            string cutmsg = longmsg.Length > 50 ? longmsg.Substring(longmsg.Length - 50) : longmsg;
            string errorMessage = $"{memberName}@{filePath}:{lineNumber}\n{message}\n{cutmsg}";
            MessageToast ms = new MessageToast(errorMessage, TimeSpan.FromSeconds(seconds));
            ms.Show();
            //并且记录，如果确实是一个exception
            if(longmsg.Length>50)
            LogHelper.logger.Error("来自Utils的报错%s", longmsg);
            //并汇报
            AddALog(message);
            AddALog(longmsg);
        }
        public static void ShowMessageToast(string message, List<MyUICommand> commands, int seconds = 15)
        {
            MessageToast ms = new MessageToast(message, TimeSpan.FromSeconds(seconds), commands);
            ms.Show();
        }
        public static void ShowComment(string oid, int commentMode, Api.CommentApi.CommentSort commentSort)
        {
            CommentDialog ms = new CommentDialog();
            ms.Show(oid, commentMode, commentSort);
        }
        public static int ToInt32(this object obj)
        {

            if (int.TryParse(obj.ToString(), out var value))
            {
                return value;
            }
            else
            {
                return 0;
            }
        }
        public static string ToCountString(this object obj)
        {
            if (obj == null) return "0";
            if (double.TryParse(obj.ToString(), out var number))
            {

                if (number >= 10000)
                {
                    return ((double)number / 10000).ToString("0.0") + "万";
                }
                return obj.ToString();
            }
            else
            {
                return obj.ToString();
            }
        }

        /// <summary>
        /// 根据Epid取番剧ID
        /// </summary>
        /// <returns></returns>
        public async static Task<string> BangumiEpidToSid(string epid)
        {
            try
            {
                var re = await HttpHelper.GetString($"https://bangumi.bilibili.com/view/web_api/season?ep_id={epid}");
                var obj = JObject.Parse(re);
                return obj["result"]["season_id"].ToString();
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// 短链接还原
        /// </summary>
        /// <returns></returns>
        public async static Task<string> GetShortLinkLocation(string shortlink)
        {
            try
            {
                HttpMessageHandler httpMessageHandler = new HttpClientHandler()
                {
                    AllowAutoRedirect = false,
                };
                using (HttpClient client = new HttpClient(httpMessageHandler))
                {
                    var response = await client.GetAsync(shortlink);
                    return response.Headers.Location.ToString();
                }
            }
            catch (Exception)
            {
                return shortlink;
            }

        }

        private static bool dialogShowing = false;
        public async static Task<bool> ShowLoginDialog()
        {
            if (!dialogShowing)
            {
                LoginDialog login = new LoginDialog();
                dialogShowing = true;
                await login.ShowAsync();
                dialogShowing = false;
            }
            if (SettingHelper.Account.Logined)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async static Task<bool> ShowDialog(string title, string content)
        {
            MessageDialog messageDialog = new MessageDialog(content, title);
            messageDialog.Commands.Add(new UICommand() { Label = "确定", Id = true });
            messageDialog.Commands.Add(new UICommand() { Label = "取消", Id = false });
            var result = await messageDialog.ShowAsync();
            return (bool)result.Id;
        }

        public static string RegexMatch(string input, string regular)
        {
            var data = Regex.Match(input, regular);
            if (data.Groups.Count >= 2 && data.Groups[1].Value != "")
            {
                return data.Groups[1].Value;
            }
            else
            {
                return "";
            }
        }
        public static async Task<T> DeserializeJson<T>(this string results)
        {
            return await Task.Run<T>(() =>
            {
                return JsonConvert.DeserializeObject<T>(results);
            });
        }
        public static string ToSimplifiedChinese(string content)
        {
            content = ChineseConverter.TraditionalToSimplified(content);
            return content;
        }
        public static bool SetClipboard(string content)
        {
            try
            {
                Windows.ApplicationModel.DataTransfer.DataPackage pack = new Windows.ApplicationModel.DataTransfer.DataPackage();
                pack.SetText(content);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(pack);
                Windows.ApplicationModel.DataTransfer.Clipboard.Flush();
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public static string HandelTimestamp(string ts)
        {
            if (ts.Length == 10)
            {
                ts += "0000000";
            }
            DateTime dtStart = new DateTime(1970, 1, 1, 0, 0, 0);
            long lTime = long.Parse(ts);
            TimeSpan toNow = new TimeSpan(lTime);
            DateTime dt = dtStart.Add(toNow).ToLocalTime();
            TimeSpan span = DateTime.Now.Date - dt.Date;
            if (span.TotalDays <= 0)
            {
                return "今天" + dt.ToString("HH:mm");
            }
            else if (span.TotalDays >= 1 && span.TotalDays < 2)
            {
                return "昨天" + dt.ToString("HH:mm");
            }
            else
            {
                return dt.ToString("yyyy-MM-dd HH:mm");
            }
        }

        public async static Task CheckVersion()
        {
            try
            {
                var result = await new GitApi().CheckUpdate().Request();
                var ver = JsonConvert.DeserializeObject<NewVersion>(result.results);
                var num = $"{SystemInformation.Instance.ApplicationVersion.Major}{SystemInformation.Instance.ApplicationVersion.Minor.ToString("00")}{SystemInformation.Instance.ApplicationVersion.Build.ToString("00")}";
                var v = int.Parse(num);
                if (ver.version_num > v)
                {
                    var dialog = new ContentDialog();

                    dialog.Title = $"发现新版本 Ver {ver.version}";
                    MarkdownTextBlock markdownText = new MarkdownTextBlock()
                    {
                        Text = ver.version_desc,
                        TextWrapping = Windows.UI.Xaml.TextWrapping.Wrap,
                        IsTextSelectionEnabled = true,
                        Background = new SolidColorBrush(Colors.Transparent)
                    };
                    markdownText.LinkClicked += new EventHandler<LinkClickedEventArgs>(async (sender, args) =>
                    {
                        await Utils.LaunchUri(new Uri(args.Link));
                    });
                    dialog.Content = markdownText;
                    dialog.PrimaryButtonText = "查看详情";
                    dialog.SecondaryButtonText = "忽略";

                    dialog.PrimaryButtonClick += new Windows.Foundation.TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs>(async (sender, e) =>
                    {
                        await LaunchUri(ver.url);
                    });
                    await dialog.ShowAsync();
                }
            }
            catch (Exception)
            {
            }
        }
        public static async Task<bool> LaunchUri(Uri uri)
        {
            var result = await Launcher.LaunchUriAsync(uri);
            if (!result)
            {
                Utils.ShowMessageToast("打开" + uri + "失败");
            }
            return result;
        }
        public static async Task<bool> LaunchUri(string uri)
        {
            var result = await Launcher.LaunchUriAsync(new Uri(uri));
            if (!result)
            {
                Utils.ShowMessageToast("打开" + uri + "失败");
            }
            return result;
        }

        public static Color ToColor(this string obj)
        {
            obj = obj.Replace("#", "");
            if (int.TryParse(obj, out var c))
            {
                obj = c.ToString("X2");
            }
            Color color = new Color();
            if (obj.Length <= 6)
            {
                obj = obj.PadLeft(6, '0');
                color.R = byte.Parse(obj.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                color.G = byte.Parse(obj.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                color.B = byte.Parse(obj.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                color.A = 255;
            }
            else
            {
                obj = obj.PadLeft(8, '0');
                color.R = byte.Parse(obj.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                color.G = byte.Parse(obj.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                color.B = byte.Parse(obj.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                color.A = byte.Parse(obj.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return color;
        }
        public static void ReadB(this Stream stream, byte[] buffer, int offset, int count)
        {
            if (offset + count > buffer.Length)
                throw new ArgumentException();
            var read = 0;
            while (read < count)
            {
                var available = stream.Read(buffer, offset, count - read);
                if (available == 0)
                {
                    // throw new ObjectDisposedException(null);
                }
                read += available;
                offset += available;
            }
        }

        public static T ObjectClone<T>(this T obj)
        {
            var type = typeof(T);

            if (!type.IsSerializable)
                return default(T);

            if (Object.ReferenceEquals(obj, null))
                return default(T);

            IFormatter format = new BinaryFormatter();

            using (MemoryStream ms = new MemoryStream())
            {
                try
                {
                    format.Serialize(ms, obj);
                    ms.Seek(0, SeekOrigin.Begin);
                    return (T)format.Deserialize(ms);
                }
                catch (Exception e)
                {
                    return default(T);
                }
            }
        }

        public static string ParseArea(string title, long mid)
        {
            if (Regex.IsMatch(title, @"僅.*港.*地區"))
            {
                return "hk";
            }
            else if (Regex.IsMatch(title, @"僅.*台.*地區"))
            {
                return "tw";
            }
            //如果是哔哩哔哩番剧出差这个账号上传的
            //且标题不含僅**地區，返回地区设置为港澳台
            if (mid == 11783021)
            {
                return "hk";
            }
            return "cn";
        }
        public static string ParseArea(string title, string mid)
        {
            return ParseArea(title, mid.ToInt32());
        }
        public static string ChooseProxyServer(string area)
        {
            var proxyUrl = SettingHelper.GetValue(SettingHelper.Roaming.CUSTOM_SERVER_URL, ApiHelper.ROMAING_PROXY_URL);
            var proxyUrlCN = SettingHelper.GetValue(SettingHelper.Roaming.CUSTOM_SERVER_URL_CN, "");
            var proxyUrlHK = SettingHelper.GetValue(SettingHelper.Roaming.CUSTOM_SERVER_URL_HK, "");
            var proxyUrlTW = SettingHelper.GetValue(SettingHelper.Roaming.CUSTOM_SERVER_URL_TW, "");
            if (area == "cn")
            {
                return string.IsNullOrEmpty(proxyUrlCN) ? proxyUrl : proxyUrlCN;
            }
            if (area == "hk")
            {
                return string.IsNullOrEmpty(proxyUrlHK) ? proxyUrl : proxyUrlHK;
            }
            if (area == "tw")
            {
                return string.IsNullOrEmpty(proxyUrlTW) ? proxyUrl : proxyUrlTW;
            }
            return proxyUrl;
        }
        public static string ProcessURL(string u, int maxLength=40)
        {
            // Remove http:// or https:// from the beginning of the URL
            if (u.StartsWith("http://"))
            {
                u = u.Substring(7);
            }
            else if (u.StartsWith("https://"))
            {
                u = u.Substring(8);
            }

            // Truncate the URL if it is too long and add ellipses
            if (u.Length > maxLength)
            {
                u = u.Substring(0, maxLength - 3) + "...";
            }

            return u;
        }
        public static SolidColorBrush GetBrush(string name)
        {
            var themeDictionaries = App.Current.Resources.ThemeDictionaries;
            var requestedTheme = ((FrameworkElement)Window.Current.Content).RequestedTheme;
            var themeKey = requestedTheme == ElementTheme.Light ? "Light" : "Dark";
            if (themeDictionaries.TryGetValue(themeKey, out object theme))
            {
                var t =  (ResourceDictionary)theme;
                if (t.ContainsKey(name))
                {
                    var highLightColor = (Color)t[name];
                    return new SolidColorBrush(highLightColor);
                }
                return GetBrush("TextColor");
            }
            else
            {
                // 处理主题字典中未找到指定的键的情况
                return new SolidColorBrush(Colors.Transparent);
            }
        }
        /*
         * video123
         * aid=123
         * avid=123
         * video/123
         * av123
         * story/123
         */
        public static long ExtractVideoId(string url)
        {
            url = url.Replace("aid","av").Replace("bid","bv").Replace("avid","av");
            string pattern = @"(video|av|story)([/=]?)(\d+)";
            long id = 0;
            var match = Regex.Matches(url, pattern);

            for(var i=0;i<match.Count;i++){
                var m = match[i];
                string videoId = m.Groups[3].Value;
                long vid;
                // 判断视频ID是否较大
                if (long.TryParse(videoId, out vid))
                {
                    if (vid >id &&vid> 10)  // 假设10为较大的ID阈值
                    {
                        id = vid ;
                    }
                }
            }

            return id;
        }
    }
    public class NewVersion
    {
        public string version { get; set; }
        public string version_desc { get; set; }
        public int version_num { get; set; }
        public string url { get; set; }
    }
}

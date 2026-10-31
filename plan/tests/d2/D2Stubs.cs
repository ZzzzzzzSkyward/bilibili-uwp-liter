using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RestSharp
{
    public enum Method { Get, Post }
}

namespace BiliLite.Models
{
    public class ReturnModel<T>
    {
        public bool success { get; set; }
        public string message { get; set; }
        public T data { get; set; }
    }
}

namespace BiliLite.Api
{
    public class ApiModel
    {
        public RestSharp.Method method { get; set; }
        public string baseUrl { get; set; }
        public string @params { get; set; }
    }

    public class PlayerAPI
    {
        // 文件导入/导出不允许触网：任何调用都会被计数
        public static int CallCount;

        public ApiModel GetDanmuFilterWords()
        {
            CallCount++;
            return new ApiModel
            {
                method = RestSharp.Method.Get,
                baseUrl = "https://api.bilibili.com/x/dm/filter/user"
            };
        }
    }
}

namespace BiliLite.Helpers
{
    public sealed class HttpResults
    {
        public int code { get; set; }
        public bool status { get; set; }
        public string message { get; set; }
        public string results { get; set; }

        public JObject GetJObject() => JObject.Parse(string.IsNullOrEmpty(results) ? "{}" : results);
    }

    public enum LogType { ERROR, INFO }

    public static class LogHelper
    {
        public static readonly List<string> Entries = new List<string>();

        public static void Log(string message, LogType type, Exception exception = null) => Entries.Add(message);

        public static bool IsNetworkError(Exception ex) => false;
    }

    public static class Utils
    {
        public static readonly List<string> Toasts = new List<string>();

        public static void ShowMessageToast(string message) => Toasts.Add(message);

        public static int ToInt32(this object obj)
        {
            if (obj == null) return 0;
            return int.TryParse(obj.ToString(), out var value) ? value : 0;
        }

        public static Task<HttpResults> Request(this BiliLite.Api.ApiModel api) =>
            Task.FromResult(new HttpResults { status = false, message = "probe-offline" });
    }

    /// <summary>
    /// 字典+JSON 存储，行为对齐真实 SettingHelper：值以 JSON 字符串落盘，读取失败回退默认值。
    /// </summary>
    public static class SettingHelper
    {
        private static readonly Dictionary<string, string> Values = new Dictionary<string, string>();

        public static T GetValue<T>(string key, T fallback)
        {
            if (!Values.TryGetValue(key, out var json)) return fallback;
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return fallback;
            }
        }

        public static void SetValue<T>(string key, T value) => Values[key] = JsonConvert.SerializeObject(value);

        public static string RawValue(string key) => Values.TryGetValue(key, out var json) ? json : null;

        public static bool HasKey(string key) => Values.ContainsKey(key);

        public static void Reset() => Values.Clear();

        public static class VideoDanmaku
        {
            public const string SHIELD_WORD = "VideoDanmuShieldWord";
            public const string SHIELD_USER = "VideoDanmuShieldUser";
            public const string SHIELD_REGULAR = "VideoDanmuShieldRegular";
        }

        public static class Live
        {
            public const string SHIELD_WORD = "LiveDanmuShieldWord";
        }
    }
}

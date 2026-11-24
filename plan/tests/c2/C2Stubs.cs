using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RestSharp
{
    public enum Method
    {
        Get,
        Post,
    }
}

namespace BiliLite.Api
{
    /// <summary>
    /// The product ApiModel only carries the request description; the probe adds the fields the
    /// fake transport needs to tell the signed and the unsigned builder apart.
    /// </summary>
    public class ApiModel
    {
        public RestSharp.Method method { get; set; }
        public string baseUrl { get; set; }
        public string parameter { get; set; }
        public string body { get; set; }
        public bool need_cookie { get; set; }

        public string Kind { get; set; }
        public string MediaListId { get; set; }
        public string LastAid { get; set; }
        public int TypeId { get; set; }
        public int PageSize { get; set; }
    }

    /// <summary>
    /// Records what the real MediaListService asks for and hands the request back to the fake
    /// transport instead of the network.
    /// </summary>
    public class VideoAPI
    {
        public ApiModel GetMediaList(string mediaListId, string lastAid, int typeId = 1, int pagesize = 20)
        {
            return C2Probe.FakeTransport.Describe("signed", mediaListId, lastAid, typeId, pagesize);
        }

        public ApiModel GetMediaListNotLogin(string mediaListId, string lastAid, int typeId = 1, int pagesize = 20)
        {
            return C2Probe.FakeTransport.Describe("unsigned", mediaListId, lastAid, typeId, pagesize);
        }
    }
}

namespace BiliLite.Helpers
{
    public enum LogType
    {
        INFO,
        DEBUG,
        ERROR,
        FATAL,
    }

    public static class LogHelper
    {
        public static void Log(string message, LogType type, Exception ex = null)
        {
            C2Probe.FakeTransport.Logs.Add(message);
        }

        public static bool IsNetworkError(Exception ex)
        {
            return false;
        }
    }

    public static class Utils
    {
        public static Task<HttpResults> Request(this BiliLite.Api.ApiModel api)
        {
            return C2Probe.FakeTransport.Handle(api);
        }

        /// <summary>
        /// Nothing compiled by this probe may publish: Toasts stays empty unless a publisher ran.
        /// </summary>
        public static void AddALog(string message) { C2Probe.FakeTransport.Logs.Add(message); }
        public static Task<bool> ShowLoginDialog() => Task.FromResult(true);
        public static void ShowMessageToast(string message, string details) => ShowMessageToast(message);
        public static void ShowMessageToast(string message)
        {
            C2Probe.FakeTransport.Toasts.Add(message);
        }
    }
}

// Request-only seams: the real FavoriteAPI owns construction. A recorder never repairs fields.
namespace BiliLite.Api
{
    public sealed class ApiKeyInfo { }
    public static class ApiHelper
    {
        public static string API_BASE_URL = "https://fixture.invalid/api";
        public static readonly ApiKeyInfo AndroidKey = new ApiKeyInfo();
        public static readonly ApiKeyInfo LoginKey = new ApiKeyInfo();
        public static bool LoggedIn;
        public static int CommonCalls, SignCalls;
        public static ApiKeyInfo CommonKey, SignKey;
        public static bool NeedAccessKey;
        public static string SignInput;
        public static string MustParameter(ApiKeyInfo key, bool needAccesskey = false)
        {
            CommonCalls++;
            CommonKey = key;
            NeedAccessKey = needAccesskey;
            // Same terminal boundary as product MustParameter: decimal ts, no trailing delimiter.
            return "appkey=dummy-android&build=123&mobi_app=android&platform=android"
                + (LoggedIn && needAccesskey ? "&access_key=dummy-access" : "") + "&ts=1700000000";
        }
        public static string GetSign(string input, ApiKeyInfo key)
        {
            SignCalls++;
            SignInput = input;
            SignKey = key;
            return "&sign=dummy-sign";
        }
        public static void ResetRecorder()
        {
            CommonCalls = SignCalls = 0;
            CommonKey = SignKey = null;
            SignInput = null;
            NeedAccessKey = false;
        }
    }
}
namespace BiliLite.Helpers
{
    public static class SettingHelper
    {
        public static class Account { public static string UserID = "dummy-uid"; public static bool Logined = true; }
    }
}

namespace Windows.UI.Xaml.Controls { public enum ListViewSelectionMode { None, Multiple } }
namespace BiliLite.Models { public class AnimeHomeModel { } }

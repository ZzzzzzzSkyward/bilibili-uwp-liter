using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

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

namespace BiliLite.Models
{
    public class ApiDataModel<T>
    {
        public int code { get; set; }
        public string message { get; set; }
        public string msg { get; set; }

        public bool success
        {
            get { return code == 0; }
        }

        public T data { get; set; }
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

    public class HttpResults
    {
        public bool status { get; set; } = true;
        public string message { get; set; }
        public string results { get; set; }

        public Task<BiliLite.Models.ApiDataModel<T>> GetData<T>()
        {
            return Task.FromResult(JsonConvert.DeserializeObject<BiliLite.Models.ApiDataModel<T>>(results));
        }
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

        public static void ShowMessageToast(string message)
        {
            C2Probe.FakeTransport.Toasts.Add(message);
        }
    }
}

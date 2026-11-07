using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

// Minimal stand-ins for the UWP/API surface the season caller needs. The parse path under test
// (Parsing/ApiEnvelope.cs, Parsing/Season/*, Models/Api/Season/Web/*, Modules/Detail/SeasonDetailVM.cs)
// is real code; only the transport and the app services it talks to are replaced here.
namespace BiliLite.Helpers
{
    public enum LogType
    {
        INFO,
        DEBUG,
        ERROR,
        FATAL
    }

    public static class LogHelper
    {
        public static readonly List<string> Entries = new List<string>();

        public static void Log(string message, LogType type, Exception ex = null)
        {
            Entries.Add(message);
        }

        public static bool IsNetworkError(Exception ex)
        {
            return false;
        }
    }

    public static class Utils
    {
        /// <summary>弹出过的提示文本，按顺序；探针用它断言解析/发布路径不弹 toast。</summary>
        public static readonly List<string> Toasts = new List<string>();

        public static void ShowMessageToast(string message, int seconds = 2)
        {
            Toasts.Add(message);
        }

        /// <summary>与产品同签名的长文案重载（HttpResults.GetJObject 用的是这个）。</summary>
        public static void ShowMessageToast(string message, string longmsg, int seconds = 10)
        {
            Toasts.Add(message);
        }

        public static Task<bool> ShowLoginDialog()
        {
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// REST reply stand-in: the probe hands the fixture JSON through <see cref="results"/>.
    /// <see cref="GetJObject"/> 与产品同行为：解不出来的正文弹 toast 并返回 null；
    /// <see cref="TryGetJObject"/> 是静默版。产品的 FindLongestValidJsonString 双 JSON 兜底没有复刻——
    /// 夹具都是单个 JSON 文档，这一层只影响"两个对象拼在一起的正文"。
    /// </summary>
    public sealed class HttpResults
    {
        public int code { get; set; }
        public bool status { get; set; }
        public string message { get; set; }
        public string results { get; set; }

        public JObject GetJObject()
        {
            JObject value;
            if (TryGetJObject(out value))
            {
                return value;
            }
            Utils.ShowMessageToast("解析json对象失败", "results 不是 JSON 对象");
            return null;
        }

        public bool TryGetJObject(out JObject value)
        {
            try
            {
                value = JObject.Parse(results ?? string.Empty);
                return true;
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }
    }

    public static class SettingHelper
    {
        public static class Account
        {
            public static bool Logined { get { return true; } }
        }
    }
}

namespace BiliLite.Api
{
    /// <summary>REST request stand-in: product code only calls <see cref="Request"/>.</summary>
    public class ApiModel
    {
        public Func<Task<BiliLite.Helpers.HttpResults>> Handler;

        public Task<BiliLite.Helpers.HttpResults> Request()
        {
            return Handler();
        }
    }

    public enum SeasonIdType
    {
        SeasonId,
        EpId,
    }

    /// <summary>
    /// pgc/view/web/ep/list 的请求替身。产品代码用 new SeasonApi().EpList(...)，所以这里只登记调用并按夹具回包。
    /// </summary>
    public class SeasonApi
    {
        public sealed class EpListCall
        {
            public string Id { get; set; }
            public SeasonIdType Type { get; set; }
        }

        /// <summary>产品代码发出的每一次 ep/list 请求，按顺序。</summary>
        public static readonly List<EpListCall> Calls = new List<EpListCall>();

        /// <summary>夹具：默认给一个业务码拒绝，单元用例自己覆盖。</summary>
        public static Func<EpListCall, string> Fixture = call => "{\"code\":-404,\"message\":\"啥都木有\"}";

        /// <summary>让下一次请求像 HTTP 失败一样返回（status=false, code=404），用一次就复位。</summary>
        public static bool FailNextRequest;

        public static void Reset()
        {
            Calls.Clear();
            Fixture = call => "{\"code\":-404,\"message\":\"啥都木有\"}";
            FailNextRequest = false;
        }

        public ApiModel EpList(string id, SeasonIdType type = SeasonIdType.SeasonId)
        {
            var call = new EpListCall { Id = id, Type = type };
            Calls.Add(call);
            return new ApiModel
            {
                Handler = () =>
                {
                    if (FailNextRequest)
                    {
                        FailNextRequest = false;
                        return Task.FromResult(new BiliLite.Helpers.HttpResults
                        {
                            status = false,
                            code = 404,
                            message = "NotFound",
                            results = string.Empty,
                        });
                    }
                    return Task.FromResult(new BiliLite.Helpers.HttpResults
                    {
                        status = true,
                        code = 200,
                        message = "0",
                        results = Fixture(call),
                    });
                },
            };
        }
    }
}

namespace BiliLite.Modules
{
    public class AnimeHomeModel
    {
    }
}

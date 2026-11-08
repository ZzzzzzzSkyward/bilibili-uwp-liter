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

        /// <summary>
        /// 与产品 GetResult 同行为：按 <see cref="BiliLite.Models.ApiResultModel{T}"/> 直解 result 信封，
        /// 解不出来就是 null（空正文、纯文本、字段类型不对都走这条）。产品在这里先打进一条 ERROR 再试
        /// FindLongestValidJsonString 双 JSON 兜底；夹具都是单个文档，兜底只把同一份正文再解一次，探针省掉它，
        /// 但仍然记一条日志，好让"解码失败"和"调用方自己记的错误"在断言里分得开。
        /// </summary>
        public Task<BiliLite.Models.ApiResultModel<T>> GetResult<T>()
        {
            try
            {
                return Task.FromResult(
                    Newtonsoft.Json.JsonConvert.DeserializeObject<BiliLite.Models.ApiResultModel<T>>(results));
            }
            catch (Exception ex)
            {
                LogHelper.Log("HTTP response parse failed endpoint=probe", LogType.ERROR, ex);
                return Task.FromResult<BiliLite.Models.ApiResultModel<T>>(null);
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

    /// <summary>产品 SeasonDetailVM 走到的两个 pgc/web 端点。</summary>
    public enum SeasonEndpoint
    {
        /// <summary>pgc/view/web/season：整页信息（标题/封面/评分/地区/追番状态）。</summary>
        Detail,

        /// <summary>pgc/view/web/ep/list：片单与分区。</summary>
        EpList,
    }

    /// <summary>
    /// pgc/view/web 的请求替身。产品代码用 new SeasonApi().Detail(...)/EpList(...)，所以这里登记调用并按夹具回包。
    /// Detail 与 EpList 的 URL/参数差别（season_id vs ep_id）由 <see cref="SeasonApiCall"/> 的 Type 记录，
    /// HTTP 层不复刻。
    /// </summary>
    public class SeasonApi
    {
        public sealed class SeasonApiCall
        {
            public SeasonEndpoint Endpoint { get; set; }
            public string Id { get; set; }
            public SeasonIdType Type { get; set; }
        }

        /// <summary>产品代码发出的每一次 pgc/view/web 请求，按顺序（Detail 在前，EpList 在后）。</summary>
        public static readonly List<SeasonApiCall> Calls = new List<SeasonApiCall>();

        /// <summary>夹具：默认给一个业务码拒绝，单元用例自己覆盖；按 call.Endpoint 分别回两个端点。</summary>
        public static Func<SeasonApiCall, string> Fixture = call => "{\"code\":-404,\"message\":\"啥都木有\"}";

        /// <summary>
        /// 让下一次 ep/list 请求像 HTTP 失败一样返回（status=false, code=404），用一次就复位。
        /// 只有 ep/list 的失败会落到页面的错误面板上；Detail 失败产品只记日志、照旧发布片单。
        /// </summary>
        public static bool FailNextEpListRequest;

        public static void Reset()
        {
            Calls.Clear();
            Fixture = call => "{\"code\":-404,\"message\":\"啥都木有\"}";
            FailNextEpListRequest = false;
        }

        public ApiModel Detail(string id, SeasonIdType type = SeasonIdType.SeasonId)
        {
            return Build(SeasonEndpoint.Detail, id, type);
        }

        public ApiModel EpList(string id, SeasonIdType type = SeasonIdType.SeasonId)
        {
            return Build(SeasonEndpoint.EpList, id, type);
        }

        private static ApiModel Build(SeasonEndpoint endpoint, string id, SeasonIdType type)
        {
            var call = new SeasonApiCall { Endpoint = endpoint, Id = id, Type = type };
            Calls.Add(call);
            return new ApiModel
            {
                Handler = () =>
                {
                    if (endpoint == SeasonEndpoint.EpList && FailNextEpListRequest)
                    {
                        FailNextEpListRequest = false;
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

namespace BiliLite.Models
{
    /// <summary>
    /// result 信封的产品形状（src/BiliLite.UWP/Models/ApiModel.cs）：code/message/msg/success/result。
    /// 产品文件没有被探针编译进去，这里只留 VM 读的三个成员（success/code/result）和 message 回落别名。
    /// </summary>
    public class ApiResultModel<T>
    {
        public int code { get; set; }
        private string _message;
        public string message
        {
            get { return string.IsNullOrEmpty(_message) ? msg : _message; }
            set { _message = value; }
        }
        public string msg { get; set; } = "";
        public bool success
        {
            get { return code == 0; }
        }
        public T result { get; set; }
    }
}

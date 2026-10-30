using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bilibili.App.Dynamic.V2;
using Newtonsoft.Json.Linq;

// Minimal stand-ins for the UWP surface the home DynamicVM class needs. The mapper and
// model closure under test (DynamicGrpcMapper.cs + Modules/Home/DynamicVM.cs) are real code.
namespace Windows.UI.Xaml
{
    public class DependencyObject
    {
    }

    public class ResourceDictionary
    {
        public object this[object key]
        {
            get { return null; }
            set { }
        }
    }
}

namespace Windows.UI.Xaml.Controls
{
    public class DataTemplate : Windows.UI.Xaml.DependencyObject
    {
    }

    public class DataTemplateSelector
    {
        protected virtual DataTemplate SelectTemplateCore(object item, Windows.UI.Xaml.DependencyObject container)
        {
            return null;
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
        FATAL
    }

    public static class LogHelper
    {
        // 探针捕获日志文本，用于断言兜底那条 INFO 确实先于换源打出
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
        public static void ShowMessageToast(string message, int seconds = 2)
        {
        }

        public static int ToInt32(this object obj)
        {
            if (obj == null) return 0;
            return int.TryParse(obj.ToString(), out var value) ? value : 0;
        }
    }

    /// <summary>REST reply stand-in: the probe hands the fixture JSON through <see cref="results"/>.</summary>
    public sealed class HttpResults
    {
        public int code { get; set; }
        public bool status { get; set; }
        public string message { get; set; }
        public string results { get; set; }

        public JObject GetJObject() => JObject.Parse(string.IsNullOrEmpty(results) ? "{}" : results);
    }
}

namespace BiliLite.Api
{
    /// <summary>REST request stand-in: product code only calls <see cref="Request"/>, the probe sets the handler.</summary>
    public class ApiModel
    {
        public Func<Task<BiliLite.Helpers.HttpResults>> Handler;

        public Task<BiliLite.Helpers.HttpResults> Request()
        {
            return Handler();
        }
    }
}

namespace BiliLite.Api.User
{
    public class DynamicAPI
    {
        public enum UserDynamicType
        {
            All = 0,
            Video = 1,
            Season = 2,
            Article = 3
        }

        /// <summary>One REST feed call the product code made, in order.</summary>
        public sealed class RestCall
        {
            public string Endpoint { get; set; }
            public UserDynamicType Type { get; set; }
            public string Cursor { get; set; }
        }

        // 夹具：REST 动态流不触网，由探针按 (endpoint, cursor) 返回固定的 JSON
        public static readonly List<RestCall> Calls = new List<RestCall>();
        public static Func<RestCall, string> Fixture = call => "{\"code\":0,\"data\":{\"cards\":[]}}";

        public ApiModel DyanmicNew(UserDynamicType type, string offset = null)
        {
            return Build(new RestCall { Endpoint = "dynamic_new", Type = type, Cursor = offset });
        }

        public ApiModel HistoryDynamic(string dynamic_id, UserDynamicType type)
        {
            return Build(new RestCall { Endpoint = "dynamic_history", Type = type, Cursor = dynamic_id });
        }

        private static ApiModel Build(RestCall call)
        {
            Calls.Add(call);
            var captured = call;
            return new ApiModel
            {
                Handler = () => Task.FromResult(new BiliLite.Helpers.HttpResults
                {
                    status = true,
                    message = "0",
                    results = Fixture(captured),
                }),
            };
        }
    }
}

namespace BiliLite.Services
{
    public class GrpcService
    {
        public const string DynVideoRejectedMessage =
            "请求失败,HTTP 200,服务端拒绝: grpc-status=2; grpc-message=-663; bili-status-code=-663,responseBytes=0";

        // 探针控制位：首页动态的 gRPC 请求可以被拒，且调用次数可断言
        public static int DynVideoCalls;
        public static bool RejectDynVideo;

        public Task<DynVideoReply> GetDynVideo(int page, string historyOffset, string updateBaseline)
        {
            DynVideoCalls++;
            if (RejectDynVideo)
            {
                throw new Exception(DynVideoRejectedMessage);
            }
            // 真实的成功回复一定带 dynamic_list；空消息体会让产品代码在读 List 时失败
            var reply = new DynVideoReply { DynamicList = new CardVideoDynList() };
            return Task.FromResult(reply);
        }

        //Space 分支新增的取数入口：这里的替身只让 Home/DynamicVM.cs 的模型闭包可编译，
        //真正的形状由 Program.cs 对 Services/GrpcService.cs 源码的断言约束。
        public Task<DynSpaceRsp> GetDynSpace(long mid, string from = "space", int page = 1, string offset = null)
        {
            return Task.FromResult(new DynSpaceRsp());
        }
    }
}

namespace BiliLite.Modules
{
    public class AnimeHomeModel
    {
    }
}

namespace BiliLite.Services
{
    /// <summary>
    /// 探针替身:真实实现写应用本地的动态来源历史快照(Windows.Storage),探针里只保留内存语义。
    /// </summary>
    internal sealed class DynamicApiHistoryFileStore : IDynamicApiHistoryStore
    {
        private readonly InMemoryDynamicApiHistoryStore _inner = new InMemoryDynamicApiHistoryStore();

        public DynamicApiBucketState Read(string bucketKey)
        {
            return _inner.Read(bucketKey);
        }

        public void Write(DynamicApiBucketState bucket)
        {
            _inner.Write(bucket);
        }
    }
}

namespace BiliLite.Helpers
{
    /// <summary>探针替身:选择器接线读取当前账号与 app key,这里给出匿名只读实现。</summary>
    public static class SettingHelper
    {
        public static class Account
        {
            public static bool Logined { get { return false; } }
            public static long UserID { get { return 0L; } }
            public static string AccessKey { get { return ""; } }
        }
    }

    /// <summary>探针替身:选择器接线读取两个 app key。</summary>
    public static class ApiHelper
    {
        public sealed class Key
        {
            public string Appkey { get; set; }
            public Key(string appkey) { Appkey = appkey; }
        }

        public static Key LoginKey { get { return new Key("probe-login"); } }
        public static Key AndroidKey { get { return new Key("probe-android"); } }
    }
}

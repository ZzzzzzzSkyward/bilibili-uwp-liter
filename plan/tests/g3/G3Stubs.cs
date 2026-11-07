using System;
using System.Threading.Tasks;
using Bilibili.App.Dynamic.V2;

// Minimal stand-ins for the UWP surface the home DynamicVM class needs. The mapper and
// model closure under test (DynamicGrpcMapper.cs + Modules/Home/DynamicVM.cs + Controls/Dynamic/DisplayModel.cs)
// are real code.
namespace Windows.UI.Xaml
{
    public class DependencyObject
    {
    }

    /// <summary>探针替身:DisplayModel 的标题槽/祖先行只比较这个枚举的两个值。</summary>
    public enum Visibility
    {
        Visible,
        Collapsed,
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

    /// <summary>探针替身:显示模型持有 RichTextBlock，但探针不渲染它。</summary>
    public class RichTextBlock : Windows.UI.Xaml.DependencyObject
    {
    }
}

namespace BiliLite.Controls.Dynamic
{
    /// <summary>
    /// 探针替身:显示类型枚举的真正定义在 Controls/Dynamic/DynamicParse.cs（依赖整片 WinRT 文本栈，
    /// 探针编译不了）。这里只保留显示模型判断宿主归属与标题槽需要的那几个成员。
    /// </summary>
    public enum DynamicDisplayType
    {
        Repost,
        Text,
        Photo,
        Video,
        ShortVideo,
        Season,
        Music,
        Web,
        Article,
        Live,
        LiveShare,
        Cheese,
        MediaList,
        Miss,
        Reserve,
        Other,
        CustomSeason,
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
        public static void Log(string message, LogType type, Exception ex = null)
        {
        }

        public static bool IsNetworkError(Exception ex)
        {
            return false;
        }
    }

    public static partial class Utils
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
}

namespace BiliLite.Services
{
    public class GrpcService
    {
        public Task<DynVideoReply> GetDynVideo(int page, string historyOffset, string updateBaseline)
        {
            return Task.FromResult(new DynVideoReply());
        }
    }
}

namespace BiliLite.Modules
{
    public class AnimeHomeModel
    {
    }
}

// Home/DynamicVM.cs also carries the home REST fallback; g3/g5 compile only its model closure,
// so these stand-ins keep that reference compiling without touching the network.
namespace BiliLite.Helpers
{
    public sealed class HttpResults
    {
        public bool status { get; set; }
        public string message { get; set; }
        public int code { get; set; }
        /// <summary>响应正文；ApiEnvelope 只通过 TryGetJObject 读它。</summary>
        public string results { get; set; }

        /// <summary>与 Helpers/HttpHelper.cs 的静默解码同义：正文不是 JSON 对象就返回 false。</summary>
        public bool TryGetJObject(out Newtonsoft.Json.Linq.JObject value)
        {
            value = null;
            if (string.IsNullOrEmpty(results))
            {
                return false;
            }
            try
            {
                value = Newtonsoft.Json.Linq.JObject.Parse(results);
                return true;
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return false;
            }
        }

        public Newtonsoft.Json.Linq.JObject GetJObject()
        {
            return Newtonsoft.Json.Linq.JObject.Parse("{}");
        }
    }
}

namespace BiliLite.Helpers
{
    /// <summary>探针替身:显示模型给头像/认证字段的默认透明图常量。</summary>
    public static class AppHelper
    {
        public const string TRANSPARENT_IMAGE = "ms-appx:///Assets/MiniIcon/transparent.png";
        public const string VERIFY_PERSONAL_IMAGE = "ms-appx:///Assets/Icon/verify0.png";
        public const string VERIFY_OGANIZATION_IMAGE = "ms-appx:///Assets/Icon/verify1.png";
    }
}

namespace BiliLite.Api
{
    public class ApiModel
    {
        public System.Threading.Tasks.Task<BiliLite.Helpers.HttpResults> Request()
        {
            return System.Threading.Tasks.Task.FromResult(new BiliLite.Helpers.HttpResults());
        }
    }
}

namespace BiliLite.Api.User
{

    /// <summary>产品代码里的请求构造是静态类，替身转发到 DynamicAPI 的寄存器。</summary>
    public static class WebDynamicFeedApi
    {
        public static ApiModel CreateFeedRequest(int page = 1, string offset = null, string type = "all")
        {
            return DynamicAPI.BuildFeed(page, offset, type);
        }
    }
    public class DynamicAPI
    {
        public enum UserDynamicType
        {
            All = 0,
            Video = 1,
            Season = 2,
            Article = 3
        }

        public ApiModel DyanmicNew(UserDynamicType type, string offset = null)
        {
            return new ApiModel();
        }

        public ApiModel HistoryDynamic(string dynamic_id, UserDynamicType type)
        {
            return new ApiModel();
        }

        /// <summary>产品代码走 WebDynamicFeedApi.CreateFeedRequest；替身只在这里登记一次 feed 调用。</summary>
        internal static ApiModel BuildFeed(int page, string offset, string type)
        {
            // 这个探针不跑聚合流的翻页：只要请求对象构造得出来。
            return new ApiModel();
        }
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

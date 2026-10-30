using System;
using System.Threading.Tasks;
using Bilibili.App.Dynamic.V2;

// Minimal stand-ins for the UWP surface the real files under test need. The mapper,
// the new repost mapper, the 2024 card models and the display model are product source.
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

    public enum Visibility
    {
        Visible,
        Collapsed,
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

    public class RichTextBlock : Windows.UI.Xaml.DependencyObject
    {
    }
}

namespace BiliLite.Controls.Dynamic
{
    public enum DynamicDisplayType
    {
        Repost,
        Text,
        Photo,
        Video,
        ShortVideo,
        Music,
        Web,
        Article,
        Live,
        LiveShare,
        Reserve,
        Miss,
        Other,
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

        public static string HandelTimestamp(string ts)
        {
            return "relative:" + ts;
        }

        public static DateTime TimestampToDatetime(long ts)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(ts);
        }
    }

    public static class AppHelper
    {
        public const string TRANSPARENT_IMAGE = "ms-appx:///Assets/MiniIcon/transparent.png";
        public const string VERIFY_PERSONAL_IMAGE = "ms-appx:///Assets/Icon/verify0.png";
        public const string VERIFY_OGANIZATION_IMAGE = "ms-appx:///Assets/Icon/verify1.png";
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

        public Newtonsoft.Json.Linq.JObject GetJObject()
        {
            return Newtonsoft.Json.Linq.JObject.Parse("{}");
        }
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

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

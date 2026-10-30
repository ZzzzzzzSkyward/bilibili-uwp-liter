using System;
using System.Threading.Tasks;
using Bilibili.App.Dynamic.V2;

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

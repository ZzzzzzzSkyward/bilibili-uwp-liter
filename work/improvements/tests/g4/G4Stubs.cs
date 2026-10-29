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

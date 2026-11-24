using System;
using System.Collections.Concurrent;

namespace BiliLite.Models { public sealed class MyProfileModel { } }
namespace BiliLite.Controls
{
    public sealed class PlayInfo { public int season_id; public int ep_id; public string cid; }
}
namespace BiliLite.Helpers
{
    public enum LogType { ERROR }
    public static class LogHelper
    {
        public static readonly ConcurrentQueue<Exception> Errors = new ConcurrentQueue<Exception>();
        public static void Log(string message, LogType type, Exception exception) => Errors.Enqueue(exception);
    }
}
namespace Microsoft.Toolkit.Uwp.Helpers
{
    // Primitive key/string store only. Real SettingHelper owns JSON and fallback behavior.
    public sealed class ApplicationDataStorageHelper
    {
        public readonly ConcurrentDictionary<string, string> Values = new ConcurrentDictionary<string, string>();
        public Action<string, string> BeforeSave;
        public Action<string, string> AfterSave;
        public static ApplicationDataStorageHelper GetCurrent() => new ApplicationDataStorageHelper();
        public bool KeyExists(string key) => Values.ContainsKey(key);
        public T Read<T>(string key) => (T)(object)Values[key];
        public void Save<T>(string key, T value)
        {
            var text = (string)(object)value;
            BeforeSave?.Invoke(key, text);
            Values[key] = text;
            AfterSave?.Invoke(key, text);
        }
    }
}

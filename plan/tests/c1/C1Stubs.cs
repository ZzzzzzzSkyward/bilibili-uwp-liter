using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

namespace BiliLite.Helpers
{
    /// <summary>
    /// REST reply stand-in: the probe hands fixture JSON through <see cref="results"/>.
    /// Mirrors Helpers/HttpHelper.cs:499 only far enough for the parse layer: transport flag,
    /// business message, and the silent JSON decode the parsers use.
    /// </summary>
    public sealed class HttpResults
    {
        public int code { get; set; }
        public bool status { get; set; }
        public string message { get; set; }
        public string results { get; set; }

        public bool TryGetJObject(out JObject value)
        {
            try
            {
                value = JObject.Parse(results ?? string.Empty);
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }
    }

    /// <summary>
    /// The parse layer must never reach the UI. Nothing under test calls this, so the probe's
    /// Toasts list stays empty; that is the executed half of "silent, no toast".
    /// </summary>
    public static class Utils
    {
        public static readonly List<string> Toasts = new List<string>();

        public static void ShowMessageToast(string message, int seconds = 2)
        {
            Toasts.Add(message);
        }
    }
}

namespace BiliLite.Modules
{
    /// <summary>
    /// The product IModules also carries the error-reporting helper, which needs the UWP logging
    /// stack. The detail models only use the change notification part, so that part is stubbed.
    /// </summary>
    public class IModules : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void DoPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// Mirrors the property set of FavoriteItemModel (Modules/User/MyFollowVideoVM.cs:218, namespace
    /// BiliLite.Modules), which cannot be compiled outside UWP. Only the fields the favourite-list
    /// mapper copies are repeated; the derived privacy/is_fav members are left out on purpose so the
    /// probe never asserts stub logic.
    /// </summary>
    public class FavoriteItemModel
    {
        public string cover { get; set; }
        public int attr { get; set; }
        public string intro { get; set; }
        public string fid { get; set; }
        public string id { get; set; }
        public int like_state { get; set; }
        public string mid { get; set; }
        public string title { get; set; }
        public int type { get; set; }
        public int media_count { get; set; }
        public int fav_state { get; set; }
    }
}

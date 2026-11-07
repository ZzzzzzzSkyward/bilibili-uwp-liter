using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BiliLite.Models.Common;
using Newtonsoft.Json.Linq;

// Stand-ins for the UWP/API surface the comment parse path needs. The code under test
// (Parsing/ApiEnvelope.cs, Parsing/Comment/*, Models/Api/Comment/*, Models/Common.cs) is the real
// product source; only the transport and the app services are replaced here.
//
// The page models themselves live in Controls/CommentControl.xaml.cs (a UWP UserControl, not
// compilable outside the UWP toolchain), so they are mirrored below member for member: every member
// the mapper writes, plus the derived members the probe reads. If the mapper starts writing a member
// that does not exist, the product build is what catches it — this mirror only has to stay faithful
// for the assertions.
namespace Windows.UI.Xaml
{
    public enum Visibility
    {
        Visible = 0,
        Collapsed = 1,
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
        /// <summary>解析层一笔记了日志就算违规：探针拿它断言解析路径不写日志。</summary>
        public static readonly List<string> Entries = new List<string>();

        public static void Log(string message, LogType type, Exception ex = null)
        {
            Entries.Add(message);
        }
    }

    public static class Utils
    {
        /// <summary>弹过的提示文本，按顺序；探针拿它断言解析层一条都不弹。</summary>
        public static readonly List<string> Toasts = new List<string>();

        public static void ShowMessageToast(string message, int seconds = 2)
        {
            Toasts.Add(message);
        }

        public static void ShowMessageToast(string message, string longmsg, int seconds = 10)
        {
            Toasts.Add(message);
        }
    }

    /// <summary>
    /// REST reply stand-in: the probe hands the fixture JSON through <see cref="results"/>.
    /// <see cref="TryGetJObject"/> 是产品的静默版（解不出来返回 false，不弹提示），也是解析层用的那个。
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
            catch (Exception)
            {
                value = null;
                return false;
            }
        }
    }
}

namespace BiliLite.Controls
{
    using Windows.UI.Xaml;

    /// <summary>镜像 Controls/CommentControl.xaml.cs 的 CommentModel（只列 mapper 会写、探针会读的那些）。</summary>
    public class CommentModel
    {
        public int action { get; set; }
        public long rpid { get; set; }
        public string rpid_str { get; set; }
        public long oid { get; set; }
        public int type { get; set; }
        public long mid { get; set; }
        public long root { get; set; }
        public long parent { get; set; }
        public int count { get; set; }
        public int rcount { get; set; }
        public int like { get; set; }
        public int floor { get; set; }
        public int state { get; set; }
        public long ctime { get; set; }

        public string rcount_str
        {
            get { return rcount > 10000 ? ((double)rcount / 10000).ToString("0.0") + "万" : rcount.ToString(); }
        }

        public string like_str
        {
            get { return like > 10000 ? ((double)like / 10000).ToString("0.0") + "万" : like.ToString(); }
        }

        public CommentMemberModel member { get; set; }
        public CommentContentModel content { get; set; }
        public CommentUPActionModel up_action { get; set; }
        public CommentReplyControlModel reply_control { get; set; }

        public ObservableCollection<CommentModel> replies { get; set; } = new ObservableCollection<CommentModel>();

        public Visibility showTop { get; set; } = Visibility.Collapsed;

        public int loadpage { get; set; } = 1;
        public Visibility showReplyMore { get; set; } = Visibility.Collapsed;
        public Visibility showLoading { get; set; } = Visibility.Collapsed;
        public string replyText { get; set; }
    }

    public class CommentReplyControlModel
    {
        public string time_desc { get; set; }
        public string location { get; set; }
    }

    public class CommentContentModel
    {
        public List<NotePicture> pictures { get; set; }

        public Visibility haspictures
        {
            get
            {
                return pictures != null && pictures.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public string message { get; set; }
        public int plat { get; set; }
        public string device { get; set; }
        public JObject emote { get; set; }

        public string plat_str
        {
            get
            {
                switch (plat)
                {
                    case 2:
                        return "来自 Android";
                    case 3:
                        return "来自 IOS";
                    case 4:
                        return "来自 WindowsPhone";
                    case 6:
                        return "来自 Windows";
                    default:
                        return "";
                }
            }
        }
    }

    public class CommentUPActionModel
    {
        public bool like { get; set; } = false;
        public bool reply { get; set; }
    }

    /// <summary>同一个类在线上既当 member，也当 pendant / official_verify / vip / level_info 的容器。</summary>
    public class CommentMemberModel
    {
        public string mid { get; set; }
        public string uname { get; set; }
        public string sex { get; set; }
        public string sign { get; set; }

        private string _avatar;
        public string avatar
        {
            get { return _avatar; }
            set { _avatar = value + "@64w_64h.jpg"; }
        }

        public CommentMemberModel level_info { get; set; }
        public CommentMemberFansDetailModel fans_detail { get; set; }
        public CommentMemberUserSailingModel user_sailing { get; set; }

        public bool ShowFansDetail
        {
            get { return fans_detail != null; }
        }

        public bool ShowCardBg
        {
            get { return user_sailing != null && user_sailing.cardbg != null && user_sailing.cardbg.fan != null; }
        }

        public int current_level { get; set; }

        public string pendant_str
        {
            get { return pendant == null || pendant.image == null ? "" : pendant.image; }
        }

        public CommentMemberModel pendant { get; set; }
        public long pid { get; set; }
        public string name { get; set; }
        public string image { get; set; }

        public CommentMemberModel official_verify { get; set; }
        public int type { get; set; }
        public string desc { get; set; }

        public CommentMemberModel vip { get; set; }
        public int vipType { get; set; }
    }

    public class CommentMemberFansDetailModel
    {
        public long uid { get; set; }
        public int medal_id { get; set; }
        public string medal_name { get; set; }
        public int level { get; set; }
        public long medal_color { get; set; }
        public long medal_color_end { get; set; }
        public long medal_color_border { get; set; }
        public long medal_color_name { get; set; }
        public long medal_color_level { get; set; }
    }

    public class CommentMemberUserSailingModel
    {
        public CommentMemberUserSailingPendantModel pendant { get; set; }
        public CommentMemberUserSailingCardbgModel cardbg { get; set; }
    }

    public class CommentMemberUserSailingPendantModel
    {
        public long id { get; set; }
        public string name { get; set; }
        public string image { get; set; }
    }

    public class CommentMemberUserSailingCardbgModel
    {
        public long id { get; set; }
        public string name { get; set; }
        public string image { get; set; }
        public string jump_url { get; set; }

        public bool show
        {
            get { return fan != null && !string.IsNullOrEmpty(fan.num_desc); }
        }

        public CommentMemberUserSailingCardbgFanModel fan { get; set; }
    }

    public class CommentMemberUserSailingCardbgFanModel
    {
        public int is_fan { get; set; }
        public int number { get; set; }
        public string color { get; set; }
        public string name { get; set; }
        public string num_desc { get; set; }
    }
}

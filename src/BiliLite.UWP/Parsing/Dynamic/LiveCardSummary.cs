using System.Text.RegularExpressions;
using BiliLite.Modules;

namespace BiliLite.Parsing.Dynamic
{
    /// <summary>
    /// 直播卡的开播状态。拿不到 live_state 时是 Unknown——"没说过"不等于"已结束"。
    /// </summary>
    public enum DynamicLiveState
    {
        Unknown,
        Live,
        Ended,
    }

    /// <summary>
    /// 动态里的直播卡只有一行：房间号、点开去哪儿、地区·人气、封面角标。
    /// 三种来源都归到这里——旧接口的 live_play_info（调用方自己取字段）、新接口的 major.live、
    /// 以及 major.live_rcmd 解出来的 live_play_info（两种 Mapper 已填进 major.live）——
    /// 显示层不再自己猜"哪个字段像房间号"。缺字段就少一行信息，绝不补占位文案。
    /// </summary>
    public sealed class LiveCardSummary
    {
        public const string EndedBadge = "直播已结束";

        private LiveCardSummary()
        {
        }

        /// <summary>直播间号；不是纯数字就当没有（aid/bvid 不是房间号）。</summary>
        public string RoomId { get; private set; }

        /// <summary>点开目标：能落到 MessageCenter 的 live(\d+) 上；拼不出来就是空串。</summary>
        public string Destination { get; private set; }

        /// <summary>地区名与观看人数，取到的部分用 " · " 连起来；都取不到就是空串。</summary>
        public string Subtitle { get; private set; }

        public DynamicLiveState State { get; private set; }

        /// <summary>封面角标：只有确认已结束才写字。</summary>
        public string CoverText { get; private set; }

        public string Title { get; private set; }

        public string Cover { get; private set; }

        /// <summary>
        /// 直播载荷 + 卡片自己的标题/封面（新接口把两者都挂在 major.live 上，但缺字段时要退回卡片预览）。
        /// live 可以是 null：这时只剩卡片的标题与封面。
        /// </summary>
        public static LiveCardSummary From(DynamicCardModel2024_live live, string fallbackTitle, string fallbackCover)
        {
            var roomId = DigitsOnly(live?.id);
            var state = StateOf(live?.live_state);
            return new LiveCardSummary
            {
                RoomId = roomId,
                Destination = DestinationOf(live?.jump_url, roomId),
                Subtitle = JoinParts(live?.desc_first, live?.desc_second),
                State = state,
                CoverText = state == DynamicLiveState.Ended ? EndedBadge : "",
                Title = FirstNonEmpty(live?.title, fallbackTitle),
                Cover = FirstNonEmpty(live?.cover, fallbackCover),
            };
        }

        private static DynamicLiveState StateOf(int? liveState)
        {
            switch (liveState)
            {
                case 1:
                    return DynamicLiveState.Live;
                case 0:
                    return DynamicLiveState.Ended;
                default:
                    return DynamicLiveState.Unknown;
            }
        }

        private static string DestinationOf(string jumpUrl, string roomId)
        {
            // 相对路径没有可用的来源基址：宁可回头用房间号拼，也不要拼出一个点不动的网页地址。
            var url = WireScalars.NormalizeWebUrl(jumpUrl, null);
            if (RoutesToRoom(url))
            {
                return url;
            }

            return roomId.Length == 0 ? "" : "https://live.bilibili.com/" + roomId;
        }

        /// <summary>
        /// 与 MessageCenter.HandleUrl 的直播分支同一套判断：把地址归一成 live+房间号。
        /// 落不到那里的地址（短链、别的站）不算目的地——短链藏着房间号，我们手里已经有真的。
        /// </summary>
        private static bool RoutesToRoom(string url)
        {
            if (url.Length == 0 || url.IndexOf("b23.tv", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            var probe = url.Replace("h5", "live").Replace("live.bilibili.com", "live").Replace("/", "");
            return Regex.IsMatch(probe, "live[0-9]+");
        }

        private static string JoinParts(string first, string second)
        {
            var area = (first ?? "").Trim();
            var watched = (second ?? "").Trim();
            if (area.Length == 0)
            {
                return watched;
            }

            return watched.Length == 0 ? area : area + " · " + watched;
        }

        private static string FirstNonEmpty(string preferred, string fallback)
        {
            var value = (preferred ?? "").Trim();
            return value.Length > 0 ? value : (fallback ?? "").Trim();
        }

        private static string DigitsOnly(string value)
        {
            var text = (value ?? "").Trim();
            if (text.Length == 0)
            {
                return "";
            }

            foreach (var c in text)
            {
                if (c < '0' || c > '9')
                {
                    return "";
                }
            }

            return text;
        }
    }
}

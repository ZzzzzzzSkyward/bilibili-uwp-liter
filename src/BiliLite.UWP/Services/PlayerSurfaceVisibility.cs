using System.Collections.Generic;

namespace BiliLite.Services
{
    /// <summary>
    /// 播放器已经有的信号。这里只放数字和真假,不放控件。
    /// </summary>
    public sealed class PlayerSurfaceSignals
    {
        /// <summary>当前播放列表的分P/视频数。</summary>
        public int PlaylistCount { get; set; }

        /// <summary>当前这一集是互动视频。</summary>
        public bool IsInteraction { get; set; }

        public int VideoTrackCount { get; set; }
        public int AudioTrackCount { get; set; }
        public int SubtitleTrackCount { get; set; }
        public int DanmakuTrackCount { get; set; }

        /// <summary>当前这一集已经拿到的弹幕条数(在线、下载、本地文件、导入的都算)。</summary>
        public int DanmakuRowCount { get; set; }

        /// <summary>当前这一集有本地弹幕文件(下载自带的弹幕 XML 或合集里的弹幕轨道)。</summary>
        public bool HasLocalDanmakuFile { get; set; }

        /// <summary>当前这一集的弹幕已经加载过一次,成功或失败都算。</summary>
        public bool DanmakuLoadSettled { get; set; }
    }

    /// <summary>
    /// 每个入口去还是留。true = 显示。
    /// </summary>
    public sealed class PlayerSurfaceVerdicts
    {
        public bool ShowDanmakuList { get; set; }
        public bool ShowPlaylistButton { get; set; }
        public bool ShowPlayNodeButton { get; set; }
        public bool ShowEpisodePivot { get; set; }
        public bool ShowLocalTracksPivot { get; set; }
    }

    /// <summary>
    /// 播放器浮层的去留:入口指向的内容不存在就不占位置。判定只看播放器手里已有的信号,
    /// 不碰 WinRT,net8.0 探针直接编译这一份。
    /// </summary>
    public static class PlayerSurfaceVisibility
    {
        /// <summary>没有任何可见标签时返回这个值,调用方保持原样。</summary>
        public const int NoVisiblePivot = -1;

        public static PlayerSurfaceVerdicts Decide(PlayerSurfaceSignals signals)
        {
            var hasPlaylist = signals != null && signals.PlaylistCount > 1;
            var isInteraction = signals != null && signals.IsInteraction;
            return new PlayerSurfaceVerdicts
            {
                ShowDanmakuList = HasDanmaku(signals),
                // 互动视频用节点按钮顶替选集:同一个标签里换成节点列表
                ShowPlaylistButton = hasPlaylist && !isInteraction,
                ShowPlayNodeButton = isInteraction,
                ShowEpisodePivot = hasPlaylist || isInteraction,
                ShowLocalTracksPivot = HasLocalTrack(signals),
            };
        }

        /// <summary>
        /// 有行就开;本地弹幕文件还没解析完也算有(文件在,列表迟早会有),解析完还是空就是真没有。
        /// 第一次结果出来之前既没有行也没有本地文件,保持藏。
        /// </summary>
        private static bool HasDanmaku(PlayerSurfaceSignals signals)
        {
            if (signals == null) return false;
            if (signals.DanmakuRowCount > 0) return true;
            return signals.HasLocalDanmakuFile && !signals.DanmakuLoadSettled;
        }

        private static bool HasLocalTrack(PlayerSurfaceSignals signals)
        {
            if (signals == null) return false;
            return signals.VideoTrackCount + signals.AudioTrackCount + signals.SubtitleTrackCount + signals.DanmakuTrackCount > 0;
        }

        /// <summary>
        /// 选中的标签刚被藏起来就换到兜底标签(弹幕),兜底也看不见就换到第一个还看得见的;
        /// 一个都看不见时返回 NoVisiblePivot,由调用方决定怎么收场。
        /// </summary>
        public static int ResolvePivotSelection(IReadOnlyList<bool> visible, int selectedIndex, int fallbackIndex)
        {
            if (visible == null || visible.Count == 0) return NoVisiblePivot;
            if (selectedIndex >= 0 && selectedIndex < visible.Count && visible[selectedIndex]) return selectedIndex;
            if (fallbackIndex >= 0 && fallbackIndex < visible.Count && visible[fallbackIndex]) return fallbackIndex;
            for (var i = 0; i < visible.Count; i++)
            {
                if (visible[i]) return i;
            }
            return NoVisiblePivot;
        }
    }
}

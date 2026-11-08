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

        /// <summary>互动视频已经拿到的节点数(story_list 的条数)。</summary>
        public int NodeCount { get; set; }

        /// <summary>当前是合集(本地 manifest)播放:视频、音频轨道的下拉只有这种播放方式才填。</summary>
        public bool IsManifestPlayback { get; set; }

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
        public bool ShowPlayNodeButton { get; set; }
        public bool ShowNodePivot { get; set; }
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
            var isInteraction = signals != null && signals.IsInteraction;
            // 节点树是异步取的:列表还没到就没什么可点,标签和按钮一起留着就是空壳
            var hasNode = isInteraction && signals.NodeCount > 0;
            return new PlayerSurfaceVerdicts
            {
                ShowDanmakuList = HasDanmaku(signals),
                // 分P/合集/播放列表都在播放目录里,播放器侧栏只留互动视频的节点树
                ShowPlayNodeButton = hasNode,
                ShowNodePivot = hasNode,
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

        /// <summary>
        /// 视频、音频轨道的下拉只有合集(manifest)播放时才填,别的播放方式拿到轨道数也点不开,
        /// 所以这两项只在 manifest 下算数;字幕、弹幕轨道不靠下拉,有就算。
        /// </summary>
        private static bool HasLocalTrack(PlayerSurfaceSignals signals)
        {
            if (signals == null) return false;
            var selectableTrackCount = signals.IsManifestPlayback
                ? signals.VideoTrackCount + signals.AudioTrackCount
                : 0;
            return selectableTrackCount + signals.SubtitleTrackCount + signals.DanmakuTrackCount > 0;
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

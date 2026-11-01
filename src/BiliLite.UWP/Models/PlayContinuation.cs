using System;

namespace BiliLite.Models.Common
{
    /// <summary>
    /// 换到某个目标时从哪开始播。续播=目标自己保存的进度,从头=0。
    /// 每个换目标的入口都要明说,不能让上一次的续播状态顺下来。
    /// </summary>
    public enum PlayTargetStart
    {
        /// <summary>
        /// 沿用目标保存的进度:直接打开、用户自己点的行/分P、往回(上一个)
        /// </summary>
        Resume = 0,
        /// <summary>
        /// 从 0 开始:往前走的续播移动(播放结束的自动连播、下一个、下一分P)
        /// </summary>
        FromStart = 1,
    }

    /// <summary>
    /// 连播的两个策略决定:自动边界要不要真的推进,换到目标后从哪开始播。
    /// 分P边界和视频边界读同一个开关,不另立第二份判断。
    /// </summary>
    public static class PlayContinuationPolicy
    {
        /// <summary>
        /// 播放结束的自动边界要不要继续推进。"自动连播"关着时停在当前这一P/这一条;
        /// 用户自己按的"下一个/上一个"不受这个开关约束。
        /// </summary>
        public static bool AdvanceAllowed(bool manual, bool autoNextEnabled)
        {
            return manual || autoNextEnabled;
        }

        /// <summary>
        /// 目标要不要跳到它保存的进度:只有"续播"这个选择在 AUTO_TO_POSITION 打开时才算数,
        /// 往前走的续播移动一律从 0 开始。
        /// </summary>
        public static bool RestoresPosition(PlayTargetStart start, bool autoToPositionEnabled)
        {
            return start == PlayTargetStart.Resume && autoToPositionEnabled;
        }
    }

    /// <summary>
    /// 一次换目标的票:发起时的视频 id、播放列表 id 和代数。
    /// 结果回来时三项都还一样才算属于现在这一拍;任何一项变了,晚到的结果都不作数。
    /// </summary>
    public struct PlayTransitionTicket
    {
        internal PlayTransitionTicket(string videoId, string mediaListId, long generation)
        {
            VideoId = videoId ?? "";
            MediaListId = mediaListId ?? "";
            Generation = generation;
        }

        public string VideoId { get; private set; }

        public string MediaListId { get; private set; }

        public long Generation { get; private set; }
    }

    /// <summary>
    /// 重试取页之后该做什么
    /// </summary>
    public enum PlayAdvanceResume
    {
        /// <summary>
        /// 没有断在这里的换目标,或者记录已经不是原身份的了
        /// </summary>
        None = 0,
        /// <summary>
        /// 记录还在、身份也没变:接着把断掉的那一步走完
        /// </summary>
        Resume = 1,
        /// <summary>
        /// 列表已经到头:这一次推进取消,记录丢掉
        /// </summary>
        Cancel = 2,
        /// <summary>
        /// 取页又失败了:记录留着,下一次重试还能接着走
        /// </summary>
        WaitForRetry = 3,
    }

    /// <summary>
    /// 断在取页上的那一步。记的是"本来要往哪走":哪个来源、从哪一行出发、跳过不跳过剩下的分P、
    /// 是不是用户自己按的;身份三项(VideoId/MediaListId/Generation)决定它还作不作数。
    /// </summary>
    public sealed class PlayPendingAdvance
    {
        public string VideoId { get; set; }

        public string MediaListId { get; set; }

        public long Generation { get; set; }

        /// <summary>
        /// 当时跟着哪个外层来源连播
        /// </summary>
        public WatchSequenceSourceKind Source { get; set; }

        /// <summary>
        /// 推进的起点:当时所在的那一行
        /// </summary>
        public int RowIndex { get; set; }

        public bool SkipRemainingParts { get; set; }

        /// <summary>
        /// 这一步是用户按的还是自动边界触发的:重试之后照原样走,不重新猜
        /// </summary>
        public bool Manual { get; set; }
    }

    /// <summary>
    /// 换目标的在途状态:谁在等、等的是哪一行、有没有一步断在取页上。
    /// 一次只允许一拍在路上:第二次请求直接丢掉,不排队。
    /// 身份是"视频 + 播放列表 + 代数",页面据此丢掉晚到的详情、取页和起播结果。
    /// </summary>
    public sealed class PlayTransitionState
    {
        private long m_generation;
        private bool m_pending;
        private string m_pendingTitle;
        private string m_pendingKey;
        private PlayPendingAdvance m_advance;

        public PlayTransitionState()
        {
            VideoId = "";
            MediaListId = "";
            m_pendingTitle = "";
            m_pendingKey = "";
        }

        /// <summary>
        /// 正在播的视频 id。换目标的结果回来时它得还是发起时那一个
        /// </summary>
        public string VideoId { get; private set; }

        /// <summary>
        /// 正在播的播放列表 id。固定内存队列是空串
        /// </summary>
        public string MediaListId { get; private set; }

        public long Generation
        {
            get { return m_generation; }
        }

        /// <summary>
        /// 有一次换目标还在路上:再来的下一个/上一个和目录换行都丢掉
        /// </summary>
        public bool IsPending
        {
            get { return m_pending; }
        }

        /// <summary>
        /// 等待中的目标文字(直接显示,不加前缀)和它在目录里的行身份
        /// </summary>
        public string PendingTitle
        {
            get { return m_pendingTitle; }
        }

        public string PendingKey
        {
            get { return m_pendingKey; }
        }

        public PlayPendingAdvance PendingAdvance
        {
            get { return m_advance; }
        }

        /// <summary>
        /// 现在这一拍的身份。请求发起时拿一张,结果回来时再对一次。
        /// </summary>
        public PlayTransitionTicket Ticket()
        {
            return new PlayTransitionTicket(VideoId, MediaListId, m_generation);
        }

        public bool IsCurrent(PlayTransitionTicket ticket)
        {
            return ticket.Generation == m_generation
                && string.Equals(ticket.VideoId ?? "", VideoId ?? "", StringComparison.Ordinal)
                && string.Equals(ticket.MediaListId ?? "", MediaListId ?? "", StringComparison.Ordinal);
        }

        /// <summary>
        /// 开始一次换目标。已经有一拍在路上时返回 false:调用方丢掉这一拍,不排队。
        /// </summary>
        public bool Begin(string targetTitle, string pendingKey)
        {
            if (m_pending) return false;
            m_pending = true;
            m_pendingTitle = targetTitle ?? "";
            m_pendingKey = pendingKey ?? "";
            m_generation++;
            return true;
        }

        /// <summary>
        /// 同一次换目标换了目标(先取页、取到后才知道会去哪一行):只改等待的文字,不改代数
        /// </summary>
        public bool Retitle(string targetTitle, string pendingKey)
        {
            if (!m_pending) return false;
            m_pendingTitle = targetTitle ?? "";
            m_pendingKey = pendingKey ?? "";
            return true;
        }

        /// <summary>
        /// 这一拍结束了(起播或失败):等待态收掉。代数不动——
        /// 起播换身份由 Commit 负责,失败则说明这一拍的结果已经用掉了。
        /// </summary>
        public void Settle()
        {
            m_pending = false;
            m_pendingTitle = "";
            m_pendingKey = "";
        }

        /// <summary>
        /// 播放换了视频(或换了播放列表):身份变了,代数加一,
        /// 等待态和断掉的那一步都属于旧身份,一起丢掉。
        /// </summary>
        public void Commit(string videoId, string mediaListId)
        {
            var changed = !string.Equals(videoId ?? "", VideoId ?? "", StringComparison.Ordinal)
                || !string.Equals(mediaListId ?? "", MediaListId ?? "", StringComparison.Ordinal);
            VideoId = videoId ?? "";
            MediaListId = mediaListId ?? "";
            if (!changed) return;
            m_generation++;
            m_pending = false;
            m_pendingTitle = "";
            m_pendingKey = "";
            m_advance = null;
        }

        /// <summary>
        /// 只换播放列表(还没开始播新的视频):列表换了就作废手头的身份
        /// </summary>
        public void SetMediaList(string mediaListId)
        {
            Commit(VideoId, mediaListId);
        }

        /// <summary>
        /// 取页失败,这一次换目标断在这里:记下本来要走的那一步
        /// </summary>
        public PlayPendingAdvance RememberAdvance(WatchSequenceSourceKind source, int rowIndex, bool skipRemainingParts, bool manual)
        {
            m_advance = new PlayPendingAdvance()
            {
                VideoId = VideoId ?? "",
                MediaListId = MediaListId ?? "",
                Generation = m_generation,
                Source = source,
                RowIndex = rowIndex,
                SkipRemainingParts = skipRemainingParts,
                Manual = manual,
            };
            return m_advance;
        }

        /// <summary>
        /// 记录只在原身份下算数:视频、播放列表和代数都还在同一拍上
        /// </summary>
        public bool AdvanceIsCurrent()
        {
            return m_advance != null
                && m_advance.Generation == m_generation
                && string.Equals(m_advance.VideoId ?? "", VideoId ?? "", StringComparison.Ordinal)
                && string.Equals(m_advance.MediaListId ?? "", MediaListId ?? "", StringComparison.Ordinal);
        }

        /// <summary>
        /// 用户点了"重试/加载更多":取到页就接着走那一步,又失败就留着记录,列表到头就取消。
        /// </summary>
        public PlayAdvanceResume DecideResume(bool landed, PlayDirectoryPagingState paging)
        {
            if (m_advance == null) return PlayAdvanceResume.None;
            // 换了视频或播放列表:这一条记录不再属于现在,只补行不推进
            if (!AdvanceIsCurrent())
            {
                m_advance = null;
                return PlayAdvanceResume.None;
            }
            if (landed)
            {
                // 取到了:记录先收掉,避免同一个重试推进两次
                m_advance = null;
                return PlayAdvanceResume.Resume;
            }
            // 又失败:记录留着,下一次重试还能接着走;列表已经到头就到此为止
            if (paging == PlayDirectoryPagingState.Exhausted)
            {
                m_advance = null;
                return PlayAdvanceResume.Cancel;
            }
            return PlayAdvanceResume.WaitForRetry;
        }

        /// <summary>
        /// 用户自己选了行、又或者这一条记录已经被用掉:直接丢掉
        /// </summary>
        public void DropAdvance()
        {
            m_advance = null;
        }

        /// <summary>
        /// 页签关掉:等待态、记录和身份一起作废,晚到的结果一张票都对不上
        /// </summary>
        public void Close()
        {
            m_generation++;
            m_pending = false;
            m_pendingTitle = "";
            m_pendingKey = "";
            m_advance = null;
            VideoId = "";
            MediaListId = "";
        }
    }
}

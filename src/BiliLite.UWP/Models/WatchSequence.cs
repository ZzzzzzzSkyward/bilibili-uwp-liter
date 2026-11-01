using System;

namespace BiliLite.Models.Common
{
    /// <summary>
    /// 连播来源:没有外层顺序、详情里的合集、打开时的播放列表
    /// </summary>
    public enum WatchSequenceSourceKind
    {
        None = 0,
        UgcSeason = 1,
        Playlist = 2,
    }

    /// <summary>
    /// 一次"下一个/上一个"到底做什么
    /// </summary>
    public enum WatchSequenceMove
    {
        None = 0,
        /// <summary>
        /// 换到本视频的另一个分P
        /// </summary>
        PlayPart = 1,
        /// <summary>
        /// 换到外层来源的另一行(合集分集或播放列表行)
        /// </summary>
        PlayRow = 2,
        /// <summary>
        /// 停在最后一行,先取下一页
        /// </summary>
        LoadNextPage = 3,
        /// <summary>
        /// 停在最后一行,下一页上次取失败了,等重试
        /// </summary>
        RetryNextPage = 4,
        /// <summary>
        /// 没有下一个了
        /// </summary>
        End = 5,
    }

    /// <summary>
    /// 判定下一个目标所需的全部信息。不可变快照:页面每次喂进来一份新的,
    /// 判定本身不读播放器、不读队列。
    /// </summary>
    public sealed class WatchSequencePosition
    {
        public WatchSequencePosition(WatchSequenceSourceKind source, int partIndex, int partCount,
            int rowIndex, int rowCount, bool hasMoreRows, bool lastRowPageFailed)
        {
            Source = source;
            PartIndex = partIndex;
            PartCount = partCount;
            RowIndex = rowIndex;
            RowCount = rowCount;
            HasMoreRows = hasMoreRows;
            LastRowPageFailed = lastRowPageFailed;
        }

        /// <summary>
        /// 当前连播跟着哪个外层来源
        /// </summary>
        public WatchSequenceSourceKind Source { get; private set; }

        /// <summary>
        /// 当前分P下标;一P都没有时为 0 且 PartCount 为 0
        /// </summary>
        public int PartIndex { get; private set; }

        public int PartCount { get; private set; }

        /// <summary>
        /// 当前视频在外层来源里的行号;不在来源里时为 -1
        /// </summary>
        public int RowIndex { get; private set; }

        public int RowCount { get; private set; }

        /// <summary>
        /// 外层来源还有没取回来的页
        /// </summary>
        public bool HasMoreRows { get; private set; }

        /// <summary>
        /// 上一次取下一页失败了,重试入口还开着
        /// </summary>
        public bool LastRowPageFailed { get; private set; }
    }

    /// <summary>
    /// 判定结果:做什么,以及做到哪一P/哪一行。用不到的下标是 -1。
    /// </summary>
    public sealed class WatchSequenceStep
    {
        public WatchSequenceStep(WatchSequenceMove move, int partIndex = -1, int rowIndex = -1)
        {
            Move = move;
            PartIndex = partIndex;
            RowIndex = rowIndex;
        }

        public WatchSequenceMove Move { get; private set; }

        public int PartIndex { get; private set; }

        public int RowIndex { get; private set; }
    }

    /// <summary>
    /// 下一个/上一个的目标判定。分P永远优先,分P走完才交给外层来源;
    /// 外层来源的翻页规则沿用 MediaListQueue,这里不另立一套。
    /// 纯函数,便于在 UWP 之外验证。
    /// </summary>
    public static class WatchSequenceOrder
    {
        private static readonly WatchSequenceStep NoneStep = new WatchSequenceStep(WatchSequenceMove.None);
        private static readonly WatchSequenceStep EndStep = new WatchSequenceStep(WatchSequenceMove.End);
        private static readonly WatchSequenceStep LoadNextPageStep = new WatchSequenceStep(WatchSequenceMove.LoadNextPage);
        private static readonly WatchSequenceStep RetryNextPageStep = new WatchSequenceStep(WatchSequenceMove.RetryNextPage);

        /// <summary>
        /// 主"下一个":还有分P就去下一P;分P走完(或 skipRemainingParts 要求跳过剩下的分P)时,
        /// 轮到外层来源。skipRemainingParts 只跳过剩下的分P,不跳过翻页。
        /// </summary>
        public static WatchSequenceStep Next(WatchSequencePosition position, bool skipRemainingParts)
        {
            if (position == null) return EndStep;

            if (!skipRemainingParts)
            {
                var part = NextPartStep(position);
                if (part != null) return part;
            }

            return NextRowStep(position) ?? EndStep;
        }

        /// <summary>
        /// 只要下一P:已经是最后一P时不动外层来源,返回 None
        /// </summary>
        public static WatchSequenceStep NextPartOnly(WatchSequencePosition position)
        {
            if (position == null) return NoneStep;
            return NextPartStep(position) ?? NoneStep;
        }

        /// <summary>
        /// "上一个":先回上一个分P,分P到头了再回外层来源的上一行。翻页不往回走。
        /// </summary>
        public static WatchSequenceStep Previous(WatchSequencePosition position)
        {
            if (position == null) return NoneStep;

            if (position.PartCount > 0 && position.PartIndex > 0)
            {
                var index = Math.Min(position.PartIndex - 1, position.PartCount - 1);
                return new WatchSequenceStep(WatchSequenceMove.PlayPart, index);
            }

            // 行号必须是来源里真实存在的一行,-1(不在来源里)和越界都不往回走
            if (position.RowIndex > 0 && position.RowIndex < position.RowCount)
            {
                return new WatchSequenceStep(WatchSequenceMove.PlayRow, -1, Math.Min(position.RowIndex - 1, position.RowCount - 1));
            }

            return NoneStep;
        }

        private static WatchSequenceStep NextPartStep(WatchSequencePosition position)
        {
            // 一P都没有、下标是负数或已经是最后一P:都没有分P这一步
            if (position.PartCount <= 0 || position.PartIndex < 0) return null;
            if (position.PartIndex >= position.PartCount - 1) return null;

            var index = position.PartIndex + 1;
            if (index >= position.PartCount) return null;
            return new WatchSequenceStep(WatchSequenceMove.PlayPart, index);
        }

        private static WatchSequenceStep NextRowStep(WatchSequencePosition position)
        {
            // 没有外层来源(或来源一行都没有)时,分P走完就是结束
            if (position.Source == WatchSequenceSourceKind.None || position.RowCount <= 0) return null;

            switch (MediaListQueue.NextAction(position.RowIndex, position.RowCount, position.HasMoreRows, position.LastRowPageFailed))
            {
                case MediaListNextAction.PlayNextRow:
                    var index = position.RowIndex + 1;
                    // 行号只可能落在已加载的行里,落不进去就当结束,不猜一行
                    if (index < 0 || index >= position.RowCount) return null;
                    return new WatchSequenceStep(WatchSequenceMove.PlayRow, -1, index);
                case MediaListNextAction.LoadNextPage:
                    return LoadNextPageStep;
                case MediaListNextAction.RetryNextPage:
                    return RetryNextPageStep;
                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// 页面持有的播放顺序状态:当前来源、当前分P、当前来源里的当前行。
    /// 只存事实,不决定目标也不发起播放。
    /// </summary>
    public sealed class WatchSequenceState
    {
        private WatchSequencePosition m_position =
            new WatchSequencePosition(WatchSequenceSourceKind.None, 0, 0, -1, 0, false, false);

        public WatchSequencePosition Position
        {
            get { return m_position; }
        }

        /// <summary>
        /// 当前视频的分P信息;换了视频也要重新喂一次
        /// </summary>
        public void SetParts(int partIndex, int partCount)
        {
            if (partCount < 0) partCount = 0;
            m_position = new WatchSequencePosition(m_position.Source, partIndex, partCount,
                m_position.RowIndex, m_position.RowCount, m_position.HasMoreRows, m_position.LastRowPageFailed);
        }

        /// <summary>
        /// 当前外层来源的行信息:行号、行数、还有没有下一页、上次翻页是否失败
        /// </summary>
        public void SetRows(WatchSequenceSourceKind source, int rowIndex, int rowCount, bool hasMoreRows, bool lastRowPageFailed)
        {
            if (rowCount < 0) rowCount = 0;
            m_position = new WatchSequencePosition(source, m_position.PartIndex, m_position.PartCount,
                rowIndex, rowCount, hasMoreRows, lastRowPageFailed);
        }

        /// <summary>
        /// 选定连播来源。行数据属于上一个来源,换来源时先清掉,由调用方补上新来源的行。
        /// </summary>
        public void SetActiveSource(WatchSequenceSourceKind source)
        {
            if (source == m_position.Source) return;
            m_position = new WatchSequencePosition(source, m_position.PartIndex, m_position.PartCount, -1, 0, false, false);
        }

        public void Clear()
        {
            m_position = new WatchSequencePosition(WatchSequenceSourceKind.None, 0, 0, -1, 0, false, false);
        }
    }
}

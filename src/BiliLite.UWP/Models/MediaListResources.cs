using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace BiliLite.Models.Common
{
    /// <summary>
    /// /x/v2/medialist/resource/list 的响应,字段名与请求参数以外的服务端键保持一致
    /// </summary>
    public class MediaListResources
    {
        [JsonProperty("media_list")]
        public List<MediaListItem> MediaList { get; set; }

        [JsonProperty("has_more")]
        public bool HasMore { get; set; }

        [JsonProperty("total_count")]
        public int TotalCount { get; set; }
    }

    public class MediaListItem
    {
        /// <summary>
        /// 资源ID,播放列表里就是视频的 aid
        /// </summary>
        public long Id { get; set; }

        public string Title { get; set; }

        public string Cover { get; set; }

        public MediaListItemUpper Upper { get; set; }

        /// <summary>
        /// 秒
        /// </summary>
        public long Duration { get; set; }
    }

    public class MediaListItemUpper
    {
        public string Name { get; set; }
    }

    /// <summary>
    /// 当前播放列表从哪来:调用方传入的内存列表,还是远程 medialist
    /// </summary>
    public enum MediaListSourceKind
    {
        InMemoryPlaylist = 0,
        MediaList = 1,
    }

    /// <summary>
    /// 远程播放列表的翻页状态。解析、游标、去重和计数都在这里,页面只负责展示,
    /// 因此这些规则可以在 UWP 之外直接执行验证。
    /// </summary>
    public class MediaListPagingState
    {
        public const string DefaultTitle = "播放列表";

        private readonly List<MediaListItem> m_rows = new List<MediaListItem>();

        public MediaListPagingState(string mediaListId, int mediaListType = 1, MediaListSourceKind sourceKind = MediaListSourceKind.MediaList, string title = null)
        {
            MediaListId = mediaListId ?? "";
            MediaListType = mediaListType;
            SourceKind = sourceKind;
            Title = string.IsNullOrEmpty(title) ? DefaultTitle : title;
        }

        public string MediaListId { get; private set; }

        public int MediaListType { get; private set; }

        public MediaListSourceKind SourceKind { get; private set; }

        public string Title { get; private set; }

        public bool HasMore { get; private set; }

        public int TotalCount { get; private set; }

        /// <summary>
        /// 上一页响应里最后一条的 aid,下一次请求的 oid;与选中的播放行无关
        /// </summary>
        public string LastAid { get; private set; }

        public IReadOnlyList<MediaListItem> Rows
        {
            get { return m_rows; }
        }

        public int LoadedCount
        {
            get { return m_rows.Count; }
        }

        /// <summary>
        /// 首页还没取过
        /// </summary>
        public bool IsFirstPage
        {
            get { return LastAid == null; }
        }

        public string NextCursor
        {
            get { return LastAid ?? ""; }
        }

        /// <summary>
        /// "已加载 n / 总数";总数未知时只显示已加载数
        /// </summary>
        public string CountText
        {
            get
            {
                return TotalCount > 0
                    ? m_rows.Count.ToString(CultureInfo.InvariantCulture) + " / " + TotalCount.ToString(CultureInfo.InvariantCulture)
                    : m_rows.Count.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// 追加一页响应,返回实际新增的行数(已存在的资源ID不会重复加入)
        /// </summary>
        public int Append(MediaListResources page)
        {
            if (page == null) return 0;

            var added = 0;
            var items = page.MediaList;
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item == null || item.Id <= 0) continue;
                    if (ContainsId(item.Id)) continue;
                    m_rows.Add(item);
                    added++;
                }
            }

            // 游标是已加载行的最后一条:响应按顺序追加,所以它就是响应里的最后一条返回项,
            // 与当前选中的播放行无关;整页都是重复行时停在原来的位置,不会往回跳
            if (m_rows.Count > 0)
            {
                LastAid = m_rows[m_rows.Count - 1].Id.ToString(CultureInfo.InvariantCulture);
            }

            // has_more 以最新一页为准
            HasMore = page.HasMore;
            TotalCount = page.TotalCount > 0 ? page.TotalCount : m_rows.Count;
            return added;
        }

        public bool ContainsId(long id)
        {
            return m_rows.Exists(x => x.Id == id);
        }
    }
}

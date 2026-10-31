using System;
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
    /// 自动连播下一步做什么
    /// </summary>
    public enum MediaListNextAction
    {
        /// <summary>
        /// 已加载的行走完了,源也取尽了
        /// </summary>
        Stop = 0,
        /// <summary>
        /// 已加载的下一行
        /// </summary>
        PlayNextRow = 1,
        /// <summary>
        /// 停在最后一行,但还有下一页
        /// </summary>
        LoadNextPage = 2,
        /// <summary>
        /// 停在最后一行,下一页上次取失败了,等用户点重试
        /// </summary>
        RetryNextPage = 3,
    }

    /// <summary>
    /// 自动连播的行选择规则。远程队列和调用方传入的内存列表共用,
    /// 只有"还有下一页"这一种情况会去取网络。
    /// </summary>
    public static class MediaListQueue
    {
        public static MediaListNextAction NextAction(int currentIndex, int loadedCount, bool hasMore, bool lastLoadFailed)
        {
            // 一行都没有:只有"上次请求失败"能继续,否则没有可播的源
            if (loadedCount <= 0) return lastLoadFailed ? MediaListNextAction.RetryNextPage : MediaListNextAction.Stop;
            if (currentIndex < 0 || currentIndex >= loadedCount) return MediaListNextAction.Stop;
            if (currentIndex < loadedCount - 1) return MediaListNextAction.PlayNextRow;
            // 已经在最后一行:取下一页失败过就先等重试,不要重复请求也别当成播完
            if (!hasMore) return MediaListNextAction.Stop;
            return lastLoadFailed ? MediaListNextAction.RetryNextPage : MediaListNextAction.LoadNextPage;
        }
    }

    /// <summary>
    /// 启动器链接 bilibili://music/playlist/playpage/{id}[?page_type=n] 的解析。
    /// 只认这一种形态:收藏夹的 ml 链接、音频/歌单链接和其它 bilibili 链接都返回 false,
    /// 交给 MessageCenter 里原有的路由继续处理。
    /// </summary>
    public class MediaListRoute
    {
        /// <summary>
        /// 链接里没写 page_type 时用的值。链接默认是 2,与接口方法默认的 1 不是一回事。
        /// </summary>
        public const int DefaultPlaylistType = 2;

        public const string PlayPagePrefix = "bilibili://music/playlist/playpage/";

        private MediaListRoute(string mediaListId, int mediaListType)
        {
            MediaListId = mediaListId;
            MediaListType = mediaListType;
        }

        /// <summary>
        /// 播单 ID,原样取链接里的数字,不与合集ID、UP主 mid、收藏夹ID 互转
        /// </summary>
        public string MediaListId { get; private set; }

        public int MediaListType { get; private set; }

        public static bool TryParse(string url, out MediaListRoute route)
        {
            route = null;
            if (string.IsNullOrWhiteSpace(url)) return false;

            var text = url.Trim();
            if (!text.StartsWith(PlayPagePrefix, StringComparison.OrdinalIgnoreCase)) return false;

            var rest = text.Substring(PlayPagePrefix.Length);
            var end = rest.IndexOfAny(new[] { '/', '?', '#' });
            var idText = end < 0 ? rest : rest.Substring(0, end);
            if (idText.Length == 0) return false;
            foreach (var ch in idText)
            {
                if (ch < '0' || ch > '9') return false;
            }

            route = new MediaListRoute(idText, ParsePageType(rest));
            return true;
        }

        private static int ParsePageType(string rest)
        {
            var query = rest.IndexOf('?');
            if (query < 0 || query == rest.Length - 1) return DefaultPlaylistType;

            foreach (var part in rest.Substring(query + 1).Split('&'))
            {
                var pair = part.Split(new[] { '=' }, 2);
                if (pair.Length != 2) continue;
                if (!string.Equals(pair[0].Trim(), "page_type", StringComparison.OrdinalIgnoreCase)) continue;

                int type;
                if (int.TryParse(pair[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out type) && type > 0)
                {
                    return type;
                }
                // 值写坏了按链接默认值,不按接口默认值
                return DefaultPlaylistType;
            }
            return DefaultPlaylistType;
        }
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

        /// <summary>
        /// 上一次翻页取失败了。已加载行和游标都不动,"加载更多"留作重试入口。
        /// </summary>
        public bool LastLoadFailed { get; private set; }

        public void MarkLoadFailed()
        {
            LastLoadFailed = true;
        }

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

            LastLoadFailed = false;
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

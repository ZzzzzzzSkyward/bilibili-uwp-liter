using System.Collections.Generic;
using BiliLite.Models.Api.MediaList;
using BiliLite.Models.Common;

namespace BiliLite.Parsing.MediaList
{
    /// <summary>
    /// resource/list 的一页 → 播放列表翻页状态吃的 <see cref="MediaListResources"/>。
    /// 去重、ID&gt;0 的过滤和游标推进仍在 MediaListPagingState.Append，本层只搬字段。
    ///
    /// 1. 行顺序原样保留；元素为 null 时跳过。
    /// 2. has_more 缺失按 false：没报就当到头。
    /// 3. total_count 缺失按 0，也就是"未知"——Append 只在 &gt;0 时才记成已知总数。
    /// 4. MediaList 永远是空表而不是 null：缺列表已经在解析层被拒，走到这里的都是"有列表"。
    /// </summary>
    public static class MediaListResourceListMapper
    {
        public static MediaListResources Map(MediaListResourceListDto page)
        {
            if (page == null) return null;

            var rows = new List<MediaListItem>();
            if (page.MediaList != null)
            {
                foreach (var item in page.MediaList)
                {
                    if (item == null) continue;
                    rows.Add(new MediaListItem
                    {
                        Id = item.Id,
                        Title = item.Title,
                        Cover = item.Cover,
                        Duration = item.Duration,
                        Upper = item.Upper == null ? null : new MediaListItemUpper { Name = item.Upper.Name },
                    });
                }
            }

            return new MediaListResources
            {
                MediaList = rows,
                HasMore = page.HasMore ?? false,
                TotalCount = page.TotalCount ?? 0,
            };
        }
    }
}

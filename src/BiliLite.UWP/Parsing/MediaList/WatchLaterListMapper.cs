using System.Collections.ObjectModel;
using System.Globalization;
using BiliLite.Models.Api.MediaList;
using BiliLite.Modules.User;

namespace BiliLite.Parsing.MediaList
{
    /// <summary>
    /// toview 的一份列表 → 稍后再看页面绑定的行。只搬字段，DeleteCommand 仍由 VM 挂。
    ///
    /// 1. list 缺失/null 给空集合：页面据此显示"没有视频"。
    /// 2. aid 从线上数字转回 string：页面模型与删除接口都按 string 用。
    /// 3. 线上 pages[] 不再搬：页面从不读它，改造前也只是被反序列化出来没人用。
    /// </summary>
    public static class WatchLaterListMapper
    {
        public static ObservableCollection<WatchlaterItemModel> Map(WatchLaterListDto page)
        {
            var rows = new ObservableCollection<WatchlaterItemModel>();
            if (page == null || page.List == null) return rows;

            foreach (var item in page.List)
            {
                if (item == null) continue;
                rows.Add(new WatchlaterItemModel
                {
                    aid = item.Aid.ToString(CultureInfo.InvariantCulture),
                    videos = item.Videos,
                    tname = item.Tname,
                    pic = item.Pic,
                    title = item.Title,
                    desc = item.Desc,
                    dynamic = item.Dynamic,
                    cid = item.Cid,
                    add_at = item.AddAt,
                    owner = item.Owner == null
                        ? null
                        : new WatchlaterOwnerModel
                        {
                            face = item.Owner.Face,
                            mid = item.Owner.Mid,
                            name = item.Owner.Name,
                        },
                    progress = item.Progress,
                    duration = item.Duration,
                });
            }
            return rows;
        }
    }
}

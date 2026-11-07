using System.Collections.ObjectModel;
using BiliLite.Models.Api.Video;
using BiliLite.Modules;

namespace BiliLite.Parsing.Video
{
    /// <summary>
    /// "我创建的收藏夹" → 详情页收藏弹窗正在渲染的 FavoriteItemModel。
    /// 只搬字段；id/fid/mid 保持字符串（和 FavoriteItemModel 一致），privacy/is_fav 是模型自己的派生属性。
    ///
    /// 1. list 缺失/null 给空集合：改造前是 NRE 折成一句 toast，现在 UI 显示"没有收藏夹"。
    /// 2. 只搬页面上真正用到的字段：like_state 是收藏夹自己的点赞状态，详情页不展示但原样保留。
    /// </summary>
    public static class FavoriteCreatedListMapper
    {
        public static ObservableCollection<FavoriteItemModel> Map(FavoriteCreatedListDto source)
        {
            var favorites = new ObservableCollection<FavoriteItemModel>();
            if (source?.List == null) return favorites;

            foreach (var item in source.List)
            {
                if (item == null) continue;
                favorites.Add(new FavoriteItemModel
                {
                    id = item.Id,
                    fid = item.Fid,
                    mid = item.Mid,
                    attr = item.Attr,
                    title = item.Title,
                    cover = item.Cover,
                    intro = item.Intro,
                    type = item.Type,
                    media_count = item.MediaCount,
                    fav_state = item.FavState,
                    like_state = item.LikeState,
                });
            }
            return favorites;
        }
    }
}

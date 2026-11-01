using System.Collections.ObjectModel;
using BiliLite.Models.Api.MediaList;
using BiliLite.Modules;

namespace BiliLite.Parsing.MediaList
{
    /// <summary>
    /// fav/resource/list 的一页 → 收藏夹详情页正在渲染的那份模型。
    /// 只搬字段：翻页（media_count）和选中/批量操作仍留在 FavoriteDetailVM。
    ///
    /// 1. medias 缺失/null 给空集合：第 1 页由此显示空状态，第 2 页什么都不追加。
    /// 2. info 缺失给 null：第 1 页缺它就不发布（标题、mid 都要它），判断在 VM。
    /// 3. id 一律保持字符串：页面模型的 id 就是 string，删/复制都按 string 传出去。
    /// </summary>
    public static class FavoriteResourceListMapper
    {
        public static FavoriteDetailModel Map(FavoriteResourceListDto page)
        {
            if (page == null) return null;

            var medias = new ObservableCollection<FavoriteInfoVideoItemModel>();
            if (page.Medias != null)
            {
                foreach (var item in page.Medias)
                {
                    if (item == null) continue;
                    medias.Add(new FavoriteInfoVideoItemModel
                    {
                        id = item.Id,
                        title = item.Title,
                        cover = item.Cover,
                        intro = item.Intro,
                        upper = MapUpper(item.Upper),
                        cnt_info = MapStat(item.CntInfo),
                    });
                }
            }

            return new FavoriteDetailModel
            {
                info = MapInfo(page.Info),
                medias = medias,
            };
        }

        private static FavoriteInfoModel MapInfo(FavoriteResourceInfoDto source)
        {
            if (source == null) return null;

            return new FavoriteInfoModel
            {
                id = source.Id,
                fid = source.Fid,
                mid = source.Mid,
                title = source.Title,
                cover = source.Cover,
                attr = source.Attr,
                fav_state = source.FavState,
                like_state = source.LikeState,
                type = source.Type,
                media_count = source.MediaCount,
                upper = MapUpper(source.Upper),
            };
        }

        private static FavoriteInfoVideoItemUpperModel MapUpper(FavoriteResourceUpperDto source)
        {
            if (source == null) return null;

            return new FavoriteInfoVideoItemUpperModel
            {
                face = source.Face,
                name = source.Name,
                mid = source.Mid,
            };
        }

        private static FavoriteInfoVideoItemStatModel MapStat(FavoriteResourceStatDto source)
        {
            if (source == null) return null;

            return new FavoriteInfoVideoItemStatModel
            {
                coin = source.Coin,
                collect = source.Collect,
                danmaku = source.Danmaku,
                play = source.Play,
                reply = source.Reply,
                share = source.Share,
            };
        }
    }
}

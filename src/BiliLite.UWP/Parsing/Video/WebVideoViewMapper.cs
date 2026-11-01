using System.Collections.Generic;
using BiliLite.Models.Api.Video;
using BiliLite.Modules;

namespace BiliLite.Parsing.Video
{
    /// <summary>
    /// view/related/tags 的 DTO → 详情页正在渲染的那份模型（Modules/Detail/VideoDetailVM.cs 底部的
    /// VideoDetailModel 一族）。只搬字段，不做网络、不判换源、不弹提示。
    ///
    /// 1. short_link 一律合成 https://b23.tv/{bvid}：线上该字段为空，改造前也是 VM 覆盖出来的。
    /// 2. owner_ext 永远非 null：web 端点从来不返回它，页面靠 HasFans 隐藏粉丝标签，null 会直接炸
    ///    （改造前 VM 里 `?? new VideoDetailOwnerExtModel()` 做的就是这个默认，现在归这里）。
    /// 3. req_user 永远非 null，但**不搬线上值**：它是本地 UI 状态，由 VM 的动作/状态解析结果驱动
    ///    （attention 来自 x/relation，like/coin/favorite 来自三条状态读）。搬线上值只会让两处都写。
    /// 4. pages 缺失给空表：页面 foreach 分P、showPages 也按空表判，null 会让起播 NRE。
    /// 5. relates 与 tag 不在这里赋值：它们来自另外两个端点，由 VM 合并；relates 失败时空表、不发 null。
    /// 6. id 一律按 string 搬：页面、播放队列、合集都按 string 传，不做数字转换。
    /// </summary>
    public static class WebVideoViewMapper
    {
        public static VideoDetailModel MapWebVideoView(WebVideoViewDto source)
        {
            if (source == null) return null;

            return new VideoDetailModel
            {
                bvid = source.Bvid,
                aid = source.Aid,
                videos = source.Videos,
                tid = source.Tid,
                tname = source.Tname,
                pic = source.Pic,
                title = source.Title,
                pubdate = source.Pubdate,
                ctime = source.Ctime,
                desc = source.Desc,
                attribute = source.Attribute,
                state = source.State,
                duration = source.Duration,
                rights = MapRights(source.Rights),
                dynamic = source.Dynamic,
                owner = MapOwner(source.Owner),
                owner_ext = MapOwnerExt(source.OwnerExt),
                stat = MapStat(source.Stat),
                req_user = new VideoDetailReqUserModel(),
                share_subtitle = source.ShareSubtitle,
                short_link = "https://b23.tv/" + (source.Bvid ?? ""),
                redirect_url = source.RedirectUrl,
                pages = MapPages(source.Pages),
                interaction = MapInteraction(source.Interaction),
                staff = MapStaff(source.Staff),
                argue_msg = source.ArgueMsg,
                history = MapHistory(source.History),
                UgcSeason = source.UgcSeason,
            };
        }

        /// <summary>
        /// 相关视频 → VideoDetailModel.relates。空 aid 的项由模型 setter 过滤掉（改造前的规则），
        /// 这里原样搬；失败时给空表由 VM 决定，所以本方法不返回 null。
        /// </summary>
        public static List<VideoDetailRelatesModel> MapWebVideoRelates(List<WebVideoRelateDto> source)
        {
            var relates = new List<VideoDetailRelatesModel>();
            if (source == null) return relates;

            foreach (var item in source)
            {
                if (item == null) continue;
                relates.Add(new VideoDetailRelatesModel
                {
                    aid = item.Aid,
                    pic = item.Pic,
                    title = item.Title,
                    owner = MapOwner(item.Owner),
                    stat = MapStat(item.Stat),
                });
            }
            return relates;
        }

        /// <summary>标签 → VideoDetailModel.tag；空表就是"没有标签"，不是 null。</summary>
        public static List<VideoDetailTagModel> MapWebVideoTags(List<WebVideoTagDto> source)
        {
            var tags = new List<VideoDetailTagModel>();
            if (source == null) return tags;

            foreach (var item in source)
            {
                if (item == null) continue;
                tags.Add(new VideoDetailTagModel
                {
                    tag_id = item.TagId,
                    tag_name = item.TagName,
                });
            }
            return tags;
        }

        private static VideoDetailRightsModel MapRights(WebVideoRightsDto source)
        {
            if (source == null) return null;

            return new VideoDetailRightsModel
            {
                bp = source.Bp,
                elec = source.Elec,
                download = source.Download,
                movie = source.Movie,
                pay = source.Pay,
            };
        }

        private static VideoDetailOwnerModel MapOwner(WebVideoOwnerDto source)
        {
            if (source == null) return null;

            return new VideoDetailOwnerModel
            {
                mid = source.Mid,
                name = source.Name,
                face = source.Face,
            };
        }

        /// <summary>owner_ext 缺失时给全默认对象：fans 0 → HasFans 为假 → 粉丝标签隐藏。</summary>
        private static VideoDetailOwnerExtModel MapOwnerExt(WebVideoOwnerExtDto source)
        {
            if (source == null) return new VideoDetailOwnerExtModel();

            return new VideoDetailOwnerExtModel
            {
                fans = source.Fans,
                vip = MapVip(source.Vip),
                official_verify = MapOfficialVerify(source.OfficialVerify),
            };
        }

        private static VideoDetailOwnerExtVipModel MapVip(WebVideoOwnerExtVipDto source)
        {
            if (source == null) return null;

            return new VideoDetailOwnerExtVipModel
            {
                vipType = source.VipType,
                vipStatus = source.VipStatus,
            };
        }

        private static VideoDetailOwnerExtOfficialVerifyModel MapOfficialVerify(WebVideoOwnerExtOfficialVerifyDto source)
        {
            if (source == null) return null;

            return new VideoDetailOwnerExtOfficialVerifyModel
            {
                type = source.Type,
                desc = source.Desc,
            };
        }

        private static VideoDetailStatModel MapStat(WebVideoStatDto source)
        {
            if (source == null) return null;

            return new VideoDetailStatModel
            {
                aid = source.Aid,
                view = source.View,
                danmaku = source.Danmaku,
                reply = source.Reply,
                favorite = source.Favorite,
                coin = source.Coin,
                share = source.Share,
                like = source.Like,
                dislike = source.Dislike,
            };
        }

        private static List<VideoDetailPagesModel> MapPages(List<WebVideoPageDto> source)
        {
            var pages = new List<VideoDetailPagesModel>();
            if (source == null) return pages;

            foreach (var item in source)
            {
                if (item == null) continue;
                pages.Add(new VideoDetailPagesModel
                {
                    cid = item.Cid,
                    page = item.Page,
                    part = item.Part,
                    duration = item.Duration,
                    dmlink = item.Dmlink,
                    download_title = item.DownloadTitle,
                    download_subtitle = item.DownloadSubtitle,
                });
            }
            return pages;
        }

        private static List<VideoDetailStaffModel> MapStaff(List<WebVideoStaffDto> source)
        {
            //staff 缺失保持 null：页面的 showStaff 就是按 null 判"没有合作成员"
            if (source == null) return null;

            var staff = new List<VideoDetailStaffModel>();
            foreach (var item in source)
            {
                if (item == null) continue;
                staff.Add(new VideoDetailStaffModel
                {
                    mid = item.Mid,
                    title = item.Title,
                    face = item.Face,
                    name = item.Name,
                    vip = MapVip(item.Vip),
                    official_verify = MapOfficialVerify(item.OfficialVerify),
                    attention = item.Attention,
                });
            }
            return staff;
        }

        private static VideoDetailInteractionModel MapInteraction(WebVideoInteractionDto source)
        {
            if (source == null) return null;

            var interaction = new VideoDetailInteractionModel
            {
                graph_version = source.GraphVersion,
            };
            if (source.HistoryNode != null)
            {
                interaction.history_node = new VideoDetailInteractionHistoryNodeModel
                {
                    node_id = source.HistoryNode.NodeId,
                    title = source.HistoryNode.Title,
                    cid = source.HistoryNode.Cid,
                };
            }
            return interaction;
        }

        private static VideoDetailHistoryModel MapHistory(WebVideoHistoryDto source)
        {
            if (source == null) return null;

            return new VideoDetailHistoryModel
            {
                cid = source.Cid,
                progress = source.Progress,
            };
        }
    }
}

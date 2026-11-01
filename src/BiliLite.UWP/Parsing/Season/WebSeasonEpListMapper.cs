using System.Collections.Generic;
using System.Linq;
using BiliLite.Models.Api.Season.Web;
using BiliLite.Modules;

namespace BiliLite.Parsing.Season
{
    /// <summary>一季的片单：Detail 是整页信息，Episodes 是正片，Previews 是预告/花絮。</summary>
    public sealed class SeasonDetailEpisodes
    {
        public SeasonDetailModel Detail { get; set; }

        public List<SeasonDetailEpisodeModel> Episodes { get; set; }

        public List<SeasonDetailEpisodeModel> Previews { get; set; }
    }

    /// <summary>
    /// ep/list → 现有 SeasonDetailModel。合并、正片/预告划分、IsPreview 派生都只在这里做，
    /// 拿到的是页面上已经在渲染的那批模型，渲染层不需要知道来源。
    ///
    /// 合并规则：
    /// 1. result.section 按线上顺序整体排在 result.episodes 之前；每个分区的片按分区内顺序。
    ///    （旧代码对每个分区做 InsertRange(0, …)，多分区时顺序会倒过来；这里按服务端给出的顺序。）
    /// 2. 同一条片同时出现在 result.episodes 和某个 section 时只保留第一次出现的那条，也就是分区那条。
    ///    判定用 ep 号（id ?? ep_id），两边都没有 ep 号的条目不去重。
    /// 3. section_type 只由来源决定：正片 0、分区 1，不信线上的 section_type 字段；IsPreview 由此派生。
    /// 4. result.total 不参与映射，只原样带进 Detail.total。
    /// </summary>
    public static class WebSeasonEpListMapper
    {
        public static SeasonDetailEpisodes MapWebSeasonEpList(WebSeasonEpListDto page)
        {
            var merged = new List<SeasonDetailEpisodeModel>();
            var seen = new HashSet<int>();

            var sections = new List<SeasonDetailSectionItemModel>();
            foreach (var source in page.Section)
            {
                var section = new SeasonDetailSectionItemModel
                {
                    title = source.Title,
                    id = source.Id,
                    type = source.Type,
                    episodes = new List<SeasonDetailEpisodeModel>(),
                };
                foreach (var episode in source.Episodes)
                {
                    var mapped = Append(merged, seen, episode, 1);
                    if (mapped != null)
                    {
                        section.episodes.Add(mapped);
                    }
                }
                sections.Add(section);
            }

            foreach (var episode in page.Episodes)
            {
                Append(merged, seen, episode, 0);
            }

            return new SeasonDetailEpisodes
            {
                Detail = MapDetail(page, merged, sections),
                Episodes = merged.Where(x => !x.IsPreview).ToList(),
                Previews = merged.Where(x => x.IsPreview).ToList(),
            };
        }

        /// <summary>去重键是 ep 号；两边都没有 ep 号的条目不去重，避免把不同的片并成一条。</summary>
        private static SeasonDetailEpisodeModel Append(List<SeasonDetailEpisodeModel> target, HashSet<int> seen,
            WebSeasonEpisodeDto source, int sectionType)
        {
            var episodeId = source.Id ?? source.EpId;
            if (episodeId.HasValue && !seen.Add(episodeId.Value))
            {
                return null;
            }

            var mapped = new SeasonDetailEpisodeModel
            {
                aid = source.Aid?.ToString() ?? "",
                bvid = source.Bvid ?? "",
                cid = source.Cid?.ToString() ?? "",
                badge = source.Badge ?? "",
                badge_type = source.BadgeType ?? 0,
                cover = source.Cover ?? "",
                //id/title/long_title 的兜底原本在模型 getter 里，来源字段这里都有，映射时一次定掉
                id = episodeId ?? 0,
                ep_id = source.EpId,
                episode_status = source.EpisodeStatus,
                //模型的 status getter 会在 _status 为空时回落到 episode_status，两边都空就抛；
                //这里一次定死，页面读 item.status 不再可能踩到那个空值
                status = source.Status ?? source.EpisodeStatus ?? 0,
                index = source.Index,
                index_title = source.IndexTitle,
                title = source.Title ?? source.Index,
                long_title = source.LongTitle ?? source.IndexTitle,
                section_type = sectionType,
            };
            target.Add(mapped);
            return mapped;
        }

        private static SeasonDetailModel MapDetail(WebSeasonEpListDto page,
            List<SeasonDetailEpisodeModel> episodes, List<SeasonDetailSectionItemModel> sections)
        {
            return new SeasonDetailModel
            {
                season_id = page.SeasonId,
                season_title = page.SeasonTitle,
                title = page.Title,
                origin_name = page.OriginName,
                cover = page.Cover,
                square_cover = page.SquareCover,
                evaluate = page.Evaluate,
                alias = page.Alias,
                badge = page.Badge,
                status = page.Status,
                subtitle = page.Subtitle,
                link = page.Link,
                short_link = page.ShortLink,
                media_id = page.MediaId,
                mode = page.Mode,
                type = page.Type,
                total = page.Total ?? 0,
                up_info = page.UpInfo == null
                    ? null
                    : new SeasonDetailUpInfoModel { mid = page.UpInfo.Mid, uname = page.UpInfo.Uname },
                areas = page.Areas == null
                    ? null
                    : page.Areas.Select(x => new SeasonDetailAreaItemModel
                    {
                        id = x.Id?.ToString() ?? "",
                        name = x.Name,
                    }).ToList(),
                rating = page.Rating == null
                    ? null
                    : new SeasonDetailRatingModel { count = page.Rating.Count, score = page.Rating.Score },
                publish = page.Publish == null
                    ? null
                    : new SeasonDetailPublishModel
                    {
                        is_finish = page.Publish.IsFinish,
                        is_started = page.Publish.IsStarted,
                        weekday = page.Publish.Weekday,
                        pub_time = page.Publish.PubTime,
                        pub_time_show = page.Publish.PubTimeShow,
                        release_date_show = page.Publish.ReleaseDateShow,
                        time_length_show = page.Publish.TimeLengthShow,
                    },
                stat = page.Stat == null
                    ? null
                    : new SeasonDetailStatModel
                    {
                        coins = page.Stat.Coins,
                        danmakus = page.Stat.Danmakus,
                        favorites = page.Stat.Favorites,
                        followers = page.Stat.Followers,
                        play = page.Stat.Play,
                        reply = page.Stat.Reply,
                        share = page.Stat.Share,
                        views = page.Stat.Views,
                    },
                seasons = page.Seasons == null
                    ? null
                    : page.Seasons.Select(x => new SeasonDetailSeasonItemModel
                    {
                        season_id = x.SeasonId,
                        cover = x.Cover,
                        season_title = x.SeasonTitle,
                        title = x.Title,
                    }).ToList(),
                user_status = MapUserStatus(page.UserStatus),
                payment = page.Payment == null
                    ? null
                    : new SeasonDetailPaymentModel
                    {
                        price = page.Payment.Price,
                        tv_price = page.Payment.TvPrice,
                        dialog = page.Payment.Dialog == null
                            ? null
                            : new SeasonDetailPaymentDialogModel
                            {
                                title = page.Payment.Dialog.Title,
                                desc = page.Payment.Dialog.Desc,
                            },
                    },
                //页面的下载按钮读 Detail.episodes，这里必须是合并后的整份片单
                episodes = episodes,
                section = sections,
            };
        }

        private static SeasonDetailUserStatusModel MapUserStatus(WebSeasonUserStatusDto source)
        {
            if (source == null)
            {
                return null;
            }

            return new SeasonDetailUserStatusModel
            {
                follow = source.Follow,
                follow_bubble = source.FollowBubble,
                follow_status = source.FollowStatus,
                pay = source.Pay,
                vip = source.Vip,
                sponsor = source.Sponsor,
                vip_frozen = source.VipFrozen,
                progress = source.Progress == null
                    ? null
                    : new SeasonDetailUserStatusProgressModel
                    {
                        last_ep_index = source.Progress.LastEpIndex,
                        last_ep_id = source.Progress.LastEpId,
                        last_time = source.Progress.LastTime,
                    },
            };
        }
    }
}

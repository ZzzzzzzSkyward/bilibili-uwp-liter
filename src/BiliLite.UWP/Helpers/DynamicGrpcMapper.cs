using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bilibili.App.Dynamic.V2;
using BiliLite.Modules;
using static BiliLite.Parsing.Dynamic.WireScalars;
using GrpcDynamicType = Bilibili.App.Dynamic.V2.DynamicType;
using Newtonsoft.Json.Linq;

namespace BiliLite.Helpers
{
    /// <summary>
    /// Adapts the Dynamic V2 feed models returned by the sibling gRPC API to the
    /// target app's existing home-card and 2024 dynamic-card models.
    /// </summary>
    public static class DynamicGrpcMapper
    {
        public static List<DynamicItemModel> MapToHomeItems(IEnumerable<DynamicItem> dynamicItems)
        {
            var result = new List<DynamicItemModel>();
            if (dynamicItems == null)
            {
                return result;
            }

            foreach (var source in dynamicItems)
            {
                if (source == null || source.Extend == null)
                {
                    continue;
                }

                var author = GetModule(source, DynModuleType.ModuleAuthor)?.ModuleAuthor;
                var dynamic = GetModule(source, DynModuleType.ModuleDynamic)?.ModuleDynamic;
                var id = source.Extend.DynIdStr;
                var item = new DynamicItemModel
                {
                    desc = new DynamicDescModel
                    {
                        uid = author?.Mid.ToString() ?? "",
                        dynamic_id = id,
                        dynamic_id_str = id,
                        time_text = author?.PtimeLabelText ?? "",
                        type = 0,
                    }
                };

                switch (source.CardType)
                {
                    case GrpcDynamicType.Av:
                        if (dynamic?.DynArchive == null)
                        {
                            continue;
                        }
                        item.desc.type = 8;
                        item.desc.rid = dynamic.DynArchive.Avid.ToString();
                        item.desc.view = dynamic.DynArchive.View;
                        item.Video = new DynamicVideoCardModel
                        {
                            aid = dynamic.DynArchive.Avid.ToString(),
                            duration = (int)dynamic.DynArchive.Duration,
                            pic = dynamic.DynArchive.Cover,
                            title = FirstNonEmpty(dynamic.DynArchive.Title, FirstDescription(source.Extend.OrigDesc)),
                            desc = dynamic.DynArchive.Title,
                            jump_url = FirstNonEmpty(dynamic.DynArchive.JumpUrl, dynamic.DynArchive.Uri),
                            owner = new DynamicVideoCardOwnerModel
                            {
                                face = source.Extend.OrigFace,
                                mid = author?.Mid.ToString() ?? "",
                                name = FirstNonEmpty(source.Extend.OrigName, author?.Author?.Name),
                            },
                            stat = new DynamicVideoCardStatModel
                            {
                                view = dynamic.DynArchive.View,
                            },
                        };
                        break;
                    case GrpcDynamicType.Pgc:
                        if (dynamic?.DynPgc == null)
                        {
                            continue;
                        }
                        item.desc.type = 512;
                        item.desc.rid = dynamic.DynPgc.Aid.ToString();
                        item.Season = new DynamicSeasonCardModel
                        {
                            aid = dynamic.DynPgc.Aid.ToString(),
                            cover = dynamic.DynPgc.Cover,
                            url = dynamic.DynPgc.JumpUrl,
                            new_desc = FirstNonEmpty(dynamic.DynPgc.CoverLeftText1, dynamic.DynPgc.CoverLeftText2),
                            season = new DynamicSeasonCardApiSeasonInfoModel
                            {
                                title = dynamic.DynPgc.Title,
                                season_id = (int)dynamic.DynPgc.SeasonId,
                                cover = dynamic.DynPgc.Cover,
                            }
                        };
                        break;
                    case GrpcDynamicType.UgcSeason:
                        if (dynamic?.DynUgcSeason == null)
                        {
                            continue;
                        }
                        item.desc.type = 4310;
                        item.desc.rid = dynamic.DynUgcSeason.Avid.ToString();
                        item.Video = new DynamicVideoCardModel
                        {
                            aid = dynamic.DynUgcSeason.Avid.ToString(),
                            duration = (int)dynamic.DynUgcSeason.Duration,
                            pic = dynamic.DynUgcSeason.Cover,
                            title = dynamic.DynUgcSeason.Title,
                            desc = dynamic.DynUgcSeason.CoverLeftText1,
                            jump_url = FirstNonEmpty(dynamic.DynUgcSeason.JumpUrl, dynamic.DynUgcSeason.Uri),
                            owner = new DynamicVideoCardOwnerModel
                            {
                                face = source.Extend.OrigFace,
                                mid = author?.Mid.ToString() ?? "",
                                name = FirstNonEmpty(source.Extend.OrigName, author?.Author?.Name),
                            },
                            stat = new DynamicVideoCardStatModel(),
                        };
                        break;
                    default:
                        continue;
                }

                result.Add(item);
            }

            return result;
        }

        public static List<DynamicCardModel2024> MapToUserCards(IEnumerable<DynamicItem> dynamicItems)
        {
            var result = new List<DynamicCardModel2024>();
            if (dynamicItems == null)
            {
                return result;
            }

            foreach (var source in dynamicItems)
            {
                if (source == null || source.Extend == null)
                {
                    continue;
                }

                var card = MapUserCard(source);
                if (card != null)
                {
                    result.Add(card);
                }
            }

            return result;
        }

        public static List<DynamicCardModel2024> MapToUserSeasonCards(IEnumerable<FollowListItem> seasonItems)
        {
            var result = new List<DynamicCardModel2024>();
            if (seasonItems == null)
            {
                return result;
            }

            foreach (var source in seasonItems)
            {
                if (source == null)
                {
                    continue;
                }

                var id = source.SeasonId.ToString();
                result.Add(new DynamicCardModel2024
                {
                    // 关注列表的合集没有作者也没有稿件，使用专门的合成卡片类型。
                    type = "CUSTOM_SEASON",
                    id_str = id,
                    // 合成卡片没有可评论的目标，空的 basic 仅供显示层读取字段。
                    basic = new DynamicCardModel2024_basic(),
                    modules = new DynamicCardModel2024_modules
                    {
                        module_dynamic = new DynamicCardModel2024_dynamic
                        {
                            major = new DynamicCardModel2024_major
                            {
                                pgc = new DynamicCardModel2024_season
                                {
                                    season_id = (int)source.SeasonId,
                                    cover = source.Cover,
                                    title = source.Title,
                                    jump_url = source.Url,
                                    stat = new DynamicCardModel2024_astat
                                    {
                                        play = source.SubTitle,
                                    }
                                }
                            }
                        }
                    },
                    visible = true,
                });
            }

            return result;
        }


        private static DynamicCardModel2024 MapUserCard(DynamicItem source)
        {
            if (source == null || source.Extend == null)
            {
                return null;
            }
            // 广告与通栏没有可展示的动态内容，在归一化之前直接丢弃。
            if (source.CardType == GrpcDynamicType.Ad || source.CardType == GrpcDynamicType.Banner)
            {
                return null;
            }

            var author = GetModule(source, DynModuleType.ModuleAuthor)?.ModuleAuthor;
            var dynamic = GetModule(source, DynModuleType.ModuleDynamic)?.ModuleDynamic;
            var stat = GetModule(source, DynModuleType.ModuleStat)?.ModuleStat;
            var moduleDesc = GetModule(source, DynModuleType.ModuleDesc)?.ModuleDesc;
            var fold = GetModule(source, DynModuleType.ModuleFold)?.ModuleFold;

            var commentId = GetCommentId(source.CardType, dynamic) ?? source.Extend.DynIdStr;
            var card = new DynamicCardModel2024
            {
                id_str = source.Extend.DynIdStr,
                type = MapCardType(source.CardType),
                visible = true,
                basic = new DynamicCardModel2024_basic
                {
                    comment_id_str = commentId,
                    rid_str = commentId,
                    comment_type = 1,
                },
                modules = new DynamicCardModel2024_modules
                {
                    module_author = MapAuthor(author),
                    module_stat = new DynamicCardModel2024_stat
                    {
                        comment = new DynamicCardModel2024_comment { count = ToInt(stat?.Reply ?? 0) },
                        forward = new DynamicCardModel2024_forward { count = ToInt(stat?.Repost ?? 0) },
                        like = new DynamicCardModel2024_like
                        {
                            count = ToInt(stat?.Like ?? 0),
                            status = stat?.LikeInfo?.IsLike ?? false,
                        },
                    },
                    module_dynamic = MapDynamic(dynamic, moduleDesc, fold),
                }
            };

            if (source.CardType == GrpcDynamicType.Forward)
            {
                // 原动态可能属于被丢弃的类型，此时用占位卡片保证父卡片仍然可读。
                card.orig = MapUserCard(dynamic?.DynForward?.Item) ?? MissingCard(source.Extend.OrigDynIdStr);
            }

            return card;
        }

        /// <summary>
        /// 评论与详情使用的 oid：视频/番剧/合集用稿件 AID，专栏用 cvid，音乐用歌曲 ID，图片用相册对象 ID；其余卡片退回动态 ID。
        /// </summary>
        private static string GetCommentId(GrpcDynamicType cardType, ModuleDynamic dynamic)
        {
            if (dynamic == null)
            {
                return null;
            }
            switch (cardType)
            {
                case GrpcDynamicType.Av:
                    return dynamic.DynArchive == null ? null : dynamic.DynArchive.Avid.ToString();
                case GrpcDynamicType.Article:
                    return dynamic.DynArticle == null ? null : dynamic.DynArticle.Id.ToString();
                case GrpcDynamicType.Pgc:
                    return dynamic.DynPgc == null ? null : dynamic.DynPgc.Aid.ToString();
                case GrpcDynamicType.UgcSeason:
                    return dynamic.DynUgcSeason == null ? null : dynamic.DynUgcSeason.Avid.ToString();
                case GrpcDynamicType.Music:
                    return dynamic.DynMusic == null ? null : dynamic.DynMusic.Id.ToString();
                case GrpcDynamicType.Draw:
                    return dynamic.DynDraw == null ? null : dynamic.DynDraw.Id.ToString();
                default:
                    return null;
            }
        }

        private static DynamicCardModel2024_dynamic MapDynamic(ModuleDynamic source, ModuleDesc moduleDesc, ModuleFold fold)
        {
            var result = new DynamicCardModel2024_dynamic();
            if (source == null)
            {
                result.desc = MapDescription(moduleDesc);
                if (fold != null && result.desc == null)
                {
                    result.desc = new DynamicCardModel2024_desc
                    {
                        text = fold.Text,
                        rich_text_nodes = new List<DynamicCardModel2024_node>(),
                    };
                }
                return result;
            }

            result.desc = MapDescription(moduleDesc);
            result.major = new DynamicCardModel2024_major();
            if (result.desc == null && fold != null)
            {
                result.desc = new DynamicCardModel2024_desc
                {
                    text = fold.Text,
                    rich_text_nodes = new List<DynamicCardModel2024_node>
                    {
                        new DynamicCardModel2024_node
                        {
                            text = fold.Text,
                            orig_text = fold.Text,
                            type = "RICH_TEXT_NODE_TYPE_TEXT",
                        }
                    },
                };
            }
            if (source.DynArchive != null)
            {
                result.major.archive = new DynamicCardModel2024_archive
                {
                    aid = source.DynArchive.Avid.ToString(),
                    bvid = source.DynArchive.Bvid,
                    cover = source.DynArchive.Cover,
                    desc = source.DynArchive.Title,
                    duration_text = source.DynArchive.Duration.ToString(),
                    jump_url = FirstNonEmpty(source.DynArchive.JumpUrl, source.DynArchive.Uri),
                    title = source.DynArchive.Title,
                    stat = new DynamicCardModel2024_astat
                    {
                        play = source.DynArchive.CoverLeftText2,
                        danmaku = source.DynArchive.CoverLeftText3,
                    },
                };
            }
            else if (source.DynPgc != null)
            {
                result.major.pgc = new DynamicCardModel2024_season
                {
                    aid = source.DynPgc.Aid > 0 ? source.DynPgc.Aid.ToString() : null,
                    cover = source.DynPgc.Cover,
                    epid = (int)source.DynPgc.Epid,
                    season_id = (int)source.DynPgc.SeasonId,
                    jump_url = FirstNonEmpty(source.DynPgc.JumpUrl, source.DynPgc.Uri),
                    title = source.DynPgc.Title,
                    stat = new DynamicCardModel2024_astat
                    {
                        play = source.DynPgc.CoverLeftText2,
                        danmaku = source.DynPgc.CoverLeftText3,
                    },
                };
            }
            else if (source.DynUgcSeason != null)
            {
                result.major.ugc_season = new DynamicCardModel2024_list
                {
                    aid = source.DynUgcSeason.Avid,
                    cover = source.DynUgcSeason.Cover,
                    desc = source.DynUgcSeason.CoverLeftText1,
                    duration_text = source.DynUgcSeason.Duration.ToString(),
                    jump_url = FirstNonEmpty(source.DynUgcSeason.JumpUrl, source.DynUgcSeason.Uri),
                    title = source.DynUgcSeason.Title,
                    stat = new DynamicCardModel2024_astat
                    {
                        play = source.DynUgcSeason.CoverLeftText2,
                        danmaku = source.DynUgcSeason.CoverLeftText3,
                    },
                };
            }
            else if (source.DynMedialist != null)
            {
                // 播单没有稿件 AID 与统计，只有 id/uri/title/sub_title/cover。
                result.major.medialist = new DynamicCardModel2024_list
                {
                    id = source.DynMedialist.Id,
                    cover = source.DynMedialist.Cover,
                    desc = source.DynMedialist.SubTitle,
                    jump_url = source.DynMedialist.Uri,
                    title = source.DynMedialist.Title,
                };
            }
            else if (source.DynArticle != null)
            {
                result.major.article = new DynamicCardModel2024_article
                {
                    id = (int)source.DynArticle.Id,
                    covers = source.DynArticle.Covers.ToList(),
                    desc = source.DynArticle.Desc,
                    jump_url = source.DynArticle.Uri,
                    label = source.DynArticle.Label,
                    title = source.DynArticle.Title,
                };
            }
            else if (source.DynDraw != null)
            {
                result.major.draw = new DynamicCardModel2024_draw
                {
                    id = source.DynDraw.Id,
                    items = source.DynDraw.Items.Select(x => new DynamicCardModel2024_pic
                    {
                        src = x.Src,
                        width = (int)x.Width,
                        height = (int)x.Height,
                        size = x.Size,
                    }).ToList(),
                };
            }
            else if (source.DynCommonLive != null)
            {
                result.major.live = new DynamicCardModel2024_live
                {
                    id = source.DynCommonLive.Id.ToString(),
                    cover = source.DynCommonLive.Cover,
                    title = source.DynCommonLive.Title,
                    desc_first = source.DynCommonLive.CoverLabel,
                    desc_second = source.DynCommonLive.CoverLabel2,
                    jump_url = FirstNonEmpty(source.DynCommonLive.Uri, "https://live.bilibili.com/" + source.DynCommonLive.Id),
                    live_state = (int)source.DynCommonLive.LiveState,
                };
            }
            else if (source.DynLiveRcmd != null)
            {
                result.major.live_rcmd = new DynamicCardModel2024_live_rcmd
                {
                    content = source.DynLiveRcmd.Content,
                };
                try
                {
                    var playInfo = Newtonsoft.Json.Linq.JObject.Parse(source.DynLiveRcmd.Content)?["live_play_info"];
                    result.major.live = new DynamicCardModel2024_live
                    {
                        id = playInfo?["room_id"]?.ToString() ?? "",
                        cover = playInfo?["cover"]?.ToString() ?? "",
                        title = playInfo?["title"]?.ToString() ?? "",
                        desc_first = playInfo?["area_name"]?.ToString() ?? "",
                        desc_second = playInfo?["watched_show"]?["text_large"]?.ToString() ?? "",
                        jump_url = playInfo?["room_id"] == null
                            ? ""
                            : "https://live.bilibili.com/" + playInfo["room_id"].ToString(),
                    };
                }
                catch
                {
                    // Keep the source live recommendation payload for the legacy placeholder.
                }
            }
            else if (source.DynMusic != null)
            {
                result.major.article = new DynamicCardModel2024_article
                {
                    id = (int)source.DynMusic.Id,
                    covers = new List<string> { source.DynMusic.Cover },
                    label = source.DynMusic.Label1,
                    jump_url = source.DynMusic.Uri,
                    title = source.DynMusic.Title,
                };
            }

            return result;
        }

        private static DynamicCardModel2024_desc MapDescription(Bilibili.App.Dynamic.V2.ModuleDesc source)
        {
            if (source == null)
            {
                return null;
            }

            var nodes = source.Desc.Select(MapDescriptionNode).ToList();
            if (nodes.Count == 0 && !string.IsNullOrEmpty(source.Text))
            {
                nodes.Add(new DynamicCardModel2024_node
                {
                    text = source.Text,
                    orig_text = source.Text,
                    type = "RICH_TEXT_NODE_TYPE_TEXT",
                });
            }

            return new DynamicCardModel2024_desc
            {
                text = source.Text,
                rich_text_nodes = nodes,
            };
        }

        private static DynamicCardModel2024_node MapDescriptionNode(Description source)
        {
            return new DynamicCardModel2024_node
            {
                text = source.Text,
                orig_text = source.OrigText,
                type = MapDescriptionType(source.Type.ToString()),
                jump_url = source.Uri,
                icon_name = source.IconName,
                rid = source.Rid,
                emoji = string.IsNullOrEmpty(source.IconUrl) ? null : new DynamicCardModel2024_emojiinfo
                {
                    icon_url = source.IconUrl,
                    text = source.Text,
                    size = source.EmojiSize,
                },
            };
        }

        private static DynamicCardModel2024_author MapAuthor(ModuleAuthor source)
        {
            var user = source?.Author;
            if (source == null)
            {
                return null;
            }

            return new DynamicCardModel2024_author
            {
                mid = source.Mid,
                name = user?.Name ?? "",
                face = user?.Face ?? "",
                pub_time = source.PtimeLabelText,
                pub_ts = 0,
                following = source.Attend != 0,
                vip = user?.Vip == null ? null : new DynamicCardModel2024_vip
                {
                    status = user.Vip.Status,
                    type = user.Vip.Type,
                    theme_type = user.Vip.ThemeType,
                    nickname_color = user.Vip.NicknameColor,
                },
                official_verify = user?.Official == null ? null : new DynamicCardModel2024_off
                {
                    type = user.Official.Type,
                    desc = user.Official.Desc,
                },
                pendant = user?.Pendant == null ? null : new DynamicCardModel2024_pendant
                {
                    pid = user.Pendant.Pid,
                    name = user.Pendant.Name,
                    image = user.Pendant.Image,
                    expire = (int)user.Pendant.Expire,
                },
                decorate = source.DecorateCard == null ? null : new DynamicCardModel2024_decorate
                {
                    name = source.DecorateCard.Fan?.NumberStr,
                    card_url = source.DecorateCard.CardUrl,
                    jump_url = source.DecorateCard.JumpUrl,
                    fan = source.DecorateCard.Fan == null ? null : new DynamicCardModel2024_fan
                    {
                        is_fan = source.DecorateCard.Fan.IsFan != 0,
                        number = source.DecorateCard.Fan.Number,
                        color = source.DecorateCard.Fan.Color,
                    },
                },
            };
        }

        private static Module GetModule(DynamicItem source, DynModuleType type)
        {
            return source?.Modules?.FirstOrDefault(x => x.ModuleType == type);
        }

        private static string MapCardType(GrpcDynamicType type)
        {
            switch (type)
            {
                case GrpcDynamicType.Word: return "DYNAMIC_TYPE_WORD";
                case GrpcDynamicType.Forward: return "DYNAMIC_TYPE_FORWARD";
                case GrpcDynamicType.Draw: return "DYNAMIC_TYPE_DRAW";
                case GrpcDynamicType.Av: return "DYNAMIC_TYPE_AV";
                case GrpcDynamicType.Pgc: return "DYNAMIC_TYPE_PGC_UNION";
                case GrpcDynamicType.UgcSeason: return "DYNAMIC_TYPE_UGC_SEASON";
                case GrpcDynamicType.Article: return "DYNAMIC_TYPE_ARTICLE";
                case GrpcDynamicType.Music: return "DYNAMIC_TYPE_MUSIC";
                case GrpcDynamicType.Medialist: return "DYNAMIC_TYPE_MEDIALIST";
                case GrpcDynamicType.Live: return "DYNAMIC_TYPE_LIVE";
                case GrpcDynamicType.LiveRcmd: return "DYNAMIC_TYPE_LIVE_RCMD";
                case GrpcDynamicType.Fold: return "DYNAMIC_TYPE_WORD";
                default: return "DYNAMIC_TYPE_UNSUPPORTED";
            }
        }


        private static string FirstDescription(IEnumerable<Description> descriptions)
        {
            return descriptions?.FirstOrDefault()?.Text ?? "";
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values?.FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? "";
        }









    }
}

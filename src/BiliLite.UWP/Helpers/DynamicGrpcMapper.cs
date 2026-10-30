using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bilibili.App.Dynamic.V2;
using BiliLite.Modules;
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

        /// <summary>
        /// web 聚合动态流（x/polymer/web-dynamic/v1/feed/all）的卡片映射。字段名与 gRPC 的
        /// DynamicItem/Module* 一一对应，所以逐类填进同一套 2024 卡片模型，渲染层不必区分来源。
        /// 不认识的类型也给一张"不支持"的卡：一页动态不能因为多了一种卡片类型就整体失败。
        /// </summary>
        public static List<DynamicCardModel2024> MapToUserFeedCards(JArray feedItems)
        {
            var result = new List<DynamicCardModel2024>();
            if (feedItems == null)
            {
                return result;
            }

            foreach (var token in feedItems)
            {
                var source = token as JObject;
                if (source == null)
                {
                    continue;
                }

                result.Add(MapWebCard(source));
            }

            return result;
        }

        /// <summary>
        /// 首页视频动态：这条流只认投稿视频（desc.type=8），其它类型没有对应的模板，由调用方跳过。
        /// </summary>
        public static List<DynamicItemModel> MapToHomeVideoItems(JArray feedItems)
        {
            var result = new List<DynamicItemModel>();
            if (feedItems == null)
            {
                return result;
            }

            foreach (var token in feedItems)
            {
                var source = token as JObject;
                if (source == null || source["type"]?.ToString() != "DYNAMIC_TYPE_AV")
                {
                    continue;
                }

                var author = source["modules"]?["module_author"] as JObject;
                var archive = source["modules"]?["module_dynamic"]?["major"]?["archive"] as JObject;
                if (archive == null)
                {
                    continue;
                }

                var id = source["id_str"]?.ToString() ?? "";
                var play = ParseCount(archive["stat"]?["play"]?.ToString());
                result.Add(new DynamicItemModel
                {
                    desc = new DynamicDescModel
                    {
                        uid = author?["mid"]?.ToString() ?? "",
                        dynamic_id = id,
                        dynamic_id_str = id,
                        time_text = author?["pub_time"]?.ToString() ?? "",
                        type = 8,
                        rid = archive["aid"]?.ToString() ?? "",
                        view = play,
                    },
                    Video = new DynamicVideoCardModel
                    {
                        aid = archive["aid"]?.ToString() ?? "",
                        duration = ParseDuration(archive["duration_text"]?.ToString()),
                        pic = archive["cover"]?.ToString() ?? "",
                        title = archive["title"]?.ToString() ?? "",
                        desc = archive["desc"]?.ToString() ?? "",
                        jump_url = NormalizeWebUrl(archive["jump_url"]?.ToString()),
                        owner = new DynamicVideoCardOwnerModel
                        {
                            face = author?["face"]?.ToString() ?? "",
                            mid = author?["mid"]?.ToString() ?? "",
                            name = author?["name"]?.ToString() ?? "",
                        },
                        stat = new DynamicVideoCardStatModel
                        {
                            view = play,
                            danmaku = ParseCount(archive["stat"]?["danmaku"]?.ToString()),
                        },
                    },
                });
            }

            return result;
        }

        private static DynamicCardModel2024 MapWebCard(JObject source)
        {
            var id = source["id_str"]?.ToString() ?? "";
            var itemType = source["type"]?.ToString() ?? "";
            var basic = source["basic"] as JObject;
            var modules = source["modules"] as JObject;
            var major = modules?["module_dynamic"]?["major"] as JObject;
            var cardType = MapWebCardType(itemType);
            if (cardType == "DYNAMIC_TYPE_UNSUPPORTED")
            {
                LogHelper.Log("Dynamic feed mapping stage=unsupported-type type=" + itemType + " dynamicId=" + id, LogType.DEBUG);
            }

            var card = new DynamicCardModel2024
            {
                type = cardType,
                id_str = id,
                visible = true,
                basic = new DynamicCardModel2024_basic
                {
                    comment_id_str = basic?["comment_id_str"]?.ToString() ?? id,
                    comment_type = ToInt(basic?["comment_type"]?.ToString()),
                    rid_str = basic?["rid_str"]?.ToString() ?? id,
                },
                modules = new DynamicCardModel2024_modules
                {
                    module_author = MapWebAuthor(modules?["module_author"] as JObject),
                    module_stat = MapWebStat(modules?["module_stat"] as JObject),
                    module_dynamic = new DynamicCardModel2024_dynamic
                    {
                        desc = MapWebDescription(modules?["module_dynamic"]?["desc"] as JObject),
                        major = MapWebMajor(major),
                    },
                },
            };

            if (itemType == "DYNAMIC_TYPE_FORWARD")
            {
                // 原动态可能是任何类型，映射不出来时用占位卡，父卡片照样可读
                var orig = source["orig"] as JObject;
                card.orig = orig == null ? MissingCard(card.basic.rid_str) : MapWebCard(orig);
            }

            return card;
        }

        private static DynamicCardModel2024_author MapWebAuthor(JObject source)
        {
            if (source == null)
            {
                return null;
            }

            return new DynamicCardModel2024_author
            {
                mid = ToLong(source["mid"]?.ToString()),
                name = source["name"]?.ToString() ?? "",
                face = source["face"]?.ToString() ?? "",
                pub_time = source["pub_time"]?.ToString() ?? "",
                // web 下发的 pub_ts 是字符串，卡片的时间显示与排序都靠它
                pub_ts = ToLong(source["pub_ts"]?.ToString()),
                vip = source["vip"] == null ? null : new DynamicCardModel2024_vip
                {
                    status = ToInt(source["vip"]?["status"]?.ToString()),
                    type = ToInt(source["vip"]?["type"]?.ToString()),
                    theme_type = ToInt(source["vip"]?["theme_type"]?.ToString()),
                    nickname_color = source["vip"]?["nickname_color"]?.ToString(),
                },
                official_verify = source["official_verify"] == null ? null : new DynamicCardModel2024_off
                {
                    type = ToInt(source["official_verify"]?["type"]?.ToString()),
                    desc = source["official_verify"]?["desc"]?.ToString() ?? "",
                },
                pendant = source["pendant"] == null ? null : new DynamicCardModel2024_pendant
                {
                    pid = ToLong(source["pendant"]?["pid"]?.ToString()),
                    name = source["pendant"]?["name"]?.ToString() ?? "",
                    image = source["pendant"]?["image"]?.ToString() ?? "",
                    expire = ToInt(source["pendant"]?["expire"]?.ToString()),
                },
            };
        }

        private static DynamicCardModel2024_stat MapWebStat(JObject source)
        {
            if (source == null)
            {
                return new DynamicCardModel2024_stat();
            }

            return new DynamicCardModel2024_stat
            {
                comment = new DynamicCardModel2024_comment
                {
                    count = ToInt(source["comment"]?["count"]?.ToString()),
                    forbidden = ToBool(source["comment"]?["forbidden"]),
                },
                forward = new DynamicCardModel2024_forward
                {
                    count = ToInt(source["forward"]?["count"]?.ToString()),
                    forbidden = ToBool(source["forward"]?["forbidden"]),
                },
                like = new DynamicCardModel2024_like
                {
                    count = ToInt(source["like"]?["count"]?.ToString()),
                    status = ToBool(source["like"]?["status"]),
                },
            };
        }

        private static DynamicCardModel2024_desc MapWebDescription(JObject source)
        {
            if (source == null)
            {
                return null;
            }

            var nodes = new List<DynamicCardModel2024_node>();
            var richNodes = source["rich_text_nodes"] as JArray;
            if (richNodes != null)
            {
                foreach (var token in richNodes)
                {
                    var node = token as JObject;
                    if (node == null)
                    {
                        continue;
                    }
                    nodes.Add(new DynamicCardModel2024_node
                    {
                        text = node["text"]?.ToString() ?? "",
                        orig_text = node["orig_text"]?.ToString() ?? "",
                        type = MapDescriptionType(node["type"]?.ToString()),
                        jump_url = node["jump_url"]?.ToString() ?? "",
                        icon_name = node["icon_name"]?.ToString() ?? "",
                        rid = node["rid"]?.ToString() ?? "",
                        emoji = node["emoji"] == null ? null : new DynamicCardModel2024_emojiinfo
                        {
                            icon_url = node["emoji"]?["icon_url"]?.ToString() ?? node["icon_url"]?.ToString() ?? "",
                            text = node["emoji"]?["text"]?.ToString() ?? "",
                            size = ToInt(node["emoji"]?["size"]?.ToString()),
                            type = ToInt(node["emoji"]?["type"]?.ToString()),
                        },
                    });
                }
            }

            var text = source["text"]?.ToString() ?? "";
            if (nodes.Count == 0 && !string.IsNullOrEmpty(text))
            {
                nodes.Add(new DynamicCardModel2024_node
                {
                    text = text,
                    orig_text = text,
                    type = "RICH_TEXT_NODE_TYPE_TEXT",
                });
            }

            return new DynamicCardModel2024_desc
            {
                text = text,
                rich_text_nodes = nodes,
            };
        }

        /// <summary>
        /// web 的 major 是"字段全在、只有一个非空"的结构，与 gRPC 的 ModuleDynamic 一一对应，
        /// 所以这里按同样的优先级挑出非空的那一路。
        /// </summary>
        private static DynamicCardModel2024_major MapWebMajor(JObject major)
        {
            var result = new DynamicCardModel2024_major();
            if (major == null)
            {
                return result;
            }

            var archive = major["archive"] as JObject;
            if (archive != null)
            {
                result.archive = new DynamicCardModel2024_archive
                {
                    aid = archive["aid"]?.ToString() ?? "",
                    bvid = archive["bvid"]?.ToString() ?? "",
                    cover = archive["cover"]?.ToString() ?? "",
                    desc = archive["desc"]?.ToString() ?? "",
                    duration_text = archive["duration_text"]?.ToString() ?? "",
                    jump_url = NormalizeWebUrl(archive["jump_url"]?.ToString()),
                    title = archive["title"]?.ToString() ?? "",
                    stat = new DynamicCardModel2024_astat
                    {
                        play = archive["stat"]?["play"]?.ToString() ?? "",
                        danmaku = archive["stat"]?["danmaku"]?.ToString() ?? "",
                    },
                };
                return result;
            }

            var pgc = major["pgc"] as JObject;
            if (pgc != null)
            {
                result.pgc = new DynamicCardModel2024_season
                {
                    aid = pgc["aid"]?.ToString(),
                    cover = pgc["cover"]?.ToString() ?? "",
                    epid = ToInt(pgc["epid"]?.ToString()),
                    season_id = ToInt(pgc["season_id"]?.ToString()),
                    sub_type = ToInt(pgc["sub_type"]?.ToString()),
                    jump_url = NormalizeWebUrl(pgc["jump_url"]?.ToString()),
                    title = pgc["title"]?.ToString() ?? "",
                    stat = new DynamicCardModel2024_astat
                    {
                        play = pgc["stat"]?["play"]?.ToString() ?? "",
                        danmaku = pgc["stat"]?["danmaku"]?.ToString() ?? "",
                    },
                };
                return result;
            }

            var draw = major["draw"] as JObject;
            if (draw != null)
            {
                var pics = new List<DynamicCardModel2024_pic>();
                var drawItems = draw["items"] as JArray;
                if (drawItems != null)
                {
                    foreach (var token in drawItems)
                    {
                        var pic = token as JObject;
                        if (pic == null)
                        {
                            continue;
                        }
                        pics.Add(new DynamicCardModel2024_pic
                        {
                            src = pic["src"]?.ToString() ?? "",
                            width = ToInt(pic["width"]?.ToString()),
                            height = ToInt(pic["height"]?.ToString()),
                        });
                    }
                }
                result.draw = new DynamicCardModel2024_draw
                {
                    id = ToLong(draw["id"]?.ToString()),
                    items = pics,
                };
                return result;
            }

            var article = major["article"] as JObject;
            if (article != null)
            {
                result.article = new DynamicCardModel2024_article
                {
                    id = ToInt(article["id"]?.ToString()),
                    covers = ReadStringArray(article["covers"] as JArray),
                    desc = article["desc"]?.ToString() ?? "",
                    jump_url = NormalizeWebUrl(article["jump_url"]?.ToString()),
                    label = article["label"]?.ToString() ?? "",
                    title = article["title"]?.ToString() ?? "",
                };
                return result;
            }

            var opus = major["opus"] as JObject;
            if (opus != null)
            {
                // 图文（OPUS）在渲染层就是专栏那张卡：封面取首图，正文取 summary.text
                var covers = new List<string>();
                var opusPics = opus["pics"] as JArray;
                if (opusPics != null)
                {
                    covers.AddRange(opusPics
                        .Select(x => x?["url"]?.ToString())
                        .Where(x => !string.IsNullOrEmpty(x)));
                }
                result.article = new DynamicCardModel2024_article
                {
                    covers = covers,
                    desc = opus["summary"]?["text"]?.ToString() ?? "",
                    jump_url = NormalizeWebUrl(opus["jump_url"]?.ToString()),
                    title = opus["title"]?.ToString() ?? "",
                };
                return result;
            }

            var live = major["live"] as JObject;
            if (live != null)
            {
                result.live = new DynamicCardModel2024_live
                {
                    id = live["id"]?.ToString() ?? "",
                    cover = live["cover"]?.ToString() ?? "",
                    title = live["title"]?.ToString() ?? "",
                    desc_first = live["area_name"]?.ToString() ?? "",
                    desc_second = live["watched_show"]?["text_large"]?.ToString() ?? "",
                    jump_url = NormalizeWebUrl(live["jump_url"]?.ToString()),
                    live_state = ToInt(live["live_state"]?.ToString()),
                };
                return result;
            }

            var liveRcmd = major["live_rcmd"] as JObject;
            if (liveRcmd != null)
            {
                var content = liveRcmd["content"]?.ToString() ?? "";
                result.live_rcmd = new DynamicCardModel2024_live_rcmd
                {
                    content = content,
                };
                try
                {
                    var playInfo = JObject.Parse(content)?["live_play_info"];
                    result.live = new DynamicCardModel2024_live
                    {
                        id = playInfo?["room_id"]?.ToString() ?? "",
                        cover = playInfo?["cover"]?.ToString() ?? "",
                        title = playInfo?["title"]?.ToString() ?? "",
                        desc_first = playInfo?["area_name"]?.ToString() ?? "",
                        desc_second = playInfo?["watched_show"]?["text_large"]?.ToString() ?? "",
                        jump_url = playInfo?["room_id"] == null
                            ? ""
                            : "https://live.bilibili.com/" + playInfo["room_id"],
                    };
                }
                catch
                {
                    // 推荐直播的 content 是嵌套的 JSON 字符串：解不开就只留原样内容给占位卡
                }
                return result;
            }

            var music = major["music"] as JObject;
            if (music != null)
            {
                // 音乐卡借的是专栏那张卡，与 gRPC 的 DynMusic 映射一致
                result.article = new DynamicCardModel2024_article
                {
                    id = ToInt(music["id"]?.ToString()),
                    covers = new List<string> { music["cover"]?.ToString() ?? "" },
                    label = music["label"]?.ToString() ?? "",
                    jump_url = NormalizeWebUrl(music["jump_url"]?.ToString()),
                    title = music["title"]?.ToString() ?? "",
                };
                return result;
            }

            return result;
        }

        private static string MapWebCardType(string itemType)
        {
            switch (itemType)
            {
                case "DYNAMIC_TYPE_WORD": return "DYNAMIC_TYPE_WORD";
                case "DYNAMIC_TYPE_FORWARD": return "DYNAMIC_TYPE_FORWARD";
                case "DYNAMIC_TYPE_DRAW": return "DYNAMIC_TYPE_DRAW";
                case "DYNAMIC_TYPE_AV": return "DYNAMIC_TYPE_AV";
                case "DYNAMIC_TYPE_MUSIC": return "DYNAMIC_TYPE_MUSIC";
                case "DYNAMIC_TYPE_LIVE": return "DYNAMIC_TYPE_LIVE";
                case "DYNAMIC_TYPE_LIVE_RCMD": return "DYNAMIC_TYPE_LIVE_RCMD";
                case "DYNAMIC_TYPE_NONE": return "DYNAMIC_TYPE_NONE";
                case "DYNAMIC_TYPE_PGC":
                case "DYNAMIC_TYPE_PGC_UNION": return "DYNAMIC_TYPE_PGC_UNION";
                case "DYNAMIC_TYPE_ARTICLE": return "DYNAMIC_TYPE_ARTICLE";
                // 渲染层没有 OPUS 这个类型：专栏与图文在它眼里是同一张卡
                case "DYNAMIC_TYPE_OPUS": return "DYNAMIC_TYPE_ARTICLE";
                default: return "DYNAMIC_TYPE_UNSUPPORTED";
            }
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

        private static string MapDescriptionType(string type)
        {
            var normalized = (type ?? "").ToLowerInvariant();
            if (normalized.Contains("aite") || normalized.Contains("at")) return "RICH_TEXT_NODE_TYPE_AT";
            if (normalized.Contains("topic")) return "RICH_TEXT_NODE_TYPE_TOPIC";
            if (normalized.Contains("web") || normalized.Contains("link")) return "RICH_TEXT_NODE_TYPE_WEB";
            return "RICH_TEXT_NODE_TYPE_TEXT";
        }

        private static string FirstDescription(IEnumerable<Description> descriptions)
        {
            return descriptions?.FirstOrDefault()?.Text ?? "";
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values?.FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? "";
        }

        private static int ToInt(long value)
        {
            return value > int.MaxValue ? int.MaxValue : value < int.MinValue ? int.MinValue : (int)value;
        }

        private static int ToInt(string value)
        {
            int result;
            return int.TryParse(value, out result) ? result : 0;
        }

        private static long ToLong(string value)
        {
            long result;
            return long.TryParse(value, out result) ? result : 0;
        }

        private static bool ToBool(JToken token)
        {
            return token != null && token.Type == JTokenType.Boolean && token.Value<bool>();
        }

        private static List<string> ReadStringArray(JArray source)
        {
            if (source == null)
            {
                return new List<string>();
            }
            return source
                .Select(x => x?.ToString())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();
        }

        /// <summary>把 "475.6万" / "1.2亿" 这类紧凑计数还原成整数；解析不出来就是 0。</summary>
        private static int ParseCount(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            double multiplier = 1;
            var digits = text;
            if (text.EndsWith("万", StringComparison.Ordinal))
            {
                multiplier = 10000;
                digits = text.Substring(0, text.Length - 1);
            }
            else if (text.EndsWith("亿", StringComparison.Ordinal))
            {
                multiplier = 100000000;
                digits = text.Substring(0, text.Length - 1);
            }

            double value;
            if (!double.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return 0;
            }

            var count = value * multiplier;
            return count > int.MaxValue ? int.MaxValue : (int)count;
        }

        /// <summary>把 "24:02" / "1:02:03" 还原成秒数；解析不出来就是 0。</summary>
        private static int ParseDuration(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            var seconds = 0;
            foreach (var part in text.Split(':'))
            {
                int value;
                if (!int.TryParse(part, out value))
                {
                    return 0;
                }
                seconds = seconds * 60 + value;
            }
            return seconds;
        }

        /// <summary>web 卡片的跳转地址是协议相对的（//www.bilibili.com/...），补成完整地址。</summary>
        private static string NormalizeWebUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return "";
            }
            return url.StartsWith("//", StringComparison.Ordinal) ? "https:" + url : url;
        }

        private static DynamicCardModel2024 MissingCard(string dynamicId)
        {
            return new DynamicCardModel2024
            {
                type = "DYNAMIC_TYPE_NONE",
                id_str = dynamicId ?? "",
                basic = new DynamicCardModel2024_basic
                {
                    rid_str = dynamicId ?? "",
                    comment_id_str = dynamicId ?? "",
                },
                modules = new DynamicCardModel2024_modules
                {
                    module_dynamic = new DynamicCardModel2024_dynamic
                    {
                        desc = new DynamicCardModel2024_desc
                        {
                            text = "原动态已不可见",
                            rich_text_nodes = new List<DynamicCardModel2024_node>(),
                        },
                        major = new DynamicCardModel2024_major(),
                    }
                },
                visible = true,
            };
        }
    }
}

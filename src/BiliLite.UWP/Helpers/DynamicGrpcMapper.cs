using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bilibili.App.Dynamic.V2;
using BiliLite.Modules;
using BiliLite.Parsing.Dynamic;
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
            // 正文之外的两块：附件卡（预约/投票/通用卡）与图文摘要。以前都不读，卡片就少了一截内容。
            var additional = GetModule(source, DynModuleType.ModuleAdditional)?.ModuleAdditional;
            var opusSummary = GetModule(source, DynModuleType.ModuleOpusSummary)?.ModuleOpusSummary
                ?? source.Extend.OpusSummary;

            var comment = GetCommentTarget(source.CardType, dynamic, source.Extend.DynIdStr);
            var commentId = comment == null ? source.Extend.DynIdStr : comment.Oid;
            var card = new DynamicCardModel2024
            {
                id_str = source.Extend.DynIdStr,
                type = MapCardType(source.CardType),
                visible = true,
                basic = new DynamicCardModel2024_basic
                {
                    // 评论目标是一对 (oid, 评论接口类型)：只写 oid 或把类型固定成占位 1，
                    // 调用方就只能按显示类型再猜一次。类型未知时才退回 0，交给按类型的老映射。
                    comment_id_str = commentId,
                    rid_str = commentId,
                    comment_type = comment == null ? 0 : comment.Type,
                },
                modules = new DynamicCardModel2024_modules
                {
                    module_author = MapAuthor(author),
                    module_stat = new DynamicCardModel2024_stat
                    {
                        comment = new DynamicCardModel2024_comment
                        {
                            count = ToInt(stat?.Reply ?? 0),
                            forbidden = stat?.NoComment ?? false,
                        },
                        forward = new DynamicCardModel2024_forward
                        {
                            count = ToInt(stat?.Repost ?? 0),
                            forbidden = stat?.NoForward ?? false,
                        },
                        like = new DynamicCardModel2024_like
                        {
                            count = ToInt(stat?.Like ?? 0),
                            status = stat?.LikeInfo?.IsLike ?? false,
                        },
                    },
                    module_dynamic = MapDynamic(dynamic, moduleDesc, fold, additional, opusSummary),
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
        /// 评论使用的 (oid, 评论接口类型) 这一对：视频/番剧/合集用稿件 AID（1），专栏用 cvid（12），
        /// 音频用歌曲 ID（14），图片用相册对象 ID（11），其余卡片退回动态 ID（17）。
        /// 这些是既有文档映射，换成一对之后调用方不必再按显示类型猜一次。
        /// </summary>
        private static DynamicCommentTarget GetCommentTarget(GrpcDynamicType cardType, ModuleDynamic dynamic, string dynamicId)
        {
            if (dynamic != null)
            {
                switch (cardType)
                {
                    case GrpcDynamicType.Av:
                        if (dynamic.DynArchive != null)
                        {
                            return DynamicCommentTarget.From(dynamic.DynArchive.Avid.ToString(), 1);
                        }
                        break;
                    case GrpcDynamicType.Article:
                        if (dynamic.DynArticle != null)
                        {
                            return DynamicCommentTarget.From(dynamic.DynArticle.Id.ToString(), 12);
                        }
                        break;
                    case GrpcDynamicType.Pgc:
                        if (dynamic.DynPgc != null)
                        {
                            return DynamicCommentTarget.From(dynamic.DynPgc.Aid.ToString(), 1);
                        }
                        break;
                    case GrpcDynamicType.UgcSeason:
                        if (dynamic.DynUgcSeason != null)
                        {
                            return DynamicCommentTarget.From(dynamic.DynUgcSeason.Avid.ToString(), 1);
                        }
                        break;
                    case GrpcDynamicType.Music:
                        if (dynamic.DynMusic != null)
                        {
                            return DynamicCommentTarget.From(dynamic.DynMusic.Id.ToString(), 14);
                        }
                        break;
                    case GrpcDynamicType.Draw:
                        if (dynamic.DynDraw != null)
                        {
                            return DynamicCommentTarget.From(dynamic.DynDraw.Id.ToString(), 11);
                        }
                        break;
                }
            }
            return DynamicCommentTarget.From(dynamicId, 17);
        }

        private static DynamicCardModel2024_dynamic MapDynamic(ModuleDynamic source, ModuleDesc moduleDesc, ModuleFold fold,
            ModuleAdditional additional, ModuleOpusSummary opusSummary)
        {
            var result = new DynamicCardModel2024_dynamic();
            result.additional = MapAdditional(additional);
            // gRPC 的图文常常没有 ModuleDesc：正文只在 Extend.OpusSummary / ModuleOpusSummary 的摘要段落里，
            // 不读它，一张 OPUS 卡片就只剩作者和时间。
            var desc = MapDescription(moduleDesc) ?? MapParagraph(opusSummary?.Summary);
            if (source == null)
            {
                result.desc = desc;
                if (fold != null && result.desc == null)
                {
                    result.desc = new DynamicCardModel2024_desc
                    {
                        text = fold.Text,
                        rich_text_nodes = new List<DynamicCardModel2024_node>(),
                    };
                }
                result.major = new DynamicCardModel2024_major();
                AppendOpusCovers(result, opusSummary);
                return result;
            }

            result.desc = desc;
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

            AppendOpusCovers(result, opusSummary);
            return result;
        }

        /// <summary>
        /// OPUS 的摘要段落 → 与 ModuleDesc 同一套 desc：节点顺序、表情、@ 目标都照搬。
        /// 只读文本段落，图片与分割线段落不是正文。
        /// </summary>
        private static DynamicCardModel2024_desc MapParagraph(Paragraph source)
        {
            var text = source?.Text;
            if (text == null || text.Nodes.Count == 0)
            {
                return null;
            }
            var nodes = text.Nodes.Select(MapTextNode).ToList();
            return new DynamicCardModel2024_desc
            {
                text = string.Concat(nodes.Select(x => x.text)),
                rich_text_nodes = nodes,
            };
        }

        private static DynamicCardModel2024_node MapTextNode(TextNode source)
        {
            var node = new DynamicCardModel2024_node
            {
                text = source.RawText,
                orig_text = source.RawText,
                type = "RICH_TEXT_NODE_TYPE_TEXT",
            };
            switch (source.NodeType)
            {
                case TextNode.Types.TextNodeType.Emote:
                    var emote = source.Emote;
                    if (emote != null && !string.IsNullOrEmpty(emote.EmoteUrl))
                    {
                        node.type = "RICH_TEXT_NODE_TYPE_EMOJI";
                        node.emoji = new DynamicCardModel2024_emojiinfo
                        {
                            icon_url = emote.EmoteUrl,
                            text = source.RawText,
                            size = emote.EmoteWidth == null ? 0 : emote.EmoteWidth.EmojiSize,
                        };
                    }
                    break;
                case TextNode.Types.TextNodeType.At:
                    // 目标放在 rid 上；没有 biz_id 时正文层会把它降级成普通文本
                    node.type = "RICH_TEXT_NODE_TYPE_AT";
                    node.rid = source.Link?.BizId ?? "";
                    node.jump_url = source.Link?.Link ?? "";
                    break;
                case TextNode.Types.TextNodeType.BizLink:
                    node.type = "RICH_TEXT_NODE_TYPE_WEB";
                    node.jump_url = source.Link?.Link ?? "";
                    break;
            }
            return node;
        }

        /// <summary>
        /// OPUS 封面在 ModuleOpusSummary.Covers 里：这张卡没有别的卡片载荷时才当成图片补上，
        /// 不覆盖 archive/pgc/draw/article 已经拿到的内容。
        /// </summary>
        private static void AppendOpusCovers(DynamicCardModel2024_dynamic result, ModuleOpusSummary opusSummary)
        {
            var major = result?.major;
            if (major == null || opusSummary == null)
            {
                return;
            }
            if (major.archive != null || major.pgc != null || major.ugc_season != null || major.medialist != null
                || major.article != null || major.draw != null || major.live != null || major.live_rcmd != null)
            {
                return;
            }
            var items = opusSummary.Covers
                .Where(x => !string.IsNullOrEmpty(x.Src))
                .Select(x => new DynamicCardModel2024_pic
                {
                    src = x.Src,
                    width = ToInt(x.Width),
                    height = ToInt(x.Height),
                    size = x.Size,
                })
                .ToList();
            if (items.Count > 0)
            {
                major.draw = new DynamicCardModel2024_draw { items = items };
            }
        }

        /// <summary>
        /// 附件卡（ModuleAdditional）：预约/投票/通用/商品/稿件，正文之外单独一张卡。
        /// 以前整块不读，一条能预约的动态和一条纯文字动态在卡片里长得一样。
        /// </summary>
        private static DynamicCardModel2024_add MapAdditional(ModuleAdditional source)
        {
            if (source == null)
            {
                return null;
            }

            var result = new DynamicCardModel2024_add
            {
                type = MapAdditionalType(source.Type),
            };

            if (source.Up != null)
            {
                var up = source.Up;
                result.reserve = new DynamicCardModel2024_addin_reserve
                {
                    title = up.Title,
                    jump_url = up.Url,
                    rid = up.Rid,
                    reserve_total = ToInt(up.ReserveTotal),
                    desc1 = new DynamicCardModel2024_reservetext { text = up.DescText1?.Text ?? "" },
                    desc2 = new DynamicCardModel2024_reservetext { text = up.DescText2 ?? "" },
                    desc3 = new DynamicCardModel2024_reservetext { text = up.DescText3?.Text ?? "" },
                    button = MapAdditionalButton(up.Button),
                };
                return result;
            }

            if (source.Vote2 != null)
            {
                var vote = source.Vote2;
                result.vote = new DynamicCardModel2024_addin
                {
                    title = vote.Title,
                    desc1 = vote.Label,
                    id_str = vote.VoteId.ToString(),
                    state = (int)vote.State,
                };
                return result;
            }

            if (source.Common != null)
            {
                var common = source.Common;
                result.common = new DynamicCardModel2024_addin
                {
                    cover = common.ImageUrl,
                    desc1 = common.DescText1,
                    desc2 = common.DescText2,
                    head_text = common.HeadText,
                    jump_url = common.Url,
                    sub_type = common.CardType,
                    title = common.Title,
                    button = MapAdditionalButton(common.Button),
                };
                return result;
            }

            if (source.Ugc != null)
            {
                var ugc = source.Ugc;
                result.ugc = new DynamicCardModel2024_addin
                {
                    cover = ugc.Cover,
                    desc1 = ugc.DescText1,
                    desc2 = ugc.DescText2,
                    head_text = ugc.HeadText,
                    jump_url = ugc.Uri,
                    sub_type = ugc.CardType,
                    title = ugc.Title,
                };
                return result;
            }

            if (source.Goods != null)
            {
                var goods = source.Goods;
                result.goods = new DynamicCardModel2024_addin
                {
                    jump_url = goods.Uri,
                    sub_type = goods.CardType,
                    title = goods.RcmdDesc,
                };
                return result;
            }

            return result;
        }

        private static DynamicCardModel2024_button2 MapAdditionalButton(AdditionalButton source)
        {
            if (source == null)
            {
                return null;
            }
            return new DynamicCardModel2024_button2
            {
                status = (int)source.Status,
                jump_url = source.JumpUrl,
                jump_style = MapButtonText(source.JumpStyle),
                check = MapButtonText(source.Check),
                uncheck = MapButtonText(source.Uncheck),
            };
        }

        private static DynamicCardModel2024_buttontext MapButtonText(AdditionalButtonStyle source)
        {
            if (source == null)
            {
                return null;
            }
            return new DynamicCardModel2024_buttontext
            {
                icon_url = source.Icon,
                text = source.Text,
            };
        }

        /// <summary>gRPC 的附件类型名换成 web 那套字符串，渲染层的 subtype 判断不区分来源。</summary>
        private static string MapAdditionalType(AdditionalType type)
        {
            switch (type)
            {
                case AdditionalType.AdditionalNone: return "ADDITIONAL_TYPE_NONE";
                case AdditionalType.Pgc: return "ADDITIONAL_TYPE_PGC";
                case AdditionalType.Goods: return "ADDITIONAL_TYPE_GOODS";
                case AdditionalType.Vote: return "ADDITIONAL_TYPE_VOTE";
                case AdditionalType.Common: return "ADDITIONAL_TYPE_COMMON";
                case AdditionalType.Esport: return "ADDITIONAL_TYPE_ESPORTS";
                case AdditionalType.UpRcmd: return "ADDITIONAL_TYPE_UP_RCMD";
                case AdditionalType.Ugc: return "ADDITIONAL_TYPE_UGC";
                case AdditionalType.UpReservation: return "ADDITIONAL_TYPE_RESERVE";
                default: return type.ToString();
            }
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

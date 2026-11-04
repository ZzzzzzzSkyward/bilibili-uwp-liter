using System;
using System.Collections.Generic;
using System.IO;
using Bilibili.App.Dynamic.V2;
using BiliLite.Controls.Dynamic;
using BiliLite.Helpers;
using BiliLite.Modules;
using BiliLite.Parsing.Dynamic;
using BiliLite.Services;
using Google.Protobuf;
using Newtonsoft.Json.Linq;
using Windows.UI.Xaml;

namespace G3Probe
{
    /// <summary>
    /// G3: dynamic detail loads through the existing gRPC mapper.
    ///
    /// Scope: the probe compiles the real src/BiliLite.UWP/Helpers/DynamicGrpcMapper.cs together
    /// with its model closure (Modules/Home/DynamicVM.cs, Modules/IModules.cs, Modules/RelayCommand.cs,
    /// Models/ReturnModel.cs) and exercises MapToUserCards directly. Only the surrounding UWP
    /// scaffolding (Windows.UI.Xaml DataTemplate/ResourceDictionary, LogHelper, Utils, GrpcService,
    /// AnimeHomeModel) is stubbed in G3Stubs.cs, because those types depend on WinRT and cannot be
    /// built outside the UWP target. Both mapper assertions therefore run against product source.
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static int Main(string[] args)
        {
            var detailEndpoint = "bilibili.app.dynamic.v2.Dynamic/DynDetail";

            DynDetailReqRoundTripsDynamicId();
            DynDetailReplyRoundTripsItem(detailEndpoint);
            EmptyDynDetailReplyHasNoItem();
            GrpcServiceUsesDynDetailEndpoint(detailEndpoint);
            OneItemWithExtendMapsToSingleCard();
            ItemWithoutExtendMapsToNothing();
            DrawCardUsesPhotoObjectId();
            UnsupportedCardTypeStaysUnrenderable();
            DetailOnlyAcceptsRenderableCards();
            WebFeedItemsMapThroughTheSameCardModel();
            WebFeedSurvivesHostileFieldTypes();
            OpusPicturesRenderAsPhotoCards();
            WebCardKeepsAdditionalModule();
            WebCardKeepsVoteAttachment();
            ReserveDescriptionJoinsOnlyThePresentParts();
            ReserveLotteryTextReachesTheLabel();
            WebOpusPicturesKeepSrcFallbackAndOrder();
            WebEmptyMajorDoesNotHideLaterPayload();
            WebDrawItemsDropMissingUrls();
            WebUgcSeasonKeepsItsOwnKind();
            WebBodyJoinsOneSentence();
            GrpcOpusSummaryBecomesTheBody();
            GrpcOpusCoversBecomePictures();
            GrpcCardKeepsAdditionalModule();
            GrpcStatKeepsForbiddenFlags();
            GrpcEmojiNodeKeepsIconAndText();
            WebOpusTitleFeedsTheCardTitle();
            GrpcOpusTitleReachesTheCardFromBothSources();
            ForwardChainStopsAfterTheSecondCard();
            DisplayTitleSlotShowsTheTitleOnce();
            AncestryRowCarriesOnlyTheTruncatedAncestor();
            CardDepthLimitIsShared();
            FeedWindowKeepsTenPagesAndEvictsTheOldest();
            FeedWindowKeepsTheAnchorPage();
            FeedWindowDropsRepeatedIds();
            FeedWindowReleasesEvictedIdsForReload();
            ArticleFeedUsesTheSharedWebParser();
            PlainTextKeepsEveryParagraph();
            BodyIsNotTruncatedAtOneThousandCharacters();

            _passed += RichTextBodyProbe.Run(args);
            _passed += RichTextFlowProbe.Run();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        private static void DynDetailReqRoundTripsDynamicId()
        {
            var request = new DynDetailReq { DynamicId = "9876543210123" };
            var parsed = DynDetailReq.Parser.ParseFrom(request.ToByteArray());
            Check(parsed.DynamicId == "9876543210123", "DynDetailReq.DynamicId survives serialize/parse");

            var blank = DynDetailReq.Parser.ParseFrom(new DynDetailReq().ToByteArray());
            Check(blank.DynamicId == "", "DynDetailReq.DynamicId defaults to empty string");
        }

        private static void DynDetailReplyRoundTripsItem(string detailEndpoint)
        {
            var reply = new DynDetailReply
            {
                Item = new DynamicItem
                {
                    CardType = DynamicType.Av,
                    ItemType = DynamicType.Av,
                    Extend = new Extend { DynIdStr = "9876543210123" },
                }
            };

            var parsed = DynDetailReply.Parser.ParseFrom(reply.ToByteArray());
            Check(parsed.Item != null, "DynDetailReply.Item survives serialize/parse");
            Check(parsed.Item.Extend != null && parsed.Item.Extend.DynIdStr == "9876543210123",
                "DynDetailReply.Item.Extend.DynIdStr survives serialize/parse");
            Check(parsed.Item.CardType == DynamicType.Av, "DynDetailReply.Item.CardType survives serialize/parse");

            Check(detailEndpoint.EndsWith("/DynDetail", StringComparison.Ordinal),
                "detail RPC method name is DynDetail");
        }

        private static void EmptyDynDetailReplyHasNoItem()
        {
            var parsed = DynDetailReply.Parser.ParseFrom(new DynDetailReply().ToByteArray());
            Check(parsed.Item == null, "empty DynDetailReply parses to a null item (VM unusable-reply branch)");

            var cards = DynamicGrpcMapper.MapToUserCards(new[] { parsed.Item });
            Check(cards != null && cards.Count == 0, "null item maps to no cards (fallback trigger)");
        }

        private static void GrpcServiceUsesDynDetailEndpoint(string detailEndpoint)
        {
            var path = Path.Combine(RepoRoot(), "src", "BiliLite.UWP", "Services", "GrpcService.cs");
            var source = File.ReadAllText(path);
            Check(source.Contains("DynamicGrpcBaseUrl + \"DynDetail\""),
                "GrpcService.GetDynDetail targets DynamicGrpcBaseUrl + \"DynDetail\"");
            Check(source.Contains("new DynDetailReq"), "GrpcService.GetDynDetail builds a DynDetailReq");
            Check(source.Contains("DynDetailReply.Parser.ParseFrom"), "GrpcService.GetDynDetail parses a DynDetailReply");
            Check(detailEndpoint.StartsWith("bilibili.app.dynamic.v2.Dynamic/", StringComparison.Ordinal),
                "detail RPC lives on the dynamic.v2 service");
        }

        private static void OneItemWithExtendMapsToSingleCard()
        {
            var item = new DynamicItem
            {
                CardType = DynamicType.Av,
                Extend = new Extend { DynIdStr = "555000111" },
            };

            var cards = DynamicGrpcMapper.MapToUserCards(new[] { item });
            Check(cards.Count == 1, "one DynamicItem with Extend maps to exactly one DynamicCardModel2024");
            Check(cards[0].id_str == "555000111", "mapped card keeps Extend.DynIdStr as id_str");
            Check(cards[0].type == "DYNAMIC_TYPE_AV", "mapped card keeps the gRPC card type");

            var forward = new DynamicItem
            {
                CardType = DynamicType.Forward,
                Extend = new Extend { DynIdStr = "555000112" },
            };
            Check(DynamicGrpcMapper.MapToUserCards(new[] { forward }).Count == 1,
                "forward card still maps to one card");
        }

        private static void ItemWithoutExtendMapsToNothing()
        {
            var noExtend = new DynamicItem { CardType = DynamicType.Av, Extend = null };
            Check(DynamicGrpcMapper.MapToUserCards(new[] { noExtend }).Count == 0,
                "item with null Extend maps to an empty list (REST fallback)");
            Check(DynamicGrpcMapper.MapToUserCards(null).Count == 0, "null collection maps to an empty list");
        }

        // Review finding 1: a photo dynamic must route comments to the album object id, not the dynamic id.
        private static void DrawCardUsesPhotoObjectId()
        {
            var draw = new DynamicItem
            {
                CardType = DynamicType.Draw,
                Extend = new Extend { DynIdStr = "712345678901234" },
            };
            draw.Modules.Add(new Module
            {
                ModuleType = DynModuleType.ModuleDynamic,
                ModuleDynamic = new ModuleDynamic { DynDraw = new MdlDynDraw { Id = 987654321L } },
            });

            var cards = DynamicGrpcMapper.MapToUserCards(new[] { draw });
            Check(cards.Count == 1, "photo card maps to exactly one card");
            Check(cards[0].id_str == "712345678901234", "photo card keeps Extend.DynIdStr as id_str");
            Check(cards[0].basic.rid_str == "987654321",
                "photo card routes comments to the photo object id, not the dynamic id");
            Check(cards[0].basic.comment_id_str == "987654321",
                "photo card comment_id_str is the photo object id");

            var av = new DynamicItem
            {
                CardType = DynamicType.Av,
                Extend = new Extend { DynIdStr = "712345678901235" },
            };
            av.Modules.Add(new Module
            {
                ModuleType = DynModuleType.ModuleDynamic,
                ModuleDynamic = new ModuleDynamic { DynArchive = new MdlDynArchive { Avid = 123456L } },
            });
            var avCards = DynamicGrpcMapper.MapToUserCards(new[] { av });
            Check(avCards.Count == 1 && avCards[0].basic.rid_str == "123456",
                "video card still routes comments to the AVID");

            var drawWithoutDraw = new DynamicItem
            {
                CardType = DynamicType.Draw,
                Extend = new Extend { DynIdStr = "712345678901236" },
            };
            Check(DynamicGrpcMapper.MapToUserCards(new[] { drawWithoutDraw })[0].basic.rid_str == "712345678901236",
                "photo card without DynDraw falls back to the dynamic id");
        }

        // Review finding 2: a type the mapper cannot render must stay visibly unsupported so the
        // detail VM can reject it and reach the REST card.
        private static void UnsupportedCardTypeStaysUnrenderable()
        {
            var item = new DynamicItem
            {
                CardType = DynamicType.Story,
                Extend = new Extend { DynIdStr = "712345678901237" },
            };
            var cards = DynamicGrpcMapper.MapToUserCards(new[] { item });
            Check(cards.Count == 1, "unsupported card type still yields one card");
            Check(cards[0].type == "DYNAMIC_TYPE_UNSUPPORTED",
                "mapper reports DYNAMIC_TYPE_UNSUPPORTED for a type it cannot render");
            Check(cards[0].basic.rid_str == "712345678901237",
                "unsupported card falls back to the dynamic id for its oid");
        }

        /// <summary>
        /// BR-17: the web feed (x/polymer/web-dynamic) is mapped into the same 2024 card model the
        /// renderer already draws. Field names come from a real feed/all item; anything the mapper
        /// does not know must still produce one card (the renderer shows it as unsupported).
        /// </summary>
        private static void WebFeedItemsMapThroughTheSameCardModel()
        {
            var feed = new JArray
            {
                new JObject
                {
                    ["id_str"] = "1248889675504943123",
                    ["type"] = "DYNAMIC_TYPE_AV",
                    ["basic"] = new JObject { ["rid_str"] = "117451568191221", ["comment_type"] = 1, ["comment_id_str"] = "117451568191221" },
                    ["modules"] = new JObject
                    {
                        ["module_author"] = new JObject { ["mid"] = 546195, ["name"] = "up", ["face"] = "http://face", ["pub_time"] = "10月17日", ["pub_ts"] = "1792207804" },
                        ["module_stat"] = new JObject
                        {
                            ["comment"] = new JObject { ["count"] = 2214 },
                            ["forward"] = new JObject { ["count"] = 301 },
                            ["like"] = new JObject { ["count"] = 186067, ["status"] = false },
                        },
                        ["module_dynamic"] = new JObject
                        {
                            ["major"] = new JObject
                            {
                                ["type"] = "MAJOR_TYPE_ARCHIVE",
                                ["archive"] = new JObject
                                {
                                    ["aid"] = "117451568191221",
                                    ["bvid"] = "BV1gKJ86JEuP",
                                    ["cover"] = "http://cover",
                                    ["jump_url"] = "//www.bilibili.com/video/BV1gKJ86JEuP",
                                    ["duration_text"] = "24:02",
                                    ["title"] = "我是人类",
                                    ["desc"] = "游戏",
                                    ["stat"] = new JObject { ["play"] = "475.6万", ["danmaku"] = "7738" },
                                },
                            },
                        },
                    },
                },
                new JObject
                {
                    ["id_str"] = "1248889675504943124",
                    ["type"] = "DYNAMIC_TYPE_WORD",
                    ["basic"] = new JObject { ["rid_str"] = "1248889675504943124", ["comment_type"] = 17 },
                    ["modules"] = new JObject
                    {
                        ["module_dynamic"] = new JObject
                        {
                            ["desc"] = new JObject
                            {
                                ["text"] = "纯文本",
                                ["rich_text_nodes"] = new JArray
                                {
                                    new JObject { ["type"] = "RICH_TEXT_NODE_TYPE_TEXT", ["text"] = "纯文本", ["orig_text"] = "纯文本" },
                                },
                            },
                        },
                    },
                },
                new JObject
                {
                    ["id_str"] = "1248889675504943125",
                    ["type"] = "DYNAMIC_TYPE_DRAW",
                    ["basic"] = new JObject { ["rid_str"] = "1248889675504943125" },
                    ["modules"] = new JObject
                    {
                        ["module_dynamic"] = new JObject
                        {
                            ["major"] = new JObject
                            {
                                ["type"] = "MAJOR_TYPE_DRAW",
                                ["draw"] = new JObject
                                {
                                    ["id"] = "1248889675504943125",
                                    ["items"] = new JArray
                                    {
                                        new JObject { ["src"] = "http://pic1", ["width"] = 1200, ["height"] = 800 },
                                    },
                                },
                            },
                        },
                    },
                },
                new JObject
                {
                    ["id_str"] = "1248889675504943126",
                    ["type"] = "DYNAMIC_TYPE_FORWARD",
                    ["basic"] = new JObject { ["rid_str"] = "1248889675504943126", ["comment_type"] = 17 },
                    ["modules"] = new JObject
                    {
                        ["module_dynamic"] = new JObject
                        {
                            ["desc"] = new JObject { ["text"] = "转发理由" },
                        },
                    },
                    ["orig"] = new JObject
                    {
                        ["id_str"] = "1248889675504943127",
                        ["type"] = "DYNAMIC_TYPE_OPUS",
                        ["basic"] = new JObject { ["rid_str"] = "1248889675504943127" },
                        ["modules"] = new JObject
                        {
                            ["module_dynamic"] = new JObject
                            {
                                ["major"] = new JObject
                                {
                                    ["type"] = "MAJOR_TYPE_OPUS",
                                    ["opus"] = new JObject
                                    {
                                        ["title"] = "图文标题",
                                        ["summary"] = new JObject { ["text"] = "图文正文" },
                                        ["jump_url"] = "//www.bilibili.com/opus/1248889675504943127",
                                        ["pics"] = new JArray { new JObject { ["url"] = "http://opus-pic" } },
                                    },
                                },
                            },
                        },
                    },
                },
                new JObject
                {
                    ["id_str"] = "1248889675504943128",
                    ["type"] = "DYNAMIC_TYPE_COURSES",
                    ["basic"] = new JObject { ["rid_str"] = "1248889675504943128" },
                },
            };

            var cards = WebDynamicFeedMapper.MapToUserFeedCards(feed);
            Check(cards.Count == 5, "a web feed page maps to one card per item (cards=" + cards.Count + ")");

            var archive = cards[0];
            Check(archive.type == "DYNAMIC_TYPE_AV" && archive.id_str == "1248889675504943123",
                "an AV item keeps its card type and dynamic id");
            Check(archive.modules.module_dynamic.major.archive.aid == "117451568191221"
                  && archive.modules.module_dynamic.major.archive.title == "我是人类",
                "an AV item maps major.archive aid/title");
            Check(archive.modules.module_dynamic.major.archive.jump_url == "https://www.bilibili.com/video/BV1gKJ86JEuP",
                "a protocol-relative archive url is completed");
            Check(archive.modules.module_author.pub_ts == 1792207804L
                  && archive.modules.module_author.name == "up",
                "the web author block (string pub_ts) maps onto the card author");
            Check(archive.modules.module_stat.like.count == 186067,
                "the web module_stat maps onto the card counters");

            var word = cards[1];
            Check(word.type == "DYNAMIC_TYPE_WORD"
                  && word.modules.module_dynamic.desc.text == "纯文本"
                  && word.modules.module_dynamic.desc.rich_text_nodes.Count == 1,
                "a word item keeps its text and rich text nodes");

            var draw = cards[2];
            Check(draw.type == "DYNAMIC_TYPE_DRAW"
                  && draw.modules.module_dynamic.major.draw.items.Count == 1
                  && draw.modules.module_dynamic.major.draw.items[0].src == "http://pic1",
                "a draw item maps its pictures");

            var forward = cards[3];
            Check(forward.type == "DYNAMIC_TYPE_FORWARD" && forward.orig != null,
                "a forward item maps its original dynamic");
            Check(forward.orig.type == "DYNAMIC_TYPE_DRAW"
                  && forward.orig.modules.module_dynamic.major.draw.items.Count == 1
                  && forward.orig.modules.module_dynamic.major.draw.items[0].src == "http://opus-pic",
                "an opus original with a picture maps to the photo card the renderer draws");
            Check(forward.orig.modules.module_dynamic.desc.text == "图文正文",
                "an opus original keeps its summary text as the card description");

            Check(cards[4].type == "DYNAMIC_TYPE_UNSUPPORTED"
                  && cards[4].basic.rid_str == "1248889675504943128",
                "an unknown web type still yields an unsupported card instead of being dropped");

            Check(WebDynamicFeedMapper.MapToUserFeedCards(null).Count == 0,
                "a null web feed maps to an empty list");

            var noOrig = new JArray
            {
                new JObject
                {
                    ["id_str"] = "1248889675504943129",
                    ["type"] = "DYNAMIC_TYPE_FORWARD",
                    ["basic"] = new JObject { ["rid_str"] = "1248889675504943129" },
                },
            };
            Check(WebDynamicFeedMapper.MapToUserFeedCards(noOrig)[0].orig.type == "DYNAMIC_TYPE_NONE",
                "a forward without an original falls back to the placeholder card");
        }

        /// <summary>
        /// BR-22: 图片动态的图片在 web 流里放在 major.opus.pics（url/width/height），
        /// 以前被映射成只有封面的 article，渲染层因此拿到 0 张图（灰色占位 + 点不开）。
        /// 这里钉住"有图就是图片卡、没图就是文字卡、正文不丢"这三条。
        /// </summary>
        private static void OpusPicturesRenderAsPhotoCards()
        {
            var feed = new JArray
            {
                new JObject
                {
                    ["id_str"] = "1248889675504943201",
                    ["type"] = "DYNAMIC_TYPE_DRAW",
                    ["basic"] = new JObject { ["rid_str"] = "1248889675504943201" },
                    ["modules"] = new JObject
                    {
                        ["module_dynamic"] = new JObject
                        {
                            ["major"] = new JObject
                            {
                                ["type"] = "MAJOR_TYPE_OPUS",
                                ["opus"] = new JObject
                                {
                                    ["summary"] = new JObject
                                    {
                                        ["text"] = "九宫格正文",
                                        ["rich_text_nodes"] = new JArray
                                        {
                                            new JObject { ["type"] = "RICH_TEXT_NODE_TYPE_TEXT", ["text"] = "九宫格正文", ["orig_text"] = "九宫格正文" },
                                        },
                                    },
                                    ["pics"] = new JArray
                                    {
                                        new JObject { ["url"] = "http://opus-1", ["width"] = 1080, ["height"] = 1440 },
                                        new JObject { ["url"] = "http://opus-2", ["width"] = 1080, ["height"] = 2400 },
                                        new JObject { ["url"] = "http://opus-3", ["width"] = 1920, ["height"] = 1080 },
                                    },
                                },
                            },
                        },
                    },
                },
                new JObject
                {
                    ["id_str"] = "1248889675504943202",
                    ["type"] = "DYNAMIC_TYPE_OPUS",
                    ["basic"] = new JObject { ["rid_str"] = "1248889675504943202" },
                    ["modules"] = new JObject
                    {
                        ["module_dynamic"] = new JObject
                        {
                            ["major"] = new JObject
                            {
                                ["type"] = "MAJOR_TYPE_OPUS",
                                ["opus"] = new JObject
                                {
                                    ["summary"] = new JObject { ["text"] = "单图正文" },
                                    ["pics"] = new JArray
                                    {
                                        new JObject { ["url"] = "http://opus-single", ["width"] = 1200, ["height"] = 800 },
                                    },
                                },
                            },
                        },
                    },
                },
                new JObject
                {
                    ["id_str"] = "1248889675504943203",
                    ["type"] = "DYNAMIC_TYPE_OPUS",
                    ["basic"] = new JObject { ["rid_str"] = "1248889675504943203" },
                    ["modules"] = new JObject
                    {
                        ["module_dynamic"] = new JObject
                        {
                            ["major"] = new JObject
                            {
                                ["type"] = "MAJOR_TYPE_OPUS",
                                ["opus"] = new JObject
                                {
                                    ["summary"] = new JObject { ["text"] = "纯文字图文" },
                                },
                            },
                        },
                    },
                },
                new JObject
                {
                    ["id_str"] = "1248889675504943204",
                    ["type"] = "DYNAMIC_TYPE_OPUS",
                    ["basic"] = new JObject { ["rid_str"] = "1248889675504943204" },
                    ["modules"] = new JObject
                    {
                        ["module_dynamic"] = new JObject
                        {
                            ["major"] = new JObject
                            {
                                ["type"] = "MAJOR_TYPE_OPUS",
                                ["opus"] = new JObject
                                {
                                    ["summary"] = new JObject { ["text"] = "缺图正文" },
                                    ["pics"] = new JArray
                                    {
                                        new JObject { ["url"] = "", ["width"] = 100, ["height"] = 100 },
                                        new JObject { ["width"] = 100, ["height"] = 100 },
                                        new JObject { ["url"] = "http://opus-kept", ["width"] = 100, ["height"] = 100 },
                                    },
                                },
                            },
                        },
                    },
                },
            };

            var cards = WebDynamicFeedMapper.MapToUserFeedCards(feed);
            Check(cards.Count == 4, "opus feed: every item still yields a card (cards=" + cards.Count + ")");

            var grid = cards[0];
            Check(grid.type == "DYNAMIC_TYPE_DRAW",
                "opus feed: a draw item with opus pictures stays a photo card (type=" + grid.type + ")");
            Check(grid.modules.module_dynamic.major.draw != null
                  && grid.modules.module_dynamic.major.draw.items.Count == 3,
                "opus feed: all three opus pictures reach the photo card");
            Check(grid.modules.module_dynamic.major.draw.items[1].src == "http://opus-2"
                  && grid.modules.module_dynamic.major.draw.items[1].width == 1080
                  && grid.modules.module_dynamic.major.draw.items[1].height == 2400,
                "opus feed: picture url/size map onto the draw item");
            Check(grid.modules.module_dynamic.desc != null
                  && grid.modules.module_dynamic.desc.text == "九宫格正文"
                  && grid.modules.module_dynamic.desc.rich_text_nodes.Count == 1,
                "opus feed: the opus summary becomes the card description");

            Check(cards[1].type == "DYNAMIC_TYPE_DRAW"
                  && cards[1].modules.module_dynamic.major.draw.items.Count == 1
                  && cards[1].modules.module_dynamic.major.draw.items[0].src == "http://opus-single",
                "opus feed: a bare opus item with one picture becomes a single photo card");

            Check(cards[2].type == "DYNAMIC_TYPE_WORD",
                "opus feed: an opus item without pictures falls back to the text card (type=" + cards[2].type + ")");
            Check(cards[2].modules.module_dynamic.desc.text == "纯文字图文",
                "opus feed: a text-only opus keeps its summary");

            Check(cards[3].modules.module_dynamic.major.draw.items.Count == 1
                  && cards[3].modules.module_dynamic.major.draw.items[0].src == "http://opus-kept",
                "opus feed: pictures without a url are dropped instead of rendering a dead cell");
        }
        /// <summary>
        /// 设备日志 2026-10-31 08:20 的回归：真实 feed/all 响应里 rich_text_nodes[].emoji 是纯字符串，
        /// 旧代码在 node["emoji"]?["icon_url"] 上抛 InvalidOperationException（JValue 没有子节点），
        /// 整页动态因此变成空白加重试。web 映射路径对任何合法 JSON 都必须给出卡片，不能抛。
        /// </summary>
        private static void WebFeedSurvivesHostileFieldTypes()
        {
            var feed = new JArray
            {
                // 1) 触发设备崩溃的那一形：emoji 是字符串，正文仍需照常显示
                new JObject
                {
                    ["id_str"] = "1248889675504950001",
                    ["type"] = "DYNAMIC_TYPE_WORD",
                    ["basic"] = new JObject { ["rid_str"] = "1248889675504950001", ["comment_type"] = 17 },
                    ["modules"] = new JObject
                    {
                        ["module_dynamic"] = new JObject
                        {
                            ["desc"] = new JObject
                            {
                                ["text"] = "[大笑]正文",
                                ["rich_text_nodes"] = new JArray
                                {
                                    new JObject
                                    {
                                        ["type"] = "RICH_TEXT_NODE_TYPE_EMOJI",
                                        ["text"] = "[大笑]",
                                        ["orig_text"] = "[大笑]",
                                        ["emoji"] = "https://i0.hdslb.com/bfs/emote/probe.png",
                                    },
                                    new JObject
                                    {
                                        ["type"] = "RICH_TEXT_NODE_TYPE_TEXT",
                                        ["text"] = "正文",
                                        ["orig_text"] = "正文",
                                    },
                                },
                            },
                        },
                    },
                },
                // 2) 整个 modules 是字符串
                new JObject
                {
                    ["id_str"] = "1248889675504950002",
                    ["type"] = "DYNAMIC_TYPE_AV",
                    ["basic"] = new JObject { ["rid_str"] = "1248889675504950002" },
                    ["modules"] = "collapsed-by-server",
                },
                // 3) module_dynamic 是数字，作者块整体类型也乱（vip 字符串、official_verify 数字、pendant 数组）
                new JObject
                {
                    ["id_str"] = "1248889675504950003",
                    ["type"] = "DYNAMIC_TYPE_AV",
                    ["basic"] = new JObject { ["rid_str"] = "1248889675504950003" },
                    ["modules"] = new JObject
                    {
                        ["module_author"] = new JObject
                        {
                            ["mid"] = 546195,
                            ["name"] = "up3",
                            ["vip"] = "none",
                            ["official_verify"] = 0,
                            ["pendant"] = new JArray(),
                        },
                        ["module_dynamic"] = 0,
                    },
                },
                // 4) major.archive 是数组，统计块里 comment 是字符串、count 是数字
                new JObject
                {
                    ["id_str"] = "1248889675504950004",
                    ["type"] = "DYNAMIC_TYPE_DRAW",
                    ["basic"] = new JObject { ["rid_str"] = "1248889675504950004" },
                    ["modules"] = new JObject
                    {
                        ["module_stat"] = new JObject
                        {
                            ["comment"] = "disabled",
                            ["forward"] = new JObject { ["count"] = 301 },
                            ["like"] = new JObject { ["count"] = 186067, ["status"] = false },
                        },
                        ["module_dynamic"] = new JObject
                        {
                            ["major"] = new JObject
                            {
                                ["type"] = "MAJOR_TYPE_ARCHIVE",
                                ["archive"] = new JArray
                                {
                                    new JObject { ["aid"] = "117451568191299", ["title"] = "不会出现的投稿" },
                                },
                            },
                        },
                    },
                },
                // 5) 转发卡被服务端收成字符串
                new JObject
                {
                    ["id_str"] = "1248889675504950005",
                    ["type"] = "DYNAMIC_TYPE_FORWARD",
                    ["basic"] = new JObject { ["rid_str"] = "1248889675504950005" },
                    ["orig"] = "gone",
                },
                // 6) 推荐直播：content 不是 JSON
                new JObject
                {
                    ["id_str"] = "1248889675504950006",
                    ["type"] = "DYNAMIC_TYPE_LIVE_RCMD",
                    ["basic"] = new JObject { ["rid_str"] = "1248889675504950006" },
                    ["modules"] = new JObject
                    {
                        ["module_dynamic"] = new JObject
                        {
                            ["major"] = new JObject
                            {
                                ["live_rcmd"] = new JObject { ["content"] = "{not json" },
                            },
                        },
                    },
                },
                // 7) 直播卡：watched_show 是字符串
                new JObject
                {
                    ["id_str"] = "1248889675504950007",
                    ["type"] = "DYNAMIC_TYPE_LIVE",
                    ["basic"] = new JObject { ["rid_str"] = "1248889675504950007" },
                    ["modules"] = new JObject
                    {
                        ["module_dynamic"] = new JObject
                        {
                            ["major"] = new JObject
                            {
                                ["live"] = new JObject
                                {
                                    ["id"] = "7001",
                                    ["title"] = "直播",
                                    ["watched_show"] = "很多人看过",
                                },
                            },
                        },
                    },
                },
            };

            var cards = WebDynamicFeedMapper.MapToUserFeedCards(feed);
            Check(cards.Count == 7,
                "hostile types: every item still maps to a card (cards=" + cards.Count + ")");

            var emoji = cards[0];
            Check(emoji.type == "DYNAMIC_TYPE_WORD" && emoji.modules.module_dynamic.desc.text == "[大笑]正文",
                "hostile types: a string emoji keeps the node text (text=" +
                (emoji.modules.module_dynamic.desc == null ? "null" : emoji.modules.module_dynamic.desc.text) + ")");
            Check(emoji.modules.module_dynamic.desc.rich_text_nodes.Count == 2
                  && emoji.modules.module_dynamic.desc.rich_text_nodes[0].text == "[大笑]",
                "hostile types: the emoji node survives as a text node");
            Check(emoji.modules.module_dynamic.desc.rich_text_nodes[0].emoji == null,
                "hostile types: a string emoji degrades to no emoji info instead of throwing");

            var stringModules = cards[1];
            Check(stringModules.type == "DYNAMIC_TYPE_AV" && stringModules.basic.rid_str == "1248889675504950002",
                "hostile types: a string modules block keeps the card basic info");
            Check(stringModules.modules.module_author == null && stringModules.modules.module_dynamic.desc == null,
                "hostile types: a string modules block maps to empty module blocks");

            var numberDynamic = cards[2];
            Check(numberDynamic.modules.module_dynamic.desc == null
                  && numberDynamic.modules.module_dynamic.major != null
                  && numberDynamic.modules.module_dynamic.major.archive == null,
                "hostile types: a numeric module_dynamic maps to an empty dynamic block");
            Check(numberDynamic.modules.module_author.name == "up3"
                  && numberDynamic.modules.module_author.mid == 546195,
                "hostile types: the author survives a numeric module_dynamic sibling");
            Check(numberDynamic.modules.module_author.vip == null
                  && numberDynamic.modules.module_author.official_verify == null
                  && numberDynamic.modules.module_author.pendant == null,
                "hostile types: vip/official_verify/pendant of the wrong JSON type degrade to null");

            var arrayArchive = cards[3];
            Check(arrayArchive.modules.module_dynamic.major.archive == null,
                "hostile types: an array major.archive degrades to a missing archive");
            Check(arrayArchive.modules.module_stat.like.count == 186067,
                "hostile types: a numeric stat count still maps (count=" +
                arrayArchive.modules.module_stat.like.count + ")");
            Check(arrayArchive.modules.module_stat.comment.count == 0
                  && arrayArchive.modules.module_stat.forward.count == 301,
                "hostile types: a string stat block degrades to zero without taking the sibling down");

            Check(cards[4].type == "DYNAMIC_TYPE_FORWARD"
                  && cards[4].orig != null
                  && cards[4].orig.type == "DYNAMIC_TYPE_NONE",
                "hostile types: a string orig falls back to the placeholder card");

            Check(cards[5].modules.module_dynamic.major.live_rcmd.content == "{not json"
                  && cards[5].modules.module_dynamic.major.live == null,
                "hostile types: unparsable live_rcmd content keeps the raw payload and no live card");
            Check(cards[6].modules.module_dynamic.major.live.title == "直播"
                  && cards[6].modules.module_dynamic.major.live.desc_second == "",
                "hostile types: a string watched_show degrades to an empty description");

            // 整页都是垃圾类型时也只能得到占位/空卡，不能抛
            var allScalars = new JArray { 1, "two", new JArray(), new JObject() };
            Check(WebDynamicFeedMapper.MapToUserFeedCards(allScalars).Count == 1,
                "hostile types: scalar items are skipped and the lone object still yields one card");
        }

        private static void DetailOnlyAcceptsRenderableCards()
        {
            var root = RepoRoot();
            var vmSource = File.ReadAllText(Path.Combine(root, "src", "BiliLite.UWP", "Modules", "User", "DynamicVM.cs"));
            var start = vmSource.IndexOf("public async Task GetDynamicDetail(string id)", StringComparison.Ordinal);
            Check(start >= 0, "GetDynamicDetail exists in the user dynamic VM");

            var restIndex = start < 0 ? -1 : vmSource.IndexOf("dynamicAPI.DynamicDetail(id)", start, StringComparison.Ordinal);
            var branch = start >= 0 && restIndex > start ? vmSource.Substring(start, restIndex - start) : "";
            Check(branch.Contains("var display = ConvertToDisplay2024(detailCards[0]);"),
                "detail converts the gRPC card before storing it");
            Check(branch.Contains("if (display.Type != DynamicDisplayType.Other)"),
                "detail accepts the gRPC card only when the display type is not Other");
            Check(branch.Contains("stage=detail-grpc-unusable"),
                "unusable gRPC detail is logged before the REST fallback");
            Check(!branch.Contains("Items = new ObservableCollection<DynamicItemDisplayModel> { ConvertToDisplay2024"),
                "detail no longer stores the raw conversion result unconditionally");

            var parseSource = File.ReadAllText(Path.Combine(root, "src", "BiliLite.UWP", "Controls", "Dynamic", "DynamicParse.cs"));
            Check(parseSource.Contains("case \"DYNAMIC_TYPE_NONE\":") && parseSource.Contains("return DynamicDisplayType.Miss;"),
                "deleted/missing dynamics still parse to Miss, which the not-Other guard accepts");
            Check(!parseSource.Contains("case \"DYNAMIC_TYPE_UNSUPPORTED\":"),
                "unsupported cards have no display type and fall to Other");
        }

        // web：附件卡（预约/投票/通用/商品）以前整块不搬，能预约的图文退化成纯文字
        private static JObject WebCardFixture(string id, string type, JObject moduleDynamic)
        {
            return new JObject
            {
                ["id_str"] = id,
                ["type"] = type,
                ["basic"] = new JObject { ["comment_id_str"] = "C-" + id, ["comment_type"] = 11, ["rid_str"] = "R-" + id },
                ["modules"] = new JObject
                {
                    ["module_author"] = new JObject
                    {
                        ["mid"] = 42,
                        ["name"] = "摄影者",
                        ["pub_time"] = "2小时前",
                        ["pub_ts"] = "1700000000",
                    },
                    ["module_stat"] = new JObject
                    {
                        ["comment"] = new JObject { ["count"] = 3 },
                        ["forward"] = new JObject { ["count"] = 0, ["forbidden"] = true },
                        ["like"] = new JObject { ["count"] = 5, ["status"] = true },
                    },
                    ["module_dynamic"] = moduleDynamic,
                },
            };
        }

        private static DynamicCardModel2024 FirstWebCard(JObject card)
        {
            return WebDynamicFeedMapper.MapToUserFeedCards(new JArray { card })[0];
        }

        private static void WebCardKeepsAdditionalModule()
        {
            var moduleDynamic = new JObject
            {
                ["desc"] = new JObject
                {
                    ["text"] = "今天和 @朋友 去拍照。\n详情：拍摄地点 结尾",
                    ["rich_text_nodes"] = new JArray
                    {
                        new JObject { ["type"] = "RICH_TEXT_NODE_TYPE_TEXT", ["text"] = "今天和 " },
                        new JObject { ["type"] = "RICH_TEXT_NODE_TYPE_AT", ["text"] = "@朋友", ["rid"] = "777" },
                        new JObject { ["type"] = "RICH_TEXT_NODE_TYPE_TEXT", ["text"] = " 去拍照。\n详情：" },
                        new JObject
                        {
                            ["type"] = "RICH_TEXT_NODE_TYPE_WEB",
                            ["text"] = "拍摄地点",
                            ["jump_url"] = "//example.com/place",
                        },
                        new JObject { ["type"] = "RICH_TEXT_NODE_TYPE_TEXT", ["text"] = " 结尾" },
                    },
                },
                ["major"] = new JObject
                {
                    ["type"] = "MAJOR_TYPE_OPUS",
                    ["opus"] = new JObject
                    {
                        ["id"] = "55",
                        ["summary"] = new JObject { ["text"] = "今天和 @朋友 去拍照。" },
                        ["pics"] = new JArray
                        {
                            new JObject { ["url"] = "//i0.example/1.jpg", ["width"] = 800, ["height"] = 600 },
                            new JObject { ["url"] = "", ["src"] = "", ["width"] = 1, ["height"] = 1 },
                            new JObject { ["src"] = "//i0.example/2.jpg", ["width"] = 10, ["height"] = 20 },
                        },
                    },
                },
                ["additional"] = new JObject
                {
                    ["type"] = "ADDITIONAL_TYPE_RESERVE",
                    ["reserve"] = new JObject
                    {
                        ["title"] = "预约直播",
                        ["desc1"] = new JObject { ["text"] = "12-01 20:00", ["style"] = 0 },
                        ["desc2"] = new JObject { ["text"] = "1.2万人预约" },
                        ["jump_url"] = "//live.bilibili.com/1",
                        ["reserve_total"] = "12000",
                        ["up_mid"] = "42",
                        ["rid"] = "88",
                        ["button"] = new JObject
                        {
                            ["status"] = 1,
                            ["jump_url"] = "//live.bilibili.com/1",
                            ["jump_style"] = new JObject { ["text"] = "去预约" },
                            ["uncheck"] = new JObject { ["icon_url"] = "https://i0.example/b.png", ["text"] = "去预约" },
                        },
                    },
                },
            };

            var card = FirstWebCard(WebCardFixture("9001", "DYNAMIC_TYPE_OPUS", moduleDynamic));
            var additional = card.modules.module_dynamic.additional;
            Check(additional != null, "web additional: the attachment module is read at all");
            Check(additional.type == "ADDITIONAL_TYPE_RESERVE",
                "web additional: the subtype stays the wire name (type=" + additional.type + ")");
            Check(additional.reserve != null && additional.reserve.title == "预约直播",
                "web additional: the reserve title survives");
            Check(additional.reserve.desc1 != null && additional.reserve.desc1.text == "12-01 20:00",
                "web additional: reserve desc1 keeps its readable text");
            Check(additional.reserve.desc2 != null && additional.reserve.desc2.text == "1.2万人预约",
                "web additional: reserve desc2 keeps the server display label");
            Check(additional.reserve.jump_url == "https://live.bilibili.com/1",
                "web additional: the reserve target is protocol-complete (url=" + additional.reserve.jump_url + ")");
            Check(additional.reserve.reserve_total == 12000 && additional.reserve.rid == 88,
                "web additional: reserve total and id survive");
            Check(additional.reserve.button != null
                  && additional.reserve.button.uncheck != null
                  && additional.reserve.button.uncheck.text == "去预约"
                  && additional.reserve.button.uncheck.icon_url == "https://i0.example/b.png",
                "web additional: the reserve button text and icon survive");
            Check(additional.reserve.desc == "12-01 20:00 1.2万人预约",
                "web reserve: both description lines reach the label (desc=" + additional.reserve.desc + ")");
        }

        private static void WebCardKeepsVoteAttachment()
        {
            var moduleDynamic = new JObject
            {
                ["desc"] = new JObject { ["text"] = "投个票" },
                ["additional"] = new JObject
                {
                    ["type"] = "ADDITIONAL_TYPE_VOTE",
                    ["vote"] = new JObject
                    {
                        ["vote_id"] = "716365292050055176",
                        ["title"] = "投票标题",
                        ["desc1"] = "已有 100 人参与",
                        ["desc2"] = "剩 2 天",
                        ["jump_url"] = "//t.bilibili.com/vote/h5/index?is_popup=1",
                    },
                },
            };

            var card = FirstWebCard(WebCardFixture("9002", "DYNAMIC_TYPE_WORD", moduleDynamic));
            var vote = card.modules.module_dynamic.additional.vote;
            Check(vote != null && vote.title == "投票标题",
                "web additional: the vote card title survives");
            Check(vote.id_str == "716365292050055176",
                "web additional: the vote id is kept as a string (id=" + vote.id_str + ")");
            Check(vote.desc1 == "已有 100 人参与" && vote.desc2 == "剩 2 天",
                "web additional: both vote description lines survive");
        }

        // web：图文图片以前只认 url、还保留空地址条目；两种形状都要认，空地址不进图库
        /// <summary>
        /// 预约说明的两行拼一条标签：旧写法 `a ?? "" + " " + b ?? ""` 按 ?? 与 + 的优先级取值，
        /// 第一行存在时第二行就被丢掉、两行都缺时留下一个空格。这里逐种组合钉住结果。
        /// </summary>
        private static void AssertJoinParts(Func<string, string, string> join, string label)
        {
            var both = join("12-01 20:00", "1.2万人预约");
            Check(both == "12-01 20:00 1.2万人预约",
                label + ": both segments are kept and joined by one space (got '" + (both ?? "null") + "')");
            Check(!both.Contains("  ") && both == both.Trim(),
                label + ": the join has no doubled or stray spaces");

            var onlySecond = join(null, "1.2万人预约");
            Check(onlySecond == "1.2万人预约",
                label + ": a missing first segment does not leave a leading space (got '" + (onlySecond ?? "null") + "')");

            var onlyFirst = join("12-01 20:00", null);
            Check(onlyFirst == "12-01 20:00",
                label + ": a missing second segment does not leave a trailing space (got '" + (onlyFirst ?? "null") + "')");

            var emptyFirst = join("", "1.2万人预约");
            Check(emptyFirst == "1.2万人预约",
                label + ": an empty string is treated as absent (got '" + (emptyFirst ?? "null") + "')");

            var neither = join(null, null);
            Check(neither == "",
                label + ": no segment means an empty label, never a space or null (got '" + (neither ?? "null") + "')");
        }

        private static void ReserveDescriptionJoinsOnlyThePresentParts()
        {
            AssertJoinParts((a, b) => WireScalars.JoinText(a, b), "reserve desc");

            Check(WireScalars.JoinText("a", "", "b") == "a b",
                "reserve desc: a middle empty segment is skipped, neighbours keep one space");
            Check(WireScalars.JoinText() == "",
                "reserve desc: no arguments is an empty label");

            // 只有第二行的预约卡：旧表达式会拼出一个空格，标签就成了空白
            var moduleDynamic = new JObject
            {
                ["desc"] = new JObject { ["text"] = "预约直播" },
                ["additional"] = new JObject
                {
                    ["type"] = "ADDITIONAL_TYPE_RESERVE",
                    ["reserve"] = new JObject
                    {
                        ["title"] = "预约直播",
                        ["desc2"] = new JObject { ["text"] = "1.2万人预约" },
                        ["jump_url"] = "//live.bilibili.com/2",
                    },
                },
            };
            var card = FirstWebCard(WebCardFixture("9010", "DYNAMIC_TYPE_WORD", moduleDynamic));
            var reserve = card.modules.module_dynamic.additional.reserve;
            Check(reserve.desc1 == null && reserve.desc2 != null && reserve.desc2.text == "1.2万人预约",
                "web reserve: the missing line stays absent instead of being invented");
            Check(reserve.desc == "1.2万人预约",
                "web reserve: the label is only the present line (desc='" + (reserve.desc ?? "null") + "')");
        }

        /// <summary>
        /// 预约卡的第三段说明（desc3 / AdditionUP.DescText3，抽奖文案）在两端都要进可见标签：
        /// web 的 additional.reserve.desc3 与 gRPC 的 addition_up.desc_text3 是同一条信息，
        /// 以前 join 只拼前两段、gRPC 映射连字段都不读，抽奖文案整条丢失。
        /// 同一段文字被服务端重复填进多个槽位时只显示一次，也不引入别的分隔符。
        /// 传 null 表示该槽位缺席，空串表示服务端给了但没内容。
        /// </summary>
        private static void ReserveLotteryTextReachesTheLabel()
        {
            var fullLabel = "12-01 20:00 1.2万人预约 预约可参与抽奖";

            var webFull = WebReserveCard("9021", "12-01 20:00", "1.2万人预约", "预约可参与抽奖");
            Check(webFull.modules.module_dynamic.additional.reserve.desc3 != null
                  && webFull.modules.module_dynamic.additional.reserve.desc3.text == "预约可参与抽奖",
                "web reserve: the third description is carried on the card model");
            Check(webFull.modules.module_dynamic.additional.reserve.desc == fullLabel,
                "web reserve: the label has all three lines in order (desc='" +
                webFull.modules.module_dynamic.additional.reserve.desc + "')");

            var webOnlyThird = WebReserveCard("9022", null, null, "预约可参与抽奖");
            Check(webOnlyThird.modules.module_dynamic.additional.reserve.desc == "预约可参与抽奖",
                "web reserve: a third-only card shows the lottery text alone (desc='" +
                webOnlyThird.modules.module_dynamic.additional.reserve.desc + "')");

            var webRepeated = WebReserveCard("9023", "12-01 20:00", "12-01 20:00", "12-01 20:00");
            var webRepeatedLabel = webRepeated.modules.module_dynamic.additional.reserve.desc;
            Check(webRepeatedLabel == "12-01 20:00",
                "web reserve: a description the server repeats is shown once (desc='" + webRepeatedLabel + "')");
            Check(!webRepeatedLabel.Contains("  "),
                "web reserve: repetition is not hidden behind a run of spaces (desc='" + webRepeatedLabel + "')");

            var webEmpty = WebReserveCard("9024", "", "", "");
            Check(webEmpty.modules.module_dynamic.additional.reserve.desc == "",
                "web reserve: three empty descriptions give an empty label, never null (desc='" +
                (webEmpty.modules.module_dynamic.additional.reserve.desc ?? "null") + "')");

            var grpcFull = GrpcReserveCard("9121", "12-01 20:00", "1.2万人预约", "预约可参与抽奖");
            var grpcFullReserve = grpcFull.modules.module_dynamic.additional.reserve;
            Check(grpcFullReserve.desc3 != null && grpcFullReserve.desc3.text == "预约可参与抽奖",
                "grpc reserve: addition_up.desc_text3 is mapped instead of dropped");
            Check(grpcFullReserve.desc == fullLabel,
                "grpc reserve: the label has all three lines in order (desc='" + grpcFullReserve.desc + "')");

            var grpcOnlyThird = GrpcReserveCard("9122", null, null, "预约可参与抽奖");
            Check(grpcOnlyThird.modules.module_dynamic.additional.reserve.desc == "预约可参与抽奖",
                "grpc reserve: a third-only card shows the lottery text alone (desc='" +
                grpcOnlyThird.modules.module_dynamic.additional.reserve.desc + "')");

            var grpcRepeated = GrpcReserveCard("9123", "12-01 20:00", "12-01 20:00", "12-01 20:00");
            var grpcRepeatedLabel = grpcRepeated.modules.module_dynamic.additional.reserve.desc;
            Check(grpcRepeatedLabel == "12-01 20:00",
                "grpc reserve: a description the server repeats is shown once (desc='" + grpcRepeatedLabel + "')");
            Check(!grpcRepeatedLabel.Contains("  "),
                "grpc reserve: repetition is not hidden behind a run of spaces (desc='" + grpcRepeatedLabel + "')");

            var grpcEmpty = GrpcReserveCard("9124", "", "", "");
            Check(grpcEmpty.modules.module_dynamic.additional.reserve.desc == "",
                "grpc reserve: three empty descriptions give an empty label, never null (desc='" +
                (grpcEmpty.modules.module_dynamic.additional.reserve.desc ?? "null") + "')");
        }

        // web 预约夹具：null 槽位不出现，空串槽位写空 text，跟服务端的两种缺省写法各走一遍
        private static DynamicCardModel2024 WebReserveCard(string id, string desc1, string desc2, string desc3)
        {
            var reserve = new JObject
            {
                ["title"] = "预约直播",
                ["jump_url"] = "//live.bilibili.com/3",
            };
            if (desc1 != null)
            {
                reserve["desc1"] = new JObject { ["text"] = desc1 };
            }
            if (desc2 != null)
            {
                reserve["desc2"] = new JObject { ["text"] = desc2 };
            }
            if (desc3 != null)
            {
                reserve["desc3"] = new JObject { ["text"] = desc3 };
            }
            var moduleDynamic = new JObject
            {
                ["desc"] = new JObject { ["text"] = "预约直播" },
                ["additional"] = new JObject
                {
                    ["type"] = "ADDITIONAL_TYPE_RESERVE",
                    ["reserve"] = reserve,
                },
            };
            return FirstWebCard(WebCardFixture(id, "DYNAMIC_TYPE_WORD", moduleDynamic));
        }

        // gRPC 预约夹具：与 web 侧同一组槽位，desc_text1/3 是 HighlightText、desc_text2 是裸串
        private static DynamicCardModel2024 GrpcReserveCard(string id, string desc1, string desc2, string desc3)
        {
            var item = new DynamicItem
            {
                CardType = DynamicType.Word,
                Extend = new Extend { DynIdStr = id },
            };
            item.Modules.Add(new Module
            {
                ModuleType = DynModuleType.ModuleDesc,
                ModuleDesc = new ModuleDesc { Text = "预约直播" },
            });
            item.Modules.Add(new Module
            {
                ModuleType = DynModuleType.ModuleAdditional,
                ModuleAdditional = new ModuleAdditional
                {
                    Type = AdditionalType.UpReservation,
                    Up = new AdditionUP
                    {
                        Title = "预约直播",
                        DescText1 = desc1 == null ? null : new HighlightText { Text = desc1 },
                        DescText2 = desc2 ?? "",
                        DescText3 = desc3 == null ? null : new HighlightText { Text = desc3 },
                        Url = "https://live.bilibili.com/3",
                        ReserveTotal = 12000,
                    },
                },
            });
            return DynamicGrpcMapper.MapToUserCards(new[] { item })[0];
        }

        private static void WebOpusPicturesKeepSrcFallbackAndOrder()
        {
            var moduleDynamic = new JObject
            {
                ["desc"] = new JObject { ["text"] = "两张图" },
                ["major"] = new JObject
                {
                    ["type"] = "MAJOR_TYPE_OPUS",
                    ["opus"] = new JObject
                    {
                        ["id"] = "56",
                        ["pics"] = new JArray
                        {
                            new JObject { ["url"] = "//i0.example/1.jpg", ["width"] = 800, ["height"] = 600 },
                            new JObject { ["src"] = "", ["width"] = 1, ["height"] = 1 },
                            new JObject { ["src"] = "//i0.example/2.jpg", ["width"] = 10, ["height"] = 20 },
                        },
                    },
                },
            };

            var card = FirstWebCard(WebCardFixture("9003", "DYNAMIC_TYPE_OPUS", moduleDynamic));
            Check(card.type == "DYNAMIC_TYPE_DRAW", "web opus: pictures still select the photo card");
            var items = card.modules.module_dynamic.major.draw.items;
            Check(items.Count == 2,
                "web opus: the url-less picture is dropped and the rest stay contiguous (count=" + items.Count + ")");
            Check(items[0].src == "https://i0.example/1.jpg" && items[0].width == 800 && items[0].height == 600,
                "web opus: pics[].url keeps the original url and dimensions");
            Check(items[1].src == "https://i0.example/2.jpg" && items[1].width == 10,
                "web opus: pics[].src is accepted when url is missing");
        }

        // web：固定的 major 顺序不该被一个空对象挡住
        private static void WebEmptyMajorDoesNotHideLaterPayload()
        {
            var moduleDynamic = new JObject
            {
                ["desc"] = new JObject { ["text"] = "空 archive 挡不住图片" },
                ["major"] = new JObject
                {
                    ["type"] = "MAJOR_TYPE_OPUS",
                    ["archive"] = new JObject(),
                    ["pgc"] = new JObject(),
                    ["article"] = new JObject(),
                    ["draw"] = new JObject
                    {
                        ["id"] = "57",
                        ["items"] = new JArray
                        {
                            new JObject { ["src"] = "//i0.example/d.jpg", ["width"] = 5, ["height"] = 6 },
                        },
                    },
                },
            };

            var card = FirstWebCard(WebCardFixture("9004", "DYNAMIC_TYPE_DRAW", moduleDynamic));
            var major = card.modules.module_dynamic.major;
            Check(major.archive == null && major.pgc == null && major.article == null,
                "web major: empty payload objects do not occupy the fixed order");
            Check(major.draw != null && major.draw.items.Count == 1 && major.draw.items[0].src == "https://i0.example/d.jpg",
                "web major: the usable draw payload is selected instead");
        }

        private static void WebDrawItemsDropMissingUrls()
        {
            var moduleDynamic = new JObject
            {
                ["desc"] = new JObject { ["text"] = "图片" },
                ["major"] = new JObject
                {
                    ["type"] = "MAJOR_TYPE_DRAW",
                    ["draw"] = new JObject
                    {
                        ["id"] = "58",
                        ["items"] = new JArray
                        {
                            new JObject { ["src"] = "" },
                            new JObject { ["src"] = "//i0.example/ok.jpg", ["width"] = 3, ["height"] = 4 },
                        },
                    },
                },
            };

            var card = FirstWebCard(WebCardFixture("9005", "DYNAMIC_TYPE_DRAW", moduleDynamic));
            var items = card.modules.module_dynamic.major.draw.items;
            Check(items.Count == 1 && items[0].src == "https://i0.example/ok.jpg",
                "web draw: pictures without a url do not reach the gallery");
        }

        private static void WebUgcSeasonKeepsItsOwnKind()
        {
            var moduleDynamic = new JObject
            {
                ["desc"] = new JObject { ["text"] = "合集更新" },
                ["major"] = new JObject
                {
                    ["type"] = "MAJOR_TYPE_UGC_SEASON",
                    ["ugc_season"] = new JObject
                    {
                        ["id"] = "4444",
                        ["aid"] = "5555",
                        ["title"] = "合集标题",
                        ["cover"] = "//i0.example/season.jpg",
                        ["jump_url"] = "//www.bilibili.com/video/BV1xx",
                        ["stat"] = new JObject { ["play"] = "1.2万", ["danmaku"] = "3" },
                    },
                },
            };

            var card = FirstWebCard(WebCardFixture("9006", "DYNAMIC_TYPE_UGC_SEASON", moduleDynamic));
            Check(card.type == "DYNAMIC_TYPE_UGC_SEASON",
                "web ugc season: the card keeps its own kind instead of becoming unsupported (type=" + card.type + ")");
            var season = card.modules.module_dynamic.major.ugc_season;
            Check(season != null && season.aid == 5555 && season.title == "合集标题",
                "web ugc season: aid and title survive");
            Check(season.jump_url == "https://www.bilibili.com/video/BV1xx",
                "web ugc season: the jump url is protocol-complete");
            Check(season.stat.play == "1.2万",
                "web ugc season: the server display count is kept as text");
        }

        // 一句话流：节点顺序、@ 目标、链接标签、只按来源换行断行
        private static void WebBodyJoinsOneSentence()
        {
            var moduleDynamic = new JObject
            {
                ["desc"] = new JObject
                {
                    ["text"] = "今天和 @朋友 去拍照。\n详情：拍摄地点 结尾",
                    ["rich_text_nodes"] = new JArray
                    {
                        new JObject { ["type"] = "RICH_TEXT_NODE_TYPE_TEXT", ["text"] = "今天和 " },
                        new JObject { ["type"] = "RICH_TEXT_NODE_TYPE_AT", ["text"] = "@朋友", ["rid"] = "777" },
                        new JObject { ["type"] = "RICH_TEXT_NODE_TYPE_TEXT", ["text"] = " 去拍照。\n详情：" },
                        new JObject
                        {
                            ["type"] = "RICH_TEXT_NODE_TYPE_WEB",
                            ["text"] = "拍摄地点",
                            ["jump_url"] = "//example.com/place",
                        },
                        new JObject { ["type"] = "RICH_TEXT_NODE_TYPE_TEXT", ["text"] = " 结尾" },
                    },
                },
            };

            var card = FirstWebCard(WebCardFixture("9007", "DYNAMIC_TYPE_WORD", moduleDynamic));
            var desc = card.modules.module_dynamic.desc;
            var segments = RichTextFlow.Build(desc.rich_text_nodes, desc.text);

            Check(RichTextFlow.PlainText(segments) == "今天和 @朋友 去拍照。\n详情：拍摄地点 结尾",
                "web body: the whole sentence survives in order (text=" + RichTextFlow.PlainText(segments) + ")");
            Check(segments.FindAll(x => x.Kind == RichTextSegmentKind.LineBreak).Count == 1,
                "web body: only the source newline breaks the line, node kinds do not");
            var mention = segments.Find(x => x.Kind == RichTextSegmentKind.Mention);
            Check(mention != null && mention.Text == "@朋友" && mention.Target == "777",
                "web body: the mention keeps its label and mid");
            var link = segments.Find(x => x.Kind == RichTextSegmentKind.Link);
            Check(link != null && link.Text == "拍摄地点",
                "web body: the link shows its label, not the destination");
            Check(link != null && link.Target == "https://example.com/place",
                "web body: the link keeps a protocol-complete target");
        }

        // gRPC：图文没有 ModuleDesc，正文只在 ModuleOpusSummary 的摘要段落里
        private static void GrpcOpusSummaryBecomesTheBody()
        {
            var item = new DynamicItem
            {
                CardType = DynamicType.Word,
                Extend = new Extend { DynIdStr = "9101" },
            };
            item.Modules.Add(new Module
            {
                ModuleType = DynModuleType.ModuleAuthor,
                ModuleAuthor = new ModuleAuthor { Mid = 42, PtimeLabelText = "1小时前" },
            });
            item.Modules.Add(new Module
            {
                ModuleType = DynModuleType.ModuleOpusSummary,
                ModuleOpusSummary = new ModuleOpusSummary
                {
                    Summary = new Paragraph
                    {
                        Text = new TextParagraph
                        {
                            Nodes =
                            {
                                new TextNode
                                {
                                    NodeType = TextNode.Types.TextNodeType.Words,
                                    RawText = "图文摘要",
                                },
                            },
                        },
                    },
                },
            });

            var card = DynamicGrpcMapper.MapToUserCards(new[] { item })[0];
            var desc = card.modules.module_dynamic.desc;
            Check(desc != null, "grpc opus: a card without ModuleDesc still gets a body");
            Check(desc != null && desc.text == "图文摘要",
                "grpc opus: the summary text is the body (text=" + (desc == null ? "null" : desc.text) + ")");
            Check(desc != null && desc.rich_text_nodes.Count == 1,
                "grpc opus: the summary paragraph becomes ordered nodes");
        }

        private static void GrpcOpusCoversBecomePictures()
        {
            var item = new DynamicItem
            {
                CardType = DynamicType.Draw,
                Extend = new Extend { DynIdStr = "9102" },
            };
            item.Modules.Add(new Module
            {
                ModuleType = DynModuleType.ModuleOpusSummary,
                ModuleOpusSummary = new ModuleOpusSummary
                {
                    Summary = new Paragraph
                    {
                        Text = new TextParagraph { Nodes = { new TextNode { RawText = "三图正文" } } },
                    },
                    Covers =
                    {
                        new MdlDynDrawItem { Src = "https://i0.example/c1.jpg", Width = 100, Height = 200 },
                        new MdlDynDrawItem { Src = "", Width = 1, Height = 1 },
                        new MdlDynDrawItem { Src = "https://i0.example/c2.jpg", Width = 300, Height = 400 },
                    },
                },
            });

            var card = DynamicGrpcMapper.MapToUserCards(new[] { item })[0];
            var major = card.modules.module_dynamic.major;
            Check(major.draw != null && major.draw.items.Count == 2,
                "grpc opus: covers become the picture array without the url-less entry (count=" +
                (major.draw == null ? -1 : major.draw.items.Count) + ")");
            Check(major.draw.items[0].src == "https://i0.example/c1.jpg" && major.draw.items[0].height == 200,
                "grpc opus: cover url and dimensions are kept");
        }

        private static void GrpcCardKeepsAdditionalModule()
        {
            var item = new DynamicItem
            {
                CardType = DynamicType.Word,
                Extend = new Extend { DynIdStr = "9103" },
            };
            item.Modules.Add(new Module
            {
                ModuleType = DynModuleType.ModuleDesc,
                ModuleDesc = new ModuleDesc { Text = "预约直播" },
            });
            item.Modules.Add(new Module
            {
                ModuleType = DynModuleType.ModuleAdditional,
                ModuleAdditional = new ModuleAdditional
                {
                    Type = AdditionalType.UpReservation,
                    Up = new AdditionUP
                    {
                        Title = "预约直播",
                        DescText1 = new HighlightText { Text = "12-01 20:00" },
                        DescText2 = "1.2万人预约",
                        Url = "https://live.bilibili.com/9",
                        ReserveTotal = 12000,
                        Rid = 88,
                        Button = new AdditionalButton
                        {
                            Status = AdditionalButtonStatus.Uncheck,
                            JumpUrl = "https://live.bilibili.com/9",
                            Uncheck = new AdditionalButtonStyle { Text = "去预约", Icon = "https://i0.example/b.png" },
                        },
                    },
                },
            });

            var card = DynamicGrpcMapper.MapToUserCards(new[] { item })[0];
            var additional = card.modules.module_dynamic.additional;
            Check(additional != null, "grpc additional: the attachment module is read at all");
            Check(additional.type == "ADDITIONAL_TYPE_RESERVE",
                "grpc additional: the subtype uses the web vocabulary (type=" + additional.type + ")");
            Check(additional.reserve != null && additional.reserve.title == "预约直播",
                "grpc additional: the reserve title survives");
            Check(additional.reserve.desc1 != null && additional.reserve.desc1.text == "12-01 20:00"
                  && additional.reserve.desc2 != null && additional.reserve.desc2.text == "1.2万人预约",
                "grpc additional: both reserve descriptions survive");
            Check(additional.reserve.reserve_total == 12000 && additional.reserve.jump_url == "https://live.bilibili.com/9",
                "grpc additional: reserve total and target survive");
            Check(additional.reserve.button != null
                  && additional.reserve.button.uncheck != null
                  && additional.reserve.button.uncheck.text == "去预约",
                "grpc additional: the reserve button text survives");
            Check(additional.reserve.desc == "12-01 20:00 1.2万人预约",
                "grpc reserve: both description lines reach the label (desc=" + additional.reserve.desc + ")");
        }

        private static void GrpcStatKeepsForbiddenFlags()
        {
            var item = new DynamicItem
            {
                CardType = DynamicType.Word,
                Extend = new Extend { DynIdStr = "9104" },
            };
            item.Modules.Add(new Module
            {
                ModuleType = DynModuleType.ModuleStat,
                ModuleStat = new ModuleStat { Reply = 2, Repost = 1, Like = 3, NoComment = true, NoForward = true },
            });

            var card = DynamicGrpcMapper.MapToUserCards(new[] { item })[0];
            Check(card.modules.module_stat.comment.forbidden,
                "grpc stat: a forbidden comment target stays flagged");
            Check(card.modules.module_stat.forward.forbidden,
                "grpc stat: a forbidden repost target stays flagged");
            Check(card.modules.module_stat.like.count == 3,
                "grpc stat: the like count is unaffected");
        }

        private static void GrpcEmojiNodeKeepsIconAndText()
        {
            var item = new DynamicItem
            {
                CardType = DynamicType.Word,
                Extend = new Extend { DynIdStr = "9105" },
            };
            item.Modules.Add(new Module
            {
                ModuleType = DynModuleType.ModuleDesc,
                ModuleDesc = new ModuleDesc
                {
                    Text = "笑[笑]",
                    Desc =
                    {
                        new Description { Type = DescType.Text, Text = "笑" },
                        new Description
                        {
                            Type = DescType.Emoji,
                            Text = "[笑]",
                            OrigText = "[笑]",
                            IconUrl = "https://i0.example/e.png",
                            EmojiSize = 2,
                        },
                        new Description { Type = DescType.Aite, Text = "@朋友", Rid = "777" },
                    },
                },
            });

            var card = DynamicGrpcMapper.MapToUserCards(new[] { item })[0];
            var nodes = card.modules.module_dynamic.desc.rich_text_nodes;
            Check(nodes[1].type == "RICH_TEXT_NODE_TYPE_EMOJI",
                "grpc emoji: the node kind is mapped explicitly, not by substring (type=" + nodes[1].type + ")");
            Check(nodes[1].emoji != null && nodes[1].emoji.icon_url == "https://i0.example/e.png"
                  && nodes[1].emoji.size == 2,
                "grpc emoji: icon url and size survive");
            Check(nodes[2].type == "RICH_TEXT_NODE_TYPE_AT" && nodes[2].rid == "777",
                "grpc mention: the at node keeps its mid");
            var segments = RichTextFlow.Build(nodes, card.modules.module_dynamic.desc.text);
            Check(RichTextFlow.PlainText(segments) == "笑[笑]@朋友",
                "grpc body: kinds switch without injected spaces or breaks (text=" +
                RichTextFlow.PlainText(segments) + ")");
            Check(segments.FindAll(x => x.Kind == RichTextSegmentKind.LineBreak).Count == 0,
                "grpc body: no artificial line breaks between nodes");
        }

        // 纯文本兜底（预约卡/旧来源）：来源换行才断行，空行保留

        /// <summary>
        /// 图文卡的标题只在 major.opus.title 里：有图走 draw、没图走 article，两条路的 card.title
        /// 都必须等于这个标题；正文仍只有 summary 那一段，标题不拼进正文、不重复出现。
        /// </summary>
        private static void WebOpusTitleFeedsTheCardTitle()
        {
            var pictureFeed = new JArray
            {
                new JObject
                {
                    ["id_str"] = "7001",
                    ["type"] = "DYNAMIC_TYPE_OPUS",
                    ["modules"] = new JObject
                    {
                        ["module_author"] = new JObject { ["mid"] = 7, ["name"] = "图文up" },
                        ["module_dynamic"] = new JObject
                        {
                            ["major"] = new JObject
                            {
                                ["opus"] = new JObject
                                {
                                    ["id"] = "7001",
                                    ["title"] = "一篇图文的标题",
                                    ["summary"] = new JObject { ["text"] = "正文只有这一段摘要" },
                                    ["pics"] = new JArray
                                    {
                                        new JObject { ["url"] = "http://pic/1.jpg", ["width"] = 800, ["height"] = 600 },
                                    },
                                },
                            },
                        },
                    },
                },
            };
            var pictureCard = WebDynamicFeedMapper.MapToUserFeedCards(pictureFeed)[0];
            Check(pictureCard.modules.module_dynamic.major.opus != null
                    && pictureCard.modules.module_dynamic.major.opus.title == "一篇图文的标题",
                "web opus: major.opus.title keeps the opus title on the picture path");
            Check(pictureCard.title == "一篇图文的标题",
                "web opus: the picture card title comes from major.opus.title (title=" + pictureCard.title + ")");
            Check(pictureCard.modules.module_dynamic.desc.text == "正文只有这一段摘要",
                "web opus: the body is the summary only, the title is not prepended");

            var textFeed = new JArray
            {
                new JObject
                {
                    ["id_str"] = "7002",
                    ["type"] = "DYNAMIC_TYPE_OPUS",
                    ["modules"] = new JObject
                    {
                        ["module_author"] = new JObject { ["mid"] = 7, ["name"] = "图文up" },
                        ["module_dynamic"] = new JObject
                        {
                            ["major"] = new JObject
                            {
                                ["opus"] = new JObject
                                {
                                    ["id"] = "7002",
                                    ["title"] = "无图图文的标题",
                                    ["summary"] = new JObject { ["text"] = "无图图文的正文" },
                                },
                            },
                        },
                    },
                },
            };
            var textCard = WebDynamicFeedMapper.MapToUserFeedCards(textFeed)[0];
            Check(textCard.modules.module_dynamic.major.opus != null
                    && textCard.modules.module_dynamic.major.opus.title == "无图图文的标题",
                "web opus: major.opus.title survives when the opus has no pictures");
            Check(textCard.title == "无图图文的标题",
                "web opus: the text card title comes from major.opus.title (title=" + textCard.title + ")");
            Check(textCard.modules.module_dynamic.desc.text == "无图图文的正文",
                "web opus: the text card body is still the summary only");
        }

        /// <summary>
        /// gRPC 的标题有两个来源：Extend.OpusSummary（老形状）与 ModuleOpusSummary 模块。
        /// 两个来源都要落到 major.opus.title，card.title 才能读到。
        /// </summary>
        /// <summary>标题在 proto 里是 Paragraph：用 RawText 节点拼出一段可读标题。</summary>
        private static Paragraph TitleParagraph(string text)
        {
            var paragraph = new Paragraph { Text = new TextParagraph() };
            paragraph.Text.Nodes.Add(new TextNode { RawText = text });
            return paragraph;
        }

        private static void GrpcOpusTitleReachesTheCardFromBothSources()
        {
            var extendOnly = new DynamicItem
            {
                CardType = DynamicType.Word,
                Extend = new Extend
                {
                    DynIdStr = "7101",
                    OpusSummary = new ModuleOpusSummary { Title = TitleParagraph("扩展里的标题") },
                },
            };
            var extendCard = DynamicGrpcMapper.MapToUserCards(new[] { extendOnly })[0];
            Check(extendCard.modules.module_dynamic.major.opus != null
                    && extendCard.modules.module_dynamic.major.opus.title == "扩展里的标题",
                "grpc opus: Extend.OpusSummary.Title reaches major.opus.title");
            Check(extendCard.title == "扩展里的标题",
                "grpc opus: Extend.OpusSummary.Title reaches card.title (title=" + extendCard.title + ")");

            var moduleOnly = new DynamicItem
            {
                CardType = DynamicType.Word,
                Extend = new Extend { DynIdStr = "7102" },
            };
            moduleOnly.Modules.Add(new Module
            {
                ModuleType = DynModuleType.ModuleOpusSummary,
                ModuleOpusSummary = new ModuleOpusSummary { Title = TitleParagraph("模块里的标题") },
            });
            var moduleCard = DynamicGrpcMapper.MapToUserCards(new[] { moduleOnly })[0];
            Check(moduleCard.modules.module_dynamic.major.opus != null
                    && moduleCard.modules.module_dynamic.major.opus.title == "模块里的标题",
                "grpc opus: ModuleOpusSummary.Title reaches major.opus.title");
            Check(moduleCard.title == "模块里的标题",
                "grpc opus: ModuleOpusSummary.Title reaches card.title (title=" + moduleCard.title + ")");
        }

        /// <summary>
        /// 三层转发 A→B→C：卡片数据只有 A 与 B 两层，B 的 orig 不再搬进模型，
        /// 只把 C 的动态 ID 记到 B 的祖先槽上，供显示层做一行跳转。
        /// </summary>
        private static void ForwardChainStopsAfterTheSecondCard()
        {
            var webFeed = new JArray
            {
                new JObject
                {
                    ["id_str"] = "7201",
                    ["type"] = "DYNAMIC_TYPE_FORWARD",
                    ["modules"] = new JObject
                    {
                        ["module_author"] = new JObject { ["mid"] = 1, ["name"] = "A" },
                        ["module_dynamic"] = new JObject
                        {
                            ["desc"] = new JObject { ["text"] = "A 的转发语" },
                        },
                    },
                    ["orig"] = new JObject
                    {
                        ["id_str"] = "7202",
                        ["type"] = "DYNAMIC_TYPE_FORWARD",
                        ["modules"] = new JObject
                        {
                            ["module_author"] = new JObject { ["mid"] = 2, ["name"] = "B" },
                            ["module_dynamic"] = new JObject
                            {
                                ["desc"] = new JObject { ["text"] = "B 的转发语" },
                            },
                        },
                        ["orig"] = new JObject
                        {
                            ["id_str"] = "7203",
                            ["type"] = "DYNAMIC_TYPE_WORD",
                            ["modules"] = new JObject
                            {
                                ["module_author"] = new JObject { ["mid"] = 3, ["name"] = "C" },
                                ["module_dynamic"] = new JObject
                                {
                                    ["desc"] = new JObject { ["text"] = "C 的正文" },
                                },
                            },
                        },
                    },
                },
            };
            var webCard = WebDynamicFeedMapper.MapToUserFeedCards(webFeed)[0];
            Check(webCard.orig != null && webCard.orig.id_str == "7202",
                "web forward: the outer card keeps exactly the original (B)");
            Check(webCard.orig.orig == null,
                "web forward: the third card (C) is not mapped into the model");
            Check(webCard.orig.AncestryDynamicID == "7203",
                "web forward: the truncated ancestor id is recorded (id="
                    + (webCard.orig.AncestryDynamicID ?? "null") + ")");

            var c = new DynamicItem
            {
                CardType = DynamicType.Word,
                Extend = new Extend { DynIdStr = "7303" },
            };
            var b = new DynamicItem
            {
                CardType = DynamicType.Forward,
                Extend = new Extend { DynIdStr = "7302", OrigDynIdStr = "7303" },
            };
            b.Modules.Add(new Module
            {
                ModuleType = DynModuleType.ModuleDynamic,
                ModuleDynamic = new ModuleDynamic
                {
                    DynForward = new MdlDynForward { Item = c },
                },
            });
            var a = new DynamicItem
            {
                CardType = DynamicType.Forward,
                Extend = new Extend { DynIdStr = "7301", OrigDynIdStr = "7302" },
            };
            a.Modules.Add(new Module
            {
                ModuleType = DynModuleType.ModuleDynamic,
                ModuleDynamic = new ModuleDynamic
                {
                    DynForward = new MdlDynForward { Item = b },
                },
            });

            var grpcCard = DynamicGrpcMapper.MapToUserCards(new[] { a })[0];
            Check(grpcCard.orig != null && grpcCard.orig.id_str == "7302",
                "grpc forward: the outer card keeps exactly the original (B)");
            Check(grpcCard.orig.orig == null,
                "grpc forward: the third card (C) is not mapped into the model");
            Check(grpcCard.orig.AncestryDynamicID == "7303",
                "grpc forward: the truncated ancestor id is recorded (id="
                    + (grpcCard.orig.AncestryDynamicID ?? "null") + ")");
        }

        /// <summary>
        /// 标题只显示一次：预览模板（OneRow / Reserve）自己渲染标题的卡片把标题槽留空，
        /// 其余卡片把标题放进标题槽；两种情况标题都不进正文。
        /// </summary>
        private static void DisplayTitleSlotShowsTheTitleOnce()
        {
            var previewCard = new DynamicItemDisplayModel
            {
                Type = DynamicDisplayType.Video,
                OneRowInfo = new DynamicItemDisplayOneRowInfo { Title = "视频标题" },
            };
            DynamicCardProjection.ApplyTitleSlot(previewCard, "视频标题", true);
            Check(previewCard.Title == "" && previewCard.TitleVisibility == Visibility.Collapsed,
                "title slot: a card whose preview template shows the title keeps the slot empty");

            var photoCard = new DynamicItemDisplayModel
            {
                Type = DynamicDisplayType.Photo,
                OneRowInfo = new DynamicItemDisplayOneRowInfo { Desc = "图片说明" },
            };
            DynamicCardProjection.ApplyTitleSlot(photoCard, "图片动态的标题", false);
            Check(photoCard.Title == "图片动态的标题" && photoCard.TitleVisibility == Visibility.Visible,
                "title slot: a photo card without a preview title shows the title once");

            var noTitle = new DynamicItemDisplayModel { Type = DynamicDisplayType.Text };
            DynamicCardProjection.ApplyTitleSlot(noTitle, "", false);
            Check(noTitle.Title == "" && noTitle.TitleVisibility == Visibility.Collapsed,
                "title slot: an empty title leaves the slot collapsed");

            Check(new DynamicItemDisplayModel().Title == "",
                "display model: the title slot defaults to the empty string (no title)");
            Check(new DynamicItemDisplayModel().AncestryVisibility == Visibility.Collapsed,
                "display model: the ancestry row is collapsed by default");
            var wired = new DynamicItemDisplayModel { DetailByIdCommand = new RelayCommand<string>(id => { }) };
            Check(wired.DetailByIdCommand != null,
                "display model: DetailByIdCommand takes a string dynamic id");
        }

        /// <summary>
        /// A 转发 B 转发 C：模型最外层只有 B，C 只留成一行祖先 ID；
        /// 祖先行归最外层所有，B 自己不能再出现一遍。
        /// </summary>
        private static void AncestryRowCarriesOnlyTheTruncatedAncestor()
        {
            var outer = new DynamicItemDisplayModel { Type = DynamicDisplayType.Repost, DynamicID = "7401" };
            var original = new DynamicItemDisplayModel { Type = DynamicDisplayType.Repost, DynamicID = "7402" };
            DynamicCardProjection.ApplyAncestry(original, "7403");
            outer.OriginInfo = new List<DynamicItemDisplayModel> { original };

            DynamicCardProjection.HoistAncestry(outer, original);
            Check(outer.AncestryDynamicID == "7403" && outer.AncestryVisibility == Visibility.Visible,
                "ancestry: the outer card carries the truncated ancestor id");
            Check(original.AncestryDynamicID == "" && original.AncestryVisibility == Visibility.Collapsed,
                "ancestry: the embedded original does not repeat the ancestor row");

            var twoLevelOuter = new DynamicItemDisplayModel { Type = DynamicDisplayType.Repost };
            var twoLevelOriginal = new DynamicItemDisplayModel { Type = DynamicDisplayType.Text };
            DynamicCardProjection.HoistAncestry(twoLevelOuter, twoLevelOriginal);
            Check(twoLevelOuter.AncestryVisibility == Visibility.Collapsed,
                "ancestry: a two-level repost shows no ancestry row");

            var orphan = new DynamicItemDisplayModel { Type = DynamicDisplayType.Repost };
            DynamicCardProjection.ApplyAncestry(orphan, "");
            Check(orphan.AncestryVisibility == Visibility.Collapsed && orphan.AncestryDynamicID == "",
                "ancestry: an unknown ancestor id leaves the row collapsed (no dead row)");

            // 深度到顶时 OriginInfo 为空：内嵌原动态那块不能画出一个空框
            DynamicCardProjection.ApplyOriginVisibility(outer);
            Check(outer.OriginVisibility == Visibility.Visible,
                "origin box: a card with an embedded original keeps the bordered block");
            DynamicCardProjection.ApplyOriginVisibility(original);
            Check(original.OriginVisibility == Visibility.Collapsed,
                "origin box: a truncated original with no embedded card collapses the bordered block");
            Check(new DynamicItemDisplayModel().OriginVisibility == Visibility.Collapsed,
                "origin box: the bordered block is collapsed by default");
        }

        /// <summary>深度上限只有一个定义：外卡可以展开一层，原卡不能再展开。</summary>
        private static void CardDepthLimitIsShared()
        {
            Check(DynamicCardDepth.CanExpandOrig(0),
                "card depth: the outer card may expand one original");
            Check(!DynamicCardDepth.CanExpandOrig(DynamicCardDepth.Original),
                "card depth: the original card may not expand a third layer");
            Check(!DynamicCardDepth.CanExpandOrig(2),
                "card depth: anything deeper than the original is also refused");
        }

        private static void PlainTextKeepsEveryParagraph()
        {
            var segments = RichTextFlow.Build(null, "第一段\r\n\r\n第二段");
            Check(RichTextFlow.PlainText(segments) == "第一段\n\n第二段",
                "plain body: CRLF is normalised once (text=" + RichTextFlow.PlainText(segments) + ")");
            Check(segments.FindAll(x => x.Kind == RichTextSegmentKind.LineBreak).Count == 2,
                "plain body: an intentional blank line stays a blank line");
            Check(RichTextFlow.Build(null, "").Count == 0,
                "plain body: an empty body yields no placeholder text");
        }

        // 旧的 XAML 拼接按 1000 字符截断；正文流不按字符数限长
        private static void BodyIsNotTruncatedAtOneThousandCharacters()
        {
            var nodes = new System.Collections.Generic.List<DynamicCardModel2024_node>();
            for (var i = 0; i < 40; i++)
            {
                nodes.Add(new DynamicCardModel2024_node
                {
                    type = "RICH_TEXT_NODE_TYPE_TEXT",
                    text = new string('长', 100),
                });
            }
            var text = RichTextFlow.PlainText(RichTextFlow.Build(nodes, ""));
            Check(text.Length == 4000,
                "long body: every node survives without the 1000-character cap (length=" + text.Length + ")");
        }

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "src", "BiliLite.UWP", "Services", "GrpcService.cs")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("repository root not found from " + AppContext.BaseDirectory);
        }

        /// <summary>一页 5 张卡：ID 按页序号可预测。</summary>
        private static List<DynamicItemDisplayModel> FeedPageItems(int page, int count)
        {
            var items = new List<DynamicItemDisplayModel>();
            for (var i = 0; i < count; i++)
            {
                items.Add(new DynamicItemDisplayModel { DynamicID = "p" + page + "-" + i });
            }
            return items;
        }

        /// <summary>按页请求时的游标（第 1 页也带一个，好断言检查点里记的就是它）。</summary>
        private static IList<DynamicFeedPageCheckpoint> AddFeedPages(DynamicFeedWindow window, int pageCount, int perPage,
            List<string> cursorsUsed)
        {
            IList<DynamicFeedPageCheckpoint> evicted = null;
            for (var page = 1; page <= pageCount; page++)
            {
                var cursor = "cursor-" + (page - 1);
                cursorsUsed.Add(cursor);
                var survivors = window.AcceptItems(FeedPageItems(page, perPage));
                evicted = window.AddPage(page, survivors, cursor, "cursor-" + page);
            }
            return evicted;
        }

        private static void FeedWindowKeepsTenPagesAndEvictsTheOldest()
        {
            var window = new DynamicFeedWindow();
            var cursors = new List<string>();
            var evicted = AddFeedPages(window, 11, 5, cursors);

            Check(DynamicFeedWindow.MaxRetainedPages == 10, "feed window: cap starts at 10 pages (CARD_PLAN §9)");
            Check(window.PageCount == 10, "feed window: 11 pages leave exactly 10 retained");
            Check(evicted != null && evicted.Count == 1, "feed window: the 11th page evicts exactly one page");
            Check(evicted[0].PageNumber == 1, "feed window: the evicted page is the oldest one");
            Check(evicted[0].CursorBeforePage == cursors[0],
                "feed window: the checkpoint keeps the cursor that page was requested with");
            Check(evicted[0].FirstDynamicId == "p1-0", "feed window: the checkpoint keeps the page's first dynamic id");
            Check(evicted[0].Count == 5, "feed window: the checkpoint keeps the page item count");
            Check(window.EvictedPageCount == 1 && window.HasEvictedPages,
                "feed window: the evicted page is reported to the view");
            Check(window.RetainedIdCount == 50, "feed window: the evicted page's ids left the dedupe set");
        }

        private static void FeedWindowKeepsTheAnchorPage()
        {
            var window = new DynamicFeedWindow();
            window.AnchorDynamicId = "p1-2";
            var cursors = new List<string>();
            var evicted = AddFeedPages(window, 11, 5, cursors);

            Check(window.PageCount == 11, "feed window: the anchor page is never evicted, so the window may exceed the cap");
            Check(evicted != null && evicted.Count == 0, "feed window: nothing is evicted while the anchor sits on the oldest page");
            Check(window.EvictedPageCount == 0, "feed window: no evicted-page hint while the anchor pins the oldest page");

            window.AnchorDynamicId = "p11-0";
            var resumed = window.AddPage(12, window.AcceptItems(FeedPageItems(12, 5)), "cursor-11", "cursor-12");
            Check(resumed.Count == 2, "feed window: moving the anchor off the oldest page lets eviction resume");
            Check(window.PageCount == 10, "feed window: eviction brings the window back down to the cap");
        }

        private static void FeedWindowDropsRepeatedIds()
        {
            var window = new DynamicFeedWindow();
            var first = window.AcceptItems(FeedPageItems(1, 5));
            Check(first.Count == 5, "feed window: a fresh page keeps all of its ids");

            var overlap = new List<DynamicItemDisplayModel>();
            overlap.Add(new DynamicItemDisplayModel { DynamicID = "p1-4" });
            overlap.Add(new DynamicItemDisplayModel { DynamicID = "p1-0" });
            overlap.Add(new DynamicItemDisplayModel { DynamicID = "p2-0" });
            overlap.Add(new DynamicItemDisplayModel { DynamicID = "p2-0" });
            overlap.Add(new DynamicItemDisplayModel { DynamicID = "p2-1" });
            var survivors = window.AcceptItems(overlap);
            Check(survivors.Count == 2, "feed window: repeated ids are dropped, first occurrence wins");
            Check(survivors[0].DynamicID == "p2-0" && survivors[1].DynamicID == "p2-1",
                "feed window: the surviving items keep page order");
            Check(window.AcceptItems(FeedPageItems(1, 5)).Count == 0,
                "feed window: an overlapping page publishes no duplicates at all");
        }

        private static void FeedWindowReleasesEvictedIdsForReload()
        {
            var window = new DynamicFeedWindow();
            var cursors = new List<string>();
            var evicted = AddFeedPages(window, 11, 5, cursors);
            Check(evicted.Count == 1, "feed window: reload test starts from exactly one evicted page");

            var reloaded = window.AcceptItems(FeedPageItems(1, 5));
            Check(reloaded.Count == 5, "feed window: evicted ids are accepted again on reload (dedupe set released)");

            DynamicFeedPageCheckpoint taken;
            Check(window.TryTakeCheckpoint(out taken), "feed window: the earliest checkpoint is handed out for reload");
            Check(taken.PageNumber == 1 && taken.CursorBeforePage == cursors[0],
                "feed window: reload replays the page with the cursor recorded before it");

            window.ReinsertPage(taken.PageNumber, reloaded, taken.CursorBeforePage, "cursor-1");
            Check(window.PageCount == 11 && window.Pages[0].PageNumber == 1,
                "feed window: the reloaded page is reinserted at the front and not evicted at once");
            Check(!window.HasEvictedPages, "feed window: taking the only checkpoint clears the hint");
        }

        private static void ArticleFeedUsesTheSharedWebParser()
        {
            var path = Path.Combine(RepoRoot(), "src", "BiliLite.UWP", "Modules", "User", "DynamicVM.cs");
            var source = File.ReadAllText(path);
            Check(!source.Contains("GetData<JObject>"), "article feed no longer reads data.items by hand");
            Check(!source.Contains("data.data[\"items\"]"), "article feed no longer indexes data.data[\"items\"]");
            Check(source.Contains("WebDynamicFeedParser.Parse(response)"),
                "article feed parses through the shared web page parser");
            Check(source.Contains("articleOffset = nextOffset"), "article keeps its own offset, not the polymer one");
            Check(!source.Contains("HashSet<"), "the feed VM keeps no permanent dedupe set of its own");
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
            {
                Console.Error.WriteLine("FAIL " + message);
                Environment.Exit(1);
            }
            _passed++;
            Console.WriteLine("ok " + message);
        }
    }
}

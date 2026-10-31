using System;
using System.Collections.Generic;
using System.IO;
using Bilibili.App.Dynamic.V2;
using BiliLite.Helpers;
using Google.Protobuf;
using Newtonsoft.Json.Linq;

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

        public static int Main()
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

            var cards = DynamicGrpcMapper.MapToUserFeedCards(feed);
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

            Check(DynamicGrpcMapper.MapToUserFeedCards(null).Count == 0,
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
            Check(DynamicGrpcMapper.MapToUserFeedCards(noOrig)[0].orig.type == "DYNAMIC_TYPE_NONE",
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

            var cards = DynamicGrpcMapper.MapToUserFeedCards(feed);
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

            var cards = DynamicGrpcMapper.MapToUserFeedCards(feed);
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
            Check(DynamicGrpcMapper.MapToUserFeedCards(allScalars).Count == 1,
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

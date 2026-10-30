using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Bilibili.App.Dynamic.V2;
using BiliLite.Api.User;
using BiliLite.Helpers;
using BiliLite.Modules;
using BiliLite.Services;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace G4Probe
{
    /// <summary>
    /// G4: user-space dynamics are served by DynSpace, rendered with the existing card mapper.
    ///
    /// Scope: the probe compiles the real src/BiliLite.UWP/Helpers/DynamicGrpcMapper.cs (plus its model
    /// closure) and round-trips the generated DynSpaceReq/DynSpaceRsp protos. GrpcService and the Space
    /// branch of Modules/User/DynamicVM.cs cannot be compiled outside the UWP target, so their contract
    /// (endpoint, request/reply types, the separate space paging state, the REST fallback) is asserted
    /// against the product source text, the same way the g3 probe pins the detail endpoint.
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static int Main()
        {
            var spaceEndpoint = "bilibili.app.dynamic.v2.Dynamic/DynSpace";

            DynSpaceReqRoundTripsPagingFields();
            DynSpaceRspRoundTripsList();
            EmptyDynSpaceRspHasNoItems();
            GrpcServiceUsesDynSpaceEndpoint(spaceEndpoint);
            DynamicRequestsReportEastAsiaLocalTime();
            SpaceListItemMapsToSingleCard();
            EmptySpacePageMapsToNoCards();
            SpaceFeedKeepsItsOwnPagingState();
            HomeFeedFallsBackToRestWhenGrpcIsRejected();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        private static void DynSpaceReqRoundTripsPagingFields()
        {
            var request = new DynSpaceReq
            {
                HostUid = 123456789,
                From = "space",
                Page = 3,
                HistoryOffset = "offset-abc",
            };
            var parsed = DynSpaceReq.Parser.ParseFrom(request.ToByteArray());
            Check(parsed.HostUid == 123456789, "DynSpaceReq.HostUid survives serialize/parse");
            Check(parsed.From == "space", "DynSpaceReq.From survives serialize/parse");
            Check(parsed.Page == 3, "DynSpaceReq.Page survives serialize/parse");
            Check(parsed.HistoryOffset == "offset-abc", "DynSpaceReq.HistoryOffset survives serialize/parse");

            var blank = DynSpaceReq.Parser.ParseFrom(new DynSpaceReq().ToByteArray());
            Check(blank.HostUid == 0 && blank.Page == 0 && blank.From == "" && blank.HistoryOffset == "",
                "empty DynSpaceReq parses to zero/empty paging fields (page-1 request shape)");
        }

        private static void DynSpaceRspRoundTripsList()
        {
            var reply = new DynSpaceRsp
            {
                HistoryOffset = "next-offset",
                HasMore = true,
            };
            reply.List.Add(new DynamicItem
            {
                CardType = DynamicType.Av,
                ItemType = DynamicType.Av,
                Extend = new Extend { DynIdStr = "700000000000001" },
            });
            reply.List.Add(new DynamicItem
            {
                CardType = DynamicType.Forward,
                Extend = new Extend { DynIdStr = "700000000000002" },
            });

            var parsed = DynSpaceRsp.Parser.ParseFrom(reply.ToByteArray());
            Check(parsed.List.Count == 2, "DynSpaceRsp.List survives serialize/parse");
            Check(parsed.HistoryOffset == "next-offset", "DynSpaceRsp.HistoryOffset survives serialize/parse");
            Check(parsed.HasMore, "DynSpaceRsp.HasMore survives serialize/parse");
            Check(parsed.List[0].Extend != null && parsed.List[0].Extend.DynIdStr == "700000000000001",
                "DynSpaceRsp.List[0].Extend.DynIdStr survives serialize/parse");
            Check(parsed.List[0].CardType == DynamicType.Av && parsed.List[1].CardType == DynamicType.Forward,
                "DynSpaceRsp.List keeps per-item card types");
        }

        private static void EmptyDynSpaceRspHasNoItems()
        {
            var parsed = DynSpaceRsp.Parser.ParseFrom(new DynSpaceRsp().ToByteArray());
            Check(parsed.List.Count == 0, "empty DynSpaceRsp parses to an empty list");
            Check(!parsed.HasMore, "empty DynSpaceRsp has HasMore == false");
            Check(parsed.HistoryOffset == "", "empty DynSpaceRsp has an empty HistoryOffset");
        }

        private static void GrpcServiceUsesDynSpaceEndpoint(string spaceEndpoint)
        {
            var source = File.ReadAllText(Path.Combine(RepoRoot(), "src", "BiliLite.UWP", "Services", "GrpcService.cs"));
            Check(source.Contains("DynamicGrpcBaseUrl = \"https://grpc.biliapi.net:443/bilibili.app.dynamic.v2.Dynamic/\"")
                  && spaceEndpoint.StartsWith("bilibili.app.dynamic.v2.Dynamic/", StringComparison.Ordinal),
                "space RPC resolves on the dynamic.v2 service (" + spaceEndpoint + ")");
            Check(source.Contains("DynamicGrpcBaseUrl + \"DynSpace\""),
                "GrpcService.GetDynSpace targets DynamicGrpcBaseUrl + \"DynSpace\"");
            Check(source.Contains("public async Task<DynSpaceRsp> GetDynSpace(long mid, string from = \"space\", int page = 1, string offset = null)"),
                "GrpcService.GetDynSpace keeps the space signature (mid/from/page/offset)");
            Check(source.Contains("new DynSpaceReq")
                  && source.Contains("HostUid = mid")
                  && source.Contains("HistoryOffset = offset ?? \"\""),
                "GrpcService.GetDynSpace builds a DynSpaceReq with HostUid and a non-null HistoryOffset");
            Check(source.Contains("DynSpaceRsp.Parser.ParseFrom"),
                "GrpcService.GetDynSpace parses a DynSpaceRsp");
            Check(source.Contains("throw new Exception(result.message)"),
                "GrpcService.GetDynSpace reports the transport message on a rejected call");
            Check(!source.Contains("DynSpaceReply"),
                "space feed uses DynSpaceRsp (the reply name the proto actually defines)");
        }

        private static void DynamicRequestsReportEastAsiaLocalTime()
        {
            // local_time is an int32 whose protobuf default is 0 (UTC), so every dynamic request that
            // carries it must assign the same client timezone; an omitted assignment silently diverges.
            var source = File.ReadAllText(Path.Combine(RepoRoot(), "src", "BiliLite.UWP", "Services", "GrpcService.cs"));

            var space = MethodBody(source, "public async Task<DynSpaceRsp> GetDynSpace(");
            Check(Regex.IsMatch(space, @"new DynSpaceReq\s*\{[^}]*LocalTime = 8"),
                "GrpcService.GetDynSpace reports the East Asia client timezone (local_time = 8) on the space request");

            var all = MethodBody(source, "public async Task<DynAllReply> GetDynAll(");
            Check(Regex.IsMatch(all, @"new DynAllReq\s*\{[^}]*LocalTime = 8"),
                "GrpcService.GetDynAll still reports local_time = 8");

            var video = MethodBody(source, "public async Task<DynVideoReply> GetDynVideo(");
            Check(Regex.IsMatch(video, @"new DynVideoReq\s*\{[^}]*LocalTime = 8"),
                "GrpcService.GetDynVideo still reports local_time = 8");
        }

        /// <summary>Text of the first method whose declaration starts with <paramref name="signature"/>, braces included.</summary>
        private static string MethodBody(string source, string signature)
        {
            var start = source.IndexOf(signature, StringComparison.Ordinal);
            if (start < 0)
            {
                return "";
            }

            var open = source.IndexOf('{', start);
            if (open < 0)
            {
                return "";
            }

            var depth = 0;
            for (var i = open; i < source.Length; i++)
            {
                if (source[i] == '{')
                {
                    depth++;
                }
                else if (source[i] == '}' && --depth == 0)
                {
                    return source.Substring(open, i - open + 1);
                }
            }

            return "";
        }

        private static void SpaceListItemMapsToSingleCard()
        {
            var page = new DynSpaceRsp();
            page.List.Add(new DynamicItem
            {
                CardType = DynamicType.Av,
                Extend = new Extend { DynIdStr = "700000000000001" },
            });

            var cards = DynamicGrpcMapper.MapToUserCards(page.List);
            Check(cards.Count == 1, "one DynSpace.list item with Extend maps to exactly one card");
            Check(cards[0].id_str == "700000000000001", "mapped space card keeps Extend.DynIdStr as id_str");
            Check(cards[0].type == "DYNAMIC_TYPE_AV", "mapped space card keeps the gRPC card type");

            var empty = new DynSpaceRsp();
            Check(DynamicGrpcMapper.MapToUserCards(empty.List).Count == 0,
                "an empty DynSpace page maps to zero cards without throwing");
        }

        private static void EmptySpacePageMapsToNoCards()
        {
            var withoutExtend = new DynSpaceRsp();
            withoutExtend.List.Add(new DynamicItem { CardType = DynamicType.Av, Extend = null });
            Check(DynamicGrpcMapper.MapToUserCards(withoutExtend.List).Count == 0,
                "a space item without Extend is dropped, so a filtered-empty page stays empty");

            var ad = new DynSpaceRsp();
            ad.List.Add(new DynamicItem { CardType = DynamicType.Ad, Extend = new Extend { DynIdStr = "1" } });
            Check(DynamicGrpcMapper.MapToUserCards(ad.List).Count == 0, "a space Ad card maps to nothing");
        }

        private static void SpaceFeedKeepsItsOwnPagingState()
        {
            var source = File.ReadAllText(Path.Combine(RepoRoot(), "src", "BiliLite.UWP", "Modules", "User", "DynamicVM.cs"));
            Check(source.Contains("private int spacePage = 1;"), "space paging has its own page counter");
            Check(source.Contains("private string spaceOffset;"), "space paging has its own history offset");
            Check(source.Contains("private DynamicApiSource? pinnedSpaceSource;"), "space source is pinned for the feed lifetime");
            Check(source.Contains("private string spaceUid;"), "space paging state remembers which uid it belongs to");
            Check(Regex.Matches(source, "ResetSpaceFeed\\(\\);").Count == 2,
                "space paging state is reset on !loadMore and on a uid change");
            Check(source.Contains("grpcService.GetDynSpace(long.Parse(Uid), page: spacePage, offset: spaceLoadMore ? spaceOffset : null)"),
                "the space feed requests DynSpace with its own page/offset");
            Check(source.Contains("DynamicGrpcMapper.MapToUserCards(reply.List)"),
                "the space feed maps DynSpaceRsp.List through the shared user-card mapper");
            Check(source.Contains("spaceOffset = nextOffset;") && source.Contains("spacePage++;"),
                "the space feed stores history_offset and advances its page counter");
            Check(source.Contains("CanLoadMore = hasMore;"),
                "the space feed stops only on the server has_more flag");
            // 换源改由选择器固定来源(API-SMART-01 batch 2),原来的 sticky bool 字段已不存在
            Check(source.Contains("pinnedSpaceSource = source;") || source.Contains("pinnedSpaceSource = alternative;"),
                "the space feed pins the source it switched to for the feed lifetime");
            Check(source.Contains("dynamicAPI.SpaceHistory(Uid, cursor)") || source.Contains("dynamicAPI.SpaceHistory(Uid, idx)"),
                "SpaceHistory stays the REST source for Space");
        }

        /// <summary>
        /// The home dynamic tab is gRPC-only in the product; this runs the real Modules/Home/DynamicVM.cs
        /// against a rejecting GrpcService and a fixture REST DynamicAPI, so the fallback, its stickiness,
        /// its cursor separation and the Refresh reset are executed rather than read off the source.
        /// </summary>
        private static void HomeFeedFallsBackToRestWhenGrpcIsRejected()
        {
            LogHelper.Entries.Clear();
            DynamicAPI.Calls.Clear();
            GrpcService.DynVideoCalls = 0;
            GrpcService.RejectDynVideo = true;
            DynamicAPI.Fixture = call => RestFixture("offset-returned-page1", "700000000000001", "700000000000011");

            var vm = new DynamicVM();
            vm.GetDynamicItems(1).GetAwaiter().GetResult();

            Check(GrpcService.DynVideoCalls == 1,
                "home fallback: the rejected page spent exactly one DynVideo call");
            // 诊断日志由 API-SMART-01 的选择器统一输出,探针不再钉具体日志文本;
            // 这里只断言换源真的发生(REST endpoint 被调用、卡片发布),行为才算数。
            Check(vm.Items != null && vm.Items.Count > 0,
                "home fallback: the rejected page still publishes rows");
            Check(DynamicAPI.Calls.Count == 1
                  && DynamicAPI.Calls[0].Endpoint == "dynamic_new"
                  && DynamicAPI.Calls[0].Cursor == null,
                "home fallback: page 1 switches to the REST newest feed (dynamic_new, no cursor)");
            Check(vm.Items != null && vm.Items.Count == 2,
                "home fallback: the rejected gRPC page still publishes the REST cards");
            Check(vm.Items[1].card.Contains("rest-700000000000011"),
                "home fallback: REST cards keep the legacy card JSON the home templates read");
            Check(vm.CanLoadMore,
                "home fallback: a REST page whose last card has desc.dynamic_id can load more");

            GrpcService.RejectDynVideo = false;
            DynamicAPI.Fixture = call => RestFixture("offset-returned-page2", "700000000000021");
            vm.GetDynamicItems(2).GetAwaiter().GetResult();

            Check(GrpcService.DynVideoCalls == 1,
                "home fallback: the mode is sticky, so the next page never calls DynVideo again");
            Check(DynamicAPI.Calls.Count == 2 && DynamicAPI.Calls[1].Endpoint == "dynamic_history",
                "home fallback: later pages use the legacy history feed");
            Check(DynamicAPI.Calls[1].Cursor == "700000000000011",
                "home fallback: the history cursor is the last published card's desc.dynamic_id");
            Check(DynamicAPI.Calls[1].Cursor != "offset-returned-page1",
                "home fallback: the REST cursor is not the response's data.offset");
            Check(vm.Items.Count == 3,
                "home fallback: later REST pages append instead of replacing");

            DynamicAPI.Fixture = call => RestFixture("");
            vm.GetDynamicItems(3).GetAwaiter().GetResult();
            Check(!vm.CanLoadMore,
                "home fallback: an empty REST page stops paging (CanLoadMore false)");
            Check(vm.Items.Count == 3,
                "home fallback: an empty REST page publishes nothing");

            DynamicAPI.Calls.Clear();
            DynamicAPI.Fixture = call => RestFixture("offset-returned-refresh", "700000000000031");
            vm.Refresh();
            WaitIdle(vm);
            // Refresh 重新打分但不重置已学到的历史:上一页的端点拒绝让 gRPC 进入冷却,
            // 所以刷新后仍然从 REST 第一页取,并且是替换而不是追加。
            Check(GrpcService.DynVideoCalls == 1,
                "home fallback: Refresh does not retry the source that rejected the page");
            Check(DynamicAPI.Calls.Count == 1
                  && DynamicAPI.Calls[0].Endpoint == "dynamic_new"
                  && DynamicAPI.Calls[0].Cursor == null,
                "home fallback: Refresh restarts the legacy feed at page 1 instead of reusing a cursor");
            Check(vm.Items != null && vm.Items.Count == 1,
                "home fallback: Refresh replaces the list instead of appending to it");
        }

        /// <summary>REST fixture shaped like dynamic_svr: cards[] plus a data.offset the product must not use as a cursor.</summary>
        private static string RestFixture(string returnedOffset, params string[] dynamicIds)
        {
            var cards = new JArray();
            foreach (var id in dynamicIds)
            {
                cards.Add(new JObject
                {
                    ["desc"] = new JObject
                    {
                        ["type"] = 8,
                        ["dynamic_id"] = id,
                        ["dynamic_id_str"] = id,
                    },
                    ["card"] = new JObject
                    {
                        ["aid"] = "a-" + id,
                        ["title"] = "rest-" + id,
                    }.ToString(Formatting.None),
                });
            }

            return new JObject
            {
                ["code"] = 0,
                ["message"] = "0",
                ["data"] = new JObject
                {
                    ["cards"] = cards,
                    ["offset"] = returnedOffset,
                    ["has_more"] = 1,
                },
            }.ToString(Formatting.None);
        }

        private static void WaitIdle(DynamicVM vm)
        {
            for (var i = 0; i < 200 && vm.Loading; i++)
            {
                System.Threading.Thread.Sleep(10);
            }
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Bilibili.App.Dynamic.V2;
using BiliLite.Api.User;
using BiliLite.Helpers;
using BiliLite.Parsing;
using BiliLite.Parsing.Dynamic;
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
            AppendFailureKeepsTheGrpcCursor();
            LoadMoreFailureStaysQuiet();
            PolymerPagesAppend();
            PolymerLegFailureIsAFailedLeg();
            WebFeedParserClassifiesFailures();
            LegacyFeedParserClassifiesFailures();

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
        /// 选择器把学到的来源历史放在静态实例里，用例之间必须清干净；
        /// 否则前一个用例留下的冷却会改变下一个用例一开始选哪一路。
        /// </summary>
        private static void ResetSourceHistory()
        {
            var selectorField = typeof(BiliLite.Services.DynamicApiSelection)
                .GetField("Selector", BindingFlags.NonPublic | BindingFlags.Static);
            var selector = selectorField == null ? null : selectorField.GetValue(null);
            ClearSourceStore(selector);
            var storeField = selector == null
                ? null
                : selector.GetType().GetField("_store", BindingFlags.NonPublic | BindingFlags.Instance);
            ClearSourceStore(storeField == null || selector == null ? null : storeField.GetValue(selector));
        }

        private static void ClearSourceStore(object owner)
        {
            if (owner == null) { return; }
            foreach (var field in owner.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var dictionary = field.GetValue(owner) as System.Collections.IDictionary;
                if (dictionary != null) { dictionary.Clear(); continue; }
                if (field.FieldType.Name.Contains("Store"))
                {
                    ClearSourceStore(field.GetValue(owner));
                }
            }
        }

        /// <summary>
        /// 评审发现: 翻页失败时如果连游标一起清掉，页脚重试就会把 null 送进 protobuf 的 string 属性，
        /// 这一页永远恢复不了。游标属于"上一次成功"的状态，失败只能让轮询停下，不能把它抹掉。
        /// </summary>
        private static void AppendFailureKeepsTheGrpcCursor()
        {
            ResetFixtures();
            ResetSourceHistory();
            LogHelper.Entries.Clear();
            DynamicAPI.Calls.Clear();
            Utils.Toasts.Clear();
            GrpcService.DynVideoCalls = 0;
            GrpcService.DynVideoRequests.Clear();
            GrpcService.RejectDynVideo = false;

            var vm = new DynamicVM();
            vm.GetDynamicItems(1).GetAwaiter().GetResult();

            Check(GrpcService.DynVideoCalls == 1,
                "gRPC cursor: a clean selector starts the feed on gRPC (calls=" + GrpcService.DynVideoCalls + ")");
            Check(GrpcService.DynVideoRequests.Count == 1 && GrpcService.DynVideoRequests[0].HistoryOffset == null,
                "gRPC cursor: the first page carries no history offset");

            GrpcService.RejectDynVideo = true;
            vm.GetDynamicItems(2).GetAwaiter().GetResult();

            Check(GrpcService.DynVideoCalls == 2 && GrpcService.DynVideoRequests[1].Page == 2,
                "gRPC cursor: page 2 is requested from the pinned gRPC source");
            Check(Utils.Toasts.Count == 0,
                "gRPC cursor: the rejected append stays quiet");
            Check(!vm.CanLoadMore,
                "gRPC cursor: the rejected append stops paging until the footer retries");

            GrpcService.RejectDynVideo = false;
            vm.GetDynamicItems(2).GetAwaiter().GetResult();

            Check(GrpcService.DynVideoCalls == 3 && GrpcService.DynVideoRequests[2].Page == 2,
                "gRPC cursor: the retry asks for the same page again");
            Check(GrpcService.DynVideoRequests[2].HistoryOffset == "hist-1",
                "gRPC cursor: the retry resends the cursor of the last good page (offset=" +
                (GrpcService.DynVideoRequests[2].HistoryOffset ?? "null") + ")");
            Check(GrpcService.DynVideoRequests[2].UpdateBaseline == "base-1",
                "gRPC cursor: the retry keeps the update baseline too (baseline=" +
                (GrpcService.DynVideoRequests[2].UpdateBaseline ?? "null") + ")");
            Check(vm.CanLoadMore == false || vm.CanLoadMore,
                "gRPC cursor: the retry completes without throwing");
        }

        /// <summary>
        /// BR-16: 加载下一页失败时不能把数据源原文弹给用户；只有第一页两路都失败才提示，
        /// 并且提示用的是 legacy 的失败原因，不是 gRPC 的 -663 文本。
        /// 选择器会把学到的历史带到下一个用例（同一进程里的静态状态），所以这里不假定第一页由 gRPC 起步。
        /// </summary>
        private static void LoadMoreFailureStaysQuiet()
        {
            ResetFixtures();
            ResetSourceHistory();
            LogHelper.Entries.Clear();
            DynamicAPI.Calls.Clear();
            Utils.Toasts.Clear();
            GrpcService.DynVideoCalls = 0;
            GrpcService.RejectDynVideo = true;
            DynamicAPI.Fixture = call => RestFixture("offset-quiet-page1", "700000000000101", "700000000000102");

            var vm = new DynamicVM();
            vm.GetDynamicItems(1).GetAwaiter().GetResult();

            Check(vm.Items != null && vm.Items.Count == 2,
                "quiet load-more: page 1 publishes the REST rows (items=" + (vm.Items == null ? -1 : vm.Items.Count) + ")");
            Check(Utils.Toasts.Count == 0,
                "quiet load-more: a page served by the fallback never shows a toast");
            var grpcCallsAfterPage1 = GrpcService.DynVideoCalls;

            // 下一页只有 legacy 一路(REST 返回 404)：不换源、不提示、已发布的行必须留着
            GrpcService.RejectDynVideo = false;
            DynamicAPI.FailNextRequest = true;
            vm.GetDynamicItems(2).GetAwaiter().GetResult();

            Check(LegacyCalls().Count == 2 && LegacyCalls()[1].Endpoint == "dynamic_history",
                "quiet load-more: page 2 still asks the legacy history endpoint");
            Check(GrpcService.DynVideoCalls == grpcCallsAfterPage1,
                "quiet load-more: a failed load-more never restarts the feed from gRPC page 1");
            Check(vm.Items != null && vm.Items.Count == 2,
                "quiet load-more: the rows already on screen survive the failed append");
            Check(!vm.CanLoadMore,
                "quiet load-more: a failed page stops paging instead of retrying forever");
            Check(Utils.Toasts.Count == 0,
                "quiet load-more: a failed load-more shows no toast");
            Check(LogHelper.Entries.Exists(entry => entry.Contains("stage=feed-failed page=2")),
                "quiet load-more: the failure is still written to the log with its page number");

            // 第一页两路都失败：允许提示一次，而且只能是 legacy 的原因，不是 grpc-message=-663
            Utils.Toasts.Clear();
            GrpcService.RejectDynVideo = true;
            DynamicAPI.FailNextRequest = true;
            var fresh = new DynamicVM();
            fresh.GetDynamicItems(1).GetAwaiter().GetResult();

            Check(Utils.Toasts.Count == 1,
                "quiet load-more: only a page-1 double failure produces a toast (toasts=" + Utils.Toasts.Count + ")");
            Check(Utils.Toasts.Count == 1 && Utils.Toasts[0] == "NotFound",
                "quiet load-more: the toast carries the legacy failure text, not the gRPC rejection (" +
                (Utils.Toasts.Count == 0 ? "none" : Utils.Toasts[0]) + ")");
        }

        /// <summary>
        /// The home dynamic tab is gRPC-only in the product; this runs the real Modules/Home/DynamicVM.cs
        /// against a rejecting GrpcService and a fixture REST DynamicAPI, so the fallback, its stickiness,
        /// its cursor separation and the Refresh reset are executed rather than read off the source.
        /// </summary>
        private static void HomeFeedFallsBackToRestWhenGrpcIsRejected()
        {
            ResetFixtures();
            ResetSourceHistory();
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
            Check(LegacyCalls().Count == 1
                  && LegacyCalls()[0].Endpoint == "dynamic_new"
                  && LegacyCalls()[0].Cursor == null,
                "home fallback: page 1 switches to the REST newest feed (dynamic_new, no cursor)");
            Check(DynamicAPI.Calls[0].Endpoint == "feed_all",
                "home fallback: the web feed is tried between gRPC and the legacy feed");
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
            Check(LegacyCalls().Count == 2 && LegacyCalls()[1].Endpoint == "dynamic_history",
                "home fallback: later pages use the legacy history feed");
            Check(LegacyCalls()[1].Cursor == "700000000000011",
                "home fallback: the history cursor is the last published card's desc.dynamic_id");
            Check(LegacyCalls()[1].Cursor != "offset-returned-page1",
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
            // 刷新重新决定来源：旧 REST 永远排在还能翻页的来源之后，
            // 所以 gRPC 一旦恢复,刷新就回到 gRPC,不会继续钉在只能取第一页的 legacy 上。
            Check(GrpcService.DynVideoCalls == 2,
                "home fallback: Refresh retries the paginating source before the legacy feed");
            Check(LegacyCalls().Count == 0,
                "home fallback: Refresh never touches the legacy feed while gRPC works");
            // gRPC 替身只回游标不回卡片，所以“被替换”的表现是 REST 的行全没了，而不是又追加了 REST 的行
            Check(vm.Items == null || vm.Items.Count == 0,
                "home fallback: Refresh replaces the REST rows with the gRPC page instead of appending");
        }

        /// <summary>
        /// BR-17: 首页视频动态的 web 聚合流（feed/all?type=video）。gRPC 被拒之后它接手，
        /// 游标是响应里的 data.offset：第二页带着上一页的 offset 追加，翻页失败时行不动、不提示。
        /// </summary>
        private static void PolymerPagesAppend()
        {
            ResetSourceHistory();
            ResetFixtures();
            LogHelper.Entries.Clear();
            DynamicAPI.Calls.Clear();
            Utils.Toasts.Clear();
            GrpcService.DynVideoCalls = 0;
            GrpcService.RejectDynVideo = true;
            DynamicAPI.Fixture = call => RestFixture("offset-legacy-page1", "700000000000201");
            DynamicAPI.FeedFixture = call => PolymerFixture("polymer-offset-1", true, 20, "polymer1");

            var vm = new DynamicVM();
            vm.GetDynamicItems(1).GetAwaiter().GetResult();

            Check(DynamicAPI.Calls.Count == 1 && DynamicAPI.Calls[0].Endpoint == "feed_all",
                "polymer: a rejected gRPC page goes to the web feed before the legacy newest feed");
            Check(DynamicAPI.Calls[0].Cursor == null && DynamicAPI.Calls[0].Page == 1,
                "polymer: page 1 carries no offset");
            Check(DynamicAPI.Calls[0].FeedType == "video",
                "polymer: the home feed asks feed/all for type=video");
            Check(vm.Items != null && vm.Items.Count == 20,
                "polymer: page 1 publishes the 20 rows of the web feed (items=" + (vm.Items == null ? -1 : vm.Items.Count) + ")");
            Check(vm.CanLoadMore,
                "polymer: has_more plus an advanced offset keeps paging enabled");
            var first = vm.Items[0];
            Check(first.desc.type == 8 && first.desc.time_text == "刚刚",
                "polymer: an AV item maps to the flat video card the home grid renders");
            Check(first.video.title == "polymer1-0" && first.video.aid == "117451568191200",
                "polymer: the video card keeps the archive aid and title (aid=" + (first.video.aid ?? "null") + ")");
            Check(first.video.stat.view == 4756000 && first.video.duration == 1442,
                "polymer: the compact play count and duration text are parsed (view=" +
                (first.video.stat == null ? "null" : first.video.stat.view.ToString()) + ")");
            Check(first.video.owner != null && first.video.owner.name == "probe-up",
                "polymer: the author of the item becomes the card owner");
            Check(first.video.jump_url == "https://www.bilibili.com/video/BV1gKJ86JEuP",
                "polymer: a protocol-relative jump url is completed (" + (first.video.jump_url ?? "null") + ")");

            DynamicAPI.FeedFixture = call => PolymerFixture("polymer-offset-2", true, 20, "polymer2");
            vm.GetDynamicItems(2).GetAwaiter().GetResult();

            Check(DynamicAPI.Calls.Count == 2 && DynamicAPI.Calls[1].Endpoint == "feed_all",
                "polymer: page 2 stays on the web feed");
            Check(DynamicAPI.Calls[1].Cursor == "polymer-offset-1" && DynamicAPI.Calls[1].Page == 2,
                "polymer: page 2 sends the offset the previous page returned (offset=" +
                (DynamicAPI.Calls[1].Cursor ?? "null") + ")");
            Check(vm.Items != null && vm.Items.Count == 40,
                "polymer: page 2 appends instead of replacing (items=" + (vm.Items == null ? -1 : vm.Items.Count) + ")");
            Check(!DynamicAPI.Calls.Exists(call => call.Endpoint == "dynamic_history"),
                "polymer: paging inside the web feed never touches the dead legacy history endpoint");

            // 第三页失败：行不动、翻页停下、不提示（BR-16 的镜像）
            DynamicAPI.FailNextFeedRequest = true;
            vm.GetDynamicItems(3).GetAwaiter().GetResult();

            Check(DynamicAPI.Calls.Count(call => call.IsFeed) == 3,
                "polymer: the failed append still goes to the web feed");
            Check(vm.Items != null && vm.Items.Count == 40,
                "polymer: the rows already on screen survive the failed append");
            Check(!vm.CanLoadMore,
                "polymer: a failed append stops paging");
            Check(Utils.Toasts.Count == 0,
                "polymer: a failed append stays quiet");
            Check(LogHelper.Entries.Exists(entry => entry.Contains("stage=feed-failed page=3")),
                "polymer: the failure is logged with its page number");
            Check(GrpcService.DynVideoCalls == 1,
                "polymer: a failed append never restarts the gRPC feed from page 1");

            // 重试：游标属于上一次成功的那一页，不能因为失败被抹掉
            DynamicAPI.FeedFixture = call => PolymerFixture("polymer-offset-3", false, 20, "polymer3");
            vm.GetDynamicItems(3).GetAwaiter().GetResult();

            Check(DynamicAPI.Calls.Last().IsFeed && DynamicAPI.Calls.Last().Cursor == "polymer-offset-2",
                "polymer: the retry resends the cursor of the last good page (offset=" +
                (DynamicAPI.Calls.Last().Cursor ?? "null") + ")");
            Check(vm.Items != null && vm.Items.Count == 60,
                "polymer: the retry appends the third page (items=" + (vm.Items == null ? -1 : vm.Items.Count) + ")");
            Check(!vm.CanLoadMore,
                "polymer: has_more=false from the last page stops paging");
        }

        /// <summary>
        /// 设备日志 2026-10-31 的第二个缺陷：聚合流的信封合法、字段却对不上时，解码/映射里抛出的异常
        /// 从腿上逃出去，整个请求就此结束（页面空白加重试）。这一路失败只能算"没拿到有效页"：
        /// 第一页照旧换下一条源，第二页安静停下、已上屏的行不动。
        /// </summary>
        private static void PolymerLegFailureIsAFailedLeg()
        {
            ResetSourceHistory();
            ResetFixtures();
            LogHelper.Entries.Clear();
            DynamicAPI.Calls.Clear();
            Utils.Toasts.Clear();
            GrpcService.DynVideoCalls = 0;
            GrpcService.RejectDynVideo = true;
            // 形状合法、字段不合法：data 是数组，"data".items 这一步就会抛
            DynamicAPI.FeedFixture = call => "{\"code\":0,\"message\":\"0\",\"data\":[{\"items\":[]}]}";
            DynamicAPI.Fixture = call => RestFixture("offset-after-polymer-failure", "700000000000301");

            var vm = new DynamicVM();
            vm.GetDynamicItems(1).GetAwaiter().GetResult();

            Check(DynamicAPI.Calls.Count > 0 && DynamicAPI.Calls[0].IsFeed,
                "polymer failure: page 1 still asks the web feed first");
            Check(LegacyCalls().Count == 1 && LegacyCalls()[0].Endpoint == "dynamic_new",
                "polymer failure: the failed leg falls through to the REST newest feed");
            Check(vm.Items != null && vm.Items.Count == 1,
                "polymer failure: page 1 publishes the next source instead of ending the request (items=" +
                (vm.Items == null ? -1 : vm.Items.Count) + ")");
            Check(LogHelper.Entries.Exists(entry => entry.Contains("stage=feed-polymer-failed page=1")),
                "polymer failure: the leg logs its own failure stage instead of stage=feed-failed");
            Check(Utils.Toasts.Count == 0,
                "polymer failure: a page the fallback served shows no toast");

            // 第二页：来源已经固定成聚合流，失败只能安静停下
            ResetFixtures();
            ResetSourceHistory();
            LogHelper.Entries.Clear();
            DynamicAPI.Calls.Clear();
            Utils.Toasts.Clear();
            GrpcService.DynVideoCalls = 0;
            GrpcService.RejectDynVideo = true;
            DynamicAPI.FeedFixture = call => PolymerFixture("polymer-offset-guard-1", true, 3, "guardpolymer");

            var pinned = new DynamicVM();
            pinned.GetDynamicItems(1).GetAwaiter().GetResult();
            Check(pinned.Items != null && pinned.Items.Count == 3 && pinned.CanLoadMore,
                "polymer failure: the good page pins the web feed (items=" +
                (pinned.Items == null ? -1 : pinned.Items.Count) + ")");

            DynamicAPI.FeedFixture = call => "{\"code\":0,\"message\":\"0\",\"data\":[{\"items\":[]}]}";
            var legacyCallsBefore = LegacyCalls().Count;
            pinned.GetDynamicItems(2).GetAwaiter().GetResult();

            Check(LegacyCalls().Count == legacyCallsBefore,
                "polymer failure: a page-2 failure never switches source");
            Check(pinned.Items != null && pinned.Items.Count == 3,
                "polymer failure: a page-2 failure leaves the published rows alone");
            Check(!pinned.CanLoadMore,
                "polymer failure: a page-2 failure stops paging");
            Check(Utils.Toasts.Count == 0,
                "polymer failure: a page-2 failure stays quiet");
            Check(LogHelper.Entries.Exists(entry => entry.Contains("stage=feed-failed page=2")),
                "polymer failure: the quiet stop is still logged with its page number");

            // 结构契约：User/DynamicVM.cs 的聚合流腿不能被编译进探针，只断言它把映射包在同一个失败分支里
            var root = RepoRoot();
            var homeSource = File.ReadAllText(Path.Combine(root, "src", "BiliLite.UWP", "Modules", "Home", "DynamicVM.cs"));
            var homeLeg = Between(homeSource,
                "private async Task<DynamicApiOutcome> RequestPolymerSourceAsync",
                "private void ClearGrpcCursorOnRestart");
            // 解析层接手信封之后，腿里只剩"解析失败 → 报自己的阶段"与"映射失败 → MalformedResponse"两段
            var homeGuard = Between(homeLeg, "var parsed = WebDynamicFeedParser.Parse(response);",
                "return DynamicApiOutcome.MalformedResponse;");
            Check(homeGuard.Contains("MapToHomeVideoItems("),
                "home polymer leg maps inside the same guard that owns its failed outcome");
            Check(homeGuard.Contains("catch (Exception ex)") && homeGuard.Contains("stage=feed-polymer-failed"),
                "home polymer leg logs stage=feed-polymer-failed before reporting a malformed response");
            Check(homeGuard.Contains("parsed.Failure") && homeGuard.Contains("ToOutcome(parsed)"),
                "home polymer leg reports parser failures through its own stage and the shared outcome mapping");

            var userSource = File.ReadAllText(Path.Combine(root, "src", "BiliLite.UWP", "Modules", "User", "DynamicVM.cs"));
            var userLeg = Between(userSource,
                "private async Task<DynamicApiSourceResult> TryUserFeedPolymerAsync",
                "private async Task<DynamicApiSourceResult> TryUserFeedGrpcAsync");
            var userGuard = Between(userLeg, "var parsed = WebDynamicFeedParser.Parse(response);",
                "DynamicApiSourceResult.Failed(DynamicApiOutcome.MalformedResponse, ex.Message)");
            Check(userGuard.Contains("MapToUserFeedCards("),
                "user polymer leg maps inside the same guard that owns its failed outcome");
            Check(userGuard.Contains("catch (Exception ex)") && userGuard.Contains("stage=feed-polymer-failed"),
                "user polymer leg logs stage=feed-polymer-failed before reporting MalformedResponse");
            Check(userGuard.Contains("parsed.Failure") && userGuard.Contains("ToOutcome(parsed)"),
                "user polymer leg reports parser failures through its own stage and the shared outcome mapping");
        }

        /// <summary>两段标记之间的源码；找不到就返回空串，避免断言因为 IndexOf 的 -1 而假通过。</summary>
        private static string Between(string source, string startMarker, string endMarker)
        {
            var start = source.IndexOf(startMarker, StringComparison.Ordinal);
            if (start < 0)
            {
                return "";
            }
            start += startMarker.Length;
            var end = source.IndexOf(endMarker, start, StringComparison.Ordinal);
            return end < 0 ? "" : source.Substring(start, end - start);
        }

        /// <summary>探针夹具是静态的：每个用例开头都恢复到默认值，用例之间不互相影响。</summary>
        private static void ResetFixtures()
        {
            DynamicAPI.Fixture = call => RestFixture("");
            DynamicAPI.FeedFixture = call => "{\"code\":-663,\"message\":\"Rejected\"}";
            DynamicAPI.FailNextRequest = false;
            DynamicAPI.FailNextFeedRequest = false;
        }

        /// <summary>只统计 legacy 两个端点的调用：web 聚合流是另外一路，混在一起数会把断言变成巧合。</summary>
        private static List<DynamicAPI.RestCall> LegacyCalls()
        {
            return DynamicAPI.Calls.Where(call => !call.IsFeed).ToList();
        }

        /// <summary>web 聚合流的响应：items[] 与真实响应同形（AV × n、has_more、offset）。</summary>
        private static string PolymerFixture(string offset, bool hasMore, int count, string titlePrefix)
        {
            var items = new JArray();
            for (var i = 0; i < count; i++)
            {
                items.Add(new JObject
                {
                    ["id_str"] = "73000000000" + i.ToString("D4"),
                    ["type"] = "DYNAMIC_TYPE_AV",
                    ["basic"] = new JObject
                    {
                        ["rid_str"] = "1174515681912" + i.ToString("D2"),
                        ["comment_type"] = 1,
                        ["comment_id_str"] = "1174515681912" + i.ToString("D2"),
                    },
                    ["modules"] = new JObject
                    {
                        ["module_author"] = new JObject
                        {
                            ["mid"] = 546195,
                            ["name"] = "probe-up",
                            ["face"] = "http://i0.hdslb.com/bfs/face/probe.jpg",
                            ["pub_time"] = "刚刚",
                            ["pub_ts"] = "1792207804",
                        },
                        ["module_stat"] = new JObject
                        {
                            ["comment"] = new JObject { ["count"] = 2214, ["forbidden"] = false },
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
                                    ["aid"] = "1174515681912" + i.ToString("D2"),
                                    ["bvid"] = "BV1gKJ86JEuP",
                                    ["cover"] = "http://i2.hdslb.com/bfs/archive/probe.jpg",
                                    ["jump_url"] = "//www.bilibili.com/video/BV1gKJ86JEuP",
                                    ["duration_text"] = "24:02",
                                    ["title"] = titlePrefix + "-" + i,
                                    ["desc"] = "probe",
                                    ["stat"] = new JObject { ["play"] = "475.6万", ["danmaku"] = "7738" },
                                },
                            },
                        },
                    },
                });
            }

            return new JObject
            {
                ["code"] = 0,
                ["message"] = "0",
                ["data"] = new JObject
                {
                    ["items"] = items,
                    ["has_more"] = hasMore,
                    ["offset"] = offset,
                },
            }.ToString(Formatting.None);
        }

        /// <summary>REST fixture shaped like dynamic_svr: cards[] plus a data.offset the product must not use as a cursor.</summary>
        /// <summary>
        /// 解析层（Parsing/Dynamic/WebDynamicFeedParser）的类别契约：拿不到、被拒、信封无效、形状不对、
        /// 以及一个合法的空页各有各的结论，换源语义由 DynamicApiSelection.ToOutcome 统一翻译。
        /// </summary>
        private static void WebFeedParserClassifiesFailures()
        {
            var missing = WebDynamicFeedParser.Parse(null);
            Check(missing.Failure == ApiParseFailure.Transport && missing.Stage == "transport",
                "parser: no response is a transport failure");
            Check(DynamicApiSelection.ToOutcome(missing) == DynamicApiOutcome.TransportFailure,
                "parser: a transport failure maps to the transport outcome");

            var httpError = WebDynamicFeedParser.Parse(new BiliLite.Helpers.HttpResults
            {
                status = false,
                code = 412,
                message = "blocked",
                results = "",
            });
            Check(httpError.Failure == ApiParseFailure.Transport && httpError.HttpStatus == 412,
                "parser: an HTTP failure keeps its status for classification");
            Check(DynamicApiSelection.ToOutcome(httpError) == DynamicApiOutcome.EndpointRejection,
                "parser: an HTTP 4xx maps to the rejection outcome the selector records");

            var rejected = WebDynamicFeedParser.Parse(RestEnvelope("{\"code\":-663,\"message\":\"被拒绝\"}"));
            Check(rejected.Failure == ApiParseFailure.Rejected && rejected.BusinessCode == -663
                  && rejected.Message == "被拒绝",
                "parser: a business rejection keeps its code and message");
            Check(DynamicApiSelection.ToOutcome(rejected) == DynamicApiOutcome.EndpointRejection,
                "parser: -663 maps to the endpoint rejection outcome");

            var noCode = WebDynamicFeedParser.Parse(RestEnvelope("{\"message\":\"no code\"}"));
            Check(noCode.Failure == ApiParseFailure.Envelope && noCode.Stage == "envelope.code",
                "parser: an envelope without a business code is not a success");
            Check(DynamicApiSelection.ToOutcome(noCode) == DynamicApiOutcome.MalformedResponse,
                "parser: a broken envelope maps to the malformed outcome");

            var noData = WebDynamicFeedParser.Parse(RestEnvelope("{\"code\":0}"));
            Check(noData.Failure == ApiParseFailure.Envelope && noData.Stage == "data",
                "parser: a missing data object is an envelope failure");

            var arrayData = WebDynamicFeedParser.Parse(RestEnvelope("{\"code\":0,\"data\":[{\"items\":[]}]}"));
            Check(arrayData.Failure == ApiParseFailure.Payload && arrayData.Stage == "data",
                "parser: data that is an array is a payload failure");

            var stringItems = WebDynamicFeedParser.Parse(RestEnvelope("{\"code\":0,\"data\":{\"items\":\"none\"}}"));
            Check(stringItems.Failure == ApiParseFailure.Payload && stringItems.Stage == "data.items",
                "parser: items that are not an array report the field path");

            var stringHasMore = WebDynamicFeedParser.Parse(
                RestEnvelope("{\"code\":0,\"data\":{\"items\":[],\"has_more\":\"yes\"}}"));
            Check(stringHasMore.Failure == ApiParseFailure.Payload && stringHasMore.Stage == "data.has_more",
                "parser: a non-boolean has_more is rejected instead of guessed");

            var emptyPage = WebDynamicFeedParser.Parse(
                RestEnvelope("{\"code\":0,\"data\":{\"items\":[],\"has_more\":false}}"));
            Check(emptyPage.IsSuccess && emptyPage.Value.Items.Count == 0 && !emptyPage.Value.HasMore
                  && emptyPage.Value.NextCursor == null,
                "parser: a valid empty page is a success, not a failure");

            var missingItems = WebDynamicFeedParser.Parse(RestEnvelope("{\"code\":0,\"data\":{}}"));
            Check(missingItems.IsSuccess && missingItems.Value.Items.Count == 0,
                "parser: a missing items field reads as an empty page");

            var numericCursor = WebDynamicFeedParser.Parse(
                RestEnvelope("{\"code\":0,\"data\":{\"items\":[],\"has_more\":true,\"offset\":1264970784730775554}}"));
            Check(numericCursor.IsSuccess && numericCursor.Value.HasMore
                  && numericCursor.Value.NextCursor == "1264970784730775554",
                "parser: a numeric offset becomes the cursor string unchanged");
        }

        /// <summary>
        /// 旧 REST 动态接口（dynamic_new / dynamic_history / space_history / 详情）的解析契约：
        /// 信封与 web 聚合流共用 ApiEnvelope，这里钉住 cards / has_more / offset 三个字段的规则。
        /// </summary>
        private static void LegacyFeedParserClassifiesFailures()
        {
            var rejected = LegacyDynamicFeedParser.Parse(RestEnvelope("{\"code\":-663,\"message\":\"被拒绝\"}"));
            Check(rejected.Failure == ApiParseFailure.Rejected && rejected.BusinessCode == -663,
                "legacy parser: a business rejection keeps its code");
            Check(DynamicApiSelection.ToOutcome(rejected) == DynamicApiOutcome.EndpointRejection,
                "legacy parser: -663 maps to the endpoint rejection outcome");

            var noCode = LegacyDynamicFeedParser.Parse(RestEnvelope("{\"code\":null,\"data\":{}}"));
            Check(noCode.Failure == ApiParseFailure.Envelope && noCode.Stage == "envelope.code",
                "legacy parser: a null business code is not a success");

            var noData = LegacyDynamicFeedParser.Parse(RestEnvelope("{\"code\":0,\"message\":\"ok\"}"));
            Check(noData.Failure == ApiParseFailure.Envelope && noData.Stage == "data",
                "legacy parser: a missing data object is an envelope failure");

            var stringCards = LegacyDynamicFeedParser.Parse(RestEnvelope("{\"code\":0,\"data\":{\"cards\":\"none\"}}"));
            Check(stringCards.Failure == ApiParseFailure.Payload && stringCards.Stage == "data.cards",
                "legacy parser: cards that are not an array report the field path");

            var emptyPage = LegacyDynamicFeedParser.Parse(RestEnvelope("{\"code\":0,\"data\":{}}"));
            Check(emptyPage.IsSuccess && emptyPage.Value.Cards.Count == 0 && !emptyPage.Value.HasMore
                  && emptyPage.Value.NextCursor == null,
                "legacy parser: a page without cards is a valid empty page");

            var numericHasMore = LegacyDynamicFeedParser.Parse(
                RestEnvelope("{\"code\":0,\"data\":{\"cards\":[],\"has_more\":1,\"offset\":123456}}"));
            Check(numericHasMore.IsSuccess && numericHasMore.Value.HasMore
                  && numericHasMore.Value.NextCursor == "123456",
                "legacy parser: has_more accepts 0/1 and the numeric offset becomes the cursor");

            var falseHasMore = LegacyDynamicFeedParser.Parse(
                RestEnvelope("{\"code\":0,\"data\":{\"cards\":[],\"has_more\":0}}"));
            Check(falseHasMore.IsSuccess && !falseHasMore.Value.HasMore,
                "legacy parser: has_more 0 means no more pages");

            var detail = LegacyDynamicFeedParser.ParseDetail(RestEnvelope("{\"code\":0,\"data\":{\"card\":{\"desc\":{}}}}"));
            Check(detail.IsSuccess && detail.Value["card"] != null,
                "legacy detail: the data object carries the card");
            var detailArray = LegacyDynamicFeedParser.ParseDetail(RestEnvelope("{\"code\":0,\"data\":[]}"));
            Check(detailArray.Failure == ApiParseFailure.Payload && detailArray.Stage == "data",
                "legacy detail: an array where the card object belongs is a payload failure");

            var reposts = LegacyDynamicFeedParser.ParseReposts(
                RestEnvelope("{\"code\":0,\"data\":{\"items\":[{\"desc\":{}}],\"has_more\":1,\"offset\":\"o2\"}}"));
            Check(reposts.IsSuccess && reposts.Value.Items.Count == 1 && reposts.Value.HasMore
                  && reposts.Value.NextCursor == "o2",
                "legacy reposts: items/has_more/offset map onto the repost page");
            var repostsZero = LegacyDynamicFeedParser.ParseReposts(
                RestEnvelope("{\"code\":0,\"data\":{\"items\":[],\"has_more\":0}}"));
            Check(repostsZero.IsSuccess && !repostsZero.Value.HasMore && repostsZero.Value.NextCursor == "",
                "legacy reposts: has_more 0 means the last page and an empty cursor");
            var repostsString = LegacyDynamicFeedParser.ParseReposts(
                RestEnvelope("{\"code\":0,\"data\":{\"items\":\"none\"}}"));
            Check(repostsString.Failure == ApiParseFailure.Payload && repostsString.Stage == "data.items",
                "legacy reposts: items that are not an array report the field path");

            var page = emptyPage.Value;
            page.Cards = (JArray)JToken.Parse("[{\"a\":1}]");
            List<int> wrongShape;
            Check(!LegacyDynamicFeedParser.TryDeserialize(page.Cards, null, out wrongShape),
                "legacy parser: cards that do not fit the model fail the page instead of throwing");
        }
        /// <summary>把 JSON 正文裹进一个 HTTP 成功的结果，供解析层用例使用。</summary>
        private static BiliLite.Helpers.HttpResults RestEnvelope(string json)
        {
            return new BiliLite.Helpers.HttpResults
            {
                status = true,
                code = 200,
                results = json,
            };
        }
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

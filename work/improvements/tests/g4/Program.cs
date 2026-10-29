using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Bilibili.App.Dynamic.V2;
using BiliLite.Helpers;
using Google.Protobuf;

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
            Check(source.Contains("private bool spaceRestMode;"), "space REST fallback is a sticky instance flag");
            Check(source.Contains("private string spaceUid;"), "space paging state remembers which uid it belongs to");
            Check(Regex.Matches(source, "ResetSpaceFeed\\(\\);").Count == 2,
                "space paging state is reset on !loadMore and on a uid change");
            Check(source.Contains("grpcService.GetDynSpace(long.Parse(Uid), page: spacePage, offset: spaceLoadMore ? spaceOffset : null)"),
                "the space feed requests DynSpace with its own page/offset");
            Check(source.Contains("DynamicGrpcMapper.MapToUserCards(reply.List)"),
                "the space feed maps DynSpaceRsp.List through the shared user-card mapper");
            Check(source.Contains("spaceOffset = reply.HistoryOffset;") && source.Contains("spacePage++;"),
                "the space feed stores history_offset and advances its page counter");
            Check(source.Contains("CanLoadMore = reply.HasMore;"),
                "the space feed stops only on the server has_more flag");
            Check(source.Contains("stage=space-rest-fallback"), "a failed DynSpace call logs the REST fallback");
            Check(source.Contains("spaceRestMode = true;") && source.Contains("Items = null;") && source.Contains("idx = \"\";"),
                "the fallback engages sticky REST, drops the gRPC page and restarts with an empty SpaceHistory idx");
            Check(source.Contains("api = dynamicAPI.SpaceHistory(Uid, idx);"),
                "SpaceHistory stays the REST source for Space");
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

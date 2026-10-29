using System;
using System.Collections.Generic;
using System.IO;
using Bilibili.App.Dynamic.V2;
using BiliLite.Helpers;
using Google.Protobuf;

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

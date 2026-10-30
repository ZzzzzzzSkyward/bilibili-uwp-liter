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
            DrawCardUsesPhotoObjectId();
            UnsupportedCardTypeStaysUnrenderable();
            DetailOnlyAcceptsRenderableCards();

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

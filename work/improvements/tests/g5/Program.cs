using System;
using System.IO;
using System.Windows.Input;
using Bilibili.App.Dynamic.V2;
using BiliLite.Controls.Dynamic;
using BiliLite.Helpers;
using BiliLite.Modules;
using Google.Protobuf;

namespace G5Probe
{
    /// <summary>
    /// G5: the dynamic repost panel reads through gRPC RepostList.
    ///
    /// Executed against product source: RepostListReq/RepostListRsp protobuf round-trips,
    /// DynamicGrpcMapper.MapToUserCards and the new Helpers/DynamicRepostMapper. The VM
    /// (Modules/User/DynamicRepostVM.cs) cannot run outside UWP because it binds WinRT
    /// commands and the REST DynamicAPI, so its paging wiring is asserted by reading the
    /// source; every such check is named "source:" below. Remaining WinRT scaffolding
    /// (RichTextBlock, Visibility, LogHelper, AppHelper constants) is stubbed in G5Stubs.cs.
    /// </summary>
    public static class Program
    {
        private const string DynamicId = "8123456789012";
        private const string ContentText = "转发正文";
        private static int _passed;
        private static readonly ICommand UserCommand = new NoopCommand();

        public static int Main()
        {
            RepostListReqRoundTrips();
            RepostListRspRoundTrips();
            GrpcServiceUsesRepostListEndpoint();
            RepostItemMapsToDisplayModel();
            TimeFallsBackToPubdate();
            ItemWithoutTextYieldsEmptyContent();
            NullCardsAreSkipped();
            UnusableRepostPageFallsBackToRest();
            RepostVmKeepsCursorsSeparate();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        private static void RepostListReqRoundTrips()
        {
            var request = new RepostListReq { DynamicId = DynamicId, Offset = "cursor-2" };
            var parsed = RepostListReq.Parser.ParseFrom(request.ToByteArray());
            Check(parsed.DynamicId == DynamicId, "RepostListReq.DynamicId survives serialize/parse");
            Check(parsed.Offset == "cursor-2", "RepostListReq.Offset carries the page cursor");
            Check(new RepostListReq().Offset == "", "RepostListReq.Offset defaults to empty (first page)");
        }

        private static void RepostListRspRoundTrips()
        {
            var response = new RepostListRsp
            {
                Offset = "cursor-3",
                HasMore = true,
                List =
                {
                    new DynamicItem
                    {
                        CardType = DynamicType.Forward,
                        Extend = new Extend { DynIdStr = DynamicId },
                    }
                }
            };

            var parsed = RepostListRsp.Parser.ParseFrom(response.ToByteArray());
            Check(parsed.List.Count == 1, "RepostListRsp.List survives serialize/parse");
            Check(parsed.List[0].Extend != null && parsed.List[0].Extend.DynIdStr == DynamicId,
                "RepostListRsp.List item keeps Extend.DynIdStr");
            Check(parsed.Offset == "cursor-3", "RepostListRsp.Offset is the next-page cursor");
            Check(parsed.HasMore, "RepostListRsp.HasMore survives serialize/parse");

            var empty = RepostListRsp.Parser.ParseFrom(new RepostListRsp().ToByteArray());
            Check(empty.List.Count == 0, "empty RepostListRsp parses to an empty list");
            Check(!empty.HasMore, "empty RepostListRsp parses HasMore=false");
        }

        private static void GrpcServiceUsesRepostListEndpoint()
        {
            var source = File.ReadAllText(Path.Combine(RepoRoot(), "src", "BiliLite.UWP", "Services", "GrpcService.cs"));
            var method = SliceMethod(source, "public async Task<RepostListRsp> GetDynRepostList(");

            Check(method.Contains("public async Task<RepostListRsp> GetDynRepostList(string dynamicId, string offset = \"\")"),
                "GrpcService.GetDynRepostList takes dynamicId plus an optional offset");
            Check(method.Contains("new RepostListReq"), "GrpcService.GetDynRepostList builds a RepostListReq");
            Check(method.Contains("DynamicId = dynamicId,"), "GetDynRepostList sends RepostListReq.DynamicId");
            Check(method.Contains("Offset = offset ?? \"\","), "GetDynRepostList sends RepostListReq.Offset");
            Check(method.Contains("DynamicGrpcBaseUrl + \"RepostList\""),
                "GetDynRepostList targets DynamicGrpcBaseUrl + RepostList");
            Check(method.Contains("RepostListRsp.Parser.ParseFrom(result.results)"),
                "GetDynRepostList parses a RepostListRsp");
            Check(method.Contains("throw new Exception(result.message);"),
                "GetDynRepostList keeps the GetDynSpace error shape");
        }

        private static void RepostItemMapsToDisplayModel()
        {
            var item = new DynamicItem
            {
                CardType = DynamicType.Forward,
                Extend = new Extend { DynIdStr = DynamicId },
                Modules =
                {
                    new Module
                    {
                        ModuleType = DynModuleType.ModuleAuthor,
                        ModuleAuthor = new ModuleAuthor
                        {
                            Mid = 4242,
                            PtimeLabelText = "3小时前",
                            Author = new UserInfo
                            {
                                Name = "转发者",
                                Face = "https://i0.hdslb.com/bfs/face/1.jpg",
                                Vip = new VipInfo { Status = 1, Type = 2 },
                                Official = new OfficialVerify { Type = 0 },
                            },
                        },
                    },
                    new Module
                    {
                        ModuleType = DynModuleType.ModuleDesc,
                        ModuleDesc = new ModuleDesc { Text = ContentText },
                    },
                },
            };

            var cards = DynamicGrpcMapper.MapToUserCards(new[] { item });
            Check(cards.Count == 1, "a repost DynamicItem maps to one 2024 card");
            Check(cards[0].id_str == DynamicId, "mapped repost card keeps Extend.DynIdStr");

            var displays = DynamicRepostMapper.ToDisplayList(cards, UserCommand);
            Check(displays.Count == 1, "the repost mapper yields one display model");

            var display = displays[0];
            Check(display.Type == DynamicDisplayType.Repost, "repost display uses DynamicDisplayType.Repost");
            Check(display.DynamicID == DynamicId, "repost display keeps the dynamic id");
            Check(display.Mid == 4242, "repost display takes the author mid");
            Check(display.UserName == "转发者", "repost display takes the author name");
            Check(display.Photo == "https://i0.hdslb.com/bfs/face/1.jpg", "repost display takes the author face");
            Check(display.IsYearVip, "repost display detects an annual VIP");
            Check(display.Verify == AppHelper.VERIFY_PERSONAL_IMAGE, "repost display maps personal verification");
            Check(display.Time == "3小时前", "repost display shows the gRPC ptime label as relative time");
            Check(display.Datetime == "3小时前", "repost display falls back to the ptime label for the absolute time");
            // Bite test (a): deleting the ContentStr fill from DynamicRepostMapper fails this line.
            Check(display.ContentStr == ContentText, "repost display ContentStr equals the card plain text");
            Check(DynamicRepostMapper.GetContentText(cards[0]) == ContentText,
                "GetContentText returns the module desc text");
            Check(display.IsRepost, "repost display is flagged IsRepost");
            Check(ReferenceEquals(display.UserCommand, UserCommand), "repost display receives the VM user command");
        }

        private static void TimeFallsBackToPubdate()
        {
            var card = new DynamicCardModel2024
            {
                id_str = "777",
                modules = new DynamicCardModel2024_modules
                {
                    module_author = new DynamicCardModel2024_author
                    {
                        mid = 99,
                        pub_time = "",
                        pub_ts = 7200,
                        name = "无时间标签",
                        face = "face.png",
                    },
                },
            };

            var display = DynamicRepostMapper.ToDisplay(card, UserCommand);
            Check(display.Time == "relative:7200",
                "empty pub_time falls back to Utils.HandelTimestamp(pubdate)");
            Check(display.Datetime == new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(7200).ToString(),
                "empty pub_time falls back to Utils.TimestampToDatetime(pubdate)");
        }

        private static void ItemWithoutTextYieldsEmptyContent()
        {
            var noDesc = new DynamicItem
            {
                CardType = DynamicType.Forward,
                Extend = new Extend { DynIdStr = "8150000000001" },
            };

            var cards = DynamicGrpcMapper.MapToUserCards(new[] { noDesc });
            var displays = DynamicRepostMapper.ToDisplayList(cards, UserCommand);
            Check(displays.Count == 1, "a repost without desc still maps to one display model");
            Check(displays[0].ContentStr == "", "a repost without desc yields empty ContentStr instead of throwing");

            var noTextCard = new DynamicCardModel2024 { id_str = "8160000000001" };
            var display = DynamicRepostMapper.ToDisplay(noTextCard, UserCommand);
            Check(display != null && display.ContentStr == "",
                "a null desc yields empty ContentStr instead of throwing");
            Check(display.Time == "" && display.Datetime == "",
                "a card without author yields empty times instead of throwing");
        }

        private static void NullCardsAreSkipped()
        {
            Check(DynamicRepostMapper.ToDisplay(null, UserCommand) == null,
                "a null card maps to no display model");
            Check(DynamicRepostMapper.ToDisplayList(null, UserCommand).Count == 0,
                "a null card list maps to an empty display list");
            Check(DynamicRepostMapper.ToDisplayList(
                    new[] { (DynamicCardModel2024)null, new DynamicCardModel2024 { id_str = "1" } },
                    UserCommand).Count == 1,
                "null entries inside the card list are skipped");
        }

        private static void UnusableRepostPageFallsBackToRest()
        {
            var noExtend = new DynamicItem { CardType = DynamicType.Forward, Extend = null };
            var cards = DynamicGrpcMapper.MapToUserCards(new[] { noExtend });
            Check(cards.Count == 0, "a repost item without Extend maps to no cards");
            Check(DynamicRepostMapper.ToDisplayList(cards, UserCommand).Count == 0,
                "zero mapped cards produce zero display models (the VM unusable gate)");

            var vmSource = File.ReadAllText(Path.Combine(RepoRoot(), "src", "BiliLite.UWP", "Modules", "User", "DynamicRepostVM.cs"));
            var grpcPage = SliceMethod(vmSource, "private async Task<bool> TryLoadGrpcPage()");
            Check(grpcPage.Contains("if (displays.Count > 0)"),
                "source: the VM only accepts a gRPC page with at least one display model");
            Check(grpcPage.Contains("stage=repost-grpc-unusable"),
                "source: an unusable gRPC repost page is logged as repost-grpc-unusable");
            Check(grpcPage.Contains("useRestFallback = true;"),
                "source: an unusable gRPC repost page turns on the sticky REST fallback");
        }

        private static void RepostVmKeepsCursorsSeparate()
        {
            var root = RepoRoot();
            var vmSource = File.ReadAllText(Path.Combine(root, "src", "BiliLite.UWP", "Modules", "User", "DynamicRepostVM.cs"));
            var grpcPage = SliceMethod(vmSource, "private async Task<bool> TryLoadGrpcPage()");
            var restPage = SliceMethod(vmSource, "private async Task LoadRestPage()");
            var getItems = SliceMethod(vmSource, "public async Task GetDynamicItems()");
            var refresh = SliceMethod(vmSource, "public async void Refresh()");
            var clear = SliceMethod(vmSource, "public void Clear()");

            Check(vmSource.Contains("string grpcOffset = \"\";") && vmSource.Contains("string restOffset = \"\";"),
                "source: gRPC and REST paging cursors are separate fields");
            // Bite test (b), gRPC half: ignoring HasMore or setting CanLoadMore=true here fails this line.
            Check(grpcPage.Contains("CanLoadMore = rsp?.HasMore ?? false;"),
                "source: the gRPC path derives CanLoadMore only from RepostListRsp.HasMore");
            Check(!grpcPage.Contains("CanLoadMore = true;"),
                "source: the gRPC path never forces CanLoadMore on");
            Check(grpcPage.Contains("grpcOffset = rsp?.Offset ?? \"\";"),
                "source: the gRPC path advances grpcOffset from RepostListRsp.Offset");

            // Bite test (b), REST half: dropping the non-empty-offset guard or restoring the
            // has_more-into-cursor bug fails these lines.
            Check(restPage.Contains("CanLoadMore = hasMore && !string.IsNullOrEmpty(restOffset);"),
                "source: REST CanLoadMore requires has_more and a non-empty offset");
            Check(restPage.Contains("restOffset = data[\"data\"]?[\"offset\"]?.ToString() ?? \"\";"),
                "source: the REST path advances restOffset from the response offset");
            Check(!restPage.Contains("next = data[\"data\"]?[\"has_more\"]"),
                "source: the REST path no longer stores has_more as the cursor");
            Check(restPage.Contains("dynamicAPI.DynamicRepost(ID, restOffset)"),
                "source: the REST fallback requests the page with restOffset");
            Check(restPage.Contains("ConvertToDisplayRepost(item)"),
                "source: the REST fallback keeps the legacy JSON converter");

            Check(getItems.Contains("if (!useRestFallback && await TryLoadGrpcPage())"),
                "source: GetDynamicItems tries gRPC first while the fallback is off");
            Check(getItems.Contains("await LoadRestPage();"),
                "source: GetDynamicItems reaches the REST page when gRPC is unusable");
            Check(refresh.Contains("grpcOffset = \"\";") && refresh.Contains("restOffset = \"\";")
                  && refresh.Contains("useRestFallback = false;"),
                "source: Refresh resets both cursors and the fallback flag");
            Check(clear.Contains("grpcOffset = \"\";") && clear.Contains("restOffset = \"\";")
                  && clear.Contains("useRestFallback = false;"),
                "source: Clear resets both cursors and the fallback flag");
            Check(vmSource.Contains("JObject.Parse(item.card)"),
                "source: the legacy ConvertToDisplayRepost JSON shape is untouched");

            var csproj = File.ReadAllText(Path.Combine(root, "src", "BiliLite.UWP", "BiliLite.UWP.csproj"));
            Check(csproj.Contains("<Compile Include=\"Helpers\\DynamicRepostMapper.cs\" />"),
                "the UWP project compiles the new DynamicRepostMapper helper");
        }

        private static string SliceMethod(string source, string signature)
        {
            var start = source.IndexOf(signature, StringComparison.Ordinal);
            if (start < 0)
            {
                Check(false, "expected member not found in source: " + signature);
                return "";
            }

            var next = -1;
            foreach (var marker in new[] { "\n        public ", "\n        private ", "\n        internal " })
            {
                var index = source.IndexOf(marker, start + signature.Length, StringComparison.Ordinal);
                if (index >= 0 && (next < 0 || index < next))
                {
                    next = index;
                }
            }

            return next < 0 ? source.Substring(start) : source.Substring(start, next - start);
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

        private sealed class NoopCommand : ICommand
        {
            public event EventHandler CanExecuteChanged
            {
                add { }
                remove { }
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
            }
        }
    }
}

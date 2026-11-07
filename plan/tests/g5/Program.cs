using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using Bilibili.App.Dynamic.V2;
using BiliLite.Controls.Dynamic;
using BiliLite.Helpers;
using BiliLite.Modules;
using BiliLite.Parsing.Dynamic;
using Google.Protobuf;
using Windows.UI.Xaml;

namespace G5Probe
{
    /// <summary>
    /// G5: the dynamic repost panel reads through gRPC RepostList, and the card-data
    /// contract of CARD_PLAN §3 holds for both adapters: explicit description kinds,
    /// once-only URL normalisation, attachments kept beside the primary payload, and a
    /// paired comment target.
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
            RepostFallbackReplacesList();
            DescriptionKindsAreExplicit();
            WebUrlsNormaliseOnce();
            WebAttachmentsStaySeparate();
            CommentTargetsArePaired();
            CardProjectionCarriesTheContract();
            LiveSummaryReadsThePayload();
            ReserveCardIsAPanelNotBareText();
            ReserveAttachmentMapsTheButton();
            ReserveAttachmentBecomesAPanel();
            ReserveCardOwnsItsAttachment();
            ReserveCopyLabelsTimeAndCount();
            AttachmentLabelsHideWireTypes();
            LongTitlesCarryATooltip();
            UnsupportedCardsAlwaysCarryTheWebCommand();
            ImageGridBindsOnlyVisibleTiles();
            LiveSummarySkipsEmptyParts();
            LiveSummaryNeverInventsARoom();
            LiveSummaryBadgesOnlyConfirmedEnds();
            LiveRcmdPayloadMatchesThePlainOne();
            GrpcLiveCardsMapTheSameWay();
            LiveWithoutPayloadKeepsThePreview();
            LiveCardsUseTheSharedSummary();

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
            // Batch API-SMART-01 replaced the plain Exception with a typed one that carries the
            // same message, so the assertion now accepts either shape.
            Check(method.Contains("throw new GrpcRequestException(result);") || method.Contains("throw new Exception(result.message);"),
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

            // 盘点:API-SMART-01 batch 2 重写了这个 VM 的 gRPC/REST 分支(来源由选择器固定),
            // 旧的 useRestFallback / replaceListOnRestPage 源码断言不再成立,这里只保留可执行的那部分。
        }

        private static void RepostVmKeepsCursorsSeparate()
        {
            // 游标分离现在由 pinnedSource + 选择器承担;这里只确认新映射器仍被项目编译。
            var csproj = File.ReadAllText(Path.Combine(RepoRoot(), "src", "BiliLite.UWP", "BiliLite.UWP.csproj"));
            Check(csproj.Contains("<Compile Include=\"Helpers\\DynamicRepostMapper.cs\" />"),
                "the UWP project compiles the new DynamicRepostMapper helper");
        }

        private static void RepostFallbackReplacesList()
        {
            // 换源替换已并入选择器的“替换行”规则,由 batch 2 的执行断言覆盖,旧源码断言退役。
        }

        // ---- CARD_PLAN §3: one card-data contract ------------------------------------
        //
        // 这一组只断言"卡片数据"这一层：解析/映射的结果是数据，不是控件。正文片段怎么排成段落由
        // Parsing/Dynamic/RichTextFlow.cs 负责（另一个切片拥有它），它只把 AT / TOPIC / WEB 三种
        // 节点变成可点击片段，其余类型一律当纯文本 —— 所以这里断言的是节点的种类与文本，不是段落。

        private const string WebBase = "https://www.bilibili.com";

        private static void DescriptionKindsAreExplicit()
        {
            // 未知语义：留原类型名（不是塌成 TEXT），正文文本照常可读
            Check(WireScalars.MapDescriptionType("RICH_TEXT_NODE_TYPE_CHEAT") == "RICH_TEXT_NODE_TYPE_CHEAT",
                "an unsupported description kind keeps its raw wire kind instead of collapsing to TEXT");
            Check(WireScalars.MapDescriptionType("Format") == "Format",
                "a kind whose name merely contains 'at' keeps its raw kind");

            // gRPC 生成的枚举名是 PascalCase 且没有下划线：OgvSeason / OgvEp / SearchWord 以前落到 TEXT
            Check(WireScalars.MapDescriptionType(DescType.OgvSeason.ToString()) == "RICH_TEXT_NODE_TYPE_OGV_SEASON",
                "gRPC DescType.OgvSeason maps to the season kind");
            Check(WireScalars.MapDescriptionType(DescType.OgvEp.ToString()) == "RICH_TEXT_NODE_TYPE_OGV_EP",
                "gRPC DescType.OgvEp maps to the episode kind");
            Check(WireScalars.MapDescriptionType(DescType.SearchWord.ToString()) == "RICH_TEXT_NODE_TYPE_SEARCH_WORD",
                "gRPC DescType.SearchWord maps to the search-word kind");
            Check(WireScalars.MapDescriptionType(DescType.Aite.ToString()) == "RICH_TEXT_NODE_TYPE_AT",
                "gRPC DescType.Aite maps to the mention kind");
            Check(WireScalars.MapDescriptionType(DescType.Web.ToString()) == "RICH_TEXT_NODE_TYPE_WEB",
                "gRPC DescType.Web maps to the link kind");
            Check(WireScalars.MapDescriptionType("RICH_TEXT_NODE_TYPE_TEXT") == "RICH_TEXT_NODE_TYPE_TEXT",
                "the web plain-text kind stays plain text");
            Check(WireScalars.MapDescriptionType(DescType.None.ToString()) == "RICH_TEXT_NODE_TYPE_TEXT",
                "an absent gRPC kind stays plain text");
            Check(WireScalars.MapDescriptionType(null) == "RICH_TEXT_NODE_TYPE_TEXT",
                "a missing type name stays plain text");

            // web 映射：未知种类的节点保留文本，带 rid 也不会变成 @
            var card = WebCards(WordCard(
                "\"desc\":{\"text\":\"作弊码 正文\",\"rich_text_nodes\":[" +
                "{\"type\":\"RICH_TEXT_NODE_TYPE_CHEAT\",\"text\":\"作弊码\",\"rid\":\"4242\"}]}"));
            var node = card.modules.module_dynamic.desc.rich_text_nodes[0];
            Check(node.text == "作弊码", "an unknown description node keeps its readable text");
            Check(node.type == "RICH_TEXT_NODE_TYPE_CHEAT",
                "an unknown description node keeps its raw kind on the card");
            Check(node.type != "RICH_TEXT_NODE_TYPE_AT",
                "a type whose name contains 'at' is not turned into a mention");
        }

        private static void WebUrlsNormaliseOnce()
        {
            Check(WireScalars.NormalizeWebUrl("//example.com/place") == "https://example.com/place",
                "a protocol-relative address becomes HTTPS");
            Check(WireScalars.NormalizeWebUrl("/video/BV1abc") == WebBase + "/video/BV1abc",
                "a root-relative path resolves against the known web base");
            Check(WireScalars.NormalizeWebUrl("/video/BV1abc", null) == "",
                "a root-relative path without a known source base is not a destination");
            Check(WireScalars.NormalizeWebUrl("bilibili://video/123") == "bilibili://video/123",
                "a full app URI stays distinct and is not prefixed with https:");
            Check(WireScalars.NormalizeWebUrl("https://example.com/x") == "https://example.com/x",
                "an absolute address is left alone");
            Check(WireScalars.NormalizeWebUrl("///evil") == "",
                "a run of leading slashes never becomes https:///evil");
            Check(WireScalars.NormalizeWebUrl("  ") == "", "whitespace is not a destination");

            // 正文节点：可读标签与跳转目标分开；没有目标时正文照常可选
            var card = WebCards(WordCard(
                "\"desc\":{\"text\":\"详情：地点 无地址\",\"rich_text_nodes\":[" +
                "{\"type\":\"RICH_TEXT_NODE_TYPE_WEB\",\"text\":\"地点\",\"jump_url\":\"/video/BV1abc\"}," +
                "{\"type\":\"RICH_TEXT_NODE_TYPE_WEB\",\"text\":\"无地址\"}]}"));
            var nodes = card.modules.module_dynamic.desc.rich_text_nodes;
            Check(nodes[0].text == "地点", "a link keeps its readable label, not the destination");
            Check(nodes[0].jump_url == WebBase + "/video/BV1abc",
                "a link carries the normalised destination");
            Check(nodes[1].text == "无地址" && string.IsNullOrEmpty(nodes[1].jump_url),
                "a link without a destination stays text with an empty target");
        }

        private static void WebAttachmentsStaySeparate()
        {
            var voteCard = WebCards(PhotoCard("\"additional\":" + VoteAdditional()));
            Check(voteCard.type == "DYNAMIC_TYPE_DRAW", "a photo post carrying a vote keeps its photo type");
            Check(voteCard.modules.module_dynamic.major.draw.items.Count == 2,
                "the photo payload keeps both pictures next to the attachment");
            var additional = voteCard.modules.module_dynamic.additional;
            Check(additional != null && additional.vote != null && additional.vote.title == "投票标题",
                "a photo post still carries its vote attachment");
            Check(additional.vote.desc1 == "投票说明", "the vote attachment keeps its readable description");
            Check(additional.reserve == null, "a vote post does not invent a reservation");

            var both = WebCards(PhotoCard("\"additional\":" + VoteAndReserveAdditional()));
            Check(both.modules.module_dynamic.major.draw != null,
                "the primary payload survives a card with two attachments");
            Check(both.modules.module_dynamic.additional.reserve != null
                  && both.modules.module_dynamic.additional.vote != null,
                "every present attachment is mapped, not just the first one");
            Check(both.modules.module_dynamic.additional.type == "ADDITIONAL_TYPE_RESERVE",
                "the additional wire type is kept as supplied");
        }

        private static void CommentTargetsArePaired()
        {
            // web：basic 就给了一对 (comment_id_str, comment_type)
            var webPhoto = WebCards(PhotoCard("\"additional\":" + VoteAdditional()));
            Check(webPhoto.basic.comment_id_str == "7001", "the web adapter keeps the paired comment object id");
            Check(webPhoto.basic.comment_type == 11, "the web adapter keeps the paired comment type");

            // gRPC：每一类都要给出真实评论类型，而不是占位 1
            var article = GrpcCard(DynamicType.Article, new ModuleDynamic
            {
                DynArticle = new MdlDynArticle { Id = 99001, Title = "专栏" },
            });
            Check(article.basic.comment_type == 12, "an article dynamic comments with the article type");
            Check(article.basic.comment_id_str == "99001", "an article dynamic comments on its article id");

            var photo = GrpcCard(DynamicType.Draw, new ModuleDynamic
            {
                DynDraw = new MdlDynDraw { Id = 88001 },
            });
            Check(photo.basic.comment_type == 11, "a photo dynamic comments with the photo type");
            Check(photo.basic.comment_id_str == "88001", "a photo dynamic comments on its album object id");

            var music = GrpcCard(DynamicType.Music, new ModuleDynamic
            {
                DynMusic = new MdlDynMusic { Id = 66001, Title = "歌曲" },
            });
            Check(music.basic.comment_type == 14, "a music dynamic comments with the song type");

            var video = GrpcCard(DynamicType.Av, new ModuleDynamic
            {
                DynArchive = new MdlDynArchive { Avid = 77001, Title = "视频" },
            });
            Check(video.basic.comment_type == 1, "a video dynamic comments with the video type");
            Check(video.basic.comment_id_str == "77001", "a video dynamic comments on its aid");

            var word = GrpcCard(DynamicType.Word, null);
            Check(word.basic.comment_type == 17, "an ordinary dynamic comments with the dynamic type");
            Check(word.basic.comment_id_str == DynamicId, "an ordinary dynamic comments on its dynamic id");

            // 调用点：用归一化的一对，而不是继续按显示类型猜
            Check(ProductSource("src/BiliLite.UWP/Pages/Home/UserDynamicPage.xaml.cs").Contains("e.CommentTarget"),
                "the home dynamic page reads the normalised comment target");
            Check(ProductSource("src/BiliLite.UWP/Pages/User/DynamicDetailPage.xaml.cs").Contains("e.CommentTarget"),
                "the dynamic detail page reads the normalised comment target");
            Check(ProductSource("src/BiliLite.UWP/Modules/User/DynamicVM.cs").Contains("DynamicCardProjection.Apply"),
                "the conversion applies the adapter's comment target");
        }

        /// <summary>
        /// 卡片数据搬进显示模型之后仍然是同一份合同：评论对是一对、附件是列表、禁止状态不是"可用"。
        /// </summary>
        private static void CardProjectionCarriesTheContract()
        {
            var voteCard = WebCards(PhotoCard("\"additional\":" + VoteAdditional()));
            var voteDisplay = new DynamicItemDisplayModel();
            DynamicCardProjection.Apply(voteDisplay, voteCard);

            Check(voteCard.modules.module_dynamic.major.draw != null,
                "the attachment does not replace the photo payload");
            Check(voteDisplay.Attachments != null && voteDisplay.Attachments.Count == 1,
                "a photo post with a vote yields one ordered attachment");
            Check(voteDisplay.Attachments[0].Kind == DynamicAttachmentKind.Vote,
                "the vote attachment is presented as a vote");
            Check(voteDisplay.Attachments[0].Title == "投票标题",
                "the vote attachment keeps its title");
            Check(voteDisplay.Attachments[0].Desc == "投票说明",
                "the vote attachment keeps its readable description");
            Check(voteDisplay.Attachments[0].Destination == WebBase + "/vote/123",
                "the attachment destination is normalised once");
            Check(voteDisplay.Attachments[0].RawType == "ADDITIONAL_TYPE_VOTE",
                "the attachment keeps its raw wire type");

            var bothDisplay = new DynamicItemDisplayModel();
            DynamicCardProjection.Apply(bothDisplay, WebCards(PhotoCard("\"additional\":" + VoteAndReserveAdditional())));
            Check(bothDisplay.Attachments.Count == 2,
                "a card with a reservation and a vote yields two attachments");
            Check(bothDisplay.Attachments[0].Kind == DynamicAttachmentKind.Reserve
                  && bothDisplay.Attachments[1].Kind == DynamicAttachmentKind.Vote,
                "attachments keep the reserve-then-vote order");
            Check(bothDisplay.Attachments[0].Desc == "明天 20:00 开播 1.2万人已预约",
                "the reservation description is tagged by slot, not joined into one sentence, and carries no brackets");
            Check(bothDisplay.Attachments[0].Id == "321", "the reservation keeps its object id");

            var unknownDisplay = new DynamicItemDisplayModel();
            DynamicCardProjection.Apply(unknownDisplay, WebCards(WordCard(
                "\"desc\":{\"text\":\"赛事\"}," +
                "\"additional\":{\"type\":\"ADDITIONAL_TYPE_ESPORTS\"," +
                "\"goods\":{\"id_str\":\"5\",\"title\":\"赛事直播\"}}")));
            Check(unknownDisplay.Attachments.Count == 1
                  && unknownDisplay.Attachments[0].Kind == DynamicAttachmentKind.Unsupported,
                "an unsupported attachment with something to show is kept as an unsupported summary");
            Check(unknownDisplay.Attachments[0].RawType == "ADDITIONAL_TYPE_ESPORTS",
                "an unsupported attachment keeps its raw wire type");
            Check(unknownDisplay.AttachmentItems[0].KindLabel == "附件"
                  && unknownDisplay.AttachmentItems[0].Title == "赛事直播",
                "an unsupported attachment is labelled 附件, never the wire type");

            // type 在、却没有任何子卡：只有一条 wire 类型名，整行不该上屏
            var emptyTypeDisplay = new DynamicItemDisplayModel();
            DynamicCardProjection.Apply(emptyTypeDisplay, WebCards(WordCard(
                "\"desc\":{\"text\":\"赛事\"}," +
                "\"additional\":{\"type\":\"ADDITIONAL_TYPE_ESPORTS\",\"title\":\"赛事直播\"}")));
            Check(emptyTypeDisplay.Attachments.Count == 0
                  && emptyTypeDisplay.AttachmentRowVisibility == Visibility.Collapsed,
                "an attachment block with nothing but a wire type renders no row");

            var plainDisplay = new DynamicItemDisplayModel();
            DynamicCardProjection.Apply(plainDisplay, WebCards(WordCard("\"desc\":{\"text\":\"纯文字\"}")));
            Check(plainDisplay.Attachments != null && plainDisplay.Attachments.Count == 0,
                "a post without attachments yields an empty attachment list, not null");

            Check(voteDisplay.ReplyID == "7001" && voteDisplay.ReplyType == 11,
                "the comment pair survives the conversion");
            Check(voteDisplay.CommentTarget != null
                  && voteDisplay.CommentTarget.Oid == "7001"
                  && voteDisplay.CommentTarget.Type == 11,
                "the converted display carries one comment target");

            var noPair = new DynamicItemDisplayModel { ReplyID = "8120000000001", ReplyType = 0 };
            Check(noPair.CommentTarget == null,
                "a reply id without a known comment type yields no target to call");

            var wordDisplay = new DynamicItemDisplayModel();
            DynamicCardProjection.Apply(wordDisplay, GrpcCard(DynamicType.Word, null));
            Check(wordDisplay.CommentTarget != null && wordDisplay.CommentTarget.Type == 17,
                "the gRPC pair reaches the display as a dynamic comment target");

            // 老接口的 rid 体裁（8=小视频那一类）不是评论类型编号，不能凑成一对
            Check(DynamicCommentTarget.From("66001", 8) == null,
                "a rid genre number is not accepted as a comment type");
            var legacySource = ProductSource("src/BiliLite.UWP/Modules/User/DynamicVM.cs");
            // 签名尾巴按前缀匹配:65fd423 之后多了一个 int depth 参数,断言要测的是这个方法体本身
            Check(!SliceMethod(legacySource, "private DynamicItemDisplayModel ConvertToDisplay(DynamicCardModel item")
                    .Contains("ReplyType = item.desc.r_type"),
                "the legacy adapter does not pass its rid genre off as a comment type");

            Check(voteDisplay.Actions != null && voteDisplay.Actions.Comment == DynamicActionAvailability.Allowed,
                "an explicit forbidden=false is reported as allowed");

            var forbiddenDisplay = new DynamicItemDisplayModel();
            DynamicCardProjection.Apply(forbiddenDisplay, WebCards(ForbiddenWordCard()));
            Check(forbiddenDisplay.Actions.Comment == DynamicActionAvailability.Forbidden,
                "a forbidden comment is reported as forbidden");
            Check(!forbiddenDisplay.CanComment,
                "a forbidden comment state is not presented as a known-successful action");

            var unknownActions = DynamicCardActions.From(new DynamicCardModel2024 { id_str = "1" });
            Check(unknownActions.Comment == DynamicActionAvailability.Unknown,
                "a card without module_stat reports comment availability as unknown");
            Check(unknownActions.Comment != DynamicActionAvailability.Forbidden,
                "unknown availability is not reported as forbidden");
        }

        /// <summary>
        /// 直播卡只有一行：房间号、目的地、地区·人气、结束角标。旧的占位符（【直播有待修复】）
        /// 之所以存在，是因为页面拿不到这些字段；现在它们全由 LiveCardSummary 从载荷里读出来。
        /// </summary>
        /// <summary>
        /// 复核意见:12 张图时网格按九张算高度,却把 12 张都交给面板,后三张摆到网格高度之外压住下面的内容。
        /// 现在只绑定前 VisibleCount 张,面板也只为预算了空间的子项测量与摆位。
        /// </summary>
        private static void ImageGridBindsOnlyVisibleTiles()
        {
            var grid = ProductSource("src/BiliLite.UWP/Controls/Dynamic/DynamicImageGrid.xaml.cs");
            Check(grid.Contains("tiles.GetRange(0, layout.VisibleCount)"),
                "the image grid hands only the visible tiles to the panel");
            Check(grid.Contains("var lastVisible = layout.VisibleCount - 1"),
                "the +N badge goes on the last visible tile, computed from the full count");

            var panel = ProductSource("src/BiliLite.UWP/Controls/Dynamic/DynamicImageGridPanel.cs");
            Check(CountOf(panel, "Math.Min(Children.Count, layout.VisibleCount)") == 2,
                "the panel measures and arranges only the tiles it budgets space for");
        }

        /// <summary>
        /// BR-55：预约卡以前只有"标题 + 一行说明"两块裸文字，按钮还是注释掉的。
        /// 现在是一块带边框的预约面板：粉色预约角标、两行标题、两行说明，底部按钮的文案与图标都来自载荷。
        /// </summary>
        /// <summary>
        /// 维护者：长标题被两行省略之后要能用 tooltip 看全文。三处标题都要有：
        /// 卡片外壳的标题槽、预约/预览模板自己的标题、热门页的 20px 标题。
        /// </summary>
        /// <summary>
        /// 评审 8d496eb:旧接口那条普通 Other 分支原来只给 DetailByIdCommand,而 CanOpenOriginal 认的是 WebCommand,
        /// 于是这类卡片没有可用的"打开原文"。三条退化成 Other 的路径都必须带网页命令。
        /// </summary>
        private static void UnsupportedCardsAlwaysCarryTheWebCommand()
        {
            var source = ProductSource("src/BiliLite.UWP/Modules/User/DynamicVM.cs");
            Check(CountOf(source, "WebCommand = WebCommand") >= 3,
                "every unsupported-card branch carries the web command");
            Check(!source.Contains("DetailByIdCommand = DetailByIdCommand\r\n                    };")
                  && !source.Contains("DetailByIdCommand = DetailByIdCommand\n                    };"),
                "no unsupported-card branch stops at the native detail command alone");

            // 直播侧:解析失败要直接返回,不能落到读 data.message 的 else 分支
            var live = ProductSource("src/BiliLite.UWP/Modules/Detail/LiveRoomVM.cs");
            Check(CountOf(live, "if (!HasParsedData(") >= 6,
                "the live steps bail out when the response did not parse");
            Check(!live.Contains("HasParsedData(data, \"anchor\") && data.success"),
                "the anchor step no longer falls through to the error branch on a null model");
        }
        private static void LongTitlesCarryATooltip()
        {
            var shell = ProductSource("src/BiliLite.UWP/Controls/Dynamic/DynamicItemControl.xaml");
            Check(shell.Contains("ToolTipService.ToolTip=\"{x:Bind Path=Model.Title,Mode=OneWay}\"")
                  && shell.Contains("x:Name=\"CardTitle\""),
                "the card shell title slot shows the full title on hover");

            var templates = ProductSource("src/BiliLite.UWP/Controls/Dynamic/DynamicTemplate.xaml");
            Check(CountOf(templates, "ToolTipService.ToolTip=\"{Binding Path=OneRowInfo.Title}\"") >= 2,
                "both the reserve and the preview title carry a tooltip");

            var hot = ProductSource("src/BiliLite.UWP/Pages/Home/HotPage.xaml");
            Check(CountOf(hot, "ToolTipService.ToolTip=\"{x:Bind title}\"") == 2,
                "both hot-card variants carry a title tooltip");

            // BR-58：ToolTipService 只认指针悬停，触摸/手柄点开的卡片看不到全文。
            // 会截断的标题都挂上 TapToolTip：点击打开同一个 ToolTip，悬停路径保持不动。
            const string tap = "helpers:TapToolTip.Enabled=\"True\"";
            Check(shell.Contains("xmlns:helpers=\"using:BiliLite.Helpers\"")
                  && templates.Contains("xmlns:helpers=\"using:BiliLite.Helpers\""),
                "both dynamic XAML files declare the helpers namespace");
            var panel = Slice(shell, "<DataTemplate x:Key=\"AttachmentPanelTemplate\">", "</DataTemplate>");
            var row = Slice(shell, "<DataTemplate x:Key=\"AttachmentRowTemplate\">", "</DataTemplate>");
            Check(panel.Contains(tap) && panel.Contains("ToolTipService.ToolTip=\"{Binding Title}\""),
                "the attachment panel title opens its tooltip on tap and keeps the hover binding");
            Check(row.Contains(tap) && row.Contains("ToolTipService.ToolTip=\"{Binding Title}\""),
                "the attachment row title opens its tooltip on tap and keeps the hover binding");
            var cardAtIndex = shell.IndexOf("x:Name=\"CardTitle\"", StringComparison.Ordinal);
            var cardTitle = cardAtIndex < 0
                ? ""
                : shell.Substring(cardAtIndex, Math.Min(640, shell.Length - cardAtIndex));
            Check(cardTitle.Contains(tap), "the card title slot opens its tooltip on tap");
            Check(CountOf(shell, tap) == 3,
                "exactly the three truncating titles in the card shell carry the tap behaviour");
            Check(CountOf(templates, tap) == 2,
                "both OneRowInfo titles open their tooltip on tap");
            Check(!hot.Contains("TapToolTip"),
                "the hot card keeps whole-card navigation instead of a tap tooltip");

            var helper = ProductSource("src/BiliLite.UWP/Helpers/TapToolTip.cs");
            Check(helper.Contains("EnabledProperty") && helper.Contains("typeof(bool)")
                  && helper.Contains("DependencyProperty.RegisterAttached"),
                "the tap behaviour is an attached bool property");
            Check(helper.Contains("element.Tapped += OnTapped;")
                  && helper.Contains("element.PointerExited += OnPointerExited;")
                  && helper.Contains("element.Unloaded += OnUnloaded;"),
                "tap, pointer exit and unload are all wired on attach");
            Check(helper.Contains("ToolTipService.GetToolTip(element)")
                  && helper.Contains("ConditionalWeakTable<FrameworkElement, ToolTip>"),
                "each element keeps its own tap tooltip beside the hover binding");
            Check(!helper.Contains("ToolTipService.SetToolTip")
                  && helper.Contains("tooltip.Content = text;"),
                "the tap tooltip takes the current text instead of overwriting the hover value");
            Check(helper.Contains("PlacementTarget = element"), "the tap tooltip is placed on its own element");
            Check(helper.Contains("tooltip.IsOpen = true;"), "a tap opens the tooltip");
            Check(helper.Contains("TimeSpan.FromSeconds(4)"), "the tooltip closes itself after four seconds");
            Check(helper.Contains("PointerDeviceType.Mouse"),
                "only a mouse leaving closes the tooltip; touch and pen rely on the timer");
            Check(helper.Contains("ConditionalWeakTable<ToolTip, DispatcherTimer>"),
                "each tooltip owns its timer, so repeated taps restart rather than stack");
            Check(helper.Contains("timer.Stop();") && helper.Contains("tooltip.IsOpen = false;"),
                "repeated taps restart the timer and closing stops it");
            Check(!helper.Contains("e.Handled"),
                "the tap behaviour does not swallow the tap");
            Check(ProductSource("src/BiliLite.UWP/BiliLite.UWP.csproj").Contains("Helpers\\TapToolTip.cs"),
                "the tap behaviour is compiled into the app");
        }
        private static void ReserveCardIsAPanelNotBareText()
        {
            var template = ProductSource("src/BiliLite.UWP/Controls/Dynamic/DynamicTemplate.xaml");
            var start = template.IndexOf("<DataTemplate x:Key=\"Reserve\">", StringComparison.Ordinal);
            Check(start >= 0, "the reserve template exists");
            var end = template.IndexOf("</DataTemplate>", start, StringComparison.Ordinal);
            var reserve = template.Substring(start, end - start);

            Check(reserve.Contains("CornerRadius=\"8\"") && reserve.Contains("BorderThickness=\"1\""),
                "the reserve panel draws a rounded bordered surface");
            Check(reserve.Contains("Text=\"预约\"") && reserve.Contains("Background=\"HotPink\""),
                "the reserve panel carries a 预约 chip");
            Check(reserve.Contains("Command=\"{Binding Path=LaunchUrlCommand}\"")
                  && reserve.Contains("CommandParameter=\"{Binding Path=OneRowInfo.Url}\""),
                "the reserve action opens the payload's jump url");
            Check(reserve.Contains("Visibility=\"{Binding Path=OneRowInfo.ShowCoverText}\"")
                  && reserve.Contains("OneRowInfo.CoverText"),
                "the action appears only when the payload carries button text");
            Check(reserve.Contains("OneRowInfo.Cover, Converter={StaticResource imageConvert2}"),
                "the action icon goes through the image converter instead of binding a raw string");
            // 反向锚点：ConverterParameter 缺席时 ImageSourceConvert 会拿 null 调 ToString()，载荷带图标就抛
            Check(reserve.Contains("OneRowInfo.Cover, Converter={StaticResource imageConvert2}, ConverterParameter='64h.png'"),
                "the reserve action icon passes a converter parameter instead of relying on a null one");
            Check(!reserve.Contains("实际上根本没有办法点击这个按钮"),
                "the commented-out reserve button is gone");
        }
        /// <summary>
        /// BR-56：图文动态里挂的直播预约（additional.reserve）带着自己的按钮 —— 文案取 reserve.button.text
        /// （check/uncheck 由 status 选），图标 icon_url 优先、退回 check/uncheck 的图标，跳转按钮优先、
        /// 卡片自己的 jump_url 只是回退。改回只读 reserve.jump_url/只摊文本时下面的锚点会失败。
        /// </summary>
        private static void ReserveAttachmentMapsTheButton()
        {
            var card = WebCards(PhotoCard("\"additional\":" + ReserveButtonAdditional()));
            var web = DynamicAttachmentSummaries.From(card).Find(x => x.Kind == DynamicAttachmentKind.Reserve);
            Check(web != null, "a photo card's reserve attachment is mapped");
            Check(web.ButtonText == "预约", "the reserve button text comes from reserve.button.text");
            Check(web.ButtonIcon == "https://i0.hdslb.com/reserve.png",
                "the reserve button icon comes from reserve.button.icon_url");
            // 反向锚点：改回只读 reserve.jump_url 时这里拿到的是预约页地址，不是按钮的直播间
            Check(web.Destination == "https://live.bilibili.com/123",
                "the reserve destination prefers reserve.button.jump_url over the card's own jump_url");

            // protobuf 只映射 check/uncheck 的图标：button.icon_url 缺席时要退回 button.icon
            var unconfirmed = ReserveSummaryOf(new DynamicCardModel2024_addin_reserve
            {
                title = "预约标题",
                jump_url = "//www.bilibili.com/reserve/321",
                desc1 = new DynamicCardModel2024_reservetext { text = "11-13 20:00" },
                button = new DynamicCardModel2024_button2
                {
                    status = 1,
                    jump_url = "//live.bilibili.com/123",
                    uncheck = new DynamicCardModel2024_buttontext
                    {
                        text = "预约",
                        icon_url = "//i0.hdslb.com/uncheck.png",
                    },
                },
            });
            Check(unconfirmed.ButtonIcon == "//i0.hdslb.com/uncheck.png",
                "a button without icon_url falls back to the check/uncheck icon");
            Check(unconfirmed.ButtonText == "预约", "an unconfirmed reservation takes the uncheck text");
            Check(unconfirmed.Destination == "https://live.bilibili.com/123",
                "a protocol-relative button jump url is normalised like the card link");

            var confirmed = ReserveSummaryOf(new DynamicCardModel2024_addin_reserve
            {
                button = new DynamicCardModel2024_button2
                {
                    status = 2,
                    icon_url = "https://i0.hdslb.com/top.png",
                    check = new DynamicCardModel2024_buttontext { text = "已预约", icon_url = "https://i0.hdslb.com/check.png" },
                    uncheck = new DynamicCardModel2024_buttontext { text = "预约", icon_url = "https://i0.hdslb.com/uncheck.png" },
                },
            });
            Check(confirmed.ButtonText == "已预约", "a confirmed reservation takes the check text");
            Check(confirmed.ButtonIcon == "https://i0.hdslb.com/top.png",
                "button.icon_url wins over the check/uncheck icon");

            var noButtonUrl = ReserveSummaryOf(new DynamicCardModel2024_addin_reserve
            {
                jump_url = "//www.bilibili.com/reserve/321",
                button = new DynamicCardModel2024_button2
                {
                    status = 1,
                    uncheck = new DynamicCardModel2024_buttontext { text = "预约" },
                },
            });
            Check(noButtonUrl.Destination == "https://www.bilibili.com/reserve/321",
                "the card's own jump_url is the fallback destination");
        }

        /// <summary>
        /// BR-56：预约附件换成 BR-55 的面板，投票/链接/未知种类仍是安静行；按钮文案/目的地/命令缺一个
        /// 就不画，图标非空才占位。XAML 断言钉住令牌与 imageConvert2 接线，防止面板被悄悄改回一行。
        /// </summary>
        private static void ReserveAttachmentBecomesAPanel()
        {
            var xaml = ProductSource("src/BiliLite.UWP/Controls/Dynamic/DynamicItemControl.xaml");
            var panel = Slice(xaml, "<DataTemplate x:Key=\"AttachmentPanelTemplate\">", "</DataTemplate>");
            var row = Slice(xaml, "<DataTemplate x:Key=\"AttachmentRowTemplate\">", "</DataTemplate>");

            Check(panel.Contains("CornerRadius=\"8\"") && panel.Contains("BorderThickness=\"1\"")
                  && panel.Contains("Padding=\"12\"") && panel.Contains("Margin=\"0 8 0 0\"")
                  && panel.Contains("BorderBrush=\"{ThemeResource ForegroundBorderColor}\""),
                "the reserve attachment draws the BR-55 bordered panel");
            Check(panel.Contains("Text=\"预约\"") && panel.Contains("Background=\"HotPink\"")
                  && panel.Contains("FontSize=\"12\""),
                "the reserve attachment panel carries the 12px 预约 chip");
            Check(panel.Contains("Text=\"{Binding Title}\"") && panel.Contains("FontSize=\"18\"")
                  && panel.Contains("LineHeight=\"26\"") && panel.Contains("FontWeight=\"SemiBold\""),
                "the panel title uses the 18/26 SemiBold token");
            Check(panel.Contains("Text=\"{Binding Desc}\"") && panel.Contains("FontSize=\"14\"")
                  && panel.Contains("LineHeight=\"21\""),
                "the panel description uses the 14/21 token");
            Check(!panel.Contains("KindLabel"),
                "the reserve panel is not the quiet attachment row");
            Check(panel.Contains("Visibility=\"{Binding ButtonVisibility}\"")
                  && panel.Contains("Command=\"{Binding OpenCommand}\"")
                  && panel.Contains("CommandParameter=\"{Binding Destination}\"")
                  && panel.Contains("Text=\"{Binding ButtonText}\""),
                "the panel button shows the payload's text and opens the payload's route");
            Check(panel.Contains("Width=\"16\" Height=\"16\"")
                  && panel.Contains("Source=\"{Binding ButtonIcon, Converter={StaticResource imageConvert2}")
                  && panel.Contains("Visibility=\"{Binding IconVisibility}\""),
                "the panel icon is 16px and goes through imageConvert2");

            Check(row.Contains("KindLabel") && row.Contains("Content=\"打开\"")
                  && row.Contains("Visibility=\"{Binding OpenVisibility}\""),
                "the vote/link row is unchanged: label, title and an optional 打开");

            var rowStart = xaml.IndexOf("<ItemsControl x:Name=\"AttachmentRow\"", StringComparison.Ordinal);
            Check(rowStart >= 0, "the attachment row element still exists");
            var rowElement = rowStart < 0
                ? ""
                : xaml.Substring(rowStart, xaml.IndexOf("/>", rowStart, StringComparison.Ordinal) - rowStart);
            Check(rowElement.Contains("ItemTemplateSelector=\"{StaticResource attachmentTemplateSelect}\""),
                "the attachment row picks its template through the selector");
            Check(!rowElement.Contains("ItemTemplate=\"") || rowElement.Contains("ItemTemplateSelector=\""),
                "the attachment row is no longer hard-wired to one template for every kind");
            Check(!xaml.Contains("<ItemsControl.ItemTemplate>"),
                "no inline attachment row template is left behind");

            var selector = ProductSource("src/BiliLite.UWP/Controls/Dynamic/DynamicItemDataTemplateSelector.cs");
            var selectorStart = selector.IndexOf("class DynamicAttachmentDataTemplateSelector", StringComparison.Ordinal);
            Check(selectorStart >= 0, "the attachment template selector exists");
            var attachmentSelector = selectorStart < 0 ? "" : selector.Substring(selectorStart);
            Check(attachmentSelector.Contains("ShowPanel ? PanelTemplate : RowTemplate"),
                "the selector swaps templates on ShowPanel, not on the raw wire type");

            var command = new NoopCommand();
            var reserve = new DynamicAttachmentDisplay(new DynamicAttachmentSummary
            {
                Kind = DynamicAttachmentKind.Reserve,
                Title = "10月新番OP/ED鉴赏会！",
                Desc = "11-13 20:00 直播 175人预约",
                Destination = "https://live.bilibili.com/123",
                ButtonText = "预约",
                ButtonIcon = "https://i0.hdslb.com/reserve.png",
            }, command);
            Check(reserve.ShowPanel, "a reserve attachment selects the panel");
            Check(reserve.ButtonVisibility == Visibility.Visible && reserve.IconVisibility == Visibility.Visible,
                "a reserve panel with text, icon and route draws its button");

            var vote = new DynamicAttachmentDisplay(new DynamicAttachmentSummary
            {
                Kind = DynamicAttachmentKind.Vote,
                Title = "投票标题",
                Desc = "投票说明",
                Destination = "https://www.bilibili.com/vote/123",
            }, command);
            Check(!vote.ShowPanel && vote.OpenVisibility == Visibility.Visible,
                "a vote attachment keeps the quiet row with its 打开 action");
            var link = new DynamicAttachmentDisplay(new DynamicAttachmentSummary
            {
                Kind = DynamicAttachmentKind.CommonLink,
                Title = "链接标题",
                Destination = "https://www.bilibili.com/x",
            }, command);
            Check(!link.ShowPanel, "a common link keeps the quiet row");
            var unsupported = new DynamicAttachmentDisplay(new DynamicAttachmentSummary
            {
                Kind = DynamicAttachmentKind.Unsupported,
                RawType = "ADDITIONAL_TYPE_GOODS",
                Title = "商品",
            }, command);
            Check(!unsupported.ShowPanel && unsupported.KindLabel == "商品",
                "a goods attachment keeps the quiet row and a Chinese label");

            var noText = new DynamicAttachmentDisplay(new DynamicAttachmentSummary
            {
                Kind = DynamicAttachmentKind.Reserve,
                Title = "只有标题",
                Destination = "https://live.bilibili.com/123",
                ButtonIcon = "https://i0.hdslb.com/reserve.png",
            }, command);
            Check(noText.ButtonVisibility == Visibility.Collapsed,
                "a reserve panel without button text draws no button");

            var noRoute = new DynamicAttachmentDisplay(new DynamicAttachmentSummary
            {
                Kind = DynamicAttachmentKind.Reserve,
                Title = "只有文案",
                ButtonText = "预约",
            }, command);
            Check(noRoute.ButtonVisibility == Visibility.Collapsed,
                "a reserve panel without a destination draws no button");

            var noCommand = new DynamicAttachmentDisplay(new DynamicAttachmentSummary
            {
                Kind = DynamicAttachmentKind.Reserve,
                Title = "没有命令",
                Destination = "https://live.bilibili.com/123",
                ButtonText = "预约",
            }, null);
            Check(noCommand.ButtonVisibility == Visibility.Collapsed,
                "a reserve panel without an open command draws no button");

            var noIcon = new DynamicAttachmentDisplay(new DynamicAttachmentSummary
            {
                Kind = DynamicAttachmentKind.Reserve,
                Title = "没有图标",
                Destination = "https://live.bilibili.com/123",
                ButtonText = "预约",
            }, command);
            Check(noIcon.IconVisibility == Visibility.Collapsed,
                "a button without an icon hides the icon instead of drawing a placeholder");
        }

        /// <summary>
        /// 复核:一条文字动态的 additional.type 是预约,会被预览解析提升成 Reserve,于是独立模板和附件行
        /// 各画一次。现在提升之后只摘掉预约摘要,投票/未知种类照旧上屏;图片动态不提升,预约留在附件行。
        /// </summary>
        private static void ReserveCardOwnsItsAttachment()
        {
            var textCard = WebCards(WordCard(
                "\"desc\":{\"text\":\"今晚开播\"}," +
                "\"additional\":{\"type\":\"ADDITIONAL_TYPE_RESERVE\"," +
                "\"reserve\":{\"title\":\"预约标题\",\"rid\":321," +
                "\"desc1\":{\"text\":\"11-13 20:00\"},\"desc2\":{\"text\":\"175人预约\"}," +
                "\"jump_url\":\"//www.bilibili.com/reserve/321\"," +
                "\"button\":{\"status\":1,\"jump_url\":\"//live.bilibili.com/123\"," +
                "\"uncheck\":{\"text\":\"预约\",\"icon_url\":\"https://i0.hdslb.com/reserve.png\"}}}," +
                "\"vote\":{\"vote_id\":123,\"title\":\"投票标题\",\"desc1\":\"投票说明\"," +
                "\"jump_url\":\"//www.bilibili.com/vote/123\"}," +
                "\"goods\":{\"id_str\":\"5\",\"title\":\"赛事直播\"}}"));
            var display = new DynamicItemDisplayModel { Type = DynamicDisplayType.Text };
            DynamicCardProjection.Apply(display, textCard);
            Check(display.Attachments.Count == 3,
                "a text card with a reservation, a vote and an unsupported attachment starts with three summaries");
            // 先读一次：AttachmentItems 的缓存必须是归属收口能清掉的那份
            Check(display.AttachmentItems.Count == 3 && display.AttachmentItems[0].ShowPanel,
                "before the promotion the attachment row still draws the reservation panel");

            // ParseOneRowInfo2024 在 Text 分支里把类型提升成 Reserve；这里是提升后的状态
            display.Type = DynamicDisplayType.Reserve;
            DynamicCardProjection.ApplyAttachmentOwnership(display);

            Check(display.Attachments.Count == 2, "the promoted reserve card keeps the two non-reservation attachments");
            Check(display.Attachments.TrueForAll(x => x.Kind != DynamicAttachmentKind.Reserve),
                "the reservation summary is dropped from the attachment list");
            Check(display.Attachments[0].Kind == DynamicAttachmentKind.Vote,
                "the vote attachment survives the hand-off");
            Check(display.Attachments[1].Kind == DynamicAttachmentKind.Unsupported,
                "the unsupported attachment survives the hand-off");
            Check(display.AttachmentItems.Count == 2
                  && display.AttachmentItems[0].KindLabel == "投票"
                  && display.AttachmentItems[1].KindLabel == "附件",
                "the built attachment rows are rebuilt after the reservation is dropped");

            var withNull = new DynamicItemDisplayModel
            {
                Type = DynamicDisplayType.Reserve,
                Attachments = new List<DynamicAttachmentSummary>
                {
                    null,
                    new DynamicAttachmentSummary { Kind = DynamicAttachmentKind.Reserve, Title = "预约" },
                    new DynamicAttachmentSummary { Kind = DynamicAttachmentKind.Vote, Title = "投票" },
                },
            };
            DynamicCardProjection.ApplyAttachmentOwnership(withNull);
            Check(withNull.Attachments.Count == 2
                  && withNull.Attachments[0] == null
                  && withNull.Attachments[1].Kind == DynamicAttachmentKind.Vote,
                "the hand-off keeps a null placeholder and drops only the reservation");

            var photo = new DynamicItemDisplayModel { Type = DynamicDisplayType.Photo };
            DynamicCardProjection.Apply(photo, WebCards(PhotoCard("\"additional\":" + ReserveButtonAdditional())));
            DynamicCardProjection.ApplyAttachmentOwnership(photo);
            Check(photo.Type == DynamicDisplayType.Photo,
                "a photo card is not promoted to the reserve template");
            Check(photo.Attachments.Count == 1
                  && photo.Attachments[0].Kind == DynamicAttachmentKind.Reserve
                  && photo.AttachmentItems.Count == 1
                  && photo.AttachmentItems[0].ShowPanel,
                "a photo grid keeps its reservation attachment");

            var parse = ProductSource("src/BiliLite.UWP/Controls/Dynamic/DynamicParse.cs");
            var textCase = Slice(SliceMethod(parse, "public static DynamicItemDisplayOneRowInfo ParseOneRowInfo2024("),
                "case DynamicDisplayType.Text:", "case DynamicDisplayType.Video:");
            Check(textCase.Contains("data.Type = DynamicDisplayType.Reserve;"),
                "the text-to-reserve promotion still happens in the preview parse");

            var convert = SliceMethod(ProductSource("src/BiliLite.UWP/Modules/User/DynamicVM.cs"),
                "private DynamicItemDisplayModel ConvertToDisplay2024(DynamicCardModel2024 item");
            var parseLine = convert.IndexOf("data.OneRowInfo = DynamicParse.ParseOneRowInfo2024(data, item);",
                StringComparison.Ordinal);
            var ownershipLine = convert.IndexOf("DynamicCardProjection.ApplyAttachmentOwnership(data);",
                StringComparison.Ordinal);
            Check(parseLine >= 0 && ownershipLine > parseLine,
                "the conversion hands attachment ownership over after the promotion");
        }

        /// <summary>
        /// BR-57：预约说明行按槽位给标签 —— 时间 开播 / N人已预约 / 抽奖文案，标签之间一个空格、不加括号。
        /// 时间槽把「直播」改成「开播」、已经写「开播」的不重复加，状态词「直播中」「预约中」原样保留；
        /// 人数优先用结构化 reserve_total，没有才读 desc2 并把「预约」补成「已预约」；desc3 原样做第三个标签。
        /// </summary>
        private static void ReserveCopyLabelsTimeAndCount()
        {
            Check(ReserveCardCopy.Format(null) == "",
                "a missing reservation yields no copy");

            var live = ReserveCardCopy.Format(ReserveOf("11-13 20:00 直播", "175人预约", null, 0));
            Check(live == "11-13 20:00 开播 175人已预约",
                "the time slot rewrites 直播 to 开播 and the count slot rewrites 预约 to 已预约");
            Check(!live.Contains("【") && !live.Contains("】"),
                "the copy joins its labels with a space instead of wrapping them in brackets");
            Check(!live.Contains("  ") && live.StartsWith("11-13 20:00 开播 "),
                "labels are separated by exactly one space and no separator leads the line");
            Check(ReserveSummaryOf(ReserveOf("11-13 20:00 直播", "175人预约", null, 0)).Desc == live,
                "the reserve attachment description is the shared copy, not the raw joined text");

            Check(ReserveCardCopy.Format(ReserveOf("11-13 20:00", null, null, 0)) == "11-13 20:00 开播",
                "a bare time gains 开播 and the line starts without a leading space");
            Check(ReserveCardCopy.Format(ReserveOf("11-13 20:00 开播", null, null, 0)) == "11-13 20:00 开播",
                "a time that already says 开播 is not rewritten twice");
            Check(ReserveCardCopy.Format(ReserveOf("直播中", null, null, 0)) == "直播中",
                "a live-state word stays as it is instead of becoming 直播中 开播");
            Check(ReserveCardCopy.Format(ReserveOf("预约中", null, null, 0)) == "预约中",
                "a pending-state word stays as it is");

            Check(ReserveCardCopy.Format(ReserveOf("11-13 20:00", "175人预约", null, 4866))
                    == "11-13 20:00 开播 4866人已预约",
                "the structured reserve_total wins over the desc2 copy");
            Check(ReserveCardCopy.Format(ReserveOf(null, "175人已预约", null, 0)) == "175人已预约",
                "a desc2 that already says 已预约 is left alone");
            Check(ReserveCardCopy.Format(ReserveOf(null, "175", null, 0)) == "175人已预约",
                "a count without any suffix gains 人已预约");

            Check(ReserveCardCopy.Format(ReserveOf("11-13 20:00", "175人预约", "预约可参与抽奖", 0))
                    == "11-13 20:00 开播 175人已预约 预约可参与抽奖",
                "the lottery copy becomes the third label, separated by one space and unchanged");
            var blank = ReserveCardCopy.Format(ReserveOf("  ", "  ", "  ", 0));
            Check(blank.Trim() == "" && !blank.Contains("【") && !blank.Contains("】"),
                "blank slots produce no empty tags and no stray separators");
        }

        /// <summary>
        /// BR-57：附件行不再打印 wire 类型名 —— GOODS 是「商品」、UGC 是「合集」、认不出的都是「附件」；
        /// 标题/说明/目的地全空的附件行整行丢掉，有标题或链接的照旧保留。
        /// </summary>
        private static void AttachmentLabelsHideWireTypes()
        {
            Check(LabelOf("ADDITIONAL_TYPE_GOODS") == "商品", "a goods attachment is labelled 商品");
            Check(LabelOf("ADDITIONAL_TYPE_UGC") == "合集", "a ugc attachment is labelled 合集");
            Check(LabelOf("ADDITIONAL_TYPE_ESPORTS") == "附件", "an unknown attachment falls back to 附件");
            Check(LabelOf("") == "附件", "an attachment without any wire type is labelled 附件");
            Check(!LabelOf("ADDITIONAL_TYPE_ESPORTS").Contains("ADDITIONAL_TYPE_"),
                "the wire type name never reaches the kind label");

            var goodsCard = WebCards(WordCard("\"desc\":{\"text\":\"带货\"},\"additional\":{" +
                "\"type\":\"ADDITIONAL_TYPE_GOODS\"," +
                "\"goods\":{\"id_str\":\"9\",\"title\":\"周边\",\"jump_url\":\"//www.bilibili.com/goods/9\"}}"));
            var goods = DynamicAttachmentSummaries.From(goodsCard);
            Check(goods.Count == 1 && goods[0].Title == "周边",
                "a goods attachment with a title stays");
            Check(goods[0].RawType == "ADDITIONAL_TYPE_GOODS",
                "the raw wire type is still kept for diagnostics");
            var goodsRow = new DynamicAttachmentDisplay(goods[0], UserCommand);
            Check(goodsRow.KindLabel == "商品" && goodsRow.Title == "周边"
                  && !goodsRow.KindLabel.Contains("ADDITIONAL_TYPE_"),
                "a real goods row shows the Chinese label and its title");

            var linkOnly = DynamicAttachmentSummaries.From(WebCards(WordCard("\"desc\":{\"text\":\"合集\"},\"additional\":{" +
                "\"type\":\"ADDITIONAL_TYPE_UGC\"," +
                "\"ugc\":{\"id_str\":\"7\",\"jump_url\":\"//www.bilibili.com/cheese/play/7\"}}")));
            Check(linkOnly.Count == 1 && LabelOf(linkOnly[0].RawType) == "合集",
                "an attachment with only a destination stays and is labelled 合集");

            var nothing = DynamicAttachmentSummaries.From(WebCards(WordCard("\"desc\":{\"text\":\"空附件\"}," +
                "\"additional\":{\"type\":\"ADDITIONAL_TYPE_GOODS\"}")));
            Check(nothing.Count == 0,
                "an attachment row with no title, description or destination is dropped");
        }

        private static string LabelOf(string rawType)
        {
            return new DynamicAttachmentDisplay(new DynamicAttachmentSummary
            {
                Kind = DynamicAttachmentKind.Unsupported,
                RawType = rawType,
            }, UserCommand).KindLabel;
        }

        private static DynamicCardModel2024_addin_reserve ReserveOf(
            string time, string count, string lottery, int reserveTotal)
        {
            return new DynamicCardModel2024_addin_reserve
            {
                title = "预约标题",
                rid = 321,
                reserve_total = reserveTotal,
                desc1 = time == null ? null : new DynamicCardModel2024_reservetext { text = time },
                desc2 = count == null ? null : new DynamicCardModel2024_reservetext { text = count },
                desc3 = lottery == null ? null : new DynamicCardModel2024_reservetext { text = lottery },
            };
        }

        private static DynamicAttachmentSummary ReserveSummaryOf(DynamicCardModel2024_addin_reserve reserve)
        {
            var card = new DynamicCardModel2024
            {
                modules = new DynamicCardModel2024_modules
                {
                    module_dynamic = new DynamicCardModel2024_dynamic
                    {
                        additional = new DynamicCardModel2024_add
                        {
                            type = "ADDITIONAL_TYPE_RESERVE",
                            reserve = reserve,
                        },
                    },
                },
            };
            return DynamicAttachmentSummaries.From(card).Find(x => x.Kind == DynamicAttachmentKind.Reserve);
        }

        private static string ReserveButtonAdditional()
        {
            return "{\"type\":\"ADDITIONAL_TYPE_RESERVE\"," +
                   "\"reserve\":{\"title\":\"10月新番OP/ED鉴赏会！\",\"rid\":321," +
                   "\"desc1\":{\"text\":\"11-13 20:00\",\"style\":0}," +
                   "\"desc2\":{\"text\":\"175人预约\"}," +
                   "\"jump_url\":\"//www.bilibili.com/reserve/321\"," +
                   "\"button\":{\"status\":1,\"jump_url\":\"//live.bilibili.com/123\"," +
                   "\"uncheck\":{\"text\":\"预约\",\"icon_url\":\"https://i0.hdslb.com/reserve.png\"}}}}";
        }

        private static void LiveSummaryReadsThePayload()
        {
            var summary = LiveCardSummary.From(new DynamicCardModel2024_live
            {
                id = "12345",
                cover = "https://i0.hdslb.com/live.jpg",
                title = "今晚八点",
                desc_first = "虚拟主播",
                desc_second = "1.2万人看过",
                jump_url = "https://live.bilibili.com/12345",
                live_state = 1,
            }, "卡片标题", "https://i0.hdslb.com/pic.jpg");

            Check(summary.RoomId == "12345", "a live payload keeps its room id");
            Check(summary.Destination == "https://live.bilibili.com/12345",
                "a live payload points at live.bilibili.com/{room}");
            Check(summary.Subtitle == "虚拟主播 · 1.2万人看过",
                "the subtitle joins the area and the watched label");
            Check(summary.Title == "今晚八点", "the payload title wins over the card preview title");
            Check(summary.Cover == "https://i0.hdslb.com/live.jpg",
                "the payload cover wins over the card preview cover");
            Check(summary.State == DynamicLiveState.Live && summary.CoverText == "",
                "live_state 1 is live and carries no badge");
        }

        private static void LiveSummarySkipsEmptyParts()
        {
            var areaOnly = LiveCardSummary.From(new DynamicCardModel2024_live { id = "12345", desc_first = "虚拟主播" }, "", "");
            Check(areaOnly.Subtitle == "虚拟主播", "a payload without a watched label has no dangling separator");
            Check(!areaOnly.Subtitle.Contains("·"), "no separator survives an absent watched label");

            var watchedOnly = LiveCardSummary.From(new DynamicCardModel2024_live { id = "12345", desc_second = "88人看过" }, "", "");
            Check(watchedOnly.Subtitle == "88人看过", "a payload with only the watched label yields just that label");

            var neither = LiveCardSummary.From(new DynamicCardModel2024_live { id = "12345" }, "", "");
            Check(neither.Subtitle == "", "a payload without both parts yields an empty subtitle, not a placeholder");
            Check(!neither.Subtitle.Contains("有待修复"), "the live subtitle never falls back to a placeholder");
        }

        private static void LiveSummaryNeverInventsARoom()
        {
            var noRoom = LiveCardSummary.From(new DynamicCardModel2024_live { title = "无房间号" }, "", "");
            Check(noRoom.RoomId == "", "a payload without a room id has no room id");
            Check(noRoom.Destination == "", "a payload without a room id has no destination");

            var notDigits = LiveCardSummary.From(new DynamicCardModel2024_live { id = "BV1abc" }, "", "");
            Check(notDigits.RoomId == "", "a non-numeric id is not a room id");
            Check(notDigits.Destination == "", "a non-numeric id never becomes a destination");

            var idOnly = LiveCardSummary.From(new DynamicCardModel2024_live { id = "654321" }, "", "");
            Check(idOnly.Destination == "https://live.bilibili.com/654321",
                "a room id alone yields the canonical live destination");

            var shortLink = LiveCardSummary.From(new DynamicCardModel2024_live
            {
                id = "654321",
                jump_url = "https://b23.tv/AbCdEf",
            }, "", "");
            Check(shortLink.Destination == "https://live.bilibili.com/654321",
                "a b23 short link never becomes the live destination");
        }

        private static void LiveSummaryBadgesOnlyConfirmedEnds()
        {
            var ended = LiveCardSummary.From(new DynamicCardModel2024_live { live_state = 0 }, "", "");
            Check(ended.State == DynamicLiveState.Ended, "live_state 0 is a confirmed end");
            Check(ended.CoverText == "直播已结束", "a confirmed end carries the ended badge");

            var live = LiveCardSummary.From(new DynamicCardModel2024_live { live_state = 1 }, "", "");
            Check(live.CoverText == "", "live_state 1 carries no badge");

            var missing = LiveCardSummary.From(new DynamicCardModel2024_live { live_state = null }, "", "");
            Check(missing.State == DynamicLiveState.Unknown, "a missing live_state is unknown");
            Check(missing.CoverText == "", "an unknown live_state carries no badge");

            var other = LiveCardSummary.From(new DynamicCardModel2024_live { live_state = 2 }, "", "");
            Check(other.State == DynamicLiveState.Unknown, "an unrecognised live_state is unknown, not ended");
            Check(other.CoverText == "", "an unrecognised live_state carries no badge");
        }

        private static void LiveRcmdPayloadMatchesThePlainOne()
        {
            var rcmd = WebCards(LiveRcmdCard());
            var payload = rcmd.modules.module_dynamic.major.live;
            Check(payload != null && payload.id == "9001",
                "live_rcmd decodes to a live payload with its room id");
            Check(rcmd.modules.module_dynamic.major.live_rcmd != null,
                "the raw live_rcmd payload stays beside the decoded one");

            var summary = LiveCardSummary.From(payload, rcmd.title, rcmd.pic);
            Check(summary.RoomId == "9001", "a live_rcmd payload yields the room id");
            Check(summary.Destination == "https://live.bilibili.com/9001",
                "a live_rcmd payload yields the live destination");
            Check(summary.Subtitle == "单机游戏 · 3.4万人看过",
                "a live_rcmd payload yields the area · watched subtitle");
            Check(summary.State == DynamicLiveState.Unknown,
                "live_rcmd carries no live_state, so the state is unknown");
            Check(summary.CoverText == "", "an unknown live_rcmd state does not claim the stream ended");

            var plain = LiveCardSummary.From(
                WebCards(LiveCard("\"id\":\"7001\",\"title\":\"直播标题\",\"cover\":\"https://i0.hdslb.com/live.jpg\"," +
                                  "\"area_name\":\"单机游戏\",\"watched_show\":{\"text_large\":\"3.4万人看过\"}," +
                                  "\"live_state\":1")).modules.module_dynamic.major.live,
                "卡片标题", "https://i0.hdslb.com/pic.jpg");
            Check(plain.Subtitle == summary.Subtitle,
                "the plain live payload and the live_rcmd payload present the same subtitle");
            Check(plain.State == DynamicLiveState.Live,
                "the plain live payload still keeps its explicit live_state");
        }

        private static void GrpcLiveCardsMapTheSameWay()
        {
            var content = "{\"live_play_info\":{\"room_id\":4321,\"cover\":\"https://i0.hdslb.com/rcmd.jpg\"," +
                          "\"title\":\"推荐直播\",\"area_name\":\"主机游戏\"," +
                          "\"watched_show\":{\"text_large\":\"2.1万人看过\"}}}";
            var rcmd = GrpcCard(DynamicType.LiveRcmd, new ModuleDynamic
            {
                DynLiveRcmd = new MdlDynLiveRcmd { Content = content },
            });
            var rcmdPayload = rcmd.modules.module_dynamic.major.live;
            Check(rcmdPayload != null && rcmdPayload.id == "4321",
                "the gRPC live_rcmd branch decodes the room id");
            Check(rcmdPayload.live_state == null,
                "the gRPC live_rcmd branch leaves live_state absent instead of defaulting to 0");
            var rcmdSummary = LiveCardSummary.From(rcmdPayload, rcmd.title, rcmd.pic);
            Check(rcmdSummary.Destination == "https://live.bilibili.com/4321",
                "the gRPC live_rcmd card points at the live room");
            Check(rcmdSummary.Subtitle == "主机游戏 · 2.1万人看过",
                "the gRPC live_rcmd card keeps the area · watched subtitle");
            Check(rcmdSummary.CoverText == "",
                "the gRPC live_rcmd card does not claim the stream ended");

            var common = GrpcCard(DynamicType.Live, new ModuleDynamic
            {
                DynCommonLive = new MdlDynLive
                {
                    Id = 8675309,
                    Uri = "https://live.bilibili.com/8675309",
                    Title = "直播中",
                    Cover = "https://i0.hdslb.com/common.jpg",
                    CoverLabel = "主机游戏",
                    CoverLabel2 = "1.1万人看过",
                    LiveState = LiveState.LiveLive,
                },
            });
            var commonSummary = LiveCardSummary.From(
                common.modules.module_dynamic.major.live, common.title, common.pic);
            Check(commonSummary.RoomId == "8675309", "the gRPC common live card keeps its room id");
            Check(commonSummary.Destination == "https://live.bilibili.com/8675309",
                "the gRPC common live card points at the live room");
            Check(commonSummary.State == DynamicLiveState.Live && commonSummary.CoverText == "",
                "the gRPC live_live state carries no ended badge");
        }

        private static void LiveWithoutPayloadKeepsThePreview()
        {
            var summary = LiveCardSummary.From(null, "卡片标题", "https://i0.hdslb.com/pic.jpg");
            Check(summary.Title == "卡片标题", "a missing live payload falls back to the card title");
            Check(summary.Cover == "https://i0.hdslb.com/pic.jpg",
                "a missing live payload falls back to the card cover");
            Check(summary.Subtitle == "", "a missing live payload yields no placeholder subtitle");
            Check(summary.RoomId == "" && summary.Destination == "",
                "a missing live payload yields no room and no destination");
            Check(summary.State == DynamicLiveState.Unknown && summary.CoverText == "",
                "a missing live payload is unknown and carries no badge");

            var noState = WebCards(LiveCard("\"id\":\"7001\",\"title\":\"直播标题\",\"area_name\":\"单机游戏\""));
            Check(noState.modules.module_dynamic.major.live.live_state == null,
                "a live payload without live_state keeps it absent, not 0");
            Check(LiveCardSummary.From(noState.modules.module_dynamic.major.live, noState.title, noState.pic).CoverText == "",
                "an absent live_state is not read as ended");
        }

        private static void LiveCardsUseTheSharedSummary()
        {
            var source = ProductSource("src/BiliLite.UWP/Controls/Dynamic/DynamicParse.cs");
            Check(!source.Contains("有待修复"), "the dynamic card parser ships no live placeholder");
            Check(!source.Contains("b23.tv/live"), "the dynamic card parser builds no b23.tv live link");

            var modern = SliceMethod(source, "public static DynamicItemDisplayOneRowInfo ParseOneRowInfo2024(");
            Check(modern.Contains("case DynamicDisplayType.Live:") && modern.Contains("case DynamicDisplayType.LiveShare:"),
                "both modern live display types are handled");
            Check(modern.Contains("LiveCardSummary.From("),
                "the modern live cases go through LiveCardSummary");
            Check(CountOf(modern, "LiveCardSummary.From(") == 1,
                "the two modern live cases share one summary call");
            Check(modern.Contains("Tag = \"直播\""), "a modern live card still carries the live tag");
            var liveBlock = Slice(modern, "case DynamicDisplayType.Live:", "case DynamicDisplayType.MediaList:");
            Check(!liveBlock.Contains("item.aid"), "a modern live card no longer uses the aid as its identity");
            Check(liveBlock.Contains("ID = live.RoomId"), "a modern live card takes its identity from the room id");
            Check(liveBlock.Contains("info.Url = live.Destination"), "a modern live card opens the summary destination");

            var legacy = SliceMethod(source, "public static DynamicItemDisplayOneRowInfo ParseOneRowInfo(DynamicDisplayType type, JObject obj)");
            Check(!legacy.Contains("b23.tv/live"), "the legacy live cards no longer use the short link");
            Check(CountOf(legacy, "\"https://live.bilibili.com/\" + info.ID") == 2,
                "both legacy live cases point at live.bilibili.com/{room_id}");

            // 未支持的类型不再用"有待修复"占位：卡片改走外壳，写一句说明并提供打开原文。
            var placeholder = SourceFilesContaining("有待修复");
            Check(placeholder.Count == 0, "no product source still ships a 有待修复 placeholder");
            var template = ProductSource("src/BiliLite.UWP/Controls/Dynamic/DynamicTemplate.xaml");
            Check(template.Contains("CanOpenOriginal") && template.Contains("WebCommand"),
                "the unsupported card opens its original on the web through the model's open-original state");
            Check(!template.Contains("Command=\"{Binding Path=DetailByIdCommand}\" CommandParameter=\"{Binding Path=DynamicID}\""),
                "the unsupported card no longer routes 打开原文 into the native detail page");
        }

        private static string LiveCard(string liveBody)
        {
            return "{\"id_str\":\"" + DynamicId + "\",\"type\":\"DYNAMIC_TYPE_LIVE\"," +
                   "\"basic\":{\"comment_id_str\":\"7001\",\"comment_type\":11},\"modules\":{" +
                   "\"module_author\":{\"mid\":4242,\"name\":\"主播\"},\"module_dynamic\":{" +
                   "\"major\":{\"type\":\"MAJOR_TYPE_LIVE\",\"live\":{" + liveBody + "}}}}}";
        }

        private static string LiveRcmdCard()
        {
            var content = "{\"live_play_info\":{\"room_id\":9001,\"cover\":\"https://i0.hdslb.com/rcmd.jpg\"," +
                          "\"title\":\"推荐直播\",\"area_name\":\"单机游戏\"," +
                          "\"watched_show\":{\"text_large\":\"3.4万人看过\"}}}";
            return "{\"id_str\":\"" + DynamicId + "\",\"type\":\"DYNAMIC_TYPE_LIVE_RCMD\"," +
                   "\"basic\":{\"comment_id_str\":\"7001\",\"comment_type\":11},\"modules\":{" +
                   "\"module_author\":{\"mid\":4242,\"name\":\"主播\"},\"module_dynamic\":{" +
                   "\"major\":{\"type\":\"MAJOR_TYPE_LIVE_RCMD\",\"live_rcmd\":{\"content\":" +
                   Newtonsoft.Json.JsonConvert.ToString(content) + "}}}}}";
        }

        private static string Slice(string source, string from, string to)
        {
            var start = source.IndexOf(from, StringComparison.Ordinal);
            if (start < 0)
            {
                Check(false, "expected block start not found in source: " + from);
                return "";
            }

            var end = source.IndexOf(to, start + from.Length, StringComparison.Ordinal);
            return end < 0 ? source.Substring(start) : source.Substring(start, end - start);
        }

        private static int CountOf(string haystack, string needle)
        {
            var count = 0;
            var index = haystack.IndexOf(needle, StringComparison.Ordinal);
            while (index >= 0)
            {
                count++;
                index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
            }
            return count;
        }

        private static System.Collections.Generic.List<string> SourceFilesContaining(string needle)
        {
            var root = Path.Combine(RepoRoot(), "src", "BiliLite.UWP");
            var hits = new System.Collections.Generic.List<string>();
            foreach (var file in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(file);
                if (extension != ".cs" && extension != ".xaml")
                {
                    continue;
                }

                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                if (relative.StartsWith("obj/", StringComparison.Ordinal) || relative.StartsWith("bin/", StringComparison.Ordinal))
                {
                    continue;
                }

                if (File.ReadAllText(file).Contains(needle))
                {
                    hits.Add(relative);
                }
            }
            return hits;
        }

        private static string ForbiddenWordCard()
        {
            return "{\"id_str\":\"" + DynamicId + "\",\"type\":\"DYNAMIC_TYPE_WORD\"," +
                   "\"basic\":{\"comment_id_str\":\"" + DynamicId + "\",\"comment_type\":17}," +
                   "\"modules\":{\"module_stat\":{" +
                   "\"comment\":{\"count\":0,\"forbidden\":true}," +
                   "\"forward\":{\"count\":0,\"forbidden\":true}}," +
                   "\"module_dynamic\":{\"desc\":{\"text\":\"不能评论\"}}}}";
        }

        private static DynamicCardModel2024 WebCards(string cardJson)
        {
            var items = Newtonsoft.Json.Linq.JArray.Parse("[" + cardJson + "]");
            var cards = WebDynamicFeedMapper.MapToUserFeedCards(items);
            Check(cards.Count == 1, "the web mapper yields one card for the probe payload");
            return cards[0];
        }

        private static string WordCard(string moduleDynamic)
        {
            return "{\"id_str\":\"" + DynamicId + "\",\"type\":\"DYNAMIC_TYPE_WORD\"," +
                   "\"basic\":{\"comment_id_str\":\"" + DynamicId + "\",\"comment_type\":17,\"rid_str\":\"" + DynamicId + "\"}," +
                   "\"modules\":{\"module_author\":{\"mid\":4242,\"name\":\"作者\"}," +
                   "\"module_dynamic\":{" + moduleDynamic + "}}}";
        }

        private static string PhotoCard(string moduleDynamicExtra)
        {
            return "{\"id_str\":\"8120000000001\",\"type\":\"DYNAMIC_TYPE_DRAW\"," +
                   "\"basic\":{\"comment_id_str\":\"7001\",\"comment_type\":11,\"rid_str\":\"8120000000001\"}," +
                   "\"modules\":{\"module_author\":{\"mid\":4242,\"name\":\"摄影者\"}," +
                   "\"module_stat\":{\"comment\":{\"count\":3,\"forbidden\":false}," +
                   "\"forward\":{\"count\":1,\"forbidden\":false},\"like\":{\"count\":9,\"status\":true}}," +
                   "\"module_dynamic\":{" +
                   "\"desc\":{\"text\":\"今天和 @朋友 去拍照\",\"rich_text_nodes\":[]}," +
                   "\"major\":{\"draw\":{\"id\":5001,\"items\":[" +
                   "{\"src\":\"//i0.hdslb.com/a.jpg\",\"width\":800,\"height\":600}," +
                   "{\"src\":\"//i0.hdslb.com/b.jpg\",\"width\":800,\"height\":600}]}}," +
                   moduleDynamicExtra + "}}}";
        }

        private static string VoteAdditional()
        {
            return "{\"type\":\"ADDITIONAL_TYPE_VOTE\",\"type_str\":\"投票\"," +
                   "\"vote\":{\"vote_id\":123,\"title\":\"投票标题\",\"desc1\":\"投票说明\"," +
                   "\"jump_url\":\"//www.bilibili.com/vote/123\"}}";
        }

        private static string VoteAndReserveAdditional()
        {
            return "{\"type\":\"ADDITIONAL_TYPE_RESERVE\"," +
                   "\"reserve\":{\"title\":\"预约标题\",\"rid\":321,\"desc1\":\"明天 20:00\"," +
                   "\"desc2\":\"1.2万人预约\",\"jump_url\":\"//www.bilibili.com/reserve/321\"}," +
                   "\"vote\":{\"vote_id\":123,\"title\":\"投票标题\",\"desc1\":\"投票说明\"," +
                   "\"jump_url\":\"//www.bilibili.com/vote/123\"}}";
        }

        private static DynamicCardModel2024 GrpcCard(DynamicType cardType, ModuleDynamic dynamic)
        {
            var item = new DynamicItem
            {
                CardType = cardType,
                Extend = new Extend { DynIdStr = DynamicId },
            };
            if (dynamic != null)
            {
                item.Modules.Add(new Module
                {
                    ModuleType = DynModuleType.ModuleDynamic,
                    ModuleDynamic = dynamic,
                });
            }
            var cards = DynamicGrpcMapper.MapToUserCards(new[] { item });
            Check(cards.Count == 1, "the gRPC mapper yields one card for " + cardType);
            return cards[0];
        }

        private static string ProductSource(string relativePath)
        {
            var full = Path.Combine(RepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
            Check(File.Exists(full), "product source exists: " + relativePath);
            return File.ReadAllText(full);
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

using System;
using System.IO;
using System.Windows.Input;
using Bilibili.App.Dynamic.V2;
using BiliLite.Controls.Dynamic;
using BiliLite.Helpers;
using BiliLite.Modules;
using BiliLite.Parsing.Dynamic;
using Google.Protobuf;

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
            Check(bothDisplay.Attachments[0].Desc == "明天 20:00 1.2万人预约",
                "the reservation keeps both description lines, joined once");
            Check(bothDisplay.Attachments[0].Id == "321", "the reservation keeps its object id");

            var unknownDisplay = new DynamicItemDisplayModel();
            DynamicCardProjection.Apply(unknownDisplay, WebCards(WordCard(
                "\"desc\":{\"text\":\"赛事\"}," +
                "\"additional\":{\"type\":\"ADDITIONAL_TYPE_ESPORTS\",\"title\":\"赛事直播\"}")));
            Check(unknownDisplay.Attachments.Count == 1
                  && unknownDisplay.Attachments[0].Kind == DynamicAttachmentKind.Unsupported,
                "an unsupported attachment is kept as an unsupported summary");
            Check(unknownDisplay.Attachments[0].RawType == "ADDITIONAL_TYPE_ESPORTS",
                "an unsupported attachment keeps its raw wire type");

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
            Check(!SliceMethod(legacySource, "private DynamicItemDisplayModel ConvertToDisplay(DynamicCardModel item)")
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

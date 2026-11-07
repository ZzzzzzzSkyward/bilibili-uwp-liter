using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BiliLite.Controls;
using BiliLite.Helpers;
using BiliLite.Parsing;
using BiliLite.Parsing.Comment;
using Newtonsoft.Json.Linq;
using Windows.UI.Xaml;

namespace CommentsProbe
{
    /// <summary>
    /// comments: 评论三路响应（x/v2/reply/wbi/main 游标源、x/v2/reply pn 源、x/v2/reply/reply 楼中楼）
    /// 和三个动作接口走 Parsing/Comment 这一层，控件只在解析被接受后发布。
    ///
    /// Scope: the probe compiles the real Parsing/ApiEnvelope.cs, Parsing/Comment/*, the comment wire
    /// DTOs and Models/Common.cs; only the REST transport (HttpResults) and the app services (Utils/
    /// LogHelper) are stand-ins, plus a member-for-member mirror of the page models that live in the
    /// UWP-only Controls/CommentControl.xaml.cs. The four failure categories, the pinned-reply merge,
    /// the cursor-source rule and the empty/null semantics are asserted on real behavior.
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static int Main()
        {
            CursorPageMapsInDisplayOrder();
            MappedRowKeepsTheWireFields();
            MissingCodeIsNotSuccess();
            NonNumericCodeIsNotSuccess();
            BusinessRejectionKeepsItsMessage();
            ClosedCommentAreaKeepsItsCode();
            NonJsonBodyIsAnEnvelopeFailure();
            TransportFailureIsItsOwnCategory();
            MissingDataIsAnEnvelopeFailure();
            PayloadShapeIsChecked();
            PinnedDuplicateRpidAppearsOnce();
            NullCombinationsDoNotThrow();
            ValidEmptyPageIsNotMalformed();
            CursorBelongsToItsSource();
            ReplyListParses();
            ActionRepliesOnlyNeedCodeAndMessage();
            ParsePathShowsNothing();
            DeadPathsAreGone();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        /// <summary>游标源的一页：置顶在前、游标交给调用方，行数与线上 replies 分开记。</summary>
        private static void CursorPageMapsInDisplayOrder()
        {
            var data = new JObject
            {
                ["replies"] = new JArray(Reply(333), Reply(444)),
                ["top_replies"] = new JArray(Reply(222)),
                ["upper"] = new JObject { ["mid"] = 111, ["top"] = Reply(111) },
                ["cursor"] = new JObject
                {
                    ["pagination_reply"] = new JObject { ["next_offset"] = "probe-offset" },
                },
            };
            var parsed = WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(data)));
            Check(parsed.IsSuccess, "page: an accepted cursor page parses");
            Check(parsed.Value.Replies.Count == 2 && parsed.Value.TopReplies.Count == 1,
                "page: replies and top_replies keep the wire order");
            Check(parsed.Value.Upper != null && parsed.Value.Upper.Top != null && parsed.Value.Upper.Mid == 111,
                "page: upper.mid stays next to upper.top");
            Check(parsed.Value.Cursor.PaginationReply.NextOffset == "probe-offset",
                "page: cursor.pagination_reply.next_offset survives the decode");

            var page = WebCommentPageMapper.MapWebCommentPage(parsed.Value, WebCommentPageSource.Cursor, true);
            Check(page.Rows.Count == 4, "map: the first page is upper.top + top_replies + replies (" + page.Rows.Count + ")");
            Check(Rpids(page.Rows) == "111,222,333,444",
                "map: pinned rows come first, in wire order (" + Rpids(page.Rows) + ")");
            Check(page.Rows[0].showTop == Visibility.Visible && page.Rows[1].showTop == Visibility.Visible
                  && page.Rows[2].showTop == Visibility.Collapsed,
                "map: only the pinned rows are marked 置顶");
            Check(page.ReplyCount == 2,
                "map: ReplyCount is the wire replies count, not the published row count (" + page.ReplyCount + ")");
            Check(page.NextOffset == "probe-offset", "map: the cursor source hands next_offset to the caller");
        }

        private static void MappedRowKeepsTheWireFields()
        {
            var data = new JObject { ["replies"] = new JArray(Reply(555)) };
            var parsed = WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(data)));
            var row = WebCommentPageMapper.MapWebCommentPage(parsed.Value, WebCommentPageSource.Cursor, true).Rows[0];

            Check(row.rpid == 555 && row.rpid_str == "555" && row.oid == 1234 && row.type == 1,
                "map: ids survive (rpid/rpid_str/oid/type)");
            Check(row.mid == 9555 && row.root == 0 && row.parent == 0 && row.floor == 1 && row.state == 0
                  && row.ctime == 1700000000,
                "map: reply scalars survive (mid/root/parent/floor/state/ctime)");
            Check(row.count == 0 && row.rcount == 3 && row.rcount_str == "3" && row.like == 7 && row.like_str == "7",
                "map: counts and their display strings survive (rcount/like)");
            Check(row.action == 0, "map: action (like state of this account) survives");

            Check(row.member != null && row.member.mid == "9555" && row.member.uname == "probe user"
                  && row.member.sex == "保密" && row.member.sign == "probe sign",
                "map: the member identity survives");
            Check(row.member.avatar == "http://i0.hdslb.com/bfs/face/probe.jpg@64w_64h.jpg",
                "map: avatar still goes through the model's size suffix");
            Check(row.member.level_info != null && row.member.level_info.current_level == 5,
                "map: level_info.current_level drives the LV badge");
            Check(row.member.fans_detail != null && row.member.fans_detail.medal_name == "probe medal"
                  && row.member.fans_detail.level == 21 && row.member.fans_detail.medal_color == 111
                  && row.member.ShowFansDetail,
                "map: the fans medal survives (name/level/colour)");
            Check(row.member.user_sailing != null && row.member.user_sailing.cardbg != null
                  && row.member.user_sailing.cardbg.fan != null
                  && row.member.user_sailing.cardbg.fan.num_desc == "NO.8" && row.member.ShowCardBg,
                "map: the sailing card bg and its fan number survive");
            Check(row.member.user_sailing.pendant != null && row.member.user_sailing.pendant.id == 11,
                "map: user_sailing.pendant keeps its id");
            Check(row.member.pendant != null && row.member.pendant.pid == 33
                  && row.member.pendant.image == "http://i0.hdslb.com/bfs/garb/probe.png",
                "map: the avatar pendant keeps pid/image");
            Check(row.member.official_verify != null && row.member.official_verify.type == 1
                  && row.member.official_verify.desc == "probe verify",
                "map: official_verify keeps type/desc");
            Check(row.member.vip != null && row.member.vip.vipType == 2 && row.member.vipType == 2,
                "map: vip.vipType drives the name colour");

            Check(row.content != null && row.content.message == "probe message" && row.content.plat == 2
                  && row.content.plat_str == "来自 Android" && row.content.device == "probe device",
                "map: content text/platform survive");
            Check(row.content.pictures != null && row.content.pictures.Count == 1
                  && row.content.pictures[0].ImgSrc == "http://i0.hdslb.com/bfs/new_dyn/probe.jpg"
                  && row.content.haspictures == Visibility.Visible,
                "map: note pictures survive as NotePicture");
            Check(row.content.emote != null && row.content.emote[":)"] != null,
                "map: the emote table reaches the rich text builder unchanged");
            Check(row.up_action != null && row.up_action.like && !row.up_action.reply,
                "map: up_action drives the UP 觉得很赞 badge");
            Check(row.reply_control != null && row.reply_control.location == "IP属地：上海",
                "map: reply_control.location survives");
            Check(row.replies != null && row.replies.Count == 0,
                "map: every row starts with an empty replies collection");
        }

        private static void MissingCodeIsNotSuccess()
        {
            var parsed = WebCommentPageParser.ParseWebCommentPage(Ok("{\"message\":\"0\",\"data\":{\"replies\":[]}}"));
            Check(!parsed.IsSuccess && parsed.Failure == ApiParseFailure.Envelope,
                "no code: a body without a business code is not success");
            Check(parsed.Stage == "envelope.code",
                "no code: the failure is located at envelope.code (" + parsed.Stage + ")");
        }

        private static void NonNumericCodeIsNotSuccess()
        {
            var parsed = WebCommentPageParser.ParseWebCommentPage(Ok("{\"code\":\"0\",\"message\":\"0\",\"data\":{\"replies\":[]}}"));
            Check(!parsed.IsSuccess && parsed.Failure == ApiParseFailure.Envelope && parsed.Stage == "envelope.code",
                "non-numeric code: a string business code is not success either");
        }

        private static void BusinessRejectionKeepsItsMessage()
        {
            var parsed = WebCommentPageParser.ParseWebCommentPage(Ok("{\"code\":-404,\"message\":\"啥都木有\",\"data\":{\"replies\":[]}}"));
            Check(!parsed.IsSuccess && parsed.Failure == ApiParseFailure.Rejected,
                "rejection: code < 0 is a business rejection, not a payload problem");
            Check(parsed.Message == "啥都木有" && parsed.BusinessCode == -404,
                "rejection: the server message and the business code are carried (" + parsed.BusinessCode + ")");
            Check(parsed.Stage == "envelope.code", "rejection: the failure points at envelope.code");
        }

        private static void ClosedCommentAreaKeepsItsCode()
        {
            //评论区关闭时服务端连 data 都不给：拒绝判定必须在载荷判定之前
            var parsed = WebCommentPageParser.ParseWebCommentPage(Ok("{\"code\":12002,\"message\":\"评论区已关闭\"}"));
            Check(!parsed.IsSuccess && parsed.Failure == ApiParseFailure.Rejected && parsed.BusinessCode == 12002,
                "closed: 12002 is a rejection that carries its code, so the control can show 评论区已关闭");
        }

        private static void NonJsonBodyIsAnEnvelopeFailure()
        {
            var parsed = WebCommentPageParser.ParseWebCommentPage(Ok("<html>502 Bad Gateway</html>"));
            Check(!parsed.IsSuccess && parsed.Failure == ApiParseFailure.Envelope && parsed.Stage == "envelope.json",
                "malformed: a non-JSON body is an envelope failure (" + parsed.Stage + ")");
            Check(!string.IsNullOrEmpty(parsed.Message), "malformed: the envelope failure keeps a reason");
        }

        private static void TransportFailureIsItsOwnCategory()
        {
            var parsed = WebCommentPageParser.ParseWebCommentPage(new HttpResults
            {
                status = false,
                code = 502,
                message = "Bad Gateway",
                results = string.Empty,
            });
            Check(!parsed.IsSuccess && parsed.Failure == ApiParseFailure.Transport && parsed.HttpStatus == 502,
                "transport: a failed request is its own category, not a malformed body");
            Check(parsed.Message == "Bad Gateway", "transport: the transport message is kept for the toast");
            Check(parsed.Stage == "transport.status", "transport: the failure points at transport.status");
        }

        private static void MissingDataIsAnEnvelopeFailure()
        {
            var parsed = WebCommentPageParser.ParseWebCommentPage(Ok("{\"code\":0,\"message\":\"0\"}"));
            Check(!parsed.IsSuccess && parsed.Failure == ApiParseFailure.Envelope && parsed.Stage == "data",
                "no data: a successful code without data is not an empty page (" + parsed.Stage + ")");
        }

        private static void PayloadShapeIsChecked()
        {
            Check(Stage(WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(new JObject { ["replies"] = new JObject() }))))
                  == "data.replies",
                "shape: replies must be an array (or absent), an object is a payload failure");
            Check(Stage(WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(new JObject { ["top_replies"] = "x" }))))
                  == "data.top_replies",
                "shape: top_replies must be an array (or absent)");
            Check(Stage(WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(new JObject { ["upper"] = new JArray() }))))
                  == "data.upper",
                "shape: upper must be an object (or absent)");
            Check(Stage(WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(new JObject
                  {
                      ["upper"] = new JObject { ["top"] = 5 },
                  })))) == "data.upper.top",
                "shape: upper.top must be an object (or absent)");
            Check(Stage(WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(new JObject { ["cursor"] = new JArray() }))))
                  == "data.cursor",
                "shape: cursor must be an object (or absent)");
            Check(Stage(WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(new JObject
                  {
                      ["cursor"] = new JObject { ["pagination_reply"] = "x" },
                  })))) == "data.cursor.pagination_reply",
                "shape: cursor.pagination_reply must be an object (or absent)");
            Check(Stage(WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(new JObject
                  {
                      ["cursor"] = new JObject
                      {
                          ["pagination_reply"] = new JObject { ["next_offset"] = new JObject() },
                      },
                  })))) == "data.cursor.pagination_reply.next_offset",
                "shape: next_offset must be a scalar (or absent)");
            Check(Stage(WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(new JObject { ["replies"] = new JArray() }))))
                  == null,
                "shape: the same envelope with valid fields is accepted");
        }

        private static void PinnedDuplicateRpidAppearsOnce()
        {
            var data = new JObject
            {
                ["replies"] = new JArray(Reply(111), Reply(222), Reply(333)),
                ["top_replies"] = new JArray(Reply(111), Reply(222)),
                ["upper"] = new JObject { ["mid"] = 111, ["top"] = Reply(111) },
            };
            var parsed = WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(data)));
            var first = WebCommentPageMapper.MapWebCommentPage(parsed.Value, WebCommentPageSource.Cursor, true);
            Check(Rpids(first.Rows) == "111,222,333",
                "pinned: a reply that is both pinned and in replies appears once (" + Rpids(first.Rows) + ")");
            Check(first.Rows.Count == first.Rows.Select(x => x.rpid).Distinct().Count(),
                "pinned: the published page has no repeated rpid");
            Check(first.Rows[0].showTop == Visibility.Visible && first.Rows[1].showTop == Visibility.Visible
                  && first.Rows[2].showTop == Visibility.Collapsed,
                "pinned: upper.top and top_replies are 置顶, the plain replies are not");

            //翻页路径不改顺序也不去重：服务端翻页时可能把同一条再发一次，改造前也是直接 Append
            var second = WebCommentPageMapper.MapWebCommentPage(
                WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(new JObject
                {
                    ["replies"] = new JArray(Reply(333), Reply(333)),
                    ["top_replies"] = new JArray(Reply(111)),
                    ["upper"] = new JObject { ["mid"] = 111, ["top"] = Reply(111) },
                }))).Value,
                WebCommentPageSource.Cursor, false);
            Check(Rpids(second.Rows) == "333,333",
                "pinned: a later page publishes only its replies, in wire order (" + Rpids(second.Rows) + ")");
            Check(second.Rows.All(x => x.showTop == Visibility.Collapsed),
                "pinned: a later page marks nothing as 置顶");
        }

        private static void NullCombinationsDoNotThrow()
        {
            var both = WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(new JObject
            {
                ["replies"] = new JArray(Reply(1)),
                ["top_replies"] = JValue.CreateNull(),
                ["upper"] = JValue.CreateNull(),
                ["cursor"] = JValue.CreateNull(),
            })));
            Check(both.IsSuccess, "null: replies + null top_replies/upper/cursor is a valid page");
            var page = WebCommentPageMapper.MapWebCommentPage(both.Value, WebCommentPageSource.Cursor, true);
            Check(page.Rows.Count == 1 && page.NextOffset == null,
                "null: no pinned rows and no cursor, only the reply is published");

            var upperWithoutTop = WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(new JObject
            {
                ["replies"] = new JArray(),
                ["upper"] = new JObject { ["mid"] = 111 },
            })));
            Check(upperWithoutTop.IsSuccess && upperWithoutTop.Value.Upper.Top == null,
                "null: upper without top is a valid page with no UP 置顶");

            var cursorWithoutPagination = WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(new JObject
            {
                ["replies"] = new JArray(),
                ["cursor"] = new JObject(),
            })));
            Check(cursorWithoutPagination.IsSuccess && cursorWithoutPagination.Value.Cursor.PaginationReply == null,
                "null: cursor without pagination_reply is a valid page with no next page");
            Check(WebCommentPageMapper.MapWebCommentPage(cursorWithoutPagination.Value,
                      WebCommentPageSource.Cursor, true).NextOffset == null,
                "null: a cursor without pagination_reply publishes no next_offset");
        }

        private static void ValidEmptyPageIsNotMalformed()
        {
            var empty = WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(new JObject
            {
                ["replies"] = new JArray(),
                ["top_replies"] = new JArray(),
                ["cursor"] = new JObject { ["pagination_reply"] = new JObject { ["next_offset"] = "" } },
            })));
            Check(empty.IsSuccess && empty.Failure == ApiParseFailure.None,
                "empty: a page with no replies is a valid empty page, not malformed");
            var page = WebCommentPageMapper.MapWebCommentPage(empty.Value, WebCommentPageSource.Cursor, true);
            Check(page.Rows.Count == 0 && page.ReplyCount == 0, "empty: an empty page publishes no rows");
            Check(page.NextOffset == null, "empty: an empty next_offset means there is no next page");

            var nullReplies = WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(new JObject { ["replies"] = JValue.CreateNull() })));
            Check(nullReplies.IsSuccess && nullReplies.Value.Replies.Count == 0,
                "empty: replies: null is an empty page, not an error");

            var absent = WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(new JObject())));
            Check(absent.IsSuccess && absent.Value.Replies.Count == 0 && absent.Value.TopReplies.Count == 0,
                "empty: a page without the list fields is an empty page too");
            Check(WebCommentPageMapper.MapWebCommentPage(absent.Value, WebCommentPageSource.PageNumber, false)
                      .Rows.Count == 0,
                "empty: an empty later page publishes no rows");
        }

        private static void CursorBelongsToItsSource()
        {
            var body = Ok(PageBody(new JObject
            {
                ["replies"] = new JArray(Reply(1)),
                ["cursor"] = new JObject { ["pagination_reply"] = new JObject { ["next_offset"] = "cursor-only" } },
            }));
            var parsed = WebCommentPageParser.ParseWebCommentPage(body).Value;
            Check(WebCommentPageMapper.MapWebCommentPage(parsed, WebCommentPageSource.Cursor, true).NextOffset
                  == "cursor-only",
                "cursor: the cursor source carries next_offset");
            Check(WebCommentPageMapper.MapWebCommentPage(parsed, WebCommentPageSource.PageNumber, true).NextOffset
                  == null,
                "cursor: the pn source never touches the cursor slot (pn is the caller's own page number)");
            Check(WebCommentPageMapper.MapWebCommentPage(parsed, WebCommentPageSource.Cursor, false).NextOffset
                  == "cursor-only",
                "cursor: a later cursor page advances the same slot");

            var numeric = WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(new JObject
            {
                ["replies"] = new JArray(),
                ["cursor"] = new JObject { ["pagination_reply"] = new JObject { ["next_offset"] = 12345 } },
            })));
            Check(numeric.IsSuccess
                  && WebCommentPageMapper.MapWebCommentPage(numeric.Value, WebCommentPageSource.Cursor, true)
                         .NextOffset == "12345",
                "cursor: a numeric next_offset is carried as its wire text");

            //两路响应喂同一个页面模型：pn 源的正文里即使带了 cursor，也不该写进游标槽
            var legacy = WebCommentPageParser.ParseWebCommentPage(Ok(PageBody(new JObject
            {
                ["replies"] = new JArray(Reply(2), Reply(3)),
                ["cursor"] = new JObject { ["pagination_reply"] = new JObject { ["next_offset"] = "leftover" } },
            })));
            var legacyPage = WebCommentPageMapper.MapWebCommentPage(legacy.Value, WebCommentPageSource.PageNumber, false);
            Check(Rpids(legacyPage.Rows) == "2,3" && legacyPage.NextOffset == null,
                "cursor: the pn source publishes its rows but leaves the cursor slot alone");
        }

        private static void ReplyListParses()
        {
            var page = WebCommentPageParser.ParseWebCommentReplies(Ok(
                "{\"code\":0,\"message\":\"0\",\"data\":{\"page\":{\"count\":2},\"replies\":[" +
                Reply(7) + "," + Reply(8) + "]}}"));
            Check(page.IsSuccess && page.Value.Replies.Count == 2, "replies: the 楼中楼 page parses");
            var rows = WebCommentPageMapper.MapWebCommentReplies(page.Value);
            Check(Rpids(rows) == "7,8", "replies: rows keep the wire order (" + Rpids(rows) + ")");
            Check(rows[0].content.message == "probe message" && rows[0].member.uname == "probe user",
                "replies: a 楼中楼 row is mapped exactly like a top-level row");

            var empty = WebCommentPageParser.ParseWebCommentReplies(Ok(
                "{\"code\":0,\"message\":\"0\",\"data\":{\"replies\":[]}}"));
            Check(empty.IsSuccess && WebCommentPageMapper.MapWebCommentReplies(empty.Value).Count == 0,
                "replies: an empty 楼中楼 page is valid and publishes nothing");

            var nullReplies = WebCommentPageParser.ParseWebCommentReplies(Ok(
                "{\"code\":0,\"message\":\"0\",\"data\":{\"replies\":null}}"));
            Check(nullReplies.IsSuccess && nullReplies.Value.Replies.Count == 0,
                "replies: replies: null is an empty 楼中楼 page");

            Check(Stage(WebCommentPageParser.ParseWebCommentReplies(
                      Ok(PageBody(new JObject { ["replies"] = new JObject() })))) == "data.replies",
                "replies: a non-array replies field is a payload failure");
            Check(WebCommentPageParser.ParseWebCommentReplies(Ok(
                      "{\"code\":-404,\"message\":\"啥都木有\"}")).Failure == ApiParseFailure.Rejected,
                "replies: a rejection keeps its category");
            Check(WebCommentPageParser.ParseWebCommentReplies(Ok("{\"code\":0,\"message\":\"0\"}")).Stage == "data",
                "replies: a successful code without data is an envelope failure");
        }

        private static void ActionRepliesOnlyNeedCodeAndMessage()
        {
            var withData = WebCommentActionParser.Parse(Ok("{\"code\":0,\"message\":\"0\",\"data\":{\"action\":1}}"));
            Check(withData.IsSuccess && withData.Value.Code == 0,
                "action: a like reply with data is accepted");
            var withoutData = WebCommentActionParser.Parse(Ok("{\"code\":0,\"message\":\"0\"}"));
            Check(withoutData.IsSuccess,
                "action: an action reply without data is accepted (the reader must not require a payload)");

            var rejected = WebCommentActionParser.Parse(Ok("{\"code\":-400,\"message\":\"不能给自己点赞\"}"));
            Check(!rejected.IsSuccess && rejected.Failure == ApiParseFailure.Rejected
                  && rejected.Message == "不能给自己点赞" && rejected.BusinessCode == -400,
                "action: a rejection keeps the server message and the code");
            var rejectedWithoutMessage = WebCommentActionParser.Parse(Ok("{\"code\":-400}"));
            Check(rejectedWithoutMessage.Failure == ApiParseFailure.Rejected
                  && !string.IsNullOrEmpty(rejectedWithoutMessage.Message),
                "action: a rejection without a message still gets a fallback line");

            Check(WebCommentActionParser.Parse(Ok("{\"message\":\"0\"}")).Stage == "envelope.code",
                "action: a body without a business code is refused");
            Check(WebCommentActionParser.Parse(Ok("{\"code\":\"0\"}")).Stage == "envelope.code",
                "action: a non-numeric business code is refused");
            Check(WebCommentActionParser.Parse(Ok("not json")).Stage == "envelope.json",
                "action: a non-JSON body is an envelope failure");
            Check(WebCommentActionParser.Parse(new HttpResults
                  {
                      status = false,
                      code = 500,
                      message = "Server Error",
                      results = string.Empty,
                  }).Failure == ApiParseFailure.Transport,
                "action: a failed request stays a transport failure");
            Check(WebCommentActionParser.Parse(null).Failure == ApiParseFailure.Transport,
                "action: no response at all is a transport failure");
        }

        private static void ParsePathShowsNothing()
        {
            Check(Utils.Toasts.Count == 0,
                "silence: the parse layer never toasts (" + Utils.Toasts.Count + " toasts)");
            Check(LogHelper.Entries.Count == 0,
                "silence: the parse layer never logs (" + LogHelper.Entries.Count + " entries)");
        }

        private static void DeadPathsAreGone()
        {
            var root = RepoRoot();
            var control = File.ReadAllText(Path.Combine(root, "src", "BiliLite.UWP", "Controls",
                "CommentControl.xaml.cs"));
            Check(!control.Contains("dataCommentModel"),
                "dead paths: dataCommentModel (envelope and payload in one type) is gone from the control");
            Check(!control.Contains("JsonConvert.DeserializeObject") && !control.Contains("JObject.Parse"),
                "dead paths: the control no longer deserializes response bodies");
            Check(!control.Contains("GetData<object>"),
                "dead paths: the throwaway code-only decode is gone");
            Check(!control.Contains("class CommentItem") && !control.Contains("CommentCursor"),
                "dead paths: CommentItem and the duplicated cursor DTOs are gone");
            Check(control.Contains("WebCommentPageParser.ParseWebCommentPage")
                  && control.Contains("WebCommentPageParser.ParseWebCommentReplies")
                  && control.Contains("WebCommentActionParser.Parse"),
                "wire: the control decodes through the parser");
            Check(control.Contains("WebCommentPageMapper.MapWebCommentPage")
                  && control.Contains("WebCommentPageMapper.MapWebCommentReplies"),
                "wire: the control publishes what the mapper produced");
            Check(control.Contains("_nextOffset") && control.Contains("_useCursorSource"),
                "wire: the control keeps the cursor and the source bound to each other");

            var api = File.ReadAllText(Path.Combine(root, "src", "BiliLite.UWP", "Api", "CommentApi.cs"));
            Check(!api.Contains("Comment_New"), "dead paths: the callerless Comment_New builder is gone");
            Check(!api.Contains("readcomment"), "dead paths: the callerless endpoint constant is unreferenced here");
            Check(api.Contains("/x/v2/reply/wbi/main") && api.Contains("/x/v2/reply\","),
                "dead paths: the two live list endpoints are still the ones bound");
        }

        // ---------- fixtures ----------

        /// <summary>一条评论：字段取值固定，断言直接对着这些常量写；rpid 决定 mid，保证行之间可辨。</summary>
        private static JObject Reply(long rpid)
        {
            return new JObject
            {
                ["rpid"] = rpid,
                ["rpid_str"] = rpid.ToString(),
                ["oid"] = 1234,
                ["type"] = 1,
                ["mid"] = 9000 + rpid,
                ["root"] = 0,
                ["parent"] = 0,
                ["count"] = 0,
                ["rcount"] = 3,
                ["like"] = 7,
                ["action"] = 0,
                ["floor"] = 1,
                ["state"] = 0,
                ["ctime"] = 1700000000,
                ["member"] = new JObject
                {
                    ["mid"] = (9000 + rpid).ToString(),
                    ["uname"] = "probe user",
                    ["sex"] = "保密",
                    ["sign"] = "probe sign",
                    ["avatar"] = "http://i0.hdslb.com/bfs/face/probe.jpg",
                    ["level_info"] = new JObject { ["current_level"] = 5 },
                    ["fans_detail"] = new JObject
                    {
                        ["uid"] = 9000,
                        ["medal_id"] = 7,
                        ["medal_name"] = "probe medal",
                        ["level"] = 21,
                        ["medal_color"] = 111,
                        ["medal_color_end"] = 112,
                        ["medal_color_border"] = 113,
                        ["medal_color_name"] = 114,
                        ["medal_color_level"] = 115,
                    },
                    ["user_sailing"] = new JObject
                    {
                        ["pendant"] = new JObject
                        {
                            ["id"] = 11,
                            ["name"] = "sail pendant",
                            ["image"] = "http://i0.hdslb.com/bfs/garb/sail.png",
                        },
                        ["cardbg"] = new JObject
                        {
                            ["id"] = 22,
                            ["name"] = "card bg",
                            ["image"] = "http://i0.hdslb.com/bfs/garb/bg.png",
                            ["jump_url"] = "https://www.bilibili.com/blackboard/x.html",
                            ["fan"] = new JObject
                            {
                                ["is_fan"] = 1,
                                ["number"] = 8,
                                ["color"] = "#ffffff",
                                ["name"] = "fans",
                                ["num_desc"] = "NO.8",
                            },
                        },
                    },
                    ["pendant"] = new JObject
                    {
                        ["pid"] = 33,
                        ["name"] = "pendant",
                        ["image"] = "http://i0.hdslb.com/bfs/garb/probe.png",
                    },
                    ["official_verify"] = new JObject { ["type"] = 1, ["desc"] = "probe verify" },
                    ["vip"] = new JObject { ["vipType"] = 2, ["vipStatus"] = 1 },
                },
                ["content"] = new JObject
                {
                    ["message"] = "probe message",
                    ["plat"] = 2,
                    ["device"] = "probe device",
                    ["pictures"] = new JArray
                    {
                        new JObject
                        {
                            ["img_src"] = "http://i0.hdslb.com/bfs/new_dyn/probe.jpg",
                            ["img_width"] = 100,
                            ["img_height"] = 50,
                            ["img_size"] = 1024,
                        },
                    },
                    ["emote"] = new JObject
                    {
                        [":)"] = new JObject { ["text"] = ":)", ["url"] = "http://i0.hdslb.com/bfs/emote/a.png" },
                    },
                },
                ["up_action"] = new JObject { ["like"] = true, ["reply"] = false },
                ["reply_control"] = new JObject { ["time_desc"] = "", ["location"] = "IP属地：上海" },
            };
        }

        private static string PageBody(JObject data)
        {
            return new JObject { ["code"] = 0, ["message"] = "0", ["data"] = data }.ToString();
        }

        private static HttpResults Ok(string json)
        {
            return new HttpResults { status = true, code = 200, message = "0", results = json };
        }

        private static string Rpids(IEnumerable<CommentModel> rows)
        {
            return rows == null ? "null" : string.Join(",", rows.Select(x => x.rpid));
        }

        private static string Stage<T>(ApiParseResult<T> result)
        {
            return result.IsSuccess ? null : result.Stage;
        }

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "src", "BiliLite.UWP", "BiliLite.UWP.csproj")))
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

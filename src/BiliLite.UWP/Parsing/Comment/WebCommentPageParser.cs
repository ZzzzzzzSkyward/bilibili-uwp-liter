using System;
using System.Collections.Generic;
using BiliLite.Helpers;
using BiliLite.Models.Api.Comment;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.Comment
{
    /// <summary>
    /// 评论列表的两条读路径（游标源 x/v2/reply/wbi/main、pn 源 x/v2/reply）与楼中楼
    /// （x/v2/reply/reply）的解析：data 信封、字段形状。不选源、不发布、不弹提示、不记日志、不动游标。
    ///
    /// 字段/行为约定（媒体：改造前是一次 JsonConvert.DeserializeObject，落到一个既当信封又当载荷的
    /// 类型上，见 plan/tests/comments 夹具）
    ///
    /// | 线上路径                              | 线上类型 | 归一化                            | 缺失/null 时                      |
    /// | ------------------------------------- | -------- | --------------------------------- | --------------------------------- |
    /// | code                                  | int      | 业务码，非 0 = Rejected           | 缺失/非数字 = 信封无效            |
    /// | message                               | string   | Rejected 的说明，随业务码一起带出  | Rejected 时用兜底文案            |
    /// | data                                  | object   | 载荷                              | 缺失 = 信封无效                   |
    /// | data.replies                          | array    | 这一页的行，顺序原样              | 缺失/null = 空页（合法）          |
    /// | data.top_replies                      | array    | 第一页的置顶来源之一              | 缺失/null = 没有这类置顶          |
    /// | data.upper                            | object   | 只有 upper.top 有意义             | 缺失/null = 没有 UP 置顶          |
    /// | data.upper.top                        | object   | 置顶评论                          | 缺失/null = 没有                  |
    /// | data.cursor                           | object   | 游标源自己的游标，pn 源没有       | 缺失/null = 服务端没给下一页      |
    /// | data.cursor.pagination_reply          | object   | —                                 | 缺失/null = 没有下一页            |
    /// | data.cursor.pagination_reply.next_offset | string | 原样交给 mapper 当下一页游标   | 缺失/null/空串 = 没有下一页       |
    /// | data.page                             | object   | 不读（改造前也没读）              | 不检查                            |
    ///
    /// 1. 缺 code、code 不是数字、正文不是 JSON、没有 data 都是 Envelope 失败。改造前这些情况
    ///    解码成 code = 0 走成功分支（没有 data 才会 NRE），也就是"没拿到业务码"被当成成功——这里明确拒绝。
    /// 2. code != 0 是 Rejected，带业务码和 message；12002（评论区已关闭）怎么显示由控件判断。
    /// 3. replies 缺失/null 是合法空页，不是形状错误；空页与"形状不对"分属 Success / Payload。
    /// 4. top_replies/upper/top/cursor 各自独立可空，本层只把 replies/top_replies 补成空表：
    ///    "没有置顶/没有游标"是语义，不拿空对象去补。
    /// 5. 游标归属来源：本层只把 next_offset 交出去，写进哪个槽、pn 怎么走由 mapper 与控件决定。
    /// 6. 置顶合并顺序、rpid 去重、showTop 都在 WebCommentPageMapper。
    /// </summary>
    public static class WebCommentPageParser
    {
        public static ApiParseResult<WebCommentPageDto> ParseWebCommentPage(HttpResults response)
        {
            //传输、JSON、业务码、data 对象这一层两个源一样，交给 ApiEnvelope 统一判断
            var envelope = ApiEnvelope.Read(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<WebCommentPageDto>.FromFailure(envelope);
            }
            var data = envelope.Value;

            //逐个字段查形状，失败时能报到具体路径；null 都是合法的"没有"
            var shape = RequireArray(data, "replies")
                        ?? RequireArray(data, "top_replies")
                        ?? RequireObject(data, "upper")
                        ?? RequireObject(data, "upper.top")
                        ?? RequireObject(data, "cursor")
                        ?? RequireObject(data, "cursor.pagination_reply")
                        ?? RequireScalar(data, "cursor.pagination_reply.next_offset");
            if (shape != null)
            {
                return ApiParseResult<WebCommentPageDto>.Fail(ApiParseFailure.Payload, shape.Stage, shape.Message);
            }

            WebCommentPageDto page;
            try
            {
                page = data.ToObject<WebCommentPageDto>();
            }
            catch (Exception ex)
            {
                return ApiParseResult<WebCommentPageDto>.Fail(ApiParseFailure.Payload, "data",
                    "data 无法解码：" + ex.Message);
            }
            if (page == null)
            {
                return ApiParseResult<WebCommentPageDto>.Fail(ApiParseFailure.Payload, "data", "data 解码为空");
            }
            if (page.Replies == null)
            {
                page.Replies = new List<WebCommentReplyDto>();
            }
            if (page.TopReplies == null)
            {
                page.TopReplies = new List<WebCommentReplyDto>();
            }
            if (page.Cursor != null && page.Cursor.PaginationReply != null
                && string.IsNullOrWhiteSpace(page.Cursor.PaginationReply.NextOffset))
            {
                //空串是"没有下一页"，别让控件拿它去请求
                page.Cursor.PaginationReply.NextOffset = null;
            }
            return ApiParseResult<WebCommentPageDto>.Success(page);
        }

        public static ApiParseResult<WebCommentReplyListDto> ParseWebCommentReplies(HttpResults response)
        {
            var envelope = ApiEnvelope.Read(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<WebCommentReplyListDto>.FromFailure(envelope);
            }
            var data = envelope.Value;

            var shape = RequireArray(data, "replies");
            if (shape != null)
            {
                return ApiParseResult<WebCommentReplyListDto>.Fail(ApiParseFailure.Payload, shape.Stage, shape.Message);
            }

            WebCommentReplyListDto page;
            try
            {
                page = data.ToObject<WebCommentReplyListDto>();
            }
            catch (Exception ex)
            {
                return ApiParseResult<WebCommentReplyListDto>.Fail(ApiParseFailure.Payload, "data",
                    "data 无法解码：" + ex.Message);
            }
            if (page == null)
            {
                return ApiParseResult<WebCommentReplyListDto>.Fail(ApiParseFailure.Payload, "data", "data 解码为空");
            }
            if (page.Replies == null)
            {
                page.Replies = new List<WebCommentReplyDto>();
            }
            return ApiParseResult<WebCommentReplyListDto>.Success(page);
        }

        /// <summary>形状不对时的定位与说明；路径一律以 data 为根，例如 data.upper.top。</summary>
        private sealed class ShapeError
        {
            public string Stage;
            public string Message;
        }

        private static ShapeError RequireArray(JObject data, string path)
        {
            return Require(data, path, JTokenType.Array, "数组");
        }

        private static ShapeError RequireObject(JObject data, string path)
        {
            return Require(data, path, JTokenType.Object, "对象");
        }

        /// <summary>游标只可能是字符串或数字（线上两种都出现过），对象/数组都不是游标。</summary>
        private static ShapeError RequireScalar(JObject data, string path)
        {
            var token = data.SelectToken(path);
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.String
                || token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
            {
                return null;
            }
            return new ShapeError
            {
                Stage = "data." + path,
                Message = path + " 不是标量（实际是 " + token.Type + "）",
            };
        }

        private static ShapeError Require(JObject data, string path, JTokenType expected, string label)
        {
            var token = data.SelectToken(path);
            if (token == null || token.Type == JTokenType.Null || token.Type == expected)
            {
                return null;
            }
            return new ShapeError
            {
                Stage = "data." + path,
                Message = path + " 不是" + label + "（实际是 " + token.Type + "）",
            };
        }
    }
}

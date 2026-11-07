using System;
using BiliLite.Helpers;
using BiliLite.Models.Api.Comment;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.Comment
{
    /// <summary>
    /// 评论动作接口（x/v2/reply/action、x/v2/reply/add、x/v2/reply/del）的响应读取：
    /// 只有 code/message，没有 data 载荷，所以不能用 ApiEnvelope.Read（那条路要求 data 是对象）。
    /// 顺序与 ApiEnvelope 相同：传输 → 正文是否 JSON → 业务码是不是数字 → 业务码。不弹提示、不记日志。
    ///
    /// | 线上路径 | 线上类型 | 归一化                       | 缺失/null 时           |
    /// | -------- | -------- | ---------------------------- | ---------------------- |
    /// | code     | int      | 0 = 成功，非 0 = Rejected    | 缺失/非数字 = 信封无效 |
    /// | message  | string   | Rejected 的说明              | 用兜底文案             |
    /// | data     | —        | 不读（有些接口给，有些没有） | 不检查                 |
    ///
    /// 1. 只有 status 与业务码都是"成功"才算成功；改造前是 JObject.Parse 后直接 ToInt32，正文不是 JSON 会抛。
    /// 2. action=1（点赞）成功时 data.action 是服务端状态，界面自己在本地改计数，不读它。
    /// </summary>
    public static class WebCommentActionParser
    {
        public static ApiParseResult<WebCommentActionResultDto> Parse(HttpResults response)
        {
            if (response == null)
            {
                return ApiParseResult<WebCommentActionResultDto>.Fail(ApiParseFailure.Transport, "transport", "没有响应");
            }
            if (!response.status)
            {
                return ApiParseResult<WebCommentActionResultDto>.Fail(ApiParseFailure.Transport, "transport.status",
                    response.message, 0, response.code);
            }

            //静默解码：正文不是 JSON 对象是信封结论，提示由控件按类别弹
            JObject body;
            if (!response.TryGetJObject(out body))
            {
                return ApiParseResult<WebCommentActionResultDto>.Fail(ApiParseFailure.Envelope, "envelope.json",
                    "响应正文不是 JSON 对象");
            }

            var codeToken = body["code"];
            if (codeToken == null || codeToken.Type != JTokenType.Integer)
            {
                return ApiParseResult<WebCommentActionResultDto>.Fail(ApiParseFailure.Envelope, "envelope.code",
                    "响应没有可用的业务码");
            }
            var code = codeToken.Value<int>();

            WebCommentActionResultDto result;
            try
            {
                result = body.ToObject<WebCommentActionResultDto>();
            }
            catch (Exception ex)
            {
                return ApiParseResult<WebCommentActionResultDto>.Fail(ApiParseFailure.Payload, "envelope",
                    "信封无法解码：" + ex.Message);
            }
            if (result == null)
            {
                return ApiParseResult<WebCommentActionResultDto>.Fail(ApiParseFailure.Payload, "envelope",
                    "信封解码为空");
            }
            if (code != 0)
            {
                var message = string.IsNullOrWhiteSpace(result.Message) ? "操作被服务端拒绝" : result.Message;
                return ApiParseResult<WebCommentActionResultDto>.Fail(ApiParseFailure.Rejected, "envelope.code",
                    message, code, 0);
            }
            return ApiParseResult<WebCommentActionResultDto>.Success(result);
        }
    }
}

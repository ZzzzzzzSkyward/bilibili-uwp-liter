using BiliLite.Helpers;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.Video
{
    /// <summary>
    /// data 不一定是对象的端点用的信封读取：archive/has/like 的 data 是 0/1 数字，
    /// 动作端点（点赞、投币、三连、收藏、关注）的 data 可能是 null 或对象。
    /// <see cref="ApiEnvelope.Read"/> 只接受对象载荷，这里把载荷保留为原始 JToken；
    /// 传输、JSON、业务码三步判断与它逐句一致，形状判断留给各解析器。
    /// </summary>
    internal static class VideoEnvelope
    {
        /// <summary>data 的原始 JToken；载荷不存在时 Value 为 null，由调用方判形状。</summary>
        public static ApiParseResult<JToken> ReadDataToken(HttpResults response)
        {
            if (response == null)
            {
                return ApiParseResult<JToken>.Fail(ApiParseFailure.Transport, "transport", "没有响应");
            }
            if (!response.status)
            {
                return ApiParseResult<JToken>.Fail(ApiParseFailure.Transport, "transport.status",
                    response.message, 0, response.code);
            }

            //静默解码：正文不是 JSON 对象是信封结论，要提示什么由调用方决定，解析层不弹 toast
            JObject body;
            if (!response.TryGetJObject(out body))
            {
                return ApiParseResult<JToken>.Fail(ApiParseFailure.Envelope, "envelope.json", "响应正文不是 JSON 对象");
            }

            var codeToken = body["code"];
            if (codeToken == null || codeToken.Type != JTokenType.Integer)
            {
                //缺 code 或 code 不是数字都算信封无效：不能把"没拿到业务码"当成成功
                return ApiParseResult<JToken>.Fail(ApiParseFailure.Envelope, "envelope.code", "响应没有可用的业务码");
            }
            var code = codeToken.Value<int>();
            if (code != 0)
            {
                var message = body["message"]?.ToString();
                if (string.IsNullOrEmpty(message))
                {
                    message = body["msg"]?.ToString();
                }
                return ApiParseResult<JToken>.Fail(ApiParseFailure.Rejected, "envelope.code", message, code, 0);
            }
            return ApiParseResult<JToken>.Success(body["data"]);
        }
    }
}

using BiliLite.Helpers;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing
{
    /// <summary>
    /// REST 信封：传输状态、JSON、业务码、载荷对象，四步分开判断。
    /// 载荷是 data 还是 result 由端点决定，调用方按名字选一个 reader，不做"两个都试一下"。
    /// </summary>
    public static class ApiEnvelope
    {
        /// <summary>data 信封：web 聚合流、旧动态接口这类端点。</summary>
        public static ApiParseResult<JObject> Read(HttpResults response)
        {
            return ReadPayload(response, "data");
        }

        /// <summary>result 信封：pgc/web 番剧类端点。</summary>
        public static ApiParseResult<JObject> ReadResult(HttpResults response)
        {
            return ReadPayload(response, "result");
        }

        private static ApiParseResult<JObject> ReadPayload(HttpResults response, string field)
        {
            if (response == null)
            {
                return ApiParseResult<JObject>.Fail(ApiParseFailure.Transport, "transport", "没有响应");
            }
            if (!response.status)
            {
                return ApiParseResult<JObject>.Fail(ApiParseFailure.Transport, "transport.status",
                    response.message, 0, response.code);
            }

            //静默解码：正文不是 JSON 对象是信封结论，要提示什么由调用方决定，解析层不弹 toast
            JObject body;
            if (!response.TryGetJObject(out body))
            {
                return ApiParseResult<JObject>.Fail(ApiParseFailure.Envelope, "envelope.json", "响应正文不是 JSON 对象");
            }

            var codeToken = body["code"];
            if (codeToken == null || codeToken.Type != JTokenType.Integer)
            {
                //缺 code 或 code 不是数字都算信封无效：不能把"没拿到业务码"当成成功
                return ApiParseResult<JObject>.Fail(ApiParseFailure.Envelope, "envelope.code", "响应没有可用的业务码");
            }
            var code = codeToken.Value<int>();
            if (code != 0)
            {
                var message = body["message"]?.ToString();
                if (string.IsNullOrEmpty(message))
                {
                    message = body["msg"]?.ToString();
                }
                return ApiParseResult<JObject>.Fail(ApiParseFailure.Rejected, "envelope.code", message, code, 0);
            }

            var payloadToken = body[field];
            if (payloadToken == null || payloadToken.Type == JTokenType.Null)
            {
                return ApiParseResult<JObject>.Fail(ApiParseFailure.Envelope, field, "响应里没有 " + field);
            }
            if (payloadToken.Type != JTokenType.Object)
            {
                return ApiParseResult<JObject>.Fail(ApiParseFailure.Payload, field,
                    field + " 不是对象（实际是 " + payloadToken.Type + "）");
            }
            return ApiParseResult<JObject>.Success((JObject)payloadToken);
        }
    }
}

using BiliLite.Helpers;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing
{
    /// <summary>
    /// REST 信封：传输状态、JSON、业务码、data 对象，四步分开判断。
    /// web 聚合流与旧动态接口这一层完全一样，差别只在 data 里面的字段，所以只在这里做一次。
    /// </summary>
    public static class ApiEnvelope
    {
        public static ApiParseResult<JObject> Read(HttpResults response)
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

            var body = response.GetJObject();
            if (body == null)
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

            var dataToken = body["data"];
            if (dataToken == null || dataToken.Type == JTokenType.Null)
            {
                return ApiParseResult<JObject>.Fail(ApiParseFailure.Envelope, "data", "响应里没有 data");
            }
            if (dataToken.Type != JTokenType.Object)
            {
                return ApiParseResult<JObject>.Fail(ApiParseFailure.Payload, "data",
                    "data 不是对象（实际是 " + dataToken.Type + "）");
            }
            return ApiParseResult<JObject>.Success((JObject)dataToken);
        }
    }
}

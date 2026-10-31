using System;
using BiliLite.Helpers;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.Dynamic
{
    /// <summary>
    /// web 聚合动态流的一页。图片字段原样留在 items 里交给映射层，
    /// 这里只负责信封、形状和这一路自己的游标。
    /// </summary>
    public sealed class WebDynamicFeedPage
    {
        public JArray Items { get; set; }

        /// <summary>下一页要回传的 offset；空串/null 表示服务端没给游标</summary>
        public string NextCursor { get; set; }

        public bool HasMore { get; set; }
    }

    /// <summary>
    /// x/polymer/web-dynamic/v1/feed/{all,space} 的解析：解码一次、查信封、查字段形状。
    /// 不判断要不要换源，也不发布任何东西。
    /// </summary>
    public static class WebDynamicFeedParser
    {
        public static ApiParseResult<WebDynamicFeedPage> Parse(HttpResults response)
        {
            if (response == null)
            {
                return ApiParseResult<WebDynamicFeedPage>.Fail(ApiParseFailure.Transport, "transport", "没有响应");
            }
            if (!response.status)
            {
                return ApiParseResult<WebDynamicFeedPage>.Fail(ApiParseFailure.Transport, "transport.status",
                    response.message, 0, response.code);
            }

            var body = response.GetJObject();
            if (body == null)
            {
                return ApiParseResult<WebDynamicFeedPage>.Fail(ApiParseFailure.Envelope, "envelope.json",
                    "响应正文不是 JSON 对象");
            }

            var codeToken = body["code"];
            if (codeToken == null || codeToken.Type != JTokenType.Integer)
            {
                //缺 code 或 code 不是数字都算信封无效：不能把"没拿到业务码"当成成功
                return ApiParseResult<WebDynamicFeedPage>.Fail(ApiParseFailure.Envelope, "envelope.code",
                    "响应没有可用的业务码");
            }
            var code = codeToken.Value<int>();
            if (code != 0)
            {
                var message = body["message"]?.ToString();
                if (string.IsNullOrEmpty(message))
                {
                    message = body["msg"]?.ToString();
                }
                return ApiParseResult<WebDynamicFeedPage>.Fail(ApiParseFailure.Rejected, "envelope.code",
                    message, code, 0);
            }

            var dataToken = body["data"];
            if (dataToken == null || dataToken.Type == JTokenType.Null)
            {
                return ApiParseResult<WebDynamicFeedPage>.Fail(ApiParseFailure.Envelope, "data",
                    "响应里没有 data");
            }
            if (dataToken.Type != JTokenType.Object)
            {
                return ApiParseResult<WebDynamicFeedPage>.Fail(ApiParseFailure.Payload, "data",
                    "data 不是对象（实际是 " + dataToken.Type + "）");
            }
            var data = (JObject)dataToken;

            //items 缺失或为 null 是一个合法的空页；给了别的类型才是形状问题
            var itemsToken = data["items"];
            JArray items;
            if (itemsToken == null || itemsToken.Type == JTokenType.Null)
            {
                items = new JArray();
            }
            else if (itemsToken.Type == JTokenType.Array)
            {
                items = (JArray)itemsToken;
            }
            else
            {
                return ApiParseResult<WebDynamicFeedPage>.Fail(ApiParseFailure.Payload, "data.items",
                    "items 不是数组（实际是 " + itemsToken.Type + "）");
            }

            var hasMoreToken = data["has_more"];
            var hasMore = false;
            if (hasMoreToken != null && hasMoreToken.Type != JTokenType.Null)
            {
                if (hasMoreToken.Type != JTokenType.Boolean)
                {
                    return ApiParseResult<WebDynamicFeedPage>.Fail(ApiParseFailure.Payload, "data.has_more",
                        "has_more 不是布尔值（实际是 " + hasMoreToken.Type + "）");
                }
                hasMore = hasMoreToken.Value<bool>();
            }

            var cursorToken = data["offset"];
            return ApiParseResult<WebDynamicFeedPage>.Success(new WebDynamicFeedPage
            {
                Items = items,
                HasMore = hasMore,
                NextCursor = cursorToken == null || cursorToken.Type == JTokenType.Null
                    ? null
                    : cursorToken.ToString(),
            });
        }
    }
}

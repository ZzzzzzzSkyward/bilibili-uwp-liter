using System.Collections.Generic;
using BiliLite.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.Dynamic
{
    /// <summary>
    /// 旧动态接口（dynamic_new / dynamic_history / space_history）的一页卡片。
    /// cards 只做形状检查，卡片模型由持有模型的调用方反序列化——首页视频流与关注流用的是两套卡片。
    /// </summary>
    public sealed class LegacyCardPage
    {
        public JArray Cards { get; set; }

        public bool HasMore { get; set; }

        /// <summary>data.offset；这一路的翻页游标，空串/null 表示没有</summary>
        public string NextCursor { get; set; }
    }

    /// <summary>
    /// 旧 REST 动态接口的解析：信封交给 ApiEnvelope，这里只管 cards / has_more / offset 三个字段。
    /// </summary>
    public static class LegacyDynamicFeedParser
    {
        public static ApiParseResult<LegacyCardPage> Parse(HttpResults response)
        {
            var envelope = ApiEnvelope.Read(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<LegacyCardPage>.FromFailure(envelope);
            }
            var data = envelope.Value;

            //cards 缺失或为 null 是合法的空页；给了别的类型才是形状问题
            var cardsToken = data["cards"];
            JArray cards;
            if (cardsToken == null || cardsToken.Type == JTokenType.Null)
            {
                cards = new JArray();
            }
            else if (cardsToken.Type == JTokenType.Array)
            {
                cards = (JArray)cardsToken;
            }
            else
            {
                return ApiParseResult<LegacyCardPage>.Fail(ApiParseFailure.Payload, "data.cards",
                    "cards 不是数组（实际是 " + cardsToken.Type + "）");
            }

            var cursorToken = data["offset"];
            return ApiParseResult<LegacyCardPage>.Success(new LegacyCardPage
            {
                Cards = cards,
                HasMore = ReadHasMore(data["has_more"]),
                NextCursor = cursorToken == null || cursorToken.Type == JTokenType.Null
                    ? null
                    : cursorToken.ToString(),
            });
        }

        /// <summary>
        /// 动态详情：data 是唯一的卡片信封，缺了就解析不出详情。
        /// </summary>
        public static ApiParseResult<JObject> ParseDetail(HttpResults response)
        {
            return ApiEnvelope.Read(response);
        }

        /// <summary>
        /// 旧接口的 has_more 既出现布尔值也出现 0/1，两种都认；别的类型按"没有更多"处理。
        /// </summary>
        public static bool ReadHasMore(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return false;
            }
            if (token.Type == JTokenType.Boolean)
            {
                return token.Value<bool>();
            }
            if (token.Type == JTokenType.Integer)
            {
                return token.Value<long>() != 0;
            }
            return false;
        }

        /// <summary>
        /// 把这一页的 cards 交给持有卡片模型的调用方反序列化；元素形状不对时算整页 Payload 失败，
        /// 而不是抛到请求线程上。
        /// </summary>
        public static bool TryDeserialize<T>(LegacyCardPage page, JsonSerializerSettings settings, out List<T> cards)
        {
            cards = null;
            if (page == null)
            {
                return false;
            }
            try
            {
                cards = JsonConvert.DeserializeObject<List<T>>(page.Cards.ToString(), settings) ?? new List<T>();
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}

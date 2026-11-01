using BiliLite.Helpers;
using BiliLite.Models.Api.Video;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.Video
{
    /// <summary>
    /// 详情页的三条"我有没有点过"读：archive/has/like、archive/coins、fav/video/favoured。
    /// 三条只回答状态、不决定详情是否发布；不弹提示、不记日志。
    ///
    /// 字段/行为约定（媒体：详情页真实响应形状；夹具见 plan/tests/c1）
    ///
    /// | 端点 / 线上路径    | 线上类型 | 归一化                        | 缺失/null 时           |
    /// | ------------------ | -------- | ----------------------------- | ---------------------- |
    /// | code               | int      | 业务码，非 0 = Rejected       | 缺失/非数字 = 信封无效 |
    /// | has/like: data     | int (0/1)| 原样                          | Payload，定位 data     |
    /// | has/like: data     | 其它类型 | —                             | Payload，定位 data     |
    /// | coins: data        | object   | 载荷                          | 缺失/null = 信封无效   |
    /// | coins: data.multiply | int    | 已投币枚数原样                | Payload，定位 data.multiply |
    /// | favoured: data     | object   | 载荷                          | 缺失/null = 信封无效   |
    /// | favoured: data.favoured | bool | 原样                        | Payload，定位 data.favoured |
    ///
    /// 1. has/like 的 data 是 0/1 数字而不是对象，所以它走 VideoEnvelope：
    ///    ApiEnvelope.Read 会把"不是对象"判成 Payload，那样点赞状态永远读不到。
    /// 2. multiply 保留枚数、"是否投币"由 VM 折成 1/0：改造前就是 `> 0`，折法留在发布者。
    /// 3. 三条都是"读不到就保持原值"，失败不打断页面。
    /// </summary>
    public static class WebVideoStateParser
    {
        public static ApiParseResult<int> ParseWebVideoStateLike(HttpResults response)
        {
            var envelope = VideoEnvelope.ReadDataToken(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<int>.FromFailure(envelope);
            }

            var data = envelope.Value;
            if (data == null || data.Type == JTokenType.Null)
            {
                return ApiParseResult<int>.Fail(ApiParseFailure.Payload, "data", "响应里没有点赞状态");
            }
            if (data.Type != JTokenType.Integer)
            {
                return ApiParseResult<int>.Fail(ApiParseFailure.Payload, "data",
                    "data 不是 0/1 数字（实际是 " + data.Type + "）");
            }
            return ApiParseResult<int>.Success(data.Value<int>());
        }

        public static ApiParseResult<int> ParseWebVideoStateCoin(HttpResults response)
        {
            var envelope = ApiEnvelope.Read(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<int>.FromFailure(envelope);
            }
            var data = envelope.Value;

            var multiply = data["multiply"];
            if (multiply == null || multiply.Type == JTokenType.Null)
            {
                return ApiParseResult<int>.Fail(ApiParseFailure.Payload, "data.multiply",
                    "响应里没有 multiply");
            }
            if (multiply.Type != JTokenType.Integer)
            {
                return ApiParseResult<int>.Fail(ApiParseFailure.Payload, "data.multiply",
                    "multiply 不是整数（实际是 " + multiply.Type + "）");
            }
            return ApiParseResult<int>.Success(multiply.Value<int>());
        }

        public static ApiParseResult<bool> ParseWebVideoStateFavoured(HttpResults response)
        {
            var envelope = ApiEnvelope.Read(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<bool>.FromFailure(envelope);
            }
            var data = envelope.Value;

            var favoured = data["favoured"];
            if (favoured == null || favoured.Type == JTokenType.Null)
            {
                return ApiParseResult<bool>.Fail(ApiParseFailure.Payload, "data.favoured",
                    "响应里没有 favoured");
            }
            if (favoured.Type != JTokenType.Boolean)
            {
                return ApiParseResult<bool>.Fail(ApiParseFailure.Payload, "data.favoured",
                    "favoured 不是布尔（实际是 " + favoured.Type + "）");
            }
            return ApiParseResult<bool>.Success(favoured.Value<bool>());
        }
    }
}

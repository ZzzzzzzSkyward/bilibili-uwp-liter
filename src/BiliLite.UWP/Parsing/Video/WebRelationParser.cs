using BiliLite.Helpers;
using BiliLite.Models.Api.Video;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.Video
{
    /// <summary>
    /// 详情页的两条"UP主关系"读：x/relation/stat（粉丝数）与 x/relation（关注状态）。
    /// 两个端点都是 data 对象信封，都只取一个标量；不弹提示、不记日志。
    ///
    /// 字段/行为约定（媒体：详情页真实响应形状；夹具见 plan/tests/c1）
    ///
    /// | 端点 / 线上路径    | 线上类型 | 归一化                          | 缺失/null 时           |
    /// | ------------------ | -------- | ------------------------------- | ---------------------- |
    /// | code               | int      | 业务码，非 0 = Rejected         | 缺失/非数字 = 信封无效 |
    /// | data               | object   | 载荷                            | 缺失/null = 信封无效   |
    /// | relation/stat: data.follower | int | 粉丝数原样                   | 按 0（粉丝标签隐藏）   |
    /// | relation: data.attribute     | int | 原样（2/6 = 已关注/互关）    | Payload，定位 data.attribute |
    ///
    /// 1. follower 缺失按 0、不判失败：改造前就是 `?? 0`，0 与"没有粉丝数"在页面上是同一件事
    ///    （HasFans 只认 > 0）。
    /// 2. attribute 缺失判 Payload：关注状态没有默认值，VM 失败时保持既有状态，
    ///    改造前是"缺了就当成未关注（-999）"，两种写法在登录游客路径上结果相同。
    /// 3. 这两个值都不决定换源，也不影响详情是否发布。
    /// </summary>
    public static class WebRelationParser
    {
        public static ApiParseResult<int> ParseWebRelationStat(HttpResults response)
        {
            var envelope = ApiEnvelope.Read(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<int>.FromFailure(envelope);
            }
            var data = envelope.Value;

            var follower = data["follower"];
            if (follower == null || follower.Type == JTokenType.Null)
            {
                return ApiParseResult<int>.Success(0);
            }
            if (follower.Type != JTokenType.Integer)
            {
                return ApiParseResult<int>.Fail(ApiParseFailure.Payload, "data.follower",
                    "follower 不是整数（实际是 " + follower.Type + "）");
            }
            return ApiParseResult<int>.Success(follower.Value<int>());
        }

        public static ApiParseResult<int> ParseWebRelationAttribute(HttpResults response)
        {
            var envelope = ApiEnvelope.Read(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<int>.FromFailure(envelope);
            }
            var data = envelope.Value;

            var attribute = data["attribute"];
            if (attribute == null || attribute.Type == JTokenType.Null)
            {
                return ApiParseResult<int>.Fail(ApiParseFailure.Payload, "data.attribute",
                    "响应里没有 attribute");
            }
            if (attribute.Type != JTokenType.Integer)
            {
                return ApiParseResult<int>.Fail(ApiParseFailure.Payload, "data.attribute",
                    "attribute 不是整数（实际是 " + attribute.Type + "）");
            }
            return ApiParseResult<int>.Success(attribute.Value<int>());
        }
    }
}

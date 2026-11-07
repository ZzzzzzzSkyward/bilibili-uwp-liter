using System;
using System.Collections.Generic;
using BiliLite.Helpers;
using BiliLite.Models.Api.Video;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.Video
{
    /// <summary>
    /// x/tag/archive/tags 的解析：data 是数组本身（走 VideoEnvelope，不走只接受对象的 ApiEnvelope）。
    /// 不弹提示、不记日志；标签只是详情页的附加信息，失败时 VM 保持原有标签。
    ///
    /// 字段/行为约定（媒体：详情页真实响应形状；夹具见 plan/tests/c1）
    ///
    /// | 线上路径       | 线上类型 | 归一化                              | 缺失/null 时            |
    /// | -------------- | -------- | ----------------------------------- | ----------------------- |
    /// | code           | int      | 业务码，非 0 = Rejected             | 缺失/非数字 = 信封无效  |
    /// | data           | array    | List&lt;WebVideoTagDto&gt;，顺序原样 | 缺失/null = Payload     |
    /// | data           | 对象/标量| —                                   | Payload，定位 data      |
    /// | data[].tag_id  | int      | 原样                                | 按 0                    |
    /// | data[].tag_name| string   | 原样                                | 保持 null               |
    /// </summary>
    public static class WebVideoTagsParser
    {
        public static ApiParseResult<List<WebVideoTagDto>> ParseWebVideoTags(HttpResults response)
        {
            var envelope = VideoEnvelope.ReadDataToken(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<List<WebVideoTagDto>>.FromFailure(envelope);
            }

            var data = envelope.Value;
            if (data == null || data.Type == JTokenType.Null)
            {
                return ApiParseResult<List<WebVideoTagDto>>.Fail(ApiParseFailure.Payload, "data",
                    "响应里没有标签列表");
            }
            if (data.Type != JTokenType.Array)
            {
                return ApiParseResult<List<WebVideoTagDto>>.Fail(ApiParseFailure.Payload, "data",
                    "data 不是数组（实际是 " + data.Type + "）");
            }

            List<WebVideoTagDto> tags;
            try
            {
                tags = data.ToObject<List<WebVideoTagDto>>();
            }
            catch (Exception ex)
            {
                return ApiParseResult<List<WebVideoTagDto>>.Fail(ApiParseFailure.Payload, "data",
                    "data 无法解码：" + ex.Message);
            }
            if (tags == null)
            {
                return ApiParseResult<List<WebVideoTagDto>>.Fail(ApiParseFailure.Payload, "data",
                    "data 解码为空");
            }
            tags.RemoveAll(x => x == null);
            return ApiParseResult<List<WebVideoTagDto>>.Success(tags);
        }
    }
}

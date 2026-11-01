using System;
using System.Collections.Generic;
using BiliLite.Helpers;
using BiliLite.Models.Api.Video;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.Video
{
    /// <summary>
    /// x/web-interface/archive/related 的解析：data 就是数组本身（不是对象，所以走 VideoEnvelope）。
    /// 不合并进详情（合并是 VideoDetailVM 的事），不弹提示、不记日志。
    ///
    /// 字段/行为约定（媒体：详情页真实响应形状；夹具见 plan/tests/c1）
    ///
    /// | 线上路径   | 线上类型 | 归一化                                 | 缺失/null 时            |
    /// | ---------- | -------- | -------------------------------------- | ----------------------- |
    /// | code       | int      | 业务码，非 0 = Rejected                | 缺失/非数字 = 信封无效  |
    /// | data       | array    | List&lt;WebVideoRelateDto&gt;，顺序原样 | 缺失/null = Payload     |
    /// | data       | 对象/标量| —                                      | Payload，定位 data      |
    /// | data[].aid | 数字     | 读进 string                            | 保持 null（mapper 过滤）|
    ///
    /// 1. data 缺失/null 是 Payload 而不是"没有推荐"：线上推荐为空是空数组，
    ///    拿不到数组说明这一路读本身没成；VM 据此保留整页、给空推荐表。
    /// 2. 这一路失败只影响推荐区：见 VideoDetailVM，详情照常发布。
    /// 3. 数组里的 null 项直接丢掉，mapper 不再逐项判空。
    /// </summary>
    public static class WebVideoRelatesParser
    {
        public static ApiParseResult<List<WebVideoRelateDto>> ParseWebVideoRelates(HttpResults response)
        {
            var envelope = VideoEnvelope.ReadDataToken(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<List<WebVideoRelateDto>>.FromFailure(envelope);
            }

            var data = envelope.Value;
            if (data == null || data.Type == JTokenType.Null)
            {
                return ApiParseResult<List<WebVideoRelateDto>>.Fail(ApiParseFailure.Payload, "data",
                    "响应里没有推荐列表");
            }
            if (data.Type != JTokenType.Array)
            {
                return ApiParseResult<List<WebVideoRelateDto>>.Fail(ApiParseFailure.Payload, "data",
                    "data 不是数组（实际是 " + data.Type + "）");
            }

            List<WebVideoRelateDto> relates;
            try
            {
                relates = data.ToObject<List<WebVideoRelateDto>>();
            }
            catch (Exception ex)
            {
                return ApiParseResult<List<WebVideoRelateDto>>.Fail(ApiParseFailure.Payload, "data",
                    "data 无法解码：" + ex.Message);
            }
            if (relates == null)
            {
                return ApiParseResult<List<WebVideoRelateDto>>.Fail(ApiParseFailure.Payload, "data",
                    "data 解码为空");
            }
            relates.RemoveAll(x => x == null);
            return ApiParseResult<List<WebVideoRelateDto>>.Success(relates);
        }
    }
}

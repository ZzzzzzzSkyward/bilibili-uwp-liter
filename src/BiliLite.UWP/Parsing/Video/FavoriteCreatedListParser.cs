using System;
using System.Collections.Generic;
using BiliLite.Helpers;
using BiliLite.Models.Api.Video;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.Video
{
    /// <summary>
    /// medialist/gateway/base/created 的解析：详情页"收藏到哪个收藏夹"用。
    /// 不弹提示、不记日志；收藏夹列表读不到只影响收藏弹窗，不影响详情页。
    ///
    /// 字段/行为约定（媒体：详情页真实响应形状；夹具见 plan/tests/c1）
    ///
    /// | 线上路径      | 线上类型      | 归一化                                    | 缺失/null 时            |
    /// | ------------- | ------------- | ----------------------------------------- | ----------------------- |
    /// | code          | int           | 业务码，非 0 = Rejected                   | 缺失/非数字 = 信封无效  |
    /// | data          | object        | 载荷                                      | 缺失/null = 信封无效    |
    /// | data.list     | array | null  | List&lt;FavoriteCreatedListItemDto&gt;    | 视为什么都没有          |
    /// | data.list     | 其它类型      | —                                         | Payload，定位 data.list |
    /// | data.count    | int           | 原样带上，不参与判断                      | 按 0                    |
    ///
    /// 改造前这里是把 data["list"] 再 DeserializeObject 一次（拿不到就是 NRE 被 catch 折成一句 toast），
    /// 现在由 DTO 直接读出形状；list 缺失给空表，UI 显示"没有收藏夹"而不是报错。
    /// </summary>
    public static class FavoriteCreatedListParser
    {
        public static ApiParseResult<FavoriteCreatedListDto> ParseFavoriteCreatedList(HttpResults response)
        {
            var envelope = ApiEnvelope.Read(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<FavoriteCreatedListDto>.FromFailure(envelope);
            }
            var data = envelope.Value;

            var listToken = data["list"];
            if (listToken != null && listToken.Type != JTokenType.Null && listToken.Type != JTokenType.Array)
            {
                return ApiParseResult<FavoriteCreatedListDto>.Fail(ApiParseFailure.Payload, "data.list",
                    "list 不是数组（实际是 " + listToken.Type + "）");
            }

            FavoriteCreatedListDto created;
            try
            {
                created = data.ToObject<FavoriteCreatedListDto>();
            }
            catch (Exception ex)
            {
                return ApiParseResult<FavoriteCreatedListDto>.Fail(ApiParseFailure.Payload, "data",
                    "data 无法解码：" + ex.Message);
            }
            if (created == null)
            {
                return ApiParseResult<FavoriteCreatedListDto>.Fail(ApiParseFailure.Payload, "data",
                    "data 解码为空");
            }
            if (created.List == null)
            {
                created.List = new List<FavoriteCreatedListItemDto>();
            }
            created.List.RemoveAll(x => x == null);
            return ApiParseResult<FavoriteCreatedListDto>.Success(created);
        }
    }
}

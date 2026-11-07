using System;
using BiliLite.Helpers;
using BiliLite.Models.Api.Video;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.Video
{
    /// <summary>
    /// x/web-interface/view（以及 app/x/v2/view、第三方代理）的解析：data 信封与载荷形状。
    /// 不判断要不要换源（三个源的先后由 VideoDetailVM 定），不弹提示、不记日志、不碰 req_user。
    ///
    /// 字段/行为约定（媒体：详情页真实响应形状；夹具见 plan/tests/c1）
    ///
    /// | 线上路径                | 线上类型      | 归一化                          | 缺失/null 时                    |
    /// | ----------------------- | ------------- | ------------------------------- | ------------------------------- |
    /// | code                    | int           | 业务码，非 0 = Rejected         | 缺失/非数字 = 信封无效          |
    /// | data                    | object        | 载荷                            | 缺失/null = 信封无效            |
    /// | data.pages              | array | null  | List&lt;WebVideoPageDto&gt;     | 视为没有分P（mapper 给空表）    |
    /// | data.staff              | array | null  | List&lt;WebVideoStaffDto&gt;    | 保持 null（页面据此隐藏合作栏） |
    /// | data.ugc_season         | object | null | VideoUgcSeason，顺序原样        | 视为不属于合集                  |
    /// | data.ugc_season.sections| array | null  | 保持顺序                        | 视为空分区                      |
    /// | data.aid/cid/mid        | 数字          | 读进 string，不转数字           | 保持 null                       |
    /// | data.req_user           | object | null | 只做形状保真，mapper 不搬       | 无所谓（本地状态）              |
    ///
    /// 1. 形状不对（pages/staff/ugc_season/sections 不是该有的类型、data 解码不了）是 Payload 失败，
    ///    带字段路径；这样 VM 的换源决定能落在"这一源不可用"而不是"整页炸了"。
    /// 2. 只有"三个源都失败"才由 VM 显示错误面板。单个源解码失败在这里就结束，不抛异常。
    /// 3. relates 不在这条响应里解析：它是另一个端点，见 WebVideoRelatesParser。
    /// </summary>
    public static class WebVideoViewParser
    {
        public static ApiParseResult<WebVideoViewDto> ParseWebVideoView(HttpResults response)
        {
            var envelope = ApiEnvelope.Read(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<WebVideoViewDto>.FromFailure(envelope);
            }
            var data = envelope.Value;

            //三个数组/对象字段先单独查形状，失败时能报到具体字段
            var pagesToken = data["pages"];
            if (pagesToken != null && pagesToken.Type != JTokenType.Null && pagesToken.Type != JTokenType.Array)
            {
                return ApiParseResult<WebVideoViewDto>.Fail(ApiParseFailure.Payload, "data.pages",
                    "pages 不是数组（实际是 " + pagesToken.Type + "）");
            }
            var staffToken = data["staff"];
            if (staffToken != null && staffToken.Type != JTokenType.Null && staffToken.Type != JTokenType.Array)
            {
                return ApiParseResult<WebVideoViewDto>.Fail(ApiParseFailure.Payload, "data.staff",
                    "staff 不是数组（实际是 " + staffToken.Type + "）");
            }
            var seasonToken = data["ugc_season"];
            if (seasonToken != null && seasonToken.Type != JTokenType.Null && seasonToken.Type != JTokenType.Object)
            {
                return ApiParseResult<WebVideoViewDto>.Fail(ApiParseFailure.Payload, "data.ugc_season",
                    "ugc_season 不是对象（实际是 " + seasonToken.Type + "）");
            }
            if (seasonToken != null && seasonToken.Type == JTokenType.Object)
            {
                var sectionsToken = seasonToken["sections"];
                if (sectionsToken != null && sectionsToken.Type != JTokenType.Null
                    && sectionsToken.Type != JTokenType.Array)
                {
                    return ApiParseResult<WebVideoViewDto>.Fail(ApiParseFailure.Payload,
                        "data.ugc_season.sections", "sections 不是数组（实际是 " + sectionsToken.Type + "）");
                }
            }

            WebVideoViewDto view;
            try
            {
                view = data.ToObject<WebVideoViewDto>();
            }
            catch (Exception ex)
            {
                return ApiParseResult<WebVideoViewDto>.Fail(ApiParseFailure.Payload, "data",
                    "data 无法解码：" + ex.Message);
            }
            if (view == null)
            {
                return ApiParseResult<WebVideoViewDto>.Fail(ApiParseFailure.Payload, "data", "data 解码为空");
            }
            return ApiParseResult<WebVideoViewDto>.Success(view);
        }
    }
}

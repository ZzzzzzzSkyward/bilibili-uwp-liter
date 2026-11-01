using System;
using BiliLite.Helpers;
using BiliLite.Models.Api.MediaList;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.MediaList
{
    /// <summary>
    /// /x/v3/fav/resource/list 的解析：data 信封、收藏夹信息与这一页的视频。
    /// 不判断翻页（页面用 media_count 判），不发布、不弹提示、不记日志。
    ///
    /// 字段/行为约定（媒体：收藏夹详情页夹具见 plan/tests/c3 的 source 断言，解析层由 c2 直接执行）
    ///
    /// | 线上路径        | 线上类型 | 归一化                   | 缺失/null 时                         |
    /// | --------------- | -------- | ------------------------ | ------------------------------------- |
    /// | code            | int      | 业务码，非 0 = Rejected  | 缺失/非数字 = 信封无效                |
    /// | data            | object   | 载荷                     | 缺失 = 信封无效                       |
    /// | data.info       | object   | 收藏夹信息（标题、mid）  | 缺失/null = null（第 1 页由发布者拦） |
    /// | data.medias     | array    | 这一页的视频，顺序原样   | 缺失/null = 空表（空收藏夹不是错误）  |
    /// | data.medias     | 其它类型 | —                        | Payload，定位 data.medias             |
    /// | data.has_more   | bool     | 还有下一页               | 缺失/null = false（只原样带上）       |
    /// | data.has_more   | 其它类型 | —                        | Payload，定位 data.has_more           |
    ///
    /// 1. info 缺失给 null、不判失败：改造前它在页面第一条 `FavoriteInfo.mid` 上 NRE 再被 catch
    ///    折成一句通用提示，结论是"不发布"；这里把判断留给发布者（第 1 页缺 info 就不发布），
    ///    这样 Type==21 那条没有夹具的路径也不会被解析层硬拒。
    /// 2. medias 缺失/null 是合法的空页——改造前就是"这一页没有视频"，第 1 页显示空状态、
    ///    第 2 页什么都不追加，这里保持同样的结论。
    /// 3. 只由 x/v3/fav/resource/list 读出形状。同一 VM 里 Type==21 走的
    ///    x/space/fav/season/list 用的是同一份页面模型，没有实网夹具，按同一形状解析。
    /// </summary>
    public static class FavoriteResourceListParser
    {
        public static ApiParseResult<FavoriteResourceListDto> Parse(HttpResults response)
        {
            var envelope = ApiEnvelope.Read(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<FavoriteResourceListDto>.FromFailure(envelope);
            }
            var data = envelope.Value;

            var infoToken = data["info"];
            if (infoToken != null && infoToken.Type != JTokenType.Null && infoToken.Type != JTokenType.Object)
            {
                return ApiParseResult<FavoriteResourceListDto>.Fail(ApiParseFailure.Payload, "data.info",
                    "info 不是对象（实际是 " + infoToken.Type + "）");
            }

            //medias 缺失/null 是合法的空页，只有给了别的类型才算形状问题
            var mediasToken = data["medias"];
            if (mediasToken != null && mediasToken.Type != JTokenType.Null
                && mediasToken.Type != JTokenType.Array)
            {
                return ApiParseResult<FavoriteResourceListDto>.Fail(ApiParseFailure.Payload, "data.medias",
                    "medias 不是数组（实际是 " + mediasToken.Type + "）");
            }

            var hasMoreToken = data["has_more"];
            if (hasMoreToken != null && hasMoreToken.Type != JTokenType.Null
                && hasMoreToken.Type != JTokenType.Boolean)
            {
                return ApiParseResult<FavoriteResourceListDto>.Fail(ApiParseFailure.Payload, "data.has_more",
                    "has_more 不是布尔（实际是 " + hasMoreToken.Type + "）");
            }

            FavoriteResourceListDto page;
            try
            {
                page = data.ToObject<FavoriteResourceListDto>();
            }
            catch (Exception ex)
            {
                return ApiParseResult<FavoriteResourceListDto>.Fail(ApiParseFailure.Payload, "data",
                    "data 无法解码：" + ex.Message);
            }
            if (page == null)
            {
                return ApiParseResult<FavoriteResourceListDto>.Fail(ApiParseFailure.Payload, "data", "data 解码为空");
            }
            return ApiParseResult<FavoriteResourceListDto>.Success(page);
        }
    }
}

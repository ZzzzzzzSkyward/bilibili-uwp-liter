using System;
using BiliLite.Helpers;
using BiliLite.Models.Api.MediaList;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.MediaList
{
    /// <summary>
    /// /x/v2/medialist/resource/list 的解析：data 信封、这一页的行与两个翻页字段。
    /// 不判断要不要换源（签名/未登录由 Services 决定），不发布任何东西，不弹提示，不记日志。
    ///
    /// 字段/行为约定（媒体：sign 与未登录两个取法返回同一个形状，夹具见 plan/tests/c2）
    ///
    /// | 线上路径          | 线上类型     | 归一化                       | 缺失/null 时                                        |
    /// | ----------------- | ------------ | ---------------------------- | ---------------------------------------------------- |
    /// | code              | int          | 业务码，非 0 = Rejected      | 缺失/非数字 = 信封无效                               |
    /// | data              | object       | 载荷                         | 缺失 = 信封无效                                      |
    /// | data.media_list   | array        | 这一页的行，顺序原样         | 缺失 = Payload，定位 data.media_list（不是空页）     |
    /// | data.media_list   | 其它类型     | —                            | Payload，定位 data.media_list[]                      |
    /// | data.media_list[] | object       | 一条资源；元素为 null 时丢弃 | 元素为 null 直接跳过                                 |
    /// | data.has_more     | bool         | 还有下一页                   | 缺失/null = false（今天就是这样：没报就当到头）       |
    /// | data.has_more     | 其它类型     | —                            | Payload，定位 data.has_more                          |
    /// | data.total_count  | int          | 服务端报的总数               | 缺失/null = 未知；0 也当未知（与 HasKnownTotal 一致） |
    /// | data.total_count  | 其它类型     | —                            | Payload，定位 data.total_count                       |
    ///
    /// 1. media_list 缺失是失败，不是空页：签名接口正是靠这一条换未登录接口，
    ///    把"没有列表"折成空数组会把重试吞掉（改造前 MediaListService 也已区分这两者）。
    ///    定位字符串由 <see cref="NoMediaListStage"/> 给出，Services 的比较对象就是它。
    /// 2. media_list 存在但是空数组是合法的空页：Success + 0 行。
    /// 3. 客户端形态问题（元素缺 id、id 重复）不在这里判：去重、ID &gt; 0 的过滤仍在
    ///    MediaListPagingState.Append，那一层不动。
    /// </summary>
    public static class MediaListResourceListParser
    {
        /// <summary>响应里没有 media_list 时的失败定位；服务层据此判断要不要换未登录接口</summary>
        public const string NoMediaListStage = "data.media_list";

        /// <summary>media_list 不是数组时的失败定位（形状错误，不值得换源）</summary>
        public const string MediaListShapeStage = "data.media_list[]";

        public static ApiParseResult<MediaListResourceListDto> Parse(HttpResults response)
        {
            //传输、JSON、业务码、载荷对象这一层和别的 data 端点一样
            var envelope = ApiEnvelope.Read(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<MediaListResourceListDto>.FromFailure(envelope);
            }
            var data = envelope.Value;

            var listToken = data["media_list"];
            if (listToken == null || listToken.Type == JTokenType.Null)
            {
                return ApiParseResult<MediaListResourceListDto>.Fail(ApiParseFailure.Payload, NoMediaListStage,
                    "响应里没有 media_list");
            }
            if (listToken.Type != JTokenType.Array)
            {
                return ApiParseResult<MediaListResourceListDto>.Fail(ApiParseFailure.Payload, MediaListShapeStage,
                    "media_list 不是数组（实际是 " + listToken.Type + "）");
            }

            //两个翻页字段先单独查形状，失败时能报到具体字段
            var hasMoreToken = data["has_more"];
            if (hasMoreToken != null && hasMoreToken.Type != JTokenType.Null
                && hasMoreToken.Type != JTokenType.Boolean)
            {
                return ApiParseResult<MediaListResourceListDto>.Fail(ApiParseFailure.Payload, "data.has_more",
                    "has_more 不是布尔（实际是 " + hasMoreToken.Type + "）");
            }
            var totalToken = data["total_count"];
            if (totalToken != null && totalToken.Type != JTokenType.Null
                && totalToken.Type != JTokenType.Integer)
            {
                return ApiParseResult<MediaListResourceListDto>.Fail(ApiParseFailure.Payload, "data.total_count",
                    "total_count 不是整数（实际是 " + totalToken.Type + "）");
            }

            MediaListResourceListDto page;
            try
            {
                page = data.ToObject<MediaListResourceListDto>();
            }
            catch (Exception ex)
            {
                return ApiParseResult<MediaListResourceListDto>.Fail(ApiParseFailure.Payload, "data",
                    "data 无法解码：" + ex.Message);
            }
            if (page == null)
            {
                return ApiParseResult<MediaListResourceListDto>.Fail(ApiParseFailure.Payload, "data", "data 解码为空");
            }
            return ApiParseResult<MediaListResourceListDto>.Success(page);
        }
    }
}

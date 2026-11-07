using System;
using BiliLite.Helpers;
using BiliLite.Models.Api.MediaList;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.MediaList
{
    /// <summary>
    /// /x/v2/history/toview 的解析：data 信封、data.list 里的整份稍后再看。
    /// 整份一次返回（ps=100），没有游标、没有 has_more；不发布、不弹提示、不记日志。
    ///
    /// 字段/行为约定（媒体：改造前的解析是 GetJson&lt;ApiDataModel&lt;JObject&gt;&gt; 再
    /// data["list"] 二次反序列化，见 plan/tests/c2 夹具）
    ///
    /// | 线上路径   | 线上类型 | 归一化               | 缺失/null 时                    |
    /// | ---------- | -------- | -------------------- | -------------------------------- |
    /// | code       | int      | 业务码，非 0 = Rejected | 缺失/非数字 = 信封无效        |
    /// | data       | object   | 载荷                 | 缺失 = 信封无效                  |
    /// | data.list  | array    | 整份列表，顺序原样   | 缺失/null = 空表（没有视频不是错误） |
    /// | data.list  | 其它类型 | —                    | Payload，定位 data.list          |
    ///
    /// 1. 缺失/null 的空表与"真空表"结论相同：改造前 `data["list"]` 取不到会抛，
    ///    再被 catch 折成一句通用提示；那一句对空稍后再看是错话，这里明确成空表。
    /// 2. 这一路只有一页：没有游标，翻页规则不在本层。
    /// </summary>
    public static class WatchLaterListParser
    {
        public static ApiParseResult<WatchLaterListDto> Parse(HttpResults response)
        {
            var envelope = ApiEnvelope.Read(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<WatchLaterListDto>.FromFailure(envelope);
            }
            var data = envelope.Value;

            var listToken = data["list"];
            if (listToken != null && listToken.Type != JTokenType.Null
                && listToken.Type != JTokenType.Array)
            {
                return ApiParseResult<WatchLaterListDto>.Fail(ApiParseFailure.Payload, "data.list",
                    "list 不是数组（实际是 " + listToken.Type + "）");
            }

            WatchLaterListDto page;
            try
            {
                page = data.ToObject<WatchLaterListDto>();
            }
            catch (Exception ex)
            {
                return ApiParseResult<WatchLaterListDto>.Fail(ApiParseFailure.Payload, "data",
                    "data 无法解码：" + ex.Message);
            }
            if (page == null)
            {
                return ApiParseResult<WatchLaterListDto>.Fail(ApiParseFailure.Payload, "data", "data 解码为空");
            }
            return ApiParseResult<WatchLaterListDto>.Success(page);
        }
    }
}

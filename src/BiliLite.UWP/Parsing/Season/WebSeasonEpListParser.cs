using System;
using System.Collections.Generic;
using BiliLite.Helpers;
using BiliLite.Models.Api.Season.Web;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.Season
{
    /// <summary>
    /// pgc/view/web/ep/list 的解析：result 信封、字段形状。不判断要不要换源，不发布任何东西，不弹提示。
    ///
    /// 字段/行为约定（page 1 正片 + section 花絮/预告，媒体：ep/list 实际响应与 plan/tests/season1 夹具）
    ///
    /// | 线上路径                | 线上类型        | 归一化                          | 缺失/null 时              |
    /// | ----------------------- | --------------- | ------------------------------- | ------------------------- |
    /// | code                    | int             | 业务码，非 0 = Rejected         | 缺失/非数字 = 信封无效    |
    /// | result                  | object          | 载荷                            | 缺失 = 信封无效           |
    /// | result.episodes         | array | null    | List&lt;WebSeasonEpisodeDto&gt; | 视为空表（今天这里 NRE）  |
    /// | result.section          | array | null    | 按线上顺序保留                  | 视为无分区                |
    /// | result.section[].episodes | array | null  | 视为空分区                      | 视为空分区                |
    /// | result.episodes ∩ section[].episodes | 同一 ep 号两处都出现 | 只留一条：分区那条（算预告），正片那条丢弃 | 任一边缺 id/ep_id 就不去重 |
    /// | result.total            | int             | 只做展示提示                    | 按 0                     |
    /// | result.section[].id     | long            | 原宽进模型（season_id 是 int）  | 按 0                     |
    ///
    /// 本层只保证"拿到了一个形状合法的 result"：
    /// 1. total 不参与判断。今天 total==0 那段分支里只有注释，行为上从来没拦过任何东西；这里显式写死为"不拦"。
    /// 2. episodes 为 null 是合法的空片单，不是形状错误——今天它在 SeasonDetailVM 的 Where 里直接 NRE。
    /// 3. 空但合法的季（total==0 且没有 episodes/section）是 Success，由调用方发布空状态。
    /// 4. 形状不对（episodes/section 不是数组、result 里塞了解码不了的类型）是 Payload 失败，带字段路径。
    /// 5. 同片去重是与改造前不同的取舍：旧代码把分区的片 InsertRange(0, …) 进 result.episodes，两处都有时
    ///    会发布两条（正片一条 + 预告一条，正片 tab 里也留着）；这里按 ep 号只留一条，分区那条胜出，
    ///    正片那条不再出现。线上从未观测到这种响应，没有实网证据；只由 plan/tests/season1 的
    ///    DuplicateEpisodesPublishOnce 钉住（"duplicates: the section copy wins and the repeated ep is dropped"、
    ///    "duplicates: a row is classified once (预告 2,3 / 正片 1)"）。
    /// 合并顺序、同片去重、section_type → 正片/预告、IsPreview 派生都在 WebSeasonEpListMapper。
    /// </summary>
    public static class WebSeasonEpListParser
    {
        public static ApiParseResult<WebSeasonEpListDto> ParseWebSeasonEpList(HttpResults response)
        {
            //传输、JSON、业务码、载荷对象这一层和其它 web 端点一样，只有载荷字段名不同
            var envelope = ApiEnvelope.ReadResult(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<WebSeasonEpListDto>.FromFailure(envelope);
            }
            var result = envelope.Value;

            //两个数组字段先单独查形状，失败时能报到具体字段；null 是合法的空片单
            var episodesToken = result["episodes"];
            if (episodesToken != null && episodesToken.Type != JTokenType.Null
                && episodesToken.Type != JTokenType.Array)
            {
                return ApiParseResult<WebSeasonEpListDto>.Fail(ApiParseFailure.Payload, "result.episodes",
                    "episodes 不是数组（实际是 " + episodesToken.Type + "）");
            }
            var sectionToken = result["section"];
            if (sectionToken != null && sectionToken.Type != JTokenType.Null
                && sectionToken.Type != JTokenType.Array)
            {
                return ApiParseResult<WebSeasonEpListDto>.Fail(ApiParseFailure.Payload, "result.section",
                    "section 不是数组（实际是 " + sectionToken.Type + "）");
            }

            WebSeasonEpListDto page;
            try
            {
                page = result.ToObject<WebSeasonEpListDto>();
            }
            catch (Exception ex)
            {
                return ApiParseResult<WebSeasonEpListDto>.Fail(ApiParseFailure.Payload, "result",
                    "result 无法解码：" + ex.Message);
            }
            if (page == null)
            {
                return ApiParseResult<WebSeasonEpListDto>.Fail(ApiParseFailure.Payload, "result", "result 解码为空");
            }

            if (page.Episodes == null)
            {
                page.Episodes = new List<WebSeasonEpisodeDto>();
            }
            if (page.Section == null)
            {
                page.Section = new List<WebSeasonSectionDto>();
            }
            foreach (var section in page.Section)
            {
                if (section.Episodes == null)
                {
                    section.Episodes = new List<WebSeasonEpisodeDto>();
                }
            }
            return ApiParseResult<WebSeasonEpListDto>.Success(page);
        }
    }
}

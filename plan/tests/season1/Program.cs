using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Modules;
using BiliLite.Parsing;
using BiliLite.Parsing.Season;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Season1Probe
{
    /// <summary>
    /// season1: pgc/view/web/ep/list 走 Parsing/Season 这一层，SeasonDetailVM 只在解析被接受后发布。
    ///
    /// Scope: the probe compiles the real Parsing/ApiEnvelope.cs, Parsing/Season/*, the season wire DTO,
    /// Modules/Detail/SeasonDetailVM.cs and its model closure; only the REST transport (SeasonApi/ApiModel)
    /// and the app services (Utils/SettingHelper/LogHelper) are stand-ins. The four failure categories, the
    /// section merge, the 正片/预告 rule and the empty/null/total semantics are asserted on real behavior.
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static int Main()
        {
            ResultEnvelopePublishesTheSeason();
            BusinessRejectionPublishesNothing();
            MalformedEnvelopeKeepsItsFieldPath();
            TransportFailureStaysAParseableCategory();
            ValidEmptySeasonPublishesTheEmptyState();
            NullEpisodesIsAnEmptyListNotACrash();
            ZeroTotalDoesNotGateThePage();
            SectionMergeKeepsWireOrder();
            MainAndPreviewFollowTheirSource();
            DuplicateEpisodesPublishOnce();
            EpisodeStatusFeedsTheVipFlag();
            ParsePathShowsNoToast();
            NonJsonBodyIsAnEnvelopeFailure();
            RefreshFailureKeepsTheLoadedSeason();
            DeadPathsAreGone();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        /// <summary>result 信封的成功路径：字段映射、正片/预告划分、Detail.episodes 是合并后整份片单。</summary>
        private static void ResultEnvelopePublishesTheSeason()
        {
            var result = BaseResult();
            result["section"] = TwoSections();
            var vm = LoadSeason(SuccessBody(result));

            Check(SeasonApi.Calls.Count == 1 && SeasonApi.Calls[0].Id == "12345",
                "ep/list: the page asks pgc/view/web/ep/list once for the requested episode");
            Check(SeasonApi.Calls[0].Type == SeasonIdType.EpId,
                "ep/list: the caller keeps asking by ep_id (SeasonIdType.EpId)");
            Check(vm.Loaded && !vm.Loading && !vm.ShowError,
                "publish: an accepted parse sets Loaded and leaves the error panel closed");

            var detail = vm.Detail;
            Check(detail != null && detail.title == "probe title" && detail.season_title == "probe season"
                  && detail.season_id == 45886 && detail.media_id == 28229 && detail.type == 1,
                "map: the season header comes from result (title/season_title/season_id/media_id/type)");
            Check(detail.origin_name == "probe origin" && detail.evaluate == "probe evaluate"
                  && detail.alias == "probe alias" && detail.badge == "会员" && detail.show_badge,
                "map: text fields survive the mapping (origin_name/evaluate/alias/badge)");
            Check(detail.short_link == "https://b23.tv/ss45886" && detail.square_cover != null,
                "map: short_link/square_cover are carried when the endpoint sends them");
            Check(detail.up_info != null && detail.up_info.mid == 928123 && detail.up_info.uname == "probe up",
                "map: up_info keeps the long mid");
            Check(detail.areas != null && detail.areas.Count == 1 && detail.areas[0].id == "2"
                  && detail.areas[0].name == "日本" && detail.area == "日本 ",
                "map: a numeric area id is normalised to the string the index link needs");
            Check(detail.rating != null && detail.rating.count == 1234 && detail.rating.score == 9.7
                  && Math.Abs(detail.rating.score_5 - 4.85) < 0.0001 && detail.show_rating,
                "map: rating keeps count/score and the derived 5-point score");
            Check(detail.publish != null && detail.publish.is_finish == 1 && detail.publish.pub_time_show == "每周六",
                "map: publish keeps its flags and display text");
            Check(detail.stat != null && detail.stat.play == "1.2亿" && detail.stat.followers == "12.3万"
                  && detail.stat.views == 6,
                "map: display counts stay text while numeric counts stay numbers");
            Check(detail.user_status != null && detail.user_status.follow == 1
                  && detail.user_status.follow_status == 2 && detail.user_status.progress != null
                  && detail.user_status.progress.last_ep_id == 2 && detail.user_status.progress.last_time == 300,
                "map: user_status/progress drive the follow button and the resume position");
            Check(detail.seasons != null && detail.seasons.Count == 1 && detail.show_seasons == false
                  && detail.current_season != null && detail.current_season.season_id == 45886,
                "map: seasons list and current_season still resolve by season_id");
            Check(detail.payment != null && detail.show_payment == true
                  && detail.payment.dialog.title == "大会员" && detail.payment.tv_price == "20",
                "map: payment dialog keeps the 大会员 prompt");
            Check(detail.section != null && detail.section.Count == 2
                  && detail.section[0].title == "预告" && detail.section[0].id == 1234567890123L
                  && detail.section[1].title == "花絮" && detail.section[1].id == 1234567890124L,
                "map: the section list keeps wire order and the long section id");

            Check(Ids(vm.Episodes) == "1,2,3",
                "split: result.episodes are the 正片 rows in server order (" + Ids(vm.Episodes) + ")");
            Check(Ids(vm.Previews) == "21,22,31",
                "split: section[].episodes are the 预告 rows in wire order (" + Ids(vm.Previews) + ")");
            Check(Ids(detail.episodes) == "21,22,31,1,2,3",
                "merge: Detail.episodes keeps sections ahead of the main list, as the page's download list expects (" +
                Ids(detail.episodes) + ")");
            Check(detail.section[0].episodes.Count == 2 && Ids(detail.section[0].episodes) == "21,22"
                  && Ids(detail.section[1].episodes) == "31",
                "merge: each section model keeps only its own episodes");

            var first = vm.Episodes[0];
            Check(first.id == 1 && first.ep_id == 1 && first.index == "1" && first.title == "title-1"
                  && first.long_title == "long-1" && first.index_title == "index-title-1",
                "map: the episode ordinal/title/long_title the tabs render survive");
            Check(first.aid == "100001" && first.cid == "200001" && first.bvid == "BV1probe1",
                "map: numeric aid/cid become the strings playurl needs");
            Check(vm.ShowEpisodes && vm.ShowPreview && !vm.NothingPlay,
                "state: both tabs are shown when both lists are non-empty");
            Check(Utils.Toasts.Count == 0,
                "state: loading a valid season never toasts");
        }

        /// <summary>业务码非 0：Rejected 保留码与文案，VM 只开错误面板，不发布、不弹 toast。</summary>
        private static void BusinessRejectionPublishesNothing()
        {
            var rejected = WebSeasonEpListParser.ParseWebSeasonEpList(
                RestEnvelope("{\"code\":-404,\"message\":\"啥都木有\"}"));
            Check(rejected.Failure == ApiParseFailure.Rejected && rejected.BusinessCode == -404
                  && rejected.Message == "啥都木有" && rejected.Stage == "envelope.code",
                "rejection: a non-zero business code keeps its code and message");

            var msgOnly = WebSeasonEpListParser.ParseWebSeasonEpList(
                RestEnvelope("{\"code\":-10403,\"msg\":\"仅限大会员\"}"));
            Check(msgOnly.Failure == ApiParseFailure.Rejected && msgOnly.BusinessCode == -10403
                  && msgOnly.Message == "仅限大会员",
                "rejection: the msg alias is normalised to the same message slot");

            var vm = LoadSeason("{\"code\":-404,\"message\":\"啥都木有\"}");
            Check(vm.ShowError && !vm.Loaded && !vm.Loading, "rejection: the caller shows the error state");
            Check(vm.Detail == null && vm.Episodes == null && vm.Previews == null,
                "rejection: nothing is published when the parse is refused");
            Check(vm.ErrorMsg == "啥都木有",
                "rejection: the server message reaches the error panel (" + (vm.ErrorMsg ?? "null") + ")");
            Check(Utils.Toasts.Count == 0, "rejection: the parse path itself shows no toast");
            Check(LogHelper.Entries.Count == 0,
                "rejection: a refused response is not reported as an unhandled error");
        }

        /// <summary>信封/形状坏掉的每一档都带字段路径，且不发布、不当成空页。</summary>
        private static void MalformedEnvelopeKeepsItsFieldPath()
        {
            var noCode = WebSeasonEpListParser.ParseWebSeasonEpList(RestEnvelope("{\"message\":\"no code\"}"));
            Check(noCode.Failure == ApiParseFailure.Envelope && noCode.Stage == "envelope.code",
                "malformed: a body without a business code is an envelope failure");

            var nullCode = WebSeasonEpListParser.ParseWebSeasonEpList(RestEnvelope("{\"code\":null,\"result\":{}}"));
            Check(nullCode.Failure == ApiParseFailure.Envelope && nullCode.Stage == "envelope.code",
                "malformed: a null business code is not an implicit success");

            var noResult = WebSeasonEpListParser.ParseWebSeasonEpList(RestEnvelope("{\"code\":0}"));
            Check(noResult.Failure == ApiParseFailure.Envelope && noResult.Stage == "result",
                "malformed: a missing result object is an envelope failure named after the field");

            var arrayResult = WebSeasonEpListParser.ParseWebSeasonEpList(RestEnvelope("{\"code\":0,\"result\":[]}"));
            Check(arrayResult.Failure == ApiParseFailure.Payload && arrayResult.Stage == "result",
                "malformed: an array where result belongs is a payload failure");

            var stringEpisodes = WebSeasonEpListParser.ParseWebSeasonEpList(
                RestEnvelope("{\"code\":0,\"result\":{\"episodes\":\"none\"}}"));
            Check(stringEpisodes.Failure == ApiParseFailure.Payload && stringEpisodes.Stage == "result.episodes",
                "malformed: episodes that are not an array report result.episodes");

            var stringSection = WebSeasonEpListParser.ParseWebSeasonEpList(
                RestEnvelope("{\"code\":0,\"result\":{\"section\":\"none\"}}"));
            Check(stringSection.Failure == ApiParseFailure.Payload && stringSection.Stage == "result.section",
                "malformed: section that is not an array reports result.section");

            var stringSectionEpisodes = WebSeasonEpListParser.ParseWebSeasonEpList(
                RestEnvelope("{\"code\":0,\"result\":{\"section\":[{\"title\":\"预告\",\"episodes\":\"none\"}]}}"));
            Check(stringSectionEpisodes.Failure == ApiParseFailure.Payload
                  && stringSectionEpisodes.Stage == "result",
                "malformed: a section whose episodes are not an array fails decoding instead of throwing");

            var badEpisode = WebSeasonEpListParser.ParseWebSeasonEpList(
                RestEnvelope("{\"code\":0,\"result\":{\"episodes\":[{\"id\":1,\"aid\":\"not-a-number\"}]}}"));
            Check(badEpisode.Failure == ApiParseFailure.Payload && badEpisode.Stage == "result",
                "malformed: an episode field of the wrong wire type fails the page, not the app");

            var vm = LoadSeason("{\"code\":0,\"result\":{\"episodes\":\"none\"}}");
            Check(vm.ShowError && !vm.Loaded && vm.Detail == null && vm.Episodes == null,
                "malformed: the caller publishes nothing for a broken payload");
            Check(!string.IsNullOrEmpty(vm.ErrorMsg) && vm.ErrorMsg.Contains("episodes"),
                "malformed: the error text names the field (" + (vm.ErrorMsg ?? "null") + ")");
            Check(Utils.Toasts.Count == 0, "malformed: no toast from the parse path");
        }

        /// <summary>拿不到响应/HTTP 失败是传输类别，和"被业务码拒绝"分开。</summary>
        private static void TransportFailureStaysAParseableCategory()
        {
            var missing = WebSeasonEpListParser.ParseWebSeasonEpList(null);
            Check(missing.Failure == ApiParseFailure.Transport && missing.Stage == "transport",
                "transport: no response is a transport failure");

            var http = WebSeasonEpListParser.ParseWebSeasonEpList(new HttpResults
            {
                status = false,
                code = 412,
                message = "blocked",
                results = string.Empty,
            });
            Check(http.Failure == ApiParseFailure.Transport && http.HttpStatus == 412
                  && http.Message == "blocked",
                "transport: an HTTP failure keeps its status and message");

            SeasonApi.Reset();
            SeasonApi.FailNextRequest = true;
            var vm = new SeasonDetailVM();
            vm.LoadSeasonDetail("12345").GetAwaiter().GetResult();
            Check(vm.ShowError && !vm.Loaded && vm.Detail == null && vm.ErrorMsg == "NotFound",
                "transport: the caller shows the transport message and publishes nothing");
            Check(Utils.Toasts.Count == 0, "transport: the caller does not toast from the parse path");
        }

        /// <summary>空但合法的季：Detail 照样发布，两个列表为空，页面走 NothingPlay。</summary>
        private static void ValidEmptySeasonPublishesTheEmptyState()
        {
            var parsed = WebSeasonEpListParser.ParseWebSeasonEpList(RestEnvelope("{\"code\":0,\"result\":{}}"));
            Check(parsed.IsSuccess && parsed.Value.Episodes.Count == 0 && parsed.Value.Section.Count == 0
                  && parsed.Value.Total == null,
                "empty: a result without episodes/section/total is a valid empty page");

            var vm = LoadSeason("{\"code\":0,\"result\":{\"title\":\"空季\",\"total\":0}}");
            Check(!vm.ShowError && vm.Loaded && vm.Detail != null && vm.Detail.title == "空季",
                "empty: the season header is still published");
            Check(vm.Episodes != null && vm.Episodes.Count == 0 && vm.Previews != null && vm.Previews.Count == 0,
                "empty: both lists publish empty instead of null");
            Check(!vm.ShowEpisodes && !vm.ShowPreview && vm.NothingPlay,
                "empty: NothingPlay is the state the page renders");
            Check(Utils.Toasts.Count == 0, "empty: a valid empty season is not an error and shows no toast");
        }

        /// <summary>result.episodes 为 null（旧代码在这里 NRE）：按空表处理，分区照常合并。</summary>
        private static void NullEpisodesIsAnEmptyListNotACrash()
        {
            var result = new JObject
            {
                ["title"] = "只有预告",
                ["total"] = 3,
                ["episodes"] = JValue.CreateNull(),
                ["section"] = new JArray(new JObject
                {
                    ["title"] = "预告",
                    ["id"] = 1L,
                    ["type"] = 1,
                    ["episodes"] = new JArray(Episode(21), Episode(22)),
                }),
            };

            var parsed = WebSeasonEpListParser.ParseWebSeasonEpList(RestEnvelope(SuccessBody(result)));
            Check(parsed.IsSuccess && parsed.Value.Episodes.Count == 0 && parsed.Value.Section.Count == 1,
                "null episodes: a null episodes field reads as an empty main list");

            var vm = LoadSeason(SuccessBody(result));
            Check(!vm.ShowError && vm.Loaded && vm.Detail != null,
                "null episodes: the page still loads (the old path threw on this)");
            Check(vm.Episodes.Count == 0 && vm.ShowEpisodes == false,
                "null episodes: no 正片 tab content");
            Check(Ids(vm.Previews) == "21,22" && vm.ShowPreview && !vm.NothingPlay,
                "null episodes: the section rows still publish as 预告 (" + Ids(vm.Previews) + ")");
            Check(Ids(vm.Detail.episodes) == "21,22",
                "null episodes: Detail.episodes is the merged section list");
        }

        /// <summary>total == 0 不拦任何东西：旧代码那个分支里只有注释，行为上从来没拦过。</summary>
        private static void ZeroTotalDoesNotGateThePage()
        {
            var result = BaseResult();
            result["total"] = 0;

            var vm = LoadSeason(SuccessBody(result));
            Check(vm.Detail != null && vm.Detail.total == 0,
                "total 0: the total is carried as-is");
            Check(Ids(vm.Episodes) == "1,2,3" && vm.ShowEpisodes,
                "total 0: episodes are published although total is 0 (" + Ids(vm.Episodes) + ")");
            Check(!vm.NothingPlay && Utils.Toasts.Count == 0,
                "total 0: total never gates publication");
        }

        /// <summary>合并顺序：section 按线上顺序排在 result.episodes 之前。</summary>
        private static void SectionMergeKeepsWireOrder()
        {
            var result = BaseResult();
            result["section"] = new JArray(
                new JObject
                {
                    ["title"] = "预告",
                    ["id"] = 9001L,
                    ["type"] = 1,
                    ["episodes"] = new JArray(Episode(21), Episode(22)),
                },
                new JObject
                {
                    ["title"] = "花絮",
                    ["id"] = 9002L,
                    ["type"] = 2,
                    ["episodes"] = new JArray(Episode(31), Episode(32)),
                });

            var mapped = WebSeasonEpListMapper.MapWebSeasonEpList(
                WebSeasonEpListParser.ParseWebSeasonEpList(RestEnvelope(SuccessBody(result))).Value);
            Check(Ids(mapped.Detail.episodes) == "21,22,31,32,1,2,3",
                "merge: sections come first in wire order, then result.episodes (" +
                Ids(mapped.Detail.episodes) + ")");
            Check(Ids(mapped.Previews) == "21,22,31,32",
                "merge: previews keep the section order the server sent (" + Ids(mapped.Previews) + ")");
            Check(Ids(mapped.Episodes) == "1,2,3",
                "merge: the 正片 tab keeps the main list order (" + Ids(mapped.Episodes) + ")");
        }

        /// <summary>正片/预告只看来源：wire 上的 section_type 不参与，IsPreview 由 mapper 派生。</summary>
        private static void MainAndPreviewFollowTheirSource()
        {
            var result = BaseResult();
            var wireMarkedMain = Episode(1);
            wireMarkedMain["section_type"] = 1;
            var wireMarkedSection = Episode(21);
            wireMarkedSection["section_type"] = 0;
            result["episodes"] = new JArray(wireMarkedMain, Episode(2));
            result["section"] = new JArray(new JObject
            {
                ["title"] = "预告",
                ["id"] = 9100L,
                ["type"] = 1,
                ["episodes"] = new JArray(wireMarkedSection),
            });

            var mapped = WebSeasonEpListMapper.MapWebSeasonEpList(
                WebSeasonEpListParser.ParseWebSeasonEpList(RestEnvelope(SuccessBody(result))).Value);
            Check(mapped.Detail.episodes[0].id == 21 && mapped.Detail.episodes[0].IsPreview
                  && mapped.Detail.episodes[0].section_type == 1,
                "split: a section episode is 预告 even when the wire says section_type 0");
            Check(mapped.Detail.episodes[1].id == 1 && !mapped.Detail.episodes[1].IsPreview
                  && mapped.Detail.episodes[1].section_type == 0,
                "split: a main-list episode is 正片 even when the wire says section_type 1");
            Check(Ids(mapped.Episodes) == "1,2" && Ids(mapped.Previews) == "21",
                "split: the tabs are built from the source, not from the wire field");
        }

        /// <summary>同一条片在两个数组里只发布一次，且保留分区那条（预告归类优先）。</summary>
        private static void DuplicateEpisodesPublishOnce()
        {
            var result = BaseResult();
            result["episodes"] = new JArray(Episode(1), Episode(2), Episode(2));
            result["section"] = new JArray(new JObject
            {
                ["title"] = "预告",
                ["id"] = 9200L,
                ["type"] = 1,
                ["episodes"] = new JArray(Episode(2), Episode(3)),
            });

            var mapped = WebSeasonEpListMapper.MapWebSeasonEpList(
                WebSeasonEpListParser.ParseWebSeasonEpList(RestEnvelope(SuccessBody(result))).Value);
            Check(Ids(mapped.Detail.episodes) == "2,3,1",
                "duplicates: the section copy wins and the repeated ep is dropped (" +
                Ids(mapped.Detail.episodes) + ")");
            Check(Ids(mapped.Previews) == "2,3" && Ids(mapped.Episodes) == "1",
                "duplicates: a row is classified once (预告 2,3 / 正片 1)");
            Check(mapped.Detail.episodes.Count == 3,
                "duplicates: three distinct episodes publish once each");

            var anonymous = new JObject
            {
                ["title"] = "无 ep 号",
                ["episodes"] = new JArray(
                    new JObject { ["title"] = "a", ["aid"] = 11L },
                    new JObject { ["title"] = "b", ["aid"] = 12L }),
            };
            var anonymousMapped = WebSeasonEpListMapper.MapWebSeasonEpList(
                WebSeasonEpListParser.ParseWebSeasonEpList(RestEnvelope(SuccessBody(anonymous))).Value);
            Check(anonymousMapped.Detail.episodes.Count == 2 && anonymousMapped.Episodes.Count == 2,
                "duplicates: episodes without an ep number are never merged together");
        }

        /// <summary>
        /// 页面用 item.status != 2 判 is_vip。模型的 status getter 会在 _status 与 episode_status 都为空时抛，
        /// 所以映射必须一次定死，不能把"没状态"留给页面去踩。
        /// </summary>
        private static void EpisodeStatusFeedsTheVipFlag()
        {
            var vip = Episode(2);
            vip["status"] = 1;
            var result = new JObject
            {
                ["title"] = "状态",
                ["episodes"] = new JArray(
                    Episode(1),
                    vip,
                    new JObject { ["id"] = 3, ["title"] = "无状态" }),
            };

            var mapped = WebSeasonEpListMapper.MapWebSeasonEpList(
                WebSeasonEpListParser.ParseWebSeasonEpList(RestEnvelope(SuccessBody(result))).Value);
            Check(mapped.Episodes[0].status == 2,
                "status: an episode without status falls back to episode_status (the 已上线 value the page compares)");
            Check(mapped.Episodes[1].status == 1,
                "status: the wire status wins when both fields are present");
            Check(mapped.Episodes[2].status == 0,
                "status: an episode without either field maps to 0 instead of throwing in the getter");
            Check(mapped.Episodes.Count == 3 && Ids(mapped.Episodes) == "1,2,3",
                "status: the status mapping keeps every episode (" + Ids(mapped.Episodes) + ")");
        }

        /// <summary>解析/发布路径不弹提示：只有改动作类命令才允许 toast。每个夹具各自查一次，不能只查最后一个。</summary>
        private static void ParsePathShowsNoToast()
        {
            var bodies = new[]
            {
                new KeyValuePair<string, string>("success", SuccessBody(BaseResult())),
                new KeyValuePair<string, string>("rejection", "{\"code\":-404,\"message\":\"啥都木有\"}"),
                new KeyValuePair<string, string>("malformed", "{\"code\":0,\"result\":{\"episodes\":\"none\"}}"),
                new KeyValuePair<string, string>("empty", "{\"code\":0,\"result\":{}}"),
            };

            foreach (var body in bodies)
            {
                LoadSeason(body.Value);
                Check(Utils.Toasts.Count == 0,
                    "no toast: the " + body.Key + " body stays silent (" + string.Join("|", Utils.Toasts) + ")");
                Check(LogHelper.Entries.Count == 0,
                    "no toast: the " + body.Key + " body is not logged as an unhandled error");
            }

            Check(Utils.Toasts.Count == 0,
                "no toast: success, rejection, malformed and empty all stay silent");
            Check(LogHelper.Entries.Count == 0,
                "no toast: no failure category falls through to the unhandled-error message");
        }

        /// <summary>
        /// 非 JSON 正文（纯文本 / HTML 错误页 / 空正文）：必须落到信封失败，而且不能弹 toast。
        /// 旧路径 GetJObject 在这里弹"解析json对象失败"，解析层不允许把解码失败变成用户提示。
        /// </summary>
        private static void NonJsonBodyIsAnEnvelopeFailure()
        {
            var bodies = new[]
            {
                new KeyValuePair<string, string>("plain text", "正在维护"),
                new KeyValuePair<string, string>("html error page", "<html><body>502</body></html>"),
                new KeyValuePair<string, string>("empty body", string.Empty),
            };

            foreach (var body in bodies)
            {
                var parsed = WebSeasonEpListParser.ParseWebSeasonEpList(RestEnvelope(body.Value));
                Check(parsed.Failure == ApiParseFailure.Envelope && parsed.Stage == "envelope.json",
                    "non-json (" + body.Key + "): the body is an envelope failure at envelope.json, not an empty page (" +
                    parsed.Failure + "/" + (parsed.Stage ?? "null") + ")");
            }

            foreach (var body in bodies)
            {
                var vm = LoadSeason(body.Value);
                Check(vm.ShowError && !vm.Loaded && vm.Detail == null && vm.Episodes == null
                      && vm.Previews == null,
                    "non-json (" + body.Key + "): the caller publishes nothing and opens the error panel");
                Check(vm.ErrorMsg == "响应正文不是 JSON 对象",
                    "non-json (" + body.Key + "): the error panel says the body was not JSON (" +
                    (vm.ErrorMsg ?? "null") + ")");
                Check(Utils.Toasts.Count == 0,
                    "non-json (" + body.Key + "): a non-JSON body shows no toast (" +
                    string.Join("|", Utils.Toasts) + ")");
                Check(LogHelper.Entries.Count == 0,
                    "non-json (" + body.Key + "): a non-JSON body is not reported as an unhandled error");
            }
        }

        /// <summary>
        /// 刷新失败保留上一次成功发布的片单，错误面板反映这一次尝试。这是改造前就有的行为（可选项），
        /// 探针把它钉住，免得以后重写加载路径时悄悄清空页面正在渲染的数据。
        /// </summary>
        private static void RefreshFailureKeepsTheLoadedSeason()
        {
            SeasonApi.Reset();
            Utils.Toasts.Clear();
            LogHelper.Entries.Clear();
            var attempt = 0;
            SeasonApi.Fixture = call => attempt++ == 0
                ? SuccessBody(BaseResult())
                : "{\"code\":-404,\"message\":\"刷新失败\"}";

            var vm = new SeasonDetailVM();
            vm.LoadSeasonDetail("12345").GetAwaiter().GetResult();
            Check(vm.Loaded && !vm.ShowError && Ids(vm.Episodes) == "1,2,3",
                "refresh: the first load publishes the season (" + Ids(vm.Episodes) + ")");
            var detail = vm.Detail;
            var episodes = vm.Episodes;
            var previews = vm.Previews;

            vm.LoadSeasonDetail("12345").GetAwaiter().GetResult();
            Check(SeasonApi.Calls.Count == 2, "refresh: the second load asks the endpoint again");
            Check(vm.ShowError && !vm.Loaded && vm.ErrorMsg == "刷新失败",
                "refresh: a refused reload opens the error panel with the server message");
            Check(ReferenceEquals(vm.Detail, detail) && ReferenceEquals(vm.Episodes, episodes)
                  && ReferenceEquals(vm.Previews, previews) && Ids(vm.Episodes) == "1,2,3"
                  && Ids(vm.Previews) == Ids(previews),
                "refresh: the already published season survives the failed reload");
            Check(Utils.Toasts.Count == 0, "refresh: a failed reload still shows no toast");
        }

        /// <summary>这一批删掉的死路不能被悄悄复活。</summary>
        private static void DeadPathsAreGone()
        {
            var root = RepoRoot();
            var seasonApi = File.ReadAllText(Path.Combine(root, "src", "BiliLite.UWP", "Api", "SeasonApi.cs"));
            Check(!seasonApi.Contains("DetailWeb") && !seasonApi.Contains("Detail_Web")
                  && !seasonApi.Contains("pgc/view/v2/app/season"),
                "dead paths: SeasonApi.Detail/Detail_Web/DetailWeb are gone");
            Check(seasonApi.Contains("/pgc/view/web/ep/list"),
                "dead paths: the one live endpoint is still pgc/view/web/ep/list");

            var vm = File.ReadAllText(Path.Combine(root, "src", "BiliLite.UWP", "Modules", "Detail",
                "SeasonDetailVM.cs"));
            Check(!vm.Contains("GetJson") && !vm.Contains("GetJObject") && !vm.Contains("SeasonDetailModel_Web")
                  && !vm.Contains("GetWebSeasonDetail") && !vm.Contains("InsertRange"),
                "dead paths: the VM no longer decodes responses, merges sections or reaches the web fallback");
            Check(vm.Contains("ParseWebSeasonEpList") && vm.Contains("MapWebSeasonEpList"),
                "dead paths: the VM goes through the parser and the mapper");

            var parser = File.ReadAllText(Path.Combine(root, "src", "BiliLite.UWP", "Parsing", "Season",
                "WebSeasonEpListParser.cs"));
            var mapper = File.ReadAllText(Path.Combine(root, "src", "BiliLite.UWP", "Parsing", "Season",
                "WebSeasonEpListMapper.cs"));
            Check(!parser.Contains("Toast") && !mapper.Contains("Toast")
                  && !parser.Contains("ShowMessage") && !mapper.Contains("ShowMessage"),
                "boundaries: the parse layer shows no user message");
            Check(!parser.Contains("Api/") && !parser.Contains("HttpClient") && !parser.Contains(".Request("),
                "boundaries: the parse layer performs no network work");

            var csproj = File.ReadAllText(Path.Combine(root, "src", "BiliLite.UWP", "BiliLite.UWP.csproj"));
            Check(csproj.Contains("Parsing\\Season\\WebSeasonEpListParser.cs")
                  && csproj.Contains("Parsing\\Season\\WebSeasonEpListMapper.cs")
                  && csproj.Contains("Models\\Api\\Season\\Web\\WebSeasonEpListDto.cs"),
                "boundaries: the new parsing and DTO files are part of the app project");
        }

        private static SeasonDetailVM LoadSeason(string body)
        {
            SeasonApi.Reset();
            Utils.Toasts.Clear();
            LogHelper.Entries.Clear();
            SeasonApi.Fixture = call => body;
            var vm = new SeasonDetailVM();
            vm.LoadSeasonDetail("12345").GetAwaiter().GetResult();
            return vm;
        }

        private static string SuccessBody(JObject result)
        {
            return new JObject
            {
                ["code"] = 0,
                ["message"] = "success",
                ["result"] = result,
            }.ToString(Formatting.None);
        }

        private static HttpResults RestEnvelope(string json)
        {
            return new HttpResults
            {
                status = true,
                code = 200,
                message = "0",
                results = json,
            };
        }

        /// <summary>ep/list 的 result：与线上同形的季信息，测试各自替换 episodes/section/total。</summary>
        private static JObject BaseResult()
        {
            return new JObject
            {
                ["season_id"] = 45886,
                ["season_title"] = "probe season",
                ["title"] = "probe title",
                ["origin_name"] = "probe origin",
                ["cover"] = "http://i0.hdslb.com/bfs/bangumi/cover.jpg",
                ["square_cover"] = "http://i0.hdslb.com/bfs/bangumi/square.jpg",
                ["evaluate"] = "probe evaluate",
                ["alias"] = "probe alias",
                ["badge"] = "会员",
                ["status"] = 13,
                ["subtitle"] = "probe subtitle",
                ["link"] = "https://www.bilibili.com/bangumi/play/ss45886",
                ["short_link"] = "https://b23.tv/ss45886",
                ["media_id"] = 28229,
                ["mode"] = 1,
                ["type"] = 1,
                ["total"] = 3,
                ["up_info"] = new JObject { ["mid"] = 928123, ["uname"] = "probe up" },
                ["areas"] = new JArray(new JObject { ["id"] = 2, ["name"] = "日本" }),
                ["episodes"] = new JArray(Episode(1), Episode(2), Episode(3)),
                ["seasons"] = new JArray(new JObject
                {
                    ["season_id"] = 45886,
                    ["season_title"] = "probe season",
                    ["title"] = "probe title",
                    ["cover"] = "http://i0.hdslb.com/bfs/bangumi/season.jpg",
                }),
                ["rating"] = new JObject { ["count"] = 1234, ["score"] = 9.7 },
                ["publish"] = new JObject
                {
                    ["is_finish"] = 1,
                    ["is_started"] = 1,
                    ["weekday"] = 6,
                    ["pub_time"] = "20200101",
                    ["pub_time_show"] = "每周六",
                    ["release_date_show"] = "2020",
                    ["time_length_show"] = "24分钟",
                },
                ["stat"] = new JObject
                {
                    ["coins"] = 1,
                    ["danmakus"] = 2,
                    ["favorites"] = 3,
                    ["followers"] = "12.3万",
                    ["play"] = "1.2亿",
                    ["reply"] = 4,
                    ["share"] = 5,
                    ["views"] = 6,
                },
                ["user_status"] = new JObject
                {
                    ["follow"] = 1,
                    ["follow_bubble"] = 0,
                    ["follow_status"] = 2,
                    ["pay"] = 1,
                    ["vip"] = 0,
                    ["sponsor"] = 0,
                    ["vip_frozen"] = 0,
                    ["progress"] = new JObject
                    {
                        ["last_ep_index"] = "第2话",
                        ["last_ep_id"] = 2,
                        ["last_time"] = 300,
                    },
                },
                ["payment"] = new JObject
                {
                    ["price"] = "10",
                    ["tv_price"] = "20",
                    ["dialog"] = new JObject { ["title"] = "大会员", ["desc"] = "开通" },
                },
            };
        }

        private static JArray TwoSections()
        {
            return new JArray(
                new JObject
                {
                    ["title"] = "预告",
                    ["id"] = 1234567890123L,
                    ["type"] = 1,
                    ["episodes"] = new JArray(Episode(21), Episode(22)),
                },
                new JObject
                {
                    ["title"] = "花絮",
                    ["id"] = 1234567890124L,
                    ["type"] = 2,
                    ["episodes"] = new JArray(Episode(31)),
                });
        }

        private static JObject Episode(int epId)
        {
            return new JObject
            {
                ["aid"] = 100000L + epId,
                ["bvid"] = "BV1probe" + epId,
                ["cid"] = 200000L + epId,
                ["id"] = epId,
                ["ep_id"] = epId,
                ["title"] = "title-" + epId,
                ["long_title"] = "long-" + epId,
                ["index"] = epId.ToString(),
                ["index_title"] = "index-title-" + epId,
                ["cover"] = "http://i0.hdslb.com/bfs/archive/" + epId + ".jpg",
                ["badge"] = "",
                ["badge_type"] = 0,
                ["episode_status"] = 2,
            };
        }

        private static string Ids(List<SeasonDetailEpisodeModel> episodes)
        {
            return episodes == null ? "null" : string.Join(",", episodes.Select(x => x.id));
        }

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "src", "BiliLite.UWP", "BiliLite.UWP.csproj")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("repository root not found from " + AppContext.BaseDirectory);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
            {
                Console.Error.WriteLine("FAIL " + message);
                Environment.Exit(1);
            }
            _passed++;
            Console.WriteLine("ok " + message);
        }
    }
}

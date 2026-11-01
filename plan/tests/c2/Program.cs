using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Models.Api.MediaList;
using BiliLite.Models.Common;
using BiliLite.Parsing;
using BiliLite.Parsing.MediaList;
using BiliLite.Services;

namespace C2Probe
{
    /// <summary>
    /// No network: every request the real MediaListService issues is recorded here and answered
    /// from a queue of canned /x/v2/medialist/resource/list payloads.
    /// </summary>
    public static class FakeTransport
    {
        public sealed class Call
        {
            public string Kind { get; set; }
            public string MediaListId { get; set; }
            public string LastAid { get; set; }
            public int TypeId { get; set; }
            public int PageSize { get; set; }
        }

        public static readonly List<Call> Calls = new List<Call>();
        private static readonly Queue<string> SignedResponses = new Queue<string>();
        private static readonly Queue<string> UnsignedResponses = new Queue<string>();

        public static readonly List<string> Toasts = new List<string>();
        public static readonly List<string> Logs = new List<string>();

        public static Exception NextThrow;

        public static void Reset()
        {
            Calls.Clear();
            SignedResponses.Clear();
            UnsignedResponses.Clear();
            Toasts.Clear();
            Logs.Clear();
            NextThrow = null;
        }

        public static void Enqueue(string kind, string json)
        {
            if (kind == "signed") SignedResponses.Enqueue(json);
            else UnsignedResponses.Enqueue(json);
        }

        public static ApiModel Describe(string kind, string mediaListId, string lastAid, int typeId, int pageSize)
        {
            return new ApiModel()
            {
                Kind = kind,
                MediaListId = mediaListId,
                LastAid = lastAid,
                TypeId = typeId,
                PageSize = pageSize,
                baseUrl = "https://api.bilibili.com/x/v2/medialist/resource/list",
            };
        }

        public static Task<HttpResults> Handle(ApiModel api)
        {
            Calls.Add(new Call()
            {
                Kind = api.Kind,
                MediaListId = api.MediaListId,
                LastAid = api.LastAid,
                TypeId = api.TypeId,
                PageSize = api.PageSize,
            });

            if (NextThrow != null)
            {
                var ex = NextThrow;
                NextThrow = null;
                throw ex;
            }

            var queue = api.Kind == "signed" ? SignedResponses : UnsignedResponses;
            if (queue.Count == 0) return Task.FromResult<HttpResults>(null);
            return Task.FromResult(new HttpResults() { results = queue.Dequeue() });
        }
    }

    /// <summary>
    /// Executed against product source: the parsing layer (Parsing/ApiEnvelope.cs,
    /// Parsing/ApiParseResult.cs, Parsing/MediaList/*), Models/Api/MediaList/* DTOs,
    /// Models/MediaListResources.cs (paging state + single-flight loader) and
    /// Services/MediaListService.cs (request order, cursor, unsigned retry, failure categories).
    /// Pages/VideoDetailPage.xaml.cs cannot run outside UWP, so its wiring is read as text and
    /// every such check is named "source:".
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static async Task<int> Main()
        {
            ParsesResourceListResponse();
            ParsesItemFields();
            MissingAndEmptyMediaListAreDifferent();
            EnvelopeFailuresAreCategorised();
            PageFieldsAreShapeChecked();
            NewStateStartsEmpty();
            FirstPageFillsState();
            SecondPageAppendsWithoutDuplicates();
            HasMoreAndCountsFollowTheLatestPage();
            DuplicateOnlyPageStillAdvancesTheCursor();
            EmptyPageAndNullPageAreSafe();
            UnknownTotalStaysUnknown();
            await FirstRequestUsesAnEmptyOid();
            await UnsignedRetryOnlyWhenMediaListIsMissing();
            await LoadMoreUsesTheStateCursor();
            await FailureCategoriesReachThePublisher();
            await FailedPageKeepsRowsAndCursor();
            await SingleFlightSharesOneRequest();
            FavoriteResourcePageParses();
            WatchLaterListParses();
            ServiceSourceWiring();
            PageSourceWiring();
            ProjectEntries();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        private static void ParsesResourceListResponse()
        {
            var parsed = ParsePage(Page1Json);
            var page = MediaListResourceListMapper.Map(parsed.Value);

            Check(parsed.IsSuccess, "a resource list response decodes through the parser");
            Check(parsed.Value.HasMore == true && parsed.Value.TotalCount == 42,
                "the raw page keeps has_more and total_count as reported");
            Check(page.MediaList != null && page.MediaList.Count == 3, "media_list keeps every returned resource");
            Check(page.HasMore, "has_more is retained");
            Check(page.TotalCount == 42, "total_count is retained");
            Check(page.MediaList[0].Id == 1001 && page.MediaList[1].Id == 1002 && page.MediaList[2].Id == 1003,
                "source order is preserved");
        }

        private static void ParsesItemFields()
        {
            var item = MapPage(Page1Json).MediaList[0];

            Check(item.Id == 1001, "list resource id maps to the video id");
            Check(item.Title == "第一集", "item title is retained");
            Check(item.Cover == "http://i0.hdslb.com/bfs/archive/c1.jpg", "item cover is retained");
            Check(item.Upper != null && item.Upper.Name == "UP一", "item upper.name is retained");
            Check(item.Duration == 754, "item duration is kept in seconds");
            Check(MapPage(Page1Json).MediaList[2].Upper == null, "an item without upper keeps upper null");
        }

        private static void MissingAndEmptyMediaListAreDifferent()
        {
            var missing = ParsePage(NoMediaListJson);
            var empty = ParsePage(EmptyMediaListJson);

            Check(!missing.IsSuccess && missing.Failure == ApiParseFailure.Payload,
                "a response without media_list is a payload failure, not an empty page");
            Check(missing.Stage == MediaListResourceListParser.NoMediaListStage,
                "the missing list failure points at data.media_list");
            Check(empty.IsSuccess, "a present but empty media_list is not the same as missing data");
            var emptyPage = MediaListResourceListMapper.Map(empty.Value);
            Check(emptyPage.MediaList != null && emptyPage.MediaList.Count == 0,
                "an empty media_list maps to an empty row list");
            Check(!emptyPage.HasMore && emptyPage.TotalCount == 0,
                "an empty media_list keeps has_more and total_count");
        }

        private static void EnvelopeFailuresAreCategorised()
        {
            var none = MediaListResourceListParser.Parse(null);
            Check(none.Failure == ApiParseFailure.Transport && none.Stage == "transport",
                "no response at all is a transport failure");

            var offline = MediaListResourceListParser.Parse(new HttpResults()
            {
                status = false,
                message = "连接失败",
                code = 500,
            });
            Check(offline.Failure == ApiParseFailure.Transport && offline.Message == "连接失败"
                && offline.HttpStatus == 500,
                "a failed transport keeps its message and HTTP code");

            var notJson = MediaListResourceListParser.Parse(new HttpResults() { results = "<html></html>" });
            Check(notJson.Failure == ApiParseFailure.Envelope && notJson.Stage == "envelope.json",
                "a body that is not a JSON object is an envelope failure");

            var noCode = MediaListResourceListParser.Parse(new HttpResults()
            {
                results = "{\"data\":" + Page1Json + "}",
            });
            Check(noCode.Failure == ApiParseFailure.Envelope && noCode.Stage == "envelope.code",
                "a body without a business code is an envelope failure");

            var rejected = MediaListResourceListParser.Parse(new HttpResults()
            {
                results = "{\"code\":-404,\"message\":\"啥都木有\",\"data\":" + Page1Json + "}",
            });
            Check(rejected.Failure == ApiParseFailure.Rejected && rejected.BusinessCode == -404
                && rejected.Message == "啥都木有",
                "a non-zero business code is a rejection carrying its own message");

            var msgOnly = MediaListResourceListParser.Parse(new HttpResults()
            {
                results = "{\"code\":-1,\"msg\":\"拒绝\"}",
            });
            Check(msgOnly.Failure == ApiParseFailure.Rejected && msgOnly.Message == "拒绝",
                "a rejection without message reads the msg field");

            var noData = MediaListResourceListParser.Parse(new HttpResults() { results = "{\"code\":0,\"message\":\"0\"}" });
            Check(noData.Failure == ApiParseFailure.Envelope && noData.Stage == "data",
                "a body without data is an envelope failure");

            var dataNotObject = MediaListResourceListParser.Parse(new HttpResults() { results = "{\"code\":0,\"data\":[]}" });
            Check(dataNotObject.Failure == ApiParseFailure.Payload && dataNotObject.Stage == "data",
                "a data that is not an object is a payload failure");
        }

        private static void PageFieldsAreShapeChecked()
        {
            var wrongList = ParsePage("{\"media_list\":{\"id\":1}}");
            Check(wrongList.Failure == ApiParseFailure.Payload
                && wrongList.Stage == MediaListResourceListParser.MediaListShapeStage,
                "a media_list that is not an array is a shape failure, not a missing list");

            var wrongHasMore = ParsePage("{\"media_list\":[],\"has_more\":\"true\"}");
            Check(wrongHasMore.Failure == ApiParseFailure.Payload && wrongHasMore.Stage == "data.has_more",
                "a has_more that is not a boolean is a shape failure");

            var wrongTotal = ParsePage("{\"media_list\":[],\"total_count\":\"42\"}");
            Check(wrongTotal.Failure == ApiParseFailure.Payload && wrongTotal.Stage == "data.total_count",
                "a total_count that is not an integer is a shape failure");

            var absent = ParsePage("{\"media_list\":[]}");
            var absentPage = MediaListResourceListMapper.Map(absent.Value);
            Check(absent.IsSuccess && absent.Value.HasMore == null && absent.Value.TotalCount == null,
                "absent has_more and total_count stay absent, not invented");
            Check(!absentPage.HasMore && absentPage.TotalCount == 0,
                "absent paging fields map to no more pages and an unknown total");

            var nullItem = MapPage("{\"media_list\":[null," + ItemJson(1009) + "],\"has_more\":false}");
            Check(nullItem.MediaList.Count == 1 && nullItem.MediaList[0].Id == 1009,
                "a null row is dropped instead of crashing the page");
        }

        private static void NewStateStartsEmpty()
        {
            var state = new MediaListPagingState("12345");

            Check(state.LoadedCount == 0, "a new paging state has no rows");
            Check(!state.HasMore, "a new paging state does not claim more pages");
            Check(state.TotalCount == 0, "a new paging state has no total");
            Check(state.LastAid == null && state.IsFirstPage, "a new paging state is the first page");
            Check(state.NextCursor == "", "the first request uses an empty oid");
            Check(state.MediaListId == "12345" && state.MediaListType == 1, "the state keeps the medialist id and default type");
            Check(state.SourceKind == MediaListSourceKind.MediaList, "the state records the remote source kind");
            Check(state.Title == MediaListPagingState.DefaultTitle, "the default label is the generic playlist title");
            Check(new MediaListPagingState("1", 2, MediaListSourceKind.InMemoryPlaylist, "我的收藏").Title == "我的收藏",
                "a navigation supplied title wins over the generic label");
        }

        private static void FirstPageFillsState()
        {
            var state = new MediaListPagingState("12345");
            var added = state.Append(MapPage(Page1Json));

            Check(added == 3 && state.LoadedCount == 3, "the first page appends every row");
            Check(state.Rows[0].Id == 1001 && state.Rows[2].Id == 1003, "appended rows keep the response order");
            Check(state.LastAid == "1003", "the cursor is the last returned aid");
            Check(!state.IsFirstPage, "the state is no longer the first page");
            Check(state.HasMore && state.TotalCount == 42, "has_more and total_count come from the first page");
            Check(state.CountText == "3 / 42", "the counts read loaded / total");
        }

        private static void SecondPageAppendsWithoutDuplicates()
        {
            var state = new MediaListPagingState("12345");
            state.Append(MapPage(Page1Json));
            var added = state.Append(MapPage(Page2Json));

            Check(added == 2, "only the rows not already loaded are appended");
            Check(state.LoadedCount == 5, "the loaded count grows by the new rows only");
            Check(string.Join(",", IdsOf(state)) == "1001,1002,1003,1004,1005",
                "a repeated id from the next page does not duplicate a visible row");
            Check(!state.HasMore, "has_more follows the latest page");
            Check(state.CountText == "5 / 45", "the total comes from the latest page, not the first one");
        }

        private static void HasMoreAndCountsFollowTheLatestPage()
        {
            var state = new MediaListPagingState("12345");
            state.Append(MapPage(Page1Json));
            Check(state.HasMore, "has_more is true while the source has more pages");

            state.Append(MapPage(LastPageJson));
            Check(!state.HasMore, "has_more turns false on the last page");
            Check(state.LoadedCount == 3, "an empty last page does not invent rows");
        }

        private static void DuplicateOnlyPageStillAdvancesTheCursor()
        {
            var state = new MediaListPagingState("12345");
            state.Append(MapPage(Page1Json));
            var added = state.Append(MapPage(DuplicatePageJson));

            Check(added == 0, "a page of already loaded rows adds nothing");
            Check(state.LastAid == "1003", "duplicate rows never move the cursor backwards");
            Check(state.LoadedCount == 3, "duplicates leave the loaded rows alone");
        }

        private static void EmptyPageAndNullPageAreSafe()
        {
            var state = new MediaListPagingState("12345");
            state.Append(MapPage(Page1Json));
            var added = state.Append(MapPage(EmptyMediaListJson));

            Check(added == 0, "an empty but present page appends nothing");
            Check(state.LoadedCount == 3 && state.LastAid == "1003", "an empty page keeps the loaded rows and the cursor");
            Check(!state.HasMore, "an empty page ends the paging");

            var untouched = new MediaListPagingState("12345");
            Check(untouched.Append(null) == 0 && untouched.LoadedCount == 0, "a null page is ignored");
        }

        private static void UnknownTotalStaysUnknown()
        {
            var noTotal = new MediaListPagingState("12345");
            noTotal.Append(MapPage(NoTotalJson));

            Check(!noTotal.HasKnownTotal && noTotal.TotalCount == 0,
                "a page without total_count leaves the total unknown");
            Check(noTotal.CountText == "3", "an unknown total shows the loaded count alone");

            noTotal.Append(MapPage(Page2Json));
            Check(noTotal.HasKnownTotal && noTotal.TotalCount == 45,
                "a later page that reports a total makes it known");
            Check(noTotal.CountText == "5 / 45", "the known total then shows loaded / total");
        }

        private static async Task FirstRequestUsesAnEmptyOid()
        {
            FakeTransport.Reset();
            FakeTransport.Enqueue("signed", Wrapped(Page1Json));

            var service = new MediaListService();
            var page = await service.GetMediaList("12345");

            Check(FakeTransport.Calls.Count == 1, "the first page needs a single request");
            Check(FakeTransport.Calls[0].Kind == "signed", "the first page uses the signed builder");
            Check(FakeTransport.Calls[0].LastAid == "", "the first request sends an empty oid");
            Check(FakeTransport.Calls[0].MediaListId == "12345", "the first request sends the medialist as biz_id");
            Check(FakeTransport.Calls[0].TypeId == 1 && FakeTransport.Calls[0].PageSize == 20,
                "the default type and page size are used");
            Check(page.IsSuccess && page.Value.MediaList.Count == 3, "the first page is returned to the caller");
            Check(FakeTransport.Toasts.Count == 0, "a successful page is never toasted");

            FakeTransport.Reset();
            FakeTransport.Enqueue("signed", Wrapped(Page1Json));
            var typed = new MediaListService();
            await typed.GetMediaList("999", 2, 5);
            Check(FakeTransport.Calls[0].TypeId == 2 && FakeTransport.Calls[0].PageSize == 5,
                "an explicit type and page size reach the builder");
        }

        private static async Task UnsignedRetryOnlyWhenMediaListIsMissing()
        {
            FakeTransport.Reset();
            FakeTransport.Enqueue("signed", Wrapped(NoMediaListJson));
            FakeTransport.Enqueue("unsigned", Wrapped(Page1Json));

            var page = await new MediaListService().GetMediaList("12345");

            Check(FakeTransport.Calls.Count == 2, "a signed response without media_list triggers one retry");
            Check(FakeTransport.Calls[1].Kind == "unsigned", "the retry uses the unsigned builder");
            Check(FakeTransport.Calls[1].LastAid == "", "the retry keeps the same oid");
            Check(page.IsSuccess && page.Value.MediaList.Count == 3, "the unsigned response is what the caller receives");

            FakeTransport.Reset();
            FakeTransport.Enqueue("signed", Wrapped(EmptyMediaListJson));

            var empty = await new MediaListService().GetMediaList("12345");

            Check(FakeTransport.Calls.Count == 1, "an empty but present media_list does not trigger the unsigned retry");
            Check(FakeTransport.Calls[0].Kind == "signed", "the empty page is served by the signed builder");
            Check(empty.IsSuccess && empty.Value.MediaList != null && empty.Value.MediaList.Count == 0,
                "an empty page is returned as an empty list, not as missing data");
            Check(FakeTransport.Toasts.Count == 0, "an empty page is not reported as a failure");

            FakeTransport.Reset();
            FakeTransport.Enqueue("signed", RejectedJson);
            var rejected = await new MediaListService().GetMediaList("12345");
            Check(FakeTransport.Calls.Count == 1 && rejected.Failure == ApiParseFailure.Rejected,
                "a business rejection does not fall back to the unsigned builder");

            FakeTransport.Reset();
            FakeTransport.Enqueue("signed", "<html></html>");
            var malformed = await new MediaListService().GetMediaList("12345");
            Check(FakeTransport.Calls.Count == 1 && malformed.Failure == ApiParseFailure.Envelope,
                "a body that is not JSON does not fall back to the unsigned builder");

            FakeTransport.Reset();
            var unsignedOnly = new MediaListService();
            FakeTransport.Enqueue("unsigned", Wrapped(Page1Json));
            var single = await unsignedOnly.GetPageAsync(MediaListEndpointKind.Unsigned, "12345", "");
            Check(FakeTransport.Calls.Count == 1 && FakeTransport.Calls[0].Kind == "unsigned",
                "one call to GetPageAsync asks exactly one source");
            Check(single.IsSuccess, "the chosen source alone can satisfy a caller");
        }

        private static async Task LoadMoreUsesTheStateCursor()
        {
            FakeTransport.Reset();
            var state = new MediaListPagingState("12345", 2);
            FakeTransport.Enqueue("signed", Wrapped(Page1Json));
            var service = new MediaListService();
            var first = await service.GetMediaList(state.MediaListId, state.MediaListType);
            state.Append(first.Value);

            FakeTransport.Enqueue("signed", Wrapped(Page2Json));
            var page = await service.LoadMoreMediaList(state);

            Check(FakeTransport.Calls.Count == 2, "loading more issues exactly one request");
            Check(FakeTransport.Calls[1].LastAid == "1003", "loading more sends the last returned aid as oid");
            Check(FakeTransport.Calls[1].TypeId == 2, "loading more reuses the playlist type");
            Check(FakeTransport.Calls[1].MediaListId == "12345", "loading more reuses the medialist id");

            state.Append(page.Value);
            Check(state.LoadedCount == 5 && state.LastAid == "1005", "the appended page moves the cursor forward");
            Check(FakeTransport.Calls[1].LastAid != "1001",
                "the cursor is the last returned aid, never the selected row");

            FakeTransport.Reset();
            var noCall = await new MediaListService().LoadMoreMediaList(null);
            Check(!noCall.IsSuccess && noCall.Stage == MediaListFailureText.InvalidIdStage
                && FakeTransport.Calls.Count == 0,
                "loading more without state makes no request and says so");
        }

        private static async Task FailureCategoriesReachThePublisher()
        {
            FakeTransport.Reset();
            var service = new MediaListService();

            var noResponse = await service.GetMediaList("12345");
            Check(!noResponse.IsSuccess && noResponse.Failure == ApiParseFailure.Transport,
                "a request that returns nothing is a transport failure");
            Check(MediaListFailureText.For(noResponse) == MediaListFailureText.TransportText,
                "a transport failure asks the user to check the connection");
            Check(FakeTransport.Toasts.Count == 0, "the service does not toast; the publisher does");

            FakeTransport.Reset();
            FakeTransport.NextThrow = new InvalidOperationException("boom");
            var threw = await service.GetMediaList("12345");
            Check(!threw.IsSuccess && threw.Failure == ApiParseFailure.Transport
                && threw.Stage == "transport.exception", "an exception is reported as a transport failure");
            Check(FakeTransport.Calls.Count == 1, "an exception does not trigger the unsigned retry");
            Check(MediaListFailureText.For(threw) == "出现了一个未处理错误，已记录",
                "an unexpected exception reads as a handled error, not as a connection problem");

            FakeTransport.Reset();
            FakeTransport.Enqueue("signed", Wrapped(NoMediaListJson));
            FakeTransport.Enqueue("unsigned", Wrapped(NoMediaListJson));
            var missingTwice = await service.GetMediaList("12345");
            Check(!missingTwice.IsSuccess && missingTwice.Stage == MediaListResourceListParser.NoMediaListStage,
                "missing media_list on both builders stays a missing-list failure");
            Check(MediaListFailureText.For(missingTwice) == MediaListFailureText.NoMediaListText,
                "a response without any video list reads as such");
            Check(FakeTransport.Toasts.Count == 0, "the missing-list failure is left to the publisher");

            FakeTransport.Reset();
            FakeTransport.Enqueue("signed", Wrapped(EmptyMediaListJson));
            var emptyText = MediaListFailureText.For(await service.GetMediaList("12345"));
            Check(emptyText == "", "a successful page has no failure text to publish");

            FakeTransport.Reset();
            var noId = await service.GetMediaList("");
            Check(!noId.IsSuccess && noId.Stage == MediaListFailureText.InvalidIdStage
                && FakeTransport.Calls.Count == 0, "an empty medialist id makes no request");
            Check(MediaListFailureText.For(noId) == "播放列表ID无效", "an empty medialist id reads as an invalid id");

            FakeTransport.Reset();
            FakeTransport.Enqueue("signed", RejectedJson);
            var rejected = await service.GetMediaList("12345");
            Check(MediaListFailureText.For(rejected) == "啥都木有",
                "a rejection publishes the server's own message instead of a generic one");

            Check(MediaListFailureText.For<string>(null) == MediaListFailureText.GenericText,
                "a missing parse result still has a safe sentence");
        }

        private static async Task FailedPageKeepsRowsAndCursor()
        {
            var state = new MediaListPagingState("12345");
            state.Append(MapPage(Page1Json));
            Check(state.NextCursor == "1003" && state.HasMore, "the first page fills the cursor");

            var failing = new MediaListPageLoader(state, () => Task.FromResult<MediaListResources>(null));
            var landed = await failing.LoadAsync();

            Check(!landed, "a failed page is not appended");
            Check(state.LoadedCount == 3 && state.LastAid == "1003" && state.HasMore,
                "a failed append keeps the loaded rows, the last cursor and HasMore");
            Check(state.LastLoadFailed && !state.IsLoading,
                "the failure is recorded and the loading mark is cleared");

            var retry = new MediaListPageLoader(state, () => Task.FromResult(MapPage(Page2Json)));
            Check(await retry.LoadAsync(), "the retry appends the next page");
            Check(state.LoadedCount == 5 && state.LastAid == "1005" && !state.LastLoadFailed,
                "the retried page moves the cursor forward and clears the failure");
        }

        private static async Task SingleFlightSharesOneRequest()
        {
            var calls = 0;
            var state = new MediaListPagingState("12345");
            var loader = new MediaListPageLoader(state, async () =>
            {
                calls++;
                await Task.Delay(10);
                return MapPage(Page1Json);
            });

            var first = loader.LoadAsync();
            var second = loader.LoadAsync();
            var results = await Task.WhenAll(first, second);

            Check(calls == 1, "two callers share the one in-flight request");
            Check(results[0] && results[1], "both callers are told the page landed");
            Check(state.LoadedCount == 3, "the shared page is appended once");
            Check(!state.IsLoading, "the loading mark is cleared when the shared request finishes");
        }

        private static void FavoriteResourcePageParses()
        {
            var parsed = FavoriteResourceListParser.Parse(Response(Wrapped(FavoritePageJson)));

            Check(parsed.IsSuccess, "a favorite resource page decodes through the parser");
            Check(parsed.Value.Info != null && parsed.Value.Info.Title == "收藏夹A" && parsed.Value.Info.Mid == "123",
                "the favorite info keeps the title and mid as strings");
            Check(parsed.Value.Medias != null && parsed.Value.Medias.Count == 2,
                "every favorite row is kept in source order");
            Check(parsed.Value.Medias[0].Id == "1001" && parsed.Value.Medias[0].Title == "第一集"
                && parsed.Value.Medias[0].Upper.Name == "UP一",
                "a favorite row keeps its id, title and upper name");
            Check(parsed.Value.Medias[0].CntInfo != null && parsed.Value.Medias[0].CntInfo.Play == 100,
                "a favorite row keeps the wire stat block");
            Check(parsed.Value.HasMore == true, "has_more is retained for favorites");

            var noMedias = FavoriteResourceListParser.Parse(Response(Wrapped(FavoriteNoMediasJson)));
            Check(noMedias.IsSuccess && noMedias.Value.Medias == null,
                "a page without medias is a readable page, not a shape error");

            var noInfo = FavoriteResourceListParser.Parse(Response("{\"code\":0,\"data\":{\"medias\":[]}}"));
            Check(noInfo.IsSuccess && noInfo.Value.Info == null,
                "a page without info decodes with no info for the publisher to reject");

            var badMedias = FavoriteResourceListParser.Parse(Response("{\"code\":0,\"data\":{\"info\":{},\"medias\":{}}}"));
            Check(badMedias.Failure == ApiParseFailure.Payload && badMedias.Stage == "data.medias",
                "a medias that is not an array is a shape failure");

            var rejected = FavoriteResourceListParser.Parse(Response("{\"code\":-400,\"message\":\"没有收藏夹\"}"));
            Check(rejected.Failure == ApiParseFailure.Rejected && rejected.Message == "没有收藏夹",
                "a rejected favorite page carries the server message");
        }

        private static void WatchLaterListParses()
        {
            var parsed = WatchLaterListParser.Parse(Response(Wrapped(WatchLaterJson)));

            Check(parsed.IsSuccess && parsed.Value.List != null && parsed.Value.List.Count == 1,
                "the watch later list decodes through the parser");
            Check(parsed.Value.List[0].Aid == 1001 && parsed.Value.List[0].Title == "第一集",
                "a watch later row keeps the aid as a number and the title");
            Check(parsed.Value.List[0].Owner != null && parsed.Value.List[0].Owner.Name == "UP一",
                "a watch later row keeps the owner");
            Check(parsed.Value.List[0].Progress == 30 && parsed.Value.List[0].Duration == 754,
                "a watch later row keeps progress and duration in seconds");

            var noList = WatchLaterListParser.Parse(Response("{\"code\":0,\"data\":{}}"));
            Check(noList.IsSuccess && noList.Value.List == null,
                "a response without list is a readable empty page");

            var badList = WatchLaterListParser.Parse(Response("{\"code\":0,\"data\":{\"list\":{}}}"));
            Check(badList.Failure == ApiParseFailure.Payload && badList.Stage == "data.list",
                "a list that is not an array is a shape failure");

            var rejected = WatchLaterListParser.Parse(Response("{\"code\":-101,\"message\":\"未登录\"}"));
            Check(rejected.Failure == ApiParseFailure.Rejected && rejected.Message == "未登录",
                "a rejected watch later page carries the server message");
        }

        private static void ServiceSourceWiring()
        {
            var service = ReadSource("src/BiliLite.UWP/Services/MediaListService.cs");

            Check(service.Contains("return LoadPageAsync(mediaListId, \"\", mediaListType, pageSize);"),
                "source: the first page is requested with an empty oid");
            Check(service.Contains("return LoadPageAsync(state.MediaListId, state.NextCursor, state.MediaListType, pageSize);"),
                "source: loading more uses the paging state's cursor and type");
            Check(service.Contains("m_videoAPI.GetMediaList(") && service.Contains("m_videoAPI.GetMediaListNotLogin("),
                "source: the service reuses the existing signed and unsigned builders");
            Check(service.Contains("MediaListResourceListParser.Parse(response)")
                && service.Contains("MediaListResourceListMapper.Map(parsed.Value)"),
                "source: decoding is delegated to the parser and the mapper");
            Check(!service.Contains("ShowMessageToast"),
                "source: the service never toasts; the publisher owns the message");
            var catchIndex = service.IndexOf("catch (Exception ex)", StringComparison.Ordinal);
            var logIndex = service.IndexOf("LogHelper.Log(", StringComparison.Ordinal);
            Check(catchIndex > 0 && logIndex > catchIndex,
                "source: the only log is the transport exception, never a decode failure");
            Check(service.Contains("public enum MediaListEndpointKind")
                && service.Contains("MediaListEndpointKind.Unsigned"),
                "source: the two sources are named and the unsigned one is a call of its own");
            Check(service.Contains("signed.Failure == ApiParseFailure.Payload")
                && service.Contains("signed.Stage == MediaListResourceListParser.NoMediaListStage"),
                "source: the unsigned retry only follows a signed response without media_list");

            var api = ReadSource("src/BiliLite.UWP/Api/VideoAPI.cs");
            Check(api.Contains("https://api.bilibili.com/x/v2/medialist/resource/list")
                && api.Contains("&biz_id={mediaListId}&oid={lastAid}&otype=2&ps={pagesize}&direction=false&desc=true&sort_field=1&tid=0&with_current=false"),
                "source: no new endpoint was added; the existing resource/list parameters are reused");
            Check(!api.Contains("media_list"), "source: the API layer stays a request builder");
        }

        private static void PageSourceWiring()
        {
            var page = ReadSource("src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs");
            var xaml = ReadSource("src/BiliLite.UWP/Pages/VideoDetailPage.xaml");
            // 队列类型搬进了模型层,携带字段的断言跟着读新文件
            var queue = ReadSource("src/BiliLite.UWP/Models/VideoQueue.cs");

            Check(queue.Contains("public string MediaListId { get; set; }") && queue.Contains("public int MediaListType { get; set; }"),
                "source: VideoPlaylist carries the remote medialist id and type");
            Check(page.Contains("if (!string.IsNullOrEmpty(playlist.MediaListId))") && page.Contains("await InitializeMediaListPlaylist();"),
                "source: a remote playlist is loaded instead of the in-memory list");
            Check(page.Contains("new MediaListPagingState(playlist.MediaListId, playlist.MediaListType, MediaListSourceKind.MediaList, playlist.Title)"),
                "source: the page builds the paging state from the navigation source");
            Check(page.Contains("mediaListState.IsFirstPage") && page.Contains("mediaListService.LoadMoreMediaList(mediaListState)"),
                "source: the page never passes a selected row id as the cursor");
            Check(ReadSource("src/BiliLite.UWP/Models/MediaListResources.cs").Contains("m_state.Append(page);")
                && page.Contains("for (var i = mediaListRows.Count; i < mediaListState.LoadedCount; i++)"),
                "source: load more appends only the rows the state reports as new");
            Check(page.Contains("playlist.Playlist = mediaListRows;") && page.Contains("ItemsSource = mediaListRows"),
                "source: the bound list and the active playlist are the same collection");
            Check(page.Contains("mediaListLoadMoreButton.Visibility = actionable ? Visibility.Visible : Visibility.Collapsed;")
                && page.Contains("state == PlayDirectoryPagingState.More || state == PlayDirectoryPagingState.Failed"),
                "source: the Load more action is only shown while a page or a retry is available, and not while one loads");
            Check(page.Contains("mediaListCountText.Text = \"已加载 \" + mediaListState.CountText;"),
                "source: the page shows the loaded and total counts");
            Check(page.Contains("ID = item.Id.ToString(CultureInfo.InvariantCulture)") && page.Contains("Duration = FormatDuration(item.Duration),"),
                "source: rows map the resource id, author and seconds to the playlist item shape");
            Check(page.Contains("Content = mediaListListView,") && page.Contains("playlistPivotItem = element;"),
                "source: the pivot item holds the ListView so auto-next keeps working");
            Check(page.Contains("Header = new TextBlock() { Text = mediaListState.Title, FontSize = 18 }"),
                "source: the tab uses the generic playlist label unless a title is supplied");
            Check(page.Contains("Title = item.Title,") && page.Contains("Author = item.Upper?.Name,"),
                "source: rows carry title and author");
            Check(page.Contains("var message = MediaListFailureText.For(page);")
                && page.Contains("Utils.ShowMessageToast(message);"),
                "source: the page publishes the message the parse result carries");

            var fetchStart = page.IndexOf("private async Task<MediaListResources> FetchMediaListPageAsync()", StringComparison.Ordinal);
            var fetchBody = fetchStart < 0 ? "" : page.Substring(fetchStart, 700);
            Check(fetchBody.Length > 0 && fetchBody.Contains("if (page.IsSuccess) return page.Value;"),
                "source: the fetch hands the page over only on success, so a failure never appends a half page");

            Check(xaml.Contains("x:Key=\"MediaListRowTpl\""), "source: the remote playlist row template exists");
            var template = xaml.Substring(xaml.IndexOf("x:Key=\"MediaListRowTpl\"", StringComparison.Ordinal));
            template = template.Substring(0, template.IndexOf("</DataTemplate>", StringComparison.Ordinal));
            Check(template.Contains("{Binding Cover, Converter={StaticResource imageConvert2}") && template.Contains("{Binding Title}")
                && template.Contains("{Binding Author}") && template.Contains("{Binding Duration}"),
                "source: the remote row shows a converted cover, title, author and duration");
        }

        private static void ProjectEntries()
        {
            var csproj = ReadSource("src/BiliLite.UWP/BiliLite.UWP.csproj");

            Check(csproj.Contains("Models\\MediaListResources.cs"), "source: the media list model is compiled by the UWP project");
            Check(csproj.Contains("Services\\MediaListService.cs"), "source: the media list service is compiled by the UWP project");
            Check(csproj.Contains("Parsing\\Season\\WebSeasonEpListParser.cs"),
                "source: the parsing folder convention already has a compiled entry");
        }

        private static List<long> IdsOf(MediaListPagingState state)
        {
            var ids = new List<long>();
            foreach (var row in state.Rows) ids.Add(row.Id);
            return ids;
        }

        private static HttpResults Response(string body)
        {
            return new HttpResults() { results = body };
        }

        private static ApiParseResult<MediaListResourceListDto> ParsePage(string dataJson)
        {
            return MediaListResourceListParser.Parse(Response(Wrapped(dataJson)));
        }

        private static MediaListResources MapPage(string dataJson)
        {
            var parsed = ParsePage(dataJson);
            if (!parsed.IsSuccess)
            {
                throw new InvalidOperationException("fixture failed to parse: " + parsed.Stage + " " + parsed.Message);
            }
            return MediaListResourceListMapper.Map(parsed.Value);
        }

        private static string Wrapped(string dataJson)
        {
            return "{\"code\":0,\"message\":\"0\",\"data\":" + dataJson + "}";
        }

        private static string ItemJson(long id)
        {
            return "{ \"id\": " + id + ", \"title\": \"第" + id + "集\", \"duration\": 60 }";
        }

        private static string ReadSource(string relativePath)
        {
            return File.ReadAllText(Path.Combine(RepoRoot(), relativePath.Replace('\\', Path.DirectorySeparatorChar)));
        }

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "src", "BiliLite.UWP", "Models", "MediaListResources.cs")))
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

        private const string Page1Json = @"{
  ""media_list"": [
    { ""id"": 1001, ""title"": ""第一集"", ""cover"": ""http://i0.hdslb.com/bfs/archive/c1.jpg"", ""upper"": { ""name"": ""UP一"" }, ""duration"": 754 },
    { ""id"": 1002, ""title"": ""第二集"", ""cover"": ""http://i0.hdslb.com/bfs/archive/c2.jpg"", ""upper"": { ""name"": ""UP二"" }, ""duration"": 61 },
    { ""id"": 1003, ""title"": ""第三集"", ""cover"": ""http://i0.hdslb.com/bfs/archive/c3.jpg"", ""duration"": 3725 }
  ],
  ""has_more"": true,
  ""total_count"": 42
}";

        private const string Page2Json = @"{
  ""media_list"": [
    { ""id"": 1003, ""title"": ""第三集"", ""cover"": ""http://i0.hdslb.com/bfs/archive/c3.jpg"", ""duration"": 3725 },
    { ""id"": 1004, ""title"": ""第四集"", ""cover"": ""http://i0.hdslb.com/bfs/archive/c4.jpg"", ""upper"": { ""name"": ""UP四"" }, ""duration"": 10 },
    { ""id"": 1005, ""title"": ""第五集"", ""cover"": ""http://i0.hdslb.com/bfs/archive/c5.jpg"", ""upper"": { ""name"": ""UP五"" }, ""duration"": 90 }
  ],
  ""has_more"": false,
  ""total_count"": 45
}";

        private const string DuplicatePageJson = @"{
  ""media_list"": [
    { ""id"": 1002, ""title"": ""第二集"", ""cover"": ""http://i0.hdslb.com/bfs/archive/c2.jpg"", ""duration"": 61 },
    { ""id"": 1001, ""title"": ""第一集"", ""cover"": ""http://i0.hdslb.com/bfs/archive/c1.jpg"", ""duration"": 754 }
  ],
  ""has_more"": true,
  ""total_count"": 42
}";

        private const string LastPageJson = @"{
  ""media_list"": [],
  ""has_more"": false,
  ""total_count"": 3
}";

        private const string EmptyMediaListJson = @"{
  ""media_list"": [],
  ""has_more"": false,
  ""total_count"": 0
}";

        private const string NoMediaListJson = @"{
  ""has_more"": false,
  ""total_count"": 0
}";

        /// <summary>线上没报总数的一页:has_more 有、total_count 没有</summary>
        private const string NoTotalJson = @"{
  ""media_list"": [
    { ""id"": 1001, ""title"": ""第一集"", ""duration"": 754 },
    { ""id"": 1002, ""title"": ""第二集"", ""duration"": 61 },
    { ""id"": 1003, ""title"": ""第三集"", ""duration"": 3725 }
  ],
  ""has_more"": true
}";

        private const string RejectedJson = @"{""code"":-404,""message"":""啥都木有"",""data"":{}}";

        private const string FavoritePageJson = @"{
  ""info"": { ""id"": ""ml1"", ""fid"": ""ml1"", ""mid"": ""123"", ""title"": ""收藏夹A"", ""attr"": 0, ""fav_state"": 1, ""media_count"": 2, ""upper"": { ""name"": ""我"", ""mid"": ""123"" } },
  ""medias"": [
    { ""id"": ""1001"", ""title"": ""第一集"", ""cover"": ""http://i0.hdslb.com/bfs/archive/f1.jpg"", ""intro"": ""简介"", ""upper"": { ""name"": ""UP一"", ""mid"": ""9"" }, ""cnt_info"": { ""play"": 100, ""danmaku"": 2 } },
    { ""id"": ""1002"", ""title"": ""第二集"", ""cover"": ""http://i0.hdslb.com/bfs/archive/f2.jpg"", ""upper"": { ""name"": ""UP二"", ""mid"": ""10"" } }
  ],
  ""has_more"": true
}";

        private const string FavoriteNoMediasJson = @"{ ""info"": { ""id"": ""ml1"", ""title"": ""收藏夹A"" } }";

        private const string WatchLaterJson = @"{
  ""count"": 1,
  ""list"": [
    { ""aid"": 1001, ""videos"": 1, ""tname"": ""动画"", ""pic"": ""http://i0.hdslb.com/bfs/archive/w1.jpg"", ""title"": ""第一集"",
      ""desc"": """", ""dynamic"": """", ""cid"": 11, ""add_at"": 1690000000,
      ""owner"": { ""face"": ""http://i0.hdslb.com/bfs/face/u1.jpg"", ""mid"": 7, ""name"": ""UP一"" },
      ""pages"": [], ""progress"": 30, ""duration"": 754 }
  ]
}";
    }
}

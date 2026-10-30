using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Models.Common;
using BiliLite.Services;
using Newtonsoft.Json;

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
    /// Executed against product source: Models/MediaListResources.cs (parsing + paging state) and
    /// Services/MediaListService.cs (request order, cursor, unsigned retry, failure reporting).
    /// Pages/VideoDetailPage.xaml(.cs) cannot run outside UWP, so their wiring is read as text and
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
            NewStateStartsEmpty();
            FirstPageFillsState();
            SecondPageAppendsWithoutDuplicates();
            HasMoreAndCountsFollowTheLatestPage();
            DuplicateOnlyPageStillAdvancesTheCursor();
            EmptyPageAndNullPageAreSafe();
            await FirstRequestUsesAnEmptyOid();
            await UnsignedRetryOnlyWhenMediaListIsMissing();
            await LoadMoreUsesTheStateCursor();
            await FailuresAreReportedThroughTheMessagePath();
            ServiceSourceWiring();
            PageSourceWiring();
            ProjectEntries();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        private static void ParsesResourceListResponse()
        {
            var page = Parse(Page1Json);

            Check(page.MediaList != null && page.MediaList.Count == 3, "media_list keeps every returned resource");
            Check(page.HasMore, "has_more is retained");
            Check(page.TotalCount == 42, "total_count is retained");
            Check(page.MediaList[0].Id == 1001 && page.MediaList[1].Id == 1002 && page.MediaList[2].Id == 1003,
                "source order is preserved");
        }

        private static void ParsesItemFields()
        {
            var item = Parse(Page1Json).MediaList[0];

            Check(item.Id == 1001, "list resource id maps to the video id");
            Check(item.Title == "第一集", "item title is retained");
            Check(item.Cover == "http://i0.hdslb.com/bfs/archive/c1.jpg", "item cover is retained");
            Check(item.Upper != null && item.Upper.Name == "UP一", "item upper.name is retained");
            Check(item.Duration == 754, "item duration is kept in seconds");
            Check(Parse(Page1Json).MediaList[2].Upper == null, "an item without upper keeps upper null");
        }

        private static void MissingAndEmptyMediaListAreDifferent()
        {
            var missing = Parse(NoMediaListJson);
            var empty = Parse(EmptyMediaListJson);

            Check(missing.MediaList == null, "a response without media_list leaves the list null");
            Check(empty.MediaList != null, "a present but empty media_list is not the same as missing data");
            Check(empty.MediaList.Count == 0, "an empty media_list has no rows");
            Check(!empty.HasMore && empty.TotalCount == 0, "an empty media_list keeps has_more and total_count");
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
            var added = state.Append(Parse(Page1Json));

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
            state.Append(Parse(Page1Json));
            var added = state.Append(Parse(Page2Json));

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
            state.Append(Parse(Page1Json));
            Check(state.HasMore, "has_more is true while the source has more pages");

            state.Append(Parse(LastPageJson));
            Check(!state.HasMore, "has_more turns false on the last page");
            Check(state.LoadedCount == 3, "an empty last page does not invent rows");
        }

        private static void DuplicateOnlyPageStillAdvancesTheCursor()
        {
            var state = new MediaListPagingState("12345");
            state.Append(Parse(Page1Json));
            var added = state.Append(Parse(DuplicatePageJson));

            Check(added == 0, "a page of already loaded rows adds nothing");
            Check(state.LastAid == "1003", "duplicate rows never move the cursor backwards");
            Check(state.LoadedCount == 3, "duplicates leave the loaded rows alone");
        }

        private static void EmptyPageAndNullPageAreSafe()
        {
            var state = new MediaListPagingState("12345");
            state.Append(Parse(Page1Json));
            var added = state.Append(Parse(EmptyMediaListJson));

            Check(added == 0, "an empty but present page appends nothing");
            Check(state.LoadedCount == 3 && state.LastAid == "1003", "an empty page keeps the loaded rows and the cursor");
            Check(!state.HasMore, "an empty page ends the paging");

            var untouched = new MediaListPagingState("12345");
            Check(untouched.Append(null) == 0 && untouched.LoadedCount == 0, "a null page is ignored");
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
            Check(page != null && page.MediaList.Count == 3, "the first page is returned to the caller");

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
            Check(page != null && page.MediaList.Count == 3, "the unsigned response is what the caller receives");

            FakeTransport.Reset();
            FakeTransport.Enqueue("signed", Wrapped(EmptyMediaListJson));

            var empty = await new MediaListService().GetMediaList("12345");

            Check(FakeTransport.Calls.Count == 1, "an empty but present media_list does not trigger the unsigned retry");
            Check(FakeTransport.Calls[0].Kind == "signed", "the empty page is served by the signed builder");
            Check(empty != null && empty.MediaList != null && empty.MediaList.Count == 0,
                "an empty page is returned as an empty list, not as missing data");
            Check(FakeTransport.Toasts.Count == 0, "an empty page is not reported as a failure");
        }

        private static async Task LoadMoreUsesTheStateCursor()
        {
            FakeTransport.Reset();
            var state = new MediaListPagingState("12345", 2);
            FakeTransport.Enqueue("signed", Wrapped(Page1Json));
            var service = new MediaListService();
            state.Append(await service.GetMediaList(state.MediaListId, state.MediaListType));

            FakeTransport.Enqueue("signed", Wrapped(Page2Json));
            var page = await service.LoadMoreMediaList(state);

            Check(FakeTransport.Calls.Count == 2, "loading more issues exactly one request");
            Check(FakeTransport.Calls[1].LastAid == "1003", "loading more sends the last returned aid as oid");
            Check(FakeTransport.Calls[1].TypeId == 2, "loading more reuses the playlist type");
            Check(FakeTransport.Calls[1].MediaListId == "12345", "loading more reuses the medialist id");

            state.Append(page);
            Check(state.LoadedCount == 5 && state.LastAid == "1005", "the appended page moves the cursor forward");
            Check(FakeTransport.Calls[1].LastAid != "1001",
                "the cursor is the last returned aid, never the selected row");

            FakeTransport.Reset();
            var noCall = await new MediaListService().LoadMoreMediaList(null);
            Check(noCall == null && FakeTransport.Calls.Count == 0, "loading more without state makes no request");
        }

        private static async Task FailuresAreReportedThroughTheMessagePath()
        {
            FakeTransport.Reset();
            var service = new MediaListService();

            var noResponse = await service.GetMediaList("12345");
            Check(noResponse == null, "a request that returns nothing yields no page");
            Check(FakeTransport.Toasts.Count == 1, "a request failure is reported through the existing toast path");
            Check(FakeTransport.Logs.Count == 1, "a request failure is logged");

            FakeTransport.Reset();
            FakeTransport.NextThrow = new InvalidOperationException("boom");
            var threw = await service.GetMediaList("12345");
            Check(threw == null, "an exception yields no page");
            Check(FakeTransport.Toasts.Count == 1 && FakeTransport.Toasts[0].Length > 0,
                "an exception is reported through the existing toast path");
            Check(FakeTransport.Calls.Count == 1, "an exception does not trigger the unsigned retry");

            FakeTransport.Reset();
            FakeTransport.Enqueue("signed", Wrapped(NoMediaListJson));
            FakeTransport.Enqueue("unsigned", Wrapped(NoMediaListJson));
            var missingTwice = await service.GetMediaList("12345");
            Check(missingTwice == null, "missing media_list on both builders yields no page");
            Check(FakeTransport.Toasts.Count == 1 && FakeTransport.Toasts[0].Contains("没有视频列表"),
                "a response without any video list is reported as such");

            FakeTransport.Reset();
            var noId = await service.GetMediaList("");
            Check(noId == null && FakeTransport.Calls.Count == 0, "an empty medialist id makes no request");
        }

        private static void ServiceSourceWiring()
        {
            var service = ReadSource("src/BiliLite.UWP/Services/MediaListService.cs");

            Check(service.Contains("return GetMediaListPage(mediaListId, \"\", mediaListType, pageSize);"),
                "source: the first page is requested with an empty oid");
            Check(service.Contains("return GetMediaListPage(state.MediaListId, state.NextCursor, state.MediaListType, pageSize);"),
                "source: loading more uses the paging state's cursor and type");
            Check(service.Contains("m_videoAPI.GetMediaList(") && service.Contains("m_videoAPI.GetMediaListNotLogin("),
                "source: the service reuses the existing signed and unsigned builders");
            Check(service.Contains("if (!reply.Succeeded) return null;") && service.Contains("reply.Resources.MediaList != null"),
                "source: the unsigned retry only follows a signed response without media_list");
            Check(service.Contains("if (reply.Resources == null || reply.Resources.MediaList == null)"),
                "source: a response without any video list is a reported failure, not an empty page");
            Check(service.Contains("Utils.ShowMessageToast(message);") && service.Contains("LogHelper.Log("),
                "source: failures go through the existing toast and log helpers");

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

            Check(page.Contains("public string MediaListId { get; set; }") && page.Contains("public int MediaListType { get; set; }"),
                "source: VideoPlaylist carries the remote medialist id and type");
            Check(page.Contains("if (!string.IsNullOrEmpty(playlist.MediaListId))") && page.Contains("await InitializeMediaListPlaylist();"),
                "source: a remote playlist is loaded instead of the in-memory list");
            Check(page.Contains("new MediaListPagingState(playlist.MediaListId, playlist.MediaListType, MediaListSourceKind.MediaList, playlist.Title)"),
                "source: the page builds the paging state from the navigation source");
            Check(page.Contains("mediaListState.IsFirstPage") && page.Contains("mediaListService.LoadMoreMediaList(mediaListState)"),
                "source: the page never passes a selected row id as the cursor");
            Check(page.Contains("mediaListState.Append(page);") && page.Contains("for (var i = loaded; i < mediaListState.LoadedCount; i++)"),
                "source: load more appends only the rows the state reports as new");
            Check(page.Contains("playlist.Playlist = mediaListRows;") && page.Contains("ItemsSource = mediaListRows"),
                "source: the bound list and the active playlist are the same collection");
            Check(page.Contains("mediaListLoadMoreButton.Visibility = mediaListState.HasMore ? Visibility.Visible : Visibility.Collapsed;"),
                "source: the Load more action is only shown while HasMore");
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

            Check(xaml.Contains("x:Key=\"MediaListRowTpl\""), "source: the remote playlist row template exists");
            var template = xaml.Substring(xaml.IndexOf("x:Key=\"MediaListRowTpl\"", StringComparison.Ordinal));
            template = template.Substring(0, template.IndexOf("</DataTemplate>", StringComparison.Ordinal));
            Check(template.Contains("{Binding Cover}") && template.Contains("{Binding Title}")
                && template.Contains("{Binding Author}") && template.Contains("{Binding Duration}"),
                "source: the remote row shows cover, title, author and duration");
        }

        private static void ProjectEntries()
        {
            var csproj = ReadSource("src/BiliLite.UWP/BiliLite.UWP.csproj");

            Check(csproj.Contains("Models\\MediaListResources.cs"), "source: the media list model is compiled by the UWP project");
            Check(csproj.Contains("Services\\MediaListService.cs"), "source: the media list service is compiled by the UWP project");
        }

        private static List<long> IdsOf(MediaListPagingState state)
        {
            var ids = new List<long>();
            foreach (var row in state.Rows) ids.Add(row.Id);
            return ids;
        }

        private static MediaListResources Parse(string json)
        {
            return JsonConvert.DeserializeObject<MediaListResources>(json);
        }

        private static string Wrapped(string dataJson)
        {
            return "{\"code\":0,\"message\":\"0\",\"data\":" + dataJson + "}";
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
    }
}

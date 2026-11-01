using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BiliLite.Models.Common;

namespace Ux1cProbe
{
    /// <summary>
    /// Ux1c: 连播边界、续播策略和唯一一条取页通道。
    ///
    /// Executed against product source: Models/MediaListResources.cs (the paging state machine and
    /// the single-flight loader), Models/PlayContinuation.cs (the continuation policy, which moved
    /// there from MediaListResources.cs) and Models/PlayDirectory.cs (the directory's own paging
    /// state), so editing those decisions changes what the executed assertions see. The player, the page and the controls cannot run outside UWP, so their
    /// wiring is read as text; every such check is named "source:".
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static async Task<int> Main()
        {
            PagingStateOutsideAPlaylist();
            PagingStateEndsAndBegins();
            PagingStateLoadingWins();
            PagingStateFailureWinsOverHasMore();
            PagingStateReachesTheDirectory();
            await SingleFlightSharesOneRequest();
            await SingleFlightKeepsRowsOnFailure();
            ContinuationGate();
            ResumePolicy();
            PlayerSourceWiring();
            PageSourceWiring();
            DrawerSourceWiring();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        /// <summary>
        /// 固定内存队列和没有播放列表时没有翻页:抽屉什么都不摆
        /// </summary>
        private static void PagingStateOutsideAPlaylist()
        {
            Check(PlayDirectoryBuilder.PagingOf(null) == PlayDirectoryPagingState.None,
                "no paging state means the directory offers no paging");

            var fixedQueue = new VideoPlaylist() { Index = 0, Playlist = QueueRows(3), Title = "稍后再看" };
            var fixedModel = PlayDirectoryBuilder.FromPlaylist(fixedQueue, null, "5001");
            Check(fixedModel.Paging == PlayDirectoryPagingState.None,
                "a fixed in-memory queue shows no paging footer");
            Check(PlayDirectoryBuilder.Combine(null, null, fixedModel, WatchSequenceSourceKind.Playlist).Paging
                == PlayDirectoryPagingState.None,
                "combining sections keeps the fixed queue's None");
        }

        private static void PagingStateEndsAndBegins()
        {
            var state = new MediaListPagingState("ml-1");
            Check(PlayDirectoryBuilder.PagingOf(state) == PlayDirectoryPagingState.More,
                "a state that has not fetched its first page still has rows to come");

            state.Append(Page(true, 2, 3, 10));
            Check(PlayDirectoryBuilder.PagingOf(state) == PlayDirectoryPagingState.More,
                "has_more keeps the footer on 加载更多");
            Check(state.LoadedCount == 2, "the first page lands in the state");

            state.Append(Page(false, 3, 5, 12));
            Check(PlayDirectoryBuilder.PagingOf(state) == PlayDirectoryPagingState.Exhausted,
                "the last page turns the state into 已到头");
            Check(state.LoadedCount == 5 && state.HasKnownTotal && state.TotalCount == 5,
                "the exhausted state still reports the honest loaded count and total");
        }

        private static void PagingStateLoadingWins()
        {
            var state = new MediaListPagingState("ml-1");
            state.Append(Page(true, 2, 0, 1));
            state.MarkLoading();
            Check(PlayDirectoryBuilder.PagingOf(state) == PlayDirectoryPagingState.Loading,
                "a request in flight is 加载中 even while has_more is true");
            // 加载中压过失败:还在路上的请求不该先摆出"重试"
            state.MarkLoadFailed();
            Check(PlayDirectoryBuilder.PagingOf(state) == PlayDirectoryPagingState.Loading,
                "loading outranks a stale failure mark");
            state.MarkLoadFinished();
            Check(PlayDirectoryBuilder.PagingOf(state) == PlayDirectoryPagingState.Failed,
                "the finished request falls back to the recorded outcome");
            Check(!state.IsLoading, "finishing the request clears the in-flight mark");
        }

        private static void PagingStateFailureWinsOverHasMore()
        {
            var state = new MediaListPagingState("ml-1");
            state.Append(Page(true, 5, 20, 1));
            state.MarkLoading();
            state.MarkLoadFailed();
            state.MarkLoadFinished();
            Check(PlayDirectoryBuilder.PagingOf(state) == PlayDirectoryPagingState.Failed,
                "a failed page shows 重试 instead of hiding it behind has_more");
            Check(state.LoadedCount == 5 && state.HasMore,
                "a failure keeps the loaded rows and the cursor untouched");

            state.Append(Page(true, 6, 20, 6));
            Check(!state.LastLoadFailed && PlayDirectoryBuilder.PagingOf(state) == PlayDirectoryPagingState.More,
                "a later page that lands clears the failure and offers 加载更多 again");
        }

        private static void PagingStateReachesTheDirectory()
        {
            var state = new MediaListPagingState("ml-1", 1, MediaListSourceKind.MediaList, "课程");
            state.Append(Page(true, 20, 40, 1));
            var playlist = new VideoPlaylist()
            {
                Index = 0,
                Playlist = QueueRows(20),
                MediaListId = "ml-1",
                MediaListState = state,
                Title = "课程",
            };

            var model = PlayDirectoryBuilder.FromPlaylist(playlist, state, "5001");
            Check(model.Paging == PlayDirectoryPagingState.More,
                "the remote playlist hands its paging state to the directory model");

            var combined = PlayDirectoryBuilder.Combine(
                PlayDirectoryBuilder.FromParts(new List<PlayDirectoryPart>(), 0, "x", false),
                null, model, WatchSequenceSourceKind.Playlist);
            Check(combined.Paging == PlayDirectoryPagingState.More,
                "the combined directory keeps the playlist's paging state");

            // 连载来源落在合集上时,播放列表那一段照样还有页:翻页状态不跟活动来源走
            var collecting = PlayDirectoryBuilder.Combine(
                PlayDirectoryBuilder.FromUgcSeason(null, "5001"), null, model, WatchSequenceSourceKind.UgcSeason);
            Check(collecting.Paging == PlayDirectoryPagingState.More,
                "the playlist's paging survives a collection continuation source");

            state.MarkLoading();
            Check(PlayDirectoryBuilder.FromPlaylist(playlist, state, "5001").Paging == PlayDirectoryPagingState.Loading,
                "a request in flight is visible in the model, so the drawer can show it");
        }

        /// <summary>
        /// 单飞:footer 和抽屉同时点,只取一次
        /// </summary>
        private static async Task SingleFlightSharesOneRequest()
        {
            var state = new MediaListPagingState("ml-1");
            state.Append(Page(true, 2, 6, 1));
            var gate = new TaskCompletionSource<MediaListResources>();
            var responses = new Queue<Task<MediaListResources>>();
            responses.Enqueue(gate.Task);
            responses.Enqueue(Task.FromResult(Page(false, 2, 6, 5)));
            var fetches = 0;
            var loader = new MediaListPageLoader(state, () => { fetches++; return responses.Dequeue(); });

            var footer = loader.LoadAsync();
            var drawer = loader.LoadAsync();
            Check(fetches == 1, "two callers asking in the same tick start exactly one request");
            Check(state.IsLoading,
                "the in-flight request is visible to both surfaces while it runs");
            Check(PlayDirectoryBuilder.PagingOf(state) == PlayDirectoryPagingState.Loading,
                "the directory shows 加载中 for the shared request");

            gate.SetResult(Page(true, 2, 6, 3));
            Check(await footer, "the first caller sees the shared page land");
            Check(await drawer, "the second caller sees the same page, not a second request");
            Check(fetches == 1 && state.LoadedCount == 4,
                "one request, one appended page, four rows");
            Check(!state.IsLoading && PlayDirectoryBuilder.PagingOf(state) == PlayDirectoryPagingState.More,
                "the channel is free again once the page is in");

            Check(await loader.LoadAsync(), "a later request fetches the next page");
            Check(fetches == 2 && state.LoadedCount == 6 && PlayDirectoryBuilder.PagingOf(state) == PlayDirectoryPagingState.Exhausted,
                "the last page ends the list and the footer disappears");
        }

        /// <summary>
        /// 失败仍然记账、保留已加载行,并让两个入口都能重试
        /// </summary>
        private static async Task SingleFlightKeepsRowsOnFailure()
        {
            var state = new MediaListPagingState("ml-1");
            state.Append(Page(true, 2, 4, 1));
            var gate = new TaskCompletionSource<MediaListResources>();
            var responses = new Queue<Task<MediaListResources>>();
            responses.Enqueue(gate.Task);
            responses.Enqueue(Task.FromResult(Page(false, 1, 4, 3)));
            var fetches = 0;
            var loader = new MediaListPageLoader(state, () => { fetches++; return responses.Dequeue(); });

            var footer = loader.LoadAsync();
            var drawer = loader.LoadAsync();
            gate.SetResult(null);
            Check(!await footer && !await drawer,
                "both callers see the same failure instead of one retrying silently");
            Check(fetches == 1, "a failure is one request, not one per surface");
            Check(state.LoadedCount == 2 && state.LastLoadFailed,
                "a failure keeps the loaded rows and marks the state");
            Check(!state.IsLoading && PlayDirectoryBuilder.PagingOf(state) == PlayDirectoryPagingState.Failed,
                "a failed page leaves the retry state visible");

            Check(await loader.LoadAsync(), "the retry goes through the same channel");
            Check(fetches == 2 && state.LoadedCount == 3 && !state.LastLoadFailed,
                "the retry appends the row it fetched and clears the failure");
            Check(PlayDirectoryBuilder.PagingOf(state) == PlayDirectoryPagingState.Exhausted,
                "after the retry the list is honestly at its end");
        }

        private static void ContinuationGate()
        {
            Check(!PlayContinuationPolicy.AdvanceAllowed(false, false),
                "with 自动连播 off the automatic boundary does not advance at all");
            Check(PlayContinuationPolicy.AdvanceAllowed(false, true),
                "with 自动连播 on the automatic boundary advances");
            Check(PlayContinuationPolicy.AdvanceAllowed(true, false),
                "a manual next is not gated by 自动连播");
            Check(PlayContinuationPolicy.AdvanceAllowed(true, true),
                "a manual next works whatever the toggle says");
        }

        private static void ResumePolicy()
        {
            Check(!PlayContinuationPolicy.RestoresPosition(PlayTargetStart.FromStart, true),
                "a forward continuation move starts at 0 even with AUTO_TO_POSITION on");
            Check(!PlayContinuationPolicy.RestoresPosition(PlayTargetStart.FromStart, false),
                "a forward continuation move starts at 0 with AUTO_TO_POSITION off");
            Check(PlayContinuationPolicy.RestoresPosition(PlayTargetStart.Resume, true),
                "a user-picked or backward target keeps today's resume");
            Check(!PlayContinuationPolicy.RestoresPosition(PlayTargetStart.Resume, false),
                "AUTO_TO_POSITION off still starts every target at 0");
        }

        private static void PlayerSourceWiring()
        {
            var player = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml.cs");

            var setItem = Body(player, "private async Task SetPlayItem");
            Check(setItem.Contains("PlayContinuationPolicy.RestoresPosition(start,"),
                "source: the part switch asks the shared policy where to start");
            Check(CountOf(player, "PlayContinuationPolicy.RestoresPosition(") == 1,
                "source: only one place decides whether a part restores its position");
            Check(setItem.Contains("_position = 0;"),
                "source: a part that must not resume starts at 0");

            Check(Body(player, "public void ChangePlayIndex").Contains("_ = SetPlayItem(index, start);"),
                "source: the page-requested part switch carries the caller's decision");
            Check(Body(player, "public void InitializePlayInfo").Contains("_ = SetPlayItem(index, start);"),
                "source: opening a video plays the part and start the page chose");
            Check(!Body(player, "public void InitializePlayInfo").Contains("EpisodeList.SelectedIndex = index;"),
                "source: the initial part no longer starts playback through the selection event");

            var nextShortcut = Body(player, "case Windows.System.VirtualKey.X:");
            Check(nextShortcut.Contains("SetPlayItem(CurrentPlayIndex + 1, PlayTargetStart.FromStart);"),
                "source: the next shortcut starts the next part at 0");
            var previousShortcut = Body(player, "case Windows.System.VirtualKey.Z:");
            Check(previousShortcut.Contains("SetPlayItem(CurrentPlayIndex - 1, PlayTargetStart.Resume);"),
                "source: the previous shortcut resumes the previous part");
            Check(Body(player, "private async void BottomBtnNext_Click")
                    .Contains("SetPlayItem(CurrentPlayIndex + 1, PlayTargetStart.FromStart);"),
                "source: the next button starts the next part at 0");
            Check(Body(player, "private async void EpisodeList_SelectionChanged")
                    .Contains("SetPlayItem(EpisodeList.SelectedIndex, PlayTargetStart.Resume);"),
                "source: a part the user picks in the list resumes where it was left");

            var ended = Body(player, "private async void Player_PlayMediaEnded");
            Check(ended.Contains("ChangePlayIndex(CurrentPlayIndex + 1, PlayTargetStart.FromStart);"),
                "source: the part auto-advance inside the player starts the next part at 0");
            Check(ended.Contains("ChangePlayIndex(0, PlayTargetStart.FromStart);"),
                "source: the list loop restarts at 0, not at the saved position");

            Check(!player.Contains("SetPlayItem(index);") && !player.Contains("SetPlayItem(EpisodeList.SelectedIndex);")
                && !player.Contains("SetPlayItem(CurrentPlayIndex + 1);")
                && !player.Contains("SetPlayItem(CurrentPlayIndex - 1);"),
                "source: no part switch is left without an explicit start decision");
            Check(!player.Contains("ChangePlayIndex(CurrentPlayIndex + 1);") && !player.Contains("ChangePlayIndex(0);"),
                "source: no automatic part move is left without a start decision");

            Check(player.Contains("public event EventHandler DirectoryLoadMoreRequested;")
                && player.Contains("DirectoryLoadMoreRequested?.Invoke(this, e);")
                && player.Contains("DirectoryDrawer_LoadMoreRequested"),
                "source: the player forwards the drawer's page request without acting on it");
            Check(!player.Contains("LoadMediaListPage") && !player.Contains("MediaListPageLoader"),
                "source: the player never fetches a page");
        }

        private static void PageSourceWiring()
        {
            var page = ReadSource("src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs");

            Check(page.Contains("player.DirectoryLoadMoreRequested += player_DirectoryLoadMoreRequested;"),
                "source: the page subscribes to the drawer's page request");

            var advance = Body(page, "private async Task<bool> AdvanceSequenceAsync");
            Check(advance.Contains("PlayContinuationPolicy.AdvanceAllowed(manual, SettingHelper.GetValue<bool>(SettingHelper.Player.AUTO_NEXT, true))"),
                "source: the page gates the automatic path on AUTO_NEXT");
            Check(Body(page, "private async void player_AllMediaEndEvent").Contains("await AdvanceSequenceAsync(false, false);"),
                "source: the completion event is the automatic boundary");
            Check(Body(page, "private async void player_NextOuterRequested").Contains("await AdvanceSequenceAsync(false, true);")
                && Body(page, "private async void player_NextVideoRequested").Contains("await AdvanceSequenceAsync(true, true);"),
                "source: the manual next requests stay manual");
            Check(!Body(page, "private async void player_NextOuterRequested").Contains("AUTO_NEXT")
                && !Body(page, "private async void player_NextVideoRequested").Contains("AUTO_NEXT")
                && !Body(page, "private async void player_PreviousOuterRequested").Contains("AUTO_NEXT"),
                "source: manual next and previous never read the automatic toggle");

            Check(advance.Contains("player.ChangePlayIndex(step.PartIndex, PlayTargetStart.FromStart);")
                && advance.Contains("PlaySequenceRowAsync(step.RowIndex, PlayTargetStart.FromStart)"),
                "source: automatic and manual forward moves start the target at 0");
            Check(Body(page, "private async void player_PreviousOuterRequested")
                    .Contains("player.ChangePlayIndex(step.PartIndex, PlayTargetStart.Resume);")
                && Body(page, "private async void player_PreviousOuterRequested")
                    .Contains("await PlaySequenceRowAsync(step.RowIndex, PlayTargetStart.Resume);"),
                "source: previous restores the previous target's saved position");

            var activate = Body(page, "private async Task ActivateDirectoryRowAsync");
            Check(activate.Contains("player.ChangePlayIndex(rowIndex, PlayTargetStart.Resume);")
                && activate.Contains("await PlayCollectionRowAsync(rowIndex, PlayTargetStart.Resume);")
                && activate.Contains("await SelectPlaylistRowAsync(rowIndex, PlayTargetStart.Resume);"),
                "source: activating a directory row keeps today's resume");
            Check(Body(page, "private async void ListView_SelectionChanged")
                    .Contains("await InitializeVideo(item.ID, PlayTargetStart.Resume);"),
                "source: a queue row click keeps today's resume");
            Check(Body(page, "private void listEpisode_SelectionChanged")
                    .Contains("player.ChangePlayIndex(listEpisode.SelectedIndex, PlayTargetStart.Resume);"),
                "source: a part row click keeps today's resume");
            Check(Body(page, "private async void UgcSeasonEpisode_SelectionChanged")
                    .Contains("await InitializeVideo(item.Aid, PlayTargetStart.Resume);"),
                "source: a season episode switch keeps today's resume");
            Check(CountOf(page, "InitializeVideo(_id, PlayTargetStart.Resume);") == 2
                && page.Contains("await InitializeVideo(id, PlayTargetStart.Resume);"),
                "source: direct opening and refresh keep today's resume");

            var initialize = Body(page, "private async Task<bool> InitializeVideo");
            Check(initialize.Contains("if (start == PlayTargetStart.Resume && videoDetailVM.VideoInfo.history != null)"),
                "source: a continuation move does not inherit the target's saved part");
            Check(initialize.Contains("player.InitializePlayInfo(playInfos, index, start);"),
                "source: the start decision reaches the player");
            Check(!page.Contains("AUTO_TO_POSITION"),
                "source: the page has no second resume policy of its own");

            Check(CountOf(page, "mediaListService.GetMediaList(") == 1
                && CountOf(page, "mediaListService.LoadMoreMediaList(") == 1,
                "source: exactly one place builds a playlist page request");
            var fetch = Body(page, "private Task<MediaListResources> FetchMediaListPageAsync");
            Check(fetch.Contains("mediaListService.GetMediaList(") && fetch.Contains("mediaListService.LoadMoreMediaList("),
                "source: the one request builder is the channel's fetch");
            Check(CountOf(page, "new MediaListPageLoader(") == 1,
                "source: the page holds one paging channel");
            Check(CountOf(page, "mediaListLoader.LoadAsync()") == 1,
                "source: only LoadMediaListPage awaits the channel");
            Check(page.Contains("var request = LoadMediaListPage();") && CountOf(page, "await LoadMediaListPage()") == 2,
                "source: the first page, the boundary and the user's action all use that one channel");
            Check(CountOf(page, "private async void MediaListLoadMore_Click") == 1
                && CountOf(page, "private async Task RequestMediaListPageAsync") == 1
                && CountOf(page, "private async Task<bool> LoadMediaListPage") == 1,
                "source: the footer and the drawer share one request action");

            var request = Body(page, "private async Task RequestMediaListPageAsync");
            Check(!request.Contains("InitializeVideo(") && !request.Contains("ChangePlayIndex(")
                && !request.Contains("SetActiveSource(") && !request.Contains("pivot.SelectedItem"),
                "source: paging never plays, never changes the continuation source and never changes the browsed tab");
            Check(request.Contains("RedrawOpenDirectory();") && request.Contains("RefreshMediaListChrome();"),
                "source: a page fetched from the drawer refreshes the drawer and the detail chrome");

            Check(Body(page, "private PlayDirectoryModel BuildDirectoryModel")
                    .Contains("PlayDirectoryBuilder.FromPlaylist(playlist, mediaListState,"),
                "source: the drawer's paging comes from the same paging state as the detail footer");
            Check(Body(page, "private string DirectorySignature").Contains("PlayDirectoryBuilder.PagingOf(mediaListState)"),
                "source: loading, failure and the end redraw the open drawer");
            Check(Body(page, "private async Task<bool> LoadMediaListPage").Contains("if (landed) AppendMediaListRows();"),
                "source: new rows land in the bound collection, so the open drawer gains them without closing");
        }

        private static void DrawerSourceWiring()
        {
            var control = ReadSource("src/BiliLite.UWP/Controls/PlayDirectoryControl.xaml.cs");
            var xaml = ReadSource("src/BiliLite.UWP/Controls/PlayDirectoryControl.xaml");
            var pageXaml = ReadSource("src/BiliLite.UWP/Pages/VideoDetailPage.xaml");

            Check(control.Contains("public event EventHandler LoadMoreRequested;")
                && xaml.Contains("Click=\"LoadMoreButton_Click\""),
                "source: the drawer's footer raises one request instead of fetching");

            var rowList = xaml.IndexOf("x:Name=\"RowList\"", StringComparison.Ordinal);
            var footer = xaml.IndexOf("x:Name=\"PagingHost\"", StringComparison.Ordinal);
            Check(footer > 0 && rowList > 0 && rowList < footer,
                "source: the paging footer sits under the row list, inside the drawer");
            Check(ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml")
                    .Contains("<local:PlayDirectoryControl x:Name=\"DirectoryDrawer\""),
                "source: the drawer lives inside the player, so its footer survives full screen");
            Check(!pageXaml.Contains("PagingHost") && !pageXaml.Contains("PagingRing")
                && !pageXaml.Contains("加载更多") && !pageXaml.Contains("重试"),
                "source: the drawer footer is not in the detail column");
            Check(pageXaml.Length > 0 && ReadSource("src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs")
                    .Contains("RightInfo.Width = new GridLength(0, GridUnitType.Pixel);"),
                "source: full screen and full window collapse the detail column");

            var footerMarkup = Between(xaml, "x:Name=\"PagingHost\"", "</StackPanel>");
            Check(CountOf(footerMarkup, "<TextBlock") == 1 && CountOf(footerMarkup, "<ProgressRing") == 1,
                "source: the footer is one label and one ring, nothing else");
            Check(!footerMarkup.Contains("（") && !footerMarkup.Contains("·") && !footerMarkup.Contains("："),
                "source: the footer copy carries no explanatory suffix");

            Check(control.Contains("state == PlayDirectoryPagingState.Failed ? \"重试\" : \"加载更多\""),
                "source: the failed state reads 重试, the more state reads 加载更多");
            Check(xaml.Contains("Text=\"加载更多\""),
                "source: the more state's label is exactly 加载更多");
            Check(!control.Contains("加载更多（") && !control.Contains("重试（") && !control.Contains("·"),
                "source: the paging labels do not explain themselves");
            Check(control.Contains("PagingRing.IsActive = owned && loading;"),
                "source: the ring only spins while a request is actually running");
            Check(control.Contains("state != PlayDirectoryPagingState.None")
                && control.Contains("m_activeKind == PlayDirectorySectionKind.Playlist"),
                "source: only a remote playlist section exposes the footer");
            Check(!control.Contains("MediaListService") && !control.Contains("GetMediaList")
                && !control.Contains("LoadMoreMediaList"),
                "source: the drawer never fetches the page it asks for");
            Check(control.Contains("AutomationProperties.SetName(LoadMoreButton, text);"),
                "source: the footer button's accessible name is the label it shows");
        }

        /// <summary>
        /// 一页响应:count 条(从 firstId 起),has_more 和已知总数由参数决定
        /// </summary>
        private static MediaListResources Page(bool hasMore, int count, int totalCount, int firstId)
        {
            var page = new MediaListResources()
            {
                MediaList = new List<MediaListItem>(),
                HasMore = hasMore,
                TotalCount = totalCount,
            };
            for (var i = 0; i < count; i++)
            {
                page.MediaList.Add(new MediaListItem() { Id = firstId + i, Title = "第" + (firstId + i) + "讲", Duration = 600 });
            }
            return page;
        }

        private static List<VideoPlaylistItem> QueueRows(int count)
        {
            var rows = new List<VideoPlaylistItem>();
            for (var i = 1; i <= count; i++)
            {
                rows.Add(new VideoPlaylistItem() { ID = (5000 + i).ToString(), Title = "第" + i + "讲" });
            }
            return rows;
        }

        private static string Between(string source, string from, string to)
        {
            var start = source.IndexOf(from, StringComparison.Ordinal);
            if (start < 0) return "";
            var end = source.IndexOf(to, start, StringComparison.Ordinal);
            return end < 0 ? source.Substring(start) : source.Substring(start, end - start);
        }

        private static string Body(string source, string signature)
        {
            var start = source.IndexOf(signature, StringComparison.Ordinal);
            if (start < 0) return "";
            // InitializeVideo 比别的方法长,窗口放宽到 8000:切到的是方法自己的收尾括号
            var length = Math.Min(8000, source.Length - start);
            var body = source.Substring(start, length);
            var end = body.IndexOf("\n        }\n", StringComparison.Ordinal);
            return end < 0 ? body : body.Substring(0, end);
        }

        private static int CountOf(string source, string text)
        {
            var count = 0;
            var index = 0;
            while ((index = source.IndexOf(text, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += text.Length;
            }
            return count;
        }

        private static string ReadSource(string relativePath)
        {
            // 文本断言按 \n 切方法体:先把 CRLF 归一,免得 Body 拿到整段文件
            var text = File.ReadAllText(Path.Combine(RepoRoot(), relativePath.Replace('\\', Path.DirectorySeparatorChar)));
            return text.Replace("\r\n", "\n");
        }

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "src", "BiliLite.UWP", "Models", "PlayDirectory.cs")))
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

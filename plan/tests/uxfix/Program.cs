using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using BiliLite.Models.Common;

namespace UxfixProbe
{
    /// <summary>
    /// Uxfix: BR-32 断在取页上的那一步只对它自己那个来源算数;BR-33 关页之后的播放队列不留给下一个视频。
    ///
    /// Executed against product source: Models/PlayContinuation.cs (the record invalidation rule and
    /// the source switch that bumps the generation) and Models/PlayDirectory.cs (the queue teardown
    /// state machine) over the real paging state and the real load-all runner. The page cannot run
    /// outside UWP, so its wiring is read as text; every such check is named "source:".
    /// </summary>
    public static class Program
    {
        private const int PageSize = 20;

        private static int _passed;

        public static int Main()
        {
            NoRecordNoResume();
            SourceSwitchDropsTheRecordAtTheSwitch();
            UnchangedSourceAndRowReplaysExactlyOnce();
            ChangedRowDoesNotResume();
            RecordWithoutARowKeyDoesNotResume();
            StaleIdentityStillDropsTheRecord();
            ResumeTable();
            ReviewerScenario();
            FreshQueueIsNotTornDown();
            TearDownClearsEveryPiece();
            TearDownIsIdempotent();
            TearDownCancelsTheRunningLoadAll();
            TearDownDropsTheQueueNotTheLaunchersRows();
            TornDownQueueOffersNoPlaylistSection();
            PageSourceWiring();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        // ---------------------------------------------------------------- 记录作废规则

        private static void NoRecordNoResume()
        {
            var state = new PlayTransitionState();
            state.Commit("7001", "ml-1");

            Check(state.DecideResume(true, PlayDirectoryPagingState.More,
                    WatchSequenceSourceKind.Playlist, "k4") == PlayAdvanceResume.None,
                "without a record a retry only loads rows");
        }

        private static void SourceSwitchDropsTheRecordAtTheSwitch()
        {
            var state = new PlayTransitionState();
            state.Commit("7001", "ml-1");
            state.Begin("下一页", "");
            var inFlight = state.Ticket();
            state.Settle();
            state.RememberAdvance(WatchSequenceSourceKind.Playlist, 4, "k4", false, true);

            // 用户在取页失败之后切到"按合集连播"
            state.SourceChanged();

            Check(state.PendingAdvance == null,
                "switching the continuation source drops the record at the switch, not at the retry");
            Check(!state.IsCurrent(inFlight),
                "a page response that lands after the switch cannot redirect the step any more");
            Check(state.DecideResume(true, PlayDirectoryPagingState.More,
                    WatchSequenceSourceKind.UgcSeason, "k4") == PlayAdvanceResume.None,
                "the retry after a source switch never advances the source the user switched to");
        }

        private static void UnchangedSourceAndRowReplaysExactlyOnce()
        {
            var state = new PlayTransitionState();
            state.Commit("7001", "ml-1");
            state.RememberAdvance(WatchSequenceSourceKind.Playlist, 4, "k4", false, true);

            Check(state.DecideResume(true, PlayDirectoryPagingState.More,
                    WatchSequenceSourceKind.Playlist, "k4") == PlayAdvanceResume.Resume,
                "the same source with the same row still replays the interrupted step");
            Check(state.PendingAdvance == null, "the resumed step consumes the record exactly once");
            Check(state.DecideResume(true, PlayDirectoryPagingState.More,
                    WatchSequenceSourceKind.Playlist, "k4") == PlayAdvanceResume.None,
                "a second press on the same retry never advances twice");
        }

        private static void ChangedRowDoesNotResume()
        {
            var state = new PlayTransitionState();
            state.Commit("7001", "ml-1");
            state.RememberAdvance(WatchSequenceSourceKind.Playlist, 4, "k4", false, true);

            Check(state.DecideResume(true, PlayDirectoryPagingState.More,
                    WatchSequenceSourceKind.Playlist, "k9") == PlayAdvanceResume.None,
                "a recorded row whose key changed no longer replays: the next step would be another video");
            Check(state.PendingAdvance == null, "a changed row drops the record instead of keeping it");

            state.RememberAdvance(WatchSequenceSourceKind.UgcSeason, 2, "k2", false, true);
            Check(state.DecideResume(false, PlayDirectoryPagingState.Failed,
                    WatchSequenceSourceKind.UgcSeason, "") == PlayAdvanceResume.None,
                "a row that is gone from its source does not wait for another retry");
        }

        private static void RecordWithoutARowKeyDoesNotResume()
        {
            var state = new PlayTransitionState();
            state.Commit("7001", "");
            state.RememberAdvance(WatchSequenceSourceKind.Playlist, -1, "", false, true);

            Check(state.DecideResume(true, PlayDirectoryPagingState.More,
                    WatchSequenceSourceKind.Playlist, "") == PlayAdvanceResume.None,
                "a record that could not pin its row only loads rows on retry");
            Check(state.PendingAdvance == null, "an unpinned record is not left behind");
        }

        private static void StaleIdentityStillDropsTheRecord()
        {
            var state = new PlayTransitionState();
            state.Commit("7001", "ml-1");
            state.RememberAdvance(WatchSequenceSourceKind.Playlist, 4, "k4", false, true);

            // 换了播放列表:来源和行都没变,记录也不属于现在
            state.SetMediaList("ml-2");
            Check(state.DecideResume(true, PlayDirectoryPagingState.More,
                    WatchSequenceSourceKind.Playlist, "k4") == PlayAdvanceResume.None,
                "the identity rule still wins over a matching source and row");
            Check(state.PendingAdvance == null, "the stale record is dropped when it is found stale");
        }

        /// <summary>
        /// 记录还作不作数的全部组合:身份三项、来源、那一行、这一页取没取到、列表是不是到头。
        /// </summary>
        private static void ResumeTable()
        {
            var cases = new[]
            {
                // 身份   来源相同 行相同 取到  翻页状态                                   期望                                记录留着
                new Case(true,  true,  true,  true,  PlayDirectoryPagingState.More,      PlayAdvanceResume.Resume,       false),
                new Case(true,  true,  true,  false, PlayDirectoryPagingState.Failed,    PlayAdvanceResume.WaitForRetry, true),
                new Case(true,  true,  true,  false, PlayDirectoryPagingState.Exhausted, PlayAdvanceResume.Cancel,       false),
                new Case(true,  false, true,  true,  PlayDirectoryPagingState.More,      PlayAdvanceResume.None,         false),
                new Case(true,  false, true,  false, PlayDirectoryPagingState.Failed,    PlayAdvanceResume.None,         false),
                new Case(true,  true,  false, true,  PlayDirectoryPagingState.More,      PlayAdvanceResume.None,         false),
                new Case(true,  true,  false, false, PlayDirectoryPagingState.Failed,    PlayAdvanceResume.None,         false),
                new Case(false, true,  true,  true,  PlayDirectoryPagingState.More,      PlayAdvanceResume.None,         false),
                new Case(false, true,  true,  false, PlayDirectoryPagingState.Failed,    PlayAdvanceResume.None,         false),
            };

            foreach (var item in cases)
            {
                var state = new PlayTransitionState();
                state.Commit("7001", "ml-1");
                state.RememberAdvance(WatchSequenceSourceKind.Playlist, 4, "k4", false, true);
                // 身份变了:换了播放列表,来源和行都还是原来那一对
                if (!item.IdentityCurrent) state.SetMediaList("ml-2");

                var activeSource = item.SourceSame ? WatchSequenceSourceKind.Playlist : WatchSequenceSourceKind.UgcSeason;
                var rowKeyNow = item.RowSame ? "k4" : "k9";
                var decision = state.DecideResume(item.Landed, item.Paging, activeSource, rowKeyNow);

                Check(decision == item.Expected,
                    "resume table: identity=" + item.IdentityCurrent + " source=" + item.SourceSame
                    + " row=" + item.RowSame + " landed=" + item.Landed + " paging=" + item.Paging
                    + " resumes as " + item.Expected);
                Check((state.PendingAdvance != null) == item.Kept,
                    "resume table: the record is " + (item.Kept ? "kept" : "dropped") + " in that case");
            }
        }

        /// <summary>
        /// 评审的那一条:远程播放列表最后一条已加载行,取页失败,切到合集,回到播放列表点"重试"。
        /// </summary>
        private static void ReviewerScenario()
        {
            // 播放列表已加载 5 行、还有下一页;视频 7005 就停在最后一行上
            var paging = new MediaListPagingState("ml-1", 1, MediaListSourceKind.MediaList, "测试列表");
            paging.Append(Page(true, 5, 25, 1));
            var sequence = new WatchSequenceState();
            sequence.SetParts(0, 1);
            sequence.SetActiveSource(WatchSequenceSourceKind.Playlist);
            sequence.SetRows(WatchSequenceSourceKind.Playlist, 4, paging.LoadedCount, paging.HasMore, paging.LastLoadFailed);

            var transition = new PlayTransitionState();
            transition.Commit("7005", "ml-1");
            var pressed = WatchSequenceOrder.Next(sequence.Position, false);
            Check(pressed.Move == WatchSequenceMove.LoadNextPage,
                "the last loaded row of a playlist with another page tries to load the page first");

            // 取页失败:记下本来要走的那一步,身份三项、来源和那一行一起记
            transition.Begin("下一页", "");
            transition.Settle();
            transition.RememberAdvance(sequence.Position.Source, 4,
                RowKeyOf(paging, 4), false, true);
            var record = transition.PendingAdvance;
            Check(record != null && record.Source == WatchSequenceSourceKind.Playlist && record.RowKey == "5",
                "the interrupted step pins its source and the row it was going to advance from");

            // 用户切到"按合集连播":合集里有同一个视频 7005
            sequence.SetActiveSource(WatchSequenceSourceKind.UgcSeason);
            sequence.SetRows(WatchSequenceSourceKind.UgcSeason, 2, 30, false, false);
            transition.SourceChanged();
            Check(transition.PendingAdvance == null,
                "the switch drops the record right away, so the retry has nothing to replay");

            // 回到播放列表分段点"重试":这一页取到了,行进了同一份状态
            var landedPage = Page(false, PageSize, 25, 6);
            var landed = landedPage != null;
            paging.Append(landedPage);
            Check(landed && paging.LoadedCount == 25 && !paging.HasMore,
                "the retry loads the playlist page that was missing");

            var decision = transition.DecideResume(landed, PlayDirectoryBuilder.PagingOf(paging),
                sequence.Position.Source, RowKeyOf(paging, 4));
            Check(decision == PlayAdvanceResume.None,
                "the retry only loads rows: nothing from the collection plays");

            // 就算用户切回播放列表:记录已经用掉了,再次重试也不会自动推进
            sequence.SetActiveSource(WatchSequenceSourceKind.Playlist);
            sequence.SetRows(WatchSequenceSourceKind.Playlist, 4, paging.LoadedCount, paging.HasMore, paging.LastLoadFailed);
            transition.SourceChanged();
            var afterSwitchBack = transition.DecideResume(false, PlayDirectoryPagingState.Failed,
                sequence.Position.Source, RowKeyOf(paging, 4));
            Check(afterSwitchBack == PlayAdvanceResume.None,
                "switching back to the playlist does not revive the record either");

            // 但这一行的下一个目标确实已经可用:下一页取回来之后,是播放列表的第 6 行
            var next = WatchSequenceOrder.Next(sequence.Position, false);
            Check(next.Move == WatchSequenceMove.PlayRow && next.RowIndex == 5,
                "the playlist's own next step is the newly loaded row, not a collection episode");
        }

        // ---------------------------------------------------------------- 拆队列

        private static void FreshQueueIsNotTornDown()
        {
            var queue = new PlayQueueState();

            Check(queue.IsEmpty, "a page that was never given a queue starts empty");
            Check(!queue.Clear(), "tearing down a queue that was never built does nothing");
            Check(queue.IsEmpty && !queue.Clear(), "and it stays empty and idempotent");
        }

        private static void TearDownClearsEveryPiece()
        {
            var queue = new PlayQueueState();
            var channel = ChannelOf();
            queue.Playlist = new VideoPlaylist() { Playlist = new List<VideoPlaylistItem>() };
            queue.Paging = channel.State;
            queue.Rows = new ObservableCollection<VideoPlaylistItem>();
            queue.LoadAll = channel.Runner();

            Check(!queue.IsEmpty, "a launch with a queue has all four pieces");
            Check(queue.Clear(), "tearing the queue down reports the pieces it dropped");
            Check(queue.Playlist == null && queue.Paging == null && queue.Rows == null && queue.LoadAll == null,
                "the queue, the paging state, the rows and 加载全部 are cleared together");
            Check(queue.IsEmpty, "nothing of the old queue is left to display, page or activate");
        }

        private static void TearDownIsIdempotent()
        {
            var queue = new PlayQueueState();
            queue.Paging = ChannelOf().State;
            queue.Rows = new ObservableCollection<VideoPlaylistItem>();

            Check(queue.Clear(), "the first teardown drops the pieces");
            Check(!queue.Clear(), "the second teardown finds nothing and reports it");
            Check(!queue.Clear(), "and keeps reporting nothing");
            Check(queue.Playlist == null && queue.Paging == null && queue.Rows == null && queue.LoadAll == null,
                "repeated teardowns leave the queue empty instead of throwing");
        }

        private static void TearDownCancelsTheRunningLoadAll()
        {
            var channel = ChannelOf(pages: 4);
            channel.Suspend = true;
            var queue = new PlayQueueState();
            queue.Paging = channel.State;
            queue.Rows = new ObservableCollection<VideoPlaylistItem>();
            var runner = channel.Runner();
            queue.LoadAll = runner;

            Check(runner.TryStart(), "加载全部 starts on the queue that is still open");
            var run = runner.RunAsync();
            Check(runner.State == LoadAllState.Running, "one page is in flight when the page closes");

            // 关页:队列整份拆掉,在跑的这一轮收手
            queue.Clear();
            Check(runner.State == LoadAllState.Stopping,
                "the teardown asks the run to stop: the page in hand finishes, no new one starts");
            Check(queue.LoadAll == null, "the runner is no longer reachable from the queue");

            var pages = run.GetAwaiter().GetResult();
            Check(pages == 1 && channel.Calls == 1,
                "the run stops after the page that was already in flight instead of walking the rest");
            Check(runner.State == LoadAllState.Idle, "the stopped run is idle again");
            Check(channel.State.LoadedCount > 5,
                "the rows of that page stay in the paging state: the teardown drops references, not data");
        }

        private static void TearDownDropsTheQueueNotTheLaunchersRows()
        {
            var launcherRows = new List<VideoPlaylistItem>()
            {
                new VideoPlaylistItem() { ID = "7001", Title = "第1讲" },
                new VideoPlaylistItem() { ID = "7002", Title = "第2讲" },
                new VideoPlaylistItem() { ID = "7003", Title = "第3讲" },
            };
            var launcherPlaylist = new VideoPlaylist() { Playlist = launcherRows, Index = 1 };
            var queue = new PlayQueueState();
            queue.Playlist = launcherPlaylist;
            queue.Rows = new ObservableCollection<VideoPlaylistItem>(launcherRows);

            queue.Clear();
            Check(launcherRows.Count == 3 && ReferenceEquals(launcherPlaylist.Playlist, launcherRows),
                "the teardown drops this page's reference instead of emptying the launcher's queue");
            Check(queue.Rows == null && queue.Playlist == null,
                "the page itself keeps nothing of that queue");
        }

        private static void TornDownQueueOffersNoPlaylistSection()
        {
            var parts = PlayDirectoryBuilder.FromParts(new List<PlayDirectoryPart>()
            {
                new PlayDirectoryPart() { Cid = "c1", Title = "P1", DurationSeconds = 600 },
                new PlayDirectoryPart() { Cid = "c2", Title = "P2", DurationSeconds = 600 },
            }, 0, "测试视频", false);
            var collection = PlayDirectoryBuilder.FromUgcSeason(null, null);

            // 队列拆掉之后页面交给目录的就是 null:没有播放列表这一段
            var withoutQueue = PlayDirectoryBuilder.Combine(parts, collection, null, WatchSequenceSourceKind.None);
            Check(SectionOf(withoutQueue, PlayDirectorySectionKind.Playlist) == null,
                "a torn-down queue gives the drawer no playlist section");

            var queue = new PlayQueueState();
            queue.Rows = new ObservableCollection<VideoPlaylistItem>(QueueRows(3));
            var playlist = new VideoPlaylist() { Playlist = queue.Rows, Index = 0 };
            var withQueue = PlayDirectoryBuilder.Combine(parts, collection,
                PlayDirectoryBuilder.FromPlaylist(playlist, null, "7001"), WatchSequenceSourceKind.Playlist);
            Check(SectionOf(withQueue, PlayDirectorySectionKind.Playlist) != null,
                "a queue that exists still shows its section and is the active one");
        }

        // ---------------------------------------------------------------- 页面的接线

        private static void PageSourceWiring()
        {
            var page = ReadSource("src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs");
            var model = ReadSource("src/BiliLite.UWP/Models/PlayContinuation.cs");

            // ---- BR-32:换来源当场作废,重试只补行

            Check(CountOf(page, "sequence.SetActiveSource(") == 1
                && Body(page, "private void SetSequenceSource(WatchSequenceSourceKind source)")
                    .Contains("sequence.SetActiveSource(source);"),
                "source: every page-side source set goes through one entry");
            var switchSource = Body(page, "private void SetSequenceSource(WatchSequenceSourceKind source)");
            Check(switchSource.Contains("var changed = sequence.Position.Source != source;")
                && switchSource.Contains("if (!changed) return;")
                && switchSource.Contains("transition.SourceChanged();"),
                "source: only a real switch invalidates the step in flight, browsing the drawer is untouched");
            Check(Body(page, "private void SwitchSequenceSource(WatchSequenceSourceKind source)").Contains("SetSequenceSource(source);")
                && Body(page, "private void AdoptDirectoryRowSource").Contains("SetSequenceSource(")
                && Body(page, "private async Task ActivateDirectoryRowAsync").Contains("SetSequenceSource("),
                "source: the source buttons and a row activation all take the same entry");

            var advance = Body(page, "private async Task<bool> AdvanceSequenceAsync");
            var loadNext = Between(advance, "case WatchSequenceMove.LoadNextPage:",
                "case WatchSequenceMove.RetryNextPage:");
            Check(loadNext.Contains("if (!transition.IsCurrent(ticket)) return false;")
                && loadNext.IndexOf("if (!transition.IsCurrent(ticket)) return false;", StringComparison.Ordinal)
                    < loadNext.IndexOf("RememberInterruptedAdvance(", StringComparison.Ordinal),
                "source: a page that lands after the source changed is never recorded as an interrupted step");

            var remember = Body(page, "private void RememberInterruptedAdvance");
            Check(remember.Contains("transition.RememberAdvance(position.Source, position.RowIndex,\n                RowKeyIn(position.Source, position.RowIndex), skipRemainingParts, manual);"),
                "source: the record pins the row key of the row it would have advanced from");

            var resume = Body(page, "private async Task ResumeInterruptedAdvanceAsync(bool landed)");
            var decide = resume.IndexOf("transition.DecideResume(landed, PlayDirectoryBuilder.PagingOf(mediaListState),",
                StringComparison.Ordinal);
            var gate = resume.IndexOf("if (decision != PlayAdvanceResume.Resume) return;", StringComparison.Ordinal);
            var replay = resume.IndexOf("await AdvanceSequenceAsync(record.SkipRemainingParts, record.Manual);",
                StringComparison.Ordinal);
            Check(decide > 0 && resume.Contains("sequence.Position.Source, RowKeyIn(record.Source, record.RowIndex));")
                && gate > decide && replay > gate,
                "source: the only replay path needs the decision for the live source and the recorded row");
            Check(!resume.Contains("SetSequenceSource(") && !resume.Contains("SetActiveSource("),
                "source: the replay never changes the continuation source itself");

            // ---- BR-33:关页与重新进入都把队列拆干净

            var close = Body(page, "private void ClosePage()");
            Check(close.Contains("RemoveMediaListPlaylist();"),
                "source: closing the page tears the queue down");
            Check(close.Contains("transition.Close();") && close.Contains("sequence.Clear();")
                && close.Contains("player?.UpdateDirectoryInfo(false, \"\");"),
                "source: the identity, the sequence and the 目录 entry go with it");
            Check(!close.Contains("loadAll.RequestCancel();"),
                "source: the run is cancelled by the teardown, not by a second path that could be missed");

            var navigate = Body(page, "protected async override void OnNavigatedTo");
            Check(navigate.Contains("RemoveMediaListPlaylist();"),
                "source: re-entering the page drops the previous launch's queue before building anything");

            var teardown = Body(page, "private void RemoveMediaListPlaylist()");
            Check(teardown.Contains("mediaListLoader = null;"),
                "source: the page loader channel is dropped, so a late page cannot land in the new queue");
            Check(teardown.Contains("mediaListListView.SelectionChanged -= ListView_SelectionChanged;")
                && teardown.Contains("mediaListListView.ItemsSource = null;"),
                "source: the tab's list is detached from the page before its rows go");
            Check(teardown.Contains("mediaListLoadMoreButton.Click -= MediaListLoadMore_Click;"),
                "source: the tab's buttons are detached too");
            Check(teardown.Contains("playlistPivotItem.Content = null;")
                && teardown.Contains("pivot.Items.Remove(playlistPivotItem);")
                && teardown.Contains("playlistPivotItem = null;"),
                "source: the playlist pivot item is emptied and removed from the pivot");
            Check(teardown.Contains("queue.Clear();"),
                "source: the queue state machine is what actually clears the four pieces");
            Check(teardown.IndexOf("pivot.Items.Remove(playlistPivotItem)", StringComparison.Ordinal) > 0
                && teardown.IndexOf("queue.Clear();", StringComparison.Ordinal) > 0,
                "source: one teardown does all of it");

            Check(CountOf(page, "pivot.Items.Insert(0, element);") == 2
                && CountOf(page, "playlistPivotItem = element;") == 2,
                "source: a playlist tab is only built on a launch that carries a queue");
            Check(!Between(page, "private void RemoveMediaListPlaylist()", "private async Task InitializeMediaListPlaylist()")
                    .Contains("pivot.Items.Insert"),
                "source: the teardown only removes, it never rebuilds a tab");

            Check(Body(page, "private async Task<bool> LoadMediaListPage()").Contains("!ReferenceEquals(mediaListRows, rows)")
                && Body(page, "private async Task<bool> LoadMediaListPage()").Contains("var state = mediaListState;"),
                "source: a page that comes back after the queue was torn down does not append to the new one");
            Check(Body(page, "private async Task RequestMediaListPageAsync").Contains("if (!ReferenceEquals(mediaListState, state)) return;"),
                "source: a retry for a replaced queue does not refresh the current one");
            Check(Body(page, "private async Task InitializeMediaListPlaylist").Contains("if (!ReferenceEquals(mediaListState, initState)) return;"),
                "source: a first page that lands after the page was reused does not select or play a row");
            Check(Body(page, "private string DirectoryCaption(out bool available)").Contains("&& playlist?.Playlist != null"),
                "source: the 目录 entry never advertises a playlist that no longer exists");
            Check(model.Contains("public void SourceChanged()")
                && model.Contains("public string RowKey { get; set; }"),
                "source: the invalidation rule lives in the record's own model");
        }

        // ---------------------------------------------------------------- helpers

        private struct Case
        {
            public Case(bool identityCurrent, bool sourceSame, bool rowSame, bool landed,
                PlayDirectoryPagingState paging, PlayAdvanceResume expected, bool kept)
            {
                IdentityCurrent = identityCurrent;
                SourceSame = sourceSame;
                RowSame = rowSame;
                Landed = landed;
                Paging = paging;
                Expected = expected;
                Kept = kept;
            }

            public bool IdentityCurrent { get; private set; }
            public bool SourceSame { get; private set; }
            public bool RowSame { get; private set; }
            public bool Landed { get; private set; }
            public PlayDirectoryPagingState Paging { get; private set; }
            public PlayAdvanceResume Expected { get; private set; }
            public bool Kept { get; private set; }
        }

        /// <summary>
        /// 测试用的取页通道:和页面一样,一页回来才取下一页;失败就停。
        /// </summary>
        private sealed class Channel
        {
            private readonly Queue<MediaListResources> m_pages = new Queue<MediaListResources>();

            public Channel(MediaListPagingState state, params MediaListResources[] pages)
            {
                State = state;
                foreach (var page in pages) m_pages.Enqueue(page);
            }

            public MediaListPagingState State { get; private set; }

            public bool Suspend;

            public int Calls { get; private set; }

            public bool HaveMore()
            {
                var paging = PlayDirectoryBuilder.PagingOf(State);
                return paging == PlayDirectoryPagingState.More || paging == PlayDirectoryPagingState.Failed;
            }

            public bool Busy()
            {
                return State.IsLoading;
            }

            public async Task<bool> LoadPageAsync()
            {
                State.MarkLoading();
                Calls++;
                var page = m_pages.Count == 0 ? null : m_pages.Dequeue();
                if (Suspend) await Task.Yield();
                var landed = false;
                if (page == null)
                {
                    State.MarkLoadFailed();
                }
                else
                {
                    State.Append(page);
                    landed = true;
                }
                State.MarkLoadFinished();
                return landed;
            }

            public PlayDirectoryLoadAll Runner()
            {
                return new PlayDirectoryLoadAll(HaveMore, Busy, LoadPageAsync);
            }
        }

        private static Channel ChannelOf(int pages = 4)
        {
            var state = new MediaListPagingState("ml-1", 1, MediaListSourceKind.MediaList, "测试列表");
            // 首页已经在手:页面打开时就是这样
            state.Append(Page(true, 5, 25, 1));
            var rest = new List<MediaListResources>();
            for (var page = 0; page < pages; page++)
            {
                rest.Add(Page(page < pages - 1, PageSize, 25, 6 + page * PageSize));
            }
            return new Channel(state, rest.ToArray());
        }

        private static MediaListResources Page(bool hasMore, int count, int totalCount, int firstId)
        {
            var page = new MediaListResources()
            {
                HasMore = hasMore,
                TotalCount = totalCount,
                MediaList = new List<MediaListItem>(),
            };
            for (var i = 0; i < count; i++)
            {
                var id = firstId + i;
                page.MediaList.Add(new MediaListItem()
                {
                    Id = id,
                    Title = "第" + id + "讲",
                    Cover = "cover-" + id,
                    Duration = 600,
                    Upper = new MediaListItemUpper() { Name = "甲老师" },
                });
            }
            return page;
        }

        /// <summary>
        /// 播放列表里第 index 行现在的身份:和页面 RowKeyIn 读的是同一件事——那一行的 aid。
        /// </summary>
        private static string RowKeyOf(MediaListPagingState state, int index)
        {
            if (state == null || index < 0 || index >= state.Rows.Count) return "";
            return state.Rows[index].Id.ToString();
        }

        private static List<VideoPlaylistItem> QueueRows(int count)
        {
            var rows = new List<VideoPlaylistItem>();
            for (var i = 1; i <= count; i++)
            {
                rows.Add(new VideoPlaylistItem() { ID = (7000 + i).ToString(), Title = "第" + i + "讲" });
            }
            return rows;
        }

        private static PlayDirectorySection SectionOf(PlayDirectoryModel model, PlayDirectorySectionKind kind)
        {
            if (model == null || model.Sections == null) return null;
            foreach (var section in model.Sections)
            {
                if (section != null && section.Kind == kind) return section;
            }
            return null;
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

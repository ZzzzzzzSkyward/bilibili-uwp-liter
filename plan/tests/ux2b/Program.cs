using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using BiliLite.Models.Common;

namespace Ux2bProbe
{
    /// <summary>
    /// Ux2b: "加载全部" walks the same single-flight channel one page at a time with progress and a
    /// cancel, and the long list only ever appends, so the drawer's rows never get rebuilt under the
    /// user's scroll position.
    ///
    /// Executed against product source: Models/PlayDirectory.cs (the load-all state machine, the
    /// footer rule per state and the append-only row collection) over the real paging state it
    /// walks. The player, the page and the control cannot run outside UWP, so their wiring is read
    /// as text; every such check is named "source:".
    /// </summary>
    public static class Program
    {
        private const int PageSize = 20;
        private const int PageCount = 10;

        private static int _passed;

        public static int Main()
        {
            StartOnlyFromIdleWithoutAPageInFlight();
            RunDrainsPageByPageUntilTheEnd();
            SecondRequestWhileRunningIsIgnored();
            StartRefusesWhileAPageIsAlreadyInFlight();
            StartRefusesWhenNothingIsLeft();
            RunWithoutStartDoesNothing();
            CancelStopsAfterThePageInFlight();
            CancelWhileSuspendedStopsAfterThatPage();
            CancelBeforeTheFirstPageStopsImmediately();
            CancelOutsideARunIsANoOp();
            CancelTwiceIsStable();
            FailureStopsTheRunAndLeavesRetry();
            AFailureNeverResumesOnALaterManualPage();
            LoadAllNeverOverlapsItsOwnPages();
            CancellationPointsTable();
            FooterTable();
            LoadAllStateIsNeverShownAsAnExplanation();
            ProgressIsTheCountTheDrawerAlreadyShows();
            FirstSyncFillsTheRows();
            PageAppendsInPlaceAndKeepsTheObjects();
            AppendNotifiesAddsOnly();
            FlagsAreWrittenBackInPlace();
            AnUnchangedRedrawTouchesNothing();
            ShrunkOrReorderedRowsAreReplaced();
            OpenDrawerGainsRowsWithoutRebuilding();
            SearchReappliesToNewRows();
            RowListMarkupWiring();
            FooterMarkupWiring();
            ControlSourceWiring();
            PlayerSourceWiring();
            PageSourceWiring();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        // ---------------------------------------------------------------- 状态机

        private static void StartOnlyFromIdleWithoutAPageInFlight()
        {
            var channel = ChannelOf(PageCount);
            var runner = channel.Runner();

            Check(runner.State == LoadAllState.Idle && !runner.IsRunning,
                "a fresh load-all is idle");
            Check(runner.TryStart(), "the first request starts the run");
            Check(runner.State == LoadAllState.Running && runner.IsRunning,
                "a started run reports running until the page in flight comes back");
        }

        private static void RunDrainsPageByPageUntilTheEnd()
        {
            var channel = ChannelOf(PageCount);
            var runner = channel.Runner();
            runner.TryStart();
            var pages = runner.RunAsync().GetAwaiter().GetResult();

            Check(pages == 9, "a first page already in hand leaves nine pages to walk");
            Check(channel.Calls == 9, "each page is requested exactly once");
            Check(runner.State == LoadAllState.Idle, "the run is idle again once the list is exhausted");
            Check(channel.State.LoadedCount == PageCount * PageSize,
                "every page's rows are in the paging state, so nothing was dropped on the way");
            Check(!channel.HaveMore(), "the exhausted state offers no page to load any more");
        }

        private static void SecondRequestWhileRunningIsIgnored()
        {
            var channel = ChannelOf(PageCount);
            PlayDirectoryLoadAll runner = null;
            var ignored = 0;
            var states = new List<LoadAllState>();
            var firstPageCalls = -1;
            channel.OnPage = n =>
            {
                if (n != 1) return;
                states.Add(runner.State);
                // 跑着的时候 footer 上只有"取消":同一个按钮不会再点出第二次
                if (!runner.TryStart()) ignored++;
                firstPageCalls = channel.Calls;
            };
            runner = channel.Runner();
            runner.TryStart();
            runner.RunAsync().GetAwaiter().GetResult();

            Check(states.Count == 1 && states[0] == LoadAllState.Running,
                "the second request arrives while the run is still running");
            Check(ignored == 1, "a second 加载全部 while running is ignored");
            Check(firstPageCalls == 1 && channel.Calls == 9,
                "the ignored request did not start a second walk: nine pages, not eighteen");
            Check(runner.State == LoadAllState.Idle, "one run, one stop");
        }

        private static void StartRefusesWhileAPageIsAlreadyInFlight()
        {
            var channel = ChannelOf(PageCount);
            var runner = channel.Runner();
            // 单飞通道上已经有一页在路上:再开一轮就是两个请求叠在一起
            channel.State.MarkLoading();
            Check(!runner.TryStart(), "load-all refuses to start while a page request is already in flight");
            Check(runner.State == LoadAllState.Idle && channel.Calls == 0,
                "refusing leaves the state idle and asks for nothing");

            channel.State.MarkLoadFinished();
            var pages = runner.TryStart() ? runner.RunAsync().GetAwaiter().GetResult() : -1;
            Check(pages == 9, "once that page has come back the same request goes through");
        }

        private static void StartRefusesWhenNothingIsLeft()
        {
            var channel = ChannelOf(PageCount);
            var runner = channel.Runner();
            while (channel.HaveMore())
            {
                channel.LoadPageAsync().GetAwaiter().GetResult();
            }
            var before = channel.Calls;

            Check(!runner.TryStart(), "an exhausted list does not start a run");
            Check(channel.Calls == before && channel.State.LoadedCount == PageCount * PageSize,
                "refusing at the end asks for nothing and keeps the rows");
        }

        private static void RunWithoutStartDoesNothing()
        {
            var channel = ChannelOf(PageCount);
            var runner = channel.Runner();
            var pages = runner.RunAsync().GetAwaiter().GetResult();

            Check(pages == 0 && channel.Calls == 0,
                "awaiting a run that was never started requests nothing");
            Check(runner.State == LoadAllState.Idle, "and leaves the state idle");
        }

        private static void CancelStopsAfterThePageInFlight()
        {
            var channel = ChannelOf(PageCount);
            PlayDirectoryLoadAll runner = null;
            var stateWhileCancel = LoadAllState.Idle;
            channel.OnPage = n =>
            {
                if (n != 3) return;
                runner.RequestCancel();
                stateWhileCancel = runner.State;
                // 取消只让手上这一页走完:页面要还想再点一次,不该被当成新的一轮
                Check(runner.TryStart() == false, "a cancel does not allow a second run to start on top");
            };
            runner = channel.Runner();
            runner.TryStart();
            var pages = runner.RunAsync().GetAwaiter().GetResult();

            Check(stateWhileCancel == LoadAllState.Stopping,
                "cancel moves a running walk to stopping instead of throwing the page away");
            Check(pages == 3, "the page in flight is allowed to land, and no fourth page is asked for");
            Check(channel.Calls == 3, "no request is made after the cancel");
            Check(runner.State == LoadAllState.Idle, "stopping finishes back at idle");
            Check(channel.State.LoadedCount == (pages + 1) * PageSize,
                "every row that already landed — the first page included — is kept");
        }

        private static void CancelWhileSuspendedStopsAfterThatPage()
        {
            // 真挂起一次:取消发生在页面还在路上的时候,不是在两页之间
            var channel = ChannelOf(PageCount);
            channel.Suspend = true;
            PlayDirectoryLoadAll runner = null;
            channel.OnPage = n =>
            {
                if (n == 2) runner.RequestCancel();
            };
            runner = channel.Runner();
            runner.TryStart();
            var pages = runner.RunAsync().GetAwaiter().GetResult();

            Check(pages == 2 && channel.Calls == 2,
                "a cancel while a page is suspended still lands that page and asks for no more");
            Check(channel.MaxInFlight == 1, "the suspended page was the only one in flight");
            Check(runner.State == LoadAllState.Idle && channel.State.LoadedCount == (pages + 1) * PageSize,
                "the run is idle again with the rows it got, the first page included");
        }

        private static void CancelBeforeTheFirstPageStopsImmediately()
        {
            var channel = ChannelOf(PageCount);
            var runner = channel.Runner();
            runner.TryStart();
            runner.RequestCancel();
            var pages = runner.RunAsync().GetAwaiter().GetResult();

            Check(pages == 0 && channel.Calls == 0,
                "cancelling before the first page asks for nothing at all");
            Check(runner.State == LoadAllState.Idle, "the state is idle, not stuck stopping");
            Check(channel.State.LoadedCount == PageSize && channel.HaveMore(),
                "the page that was already in hand is still there for 加载更多 afterwards");
        }

        private static void CancelOutsideARunIsANoOp()
        {
            var channel = ChannelOf(PageCount);
            var runner = channel.Runner();
            runner.RequestCancel();

            Check(runner.State == LoadAllState.Idle, "a cancel with nothing running changes nothing");
            Check(runner.TryStart(), "and the request right after it still goes through");
            runner.RequestCancel();
            var pages = runner.RunAsync().GetAwaiter().GetResult();
            Check(pages == 0 && channel.Calls == 0, "the walk stops before the first page");
        }

        private static void CancelTwiceIsStable()
        {
            var channel = ChannelOf(PageCount);
            PlayDirectoryLoadAll runner = null;
            var second = LoadAllState.Idle;
            channel.OnPage = n =>
            {
                if (n != 2) return;
                runner.RequestCancel();
                runner.RequestCancel();
                second = runner.State;
            };
            runner = channel.Runner();
            runner.TryStart();
            var pages = runner.RunAsync().GetAwaiter().GetResult();

            Check(second == LoadAllState.Stopping, "cancelling twice stays stopping, it does not restart");
            Check(pages == 2 && runner.State == LoadAllState.Idle, "and the walk still stops after that page");
        }

        private static void FailureStopsTheRunAndLeavesRetry()
        {
            var channel = new Channel(new MediaListPagingState("ml-1", 1, MediaListSourceKind.MediaList, "测试列表"),
                Page(true, PageSize, PageCount * PageSize, 1),
                null);
            var runner = channel.Runner();
            runner.TryStart();
            var pages = runner.RunAsync().GetAwaiter().GetResult();

            Check(pages == 1, "the page before the failure landed, the failed one did not");
            Check(runner.State == LoadAllState.Idle, "a failure stops the run instead of retrying inside it");
            Check(channel.State.LastLoadFailed, "the failure is recorded on the paging state");
            Check(channel.State.LoadedCount == PageSize, "the rows already loaded are kept");

            var footer = PlayDirectoryBuilder.FooterOf(PlayDirectoryBuilder.PagingOf(channel.State),
                runner.State, true, PlayDirectorySectionKind.Playlist);
            Check(footer.PrimaryText == PlayDirectoryBuilder.RetryText && !footer.ShowRing
                && footer.SecondaryText == PlayDirectoryBuilder.LoadAllText,
                "the footer goes back to 重试 with 加载全部 beside it, not back to a running cancel");
        }

        private static void AFailureNeverResumesOnALaterManualPage()
        {
            var channel = new Channel(new MediaListPagingState("ml-1", 1, MediaListSourceKind.MediaList, "测试列表"),
                Page(true, PageSize, PageCount * PageSize, 1),
                null,
                Page(false, PageSize, PageCount * PageSize, 41));
            var runner = channel.Runner();
            runner.TryStart();
            var pages = runner.RunAsync().GetAwaiter().GetResult();
            var callsAfterFailure = channel.Calls;

            // 用户自己点"重试":普通的一页,不是新一轮
            var landed = channel.LoadPageAsync().GetAwaiter().GetResult();
            Check(landed && channel.Calls == callsAfterFailure + 1,
                "the manual 重试 asks for exactly one page");
            Check(runner.State == LoadAllState.Idle && !runner.IsRunning,
                "the manual page never silently resumes the run behind the user's back");
            Check(channel.State.LoadedCount == 2 * PageSize, "the retried page landed");

            // 用户再次点了"加载全部"才会继续
            Check(!channel.HaveMore() && !runner.TryStart(),
                "with the list already at its end there is nothing left to ask for");
        }

        private static void LoadAllNeverOverlapsItsOwnPages()
        {
            var channel = ChannelOf(PageCount);
            var runner = channel.Runner();
            runner.TryStart();
            runner.RunAsync().GetAwaiter().GetResult();

            Check(channel.MaxInFlight == 1,
                "at most one page is ever in flight while the run walks the list");
            Check(channel.Calls == 9, "the walk is strictly sequential: one page per request");
        }

        /// <summary>
        /// 取消点表:在第 k 页在路上时取消,结果永远是"这一页落下就停",已到手的行一条不少。
        /// </summary>
        private static void CancellationPointsTable()
        {
            var stops = new[] { 1, 2, 3, 5, 9 };
            var allStopped = true;
            var allKept = true;
            var allIdle = true;
            var allSequential = true;
            var footerAlwaysNormal = true;
            foreach (var stopAt in stops)
            {
                var channel = ChannelOf(PageCount);
                PlayDirectoryLoadAll runner = null;
                channel.OnPage = n =>
                {
                    if (n == stopAt) runner.RequestCancel();
                };
                runner = channel.Runner();
                runner.TryStart();
                var pages = runner.RunAsync().GetAwaiter().GetResult();

                if (pages != stopAt || channel.Calls != stopAt) allStopped = false;
                if (channel.State.LoadedCount != (stopAt + 1) * PageSize) allKept = false;
                if (runner.State != LoadAllState.Idle) allIdle = false;
                if (channel.MaxInFlight != 1) allSequential = false;

                // 停下来之后的 footer 回到能接着走的那一套,永远不再是"取消"
                var footer = PlayDirectoryBuilder.FooterOf(PlayDirectoryBuilder.PagingOf(channel.State),
                    runner.State, true, PlayDirectorySectionKind.Playlist);
                if (footer.PrimaryText == PlayDirectoryBuilder.CancelText || footer.ShowRing) footerAlwaysNormal = false;
                // 还有页可取的时候,footer 就是"加载更多"
                if (stopAt < PageCount - 1 && footer.PrimaryText != PlayDirectoryBuilder.LoadMoreText) footerAlwaysNormal = false;
            }

            Check(allStopped, "cancelling at page 1/2/3/5/9 always stops right after that page");
            Check(allKept, "the rows of every page that landed before the cancel are kept");
            Check(allIdle, "every cancellation point ends at idle");
            Check(allSequential, "no cancellation point ever leaves two pages in flight");
            Check(footerAlwaysNormal,
                "after a cancel the footer is back to 加载更多 (or gone at the end), never 取消 or a ring");
        }

        // ---------------------------------------------------------------- footer

        private static void FooterTable()
        {
            // owned=有远程播放列表这一段;browsed=用户正看着哪一段
            FooterCase(PlayDirectoryPagingState.None, LoadAllState.Idle, true, PlayDirectorySectionKind.Playlist,
                false, false, "", "", "a fixed in-memory queue has no footer");
            FooterCase(PlayDirectoryPagingState.None, LoadAllState.Idle, false, PlayDirectorySectionKind.Playlist,
                false, false, "", "", "no playlist section means no footer");
            FooterCase(PlayDirectoryPagingState.More, LoadAllState.Idle, true, PlayDirectorySectionKind.Playlist,
                true, false, PlayDirectoryBuilder.LoadMoreText, PlayDirectoryBuilder.LoadAllText,
                "a page to load offers 加载更多 with 加载全部 beside it");
            FooterCase(PlayDirectoryPagingState.Failed, LoadAllState.Idle, true, PlayDirectorySectionKind.Playlist,
                true, false, PlayDirectoryBuilder.RetryText, PlayDirectoryBuilder.LoadAllText,
                "a failed page offers 重试 with 加载全部 beside it");
            FooterCase(PlayDirectoryPagingState.Loading, LoadAllState.Idle, true, PlayDirectorySectionKind.Playlist,
                true, true, "", "", "a page in flight is a ring and nothing else to press");
            FooterCase(PlayDirectoryPagingState.Exhausted, LoadAllState.Idle, true, PlayDirectorySectionKind.Playlist,
                false, false, "", "", "an exhausted list leaves no footer");
            FooterCase(PlayDirectoryPagingState.More, LoadAllState.Idle, true, PlayDirectorySectionKind.Parts,
                false, false, "", "", "browsing another section keeps the playlist footer away");
            FooterCase(PlayDirectoryPagingState.More, LoadAllState.Idle, true, PlayDirectorySectionKind.UgcSeason,
                false, false, "", "", "browsing the collection tab keeps the playlist footer away");
            FooterCase(PlayDirectoryPagingState.More, LoadAllState.Running, true, PlayDirectorySectionKind.Playlist,
                true, false, PlayDirectoryBuilder.CancelText, "", "while running the footer is one 取消 action");
            FooterCase(PlayDirectoryPagingState.Loading, LoadAllState.Running, true, PlayDirectorySectionKind.Playlist,
                true, false, PlayDirectoryBuilder.CancelText, "", "a page in flight inside the run is still 取消, not a ring");
            FooterCase(PlayDirectoryPagingState.More, LoadAllState.Stopping, true, PlayDirectorySectionKind.Playlist,
                true, false, PlayDirectoryBuilder.CancelText, "", "stopping still shows the cancel until the page lands");
            FooterCase(PlayDirectoryPagingState.More, LoadAllState.Running, true, PlayDirectorySectionKind.Parts,
                true, false, PlayDirectoryBuilder.CancelText, "",
                "the cancel stays reachable when the user browses another section mid-run");
            FooterCase(PlayDirectoryPagingState.Exhausted, LoadAllState.Running, true, PlayDirectorySectionKind.Playlist,
                true, false, PlayDirectoryBuilder.CancelText, "", "the last page landing still leaves the stop reachable");
        }

        private static void FooterCase(PlayDirectoryPagingState paging, LoadAllState loadAll, bool hasPlaylist,
            PlayDirectorySectionKind browsed, bool hasContent, bool ring, string primary, string secondary, string message)
        {
            var footer = PlayDirectoryBuilder.FooterOf(paging, loadAll, hasPlaylist, browsed);
            Check(footer.HasContent == hasContent && footer.ShowRing == ring
                && (footer.PrimaryText ?? "") == primary && (footer.SecondaryText ?? "") == secondary,
                message);
            Check(!footer.ShowRing || string.IsNullOrEmpty(footer.PrimaryText),
                "the ring and a button never stand for the same waiting state: " + message);
        }

        private static void LoadAllStateIsNeverShownAsAnExplanation()
        {
            var labels = new[]
            {
                PlayDirectoryBuilder.LoadMoreText, PlayDirectoryBuilder.RetryText,
                PlayDirectoryBuilder.LoadAllText, PlayDirectoryBuilder.CancelText,
            };
            var clean = true;
            foreach (var label in labels)
            {
                if (label.IndexOf('（') >= 0 || label.IndexOf('·') >= 0 || label.IndexOf('：') >= 0) clean = false;
            }
            Check(clean, "the footer copy carries no parenthetical, no · restatement and no ：suffix");
            Check(PlayDirectoryBuilder.LoadAllText == "加载全部" && PlayDirectoryBuilder.CancelText == "取消",
                "the two new labels are exactly 加载全部 and 取消");
        }

        private static void ProgressIsTheCountTheDrawerAlreadyShows()
        {
            var state = new MediaListPagingState("ml-1", 1, MediaListSourceKind.MediaList, "测试列表");
            state.Append(Page(true, PageSize, PageCount * PageSize, 1));
            var channel = new Channel(state, Pages(2, PageCount));
            var counts = new List<string>();
            var items = new List<VideoPlaylistItem>();
            var playlist = new VideoPlaylist() { Index = 0, MediaListId = "ml-1", MediaListState = state, Playlist = items };
            PlayDirectoryLoadAll runner = null;
            channel.AfterPage = (n, landed) =>
            {
                if (!landed) return;
                // 页面每取一页就重画一次:数量就是进度
                AppendItems(items, state);
                var model = PlayDirectoryBuilder.FromPlaylist(playlist, state, null);
                model.LoadAll = runner.State;
                counts.Add(model.CountText);
            };
            runner = channel.Runner();
            runner.TryStart();
            runner.RunAsync().GetAwaiter().GetResult();
            var atEnd = PlayDirectoryBuilder.FromPlaylist(playlist, state, null);

            Check(counts.Count == 9, "the walk lands nine pages, each one reported once");
            Check(counts[0] == "40/200" && counts[counts.Count - 1] == "200/200",
                "the progress is the bare loaded/total count, 40/200 rising to 200/200");
            Check(atEnd.CountText == "200/200", "the drawer's own count reads the same at the end");
            var bare = true;
            foreach (var count in counts)
            {
                if (count.IndexOf(' ') >= 0 || count.IndexOf('（') >= 0 || count.IndexOf('·') >= 0) bare = false;
            }
            Check(bare, "the progress is a bare count: no padding, no explanation");
        }

        // ---------------------------------------------------------------- 行集合

        private static void FirstSyncFillsTheRows()
        {
            var rows = new PlayDirectoryRows();
            var model = RowList(1, 3);
            rows.Sync(model);

            Check(rows.Count == 3, "the first sync fills the collection");
            Check(ReferenceEquals(rows[0], model[0]), "the collection holds the model's own rows on the first sync");
        }

        private static void PageAppendsInPlaceAndKeepsTheObjects()
        {
            var rows = new PlayDirectoryRows();
            var collection = rows;
            rows.Sync(RowList(1, PageSize));
            var anchor = rows[0];
            var anchorLast = rows[PageSize - 1];

            var addEvents = 0;
            var addedItems = 0;
            var resets = 0;
            rows.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    addEvents++;
                    addedItems += e.NewItems.Count;
                }
                if (e.Action == NotifyCollectionChangedAction.Reset) resets++;
            };

            // 一页落下来:同一个集合实例、同样的前 20 行对象,只有末尾多出 20 行
            rows.Sync(RowList(1, PageSize * 2));

            Check(ReferenceEquals(rows, collection), "the rows collection instance is stable across a page");
            Check(rows.Count == PageSize * 2, "the page's rows are appended, not swapped in");
            Check(ReferenceEquals(rows[0], anchor) && ReferenceEquals(rows[PageSize - 1], anchorLast),
                "the rows already on screen stay the very same objects");
            Check(rows[PageSize].Key == "k" + (PageSize + 1), "the appended rows start where the old ones ended");
            Check(addedItems == PageSize && resets == 0,
                "the new page arrives as additions only, with no list reset");
            Check(addEvents == PageSize, "each landed row is an addition the list can append without rebuilding");
        }

        private static void AppendNotifiesAddsOnly()
        {
            var rows = new PlayDirectoryRows();
            rows.Sync(RowList(1, PageSize));
            var resets = 0;
            var adds = 0;
            rows.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset) resets++;
                if (e.Action == NotifyCollectionChangedAction.Add) adds++;
            };
            rows.Sync(RowList(1, PageSize * 3));

            Check(resets == 0, "a page landing never resets the list, which is what throws the scroll position away");
            Check(adds == PageSize * 2, "the two pages arrive as plain additions");
        }

        private static void FlagsAreWrittenBackInPlace()
        {
            var rows = new PlayDirectoryRows();
            rows.Sync(RowList(1, 3));
            var second = rows[1];
            var changes = new List<string>();
            second.PropertyChanged += (s, e) => changes.Add(e.PropertyName);

            // 重画只把标记写回原对象:这一行现在是"正在换到"的目标
            var next = RowList(1, 3);
            next[1].IsPending = true;
            next[1].CurrentBadge = "";
            rows.Sync(next);

            Check(ReferenceEquals(rows[1], second), "the pending row keeps its object identity");
            Check(second.IsPending && second.AutomationName.IndexOf("正在切换", StringComparison.Ordinal) >= 0,
                "the pending mark lands on the row that is already on screen");
            Check(changes.Contains("IsPending") && changes.Contains("AutomationName"),
                "the pending mark is notified, so the list updates without a rebuild");
            Check(changes.IndexOf("Title") < 0, "fields that did not change are not notified");
        }

        private static void AnUnchangedRedrawTouchesNothing()
        {
            var rows = new PlayDirectoryRows();
            rows.Sync(RowList(1, 5));
            var collectionEvents = 0;
            var propertyEvents = 0;
            rows.CollectionChanged += (s, e) => collectionEvents++;
            rows[0].PropertyChanged += (s, e) => propertyEvents++;
            rows.Sync(RowList(1, 5));

            Check(collectionEvents == 0 && propertyEvents == 0 && rows.Count == 5,
                "a redraw with the same rows raises nothing, so a re-render is free");
        }

        private static void ShrunkOrReorderedRowsAreReplaced()
        {
            var rows = new PlayDirectoryRows();
            var collection = rows;
            rows.Sync(RowList(1, 5));

            // 过滤:行集合真的变了,整批替换(用户自己引起的,滚动位置跟着变是对的)
            rows.Sync(RowList(2, 2));
            Check(rows.Count == 2 && rows[0].Key == "k2" && rows[1].Key == "k3",
                "a filtered row set replaces the collection content");
            Check(ReferenceEquals(rows, collection), "even then the collection instance is the same one");

            // 重排:同一批 Key 换了位置,不能当成追加
            var reordered = RowList(1, 3);
            reordered.Reverse();
            rows.Sync(reordered);
            Check(rows.Count == 3 && rows[0].Key == "k3" && ReferenceEquals(rows, collection),
                "a reordered row set is replaced instead of patched");

            rows.Sync(null);
            Check(rows.Count == 0 && ReferenceEquals(rows, collection),
                "an empty model empties the collection without dropping it");
        }

        /// <summary>
        /// 抽屉开着时一页一页落下来:集合实例、已显示的行对象和列表本身都不用重建。
        /// </summary>
        private static void OpenDrawerGainsRowsWithoutRebuilding()
        {
            var state = new MediaListPagingState("ml-1", 1, MediaListSourceKind.MediaList, "测试列表");
            state.Append(Page(true, PageSize, PageCount * PageSize, 1));
            var channel = new Channel(state, Pages(2, PageCount));
            var items = new List<VideoPlaylistItem>();
            var playlist = new VideoPlaylist() { Index = 0, MediaListId = "ml-1", MediaListState = state, Playlist = items };
            var rows = new PlayDirectoryRows();
            var collection = rows;

            AppendItems(items, state);
            rows.Sync(SectionRows(PlayDirectoryBuilder.FromPlaylist(playlist, state, null)));
            var anchor = rows[0];
            var adds = 0;
            var resets = 0;
            rows.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add) adds += e.NewItems.Count;
                if (e.Action == NotifyCollectionChangedAction.Reset) resets++;
            };

            var lastModel = (PlayDirectoryModel)null;
            PlayDirectoryLoadAll runner = null;
            channel.AfterPage = (n, landed) =>
            {
                if (!landed) return;
                // 页面每页都做的事:行进集合、目录重画、模型带上"还在跑"
                AppendItems(items, state);
                lastModel = PlayDirectoryBuilder.FromPlaylist(playlist, state, null);
                lastModel.LoadAll = runner.State;
                rows.Sync(SectionRows(lastModel));
            };
            runner = channel.Runner();
            runner.TryStart();
            runner.RunAsync().GetAwaiter().GetResult();

            Check(ReferenceEquals(rows, collection), "the open drawer keeps one row collection for the whole run");
            Check(rows.Count == PageCount * PageSize, "every page of the run is in the drawer's list");
            Check(ReferenceEquals(rows[0], anchor), "the first row on screen is still the same object");
            Check(adds == (PageCount - 1) * PageSize && resets == 0,
                "the pages arrive as additions only, never as a list reset");
            Check(lastModel.CountText == "200/200" && lastModel.LoadAll == LoadAllState.Running,
                "the last redraw inside the run already shows the whole list while the run winds down");
            Check(rows.Count == PageCount * PageSize, "and the whole list is on screen while it runs");
        }

        private static void SearchReappliesToNewRows()
        {
            var state = new MediaListPagingState("ml-1", 1, MediaListSourceKind.MediaList, "测试列表");
            state.Append(Page(true, PageSize, PageCount * PageSize, 1));
            var channel = new Channel(state, Pages(2, PageCount));
            var items = new List<VideoPlaylistItem>();
            var playlist = new VideoPlaylist() { Index = 0, MediaListId = "ml-1", MediaListState = state, Playlist = items };
            var rows = new PlayDirectoryRows();
            AppendItems(items, state);
            rows.Sync(SectionRows(PlayDirectoryBuilder.FromPlaylist(playlist, state, null)));
            var added = 0;
            rows.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add) added += e.NewItems.Count;
            };

            PlayDirectoryLoadAll runner = null;
            channel.AfterPage = (n, landed) =>
            {
                if (!landed) return;
                AppendItems(items, state);
                // 抽屉里挂着搜索词:新落下来的行也走同一道筛子
                var filtered = PlayDirectoryBuilder.Filter(
                    PlayDirectoryBuilder.FromPlaylist(playlist, state, null), "第199讲");
                rows.Sync(SectionRows(filtered));
            };
            runner = channel.Runner();
            runner.TryStart();
            runner.RunAsync().GetAwaiter().GetResult();

            Check(state.LoadedCount == PageCount * PageSize, "the search never holds the paging back");
            Check(rows.Count == 1 && rows[0].Key == "199",
                "only the row that landed during the run and matches the search is in the list");
            Check(added == 1,
                "rows that do not match the active search never enter the list");
        }

        // ---------------------------------------------------------------- source:

        private static void RowListMarkupWiring()
        {
            var xaml = ReadSource("src/BiliLite.UWP/Controls/PlayDirectoryControl.xaml");
            var rowTag = Tag(xaml, "x:Name=\"RowList\"");

            Check(xaml.IndexOf("ScrollViewer", StringComparison.Ordinal) < 0,
                "source: the drawer declares no ScrollViewer of its own, so the row list is the only scroller");
            Check(xaml.Contains("<RowDefinition Height=\"*\"/>") && rowTag.Contains("Grid.Row=\"4\""),
                "source: the row list sits in the drawer's one star row, so its height is bounded by the drawer");
            Check(rowTag.Length > 0 && rowTag.IndexOf("Height=", StringComparison.Ordinal) < 0,
                "source: the row list has no Height of its own inside that star row");
            Check(!xaml.Contains("ItemsWrapGrid") && !xaml.Contains("ScrollViewer.VerticalScrollMode=\"Disabled\""),
                "source: the row list is not wrapped or turned into a non-scrolling panel");
            Check(xaml.IndexOf("x:Name=\"PagingHost\"", StringComparison.Ordinal)
                    > xaml.IndexOf("x:Name=\"RowList\"", StringComparison.Ordinal),
                "source: the footer sits under the bounded row area, so it stays reachable with 200 rows");
        }

        private static void FooterMarkupWiring()
        {
            var xaml = ReadSource("src/BiliLite.UWP/Controls/PlayDirectoryControl.xaml");
            var footer = Between(xaml, "x:Name=\"PagingHost\"", "</StackPanel>");

            Check(CountOf(footer, "<TextBlock") == 2 && CountOf(footer, "<ProgressRing") == 1,
                "source: the footer is two labels and one ring, nothing else");
            Check(footer.Contains("x:Name=\"LoadMoreButton\"") && footer.Contains("x:Name=\"LoadAllButton\""),
                "source: the footer carries the paging button and the load-all button");
            Check(footer.Contains("Text=\"加载全部\"") && footer.Contains("Text=\"加载更多\""),
                "source: the two buttons are labelled 加载全部 and 加载更多");
            Check(footer.IndexOf('（') < 0 && footer.IndexOf('·') < 0 && footer.IndexOf('：') < 0,
                "source: the footer copy carries no parenthetical, no · and no ：");
        }

        private static void ControlSourceWiring()
        {
            var control = ReadSource("src/BiliLite.UWP/Controls/PlayDirectoryControl.xaml.cs");
            var model = ReadSource("src/BiliLite.UWP/Models/PlayDirectory.cs");

            Check(control.Contains("public event EventHandler LoadAllRequested;")
                && control.Contains("public event EventHandler CancelLoadAllRequested;"),
                "source: the drawer raises 加载全部 and 取消 instead of walking the list itself");
            Check(Body(control, "private void LoadAllButton_Click").Contains("LoadAllRequested"),
                "source: the load-all button raises the request");
            Check(Body(control, "private void LoadMoreButton_Click").Contains("m_model.IsLoadingAll")
                && Body(control, "private void LoadMoreButton_Click").Contains("CancelLoadAllRequested"),
                "source: the same button is 取消 while a run is active and a page request otherwise");
            Check(Body(control, "private void UpdatePaging(PlayDirectoryModel model)").Contains("PlayDirectoryBuilder.FooterOf("),
                "source: what the footer shows is the model's rule, not a second copy in the control");
            Check(model.Contains("public const string CancelText = \"取消\";")
                && model.Contains("footer.PrimaryText = CancelText;"),
                "source: 取消 comes from the same place as the other footer labels");
            Check(control.Contains("if (!ReferenceEquals(RowList.ItemsSource, rows)) RowList.ItemsSource = rows;")
                && !control.Contains("RowList.ItemsSource = section.Rows")
                && !control.Contains("ItemsSource = new"),
                "source: the row list is bound to one collection per section and only rebound when that instance changes");
            var rowsOf = Body(control, "private PlayDirectoryRows RowsOf(");
            Check(rowsOf.Contains("if (!m_rows.TryGetValue(kind, out rows))")
                && rowsOf.Contains("rows.Sync(section == null ? null : section.Rows);"),
                "source: a redraw syncs the same collection instead of building another one");
            Check(Body(control, "public void LocateCurrent()").Contains("var live = LiveRow(row);"),
                "source: locate picks the row object the list actually holds, so the selection survives a redraw");
            Check(!control.Contains("GetMediaList") && !control.Contains("LoadMoreMediaList")
                && !control.Contains("MediaListPageLoader"),
                "source: the drawer never fetches a page itself");
        }

        private static void PlayerSourceWiring()
        {
            var player = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml.cs");
            var xaml = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml");

            Check(player.Contains("public event EventHandler DirectoryLoadAllRequested;")
                && player.Contains("public event EventHandler DirectoryCancelLoadAllRequested;"),
                "source: the player forwards both new requests");
            Check(Body(player, "private void DirectoryDrawer_LoadAllRequested").Contains("DirectoryLoadAllRequested?.Invoke")
                && Body(player, "private void DirectoryDrawer_CancelLoadAllRequested").Contains("DirectoryCancelLoadAllRequested?.Invoke"),
                "source: the drawer events are forwarded unchanged");
            Check(xaml.Contains("LoadAllRequested=\"DirectoryDrawer_LoadAllRequested\"")
                && xaml.Contains("CancelLoadAllRequested=\"DirectoryDrawer_CancelLoadAllRequested\""),
                "source: the drawer's two events are wired in the player markup");
        }

        private static void PageSourceWiring()
        {
            var page = ReadSource("src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs");
            var model = ReadSource("src/BiliLite.UWP/Models/PlayDirectory.cs");

            Check(page.Contains("player.DirectoryLoadAllRequested += player_DirectoryLoadAllRequested;")
                && page.Contains("player.DirectoryCancelLoadAllRequested += player_DirectoryCancelLoadAllRequested;"),
                "source: the page handles both new requests");
            Check(page.Contains("loadAll = new PlayDirectoryLoadAll(")
                && page.Contains("HaveMoreMediaListPages(loadAllState)")
                && page.Contains("() => loadAllState.IsLoading")
                && page.Contains("() => LoadAllOneMediaListPageAsync(loadAllState)"),
                "source: the run is bound to one paging state: more pages, the channel's in-flight page, one page at a time");

            var handler = Body(page, "private async void player_DirectoryLoadAllRequested");
            Check(handler.Contains("runner.TryStart()") && handler.Contains("await runner.RunAsync();"),
                "source: the 加载全部 action starts the walk and awaits it");
            Check(handler.Contains("RefreshMediaListChrome();") && handler.Contains("RedrawOpenDirectory();"),
                "source: both the detail footer and the drawer follow every state change");
            Check(handler.IndexOf("InitializeVideo(", StringComparison.Ordinal) < 0
                && handler.IndexOf("ChangePlayIndex(", StringComparison.Ordinal) < 0
                && handler.IndexOf("SetQuery(", StringComparison.Ordinal) < 0
                && handler.IndexOf("SetActiveSource", StringComparison.Ordinal) < 0
                && handler.IndexOf("pivot.SelectedIndex", StringComparison.Ordinal) < 0,
                "source: 加载全部 never plays, never changes the source, the tab or the query");

            var onePage = Body(page, "private async Task<bool> LoadAllOneMediaListPageAsync");
            Check(onePage.Contains("await LoadMediaListPage();") && onePage.Contains("RefreshSequenceState();"),
                "source: each step is one page through the existing single-flight channel");
            Check(onePage.Contains("!ReferenceEquals(mediaListState, state)"),
                "source: a page that comes back for another playlist stops the walk instead of landing in the wrong list");
            Check(onePage.IndexOf("InitializeVideo(", StringComparison.Ordinal) < 0
                && onePage.IndexOf("ChangePlayIndex(", StringComparison.Ordinal) < 0,
                "source: walking the pages never starts playback");

            Check(Body(page, "private async Task RequestMediaListPageAsync()").Contains("loadAll.IsRunning) return;"),
                "source: a manual page request gives way while the run owns the channel");
            Check(Body(page, "private void RefreshMediaListChrome()").Contains("mediaListLoadMoreButton.IsEnabled = actionable && !running;"),
                "source: the detail footer's button is not left live during the run");

            Check(Body(page, "private void ClosePage()").Contains("loadAll.RequestCancel();"),
                "source: closing the page cancels the run");
            Check(Body(page, "private async Task<bool> InitializeVideo(string id, PlayTargetStart start)").Contains("loadAll.RequestCancel();"),
                "source: a new playback target cancels the run");
            Check(Body(page, "private bool BeginTransition(string targetTitle, string pendingKey)").Contains("loadAll.RequestCancel();"),
                "source: a newer transition cancels the run");
            Check(Body(page, "private async Task InitializeMediaListPlaylist()").Contains("loadAll.RequestCancel();"),
                "source: a new playlist identity cancels the run still walking the old one");

            Check(Body(page, "private string DirectorySignature()").Contains("(int)loadAll.State"),
                "source: the footer repaints when the run starts and stops");
            Check(Body(page, "private PlayDirectoryModel BuildDirectoryModel(bool freshOpen)")
                    .Contains("model.LoadAll = loadAll == null ? LoadAllState.Idle : loadAll.State;"),
                "source: the run's state reaches the drawer through the model");
            Check(page.Contains("player.ShowDirectory(BuildDirectoryModel(false));"),
                "source: redrawing the open drawer never re-opens it, so the browsed tab and the query stay");
            Check(CountOf(model, "await m_loadPage()") == 1,
                "source: the walk awaits exactly one page per step: two requests never overlap");
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>
        /// 测试用的取页通道:和 MediaListPageLoader 一样先立"在路上"、回来才清,页面按顺序给。
        /// page 为 null 表示这一页取失败了。
        /// </summary>
        private sealed class Channel
        {
            private readonly Queue<MediaListResources> m_pages = new Queue<MediaListResources>();

            public Channel(MediaListPagingState state, params MediaListResources[] pages)
            {
                State = state;
                foreach (var page in pages)
                {
                    m_pages.Enqueue(page);
                }
            }

            public MediaListPagingState State { get; private set; }

            /// <summary>
            /// 第 n 页在路上时插一手:模拟用户在这一页取回来之前点了取消、或者又点了一次"加载全部"
            /// </summary>
            public Action<int> OnPage;

            /// <summary>
            /// 一页回来之后插一手:模拟页面在取页之后做的事(行进集合、重画抽屉)
            /// </summary>
            public Action<int, bool> AfterPage;

            /// <summary>
            /// 真挂起一次,让"取消发生在路上"这一场真的是异步的
            /// </summary>
            public bool Suspend;

            public int Calls { get; private set; }

            public int InFlight { get; private set; }

            public int MaxInFlight { get; private set; }

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
                InFlight++;
                if (InFlight > MaxInFlight) MaxInFlight = InFlight;
                var page = m_pages.Count == 0 ? null : m_pages.Dequeue();
                if (OnPage != null) OnPage(Calls);
                if (Suspend) await Task.Yield();
                InFlight--;

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
                if (AfterPage != null) AfterPage(Calls, landed);
                return landed;
            }

            public PlayDirectoryLoadAll Runner()
            {
                return new PlayDirectoryLoadAll(HaveMore, Busy, LoadPageAsync);
            }
        }

        private static Channel ChannelOf(int pageCount)
        {
            var state = new MediaListPagingState("ml-1", 1, MediaListSourceKind.MediaList, "测试列表");
            // 首页已经在手:抽屉打开时就是这样
            state.Append(Page(pageCount > 1, PageSize, pageCount * PageSize, 1));
            return new Channel(state, Pages(2, pageCount));
        }

        private static MediaListResources[] Pages(int firstPage, int pageCount)
        {
            var pages = new List<MediaListResources>();
            for (var page = firstPage; page <= pageCount; page++)
            {
                pages.Add(Page(page < pageCount, PageSize, pageCount * PageSize, (page - 1) * PageSize + 1));
            }
            return pages.ToArray();
        }

        /// <summary>
        /// 一页响应:count 条(从 firstId 起),has_more 和已知总数由参数决定
        /// </summary>
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

        private static void AppendItems(List<VideoPlaylistItem> items, MediaListPagingState state)
        {
            for (var i = items.Count; i < state.LoadedCount; i++)
            {
                var item = state.Rows[i];
                items.Add(new VideoPlaylistItem()
                {
                    ID = item.Id.ToString(),
                    Title = item.Title,
                    Cover = item.Cover,
                    Duration = PlayDirectoryBuilder.FormatDuration(item.Duration),
                });
            }
        }

        private static IList<PlayDirectoryRow> SectionRows(PlayDirectoryModel model)
        {
            if (model == null || model.Sections == null) return null;
            foreach (var section in model.Sections)
            {
                if (section != null && section.Kind == PlayDirectorySectionKind.Playlist) return section.Rows;
            }
            return null;
        }

        /// <summary>
        /// k1..k(count) 这么一批行:每次都是新对象,Key 才是身份
        /// </summary>
        private static List<PlayDirectoryRow> RowList(int from, int count)
        {
            var rows = new List<PlayDirectoryRow>();
            for (var i = 0; i < count; i++)
            {
                var index = from + i;
                rows.Add(new PlayDirectoryRow()
                {
                    Key = "k" + index,
                    Title = "标题 k" + index,
                    Ordinal = index,
                    Duration = "10:00",
                });
            }
            return rows;
        }

        private static string Tag(string source, string marker)
        {
            var start = source.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return "";
            var body = source.Substring(start, Math.Min(400, source.Length - start));
            var end = body.IndexOf('>');
            return end < 0 ? body : body.Substring(0, end);
        }

        private static string Between(string source, string from, string to)
        {
            var start = source.IndexOf(from, StringComparison.Ordinal);
            if (start < 0) return "";
            var end = source.IndexOf(to, start, StringComparison.Ordinal);
            return end < 0 ? source.Substring(start) : source.Substring(start, end - start);
        }

        private static int CountOf(string source, string needle)
        {
            var count = 0;
            var index = 0;
            while (true)
            {
                index = source.IndexOf(needle, index, StringComparison.Ordinal);
                if (index < 0) return count;
                count++;
                index += needle.Length;
            }
        }

        private static string Body(string source, string signature)
        {
            var start = source.IndexOf(signature, StringComparison.Ordinal);
            if (start < 0) return "";
            var length = Math.Min(6000, source.Length - start);
            var body = source.Substring(start, length);
            var end = body.IndexOf("\n        }\n", StringComparison.Ordinal);
            return end < 0 ? body : body.Substring(0, end);
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

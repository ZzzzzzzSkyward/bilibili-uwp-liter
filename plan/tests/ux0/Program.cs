using System;
using System.Collections.Generic;
using System.IO;
using BiliLite.Models.Common;

namespace Ux0Probe
{
    /// <summary>
    /// Ux0: the Watch Later queue opens the row that was clicked, and the playlist count only
    /// claims a total the server reported.
    ///
    /// Executed against product source: Models/VideoCollections.cs (VideoPlaylistOrder, the
    /// resolver the page uses instead of the source list index) and Models/MediaListResources.cs
    /// (MediaListPagingState, its cursor, duplicate suppression and total). Pages/VideoDetailPage
    /// and Pages/User/WatchlaterPage cannot run outside UWP, so their wiring is read as text;
    /// every such check is named "source:".
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static int Main()
        {
            ResolverReturnsTheQueuePosition();
            ResolverRejectsRowsOutsideTheQueue();
            FirstPageWithoutTotalStaysUnknown();
            LaterPagesKeepTheKnownTotal();
            DuplicateOnlyPageKeepsCursorAndCount();
            FailedPageIsClearedByTheNextGoodPage();
            WatchLaterPageSourceWiring();
            VideoDetailPageSourceWiring();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        private static void ResolverReturnsTheQueuePosition()
        {
            // 源列表是 [已失效视频, A, B],队列剔掉了失效的那条
            var queue = new List<string>() { "555272312", "666373423" };

            Check(VideoPlaylistOrder.IndexOfItem(queue, "666373423") == 1,
                "the queue, not the source list, decides the position: the filtered-out row shifts nothing");
            Check(VideoPlaylistOrder.IndexOfItem(queue, "555272312") == 0, "the first queue row resolves to 0");

            foreach (var aid in queue)
            {
                var index = VideoPlaylistOrder.IndexOfItem(queue, aid);
                Check(index >= 0 && index < queue.Count,
                    "clicking a row of the queue never lands outside the queue (aid " + aid + ")");
            }

            Check(VideoPlaylistOrder.IndexOfItem(new List<string>() { " 666373423 " }, "666373423") == 0,
                "protocol whitespace does not make the same aid a different row");
            Check(VideoPlaylistOrder.IndexOfItem(new List<string>() { "000666373423" }, "666373423") == 0,
                "the same aid written with leading zeroes is still the same row");
        }

        private static void ResolverRejectsRowsOutsideTheQueue()
        {
            var queue = new List<string>() { "555272312", "666373423" };

            Check(VideoPlaylistOrder.IndexOfItem(queue, "999999999") == -1, "an aid missing from the queue has no position");
            Check(VideoPlaylistOrder.IndexOfItem(queue, null) == -1, "a click without an aid has no position");
            Check(VideoPlaylistOrder.IndexOfItem(queue, "") == -1, "an empty aid has no position");
            Check(VideoPlaylistOrder.IndexOfItem(queue, "0") == -1, "aid 0 is not a row");
            Check(VideoPlaylistOrder.IndexOfItem(queue, "-1") == -1, "a negative aid is not a row");
            Check(VideoPlaylistOrder.IndexOfItem(queue, "BV1iv4y1M7iu") == -1, "a bvid is not an aid");

            Check(VideoPlaylistOrder.IndexOfItem(new List<string>(), "555272312") == -1,
                "an empty queue has no position, so an empty Watch Later list never navigates");
            Check(VideoPlaylistOrder.IndexOfItem(null, "555272312") == -1, "a missing queue has no position");
            Check(VideoPlaylistOrder.IndexOfItem(new List<string>() { "", "666373423" }, "666373423") == 1,
                "a row without an aid is skipped instead of matching");
        }

        private static void FirstPageWithoutTotalStaysUnknown()
        {
            var state = new MediaListPagingState("7788");
            state.Append(Page(11, 12, hasMore: true, totalCount: 0));

            Check(!state.HasKnownTotal, "a page without a total leaves the total unknown");
            Check(state.TotalCount == 0, "no total is invented from the loaded row count");
            Check(state.CountText == "2", "an unknown total shows only the loaded count");
            Check(state.CountText.IndexOf('/') < 0, "an unknown total never renders as a slash fraction");
        }

        private static void LaterPagesKeepTheKnownTotal()
        {
            var state = new MediaListPagingState("7788");
            state.Append(Page(11, 12, hasMore: true, totalCount: 0));
            Check(!state.HasKnownTotal, "the first page still leaves the total unknown");

            state.Append(Page(13, 14, hasMore: true, totalCount: 120));
            Check(state.HasKnownTotal, "a page that reports a total makes the total known");
            Check(state.TotalCount == 120, "the server-reported total is kept as written");
            Check(state.CountText == "4 / 120", "a known total renders as loaded count over total");

            state.Append(Page(15, 16, hasMore: false, totalCount: 0));
            Check(state.HasKnownTotal, "a later page that omits the total does not make it unknown again");
            Check(state.TotalCount == 120, "a later page that omits the total does not erase the known total");
            Check(state.CountText == "6 / 120", "the count keeps the known total after an omitting page");
            Check(!state.HasMore, "has_more still follows the latest page");
        }

        private static void DuplicateOnlyPageKeepsCursorAndCount()
        {
            var state = new MediaListPagingState("7788");
            state.Append(Page(11, 12, hasMore: true, totalCount: 50));
            var cursor = state.NextCursor;

            var added = state.Append(Page(11, 12, hasMore: false, totalCount: 50));

            Check(added == 0, "a page of already loaded rows adds nothing");
            Check(state.LoadedCount == 2, "the loaded count does not move on a duplicate-only page");
            Check(state.NextCursor == cursor, "the cursor does not move back on a duplicate-only page");
            Check(state.CountText == "2 / 50", "the count still shows the known total");
            Check(!state.HasMore, "has_more still follows the duplicate-only page");

            var mixed = state.Append(Page(12, 13, hasMore: true, totalCount: 50));
            Check(mixed == 1, "only the unseen row of a mixed page is added");
            Check(state.NextCursor == "13", "the cursor is the last row of the returned page");
            Check(state.CountText == "3 / 50", "the count follows the loaded rows");
        }

        private static void FailedPageIsClearedByTheNextGoodPage()
        {
            var state = new MediaListPagingState("7788");
            state.Append(Page(11, 12, hasMore: true, totalCount: 50));

            state.MarkLoadFailed();
            Check(state.LastLoadFailed, "a failed page request is recorded");
            Check(state.LoadedCount == 2 && state.NextCursor == "12", "a failed page request keeps rows and cursor");

            state.Append(Page(13, 14, hasMore: true, totalCount: 50));
            Check(!state.LastLoadFailed, "a good page clears the failure flag");
            Check(state.LoadedCount == 4 && state.NextCursor == "14", "the good page is appended after the failure");
        }

        private static void WatchLaterPageSourceWiring()
        {
            var page = ReadSource("src/BiliLite.UWP/Pages/User/WatchlaterPage.xaml.cs");

            Check(!page.Contains("Videos.IndexOf"),
                "source: the click no longer uses the index in the unfiltered source list");
            Check(page.Contains("VideoPlaylistOrder.IndexOfItem(queueAids, data == null ? null : data.aid)"),
                "source: the clicked aid is resolved against the queue that is actually navigated");
            Check(page.Contains("if (index < 0)") && page.Contains("Utils.ShowMessageToast("),
                "source: a click that resolves to nothing is reported instead of navigating");
            Check(page.IndexOf("if (index < 0)", StringComparison.Ordinal)
                < page.IndexOf("MessageCenter.NavigateToPage(", StringComparison.Ordinal),
                "source: the guard runs before navigation");
            Check(page.Contains("if (watchLaterVM.Videos == null) return;"),
                "source: a missing Watch Later list still returns without throwing");
        }

        private static void VideoDetailPageSourceWiring()
        {
            var page = ReadSource("src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs");

            var guard = page.IndexOf("playlist.Index >= playlist.Playlist.Count", StringComparison.Ordinal);
            var index = page.IndexOf("playlist.Playlist[playlist.Index]", StringComparison.Ordinal);
            Check(guard > 0, "source: the in-memory queue is bounds checked before playing");
            Check(page.Contains("playlist.Playlist == null || playlist.Playlist.Count == 0"),
                "source: an empty in-memory queue is refused, not played");
            Check(index > guard, "source: no row is indexed before the bounds check");
            Check(page.Contains("Utils.ShowMessageToast(\"播放列表已失效,请重新打开\");"),
                "source: a bad queue is reported and the page returns instead of playing a wrong video");

            var clampStart = page.IndexOf("private async Task InitializeMediaListPlaylist()", StringComparison.Ordinal);
            var clamp = clampStart < 0 ? -1 : page.IndexOf("Math.Min(Math.Max(0, playlist.Index), mediaListRows.Count - 1)",
                clampStart, StringComparison.Ordinal);
            Check(clamp > clampStart && clampStart > 0,
                "source: the remote playlist clamps its index into the loaded rows too");
            Check(page.Contains("mediaListCountText.Text = \"已加载 \" + mediaListState.CountText;"),
                "source: the header still renders the paging state's own count text");
        }

        private static MediaListResources Page(long firstId, long secondId, bool hasMore, int totalCount)
        {
            return new MediaListResources()
            {
                MediaList = new List<MediaListItem>()
                {
                    new MediaListItem() { Id = firstId, Title = "第" + firstId + "集" },
                    new MediaListItem() { Id = secondId, Title = "第" + secondId + "集" },
                },
                HasMore = hasMore,
                TotalCount = totalCount,
            };
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
                if (File.Exists(Path.Combine(dir.FullName, "src", "BiliLite.UWP", "Models", "VideoCollections.cs")))
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

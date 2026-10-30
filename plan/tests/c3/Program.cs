using System;
using System.Collections.Generic;
using System.IO;
using BiliLite.Models.Common;

namespace C3Probe
{
    /// <summary>
    /// Executed against product source: Models/MediaListResources.cs (the playpage route parser,
    /// the queue's next-row decision and the paging state's failure flag) and
    /// Models/VideoCollections.cs (section/episode playback order).
    /// Pages/VideoDetailPage.xaml(.cs) and Helpers/MessageCenter.cs cannot run outside UWP, so
    /// their wiring is read as text and every such check is named "source:".
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static int Main()
        {
            ParsesPlayPageRoute();
            KeepsTheLinkTypeSeparateFromTheApiDefault();
            RejectsUnrelatedUris();
            BuildsPagingStateFromTheRoute();
            NextActionFollowsLoadedRows();
            NextActionAtTheLastRow();
            NextActionAfterAFailedPage();
            FailedPageKeepsRowsAndCursor();
            CollectionOrder();
            MessageCenterSourceWiring();
            PageSourceWiring();
            ProjectEntries();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        private static void ParsesPlayPageRoute()
        {
            MediaListRoute route;
            Check(MediaListRoute.TryParse("bilibili://music/playlist/playpage/123456", out route),
                "the playpage link is accepted");
            Check(route.MediaListId == "123456", "the id is taken from the link as written");
            Check(MediaListRoute.TryParse("bilibili://music/playlist/playpage/123456/", out route) && route.MediaListId == "123456",
                "a trailing slash keeps the same id");
            Check(MediaListRoute.TryParse("  bilibili://music/playlist/playpage/7788  ", out route) && route.MediaListId == "7788",
                "surrounding whitespace is ignored");
            Check(MediaListRoute.TryParse("BILIBILI://music/playlist/playpage/987", out route) && route.MediaListId == "987",
                "the scheme is matched case-insensitively");
            Check(MediaListRoute.TryParse("bilibili://music/playlist/playpage/42?page_type=2", out route) && route.MediaListId == "42",
                "the query is not part of the id");
            Check(MediaListRoute.TryParse("bilibili://music/playlist/playpage/9001#frag", out route) && route.MediaListId == "9001",
                "a fragment is not part of the id");

            Check(MediaListRoute.TryParse("bilibili://music/playlist/playpage/1", out route)
                && MediaListRoute.TryParse("bilibili://music/playlist/playpage/2", out route) && route.MediaListId == "2",
                "two links keep their own ids, nothing is derived");
        }

        private static void KeepsTheLinkTypeSeparateFromTheApiDefault()
        {
            Check(MediaListRoute.DefaultPlaylistType == 2, "the link default page type is 2");

            MediaListRoute route;
            MediaListRoute.TryParse("bilibili://music/playlist/playpage/123456", out route);
            Check(route.MediaListType == 2, "a link without page_type uses the link default of 2");

            MediaListRoute.TryParse("bilibili://music/playlist/playpage/123456?page_type=1", out route);
            Check(route.MediaListType == 1, "an explicit page_type=1 is kept as written");

            MediaListRoute.TryParse("bilibili://music/playlist/playpage/123456?page_type=2", out route);
            Check(route.MediaListType == 2, "an explicit page_type=2 is kept as written");

            MediaListRoute.TryParse("bilibili://music/playlist/playpage/123456?page_type=12&from=app", out route);
            Check(route.MediaListType == 12, "page_type is read from the first matching query key");

            MediaListRoute.TryParse("bilibili://music/playlist/playpage/123456?from=app&page_type=3", out route);
            Check(route.MediaListType == 3, "page_type is read from any query position");

            MediaListRoute.TryParse("bilibili://music/playlist/playpage/123456?page_type=abc", out route);
            Check(route.MediaListType == 2, "a non-numeric page_type falls back to the link default of 2");

            MediaListRoute.TryParse("bilibili://music/playlist/playpage/123456?page_type=0", out route);
            Check(route.MediaListType == 2, "page_type=0 falls back to the link default of 2");

            var service = ReadSource("src/BiliLite.UWP/Services/MediaListService.cs");
            Check(service.Contains("public const int DefaultMediaListType = 1;"),
                "source: the API method's default type stays 1, separate from the link default");
        }

        private static void RejectsUnrelatedUris()
        {
            MediaListRoute route;
            Check(!MediaListRoute.TryParse("bilibili://music/playlist/playpage/abc", out route), "a non-numeric id is rejected");
            Check(!MediaListRoute.TryParse("bilibili://music/playlist/playpage/123abc", out route), "a partly numeric id is rejected");
            Check(!MediaListRoute.TryParse("bilibili://music/playlist/playpage/", out route), "a link without an id is rejected");
            Check(!MediaListRoute.TryParse("bilibili://music/playlist/playpage", out route), "the bare prefix without an id is rejected");
            Check(!MediaListRoute.TryParse("https://www.bilibili.com/medialist/detail/ml159001856?type=1", out route),
                "the ml favorite-folder link is not this route");
            Check(!MediaListRoute.TryParse("bilibili://music/detail/247991", out route), "an audio link is not this route");
            Check(!MediaListRoute.TryParse("bilibili://music/menu/detail/78723", out route), "a music menu link is not this route");
            Check(!MediaListRoute.TryParse("bilibili://video/19239064", out route), "a video link is not this route");
            Check(!MediaListRoute.TryParse("https://www.bilibili.com/playlist/video/pl688?aid=19827477", out route),
                "a pl playlist link is not this route");
            Check(!MediaListRoute.TryParse("https://www.bilibili.com/video/BV1EE411w75R", out route), "a bv link is not this route");
            Check(!MediaListRoute.TryParse("", out route) && !MediaListRoute.TryParse(null, out route),
                "empty and null urls are rejected");
            Check(route == null, "a rejected url leaves no route behind");
        }

        private static void BuildsPagingStateFromTheRoute()
        {
            MediaListRoute route;
            MediaListRoute.TryParse("bilibili://music/playlist/playpage/159001856", out route);
            var state = new MediaListPagingState(route.MediaListId, route.MediaListType, MediaListSourceKind.MediaList);

            Check(state.MediaListId == "159001856", "the state takes the link id");
            Check(state.MediaListType == 2, "the state takes the link page type, not the API default of 1");
            Check(state.SourceKind == MediaListSourceKind.MediaList, "the state records the remote source kind");
            Check(state.IsFirstPage && state.LoadedCount == 0 && !state.HasMore, "a fresh state starts with no page");
        }

        private static void NextActionFollowsLoadedRows()
        {
            Check(MediaListQueue.NextAction(0, 5, true, false) == MediaListNextAction.PlayNextRow,
                "mid-list with more pages plays the next loaded row");
            Check(MediaListQueue.NextAction(1, 5, false, false) == MediaListNextAction.PlayNextRow,
                "mid-list without more pages plays the next loaded row");
            Check(MediaListQueue.NextAction(3, 5, true, false) == MediaListNextAction.PlayNextRow,
                "a row before the last loaded row plays the next loaded row");
            Check(MediaListQueue.NextAction(2, 5, true, true) == MediaListNextAction.PlayNextRow,
                "a failed next page does not block rows that are already loaded");
        }

        private static void NextActionAtTheLastRow()
        {
            Check(MediaListQueue.NextAction(4, 5, true, false) == MediaListNextAction.LoadNextPage,
                "the last loaded row with HasMore fetches the next page");
            Check(MediaListQueue.NextAction(4, 5, false, false) == MediaListNextAction.Stop,
                "the last loaded row with the source exhausted finishes");
            Check(MediaListQueue.NextAction(0, 1, true, false) == MediaListNextAction.LoadNextPage,
                "a single loaded row with HasMore fetches the next page");
            Check(MediaListQueue.NextAction(0, 1, false, false) == MediaListNextAction.Stop,
                "a single loaded row with the source exhausted finishes");
            Check(MediaListQueue.NextAction(0, 0, false, false) == MediaListNextAction.Stop,
                "an empty queue finishes");
            Check(MediaListQueue.NextAction(-1, 5, true, false) == MediaListNextAction.Stop,
                "no current row finishes");
            Check(MediaListQueue.NextAction(5, 5, true, false) == MediaListNextAction.Stop,
                "a current row past the loaded rows finishes");
        }

        private static void NextActionAfterAFailedPage()
        {
            Check(MediaListQueue.NextAction(4, 5, true, true) == MediaListNextAction.RetryNextPage,
                "the last loaded row with a failed next page waits for a retry instead of finishing");
            Check(MediaListQueue.NextAction(4, 5, false, true) == MediaListNextAction.Stop,
                "a failed page on an exhausted source still finishes");
            Check(MediaListQueue.NextAction(0, 0, true, true) == MediaListNextAction.RetryNextPage,
                "a failed first page is retryable");
            Check(MediaListQueue.NextAction(0, 0, false, true) == MediaListNextAction.RetryNextPage,
                "a failed first page is retryable even without HasMore");
            Check(MediaListQueue.NextAction(4, 5, true, true) != MediaListNextAction.LoadNextPage,
                "a failed next page is not requested again automatically");
        }

        private static void FailedPageKeepsRowsAndCursor()
        {
            var state = new MediaListPagingState("159001856", 2, MediaListSourceKind.MediaList);
            state.Append(Page1());

            Check(state.LoadedCount == 2 && state.NextCursor == "2002" && state.HasMore,
                "a successful page fills rows and the cursor");

            state.MarkLoadFailed();
            Check(state.LastLoadFailed, "a failed request is recorded on the state");
            Check(state.LoadedCount == 2 && state.NextCursor == "2002" && state.HasMore,
                "a failed request keeps the loaded rows, the cursor and HasMore");
            Check(MediaListQueue.NextAction(1, state.LoadedCount, state.HasMore, state.LastLoadFailed)
                    == MediaListNextAction.RetryNextPage,
                "after a failed request the queue asks for a retry rather than skipping");

            state.Append(Page2());
            Check(!state.LastLoadFailed, "a successful page clears the failure flag");
            Check(state.LoadedCount == 3 && state.NextCursor == "2003" && !state.HasMore,
                "the page after a retry appends without duplicating rows");
            Check(MediaListQueue.NextAction(1, state.LoadedCount, state.HasMore, state.LastLoadFailed)
                    == MediaListNextAction.PlayNextRow,
                "the row after a retried page plays normally");
        }

        private static void CollectionOrder()
        {
            var season = Season();
            var order = VideoUgcSeasonOrder.AidOrder(season);

            Check(order.Count == 4, "every episode with an aid joins the order");
            Check(order[0] == "100" && order[1] == "101" && order[2] == "200" && order[3] == "201",
                "episodes follow section order then episode order");
            Check(order.IndexOf("101") + 1 == order.IndexOf("200"),
                "the last episode of a section is followed by the next section");
            Check(VideoUgcSeasonOrder.NextAid(season, "100") == "101", "the next episode inside a section is returned");
            Check(VideoUgcSeasonOrder.NextAid(season, "101") == "200", "the next section's first episode is returned");
            Check(VideoUgcSeasonOrder.NextAid(season, "201") == null, "the last episode of the collection finishes");
            Check(VideoUgcSeasonOrder.NextAid(season, "999") == null, "an aid outside the collection has no next episode");
            Check(VideoUgcSeasonOrder.NextAid(season, null) == null, "no current aid has no next episode");
            Check(VideoUgcSeasonOrder.NextAid(null, "100") == null, "a video without a collection has no next episode");
            Check(VideoUgcSeasonOrder.AidOrder(null).Count == 0, "a video without a collection has no order");
            Check(VideoUgcSeasonOrder.AidOrder(new VideoUgcSeason()).Count == 0, "an empty collection has no order");
        }

        private static void MessageCenterSourceWiring()
        {
            var center = ReadSource("src/BiliLite.UWP/Helpers/MessageCenter.cs");

            Check(center.Contains("MediaListRoute.TryParse(url, out mediaListRoute)"),
                "source: MessageCenter asks the route parser before the other routes");
            Check(center.Contains("await OpenMediaListPlaylist(mediaListRoute);"),
                "source: a matched playpage link opens the playlist instead of falling through");
            Check(center.Contains("GetMediaList(route.MediaListId, route.MediaListType)"),
                "source: the first page is loaded with the link's own id and page type");
            Check(center.Contains("state.Append(page);") && center.Contains("if (state.LoadedCount == 0)"),
                "source: the first page is appended and an empty playlist is reported instead of opening");
            Check(center.Contains("if (page == null) return;"), "source: a failed first page does not open an empty queue");
            Check(center.Contains("parameters = new VideoPlaylist()") && center.Contains("MediaListId = route.MediaListId,")
                && center.Contains("MediaListType = route.MediaListType,") && center.Contains("MediaListState = state,"),
                "source: the navigation carries the remote id, type and the paging state");

            var routeIndex = center.IndexOf("MediaListRoute.TryParse(url, out mediaListRoute)", StringComparison.Ordinal);
            var mlIndex = center.IndexOf("var medialist_id = Utils.RegexMatch(url, @\"ml(\\d+)\");", StringComparison.Ordinal);
            Check(routeIndex > 0 && mlIndex > routeIndex, "source: the playpage branch sits before the ml favorite-folder branch");
            Check(center.Contains("page = typeof(FavoriteDetailPage),") && center.Contains("Id = medialist_id,"),
                "source: the ml favorite-folder route still opens the favorite page with the ml id");
            Check(!center.Contains("MediaListId = medialist_id"), "source: a favorite-folder id is never used as a medialist id");
            Check(center.Contains("private static async Task OpenMediaListPlaylist(MediaListRoute route)"),
                "source: the playlist opening is one helper, not inline page logic");
        }

        private static void PageSourceWiring()
        {
            var page = ReadSource("src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs");

            Check(page.Contains("public MediaListPagingState MediaListState { get; set; }"),
                "source: VideoPlaylist carries the paging state");
            Check(page.Contains("mediaListState = playlist.MediaListState ?? new MediaListPagingState(playlist.MediaListId, playlist.MediaListType, MediaListSourceKind.MediaList, playlist.Title);"),
                "source: the page reuses the state the link supplied");
            Check(page.Contains("AppendMediaListRows(0);") && page.Contains("if (mediaListState.LoadedCount == 0 && !await LoadMediaListPage())"),
                "source: rows already in the state are bound without a second first-page request");
            Check(page.Contains("playlist.MediaListId = mediaListState.MediaListId;") && page.Contains("playlist.MediaListType = mediaListState.MediaListType;"),
                "source: the page takes the id and type from the supported source");
            Check(page.Contains("mediaListState.MarkLoadFailed();"),
                "source: a failed page request is recorded on the state");
            Check(page.Contains("if (!mediaListState.HasMore && !mediaListState.LastLoadFailed) return;"),
                "source: the Load more action retries a failed page without waiting for HasMore");
            Check(page.Contains("if (mediaListState.LastLoadFailed)")
                && page.Contains("mediaListLoadMoreButton.Visibility = Visibility.Visible;"),
                "source: a failed page keeps the retry action visible after the source runs out");
            Check(page.Contains("var action = MediaListQueue.NextAction(")
                && page.Contains("mediaListState != null && mediaListState.HasMore,")
                && page.Contains("mediaListState != null && mediaListState.LastLoadFailed);"),
                "source: auto-advance asks the queue decision with the paging state");
            Check(page.Contains("case MediaListNextAction.LoadNextPage:") && page.Contains("if (await LoadMediaListPage())"),
                "source: the last loaded row fetches the next page before advancing");
            Check(page.Contains("case MediaListNextAction.RetryNextPage:") && page.Contains("RefreshMediaListChrome();")
                && page.Contains("下一页加载失败,点\\\"加载更多\\\"重试"),
                "source: a failed next page leaves the Load more retry visible and does not finish playback");
            Check(page.Contains("private async Task SelectPlaylistRowAsync(int index)")
                && page.Contains("listView.SelectedIndex = index;"),
                "source: advancing selects the next row through the existing selection handler");
            Check(page.Contains("var next = VideoUgcSeasonOrder.NextAid(videoDetailVM?.VideoInfo?.UgcSeason, ugcSeasonAid);"),
                "source: a collection continues in section/episode order");
            Check(page.Contains("playFromUgcSeason = true;") && page.Contains("playFromUgcSeason = false;"),
                "source: the page records whether the current video came from the collection or the queue");
            Check(page.Contains("if (playFromUgcSeason)")
                && page.Contains("if (await AdvanceUgcSeasonAsync()) return;")
                && page.Contains("// 跟着合集走:合集播完就结束,不跳回打开时的队列"),
                "source: a collection episode advances inside the collection, leaving the opened queue alone");
            Check(!page.Contains("playlist.Index == playlist.Playlist.Count - 1"),
                "source: the old stop-at-last-row condition is gone");

            var selectionStart = page.IndexOf("private async void ListView_SelectionChanged", StringComparison.Ordinal);
            var selectionBody = selectionStart < 0 ? "" : page.Substring(selectionStart, 700);
            Check(selectionBody.Contains("playlist.Index = playlist.Playlist.IndexOf(item);")
                && selectionBody.Contains("await InitializeVideo(item.ID);"),
                "source: choosing a row loads that video's details and its own pages");
            Check(!selectionBody.Contains("playlist = "), "source: choosing a row never replaces the active queue");

            Check(page.Contains("playFromUgcSeason = true;") && page.Contains("item.Aid == ugcSeasonAid) return;"),
                "source: an embedded collection stays browseable inside the queued video");
        }

        private static void ProjectEntries()
        {
            var csproj = ReadSource("src/BiliLite.UWP/BiliLite.UWP.csproj");

            Check(csproj.Contains("Models\\MediaListResources.cs"), "source: the media list model is compiled by the UWP project");
            Check(csproj.Contains("Models\\VideoCollections.cs"), "source: the collection model is compiled by the UWP project");
        }

        private static MediaListResources Page1()
        {
            return new MediaListResources()
            {
                MediaList = new List<MediaListItem>()
                {
                    new MediaListItem() { Id = 2001, Title = "第一集" },
                    new MediaListItem() { Id = 2002, Title = "第二集" },
                },
                HasMore = true,
                TotalCount = 3,
            };
        }

        private static MediaListResources Page2()
        {
            return new MediaListResources()
            {
                MediaList = new List<MediaListItem>()
                {
                    new MediaListItem() { Id = 2002, Title = "第二集" },
                    new MediaListItem() { Id = 2003, Title = "第三集" },
                },
                HasMore = false,
                TotalCount = 3,
            };
        }

        private static VideoUgcSeason Season()
        {
            return new VideoUgcSeason()
            {
                Id = 77,
                Title = "合集",
                Sections = new List<VideoUgcSeasonSection>()
                {
                    new VideoUgcSeasonSection()
                    {
                        Id = 1,
                        Title = "上",
                        Episodes = new List<VideoUgcSeasonSectionEpisode>()
                        {
                            new VideoUgcSeasonSectionEpisode() { Aid = "100", Title = "第一集" },
                            new VideoUgcSeasonSectionEpisode() { Aid = "", Title = "占位" },
                            new VideoUgcSeasonSectionEpisode() { Aid = "101", Title = "第二集" },
                        },
                    },
                    new VideoUgcSeasonSection()
                    {
                        Id = 2,
                        Title = "下",
                        Episodes = new List<VideoUgcSeasonSectionEpisode>()
                        {
                            new VideoUgcSeasonSectionEpisode() { Aid = "200", Title = "第三集" },
                            new VideoUgcSeasonSectionEpisode() { Aid = "201", Title = "第四集" },
                        },
                    },
                    null,
                },
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
    }
}

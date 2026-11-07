using System;
using System.Collections.Generic;
using System.IO;
using BiliLite.Models.Common;

namespace Ux2aProbe
{
    /// <summary>
    /// Ux2a: a long loaded list is searchable inside the drawer, and a row resolves to its source
    /// by its stable identity (Key), never by its display position (Ordinal).
    ///
    /// Executed against product source: Models/PlayDirectory.cs (the row → source index resolver and
    /// Filter over the rows the drawer already holds) together with the queue, collection and paging
    /// models it reads. The player, the page and the control cannot run outside UWP, so their wiring
    /// is read as text; every such check is named "source:".
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static int Main()
        {
            ActivatingAFilteredRowPlaysItsSourceIndex();
            ResolverIgnoresTheDisplayPosition();
            StaleRowResolvesToNoIndex();
            PartAndCollectionRowsResolveByIdentity();
            FilterKeepsSourceOrderAndRowIdentity();
            FilterIgnoresCaseAndOuterWhitespace();
            FilterCarriesEverySectionAndTheSourceHeader();
            FilterKeepsCurrentAndPendingRows();
            FilteredOutCurrentRowIsNotLocatable();
            FilteringCoexistsWithPaging();
            PageSourceWiring();
            ControlSourceWiring();
            PlayerSourceWiring();
            DrawerMarkupWiring();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        /// <summary>
        /// 验收场景:过滤后的行是来源里的第 3、7、19 条,点第 19 条播的就是第 19 条
        /// </summary>
        private static void ActivatingAFilteredRowPlaysItsSourceIndex()
        {
            var queue = MarkedQueue(24, 3, 7, 19);
            var model = PlayDirectoryBuilder.FromPlaylist(queue.Item1, null, "5005");
            var filtered = PlayDirectoryBuilder.Filter(model, "重点");
            var rows = Rows(filtered, PlayDirectorySectionKind.Playlist);

            Check(rows.Count == 3, "the filter keeps the marked rows only");
            Check(rows[0].Ordinal == 3 && rows[1].Ordinal == 7 && rows[2].Ordinal == 19,
                "every surviving row keeps the ordinal of its place in the source");
            Check(rows[0].Key == "5003" && rows[1].Key == "5007" && rows[2].Key == "5019",
                "every surviving row keeps the key of its source row");

            var playIndex = PlayDirectoryBuilder.SourceIndexOf(rows[2].Key, queue.Item2);
            Check(playIndex == 18,
                "the row for source index 19 resolves to the nineteenth loaded row");
            Check(queue.Item2[playIndex] == "5019" && queue.Item1.Playlist[playIndex].ID == "5019",
                "activating that filtered row plays the nineteenth item, not the third");
            Check(queue.Item1.Playlist[playIndex].Title.IndexOf("重点", StringComparison.Ordinal) >= 0,
                "the played item is the one the row was showing");
        }

        private static void ResolverIgnoresTheDisplayPosition()
        {
            var queue = MarkedQueue(24, 3, 7, 19);
            var filtered = PlayDirectoryBuilder.Filter(PlayDirectoryBuilder.FromPlaylist(queue.Item1, null, "5005"), "重点");
            var rows = Rows(filtered, PlayDirectorySectionKind.Playlist);

            // 任何只做展示的路径都可能把序号重排成 1、2、3;身份不跟着动
            rows[0].Ordinal = 1;
            Check(PlayDirectoryBuilder.SourceIndexOf(rows[0].Key, queue.Item2) == 2,
                "the resolver reads the key, so a renumbered display row still points at its own source row");
            Check(rows[0].Ordinal - 1 != 2,
                "that renumbered row's ordinal is nowhere near its source index, which is why it cannot be used");
        }

        private static void StaleRowResolvesToNoIndex()
        {
            var keys = MarkedQueue(24, 3, 7, 19).Item2;

            Check(PlayDirectoryBuilder.SourceIndexOf("9999", keys) == -1,
                "a key the source no longer has resolves to no index");
            Check(PlayDirectoryBuilder.SourceIndexOf("", keys) == -1
                && PlayDirectoryBuilder.SourceIndexOf(null, keys) == -1,
                "an empty or missing key never resolves");
            Check(PlayDirectoryBuilder.SourceIndexOf("5019", null) == -1
                && PlayDirectoryBuilder.SourceIndexOf("5019", new List<string>()) == -1,
                "a source with no rows resolves nothing");
            Check(PlayDirectoryBuilder.SourceIndexOf("5019", new List<string>() { null, "", "5019" }) == 2,
                "empty source keys are skipped instead of matching a stale row");
            Check(PlayDirectoryBuilder.SourceIndexOf("part-2", keys) == -1,
                "a positional part key is not a cid in the source");
        }

        private static void PartAndCollectionRowsResolveByIdentity()
        {
            var parts = Parts(Part("cid-1", "P1 开场", 90), Part("cid-2", "P2 正片", 95), Part("cid-3", "P3 结尾", 60));
            var partRows = Rows(PlayDirectoryBuilder.FromParts(parts, 1, "测试视频", false), PlayDirectorySectionKind.Parts);
            var cids = new List<string>();
            foreach (var part in parts) cids.Add(part.Cid);
            Check(PlayDirectoryBuilder.SourceIndexOf(partRows[2].Key, cids) == 2,
                "a part row resolves by its cid");

            // 详情没给 cid 的分P 也有身份:建目录和解析用同一条键规则,行总能回到自己的位置
            var cidless = Rows(PlayDirectoryBuilder.FromParts(
                Parts(Part("", "P1 开场", 10), Part("cid-2", "P2 正片", 10)), 0, "测试视频", false),
                PlayDirectorySectionKind.Parts);
            var minted = new List<string>()
            {
                PlayDirectoryBuilder.PartKey("", 0),
                PlayDirectoryBuilder.PartKey("cid-2", 1),
            };
            Check(cidless[0].Key == "part-1" && PlayDirectoryBuilder.SourceIndexOf(cidless[0].Key, minted) == 0
                && PlayDirectoryBuilder.SourceIndexOf(cidless[1].Key, minted) == 1,
                "a part without a cid still resolves to its own source index");

            var aids = VideoUgcSeasonOrder.AidOrder(Season());
            var collectionRows = Rows(PlayDirectoryBuilder.FromUgcSeason(Season(), "7003"), PlayDirectorySectionKind.UgcSeason);
            Check(PlayDirectoryBuilder.SourceIndexOf(collectionRows[6].Key, aids) == 6,
                "a collection row resolves by its aid");
            Check(PlayDirectoryBuilder.SourceIndexOf("7003", aids) == 2,
                "the playing episode resolves to the index the collection path plays");
        }

        private static void FilterKeepsSourceOrderAndRowIdentity()
        {
            var source = Combined();
            var filtered = PlayDirectoryBuilder.Filter(source, "第");
            var sections = filtered.Sections;

            Check(sections.Count == 3
                && sections[0].Kind == PlayDirectorySectionKind.Parts
                && sections[1].Kind == PlayDirectorySectionKind.UgcSeason
                && sections[2].Kind == PlayDirectorySectionKind.Playlist,
                "the filtered model keeps every section in source order");
            Check(sections[0].Rows.Count == 0,
                "a section with no match stays as an empty section");
            Check(sections[1].Rows.Count == 7 && sections[2].Rows.Count == 6,
                "each section keeps only its own matches");

            var sourceCollection = source.Sections[1];
            Check(ReferenceEquals(sections[1].Rows[0], sourceCollection.Rows[0])
                && ReferenceEquals(sections[1].Rows[6], sourceCollection.Rows[6]),
                "a surviving row is the same row object, so its key, ordinal, current and pending state come along");
            Check(sections[1].Rows[0].Ordinal == 1 && sections[1].Rows[6].Ordinal == 7,
                "the surviving rows keep their source ordinals in source order");
            Check(SameKeys(sections[1].Rows, Rows(source, PlayDirectorySectionKind.UgcSeason)),
                "the surviving rows follow the order the source has");

            Check(source.Query == null && !source.IsFiltered
                && source.Sections[1].Rows.Count == 7 && source.Sections[2].Rows.Count == 6,
                "filtering never touches the model it was given");
            Check(filtered.Query == "第" && filtered.IsFiltered,
                "the filtered model says which query produced it");
        }

        private static void FilterIgnoresCaseAndOuterWhitespace()
        {
            var items = new List<VideoPlaylistItem>();
            items.Add(new VideoPlaylistItem() { ID = "9001", Title = "Live ABC 第一场" });
            items.Add(new VideoPlaylistItem() { ID = "9002", Title = "live abc 第二场" });
            items.Add(new VideoPlaylistItem() { ID = "9003", Title = "  LIVE ABC 围着头尾空白 " });
            items.Add(new VideoPlaylistItem() { ID = "9004", Title = "别的内容" });
            items.Add(new VideoPlaylistItem() { ID = "9005", Title = null });
            var model = PlayDirectoryBuilder.FromPlaylist(new VideoPlaylist() { Playlist = items, Title = "场次" }, null, "9002");

            var filtered = PlayDirectoryBuilder.Filter(model, "  abc  ");
            var rows = Rows(filtered, PlayDirectorySectionKind.Playlist);
            Check(rows.Count == 3 && rows[0].Key == "9001" && rows[1].Key == "9002" && rows[2].Key == "9003",
                "the match ignores case and the whitespace around the query");
            Check(Rows(PlayDirectoryBuilder.Filter(model, "ABC"), PlayDirectorySectionKind.Playlist).Count == 3,
                "an upper-case query matches the same rows");
            Check(Rows(PlayDirectoryBuilder.Filter(model, "别的内容"), PlayDirectorySectionKind.Playlist).Count == 1
                && Rows(PlayDirectoryBuilder.Filter(model, "别的东西"), PlayDirectorySectionKind.Playlist).Count == 0,
                "a query matchers nothing returns no rows");
            Check(ReferenceEquals(PlayDirectoryBuilder.Filter(model, ""), model)
                && ReferenceEquals(PlayDirectoryBuilder.Filter(model, "   "), model)
                && ReferenceEquals(PlayDirectoryBuilder.Filter(model, null), model),
                "an empty query restores the unfiltered model itself");
        }

        private static void FilterCarriesEverySectionAndTheSourceHeader()
        {
            var source = Combined();
            var filtered = PlayDirectoryBuilder.Filter(source, "第7讲");

            Check(filtered.Sections.Count == 3 && filtered.Sections[0].Rows.Count == 0 && filtered.Sections[1].Rows.Count == 1,
                "the drawer still has all three sections to show when only one row matches");
            Check(filtered.HasSections, "a filter with no rows in a section does not remove the section from the drawer");
            Check(filtered.ActiveKind == source.ActiveKind && filtered.HeaderText == source.HeaderText
                && filtered.Caption == source.Caption && filtered.CountText == source.CountText,
                "the header and the loaded count keep describing the source, not the match set");
            Check(filtered.CurrentKey == source.CurrentKey && filtered.CurrentPositionText == source.CurrentPositionText,
                "where playback is does not change because of a search");
            Check(filtered.Sections[1].Rows[0].Key == "7007" && filtered.Sections[1].Rows[0].PositionText == "合集 7/7",
                "a match keeps the position text of its place in the source");
        }

        private static void FilterKeepsCurrentAndPendingRows()
        {
            var source = Combined();
            PlayDirectoryBuilder.ApplyPending(source, "7004");
            var filtered = PlayDirectoryBuilder.Filter(source, "第");

            var current = PlayDirectoryBuilder.CurrentRowOf(filtered, PlayDirectorySectionKind.UgcSeason);
            Check(current != null && current.Key == "7003" && current.IsCurrent
                && current.CurrentBadge == PlayDirectoryBuilder.NowPlayingBadge,
                "the playing row still says it is playing after a search");
            Check(filtered.PendingKey == "7004" && filtered.CurrentKey == "7003",
                "the filtered model keeps both the playing key and the waiting key");
            var pending = RowByKey(filtered, "7004");
            Check(pending != null && pending.IsPending && !pending.IsCurrent,
                "the waiting row keeps its own state, and is still not the playing row");
            Check(!RowByKey(filtered, "7005").IsPending, "the other rows are not marked as waiting");
        }

        private static void FilteredOutCurrentRowIsNotLocatable()
        {
            var source = Combined();
            var filtered = PlayDirectoryBuilder.Filter(source, "第7讲");

            Check(PlayDirectoryBuilder.CurrentRowOf(filtered, PlayDirectorySectionKind.UgcSeason) == null,
                "a filtered-out playing row leaves nothing to locate in that section");
            Check(PlayDirectoryBuilder.CurrentRowOf(source, PlayDirectorySectionKind.UgcSeason) != null,
                "the unfiltered model still knows where playback is");
        }

        private static void FilteringCoexistsWithPaging()
        {
            var remote = RemoteQueue(3, 0, 24, true, "我的收藏");
            var model = PlayDirectoryBuilder.FromPlaylist(remote.Item1, remote.Item2, "4001");
            Check(model.Paging == PlayDirectoryPagingState.More, "a remote queue with more pages says so");
            Check(model.CountText == "3/24", "the count text still counts the loaded rows against the total");

            var filtered = PlayDirectoryBuilder.Filter(model, "第2讲");
            Check(filtered.Paging == PlayDirectoryPagingState.More,
                "searching leaves the paging state alone: the footer still offers the next page");
            Check(filtered.CountText == "3/24",
                "searching does not rewrite the loaded count as if it were a total");
            Check(filtered.Sections[0].Rows.Count == 1 && filtered.Sections[0].Rows[0].Key == "4002",
                "the filtered view holds the loaded rows that match");
            Check(PlayDirectoryBuilder.PagingOf(remote.Item2) == filtered.Paging,
                "the footer state comes from the paging model, not from the search");
        }

        private static void PageSourceWiring()
        {
            var page = ReadSource("src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs");

            var activate = Body(page, "private async Task ActivateDirectoryRowAsync");
            Check(activate.Contains("var rowIndex = DirectorySourceIndex(kind, row);"),
                "source: activation asks the identity resolver for the source index");
            Check(!activate.Contains("row.Ordinal") && !activate.Contains("Ordinal -"),
                "source: activation never turns the display ordinal into a source index");
            var stale = activate.IndexOf("if (rowIndex < 0)", StringComparison.Ordinal);
            var plays = activate.IndexOf("ChangePlayIndex(", StringComparison.Ordinal);
            Check(stale > 0 && plays > stale,
                "source: a row whose key is gone redraws the drawer and returns before anything plays");
            Check(activate.Contains("RedrawOpenDirectory();"),
                "source: the stale row path redraws instead of playing");

            Check(!page.Contains("row.Ordinal") && !page.Contains(".Ordinal - 1"),
                "source: the page has no ordinal arithmetic left in the activation path");

            var resolver = Body(page, "private int DirectorySourceIndex");
            Check(resolver.Contains("PlayDirectoryBuilder.SourceIndexOf(row.Key, PartKeys())")
                && resolver.Contains("PlayDirectoryBuilder.SourceIndexOf(row.Key,\n                        VideoUgcSeasonOrder.AidOrder(videoDetailVM?.VideoInfo?.UgcSeason))")
                && resolver.Contains("PlayDirectoryBuilder.SourceIndexOf(row.Key, PlaylistIds())"),
                "source: the resolver looks the key up in each section's own source order");
            Check(Body(page, "private List<string> PartKeys")
                    .Contains("PlayDirectoryBuilder.PartKey(info == null ? null : info.cid, i)")
                && Body(page, "private List<string> PlaylistIds").Contains("item == null ? null : item.ID"),
                "source: the resolver's key sequences are the part keys the rows were minted with and the aids of the queue rows");

            var playing = Body(page, "private bool IsPlayingDirectoryRow");
            Check(playing.Contains("var index = DirectorySourceIndex(kind, row);")
                && playing.Contains("index == player.CurrentPlayIndex"),
                "source: the playing row is decided by the same resolved index");
            Check(!playing.Contains("string.Equals(row.Key, cid"),
                "source: the playing check no longer compares the row key on its own");
        }

        private static void ControlSourceWiring()
        {
            var control = ReadSource("src/BiliLite.UWP/Controls/PlayDirectoryControl.xaml.cs");
            var xaml = ReadSource("src/BiliLite.UWP/Controls/PlayDirectoryControl.xaml");

            Check(control.Contains("public void SetQuery(string query)"),
                "source: the drawer owns the query");
            Check(control.Contains("return m_query.Length == 0 ? m_source : PlayDirectoryBuilder.Filter(m_source, m_query);"),
                "source: filtering runs against the model the drawer already holds");

            var setQuery = Body(control, "public void SetQuery(string query)");
            Check(!setQuery.Contains("LoadMore") && !setQuery.Contains("ScopeChanged")
                && !setQuery.Contains("RowActivated") && !setQuery.Contains("Requested"),
                "source: filtering starts no page, changes no continuation source and plays nothing");
            Check(!control.Contains("MediaListService") && !control.Contains("GetMediaList")
                && !control.Contains("LoadMoreMediaList"),
                "source: the drawer searches the rows it has instead of asking for more");

            var setModel = Body(control, "public void SetModel(PlayDirectoryModel model)");
            Check(setModel.Contains("m_model = DisplayModel();"),
                "source: a redraw re-applies the active query to the rows that just arrived");
            Check(!setModel.Contains("m_query = \"\"; DisplayModel"),
                "source: a redraw keeps the query instead of dropping it");

            var tabs = Body(control, "private void BuildTabs(PlayDirectoryModel model)");
            Check(tabs.Contains("if (filtering) title += \" \" + count.ToString(CultureInfo.InvariantCulture);"),
                "source: a tab carries the match count as a bare number");
            Check(tabs.Contains("IsEnabled = !filtering || count > 0,"),
                "source: a section with no match stays in the tab row and is disabled");
            Check(!tabs.Contains("Remove") && !control.Contains("Sections.Remove"),
                "source: filtering never removes a section");

            var queryKeys = Body(control, "private void QueryBox_KeyDown");
            Check(queryKeys.Contains("VirtualKey.Enter") && queryKeys.Contains("VirtualKey.Up")
                && queryKeys.Contains("VirtualKey.Down"),
                "source: typing stays in the box while up/down move into the result list");
            Check(queryKeys.Contains("Activate(selected);"),
                "source: Enter activates the focused result row");
            Check(queryKeys.Contains("MoveSelection(")
                && Body(control, "private void MoveSelection").Contains("RowList.SelectedIndex = index;"),
                "source: up/down move the selection inside the filtered rows");

            var locate = Body(control, "public void LocateCurrent()");
            Check(locate.Contains("m_model.IsFiltered") && locate.Contains("CurrentRowOf(m_model, kind)"),
                "source: locate does nothing when the playing row was filtered out, instead of jumping elsewhere");
        }

        private static void PlayerSourceWiring()
        {
            var player = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml.cs");
            var keydown = Body(player, "private async void PlayerControl_KeyDown");

            Check(keydown.Contains("if (DirectoryDrawer.HasQuery) DirectoryDrawer.SetQuery(\"\");")
                && keydown.Contains("else HideDirectory();"),
                "source: Escape clears a non-empty query first and only closes the drawer when the query is empty");
            Check(keydown.IndexOf("if (_directoryMotion.IsShown)", StringComparison.Ordinal)
                    < keydown.IndexOf("ele is TextBox", StringComparison.Ordinal),
                "source: the drawer's Escape handling also applies while the search box has focus");
        }

        private static void DrawerMarkupWiring()
        {
            var xaml = ReadSource("src/BiliLite.UWP/Controls/PlayDirectoryControl.xaml");
            var pageXaml = ReadSource("src/BiliLite.UWP/Pages/VideoDetailPage.xaml");

            var box = Tag(xaml, "x:Name=\"QueryBox\"");
            Check(box.Contains("PlaceholderText=\"搜索\""),
                "source: the search box's placeholder is just 搜索");
            Check(box.Contains("AutomationProperties.Name=\"搜索\""),
                "source: the search box carries the same word for screen readers");
            Check(!box.Contains("（") && !box.Contains("：") && !xaml.Contains("搜索已加载"),
                "source: the search box explains nothing");
            Check(box.Contains("KeyDown=\"QueryBox_KeyDown\"") && box.Contains("TextChanged=\"QueryBox_TextChanged\""),
                "source: the box feeds the drawer on every keystroke and keeps its own keys");

            var search = xaml.IndexOf("x:Name=\"QueryBox\"", StringComparison.Ordinal);
            var tabs = xaml.IndexOf("x:Name=\"TabHost\"", StringComparison.Ordinal);
            var rows = xaml.IndexOf("x:Name=\"RowList\"", StringComparison.Ordinal);
            Check(search > 0 && search < tabs && tabs < rows,
                "source: the search box sits inside the drawer above the tabs and the rows");
            Check(!pageXaml.Contains("QueryBox") && !pageXaml.Contains("PlaceholderText=\"搜索\""),
                "source: the search box is not in the detail column, so full screen and full window keep it");
        }

        private static PlayDirectoryModel Combined()
        {
            var parts = PlayDirectoryBuilder.FromParts(
                Parts(Part("cid-1", "P1 开场", 90), Part("cid-2", "P2 正片", 95)), 1, "测试视频", false);
            var collection = PlayDirectoryBuilder.FromUgcSeason(Season(), "7003");
            var playlist = PlayDirectoryBuilder.FromPlaylist(QueueList(6), null, "3003");
            return PlayDirectoryBuilder.Combine(parts, collection, playlist, WatchSequenceSourceKind.UgcSeason);
        }

        /// <summary>
        /// 24 行的队列,marks 里的那几行标题多带"重点";返回队列和它的 aid 顺序
        /// </summary>
        private static Tuple<VideoPlaylist, List<string>> MarkedQueue(int count, params int[] marks)
        {
            var items = new List<VideoPlaylistItem>();
            var keys = new List<string>();
            for (var i = 1; i <= count; i++)
            {
                var marked = Array.IndexOf(marks, i) >= 0;
                var id = (5000 + i).ToString();
                items.Add(new VideoPlaylistItem()
                {
                    ID = id,
                    Title = "第" + i + "讲" + (marked ? " 重点" : ""),
                    Duration = "10:00",
                });
                keys.Add(id);
            }
            return Tuple.Create(new VideoPlaylist() { Index = 4, Playlist = items, Title = "播放列表" }, keys);
        }

        private static VideoPlaylist QueueList(int count)
        {
            var items = new List<VideoPlaylistItem>();
            for (var i = 1; i <= count; i++)
            {
                items.Add(new VideoPlaylistItem()
                {
                    ID = (3000 + i).ToString(),
                    Title = "第" + i + "讲",
                    Author = "甲老师",
                    Cover = "cover-" + i,
                    Duration = "10:00",
                });
            }
            return new VideoPlaylist() { Index = 2, Playlist = items, Title = "播放列表" };
        }

        private static Tuple<VideoPlaylist, MediaListPagingState> RemoteQueue(int count, int index,
            int totalCount, bool hasMore, string title)
        {
            var page = new MediaListResources()
            {
                MediaList = new List<MediaListItem>(),
                HasMore = hasMore,
                TotalCount = totalCount,
            };
            var items = new List<VideoPlaylistItem>();
            for (var i = 1; i <= count; i++)
            {
                page.MediaList.Add(new MediaListItem() { Id = 4000 + i, Title = "第" + i + "讲", Duration = 600 });
                items.Add(new VideoPlaylistItem() { ID = (4000 + i).ToString(), Title = "第" + i + "讲", Duration = "10:00" });
            }

            var state = new MediaListPagingState("ml-1", 1, MediaListSourceKind.MediaList, title);
            state.Append(page);
            var playlist = new VideoPlaylist()
            {
                Index = index,
                Playlist = items,
                MediaListId = "ml-1",
                MediaListState = state,
                Title = title,
            };
            return Tuple.Create(playlist, state);
        }

        private static VideoUgcSeason Season()
        {
            var first = new VideoUgcSeasonSection() { Title = "第一章", Episodes = new List<VideoUgcSeasonSectionEpisode>() };
            for (var i = 1; i <= 3; i++)
            {
                first.Episodes.Add(Episode("700" + i.ToString(), "第" + i + "讲", 600));
            }
            var second = new VideoUgcSeasonSection() { Title = "第二章", Episodes = new List<VideoUgcSeasonSectionEpisode>() };
            for (var i = 4; i <= 7; i++)
            {
                second.Episodes.Add(Episode("700" + i.ToString(), "第" + i + "讲", 600));
            }
            return new VideoUgcSeason() { Title = "课程合集", Sections = new List<VideoUgcSeasonSection>() { first, second } };
        }

        private static VideoUgcSeasonSectionEpisode Episode(string aid, string title, long seconds)
        {
            return new VideoUgcSeasonSectionEpisode()
            {
                Aid = aid,
                Title = title,
                Author = new VideoAuthor() { Name = "甲老师" },
                Cover = "cover-" + aid,
                Page = new VideoUgcSeasonSectionEpisodePage() { Duration = seconds },
            };
        }

        private static List<PlayDirectoryPart> Parts(params PlayDirectoryPart[] parts)
        {
            return new List<PlayDirectoryPart>(parts);
        }

        private static PlayDirectoryPart Part(string cid, string title, long seconds)
        {
            return new PlayDirectoryPart() { Cid = cid, Title = title, DurationSeconds = seconds };
        }

        private static List<PlayDirectoryRow> Rows(PlayDirectoryModel model, PlayDirectorySectionKind kind)
        {
            var rows = new List<PlayDirectoryRow>();
            if (model == null || model.Sections == null) return rows;
            foreach (var section in model.Sections)
            {
                if (section == null || section.Kind != kind || section.Rows == null) continue;
                foreach (var row in section.Rows)
                {
                    if (row != null) rows.Add(row);
                }
            }
            return rows;
        }

        private static PlayDirectoryRow RowByKey(PlayDirectoryModel model, string key)
        {
            if (model == null || model.Sections == null) return null;
            foreach (var section in model.Sections)
            {
                if (section == null || section.Rows == null) continue;
                foreach (var row in section.Rows)
                {
                    if (row != null && row.Key == key) return row;
                }
            }
            return null;
        }

        private static bool SameKeys(IList<PlayDirectoryRow> rows, List<PlayDirectoryRow> source)
        {
            if (rows.Count != source.Count) return false;
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].Key != source[i].Key) return false;
            }
            return true;
        }

        private static string Tag(string source, string marker)
        {
            var start = source.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return "";
            var body = source.Substring(start, Math.Min(400, source.Length - start));
            var end = body.IndexOf('>');
            return end < 0 ? body : body.Substring(0, end);
        }

        private static string Body(string source, string signature)
        {
            var start = source.IndexOf(signature, StringComparison.Ordinal);
            if (start < 0) return "";
            var length = Math.Min(4000, source.Length - start);
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

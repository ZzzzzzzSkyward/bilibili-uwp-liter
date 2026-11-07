using System;
using System.Collections.Generic;
using System.IO;
using BiliLite.Models.Common;

namespace Ux3Probe
{
    /// <summary>
    /// Ux3: 剧集页的目录是正片、预告两段,段内保持详情页自己的顺序,谁也不混进谁;
    /// 队列带着用户认得的名字到抽屉页头,没有名字就不编一个。
    ///
    /// Executed against product source: Models/PlayDirectory.cs (the season sections, their identity
    /// and current-row rules, the browsed-section header, the footer rule and the queue title rule).
    /// The pages, the player and the drawer cannot run outside UWP, so their wiring is read as text;
    /// every such check is named "source:".
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static int Main()
        {
            TwoOrderedSectionsWithoutCrossContamination();
            RowsKeepTheSourceOrder();
            CurrentMarkerFollowsThePlayingEpisodeId();
            CurrentMarkerNeverLeaksIntoTheOtherSection();
            CaptionIsThePositionInsideTheActiveSectionOnly();
            ActivationResolvesAKeyInItsOwnSection();
            ActivationOfAKeyFromTheOtherSectionIsNotAnIndex();
            SingleEpisodeSeasonHasNoDirectory();
            FullyLoadedSeasonHasNoFooter();
            SearchAndLocateStayInsideTheBrowsedSection();
            HeaderFollowsTheBrowsedSection();
            QueueTitlesAreTheOnesTheUserRecognises();
            QueueWithoutANameHasNoInventedTitle();
            SeasonPageSourceWiring();
            PlayerForwardsScopeOnly();
            DetailPageFollowsTheBrowsedSection();
            QueueProducerSourceWiring();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        // ---------------------------------------------------------------- 剧集页的两段

        private static void TwoOrderedSectionsWithoutCrossContamination()
        {
            var model = SeasonModel("1002", "", false);

            Check(model.Sections.Count == 2, "a season with previews builds exactly two sections");
            Check(model.Sections[0].Kind == PlayDirectorySectionKind.SeasonEpisodes
                && model.Sections[0].Title == "正片", "正片 is the first section and is named 正片");
            Check(model.Sections[1].Kind == PlayDirectorySectionKind.SeasonPreviews
                && model.Sections[1].Title == "预告", "预告 is the second section and is named 预告");

            var episodeKeys = Keys(model.Sections[0]);
            var previewKeys = Keys(model.Sections[1]);
            Check(Join(episodeKeys) == "1001|1002|1003",
                "正片 rows are the season's own episode list in its own order");
            Check(Join(previewKeys) == "2001|2002",
                "预告 rows are the season's own preview list in its own order");
            Check(NoOverlap(episodeKeys, previewKeys), "no episode key appears in both sections");
            Check(KeysOf(model).Count == 5, "the two sections are never merged into one list");
            Check(!episodeKeys.Contains("2001") && !previewKeys.Contains("1001"),
                "a preview is never listed as a 正片 episode and the other way round");
        }

        private static void RowsKeepTheSourceOrder()
        {
            // 详情页给的顺序就是目录的顺序:播出时间、集号大小都不参与排序
            var model = PlayDirectoryBuilder.CombineSeason(
                PlayDirectoryBuilder.FromSeasonSection(PlayDirectorySectionKind.SeasonEpisodes,
                    Episodes("1003", "1001", "1002"), "1001", "剧名"),
                PlayDirectoryBuilder.FromSeasonSection(PlayDirectorySectionKind.SeasonPreviews,
                    Episodes("2002", "2001"), "", "剧名"),
                false);

            Check(Join(Keys(model.Sections[0])) == "1003|1001|1002",
                "the rows are not reordered by episode number or air date");
            Check(model.Sections[0].Rows[0].Ordinal == 1 && model.Sections[0].Rows[2].Ordinal == 3,
                "Ordinal is the display position inside that section");
            Check(model.Sections[0].Rows[0].Title == "第1话 标题1",
                "a row keeps the title the episode list gives it");
        }

        private static void CurrentMarkerFollowsThePlayingEpisodeId()
        {
            var playingEpisode = SeasonModel("1002", "", false);
            Check(Join(CurrentKeys(playingEpisode)) == "1002", "only the playing episode is marked current");
            Check(KeyOf(PlayDirectoryBuilder.CurrentRowOf(playingEpisode, PlayDirectorySectionKind.SeasonEpisodes)) == "1002",
                "the current marker sits in 正片");
            Check(PlayDirectoryBuilder.CurrentRowOf(playingEpisode, PlayDirectorySectionKind.SeasonPreviews) == null,
                "预告 has no current row while 正片 plays");
            Check(playingEpisode.ActiveKind == PlayDirectorySectionKind.SeasonEpisodes,
                "the active section is the one that is playing");
            Check(playingEpisode.CurrentPositionText == "2/3",
                "the position is the ordinal inside 正片");

            var playingPreview = SeasonModel("", "2002", true);
            Check(Join(CurrentKeys(playingPreview)) == "2002", "the marker follows the episode id into 预告");
            Check(KeyOf(PlayDirectoryBuilder.CurrentRowOf(playingPreview, PlayDirectorySectionKind.SeasonPreviews)) == "2002",
                "the current marker sits in 预告");
            Check(PlayDirectoryBuilder.CurrentRowOf(playingPreview, PlayDirectorySectionKind.SeasonEpisodes) == null,
                "正片 has no current row while a preview plays");
            Check(playingPreview.ActiveKind == PlayDirectorySectionKind.SeasonPreviews,
                "the active section follows the preview that is playing");
            Check(playingPreview.CurrentPositionText == "2/2",
                "the position is the ordinal inside 预告");
        }

        private static void CurrentMarkerNeverLeaksIntoTheOtherSection()
        {
            // 当前 id 只交给它自己那一段:另一段拿到空 id,行上不会有当前标记
            var model = SeasonModel("1001", "", false);
            Check(CountCurrent(model) == 1, "one playing episode produces exactly one current row");
            Check(Join(CurrentKeys(model)) == "1001", "the other section never carries the same marker");

            // 分集 id 不是位置:拿预告的 id 去建正片段,正片段里一条都不标
            var wrongSection = PlayDirectoryBuilder.FromSeasonSection(PlayDirectorySectionKind.SeasonEpisodes,
                Episodes("1001", "1002"), "2001", "剧名");
            Check(CountCurrent(wrongSection) == 0,
                "a preview id marks nothing in 正片, not even the row at the same position");

            // 两段都拿到同一个 id 时也只标它自己那一段能对上的行:身份是 id,不是序号
            var both = PlayDirectoryBuilder.CombineSeason(
                PlayDirectoryBuilder.FromSeasonSection(PlayDirectorySectionKind.SeasonEpisodes, Episodes("1001", "1002"), "1002", "剧名"),
                PlayDirectoryBuilder.FromSeasonSection(PlayDirectorySectionKind.SeasonPreviews, Episodes("2001", "2002"), "", "剧名"),
                false);
            Check(CountCurrent(both) == 1 && Join(CurrentKeys(both)) == "1002",
                "the current marker stays inside the section that holds the playing id");
        }

        private static void CaptionIsThePositionInsideTheActiveSectionOnly()
        {
            var episode = SeasonModel("1003", "", false);
            Check(episode.Caption == "3/3", "the 目录 caption counts inside 正片 only");
            var preview = SeasonModel("", "2001", true);
            Check(preview.Caption == "1/2", "the 目录 caption counts inside 预告 only");
            Check(PlayDirectoryBuilder.Caption(PlayDirectorySectionKind.SeasonPreviews, 0, 2) == "",
                "no current row in the active section means no position on the button");
            Check(PlayDirectoryBuilder.Caption(PlayDirectorySectionKind.SeasonEpisodes, 2, 3) == "2/3",
                "the season caption is a position, not a section name");
        }

        private static void ActivationResolvesAKeyInItsOwnSection()
        {
            var model = SeasonModel("1002", "", false);
            var episodeKeys = Keys(model.Sections[0]);
            var previewKeys = Keys(model.Sections[1]);

            Check(PlayDirectoryBuilder.SourceIndexOf("1003", episodeKeys) == 2,
                "a 正片 key resolves to its index in the episode list");
            Check(PlayDirectoryBuilder.SourceIndexOf("2001", previewKeys) == 0,
                "a 预告 key resolves to its index in the preview list");

            // 搜过之后 Ordinal 还是来源位置,行在列表里的位置却变了:身份只认 Key
            var filtered = PlayDirectoryBuilder.Filter(model, "标题3");
            var row = filtered.Sections[0].Rows[0];
            Check(row.Ordinal == 3 && PlayDirectoryBuilder.SourceIndexOf(row.Key, episodeKeys) == 2,
                "a filtered row still resolves to its source index");
            Check(PlayDirectoryBuilder.SourceIndexOf(row.Key, previewKeys) == -1,
                "a 正片 key found in 正片 is not a 预告 index");
        }

        private static void ActivationOfAKeyFromTheOtherSectionIsNotAnIndex()
        {
            var model = SeasonModel("1002", "", false);
            var episodeKeys = Keys(model.Sections[0]);
            var previewKeys = Keys(model.Sections[1]);

            Check(PlayDirectoryBuilder.SourceIndexOf("2002", episodeKeys) == -1,
                "a 预告 key has no index in the 正片 list");
            Check(PlayDirectoryBuilder.SourceIndexOf("1001", previewKeys) == -1,
                "a 正片 key has no index in the 预告 list");
            Check(PlayDirectoryBuilder.SourceIndexOf("", episodeKeys) == -1
                && PlayDirectoryBuilder.SourceIndexOf("9999", episodeKeys) == -1,
                "an empty or stale key is never treated as an index");
        }

        private static void SingleEpisodeSeasonHasNoDirectory()
        {
            Check(!PlayDirectoryBuilder.HasSeasonDirectory(1, 0),
                "a season with a single episode and no previews gets no 目录 button");
            Check(!PlayDirectoryBuilder.HasSeasonDirectory(0, 1),
                "a single preview with no episodes gets no 目录 button either");
            Check(!PlayDirectoryBuilder.HasSeasonDirectory(0, 0), "nothing to play means no directory");
            Check(PlayDirectoryBuilder.HasSeasonDirectory(2, 0), "two episodes expose the directory");
            Check(PlayDirectoryBuilder.HasSeasonDirectory(1, 1),
                "one episode plus a preview exposes the directory");

            var single = PlayDirectoryBuilder.CombineSeason(
                PlayDirectoryBuilder.FromSeasonSection(PlayDirectorySectionKind.SeasonEpisodes, Episodes("1001"), "1001", "剧名"),
                PlayDirectoryBuilder.FromSeasonSection(PlayDirectorySectionKind.SeasonPreviews,
                    new List<PlayDirectorySeasonEpisode>(), "", "剧名"),
                false);
            Check(single.Sections.Count == 1, "an empty section is not shown");
            Check(single.Sections[0].Kind == PlayDirectorySectionKind.SeasonEpisodes,
                "the single section left is the one that has rows");

            var onlyPreviews = PlayDirectoryBuilder.CombineSeason(
                PlayDirectoryBuilder.FromSeasonSection(PlayDirectorySectionKind.SeasonEpisodes,
                    new List<PlayDirectorySeasonEpisode>(), "", "剧名"),
                PlayDirectoryBuilder.FromSeasonSection(PlayDirectorySectionKind.SeasonPreviews, Episodes("2001", "2002"), "2001", "剧名"),
                true);
            Check(onlyPreviews.Sections.Count == 1
                && onlyPreviews.Sections[0].Kind == PlayDirectorySectionKind.SeasonPreviews,
                "a season with only previews shows only the 预告 section");
        }

        private static void FullyLoadedSeasonHasNoFooter()
        {
            var model = SeasonModel("1002", "", false);
            Check(model.Paging == PlayDirectoryPagingState.None,
                "a season list is fully loaded, so it carries no paging state");

            var browsingEpisodes = PlayDirectoryBuilder.FooterOf(model.Paging, LoadAllState.Idle,
                HasSection(model, PlayDirectorySectionKind.Playlist), PlayDirectorySectionKind.SeasonEpisodes);
            Check(!browsingEpisodes.HasContent, "the drawer footer renders nothing for a fully loaded list");
            var browsingPreviews = PlayDirectoryBuilder.FooterOf(model.Paging, LoadAllState.Idle,
                HasSection(model, PlayDirectorySectionKind.Playlist), PlayDirectorySectionKind.SeasonPreviews);
            Check(!browsingPreviews.HasContent, "no footer in 预告 either");
            Check(string.IsNullOrEmpty(browsingEpisodes.PrimaryText)
                && string.IsNullOrEmpty(browsingEpisodes.SecondaryText)
                && !browsingEpisodes.ShowRing,
                "nothing is left in the footer: no ring, no 加载更多, no 加载全部");
        }

        private static void SearchAndLocateStayInsideTheBrowsedSection()
        {
            var model = SeasonModel("1002", "", false);
            // 预告那两条叫 标题1、标题2,只有正片的第三条叫 标题3
            var filtered = PlayDirectoryBuilder.Filter(model, "标题3");

            Check(filtered.Sections.Count == 2, "searching keeps both sections visible");
            Check(filtered.Sections[0].Rows.Count == 1 && filtered.Sections[0].Rows[0].Key == "1003",
                "search matches inside 正片 only");
            Check(filtered.Sections[1].Rows.Count == 0, "a section with no hit stays empty, it is not filled from the other");

            // 定位:先看正在浏览的那一段,这一段没有当前行才退到连播来源那一段
            Check(PlayDirectoryBuilder.LocateSection(model, PlayDirectorySectionKind.SeasonPreviews)
                    == PlayDirectorySectionKind.SeasonEpisodes,
                "locate falls back to the section that is playing when the browsed one has no current row");
            Check(PlayDirectoryBuilder.LocateSection(model, PlayDirectorySectionKind.SeasonEpisodes)
                    == PlayDirectorySectionKind.SeasonEpisodes,
                "locate stays in the browsed section when that section holds the current row");
            Check(PlayDirectoryBuilder.ResolveBrowsedSection(model, PlayDirectorySectionKind.SeasonPreviews)
                    == PlayDirectorySectionKind.SeasonPreviews,
                "a redraw keeps the section the user is browsing");
            Check(PlayDirectoryBuilder.ResolveBrowsedSection(model, null) == PlayDirectorySectionKind.SeasonEpisodes,
                "a fresh open lands on the section that is playing");
        }

        private static void HeaderFollowsTheBrowsedSection()
        {
            var model = SeasonModel("1002", "", false);
            Check(model.HeaderText == "正片", "the header names the section the drawer opens on");
            Check(model.SourceTitle == "剧名", "the season title stays on its own line");
            Check(string.IsNullOrEmpty(model.CountText),
                "the header does not restate the count the position already carries");

            Check(PlayDirectoryBuilder.HeaderOf(PlayDirectorySectionKind.SeasonEpisodes, "剧名") == "正片"
                && PlayDirectoryBuilder.HeaderOf(PlayDirectorySectionKind.SeasonPreviews, "剧名") == "预告",
                "the season header never repeats the season title");

            var browsed = PlayDirectoryBuilder.WithHeader(model, PlayDirectorySectionKind.SeasonPreviews);
            Check(browsed.HeaderText == "预告", "the header follows the browsed section after a scope change");
            Check(browsed.ActiveKind == PlayDirectorySectionKind.SeasonEpisodes && browsed.Sections.Count == 2,
                "browsing another section does not change which section is playing");
            Check(PlayDirectoryBuilder.WithHeader(model, PlayDirectorySectionKind.Playlist).HeaderText == "预告",
                "a section that is not in the directory leaves the header alone");

            var filtered = PlayDirectoryBuilder.Filter(model, "标题2");
            Check(PlayDirectoryBuilder.WithHeader(filtered, PlayDirectorySectionKind.SeasonPreviews).HeaderText == "预告",
                "a filtered directory still names the browsed section");
            Check(filtered.Sections[0].SourceTitle == "剧名",
                "filtering keeps each section's own name for the header");

            // 视频页那一份:页头写来源和名字,名字跟着用户正在浏览的那一段
            var playlistSection = PlayDirectoryBuilder.FromPlaylist(
                new VideoPlaylist() { Playlist = Items("11", "12"), Title = "稍后再看" }, null, "11");
            var video = PlayDirectoryBuilder.Combine(null, Collection("合集标题", "21", "22"), playlistSection,
                WatchSequenceSourceKind.Playlist);
            Check(video.HeaderText == "播放列表《稍后再看》", "the video page header names the playlist it opens on");
            Check(PlayDirectoryBuilder.WithHeader(video, PlayDirectorySectionKind.UgcSeason).HeaderText == "合集《合集标题》",
                "the video page header follows the browsed section too");
            Check(PlayDirectoryBuilder.WithHeader(video, PlayDirectorySectionKind.Playlist).HeaderText == "播放列表《稍后再看》",
                "switching back to the playlist section restores its header");

            // BR-31:段名只陈述事实,不带括号解释、不带 ·
            foreach (var label in new[] { "正片", "预告" })
            {
                Check(label.IndexOf('（') < 0 && label.IndexOf('·') < 0 && label.IndexOf('：') < 0,
                    "BR-31: " + label + " carries no parenthetical, no · and no ：");
            }
        }

        // ---------------------------------------------------------------- 队列的名字

        private static void QueueTitlesAreTheOnesTheUserRecognises()
        {
            var watchLater = new VideoPlaylist() { Playlist = Items("11", "12"), Title = "稍后再看" };
            var watchModel = PlayDirectoryBuilder.FromPlaylist(watchLater, null, "11");
            Check(watchModel.SourceTitle == "稍后再看", "the Watch Later queue carries its own name");
            var watchCombined = PlayDirectoryBuilder.Combine(null, null, watchModel, WatchSequenceSourceKind.Playlist);
            Check(watchCombined.HeaderText == "播放列表《稍后再看》",
                "the drawer's playlist header shows that name");

            var favorite = new VideoPlaylist() { Playlist = Items("21", "22"), Title = "我喜欢的视频" };
            var favoriteModel = PlayDirectoryBuilder.FromPlaylist(favorite, null, "21");
            Check(favoriteModel.SourceTitle == "我喜欢的视频",
                "the Favorites queue carries the favorite's own title");

            var linked = new MediaListPagingState("777", 2, MediaListSourceKind.MediaList, "链接里的名字");
            var linkedModel = PlayDirectoryBuilder.FromPlaylist(
                new VideoPlaylist() { MediaListId = "777", Playlist = Items("31", "32"), Title = "链接里的名字" },
                linked, "31");
            Check(linkedModel.SourceTitle == "链接里的名字",
                "the launcher's playlist title reaches the drawer");
            Check(PlayDirectoryBuilder.PlaylistTitleOf(null, linked) == "链接里的名字",
                "the paging state's own name is used when the queue carries none");
        }

        private static void QueueWithoutANameHasNoInventedTitle()
        {
            var unnamedState = new MediaListPagingState("888", 2, MediaListSourceKind.MediaList);
            var unnamed = PlayDirectoryBuilder.FromPlaylist(
                new VideoPlaylist() { MediaListId = "888", Playlist = Items("41", "42"), Title = "" },
                unnamedState, "41");
            Check(unnamed.SourceTitle == "", "a medialist opened by id without a name gets no invented title");

            var combined = PlayDirectoryBuilder.Combine(null, null, unnamed, WatchSequenceSourceKind.Playlist);
            Check(combined.HeaderText == "播放列表", "with no title the header stays bare");
            Check(combined.HeaderText.IndexOf('《') < 0, "no empty 《》 pair stands in for a missing name");

            Check(PlayDirectoryBuilder.PlaylistTitleOf(null, unnamedState) == "",
                "the paging state's placeholder word is not a title");
            Check(PlayDirectoryBuilder.PlaylistTitleOf(new VideoPlaylist(), null) == "",
                "no queue and no paging state means no title");
            Check(PlayDirectoryBuilder.PlaylistTitleOf(new VideoPlaylist() { Title = "稍后再看" }, unnamedState) == "稍后再看",
                "the queue's own name wins over the placeholder");
            Check(unnamed.Sections.Count == 1 && unnamed.Sections[0].SourceTitle == "",
                "the section carries the same empty name, so the header stays bare after a redraw");
            Check(PlayDirectoryBuilder.WithHeader(combined, PlayDirectorySectionKind.Playlist).HeaderText == "播放列表",
                "browsing the playlist section does not invent a name either");
        }

        // ---------------------------------------------------------------- 接线

        private static void SeasonPageSourceWiring()
        {
            var page = ReadSource("src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs");

            Check(page.Contains("player.DirectoryRequested += Player_DirectoryRequested;")
                && page.Contains("player.DirectoryRowRequested += Player_DirectoryRowRequested;")
                && page.Contains("player.DirectoryScopeChanged += Player_DirectoryScopeChanged;"),
                "source: the season page answers the player's directory events");

            var build = Body(page, "private PlayDirectoryModel BuildSeasonDirectory");
            Check(build.Contains("PlayDirectoryBuilder.FromSeasonSection(")
                && build.Contains("PlayDirectoryBuilder.CombineSeason("),
                "source: the season directory is built from the season's own two lists");
            Check(CountOf(build, "PlayDirectoryBuilder.FromSeasonSection(") == 2
                && build.Contains("SeasonEpisodeRows(false)") && build.Contains("SeasonEpisodeRows(true)"),
                "source: each section is built from its own list, never from a merged one");
            Check(!build.Contains("Concat") && !build.Contains("OrderBy"),
                "source: the two lists are never concatenated and never reordered by air date");
            Check(build.Contains("selectPreview ? \"\" : ep_id") && build.Contains("selectPreview ? ep_id : \"\""),
                "source: the current episode id is handed to its own section only");

            var activate = Body(page, "private void Player_DirectoryRowRequested");
            Check(activate.Contains("PlayDirectoryBuilder.SourceIndexOf(e.Row.Key, SeasonEpisodeKeys(preview))"),
                "source: activation resolves the row's Key into its own section's list");
            Check(activate.Contains("listEpisode.SelectedIndex = index;") && activate.Contains("listPreview.SelectedIndex = index;"),
                "source: activation reuses the page's own episode lists instead of a second switch path");
            Check(!activate.Contains("ChangePlayIndex") && !activate.Contains("InitializePlayInfo"),
                "source: activation starts nothing itself; the existing selection path does the switch");
            Check(activate.Contains("index == CurrentSeasonIndex()"),
                "source: activating the row that is already playing does not restart it");

            var change = Body(page, "private void PlayerControl_ChangeEpisodeEvent");
            Check(change.Contains("RefreshSeasonDirectory();"),
                "source: the current marker follows the player's episode change");

            var refresh = Body(page, "private void RefreshSeasonDirectory");
            Check(refresh.Contains("player.UpdateDirectoryInfo(") && refresh.Contains("SeasonDirectoryCaption()"),
                "source: the 目录 button carries the position the page computed");

            var caption = Body(page, "private string SeasonDirectoryCaption");
            Check(caption.Contains("PlayDirectoryBuilder.Caption(") && caption.Contains("CurrentSeasonIndex()"),
                "source: the caption is the position inside the active section only");

            var available = Body(page, "private bool HasSeasonDirectory");
            Check(available.Contains("PlayDirectoryBuilder.HasSeasonDirectory(episodes, previews)"),
                "source: whether a season has a directory is the model's rule, not a second copy");

            var initialize = Body(page, "private void InitializePlayInfo");
            Check(initialize.Contains("RefreshSeasonDirectory();"),
                "source: opening a season reports the directory as soon as the first episode is set");

            var requested = Body(page, "private void Player_DirectoryRequested");
            Check(requested.Contains("player.ShowDirectory(BuildSeasonDirectory("),
                "source: the 目录 button opens the drawer with the season model");

            var scope = Body(page, "private void Player_DirectoryScopeChanged");
            Check(scope.Contains("browsedDirectoryKind = e.Kind;") && scope.Contains("BuildSeasonDirectory(false)"),
                "source: a scope change repushes the model with the browsed section's header");
        }

        private static void PlayerForwardsScopeOnly()
        {
            var player = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml.cs");
            var xaml = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml");

            Check(player.Contains("public event EventHandler<PlayDirectorySourceEventArgs> DirectoryScopeChanged;"),
                "source: the player exposes the drawer's scope change");
            var forward = Body(player, "private void DirectoryDrawer_ScopeChanged");
            Check(forward.Contains("DirectoryScopeChanged?.Invoke(this, e);"),
                "source: the player only forwards the browsed section");
            Check(forward.IndexOf("Season", StringComparison.Ordinal) < 0
                && forward.IndexOf("Header", StringComparison.Ordinal) < 0,
                "source: the player carries no season knowledge and no header logic");
            Check(xaml.Contains("ScopeChanged=\"DirectoryDrawer_ScopeChanged\""),
                "source: the drawer's scope event is wired in the player markup");
        }

        private static void DetailPageFollowsTheBrowsedSection()
        {
            var detail = ReadSource("src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs");

            Check(detail.Contains("player.DirectoryScopeChanged += player_DirectoryScopeChanged;"),
                "source: the video page subscribes to the same scope change");
            var scope = Body(detail, "private void player_DirectoryScopeChanged");
            Check(scope.Contains("browsedDirectoryKind = e.Kind;") && scope.Contains("BuildDirectoryModel(false)"),
                "source: the video page repushes the model with the browsed section");
            var build = Body(detail, "private PlayDirectoryModel BuildDirectoryModel");
            Check(build.Contains("PlayDirectoryBuilder.WithHeader(model, browsedDirectoryKind.Value)"),
                "source: a redraw keeps the header of the section the user is browsing");
            Check(build.Contains("if (freshOpen) browsedDirectoryKind = null;"),
                "source: a fresh open starts on the section that is playing");
        }

        private static void QueueProducerSourceWiring()
        {
            var watch = ReadSource("src/BiliLite.UWP/Pages/User/WatchlaterPage.xaml.cs");
            Check(watch.Contains("Title = \"稍后再看\""),
                "source: Watch Later names its queue 稍后再看");

            var favorite = ReadSource("src/BiliLite.UWP/Pages/User/FavoriteDetailPage.xaml.cs");
            Check(favorite.Contains("Title = favoriteDetailVM.FavoriteInfo == null ? \"\" : favoriteDetailVM.FavoriteInfo.title"),
                "source: Favorites name their queue with the favorite's own title");

            var center = ReadSource("src/BiliLite.UWP/Helpers/MessageCenter.cs");
            Check(center.Contains("await OpenMediaListPlaylist(mediaListRoute, PlaylistTitleOf(url));"),
                "source: the launcher passes the link's title into the queue");
            var open = Body(center, "private static async Task OpenMediaListPlaylist");
            Check(open.Contains("Title = title,") && !open.Contains("Title = state.Title,"),
                "source: the launcher queue takes the link's title, not the placeholder");
            var title = Body(center, "private static string PlaylistTitleOf");
            Check(title.Contains("\"title\"") && title.Contains("return \"\";"),
                "source: a link without a title yields no title");
        }

        // ---------------------------------------------------------------- 夹具

        private static PlayDirectoryModel SeasonModel(string currentEpId, string previewCurrentEpId, bool previewsActive)
        {
            var episodes = PlayDirectoryBuilder.FromSeasonSection(PlayDirectorySectionKind.SeasonEpisodes,
                Episodes("1001", "1002", "1003"), currentEpId, "剧名");
            var previews = PlayDirectoryBuilder.FromSeasonSection(PlayDirectorySectionKind.SeasonPreviews,
                Episodes("2001", "2002"), previewCurrentEpId, "剧名");
            return PlayDirectoryBuilder.CombineSeason(episodes, previews, previewsActive);
        }

        private static List<PlayDirectorySeasonEpisode> Episodes(params string[] ids)
        {
            var rows = new List<PlayDirectorySeasonEpisode>();
            for (var i = 0; i < ids.Length; i++)
            {
                rows.Add(new PlayDirectorySeasonEpisode()
                {
                    EpId = ids[i],
                    Title = "第" + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)
                        + "话 标题" + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Cover = "",
                });
            }
            return rows;
        }

        private static List<VideoPlaylistItem> Items(params string[] ids)
        {
            var items = new List<VideoPlaylistItem>();
            foreach (var id in ids) items.Add(new VideoPlaylistItem() { ID = id, Title = "视频" + id });
            return items;
        }

        private static PlayDirectoryModel Collection(string title, params string[] aids)
        {
            var episodes = new List<VideoUgcSeasonSectionEpisode>();
            foreach (var aid in aids) episodes.Add(new VideoUgcSeasonSectionEpisode() { Aid = aid, Title = "分集" + aid });
            return PlayDirectoryBuilder.FromUgcSeason(new VideoUgcSeason()
            {
                Title = title,
                Sections = new List<VideoUgcSeasonSection>()
                {
                    new VideoUgcSeasonSection() { Title = "正片", Episodes = episodes },
                },
            }, aids.Length == 0 ? "" : aids[0]);
        }

        private static List<string> Keys(PlayDirectorySection section)
        {
            var keys = new List<string>();
            if (section == null || section.Rows == null) return keys;
            foreach (var row in section.Rows) keys.Add(row == null ? "" : row.Key);
            return keys;
        }

        private static List<string> KeysOf(PlayDirectoryModel model)
        {
            var keys = new List<string>();
            if (model == null || model.Sections == null) return keys;
            foreach (var section in model.Sections) keys.AddRange(Keys(section));
            return keys;
        }

        private static List<string> CurrentKeys(PlayDirectoryModel model)
        {
            var keys = new List<string>();
            if (model == null || model.Sections == null) return keys;
            foreach (var section in model.Sections)
            {
                if (section == null || section.Rows == null) continue;
                foreach (var row in section.Rows)
                {
                    if (row != null && row.IsCurrent) keys.Add(row.Key);
                }
            }
            return keys;
        }

        private static int CountCurrent(PlayDirectoryModel model)
        {
            return CurrentKeys(model).Count;
        }

        private static string KeyOf(PlayDirectoryRow row)
        {
            return row == null ? "" : row.Key;
        }

        private static bool HasSection(PlayDirectoryModel model, PlayDirectorySectionKind kind)
        {
            if (model == null || model.Sections == null) return false;
            foreach (var section in model.Sections)
            {
                if (section != null && section.Kind == kind) return true;
            }
            return false;
        }

        private static string Join(List<string> values)
        {
            return string.Join("|", values.ToArray());
        }

        private static bool NoOverlap(List<string> left, List<string> right)
        {
            foreach (var value in left)
            {
                if (right.Contains(value)) return false;
            }
            return true;
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

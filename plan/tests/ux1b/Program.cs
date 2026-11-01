using System;
using System.Collections.Generic;
using System.IO;
using BiliLite.Models.Common;

namespace Ux1bProbe
{
    /// <summary>
    /// Ux1b: the 目录 directory shows the full outer list in every viewing mode.
    ///
    /// Executed against product source: Models/PlayDirectory.cs (the directory model and the pure
    /// builder: captions, ordinals, identity, section presence, count honesty) together with the
    /// queue, collection and paging models it reads. The player, the page and the control cannot run
    /// outside UWP, so their wiring is read as text; every such check is named "source:".
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static int Main()
        {
            PartsDirectory();
            PartsPresence();
            CollectionDirectory();
            CollectionWithoutCurrentRow();
            FixedPlaylistDirectory();
            RemotePlaylistCounts();
            CountTextNeverFakesCompletion();
            CombineKeepsSourceAndOrder();
            CombineFallsBackWithoutTheActiveSection();
            IdentitySurvivesDuplicateTitles();
            PlaylistCurrentFollowsPlayingAid();
            LocateFollowsBrowsedSection();
            BrowsedSectionSurvivesRedraw();
            PlayerSourceWiring();
            PageSourceWiring();
            ControlSourceWiring();
            ProjectEntries();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        private static void PartsDirectory()
        {
            var parts = Parts(
                Part("cid-1", "P1 开场", 90),
                Part("cid-2", "P2 正片", 95),
                Part("cid-3", "P3 结尾", 3672));

            var model = PlayDirectoryBuilder.FromParts(parts, 1, "测试视频", false);

            Check(model.Caption == "2/3", "the parts caption says which part of the video plays");
            Check(model.CountText == "3", "the parts count text counts the parts");
            Check(model.SourceTitle == "测试视频", "the parts section carries the video title");
            Check(model.ActiveKind == PlayDirectorySectionKind.Parts, "parts are the active section for the parts model");

            var rows = Rows(model, PlayDirectorySectionKind.Parts);
            Check(rows.Count == 3, "every part becomes a row");
            Check(rows[0].Ordinal == 1 && rows[1].Ordinal == 2 && rows[2].Ordinal == 3,
                "ordinals are derived from the position in the section");
            Check(rows[1].IsCurrent && !rows[0].IsCurrent && !rows[2].IsCurrent,
                "only the playing part is flagged as current");
            Check(rows[0].Key == "cid-1" && rows[1].Key == "cid-2" && rows[2].Key == "cid-3",
                "a part row is identified by its cid, never by the display index");
            Check(model.CurrentKey == "cid-2", "the model exposes the current row's key for locating");
            Check(model.CurrentPositionText == "P2/3", "the current position text names the part");
            Check(rows[0].Duration == "01:30" && rows[2].Duration == "01:01:12",
                "row durations are formatted, hours included");
            Check(rows[1].Title == "P2 正片", "the row keeps the part title");
            Check(PlayDirectoryBuilder.FormatDuration(600) == "10:00"
                && PlayDirectoryBuilder.FormatDuration(59) == "00:59"
                && Rows(PlayDirectoryBuilder.FromParts(Parts(Part("cid-9", "十分钟", 600), Part("cid-8", "一分钟", 60)), 0, "x", false),
                    PlayDirectorySectionKind.Parts)[0].Duration == "10:00",
                "a ten-minute part is written as 10:00 from its seconds value");
            Check(rows[1].CurrentBadge == "正在播放" && rows[0].CurrentBadge == "" && rows[2].CurrentBadge == "",
                "only the playing row carries the 正在播放 badge");
        }

        private static void PartsPresence()
        {
            var single = PlayDirectoryBuilder.FromParts(Parts(Part("cid-1", "P1 全部", 60)), 0, "单P视频", false);
            Check(!single.HasSections, "a one-part video without interaction gets no directory section");

            var interaction = PlayDirectoryBuilder.FromParts(Parts(Part("cid-1", "节点 A", 60)), 0, "互动视频", true);
            Check(interaction.HasSections && interaction.Caption == "1/1",
                "an interaction video keeps the parts section even with one node");

            var empty = PlayDirectoryBuilder.FromParts(new List<PlayDirectoryPart>(), 0, "空", false);
            Check(!empty.HasSections, "no parts means no parts section");
            Check(empty.CurrentKey == "" && empty.CurrentPositionText == "",
                "an empty part list invents neither a current row nor a position");

            var past = PlayDirectoryBuilder.FromParts(Parts(Part("a", "P1", 1), Part("b", "P2", 1)), 9, "x", false);
            Check(Rows(past, PlayDirectorySectionKind.Parts)[1].IsCurrent,
                "an out-of-range index clamps to the last part instead of dropping the flag");
        }

        private static void CollectionDirectory()
        {
            var season = Season();
            var model = PlayDirectoryBuilder.FromUgcSeason(season, "7003");

            Check(model.Caption == "3/7", "the collection caption names the episode position");
            Check(model.CountText == "7", "the collection count text counts the episodes");
            Check(model.SourceTitle == "课程合集", "the section carries the collection title");
            Check(model.CurrentKey == "7003", "the current episode is identified by its aid");
            Check(model.CurrentPositionText == "合集 3/7", "the collection position text names the episode");

            var rows = Rows(model, PlayDirectorySectionKind.UgcSeason);
            Check(rows.Count == 7, "every episode becomes a row");
            Check(SameKeys(rows, VideoUgcSeasonOrder.AidOrder(season)),
                "the directory rows follow the same order the sequence advances in");
            Check(rows[0].Subtitle != null && rows[0].Subtitle.Contains("第一章")
                && rows[6].Subtitle.Contains("第二章"),
                "each row keeps the section heading it belongs to");
            Check(rows[0].Subtitle.Contains("甲老师"), "each row keeps the uploader");
            Check(rows[0].Cover == "cover-7001", "each row keeps its cover");
            Check(rows[0].PositionText == "合集 1/7" && rows[3].PositionText == "合集 4/7",
                "each row states its own video position");
            Check(NoInventedPartCount(rows),
                "the collection rows never invent a part count for another video");
            Check(rows[2].IsCurrent && !rows[1].IsCurrent, "only the playing episode is flagged as current");
            Check(rows[0].Duration == "10:00", "the episode duration comes from the page duration");
            Check(rows[2].CurrentBadge == "正在播放" && rows[4].CurrentBadge == "",
                "the collection marks exactly the playing episode as 正在播放");
        }

        private static void CollectionWithoutCurrentRow()
        {
            var model = PlayDirectoryBuilder.FromUgcSeason(Season(), "9999");
            Check(model.Caption == "", "a video outside the collection carries no position on the button");
            Check(model.CurrentKey == "" && model.CurrentPositionText == "",
                "a video outside the collection has no current row to locate");
            Check(Rows(model, PlayDirectorySectionKind.UgcSeason).TrueForAll(x => !x.IsCurrent),
                "no row is claimed as playing when the video is not in the collection");

            var empty = PlayDirectoryBuilder.FromUgcSeason(null, "1");
            Check(!empty.HasSections, "a missing collection produces no section");
        }

        private static void FixedPlaylistDirectory()
        {
            var playlist = Queue(21, 6);
            var model = PlayDirectoryBuilder.FromPlaylist(playlist, null, "3007");

            Check(model.Caption == "第7项", "the queue caption names the current row");
            Check(model.CountText == "7/21",
                "a fixed queue states the row inside the total");
            Check(!model.CountText.Contains("已全部加载"),
                "a fixed queue does not pretend to be a paged list");
            Check(model.CurrentKey == "3007", "the queue row is identified by its aid");
            Check(model.CurrentPositionText == "第7项", "the queue position text names the row");

            var rows = Rows(model, PlayDirectorySectionKind.Playlist);
            Check(rows.Count == 21, "every loaded row becomes a row");
            Check(rows[6].IsCurrent && rows[6].Ordinal == 7, "the seventh row is the current one");
            Check(rows[0].Key == "3001" && rows[20].Key == "3021", "queue keys are the resource aids");
            Check(rows[6].CurrentBadge == "正在播放" && rows[5].CurrentBadge == "",
                "the queue marks exactly the playing row as 正在播放");

            var empty = PlayDirectoryBuilder.FromPlaylist(new VideoPlaylist() { Playlist = new List<VideoPlaylistItem>() }, null, "3007");
            Check(!empty.HasSections, "an empty queue produces no section");
            Check(empty.Caption == "", "an empty queue names no row");
        }

        private static void RemotePlaylistCounts()
        {
            var unknown = RemoteQueue(20, 6, 0, true, "课程播放列表");
            var unknownModel = PlayDirectoryBuilder.FromPlaylist(unknown.Item1, unknown.Item2, "4007");
            Check(unknownModel.SourceTitle == "课程播放列表", "the remote queue uses the source title");
            Check(unknownModel.CountText == "20", "a page without a total states only what is loaded");
            Check(!unknownModel.CountText.Contains("总数未知"),
                "the count never explains the missing total");

            var known = RemoteQueue(20, 6, 120, true, "课程播放列表");
            var knownModel = PlayDirectoryBuilder.FromPlaylist(known.Item1, known.Item2, "4007");
            Check(knownModel.CountText == "20/120", "a server total is written as loaded/total");
            Check(!knownModel.CountText.Contains("共") && !knownModel.CountText.Contains("项"),
                "the count carries no words around the numbers");
        }

        private static void CountTextNeverFakesCompletion()
        {
            Check(PlayDirectoryBuilder.CountText(20, false, 0) == "20",
                "an unknown total is never invented");
            Check(PlayDirectoryBuilder.CountText(20, true, 120) == "20/120",
                "a known total is written next to the loaded count");
            Check(PlayDirectoryBuilder.CountText(120, true, 120) == "120/120",
                "the last page keeps the server total instead of announcing an ending");
            Check(PlayDirectoryBuilder.CountText(-5, false, 0) == "0",
                "a negative loaded count is clamped");
            Check(PlayDirectoryBuilder.FixedCountText(7, 21) == "7/21",
                "a fixed queue states the row inside its total");
            Check(PlayDirectoryBuilder.FixedCountText(0, 21) == "21",
                "a fixed queue without a current row states the total only");
        }

        private static void CombineKeepsSourceAndOrder()
        {
            var parts = PlayDirectoryBuilder.FromParts(
                Parts(Part("cid-1", "P1 开场", 90), Part("cid-2", "P2 正片", 95), Part("cid-3", "P3 结尾", 60)),
                1, "测试视频", false);
            var collection = PlayDirectoryBuilder.FromUgcSeason(Season(), "7003");
            var playlist = PlayDirectoryBuilder.FromPlaylist(Queue(21, 6), null, "3007");

            var inCollection = PlayDirectoryBuilder.Combine(parts, collection, playlist,
                WatchSequenceSourceKind.UgcSeason);

            Check(inCollection.Sections.Count == 3, "all three sections are present when all three exist");
            Check(inCollection.Sections[0].Kind == PlayDirectorySectionKind.Parts
                && inCollection.Sections[1].Kind == PlayDirectorySectionKind.UgcSeason
                && inCollection.Sections[2].Kind == PlayDirectorySectionKind.Playlist,
                "sections keep the order parts, collection, playlist");
            Check(inCollection.ActiveKind == PlayDirectorySectionKind.UgcSeason,
                "the drawer opens on the active continuation source");
            Check(inCollection.Caption == "3/7", "the button caption follows the active source");
            Check(inCollection.SourceTitle == "课程合集", "the header names the active source");
            Check(inCollection.HeaderText == "合集《课程合集》",
                "the header line states the source and nothing else");
            Check(!inCollection.HeaderText.Contains("连播来源")
                && !inCollection.HeaderText.Contains("接下来")
                && !inCollection.HeaderText.Contains("·"),
                "the header carries no prefix and no next-target clause");
            Check(inCollection.CurrentKey == "7003", "the combined model locates the playing row in the active source");
            Check(inCollection.CurrentPositionText == "合集 3/7",
                "the position block states the video position only");
            var currentRow = Rows(inCollection, PlayDirectorySectionKind.UgcSeason).Find(x => x.IsCurrent);
            Check(currentRow != null && currentRow.PositionText == "合集 3/7",
                "the current row keeps the same position text");
            Check(Rows(inCollection, PlayDirectorySectionKind.UgcSeason).FindAll(x => x.IsCurrent).Count == 1,
                "only the playing row is marked current");

            var inPlaylist = PlayDirectoryBuilder.Combine(parts, collection, playlist,
                WatchSequenceSourceKind.Playlist);
            Check(inPlaylist.ActiveKind == PlayDirectorySectionKind.Playlist,
                "an opened queue keeps the drawer on the queue");
            Check(inPlaylist.Caption == "第7项", "the caption follows the queue");
            Check(inPlaylist.CurrentPositionText == "第7项", "the queue position names the row only");
            Check(inPlaylist.HeaderText == "播放列表《播放列表》",
                "the queue header names the queue and its title");

            var partsOnly = PlayDirectoryBuilder.Combine(parts, null, null, WatchSequenceSourceKind.None);
            Check(partsOnly.ActiveKind == PlayDirectorySectionKind.Parts && partsOnly.Sections.Count == 1,
                "without an outer source the drawer opens on the parts");
            Check(partsOnly.HeaderText == "分P",
                "the parts header names the section only, with no collection title");
        }

        private static void CombineFallsBackWithoutTheActiveSection()
        {
            var parts = PlayDirectoryBuilder.FromParts(
                Parts(Part("cid-1", "P1 开场", 90), Part("cid-2", "P2 正片", 95)), 1, "测试视频", false);
            var collection = PlayDirectoryBuilder.FromUgcSeason(Season(), "7003");

            var noQueue = PlayDirectoryBuilder.Combine(parts, collection, null, WatchSequenceSourceKind.Playlist);
            Check(noQueue.ActiveKind == PlayDirectorySectionKind.UgcSeason,
                "an active source without a section falls back to one that exists");
            Check(noQueue.Sections.Count == 2, "a missing queue contributes no section");
            Check(noQueue.HeaderText == "合集《课程合集》",
                "the header names the source alone when no next target exists");

            var onePart = PlayDirectoryBuilder.FromParts(Parts(Part("cid-1", "唯一一P", 90)), 0, "测试视频", false);
            var singlePartInCollection = PlayDirectoryBuilder.Combine(onePart, collection, null, WatchSequenceSourceKind.UgcSeason);
            Check(singlePartInCollection.HasSections && singlePartInCollection.Sections.Count == 1,
                "a one-part video shows only the collection section");
            Check(singlePartInCollection.CurrentPositionText == "合集 3/7",
                "a one-part video states only the video position");

            var nothing = PlayDirectoryBuilder.Combine(null, null, null, WatchSequenceSourceKind.None);
            Check(!nothing.HasSections, "nothing to show means no sections");
        }

        private static void IdentitySurvivesDuplicateTitles()
        {
            var parts = PlayDirectoryBuilder.FromParts(
                Parts(Part("cid-a", "同名", 1), Part("cid-b", "同名", 1), Part("cid-c", "同名", 1)), 0, "x", false);
            var partRows = Rows(parts, PlayDirectorySectionKind.Parts);
            Check(partRows[0].Key != partRows[1].Key && partRows[1].Key != partRows[2].Key,
                "duplicate part titles keep distinct keys");

            var season = Season();
            foreach (var section in season.Sections)
            {
                foreach (var episode in section.Episodes)
                {
                    episode.Title = "同名";
                }
            }
            var collection = PlayDirectoryBuilder.FromUgcSeason(season, "7005");
            var collectionRows = Rows(collection, PlayDirectorySectionKind.UgcSeason);
            Check(Distinct(collectionRows), "duplicate episode titles keep distinct keys");
            Check(collection.CurrentKey == "7005", "the current row is found by aid, not by title");

            var queue = Queue(5, 2);
            foreach (var item in queue.Playlist)
            {
                item.Title = "同名";
            }
            var playlist = PlayDirectoryBuilder.FromPlaylist(queue, null, "3003");
            var queueRows = Rows(playlist, PlayDirectorySectionKind.Playlist);
            Check(Distinct(queueRows), "duplicate queue titles keep distinct keys");
            Check(playlist.CurrentKey == "3003", "the current queue row is found by aid, not by title");
            Check(queueRows[2].Ordinal == 3 && queueRows[2].Key == "3003",
                "the display ordinal is derived from the position while the key stays the identity");
        }

        /// <summary>
        /// BR-28:队列游标停在旧位置,当前行只认正在播的 aid。
        /// </summary>
        private static void PlaylistCurrentFollowsPlayingAid()
        {
            // 游标还在第 7 项,正在播的却是第 11 项
            var queue = Queue(21, 6);
            var model = PlayDirectoryBuilder.FromPlaylist(queue, null, "3011");
            var rows = Rows(model, PlayDirectorySectionKind.Playlist);

            Check(model.CurrentKey == "3011", "BR-28: the queue locates the aid that is playing, not the cursor");
            Check(rows[10].IsCurrent && rows[10].CurrentBadge == "正在播放",
                "BR-28: the row of the playing aid is the only current row");
            Check(rows.FindAll(x => x.IsCurrent).Count == 1,
                "BR-28: a stale cursor leaves exactly one current row");
            Check(!rows[6].IsCurrent && rows[6].CurrentBadge == "",
                "BR-28: the row under the stale cursor is not claimed as playing");
            Check(model.Caption == "第11项" && model.CurrentPositionText == "第11项",
                "BR-28: the caption and position name the row that plays");
            Check(model.CountText == "11/21",
                "BR-28: the count text counts from the playing row");
            Check(queue.Index == 6, "BR-28: building the model never moves the queue cursor");

            // 正在播的视频根本不在这个队列里:这一段没有当前行,也不许编一个位置
            var absent = PlayDirectoryBuilder.FromPlaylist(Queue(21, 6), null, "9999");
            var absentRows = Rows(absent, PlayDirectorySectionKind.Playlist);
            Check(absent.CurrentKey == "" && absent.CurrentPositionText == "",
                "BR-28: a video outside the queue has no current row and no position");
            Check(absentRows.TrueForAll(x => !x.IsCurrent && x.CurrentBadge == ""),
                "BR-28: no queue row is claimed as playing when the video is elsewhere");
            Check(absent.Caption == "" && absent.CountText == "21",
                "BR-28: the queue caption states no position for a video outside it");

            var noAid = PlayDirectoryBuilder.FromPlaylist(Queue(3, 1), null, null);
            Check(noAid.CurrentKey == "" && Rows(noAid, PlayDirectorySectionKind.Playlist).TrueForAll(x => !x.IsCurrent),
                "BR-28: an unknown playing aid marks no row");

            var parts = PlayDirectoryBuilder.FromParts(
                Parts(Part("cid-1", "P1 开场", 90), Part("cid-2", "P2 正片", 95)), 1, "测试视频", false);
            var outside = PlayDirectoryBuilder.Combine(parts,
                PlayDirectoryBuilder.FromUgcSeason(Season(), "7003"),
                PlayDirectoryBuilder.FromPlaylist(Queue(21, 6), null, "9999"),
                WatchSequenceSourceKind.UgcSeason);
            Check(outside.CurrentKey == "7003" && outside.Caption == "3/7",
                "BR-28: the combined model locates the video in the section it really is in");
            Check(!outside.Caption.Contains("第") && outside.CurrentPositionText == "合集 3/7",
                "BR-28: the active collection states its own position and no queue position");
            Check(Rows(outside, PlayDirectorySectionKind.Playlist).TrueForAll(x => !x.IsCurrent),
                "BR-28: the queue section keeps no current row in the combined model");
        }

        /// <summary>
        /// BR-30:定位先看正在浏览的分段,这一段没有当前行才退到连播来源那一段。
        /// </summary>
        private static void LocateFollowsBrowsedSection()
        {
            var parts = PlayDirectoryBuilder.FromParts(
                Parts(Part("cid-1", "P1 开场", 90), Part("cid-2", "P2 正片", 95)), 1, "测试视频", false);
            var collection = PlayDirectoryBuilder.FromUgcSeason(Season(), "7003");
            var playlist = PlayDirectoryBuilder.FromPlaylist(Queue(21, 6), null, "3007");
            var model = PlayDirectoryBuilder.Combine(parts, collection, playlist, WatchSequenceSourceKind.UgcSeason);

            Check(PlayDirectoryBuilder.CurrentRowOf(model, PlayDirectorySectionKind.UgcSeason).Key == "7003"
                && PlayDirectoryBuilder.CurrentRowOf(model, PlayDirectorySectionKind.Parts).Key == "cid-2"
                && PlayDirectoryBuilder.CurrentRowOf(model, PlayDirectorySectionKind.Playlist).Key == "3007",
                "BR-30: every section answers with its own current row");

            Check(PlayDirectoryBuilder.LocateSection(model, PlayDirectorySectionKind.UgcSeason) == PlayDirectorySectionKind.UgcSeason,
                "BR-30: locate stays in the browsed collection instead of jumping to the parts");
            Check(PlayDirectoryBuilder.LocateSection(model, PlayDirectorySectionKind.Parts) == PlayDirectorySectionKind.Parts,
                "BR-30: locate in the browsed parts stays in the parts");
            Check(PlayDirectoryBuilder.LocateSection(model, PlayDirectorySectionKind.Playlist) == PlayDirectorySectionKind.Playlist,
                "BR-30: locate in the browsed queue stays in the queue");

            // 浏览的段里没有当前行:才退到连播来源那一段
            var playlistAbsent = PlayDirectoryBuilder.FromPlaylist(Queue(21, 6), null, "9999");
            var fallback = PlayDirectoryBuilder.Combine(parts, collection, playlistAbsent,
                WatchSequenceSourceKind.UgcSeason);
            Check(PlayDirectoryBuilder.LocateSection(fallback, PlayDirectorySectionKind.Playlist) == PlayDirectorySectionKind.UgcSeason,
                "BR-30: a browsed section without a current row falls back to the active source");
            Check(PlayDirectoryBuilder.CurrentRowOf(fallback,
                    PlayDirectoryBuilder.LocateSection(fallback, PlayDirectorySectionKind.Playlist)).Key == "7003",
                "BR-30: the fallback locates the active source's scoped key");

            var nowhere = PlayDirectoryBuilder.Combine(parts,
                PlayDirectoryBuilder.FromUgcSeason(Season(), "9999"), playlistAbsent,
                WatchSequenceSourceKind.UgcSeason);
            Check(PlayDirectoryBuilder.LocateSection(nowhere, PlayDirectorySectionKind.Playlist) == PlayDirectorySectionKind.Playlist
                && PlayDirectoryBuilder.CurrentRowOf(nowhere, PlayDirectorySectionKind.Playlist) == null,
                "BR-30: with no current row anywhere locate changes nothing");
        }

        /// <summary>
        /// BR-30:重画留在用户正在浏览的分段,只有新开目录或那一段没了才跟随连播来源。
        /// </summary>
        private static void BrowsedSectionSurvivesRedraw()
        {
            var parts = PlayDirectoryBuilder.FromParts(
                Parts(Part("cid-1", "P1 开场", 90), Part("cid-2", "P2 正片", 95)), 1, "测试视频", false);
            var collection = PlayDirectoryBuilder.FromUgcSeason(Season(), "7003");
            var playlist = PlayDirectoryBuilder.FromPlaylist(Queue(21, 6), null, "3007");
            var model = PlayDirectoryBuilder.Combine(parts, collection, playlist, WatchSequenceSourceKind.UgcSeason);

            Check(PlayDirectoryBuilder.ResolveBrowsedSection(model, null) == PlayDirectorySectionKind.UgcSeason,
                "BR-30: a fresh open starts on the active source");
            Check(PlayDirectoryBuilder.ResolveBrowsedSection(model, PlayDirectorySectionKind.Playlist) == PlayDirectorySectionKind.Playlist,
                "BR-30: a redraw keeps the browsed queue instead of pulling it to the active source");

            var redrawn = PlayDirectoryBuilder.Combine(parts, collection, playlist, WatchSequenceSourceKind.UgcSeason);
            Check(redrawn.ActiveKind == PlayDirectorySectionKind.UgcSeason
                && PlayDirectoryBuilder.ResolveBrowsedSection(redrawn, PlayDirectorySectionKind.Parts) == PlayDirectorySectionKind.Parts,
                "BR-30: a redraw that still contains the browsed parts keeps them");

            var withoutQueue = PlayDirectoryBuilder.Combine(parts, collection, null, WatchSequenceSourceKind.UgcSeason);
            Check(PlayDirectoryBuilder.ResolveBrowsedSection(withoutQueue, PlayDirectorySectionKind.Playlist)
                    == PlayDirectorySectionKind.UgcSeason,
                "BR-30: a browsed section that disappeared falls back to the active source");

            var partsOnly = PlayDirectoryBuilder.Combine(parts, null, null, WatchSequenceSourceKind.None);
            Check(PlayDirectoryBuilder.ResolveBrowsedSection(partsOnly, null) == PlayDirectorySectionKind.Parts,
                "BR-30: a parts-only directory opens on the parts");
            Check(PlayDirectoryBuilder.ResolveBrowsedSection(PlayDirectoryBuilder.Combine(null, collection, null,
                    WatchSequenceSourceKind.UgcSeason), PlayDirectorySectionKind.Playlist) == PlayDirectorySectionKind.UgcSeason,
                "BR-30: a missing browsed section with no parts still resolves to a section that exists");
        }

        private static void PlayerSourceWiring()
        {
            var player = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml.cs");
            var xaml = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml");

            var button = xaml.IndexOf("x:Name=\"BottomBtnDirectory\"", StringComparison.Ordinal);
            var episodeButton = xaml.IndexOf("x:Name=\"BottomBtnList\"", StringComparison.Ordinal);
            Check(button > 0 && episodeButton > button,
                "source: the 目录 button sits in the right transport cluster before 选集");
            Check(xaml.Contains("AutomationProperties.Name=\"播放目录\"")
                && xaml.Contains("ToolTipService.ToolTip=\"播放目录\""),
                "source: the directory button carries one name for screen readers and the tooltip");
            Check(Tag(xaml, "x:Name=\"BottomBtnDirectory\"").Contains("Visibility=\"Collapsed\""),
                "source: the 目录 button stays hidden until the page reports a directory");
            Check(xaml.Contains("<fa:FontAwesome Icon=\"Solid_ListUl\" FontSize=\"18\""),
                "source: the directory button shows a list icon at the transport icon size");
            Check(xaml.Contains("x:Name=\"BottomBtnDirectoryCaption\"") && xaml.Contains("FontSize=\"18\" Text=\"\""),
                "source: the caption starts empty so the icon alone is the button");
            Check(player.Contains("BottomBtnDirectoryCaption.Text = caption ?? \"\";"),
                "source: the button caption is exactly what the page reported");

            var playerElement = xaml.IndexOf("<local:Player x:Name=\"Player\"", StringComparison.Ordinal);
            var host = xaml.IndexOf("x:Name=\"DirectoryHost\"", StringComparison.Ordinal);
            Check(host > 0 && host > playerElement,
                "source: the drawer host is a layer inside the player, above the video");
            Check(xaml.Contains("<local:PlayDirectoryControl x:Name=\"DirectoryDrawer\""),
                "source: the drawer renders the directory control");
            Check(CountOf(xaml, "<local:PlayDirectoryControl") == 1,
                "source: the player hosts one directory view instance");

            Check(player.Contains("public event EventHandler DirectoryRequested;"),
                "source: the player raises one directory request");
            Check(player.Contains("public void UpdateDirectoryInfo(bool available, string caption)"),
                "source: the page reports whether a directory exists and what the button says");

            var show = Body(player, "public void ShowDirectory(PlayDirectoryModel model)");
            Check(show.Contains("DirectoryHost.Visibility = Visibility.Visible;"),
                "source: opening the drawer only shows the host");
            Check(!TouchesPlaybackOrWindowState(show),
                "source: opening the drawer does not touch playback, full screen, full window or the settings pane");

            var hide = Body(player, "public void HideDirectory()");
            Check(!TouchesPlaybackOrWindowState(hide),
                "source: closing the drawer does not touch playback, full screen, full window or the settings pane");

            var keydown = Body(player, "private async void PlayerControl_KeyDown");
            Check(keydown.Contains("if (_directoryOpen)") && keydown.Contains("HideDirectory();"),
                "source: escape closes the drawer before the player treats it as exit full screen");

            Check(!player.Contains("RightTapped"),
                "source: the right-tap shortcut hack is gone");
            var menu = Body(player, "private void UpdateNextVideoMenu()");
            Check(menu.Contains("if (!_canSkipToNextVideo)") && menu.Contains("BottomBtnNext.ContextFlyout = null;"),
                "source: the next button has no menu unless the page reported it can skip the remaining parts");
            Check(menu.Contains("new MenuFlyoutItem() { Text = \"下一个视频\" }"),
                "source: the menu carries 下一个视频");
            Check(Body(player, "public void UpdateSequenceInfo").Contains("UpdateNextVideoMenu();"),
                "source: the menu follows the reported sequence state");
            Check(player.Contains("NextVideoRequested?.Invoke(this, EventArgs.Empty);"),
                "source: the menu item still asks the page to skip the remaining parts");
        }

        private static void PageSourceWiring()
        {
            var page = ReadSource("src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs");

            Check(!page.Contains("public class VideoPlaylist") && !page.Contains("public class VideoPlaylistItem"),
                "source: the queue types moved out of the page");
            Check(ReadSource("src/BiliLite.UWP/Models/VideoQueue.cs").Contains("public class VideoPlaylist"),
                "source: the queue types live in the model layer now");

            var request = Body(page, "private void player_DirectoryRequested");
            Check(request.Contains("RightInfo.Width.Value > 0") && request.Contains("SelectDetailDirectory()"),
                "source: a wide window reuses the detail column instead of the overlay");
            Check(request.Contains("player.ShowDirectory(BuildDirectoryModel(!player.IsDirectoryVisible))"),
                "source: the drawer model is built when the drawer opens, and only a fresh open resets the browsed section");

            var change = Body(page, "private void PlayerControl_ChangeEpisodeEvent");
            Check(!change.Contains("BuildDirectoryModel("),
                "source: an automatic change does not rebuild the directory rows");
            Check(change.Contains("RedrawOpenDirectory();"),
                "source: an open drawer still follows the new current part");

            Check(page.Contains("private PlayDirectoryModel BuildDirectoryModel(bool freshOpen)")
                && page.Contains("PlayDirectoryBuilder.Combine(parts, collection, queue, sequence.Position.Source)"),
                "source: the drawer model is composed from parts, collection and queue");
            Check(page.Contains("PlayDirectoryBuilder.FromPlaylist(playlist, mediaListState, info == null ? null : info.aid)")
                && page.Contains("model.IsFreshOpen = freshOpen;"),
                "source: the queue section is built from the playing aid and the model carries the fresh-open intent");

            var activate = Body(page, "private async Task ActivateDirectoryRowAsync");
            Check(!activate.Contains("row.IsCurrent") && activate.Contains("if (IsPlayingDirectoryRow(kind, row))"),
                "source: activation is decided by the playing identity in that section's scope, not by row.IsCurrent");
            Check(activate.Contains("AdoptDirectoryRowSource(kind, rowIndex);"),
                "source: activating the row that already plays only adopts its source");
            Check(activate.Contains("player.ChangePlayIndex(rowIndex, PlayTargetStart.Resume);"),
                "source: a part row goes through the player's own part switch");
            Check(activate.Contains("sequence.SetActiveSource(WatchSequenceSourceKind.UgcSeason);")
                && activate.Contains("await PlayCollectionRowAsync(rowIndex, PlayTargetStart.Resume);"),
                "source: a collection row goes through the P1a sequence path");
            Check(activate.Contains("sequence.SetActiveSource(WatchSequenceSourceKind.Playlist);")
                && activate.Contains("await SelectPlaylistRowAsync(rowIndex, PlayTargetStart.Resume);"),
                "source: a queue row goes through the P1a sequence path");

            var playing = Body(page, "private bool IsPlayingDirectoryRow");
            Check(playing.Contains("string.Equals(row.Key, cid, StringComparison.Ordinal)")
                && playing.Contains("string.Equals(row.Key, aid, StringComparison.Ordinal)"),
                "source: a part row matches the playing cid while collection and queue rows match the playing aid");

            var adopt = Body(page, "private void AdoptDirectoryRowSource");
            Check(adopt.Contains("playlist.Index = rowIndex;")
                && adopt.Contains("sequence.SetActiveSource(WatchSequenceSourceKind.Playlist);"),
                "source: adopting a playing queue row moves the cursor and the continuation source");
            Check(!adopt.Contains("InitializeVideo(") && !adopt.Contains("SelectPlaylistRowAsync(")
                && !adopt.Contains("PlayCollectionRowAsync("),
                "source: adopting a playing row never restarts or reloads the video");

            var parts = Body(page, "private List<PlayDirectoryPart> CurrentParts");
            Check(parts.Contains("DurationSeconds = info.duration,"),
                "source: the page hands the part duration over as the detail reports it");
            Check(!parts.Contains("/ 1000") && !parts.Contains("* 1000"),
                "source: the page never rescales the part duration (the detail reports seconds)");

            var source = Body(page, "private void SwitchSequenceSource(WatchSequenceSourceKind source)");
            Check(source.Contains("IndexOf(info.aid) < 0") && source.Contains("PlaylistIndexOf(info.aid)"),
                "source: the source switch checks that the current video is in the target source");
            Check(!source.Contains("InitializeVideo(") && !source.Contains("ChangePlayIndex("),
                "source: switching the continuation source never restarts the current video");
            Check(source.Contains("sequence.SetActiveSource(source);"),
                "source: only an explicit source action changes the continuation source");

            Check(Body(page, "private void RefreshSequenceState").Contains("player?.UpdateDirectoryInfo("),
                "source: the directory entry follows the shared sequence state");
            Check(page.Contains("private bool SelectDetailDirectory()")
                && page.Contains("pivot.SelectedItem = playlistPivotItem;"),
                "source: the wide-window path selects the existing detail list for the active source");
        }

        private static void ControlSourceWiring()
        {
            var control = ReadSource("src/BiliLite.UWP/Controls/PlayDirectoryControl.xaml.cs");
            var xaml = ReadSource("src/BiliLite.UWP/Controls/PlayDirectoryControl.xaml");

            Check(CountOf(xaml, "<DataTemplate>") == 1,
                "source: the drawer declares exactly one row template");
            Check(control.Contains("RowList.ItemsSource = section.Rows;"),
                "source: every section renders through that one list and template");
            Check(xaml.Contains("x:Name=\"TabHost\"") && control.Contains("BuildTabs(model)"),
                "source: scope tabs are generated from the sections that exist");
            Check(!control.Contains("\" · 连播中\""),
                "source: an active tab is not restated in words");
            Check(control.Contains("AutomationProperties.SetName(dot, \"连播中\")")
                && xaml.Contains("x:Key=\"DirectoryDotStyle\""),
                "source: the continuation tab carries a dot with the 连播中 name");
            Check(xaml.Contains("x:Name=\"PositionCountText\"") && control.Contains("PositionText.Text = position;"),
                "source: the position and the count are two separate blocks");
            Check(control.Contains("ContinuationSourceRequested") && xaml.Contains("按合集连播")
                && xaml.Contains("按播放列表连播"),
                "source: the drawer offers the explicit continuation-source actions");
            Check(control.Contains("public void LocateCurrent()")
                && control.Contains("ScrollIntoView(row, ScrollIntoViewAlignment.Leading)"),
                "source: 定位正在播放 scrolls the current row into view");
            Check(!control.Contains("private PlayDirectoryRow CurrentRow()"),
                "source: the control no longer takes the first IsCurrent row across all sections");
            Check(control.Contains("PlayDirectoryBuilder.LocateSection(m_model, m_activeKind)")
                && control.Contains("PlayDirectoryBuilder.CurrentRowOf(m_model, kind)"),
                "source: locate asks the model for the browsed section's row first, the active source second");
            Check(control.Contains("PlayDirectoryBuilder.ResolveBrowsedSection(model, browsed)")
                && control.Contains("model.IsFreshOpen")
                && control.Contains("SelectScope(PlayDirectoryBuilder.ResolveBrowsedSection"),
                "source: a redraw keeps the browsed section and only a fresh open defaults to the active source");
            Check(xaml.Contains("AutomationProperties.Name=\"定位正在播放\""),
                "source: the locate action has an accessible name");
            Check(control.Contains("VirtualKey.Enter") && control.Contains("VirtualKey.Space"),
                "source: enter and space activate a row while arrows stay with the list");
            Check(!control.Contains("InitializeVideo") && !control.Contains("ChangePlayIndex")
                && !control.Contains("ShowMessageToast"),
                "source: the drawer itself never starts playback");
            Check(!control.Contains("MediaListService") && !control.Contains("GetMediaList")
                && !control.Contains("LoadMoreMediaList") && !control.Contains("MediaListPagingState"),
                "source: the drawer shows paging but never fetches a page itself");
        }

        private static void ProjectEntries()
        {
            var csproj = ReadSource("src/BiliLite.UWP/BiliLite.UWP.csproj");
            Check(csproj.Contains("Models\\VideoQueue.cs"), "source: the queue model is compiled by the UWP project");
            Check(csproj.Contains("Models\\PlayDirectory.cs"), "source: the directory model is compiled by the UWP project");
            Check(csproj.Contains("Controls\\PlayDirectoryControl.xaml")
                && csproj.Contains("Controls\\PlayDirectoryControl.xaml.cs"),
                "source: the directory control is registered as a page and a code-behind");
            Check(ReadSource("src/BiliLite.UWP/Services/MediaListService.cs")
                .Contains("public const int DefaultMediaListType = 1;"),
                "source: the stubbed default media list type still matches the service");
        }

        private static bool TouchesPlaybackOrWindowState(string body)
        {
            return body.Contains("IsFullScreen") || body.Contains("IsFullWindow") || body.Contains("FullWidnow")
                || body.Contains("Pause(") || body.Contains("Play()") || body.Contains("IsPaneOpen")
                || body.Contains("Player.Position");
        }

        private static bool NoInventedPartCount(List<PlayDirectoryRow> rows)
        {
            foreach (var row in rows)
            {
                if (row.PositionText != null && row.PositionText.IndexOf("P", StringComparison.Ordinal) >= 0) return false;
            }
            return true;
        }

        private static bool SameKeys(List<PlayDirectoryRow> rows, List<string> aids)
        {
            if (rows.Count != aids.Count) return false;
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].Key != aids[i]) return false;
            }
            return true;
        }

        private static bool Distinct(List<PlayDirectoryRow> rows)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                if (string.IsNullOrEmpty(row.Key)) return false;
                if (!seen.Add(row.Key)) return false;
            }
            return true;
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

        private static List<PlayDirectoryPart> Parts(params PlayDirectoryPart[] parts)
        {
            return new List<PlayDirectoryPart>(parts);
        }

        private static PlayDirectoryPart Part(string cid, string title, long seconds)
        {
            return new PlayDirectoryPart() { Cid = cid, Title = title, DurationSeconds = seconds };
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

        private static VideoPlaylist Queue(int count, int index)
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
            return new VideoPlaylist() { Index = index, Playlist = items, Title = "播放列表" };
        }

        /// <summary>
        /// 远程队列:已加载 count 行,服务端给了(或没给)总数,还有没有下一页都由参数决定
        /// </summary>
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

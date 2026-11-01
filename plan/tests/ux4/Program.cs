using System;
using System.Collections.Generic;
using System.IO;
using BiliLite.Models.Common;

namespace Ux4Probe
{
    /// <summary>
    /// Ux4: 抽屉页头只留一个标题、来源名只在页头没带来源时另起一行、定位按钮说清它滚到什么、
    /// 当前行的竖条不再贴着序号、省略的标题和副标题都能用悬浮提示看全。
    ///
    /// Executed against product source: Models/PlayDirectory.cs (ShowsSourceTitle and the headers it
    /// judges) together with the queue and paging models it reads. The control cannot run outside UWP,
    /// so its markup and code-behind are read as text; every such check is named "source:".
    /// </summary>
    public static class Program
    {
        private static int _passed;

        private const string ControlXaml = @"src/BiliLite.UWP/Controls/PlayDirectoryControl.xaml";
        private const string ControlCode = @"src/BiliLite.UWP/Controls/PlayDirectoryControl.xaml.cs";
        private const string IconEnum = @"src/BiliLite.UWP/Controls/FontAwesome5/EFontAwesomeIcon.cs";

        public static int Main()
        {
            SourceTitleOnlyForSeasonPages();
            SourceTitleFollowsTheRealHeaders();
            HeaderIsTheOnlyTitle();
            LocateButtonSaysWhatItDoes();
            CurrentRowIsSeparatedFromTheOrdinal();
            LongTextIsReachable();
            ControlKeepsTheSameWiring();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        /// <summary>
        /// 验收场景:剧集页要单独写剧名;合集《X》、播放列表《X》、分P 都已经带着来源,不再写第二遍
        /// </summary>
        private static void SourceTitleOnlyForSeasonPages()
        {
            Check(PlayDirectoryBuilder.ShowsSourceTitle("正片", "某剧"),
                "episodes keep the show name on its own line");
            Check(PlayDirectoryBuilder.ShowsSourceTitle("预告", "某剧"),
                "previews keep the show name on its own line");
            Check(!PlayDirectoryBuilder.ShowsSourceTitle("合集《某课程》", "某课程"),
                "a collection header already carries the name");
            Check(!PlayDirectoryBuilder.ShowsSourceTitle("播放列表《某讲》", "某讲"),
                "a playlist header already carries the name");
            Check(!PlayDirectoryBuilder.ShowsSourceTitle("分P", "某视频"),
                "the parts header never repeats the source name");
            Check(!PlayDirectoryBuilder.ShowsSourceTitle("正片", ""),
                "an empty source name leaves no line");
            Check(!PlayDirectoryBuilder.ShowsSourceTitle("正片", null),
                "a null source name leaves no line");
            Check(!PlayDirectoryBuilder.ShowsSourceTitle(null, "某剧"),
                "no header means no source line");
            Check(!PlayDirectoryBuilder.ShowsSourceTitle("合集", null),
                "a bare collection header has nothing to repeat");
        }

        /// <summary>
        /// 验收场景:判断用的是目录真正生成的页头,不是另外编的一段字
        /// </summary>
        private static void SourceTitleFollowsTheRealHeaders()
        {
            var parts = PlayDirectoryBuilder.FromParts(Parts(), 0, "某视频", false);
            Check(parts.ActiveKind == PlayDirectorySectionKind.Parts, "the parts directory browses the parts section");
            Check(parts.SourceTitle == "某视频", "the parts directory still carries the source name");
            var partsHeader = PlayDirectoryBuilder.HeaderOf(parts.ActiveKind, parts.SourceTitle);
            Check(partsHeader == "分P", "the parts directory header is the section name");
            Check(!PlayDirectoryBuilder.ShowsSourceTitle(partsHeader, parts.SourceTitle),
                "the parts page hides the source name");

            var collectionHeader = PlayDirectoryBuilder.HeaderOf(PlayDirectorySectionKind.UgcSeason, "某课程");
            Check(collectionHeader == "合集《某课程》", "the collection header carries the name");
            Check(!PlayDirectoryBuilder.ShowsSourceTitle(collectionHeader, "某课程"),
                "the collection page hides the name it already wrote");

            var playlistHeader = PlayDirectoryBuilder.HeaderOf(PlayDirectorySectionKind.Playlist, "某讲");
            Check(!PlayDirectoryBuilder.ShowsSourceTitle(playlistHeader, "某讲"),
                "the playlist page hides the name it already wrote");

            var episodes = PlayDirectoryBuilder.FromSeasonSection(PlayDirectorySectionKind.SeasonEpisodes,
                Episodes(), "e1", "某剧");
            Check(episodes.HeaderText == "正片", "the episode page header is the section name");
            Check(PlayDirectoryBuilder.ShowsSourceTitle(episodes.HeaderText, episodes.SourceTitle),
                "the episode page still shows the show name");
        }

        /// <summary>
        /// 验收场景:页头只有一行标题,固定的「目录」不再写,抽屉的自动化名称还在
        /// </summary>
        private static void HeaderIsTheOnlyTitle()
        {
            var xaml = Source(ControlXaml);
            Check(xaml.IndexOf("Text=\"目录\"", StringComparison.Ordinal) < 0,
                "the fixed drawer title is gone from the markup");
            Check(xaml.IndexOf(">目录<", StringComparison.Ordinal) < 0,
                "no bare 目录 label is left in the markup");

            var header = Tag(xaml, "x:Name=\"HeaderText\"");
            Check(header.Length > 0, "the header text block is still there for SetModel");
            Check(header.IndexOf("FontSize=\"16\"", StringComparison.Ordinal) >= 0, "the one title is 16px");
            Check(header.IndexOf("FontWeight=\"SemiBold\"", StringComparison.Ordinal) >= 0,
                "the one title is semi-bold");
            Check(header.IndexOf("MaxLines=\"1\"", StringComparison.Ordinal) >= 0,
                "the one title stays on one line");
            Check(header.IndexOf("TextTrimming=\"CharacterEllipsis\"", StringComparison.Ordinal) >= 0,
                "the one title ends with an ellipsis");
            Check(header.IndexOf("FontSize=\"12\"", StringComparison.Ordinal) < 0,
                "the title is no longer the small second line");

            var root = Tag(xaml, "x:Name=\"Root\"");
            Check(root.IndexOf("AutomationProperties.Name=\"播放目录\"", StringComparison.Ordinal) >= 0,
                "the drawer keeps its automation name on Root");

            var code = Source(ControlCode);
            Check(code.IndexOf("ToolTipService.SetToolTip(HeaderText,", StringComparison.Ordinal) >= 0,
                "source: the title's tooltip is the whole header text");
            Check(code.IndexOf("HeaderText.Text = header", StringComparison.Ordinal) >= 0,
                "source: the title shows the model header");
        }

        /// <summary>
        /// 验收场景:按钮上是"对准"的图标加目标那一行,读屏和悬浮提示说清动作
        /// </summary>
        private static void LocateButtonSaysWhatItDoes()
        {
            var xaml = Source(ControlXaml);
            var icons = Source(IconEnum);
            Check(xaml.IndexOf("xmlns:fa=\"using:FontAwesome5\"", StringComparison.Ordinal) >= 0,
                "the control declares the FontAwesome namespace");
            Check(icons.IndexOf("Solid_Crosshairs,", StringComparison.Ordinal) >= 0,
                "the icon exists in the FontAwesome5 build the repo references");

            var block = Block(xaml, "x:Name=\"LocateButton\"", 900);
            Check(block.IndexOf("Icon=\"Solid_Crosshairs\"", StringComparison.Ordinal) >= 0,
                "the locate button draws the crosshairs icon");
            Check(block.IndexOf("Text=\"正在播放\"", StringComparison.Ordinal) >= 0,
                "the locate button names the target row, not the action");
            Check(block.IndexOf("Text=\"定位", StringComparison.Ordinal) < 0,
                "the label does not say 定位 again");
            Check(block.IndexOf("ToolTipService.ToolTip=\"滚动到正在播放的一行\"", StringComparison.Ordinal) >= 0,
                "the tooltip says what the button does");
            Check(block.IndexOf("AutomationProperties.Name=\"定位正在播放\"", StringComparison.Ordinal) >= 0,
                "screen readers still hear the action");
            Check(block.IndexOf("（", StringComparison.Ordinal) < 0
                && block.IndexOf("：", StringComparison.Ordinal) < 0,
                "the button copy carries no inline explanation");
        }

        /// <summary>
        /// 验收场景:3px 的竖条和 26px 的序号之间空出 8px,序号跟着右移一列
        /// </summary>
        private static void CurrentRowIsSeparatedFromTheOrdinal()
        {
            var xaml = Source(ControlXaml);
            var template = Block(xaml, "<ListView.ItemTemplate>", 2200);
            Check(template.IndexOf("<ColumnDefinition Width=\"3\"/>", StringComparison.Ordinal) >= 0,
                "the current-row bar keeps its 3px column");
            Check(template.IndexOf("<ColumnDefinition Width=\"8\"/>", StringComparison.Ordinal) >= 0,
                "an 8px column separates the bar from the ordinal");
            Check(template.IndexOf("Grid.Column=\"2\" Text=\"{Binding Ordinal}\"", StringComparison.Ordinal) >= 0,
                "the ordinal sits after the gap");
            Check(template.IndexOf("Grid.Column=\"3\"", StringComparison.Ordinal) >= 0,
                "the cover follows the ordinal");
        }

        /// <summary>
        /// 验收场景:标题和副标题省略之后都能用悬浮提示看全,空副标题不出空白提示框
        /// </summary>
        private static void LongTextIsReachable()
        {
            var xaml = Source(ControlXaml);
            var template = Block(xaml, "<ListView.ItemTemplate>", 2200);
            Check(template.IndexOf("ToolTipService.ToolTip=\"{Binding Title,", StringComparison.Ordinal) >= 0,
                "the row title carries its full text as a tooltip");
            Check(template.IndexOf("ToolTipService.ToolTip=\"{Binding Subtitle,", StringComparison.Ordinal) >= 0,
                "the row subtitle carries its full text as a tooltip");
            Check(xaml.IndexOf("PlayDirectoryTooltipConvert", StringComparison.Ordinal) >= 0,
                "an empty string leaves no blank tooltip box");

            var code = Source(ControlCode);
            Check(code.IndexOf("class PlayDirectoryTooltipConvert", StringComparison.Ordinal) >= 0,
                "source: the empty-to-null converter is implemented");
        }

        /// <summary>
        /// 验收场景:页签还是浏览切换、来源按钮还在,页头的规则只多一个可见性判断
        /// </summary>
        private static void ControlKeepsTheSameWiring()
        {
            var xaml = Source(ControlXaml);
            var code = Source(ControlCode);

            Check(xaml.IndexOf("x:Name=\"TabHost\"", StringComparison.Ordinal) >= 0,
                "the section tabs are still the browse switcher");
            Check(xaml.IndexOf("x:Name=\"SourceActionHost\"", StringComparison.Ordinal) >= 0,
                "the source actions are still there");
            Check(xaml.IndexOf("x:Name=\"PlayCollectionButton\"", StringComparison.Ordinal) >= 0
                && xaml.IndexOf("x:Name=\"PlayPlaylistButton\"", StringComparison.Ordinal) >= 0,
                "both continuation sources keep their buttons");

            Check(code.IndexOf("private void SelectScope(", StringComparison.Ordinal) >= 0,
                "source: SelectScope still switches the browsed section");
            Check(code.IndexOf("private void Render(", StringComparison.Ordinal) >= 0,
                "source: Render still redraws the same way");
            Check(code.IndexOf("public void SetModel(", StringComparison.Ordinal) >= 0,
                "source: SetModel is still the single entry point");
            Check(code.IndexOf("SelectScope((PlayDirectorySectionKind)tab.Tag, true)", StringComparison.Ordinal) >= 0,
                "source: a tab click still browses that section");
            Check(code.IndexOf("PlayDirectoryBuilder.ShowsSourceTitle(header, sourceTitle)", StringComparison.Ordinal) >= 0,
                "source: the source line's visibility comes from the model rule");
            Check(code.IndexOf("SourceTitleText.Visibility", StringComparison.Ordinal) >= 0,
                "source: the source line is hidden, not emptied");

            Check(xaml.IndexOf("x:Name=\"HeaderText\"", StringComparison.Ordinal) >= 0
                && xaml.IndexOf("x:Name=\"SourceTitleText\"", StringComparison.Ordinal) >= 0
                && xaml.IndexOf("x:Name=\"LocateButton\"", StringComparison.Ordinal) >= 0,
                "every name the code-behind uses still exists in the markup");
        }

        private static List<PlayDirectoryPart> Parts()
        {
            return new List<PlayDirectoryPart>()
            {
                new PlayDirectoryPart() { Cid = "1", Title = "第1话", DurationSeconds = 600 },
                new PlayDirectoryPart() { Cid = "2", Title = "第2话", DurationSeconds = 600 },
            };
        }

        private static List<PlayDirectorySeasonEpisode> Episodes()
        {
            return new List<PlayDirectorySeasonEpisode>()
            {
                new PlayDirectorySeasonEpisode() { EpId = "e1", Title = "第1集", Cover = "cover-e1" },
                new PlayDirectorySeasonEpisode() { EpId = "e2", Title = "第2集", Cover = "cover-e2" },
            };
        }

        private static string Tag(string source, string marker)
        {
            var start = source.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return "";
            var body = source.Substring(start, Math.Min(400, source.Length - start));
            var end = body.IndexOf('>');
            return end < 0 ? body : body.Substring(0, end);
        }

        private static string Block(string source, string marker, int length)
        {
            var start = source.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return "";
            return source.Substring(start, Math.Min(length, source.Length - start));
        }

        private static string Source(string relativePath)
        {
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

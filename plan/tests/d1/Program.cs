using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BiliLite.Models.Common;

namespace D1Probe
{
    /// <summary>
    /// Executed against product source: Models/Common/Danmaku/DanmakuListSnapshot.cs (the pool
    /// window, the row ordering and the per-row action rules). The control, the player wiring and
    /// the project entries cannot run outside UWP, so those checks are read as text and named
    /// "source:".
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static int Main()
        {
            RecentModeWindow();
            AllModeKeepsTheLoadedPool();
            RowsComeOutSortedByTime();
            LikeOnlyForOnlineRowsWithAServerId();
            CancelLikeOnlyForRowsLikedInSession();
            RowTextFormatting();
            EmptyInputs();
            ControlSourceWiring();
            ControlXamlWiring();
            PlayerControlWiring();
            ExistingDanmakuPathsIntact();
            ProjectEntries();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        private static DanmakuListRow Row(double time, string text = "", string rowId = null, string sendId = null)
        {
            return new DanmakuListRow() { Time = time, Text = text, RowId = rowId, SendId = sendId };
        }

        private static void RecentModeWindow()
        {
            var rows = DanmakuListSnapshot.Build(new[] { Row(100) }, 100, DanmakuListMode.Recent, false);
            Check(rows.Count == 1, "a comment at the playhead is inside the recent window");

            rows = DanmakuListSnapshot.Build(new[] { Row(90) }, 100, DanmakuListMode.Recent, false);
            Check(rows.Count == 0, "a comment exactly ten seconds before the playhead is outside the window");

            rows = DanmakuListSnapshot.Build(new[] { Row(110) }, 100, DanmakuListMode.Recent, false);
            Check(rows.Count == 0, "a comment exactly ten seconds after the playhead is outside the window");

            rows = DanmakuListSnapshot.Build(new[] { Row(90.5), Row(109.5) }, 100, DanmakuListMode.Recent, false);
            Check(rows.Count == 2, "comments just inside either side of the window are kept");

            var mixed = new[] { Row(0), Row(89), Row(100), Row(150) };
            rows = DanmakuListSnapshot.Build(mixed, 100, DanmakuListMode.Recent, false);
            Check(rows.Count == 1 && Math.Abs(rows[0].Time - 100) < 0.001,
                "the recent window drops every loaded comment outside those ten seconds");

            Check(DanmakuListSnapshot.RecentWindowSeconds == 10, "the recent window is ten seconds either side");
            Check(DanmakuListSnapshot.IsNearPlayhead(95, 100) && !DanmakuListSnapshot.IsNearPlayhead(85, 100),
                "the window helper is relative to the playhead, not to the start of the video");
        }

        private static void AllModeKeepsTheLoadedPool()
        {
            var loaded = new[] { Row(0), Row(89), Row(100), Row(150), Row(1000) };
            var rows = DanmakuListSnapshot.Build(loaded, 100, DanmakuListMode.All, false);
            Check(rows.Count == loaded.Length, "the all mode keeps every comment already loaded in the pool");
            Check(rows.Any(row => Math.Abs(row.Time - 0) < 0.001) && rows.Any(row => Math.Abs(row.Time - 1000) < 0.001),
                "the all mode keeps comments far outside the playhead window");

            var recent = DanmakuListSnapshot.Build(loaded, 100, DanmakuListMode.Recent, false);
            Check(recent.Count < rows.Count, "the two modes do not return the same slice");
        }

        private static void RowsComeOutSortedByTime()
        {
            var rows = DanmakuListSnapshot.Build(
                new[] { Row(50, "c"), Row(5, "a"), Row(30, "b") }, 0, DanmakuListMode.All, false);
            Check(rows.Select(row => row.Time).SequenceEqual(new[] { 5d, 30d, 50d }),
                "the all mode sorts loaded comments oldest first");

            rows = DanmakuListSnapshot.Build(
                new[] { Row(105, "c"), Row(95, "a"), Row(100, "b") }, 100, DanmakuListMode.Recent, false);
            Check(rows.Select(row => row.Time).SequenceEqual(new[] { 95d, 100d, 105d }),
                "the recent mode sorts by time too, not by pool group order");

            rows = DanmakuListSnapshot.Build(
                new[] { Row(10, "b"), Row(10, "a") }, 10, DanmakuListMode.Recent, false);
            Check(rows.Select(row => row.Text).SequenceEqual(new[] { "a", "b" }),
                "comments at the same second keep a stable order");
        }

        private static void LikeOnlyForOnlineRowsWithAServerId()
        {
            var rows = DanmakuListSnapshot.Build(new[] { Row(10, "在线弹幕", "123", "hash") }, 10,
                DanmakuListMode.Recent, true);
            Check(rows[0].CanLike, "an online comment with a server id offers a like");
            Check(rows[0].ShowLike && !rows[0].ShowCancelLike, "a fresh likeable row shows like, not cancel");
            Check(rows[0].HasServerId, "the server id is the row id the like API takes");
            Check(rows[0].SendId == "hash", "the author hash is carried through unchanged");

            rows = DanmakuListSnapshot.Build(new[] { Row(10, "刚发的弹幕", null, null) }, 10,
                DanmakuListMode.Recent, true);
            Check(!rows[0].HasServerId, "a locally inserted comment has no server id");
            Check(!rows[0].CanLike && !rows[0].ShowLike && !rows[0].ShowCancelLike,
                "a locally inserted comment is copy only, even while playing online");

            rows = DanmakuListSnapshot.Build(new[] { Row(10, "空id", "", null) }, 10, DanmakuListMode.Recent, true);
            Check(!rows[0].CanLike, "an empty row id is treated as no server id");

            rows = DanmakuListSnapshot.Build(new[] { Row(10, "本地XML", "123", "hash") }, 10,
                DanmakuListMode.Recent, false);
            Check(!rows[0].CanLike && !rows[0].ShowLike,
                "local XML and manifest rows never offer a remote action, even if the file carries an id");

            Check(!DanmakuListSnapshot.Build(new[] { Row(10, "本地") }, 10, DanmakuListMode.Recent, true)[0].LikedInSession,
                "building the list never invents a liked state");
        }

        private static void CancelLikeOnlyForRowsLikedInSession()
        {
            var rows = DanmakuListSnapshot.Build(
                new[] { Row(10, "a", "123"), Row(20, "b", "456") }, 15, DanmakuListMode.All, true);
            DanmakuListSnapshot.ApplySessionLikes(rows, new HashSet<string>(StringComparer.Ordinal) { "123" });

            Check(rows[0].LikedInSession && !rows[1].LikedInSession, "only the row liked here is marked");
            Check(rows[0].ShowCancelLike && !rows[0].ShowLike, "a row liked in this session offers cancel");
            Check(rows[1].ShowLike && !rows[1].ShowCancelLike, "the untouched row still offers like");

            DanmakuListSnapshot.ApplySessionLikes(rows, new HashSet<string>());
            Check(!rows[0].LikedInSession && rows[0].ShowLike, "cancelling puts the row back to like");
            Check(rows.All(row => !row.LikedInSession), "clearing the session set clears every mark");

            rows = DanmakuListSnapshot.Build(new[] { Row(10, "a", "123") }, 10, DanmakuListMode.Recent, true);
            DanmakuListSnapshot.ApplySessionLikes(rows, null);
            Check(!rows[0].LikedInSession, "a null session set marks nothing");

            rows = DanmakuListSnapshot.Build(new[] { Row(10, "本地", null) }, 10, DanmakuListMode.Recent, false);
            DanmakuListSnapshot.ApplySessionLikes(rows, new HashSet<string>(StringComparer.Ordinal) { "" , null });
            Check(!rows[0].LikedInSession && !rows[0].ShowCancelLike,
                "a copy-only row can never become cancellable through the session set");

            rows = DanmakuListSnapshot.Build(new[] { Row(10, "a", "123") }, 10, DanmakuListMode.Recent, false);
            Check(rows.All(row => !row.LikedInSession), "leaving the likeable scope drops any session mark");
        }

        private static void RowTextFormatting()
        {
            Check(Row(0).TimeText == "00:00", "the start of the video reads 00:00");
            Check(Row(5.9).TimeText == "00:05", "the timestamp truncates to whole seconds");
            Check(Row(65).TimeText == "01:05", "a minute long comment reads mm:ss");
            Check(Row(3723).TimeText == "1:02:03", "an hour long video reads h:mm:ss");
            Check(Row(-4).TimeText == "00:00", "a negative time clamps to 00:00");
        }

        private static void EmptyInputs()
        {
            Check(DanmakuListSnapshot.Build(null, 10, DanmakuListMode.All, true).Count == 0,
                "an empty pool produces an empty list");
            Check(DanmakuListSnapshot.Build(new DanmakuListRow[] { null, Row(10) }, 10, DanmakuListMode.All, true).Count == 1,
                "null pool entries are skipped");
            DanmakuListSnapshot.ApplySessionLikes(null, new HashSet<string>());
            Check(true, "applying session likes to no rows does nothing");
        }

        private static void ControlSourceWiring()
        {
            var cs = ReadSource("src/BiliLite.UWP/Controls/DanmakuListControl.xaml.cs");
            var helper = ReadSource("src/BiliLite.UWP/Models/Common/Danmaku/DanmakuListSnapshot.cs");

            Check(cs.Contains("public sealed partial class DanmakuListControl : UserControl"),
                "source: the list is a user control");
            Check(cs.Contains("public PlayerControl Owner { get; set; }"),
                "source: the list reads its pool from the owning player control");
            Check(cs.Contains("DanmakuListFlyout_Opening") && cs.Contains("mode = DanmakuListMode.Recent;"),
                "source: opening the flyout rebuilds the list from the pool");
            Check(cs.Contains("DanmakuListSnapshot.ApplySessionLikes(rows, likedRowIds);"),
                "source: the session like marks are applied to a freshly built list");
            Check(cs.Contains("mode = mode == DanmakuListMode.Recent ? DanmakuListMode.All : DanmakuListMode.Recent;"),
                "source: the toggle switches between the two modes");
            Check(cs.Contains("DanmakuList.ItemsSource = rows;"), "source: every refresh re-assigns the rows");
            Check(cs.Contains("await new DanmakuApi().Like(row.RowId, cid, op).Request();"),
                "source: the like reuses the existing DanmakuApi instead of a new endpoint");
            Check(cs.Contains("await SendLike(RowOf(sender), 1);") && cs.Contains("await SendLike(RowOf(sender), 2);"),
                "source: like sends op=1 and cancel sends op=2");
            Check(cs.Contains("if (row == null || owner == null || !row.CanLike || !row.HasServerId) return;"),
                "source: a row without a server id cannot reach the like API");
            Check(cs.Contains("Utils.SetClipboard(row.Text)"), "source: copy uses the target clipboard helper");
            Check(cs.Contains("Utils.ShowMessageToast("), "source: feedback goes through the target toast helper");
            Check(!cs.Contains("IDanmakuController") && !cs.Contains("AutoMapper"),
                "source: no controller abstraction and no mapper were introduced");
            Check(!cs.Contains("thumbup") && !cs.Contains("dmid="),
                "source: the control does not build the endpoint request itself");
            Check(!cs.Contains("LikedInSession ="), "source: the control never stores a liked state of its own");
            Check(!helper.Contains("Windows.UI") && !helper.Contains("NSDanmaku") && !helper.Contains("using BiliLite"),
                "source: the list rules depend on neither WinRT nor the renderer");
        }

        private static void ControlXamlWiring()
        {
            var xaml = ReadSource("src/BiliLite.UWP/Controls/DanmakuListControl.xaml");

            Check(xaml.Contains("x:Class=\"BiliLite.Controls.DanmakuListControl\""), "source: the control has its own XAML");
            Check(xaml.Contains("<Button.Flyout>") && xaml.Contains("Opening=\"DanmakuListFlyout_Opening\""),
                "source: the list lives in a flyout that refreshes when it opens");
            Check(xaml.Contains("x:DataType=\"common:DanmakuListRow\""), "source: rows bind the small row model");
            Check(xaml.Contains("Text=\"{x:Bind TimeText}\"") && xaml.Contains("Text=\"{x:Bind Text}\""),
                "source: a row shows its time and text");
            Check(xaml.Contains("Click=\"BtnCopyRow_Click\""), "source: every row offers copy");
            Check(xaml.Contains("Click=\"BtnLikeRow_Click\"") && xaml.Contains("Visibility=\"{x:Bind ShowLike}\""),
                "source: the like action is bound to the row's own availability");
            Check(xaml.Contains("Click=\"BtnCancelLikeRow_Click\"") && xaml.Contains("Visibility=\"{x:Bind ShowCancelLike}\""),
                "source: cancel like only appears for rows liked in this session");
            Check(xaml.CountOf("Tag=\"{x:Bind}\"") == 3, "source: all three row actions hand their row to the handler");
            Check(xaml.Contains("Click=\"BtnReplaceShowMode_Click\""), "source: the flyout exposes the mode toggle");
            Check(!xaml.Contains("IDanmakuController") && !xaml.Contains("danmaku:"),
                "source: the list never hosts a second renderer");
        }

        private static void PlayerControlWiring()
        {
            var page = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml");
            var code = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml.cs");

            Check(page.Contains("<local:DanmakuListControl x:Name=\"DanmakuListButton\"/>"),
                "source: the player toolbar hosts the list control");

            var controlAt = page.IndexOf("<local:DanmakuListControl", StringComparison.Ordinal);
            var danmakuToggleAt = page.IndexOf("x:Name=\"TopBtnCloseDanmaku\"", StringComparison.Ordinal);
            var sendBoxAt = page.IndexOf("x:Name=\"BottomBtnSendDanmakuWide\"", StringComparison.Ordinal);
            Check(controlAt > danmakuToggleAt && controlAt < sendBoxAt,
                "source: the list button sits in the toolbar next to the danmaku toggles");

            Check(code.Contains("DanmakuListButton.Owner = this;"), "source: the player hands itself to the list");

            Check(code.Contains("public IReadOnlyList<DanmakuListRow> GetDanmakuListSnapshot(DanmakuListMode mode)"),
                "source: the player exposes one narrow snapshot method");
            Check(code.Contains("RowId = danmaku.rowID,") && code.Contains("SendId = danmaku.sendID,")
                && code.Contains("Text = danmaku.text,") && code.Contains("Time = danmaku.time,"),
                "source: the snapshot maps rowID, sendID, text and time from the existing pool");
            Check(code.Contains("return DanmakuListSnapshot.Build(loaded, Player.Position, mode, CanLikeDanmakuRows());"),
                "source: the snapshot is built from the current playhead and the like scope");
            Check(code.Contains("var pool = danmakuPool;"), "source: the snapshot reads the existing danmaku pool");

            var scopeStart = code.IndexOf("private bool CanLikeDanmakuRows()", StringComparison.Ordinal);
            var scopeBody = scopeStart < 0 ? "" : code.Substring(scopeStart, 600);
            Check(scopeBody.Contains("VideoPlayType.Download") && scopeBody.Contains("VideoPlayType.LoaclFile"),
                "source: downloaded and local playback never offer the remote like");
            Check(scopeBody.Contains("item.LocalPlayInfo != null"), "source: manifest playback never offers the remote like");
            Check(scopeBody.Contains("SettingHelper.Account.Logined"), "source: the like needs the current account");
            Check(scopeBody.Contains("string.IsNullOrEmpty(item.cid)"), "source: the like needs the current cid");
        }

        private static void ExistingDanmakuPathsIntact()
        {
            var code = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml.cs");
            var api = ReadSource("src/BiliLite.UWP/Models/Requests/Api/DanmakuApi.cs");

            Check(code.Contains("DanmuControl.AddDanmu("), "source: sending a comment still renders through the existing control");
            Check(code.Contains("danmakuPool = danmuList.GroupBy(x => x.time_s).ToDictionary(x => x.Key, x => x.ToList());"),
                "source: the existing pool load path is unchanged");
            Check(code.Contains("danmakuParse.ParseBiliBili("), "source: local XML and manifest danmaku still parse as before");
            Check(code.Contains("await LoadDanmaku(1);"), "source: the online segment load path is unchanged");
            Check(api.Contains("/x/v2/dm/thumbup/add") && api.Contains("op=") && api.Contains("Cookie.GetCSRF()"),
                "source: DanmakuApi.Like stays the only place that builds the like request");
        }

        private static void ProjectEntries()
        {
            var csproj = ReadSource("src/BiliLite.UWP/BiliLite.UWP.csproj");
            var flat = csproj.Replace("\r", "").Replace("\n", "").Replace(" ", "").Replace("\t", "");

            Check(csproj.Contains("<Compile Include=\"Models\\Common\\Danmaku\\DanmakuListSnapshot.cs\" />"),
                "source: the list rules are compiled by the UWP project");
            Check(flat.Contains("<CompileInclude=\"Controls\\DanmakuListControl.xaml.cs\"><DependentUpon>DanmakuListControl.xaml</DependentUpon></Compile>"),
                "source: the control code-behind follows the explicit Compile pattern");
            Check(flat.Contains("<PageInclude=\"Controls\\DanmakuListControl.xaml\"><SubType>Designer</SubType><Generator>MSBuild:Compile</Generator></Page>"),
                "source: the control XAML follows the explicit Page pattern");
        }

        private static int CountOf(this string text, string value)
        {
            var count = 0;
            var at = 0;
            while ((at = text.IndexOf(value, at, StringComparison.Ordinal)) >= 0)
            {
                count++;
                at += value.Length;
            }
            return count;
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
                if (File.Exists(Path.Combine(dir.FullName, "src", "BiliLite.UWP", "Models", "Common", "Danmaku", "DanmakuListSnapshot.cs")))
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

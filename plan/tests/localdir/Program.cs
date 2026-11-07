using System;
using System.Collections.Generic;
using System.IO;
using BiliLite.Models.Common;

namespace LocalDirProbe
{
    /// <summary>
    /// localdir: 本地播放页把 PlayInfos 交给播放器的播放目录。
    ///
    /// 目录本身跑真的 PlayDirectoryBuilder(Models/PlayDirectory.cs),所以改构建器会改变这里看到的结论。
    /// 页面在 UWP 外跑不起来,它的接线按文本读,每条都叫 "source:"。
    /// --legacy 拿改之前的页面体跑同一套页面断言,至少一条必须红,证明锚点不是空转。
    /// </summary>
    public static class Program
    {
        private static int _passed;
        private static readonly List<string> _pageFailures = new List<string>();
        private static int _pageChecks;

        public static int Main(string[] args)
        {
            var legacy = Array.IndexOf(args, "--legacy") >= 0;

            ThreeLocalPartsMakeADirectory();
            OneLocalPartStaysCollapsed();
            RowOrdinalMapsToTheSourceIndex();
            MissingCidStillHasItsOwnKey();

            if (legacy)
            {
                RunPageWiring(ReadSource("plan/tests/localdir/LegacyLocalPlayerPage.cs.txt"));
                if (_pageFailures.Count == 0)
                {
                    Console.WriteLine("FAIL localdir LEGACY —— 改之前的页面体居然全过,页面锚点没有咬住");
                    return 1;
                }
                Console.WriteLine("LEGACY localdir red on " + _pageFailures.Count + " / " + _pageChecks + " page anchors:");
                foreach (var failure in _pageFailures) Console.WriteLine("  - " + failure);
                return 0;
            }

            RunPageWiring(ReadSource("src/BiliLite.UWP/Pages/LocalPlayerPage.xaml.cs"));
            if (_pageFailures.Count > 0)
            {
                Console.WriteLine("FAIL localdir —— " + _pageFailures.Count + " / " + _pageChecks + " page anchors:");
                foreach (var failure in _pageFailures) Console.WriteLine("  - " + failure);
                return 1;
            }
            Console.WriteLine("PASS localdir " + (_passed + _pageChecks) + " assertions");
            return 0;
        }

        /// <summary>
        /// 下载/合集的多集:每个 PlayInfo 一条,按钮写 2/3,打开的是真的三段目录。
        /// </summary>
        private static void ThreeLocalPartsMakeADirectory()
        {
            var model = PlayDirectoryBuilder.FromParts(LocalParts(), 1, "本地播放", false);

            Check(model.Caption == "2/3", "三集本地视频的目录标签是 2/3");
            Check(model.CountText == "3", "目录数的是全部三集,不是只剩没播的");
            Check(model.CurrentPositionText == "P2/3", "现在播到 P2/3");
            Check(model.CurrentKey == "cid-b", "当前行认第二集的 cid");
            Check(model.ActiveKind == PlayDirectorySectionKind.Parts, "本地目录的活动分段就是分P");
            Check(model.Sections != null && model.Sections.Count == 1
                    && model.Sections[0].Kind == PlayDirectorySectionKind.Parts
                    && model.Sections[0].Rows.Count == 3,
                "只有分P一段,行数等于本地列表条数");
            Check(model.HasSections, "三集时抽屉有内容可开");

            var rows = model.Sections[0].Rows;
            Check(rows[0].Title == "第1话" && rows[1].Title == "第2话" && rows[2].Title == "第3话",
                "行标题照 PlayInfo.title");
            Check(rows[0].Duration == "01:30" && rows[1].Duration == "01:02:05" && rows[2].Duration == "00:00",
                "毫秒换成秒之后再排版:90 秒是 01:30,3725 秒是 01:02:05");
            Check(rows[1].IsCurrent && rows[1].CurrentBadge.Length > 0
                    && !rows[0].IsCurrent && !rows[2].IsCurrent,
                "只有第二集挂正在播放");
        }

        /// <summary>
        /// 单集:模型本身没有分段,页面据此把按钮整条收起,和以前一样。
        /// </summary>
        private static void OneLocalPartStaysCollapsed()
        {
            var parts = new List<PlayDirectoryPart>()
            {
                new PlayDirectoryPart() { Cid = "cid-only", Title = "只有一集", DurationSeconds = 60 },
            };
            var model = PlayDirectoryBuilder.FromParts(parts, 0, "本地播放", false);

            Check(model.Caption == "1/1", "单集的标签仍是 1/1,但页面不报可用");
            Check(!model.HasSections, "单集且不是互动视频时没有分段,抽屉不弹空页");
        }

        /// <summary>
        /// 行上的 Ordinal 是一起的展示序号;换P用的下标就是 Ordinal - 1。
        /// </summary>
        private static void RowOrdinalMapsToTheSourceIndex()
        {
            var model = PlayDirectoryBuilder.FromParts(LocalParts(), 2, "本地播放", false);
            var rows = model.Sections[0].Rows;

            Check(rows[0].Ordinal == 1 && rows[1].Ordinal == 2 && rows[2].Ordinal == 3,
                "Ordinal 从 1 起,和列表下标差一");
            Check(rows[2].Ordinal - 1 == 2, "点第三行换到下标 2");
            Check(rows[2].IsCurrent && rows[2].Ordinal - 1 == 2, "正在播放的行回得到它自己的下标");
            Check(rows[0].Ordinal - 1 == 0 && rows[1].Ordinal - 1 == 1, "另外两行同样差一");
        }

        /// <summary>
        /// 本地文件没有 cid 时身份退到位置:每行仍有自己的 Key,不会两行撞成一个。
        /// </summary>
        private static void MissingCidStillHasItsOwnKey()
        {
            var parts = new List<PlayDirectoryPart>()
            {
                new PlayDirectoryPart() { Cid = null, Title = "A", DurationSeconds = 1 },
                new PlayDirectoryPart() { Cid = "", Title = "B", DurationSeconds = 1 },
            };
            var rows = PlayDirectoryBuilder.FromParts(parts, 0, "本地播放", false).Sections[0].Rows;

            Check(rows[0].Key == "part-1" && rows[1].Key == "part-2",
                "没有 cid 的行按位置给 Key");
            Check(rows[0].Key != rows[1].Key, "两个没有 cid 的行不会共用身份");
        }

        private static List<PlayDirectoryPart> LocalParts()
        {
            return new List<PlayDirectoryPart>()
            {
                new PlayDirectoryPart() { Cid = "cid-a", Title = "第1话", DurationSeconds = 90 },
                new PlayDirectoryPart() { Cid = "cid-b", Title = "第2话", DurationSeconds = 3725 },
                new PlayDirectoryPart() { Cid = "cid-c", Title = "第3话", DurationSeconds = 0 },
            };
        }

        private static int LocalPartCount(IList<PlayDirectoryPart> parts)
        {
            return parts == null ? 0 : parts.Count;
        }

        // ---------- 页面接线:同一套断言跑真页面和改之前的页面体 ----------

        private static void RunPageWiring(string page)
        {
            var subscribe = Body(page, "private void SubscribePlayerDirectory");
            PageCheck(subscribe.Contains("player.DirectoryRequested -= Player_DirectoryRequested;")
                    && subscribe.Contains("player.DirectoryRequested += Player_DirectoryRequested;"),
                "source: 目录按钮的请求有订阅,重复进入不会叠两次");
            PageCheck(subscribe.Contains("player.DirectoryRowRequested -= Player_DirectoryRowRequested;")
                    && subscribe.Contains("player.DirectoryRowRequested += Player_DirectoryRowRequested;"),
                "source: 目录行激活有订阅,重复进入不会叠两次");
            PageCheck(subscribe.Contains("player.ChangeEpisodeEvent -= Player_ChangeEpisodeEvent;")
                    && subscribe.Contains("player.ChangeEpisodeEvent += Player_ChangeEpisodeEvent;"),
                "source: 换集事件有订阅,重复进入不会叠两次");
            PageCheck(Body(page, "public LocalPlayerPage").Contains("SubscribePlayerDirectory();"),
                "source: 页面一建就接上目录事件");

            var navigatedTo = Body(page, "protected override void OnNavigatedTo");
            PageCheck(navigatedTo.Contains("SubscribePlayerDirectory();"),
                "source: 导航回来也重新接上目录事件");
            PageCheck(navigatedTo.Contains("player.InitializePlayInfo(data.PlayInfos, data.Index, PlayTargetStart.Resume);"),
                "source: 本地列表仍交给播放器初始化");
            PageCheck(navigatedTo.Contains("RefreshDirectoryState();"),
                "source: 初始化之后报一次目录可用性和位置");

            var parts = Body(page, "private List<PlayDirectoryPart> LocalParts");
            PageCheck(parts.Contains("Cid = info.cid,") && parts.Contains("Title = info.title,"),
                "source: 目录行照 PlayInfo 的 cid 和标题");
            PageCheck(parts.Contains("DurationSeconds = info.duration / 1000,"),
                "source: PlayInfo.duration 是毫秒,交给目录前换成秒");

            var build = Body(page, "private PlayDirectoryModel BuildDirectoryModel");
            PageCheck(build.Contains("PlayDirectoryBuilder.FromParts(LocalParts(),")
                    && build.Contains("player.CurrentPlayIndex")
                    && build.Contains("LocalDirectorySourceTitle,"),
                "source: 目录只有一个来源:本地列表 + 播放器当前下标");
            PageCheck(build.Contains("false);") && build.Contains("model.IsFreshOpen = freshOpen;"),
                "source: 本地播放不是互动视频,新开/重画分开报");
            PageCheck(page.Contains("private const string LocalDirectorySourceTitle = \"本地播放\";"),
                "source: 目录来源名读起来是本地播放");

            var state = Body(page, "private void RefreshDirectoryState");
            PageCheck(state.Contains("var available = LocalPartCount() > 1;"),
                "source: 只有一集时目录不可用,按钮整条收起");
            PageCheck(state.Contains("player.UpdateDirectoryInfo(available,")
                    && state.Contains("model == null ? \"\" : model.Caption"),
                "source: 可用性和短标签一起报给播放器");
            PageCheck(state.Contains("var model = available ? BuildDirectoryModel(false) : null;"),
                "source: 按钮上的位置来自同一份目录");
            PageCheck(state.Contains("RefreshOpenDirectory(model);"),
                "source: 报完位置再看开着的抽屉要不要重画");

            var refresh = Body(page, "private void RefreshOpenDirectory");
            PageCheck(refresh.Contains("!player.IsDirectoryVisible"),
                "source: 抽屉没开就不建行");
            PageCheck(refresh.Contains("if (signature == _directorySignature) return;"),
                "source: 位置和条数都没变就不重画抽屉");
            PageCheck(refresh.Contains("player.ShowDirectory(model ?? BuildDirectoryModel(false));"),
                "source: 重画只换内容,不当成新开目录");

            var requested = Body(page, "private void Player_DirectoryRequested");
            PageCheck(requested.Contains("if (_isClosed || player == null) return;"),
                "source: 页签关了就不再开抽屉");
            PageCheck(requested.Contains("player.ShowDirectory(BuildDirectoryModel(!player.IsDirectoryVisible));"),
                "source: 点目录打开抽屉,开着时只刷新内容");

            var rowRequested = Body(page, "private void Player_DirectoryRowRequested");
            PageCheck(rowRequested.Contains("if (e.Kind != PlayDirectorySectionKind.Parts) return;"),
                "source: 本地目录只有分P一段,其他分段不理");
            PageCheck(rowRequested.Contains("var index = e.Row.Ordinal - 1;"),
                "source: 行序号减一才是播放列表下标");
            PageCheck(rowRequested.Contains("if (index == player.CurrentPlayIndex) return;"),
                "source: 点正在播的那一行不重播");
            PageCheck(rowRequested.Contains("player.ChangePlayIndex(index, PlayTargetStart.Resume);"),
                "source: 选行换P沿用这一P保存的进度");
            PageCheck(!rowRequested.Contains("ShowDirectory"),
                "source: 换P由播放器负责,行处理不自己重画抽屉");

            var changed = Body(page, "private void Player_ChangeEpisodeEvent");
            PageCheck(changed.Contains("if (_isClosed) return;") && changed.Contains("RefreshDirectoryState();"),
                "source: 换集后重新报位置并刷新开着的抽屉");

            var unsubscribe = Body(page, "private void UnsubscribePlayerDirectory");
            PageCheck(unsubscribe.Contains("player.DirectoryRequested -= Player_DirectoryRequested;")
                    && unsubscribe.Contains("player.DirectoryRowRequested -= Player_DirectoryRowRequested;")
                    && unsubscribe.Contains("player.ChangeEpisodeEvent -= Player_ChangeEpisodeEvent;"),
                "source: 离开时三个订阅都摘掉");

            PageCheck(Body(page, "protected override void OnNavigatedFrom").Contains("UnsubscribePlayerDirectory();"),
                "source: 导航离开就摘订阅");
            var closed = Body(page, "private void LocalPlayerPage_ClosedPage");
            PageCheck(closed.Contains("_isClosed = true;") && closed.Contains("UnsubscribePlayerDirectory();"),
                "source: 页签关闭先立标记再摘订阅");

            PageCheck(!page.Contains("WatchSequenceSourceKind") && !page.Contains("SetSequenceSource"),
                "source: 本地播放不自造连播来源,目录就是这一份列表");
            PageCheck(!page.Contains("LoadMediaListPage") && !page.Contains("MediaListPagingState"),
                "source: 本地目录不取页,没有远程翻页状态");
        }

        private static void PageCheck(bool condition, string message)
        {
            _pageChecks++;
            if (condition) return;
            _pageFailures.Add(message);
        }

        // ---------- 文件与方法体 ----------

        private static string Body(string source, string signature)
        {
            var start = source.IndexOf(signature, StringComparison.Ordinal);
            if (start < 0) return "";
            var length = Math.Min(8000, source.Length - start);
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

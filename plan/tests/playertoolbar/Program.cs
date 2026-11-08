using System;
using System.Collections.Generic;
using System.IO;
using BiliLite.Services;

namespace PlayerToolbarProbe
{
    /// <summary>
    /// playertoolbar:播放器浮层的去留(弹幕列表 / 节点按钮 + 节点标签 / 本地轨道标签),
    /// 入口指向的内容不存在就不该站着。判定在 PlayerSurfaceVisibility 里,探针只喂信号看结论。
    /// --legacy 用改之前的结论跑同一套断言,用来确认这套断言真的会红。
    /// </summary>
    internal static class Program
    {
        private static int checks;
        private static readonly List<string> failures = new List<string>();
        private static bool legacy;

        private static int Main(string[] args)
        {
            legacy = Array.IndexOf(args, "--legacy") >= 0;

            PlaylistCases();
            LocalTrackCases();
            DanmakuCases();
            PivotCases();
            PivotNeverEmptyCases();
            NullSignalCases();

            Console.WriteLine();
            if (failures.Count == 0)
            {
                Console.WriteLine((legacy ? "playertoolbar LEGACY " : "playertoolbar ") + "PASS —— " + checks + " 项检查全过");
                Console.WriteLine("TOTAL " + checks + " checks, 0 failed");
                return 0;
            }
            Console.WriteLine((legacy ? "playertoolbar LEGACY " : "playertoolbar ") + "FAIL —— " + failures.Count + " / " + checks + " 项没过:");
            foreach (var failure in failures) Console.WriteLine("  - " + failure);
            Console.WriteLine("TOTAL " + checks + " checks, " + failures.Count + " failed");
            return 1;
        }

        // ---------- 判定入口:正常跑真的那份,--legacy 换成改之前的结论 ----------

        private static PlayerSurfaceVerdicts Decide(PlayerSurfaceSignals signals)
        {
            return legacy ? LegacyDecision(signals) : PlayerSurfaceVisibility.Decide(signals);
        }

        private static int Resolve(IReadOnlyList<bool> visible, int selected, int fallback)
        {
            return legacy ? selected : PlayerSurfaceVisibility.ResolvePivotSelection(visible, selected, fallback);
        }

        /// <summary>
        /// 改之前:弹幕列表按钮一直在;节点标签一直在;本地轨道标签只看"是不是合集播放"(这里用有没有可播轨道近似)。
        /// 选集按钮与选集标签已经整条拆掉(方案 A:目录是唯一的列表入口),基线里不再有这两项。
        /// </summary>
        private static PlayerSurfaceVerdicts LegacyDecision(PlayerSurfaceSignals signals)
        {
            var s = signals ?? new PlayerSurfaceSignals();
            return new PlayerSurfaceVerdicts
            {
                ShowDanmakuList = true,
                ShowPlayNodeButton = s.IsInteraction,
                ShowNodePivot = true,
                ShowLocalTracksPivot = s.VideoTrackCount > 0 || s.AudioTrackCount > 0,
            };
        }

        // ---------- 节点:按钮 + 标签(分P 已交给播放目录) ----------

        /// <summary>产品源码:探针从自己的输出目录往上找仓库根。</summary>
        private static string ReadSource(string fileName)
        {
            var dir = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(dir))
            {
                var candidate = Path.Combine(dir, "src", "BiliLite.UWP", "Controls", fileName);
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }
                dir = Path.GetDirectoryName(dir);
            }
            throw new FileNotFoundException(fileName + " not found above " + AppContext.BaseDirectory);
        }

        /// <summary>产品源码里的播放器 XAML:钉住"没有第二个列表入口"这条决定。</summary>
        private static string PlayerControlXaml()
        {
            return ReadSource("PlayerControl.xaml");
        }
        private static void PlaylistCases()
        {
            var none = Decide(new PlayerSurfaceSignals { PlaylistCount = 0 });
            Check("无信号:节点按钮藏", none.ShowPlayNodeButton, false);
            Check("无信号:节点标签藏", none.ShowNodePivot, false);

            var single = Decide(new PlayerSurfaceSignals { PlaylistCount = 1 });
            Check("单P:节点按钮藏", single.ShowPlayNodeButton, false);
            Check("单P:节点标签藏", single.ShowNodePivot, false);

            var many = Decide(new PlayerSurfaceSignals { PlaylistCount = 12 });
            Check("多P:节点按钮藏(分P走播放目录)", many.ShowPlayNodeButton, false);
            Check("多P:节点标签藏(分P走播放目录)", many.ShowNodePivot, false);

            var interaction = Decide(new PlayerSurfaceSignals { PlaylistCount = 1, IsInteraction = true, NodeCount = 1 });
            Check("互动单P:节点按钮在", interaction.ShowPlayNodeButton, true);
            Check("互动单P:节点标签在", interaction.ShowNodePivot, true);

            var interactionMany = Decide(new PlayerSurfaceSignals { PlaylistCount = 8, IsInteraction = true, NodeCount = 6 });
            Check("互动多P:节点按钮在", interactionMany.ShowPlayNodeButton, true);
            Check("互动多P:节点标签在", interactionMany.ShowNodePivot, true);

            // 节点树是异步取的:还没拿到节点时按钮和标签都是空壳
            var interactionEmpty = Decide(new PlayerSurfaceSignals { PlaylistCount = 1, IsInteraction = true, NodeCount = 0 });
            Check("互动但节点还没到:节点按钮藏", interactionEmpty.ShowPlayNodeButton, false);
            Check("互动但节点还没到:节点标签藏", interactionEmpty.ShowNodePivot, false);

            // 不是互动视频:节点数为 0 也好、别的播放残留的节点数也好,都不该冒出节点入口
            var notInteraction = Decide(new PlayerSurfaceSignals { PlaylistCount = 1, IsInteraction = false, NodeCount = 3 });
            Check("非互动但节点数不为零:节点按钮藏", notInteraction.ShowPlayNodeButton, false);
            Check("非互动但节点数不为零:节点标签藏", notInteraction.ShowNodePivot, false);

            // 方案 A:播放器不再有第二个列表入口
            var xaml = PlayerControlXaml();
            Check("播放器底栏没有选集按钮", xaml.Contains("BottomBtnList"), false);
            Check("播放器侧栏没有选集列表", xaml.Contains("x:Name=\"EpisodeList\""), false);
            Check("播放器里不再出现「选集」字样", xaml.Contains("Text=\"选集\""), false);
            Check("互动视频的节点标签仍在", xaml.Contains("x:Name=\"NodePivot\""), true);
            // 页签头不看标签的 Visibility:去留只能靠 Items,所以 XAML 里不许再折起来
            Check("节点标签在 XAML 里不再用 Visibility 折", xaml.Contains("x:Name=\"NodePivot\" Visibility="), false);
            Check("本地轨道标签在 XAML 里不再用 Visibility 折", xaml.Contains("x:Name=\"LocalTracksPivot\" Visibility="), false);
            var code = ReadSource("PlayerControl.xaml.cs");
            Check("构造函数先把节点标签从 Items 里拿走", code.Contains("SetPivotItemPresent(NodePivot, _nodePivotSlot, false);"), true);
            Check("构造函数先把本地轨道标签拿走", code.Contains("SetPivotItemPresent(LocalTracksPivot, _localTracksPivotSlot, false);"), true);
            Check("节点判定落到 Items(插回/拿走)", code.Contains("SetPivotItemPresent(NodePivot, _nodePivotSlot, verdict.ShowNodePivot);"), true);
            Check("本地轨道判定落到 Items(插回/拿走)", code.Contains("SetPivotItemPresent(LocalTracksPivot, _localTracksPivotSlot, verdict.ShowLocalTracksPivot);"), true);
            Check("判定不再写节点标签的 Visibility", code.Contains("NodePivot.Visibility = verdict"), false);
            Check("判定不再写本地轨道标签的 Visibility", code.Contains("LocalTracksPivot.Visibility = verdict"), false);
        }

        // ---------- 本地轨道标签:有轨道才在 ----------

        private static void LocalTrackCases()
        {
            var none = Decide(new PlayerSurfaceSignals { PlaylistCount = 1 });
            Check("没本地轨道:本地轨道标签藏", none.ShowLocalTracksPivot, false);

            var video = Decide(new PlayerSurfaceSignals { PlaylistCount = 1, IsManifestPlayback = true, VideoTrackCount = 1 });
            Check("合集只有视频轨:本地轨道标签在", video.ShowLocalTracksPivot, true);

            var audio = Decide(new PlayerSurfaceSignals { PlaylistCount = 1, IsManifestPlayback = true, AudioTrackCount = 1 });
            Check("合集只有音频轨:本地轨道标签在", audio.ShowLocalTracksPivot, true);

            // 不在合集播放里:视频、音频轨道的下拉根本没填,标签开着也是空的
            var notManifest = Decide(new PlayerSurfaceSignals { PlaylistCount = 1, VideoTrackCount = 2, AudioTrackCount = 1 });
            Check("合集外的视频/音频轨:下拉没填,本地轨道标签藏", notManifest.ShowLocalTracksPivot, false);

            var subtitle = Decide(new PlayerSurfaceSignals { PlaylistCount = 1, SubtitleTrackCount = 2 });
            Check("只有字幕轨:本地轨道标签在", subtitle.ShowLocalTracksPivot, true);

            var danmaku = Decide(new PlayerSurfaceSignals { PlaylistCount = 1, DanmakuTrackCount = 3 });
            Check("只有弹幕轨:本地轨道标签在", danmaku.ShowLocalTracksPivot, true);

            var all = Decide(new PlayerSurfaceSignals
            {
                PlaylistCount = 4,
                IsManifestPlayback = true,
                VideoTrackCount = 2,
                AudioTrackCount = 1,
                SubtitleTrackCount = 2,
                DanmakuTrackCount = 1,
            });
            Check("每种轨道各一条:本地轨道标签在", all.ShowLocalTracksPivot, true);
        }

        // ---------- 弹幕列表:有没有东西可列 ----------

        private static void DanmakuCases()
        {
            var unknown = Decide(new PlayerSurfaceSignals { PlaylistCount = 1 });
            Check("弹幕还没结果:列表按钮藏", unknown.ShowDanmakuList, false);

            var empty = Decide(new PlayerSurfaceSignals { PlaylistCount = 1, DanmakuLoadSettled = true });
            Check("弹幕加载完是空的:列表按钮藏", empty.ShowDanmakuList, false);

            var loaded = Decide(new PlayerSurfaceSignals { PlaylistCount = 1, DanmakuRowCount = 5, DanmakuLoadSettled = true });
            Check("有弹幕:列表按钮在", loaded.ShowDanmakuList, true);

            var loadedEarly = Decide(new PlayerSurfaceSignals { PlaylistCount = 1, DanmakuRowCount = 5 });
            Check("有弹幕但还没标记完成:列表按钮在", loadedEarly.ShowDanmakuList, true);

            var localPending = Decide(new PlayerSurfaceSignals { PlaylistCount = 1, HasLocalDanmakuFile = true });
            Check("本地弹幕文件还没解析完:列表按钮在", localPending.ShowDanmakuList, true);

            var localEmpty = Decide(new PlayerSurfaceSignals { PlaylistCount = 1, HasLocalDanmakuFile = true, DanmakuLoadSettled = true });
            Check("本地弹幕文件解析完是空的:列表按钮藏", localEmpty.ShowDanmakuList, false);

            var imported = Decide(new PlayerSurfaceSignals { PlaylistCount = 1, DanmakuRowCount = 1, HasLocalDanmakuFile = true, DanmakuLoadSettled = true });
            Check("导入弹幕之后:列表按钮在", imported.ShowDanmakuList, true);
        }

        // ---------- 标签兜底:选中项被藏起来要换地方 ----------

        private static void PivotCases()
        {
            // 顺序:选集(0) 本地轨道(1) 弹幕(2) 播放(3) 字幕(4) 快捷键(5)
            var allVisible = new List<bool> { true, true, true, true, true, true };
            Check("选中的标签看得见:不动", Resolve(allVisible, 0, 2), 0);
            Check("选中的标签看得见:本地轨道也不动", Resolve(allVisible, 1, 2), 1);

            var episodeHidden = new List<bool> { false, false, true, true, true, true };
            Check("选集标签刚藏起来:退回弹幕", Resolve(episodeHidden, 0, 2), 2);

            var localHidden = new List<bool> { true, false, true, true, true, true };
            Check("本地轨道标签刚藏起来:退回弹幕", Resolve(localHidden, 1, 2), 2);

            var fallbackHidden = new List<bool> { false, false, false, true, true, true };
            Check("弹幕标签也不可见:退到第一个看得见的", Resolve(fallbackHidden, 0, 2), 3);

            var noneAccurate = new List<bool> { false, false, false, false, false, false };
            Check("一个都看不见:交给调用方(NoVisiblePivot)", Resolve(noneAccurate, 0, 2), PlayerSurfaceVisibility.NoVisiblePivot);

            Check("空标签表:NoVisiblePivot", Resolve(new List<bool>(), 0, 2), PlayerSurfaceVisibility.NoVisiblePivot);

            Check("选中下标越界:退回兜底", Resolve(allVisible, 9, 2), 2);
            Check("选中下标 -1:退回兜底", Resolve(allVisible, -1, 2), 2);
            Check("兜底下标越界:退到第一个看得见的", Resolve(episodeHidden, 0, 9), 2);
        }

        /// <summary>
        /// 设置面板永远不能变成空壳:两个被判定的标签怎么组合,都还剩至少一个看得见的,
        /// 而且换过去之后选中的一定是看得见的那个。
        /// </summary>
        private static void PivotNeverEmptyCases()
        {
            for (var mask = 0; mask < 4; mask++)
            {
                var episode = (mask & 1) != 0;
                var local = (mask & 2) != 0;
                for (var selected = 0; selected < 6; selected++)
                {
                    var visible = new List<bool> { episode, local, true, true, true, true };
                    var target = Resolve(visible, selected, 2);
                    var label = "标签组合 选集=" + episode + " 本地轨道=" + local + " 选中=" + selected;
                    Check("不空壳:" + label + " 有可见标签", target >= 0 && target < visible.Count, true);
                    Check("不空壳:" + label + " 选中可见", target >= 0 && target < visible.Count && visible[target], true);
                }
            }
        }

        private static void NullSignalCases()
        {
            var verdict = Decide(null);
            Check("没有信号:全部藏(节点按钮)", verdict.ShowPlayNodeButton, false);
            Check("没有信号:全部藏(节点按钮)", verdict.ShowPlayNodeButton, false);
            Check("没有信号:全部藏(节点标签)", verdict.ShowNodePivot, false);
            Check("没有信号:全部藏(本地轨道标签)", verdict.ShowLocalTracksPivot, false);
            Check("没有信号:全部藏(弹幕列表)", verdict.ShowDanmakuList, false);
        }

        private static void Check(string label, object actual, object expected)
        {
            checks++;
            var actualText = actual is bool actualBool ? (actualBool ? "true" : "false") : Convert.ToString(actual);
            var expectedText = expected is bool expectedBool ? (expectedBool ? "true" : "false") : Convert.ToString(expected);
            if (string.Equals(actualText, expectedText, StringComparison.Ordinal)) return;
            failures.Add(label + "(期望 " + expectedText + ",实际 " + actualText + ")");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BiliLite.Helpers;
using BiliLite.Modules;

namespace SETTINGS1
{
    /// <summary>
    /// SETTINGS-01（设置整理第一步）：
    /// 1) 云端导入不得丢掉只在本地存在的关键词/正则/用户规则；
    /// 2) 手动正则先校验再入库，输入错误留在输入框旁；
    /// 3) 添加按钮只写本地，不再回读云端规则；
    /// 4) 直播关键词只清空直播输入框；
    /// 5) 鼠标返回开关同时管住后退侧键和中键。
    /// 合并与鼠标判定跑真实 seam 实现；SettingPage.xaml.cs 无法脱离 XAML 编译，
    /// 因此接线部分用源码顺序检查钉住。
    /// SETTINGS1_MODE=legacy 时把被测实现换成修复前形状的转录，SETTINGS1_SRC_ROOT 可指向旧源码快照。
    /// </summary>
    internal static class Program
    {
        private static int assertions;
        private static readonly List<string> failures = new List<string>();
        private static bool legacy;
        private static string root;

        private static int Main()
        {
            legacy = string.Equals(Environment.GetEnvironmentVariable("SETTINGS1_MODE"), "legacy",
                StringComparison.OrdinalIgnoreCase);
            root = SourceRoot();
            Console.WriteLine("SETTINGS-01 设置保存与失效行为" +
                (legacy ? "（LEGACY：被测实现＝修复前形状转录）" : ""));

            MergeKeepsLocalOnly();
            SnapshotSurvivesClearing();
            RegexValidity();
            AddUniqueReports();
            MouseBackTable();
            SourceChecks();

            foreach (var failure in failures) Console.WriteLine("FAIL " + failure);
            if (failures.Count == 0)
            {
                Console.WriteLine("PASS " + assertions + " assertions");
                return 0;
            }
            Console.WriteLine("FAIL " + failures.Count + "/" + assertions + " assertions");
            return 1;
        }

        private static void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) failures.Add(message);
        }

        // ── 被测实现（legacy 时换成修复前的形状） ─────────────────────────────
        private static List<string> Merge(IEnumerable<string> existing, IEnumerable<string> incoming)
        {
            return legacy
                ? LegacyMerge(existing, incoming, null)
                : DanmuFilterRules.Merge(existing, incoming);
        }

        private static List<string> MergeRegex(IEnumerable<string> existing, IEnumerable<string> incoming)
        {
            Func<string, bool> accept = pattern => DanmuFilterRules.TryValidateRegex(pattern, out _);
            return legacy
                ? LegacyMerge(existing, incoming, accept)
                : DanmuFilterRules.Merge(existing, incoming, accept);
        }

        /// <summary>
        /// 修复前：Union 是延迟枚举，清空本地集合后才开始枚举，本地一侧已经空了。
        /// 正则分支当时先 ToList()，所以碰巧保住本地规则 —— 转录保留这个差别。
        /// </summary>
        private static List<string> LegacyMerge(IEnumerable<string> existing, IEnumerable<string> incoming,
            Func<string, bool> acceptIncoming)
        {
            var local = new ObservableCollection<string>(existing);
            var accepted = acceptIncoming == null ? incoming : incoming.Where(acceptIncoming);
            IEnumerable<string> merged = local.Union(accepted);
            if (acceptIncoming != null) merged = merged.ToList();
            local.Clear();
            foreach (var item in merged)
            {
                local.Add(item);
            }
            return local.ToList();
        }

        private static bool ShouldHandle(bool enabled, bool back, bool middle)
        {
            return legacy
                ? enabled && back || middle
                : MouseBackPolicy.ShouldHandle(enabled, back, middle);
        }

        // ── 1. 三个类别都要保住本地独有的规则 ─────────────────────────────────
        private static void MergeKeepsLocalOnly()
        {
            var words = Merge(new[] { "local-word", "shared" }, new[] { "shared", "server-word" });
            Check(words.SequenceEqual(new[] { "local-word", "shared", "server-word" }),
                "the keyword merge keeps the local-only word and appends the server-only one");

            var users = Merge(new[] { "uid-local", "uid-shared" }, new[] { "uid-shared", "uid-server" });
            Check(users.SequenceEqual(new[] { "uid-local", "uid-shared", "uid-server" }),
                "the user merge keeps the local-only id and appends the server-only one");

            var regulars = MergeRegex(new[] { "^local$", "^shared$" },
                new[] { "^shared$", "^server$", "(" });
            Check(regulars.SequenceEqual(new[] { "^local$", "^shared$", "^server$" }),
                "the regex merge keeps the local-only pattern and appends only the valid server pattern");
        }

        // ── 2. 快照独立于被清空的源集合 ───────────────────────────────────────
        private static void SnapshotSurvivesClearing()
        {
            var local = new ObservableCollection<string> { "keep", "shared" };
            var snapshot = Merge(local, new[] { "shared", "server" });
            local.Clear();
            Check(snapshot.SequenceEqual(new[] { "keep", "shared", "server" }),
                "the merged snapshot still holds the local entries after the source collection is cleared");
            Check(!ReferenceEquals(snapshot, local),
                "the merge returns its own list instead of the observable collection it read");
        }

        // ── 3. 正则有效性只过滤外来规则，失效规则不进入结果 ───────────────────
        private static void RegexValidity()
        {
            Check(!DanmuFilterRules.TryValidateRegex("(", out var error) && !string.IsNullOrEmpty(error),
                "an unbalanced pattern is rejected with a reason that can be shown next to the input");
            Check(DanmuFilterRules.TryValidateRegex("^ok$", out var noError) && noError == null,
                "a valid pattern passes without an error message");
            Check(!DanmuFilterRules.TryValidateRegex("   ", out _),
                "a blank pattern is rejected");

            var withStoredBad = MergeRegex(new[] { "^ok$", "(" }, new[] { "^new$" });
            Check(withStoredBad.SequenceEqual(new[] { "^ok$", "(", "^new$" }),
                "the incoming-only filter never drops a rule the local list already holds");
        }

        // ── 4. AddUnique 去重并如实报告 ───────────────────────────────────────
        private static void AddUniqueReports()
        {
            var target = new ObservableCollection<string> { "a" };
            Check(DanmuFilterRules.AddUnique(target, "b") && target.Count == 2,
                "AddUnique adds a new value and reports that it did");
            Check(!DanmuFilterRules.AddUnique(target, "a") && target.Count == 2,
                "AddUnique refuses a duplicate and reports that it did not add");
            Check(!DanmuFilterRules.AddUnique(target, "  ") && target.Count == 2,
                "AddUnique refuses a blank value");
            Check(!DanmuFilterRules.AddUnique(target, null) && target.Count == 2,
                "AddUnique refuses null");
        }

        // ── 5. 鼠标返回开关的判定表 ───────────────────────────────────────────
        private static void MouseBackTable()
        {
            Check(ShouldHandle(true, true, false), "enabled + back-side button is handled");
            Check(ShouldHandle(true, false, true), "enabled + middle button is handled");
            Check(ShouldHandle(true, true, true), "enabled + both buttons is handled");
            Check(!ShouldHandle(false, true, true), "disabled + both buttons is ignored");
            Check(!ShouldHandle(false, false, true), "disabled + middle button is ignored");
            Check(!ShouldHandle(true, false, false), "enabled + neither button is ignored");
            Check(!ShouldHandle(false, false, false), "disabled + neither button is ignored");
        }

        // ── 6. 接线：页面无法编译，按源码顺序钉住 ─────────────────────────────
        private static void SourceChecks()
        {
            var settingPageCs = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Pages/SettingPage.xaml.cs"));

            var live = Block(settingPageCs, "private void LiveDanmuSettingAddWord_Click");
            Check(live.Length > 0, "the live keyword handler is present");
            Check(live.Contains("LiveDanmuSettingTxtWord.Text = \"\""),
                "the live keyword handler clears the live input box after the add");
            Check(!Regex.IsMatch(live, @"(?<!Live)DanmuSettingTxtWord"),
                "the live keyword handler never touches the video keyword box");

            var regexHandler = Block(settingPageCs, "private async void DanmuSettingAddRegex_Click");
            Check(regexHandler.Length > 0, "the manual regex handler is present");
            var validated = regexHandler.IndexOf("TryValidateRegex", StringComparison.Ordinal);
            var addCall = regexHandler.IndexOf("AddDanmuFilterItem", StringComparison.Ordinal);
            var cleared = regexHandler.IndexOf("DanmuSettingTxtRegex.Text = \"\"", StringComparison.Ordinal);
            Check(validated >= 0 && addCall > validated,
                "the regex handler validates the pattern before it can reach the local list");
            Check(!regexHandler.Contains("ShieldRegulars.Add("),
                "the regex handler writes no rule itself, so nothing is saved before validation");
            Check(cleared > addCall && validated >= 0 && cleared > validated,
                "the regex handler clears the input only after the validated rule was added");
            Check(regexHandler.Contains("ShowRegexError"),
                "a rejected pattern is reported next to the input instead of only being logged");

            var settingPageXaml = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Pages/SettingPage.xaml"));
            Check(Regex.IsMatch(settingPageXaml,
                    "<TextBlock x:Name=\"DanmuSettingRegexError\"[^>]*Visibility=\"Collapsed\""),
                "the regex error line sits under the input and starts collapsed");

            var vm = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Modules/SettingVM.cs"));
            var add = Block(vm, "public Task<bool> AddDanmuFilterItem");
            Check(add.Length > 0, "AddDanmuFilterItem is present");
            Check(!add.Contains("GetDanmuFilterWords"),
                "AddDanmuFilterItem no longer reads the cloud rule list");
            Check(add.Contains("DanmuFilterRules.AddUnique") && add.Contains("TryValidateRegex"),
                "AddDanmuFilterItem validates and dedupes before adding to local");

            var sync = Block(vm, "public async Task SyncDanmuFilter");
            Check(sync.Length > 0, "SyncDanmuFilter is present");
            Check(!sync.Contains(".Union("),
                "SyncDanmuFilter no longer unions straight into a collection it is about to clear");
            Check(!sync.Contains(".Clear()"),
                "SyncDanmuFilter does not clear the observable collections itself");
            var mergeCalls = Regex.Matches(sync, "DanmuFilterRules\\.Merge").Count;
            var firstReset = sync.IndexOf("ResetShieldList(", StringComparison.Ordinal);
            Check(mergeCalls == 3, "SyncDanmuFilter builds a snapshot for each of the three categories");
            Check(firstReset > 0 &&
                    Regex.Matches(sync.Substring(0, firstReset), "DanmuFilterRules\\.Merge").Count == 3,
                "all three snapshots are materialised before any list is rewritten");
            var firstSave = sync.IndexOf("SettingHelper.SetValue", StringComparison.Ordinal);
            var lastReset = sync.LastIndexOf("ResetShieldList(", StringComparison.Ordinal);
            Check(firstSave > lastReset && lastReset > 0,
                "the merged lists are persisted only after they are in place");
            var failureReturn = sync.IndexOf("if (!result.status)", StringComparison.Ordinal);
            var firstMerge = sync.IndexOf("DanmuFilterRules.Merge", StringComparison.Ordinal);
            Check(failureReturn >= 0 && failureReturn < firstMerge,
                "a failed request returns before any rule list is touched");

            var noTab = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/NoTabMainPage.xaml.cs"));
            Check(noTab.Contains("MouseBackPolicy.ShouldHandle"),
                "the single-window page routes pointer presses through the mouse-back policy");
            Check(!noTab.Contains("XButton1Pressed ||") && !noTab.Contains("|| par =="),
                "the single-window switch is not bypassed by button precedence");

            var playerControl = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Controls/PlayerControl.xaml.cs"));
            var overlay = Block(playerControl, "private async void DanmuSettingAddWord_Click");
            Check(overlay.Length > 0, "the player overlay keyword handler is present");
            Check(!overlay.Contains("ShieldWords.Add("),
                "the player overlay no longer writes the keyword itself before the add call");
            Check(!playerControl.Contains("已经添加到本地，但远程同步失败"),
                "the misleading remote-sync failure toast is gone from the player overlay");
        }

        private static string Block(string text, string marker)
        {
            var start = text.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return string.Empty;
            var open = text.IndexOf('{', start);
            if (open < 0) return string.Empty;
            var depth = 0;
            for (var i = open; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0) return text.Substring(open, i - open + 1);
                }
            }
            return string.Empty;
        }

        private static string SourceRoot()
        {
            var configured = Environment.GetEnvironmentVariable("SETTINGS1_SRC_ROOT");
            if (!string.IsNullOrEmpty(configured)) return configured;
            var dir = AppContext.BaseDirectory;
            while (dir != null && !Directory.Exists(Path.Combine(dir, "src/BiliLite.UWP")))
            {
                dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
            }
            if (dir == null) throw new DirectoryNotFoundException("repository root not found");
            return dir;
        }
    }
}

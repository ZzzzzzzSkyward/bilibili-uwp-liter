using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using BiliLite.Services;

namespace PlayersessProbe
{
    /// <summary>
    /// 计数用的假资源:释放几次由它自己记着,"恰好一次"才有得断言。
    /// </summary>
    internal sealed class FakeResource : IDisposable
    {
        private readonly bool throws;

        public FakeResource(string name, bool throws = false)
        {
            Name = name;
            this.throws = throws;
        }

        public string Name { get; }

        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
            if (throws) throw new InvalidOperationException("dispose failed: " + Name);
        }
    }

    internal static class Program
    {
        private static int checks;
        private static readonly List<string> failures = new List<string>();

        private static int Main()
        {
            StateCases();
            SourceCases();

            Console.WriteLine();
            if (failures.Count == 0)
            {
                Console.WriteLine("全部通过:" + checks + " 项");
                return 0;
            }
            Console.WriteLine("失败 " + failures.Count + " 项(共 " + checks + " 项):");
            foreach (var failure in failures) Console.WriteLine("  - " + failure);
            return 1;
        }

        private static void StateCases()
        {
            // 一次打开:Begin 发 id、记下原因、还是当前的那一次
            var state = new PlayerSessionState();
            Check(state.CurrentId == 0, "没有打开过时 CurrentId 是 0");
            Check(state.Current == null, "没有打开过时 Current 是 null");
            var first = state.Begin(PlayerSessionReason.QualityChange);
            Check(first.Id == 1, "第一次 Begin 拿到 id 1");
            Check(first.Reason == PlayerSessionReason.QualityChange, "票据记下 Begin 给的原因");
            Check(state.CurrentId == 1, "CurrentId 跟着当前会话");
            Check(first.IsCurrent, "刚开始的会话是当前的");
            Check(state.IsCurrent(first.Id), "IsCurrent(id) 认得当前会话");
            Check(state.IsPending(first.Id), "刚开始的会话还没结束");
            Check(!state.IsCurrent(999), "没发过的 id 不是当前会话");

            // 两次重叠打开:只有第二次被接受,第一次的 Accept 被拒
            var stale = first;
            var second = state.Begin(PlayerSessionReason.QualityChange);
            Check(second.Id == 2, "第二次 Begin 的 id 递增");
            Check(!stale.Accept(), "被取代的那一次 Accept 被拒");
            Check(second.Accept(), "当前的那一次 Accept 通过");
            Check(!second.Accept(), "同一次会话不会接受第二个 Accept");
            Check(!state.IsPending(second.Id), "接受之后不再算没结束");
            Check(second.IsCurrent, "接受之后仍是当前会话(还在播)");

            // 作废:Accept 被拒、资源释放一次、当前会话没了
            var third = state.Begin(PlayerSessionReason.Open);
            var pendingResource = new FakeResource("pending-source");
            Check(third.Hold(pendingResource), "在飞的时候资源挂到会话名下");
            Check(pendingResource.DisposeCount == 0, "挂着的资源还没释放");
            Check(state.Invalidate(), "作废当前会话返回真");
            Check(!third.Accept(), "作废之后它的 Accept 被拒");
            Check(!third.IsCurrent, "作废之后 IsCurrent 是假");
            Check(state.Current == null, "作废之后没有当前会话");
            Check(state.CurrentId == 0, "作废之后 CurrentId 回到 0");
            Check(pendingResource.DisposeCount == 1, "在飞的那次挂着的资源被释放一次");
            Check(!state.Invalidate(), "重复作废返回假");
            Check(pendingResource.DisposeCount == 1, "重复作废不再释放第二次");
            Check(!third.Release(), "作废之后再 Release 什么都不做");
            Check(pendingResource.DisposeCount == 1, "作废之后再 Release 不释放第二次");

            // 释放:失败路径由调用方调,一次有效第二次落空
            var fourth = state.Begin(PlayerSessionReason.EngineFallback);
            var failedResource = new FakeResource("failed-source");
            Check(fourth.Hold(failedResource), "换引擎重开也能挂资源");
            Check(fourth.Release(), "Release 释放这次挂着的资源");
            Check(failedResource.DisposeCount == 1, "Release 释放了一次");
            Check(!fourth.Release(), "重复 Release 返回假");
            Check(failedResource.DisposeCount == 1, "重复 Release 不释放第二次");
            Check(!fourth.IsCurrent, "Release 之后这次打开不再是当前的");
            Check(!fourth.Accept(), "Release 之后 Accept 被拒");
            Check(!fourth.Hold(new FakeResource("late")), "Release 之后再挂资源被拒");

            // 迟到的资源:会话作废之后才建出来的东西当场释放
            var fifth = state.Begin(PlayerSessionReason.LocalOpen);
            state.Invalidate();
            var late = new FakeResource("late-source");
            Check(!fifth.Hold(late), "作废之后 Hold 返回假");
            Check(late.DisposeCount == 1, "作废之后才建出来的资源当场释放");

            // 已经交给播放器的资源不归会话:作废时不碰
            var sixth = state.Begin(PlayerSessionReason.Open);
            var published = new FakeResource("published-source");
            Check(sixth.Hold(published), "发布之前挂到会话名下");
            Check(sixth.Accept(), "资源进字段时接受这次会话");
            Check(published.DisposeCount == 0, "接受时不动已经交给播放器的资源");
            state.Invalidate();
            Check(published.DisposeCount == 0, "作废时不释放已经交给播放器的资源");
            Check(!state.IsCurrent(sixth.Id), "关掉之后旧 id 全都不是当前的");
            Check(!state.IsCurrent(fifth.Id), "更早的 id 也不是当前的");

            // 释放异常不挡住后面的资源:关页时该清干净
            var seventh = state.Begin(PlayerSessionReason.Open);
            var broken = new FakeResource("broken-source", true);
            var healthy = new FakeResource("healthy-source");
            seventh.Hold(broken);
            seventh.Hold(healthy);
            state.Invalidate();
            Check(broken.DisposeCount == 1, "释放抛异常的资源也只释放一次");
            Check(healthy.DisposeCount == 1, "前一个释放抛异常不影响后一个");

            // 重叠打开:上一次挂着的资源在新 Begin 处释放
            var eighth = state.Begin(PlayerSessionReason.Open);
            var orphan = new FakeResource("orphan-source");
            eighth.Hold(orphan);
            var ninth = state.Begin(PlayerSessionReason.Open);
            Check(orphan.DisposeCount == 1, "新 Begin 释放上一次还挂着的资源");
            Check(!eighth.Accept(), "新 Begin 之后上一次的 Accept 被拒");
            Check(ninth.Accept(), "新 Begin 的那一次可以接受");
            Check(orphan.DisposeCount == 1, "换会话没有再释放一次");
            Check(!state.Hold(ninth.Id, null), "空资源不算挂上");
        }

        /// <summary>
        /// 源码接线:会话本身在 probe 里跑,这里只钉住"每条打开路径都带着它走"。
        /// </summary>
        private static void SourceCases()
        {
            var repo = FindRepoRoot();
            var controlPath = Path.Combine(repo, "src", "BiliLite.UWP", "Controls", "PlayerControl.xaml.cs");
            var playerPath = Path.Combine(repo, "src", "BiliLite.UWP", "Controls", "Player.xaml.cs");
            var control = File.ReadAllText(controlPath);
            var player = File.ReadAllText(playerPath);

            Check(control.Contains("await ChangeQuality(info.CurrentQuality);"), "SetQuality 等切清晰度收尾,不再 fire-and-forget");
            Check(CountOf(control, "ChangeQuality(info.CurrentQuality);") == CountOf(control, "await ChangeQuality(info.CurrentQuality);"),
                "SetQuality 里没有不带 await 的 ChangeQuality 调用");
            Check(control.Contains("if (_engineFallbackRunning) return;"), "换引擎重开有在飞判定");
            Check(control.Contains("_engineFallbackRunning = false;"), "换引擎重开的在飞标记会放开");
            Check(control.Contains("PlayerSessionReason.EngineFallback"), "换引擎重开开了自己的会话");
            Check(control.Contains("PlayerSessionReason.QualityChange"), "切清晰度开了自己的会话");
            Check(control.Contains("PlayerSessionReason.LocalOpen"), "本地文件打开开了自己的会话");

            var disposeStart = control.IndexOf("public async void Dispose()", StringComparison.Ordinal);
            Check(disposeStart > 0, "找得到 PlayerControl.Dispose");
            if (disposeStart > 0)
            {
                var disposeEnd = control.IndexOf("private void GridViewSelectColor_ItemClick", disposeStart, StringComparison.Ordinal);
                var dispose = disposeEnd > disposeStart ? control.Substring(disposeStart, disposeEnd - disposeStart) : control.Substring(disposeStart);
                Check(dispose.Contains("_playerSessions.Invalidate();"), "关页在 Dispose 里作废在飞的那次打开");
            }

            var setPlayItem = Section(control, "private async Task SetPlayItem(int index, PlayTargetStart start)", "private SubtitleModel subtitles;");
            Check(setPlayItem.Contains("_playerSessions.Invalidate();"), "换P/换视频作废在飞的那次打开");

            var openCalls = Regex.Matches(control, @"await Player\.(PlayDashUseNative|PlayDashUseFFmpegInterop|PlayDashUrlUseFFmpegInterop|PlaySingleFlvUseFFmpegInterop|PlaySingleFlvUseSYEngine|PlayVideoUseSYEngine|PlayerSingleMp4UseNativeAsync)\(");
            var live = 0;
            var withoutSession = 0;
            foreach (Match call in openCalls)
            {
                // 注释掉的旧调用不算打开路径
                var lineStart = control.LastIndexOf('\n', call.Index) + 1;
                if (control.Substring(lineStart, call.Index - lineStart).TrimStart().StartsWith("//")) continue;
                live++;
                var end = control.IndexOf(");", call.Index, StringComparison.Ordinal);
                if (end < 0 || !control.Substring(call.Index, end - call.Index).Contains("session: session")) withoutSession++;
            }
            Check(live == 12, "PlayerControl 里 12 条打开路径都被数到(实际 " + live + ")");
            Check(withoutSession == 0, "每个打开调用都带着会话(漏了 " + withoutSession + " 个)");

            var staleChecks = Regex.Matches(control, @"if \(!session\.IsCurrent\) return");
            Check(staleChecks.Count >= 5, "打开之后的继续处都拒收旧会话(实际 " + staleChecks.Count + " 处)");

            Check(CountOf(player, "PlayerSessionTicket session = null") >= 7, "Player 的每条打开路径都收会话参数");
            Check(CountOf(player, "if (IsSessionStale(session)) return;") >= 9, "播放器回调按会话拒收");
            Check(player.Contains("if (!HoldSessionResource(session, videoSource)) return SupersededOpen();"), "FFmpeg 源在发布前挂在会话名下");
            Check(player.Contains("if (!session.Hold(") || player.Contains("HoldSessionResource("), "Player 通过会话托管资源");
            Check(CountOf(player, "session?.Accept();") >= 3, "源进字段之前接受会话");
            Check(player.Contains("using BiliLite.Services;"), "Player 引用了会话所在命名空间");
        }

        private static string Section(string text, string start, string end)
        {
            var from = text.IndexOf(start, StringComparison.Ordinal);
            if (from < 0) return "";
            var to = text.IndexOf(end, from, StringComparison.Ordinal);
            return to > from ? text.Substring(from, to - from) : text.Substring(from);
        }

        private static int CountOf(string haystack, string needle)
        {
            var count = 0;
            var index = 0;
            while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += needle.Length;
            }
            return count;
        }

        private static void Check(bool ok, string message)
        {
            checks++;
            if (ok) Console.WriteLine("[ok] " + message);
            else Fail(message);
        }

        private static void Fail(string message)
        {
            failures.Add(message);
            Console.WriteLine("[FAIL] " + message);
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "src", "BiliLite.UWP", "Controls", "PlayerControl.xaml")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException("找不到仓库根目录(向上找 src/BiliLite.UWP/Controls/PlayerControl.xaml)");
        }
    }
}

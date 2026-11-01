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
        // 默认读仓库里的 Player.xaml.cs;--player 换一份(比如 git show HEAD:... 拷出来的旧文件)就能验红
        private static string playerPath;

        private static int Main(string[] args)
        {
            playerPath = Path.Combine(FindRepoRoot(), "src", "BiliLite.UWP", "Controls", "Player.xaml.cs");
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] != "--player") continue;
                if (i + 1 >= args.Length)
                {
                    Fail("--player 后面要跟一个源文件路径");
                    return 1;
                }
                playerPath = args[i + 1];
            }

            StateCases();
            ResumeAfterReplacementCases();
            SourceCases();
            PublishOrderCases();

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
        /// 复核意见里的场景:A 还挂在 SaveAndGetFileUriAsync(或者本地多段的文件解析)上时,
        /// 换视频把 A 作废、B 打开并且已经把自己的播放器接进 _playerVideo;A 的 await 回来时必须放弃自己那一份。
        /// 会话本身是真代码(PlayerSessionState),这里按 Player 的发布顺序走一遍:A 先 Hold,等完源再判定,
        /// 判定过了才 Accept + 发布;B 已经发布之后 A 再回来。
        /// </summary>
        private static void ResumeAfterReplacementCases()
        {
            var state = new PlayerSessionState();
            var field = "none"; // 正在播的播放器(_playerVideo)

            // A:打开开始,建出来的播放器先挂在自己的会话名下,然后去等源
            var a = state.Begin(PlayerSessionReason.Open);
            var aPlayer = new FakeResource("player-A");
            Check(a.Hold(aPlayer), "A 建出来的播放器挂到 A 名下");
            Check(field == "none", "A 等源的时候还没有接进正在播的播放器");

            // SetPlayItem 换视频:先作废在飞的 A(_playerSessions.Invalidate()),B 再开自己的会话
            Check(state.Invalidate(), "换视频作废还在等源的 A");
            Check(aPlayer.DisposeCount == 1, "A 被丢下的播放器在作废时释放一次");
            var b = state.Begin(PlayerSessionReason.Open);
            var bPlayer = new FakeResource("player-B");
            Check(b.Hold(bPlayer), "B 的播放器挂到 B 名下");
            Check(b.Accept(), "B 在发布之前接受会话");
            field = bPlayer.Name; // B 接进 _playerVideo
            Check(field == "player-B", "B 已经把自己装进正在播的播放器");

            // A 的 await 回来了:判定在会话上,不读也不写正在播的播放器
            Check(!a.IsCurrent, "A 回来时已经不是当前的会话");
            Check(aPlayer.DisposeCount == 1, "A 的播放器已经释放过,回来时不再释放第二次");
            if (a.IsCurrent) field = aPlayer.Name; // 旧的接线:判定排在发布后面,这里就会把 B 顶掉
            Check(field == "player-B", "A 迟到不覆盖 B 已经装好的播放器");
            Check(!a.Accept(), "A 迟到的 Accept 被拒");
            Check(aPlayer.DisposeCount == 1, "A 迟到的 Accept 不会再释放一次");
            Check(!a.Release(), "A 迟到的 Release 落空");
            Check(aPlayer.DisposeCount == 1, "A 被丢下的播放器恰好释放一次");
            Check(state.Current == b, "当前会话还是 B");
            Check(bPlayer.DisposeCount == 0, "A 的收尾不碰 B 的播放器");
            Check(field == bPlayer.Name, "正在播的播放器还是 B 的");

            // 关页/换视频照旧:交给过播放器的 B 不归会话,作废时不再由会话释放它(ClosePlay 管)
            state.Invalidate();
            Check(bPlayer.DisposeCount == 0, "已经交给播放器的 B 不在作废时由会话释放");
            Check(aPlayer.DisposeCount == 1, "关掉 B 时不动已经释放过的 A");
        }

        /// <summary>
        /// 源码接线:会话本身在 probe 里跑,这里只钉住"每条打开路径都带着它走"。
        /// </summary>
        private static void SourceCases()
        {
            var repo = FindRepoRoot();
            var controlPath = Path.Combine(repo, "src", "BiliLite.UWP", "Controls", "PlayerControl.xaml.cs");
            var control = File.ReadAllText(controlPath);
            if (!File.Exists(playerPath))
            {
                Fail("读不到 Player 源文件:" + playerPath);
                return;
            }
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

        /// <summary>
        /// 发布顺序:播放器先建在本地并挂到会话名下,等完源再判定会话,判定过了才装源、接受会话、接进 _playerVideo。
        /// 对着四条打开路径钉顺序;--player 换一份旧文件跑,这几条就会红。
        /// </summary>
        private static void PublishOrderCases()
        {
            if (!File.Exists(playerPath))
            {
                Fail("读不到 Player 源文件:" + playerPath);
                return;
            }
            var player = File.ReadAllText(playerPath);

            CheckPublishOrder(player, "PlayDashUseNative(", "PlayerSingleMp4UseNativeAsync(", "PlayDashUseNative");
            CheckPublishOrder(player, "PlayerSingleMp4UseNativeAsync(", "PlayDashUseFFmpegInterop(", "PlayerSingleMp4UseNativeAsync");
            CheckPublishOrder(player, "PlaySingleFlvUseSYEngine(", "PlayVideoUseSYEngine(", "PlaySingleFlvUseSYEngine");
            CheckPublishOrder(player, "PlayVideoUseSYEngine(", "public void SetRatioMode(", "PlayVideoUseSYEngine");

            // 三条 FFmpeg 路径本来就把源挂在会话名下、接受之后才建播放器:这次不动它们,只钉住没退化
            CheckHeldThenAccepted(player, "PlayDashUseFFmpegInterop(", "PlayDashUrlUseFFmpegInterop(", "PlayDashUseFFmpegInterop");
            CheckHeldThenAccepted(player, "PlayDashUrlUseFFmpegInterop(", "PlaySingleFlvUseFFmpegInterop(", "PlayDashUrlUseFFmpegInterop");
            CheckHeldThenAccepted(player, "PlaySingleFlvUseFFmpegInterop(", "PlaySingleFlvUseSYEngine(", "PlaySingleFlvUseFFmpegInterop");
        }

        private static void CheckPublishOrder(string player, string start, string end, string name)
        {
            var body = Code(Section(player, start, end));
            if (body.Length == 0)
            {
                Fail(name + " 找不到方法体");
                return;
            }
            var problem = PublishOrderProblem(body);
            Check(problem == null, name + " 的发布顺序(先挂会话名下、等完源再判定、判定过了才接进播放器)" + (problem == null ? "" : ": " + problem));
        }

        /// <summary>
        /// 返回第一条不满足的说明,null 表示顺序对。
        /// </summary>
        private static string PublishOrderProblem(string body)
        {
            if (body.Contains("_playerVideo = new MediaPlayer()")) return "播放器直接建进 _playerVideo,判定之前就顶掉了正在播的那一份";
            if (body.Contains("_playerVideo.Source =")) return "源直接装在 _playerVideo 上,判定之前就可能改到别人的播放器";
            var hold = body.IndexOf("HoldSessionPlayer(session, video)", StringComparison.Ordinal);
            if (hold < 0) return "建出来的播放器没有先挂到会话名下";
            var stale = body.IndexOf("if (IsSessionStale(session)) return SupersededOpen();", StringComparison.Ordinal);
            if (stale < 0) return "等完源之后没有判定会话";
            var source = body.IndexOf("video.Source =", StringComparison.Ordinal);
            if (source < 0) return "没有给本地播放器装源";
            var accept = body.IndexOf("session?.Accept();", StringComparison.Ordinal);
            if (accept < 0) return "接进播放器之前没有接受会话";
            var publish = body.IndexOf("_playerVideo = video;", StringComparison.Ordinal);
            if (publish < 0) return "没有把本地这一份接进 _playerVideo";
            if (hold > stale) return "判定会话排在挂到会话名下之前";
            if (stale > source) return "装源排在判定会话之前";
            if (source > accept) return "接受会话排在装源之前";
            if (accept > publish) return "接进 _playerVideo 排在接受会话之前";
            return null;
        }

        private static void CheckHeldThenAccepted(string player, string start, string end, string name)
        {
            var body = Code(Section(player, start, end));
            if (body.Length == 0)
            {
                Fail(name + " 找不到方法体");
                return;
            }
            var hold = body.IndexOf("HoldSessionResource(session, videoSource)", StringComparison.Ordinal);
            var accept = body.IndexOf("session?.Accept();", StringComparison.Ordinal);
            var publish = body.IndexOf("_playerVideo = new MediaPlayer()", StringComparison.Ordinal);
            Check(hold >= 0 && accept > hold && publish > accept,
                name + " 没动:源先挂会话名下,接受之后才建播放器(hold=" + hold + " accept=" + accept + " publish=" + publish + ")");
        }

        /// <summary>
        /// 顺序判定只看代码:注释里的旧写法不算数。
        /// </summary>
        private static string Code(string text)
        {
            return Regex.Replace(text, @"^[ \t]*//.*$", "", RegexOptions.Multiline);
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

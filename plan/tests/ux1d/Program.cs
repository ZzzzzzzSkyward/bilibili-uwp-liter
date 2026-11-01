using System;
using System.Collections.Generic;
using System.IO;
using BiliLite.Models.Common;

namespace Ux1dProbe
{
    /// <summary>
    /// Ux1d: 换目标的等待态、断在取页上的那一步、晚到结果的作废规则、抽屉的键盘与读屏。
    ///
    /// Executed against product source: Models/PlayContinuation.cs (换目标在途状态、
    /// 断在取页上的记录、身份=视频+播放列表+代数) and Models/PlayDirectory.cs (等待的那一行),
    /// so editing those decisions changes what the executed assertions see. The player, the page
    /// and the controls cannot run outside UWP, so their wiring is read as text; every such check is
    /// named "source:".
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static int Main()
        {
            PendingGate();
            PendingRowInTheDirectory();
            PendingAdvanceLifecycle();
            PendingAdvanceWithoutIdentity();
            StaleResultIdentity();
            PlayerSourceWiring();
            PageSourceWiring();
            DrawerSourceWiring();
            MovedPolicyWiring();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        /// <summary>
        /// 一拍在路上时第二次请求直接丢掉,不排队:等这一拍收掉之后也没有替它补走的那一次
        /// </summary>
        private static void PendingGate()
        {
            var state = new PlayTransitionState();
            state.Commit("6001", "ml-1");
            Check(!state.IsPending, "a settled page has nothing in flight");

            Check(state.Begin("第8讲", "7008"), "a continuation move starts one transition");
            Check(state.IsPending && state.PendingTitle == "第8讲" && state.PendingKey == "7008",
                "the pending target is the bare title and the row key");

            var generation = state.Generation;
            Check(!state.Begin("第9讲", "7009"),
                "a second request while one transition runs is ignored");
            Check(state.PendingTitle == "第8讲" && state.PendingKey == "7008",
                "the ignored request does not take over the pending target");
            Check(state.Generation == generation,
                "the ignored request does not open a new generation");

            Check(state.Retitle("第9讲", "7009"), "the same transition can be retitled once the row is known");
            Check(state.PendingTitle == "第9讲" && state.Generation == generation,
                "retitling keeps the transition and its generation");

            state.Settle();
            Check(!state.IsPending && state.PendingTitle == "" && state.PendingKey == "",
                "settling clears the wait");
            Check(state.Generation == generation, "settling does not open a new generation");

            // 没有排队:收掉之后不会自己冒出第二个目标
            Check(!state.IsPending && state.PendingTitle == "",
                "the ignored request is dropped, not queued");

            Check(state.Begin("第10讲", "7010"), "after the wait the next request is accepted again");
            state.Settle();
        }

        /// <summary>
        /// 等待的那一行只多一个转圈:正在播放的条和字仍然只在当前行上
        /// </summary>
        private static void PendingRowInTheDirectory()
        {
            var playlist = new VideoPlaylist()
            {
                Index = 0,
                Playlist = QueueRows(3),
                MediaListId = "ml-1",
                Title = "课程",
            };
            var model = PlayDirectoryBuilder.FromPlaylist(playlist, null, "5001");

            var applied = PlayDirectoryBuilder.ApplyPending(model, "5003");
            Check(ReferenceEquals(applied, model) && model.PendingKey == "5003",
                "the pending row key lands on the directory model");

            var rows = model.Sections[0].Rows;
            Check(rows[2].IsPending && !rows[2].IsCurrent && rows[2].CurrentBadge == "",
                "the pending row is not marked as the playing row");
            Check(rows[0].IsCurrent && !rows[0].IsPending && rows[0].CurrentBadge == "正在播放",
                "the playing row keeps its 正在播放 bar and text");
            Check(!rows[1].IsPending, "a row that is neither playing nor pending carries no mark");

            Check(rows[2].AutomationName == "第3讲 正在切换",
                "the pending row's accessible name states the state without repeating the label");
            Check(rows[0].AutomationName == "第1讲 正在播放",
                "the playing row's accessible name states the state");
            Check(rows[1].AutomationName == "第2讲",
                "a plain row is read as its title only");

            PlayDirectoryBuilder.ApplyPending(model, "");
            Check(!rows[2].IsPending && model.PendingKey == "",
                "ending the transition clears the pending mark from the rebuilt directory");
            Check(rows[0].IsCurrent, "clearing the pending mark leaves the playing row alone");
        }

        /// <summary>
        /// 断在取页上的那一步:取页失败时记下,重试取到就接着走,再失败留着,列表到头才取消
        /// </summary>
        private static void PendingAdvanceLifecycle()
        {
            var state = new PlayTransitionState();
            state.Commit("6001", "ml-1");
            state.Begin("下一页", "");
            state.Settle();
            var record = state.RememberAdvance(WatchSequenceSourceKind.Playlist, 19, false, true);
            var generation = state.Generation;

            Check(!state.IsPending, "recording the interrupted move does not leave a pending wait behind");
            Check(ReferenceEquals(state.PendingAdvance, record), "the pending-advance record is held by the page state");
            Check(record.VideoId == "6001" && record.MediaListId == "ml-1" && record.Generation == generation,
                "the record pins the identity the transition was interrupted under");
            Check(record.Source == WatchSequenceSourceKind.Playlist && record.RowIndex == 19,
                "the record pins the source and the row it would have advanced from");
            Check(!record.SkipRemainingParts && record.Manual,
                "the record pins how the move was asked for");

            // 重试又失败:记录留着,下一次重试还能接着走
            Check(state.DecideResume(false, PlayDirectoryPagingState.Failed) == PlayAdvanceResume.WaitForRetry,
                "a retry that fails again leaves the interrupted move waiting");
            Check(state.PendingAdvance != null && state.AdvanceIsCurrent(),
                "the record survives a failed retry");

            // 重试取到了:接着走,记录先收掉,不会推进两次
            Check(state.DecideResume(true, PlayDirectoryPagingState.More) == PlayAdvanceResume.Resume,
                "a retry that lands resumes the interrupted move");
            Check(state.PendingAdvance == null, "the resumed move consumes the record exactly once");
            Check(state.DecideResume(true, PlayDirectoryPagingState.More) == PlayAdvanceResume.None,
                "a second retry never advances twice");

            // 列表到头:这一次推进取消,记录不留下
            state.RememberAdvance(WatchSequenceSourceKind.Playlist, 19, true, false);
            Check(state.DecideResume(false, PlayDirectoryPagingState.Exhausted) == PlayAdvanceResume.Cancel,
                "an exhausted list cancels the interrupted move");
            Check(state.PendingAdvance == null, "a cancelled move leaves no record behind");

            // 用户自己选了行
            state.RememberAdvance(WatchSequenceSourceKind.Playlist, 19, false, true);
            state.DropAdvance();
            Check(state.PendingAdvance == null, "activating a row drops the record");

            // 开了另一条视频
            state.RememberAdvance(WatchSequenceSourceKind.Playlist, 19, false, true);
            state.Commit("6002", "ml-1");
            Check(state.PendingAdvance == null, "playing another video drops the record");

            // 关页
            state.RememberAdvance(WatchSequenceSourceKind.Playlist, 19, false, true);
            state.Begin("第9讲", "7009");
            state.Close();
            Check(state.PendingAdvance == null && !state.IsPending,
                "closing the page drops the record and the wait");
        }

        /// <summary>
        /// 记录只在原身份下算数:换了视频、换了播放列表、又开了新的一拍都不再复活
        /// </summary>
        private static void PendingAdvanceWithoutIdentity()
        {
            var state = new PlayTransitionState();
            state.Commit("6001", "ml-1");
            state.RememberAdvance(WatchSequenceSourceKind.Playlist, 4, false, true);

            state.SetMediaList("ml-2");
            Check(!state.AdvanceIsCurrent() && state.PendingAdvance == null,
                "another playlist drops the record instead of reviving it");

            state.Commit("6001", null);
            state.RememberAdvance(WatchSequenceSourceKind.UgcSeason, 2, false, true);
            state.Commit("6009", "");
            Check(state.PendingAdvance == null, "another video drops the record");

            state.RememberAdvance(WatchSequenceSourceKind.Playlist, 3, false, true);
            // 新的一拍换了代数:旧记录不能再被一次重试拉起来
            state.Begin("第5讲", "7005");
            Check(!state.AdvanceIsCurrent(), "a newer transition invalidates the old record");
            Check(state.DecideResume(true, PlayDirectoryPagingState.More) == PlayAdvanceResume.None,
                "a retry after a newer transition does not revive the old move");
            Check(state.PendingAdvance == null, "the stale record is dropped when it is found stale");
            state.Settle();
        }

        /// <summary>
        /// 晚到结果的作废规则:视频 id + 播放列表 id + 代数,三项都相同才算同一拍
        /// </summary>
        private static void StaleResultIdentity()
        {
            var state = new PlayTransitionState();
            state.Commit("6001", "ml-1");
            var before = state.Ticket();
            Check(state.IsCurrent(before), "a ticket taken from the settled page is current");

            state.Begin("第8讲", "7008");
            Check(!state.IsCurrent(before), "a newer transition invalidates the ticket taken before it");
            var during = state.Ticket();
            Check(state.IsCurrent(during), "the ticket taken for the transition is current");

            Check(state.Begin("第9讲", "7009") == false,
                "no second transition starts while the first is in flight");
            Check(state.IsCurrent(during), "an ignored request leaves the ticket current");

            // 分P切换不换视频:同一份身份继续有效
            state.Commit("6001", "ml-1");
            Check(state.IsCurrent(during), "a part switch inside the same video keeps the ticket current");

            state.Commit("6002", "ml-1");
            Check(!state.IsCurrent(during), "playing another video invalidates the ticket");
            Check(!state.IsPending, "playing a video settles whatever wait was open");

            var afterVideo = state.Ticket();
            state.SetMediaList("ml-9");
            Check(!state.IsCurrent(afterVideo), "another playlist invalidates the ticket");

            var afterList = state.Ticket();
            state.Close();
            Check(!state.IsCurrent(afterList), "closing the page invalidates every outstanding ticket");
            Check(state.VideoId == "" && state.MediaListId == "" && !state.IsPending,
                "closing the page clears the identity and the wait");
        }

        private static void PlayerSourceWiring()
        {
            var player = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml.cs");
            var xaml = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml");

            var show = Body(player, "public void ShowTransitionPending(string targetTitle)");
            Check(show.Contains("TransitionPendingText.Text = _pendingTargetText;"),
                "source: the pending text is the target itself");
            Check(!show.Contains("正在切换") || show.Contains("AutomationProperties.SetName(TransitionPendingHost,"),
                "source: the state word is only used for the accessible name");
            Check(show.Contains("BottomBtnNext.IsEnabled = false;"),
                "source: the next button does not stay enabled-but-dead while pending");
            var clear = Body(player, "public void ClearTransitionPending()");
            Check(clear.Contains("TransitionPendingHost.Visibility = Visibility.Collapsed;")
                && clear.Contains("BottomBtnNext.IsEnabled = true;"),
                "source: ending the wait restores the overlay and the button");

            var label = Body(player, "private void ApplyNextLabel()");
            Check(label.Contains("_transitionPending && !string.IsNullOrEmpty(_pendingTargetText)")
                && label.Contains("_pendingTargetText"),
                "source: the next button's preview keeps showing the pending target");
            Check(Body(player, "public void UpdateSequenceInfo").Contains("ApplyNextLabel();"),
                "source: a redraw goes through the same preview rule instead of overwriting the pending target");
            Check(!Body(player, "public void UpdateSequenceInfo").Contains("_transitionPending"),
                "source: the preview rule lives in one place");

            Check(Body(player, "private async void BottomBtnNext_Click").Contains("if (_transitionPending) return;"),
                "source: the next button ignores a press while pending");
            var nextShortcut = Body(player, "case Windows.System.VirtualKey.X:");
            Check(nextShortcut.Contains("if (_transitionPending) break;"),
                "source: the next shortcut is ignored while pending, not queued");
            var previousShortcut = Body(player, "case Windows.System.VirtualKey.Z:");
            Check(previousShortcut.Contains("if (_transitionPending) break;"),
                "source: the previous shortcut is ignored the same way");

            var pendingMarkup = Between(xaml, "x:Name=\"TransitionPendingHost\"", "</Border>");
            Check(CountOf(pendingMarkup, "<ProgressRing") == 1 && CountOf(pendingMarkup, "<TextBlock") == 1,
                "source: the pending overlay is one ring and one label, no second icon");
            Check(!pendingMarkup.Contains("正在切换"), "source: the pending overlay writes no state text of its own");
            Check(xaml.Contains("AutomationProperties.LiveSetting=\"Polite\""),
                "source: the pending overlay announces itself");
            Check(CountOf(xaml, "x:Name=\"TransitionPendingHost\"") == 1,
                "source: the player owns one pending overlay");

            var hide = Body(player, "public void HideDirectory()");
            Check(hide.Contains("BottomBtnDirectory.Focus(FocusState.Programmatic);"),
                "source: closing the drawer returns focus to the 目录 button");
            var showDrawer = Body(player, "public void ShowDirectory(PlayDirectoryModel model)");
            Check(showDrawer.Contains("DirectoryDrawer.LocateCurrent();") && showDrawer.Contains("DirectoryDrawer.FocusRows();"),
                "source: opening the drawer puts focus in its rows");
            Check(player.Contains("if (args.VirtualKey == Windows.System.VirtualKey.Escape)") 
                && Body(player, "private async void PlayerControl_KeyDown").Contains("if (_directoryOpen)"),
                "source: Escape is handled by the drawer before the player shortcuts");
        }

        private static void PageSourceWiring()
        {
            var page = ReadSource("src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs");

            var advance = Body(page, "private async Task<bool> AdvanceSequenceAsync");
            Check(advance.Contains("if (transition.IsPending) return false;"),
                "source: the second next request is dropped in AdvanceSequenceAsync, not queued");
            Check(CountOf(page, "transition.IsPending") >= 4,
                "source: the same pending gate is applied to the other activation entries");
            Check(advance.Contains("if (!BeginTransition(DescribeStep(step), \"\")) return false;"),
                "source: a paging step before the row is playable shows a pending state too");
            Check(advance.Contains("BeginTransition(RowTitle(step.RowIndex), RowKey(step.RowIndex))"),
                "source: a remote row's transition waits under the target's title and row key");
            Check(advance.Contains("RememberInterruptedAdvance(skipRemainingParts, manual);"),
                "source: a paging failure records the intended move");
            Check(advance.Contains("if (!transition.IsCurrent(ticket)) return false;"),
                "source: a page that lands after the identity changed no longer advances");

            var begin = Body(page, "private bool BeginTransition(string targetTitle, string pendingKey)");
            Check(begin.Contains("player.ShowTransitionPending(targetTitle);") && begin.Contains("RedrawOpenDirectory();"),
                "source: one place shows the wait in the player and in the drawer");
            Check(Body(page, "private void EndTransition()").Contains("player.ClearTransitionPending();"),
                "source: one place takes the wait down");

            var request = Body(page, "private async Task RequestMediaListPageAsync");
            Check(request.Contains("var landed = await request;") && request.Contains("await ResumeInterruptedAdvanceAsync(landed);"),
                "source: the footer and drawer retry resume the interrupted transition");
            var resume = Body(page, "private async Task ResumeInterruptedAdvanceAsync(bool landed)");
            Check(resume.Contains("transition.DecideResume(landed, PlayDirectoryBuilder.PagingOf(mediaListState))"),
                "source: the record's own decision drives the resume");
            Check(resume.Contains("await AdvanceSequenceAsync(record.SkipRemainingParts, record.Manual);"),
                "source: the resume walks the move that was recorded, not a new one");
            Check(resume.Contains("ShowSequenceEndToast(record.Manual);"),
                "source: an exhausted list cancels with the existing end message");

            var activate = Body(page, "private async Task ActivateDirectoryRowAsync");
            Check(activate.Contains("if (transition.IsPending) return;") && activate.Contains("transition.DropAdvance();"),
                "source: row activation is ignored while pending and drops the record");
            Check(Body(page, "private async void player_PreviousOuterRequested").Contains("if (transition.IsPending) return;"),
                "source: previous is ignored while pending");
            Check(Body(page, "private void listEpisode_SelectionChanged").Contains("if (transition.IsPending) return;")
                && Body(page, "private async void ListView_SelectionChanged").Contains("if (transition.IsPending) return;")
                && Body(page, "private async void UgcSeasonEpisode_SelectionChanged").Contains("if (transition.IsPending) return;"),
                "source: every other playback activation is gated by the same pending state");

            var initialize = Body(page, "private async Task<bool> InitializeVideo");
            Check(initialize.Contains("transition.DropAdvance();"),
                "source: opening another video drops the interrupted move");
            Check(initialize.Contains("if (_isClosed || _id != id || !transition.IsCurrent(ticket))"),
                "source: a late detail result is dropped by the identity rule");
            Check(initialize.Contains("transition.Commit(videoDetailVM.VideoInfo.aid, MediaListIdText());"),
                "source: playback starts the identity, which clears the wait");
            Check(Body(page, "private void ClosePage()").Contains("transition.Close();"),
                "source: closing the page drops the wait, the record and the identity");
            Check(Body(page, "protected async override void OnNavigatedTo").Contains("transition.Close();"),
                "source: re-entering the page does not inherit the previous identity");

            var build = Body(page, "private PlayDirectoryModel BuildDirectoryModel");
            Check(build.Contains("PlayDirectoryBuilder.ApplyPending(model, transition.PendingKey);"),
                "source: the rebuilt directory carries the pending row");
            Check(Body(page, "private string DirectorySignature()").Contains("transition.PendingKey"),
                "source: a change of pending row redraws the open drawer");

            var keys = Body(page, "private async void CoreWindow_KeyDown");
            var drawerCheck = keys.IndexOf("player.IsDirectoryVisible", StringComparison.Ordinal);
            var handled = keys.IndexOf("args.Handled = true;", StringComparison.Ordinal);
            Check(drawerCheck > 0 && handled > drawerCheck,
                "source: the page yields every key to the drawer before it handles any of them");
        }

        private static void DrawerSourceWiring()
        {
            var control = ReadSource("src/BiliLite.UWP/Controls/PlayDirectoryControl.xaml.cs");
            var xaml = ReadSource("src/BiliLite.UWP/Controls/PlayDirectoryControl.xaml");

            Check(xaml.Contains("TabFocusNavigation=\"Cycle\""),
                "source: Tab order stays inside the open drawer");
            Check(xaml.Contains("AutomationProperties.Name\" Value=\"{Binding AutomationName}\""),
                "source: each row carries the automation name of its own state");
            Check(xaml.Contains("Visibility=\"{Binding IsPending, Converter={StaticResource pendingVisible}}\""),
                "source: the pending row shows a ring");
            var bar = Between(xaml, "Grid.Column=\"0\"", "Grid.Column=\"1\"");
            Check(!bar.Contains("IsPending"), "source: the playing bar is still driven by the current row only");
            Check(xaml.Contains("AutomationProperties.Name=\"关闭播放目录\"")
                && xaml.Contains("AutomationProperties.Name=\"定位正在播放\"")
                && xaml.Contains("AutomationProperties.Name=\"按合集连播\"")
                && xaml.Contains("AutomationProperties.Name=\"按播放列表连播\""),
                "source: the drawer's actions carry accessible names");

            var tabs = Body(control, "private void BuildTabs(PlayDirectoryModel model)");
            Check(tabs.Contains("AutomationProperties.SetName(tab, isContinuation ? title + \" 连播中\" : title);"),
                "source: a tab's accessible name is its title plus the continuation state");
            Check(control.Contains("AutomationProperties.SetAccessibilityView(dot, AccessibilityView.Raw);"),
                "source: the continuation dot is not read twice");
            Check(control.Contains("AutomationProperties.SetName(LoadMoreButton, text);"),
                "source: the footer's accessible name is the label it shows");
            Check(Body(control, "private void Activate(PlayDirectoryRow row)")
                    .Contains("RowActivated"),
                "source: a row activation raises one event instead of playing");
            Check(Body(control, "private void RowList_KeyDown").Contains("VirtualKey.Enter")
                && Body(control, "private void RowList_KeyDown").Contains("VirtualKey.Space"),
                "source: Enter and Space activate the focused row");
        }

        /// <summary>
        /// PlayTargetStart / PlayContinuationPolicy 已经搬到 Models/PlayContinuation.cs,
        /// MediaListResources.cs 里不再有第二份
        /// </summary>
        private static void MovedPolicyWiring()
        {
            var continuation = ReadSource("src/BiliLite.UWP/Models/PlayContinuation.cs");
            var resources = ReadSource("src/BiliLite.UWP/Models/MediaListResources.cs");
            var project = ReadSource("src/BiliLite.UWP/BiliLite.UWP.csproj");

            Check(continuation.Contains("public enum PlayTargetStart")
                && continuation.Contains("public static class PlayContinuationPolicy"),
                "the continuation policy lives in PlayContinuation.cs");
            Check(continuation.Contains("public sealed class PlayTransitionState")
                && continuation.Contains("public sealed class PlayPendingAdvance")
                && continuation.Contains("public struct PlayTransitionTicket"),
                "the in-flight state, the record and its ticket live in the same file");
            Check(!resources.Contains("PlayTargetStart") && !resources.Contains("PlayContinuationPolicy"),
                "MediaListResources.cs keeps only list resources and launcher URLs");
            Check(project.Contains("<Compile Include=\"Models\\PlayContinuation.cs\" />"),
                "the new model is registered in the project file");
        }

        private static List<VideoPlaylistItem> QueueRows(int count)
        {
            var rows = new List<VideoPlaylistItem>();
            for (var i = 1; i <= count; i++)
            {
                rows.Add(new VideoPlaylistItem() { ID = (5000 + i).ToString(), Title = "第" + i + "讲" });
            }
            return rows;
        }

        private static string Between(string source, string from, string to)
        {
            var start = source.IndexOf(from, StringComparison.Ordinal);
            if (start < 0) return "";
            var end = source.IndexOf(to, start, StringComparison.Ordinal);
            return end < 0 ? source.Substring(start) : source.Substring(start, end - start);
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
                if (File.Exists(Path.Combine(dir.FullName, "src", "BiliLite.UWP", "Models", "PlayContinuation.cs")))
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

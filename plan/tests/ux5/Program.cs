using System;
using System.IO;
using BiliLite.Models.Common;

namespace Ux5Probe
{
    /// <summary>
    /// Ux5: 抽屉开关走位移 + 淡入淡出,关门动画结束才隐藏;关门途中重新打开的那一次不能被
    /// 迟到的关门回调藏掉;点抽屉外的空白等于关闭,而且那一下不能落到播放器的手势上。
    ///
    /// Executed against product source: Models/DirectoryDrawerMotion.cs 的状态机、令牌和"动画关掉
    /// 就当场生效"的分支都是真跑的代码。播放器/页面/控件在 UWP 之外跑不起来,它们的接线按文本读,
    /// 每条都叫 "source:"。
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static int Main()
        {
            OpeningShowsTheDrawerAndItsScrim();
            ClosingKeepsTheDrawerUntilItsTokenComesBack();
            ReopeningDuringCloseKeepsTheDrawerVisible();
            TokensAreMonotonicAndSupersededOnesAreRejected();
            AnimationsDisabledSnapsBothWays();
            AnimationsOffDuringCloseLeavesNoPendingCompletion();
            ResetClosesWithoutWaiting();
            DurationsArePositiveAndOrdered();
            PlayerSourceWiring();
            ScrimMarkupWiring();
            ScrimSwallowsTheGesture();
            EscapeAndCloseRequestedShareTheClosePath();
            ButtonFeedbackWiring();
            PageHighlightWiring();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        /// <summary>
        /// 验收场景:打开后抽屉和遮罩都在画面上
        /// </summary>
        private static void OpeningShowsTheDrawerAndItsScrim()
        {
            var motion = new DirectoryDrawerMotion(true);
            Check(!motion.IsShown && !motion.IsScrimVisible && motion.State == DirectoryDrawerState.Closed,
                "a fresh drawer is closed and has no scrim");

            var slide = motion.BeginOpen();
            Check(slide, "with animations on, opening asks for the slide storyboard");
            Check(motion.State == DirectoryDrawerState.Open, "opening lands in the open state");
            Check(motion.IsShown, "an opened drawer counts as shown");
            Check(motion.IsScrimVisible, "an opened drawer brings its scrim up with it");
            Check(!motion.IsClosing, "an opened drawer is not on its way out");
        }

        /// <summary>
        /// 验收场景:关门有动画时,画面要留到完成回调,不是一点就把抽屉抽走
        /// </summary>
        private static void ClosingKeepsTheDrawerUntilItsTokenComesBack()
        {
            var motion = new DirectoryDrawerMotion(true);
            motion.BeginOpen();

            var token = motion.BeginClose();
            Check(token > 0, "a close with animations on hands out a token");
            Check(motion.State == DirectoryDrawerState.Closing, "the drawer is closing, not gone");
            Check(motion.IsShown, "the drawer is still on screen while the close storyboard runs");
            Check(motion.IsScrimVisible, "the scrim stays while the drawer is still on screen");
            Check(motion.CanHide(token), "its own token may hide the drawer");
            Check(motion.CompleteClose(token), "the completion of that token closes the drawer");
            Check(!motion.IsShown && !motion.IsScrimVisible, "a completed close hides the drawer and the scrim");
            Check(!motion.CompleteClose(token), "the same token cannot close a second time");
        }

        /// <summary>
        /// 验收场景:点开 → 点关 → 关门动画没跑完又点开。迟到的关门回调不能把抽屉藏掉
        /// </summary>
        private static void ReopeningDuringCloseKeepsTheDrawerVisible()
        {
            var motion = new DirectoryDrawerMotion(true);
            motion.BeginOpen();
            var stale = motion.BeginClose();
            Check(motion.IsClosing, "the first close is in flight");

            var slide = motion.BeginOpen();
            Check(slide, "reopening asks for the open storyboard again");
            Check(motion.State == DirectoryDrawerState.Open, "reopening wins over the close in flight");
            Check(!motion.CanHide(stale), "the stale close token is rejected");
            Check(!motion.CompleteClose(stale), "the stale completion reports that it changed nothing");
            Check(motion.IsShown && motion.IsScrimVisible,
                "the drawer the user just reopened is still shown after the stale completion");

            var second = motion.BeginClose();
            Check(second > stale, "the next close gets a later token than the stale one");
            Check(!motion.CompleteClose(stale), "the stale token stays dead even once a new close is running");
            Check(motion.CompleteClose(second), "the newest token closes the drawer");
            Check(!motion.IsShown, "the drawer is gone after the newest close completes");
        }

        /// <summary>
        /// 验收场景:令牌只往前走,旧令牌永远认领不了抽屉
        /// </summary>
        private static void TokensAreMonotonicAndSupersededOnesAreRejected()
        {
            var motion = new DirectoryDrawerMotion(true);
            var seen = new int[5];
            for (var i = 0; i < seen.Length; i++)
            {
                motion.BeginOpen();
                seen[i] = motion.BeginClose();
            }
            for (var i = 1; i < seen.Length; i++)
            {
                Check(seen[i] > seen[i - 1], "close token " + i + " is larger than the one before it");
            }
            Check(!motion.CanHide(seen[0]) && !motion.CanHide(seen[seen.Length - 2]),
                "only the newest close token is honoured");
            Check(motion.CompleteClose(seen[seen.Length - 1]), "the newest close token still closes the drawer");
            Check(!motion.CanHide(0) && !motion.CompleteClose(0),
                "token zero is never a close token: the animations-off path cannot be mistaken for one");
            Check(!motion.CanHide(-1), "a negative token is rejected");
        }

        /// <summary>
        /// 验收场景:系统关掉动画时,开关都当场生效,没有故事板也没有要等的回调
        /// </summary>
        private static void AnimationsDisabledSnapsBothWays()
        {
            var motion = new DirectoryDrawerMotion(false);
            Check(!motion.BeginOpen(), "with animations off, opening asks for no storyboard");
            Check(motion.State == DirectoryDrawerState.Open && motion.IsShown,
                "with animations off, an opened drawer is immediately shown");

            var token = motion.BeginClose();
            Check(token == 0, "with animations off, closing hands out no token");
            Check(motion.State == DirectoryDrawerState.Closed, "with animations off, closing is immediate");
            Check(!motion.IsShown && !motion.IsScrimVisible, "with animations off, the drawer and scrim go at once");
            Check(!motion.IsClosing, "with animations off there is nothing to wait for");
        }

        /// <summary>
        /// 验收场景:动画在关门途中被系统关掉,剩下那次关门不留悬空的回调
        /// </summary>
        private static void AnimationsOffDuringCloseLeavesNoPendingCompletion()
        {
            var motion = new DirectoryDrawerMotion(true);
            motion.BeginOpen();
            var token = motion.BeginClose();
            motion.AnimationsEnabled = false;
            var snap = motion.BeginClose();
            Check(snap == 0, "the switched-off close snaps instead of issuing a token");
            Check(!motion.IsShown, "the snapped close hides the drawer at once");
            Check(!motion.CanHide(token), "the token from before the switch can no longer hide anything");
        }

        /// <summary>
        /// 验收场景:目录不可用时页面把抽屉收走,不用等动画
        /// </summary>
        private static void ResetClosesWithoutWaiting()
        {
            var motion = new DirectoryDrawerMotion(true);
            motion.BeginOpen();
            motion.BeginClose();
            motion.Reset();
            Check(!motion.IsShown && motion.State == DirectoryDrawerState.Closed,
                "resetting shuts the drawer without a storyboard");
            Check(!motion.IsClosing, "resetting leaves nothing waiting for a completion");
        }

        /// <summary>
        /// 验收场景:时长常量是正的,收起不慢于打开,遮罩不慢于抽屉
        /// </summary>
        private static void DurationsArePositiveAndOrdered()
        {
            Check(DirectoryDrawerMotion.OpenDurationMilliseconds > 0
                && DirectoryDrawerMotion.CloseDurationMilliseconds > 0
                && DirectoryDrawerMotion.ScrimFadeMilliseconds > 0,
                "every storyboard duration is positive");
            Check(DirectoryDrawerMotion.OpenDurationMilliseconds <= 400
                && DirectoryDrawerMotion.CloseDurationMilliseconds <= 400,
                "open and close stay short enough to read as a slide, not a scene change");
            Check(DirectoryDrawerMotion.CloseDurationMilliseconds <= DirectoryDrawerMotion.OpenDurationMilliseconds,
                "closing is not slower than opening");
            Check(DirectoryDrawerMotion.ScrimFadeMilliseconds <= DirectoryDrawerMotion.OpenDurationMilliseconds,
                "the scrim does not outlive the drawer sliding in");
            Check(DirectoryDrawerMotion.ButtonPressMilliseconds > 0
                && DirectoryDrawerMotion.ButtonPressMilliseconds <= DirectoryDrawerMotion.OpenDurationMilliseconds,
                "the button press flash fits inside the open animation");
            Check(DirectoryDrawerMotion.HighlightMilliseconds > 0,
                "the detail column highlight has a duration");
            Check(Math.Abs(DirectoryDrawerMotion.DrawerWidth - 380) < 0.01,
                "the slide distance is the drawer's own width");
        }

        private static void PlayerSourceWiring()
        {
            var player = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml.cs");

            var show = Body(player, "public void ShowDirectory(");
            Check(show.Contains("_directoryMotion.BeginOpen()"),
                "source: opening goes through the motion state machine");
            Check(show.Contains("DirectoryHost.Visibility = Visibility.Visible;")
                && show.Contains("DirectoryScrim.Visibility = Visibility.Visible;"),
                "source: opening shows the drawer and the scrim");
            Check(show.Contains("if (wasOpen && !wasClosing) return;"),
                "source: a drawer that is already open only swaps content, it does not slide again");
            Check(show.IndexOf("if (wasOpen && !wasClosing) return;", StringComparison.Ordinal)
                    < show.IndexOf("if (slide) StartDirectoryStoryboard(true);", StringComparison.Ordinal),
                "source: the already-open path returns before the open storyboard starts");
            Check(show.Contains("if (slide) StartDirectoryStoryboard(true);")
                && show.Contains("else SnapDirectoryOpen();"),
                "source: opening either animates or snaps, never both");

            var hide = Body(player, "public void HideDirectory(");
            Check(hide.Contains("_directoryCloseToken = _directoryMotion.BeginClose();"),
                "source: closing asks the model for a token");
            Check(hide.Contains("if (_directoryMotion.IsClosing)")
                && hide.Contains("StartDirectoryStoryboard(false);")
                && hide.Contains("return;"),
                "source: closing with animations waits for the storyboard and hides later");
            Check(hide.Contains("FinishHideDirectory();"),
                "source: closing with animations off hides at once");

            var completed = Body(player, "private void DirectoryCloseStoryboard_Completed");
            Check(completed.Contains("_directoryMotion.CompleteClose(_directoryCloseToken)")
                && completed.Contains("FinishHideDirectory();"),
                "source: the close completion hides only when the model accepts the token");
            Check(!Body(player, "private void DirectoryOpenStoryboard_Completed").Contains("FinishHideDirectory"),
                "source: the open completion never hides anything");

            Check(Body(player, "public bool IsDirectoryVisible").Contains("_directoryMotion.IsShown"),
                "source: IsDirectoryVisible reports the model's shown state");
            Check(!player.Contains("_directoryOpen"),
                "source: the old plain flag is gone, the model owns the state");
            Check(Body(player, "public void UpdateDirectoryInfo").Contains("if (!available) HideDirectory();"),
                "source: a drawer that lost its directory still goes through the close path");

            var storyboard = Body(player, "private void StartDirectoryStoryboard(");
            Check(storyboard.Contains("DirectoryDrawerMotion.OpenDurationMilliseconds")
                && storyboard.Contains("DirectoryDrawerMotion.CloseDurationMilliseconds")
                && storyboard.Contains("DirectoryDrawerMotion.ScrimFadeMilliseconds"),
                "source: the storyboard durations come from the model, not from magic numbers");
            Check(storyboard.Contains("Storyboard.SetTarget(slide, DirectoryHostTranslate);")
                && storyboard.Contains("Storyboard.SetTarget(fade, DirectoryHost);")
                && storyboard.Contains("Storyboard.SetTarget(scrimFade, DirectoryScrim);"),
                "source: the storyboard slides the drawer and fades the drawer and the scrim");
            Check(storyboard.Contains("From = open ? DirectoryDrawerMotion.DrawerWidth : 0")
                && storyboard.Contains("To = open ? 0 : DirectoryDrawerMotion.DrawerWidth"),
                "source: the drawer slides in from its own width and back out to it");
            Check(storyboard.Contains("StopDirectoryStoryboard();"),
                "source: a new storyboard stops the one in flight, so its completion cannot fire");
        }

        private static void ScrimMarkupWiring()
        {
            var xaml = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml");
            var player = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml.cs");
            var scrim = xaml.IndexOf("<Grid x:Name=\"DirectoryScrim\"", StringComparison.Ordinal);
            var host = xaml.IndexOf("<Grid x:Name=\"DirectoryHost\"", StringComparison.Ordinal);
            Check(scrim > 0 && host > 0, "the scrim and the drawer are both in the player markup");
            Check(scrim < host, "source: the scrim is declared before the drawer, so the drawer draws on top");

            var scrimTag = Tag(xaml, "<Grid x:Name=\"DirectoryScrim\"");
            Check(scrimTag.Contains("Tapped=\"DirectoryScrim_Tapped\""),
                "source: the scrim closes the drawer on tap");
            Check(scrimTag.Contains("DoubleTapped=\"DirectoryScrim_DoubleTapped\""),
                "source: the scrim takes the double tap instead of the player");
            Check(scrimTag.Contains("PointerPressed=\"DirectoryScrim_PointerPressed\"")
                && scrimTag.Contains("PointerReleased=\"DirectoryScrim_PointerReleased\""),
                "source: the scrim takes the press and release the player would otherwise see");
            Check(scrimTag.Contains("Visibility=\"Collapsed\""),
                "source: the scrim starts out of the way");
            Check(scrimTag.Contains("Background="),
                "source: the scrim has a background, otherwise nothing hits it");
            Check(scrimTag.Contains("Opacity=\"0\""),
                "source: the scrim starts transparent and is faded in by the open storyboard");

            var hostTag = Tag(xaml, "<Grid x:Name=\"DirectoryHost\"");
            Check(hostTag.Contains("Opacity=\"0\""), "source: the drawer starts transparent");
            Check(xaml.Contains("<CompositeTransform x:Name=\"DirectoryHostTranslate\" TranslateX=\"380\"/>"),
                "source: the drawer starts translated out by its own width");
            Check(hostTag.Contains("Width=\"380\""),
                "source: the drawer width matches the slide distance the model reports");

            Check(CountOccurrences(player, "DirectoryScrim.Visibility = Visibility.Collapsed;") == 1,
                "source: the scrim is hidden in exactly one place, the shared teardown");
            Check(Body(player, "private void FinishHideDirectory").Contains("DirectoryScrim.Visibility = Visibility.Collapsed;"),
                "source: the scrim goes away with the drawer, never before it");
            Check(CountOccurrences(player, "DirectoryScrim.Visibility = Visibility.Visible;") == 1,
                "source: the scrim comes up in exactly one place, the open path");
        }

        private static int CountOccurrences(string source, string token)
        {
            var count = 0;
            var index = 0;
            while (true)
            {
                index = source.IndexOf(token, index, StringComparison.Ordinal);
                if (index < 0) return count;
                count++;
                index += token.Length;
            }
        }

        private static void ScrimSwallowsTheGesture()
        {
            var player = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml.cs");

            var tapped = Body(player, "private void DirectoryScrim_Tapped");
            Check(tapped.Contains("e.Handled = true;"),
                "source: the scrim tap is marked handled before anything else");
            Check(tapped.Contains("HideDirectory();"),
                "source: the scrim tap closes the drawer through the same close path");
            Check(tapped.IndexOf("e.Handled = true;", StringComparison.Ordinal)
                    < tapped.IndexOf("HideDirectory();", StringComparison.Ordinal),
                "source: the gesture is swallowed first, then the drawer closes");

            Check(Body(player, "private void DirectoryScrim_DoubleTapped").Contains("e.Handled = true;"),
                "source: a double tap on the scrim never reaches the fullscreen toggle");
            Check(Body(player, "private void DirectoryScrim_PointerPressed").Contains("e.Handled = true;"),
                "source: a press on the scrim never reaches the play/pause handling");
            Check(Body(player, "private void DirectoryScrim_PointerReleased").Contains("e.Handled = true;"),
                "source: a release on the scrim never reaches the play/pause handling");
        }

        private static void EscapeAndCloseRequestedShareTheClosePath()
        {
            var player = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml.cs");
            var keyDown = Body(player, "private async void PlayerControl_KeyDown");
            Check(keyDown.Contains("if (_directoryMotion.IsShown)"),
                "source: the escape handling is gated on the model, not on a parallel flag");
            Check(keyDown.Contains("else HideDirectory();"),
                "source: escape closes the drawer with the same HideDirectory the drawer button uses");
            Check(Body(player, "private void DirectoryDrawer_CloseRequested").Contains("HideDirectory();"),
                "source: the drawer's own close button uses the same HideDirectory");
            Check(Body(player, "private void DirectoryScrim_Tapped").Contains("HideDirectory();"),
                "source: the scrim uses the same HideDirectory");
        }

        private static void ButtonFeedbackWiring()
        {
            var xaml = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml");
            var player = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml.cs");

            Check(xaml.Contains("<CompositeTransform x:Name=\"BottomBtnDirectoryPress\"/>"),
                "source: the directory button carries its own press transform");
            Check(xaml.Contains("x:Key=\"DirectoryButtonActiveBrush\"") && xaml.Contains("x:Key=\"DirectoryButtonIdleBrush\""),
                "source: the active and idle button backgrounds exist as resources");

            var click = Body(player, "private void BottomBtnDirectory_Click");
            Check(click.Contains("PlayDirectoryButtonPress();"),
                "source: every directory click flashes the button");
            Check(click.IndexOf("PlayDirectoryButtonPress();", StringComparison.Ordinal)
                    < click.IndexOf("DirectoryRequested?.Invoke", StringComparison.Ordinal),
                "source: the flash happens before the decision, so no window mode can skip it");

            var press = Body(player, "private void PlayDirectoryButtonPress");
            Check(press.Contains("DirectoryDrawerMotion.ButtonPressMilliseconds"),
                "source: the press flash takes its duration from the model");
            Check(press.Contains("\"Opacity\""), "source: the press flash also dims, visible on a dark video");
            Check(ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml.cs")
                    .Contains("SetDirectoryButtonActive(true);"),
                "source: opening the drawer lights the button up");

            var finish = Body(player, "private void FinishHideDirectory");
            Check(finish.Contains("SetDirectoryButtonActive(false);"),
                "source: hiding the drawer drops the active background");
        }

        private static void PageHighlightWiring()
        {
            var page = ReadSource("src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs");

            var select = Body(page, "private bool SelectDetailDirectory()");
            Check(select.Contains("var before = pivot == null ? null : pivot.SelectedItem;"),
                "source: the page remembers which detail tab was selected before the switch");
            Check(select.Contains("if (!SelectDetailTarget()) return false;"),
                "source: the existing target selection still decides whether a detail column was found");
            Check(select.Contains("ReferenceEquals(before, pivot.SelectedItem)")
                && select.Contains("HighlightDetailColumn();"),
                "source: an unchanged tab produces the visible highlight instead of a silent success");

            var highlight = Body(page, "private void HighlightDetailColumn()");
            Check(highlight.Contains("IsHitTestVisible = false"),
                "source: the highlight does not eat clicks meant for the detail column");
            Check(highlight.Contains("DirectoryDrawerMotion.HighlightMilliseconds"),
                "source: the highlight duration comes from the model");
            Check(highlight.Contains("Utils.GetBrush(\"HighLightColor\")"),
                "source: the highlight uses the theme's highlight colour");

            Check(!Body(page, "private void player_DirectoryRequested").Contains("HighlightDetailColumn"),
                "source: the page still decides on the drawer path, the highlight stays inside the detail path");
        }

        private static string Tag(string source, string marker)
        {
            var start = source.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return "";
            var body = source.Substring(start, Math.Min(600, source.Length - start));
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
                if (File.Exists(Path.Combine(dir.FullName, "src", "BiliLite.UWP", "Models", "DirectoryDrawerMotion.cs")))
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

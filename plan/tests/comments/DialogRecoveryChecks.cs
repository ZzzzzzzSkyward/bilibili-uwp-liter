using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BiliLite.Dialogs;
using BiliLite.Helpers;
using BiliLite.Modules;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CommentsProbe
{
    // Runs full real dialog + real builder/parser/DTO + exactly extracted HttpResults.
    // Fakes deliver tasks and expose platform properties/events; no lifecycle policy is faked.
    internal static class DialogRecoveryChecks
    {
        private const string Draft = "  中文 & + = % 😀\nsecond line  ";
        private static int _checks, _failures;
        private static Pump _pump;

        private sealed class Pump : SynchronizationContext
        {
            private readonly Queue<Action> _queue = new Queue<Action>();
            public readonly List<Exception> Unhandled = new List<Exception>();
            public int Active;
            public override void OperationStarted() { Active++; }
            public override void OperationCompleted() { Active--; }
            public override void Post(SendOrPostCallback callback, object state) { _queue.Enqueue(() => callback(state)); }
            public void Drain()
            {
                while (_queue.Count > 0)
                {
                    try { _queue.Dequeue()(); }
                    catch (Exception ex) { Unhandled.Add(ex); }
                }
            }
        }

        public static int Run()
        {
            var previous = SynchronizationContext.Current;
            _pump = new Pump();
            SynchronizationContext.SetSynchronizationContext(_pump);
            try
            {
                BlankInput();
                ResponseTruthAndRawBodies();
                DelayedRejectionThenRetry();
                ExceptionsRestoreRetry();
                AbandonmentAndIsolation();
                FaceFaultIsContained();
                Check(_pump.Active == 0 && _pump.Unhandled.Count == 0, "every async event completes without escaped exceptions");
                Console.WriteLine("DIALOG_CHECKS=" + _checks + " FAILURES=" + _failures + "; actual dialog/builder/parser/HttpResults; controlled UI/delivery only");
                return _failures == 0 ? _checks : -1;
            }
            finally
            {
                Utils.Send = null;
                Utils.Requests.Clear();
                Utils.Toasts.Clear();
                LogHelper.Entries.Clear();
                EmoteVM.Delivery = () => Task.CompletedTask;
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        private static void Check(bool condition, string label)
        {
            _checks++;
            if (!condition) { _failures++; Console.Error.WriteLine("FAIL dialog: " + label); }
        }

        private static SendCommentDialog New(string draft = Draft, bool reset = true)
        {
            if (reset) { Utils.Requests.Clear(); Utils.Toasts.Clear(); LogHelper.Entries.Clear(); }
            var dialog = new SendCommentDialog("9007199254740993123", BiliLite.Api.CommentApi.CommentType.Dynamic);
            dialog.txt_Comment.Text = draft;
            return dialog;
        }

        private static object Invoke(SendCommentDialog dialog, string method, params object[] arguments)
        {
            return typeof(SendCommentDialog).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(dialog, arguments);
        }

        private static ContentDialogButtonClickEventArgs Primary(SendCommentDialog dialog)
        {
            var args = new ContentDialogButtonClickEventArgs();
            Invoke(dialog, "ContentDialog_PrimaryButtonClick", dialog, args);
            _pump.Drain();
            return args;
        }

        private static ContentDialogButtonClickEventArgs Cancel(SendCommentDialog dialog)
        {
            var args = new ContentDialogButtonClickEventArgs();
            Invoke(dialog, "ContentDialog_SecondaryButtonClick", dialog, args);
            _pump.Drain();
            return args;
        }

        private static HttpResults Response(string body, bool status = true)
        {
            return new HttpResults { status = status, code = status ? 200 : 503, results = body, message = "controlled transport" };
        }

        private static bool Ready(SendCommentDialog d)
        {
            return d.IsPrimaryButtonEnabled && !d.txt_Comment.IsReadOnly && d.btnOpenFace.IsEnabled
                && d.SendProgress.Visibility == Visibility.Collapsed;
        }

        private static string Snapshot(SendCommentDialog d)
        {
            return d.txt_Comment.Text + "|" + d.IsPrimaryButtonEnabled + "|" + d.txt_Comment.IsReadOnly + "|"
                + d.btnOpenFace.IsEnabled + "|" + d.SendProgress.Visibility + "|" + d.SendStatus.Text + "|" + d.HideCount;
        }

        private static void BlankInput()
        {
            foreach (var text in new[] { null, "", " ", "\r\n\t" })
            {
                Utils.Send = _ => throw new InvalidOperationException("blank must not dispatch");
                var d = New(text);
                var args = Primary(d);
                Check(args.Cancel && Utils.Requests.Count == 0 && d.HideCount == 0, "blank cancels default close without dispatch");
                Check(Ready(d) && d.txt_Comment.Text == text && !string.IsNullOrEmpty(d.SendStatus.Text), "blank retains draft/editable send and inline validation");
            }
        }

        private static void ResponseTruthAndRawBodies()
        {
            var cases = new List<(string body, bool transport, bool success)>
            {
                ("{\"code\":0}", true, true), ("{\"code\":0,\"data\":null}", true, true), ("{\"code\":0,\"data\":{}}", true, true),
                ("{\"code\":-400,\"message\":\"controlled rejection\"}", true, false), ("{\"code\":-400}", true, false),
                ("{\"code\":0}", false, false),
                ("<html>error</html>", true, false), ("{\"code\":", true, false),
                ("<html>{\"code\":0}</html>", true, true), ("prefix {\"code\":0} suffix", true, true),
                ("{\"bad\":} prefix {\"code\":0}", true, true), ("{\"bad\": {\"code\":0}", true, false),
                ("[{\"code\":0}]", true, false),
                ("{\"code\":0}{\"code\":-400,\"message\":\"long rejection\"}", true, false),
                ("{\"code\":-400,\"message\":\"long rejection\"}{\"code\":0}", true, false),
                ("{\"code\":0}{\"code\":1}", true, true), ("{\"code\":1}{\"code\":0}", true, false),
                ("<html>{}</html>", true, false), ("prefix {\"code\":\"0\"}", true, false),
                ("<html>{\"code\":0}</html>", false, false), ("{}", true, false),
            };
            foreach (var literal in new[] { "null", "\"0\"", "false", "true", "0.0", "{}", "[]",
                "2147483648", "-2147483649", "9223372036854775808", "-9223372036854775809", "2147483647", "-2147483648" })
                cases.Add(("{\"code\":" + literal + "}", true, false));
            foreach (var fixture in cases)
            {
                var response = Response(fixture.body, fixture.transport);
                Utils.Send = _ => Task.FromResult(response);
                var d = New();
                var args = Primary(d);
                Check(args.Cancel, "default close cancelled for response " + fixture.body);
                Check(Utils.Requests.Count == 1 && d.txt_Comment.Text == Draft, "one captured attempt/exact draft for " + fixture.body);
                Check(fixture.success ? d.HideCount == 1 && Utils.Toasts.SequenceEqual(new[] { "发表评论成功" })
                    : d.HideCount == 0 && Utils.Toasts.Count == 0 && Ready(d) && !string.IsNullOrEmpty(d.SendStatus.Text),
                    "typed dialog outcome with actual raw decoder " + fixture.body + " transport=" + fixture.transport);
                if (fixture.success)
                {
                    Primary(d); d.RaiseClosing(); d.RaiseClosed(); d.RaiseClosed();
                    Check(Utils.Requests.Count == 1 && d.HideCount == 1 && Utils.Toasts.Count == 1,
                        "accepted success terminal before later callback/close signals");
                }
            }
            Utils.Send = _ => Task.FromResult<HttpResults>(null);
            var nullDialog = New(); Primary(nullDialog);
            Check(Ready(nullDialog) && nullDialog.HideCount == 0 && Utils.Toasts.Count == 0
                && nullDialog.SendStatus.Text.Contains("未确认"), "null transport response retains draft and shows uncertainty");
        }

        private static void DelayedRejectionThenRetry()
        {
            var delivery = new TaskCompletionSource<HttpResults>();
            Utils.Send = _ => delivery.Task;
            EmoteVM.Delivery = () => Task.CompletedTask;
            var d = New();
            Invoke(d, "btnOpenFace_Click", d.btnOpenFace, new RoutedEventArgs()); _pump.Drain();
            Check(d.FaceFlyout.ShowCount == 1, "ready face opener works");
            var args = Primary(d);
            Check(args.Cancel && !d.IsPrimaryButtonEnabled && d.txt_Comment.IsReadOnly && !d.btnOpenFace.IsEnabled
                && d.SendProgress.Visibility == Visibility.Visible && d.SendStatus.Text == "正在发送", "pending request has perceivable protected busy state before await");
            var owned = typeof(SendCommentDialog).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => typeof(Task).IsAssignableFrom(x.FieldType)).Select(x => x.GetValue(d) as Task).ToList();
            Check(owned.Count == 1 && owned[0] != null && !owned[0].IsCompleted, "pending send Task has an explicit owner");
            Primary(d);
            Invoke(d, "btnOpenFace_Click", d.btnOpenFace, new RoutedEventArgs());
            Invoke(d, "GridView_ItemClick", null, new ItemClickEventArgs { ClickedItem = new EmotePackageItemModel { text = "[late-face]" } });
            _pump.Drain();
            Check(Utils.Requests.Count == 1 && d.txt_Comment.Text == Draft && d.FaceFlyout.ShowCount == 1,
                "forced repeat and already-open face callback cannot dispatch/change draft");
            var fields = Utils.Requests[0].body.Split('&').Where(x => x.Length != 0).Select(x => x.Split(new[] { '=' }, 2)).ToDictionary(x => x[0], x => Uri.UnescapeDataString(x[1]));
            Check(fields["message"] == Draft && fields["oid"] == "9007199254740993123" && fields["type"] == "17", "exact raw text/opaque target/type snapshot reaches real builder");
            delivery.SetResult(Response("{\"code\":-400,\"message\":\"controlled rejection\"}")); _pump.Drain();
            Check(Ready(d) && d.txt_Comment.Text == Draft && d.SendStatus.Text == "controlled rejection"
                && d.txt_Comment.FocusCount > 0 && d.HideCount == 0 && Utils.Toasts.Count == 0, "rejection retains draft and restores editable focused retry once");
            Check(owned.Count == 1 && owned[0].IsCompletedSuccessfully, "owned rejection Task is completed and observed");
            Utils.Send = _ => Task.FromResult(Response("{\"code\":0}"));
            Primary(d);
            Check(Utils.Requests.Count == 2 && d.HideCount == 1 && Utils.Toasts.Count == 1, "explicit retry success closes/notices once");
        }

        private static void ExceptionsRestoreRetry()
        {
            Utils.Send = _ => throw new InvalidOperationException("dummy sensitive transport detail");
            var d = New(); Primary(d);
            Check(Ready(d) && d.txt_Comment.Text == Draft && d.HideCount == 0 && Utils.Toasts.Count == 0
                && d.SendStatus.Text.Contains("未确认"), "synchronous transport exception is contained with uncertain result");
            var pending = new TaskCompletionSource<HttpResults>(); Utils.Send = _ => pending.Task;
            d = New(); Primary(d); pending.SetException(new InvalidOperationException("dummy sensitive delayed detail")); _pump.Drain();
            Check(Ready(d) && d.txt_Comment.Text == Draft && d.HideCount == 0 && Utils.Toasts.Count == 0
                && d.SendStatus.Text.Contains("未确认"), "delayed fault restores explicit retry without automatic retransmission");
            Check(!d.SendStatus.Text.Contains("sensitive") && LogHelper.Entries.All(x => !x.Contains("sensitive") && !x.Contains(Draft)),
                "new status/log messages do not disclose draft or transport exception payload");
        }

        private static void Complete(TaskCompletionSource<HttpResults> pending, string outcome)
        {
            if (outcome == "fault") pending.SetException(new InvalidOperationException("abandoned failure"));
            else pending.SetResult(Response(outcome == "success" ? "{\"code\":0}" : "{\"code\":-400}"));
            _pump.Drain();
        }

        private static void AbandonmentAndIsolation()
        {
            foreach (var mode in new[] { "secondary", "Closing", "Closed-fallback" })
            foreach (var outcome in new[] { "success", "rejection", "fault" })
            {
                var pending = new TaskCompletionSource<HttpResults>(); Utils.Send = _ => pending.Task;
                var d = New(); Primary(d);
                if (mode == "secondary") Check(Cancel(d).Cancel && d.HideCount == 1, "secondary retires before a single explicit Hide");
                else if (mode == "Closing") d.RaiseClosing();
                else d.RaiseClosed();
                var snapshot = Snapshot(d);
                Primary(d);
                Complete(pending, outcome);
                Check(Snapshot(d) == snapshot && Utils.Toasts.Count == 0 && Utils.Requests.Count == 1,
                    mode + " then " + outcome + " before Closed cannot publish/restore/dispatch/Hide");
                d.RaiseClosed(); d.RaiseClosed();
                Check(Snapshot(d) == snapshot && Utils.Toasts.Count == 0, mode + " repeated Closed cleanup is harmless");
            }
            var aPending = new TaskCompletionSource<HttpResults>(); Utils.Send = _ => aPending.Task;
            var a = New(); Primary(a); a.RaiseClosing();
            var bPending = new TaskCompletionSource<HttpResults>(); Utils.Send = _ => bPending.Task;
            var b = New("fresh B", false); Primary(b); var bSnapshot = Snapshot(b);
            Complete(aPending, "success");
            Check(Snapshot(b) == bSnapshot && Utils.Toasts.Count == 0 && a.HideCount == 0 && Utils.Requests.Count == 2,
                "A completion during Closing interval leaves fresh B state and global notices isolated");
            Complete(bPending, "rejection"); a.RaiseClosed(); b.RaiseClosing(); b.RaiseClosed();
        }

        private static void FaceFaultIsContained()
        {
            EmoteVM.Delivery = () => throw new InvalidOperationException("dummy emote failure");
            var d = New();
            Invoke(d, "btnOpenFace_Click", d.btnOpenFace, new RoutedEventArgs()); _pump.Drain();
            Check(_pump.Unhandled.Count == 0, "async face event contains service exception");
            EmoteVM.Delivery = () => Task.CompletedTask;
        }
    }
}

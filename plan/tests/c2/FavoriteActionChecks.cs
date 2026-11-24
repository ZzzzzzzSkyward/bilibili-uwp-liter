using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BiliLite.Helpers;
using BiliLite.Modules;

namespace C2Probe
{
    // Only delivery and platform scheduling are substituted; the full VM, API, decoder and parser run.
    internal static class FavoriteActionChecks
    {
        private static int passed;
        private static readonly List<string> failures = new List<string>();
        private static readonly string[] Actions = { "delete", "clean", "collect", "cancel", "sort" };

        public static async Task Run()
        {
            var malformed = new[] { "{}", "{\"data\":{},\"message\":\"not a success envelope\"}",
                "{\"code\":0.4}", "{\"code\":\"0\"}", "{\"code\":null}", "{\"code\":true}",
                "{\"code\":2147483648}", "{\"code\":-2147483649}",
                "{\"code\":9223372036854775808}", "{\"code\":-9223372036854775809}", "not JSON", "null" };
            foreach (var action in Actions)
            {
                foreach (var body in malformed)
                    await Exercise(action, Reply(body), false, "操作失败", action + " malformed " + body);
                await Exercise(action, null, false, "没有响应", action + " null response");
                await Exercise(action, new HttpResults { status = false, code = 503, message = "fixture offline" }, false,
                    "fixture offline", action + " failed HTTP");
                await Exercise(action, Reply("{\"code\":0}"), false, "出现了一个未处理错误，已记录", action + " request throws", true);
                await Exercise(action, Reply("{\"code\":-403,\"message\":\"fixture denied\"}"), false,
                    "fixture denied", action + " business message");
                await Exercise(action, Reply("{\"code\":-403,\"msg\":\"fixture msg\"}"), false,
                    "fixture msg", action + " business msg");
                await Exercise(action, Reply("{\"code\":0}"), true, null, action + " code-only success");
                await Exercise(action, Reply("prefix {\"code\":0} suffix"), true, null, action + " production salvage success");
            }
            Console.WriteLine("Favorite action checks: passed=" + passed + " failed=" + failures.Count);
            foreach (var failure in failures) Console.Error.WriteLine("FAIL favorite action: " + failure);
            if (failures.Count != 0) throw new InvalidOperationException("Favorite action boundary failures=" + failures.Count);
            FakeTransport.Reset();
        }

        private static HttpResults Reply(string body) => new HttpResults { status = true, code = 200, results = body };
        private static void Check(bool condition, string label)
        {
            if (condition) passed++;
            else failures.Add(label);
        }

        private static async Task Exercise(string action, HttpResults response, bool accepted, string message, string label, bool throws = false)
        {
            FakeTransport.Reset();
            var row = new FavoriteInfoVideoItemModel { id = "9007199254740993" };
            var neighbor = new FavoriteInfoVideoItemModel { id = "9007199254740995" };
            var originalRows = new ObservableCollection<FavoriteInfoVideoItemModel> { row, neighbor };
            var info = new FavoriteInfoModel { id = "folder-A", mid = "other-owner", media_count = 2 };
            var vm = new FavoriteDetailVM { Id = "folder-A", FavoriteInfo = info, Videos = originalRows,
                ShowCollect = action != "cancel", ShowCancelCollect = action == "cancel", Page = 3 };
            var expectedCollect = vm.ShowCollect;
            var expectedCancel = vm.ShowCancelCollect;
            var removals = 0;
            originalRows.CollectionChanged += (_, e) => { if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove) removals++; };
            var collectChanges = 0;
            vm.PropertyChanged += (_, e) => { if (e.PropertyName == "ShowCollect") collectChanges++; };
            var deliveries = 0;
            FakeTransport.Delivery = async request =>
            {
                await Task.Yield();
                deliveries++;
                if (deliveries > 1)
                {
                    return Reply("{\"code\":0,\"data\":{\"info\":{\"id\":\"folder-A\",\"mid\":\"other-owner\",\"media_count\":1},\"medias\":[{\"id\":\"reloaded\"}]}}");
                }
                if (throws) throw new InvalidOperationException("fixture request exception");
                return response;
            };
            var tracker = new ActionCompletion();
            tracker.Start(() =>
            {
                switch (action)
                {
                    case "delete": RunTask(() => vm.Delete(row)); break;
                    case "clean": RunTask(vm.Clean); break;
                    case "collect": vm.DoCollect(); break;
                    case "cancel": vm.DoCancelCollect(); break;
                    case "sort": RunTask(() => vm.Sort(row.id, neighbor.id)); break;
                }
            });
            await tracker.Done.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Check(tracker.Errors.Count == 0, label + " no exception escapes");
            Check(deliveries == (accepted && action == "clean" ? 2 : 1), label + " exact action/refresh request count");
            Check(FakeTransport.ActualRequests[0].baseUrl.Contains("/"), label + " real request builder executes");
            if (!accepted)
            {
                Check(ReferenceEquals(vm.Videos, originalRows) && vm.Videos.SequenceEqual(new[] { row, neighbor })
                    && ReferenceEquals(vm.FavoriteInfo, info) && vm.Page == 3, label + " rows/info/page retained");
                Check(vm.ShowCollect == expectedCollect && vm.ShowCancelCollect == expectedCancel && collectChanges == 0,
                    label + " flags retained without publication");
                Check(!FakeTransport.Toasts.Contains("排序成功") && FakeTransport.Toasts.Count == 1
                    && FakeTransport.Toasts[0] == (action == "sort" ? "排序失败" : "") + message,
                    label + " caller failure text");
                if (throws) Check(FakeTransport.Logs.Count == 1, label + " exception logged through real IModules");
            }
            else
            {
                if (action == "delete") Check(vm.Videos.SequenceEqual(new[] { neighbor }) && removals == 1, label + " removes requested row exactly once");
                if (action == "clean") Check(vm.Page == 1 && vm.Videos.Count == 1 && vm.Videos[0].id == "reloaded", label + " refresh publishes once");
                if (action == "collect" || action == "cancel") Check(vm.ShowCollect == (action == "cancel")
                    && vm.ShowCancelCollect == (action == "collect") && collectChanges == 1, label + " flags publish exactly once");
                Check(FakeTransport.Toasts.SequenceEqual(action == "sort" ? new[] { "排序成功" } : Array.Empty<string>()), label + " ordinary success feedback");
            }
        }
        private static async void RunTask(Func<Task> work) { await work(); }

        private sealed class ActionCompletion : SynchronizationContext
        {
            private int active;
            private int pending;
            public readonly TaskCompletionSource<bool> Done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public readonly List<Exception> Errors = new List<Exception>();
            public override void OperationStarted() => Interlocked.Increment(ref active);
            private void CompleteIfIdle() { if (Volatile.Read(ref active) == 0 && Volatile.Read(ref pending) == 0) Done.TrySetResult(true); }
            public override void OperationCompleted() { Interlocked.Decrement(ref active); CompleteIfIdle(); }
            public override void Post(SendOrPostCallback callback, object state)
            {
                Interlocked.Increment(ref pending);
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    var old = Current;
                    SetSynchronizationContext(this);
                    try { callback(state); }
                    catch (Exception ex) { lock (Errors) Errors.Add(ex); }
                    finally { SetSynchronizationContext(old); Interlocked.Decrement(ref pending); CompleteIfIdle(); }
                });
            }
            public void Start(Action work)
            {
                var old = Current;
                SetSynchronizationContext(this);
                OperationStarted();
                try { work(); }
                finally { OperationCompleted(); SetSynchronizationContext(old); }
            }
        }
    }
}

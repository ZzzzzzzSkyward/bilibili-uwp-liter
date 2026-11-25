using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Modules.Season;

namespace Season1Probe
{
    // Complete production VM, IModules and RelayCommand; injected read transport only.
    // api02 separately owns the actual URL/query builder. No review writes/native events execute.
    public static class SeasonReviewOwnershipProbe
    {
        private static int passed, failed, calls;
        private static string requestedNext;
        private static Func<Task<HttpResults>> response;
        private static TaskCompletionSource<HttpResults> Held() => new TaskCompletionSource<HttpResults>();
        private static HttpResults Reply(string body) => new HttpResults { status = true, results = body };
        private static HttpResults Success(int total = 3, string next = "opaque+/=&cursor") => Reply("{\"code\":0,\"data\":{\"total\":" + total + ",\"next\":\"" + next + "\",\"list\":[{\"review_id\":7,\"content\":\"a &amp; b\"}]}}");
        private static SeasonReviewVM New()
        {
            calls = 0;
            requestedNext = null;
            Utils.Toasts.Clear();
            LogHelper.Entries.Clear();
            SeasonApi.ReviewFixture = (id, next) => { calls++; requestedNext = next; return response(); };
            return new SeasonReviewVM { MediaID = 42 };
        }
        private static void Check(bool value, string label)
        {
            if (value) { passed++; Console.WriteLine("ok review-owner " + label); }
            else { failed++; Console.Error.WriteLine("FAIL review-owner " + label); }
        }
        private static async Task Finish(Task task)
        {
            if (await Task.WhenAny(task, Task.Delay(2000)) != task) throw new Exception("bounded review task timeout");
            await task;
        }
        private static string State(SeasonReviewVM vm, int notifications) => calls + "|" + string.Join(",", vm.Items.Select(x => x.review_id)) + "|" + vm.Next + "|" + vm.CanLoadMore + "|" + vm.Loading + "|" + notifications;
        private static void Invoke(SeasonReviewVM vm, int entry)
        {
            if (entry == 0) Check(vm.GetItems().IsCompleted, "rejected public task completes synchronously");
            if (entry == 1) vm.RefreshCommand.Execute(null);
            if (entry == 2) vm.LoadMoreCommand.Execute(null);
        }
        public static async Task<bool> Run()
        {
            await PivotAndPublic();
            await ZeroId();
            await Notifications();
            await RefreshCollection();
            await Failures();
            await CompletionThrow();
            await Legacy();
            Console.WriteLine("REVIEW_OWNER_SUMMARY passed=" + passed + " failures=" + failed + " boundary=managed-real-vm/injected-read-transport");
            return failed == 0;
        }
        private static async Task PivotAndPublic()
        {
            var held = Held(); response = () => held.Task; var vm = New();
            var page = new SeasonReviewCallerHarness.Caller(vm);
            page.Enter(3); page.Enter(0); page.Enter(3); page.Enter(0); page.Enter(3);
            Check(calls == 1 && vm.Loading && vm.Items.Count == 0 && vm.Next == "", "exact pivot repeated entry single dispatch while held");
            int notices = 0; vm.PropertyChanged += (s, e) => notices++;
            var before = State(vm, notices);
            await Finish(vm.GetItems()); vm.RefreshCommand.Execute(null); vm.LoadMoreCommand.Execute(null);
            Check(State(vm, notices) == before, "held public and commands reject without mutation/notification/release");
            held.SetResult(Success());
            Check(!vm.Loading && calls == 1 && vm.Items.Count == 1 && vm.Next == "opaque+/=&cursor" && vm.CanLoadMore, "pivot publishes once and completes ownership");
            response = () => Task.FromResult(Success(2)); await Finish(vm.GetItems());
            Check(calls == 2 && requestedNext == "opaque+/=&cursor" && vm.Items.Count == 2 && !vm.CanLoadMore && !vm.Loading, "later append accepted with opaque cursor and terminal total");
        }
        private static async Task ZeroId()
        {
            var held = Held(); response = () => held.Task; var vm = New();
            vm.Items.Add(new SeasonShortReviewItemModel { review_id = 99 }); vm.Next = "seed"; vm.CanLoadMore = true; vm.MediaID = 0;
            int notices = 0; vm.PropertyChanged += (s, e) => notices++;
            var before = State(vm, notices); await Finish(vm.GetItems());
            Check(State(vm, notices) == before, "idle zero id keeps rows/cursor/load flags without notification");
            vm.MediaID = 42; var owner = vm.GetItems(); vm.MediaID = 0; before = State(vm, notices);
            await Finish(vm.GetItems());
            Check(State(vm, notices) == before && !owner.IsCompleted, "zero id cannot release a held owner");
            vm.MediaID = 42; vm.LoadMoreCommand.Execute(null); vm.RefreshCommand.Execute(null);
            Check(calls == 1 && vm.Loading && vm.Next == "seed" && vm.Items.Count == 1, "restored id commands remain blocked");
            held.SetResult(Success()); await Finish(owner);
            Check(vm.Items.Count == 2 && !vm.Loading, "held response retains existing media-id semantics");
        }
        private static async Task Notifications()
        {
            foreach (var property in new[] { "LoadingTrue", "CanLoadMoreFalse", "LoadingFalse" })
            for (int entry = 0; entry < 3; entry++)
            {
                var held = Held(); response = () => held.Task; var vm = New();
                int notices = 0; bool ran = false;
                vm.PropertyChanged += (s, e) =>
                {
                    notices++;
                    bool match = property == "LoadingTrue" ? e.PropertyName == "Loading" && vm.Loading : property == "LoadingFalse" ? e.PropertyName == "Loading" && !vm.Loading : e.PropertyName == "CanLoadMore" && !vm.CanLoadMore;
                    if (!match || ran) return;
                    ran = true; var before = State(vm, notices); Invoke(vm, entry);
                    Check(State(vm, notices) == before, property + " entry=" + entry + " rejects with exact state/dispatch/notification preservation");
                };
                var owner = vm.GetItems(); held.SetResult(Success()); await Finish(owner);
                Check(ran && calls == 1 && vm.Items.Count == 1 && vm.Next == "opaque+/=&cursor" && vm.CanLoadMore && !vm.Loading, property + " entry=" + entry + " success row/cursor survives");
                var refreshHeld = Held(); response = () => refreshHeld.Task; vm.RefreshCommand.Execute(null);
                Check(calls == 2 && vm.Loading && vm.Items.Count == 0 && vm.Next == "" && requestedNext == "", "idle refresh clears/resets and dispatches after " + property + "/" + entry);
                refreshHeld.SetResult(Success(1));
                Check(!vm.Loading && vm.Items.Count == 1 && !vm.CanLoadMore, "idle refresh completes after " + property + "/" + entry);
            }
        }
        private static async Task RefreshCollection()
        {
            for (int entry = 0; entry < 3; entry++)
            {
                var held = Held(); response = () => held.Task; var vm = New(); vm.Next = "old";
                vm.Items.Add(new SeasonShortReviewItemModel { review_id = 99 });
                bool ran = false; int notices = 0; vm.PropertyChanged += (s, e) => notices++;
                vm.Items.CollectionChanged += (s, e) => { if (ran) return; ran = true; var before = State(vm, notices); Invoke(vm, entry); Check(State(vm, notices) == before, "refresh collection callback entry=" + entry + " is quiet"); };
                vm.RefreshCommand.Execute(null);
                Check(ran && calls == 1 && vm.Loading && vm.Items.Count == 0 && vm.Next == "" && requestedNext == "", "refresh claims before collection/reset entry=" + entry);
                held.SetResult(Success(1));
                Check(!vm.Loading && vm.Items.Count == 1, "refresh collection case completes entry=" + entry);
                await Task.CompletedTask;
            }
        }
        private static async Task Failures()
        {
            foreach (var kind in new[] { "transport", "business", "malformed", "parse", "fault", "throw" })
            {
                var held = Held(); response = () => held.Task; var vm = New();
                vm.Items.Add(new SeasonShortReviewItemModel { review_id = 99 }); vm.Next = "retained"; vm.CanLoadMore = true;
                if (kind == "throw") response = () => throw new InvalidOperationException("sync transport");
                var owner = vm.GetItems();
                if (kind == "fault") held.SetException(new InvalidOperationException("async transport"));
                else if (kind != "throw") held.SetResult(kind == "transport" ? new HttpResults { status = false, message = "transport rejected" } : kind == "business" ? Reply("{\"code\":-1,\"message\":\"business rejected\"}") : kind == "malformed" ? Reply("not json") : Reply("{\"code\":0,\"data\":{\"list\":{\"wrong\":true}}}"));
                await Finish(owner);
                Check(!vm.Loading && !vm.CanLoadMore && vm.Items.Count == 1 && vm.Next == "retained" && calls == 1 && Utils.Toasts.Count > 0, kind + " preserves legacy failure/toast/cursor and releases");
                response = () => Task.FromResult(Success(2)); await Finish(vm.GetItems());
                Check(calls == 2 && vm.Items.Count == 2 && !vm.Loading, kind + " retry dispatches once");
            }
        }
        private static async Task CompletionThrow()
        {
            var held = Held(); response = () => held.Task; var vm = New();
            PropertyChangedEventHandler listener = (s, e) => { if (e.PropertyName == "Loading" && !vm.Loading) throw new InvalidOperationException("completion observer"); };
            vm.PropertyChanged += listener; var owner = vm.GetItems(); held.SetResult(Success());
            bool threw = false; try { await Finish(owner); } catch (InvalidOperationException ex) { threw = ex.Message == "completion observer"; }
            vm.PropertyChanged -= listener;
            Check(threw && !vm.Loading && calls == 1, "completion observer exception retains propagation and false Loading");
            response = () => Task.FromResult(Success(2)); await Finish(vm.GetItems());
            Check(calls == 2 && vm.Items.Count == 2 && !vm.Loading, "completion observer exception always releases private claim");
        }
        private static async Task Legacy()
        {
            response = () => Task.FromResult(Success(1)); var vm = New(); await Finish(vm.GetItems());
            Check(vm.Items.Count == 1 && vm.Items[0].content == "a & b" && !vm.CanLoadMore && !vm.Loading, "ordinary one page retains HTML decode and terminal behavior");
            vm.Loading = true; int notices = 0; vm.PropertyChanged += (s, e) => notices++; var before = State(vm, notices);
            await Finish(vm.GetItems()); vm.RefreshCommand.Execute(null); vm.LoadMoreCommand.Execute(null);
            Check(State(vm, notices) == before, "external idle Loading compatibility blocks every entry");
            vm.Loading = false; await Finish(vm.GetItems()); Check(calls == 2, "external Loading release permits later normal read");
        }
    }
}

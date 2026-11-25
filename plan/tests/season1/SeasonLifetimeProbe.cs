using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Modules;
using Newtonsoft.Json;

namespace SeasonLifetimeHarness
{
    public static class LifetimeProbe
    {
        static int passed, failed;
        static int[] closedCounters;
        static readonly Shell Root = new Shell();
        static readonly MethodInfo InvalidateMethod = typeof(SeasonDetailVM).GetMethod("InvalidateLoad", Type.EmptyTypes);
        static void Check(bool value, string label)
        {
            if (value) passed++;
            else { failed++; Console.WriteLine("FAIL lifetime: " + label); }
        }
        static HttpResults Response(string body) => new HttpResults { status = true, code = 200, results = body };
        static string Metadata(string id) => "{\"code\":0,\"result\":{\"title\":\"season-" + id + "\",\"season_id\":7,\"media_id\":17,\"type\":1}}";
        const string Episodes = "{\"code\":0,\"result\":{\"season_id\":7,\"season_title\":\"fallback\",\"episodes\":[{\"id\":11,\"aid\":\"22\",\"cid\":\"33\",\"status\":2,\"title\":\"one\"}]}}";
        const string BadEpisodes = "{\"code\":0,\"result\":{\"episodes\":[7]}}";
        static HttpResults Normal(SeasonApi.SeasonApiCall call) => Response(call.Endpoint == SeasonEndpoint.Detail ? Metadata(call.Id) : Episodes);
        static void Reset()
        {
            SeasonApi.Reset(); LogHelper.Entries.Clear(); Utils.Toasts.Clear();
            SeasonApi.AsyncFixture = call => Task.FromResult(Normal(call));
        }
        static void Invalidate(SeasonDetailVM vm) => InvalidateMethod?.Invoke(vm, null);
        static List<string> Observe(SeasonDetailVM vm)
        {
            var changes = new List<string>();
            vm.PropertyChanged += (sender, args) => changes.Add(args.PropertyName);
            return changes;
        }
        static int Effects(SeasonPage page) => page.TitleWrites + page.Review.Writes + page.player.Attempts + page.comment.Calls + page.QRs + page.Directories;
        public static bool Run()
        {
            Check(InvalidateMethod != null, "explicit reusable VM invalidation API exists");
            ActiveAndLiveRefresh();
            foreach (var endpoint in new[] { SeasonEndpoint.Detail, SeasonEndpoint.EpList })
            {
                var weak = ClosedBatch(endpoint);
                Collect();
                Check(weak.All(w => !w.IsAlive), endpoint + " 50 settled actual-close page owners collect");
                ClosedFailure(endpoint, false);
                ClosedFailure(endpoint, true);
            }
            foreach (var invalidated in new[] { false, true })
                foreach (var endpoint in new[] { SeasonEndpoint.Detail, SeasonEndpoint.EpList })
                    foreach (var fault in new[] { false, true })
                        ReverseVM(invalidated, endpoint, fault);
            DecodeGate();
            NotificationInvalidation();
            foreach (var fault in new[] { false, true }) FreshAndClosedEntries(fault);
            ReversePage(false); ReversePage(true);
            ActiveFailuresAndRetry();
            PageBoundaryException();
            RootControls();
            SeasonApi.Reset();
            Check(DataTransferManager.Subscribers == 0 && Root.tabView.TabItems.Count == 0, "all static fixture owners/gates cleaned");
            Console.WriteLine("Season lifetime: " + passed + " passed, " + failed + " failed; player boundary is an attempted-call spy; native behavior unmeasured");
            return failed == 0;
        }

        static void ActiveAndLiveRefresh()
        {
            Reset();
            var page = new SeasonPage("active"); var tab = Root.Attach(page); var vm = page.VM;
            var metadata = new TaskCompletionSource<HttpResults>();
            var episodes = new TaskCompletionSource<HttpResults>();
            SeasonApi.AsyncFixture = call => call.Endpoint == SeasonEndpoint.Detail ? metadata.Task : episodes.Task;
            var task = page.Start();
            Check(SeasonApi.Calls.Count == 1 && vm.Loading && page.player.Attempts == 0, "active metadata gate really delays real page/VM");
            metadata.SetResult(Response(Metadata("active")));
            Check(SeasonApi.Calls.Count == 2 && !task.IsCompleted && vm.Detail == null, "active episode gate precedes real publication");
            episodes.SetResult(Response(Episodes)); task.GetAwaiter().GetResult();
            Check(vm.Loaded && !vm.Loading && !vm.ShowError && vm.Detail.title == "season-active" && vm.Episodes.Count == 1, "active complete real VM/parser accepts delayed metadata and episodes");
            Check(page.player.Attempts == 1 && page.comment.Calls == 1 && page.Review.Writes == 1 && page.QRs == 1 && page.TitleWrites == 1, "active complete page suffix executes once");
            SeasonApi.AsyncFixture = call => Task.FromResult(Normal(call));
            page.Refresh();
            Check(ReferenceEquals(vm, page.VM) && page.player.Attempts == 2 && vm.Loaded && !vm.ShowError, "real UI refresh reuses live page/player and x:Bind VM identity");
            page.LeaveRetained();
            Check(DataTransferManager.Subscribers == 0 && page.player.Disposes == 0, "ordinary forward leave retains live player without disposal");
            page.Activate(); page.Navigate(NavigationMode.Back, null);
            Check(page.player.Disposes == 0 && DataTransferManager.Subscribers == 1, "retained native-navigation double reentry remains live");
            var hold = new TaskCompletionSource<HttpResults>();
            SeasonApi.AsyncFixture = call => call.Endpoint == SeasonEndpoint.Detail ? hold.Task : Task.FromResult(Normal(call));
            task = page.Start(); var requests = SeasonApi.Calls.Count; page.Refresh();
            Check(SeasonApi.Calls.Count == requests, "existing refresh Loading guard rejects overlapping UI refresh");
            hold.SetResult(Response(Metadata("refresh"))); task.GetAwaiter().GetResult();
            Root.Close(tab);
            Check(page.player.Disposes == 1 && DataTransferManager.Subscribers == 0, "actual shell/frame/page close disposes and releases share");
            page.Close();
            Check(page.player.Disposes == 1, "repeat terminal page close is safe and idempotent");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static WeakReference[] ClosedBatch(SeasonEndpoint endpoint)
        {
            var weak = new WeakReference[50];
            closedCounters = new int[9];
            for (var i = 0; i < weak.Length; i++) weak[i] = ClosedOne(endpoint, i);
            Console.WriteLine("Closed gate " + endpoint + ": closes=50 inFlight=" + closedCounters[0] +
                " lateRequests=" + closedCounters[1] + " lateNotifications=" + closedCounters[2] +
                " lateTitle=" + closedCounters[3] + " lateReview=" + closedCounters[4] +
                " latePlayerAttempts=" + closedCounters[5] + " lateComments=" + closedCounters[6] +
                " lateQR=" + closedCounters[7] + " lateDirectories=" + closedCounters[8]);
            return weak;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        static WeakReference ClosedOne(SeasonEndpoint endpoint, int i)
        {
            Reset();
            var gate = new TaskCompletionSource<HttpResults>();
            SeasonApi.AsyncFixture = call => call.Endpoint == endpoint ? gate.Task : Task.FromResult(Normal(call));
            var page = new SeasonPage("closed-" + i); var tab = Root.Attach(page);
            var changes = Observe(page.VM); var task = page.Start();
            Check(!task.IsCompleted && SeasonApi.Calls.Count == (endpoint == SeasonEndpoint.Detail ? 1 : 2), endpoint + " pending in-flight request " + i);
            Root.Close(tab); changes.Clear();
            var effects = Effects(page); var requests = SeasonApi.Calls.Count;
            gate.SetResult(Response(endpoint == SeasonEndpoint.Detail ? Metadata("late") : Episodes));
            task.GetAwaiter().GetResult();
            Check(changes.Count == 0 && page.VM.Detail == null && !page.VM.Loaded && page.VM.Loading && !page.VM.ShowError,
                  endpoint + " zero late Detail/list/error/loading notifications " + i);
            Check(Effects(page) == effects && page.player.AfterDisposeAttempts == 0 && page.player.Attempts == 0,
                  endpoint + " zero late title/review/attempted-player/comment/QR/directory work " + i);
            Check(SeasonApi.Calls.Count == requests && Utils.Toasts.Count == 0 && LogHelper.Entries.Count == 0,
                  endpoint + " no next request or stale diagnostic " + i);
            closedCounters[0]++;
            closedCounters[1] += SeasonApi.Calls.Count - requests;
            closedCounters[2] += changes.Count;
            closedCounters[3] += page.TitleWrites;
            closedCounters[4] += page.Review.Writes;
            closedCounters[5] += page.player.Attempts;
            closedCounters[6] += page.comment.Calls;
            closedCounters[7] += page.QRs;
            closedCounters[8] += page.Directories;
            var weak = new WeakReference(page);
            SeasonApi.Reset();
            return weak;
        }

        static void ClosedFailure(SeasonEndpoint endpoint, bool fault)
        {
            Reset();
            var gate = new TaskCompletionSource<HttpResults>();
            SeasonApi.AsyncFixture = call => call.Endpoint == endpoint ? gate.Task : Task.FromResult(Normal(call));
            var page = new SeasonPage("closed-failure"); var tab = Root.Attach(page); var task = page.Start();
            Root.Close(tab); var changes = Observe(page.VM); var effects = Effects(page); var requests = SeasonApi.Calls.Count;
            if (fault) gate.SetException(new InvalidOperationException("late transport fault"));
            else gate.SetResult(Response(endpoint == SeasonEndpoint.Detail ? "{\"code\":-1}" : BadEpisodes));
            task.GetAwaiter().GetResult();
            Check(changes.Count == 0 && Effects(page) == effects && SeasonApi.Calls.Count == requests && LogHelper.Entries.Count == 0 && Utils.Toasts.Count == 0,
                  "closed " + endpoint + " " + (fault ? "exception" : "parse/rejection") + " is quiet with no publication/next request");
        }

        static void ReverseVM(bool invalidated, SeasonEndpoint endpoint, bool fault)
        {
            Reset();
            var gate = new TaskCompletionSource<HttpResults>();
            SeasonApi.AsyncFixture = call => call.Id == "A" && call.Endpoint == endpoint ? gate.Task : Task.FromResult(Normal(call));
            var vm = new SeasonDetailVM(); var a = vm.LoadSeasonDetail("A");
            if (invalidated) { var before = Observe(vm); Invalidate(vm); Invalidate(vm); Check(before.Count == 0, "VM invalidation twice has no notifications"); }
            vm.LoadSeasonDetail("B").GetAwaiter().GetResult();
            var detail = vm.Detail; var episodes = vm.Episodes; var previews = vm.Previews;
            var changes = Observe(vm); var requests = SeasonApi.Calls.Count; var error = vm.ErrorMsg;
            if (fault) gate.SetException(new InvalidOperationException("superseded A failure"));
            else gate.SetResult(Response(endpoint == SeasonEndpoint.Detail ? Metadata("A") : Episodes));
            a.GetAwaiter().GetResult();
            var label = (invalidated ? "invalidated/reusable" : "direct reverse") + " VM " + endpoint + " " + (fault ? "fault" : "success");
            Check(ReferenceEquals(vm.Detail, detail) && ReferenceEquals(vm.Episodes, episodes) && ReferenceEquals(vm.Previews, previews) && detail.title == "season-B", label + " retains distinct B models/identities");
            Check(vm.Loaded && !vm.Loading && !vm.ShowError && vm.ErrorMsg == error && changes.Count == 0, label + " retains B loading/error state with zero stale notifications");
            Check(SeasonApi.Calls.Count == requests && LogHelper.Entries.Count == 0, label + " starts no next endpoint or stale log");
            Invalidate(vm);
            vm.LoadSeasonDetail("C").GetAwaiter().GetResult();
            Check(vm.Detail.title == "season-C" && vm.Loaded && !vm.Loading, label + " VM remains independently reusable");
        }

        static void DecodeGate()
        {
            Reset();
            var decode = new TaskCompletionSource<object>();
            HttpResults.DecodeFixture = (reply, type) => decode.Task;
            var vm = new SeasonDetailVM(); var task = vm.LoadSeasonDetail("decode");
            Check(SeasonApi.Calls.Count == 1 && !task.IsCompleted, "real VM metadata decode await is held separately");
            Invalidate(vm); var changes = Observe(vm);
            decode.SetResult(JsonConvert.DeserializeObject<BiliLite.Models.ApiResultModel<SeasonDetailModel>>(Metadata("decode")));
            task.GetAwaiter().GetResult();
            Check(SeasonApi.Calls.Count == 1 && changes.Count == 0 && vm.Detail == null, "post-decode invalidation rejects publication and next endpoint");
            HttpResults.DecodeFixture = null;
        }

        static void NotificationInvalidation()
        {
            // Real PropertyChanged executes inline. Each setter is an ownership boundary,
            // including loading/error state before the first request and the accepted batch.
            foreach (var boundary in new[] { "Loaded", "Loading", "ShowError", "Detail", "Episodes", "ShowEpisodes", "Previews", "ShowPreview", "NothingPlay" })
            {
                Reset();
                var vm = new SeasonDetailVM();
                var notificationsAfterInvalidation = new List<string>();
                var invalidated = false;
                vm.PropertyChanged += (sender, args) =>
                {
                    if (invalidated) notificationsAfterInvalidation.Add(args.PropertyName);
                    else if (args.PropertyName == boundary) { invalidated = true; Invalidate(vm); }
                };
                vm.LoadSeasonDetail("notification").GetAwaiter().GetResult();
                Check(invalidated && notificationsAfterInvalidation.Count == 0,
                      "synchronous " + boundary + " invalidation prevents every later publication");
                Check(SeasonApi.Calls.Count == (boundary == "Loaded" || boundary == "Loading" || boundary == "ShowError" ? 0 : 2),
                      "synchronous " + boundary + " invalidation prevents any later request");
            }
            Reset();
            var page = new SeasonPage("notify-close"); var tab = Root.Attach(page);
            var closed = false;
            page.VM.PropertyChanged += (sender, args) =>
            {
                if (!closed && args.PropertyName == "Detail") { closed = true; Root.Close(tab); }
            };
            var changes = Observe(page.VM);
            page.Start().GetAwaiter().GetResult();
            Check(closed && page.VM.Detail == null && !page.VM.Loaded && page.VM.Loading && page.player.Attempts == 0 && page.comment.Calls == 0 && page.QRs == 0,
                  "actual close during Detail notification rejects remaining VM/page suffix");
            Check(changes.Count(name => name == "Episodes" || name == "Previews" || name == "ShowEpisodes" || name == "ShowPreview" || name == "NothingPlay") == 0,
                  "actual reentrant close makes no post-close list/visibility publication");
        }

        static void FreshAndClosedEntries(bool fault)
        {
            Reset();
            var gate = new TaskCompletionSource<HttpResults>(); var first = true;
            SeasonApi.AsyncFixture = call =>
            {
                if (call.Endpoint == SeasonEndpoint.Detail && first) { first = false; return gate.Task; }
                return Task.FromResult(Normal(call));
            };
            // Same content/id, distinct page/player/VM owners, matching fresh-shell preconditions.
            var old = new SeasonPage("same"); var oldTab = Root.Attach(old); var a = old.Start();
            Root.Close(oldTab); var oldChanges = Observe(old.VM); var oldEffects = Effects(old);
            var fresh = new SeasonPage("same"); var newTab = Root.Attach(fresh); fresh.Start().GetAwaiter().GetResult();
            var freshDetail = fresh.VM.Detail; var freshEffects = Effects(fresh); var freshChanges = Observe(fresh.VM);
            var requests = SeasonApi.Calls.Count;
            old.Start().GetAwaiter().GetResult(); old.Navigate(NavigationMode.New, "stale"); old.Navigate(NavigationMode.Back, null); old.Activate(); old.Refresh();
            Check(SeasonApi.Calls.Count == requests && Effects(old) == oldEffects && DataTransferManager.Subscribers == 1,
                  "closed stale Init/OnNavigated/Loaded/refresh entries make no request or subscription (" + fault + ")");
            if (fault) gate.SetException(new InvalidOperationException("old owner failed"));
            else gate.SetResult(Response(Metadata("same")));
            a.GetAwaiter().GetResult();
            Check(oldChanges.Count == 0 && Effects(old) == oldEffects && old.player.AfterDisposeAttempts == 0 && old.VM.Detail == null,
                  "terminal old owner rejects late " + (fault ? "fault" : "success") + " despite same content identity");
            Check(ReferenceEquals(fresh.VM.Detail, freshDetail) && Effects(fresh) == freshEffects && freshChanges.Count == 0 && fresh.VM.Loaded && !fresh.VM.Loading && !fresh.VM.ShowError && fresh.player.Attempts == 1,
                  "fresh same-content owner survives old completion unchanged");
            Check(SeasonApi.Calls.Count == requests && LogHelper.Entries.Count == 0, "fresh-owner scenario starts no stale endpoint/log");
            old.Close(); Root.Close(newTab);
        }

        static void ReversePage(bool fault)
        {
            Reset();
            var gate = new TaskCompletionSource<HttpResults>(); var first = true;
            SeasonApi.AsyncFixture = call =>
            {
                if (call.Endpoint == SeasonEndpoint.Detail && first) { first = false; return gate.Task; }
                return Task.FromResult(Normal(call));
            };
            var page = new SeasonPage("page-A"); var tab = Root.Attach(page); var vm = page.VM;
            var a = page.Start(); page.Start().GetAwaiter().GetResult();
            var detail = vm.Detail; var effects = Effects(page); var changes = Observe(vm); var requests = SeasonApi.Calls.Count;
            if (fault) gate.SetException(new InvalidOperationException("old page operation fault"));
            else gate.SetResult(Response(Metadata("old-page")));
            a.GetAwaiter().GetResult();
            Check(ReferenceEquals(vm, page.VM) && ReferenceEquals(detail, vm.Detail) && Effects(page) == effects && changes.Count == 0 && page.player.Attempts == 1 && SeasonApi.Calls.Count == requests && !vm.ShowError,
                  "captured page generation rejects reverse " + (fault ? "fault" : "success") + " suffix and retains binding identity");
            Root.Close(tab);
        }

        static void ActiveFailuresAndRetry()
        {
            foreach (var endpoint in new[] { SeasonEndpoint.Detail, SeasonEndpoint.EpList })
            {
                Reset();
                SeasonApi.AsyncFixture = call => call.Endpoint == endpoint ? Task.FromException<HttpResults>(new InvalidOperationException("active transport")) : Task.FromResult(Normal(call));
                var vm = new SeasonDetailVM(); vm.LoadSeasonDetail("active-fault").GetAwaiter().GetResult();
                Check(vm.ShowError && !vm.Loading && !vm.Loaded && vm.Detail == null && LogHelper.Entries.Count == 1 && Utils.Toasts.Count == 0, "active " + endpoint + " exception retains error panel/log semantics");
            }
            Reset();
            var page = new SeasonPage("retry"); var tab = Root.Attach(page); var vmIdentity = page.VM;
            SeasonApi.AsyncFixture = call => Task.FromResult(Response(call.Endpoint == SeasonEndpoint.Detail ? Metadata("retry") : BadEpisodes));
            page.Navigate(NavigationMode.New, "retry");
            Check(page.VM.ShowError && !page.VM.Loading && page.player.Attempts == 0 && Utils.Toasts.Count == 0, "actual navigation active episode parse failure publishes error without player");
            SeasonApi.AsyncFixture = call => Task.FromResult(Normal(call)); page.Refresh();
            Check(ReferenceEquals(vmIdentity, page.VM) && page.VM.Loaded && !page.VM.ShowError && page.player.Attempts == 1, "actual refresh/error retry succeeds on same live owner");
            var detail = page.VM.Detail; var episodes = page.VM.Episodes;
            SeasonApi.AsyncFixture = call => Task.FromResult(Response(call.Endpoint == SeasonEndpoint.Detail ? Metadata("retry") : BadEpisodes));
            page.Refresh();
            Check(page.VM.ShowError && !page.VM.Loading && ReferenceEquals(detail, page.VM.Detail) && ReferenceEquals(episodes, page.VM.Episodes), "failed live refresh preserves all last-good model identities");
            page.Back(); Check(page.player.Disposes == 1 && DataTransferManager.Subscribers == 0, "actual Back close remains terminal"); Root.Close(tab);
        }

        static void PageBoundaryException()
        {
            Reset();
            var page = new SeasonPage("boundary"); var tab = Root.Attach(page);
            page.player.OnInitialize = () => throw new InvalidOperationException("modeled player failure");
            var previous = SynchronizationContext.Current;
            var context = new AsyncVoidBoundary();
            try
            {
                SynchronizationContext.SetSynchronizationContext(context);
                page.Navigate(NavigationMode.New, "boundary");
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
            Check(LogHelper.Entries.Count == 1 && context.Errors == 0 && page.QRs == 0 && page.player.Attempts == 1, "active page async boundary logs suffix failure without unhandled async-void exception");
            Root.Close(tab);
        }

        sealed class AsyncVoidBoundary : SynchronizationContext
        {
            public int Errors;
            public override void Post(SendOrPostCallback callback, object state)
            {
                try { callback(state); }
                catch (Exception) { Errors++; }
            }
        }

        static void RootControls()
        {
            var active = CreateRooted(false); Collect();
            Check(active.Count(w => w.IsAlive) == 50 && DataTransferManager.Subscribers == 50 && Root.tabView.TabItems.Count == 50,
                  "positive 50 active tabs/static share subscriptions retain owners");
            CloseAll(); Collect();
            Check(active.All(w => !w.IsAlive) && DataTransferManager.Subscribers == 0, "50 completed ordinary actual shell closes collect");
            var omitted = CreateRooted(true); Collect();
            Check(omitted.Count(w => w.IsAlive) == 50 && DataTransferManager.Subscribers == 50 && Root.tabView.TabItems.Count == 0,
                  "negative detach-without-close retains 50 via declared static share root");
            CloseWeak(omitted); Collect();
            Check(omitted.All(w => !w.IsAlive) && DataTransferManager.Subscribers == 0, "actual close cleans omitted-notification root control");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        static WeakReference[] CreateRooted(bool omitClose)
        {
            Reset(); var weak = new WeakReference[50];
            for (var i = 0; i < 50; i++)
            {
                var page = new SeasonPage("root-" + i); var tab = Root.Attach(page); page.Start().GetAwaiter().GetResult();
                weak[i] = new WeakReference(page);
                if (omitClose) Root.DetachWithoutClose(tab);
            }
            SeasonApi.Reset(); return weak;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CloseAll() { while (Root.tabView.TabItems.Count != 0) Root.Close(Root.tabView.TabItems[0]); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CloseWeak(WeakReference[] weak) { foreach (var w in weak) (w.Target as SeasonPage)?.Close(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Collect() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
    }
}

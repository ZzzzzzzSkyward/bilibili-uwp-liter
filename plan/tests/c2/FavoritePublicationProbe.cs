// Frozen residual assertions retain their expectations; only namespace and entrypoint adapt.
namespace OriginalPublicationProof {
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Modules;
using C2Probe;

internal static class ResidualProbe
{
    private static int passed, failed;
    private static void Check(bool okay, string label)
    {
        Console.WriteLine((okay ? "PASS " : "FAIL ") + label);
        if (okay) passed++; else failed++;
    }
    private static HttpResults Response(string row, int total) => new HttpResults
    {
        status = true, code = 200,
        results = "{\"code\":0,\"data\":{\"info\":{\"id\":\"folder\",\"mid\":\"owner\",\"media_count\":" + total + "},\"medias\":[{\"id\":\"" + row + "\",\"title\":\"" + row + "\"}],\"has_more\":false}}"
    };
    // A binding observer consumes only the actual INotifyPropertyChanged signal. It has no paging policy.
    private sealed class BindingObserver
    {
        public ObservableCollection<FavoriteInfoVideoItemModel> Items;
        public int Replacements, Appends;
        public BindingObserver(FavoriteDetailVM vm)
        {
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != "Videos") return;
                if (Items != null) Items.CollectionChanged -= Changed;
                Items = vm.Videos;
                if (Items != null) Items.CollectionChanged += Changed;
                Replacements++;
            };
        }
        private void Changed(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { Appends++; }
    }
    private static async Task ReplacementJourney(bool search)
    {
        FakeTransport.Reset();
        var vm = new FavoriteDetailVM { Type = 2, Id = "folder", Keyword = "", Page = 1 };
        var binding = new BindingObserver(vm);
        FakeTransport.Delivery = _ => Task.FromResult(Response("initial", 1));
        await vm.LoadFavoriteInfo();
        var initial = binding.Items;
        Check(binding.Replacements == 1 && initial != null && initial.Single().id == "initial", (search ? "search" : "refresh") + " positive initial publication signal");
        FakeTransport.Delivery = _ => Task.FromResult(Response(search ? "matching" : "updated", 1));
        if (search) vm.Search("C++&literal%2B"); else vm.RefreshCommand.Execute(null);
        var label = search ? "search" : "refresh";
        Check(FakeTransport.ActualRequests.Count == 2 && !vm.Loading && vm.Page == 1 && !vm.ShowLoadMore && !vm.Nothing,
            label + " actual command journey completes two current requests");
        Check(vm.Videos.Single().id == (search ? "matching" : "updated") && !ReferenceEquals(vm.Videos, initial),
            label + " actual model accepted replacement rows");
        Check(FakeTransport.ActualRequests.Last().parameter.Contains("keyword=" + (search ? "C%2B%2B%26literal%252B" : "") + "&pn=1&ps=20"),
            label + " real builder retains raw text as one encoded field on first page");
        Check(binding.Replacements >= 2 && ReferenceEquals(binding.Items, vm.Videos) && binding.Items.Single().id == (search ? "matching" : "updated"),
            label + " desired-safe: accepted replacement reaches Videos binding; observed signals=" + binding.Replacements + ", view=" + binding.Items.Single().id + ", model=" + vm.Videos.Single().id);
    }
    private static async Task AppendControl()
    {
        FakeTransport.Reset();
        var vm = new FavoriteDetailVM { Type = 2, Id = "folder", Keyword = "", Page = 1 };
        var binding = new BindingObserver(vm);
        // No flag: this control stays on the explicitly preserved COUNT compatibility path.
        FakeTransport.Delivery = _ => Task.FromResult(new HttpResults {status=true,code=200,results="{\"code\":0,\"data\":{\"info\":{\"mid\":\"owner\",\"media_count\":2},\"medias\":[{\"id\":\"initial\"}]}}"});
        await vm.LoadFavoriteInfo();
        var initial = binding.Items;
        FakeTransport.Delivery = _ => Task.FromResult(Response("appended",2));
        vm.LoadMoreCommand.Execute(null);
        Check(ReferenceEquals(initial, vm.Videos) && ReferenceEquals(binding.Items, vm.Videos) && binding.Replacements == 1 && binding.Appends == 1 && binding.Items.Select(x=>x.id).SequenceEqual(new[]{"initial","appended"}),
            "positive append control: existing observable collection signals the accepted row");
        Check(vm.Page == 2 && !vm.ShowLoadMore && !vm.Loading && FakeTransport.ActualRequests.Count == 2,
            "positive append control: preserved terminal state");
    }
    public static async Task<int> Run()
    {
        ApiHelper.API_BASE_URL = "https://api.bilibili.com";
        await ReplacementJourney(true);
        await ReplacementJourney(false);
        await AppendControl();
        Console.WriteLine("RESIDUAL passed=" + passed + " failed=" + failed);
        return failed == 0 ? 0 : 1;
    }
}

}
namespace C2Probe
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BiliLite.Api;
    using BiliLite.Helpers;
    using BiliLite.Modules;

    internal static class FavoritePublicationProbe
    {
        private static int passed, failed;
        private const string Keyword = "C++&literal%2B 中文👋";
        private static void Check(bool condition, string label)
        {
            if (condition) passed++;
            else { failed++; Console.Error.WriteLine("FAIL favorite publication: " + label); }
        }
        private static HttpResults Reply(string json) => new HttpResults { status = true, code = 200, results = json };
        private static HttpResults Page(string id, bool more = false, int total = 1)
            => Reply("{\"code\":0,\"data\":{\"info\":{\"id\":\"folder\",\"mid\":\"owner\",\"media_count\":"
                + total + "},\"medias\":[null,{\"id\":\"" + id + "\"}],\"has_more\":" + (more ? "true" : "false") + "}}");
        private static FavoriteDetailVM NewVm(int type = 2) => new FavoriteDetailVM { Type = type, Id = "folder", Keyword = Keyword };
        private static string Ids(BindingObserver binding) => binding.Items == null ? "<null>" : string.Join(",", binding.Items.Select(x => x.id));
        public static async Task Run()
        {
            var oldHost = ApiHelper.API_BASE_URL;
            try
            {
                var residual = await OriginalPublicationProof.ResidualProbe.Run();
                PropertyIdentity();
                foreach (var type in new[] { 2, 21 })
                {
                    await RepeatedCommands(type);
                    await FirstPagePublicationBoundaries(type);
                    await FailedCommandsAndRetry(type);
                    await AppendFailureAndRetry(type);
                }
                Console.WriteLine("Favorite publication added checks: passed=" + passed + " failed=" + failed);
                if (residual + failed != 0) throw new InvalidOperationException("Favorite publication failures: residual=" + residual + " added=" + failed);
            }
            finally { ApiHelper.API_BASE_URL = oldHost; FakeTransport.Reset(); }
        }

        // It replaces its retained Items only on the actual inherited Videos notification.
        // Reads/echoes inside that callback exercise a bounded TwoWay binding echo.
        private sealed class BindingObserver
        {
            private readonly FavoriteDetailVM vm;
            private int depth;
            public ObservableCollection<FavoriteInfoVideoItemModel> Items { get; private set; }
            public int Replacements, Changes, Adds, Removes, Moves, MaxDepth, ExcessiveEcho;
            public readonly List<ObservableCollection<FavoriteInfoVideoItemModel>> Seen = new List<ObservableCollection<FavoriteInfoVideoItemModel>>();
            public BindingObserver(FavoriteDetailVM source)
            {
                vm = source;
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName != "Videos") return;
                    Replacements++;
                    depth++;
                    MaxDepth = Math.Max(MaxDepth, depth);
                    try
                    {
                        if (Items != null) Items.CollectionChanged -= Changed;
                        Items = vm.Videos;
                        Seen.Add(Items);
                        if (Items != null) Items.CollectionChanged += Changed;
                        if (depth <= 8) vm.Videos = vm.Videos;
                        else ExcessiveEcho++;
                    }
                    finally { depth--; }
                };
            }
            private void Changed(object sender, NotifyCollectionChangedEventArgs e)
            {
                Changes++;
                if (e.Action == NotifyCollectionChangedAction.Add) Adds++;
                if (e.Action == NotifyCollectionChangedAction.Remove) Removes++;
                if (e.Action == NotifyCollectionChangedAction.Move) Moves++;
            }
        }
        private static void PropertyIdentity()
        {
            var vm = NewVm(); var view = new BindingObserver(vm);
            Check(vm.Videos == null && vm.Videos == null && view.Replacements == 0, "fresh getter reads are pure/null without events");
            vm.Videos = null;
            Check(view.Replacements == 0 && view.Items == null, "first null to null emits nothing");
            var row = new FavoriteInfoVideoItemModel { id = "9007199254740993" };
            var a = new ObservableCollection<FavoriteInfoVideoItemModel> { row };
            vm.Videos = a;
            Check(view.Replacements == 1 && ReferenceEquals(view.Items, a) && ReferenceEquals(view.Seen.LastOrDefault(), a)
                && Ids(view) == row.id, "null to A stores before exactly one callback and binding sees rows");
            var before = view.Replacements;
            vm.Videos = a; var read = vm.Videos; var readAgain = vm.Videos; vm.Videos = a;
            Check(view.Replacements == before && ReferenceEquals(read, a) && ReferenceEquals(readAgain, a)
                && ReferenceEquals(vm.Videos[0], row), "same A with/without getter reads emits nothing");
            var b = new ObservableCollection<FavoriteInfoVideoItemModel> { row };
            vm.Videos = b;
            Check(view.Replacements == before + 1 && ReferenceEquals(view.Items, b) && ReferenceEquals(view.Seen.LastOrDefault(), b)
                && Ids(view) == row.id, "distinct B with identical contents publishes once");
            var changes = view.Changes;
            a.Add(new FavoriteInfoVideoItemModel { id = "detached-A" });
            Check(view.Changes == changes && Ids(view) == row.id, "replacement detaches A collection observer");
            before = view.Replacements;
            b.Add(new FavoriteInfoVideoItemModel { id = "second" }); b.Move(1, 0); b.RemoveAt(1);
            Check(view.Changes == changes + 3 && view.Adds == 1 && view.Moves == 1 && view.Removes == 1
                && Ids(view) == "second" && view.Replacements == before && ReferenceEquals(vm.Videos, b),
                "in-place add/move/remove publishes collection events without reference replacement");
            vm.Videos = null;
            Check(view.Replacements == before + 1 && view.Items == null && view.Seen.LastOrDefault() == null, "B to null clears listener once");
            changes = view.Changes; b.Add(row);
            Check(view.Changes == changes && view.Items == null, "null reset detaches B observer");
            before = view.Replacements; vm.Videos = null; var emptyRead = vm.Videos; vm.Videos = null;
            Check(view.Replacements == before && emptyRead == null, "repeated null after clear remains no-op");
            var c = new ObservableCollection<FavoriteInfoVideoItemModel> { row }; vm.Videos = c;
            Check(view.Replacements == before + 1 && ReferenceEquals(view.Items, c) && Ids(view) == row.id,
                "later C assignment is not suppressed by initialization/echo history");
            Check(view.MaxDepth == 1 && view.ExcessiveEcho == 0, "bounded current-reference callback echo never recurses");
        }
        private static async Task Initial(FavoriteDetailVM vm, BindingObserver view, bool more = false, int total = 1)
        {
            FakeTransport.Reset(); FakeTransport.Delivery = _ => Task.FromResult(Page("initial", more, total));
            await vm.LoadFavoriteInfo();
            Check(view.Replacements == 1 && Ids(view) == "initial" && ReferenceEquals(view.Items, vm.Videos)
                && !vm.Loading && view.ExcessiveEcho == 0, "actual initial publication type=" + vm.Type);
        }
        private static void Request(int type, int page, string keyword, string label)
        {
            Check(FakeTransport.ActualRequests.Count == 1, label + " one request");
            var request = FakeTransport.ActualRequests.Single();
            Check(request.baseUrl == "https://api.bilibili.com/" + (type == 21 ? "x/space/fav/season/list" : "x/v3/fav/resource/list")
                && request.parameter.Contains("keyword=" + Uri.EscapeDataString(keyword) + "&pn=" + page + "&ps=20"),
                label + " actual source/encoded keyword/request page");
        }
        private static Action Command(FavoriteDetailVM vm, bool search) => search ? () => vm.Search(Keyword) : () => vm.RefreshCommand.Execute(null);
        private static TaskCompletionSource<HttpResults> Gate() => new TaskCompletionSource<HttpResults>(TaskCreationOptions.RunContinuationsAsynchronously);
        private static async Task Complete(Completion completion, string label)
        {
            await completion.Done.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Check(completion.Errors.Count == 0, label + " async-void completion with no escaped fault");
        }
        private static async Task RepeatedCommands(int type)
        {
            var vm = NewVm(type); var view = new BindingObserver(vm); await Initial(vm, view);
            for (var cycle = 0; cycle < 4; cycle++)
            {
                var search = cycle % 2 == 0; var label = (search ? "Search" : "RefreshCommand") + " type=" + type + " cycle=" + cycle;
                FakeTransport.Reset(); var old = vm.Videos; var replacements = view.Replacements; var changes = view.Changes;
                var gate = Gate(); var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                FakeTransport.Delivery = _ => { entered.TrySetResult(true); return gate.Task; };
                var completion = new Completion(); completion.Start(Command(vm, search));
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Check(vm.Loading && !completion.Done.Task.IsCompleted && vm.Videos == null && view.Items == null
                    && vm.Page == 1 && vm.FavoriteInfo == null && view.Replacements == replacements + 1,
                    label + " reset reaches binding before pending response");
                old.Add(new FavoriteInfoVideoItemModel { id = "detached" });
                Check(view.Changes == changes && view.Items == null, label + " pending clear detaches old collection");
                completion.Start(() => { vm.Search("ignored"); vm.RefreshCommand.Execute(null); vm.LoadMoreCommand.Execute(null); });
                Check(FakeTransport.ActualRequests.Count == 1 && vm.Keyword == Keyword && vm.Page == 1 && vm.Loading
                    && vm.Videos == null && view.Replacements == replacements + 1, label + " Loading guards keep pending owner/reference");
                gate.SetResult(Page("updated-" + cycle)); await Complete(completion, label);
                Check(!vm.Loading && !vm.Nothing && !vm.ShowLoadMore && vm.Page == 1 && view.Replacements == replacements + 2
                    && Ids(view) == "updated-" + cycle && ReferenceEquals(view.Items, vm.Videos) && !ReferenceEquals(view.Items, old)
                    && view.MaxDepth == 1 && view.ExcessiveEcho == 0, label + " accepted collection reaches binding once after null");
                Request(type, 1, Keyword, label);
            }
        }
        private static async Task FirstPagePublicationBoundaries(int type)
        {
            foreach (var body in new[] {
                "{\"code\":0,\"data\":{\"info\":{\"id\":\"empty\",\"mid\":\"owner\",\"media_count\":42},\"medias\":[],\"has_more\":true}}",
                "{\"code\":0,\"data\":{\"medias\":[{\"id\":\"unpublished\"}],\"has_more\":true}}" })
            {
                var empty = body.Contains("\"id\":\"empty\"");
                var vm = NewVm(type); var view = new BindingObserver(vm); await Initial(vm, view);
                var rows = vm.Videos; var info = vm.FavoriteInfo; var replacements = view.Replacements;
                FakeTransport.Reset(); FakeTransport.Delivery = _ => Task.FromResult(Reply(body));
                vm.Page = 1; await vm.LoadFavoriteInfo();
                Check(ReferenceEquals(vm.Videos, rows) && ReferenceEquals(view.Items, rows) && view.Replacements == replacements
                    && Ids(view) == "initial" && vm.Page == 1 && !vm.ShowLoadMore && !vm.Loading && vm.Nothing == empty
                    && (empty ? vm.FavoriteInfo.id == "empty" : ReferenceEquals(vm.FavoriteInfo, info))
                    && FakeTransport.Toasts.Count == (empty ? 0 : 1), "direct first empty/missing info retains published reference type=" + type + " empty=" + empty);
                FakeTransport.Reset(); FakeTransport.Delivery = _ => Task.FromResult(Reply(body));
                var completion = new Completion(); completion.Start(Command(vm, true)); await Complete(completion, "first boundary search");
                Check(vm.Videos == null && view.Items == null && view.Replacements == replacements + 1 && !vm.Loading
                    && vm.Nothing == empty && vm.Page == 1 && !vm.ShowLoadMore,
                    "Search preceding null reset remains observable for first empty/missing info type=" + type + " empty=" + empty);
            }
        }
        private static IEnumerable<(string Label, HttpResults Result, string Notice)> FailureCases()
        {
            yield return ("null transport", null, "请检查你的网络连接");
            yield return ("HTTP transport", new HttpResults { status = false, code = 503, message = "fixture offline" }, "fixture offline");
            yield return ("business", Reply("{\"code\":-403,\"message\":\"fixture denied\"}"), "fixture denied");
            yield return ("envelope", Reply("{\"data\":{}}"), "读取播放列表失败");
            yield return ("payload", Reply("{\"code\":0,\"data\":{\"has_more\":\"false\",\"medias\":[]}}"), "读取播放列表失败");
        }
        private static async Task FailedCommandsAndRetry(int type)
        {
            foreach (var search in new[] { true, false })
            foreach (var test in FailureCases())
            {
                var label = test.Label + " " + (search ? "Search" : "RefreshCommand") + " type=" + type;
                var vm = NewVm(type); var view = new BindingObserver(vm); await Initial(vm, view);
                FakeTransport.Reset(); var before = view.Replacements; var gate = Gate();
                FakeTransport.Delivery = _ => gate.Task;
                var completion = new Completion(); completion.Start(Command(vm, search));
                Check(vm.Loading && view.Items == null && vm.Videos == null && view.Replacements == before + 1
                    && !completion.Done.Task.IsCompleted, label + " clears binding while pending failure");
                gate.SetResult(test.Result); await Complete(completion, label);
                Check(!vm.Loading && !vm.Nothing && !vm.ShowLoadMore && vm.Page == 1 && vm.Videos == null && view.Items == null
                    && view.Replacements == before + 1 && FakeTransport.Toasts.SequenceEqual(new[] { test.Notice }),
                    label + " categorized failure retains cleared binding and Page1");
                Request(type, 1, Keyword, label);
                FakeTransport.Reset(); var retry = new Completion(); var retryGate = Gate(); FakeTransport.Delivery = _ => retryGate.Task;
                retry.Start(() => vm.RefreshCommand.Execute(null));
                Check(vm.Loading && view.Items == null && view.Replacements == before + 1 && !retry.Done.Task.IsCompleted,
                    label + " retry null-to-null emits no replacement while pending");
                retryGate.SetResult(Page("retry")); await Complete(retry, label + " retry");
                Check(!vm.Loading && view.Replacements == before + 2 && ReferenceEquals(view.Items, vm.Videos) && Ids(view) == "retry"
                    && vm.Page == 1 && !vm.ShowLoadMore && FakeTransport.Toasts.Count == 0, label + " retry publishes once");
                Request(type, 1, Keyword, label + " retry");
            }
        }
        private static async Task AppendFailureAndRetry(int type)
        {
            foreach (var test in FailureCases())
            {
                var vm = NewVm(type); var view = new BindingObserver(vm); await Initial(vm, view, true, 2);
                var rows = vm.Videos; var row = rows.Single(); var info = vm.FavoriteInfo; var before = view.Replacements;
                var changes = view.Changes; FakeTransport.Reset(); var gate = Gate(); FakeTransport.Delivery = _ => gate.Task;
                var completion = new Completion(); completion.Start(() => vm.LoadMoreCommand.Execute(null));
                Check(vm.Loading && vm.Page == 2 && ReferenceEquals(view.Items, rows) && view.Replacements == before
                    && !completion.Done.Task.IsCompleted, test.Label + " append pending retains binding type=" + type);
                gate.SetResult(test.Result); await Complete(completion, "failed append");
                Check(!vm.Loading && !vm.Nothing && !vm.ShowLoadMore && vm.Page == 2 && ReferenceEquals(vm.FavoriteInfo, info)
                    && ReferenceEquals(vm.Videos, rows) && ReferenceEquals(view.Items, rows) && ReferenceEquals(rows.Single(), row)
                    && view.Replacements == before && view.Changes == changes && FakeTransport.Toasts.SequenceEqual(new[] { test.Notice }),
                    test.Label + " append failure retains reference without replacement type=" + type);
                Request(type, 2, Keyword, test.Label + " append");
                FakeTransport.Reset(); var retry = new Completion(); FakeTransport.Delivery = _ => Task.FromResult(Page("appended", false, 999));
                retry.Start(() => vm.LoadMoreCommand.Execute(null)); await Complete(retry, "append retry");
                Check(!vm.Loading && !vm.Nothing && !vm.ShowLoadMore && vm.Page == 2 && ReferenceEquals(vm.FavoriteInfo, info)
                    && ReferenceEquals(vm.Videos, rows) && ReferenceEquals(view.Items, rows) && ReferenceEquals(rows[0], row)
                    && view.Replacements == before && view.Changes == changes + 1 && view.Adds == 1 && Ids(view) == "initial,appended",
                    test.Label + " same-page retry appends one accepted row without replacement type=" + type);
                Request(type, 2, Keyword, test.Label + " append retry");
            }
        }
        // Tracks real async-void operation ownership and queued continuations; no sleeps/polling.
        private sealed class Completion : SynchronizationContext
        {
            private int active, pending;
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
                    var previous = Current; SetSynchronizationContext(this);
                    try { callback(state); }
                    catch (Exception ex) { lock (Errors) Errors.Add(ex); }
                    finally { SetSynchronizationContext(previous); Interlocked.Decrement(ref pending); CompleteIfIdle(); }
                });
            }
            public void Start(Action work)
            {
                var previous = Current; SetSynchronizationContext(this); OperationStarted();
                try { work(); }
                finally { OperationCompleted(); SetSynchronizationContext(previous); }
            }
        }
    }
}

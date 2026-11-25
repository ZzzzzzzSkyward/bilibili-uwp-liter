// Frozen local proofs retain every assertion; namespace and entrypoint alone adapt.
namespace OriginalFolderProof {
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Modules;
using C2Probe;
using Newtonsoft.Json.Linq;

internal static class AF2Probe
{
    private static int passed, failed;
    private static void Check(bool okay, string label)
    { Console.WriteLine((okay ? "PASS " : "FAIL ") + label); if (okay) passed++; else failed++; }
    private static HttpResults Folders(string suffix, bool more) => new HttpResults
    {
        code=200, status=true,
        results=new JObject { ["code"]=0, ["data"]=new JObject {
            ["default_folder"]=new JObject { ["folder_detail"]=new JObject { ["id"]="default-"+suffix, ["title"]="default-"+suffix } },
            ["space_infos"]=new JArray(
                new JObject { ["mediaListResponse"]=new JObject { ["list"]=new JArray(new JObject { ["id"]="folder-"+suffix, ["title"]="folder-"+suffix }), ["has_more"]=more } },
                new JObject { ["mediaListResponse"]=new JObject { ["list"]=new JArray(new JObject { ["id"]="collected-"+suffix }) } })
        }}.ToString()
    };
    private sealed class BindingObserver
    {
        public ObservableCollection<FavoriteItemModel> Items;
        public int Signals, CollectionSignals;
        public BindingObserver(MyFollowVideoVM vm)
        {
            vm.PropertyChanged += (_,e) => {
                if(e.PropertyName!="MyFavorite") return;
                if(Items!=null) Items.CollectionChanged-=Changed;
                Items=vm.MyFavorite;
                if(Items!=null) Items.CollectionChanged+=Changed;
                Signals++;
            };
        }
        private void Changed(object sender,NotifyCollectionChangedEventArgs e) { CollectionSignals++; }
    }
    private static async Task RefreshJourney()
    {
        FakeTransport.Reset(); var vm=new MyFollowVideoVM(); var binding=new BindingObserver(vm);int collectSignals=0;
        vm.PropertyChanged+=(_,e)=>{if(e.PropertyName=="CollectFavorite")collectSignals++;};
        FakeTransport.Delivery=_=>Task.FromResult(Folders("initial",true));await vm.LoadFavorite();var initial=binding.Items;
        Check(binding.Signals==1&&ReferenceEquals(initial,vm.MyFavorite)&&binding.Items.Select(x=>x.id).SequenceEqual(new[]{"default-initial","folder-initial"}),"initial load reaches bound folders with default first/source order");
        Check(binding.CollectionSignals==1,"initial default insertion reaches active collection listener");
        Check(collectSignals==1&&vm.CollectFavorite.Single().id=="collected-initial"&&vm.HasMore&&!vm.Loading,"initial collected/has_more/completion remain active");
        Check(FakeTransport.ActualRequests.Count==1&&FakeTransport.ActualRequests[0].baseUrl=="https://api.bilibili.com/x/v3/fav/folder/space/v2"&&!FakeTransport.ActualRequests[0].need_cookie&&FakeTransport.ActualRequests[0].parameter.Contains("&up_mid=dummy-uid&sign="),"initial actual app-family builder/route/identity/signing seam");
        var response=new TaskCompletionSource<HttpResults>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.PropertyChanged+=(_,e)=>{if(e.PropertyName=="Loading"&&!vm.Loading)completed.TrySetResult(true);};
        FakeTransport.Delivery=_=>response.Task; vm.RefreshCommand.Execute(null);
        Check(vm.Loading&&vm.MyFavorite==null&&vm.CollectFavorite==null&&FakeTransport.ActualRequests.Count==2,"actual RefreshCommand resets model and starts pending second request");
        Check(collectSignals==2,"existing CollectFavorite null-reset event unchanged");
        Check(binding.Items==null&&binding.Signals==2,"desired-safe pending refresh clears the bound MyFavorite reference");
        response.SetResult(Folders("updated",false));await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Check(vm.MyFavorite.Select(x=>x.id).SequenceEqual(new[]{"default-updated","folder-updated"})&&!ReferenceEquals(vm.MyFavorite,initial),"real full decoder/VM accepts replacement/default insertion order");
        Check(vm.CollectFavorite.Single().id=="collected-updated"&&collectSignals==3&&!vm.Loading&&!vm.HasMore&&FakeTransport.Toasts.Count==0,"collected replacement and successful completion preserved");
        Check(binding.Signals==3&&ReferenceEquals(binding.Items,vm.MyFavorite)&&binding.Items.Select(x=>x.id).SequenceEqual(new[]{"default-updated","folder-updated"}),"desired-safe refresh replacement reaches bound folder list; signals="+binding.Signals+" view="+(binding.Items==null?"null":binding.Items[1].id)+" model="+vm.MyFavorite[1].id);
    }
    private static async Task AppendControl()
    {
        FakeTransport.Reset();var vm=new MyFollowVideoVM();var binding=new BindingObserver(vm);
        FakeTransport.Delivery=_=>Task.FromResult(Folders("append-base",true));await vm.LoadFavorite();var original=vm.MyFavorite;var collected=vm.CollectFavorite;
        FakeTransport.Delivery=_=>Task.FromResult(new HttpResults {code=200,status=true,results="{\"code\":0,\"data\":{\"list\":[{\"id\":\"9007199254740993\"}],\"has_more\":false}}"});await vm.LoadCreateList();
        Check(ReferenceEquals(original,vm.MyFavorite)&&ReferenceEquals(binding.Items,original)&&binding.Signals==1&&binding.CollectionSignals==2&&binding.Items.Select(x=>x.id).SequenceEqual(new[]{"default-append-base","folder-append-base","9007199254740993"}),"positive same-reference append reaches bound collection/string IDs");
        Check(ReferenceEquals(collected,vm.CollectFavorite)&&!vm.HasMore&&!vm.Loading&&FakeTransport.Toasts.Count==0,"append preserves collected/state behavior");
        Check(FakeTransport.ActualRequests.Count==2&&FakeTransport.ActualRequests[1].baseUrl=="https://api.bilibili.com/x/v3/fav/folder/created/list"&&FakeTransport.ActualRequests[1].parameter.Contains("&pn=2&ps=20&sign="),"append actual Page2 route/order/signing seam");
    }
    public static async Task<int> Run()
    {
        ApiHelper.API_BASE_URL="https://api.bilibili.com";await RefreshJourney();await AppendControl();
        Console.WriteLine("AF2 passed="+passed+" failed="+failed);return failed==0?0:1;
    }
}

}
namespace OriginalFolderSafeProof {
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Modules;
using C2Probe;
using Newtonsoft.Json.Linq;
internal static class AF2SafeBoundaryProbe {
 static int passed,failed;
 static void Check(bool okay,string label){Console.WriteLine((okay?"PASS ":"FAIL ")+label);if(okay)passed++;else failed++;}
 static HttpResults Reply(JObject data)=>new HttpResults{code=200,status=true,results=new JObject{["code"]=0,["data"]=data}.ToString()};
 static JObject Data(string suffix,bool more)=>new JObject{
  ["default_folder"]=new JObject{["folder_detail"]=new JObject{["id"]="default-"+suffix}},
  ["space_infos"]=new JArray(new JObject{["mediaListResponse"]=new JObject{["list"]=new JArray(new JObject{["id"]="created-"+suffix}),["has_more"]=more}},new JObject{["mediaListResponse"]=new JObject{["list"]=new JArray(new JObject{["id"]="collected-"+suffix})}})
 };
 static int Page(MyFollowVideoVM vm)=>(int)typeof(MyFollowVideoVM).GetField("Page",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(vm);
 sealed class Observer{
  public ObservableCollection<FavoriteItemModel> Items;public int Signals,Changes;
  public Observer(MyFollowVideoVM vm){vm.PropertyChanged+=(_,e)=>{if(e.PropertyName!="MyFavorite")return;if(Items!=null)Items.CollectionChanged-=Changed;Items=vm.MyFavorite;if(Items!=null)Items.CollectionChanged+=Changed;Signals++;};}
  void Changed(object sender,System.Collections.Specialized.NotifyCollectionChangedEventArgs e){Changes++;}
 }
 static async Task<MyFollowVideoVM> Initial(){FakeTransport.Reset();var vm=new MyFollowVideoVM();FakeTransport.Delivery=_=>Task.FromResult(Reply(Data("initial",true)));await vm.LoadFavorite();return vm;}
 static async Task Exercise(string kind,bool refresh){
  FakeTransport.Reset();var vm=new MyFollowVideoVM();var view=new Observer(vm);FakeTransport.Delivery=_=>Task.FromResult(Reply(Data("initial",true)));await vm.LoadFavorite();var rows=vm.MyFavorite;var collected=vm.CollectFavorite;
  var bad=Data("partial",true);
  if(kind=="missing-default")bad["default_folder"]=new JObject();
  if(kind=="invalid-more")bad["space_infos"][0]["mediaListResponse"]["has_more"]="not-bool";
  if(kind=="invalid-collected")bad["space_infos"][1]["mediaListResponse"]["list"]="not-a-list";
  FakeTransport.Delivery=_=>Task.FromResult(Reply(bad));
  if(refresh){var context=new Completion();context.Start(()=>vm.RefreshCommand.Execute(null));await context.Done.Task.WaitAsync(TimeSpan.FromSeconds(5));Check(context.Errors.Count==0,kind+" refresh async-void settled without escaped faults");}else await vm.LoadFavorite();
  string label=kind+" "+(refresh?"refresh":"direct");
  Check(refresh?vm.MyFavorite==null&&vm.CollectFavorite==null:ReferenceEquals(vm.MyFavorite,rows)&&ReferenceEquals(vm.CollectFavorite,collected)&&rows.Select(x=>x.id).SequenceEqual(new[]{"default-initial","created-initial"}),label+" desired-safe response error publishes no partial folders");
  Check(refresh?view.Items==null:ReferenceEquals(view.Items,rows),label+" bound reference matches retained/reset state");
  Check(!vm.Loading&&!vm.HasMore&&Page(vm)==1&&FakeTransport.Toasts.Count==1,label+" precommit reset/Page/notice completion");
  FakeTransport.Delivery=_=>Task.FromResult(Reply(Data("retry",false)));await vm.LoadFavorite();
  Check(vm.MyFavorite.Select(x=>x.id).SequenceEqual(new[]{"default-retry","created-retry"})&&vm.CollectFavorite.Single().id=="collected-retry"&&!vm.Loading&&!vm.HasMore&&Page(vm)==2,label+" valid retry commits default/order/collected/state");
  Check(ReferenceEquals(view.Items,vm.MyFavorite)&&view.Items.Select(x=>x.id).SequenceEqual(new[]{"default-retry","created-retry"}),label+" valid retry reaches binding");
 }
 static async Task Append(){
  FakeTransport.Reset();var vm=new MyFollowVideoVM();var view=new Observer(vm);FakeTransport.Delivery=_=>Task.FromResult(Reply(Data("initial",true)));await vm.LoadFavorite();var rows=vm.MyFavorite;var collected=vm.CollectFavorite;
  FakeTransport.Delivery=_=>Task.FromResult(Reply(new JObject{["list"]=new JArray(new JObject{["id"]="new-row"}),["has_more"]="not-bool"}));await vm.LoadCreateList();
  Check(ReferenceEquals(rows,vm.MyFavorite)&&ReferenceEquals(collected,vm.CollectFavorite)&&rows.Select(x=>x.id).SequenceEqual(new[]{"default-initial","created-initial"})&&view.Changes==1,"append desired-safe invalid metadata appends no rows/events");
  Check(Page(vm)==2&&!vm.HasMore&&!vm.Loading&&FakeTransport.Toasts.Count==1,"append metadata failure preserves Page2/reset state/notice");
  FakeTransport.Delivery=_=>Task.FromResult(Reply(new JObject{["list"]=new JArray(new JObject{["id"]="new-row"}),["has_more"]=false}));await vm.LoadCreateList();
  Check(FakeTransport.ActualRequests.Skip(1).All(x=>x.parameter.Contains("&pn=2&ps=20"))&&Page(vm)==3&&rows.Select(x=>x.id).SequenceEqual(new[]{"default-initial","created-initial","new-row"}),"same-Page retry commits one row without rejected-response duplicate");
  Check(view.Signals==1&&view.Changes==2&&ReferenceEquals(view.Items,rows)&&ReferenceEquals(collected,vm.CollectFavorite),"append retry keeps bound identity and one accepted add");
 }
 public static async Task<int> Run(){ApiHelper.API_BASE_URL="https://api.bilibili.com";foreach(string kind in new[]{"missing-default","invalid-more","invalid-collected"})foreach(bool refresh in new[]{false,true})await Exercise(kind,refresh);await Append();Console.WriteLine("SAFE BOUNDARY passed="+passed+" failed="+failed);return failed==0?0:1;}
 sealed class Completion:SynchronizationContext{
  int active,pending;public readonly TaskCompletionSource<bool> Done=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);public readonly List<Exception> Errors=new List<Exception>();
  public override void OperationStarted()=>Interlocked.Increment(ref active);
  void Complete(){if(Volatile.Read(ref active)==0&&Volatile.Read(ref pending)==0)Done.TrySetResult(true);}
  public override void OperationCompleted(){Interlocked.Decrement(ref active);Complete();}
  public override void Post(SendOrPostCallback callback,object state){Interlocked.Increment(ref pending);ThreadPool.QueueUserWorkItem(_=>{var old=Current;SetSynchronizationContext(this);try{callback(state);}catch(Exception ex){lock(Errors)Errors.Add(ex);}finally{SetSynchronizationContext(old);Interlocked.Decrement(ref pending);Complete();}});}
  public void Start(Action action){var old=Current;SetSynchronizationContext(this);OperationStarted();try{action();}finally{OperationCompleted();SetSynchronizationContext(old);}}
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
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using BiliLite.Api;
    using BiliLite.Helpers;
    using BiliLite.Modules;
    using Newtonsoft.Json.Linq;

    internal static class FavoriteFolderPublicationProbe
    {
        private static int passed, failed;
        private const string Generic = "出现了一个未处理错误，已记录";
        private static void Check(bool condition, string label)
        {
            if (condition) passed++;
            else { failed++; Console.Error.WriteLine("FAIL folder publication: " + label); }
        }
        private static int Page(MyFollowVideoVM vm) => (int)typeof(MyFollowVideoVM)
            .GetField("Page", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(vm);
        private static string Ids(IEnumerable<FavoriteItemModel> rows)
            => rows == null ? "<null>" : string.Join(",", rows.Select(x => x == null ? "<null>" : x.id));
        private static HttpResults Reply(JObject data) => Raw(new JObject { ["code"] = 0, ["data"] = data }.ToString());
        private static HttpResults Raw(string body) => new HttpResults { code = 200, status = true, results = body };
        private static JObject Data(string suffix = "initial", JToken more = null)
            => new JObject {
                ["default_folder"] = new JObject { ["folder_detail"] = new JObject { ["id"] = "default-" + suffix, ["title"] = "default" } },
                ["space_infos"] = new JArray(
                    new JObject { ["mediaListResponse"] = new JObject { ["list"] = new JArray(
                        new JObject { ["id"] = "0009007199254740993", ["title"] = suffix, ["attr"] = 3 },
                        new JObject { ["id"] = "9007199254740995", ["title"] = "second" }), ["has_more"] = more ?? new JValue(true) } },
                    new JObject { ["mediaListResponse"] = new JObject { ["list"] = new JArray(
                        new JObject { ["id"] = "collected-" + suffix }, new JObject { ["id"] = "collected-second" }) } })
            };
        private static JObject Created(JObject data) => (JObject)data["space_infos"][0]["mediaListResponse"];
        private static JObject Collected(JObject data) => (JObject)data["space_infos"][1]["mediaListResponse"];
        private static TaskCompletionSource<HttpResults> Gate() => new TaskCompletionSource<HttpResults>(TaskCreationOptions.RunContinuationsAsynchronously);
        private static async Task Settled(Completion completion, string label)
        {
            await completion.Done.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Check(completion.Errors.Count == 0, label + " async-void settled with no escaped fault");
        }
        public static async Task Run()
        {
            var host = ApiHelper.API_BASE_URL;
            try
            {
                var original = await OriginalFolderProof.AF2Probe.Run();
                var safe = await OriginalFolderSafeProof.AF2SafeBoundaryProbe.Run();
                PropertyIdentity();
                await ValidOrderAndReorder();
                await OptionalAndCastCompatibility();
                await RepeatedRefreshAndLoading();
                await FailurePhasesAndRetry();
                await AppendPhasesAndRetry();
                Console.WriteLine("Folder publication added checks: passed=" + passed + " failed=" + failed);
                if (original + safe + failed != 0)
                    throw new InvalidOperationException("Folder publication failures: original=" + original + " safe=" + safe + " added=" + failed);
            }
            finally { ApiHelper.API_BASE_URL = host; FakeTransport.Reset(); }
        }
        private sealed class Observer
        {
            private readonly MyFollowVideoVM vm;
            private int depth;
            public ObservableCollection<FavoriteItemModel> Items { get; private set; }
            public int Replacements, Changes, Adds, Removes, Moves, MaxDepth, ExcessiveEcho;
            public readonly List<string> AtReplacement = new List<string>();
            public readonly List<string> CommitOrder = new List<string>();
            public readonly List<int> InsertIndexes = new List<int>();
            public Observer(MyFollowVideoVM source)
            {
                vm = source;
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == "HasMore" && vm.HasMore) CommitOrder.Add("more");
                    if (e.PropertyName == "CollectFavorite") CommitOrder.Add("collected@" + Page(vm));
                    if (e.PropertyName != "MyFavorite") return;
                    Replacements++; depth++; MaxDepth = Math.Max(MaxDepth, depth);
                    try
                    {
                        if (Items != null) Items.CollectionChanged -= Changed;
                        Items = vm.MyFavorite;
                        if (Items != null) Items.CollectionChanged += Changed;
                        AtReplacement.Add(Ids(Items)); CommitOrder.Add("created@" + Page(vm));
                        if (depth <= 8) vm.MyFavorite = vm.MyFavorite;
                        else ExcessiveEcho++;
                    }
                    finally { depth--; }
                };
            }
            private void Changed(object sender, NotifyCollectionChangedEventArgs e)
            {
                Changes++;
                if (e.Action == NotifyCollectionChangedAction.Add) { Adds++; InsertIndexes.Add(e.NewStartingIndex); CommitOrder.Add("add@" + e.NewStartingIndex); }
                if (e.Action == NotifyCollectionChangedAction.Remove) Removes++;
                if (e.Action == NotifyCollectionChangedAction.Move) Moves++;
            }
        }
        private static void PropertyIdentity()
        {
            var vm = new MyFollowVideoVM(); var view = new Observer(vm);
            Check(vm.MyFavorite == null && vm.MyFavorite == null && view.Replacements == 0, "fresh getter reads emit nothing");
            vm.MyFavorite = null; Check(view.Replacements == 0 && view.Items == null, "first null-to-null emits no event");
            var row = new FavoriteItemModel { id = "0009007199254740993", attr = 3 };
            var a = new ObservableCollection<FavoriteItemModel> { row }; vm.MyFavorite = a;
            Check(view.Replacements == 1 && ReferenceEquals(view.Items, a) && view.AtReplacement.Last() == row.id,
                "null-to-A stores before one inherited notification");
            var before = view.Replacements; vm.MyFavorite = a; var firstRead = vm.MyFavorite; var secondRead = vm.MyFavorite; vm.MyFavorite = a;
            Check(view.Replacements == before && ReferenceEquals(firstRead, a) && ReferenceEquals(secondRead, a)
                && ReferenceEquals(vm.MyFavorite[0], row), "same-reference no-op before/after getter reads");
            var b = new ObservableCollection<FavoriteItemModel> { row }; vm.MyFavorite = b;
            Check(view.Replacements == before + 1 && ReferenceEquals(view.Items, b) && view.AtReplacement.Last() == row.id,
                "distinct equal-content B publishes once");
            var changes = view.Changes; a.Add(new FavoriteItemModel { id = "stale" });
            Check(view.Changes == changes && Ids(view.Items) == row.id, "B publication detaches A");
            before = view.Replacements; b.Insert(0, new FavoriteItemModel { id = "new" }); b.Move(0, 1); b.RemoveAt(1);
            Check(view.Replacements == before && ReferenceEquals(view.Items, b) && view.Adds == 1 && view.Moves == 1 && view.Removes == 1
                && view.Changes == changes + 3 && Ids(view.Items) == row.id, "in-place Insert/Move/Remove retain binding and collection events");
            var itemEvents = new List<string>(); row.PropertyChanged += (_, e) => itemEvents.Add(e.PropertyName);
            row.media_count = 7; row.is_fav = true;
            Check(Ids(view.Items) == row.id && view.Items[0].media_count == 7 && row.privacy && row.fav_state == 1
                && itemEvents.SequenceEqual(new[] { "media_count", "is_fav", "fav_state" }) && view.Replacements == before,
                "real item updates notify items without replacing folder collection");
            vm.MyFavorite = null; Check(view.Replacements == before + 1 && view.Items == null, "nonnull-to-null clears once");
            changes = view.Changes; b.Add(row); Check(view.Changes == changes, "clear detaches previous collection");
            before = view.Replacements; vm.MyFavorite = null; var nullRead = vm.MyFavorite; vm.MyFavorite = null;
            Check(view.Replacements == before && nullRead == null, "repeated null/getter remains no-op");
            var c = new ObservableCollection<FavoriteItemModel> { row }; vm.MyFavorite = c;
            Check(view.Replacements == before + 1 && ReferenceEquals(view.Items, c) && Ids(view.Items) == row.id,
                "later C is not suppressed by prior echo/null history");
            Check(view.MaxDepth == 1 && view.ExcessiveEcho == 0, "bounded same-reference callback echo never recurses");
            var collectedEvents = 0; vm.PropertyChanged += (_, e) => { if (e.PropertyName == "CollectFavorite") collectedEvents++; };
            vm.CollectFavorite = null; vm.CollectFavorite = null; vm.CollectFavorite = c; vm.CollectFavorite = c;
            Check(collectedEvents == 4 && ReferenceEquals(vm.CollectFavorite, c), "CollectFavorite retains its unconditional setter notifications");
        }
        private static async Task Initial(MyFollowVideoVM vm, Observer view)
        {
            FakeTransport.Reset(); FakeTransport.Delivery = _ => Task.FromResult(Reply(Data())); await vm.LoadFavorite();
            Check(view.Replacements == 1 && ReferenceEquals(view.Items, vm.MyFavorite) && Ids(view.Items) == "default-initial,0009007199254740993,9007199254740995"
                && Ids(vm.CollectFavorite) == "collected-initial,collected-second" && Page(vm) == 2 && vm.HasMore && !vm.Loading,
                "actual initial folder/default/collected/state publication");
        }
        private static void Request(int page, string label)
        {
            Check(FakeTransport.ActualRequests.Count == 1, label + " exactly one request");
            var request = FakeTransport.ActualRequests.Single();
            Check(request.baseUrl == "https://api.bilibili.com/x/v3/fav/folder/" + (page == 0 ? "space/v2" : "created/list")
                && request.method == RestSharp.Method.Get && !request.need_cookie
                && request.parameter.Contains("&up_mid=" + SettingHelper.Account.UserID + (page == 0 ? "&sign=" : "&pn=" + page + "&ps=20&sign="))
                && request.parameter.EndsWith("&sign=dummy-sign"), label + " current app-family route/identity/numbered Page/ps/signing seam");
        }
        private static async Task ValidOrderAndReorder()
        {
            var vm = new MyFollowVideoVM(); var view = new Observer(vm); await Initial(vm, view);
            Check(view.AtReplacement.SequenceEqual(new[] { "0009007199254740993,9007199254740995" })
                && view.InsertIndexes.SequenceEqual(new[] { 0 })
                && view.CommitOrder.SequenceEqual(new[] { "created@1", "add@0", "more", "collected@2" }),
                "valid commit keeps created publication/default Insert/flag/Page/collected order");
            Request(0, "initial folders");
            var rows = vm.MyFavorite; var replacements = view.Replacements; var changes = view.Changes;
            rows.Move(2, 1);
            Check(ReferenceEquals(view.Items, rows) && Ids(view.Items) == "default-initial,9007199254740995,0009007199254740993"
                && view.Changes == changes + 1 && view.Moves == 1 && view.Replacements == replacements, "actual collection reorder reaches listener without replacement");
            FakeTransport.Reset(); FakeTransport.Delivery = _ => Task.FromResult(Raw("{\"code\":0}"));
            await vm.SortMyFavorite();
            var request = FakeTransport.ActualRequests.Single();
            Check(request.method == RestSharp.Method.Post && request.baseUrl == "https://api.bilibili.com/x/v3/fav/folder/sort"
                && request.body.Contains("&sort=default-initial,9007199254740995,0009007199254740993&sign=dummy-sign")
                && FakeTransport.Toasts.SequenceEqual(new[] { "排序成功" }), "offline unchanged sort preserves current visible ID order in real signed request");
        }
        private static async Task OptionalAndCastCompatibility()
        {
            (JToken Token, bool Expected)[] flags = { (new JValue(true), true), (new JValue(false), false),
                (new JValue(0), false), (new JValue(1), true), (new JValue(-1), true),
                (new JValue(0.0), false), (new JValue(1.5), true), (new JValue("true"), true), (new JValue("False"), false) };
            foreach (var flag in flags)
            {
                FakeTransport.Reset(); var vm = new MyFollowVideoVM(); var view = new Observer(vm);
                FakeTransport.Delivery = _ => Task.FromResult(Reply(Data("cast", flag.Token))); await vm.LoadFavorite();
                Check(vm.HasMore == flag.Expected && Page(vm) == 2 && !vm.Loading && view.Replacements == 1
                    && Ids(view.Items) == "default-cast,0009007199254740993,9007199254740995" && FakeTransport.Toasts.Count == 0,
                    "existing explicit bool cast accepted token " + flag.Token.Type + ":" + flag.Token);
            }
            foreach (var created in new[] { true, false })
            foreach (var collected in new[] { true, false })
            {
                var vm = new MyFollowVideoVM(); var view = new Observer(vm); await Initial(vm, view);
                var rows = vm.MyFavorite; var oldCollected = vm.CollectFavorite; var before = view.Replacements;
                var data = Data("optional", new JValue(false));
                if (!created) { ((JObject)data["space_infos"][0]).Remove("mediaListResponse"); data.Remove("default_folder"); }
                if (!collected) ((JObject)data["space_infos"][1]).Remove("mediaListResponse");
                FakeTransport.Reset(); FakeTransport.Delivery = _ => Task.FromResult(Reply(data)); await vm.LoadFavorite();
                Check((created ? !ReferenceEquals(vm.MyFavorite, rows) && Ids(view.Items) == "default-optional,0009007199254740993,9007199254740995"
                    : ReferenceEquals(vm.MyFavorite, rows) && ReferenceEquals(view.Items, rows)) && view.Replacements == before + (created ? 1 : 0)
                    && (collected ? !ReferenceEquals(vm.CollectFavorite, oldCollected) && Ids(vm.CollectFavorite) == "collected-optional,collected-second"
                        : ReferenceEquals(vm.CollectFavorite, oldCollected)) && Page(vm) == (created ? 2 : 1) && !vm.HasMore && !vm.Loading
                    && FakeTransport.Toasts.Count == 0, "CLR-absent branch compatibility created=" + created + " collected=" + collected);
            }
            foreach (var kind in new[] { "null-created-list", "null-default", "null-collected-list", "null-rows", "serialized-lists", "legacy-missing-code", "salvage" })
            {
                FakeTransport.Reset(); var vm = new MyFollowVideoVM(); var view = new Observer(vm); var data = Data(kind, new JValue(false));
                var expectedCreated = "default-" + kind + ",0009007199254740993,9007199254740995";
                var expectedCollected = "collected-" + kind + ",collected-second";
                if (kind == "null-created-list") { Created(data)["list"] = JValue.CreateNull(); expectedCreated = "default-" + kind; }
                if (kind == "null-default") { data["default_folder"]["folder_detail"] = JValue.CreateNull(); expectedCreated = "<null>,0009007199254740993,9007199254740995"; }
                if (kind == "null-collected-list") { Collected(data)["list"] = JValue.CreateNull(); expectedCollected = "<null>"; }
                if (kind == "null-rows") { ((JArray)Created(data)["list"]).Insert(0, JValue.CreateNull()); expectedCreated = "default-" + kind + ",<null>,0009007199254740993,9007199254740995"; }
                if (kind == "serialized-lists") { Created(data)["list"] = Created(data)["list"].ToString(); Collected(data)["list"] = Collected(data)["list"].ToString(); }
                var result = kind == "legacy-missing-code" ? Raw(new JObject { ["data"] = data }.ToString()) : Reply(data);
                if (kind == "salvage") result.results = "prefix " + result.results + " suffix";
                FakeTransport.Delivery = _ => Task.FromResult(result); await vm.LoadFavorite();
                Check(Ids(view.Items) == expectedCreated && Ids(vm.MyFavorite) == expectedCreated && Ids(vm.CollectFavorite) == expectedCollected
                    && view.Replacements == 1 && view.Adds == 1 && Page(vm) == 2 && !vm.HasMore && !vm.Loading && FakeTransport.Toasts.Count == 0,
                    "legacy accepted shape remains " + kind);
            }
            // On Refresh skipped branches retain its already-cleared references, not their pre-reset values.
            {
                var vm = new MyFollowVideoVM(); var view = new Observer(vm); await Initial(vm, view); var before = view.Replacements;
                var data = Data(); ((JObject)data["space_infos"][0]).Remove("mediaListResponse"); ((JObject)data["space_infos"][1]).Remove("mediaListResponse");
                FakeTransport.Reset(); FakeTransport.Delivery = _ => Task.FromResult(Reply(data)); var completion = new Completion(); completion.Start(vm.Refresh);
                await Settled(completion, "skipped-branch Refresh");
                Check(vm.MyFavorite == null && vm.CollectFavorite == null && view.Items == null && view.Replacements == before + 1
                    && Page(vm) == 1 && !vm.HasMore && !vm.Loading && FakeTransport.Toasts.Count == 0, "Refresh skipped branches retain prior caller null reset");
            }
        }
        private static async Task RepeatedRefreshAndLoading()
        {
            var vm = new MyFollowVideoVM(); var view = new Observer(vm); await Initial(vm, view);
            var collectedEvents = 0; vm.PropertyChanged += (_, e) => { if (e.PropertyName == "CollectFavorite") collectedEvents++; };
            for (var cycle = 0; cycle < 4; cycle++)
            {
                FakeTransport.Reset(); var old = vm.MyFavorite; var before = view.Replacements; var changes = view.Changes; var priorCollected = collectedEvents;
                var gate = Gate(); FakeTransport.Delivery = _ => gate.Task; var completion = new Completion();
                completion.Start(cycle % 2 == 0 ? () => vm.RefreshCommand.Execute(null) : vm.Refresh);
                Check(vm.Loading && !completion.Done.Task.IsCompleted && vm.MyFavorite == null && vm.CollectFavorite == null
                    && view.Items == null && view.Replacements == before + 1 && collectedEvents == priorCollected + 1
                    && Page(vm) == 1 && !vm.HasMore, "real repeated Refresh null reset reaches binding before Task.Run decode cycle=" + cycle);
                old.Add(new FavoriteItemModel { id = "stale" });
                Check(view.Items == null && view.Changes == changes, "pending refresh detaches prior collection cycle=" + cycle);
                completion.Start(() => { vm.RefreshCommand.Execute(null); vm.Refresh(); });
                Check(FakeTransport.ActualRequests.Count == 1 && vm.Loading && view.Replacements == before + 1
                    && collectedEvents == priorCollected + 1 && vm.MyFavorite == null, "Loading guard starts no request/reset/event cycle=" + cycle);
                gate.SetResult(Reply(Data("cycle-" + cycle, new JValue(false)))); await Settled(completion, "refresh cycle=" + cycle);
                Check(!vm.Loading && !vm.HasMore && Page(vm) == 2 && view.Replacements == before + 2 && ReferenceEquals(view.Items, vm.MyFavorite)
                    && Ids(view.Items) == "default-cycle-" + cycle + ",0009007199254740993,9007199254740995"
                    && Ids(vm.CollectFavorite) == "collected-cycle-" + cycle + ",collected-second" && collectedEvents == priorCollected + 2
                    && view.MaxDepth == 1 && view.ExcessiveEcho == 0, "replacement/default/collected repeated command reaches binding cycle=" + cycle);
                Request(0, "repeat refresh cycle=" + cycle);
            }
            FakeTransport.Reset(); var rows = vm.MyFavorite; var collected = vm.CollectFavorite; var replacements = view.Replacements;
            var page = Page(vm); var more = vm.HasMore; var events = collectedEvents; vm.Loading = true;
            var guard = new Completion(); guard.Start(() => { vm.RefreshCommand.Execute(null); vm.Refresh(); }); await Settled(guard, "idle-reference Loading guard");
            Check(vm.Loading && Page(vm) == page && vm.HasMore == more && ReferenceEquals(vm.MyFavorite, rows) && ReferenceEquals(vm.CollectFavorite, collected)
                && ReferenceEquals(view.Items, rows) && view.Replacements == replacements && collectedEvents == events
                && FakeTransport.ActualRequests.Count == 0 && FakeTransport.Toasts.Count == 0, "Loading guard preserves accepted references/state/notice without request");
            vm.Loading = false;
        }
        private static IEnumerable<(string Kind, HttpResults Response, string Notice)> FailureCases()
        {
            yield return ("null transport", null, Generic);
            yield return ("HTTP transport", new HttpResults { status = false, code = 503, message = "fixture offline" }, "fixture offline");
            yield return ("business", Raw("{\"code\":-403,\"message\":\"fixture denied\"}"), "fixture denied");
            yield return ("decoder", Raw("not JSON"), Generic);
            yield return ("null data", Raw("{\"code\":0,\"data\":null}"), Generic);
            foreach (var kind in new[] { "created-decode", "missing-default", "invalid-more", "null-more", "collected-decode", "created-JSON-null", "collected-JSON-null", "missing-created-list" })
            {
                var data = Data("rejected");
                switch (kind)
                {
                    case "created-decode": Created(data)["list"] = "not-a-list"; break;
                    case "missing-default": data["default_folder"] = new JObject(); break;
                    case "invalid-more": Created(data)["has_more"] = "not-bool"; break;
                    case "null-more": Created(data)["has_more"] = JValue.CreateNull(); break;
                    case "collected-decode": Collected(data)["list"] = "not-a-list"; break;
                    case "created-JSON-null": data["space_infos"][0]["mediaListResponse"] = JValue.CreateNull(); break;
                    case "collected-JSON-null": data["space_infos"][1]["mediaListResponse"] = JValue.CreateNull(); break;
                    case "missing-created-list": Created(data).Remove("list"); break;
                }
                yield return (kind, Reply(data), Generic);
            }
        }
        private static async Task FailurePhasesAndRetry()
        {
            foreach (var test in FailureCases())
            foreach (var mode in new[] { "fresh", "direct", "refresh" })
            {
                var vm = new MyFollowVideoVM(); var view = new Observer(vm);
                if (mode != "fresh") await Initial(vm, view);
                var rows = vm.MyFavorite; var collected = vm.CollectFavorite; var rowObjects = rows?.ToArray(); var collectedObjects = collected?.ToArray();
                var before = view.Replacements; var changes = view.Changes; var collectEvents = 0;
                vm.PropertyChanged += (_, e) => { if (e.PropertyName == "CollectFavorite") collectEvents++; };
                FakeTransport.Reset(); var gate = Gate(); FakeTransport.Delivery = _ => gate.Task; Completion completion = null; Task direct = null;
                if (mode == "refresh") { completion = new Completion(); completion.Start(() => vm.RefreshCommand.Execute(null)); }
                else direct = vm.LoadFavorite();
                var reset = mode == "refresh";
                Check(vm.Loading && Page(vm) == 1 && !vm.HasMore && (reset ? vm.MyFavorite == null && vm.CollectFavorite == null && view.Items == null
                        : ReferenceEquals(vm.MyFavorite, rows) && ReferenceEquals(vm.CollectFavorite, collected) && ReferenceEquals(view.Items, rows))
                    && view.Replacements == before + (reset && rows != null ? 1 : 0), test.Kind + " " + mode + " pre-response phase/reset");
                gate.SetResult(test.Response);
                if (completion != null) await Settled(completion, test.Kind + " " + mode); else await direct.WaitAsync(TimeSpan.FromSeconds(5));
                var retained = reset ? vm.MyFavorite == null && vm.CollectFavorite == null && view.Items == null
                    : ReferenceEquals(vm.MyFavorite, rows) && ReferenceEquals(vm.CollectFavorite, collected) && ReferenceEquals(view.Items, rows)
                        && (rows == null || rows.SequenceEqual(rowObjects)) && (collected == null || collected.SequenceEqual(collectedObjects));
                Check(retained && view.Replacements == before + (reset && rows != null ? 1 : 0) && view.Changes == changes
                    && collectEvents == (reset ? 1 : 0), test.Kind + " " + mode + " failed validation publishes no response references/default/events");
                Check(!vm.Loading && !vm.HasMore && Page(vm) == 1 && FakeTransport.Toasts.SequenceEqual(new[] { test.Notice }),
                    test.Kind + " " + mode + " failure completion/Page/reset/notice");
                Request(0, test.Kind + " " + mode);
                FakeTransport.Reset(); var retryBefore = view.Replacements; var retryChanges = view.Changes; var retryCollect = collectEvents;
                FakeTransport.Delivery = _ => Task.FromResult(Reply(Data("retry", new JValue(false)))); await vm.LoadFavorite();
                Check(!vm.Loading && !vm.HasMore && Page(vm) == 2 && ReferenceEquals(view.Items, vm.MyFavorite)
                    && Ids(view.Items) == "default-retry,0009007199254740993,9007199254740995"
                    && Ids(vm.CollectFavorite) == "collected-retry,collected-second" && view.Replacements == retryBefore + 1
                    && view.Changes == retryChanges + 1 && collectEvents == retryCollect + 1 && FakeTransport.Toasts.Count == 0,
                    test.Kind + " " + mode + " valid retry commits one created/default/collected publication");
                Request(0, test.Kind + " " + mode + " retry");
            }
        }
        private static IEnumerable<(string Kind, HttpResults Response, string Notice)> AppendFailureCases()
        {
            yield return ("null transport", null, Generic);
            yield return ("HTTP transport", new HttpResults { status = false, message = "fixture offline" }, "fixture offline");
            yield return ("business", Raw("{\"code\":-403,\"message\":\"fixture denied\"}"), "fixture denied");
            yield return ("decoder", Raw("not JSON"), Generic);
            yield return ("list decode", Reply(new JObject { ["list"] = "not-a-list", ["has_more"] = false }), Generic);
            yield return ("null list", Reply(new JObject { ["list"] = JValue.CreateNull(), ["has_more"] = false }), Generic);
            yield return ("late flag", Reply(new JObject { ["list"] = new JArray(new JObject { ["id"] = "retry-row" }), ["has_more"] = "not-bool" }), Generic);
        }
        private static async Task AppendPhasesAndRetry()
        {
            foreach (var incomingLoading in new[] { false, true })
            foreach (var test in AppendFailureCases())
            {
                var vm = new MyFollowVideoVM(); var view = new Observer(vm); await Initial(vm, view);
                var rows = vm.MyFavorite; var collected = vm.CollectFavorite; var accepted = rows.ToArray(); var before = view.Replacements; var changes = view.Changes;
                vm.Loading = incomingLoading; FakeTransport.Reset(); var gate = Gate(); FakeTransport.Delivery = _ => gate.Task;
                var completion = new Completion(); completion.Start(() => vm.LoadMoreCommand.Execute(null));
                Check(!completion.Done.Task.IsCompleted && vm.Loading == incomingLoading && Page(vm) == 2 && !vm.HasMore
                    && ReferenceEquals(view.Items, rows) && view.Replacements == before && view.Changes == changes,
                    test.Kind + " append pending retains Loading owner/reference L=" + incomingLoading);
                gate.SetResult(test.Response); await Settled(completion, test.Kind + " append");
                Check(ReferenceEquals(vm.MyFavorite, rows) && ReferenceEquals(view.Items, rows) && rows.SequenceEqual(accepted)
                    && ReferenceEquals(vm.CollectFavorite, collected) && Page(vm) == 2 && !vm.HasMore && vm.Loading == incomingLoading
                    && view.Replacements == before && view.Changes == changes && FakeTransport.Toasts.SequenceEqual(new[] { test.Notice }),
                    test.Kind + " append rejected before any Add/Page/flag publication L=" + incomingLoading);
                Request(2, test.Kind + " append");
                FakeTransport.Reset(); FakeTransport.Delivery = _ => Task.FromResult(Reply(new JObject {
                    ["list"] = new JArray(new JObject { ["id"] = "retry-row" }), ["has_more"] = false }));
                var retry = new Completion(); retry.Start(() => vm.LoadMoreCommand.Execute(null)); await Settled(retry, test.Kind + " append retry");
                Check(ReferenceEquals(vm.MyFavorite, rows) && ReferenceEquals(view.Items, rows) && ReferenceEquals(vm.CollectFavorite, collected)
                    && rows.Take(accepted.Length).SequenceEqual(accepted) && Ids(rows) == "default-initial,0009007199254740993,9007199254740995,retry-row"
                    && Page(vm) == 3 && !vm.HasMore && vm.Loading == incomingLoading && view.Replacements == before && view.Changes == changes + 1
                    && FakeTransport.Toasts.Count == 0, test.Kind + " same-pn2 retry adds exactly one accepted row and advances once L=" + incomingLoading);
                Request(2, test.Kind + " append retry");
            }
            foreach (var flag in new (JToken Token, bool Expected)[] { (new JValue(1), true), (new JValue("False"), false) })
            {
                var vm = new MyFollowVideoVM(); var view = new Observer(vm); await Initial(vm, view);
                var rows = vm.MyFavorite; var collected = vm.CollectFavorite; var before = view.Replacements; var changes = view.Changes;
                FakeTransport.Reset(); FakeTransport.Delivery = _ => Task.FromResult(Reply(new JObject {
                    ["list"] = new JArray(JValue.CreateNull(), new JObject { ["id"] = "0009007199254740993" }, new JObject { ["id"] = "tail" }),
                    ["has_more"] = flag.Token })); await vm.LoadCreateList();
                Check(ReferenceEquals(view.Items, rows) && ReferenceEquals(vm.CollectFavorite, collected) && view.Replacements == before
                    && view.Changes == changes + 3 && Ids(view.Items) == "default-initial,0009007199254740993,9007199254740995,<null>,0009007199254740993,tail"
                    && Page(vm) == 3 && vm.HasMore == flag.Expected && !vm.Loading && FakeTransport.Toasts.Count == 0,
                    "valid append retains accepted null/duplicate/order/bool-cast policy " + flag.Token);
            }
        }
        // Complete actual Task.Run helper continuations and async-void commands without sleeps.
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

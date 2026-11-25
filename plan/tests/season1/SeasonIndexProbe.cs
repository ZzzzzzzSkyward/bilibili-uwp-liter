using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using BiliLite.Api;
using BiliLite.Controls;
using BiliLite.Helpers;
using BiliLite.Modules;
using BiliLite.Pages.Bangumi;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Complete real index VM/page/grid execute below. This facade only records requests and
// delivers synthetic responses. Actual routes/app signing are checked independently.
namespace BiliLite.Api
{
    public sealed class SeasonIndexAPI
    {
        public sealed class Call
        {
            public bool IsCondition;
            public int Page;
            public int SeasonType;
            public int PageSize;
            public string Filters;
        }
        public static readonly List<Call> Calls = new List<Call>();
        public static Func<Call, Task<HttpResults>> Send;
        public ApiModel Condition(int season_type)
        {
            return Build(new Call { IsCondition = true, SeasonType = season_type });
        }
        public ApiModel Result(int page, int season_type, string condition, int pagesize = 24)
        {
            return Build(new Call { Page = page, SeasonType = season_type, Filters = condition, PageSize = pagesize });
        }
        private static ApiModel Build(Call call)
        {
            Calls.Add(call);
            return new ApiModel { Handler = () => Send(call) };
        }
    }
}

namespace Season1Probe
{
    internal static class SeasonIndexChecks
    {
        private static int checks, failures;
        private const string Good = "{\"code\":0,\"data\":{\"filter\":[{\"field\":\"area\",\"name\":\"地区\",\"values\":[{\"keyword\":\"-1\",\"name\":\"全部\"}]}],\"order\":[{\"field\":\"3\",\"name\":\"追番人数\"},{\"field\":\"4\",\"name\":\"最高评分\"}]}}";
        private static string Rows(int id) => "{\"code\":0,\"data\":{\"list\":[{\"season_id\":" + id + ",\"title\":\"synthetic season\",\"order\":\"42\",\"order_type\":\"follow\"}],\"has_next\":1}}";
        private static HttpResults Reply(string body, bool ok = true) => new HttpResults { code = ok ? 200 : 503, status = ok, results = body, message = ok ? "" : "synthetic transport" };
        private static int C => SeasonIndexAPI.Calls.Count(a => a.IsCondition);
        private static int R => SeasonIndexAPI.Calls.Count(a => !a.IsCondition);
        private static void Check(bool ok, string name)
        {
            checks++;
            if (!ok) failures++;
            Console.WriteLine((ok ? "PASS index " : "FAIL index ") + name);
        }
        private static void Reset()
        {
            SeasonIndexAPI.Calls.Clear(); Utils.Toasts.Clear(); LogHelper.Entries.Clear();
            SeasonIndexAPI.Send = a => Task.FromResult(Reply(a.IsCondition ? Good : Rows(101)));
        }
        private static SeasonIndexVM Vm(AnimeIndexPage page) => (SeasonIndexVM)typeof(AnimeIndexPage).GetField("seasonIndexVM", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(page);
        private static void Navigate(AnimeIndexPage page, object parameter = null) => typeof(AnimeIndexPage).GetMethod("OnNavigatedTo", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(page, new object[] { new NavigationEventArgs { NavigationMode = NavigationMode.New, Parameter = parameter } });
        private static void Selection(AnimeIndexPage page, SeasonIndexVM vm) => typeof(AnimeIndexPage).GetMethod("ComboBox_SelectionChanged", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(page, new object[] { new ComboBox { SelectedItem = vm.Conditions[0].current }, new SelectionChangedEventArgs() });
        private static async Task Wait(Func<bool> done)
        {
            for (int i = 0; i < 1000 && !done(); i++) await Task.Delay(1);
            if (!done()) throw new Exception("index completion timeout");
        }
        private static async Task Case(Func<Task> action, string name)
        {
            Reset();
            try { await action(); }
            catch (Exception ex) { Check(false, name + " threw " + ex.GetBaseException().Message); }
        }
        private static async Task<SeasonIndexVM> Ready()
        {
            var vm = new SeasonIndexVM { Parameter = new SeasonIndexParameter() };
            await vm.LoadConditions();
            return vm;
        }
        internal static async Task<bool> Run()
        {
            await Case(FreshNavigation, "fresh navigation");
            await Case(NotReady, "not ready");
            await Case(HeldConditions, "held conditions");
            await Case(ConditionsPublication, "conditions publication");
            await Case(ConditionFailures, "condition retry");
            await Case(UnusableMetadata, "unusable metadata");
            await Case(StructuralReadiness, "structural readiness");
            await Case(ResultNotification, "result notification");
            await Case(HeldResult, "held result");
            await Case(GridCompletion, "grid completion");
            await Case(Compatibility, "result compatibility");
            await Case(OrderCodePriority, "order code priority");
            await Case(TerminalPages, "integer-zero terminal pages");
            await Case(OptionalFlagCompatibility, "optional terminal flag compatibility");
            await Case(TerminalGridCompletion, "terminal actual-grid completion");
            await Case(RejectedZeroRetry, "unaccepted terminal flags");
            Console.WriteLine("INDEX CHECKS=" + checks + " FAILURES=" + failures);
            return failures == 0;
        }
        private static async Task FreshNavigation()
        {
            // No public not-ready warm-up, lifecycle flag assignment or private-field mutation.
            var page = new AnimeIndexPage();
            var vm = Vm(page);
            Check(!vm.Loading && !vm.ConditionsLoading && vm.Parameter == null && vm.Conditions == null && vm.Page == 1, "constructor owns neither request");
            int publications = 0;
            vm.PropertyChanged += (s, e) => { if (e.PropertyName == "Conditions") { publications++; Selection(page, vm); } };
            Navigate(page);
            await Wait(() => !vm.Loading && !vm.ConditionsLoading);
            Check(C == 1 && R == 1 && publications == 1, "true constructor navigation requests each endpoint once");
            Check(vm.Result?.Count == 1 && vm.Result[0].season_id == 101 && vm.Page == 2, "true constructor navigation publishes first page");
            Check(Utils.Toasts.Count == 0, "fresh navigation and actual synchronous selection add no errors");
            var call = SeasonIndexAPI.Calls.Last();
            Check(call.Page == 1 && call.PageSize == 24 && call.SeasonType == 1 && call.Filters == "&order=3&area=-1&sort=0", "first request retains exact filter order/default/page contract");
        }

        private static async Task NotReady()
        {
            var vm = new SeasonIndexVM { Page = 4 };
            int notifications = 0;
            vm.PropertyChanged += (s, e) => notifications++;
            await vm.LoadConditions(); await vm.LoadResult(); vm.RefreshCommand.Execute(null); vm.LoadMoreCommand.Execute(null);
            Check(C == 0 && R == 0 && vm.Page == 4, "null Parameter rejects every direct and command entry before page movement");
            Check(notifications == 0 && Utils.Toasts.Count == 0 && !vm.Loading && !vm.ConditionsLoading, "null Parameter causes no notifications, owner or error");
            vm.Parameter = new SeasonIndexParameter();
            await vm.LoadResult(); vm.LoadMoreCommand.Execute(null);
            Check(C == 0 && R == 0 && vm.Page == 4 && vm.Result == null, "missing Conditions preserves rows/page and dispatches nothing");
            Check(notifications == 0 && Utils.Toasts.Count == 0, "missing Conditions rejects before acquisition and notification");
        }

        private static async Task HeldConditions()
        {
            var page = new AnimeIndexPage(); var vm = Vm(page);
            var gate = new TaskCompletionSource<HttpResults>();
            SeasonIndexAPI.Send = a => a.IsCondition ? gate.Task : Task.FromResult(Reply("{\"code\":0,\"data\":{\"list\":[]}}"));
            int publications = 0, resultAcquisitions = 0, notifications = 0;
            vm.PropertyChanged += (s, e) =>
            {
                notifications++;
                if (e.PropertyName == "Conditions") { publications++; Selection(page, vm); }
                if (e.PropertyName == "Loading" && vm.Loading) resultAcquisitions++;
            };
            Navigate(page);
            Check(C == 1 && R == 0 && vm.ConditionsLoading && !vm.Loading, "navigation holds only the conditions owner");
            vm.Page = 4;
            int before = notifications;
            var duplicate = vm.LoadConditions(); vm.RefreshCommand.Execute(null); vm.LoadMoreCommand.Execute(null); await vm.LoadResult();
            Check(duplicate.IsCompleted && C == 1 && R == 0, "held conditions reject direct duplicate and every command");
            Check(vm.Page == 4 && notifications == before && resultAcquisitions == 0 && Utils.Toasts.Count == 0, "rejected conditions conflicts are entirely quiet");
            Check(vm.ConditionsLoading && !vm.Loading, "non-owner never releases conditions ownership");
            vm.Page = 1;
            gate.SetResult(Reply(Good)); await duplicate;
            await Wait(() => !vm.ConditionsLoading && !vm.Loading);
            Check(C == 1 && R == 1 && publications == 1, "navigation continues once after sole conditions publication");
            Check(resultAcquisitions == 1 && Utils.Toasts.SequenceEqual(new[] { "加载完了" }), "selection during metadata publication creates no premature owner or error");
        }

        private static async Task ConditionsPublication()
        {
            var page = new AnimeIndexPage(); var vm = Vm(page); vm.Parameter = new SeasonIndexParameter(); vm.Page = 4;
            bool ownedAtTrue = false, ownedAtPublication = false; int snapshots = 0;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "ConditionsLoading" && vm.ConditionsLoading && !ownedAtTrue)
                {
                    ownedAtTrue = true;
                    vm.LoadConditions().GetAwaiter().GetResult(); vm.RefreshCommand.Execute(null); vm.LoadMoreCommand.Execute(null); vm.LoadResult().GetAwaiter().GetResult();
                }
                if (e.PropertyName == "Conditions" && !ownedAtPublication)
                {
                    ownedAtPublication = vm.ConditionsLoading;
                    int calls = C + R;
                    Selection(page, vm); vm.RefreshCommand.Execute(null); vm.LoadMoreCommand.Execute(null);
                    vm.LoadConditions().GetAwaiter().GetResult(); vm.LoadResult().GetAwaiter().GetResult();
                    if (C + R == calls && vm.Page == 4) snapshots++;
                }
            };
            await vm.LoadConditions();
            Check(ownedAtTrue && ownedAtPublication && snapshots == 1, "conditions owner spans true notification and actual synchronous selection publication");
            Check(C == 1 && R == 0 && vm.Page == 4 && !vm.ConditionsLoading && !vm.Loading, "direct LoadConditions publishes metadata without starting results");
            Check(Utils.Toasts.Count == 0, "all publication reentry remains quiet");
        }

        private static async Task ConditionFailures()
        {
            foreach (string kind in new[] { "business", "transport", "malformed" })
            {
                Reset(); var page = new AnimeIndexPage(); var vm = Vm(page); int publications = 0;
                vm.PropertyChanged += (s, e) => { if (e.PropertyName == "Conditions") { publications++; Selection(page, vm); } };
                var failed = kind == "business" ? Reply("{\"code\":-400,\"message\":\"synthetic refusal\"}") : kind == "transport" ? Reply("", false) : Reply("{\"code\":0,\"data\":{\"filter\":{},\"order\":[]}}");
                SeasonIndexAPI.Send = a => Task.FromResult(failed);
                Navigate(page); await Wait(() => !vm.ConditionsLoading && !vm.Loading);
                Check(C == 1 && R == 0 && vm.Conditions == null && vm.Result == null, kind + " conditions failure settles without result");
                Check(Utils.Toasts.Count == (kind == "transport" ? 0 : 1), kind + " retains current notice count");
                var gate = new TaskCompletionSource<HttpResults>();
                SeasonIndexAPI.Send = a => a.IsCondition ? Task.FromResult(Reply(Good)) : gate.Task;
                vm.RefreshCommand.Execute(null);
                Check(C == 2 && R == 1 && publications == 1 && vm.Loading && !vm.ConditionsLoading, kind + " Refresh retries conditions once then owns one result");
                gate.SetResult(Reply(Rows(201))); await Wait(() => !vm.Loading);
                Check(vm.Result?.Count == 1 && vm.Page == 2, kind + " retry publishes one first page");
            }
            Reset(); var failedVm = new SeasonIndexVM { Parameter = new SeasonIndexParameter(), Page = 4 };
            SeasonIndexAPI.Send = a => Task.FromResult(Reply("", false));
            failedVm.RefreshCommand.Execute(null);
            Check(C == 1 && R == 0 && failedVm.Page == 1 && !failedVm.ConditionsLoading && !failedVm.Loading, "failed Refresh attempts metadata once with no automatic loop");
            failedVm.RefreshCommand.Execute(null);
            Check(C == 2 && R == 0, "next separately invoked Refresh may retry metadata once again");
        }

        private static async Task UnusableMetadata()
        {
            var page = new AnimeIndexPage(); var vm = Vm(page); int acquisitions = 0;
            vm.PropertyChanged += (s, e) => { if (e.PropertyName == "Loading" && vm.Loading) acquisitions++; };
            SeasonIndexAPI.Send = a => Task.FromResult(Reply(Good.Replace("\"keyword\":\"-1\"", "\"keyword\":\"0\"")));
            Navigate(page); await Wait(() => !vm.ConditionsLoading && !vm.Loading);
            Check(vm.Conditions != null && vm.Conditions.Any(a => a.current == null) && R == 0, "decoder retains unmatched selection without fallback");
            Check(acquisitions == 0 && Utils.Toasts.Count == 0, "unmatched selection declines before result ownership/error");
            SeasonIndexAPI.Calls.Clear();
            var gate = new TaskCompletionSource<HttpResults>();
            SeasonIndexAPI.Send = a => a.IsCondition ? Task.FromResult(Reply(Good)) : gate.Task;
            vm.RefreshCommand.Execute(null);
            Check(C == 1 && R == 1 && vm.Loading, "Refresh reloads unusable metadata once then starts one usable result");
            gate.SetResult(Reply(Rows(301))); await Wait(() => !vm.Loading);
            Check(vm.Result?.Count == 1 && vm.Page == 2 && acquisitions == 1, "usable metadata retry publishes once");
        }

        private static async Task StructuralReadiness()
        {
            var invalid = new ObservableCollection<SeasonIndexConditionFilterModel>[]
            {
                null,
                new ObservableCollection<SeasonIndexConditionFilterModel>(),
                new ObservableCollection<SeasonIndexConditionFilterModel> { null },
                new ObservableCollection<SeasonIndexConditionFilterModel> { new SeasonIndexConditionFilterModel { field = "", current = new SeasonIndexConditionFilterItemModel { keyword = "" } } },
                new ObservableCollection<SeasonIndexConditionFilterModel> { new SeasonIndexConditionFilterModel { field = null, current = new SeasonIndexConditionFilterItemModel { keyword = "" } } },
                new ObservableCollection<SeasonIndexConditionFilterModel> { new SeasonIndexConditionFilterModel { field = "area", current = null } },
                new ObservableCollection<SeasonIndexConditionFilterModel> { new SeasonIndexConditionFilterModel { field = "area", current = new SeasonIndexConditionFilterItemModel { keyword = null } } },
            };
            foreach (var conditions in invalid)
            {
                Reset(); var vm = new SeasonIndexVM { Parameter = new SeasonIndexParameter(), Page = 4, Conditions = conditions };
                int notices = 0; vm.PropertyChanged += (s, e) => notices++;
                await vm.LoadResult(); vm.LoadMoreCommand.Execute(null);
                Check(R == 0 && vm.Page == 4 && notices == 0 && Utils.Toasts.Count == 0 && !vm.Loading, "structurally unusable metadata rejects quietly " + (conditions == null ? "null" : conditions.Count.ToString()));
            }
            Reset(); var valid = new SeasonIndexVM
            {
                Parameter = new SeasonIndexParameter(),
                Conditions = new ObservableCollection<SeasonIndexConditionFilterModel>
                {
                    new SeasonIndexConditionFilterModel { field = "unknown_field", current = new SeasonIndexConditionFilterItemModel { keyword = "" } },
                    new SeasonIndexConditionFilterModel { field = "area", current = new SeasonIndexConditionFilterItemModel { keyword = "中文 + % &x=1" } }
                }
            };
            await valid.LoadResult();
            Check(R == 1 && valid.Page == 2 && Utils.Toasts.Count == 0, "unknown nonempty field and empty keyword remain usable");
            Check(SeasonIndexAPI.Calls.Single().Filters == "&unknown_field=&area=" + Uri.EscapeDataString("中文 + % &x=1") + "&sort=0", "accepted filter values retain exact existing escaping");
        }
        private static async Task ResultNotification()
        {
            var vm = await Ready();
            var gate = new TaskCompletionSource<HttpResults>();
            SeasonIndexAPI.Send = a => gate.Task;
            bool once = false, ownerAtReset = false, ownerAtTrue = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Loading" && vm.Loading && !ownerAtTrue)
                {
                    ownerAtTrue = true;
                    vm.LoadResult().GetAwaiter().GetResult(); vm.LoadConditions().GetAwaiter().GetResult();
                    vm.RefreshCommand.Execute(null); vm.LoadMoreCommand.Execute(null);
                }
                if (e.PropertyName == "Result" && !once)
                {
                    once = true; ownerAtReset = vm.Loading;
                    vm.RefreshCommand.Execute(null); vm.LoadMoreCommand.Execute(null);
                    vm.LoadResult().GetAwaiter().GetResult(); vm.LoadConditions().GetAwaiter().GetResult();
                }
            };
            var pending = vm.LoadResult();
            Check(once && ownerAtTrue && ownerAtReset, "result owner acquired before Loading=true and Result=null notifications");
            Check(C == 1 && R == 1 && vm.Loading && vm.Page == 1, "all synchronous result observers reject conflicting entries");
            gate.SetResult(Reply(Rows(401))); await pending;
            Check(vm.Result?.Count == 1 && vm.Page == 2 && !vm.Loading && Utils.Toasts.Count == 0, "reentrant first-page reset cannot duplicate publication");
        }

        private static async Task HeldResult()
        {
            var vm = await Ready();
            var gate = new TaskCompletionSource<HttpResults>(); SeasonIndexAPI.Send = a => gate.Task;
            var pending = vm.LoadResult();
            int notifications = 0; vm.PropertyChanged += (s, e) => notifications++;
            var rows = vm.Result;
            var duplicate = vm.LoadResult(); var conditions = vm.LoadConditions(); vm.RefreshCommand.Execute(null); vm.LoadMoreCommand.Execute(null);
            Check(duplicate.IsCompleted && conditions.IsCompleted && C == 1 && R == 1 && vm.Loading && !vm.ConditionsLoading, "held result keeps sole ownership after direct/command duplicates");
            Check(vm.Page == 1 && ReferenceEquals(rows, vm.Result) && notifications == 0 && Utils.Toasts.Count == 0, "held-result rejected calls preserve all observable state");
            var third = vm.LoadResult();
            Check(third.IsCompleted && R == 1 && vm.Loading && notifications == 0, "third direct result cannot exploit another invocation's finally");
            gate.SetResult(Reply(Rows(402))); await pending; await duplicate; await conditions; await third;
            Check(vm.Result?.Count == 1 && vm.Page == 2 && !vm.Loading, "sole held owner publishes one row and advances once");
        }

        private static async Task GridCompletion()
        {
            var page = new AnimeIndexPage(); var vm = Vm(page);
            var first = new TaskCompletionSource<HttpResults>(); var append = new TaskCompletionSource<HttpResults>();
            SeasonIndexAPI.Send = a => a.IsCondition ? Task.FromResult(Reply(Good)) : a.Page == 1 ? first.Task : append.Task;
            var grid = new MyAdaptiveGridView { LoadMoreCommand = vm.LoadMoreCommand, CanLoadMore = true };
            typeof(MyAdaptiveGridView).GetMethod("OnApplyTemplate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(grid, null);
            vm.PropertyChanged += (s, e) => { if (e.PropertyName == "Loading") grid.Loading = vm.Loading; };
            grid.Loading = vm.Loading;
            Check(C == 0 && R == 0, "actual grid idle callback before navigation cannot append");
            Navigate(page);
            Check(C == 1 && R == 1 && vm.Loading, "true constructor navigation holds first result with actual grid attached");
            first.SetResult(Reply(Rows(501)));
            await Wait(() => R >= 2);
            var pages = SeasonIndexAPI.Calls.Where(a => !a.IsCondition).Select(a => a.Page).ToArray();
            Check(pages.SequenceEqual(new[] { 1, 2 }) && vm.Page == 2 && vm.Result?.Count == 1 && vm.Loading, "real grid completion acquires page2 after row/Page commit");
            await vm.LoadResult();
            Check(vm.Loading && R == 2, "old completion and rejected append cannot clear the new page2 owner");
            grid.TemplateScrollViewer.ScrollableHeight = 100;
            append.SetResult(Reply(Rows(502))); await Wait(() => !vm.Loading);
            Check(R == 2 && vm.Page == 3 && vm.Result.Count == 2 && vm.Result[0].season_id == 501 && vm.Result[1].season_id == 502, "filled viewport completes ordered append and stops autofill");
        }

        private static async Task Compatibility()
        {
            var vm = await Ready();
            Check(vm.Conditions[0].field == "order" && vm.Conditions[0].current.keyword == "3" && vm.Conditions[1].current.keyword == "-1", "existing default order/filter selection retained");
            await vm.LoadResult();
            Check(vm.Result?.Count == 1 && vm.Result[0].order == "42" && vm.Result[0].order_type == "follow" && vm.Page == 2, "legacy data.list deserialization/casts retained");
            var collection = vm.Result;
            SeasonIndexAPI.Send = a => Task.FromResult(Reply(Rows(102)));
            vm.LoadMoreCommand.Execute(null);
            Check(R == 2 && ReferenceEquals(collection, vm.Result) && vm.Page == 3 && vm.Result.Select(a => a.season_id).SequenceEqual(new[] { 101, 102 }), "nonempty LoadMore appends in wire order and advances once");
            foreach (var failure in new[] { "business", "transport", "malformed" })
            {
                int before = R; Utils.Toasts.Clear();
                SeasonIndexAPI.Send = a => Task.FromResult(failure == "business" ? Reply("{\"code\":-400,\"message\":\"synthetic refusal\"}") : failure == "transport" ? Reply("", false) : Reply("{\"code\":0,\"data\":{\"list\":{}}}"));
                await vm.LoadResult();
                Check(R == before + 1 && vm.Page == 3 && ReferenceEquals(collection, vm.Result) && vm.Result.Count == 2 && !vm.Loading, failure + " append retains rows/page and releases owner");
                Check(Utils.Toasts.Count == (failure == "transport" ? 0 : 1), failure + " append retains current notice count");
            }
            SeasonIndexAPI.Send = a => Task.FromResult(Reply(Rows(103))); await vm.LoadResult();
            Check(SeasonIndexAPI.Calls.Last().Page == 3 && vm.Page == 4 && vm.Result[2].season_id == 103, "failed append retries the same accepted page");
            SeasonIndexAPI.Send = a => Task.FromResult(Reply("{\"code\":0,\"data\":{\"list\":[],\"has_next\":1}}"));
            Utils.Toasts.Clear(); await vm.LoadResult(); int calls = R;
            Check(vm.Page == 4 && vm.Result.Count == 3 && Utils.Toasts.SequenceEqual(new[] { "加载完了" }), "empty append stops without increment or row loss");
            int notifications = 0; vm.PropertyChanged += (s, e) => notifications++;
            await vm.LoadResult(); vm.LoadMoreCommand.Execute(null);
            Check(R == calls && notifications == 0 && vm.Page == 4, "stopped append declines before all notifications");
            SeasonIndexAPI.Send = a => Task.FromResult(Reply(Rows(104)));
            int conditions = C; vm.RefreshCommand.Execute(null);
            Check(C == conditions && R == calls + 1 && vm.Page == 2 && vm.Result.Count == 1 && vm.Result[0].season_id == 104 && !ReferenceEquals(collection, vm.Result), "usable Refresh resets page/replaces rows and reopens more without metadata reload");
            SeasonIndexAPI.Send = a => Task.FromResult(Reply(Rows(105).Replace("\"has_next\":1", "\"has_next\":0")));
            await vm.LoadResult();
            Check(vm.Page == 3 && vm.Result.Count == 2, "nonempty has_next0 retains existing append behavior");
            // Re-establish a public nonterminal two-row state for the independent missing-list
            // compatibility assertion: FI3 now correctly stops after the accepted integer-zero page.
            SeasonIndexAPI.Send = a => Task.FromResult(Reply(Rows(104)));
            vm.RefreshCommand.Execute(null);
            SeasonIndexAPI.Send = a => Task.FromResult(Reply(Rows(105)));
            await vm.LoadResult();
            SeasonIndexAPI.Send = a => Task.FromResult(Reply("{\"code\":0,\"data\":{}}"));
            Utils.Toasts.Clear(); await vm.LoadResult();
            Check(vm.Page == 3 && vm.Result.Count == 2 && Utils.Toasts.SequenceEqual(new[] { "加载完了" }), "absent data.list retains existing empty-page behavior");
            Reset(); vm = await Ready();
            SeasonIndexAPI.Send = a => Task.FromResult(Reply("{\"code\":0,\"data\":{\"list\":null}}"));
            await vm.LoadResult();
            Check(vm.Page == 1 && vm.Result == null && !vm.Loading && Utils.Toasts.SequenceEqual(new[] { "加载完了" }), "first empty/null list retains clear-and-stop behavior");
            Reset(); vm = await Ready(); await vm.LoadResult();
            SeasonIndexAPI.Send = a => Task.FromResult(Reply("{\"code\":-400,\"message\":\"synthetic refusal\"}"));
            vm.RefreshCommand.Execute(null);
            Check(vm.Result == null && vm.Page == 1 && !vm.Loading && Utils.Toasts.SequenceEqual(new[] { "synthetic refusal" }), "first-page business failure retains existing clearing/notice/retry policy");
            SeasonIndexAPI.Send = a => Task.FromResult(Reply(Rows(106))); await vm.LoadResult();
            Check(vm.Result?.Count == 1 && vm.Page == 2, "first-page failure permits legitimate retry");
        }
        private static string MetadataOrders(string orders)
        {
            var metadata = Newtonsoft.Json.Linq.JObject.Parse(Good);
            metadata["data"]["order"] = Newtonsoft.Json.Linq.JArray.Parse(orders);
            return metadata.ToString(Newtonsoft.Json.Formatting.None);
        }
        private static string FlagRows(int id, string flag)
        {
            return flag == null ? Rows(id).Replace(",\"has_next\":1", "") : Rows(id).Replace("\"has_next\":1", "\"has_next\":" + flag);
        }
        private static async Task OrderCodePriority()
        {
            var cases = new[]
            {
                new { Name = "default reordered", Code = "3", Orders = "[{\"field\":\"4\",\"name\":\"评分\"},{\"field\":\"3\",\"name\":\"追番\"}]", Expected = "3" },
                new { Name = "explicit nonfirst", Code = "4", Orders = "[{\"field\":\"3\",\"name\":\"追番\"},{\"field\":\"4\",\"name\":\"评分\"}]", Expected = "4" },
                new { Name = "code precedes conflicting label", Code = "3", Orders = "[{\"field\":\"4\",\"name\":\"3\"},{\"field\":\"3\",\"name\":\"renamed\"}]", Expected = "3" },
                new { Name = "localized label", Code = "4", Orders = "[{\"field\":\"3\",\"name\":\"A\"},{\"field\":\"4\",\"name\":\"评分 ✨\"}]", Expected = "4" },
                new { Name = "opaque exact field", Code = "custom + 评分 &x=2", Orders = "[{\"field\":\"3\",\"name\":\"A\"},{\"field\":\"custom + 评分 &x=2\",\"name\":\"B\"}]", Expected = "custom + 评分 &x=2" },
                new { Name = "numeric decoded field", Code = "4", Orders = "[{\"field\":3,\"name\":\"A\"},{\"field\":4,\"name\":\"B\"}]", Expected = "4" },
                new { Name = "legacy label fallback", Code = "评分", Orders = "[{\"field\":\"3\",\"name\":\"追番\"},{\"field\":\"4\",\"name\":\"评分\"}]", Expected = "4" },
                new { Name = "unknown first fallback", Code = "unknown", Orders = "[{\"field\":\"3\",\"name\":\"追番\"},{\"field\":\"4\",\"name\":\"评分\"}]", Expected = "3" },
                new { Name = "null first fallback", Code = (string)null, Orders = "[{\"field\":\"3\",\"name\":\"追番\"},{\"field\":\"4\",\"name\":\"评分\"}]", Expected = "3" },
                new { Name = "no whitespace normalization", Code = " 4 ", Orders = "[{\"field\":\"3\",\"name\":\"追番\"},{\"field\":\"4\",\"name\":\"评分\"}]", Expected = "3" },
            };
            foreach (var item in cases)
            {
                Reset(); var page = new AnimeIndexPage(); var vm = Vm(page);
                SeasonIndexAPI.Send = a => Task.FromResult(Reply(a.IsCondition ? MetadataOrders(item.Orders) : Rows(601)));
                Navigate(page, new SeasonIndexParameter { order = item.Code });
                await Wait(() => !vm.Loading && !vm.ConditionsLoading);
                Check(C == 1 && R == 1 && vm.Page == 2 && vm.Result?.Count == 1, "FI2 true constructor navigation remains complete: " + item.Name);
                var query = HttpUtility.ParseQueryString(SeasonIndexAPI.Calls.Last().Filters.TrimStart('&'));
                Check(vm.Conditions[0].current.keyword == item.Expected && query["order"] == item.Expected && query["x"] == null, "FI2 exact code/legacy fallback and encoded value: " + item.Name);
            }
            Reset(); var selectedPage = new AnimeIndexPage(); Navigate(selectedPage); var selectedVm = Vm(selectedPage);
            var score = selectedVm.Conditions[0].values.Single(a => a.keyword == "4"); selectedVm.Conditions[0].current = score;
            Selection(selectedPage, selectedVm);
            Check(R == 2 && selectedVm.Parameter.order == "3" && SeasonIndexAPI.Calls.Last().Page == 1 && SeasonIndexAPI.Calls.Last().Filters.Contains("&order=4&"), "FI2 actual selection submits current keyword without rewriting navigation order");
        }
        private static async Task TerminalPages()
        {
            foreach (bool append in new[] { false, true })
            {
                Reset(); var vm = await Ready();
                SeasonIndexAPI.Send = a => Task.FromResult(Reply(FlagRows(610, append ? "1" : "0")));
                await vm.LoadResult();
                if (append) { SeasonIndexAPI.Send = a => Task.FromResult(Reply(FlagRows(611, "0"))); vm.LoadMoreCommand.Execute(null); }
                var rows = vm.Result; int count = R, expected = append ? 2 : 1;
                Check(rows?.Count == expected && vm.Page == expected + 1 && !vm.Loading && Utils.Toasts.Count == 0, "FI3 accepted terminal rows publish and Page advances once: " + append);
                int notices = 0; vm.PropertyChanged += (s, e) => notices++;
                await vm.LoadResult(); vm.LoadMoreCommand.Execute(null);
                Check(R == count && ReferenceEquals(rows, vm.Result) && rows.Count == expected && vm.Page == expected + 1 && notices == 0 && Utils.Toasts.Count == 0, "FI3 terminal direct/command append rejects without notification or end toast: " + append);
                int conditions = C; SeasonIndexAPI.Send = a => Task.FromResult(Reply(FlagRows(612, "1")));
                vm.RefreshCommand.Execute(null);
                Check(C == conditions && R == count + 1 && vm.Result?.Count == 1 && vm.Result[0].season_id == 612 && vm.Page == 2 && !ReferenceEquals(rows, vm.Result), "FI3 terminal Refresh replaces page1 using retained metadata: " + append);
                vm.LoadMoreCommand.Execute(null);
                Check(R == count + 2 && vm.Result.Count == 2 && vm.Page == 3 && SeasonIndexAPI.Calls.Last().Page == 2, "FI3 Refresh reopens nonterminal continuation: " + append);
            }
        }
        private static async Task OptionalFlagCompatibility()
        {
            foreach (string flag in new[] { null, "null", "1", "2", "-1", "99999999999999999999999999999999999999999999999999", "\"0\"", "\"1\"", "false", "true", "0.0", "1.0", "{}", "[]", "\"bad\"" })
            {
                Reset(); var vm = await Ready(); await vm.LoadResult();
                SeasonIndexAPI.Send = a => Task.FromResult(Reply(FlagRows(620, flag))); vm.LoadMoreCommand.Execute(null);
                Check(R == 2 && vm.Result?.Count == 2 && vm.Page == 3 && Utils.Toasts.Count == 0 && !vm.Loading, "FI3 unsupported/nonterminal flag does not coerce/reject/overflow: " + (flag ?? "absent"));
                SeasonIndexAPI.Send = a => Task.FromResult(Reply(Rows(621))); vm.LoadMoreCommand.Execute(null);
                Check(R == 3 && vm.Result.Count == 3 && vm.Page == 4, "FI3 unsupported/nonterminal flag retains continuation: " + (flag ?? "absent"));
            }
            foreach (string list in new[] { "[]", "null" })
            {
                Reset(); var vm = await Ready(); await vm.LoadResult(); var rows = vm.Result;
                SeasonIndexAPI.Send = a => Task.FromResult(Reply("{\"code\":0,\"data\":{\"list\":" + list + ",\"has_next\":1}}"));
                vm.LoadMoreCommand.Execute(null); int count = R; vm.LoadMoreCommand.Execute(null);
                Check(R == count && vm.Page == 2 && ReferenceEquals(rows, vm.Result) && Utils.Toasts.SequenceEqual(new[] { "加载完了" }), "FI3 empty/null lists retain existing stop even with integer1: " + list);
            }
        }
        private static async Task TerminalGridCompletion()
        {
            foreach (string flag in new[] { "0", "1", (string)null })
            {
                Reset(); var page = new AnimeIndexPage(); var vm = Vm(page);
                var first = new TaskCompletionSource<HttpResults>(); var append = new TaskCompletionSource<HttpResults>();
                SeasonIndexAPI.Send = a => a.IsCondition ? Task.FromResult(Reply(Good)) : a.Page == 1 ? first.Task : append.Task;
                var grid = new MyAdaptiveGridView { LoadMoreCommand = vm.LoadMoreCommand, CanLoadMore = true };
                typeof(MyAdaptiveGridView).GetMethod("OnApplyTemplate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(grid, null);
                vm.PropertyChanged += (s, e) => { if (e.PropertyName == "Loading") grid.Loading = vm.Loading; };
                Navigate(page);
                Check(C == 1 && R == 1 && vm.Loading && vm.Page == 1, "FI3 true constructor grid navigation holds page1: " + (flag ?? "absent"));
                first.SetResult(Reply(FlagRows(630, flag)));
                await Wait(() => R >= 2 || !vm.Loading);
                if (flag == "0")
                {
                    Check(R == 1 && !vm.Loading && vm.Result?.Count == 1 && vm.Page == 2 && Utils.Toasts.Count == 0, "FI3 integer0 terminal fact precedes actual short-grid Loading=false callback");
                }
                else
                {
                    Check(R == 2 && vm.Loading && vm.Page == 2 && vm.Result?.Count == 1 && SeasonIndexAPI.Calls.Last().Page == 2, "FI3 nonterminal actual grid still acquires legitimate held page2: " + (flag ?? "absent"));
                }
                if (R >= 2)
                {
                    grid.TemplateScrollViewer.ScrollableHeight = 100; append.SetResult(Reply(Rows(631))); await Wait(() => !vm.Loading);
                    if (flag != "0") Check(vm.Result.Count == 2 && vm.Page == 3, "FI3 nonterminal grid completes ordered page2: " + (flag ?? "absent"));
                }
            }
        }
        private static async Task RejectedZeroRetry()
        {
            foreach (string failure in new[] { "business", "transport", "malformed-list", "wrong-payload" })
            {
                Reset(); var vm = await Ready(); await vm.LoadResult(); var rows = vm.Result;
                var failed = failure == "business" ? Reply("{\"code\":-400,\"message\":\"synthetic refusal\",\"data\":{\"has_next\":0}}") :
                    failure == "transport" ? Reply(FlagRows(640, "0"), false) :
                    failure == "malformed-list" ? Reply("{\"code\":0,\"data\":{\"list\":{},\"has_next\":0}}") :
                    Reply("{\"code\":0,\"result\":{\"list\":[],\"has_next\":0}}");
                SeasonIndexAPI.Send = a => Task.FromResult(failed); vm.LoadMoreCommand.Execute(null);
                Check(R == 2 && ReferenceEquals(rows, vm.Result) && vm.Result.Count == 1 && vm.Page == 2 && !vm.Loading && Utils.Toasts.Count == (failure == "transport" ? 0 : 1), "FI3 unaccepted zero retains old rows/Page/notices: " + failure);
                SeasonIndexAPI.Send = a => Task.FromResult(Reply(Rows(641))); vm.LoadMoreCommand.Execute(null);
                Check(R == 3 && rows.Count == 2 && rows[1].season_id == 641 && vm.Page == 3 && SeasonIndexAPI.Calls.Last().Page == 2, "FI3 unaccepted zero cannot commit terminal state; same-page retry succeeds: " + failure);
            }
        }
    }
}
namespace Windows.UI.Xaml {
 public class DependencyProperty { public object DefaultValue; public static DependencyProperty Register(string n,Type t,Type owner,PropertyMetadata m)=>new DependencyProperty{DefaultValue=m.Value}; }
 public class PropertyMetadata { public PropertyMetadata(object value){Value=value;} public object Value; }
 public delegate void DependencyPropertyChangedCallback(DependencyObject sender,DependencyProperty property);
 public class DependencyObject {
  readonly Dictionary<DependencyProperty,object> values=new Dictionary<DependencyProperty,object>();
  readonly Dictionary<DependencyProperty,List<DependencyPropertyChangedCallback>> callbacks=new Dictionary<DependencyProperty,List<DependencyPropertyChangedCallback>>();
  public object GetValue(DependencyProperty p)=>values.TryGetValue(p,out var v)?v:p.DefaultValue;
  public void SetValue(DependencyProperty p,object value){var old=GetValue(p);values[p]=value;if(!Equals(old,value)&&callbacks.TryGetValue(p,out var entries))foreach(var callback in entries.ToArray())callback(this,p);}
  public long RegisterPropertyChangedCallback(DependencyProperty p,DependencyPropertyChangedCallback callback){if(!callbacks.TryGetValue(p,out var list))callbacks[p]=list=new List<DependencyPropertyChangedCallback>();list.Add(callback);return list.Count;}
 }
}
namespace Windows.UI.Xaml.Controls {
 public enum Symbol { Play }
 public class ComboBox { public object SelectedItem {get;set;} }
 public class SelectionChangedEventArgs : EventArgs {}
 public class ItemClickEventArgs : EventArgs { public object ClickedItem {get;set;} }
 public class ScrollViewerViewChangedEventArgs : EventArgs {}
 public class ScrollViewer {
  public double ScrollableHeight {get;set;} public double VerticalOffset {get;set;}
  public event EventHandler<ScrollViewerViewChangedEventArgs> ViewChanged;
  public void RaiseViewChanged()=>ViewChanged?.Invoke(this,new ScrollViewerViewChangedEventArgs());
 }
}
namespace Microsoft.Toolkit.Uwp.UI.Controls {
 public class AdaptiveGridView:Windows.UI.Xaml.DependencyObject {
  public bool Loading {get;set;} public Windows.UI.Xaml.Controls.ScrollViewer TemplateScrollViewer {get;}=new Windows.UI.Xaml.Controls.ScrollViewer();
  protected object GetTemplateChild(string name)=>TemplateScrollViewer;
  protected virtual void OnApplyTemplate() {}
 }
}
namespace Windows.UI.Xaml.Navigation {
 public enum NavigationMode { New, Back } public enum NavigationCacheMode { Enabled }
 public class NavigationEventArgs : EventArgs { public NavigationMode NavigationMode {get;set;} public object Parameter {get;set;} }
}
namespace BiliLite.Pages {
 public class BasePage { public string Title {get;set;} public Windows.UI.Xaml.Navigation.NavigationCacheMode NavigationCacheMode {get;set;} protected virtual void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e) {} }
 public class SeasonDetailPage {}
}
namespace BiliLite.Pages.Bangumi { public sealed partial class AnimeIndexPage { private void InitializeComponent() {} } }
namespace BiliLite.Helpers {
 public class NavigationInfo { public Windows.UI.Xaml.Controls.Symbol icon;public Type page;public object parameters;public string title; }
 public static class MessageCenter { public static void NavigateToPage(object sender,NavigationInfo info) {} }
}

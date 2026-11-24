using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Modules;
using BiliLite.Parsing;
using BiliLite.Parsing.Video;

namespace C1Probe
{
    // Delivery gates never decide acceptance or ownership; the complete product VM does both.
    internal static class BoundaryChecks
    {
        static int passed, failed;
        static HttpResults Ok(string body) => new HttpResults { status = true, code = 200, results = body };
        static void Check(bool value, string label) { Console.WriteLine((value ? "PASS boundary " : "FAIL boundary ") + label); if (value) passed++; else failed++; }
        static async Task Case(string name, Func<Task> test) { try { await test(); } catch (Exception ex) { Check(false, name + " threw " + ex.GetType().Name); } }
        public static async Task Run()
        {
            var readers = new (string Field, Func<HttpResults, ApiParseResult<int>> Read)[] {
                ("", WebVideoStateParser.ParseWebVideoStateLike), ("multiply", WebVideoStateParser.ParseWebVideoStateCoin),
                ("follower", WebRelationParser.ParseWebRelationStat), ("attribute", WebRelationParser.ParseWebRelationAttribute) };
            foreach (var reader in readers)
            foreach (var literal in new[] { "2147483648", "-2147483649", "9223372036854775808", "-9223372036854775809", "0", "1", "-2147483648", "2147483647" })
                await Case("scalar " + reader.Field + " " + literal, () => {
                    var data = reader.Field == "" ? literal : "{\"" + reader.Field + "\":" + literal + "}";
                    var result = reader.Read(Ok("{\"code\":0,\"data\":" + data + "}"));
                    int expected; bool valid = int.TryParse(literal, out expected);
                    Check(valid ? result.IsSuccess && result.Value == expected : result.Failure == ApiParseFailure.Payload && result.Stage == "data" + (reader.Field == "" ? "" : "." + reader.Field), "bounded scalar " + reader.Field + " " + literal);
                    return Task.CompletedTask;
                });
            await Case("independent optional states", async () => {
                Reset("B"); var prior = NewOwner("A"); prior.req_user.like = 7;
                var vm = new VideoDetailVM { VideoInfo = prior };
                FakeTransport.Handler = (kind, id) => Task.FromResult(kind == "video.haslike" ? Ok("{\"code\":0,\"data\":9223372036854775808}") : Default(kind, id, "B"));
                var method = typeof(VideoDetailVM).GetMethod("GetUserVideoStates", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                await (Task)method.Invoke(vm, null);
                Check(vm.VideoInfo.req_user.like == 7 && vm.VideoInfo.req_user.coin == 1 && vm.VideoInfo.req_user.favorite == 1, "bad like preserves prior value and later coin/favorite publish");
            });
            foreach (var action in new[] { "like", "dislike", "triple", "coin", "favorite", "attention" })
            foreach (var replacement in new[] { "B", "A" })
            foreach (var accepted in new[] { true, false })
                await Case("retired " + action, async () => {
                    Reset(replacement);
                    var gate = new TaskCompletionSource<HttpResults>(TaskCreationOptions.RunContinuationsAsynchronously);
                    FakeTransport.Handler = (kind, id) => IsAction(kind, action) ? gate.Task : Task.FromResult(Default(kind, id, replacement));
                    var vm = new VideoDetailVM { VideoInfo = NewOwner("A") }; var old = vm.VideoInfo;
                    var tracker = Start(vm, action);
                    Check(FakeTransport.Calls.Any(c => IsAction(c.Kind, action) && c.Id == (action == "attention" ? "owner-A" : "A")), "request captured initiating " + action);
                    await vm.LoadVideoDetail(replacement); var fresh = vm.VideoInfo; string before = State(vm); int lists = FakeTransport.Calls.Count(c => c.Kind == "fav.created");
                    gate.SetResult(Ok(accepted ? "{\"code\":0}" : "{\"code\":-403,\"message\":\"denied\"}")); await tracker.Done.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    Check(!ReferenceEquals(old, fresh) && ReferenceEquals(fresh, vm.VideoInfo) && before == State(vm), "retired " + action + " " + replacement + " accepted=" + accepted + " preserves replacement");
                    Check(FakeTransport.Calls.Count(c => c.Kind == "fav.created") == lists, "retired " + action + " starts no list refresh");
                });
            foreach (var action in new[] { "like", "dislike", "triple", "coin", "favorite", "attention" })
            foreach (var accepted in new[] { true, false })
                await Case("current " + action, async () => {
                    Reset("A"); FakeTransport.Handler = (kind, id) => Task.FromResult(IsAction(kind, action) ? Ok(accepted ? "{\"code\":0}" : "{\"code\":-403}") : Default(kind, id, "A"));
                    var vm = new VideoDetailVM { VideoInfo = NewOwner("A") }; string before = State(vm);
                    await Start(vm, action).Done.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    var d = vm.VideoInfo;
                    bool expected = action == "like" ? d.req_user.like == 1 && d.stat.like == 11 : action == "dislike" ? d.req_user.dislike == 1 : action == "triple" ? d.req_user.like == 1 && d.req_user.coin == 1 && d.req_user.favorite == 1 && d.stat.coin == 21 : action == "coin" ? d.req_user.coin == 1 && d.stat.coin == 21 : action == "favorite" ? d.req_user.favorite == 1 && d.stat.favorite == 31 : d.req_user.attention == 1 && d.owner_ext.fans == 101;
                    Check(accepted ? expected : before == State(vm), "current " + action + " accepted=" + accepted + " exact effects");
                });
            foreach (var action in new[] { "like", "dislike", "coin", "favorite", "attention" })
                await Case("current undo " + action, async () => {
                    Reset("A"); var vm = new VideoDetailVM { VideoInfo = NewOwner("A") };
                    await Start(vm, action).Done.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    var tracker = new AsyncVoidTracker();
                    if (action == "favorite") tracker.Start(() => vm.DoFavorite(new List<string>(), "A")); else tracker = Start(vm, action);
                    await tracker.Done.Task.WaitAsync(TimeSpan.FromSeconds(5)); var d = vm.VideoInfo;
                    Check(action == "like" ? d.req_user.like == 0 && d.stat.like == 10 : action == "dislike" ? d.req_user.dislike == 0 : action == "coin" ? d.req_user.coin == 0 && d.stat.coin == 20 : action == "favorite" ? d.req_user.favorite == 0 && d.stat.favorite == 30 : d.req_user.attention == -999 && d.owner_ext.fans == 100, "current " + action + " preserves original undo/count behavior");
                });
            foreach (var action in new[] { "like", "dislike", "triple", "coin", "favorite", "attention" })
                await Case("login " + action, async () => {
                    Reset("B"); SettingHelper.Account.Logined = false;
                    var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); Utils.Login = () => gate.Task;
                    var vm = new VideoDetailVM { VideoInfo = NewOwner("A") }; var tracker = Start(vm, action);
                    await vm.LoadVideoDetail("B"); int calls = FakeTransport.Calls.Count;
                    gate.SetResult(true); await tracker.Done.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    Check(FakeTransport.Calls.Count == calls, "retired during login " + action + " sends no request");
                });
            foreach (var action in new[] { "like", "dislike", "triple", "coin", "favorite", "attention" })
                await Case("current login " + action, async () => {
                    Reset("A"); SettingHelper.Account.Logined = false;
                    var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); Utils.Login = () => gate.Task;
                    var vm = new VideoDetailVM { VideoInfo = NewOwner("A") }; var ids = new List<string> { "folder-exact", "folder-second" }; var tracker = new AsyncVoidTracker();
                    if (action == "favorite") tracker.Start(() => vm.DoFavorite(ids, "A")); else tracker = Start(vm, action);
                    ids.Clear(); vm.VideoInfo.req_user.like = 1; vm.VideoInfo.req_user.dislike = 1;
                    gate.SetResult(true); await tracker.Done.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    var request = FakeTransport.Requests.Single(c => IsAction(c.Kind, action));
                    string fields = string.Join(",", request.Fields);
                    Check(request.Id == (action == "attention" ? "owner-A" : "A") && fields == (action == "like" || action == "dislike" ? "0,0" : action == "coin" || action == "attention" ? "1" : action == "favorite" ? "folder-exact,folder-second" : ""), "current login " + action + " retains initiating target and arguments");
                });
            await Case("favorite stale list", async () => {
                Reset("B"); var gate = new TaskCompletionSource<HttpResults>(TaskCreationOptions.RunContinuationsAsynchronously);
                FakeTransport.Handler = (kind, id) => kind == "fav.created" && id == "A" ? gate.Task : Task.FromResult(Default(kind, id, "B"));
                var vm = new VideoDetailVM { VideoInfo = NewOwner("A") }; var tracker = Start(vm, "favorite");
                Check(FakeTransport.Calls.Any(c => c.Kind == "fav.created" && c.Id == "A"), "accepted favorite began A list");
                await vm.LoadVideoDetail("B"); gate.SetResult(Ok("{\"code\":0,\"data\":{\"list\":[{\"id\":\"A-folder\"}]}}"));
                await tracker.Done.Task.WaitAsync(TimeSpan.FromSeconds(5)); Check(vm.MyFavorite.Single().id == "B-folder", "late A list preserves B list");
            });
            await Case("favorite supplied avid", async () => {
                Reset("A"); var vm = new VideoDetailVM { VideoInfo = NewOwner("A") }; string before = State(vm); var tracker = new AsyncVoidTracker();
                tracker.Start(() => vm.DoFavorite(new List<string> { "folder-exact" }, "other-avid")); await tracker.Done.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Check(FakeTransport.Calls.Any(c => c.Kind == "fav.add" && c.Id == "other-avid") && before == State(vm), "supplied other avid remains request target without visible mutation");
            });
            Reset("A"); Check(await new VideoDetailVM().AttentionUP("standalone", 1), "standalone AttentionUP remains usable");
            Check(WebVideoActionParser.ParseWebVideoAction(Ok("prefix {\"code\":0} suffix")).IsSuccess, "actual decoder object salvage retained");
            Console.WriteLine("BOUNDARY passed=" + passed + " failed=" + failed); if (failed != 0) throw new Exception("boundary failures=" + failed);
        }
        static bool IsAction(string kind, string action) => kind == (action == "favorite" ? "fav.add" : "video." + action);
        static AsyncVoidTracker Start(VideoDetailVM vm, string action) { var tracker = new AsyncVoidTracker(); tracker.Start(() => { switch (action) { case "like": vm.DoLike(); break; case "dislike": vm.DoDislike(); break; case "triple": vm.DoTriple(); break; case "coin": vm.DoCoin("1"); break; case "favorite": vm.DoFavorite(new List<string> { "folder" }, "A"); break; case "attention": vm.DoAttentionUP(); break; } }); return tracker; }
        static VideoDetailModel NewOwner(string aid) => new VideoDetailModel { aid = aid, owner = new VideoDetailOwnerModel { mid = "owner-" + aid }, owner_ext = new VideoDetailOwnerExtModel { fans = 100 }, req_user = new VideoDetailReqUserModel { attention = -999 }, stat = new VideoDetailStatModel { like = 10, coin = 20, favorite = 30 } };
        static string State(VideoDetailVM vm) => Newtonsoft.Json.JsonConvert.SerializeObject(new { vm.VideoInfo.req_user, vm.VideoInfo.stat, vm.VideoInfo.owner_ext, folders = vm.MyFavorite?.Select(x => x.id).ToArray() });
        static void Reset(string replacement) { SettingHelper.Account.Logined = true; Utils.Login = () => Task.FromResult(true); FakeTransport.Calls.Clear(); FakeTransport.Requests.Clear(); Utils.Toasts.Clear(); FakeTransport.Handler = (kind, id) => Task.FromResult(Default(kind, id, replacement)); }
        static HttpResults Default(string kind, string id, string replacement) => Ok(kind switch {
            "video.web" => "{\"code\":0,\"data\":{\"aid\":\"" + replacement + "\",\"owner\":{\"mid\":\"owner-" + replacement + "\"},\"stat\":{\"like\":10,\"coin\":20,\"favorite\":30}}}",
            "video.relates" => "{\"code\":0,\"data\":[]}", "video.haslike" => "{\"code\":0,\"data\":0}", "video.hascoin" => "{\"code\":0,\"data\":{\"multiply\":1}}", "video.hasfav" => "{\"code\":0,\"data\":{\"favoured\":true}}",
            "fav.created" => "{\"code\":0,\"data\":{\"list\":[{\"id\":\"" + replacement + "-folder\"}]}}", "video.fans" => "{\"code\":0,\"data\":{\"follower\":100}}", "video.relation" => "{\"code\":0,\"data\":{\"attribute\":0}}", "video.tags" => "{\"code\":0,\"data\":[]}", _ => "{\"code\":0}" });
    }
}

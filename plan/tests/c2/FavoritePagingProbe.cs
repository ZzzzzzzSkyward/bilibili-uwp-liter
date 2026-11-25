// Actual FavoriteDetailVM/API/parser/mapper/HttpResults; only delivery/platform primitives are substituted.
namespace OriginalFavoriteProof {
using System;
using System.Threading.Tasks;
using BiliLite.Modules;
using BiliLite.Helpers;
using C2Probe;
internal static class FavoriteRepro {
 static int failed;
 static void Check(bool okay,string label) { Console.WriteLine((okay?"PASS ":"FAIL ")+label); if(!okay) failed++; }
 public static async Task<int> Run() {
  foreach(var test in new[]{("filtered-terminal",42,false,1,false,1),("count-equal-more",1,true,1,true,2),("appended-terminal",42,false,2,false,2)}) {
   FakeTransport.Reset();
   var vm=new FavoriteDetailVM{Id="folder-fixture",Keyword="filtered",Type=2,Page=test.Item4};
   if(test.Item4==2) { vm.FavoriteInfo=new FavoriteInfoModel{mid="owner",media_count=42}; vm.Videos=new System.Collections.ObjectModel.ObservableCollection<FavoriteInfoVideoItemModel>{new FavoriteInfoVideoItemModel{id="earlier"}}; }
   FakeTransport.Delivery=_=>Task.FromResult(new HttpResults{code=200,status=true,results="{\"code\":0,\"data\":{\"info\":{\"mid\":\"owner\",\"media_count\":"+test.Item2+"},\"medias\":[{\"id\":\"row\"}],\"has_more\":"+(test.Item3?"true":"false")+"}}"});
   await vm.LoadFavoriteInfo();
   Check(vm.ShowLoadMore==test.Item5,test.Item1+" ShowLoadMore expected="+test.Item5+" actual="+vm.ShowLoadMore);
   Check(vm.Page==test.Item6,test.Item1+" Page expected="+test.Item6+" actual="+vm.Page);
   Check(!vm.Loading && FakeTransport.ActualRequests.Count==1,test.Item1+" actual caller/builder one request completed");
  }
  Console.WriteLine("failures="+failed);return failed==0?0:1;
 }
}

}
namespace OriginalBoundaryProof {
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BiliLite.Modules;
using BiliLite.Helpers;
using C2Probe;
internal static class As3BoundaryControls {
 static int passed,failed;
 static void Check(bool okay,string label) { if(okay) passed++; else {failed++;Console.WriteLine("FAIL "+label);} }
 public static async Task<int> Run() {
  BiliLite.Api.ApiHelper.API_BASE_URL="https://api.bilibili.com";
  foreach(int type in new[]{2,21}) foreach(string flag in new[]{"absent","null","true","false"}) foreach(bool empty in new[]{true,false}) {
   if(type==2 && (flag=="true"||flag=="false")) continue;
   await Append(type,flag,empty,42);
   await Append(type,flag,empty,empty?1:2);
  }
  foreach(int type in new[]{2,21}) foreach(string flag in new[]{"absent","null","true","false"}) await FirstEmpty(type,flag);
  foreach(int type in new[]{2,21}) await FirstMissingInfo(type);
  Console.WriteLine("unchanged-green controls: passed="+passed+" failed="+failed);return failed==0?0:1;
 }
 static string Flag(string flag)=>flag=="absent"?"":",\"has_more\":"+flag;
 static HttpResults Response(string body)=>new HttpResults{code=200,status=true,results=body};
 static string Path(int type)=>"https://api.bilibili.com/"+(type==21?"x/space/fav/season/list":"x/v3/fav/resource/list");
 static async Task Append(int type,string flag,bool empty,int total) {
  FakeTransport.Reset();var row=new FavoriteInfoVideoItemModel{id="9007199254740993"};var info=new FavoriteInfoModel{id="accepted-folder",mid="owner",media_count=total};var rows=new ObservableCollection<FavoriteInfoVideoItemModel>{row};
  var vm=new FavoriteDetailVM{Id="folder-fixture",Keyword="A+B&literal%2B",Type=type,Page=2,FavoriteInfo=info,Videos=rows,ShowLoadMore=true};
  FakeTransport.Delivery=_=>Task.FromResult(Response("{\"code\":0,\"data\":{\"info\":{\"mid\":\"ignored-append-info\",\"media_count\":999},\"medias\":"+(empty?"[]":"[{\"id\":\"9007199254740995\"}]")+Flag(flag)+"}}"));
  await vm.LoadFavoriteInfo(); bool more=(empty?1:2)!=total;string label="type="+type+" "+flag+" empty="+empty+" total="+total;
  Check(vm.ShowLoadMore==more&&vm.Page==(more?3:2),label+" exact existing !=/Page");
  Check(ReferenceEquals(vm.Videos,rows)&&ReferenceEquals(vm.Videos[0],row)&&vm.Videos.Count==(empty?1:2)&&ReferenceEquals(vm.FavoriteInfo,info),label+" rows/info identities");
  Check(FakeTransport.ActualRequests.Count==1&&FakeTransport.ActualRequests[0].baseUrl==Path(type)&&FakeTransport.ActualRequests[0].parameter.Contains("&pn=2&ps=20")&&FakeTransport.ActualRequests[0].parameter.Contains("keyword=A%2BB%26literal%252B"),label+" real route/pn/encoded keyword");
  Check(!vm.Loading&&!vm.Nothing&&FakeTransport.Toasts.Count==0,label+" completed successful append");
 }
 static async Task FirstEmpty(int type,string flag) {
  FakeTransport.Reset();var oldInfo=new FavoriteInfoModel{id="old"};var oldRows=new ObservableCollection<FavoriteInfoVideoItemModel>{new FavoriteInfoVideoItemModel{id="old-row"}};
  var vm=new FavoriteDetailVM{Id="folder-fixture",Type=type,Page=1,FavoriteInfo=oldInfo,Videos=oldRows,ShowLoadMore=true};
  FakeTransport.Delivery=_=>Task.FromResult(Response("{\"code\":0,\"data\":{\"info\":{\"id\":\"new-info\",\"mid\":\"owner\",\"media_count\":42},\"medias\":[]"+Flag(flag)+"}}"));
  await vm.LoadFavoriteInfo(); string label="first-empty type="+type+" "+flag;
  Check(!vm.ShowLoadMore&&vm.Page==1&&vm.Nothing&&!vm.Loading,label+" terminal first-empty state");
  Check(vm.FavoriteInfo.id=="new-info"&&!ReferenceEquals(vm.FavoriteInfo,oldInfo)&&ReferenceEquals(vm.Videos,oldRows),label+" publishes info and returns before Videos assignment");
  Check(FakeTransport.ActualRequests.Count==1&&FakeTransport.ActualRequests[0].baseUrl==Path(type)&&FakeTransport.Toasts.Count==0,label+" real route/success");
 }
 static async Task FirstMissingInfo(int type) {
  FakeTransport.Reset();var info=new FavoriteInfoModel{id="old"};var rows=new ObservableCollection<FavoriteInfoVideoItemModel>{new FavoriteInfoVideoItemModel{id="old-row"}};
  var vm=new FavoriteDetailVM{Id="folder-fixture",Type=type,Page=1,FavoriteInfo=info,Videos=rows};
  FakeTransport.Delivery=_=>Task.FromResult(Response("{\"code\":0,\"data\":{\"medias\":[{\"id\":\"unpublished\"}],\"has_more\":true}}"));await vm.LoadFavoriteInfo();
  Check(ReferenceEquals(vm.FavoriteInfo,info)&&ReferenceEquals(vm.Videos,rows)&&vm.Page==1&&!vm.ShowLoadMore&&!vm.Nothing&&!vm.Loading,"first missing-info type="+type+" rejects publication");
  Check(FakeTransport.ActualRequests.Count==1&&FakeTransport.ActualRequests[0].baseUrl==Path(type)&&FakeTransport.Toasts.Count==1,"first missing-info type="+type+" real route/failure notice");
 }
}

}
namespace OriginalPresentEmptyProof {
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BiliLite.Modules;
using BiliLite.Helpers;
using C2Probe;
internal static class As3PresentEmpty {
 static int passed,failed;
 static void Check(bool okay,string label) {Console.WriteLine((okay?"PASS ":"FAIL ")+label);if(okay)passed++;else failed++;}
 public static async Task<int> Run() {
  BiliLite.Api.ApiHelper.API_BASE_URL="https://api.bilibili.com";
  foreach(string flag in new[]{"true","false"}) {
   FakeTransport.Reset();var row=new FavoriteInfoVideoItemModel{id="9007199254740993"};var info=new FavoriteInfoModel{id="accepted-folder",mid="owner",media_count=42};var rows=new ObservableCollection<FavoriteInfoVideoItemModel>{row};
   var vm=new FavoriteDetailVM{Id="folder-fixture",Keyword="A+B&literal%2B",Type=2,Page=2,FavoriteInfo=info,Videos=rows,ShowLoadMore=true};
   FakeTransport.Delivery=_=>Task.FromResult(new HttpResults{code=200,status=true,results="{\"code\":0,\"data\":{\"medias\":[],\"has_more\":"+flag+"}}"});
   await vm.LoadFavoriteInfo();string label="normal present "+flag+" empty append";
   Check(!vm.ShowLoadMore&&vm.Page==2,label+" terminal/no Page increment actual="+vm.ShowLoadMore+"/"+vm.Page);
   Check(ReferenceEquals(vm.Videos,rows)&&ReferenceEquals(vm.Videos[0],row)&&vm.Videos.Count==1&&ReferenceEquals(vm.FavoriteInfo,info),label+" rows/info preserved");
   Check(FakeTransport.ActualRequests.Count==1&&FakeTransport.ActualRequests[0].baseUrl=="https://api.bilibili.com/x/v3/fav/resource/list"&&FakeTransport.ActualRequests[0].parameter.Contains("keyword=A%2BB%26literal%252B&pn=2&ps=20"),label+" actual route/pn/encoded keyword");
   Check(!vm.Loading&&!vm.Nothing&&FakeTransport.Toasts.Count==0,label+" completed successful append");
  }
  Console.WriteLine("present-empty desired-safe: passed="+passed+" failed="+failed);return failed==0?0:1;
 }
}

}
namespace C2Probe
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using BiliLite.Api;
    using BiliLite.Helpers;
    using BiliLite.Modules;
    using BiliLite.Parsing;
    using BiliLite.Parsing.MediaList;

    internal static class FavoritePagingProbe
    {
        private static int passed, failed;
        private const string Keyword = "A+B&literal%2B 中文👋";
        private static void Check(bool condition, string label)
        {
            if (condition) passed++;
            else { failed++; Console.Error.WriteLine("FAIL favorite paging: " + label); }
        }
        public static async Task Run()
        {
            // Frozen discovery checks retain every expectation; only their entry points are renamed.
            var original = await OriginalFavoriteProof.FavoriteRepro.Run();
            var compatibility = await OriginalBoundaryProof.As3BoundaryControls.Run();
            var empty = await OriginalPresentEmptyProof.As3PresentEmpty.Run();
            var oldHost = ApiHelper.API_BASE_URL;
            try
            {
                ApiHelper.API_BASE_URL = "https://api.bilibili.com";
                foreach (var type in new[] { 2, 21 })
                foreach (var flag in new[] { "true", "false", "absent", "null" })
                foreach (var first in new[] { true, false })
                foreach (var noRows in new[] { true, false })
                foreach (var equal in new[] { true, false })
                    await Decision(type, flag, first, noRows, equal);
                await OptionalRowsAndOwnership();
                await FailuresAndRetry();
            }
            finally { ApiHelper.API_BASE_URL = oldHost; FakeTransport.Reset(); }
            Console.WriteLine("Favorite paging added checks: passed=" + passed + " failed=" + failed);
            if (original + compatibility + empty + failed != 0)
                throw new InvalidOperationException("Favorite paging proof failed: original=" + original
                    + " compatibility=" + compatibility + " empty=" + empty + " added=" + failed);
        }
        private static string Flag(string flag) => flag == "absent" ? "" : ",\"has_more\":" + flag;
        private static HttpResults Reply(string body) => new HttpResults { status = true, code = 200, results = body };
        private static string Body(string flag, string medias, int total = 42, string mid = "owner", int fav = 0)
            => "{\"code\":0,\"data\":{\"info\":{\"id\":\"0009007199254740993\",\"mid\":\"" + mid
                + "\",\"fav_state\":" + fav + ",\"media_count\":" + total + "}" + medias + Flag(flag) + "}}";
        private static string Rows(bool empty) => ",\"medias\":" + (empty ? "[]"
            : "[null,{\"id\":\"0009007199254740995\",\"title\":\"first\"},null,{\"id\":\"9007199254740997\",\"title\":\"second\"}]");
        private static FavoriteDetailVM Seed(int type, int page)
            => new FavoriteDetailVM { Id = "0009007199254740993", Type = type, Page = page, Keyword = Keyword,
                FavoriteInfo = new FavoriteInfoModel { id = "accepted", mid = "owner", media_count = 42 },
                Videos = new ObservableCollection<FavoriteInfoVideoItemModel> { new FavoriteInfoVideoItemModel { id = "prior" } },
                ShowLoadMore = true, Nothing = true };
        private static void Request(FavoriteDetailVM vm, int page, string label)
        {
            Check(FakeTransport.ActualRequests.Count == 1, label + " one delivery");
            var request = FakeTransport.ActualRequests.Single();
            var fields = request.parameter.Split('&').Select(x => x.Split(new[] { '=' }, 2))
                .ToDictionary(x => x[0], x => Uri.UnescapeDataString(x.Length == 2 ? x[1] : ""));
            Check(request.baseUrl == "https://api.bilibili.com/" + (vm.Type == 21 ? "x/space/fav/season/list" : "x/v3/fav/resource/list")
                && request.method == RestSharp.Method.Get && fields["keyword"] == Keyword
                && fields["pn"] == page.ToString() && fields["ps"] == "20"
                && fields["mid"] == SettingHelper.Account.UserID
                && fields[vm.Type == 21 ? "season_id" : "media_id"] == vm.Id, label + " real GET route/fields/string ID/encoded keyword");
            Check(vm.Type == 21 ? !request.need_cookie && fields["sign"] == "dummy-sign"
                : request.need_cookie && fields["platform"] == "web", label + " source request policy");
        }
        private static async Task Decision(int type, string flag, bool first, bool noRows, bool equal)
        {
            FakeTransport.Reset();
            var page = first ? 1 : 2;
            var vm = Seed(type, page);
            var info = vm.FavoriteInfo; var rows = vm.Videos; var row = rows[0];
            var count = (first ? 0 : 1) + (noRows ? 0 : 2);
            var total = equal ? count : 42;
            info.media_count = total;
            FakeTransport.Delivery = _ => Task.FromResult(Reply(Body(flag, Rows(noRows), total)));
            await vm.LoadFavoriteInfo();
            var present = flag == "true" || flag == "false";
            var more = !(first && noRows) && (type != 21 && present ? flag == "true" && !noRows : count != total);
            var label = "table type=" + type + " flag=" + flag + " first=" + first + " empty=" + noRows + " equal=" + equal;
            Check(vm.ShowLoadMore == more && vm.Page == page + (more ? 1 : 0), label + " paging");
            Check(!vm.Loading && vm.Nothing == (first && noRows) && FakeTransport.Toasts.Count == 0, label + " completion");
            Check(first ? vm.FavoriteInfo.id == "0009007199254740993" && !ReferenceEquals(vm.FavoriteInfo, info)
                : ReferenceEquals(vm.FavoriteInfo, info), label + " info publication/append retention");
            Check(first && !noRows ? !ReferenceEquals(vm.Videos, rows) : ReferenceEquals(vm.Videos, rows), label + " collection identity");
            var ids = first && !noRows ? new[] { "0009007199254740995", "9007199254740997" }
                : noRows ? new[] { "prior" } : new[] { "prior", "0009007199254740995", "9007199254740997" };
            Check(vm.Videos.Select(x => x.id).SequenceEqual(ids) && (first && !noRows || ReferenceEquals(vm.Videos[0], row)),
                label + " null rows filtered, IDs/order/accepted object preserved");
            Request(vm, page, label);
        }
        private static async Task OptionalRowsAndOwnership()
        {
            foreach (var type in new[] { 2, 21 })
            foreach (var medias in new[] { "", ",\"medias\":null", ",\"medias\":[null,null]" })
            foreach (var flag in new[] { "true", "false", "absent", "null" })
            foreach (var page in new[] { 1, 2 })
            {
                FakeTransport.Reset(); var vm = Seed(type, page); var rows = vm.Videos; var info = vm.FavoriteInfo;
                FakeTransport.Delivery = _ => Task.FromResult(Reply(Body(flag, medias)));
                await vm.LoadFavoriteInfo();
                var more = page != 1 && (type == 21 || flag == "absent" || flag == "null");
                Check(vm.Page == page + (more ? 1 : 0) && vm.ShowLoadMore == more && vm.Nothing == (page == 1)
                    && !vm.Loading && ReferenceEquals(vm.Videos, rows) && rows.Count == 1
                    && (page == 1 ? !ReferenceEquals(vm.FavoriteInfo, info) : ReferenceEquals(vm.FavoriteInfo, info))
                    && FakeTransport.Toasts.Count == 0, "optional rows type=" + type + " " + flag + " page=" + page + " " + medias);
            }
            foreach (var type in new[] { 2, 21 })
            foreach (var flag in new[] { "true", "false", "absent", "null" })
            foreach (var infoToken in new[] { "", "\"info\":null," })
            {
                FakeTransport.Reset(); var vm = Seed(type, 1); var rows = vm.Videos; var info = vm.FavoriteInfo;
                vm.IsSelf = true; vm.ShowCollect = true; vm.ShowCancelCollect = false;
                FakeTransport.Delivery = _ => Task.FromResult(Reply("{\"code\":0,\"data\":{" + infoToken
                    + "\"medias\":[{\"id\":\"unpublished\"}]" + Flag(flag) + "}}"));
                await vm.LoadFavoriteInfo();
                Check(vm.Page == 1 && !vm.ShowLoadMore && !vm.Nothing && !vm.Loading && ReferenceEquals(vm.Videos, rows)
                    && rows.Count == 1 && ReferenceEquals(vm.FavoriteInfo, info) && vm.IsSelf && vm.ShowCollect && !vm.ShowCancelCollect
                    && FakeTransport.Toasts.SequenceEqual(new[] { MediaListFailureText.GenericText }),
                    "first missing/null info rejects rows/ownership type=" + type + " flag=" + flag + " " + infoToken);
            }
            foreach (var type in new[] { 2, 21 })
            foreach (var self in new[] { true, false })
            foreach (var collected in new[] { true, false })
            {
                FakeTransport.Reset(); var vm = Seed(type, 1); var rows = vm.Videos;
                vm.ShowCollect = true; vm.ShowCancelCollect = false;
                FakeTransport.Delivery = _ => Task.FromResult(Reply(Body("true", Rows(true), 42,
                    self ? SettingHelper.Account.UserID : "other", collected ? 1 : 0)));
                await vm.LoadFavoriteInfo();
                Check(vm.IsSelf == self && vm.ShowCollect == (self || !collected) && vm.ShowCancelCollect == (!self && collected)
                    && vm.Nothing && vm.Page == 1 && !vm.ShowLoadMore && ReferenceEquals(rows, vm.Videos),
                    "first empty derives ownership/collection fields type=" + type + " self=" + self + " collected=" + collected);
            }
        }
        private static async Task FailuresAndRetry()
        {
            var cases = new[] {
                ("null transport", (HttpResults)null, ApiParseFailure.Transport, "请检查你的网络连接"),
                ("HTTP transport", new HttpResults { status = false, code = 503, message = "fixture offline" }, ApiParseFailure.Transport, "fixture offline"),
                ("business", Reply("{\"code\":-403,\"message\":\"fixture denied\"}"), ApiParseFailure.Rejected, "fixture denied"),
                ("no code", Reply("{\"data\":{}}"), ApiParseFailure.Envelope, "读取播放列表失败"),
                ("JSON", Reply("not JSON"), ApiParseFailure.Envelope, "读取播放列表失败"),
                ("no data", Reply("{\"code\":0}"), ApiParseFailure.Envelope, "读取播放列表失败"),
                ("data shape", Reply("{\"code\":0,\"data\":[]}"), ApiParseFailure.Payload, "读取播放列表失败"),
                ("medias shape", Reply(Body("true", ",\"medias\":{}")), ApiParseFailure.Payload, "读取播放列表失败"),
                ("boolean number", Reply(Body("1", Rows(false))), ApiParseFailure.Payload, "读取播放列表失败"),
                ("boolean string", Reply(Body("\"false\"", Rows(false))), ApiParseFailure.Payload, "读取播放列表失败"),
                ("boolean object", Reply(Body("{}", Rows(false))), ApiParseFailure.Payload, "读取播放列表失败"),
                ("boolean array", Reply(Body("[]", Rows(false))), ApiParseFailure.Payload, "读取播放列表失败"),
                ("info shape", Reply("{\"code\":0,\"data\":{\"info\":[],\"medias\":[],\"has_more\":true}}"), ApiParseFailure.Payload, "读取播放列表失败")
            };
            foreach (var type in new[] { 2, 21 })
            foreach (var page in new[] { 1, 2 })
            foreach (var test in cases)
            {
                FakeTransport.Reset(); var vm = Seed(type, page); var rows = vm.Videos; var info = vm.FavoriteInfo;
                var parsed = FavoriteResourceListParser.Parse(test.Item2);
                Check(!parsed.IsSuccess && parsed.Failure == test.Item3, test.Item1 + " typed parser failure");
                FakeTransport.Delivery = _ => Task.FromResult(test.Item2);
                await vm.LoadFavoriteInfo();
                Check(ReferenceEquals(vm.Videos, rows) && rows.Count == 1 && ReferenceEquals(vm.FavoriteInfo, info)
                    && vm.Page == page && !vm.ShowLoadMore && !vm.Nothing && !vm.Loading
                    && FakeTransport.Toasts.SequenceEqual(new[] { test.Item4 }), test.Item1 + " retain/notice type=" + type + " page=" + page);
                Request(vm, page, test.Item1);
                FakeTransport.Reset();
                FakeTransport.Delivery = _ => Task.FromResult(Reply(Body("true", Rows(false))));
                await vm.LoadFavoriteInfo();
                Check(vm.Page == page + 1 && vm.ShowLoadMore && !vm.Loading && !vm.Nothing && FakeTransport.Toasts.Count == 0
                    && (page == 1 ? !ReferenceEquals(vm.Videos, rows) : ReferenceEquals(vm.Videos, rows))
                    && (page == 1 ? vm.FavoriteInfo.id == "0009007199254740993" : ReferenceEquals(vm.FavoriteInfo, info)),
                    test.Item1 + " successful retry publishes/increments once type=" + type + " page=" + page);
                Request(vm, page, test.Item1 + " retry requests same page");
            }
            foreach (var type in new[] { 2, 21 })
            {
                FakeTransport.Reset(); var vm = Seed(type, 2); var rows = vm.Videos; var info = vm.FavoriteInfo;
                FakeTransport.Delivery = _ => throw new InvalidOperationException("fixture exception");
                await vm.LoadFavoriteInfo();
                Check(vm.Page == 2 && !vm.ShowLoadMore && !vm.Loading && !vm.Nothing && ReferenceEquals(vm.Videos, rows)
                    && ReferenceEquals(vm.FavoriteInfo, info) && FakeTransport.Logs.Count == 1 && FakeTransport.Toasts.Count == 1,
                    "exception retains accepted state type=" + type);
                FakeTransport.Reset(); info.media_count = 2;
                FakeTransport.Delivery = _ => Task.FromResult(Reply(Body("true", ",\"medias\":[{\"id\":\"retry\"}]")));
                await vm.LoadFavoriteInfo();
                var next = type == 21 ? 2 : 3;
                Check(vm.Page == next && vm.ShowLoadMore == (type != 21) && rows.Select(x => x.id).SequenceEqual(new[] { "prior", "retry" })
                    && ReferenceEquals(vm.FavoriteInfo, info), "retry same page publication type=" + type);
                Request(vm, 2, "retry after failed append");
                FakeTransport.Reset();
                FakeTransport.Delivery = _ => Task.FromResult(Reply(Body("false", Rows(true))));
                await vm.LoadFavoriteInfo();
                Check(vm.Page == next && !vm.ShowLoadMore && rows.Count == 2 && ReferenceEquals(vm.FavoriteInfo, info),
                    "later terminal page preserves accepted info type=" + type);
                Request(vm, next, "next accepted request");
            }
        }
    }
}

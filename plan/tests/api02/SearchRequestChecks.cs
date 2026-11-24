using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Services;

internal static class SearchRequestChecks
{
    private static readonly WbiKey Keys = new WbiKey("7cd084941338484aae1ad9425b84077c", "4932caff0ff746eab6f01bf08b70ac45");
    private static readonly Dictionary<string, int> Failures = new Dictionary<string, int>();
    private static int passed;

    private static void Check(bool ok, string category, string name)
    {
        if (ok) { passed++; return; }
        Failures[category] = Failures.TryGetValue(category, out var count) ? count + 1 : 1;
        Console.WriteLine("FAIL " + category + ": " + name);
    }

    public static async Task RunAsync()
    {
        var oldBase = ApiHelper.API_BASE_URL;
        var oldLoginKey = ApiHelper.LoginKey;
        var oldKeys = ApiHelper.wbitoken_current;
        var oldLoggedIn = SettingHelper.Account.Logined;
        var oldAccessKey = SettingHelper.Account.AccessKey;
        var oldHandler = Api01TestHooks.RequestHandler;
        var oldSelector = Api01TestHooks.ProxySelector;
        var oldAreas = Api01TestHooks.ProxyAreas.ToArray();
        try
        {
            // API02 is a fresh process and its original refresh tests never seed WBI.
            var api = new SearchAPI();
            Api01TestHooks.RequestHandler = _ => Task.FromResult(new HttpResults { code = 503, status = false, results = "" });
            try
            {
                await api.WebSearchVideo("fixture");
                Check(false, "control", "WBI failure propagates");
            }
            catch (InvalidOperationException) { Check(true, "control", "WBI failure propagates"); }
            ApiHelper.wbitoken_current = new ApiHelper.wbiimg { img_url = Keys.ImgKey, sub_url = Keys.SubKey };
            Api01TestHooks.RequestHandler = _ => throw new Exception("Unexpected request outside offline WBI fixture");
            Check(WbiKeyService.Sign("foo=114&bar=514&baz=1919810", Keys, DateTimeOffset.FromUnixTimeSeconds(1702204169)) ==
                "bar=514&baz=1919810&foo=114&wts=1702204169&w_rid=6149fdadf571698ca7e6a567265cd0ee", "control", "independent WBI vector");

            const string keyword = "中文 a&b=2+% 😀!'()*";
            const string cleanKeyword = "中文 a&b=2+% 😀";
            var builders = new (string Name, string Type, Func<string, Task<ApiModel>> Build)[]
            {
                ("video", "video", a => api.WebSearchVideo(keyword, 3, "click", "2", "17", a)),
                ("anime", "media_bangumi", a => api.WebSearchAnime(keyword, 3, a)),
                ("movie", "media_ft", a => api.WebSearchMovie(keyword, 3, a)),
                ("user", "bili_user", a => api.WebSearchUser(keyword, 3, "&order=fans&order_sort=1", "&user_type=1", a)),
                ("live", "live", a => api.WebSearchLive(keyword, 3, a)),
                ("article", "article", a => api.WebSearchArticle(keyword, 3, "pubdate", "4", a)),
                ("topic", "topic", a => api.WebSearchTopic(keyword, 3, a))
            };
            foreach (var builder in builders)
            foreach (var apiBase in new[] { "https://api.bilibili.com", "https://configured-api.invalid" })
            foreach (var area in new[] { null, "", "cn", "hk", "tw", "unknown", " " })
            {
                var proxy = !string.IsNullOrEmpty(area);
                var name = builder.Name + "/" + apiBase + "/area=" + (area == null ? "<null>" : "[" + area + "]");
                ApiHelper.API_BASE_URL = apiBase;
                Api01TestHooks.ProxyAreas.Clear();
                // A primitive return value deliberately independent of area or host policy.
                Api01TestHooks.ProxySelector = _ => "https://selected-proxy.invalid";
                var request = await builder.Build(area);
                Check(request.baseUrl == (proxy ? "https://selected-proxy.invalid" : apiBase) + "/x/web-interface/wbi/search/type", "control", name + " route");
                Check(Api01TestHooks.ProxyAreas.Count == (proxy ? 1 : 0) && (!proxy || Api01TestHooks.ProxyAreas[0] == area), "control", name + " selector input/count");
                Check(request.method == RestSharp.Method.Get && request.need_cookie, "control", name + " GET/cookie");
                Check(!request.parameter.StartsWith("?") && request.url.Count(c => c == '?') == 1, "control", name + " query delimiter");
                var query = HttpUtility.ParseQueryString(request.parameter);
                Check(query["search_type"] == builder.Type && query["page"] == "3" && query["keyword"] == cleanKeyword, "control", name + " type/page/keyword and existing WBI sanitizer");
                Check(proxy ? query.GetValues("area")?.Length == 1 && query["area"] == area : query["area"] == null, "control", name + " area membership");
                Check(query["context"] == "" && query["__refresh__"] == "true" && query["highlight"] == "1" && query["single_column"] == "0", "control", name + " common fields");
                if (builder.Name == "video")
                    Check(query["order"] == "click" && query["duration"] == "2" && query["tids"] == "17" && query["category_id"] == "" && query["tids_2"] == "", "control", name + " video filters");
                if (builder.Name == "user")
                    Check(query["order"] == "fans" && query["order_sort"] == "1" && query["user_type"] == "1" && query["changing"] == "mid" && query["category_id"] == "", "control", name + " user fragments");
                if (builder.Name == "article")
                    Check(query["order"] == "pubdate" && query["category_id"] == "4", "control", name + " article filters");
                if (builder.Name == "live")
                    Check(query["cover_type"] == "user_cover" && query["changing"] == "mid", "control", name + " live fields");
                if (builder.Name == "anime" || builder.Name == "movie" || builder.Name == "topic")
                    Check(query["order"] == "" && query["category_id"] == "", "control", name + " empty auxiliary fields");
                Check(query.GetValues("wts")?.Length == 1 && query.GetValues("w_rid")?.Length == 1 && query["w_rid"]?.Length == 32, "control", name + " unique WBI fields");
                var unsigned = string.Join("&", request.parameter.Split('&').Where(v => !v.StartsWith("w_rid=")));
                Check(WbiKeyService.Sign(unsigned, Keys, DateTimeOffset.FromUnixTimeSeconds(long.Parse(query["wts"]))) == request.parameter, "control", name + " real WBI signature over final fields");
                var overrides = request.ExtraCookies;
                Check(proxy ? overrides != null && overrides.Count == 1 && overrides.TryGetValue("buvid3", out var identity) && identity == "temp" :
                    overrides == null || !overrides.Keys.Any(k => string.Equals(k, "buvid3", StringComparison.OrdinalIgnoreCase)), "U5", name + " BUVID policy");
                Check(query["buvid3"] == null, "control", name + " no query identity");
            }

            // Explicit mode remains decisive even when the selected proxy is the official host.
            Api01TestHooks.ProxySelector = _ => "https://api.bilibili.com";
            foreach (var builder in builders)
            {
                var request = await builder.Build("cn");
                Check(request.baseUrl.StartsWith("https://api.bilibili.com/") && request.ExtraCookies?["buvid3"] == "temp", "control", builder.Name + " explicit mode with official-host proxy");
            }

            ApiHelper.LoginKey = new ApiKeyInfo("dummy-app", "dummy-secret");
            foreach (var loggedIn in new[] { false, true })
            foreach (var order in new[] { null, "", "view", "pubdate", "order&injected=1+% 😀", "unknown-order" })
            {
                SettingHelper.Account.Logined = loggedIn;
                SettingHelper.Account.AccessKey = "access-fixture";
                Api01TestHooks.ProxyAreas.Clear();
                var request = api.Search(keyword, order, 2, 17, 3, 30);
                var query = HttpUtility.ParseQueryString(request.parameter);
                var name = "app/" + loggedIn + "/" + (order ?? "<null>");
                Check(request.baseUrl == "https://app.bilibili.com/x/v2/search" && request.method == RestSharp.Method.Get && !request.need_cookie && Api01TestHooks.ProxyAreas.Count == 0, "control", name + " app route/cookie/selector");
                Check(query.GetValues("order")?.Length == 1 && query["order"] == (order ?? "") && query["injected"] == null, "U8", name + " one exact encoded order");
                Check(query["keyword"] == keyword && query["duration"] == "2" && query["rid"] == "17" && query["pn"] == "3" && query["ps"] == "30", "control", name + " other inputs");
                Check(query["appkey"] == "dummy-app" && query["ts"] == "123" && query["local_time"] == "123" && query["build"] == ApiHelper.build && query["mobi_app"] == "android" && query["platform"] == "android" &&
                    (loggedIn ? query["access_key"] == "access-fixture" : query["access_key"] == null), "control", name + " auth/timestamps/platform");
                Check(query["fnval"] == "16" && query["fnver"] == "0" && query["force_host"] == "0" && query["fourk"] == "1" && query["from_source"] == "app_search" && query["highlight"] == "0" && query["is_org_query"] == "0" && query["qn"] == "112" && query["recommend"] == "1", "control", name + " fixed flags");
                var unsigned = string.Join("&", request.parameter.Split('&').Where(v => !v.StartsWith("sign=")));
                Check(query.GetValues("sign")?.Length == 1 && "&sign=" + query["sign"] == ApiHelper.GetSign(unsigned, ApiHelper.LoginKey), "control", name + " real app signature over final fields");
            }
            var defaults = HttpUtility.ParseQueryString(api.Search(keyword).parameter);
            Check(defaults["duration"] == null && defaults["rid"] == null && defaults["pn"] == "1" && defaults["ps"] == "20" && defaults.GetValues("order")?.Length == 1 && defaults["order"] == "", "control", "app defaults");
        }
        finally
        {
            ApiHelper.API_BASE_URL = oldBase;
            ApiHelper.LoginKey = oldLoginKey;
            ApiHelper.wbitoken_current = oldKeys;
            SettingHelper.Account.Logined = oldLoggedIn;
            SettingHelper.Account.AccessKey = oldAccessKey;
            Api01TestHooks.RequestHandler = oldHandler;
            Api01TestHooks.ProxySelector = oldSelector;
            Api01TestHooks.ProxyAreas.Clear();
            Api01TestHooks.ProxyAreas.AddRange(oldAreas);
        }
        Console.WriteLine("SEARCH PASSED=" + passed + " FAILED=" + Failures.Values.Sum() + " U5=" + Failures.GetValueOrDefault("U5") + " U8=" + Failures.GetValueOrDefault("U8") + " CONTROL=" + Failures.GetValueOrDefault("control"));
        if (Failures.Count != 0) throw new Exception("Search request checks failed");
    }
}

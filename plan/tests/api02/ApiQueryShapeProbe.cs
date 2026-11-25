using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using BiliLite.Api;
using BiliLite.Api.User;
using BiliLite.Helpers;
using BiliLite.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class ApiQueryShapeProbe
{
    private static readonly WbiKey Keys = new WbiKey("7cd084941338484aae1ad9425b84077c", "4932caff0ff746eab6f01bf08b70ac45");
    private static readonly Dictionary<string, int> Failures = new Dictionary<string, int>();
    private static readonly Dictionary<string, int> Checks = new Dictionary<string, int>();

    private static void Check(bool ok, string category, string name)
    {
        Checks[category] = Checks.GetValueOrDefault(category) + 1;
        if (ok) return;
        Failures[category] = Failures.GetValueOrDefault(category) + 1;
        Console.WriteLine("QUERY FAIL " + category + ": " + name);
    }

    private static string Clean(string value) => new string((value ?? "").Where(c => "!'()*".IndexOf(c) < 0).ToArray());
    private static bool One(NameValueCollection q, string key, string value) => q.GetValues(key)?.Length == 1 && q[key] == value;

    private static void Signed(ApiModel request, string route, bool cookie, string name, params string[] fields)
    {
        var q = HttpUtility.ParseQueryString(request.parameter);
        Check(request.baseUrl == ApiHelper.API_BASE_URL + route && request.method == RestSharp.Method.Get && request.need_cookie == cookie,
            "control", name + " configured host/route/method/cookie");
        Check(request.body == null && request.ExtraCookies == null && !request.parameter.StartsWith("?") && request.url.Count(c => c == '?') == 1,
            "control", name + " body/extra cookies/delimiter");
        Check(q.AllKeys.OrderBy(k => k, StringComparer.Ordinal).SequenceEqual(fields.Concat(new[] { "wts", "w_rid" }).OrderBy(k => k, StringComparer.Ordinal)) &&
            q.AllKeys.All(k => q.GetValues(k)?.Length == 1), "control", name + " exact unique membership");
        var timestamp = 0L;
        Check(One(q, "w_rid", q["w_rid"]) && q["w_rid"]?.Length == 32 &&
            long.TryParse(q["wts"], NumberStyles.None, CultureInfo.InvariantCulture, out timestamp), "control", name + " final timestamp/digest shape");
        // Independently canonicalize the final decoded fields, using the fixed mixin for the dummy key pair.
        // The retained known vector below also detects a shared canonicalization/signing regression.
        var canonical = string.Join("&", q.AllKeys.Where(k => k != "w_rid").OrderBy(k => k, StringComparer.Ordinal)
            .Select(k => k + "=" + Uri.EscapeDataString(Clean(q[k]))));
        using (var md5 = MD5.Create())
        {
            var digest = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(canonical + "ea1db124af3c7062474693fa704f4ff8"))).Replace("-", "").ToLowerInvariant();
            Check(request.parameter == canonical + "&w_rid=" + digest, "control", name + " actual final canonical query/digest at wts=" + timestamp);
        }
    }

    private static bool Offset(ApiModel request, string expected)
    {
        try
        {
            var q = HttpUtility.ParseQueryString(request.parameter);
            var json = JObject.Parse(q["pagination_str"]);
            return q.GetValues("pagination_str")?.Length == 1 && json.Properties().Count() == 1 && json.Property("offset") != null &&
                (expected == null ? json["offset"].Type == JTokenType.Null : json["offset"].Type == JTokenType.String && (string)json["offset"] == Clean(expected)) &&
                q["pagination_str"] == Clean(JsonConvert.SerializeObject(new { offset = expected }));
        }
        catch (JsonException) { return false; }
    }

    // Same original counterexample values and 25 checks, alongside the untouched discovery artifact.
    private static async Task OriginalDiscoveryCases()
    {
        var api = new UserDetailAPI();
        foreach (var text in new[] { "C++", "A&B", "literal%2Bvalue", "中文 + % &pn=999&order=click" })
        {
            var r = await api.SubmitVideosWbi("42", 3, 30, text, 17, SubmitVideoOrder.pubdate);
            var q = HttpUtility.ParseQueryString(r.parameter);
            Check(q["keyword"] == text, "AS1-original", "space keyword " + text);
            Check(One(q, "pn", "3") && One(q, "order", "pubdate"), "AS1-original", "space controls " + text);
            var f = await api.Followings("42", 3, 30, -1, text);
            var fq = HttpUtility.ParseQueryString(f.parameter);
            Check(fq["name"] == text, "AS1-original", "following name " + text);
            Check(One(fq, "pn", "3") && One(fq, "order", "desc"), "AS1-original", "following controls " + text);
        }
        foreach (var offset in new[] { "C++", "literal%2Bvalue", "cursor&ps=999", "{\"session\":\"A+B%2B&C\"}" })
        {
            var r = await new CommentApi().CommentV2("42", CommentApi.CommentSort.Hot, 3, 1, 30, offset, false);
            Check(Offset(r, offset), "AS2-original", "comment offset " + offset);
            Check(One(HttpUtility.ParseQueryString(r.parameter), "ps", "30"), "AS2-original", "comment page-size " + offset);
        }
        Check(WbiKeyService.Sign("foo=114&bar=514&baz=1919810", Keys, DateTimeOffset.FromUnixTimeSeconds(1702204169)) ==
            "bar=514&baz=1919810&foo=114&wts=1702204169&w_rid=6149fdadf571698ca7e6a567265cd0ee", "control", "independent WBI vector");
    }

    public static async Task RunAsync()
    {
        var oldBase = ApiHelper.API_BASE_URL;
        var oldKeys = ApiHelper.wbitoken_current;
        var oldHandler = Api01TestHooks.RequestHandler;
        var oldLogin = SettingHelper.Account.Logined;
        var oldCsrf = SettingHelper.GetValue("CookieCSRF", "");
        try
        {
            ApiHelper.wbitoken_current = new ApiHelper.wbiimg { img_url = Keys.ImgKey, sub_url = Keys.SubKey };
            Api01TestHooks.RequestHandler = _ => throw new Exception("Query probe attempted a network request");
            SettingHelper.SetValue("CookieCSRF", "csrf-query-fixture");
            await OriginalDiscoveryCases();
            var user = new UserDetailAPI();
            var texts = new[] { null, "", "plain", "C++", "A&B", "a=b", "%", "literal%2Bvalue", "中文 😀", " \t\r\n ", "back\\slash\"quote", "!'()*", "中文 + % &pn=999&order=click&injected=1" };
            foreach (var host in new[] { "https://api.bilibili.com", "https://configured-api.invalid" })
            {
                ApiHelper.API_BASE_URL = host;
                foreach (var text in texts)
                foreach (SubmitVideoOrder order in Enum.GetValues(typeof(SubmitVideoOrder)))
                foreach (var alias in new[] { false, true })
                {
                    var name = "submit/" + host + "/" + order + "/alias=" + alias + "/" + JsonConvert.SerializeObject(text);
                    var r = alias ? await user.SubmitVideosWbi("42", 3, 27, text, 17, order) : await user.SubmitVideos("42", 3, 27, text, 17, order);
                    var q = HttpUtility.ParseQueryString(r.parameter);
                    Check(One(q, "keyword", Clean(text)), "AS1", name + " raw text escaped once and sanitized");
                    Check(One(q, "mid", "42") && One(q, "pn", "3") && One(q, "ps", "27") && One(q, "tid", "17") && One(q, "order", order.ToString()), "AS1", name + " controls");
                    Check(r.headers?.Count == 1 && r.headers.TryGetValue("referer", out var referer) && referer == "https://space.bilibili.com", "control", name + " referer");
                    Signed(r, "/x/space/wbi/arc/search", true, name, "mid", "ps", "tid", "pn", "keyword", "order");
                }
                foreach (var text in texts)
                foreach (FollowingsOrder order in Enum.GetValues(typeof(FollowingsOrder)))
                foreach (var tid in new[] { -1, 0, 17 })
                {
                    var name = "following/" + host + "/" + tid + "/" + order + "/" + JsonConvert.SerializeObject(text);
                    var r = await user.Followings("42", 3, 27, tid, text, order);
                    var q = HttpUtility.ParseQueryString(r.parameter);
                    // Null historically selects search: keyword != "" is intentionally retained.
                    var search = tid == -1 && text != "";
                    var route = tid != -1 ? "/x/relation/tag" : search ? "/x/relation/followings/search" : "/x/relation/followings";
                    var fields = new List<string> { "vmid", "ps", "pn", "order", "order_type" };
                    if (search) fields.Add("name");
                    if (tid != -1) fields.AddRange(new[] { "tagid", "mid" });
                    Check(search ? One(q, "name", Clean(text)) : q["name"] == null, "AS1", name + " search field/branch");
                    Check(One(q, "vmid", "42") && One(q, "ps", "27") && One(q, "pn", "3") && One(q, "order", "desc") && One(q, "order_type", order == FollowingsOrder.attention ? "attention" : ""), "AS1", name + " controls/order");
                    Check(tid == -1 ? q["tagid"] == null && q["mid"] == null : One(q, "tagid", tid.ToString()) && One(q, "mid", "42"), "control", name + " tag controls");
                    Check(r.headers == null, "control", name + " no new headers");
                    Signed(r, route, true, name, fields.ToArray());
                }
                foreach (var offset in texts.Concat(new[] { "cursor&ps=999", "{\"session\":\"A+B%2B&C\"}", "quote\"slash\\\r\n\t\u0001" }))
                foreach (var login in new[] { false, true })
                foreach (var sort in new[] { CommentApi.CommentSort.Hot, CommentApi.CommentSort.New })
                {
                    SettingHelper.Account.Logined = login;
                    var name = "comment/" + host + "/" + login + "/" + sort + "/" + JsonConvert.SerializeObject(offset);
                    var r = await new CommentApi().CommentV2("42", sort, 7, 17, 27, offset, login);
                    var q = HttpUtility.ParseQueryString(r.parameter);
                    Check(Offset(r, offset), "AS2", name + " exact serialized JSON/offset");
                    Check(One(q, "oid", "42") && One(q, "type", "17") && One(q, "ps", "27") && One(q, "mode", sort == CommentApi.CommentSort.Hot ? "3" : "2"), "AS2", name + " controls");
                    Check(login ? One(q, "csrf", "csrf-query-fixture") : q["csrf"] == null, "control", name + " optional csrf");
                    Check(r.headers == null, "control", name + " no new headers");
                    var fields = new List<string> { "oid", "ps", "mode", "type", "pagination_str" };
                    if (login) fields.Add("csrf");
                    Signed(r, "/x/v2/reply/wbi/main", login, name, fields.ToArray());
                }
            }
            var defaults = HttpUtility.ParseQueryString((await user.SubmitVideos("42")).parameter);
            Check(One(defaults, "keyword", "") && One(defaults, "ps", "30") && One(defaults, "pn", "1") && One(defaults, "tid", "0") && One(defaults, "order", "pubdate"), "control", "submit defaults");
            var commentDefault = await new CommentApi().CommentV2("42", CommentApi.CommentSort.New, 1, 1);
            Check(Offset(commentDefault, null) && One(HttpUtility.ParseQueryString(commentDefault.parameter), "ps", "30") && commentDefault.need_cookie, "control", "comment defaults/null offset");
        }
        finally
        {
            ApiHelper.API_BASE_URL = oldBase;
            ApiHelper.wbitoken_current = oldKeys;
            Api01TestHooks.RequestHandler = oldHandler;
            SettingHelper.Account.Logined = oldLogin;
            SettingHelper.SetValue("CookieCSRF", oldCsrf);
        }
        foreach (var category in Checks.Keys.OrderBy(k => k, StringComparer.Ordinal))
            Console.WriteLine("QUERY " + category + " CHECKS=" + Checks[category] + " FAILED=" + Failures.GetValueOrDefault(category));
        Console.WriteLine("QUERY TOTAL=" + Checks.Values.Sum() + " FAILED=" + Failures.Values.Sum());
        if (Failures.Count != 0) throw new Exception("API query shape checks failed");
    }
}

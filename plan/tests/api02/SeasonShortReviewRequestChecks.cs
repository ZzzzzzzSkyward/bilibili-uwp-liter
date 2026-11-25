using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using BiliLite.Api;
using BiliLite.Helpers;

internal static class SeasonShortReviewRequestChecks
{
    public static void Run()
    {
        var assertions = 0;
        var failures = new List<string>();
        void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) failures.Add(message);
        }

        var savedHost = ApiHelper.API_BASE_URL;
        var savedLogin = SettingHelper.Account.Logined;
        var savedAccessKey = SettingHelper.Account.AccessKey;
        var cursors = new[]
        {
            null, "", "ordinary_abc-123", "0000123", "9007199254740993", "-1",
            "C++", "literal%2Bvalue", "literal%25value", "cursor&sort=99",
            "cursor&cursor=other", "cursor#fragment", "中文 + % &media_id=99",
            "back\\slash\"quote", "中文 😀", "line\r\nnext", "tab\tzero\0end\u001f",
            "question?equals=semi;", "{\"next\":\"a+b&c%2B#d\"}"
        };

        try
        {
            SettingHelper.Account.AccessKey = "short-review-fixture";
            foreach (var host in new[] { "https://api.bilibili.com", "https://configured.invalid" })
            foreach (var login in new[] { false, true })
            {
                ApiHelper.API_BASE_URL = host;
                SettingHelper.Account.Logined = login;
                foreach (var media in new[] { 0, 42, int.MaxValue })
                foreach (var sort in new[] { -1, 0, 2 })
                foreach (var cursor in cursors)
                {
                    var label = host + "/" + login + "/" + media + "/" + sort + "/cursor case " + Array.IndexOf(cursors, cursor);
                    var request = new SeasonApi().ShortReview(media, cursor, sort);
                    var uri = new Uri(request.url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    Check(query.GetValues("cursor")?.Length == 1 && query["cursor"] == (cursor ?? ""),
                        label + " exact opaque cursor after one final-URI query decode");
                    Check(query.GetValues("media_id")?.Length == 1 && query["media_id"] == media.ToString(CultureInfo.InvariantCulture) &&
                        query.GetValues("ps")?.Length == 1 && query["ps"] == "20" &&
                        query.GetValues("sort")?.Length == 1 && query["sort"] == sort.ToString(CultureInfo.InvariantCulture),
                        label + " single unchanged media/page-size/sort controls");
                    var fields = login ? new[] { "media_id", "ps", "sort", "cursor", "access_key" }
                        : new[] { "media_id", "ps", "sort", "cursor" };
                    Check(query.AllKeys.SequenceEqual(fields) &&
                        (login ? query.GetValues("access_key")?.Length == 1 && query["access_key"] == SettingHelper.Account.AccessKey &&
                            request.parameter.EndsWith("&access_key=" + SettingHelper.Account.AccessKey, StringComparison.Ordinal)
                            : query.GetValues("access_key") == null),
                        label + " exact ordered fields and optional access-key value/placement");
                    Check(uri.Fragment.Length == 0, label + " no cursor text escapes into fragment");
                    Check(request.method == RestSharp.Method.Get && !request.need_cookie &&
                        request.body == null && request.headers == null && request.ExtraCookies == null &&
                        request.baseUrl == host + "/pgc/review/short/list" && uri.Scheme == "https" &&
                        uri.Host == new Uri(host).Host && uri.AbsolutePath == "/pgc/review/short/list" &&
                        query["sign"] == null && query["appkey"] == null && query["w_rid"] == null && query["wts"] == null,
                        label + " configured route GET/no-cookie/no-app/no-WBI policy");
                }

                var defaults = new SeasonApi().ShortReview(42);
                var defaultQuery = HttpUtility.ParseQueryString(new Uri(defaults.url).Query);
                Check(defaultQuery.GetValues("cursor")?.Length == 1 && defaultQuery["cursor"] == "" &&
                    defaultQuery["sort"] == "0" && defaultQuery["ps"] == "20" && defaultQuery["media_id"] == "42",
                    host + "/" + login + " omitted next/sort include one empty cursor and original defaults");
            }
        }
        finally
        {
            ApiHelper.API_BASE_URL = savedHost;
            SettingHelper.Account.Logined = savedLogin;
            SettingHelper.Account.AccessKey = savedAccessKey;
        }

        Console.WriteLine("Season short-review request checks: " + assertions + " assertions, " + failures.Count + " failures");
        if (failures.Count != 0)
            throw new Exception("Season short-review request checks failed: " + string.Join("; ", failures));
        Console.WriteLine("PASS " + assertions + " season short-review request assertions");
    }
}

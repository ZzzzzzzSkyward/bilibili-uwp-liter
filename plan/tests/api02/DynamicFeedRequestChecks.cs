using System;
using System.Collections.Generic;
using System.Web;
using BiliLite.Api.User;

internal static class DynamicFeedRequestChecks
{
    private const string Features = "itemOpusStyle,listOnlyfans,opusBigCover,onlyfansVote,decorationCard,onlyfansAssetsV2,forwardListHidden,ugcDelete,onlyfansQaCard,commentsNewVersion,avatarAutoTheme,sunflowerStyle,cardsEnhance,eva3CardOpus,eva3CardVideo,eva3CardComment";

    public static void Run()
    {
        var assertions = 0;
        var failures = new List<string>();
        void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) failures.Add(message);
        }

        var offsets = new[]
        {
            null, "", "plain", "0000123", "9007199254740993", "-1",
            "C++", "literal%2Bvalue", "literal%25value", "cursor&features=changed",
            "cursor#fragment", "中文 + % &page=999", "token&offset=other",
            "back\\slash\"quote", "中文 😀", "line\r\nnext", "tab\tzero\0end\u001f", "question?equals=semi;"
        };

        foreach (var type in new[] { "all", "video", "article" })
        foreach (var offset in offsets)
        {
            var label = type + " offset case " + Array.IndexOf(offsets, offset);
            var request = WebDynamicFeedApi.CreateFeedRequest(3, offset, type);
            // This is the same final URL boundary used by the transport.
            var uri = new Uri(request.url);
            var query = HttpUtility.ParseQueryString(uri.Query);
            Check(request.method == RestSharp.Method.Get && request.need_cookie &&
                request.baseUrl == "https://api.bilibili.com/x/polymer/web-dynamic/v1/feed/all" &&
                uri.Scheme == "https" && uri.Host == "api.bilibili.com" &&
                uri.AbsolutePath == "/x/polymer/web-dynamic/v1/feed/all", label + " cookie GET route");
            Check(offset == null
                ? query.GetValues("offset") == null
                : query.GetValues("offset")?.Length == 1 && query["offset"] == offset,
                label + " exact opaque value after one final-URI query decode");
            Check(query.GetValues("page")?.Length == 1 && query["page"] == "3" &&
                query.GetValues("type")?.Length == 1 && query["type"] == type &&
                query.GetValues("timezone_offset")?.Length == 1 && query["timezone_offset"] == "-480" &&
                query.GetValues("features")?.Length == 1 && query["features"] == Features,
                label + " single unchanged control values");
            Check(query.Count == (offset == null ? 4 : 5) &&
                query["sign"] == null && query["w_rid"] == null && query["wts"] == null &&
                query["access_key"] == null && query["appkey"] == null,
                label + " unchanged field set without app or WBI signing");
            Check(uri.Fragment.Length == 0, label + " no offset text escapes into fragment");
        }

        Console.WriteLine("Dynamic feed request checks: " + assertions + " assertions, " + failures.Count + " failures");
        if (failures.Count != 0)
            throw new Exception("Dynamic feed request checks failed: " + string.Join("; ", failures));
        Console.WriteLine("PASS " + assertions + " dynamic feed request assertions");
    }
}

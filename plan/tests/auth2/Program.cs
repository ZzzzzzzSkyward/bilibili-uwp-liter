using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BiliLite.Helpers;
using BiliLite.Models;
using BiliLite.Services;
using WinCookie = Windows.Web.Http.HttpCookie;
using WinFilter = Windows.Web.Http.Filters.HttpBaseProtocolFilter;

internal static class Program
{
    private static readonly string[] ExpectedOrigins =
    {
        "https://bilibili.com/",
        "https://www.bilibili.com/",
        "https://passport.bilibili.com/",
        "https://api.bilibili.com/"
    };

    private static int assertions;

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception("AUTH-02 failed: " + message);
    }

    private static void Main()
    {
        SharedOriginPolicy();
        FirstValueRules();
        WriteSeedsBothHosts();
        ReadSeesEveryOrigin();
        ReadPrefersFirstNonEmptyOrigin();
        ReadFallsBackToDefault();
        SettingsOnlyCookieIsWrittenToBothHosts();
        LoginSubHostCookiesRecoverIntoSettings();
        RefreshStateLogsTheVerdict();
        SourceChecks();
        Console.WriteLine("PASS " + assertions + " assertions");
    }

    private static void SharedOriginPolicy()
    {
        Check(CookieOrigins.All.SequenceEqual(ExpectedOrigins),
            "the shared policy lists bilibili, www, passport and api in that order");
        Check(CookieOrigins.WriteHosts.SequenceEqual(new[] { "bilibili.com", "www.bilibili.com" }),
            "the write policy covers the root domain and www, the two hosts a bilibili page resolves through");
    }

    private static void FirstValueRules()
    {
        Check(CookieOrigins.FirstValue(new[] { "", "value" }) == "value",
            "the first non-empty value wins");
        Check(CookieOrigins.FirstValue(new[] { "", "   ", "\t", "second" }) == "second",
            "whitespace-only entries are skipped instead of shadowing a real value");
        Check(CookieOrigins.FirstValue(new[] { "", "   ", "" }) == "",
            "an all-empty list resolves to the empty string");
        Check(CookieOrigins.FirstValue(new string[0]) == "",
            "an empty list resolves to the empty string");
        Check(CookieOrigins.FirstValue(null) == "",
            "a missing list resolves to the empty string");
        Check(CookieOrigins.FirstValue(new[] { " x " }) == " x ",
            "the selected value is returned verbatim, not trimmed");
    }

    private static void WriteSeedsBothHosts()
    {
        var store = NewStore(out var filter);
        store.Write("ac_time_value", "token-1");
        var written = filter.CookieManager.All.Where(cookie => cookie.Name == "ac_time_value").ToList();

        Check(written.Count == 2, "one write lands exactly one cookie per configured host");
        Check(written.Any(cookie => cookie.Domain == "bilibili.com") && written.Any(cookie => cookie.Domain == "www.bilibili.com"),
            "both copies carry their own domain instead of relying on inheritance");
        Check(written.All(cookie => cookie.Value == "token-1" && cookie.Path == "/"),
            "both copies carry the same value on the root path");
        Check(store.Read("ac_time_value", "") == "token-1",
            "the freshly written value reads back through the same policy");
    }

    private static void ReadSeesEveryOrigin()
    {
        foreach (var origin in ExpectedOrigins)
        {
            var host = new Uri(origin).Host;
            var store = NewStore(out var filter);
            filter.CookieManager.SetCookie(new WinCookie("ac_time_value", host, "/") { Value = "v:" + host });

            Check(store.Read("ac_time_value", "") == "v:" + host,
                "a host-only cookie issued by " + host + " is readable through the shared policy");
            if (host != "bilibili.com")
                Check(filter.CookieManager.GetCookies(new Uri("https://bilibili.com/")).All(cookie => cookie.Name != "ac_time_value"),
                    "the " + host + " cookie really is host-only, so a root-domain-only read would miss it");
        }
    }

    private static void ReadPrefersFirstNonEmptyOrigin()
    {
        var store = NewStore(out var filter);
        filter.CookieManager.SetCookie(new WinCookie("ac_time_value", "bilibili.com", "/") { Value = "root" });
        filter.CookieManager.SetCookie(new WinCookie("ac_time_value", "www.bilibili.com", "/") { Value = "www" });
        Check(store.Read("ac_time_value", "") == "root", "the root-domain copy wins over the www copy");

        store = NewStore(out filter);
        filter.CookieManager.SetCookie(new WinCookie("ac_time_value", "bilibili.com", "/") { Value = "   " });
        filter.CookieManager.SetCookie(new WinCookie("ac_time_value", "www.bilibili.com", "/") { Value = "www" });
        Check(store.Read("ac_time_value", "") == "www",
            "a whitespace-only root copy is skipped instead of shadowing the www value");

        store = NewStore(out filter);
        filter.CookieManager.SetCookie(new WinCookie("ac_time_value", "bilibili.com", "/") { Value = "" });
        filter.CookieManager.SetCookie(new WinCookie("ac_time_value", "api.bilibili.com", "/") { Value = "api" });
        Check(store.Read("ac_time_value", "") == "api", "an empty root copy does not stop the scan before the api host");
    }

    private static void ReadFallsBackToDefault()
    {
        var store = NewStore(out var filter);
        filter.CookieManager.SetCookie(new WinCookie("other", "www.bilibili.com", "/") { Value = "unrelated" });
        Check(store.Read("ac_time_value", "fallback") == "fallback",
            "a jar without the name returns the caller's default");
    }

    private static void SettingsOnlyCookieIsWrittenToBothHosts()
    {
        var settings = new Dictionary<string, string> { ["CookieCSRF"] = "csrf-from-setting" };
        var store = NewStore(out var filter);
        var service = NewService(settings, store);

        Check(service.GetCSRF() == "csrf-from-setting", "a CSRF that only lives in settings is served back");
        var written = filter.CookieManager.All
            .Where(cookie => cookie.Name == "bili_jct").Select(cookie => cookie.Domain).OrderBy(x => x).ToList();
        Check(written.SequenceEqual(new[] { "bilibili.com", "www.bilibili.com" }),
            "the settings-only value is seeded at the root domain and at www");
    }

    private static void LoginSubHostCookiesRecoverIntoSettings()
    {
        var settings = new Dictionary<string, string>();
        var store = NewStore(out var filter);
        filter.CookieManager.SetCookie(new WinCookie("SESSDATA", "api.bilibili.com", "/") { Value = "session-value" });
        filter.CookieManager.SetCookie(new WinCookie("bili_jct", "passport.bilibili.com", "/") { Value = "csrf-value" });
        filter.CookieManager.SetCookie(new WinCookie("ac_time_value", "www.bilibili.com", "/") { Value = "refresh-value" });

        NewService(settings, store).LoadCookieFromSetting();

        Check(settings["CookieSESSDATA"] == "session-value", "the api-hosted session mirrors into settings");
        Check(settings["CookieCSRF"] == "csrf-value", "the passport-hosted CSRF mirrors into settings");
        Check(settings["CookieRefreshToken"] == "refresh-value",
            "the host-only www refresh token mirrors into settings, so the refresh chain can start");
    }

    private static void RefreshStateLogsTheVerdict()
    {
        const string TokenValue = "secret-refresh-token";
        var settings = new Dictionary<string, string>
        {
            ["CookieCSRF"] = "csrf",
            ["CookieRefreshToken"] = TokenValue
        };
        var log = new List<string>();
        var store = NewStore(out var filter);
        var service = NewService(settings, store, log,
            _ => Task.FromResult(new HttpResults
            {
                code = 200,
                results = "{\"code\":0,\"data\":{\"timestamp\":123,\"refresh\":true}}"
            }));

        Check(service.NeedRefreshCookieAsync().GetAwaiter().GetResult(),
            "the state check reports the refresh flag the server asked for");
        var lines = log.Where(message => message.StartsWith("Cookie refresh state ", StringComparison.Ordinal)).ToList();
        Check(lines.Count == 1, "a healthy session leaves exactly one refresh-state line");
        Check(lines[0] == "Cookie refresh state refresh=True timestamp=123; token=present",
            "the line carries the flag, the timestamp and that a token exists");
        Check(log.All(message => !message.Contains(TokenValue)),
            "the refresh token value never reaches the log");

        var missingSettings = new Dictionary<string, string> { ["CookieCSRF"] = "csrf" };
        var missingLog = new List<string>();
        var missingStore = NewStore(out var missingFilter);
        var missingService = NewService(missingSettings, missingStore, missingLog,
            _ => Task.FromResult(new HttpResults
            {
                code = 200,
                results = "{\"code\":0,\"data\":{\"timestamp\":456,\"refresh\":false}}"
            }));

        Check(!missingService.NeedRefreshCookieAsync().GetAwaiter().GetResult(),
            "a session the server does not want to refresh stays unflagged");
        var missingLines = missingLog.Where(message => message.StartsWith("Cookie refresh state ", StringComparison.Ordinal)).ToList();
        Check(missingLines.Count == 1 && missingLines[0] == "Cookie refresh state refresh=False timestamp=456; token=missing",
            "a session without a refresh token says missing instead of printing one");
    }

    private static void SourceChecks()
    {
        var root = FindRepoRoot();
        var cookieService = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Services/CookieService.cs"));
        var start = cookieService.IndexOf("class WindowsCookieStore", StringComparison.Ordinal);
        var end = cookieService.IndexOf("public class CookieService", StringComparison.Ordinal);
        Check(start > 0 && end > start, "the WinRT store stays a separate class from the refresh lifecycle");
        var store = cookieService.Substring(start, end - start);

        Check(Regex.IsMatch(store, @"foreach \(var origin in CookieOrigins\.All\)") &&
            Regex.IsMatch(store, @"GetCookies\(new Uri\(origin\)\)"),
            "the store queries every shared origin instead of one hard-coded host");
        Check(store.Contains("CookieOrigins.FirstValue("),
            "the store resolves the read through the shared first-value rule");
        Check(Regex.IsMatch(store, @"foreach \(var host in CookieOrigins\.WriteHosts\)") &&
            Regex.IsMatch(store, @"SetCookie\(new HttpCookie\(name, host, ""/""\)"),
            "the store seeds the jar at each shared write host");
        Check(!store.Contains("https://bilibili.com"),
            "the store keeps no private origin literal");

        var account = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Modules/User/Account.cs"));
        Check(Regex.IsMatch(account,
                @"private static List<Uri> CookieLoginOrigins\(\) => CookieOrigins\.All\.Select\(x => new Uri\(x\)\)\.ToList\(\);"),
            "the login cookie paths use the shared list directly");
        Check(!account.Contains(@"""https://passport.bilibili.com/""") &&
            !account.Contains(@"""https://www.bilibili.com/"""),
            "Account keeps no private copy of the origin list");

        var validator = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Services/PasswordLoginSession.cs"));
        Check(validator.Contains("BiliLite.Models.CookieOrigins.All") &&
            !validator.Contains(@"new Uri(""https://bilibili.com/"")"),
            "the password-login validator reads the shared list instead of a second copy");

        var stateStart = cookieService.IndexOf("private async Task CheckCookieRefreshStateAsync", StringComparison.Ordinal);
        var stateEnd = cookieService.IndexOf("private bool NeedRefreshCookieValue", StringComparison.Ordinal);
        Check(stateStart > 0 && stateEnd > stateStart, "the refresh state check is a single method");
        var stateCheck = cookieService.Substring(stateStart, stateEnd - stateStart);
        Check(Regex.IsMatch(stateCheck,
                @"log\(""Cookie refresh state refresh="" \+ refreshFlag\.Value \+ "" timestamp="" \+ timestamp\.Value \+\s*""; token="" \+ \(string\.IsNullOrEmpty\(refreshToken\) \? ""missing"" : ""present""\), null\);"),
            "the state check logs the flag, the timestamp and only whether a token exists");
        Check(Regex.Matches(stateCheck, "token=").Count == 1,
            "the state check logs the token verdict exactly once");
        Check(!Regex.IsMatch(stateCheck, @"\+\s*refreshToken\b(?!\))") && !stateCheck.Contains("{refreshToken}"),
            "the log line never appends the token value");
        Check(stateCheck.IndexOf("Cookie refresh state refresh=", StringComparison.Ordinal) <
            stateCheck.IndexOf("SetRefreshState(true, refreshFlag.Value)", StringComparison.Ordinal),
            "the line is written before the new refresh state is published");

        var model = File.ReadAllText(Path.Combine(root, "src/BiliLite.UWP/Models/CookieOrigins.cs"));
        Check(ExpectedOrigins.All(origin => model.Contains(@"""" + origin + @"""")),
            "the shared model holds the four origin literals");

        var sources = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText).ToList();
        Check(sources.Count(text => ExpectedOrigins.All(origin => text.Contains(@"""" + origin + @""""))) == 1,
            "exactly one file in src holds the four-origin list");
    }

    private static WindowsCookieStore NewStore(out WinFilter filter)
    {
        var store = new WindowsCookieStore();
        filter = WinFilter.Created.Last();
        return store;
    }

    private static CookieService NewService(IDictionary<string, string> settings, ICookieStore store,
        List<string> log = null, Func<BiliLite.Api.ApiModel, Task<HttpResults>> send = null) =>
        new CookieService(store,
            key => settings.TryGetValue(key, out var value) ? value : "",
            (key, value) => settings[key] = value,
            () => true,
            send,
            null,
            log == null ? (Action<string, Exception>)null : (message, exception) => log.Add(message));

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src/BiliLite.UWP/Services/CookieService.cs")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new Exception("AUTH-02 failed: repository root not found from " + AppContext.BaseDirectory);
    }
}

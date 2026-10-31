using System;
using System.Collections.ObjectModel;
using System.Linq;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Modules;
using Newtonsoft.Json.Linq;

internal static class Program
{
    private static int assertions;

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception("D2 failed: " + message);
    }

    private static void Seed(string[] words, string[] regulars, string[] users, string[] live)
    {
        SettingHelper.Reset();
        SettingHelper.SetValue(SettingHelper.VideoDanmaku.SHIELD_WORD, new ObservableCollection<string>(words));
        SettingHelper.SetValue(SettingHelper.VideoDanmaku.SHIELD_REGULAR, new ObservableCollection<string>(regulars));
        SettingHelper.SetValue(SettingHelper.VideoDanmaku.SHIELD_USER, new ObservableCollection<string>(users));
        SettingHelper.SetValue(SettingHelper.Live.SHIELD_WORD, new ObservableCollection<string>(live));
    }

    private static string[] Stored(string key) =>
        SettingHelper.GetValue(key, new ObservableCollection<string>()).ToArray();

    private static void Main()
    {
        ExportShape();
        ImportDropsInvalidRegex();
        ImportMergesWithoutDuplicates();
        ImportDoesNotTouchLiveWords();
        UserListLandsOnUserKey();
        InvalidInputIsTolerated();
        RoundTripIsStable();
        Check(PlayerAPI.CallCount == 0, "import/export never calls playerAPI");
        Console.WriteLine("PASS " + assertions + " assertions");
    }

    // 导出只含 type/filter，且按 0/1/2 分组保持原顺序
    private static void ExportShape()
    {
        Seed(new[] { "前方高能", "2333" },
            new[] { "^测试.*$", "\\d{6}" },
            new[] { "10001", "10002" },
            new[] { "直播词" });
        var vm = new SettingVM();

        var json = vm.ExportDanmuFilterJson();
        var arr = JArray.Parse(json);
        Check(arr.Count == 6, "export emits one entry per filter (6)");

        foreach (var token in arr)
        {
            var obj = (JObject)token;
            Check(obj.Count == 2, "entry carries exactly two properties");
            Check(obj.Property("type") != null && obj.Property("filter") != null,
                "entry properties are type and filter only");
        }

        Check(arr.Select(x => (int)x["type"]).SequenceEqual(new[] { 0, 0, 1, 1, 2, 2 }),
            "types are emitted as 0 keywords, 1 regexes, 2 users in collection order");
        Check(arr.Where(x => (int)x["type"] == 0).Select(x => (string)x["filter"]).SequenceEqual(new[] { "前方高能", "2333" }),
            "keywords map to type 0");
        Check(arr.Where(x => (int)x["type"] == 1).Select(x => (string)x["filter"]).SequenceEqual(new[] { "^测试.*$", "\\d{6}" }),
            "regexes map to type 1");
        Check(arr.Where(x => (int)x["type"] == 2).Select(x => (string)x["filter"]).SequenceEqual(new[] { "10001", "10002" }),
            "users map to type 2");

        // 正则不在导出时被过滤，只有导入时校验
        vm.ShieldRegulars.Add("([bad");
        Check(vm.ExportDanmuFilterJson().Contains("([bad"), "export does not validate regexes");
        vm.ShieldRegulars.Remove("([bad");
    }

    // 合法正则入库，非法正则被丢弃
    private static void ImportDropsInvalidRegex()
    {
        Seed(new string[0], new string[0], new string[0], new string[0]);
        var vm = new SettingVM();

        var added = vm.ImportDanmuFilterJson(
            "[{\"type\":1,\"filter\":\"^good$\"},{\"type\":1,\"filter\":\"([bad\"}]");

        Check(added == 1, "invalid regex is not counted as added");
        Check(vm.ShieldRegulars.SequenceEqual(new[] { "^good$" }), "valid regex is imported");
        Check(!vm.ShieldRegulars.Contains("([bad"), "invalid regex is dropped");
        Check(Stored(SettingHelper.VideoDanmaku.SHIELD_REGULAR).SequenceEqual(new[] { "^good$" }),
            "validated regex list is persisted");
    }

    // 重叠导入只加新增项，重复导入幂等
    private static void ImportMergesWithoutDuplicates()
    {
        Seed(new[] { "前方高能" }, new string[0], new[] { "10001" }, new string[0]);
        var vm = new SettingVM();
        var file = "[{\"type\":0,\"filter\":\"前方高能\"},{\"type\":0,\"filter\":\"新词\"}," +
                   "{\"type\":2,\"filter\":\"10001\"},{\"type\":2,\"filter\":\"99999\"}]";

        Check(vm.ImportDanmuFilterJson(file) == 2, "overlapping import reports only the two new entries");
        Check(vm.ShieldWords.SequenceEqual(new[] { "前方高能", "新词" }), "keyword merged in place");
        Check(vm.ShieldUsers.SequenceEqual(new[] { "10001", "99999" }), "user merged in place");

        var before = vm.ExportDanmuFilterJson();
        Check(vm.ImportDanmuFilterJson(file) == 0, "second import adds nothing");
        Check(vm.ExportDanmuFilterJson() == before, "second import leaves the collection untouched");
    }

    // 导入永远不碰直播屏蔽词
    private static void ImportDoesNotTouchLiveWords()
    {
        Seed(new string[0], new string[0], new string[0], new[] { "直播词" });
        var liveBefore = SettingHelper.RawValue(SettingHelper.Live.SHIELD_WORD);
        var vm = new SettingVM();

        vm.ImportDanmuFilterJson("[{\"type\":0,\"filter\":\"弹幕词\"},{\"type\":2,\"filter\":\"10001\"}]");

        Check(vm.LiveWords.SequenceEqual(new[] { "直播词" }), "LiveWords unchanged in memory");
        Check(SettingHelper.RawValue(SettingHelper.Live.SHIELD_WORD) == liveBefore, "LiveWords unchanged in storage");
    }

    // 用户进 SHIELD_USER，关键词进 SHIELD_WORD，互不覆盖
    private static void UserListLandsOnUserKey()
    {
        Seed(new[] { "前方高能" }, new string[0], new string[0], new string[0]);
        var vm = new SettingVM();
        var wordsBefore = SettingHelper.RawValue(SettingHelper.VideoDanmaku.SHIELD_WORD);

        Check(vm.ImportDanmuFilterJson("[{\"type\":2,\"filter\":\"777\"}]") == 1, "user-only import adds one entry");

        Check(SettingHelper.RawValue(SettingHelper.VideoDanmaku.SHIELD_WORD) == wordsBefore,
            "user import leaves the keyword key byte-identical");
        Check(Stored(SettingHelper.VideoDanmaku.SHIELD_USER).SequenceEqual(new[] { "777" }),
            "users land under SHIELD_USER");
        Check(!Stored(SettingHelper.VideoDanmaku.SHIELD_WORD).Contains("777"),
            "users never leak into SHIELD_WORD");

        Check(vm.ImportDanmuFilterJson("[{\"type\":0,\"filter\":\"新词\"}]") == 1, "keyword import adds one entry");
        Check(Stored(SettingHelper.VideoDanmaku.SHIELD_WORD).SequenceEqual(new[] { "前方高能", "新词" }),
            "keywords land under SHIELD_WORD");
        Check(Stored(SettingHelper.VideoDanmaku.SHIELD_USER).SequenceEqual(new[] { "777" }),
            "keyword import leaves SHIELD_USER intact");
    }

    // 空/非法 JSON 返回 0、不抛异常、只记一条日志
    private static void InvalidInputIsTolerated()
    {
        Seed(new[] { "前方高能" }, new string[0], new string[0], new string[0]);
        var vm = new SettingVM();
        var wordsBefore = SettingHelper.RawValue(SettingHelper.VideoDanmaku.SHIELD_WORD);

        foreach (var bad in new[] { null, "", "   ", "not json", "{\"a\":1}", "null", "[1,2]" })
        {
            var logsBefore = LogHelper.Entries.Count;
            var added = vm.ImportDanmuFilterJson(bad);
            Check(added == 0, "rejected input returns 0 for [" + (bad ?? "null") + "]");
            Check(LogHelper.Entries.Count == logsBefore + 1, "rejected input logs exactly once for [" + (bad ?? "null") + "]");
        }

        Check(vm.ShieldWords.SequenceEqual(new[] { "前方高能" }), "rejected input leaves collections intact");
        Check(SettingHelper.RawValue(SettingHelper.VideoDanmaku.SHIELD_WORD) == wordsBefore,
            "rejected input leaves storage intact");
    }

    // 固定种子：导出→导入→导出 字节一致
    private static void RoundTripIsStable()
    {
        var words = new[] { "前方高能", "2333", "awsl" };
        var regulars = new[] { "^测试.*$", "\\d{6}", "\\[.*\\]" };
        var users = new[] { "10001", "10002", "10003" };
        Seed(words, regulars, users, new string[0]);
        var source = new SettingVM();
        var first = source.ExportDanmuFilterJson();

        Seed(new string[0], new string[0], new string[0], new string[0]);
        var target = new SettingVM();
        Check(target.ImportDanmuFilterJson(first) == 9, "round trip imports all nine entries");

        var second = target.ExportDanmuFilterJson();
        Check(second == first, "export -> import -> export is byte-identical");
        Check(target.ShieldWords.SequenceEqual(words), "round trip keeps keyword order");
        Check(target.ShieldRegulars.SequenceEqual(regulars), "round trip keeps regex order");
        Check(target.ShieldUsers.SequenceEqual(users), "round trip keeps user order");
    }
}

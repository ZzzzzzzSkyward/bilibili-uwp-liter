using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

sealed class DanmuFilterItem { public int type; public string filter; }
sealed class DanmakuModel { public int weight = 1; public string text = "visible fixture"; public string sendID = "user-fixture"; }
sealed class SettingVMStub
{
    public List<string> ShieldWords = new();
    public List<string> ShieldUsers = new();
    public ObservableCollection<string> ShieldRegulars = new();
}
static class SettingHelper
{
    public static class VideoDanmaku { public const string SHIELD_REGULAR = "fixture-shield-regular"; }
    public static List<string> PersistedRegulars = new();
    public static T GetValue<T>(string key, T fallback)
    {
        if (key == VideoDanmaku.SHIELD_REGULAR)
            return (T)(object)new ObservableCollection<string>(PersistedRegulars);
        return fallback;
    }
    public static void SetValue(string key, IEnumerable<string> values)
    {
        if (key == VideoDanmaku.SHIELD_REGULAR) PersistedRegulars = values.ToList();
    }
}
sealed class CompareDanmakuModel : IEqualityComparer<DanmakuModel>
{
    public bool Equals(DanmakuModel a, DanmakuModel b) => ReferenceEquals(a, b);
    public int GetHashCode(DanmakuModel value) => value.GetHashCode();
}
static class Windows
{
    public static class UI
    {
        public static class Core { public enum CoreDispatcherPriority { Low } }
    }
}
sealed class DispatcherStub
{
    public Task RunAsync(Windows.UI.Core.CoreDispatcherPriority priority, Action action)
    {
        action();
        return Task.CompletedTask;
    }
}
sealed class DanmuControlStub
{
    public int AddCount;
    public void AddDanmu(DanmakuModel model, bool ignored) { AddCount++; }
}
sealed class Harness
{
    public SettingVMStub settingVM = new();
    public ObservableCollection<string> ShieldRegulars;
    public int p = 1;
    public int level = 0;
    public bool needDistinct = false;
    public int max = 0;
    public Dictionary<int, List<DanmakuModel>> danmakuPool = new()
    {
        [1] = new List<DanmakuModel> { new DanmakuModel() }
    };
    public DispatcherStub Dispatcher = new();
    public DanmuControlStub DanmuControl = new();
    public List<DanmuFilterItem> items;
    public Harness(string fixtureRule, string validRule)
    {
        SettingHelper.PersistedRegulars = new List<string> { fixtureRule, "local-valid" };
        LoadCachedRules();
        settingVM.ShieldRegulars = ShieldRegulars;
        items = new List<DanmuFilterItem>
        {
            new DanmuFilterItem { type = 1, filter = fixtureRule },
            new DanmuFilterItem { type = 1, filter = validRule },
            new DanmuFilterItem { type = 1, filter = string.Empty }
        };
    }
    private static bool IsValidDanmuRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;
            try
            {
                new Regex(pattern);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    public void LoadCachedRules()
    {
        ShieldRegulars = SettingHelper.GetValue<ObservableCollection<string>>(SettingHelper.VideoDanmaku.SHIELD_REGULAR, new ObservableCollection<string>() { });
            var validRegulars = ShieldRegulars.Where(IsValidDanmuRegex).ToList();
            if (validRegulars.Count != ShieldRegulars.Count)
            {
                ShieldRegulars = new ObservableCollection<string>(validRegulars);
                SettingHelper.SetValue(SettingHelper.VideoDanmaku.SHIELD_REGULAR, ShieldRegulars);
            }
    }
    public void Import()
    {
        var ShieldRegulars = settingVM.ShieldRegulars;
                        var users = items.Where(x => x.type == 1).Select(x => x.filter).ToList();
                        var ls = ShieldRegulars.Union(users).Where(IsValidDanmuRegex).ToList();
                        ShieldRegulars.Clear();
                        foreach (var item in ls)
                        {
                            ShieldRegulars.Add(item);
                        }
                        SettingHelper.SetValue(SettingHelper.VideoDanmaku.SHIELD_REGULAR, ShieldRegulars);
    }
    public async Task Render()
    {
            await Task.Run(async () =>
            {
                try
                {
                    if (danmakuPool != null && danmakuPool.ContainsKey(p))
                    {
                        var data = danmakuPool[p].Where(x => true);
                        //云屏蔽
                        data = data.Where(x => x.weight >= level);
                        //去重
                        if (needDistinct)
                        {
                            data = data.Distinct(new CompareDanmakuModel());
                        }
                        //关键词
                        foreach (var item in settingVM.ShieldWords)
                        {
                            data = data.Where(x => !x.text.Contains(item));
                        }
                        //用户
                        foreach (var item in settingVM.ShieldUsers)
                        {
                            data = data.Where(x => !x.sendID.Equals(item));
                        }
                        //正则
                        foreach (var item in settingVM.ShieldRegulars)
                        {
                            data = data.Where(x => !Regex.IsMatch(x.text, item));
                        }
                        if (max > 0)
                        {
                            data = data.Take(max);
                        }
                        //加载弹幕
                        foreach (var item in data)
                        {
                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                            {
                                DanmuControl.AddDanmu(item, false);
                            });
                        }
                        data = null;
                    }
                }
                catch (Exception)
                {
                }
            });
    }
}

// Exact malformed rule quoted in issue #611's discussion; the cloud/API boundary is stubbed.
var rule = @"/.\1{16}/";
var validRule = "cloud-valid";
var regexRejected = false;
try { Regex.IsMatch("visible fixture", rule); }
catch (ArgumentException) { regexRejected = true; }
Console.WriteLine($"Regex parser rejects imported fixture: {regexRejected}");
var harness = new Harness(rule, validRule);
var cachedRulesPrunedBeforeSync = !SettingHelper.PersistedRegulars.Contains(rule) && SettingHelper.PersistedRegulars.Contains("local-valid");
Console.WriteLine($"Cached invalid rule pruned before sync: {cachedRulesPrunedBeforeSync}");
harness.Import();
Console.WriteLine($"Invalid imported rule persisted: {SettingHelper.PersistedRegulars.Contains(rule)}");
Console.WriteLine($"Empty regex persisted: {SettingHelper.PersistedRegulars.Contains(string.Empty)}");
Console.WriteLine($"Valid local/cloud rules preserved: {SettingHelper.PersistedRegulars.Contains("local-valid") && SettingHelper.PersistedRegulars.Contains(validRule)}");
await harness.Render();
Console.WriteLine($"Danmaku added after render pass: {harness.DanmuControl.AddCount}");
var passed = regexRejected && cachedRulesPrunedBeforeSync && !SettingHelper.PersistedRegulars.Contains(rule) &&
    !SettingHelper.PersistedRegulars.Contains(string.Empty) &&
    SettingHelper.PersistedRegulars.Contains("local-valid") && SettingHelper.PersistedRegulars.Contains(validRule) &&
    harness.DanmuControl.AddCount > 0;
Console.WriteLine(passed
    ? "PASS: imported rule allowed a render pass"
    : "FAIL: imported invalid rule aborted this render pass before any danmaku was added");
Environment.ExitCode = passed ? 0 : 1;

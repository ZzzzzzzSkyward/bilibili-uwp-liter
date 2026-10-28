using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

sealed class DanmuFilterItem { public int type; public string filter; }
sealed class DanmakuModel { public int weight = 1; public string text = "visible fixture"; public string sendID = "user-fixture"; }
sealed class SettingVMStub
{
    public List<string> ShieldWords = new();
    public List<string> ShieldUsers = new();
    public List<string> ShieldRegulars = new();
}
static class SettingHelper
{
    public static class VideoDanmaku { public const string SHIELD_REGULAR = "fixture-shield-regular"; }
    public static List<string> PersistedRegulars = new();
    public static void SetValue(string key, List<string> values)
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
    public List<string> ShieldRegulars;
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
    public Harness(string fixtureRule)
    {
        ShieldRegulars = settingVM.ShieldRegulars;
        items = new List<DanmuFilterItem> { new DanmuFilterItem { type = 1, filter = fixtureRule } };
    }
    public void Import()
    {
        var ShieldRegulars = settingVM.ShieldRegulars;
                    {
                        var users = items.Where(x => x.type == 1).Select(x => x.filter).ToList();
                        var ls = ShieldRegulars.Union(users);
                        ShieldRegulars.Clear();
                        foreach (var item in ls)
                        {
                            ShieldRegulars.Add(item);
                        }
                        SettingHelper.SetValue(SettingHelper.VideoDanmaku.SHIELD_REGULAR, ShieldRegulars);
                    }
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
var regexRejected = false;
try { Regex.IsMatch("visible fixture", rule); }
catch (ArgumentException) { regexRejected = true; }
Console.WriteLine($"Regex parser rejects imported fixture: {regexRejected}");
var harness = new Harness(rule);
harness.Import();
Console.WriteLine($"Imported rule persisted: {SettingHelper.PersistedRegulars.Contains(rule)}");
await harness.Render();
Console.WriteLine($"Danmaku added after render pass: {harness.DanmuControl.AddCount}");
var passed = regexRejected && SettingHelper.PersistedRegulars.Contains(rule) && harness.DanmuControl.AddCount > 0;
Console.WriteLine(passed
    ? "PASS: imported rule allowed a render pass"
    : "FAIL: imported invalid rule aborted this render pass before any danmaku was added");
Environment.ExitCode = passed ? 0 : 1;

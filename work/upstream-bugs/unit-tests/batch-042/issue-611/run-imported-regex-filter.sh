#!/usr/bin/env bash
set -euo pipefail
script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../../../../../" && pwd)"
tools_root="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
settings_source="$repo_root/src/BiliLite.UWP/Modules/SettingVM.cs"
player_source="$repo_root/src/BiliLite.UWP/Controls/PlayerControl.xaml.cs"
import_fragment="$script_dir/import-regex-rules.csx"
load_fragment="$script_dir/load-cached-regex-rules.csx"
render_fragment="$script_dir/render-filter-loop.csx"
harness="$script_dir/imported-regex-filter-test.csx"
python3 - "$settings_source" "$import_fragment" <<'PY'
import sys
source = open(sys.argv[1], encoding='utf-8-sig').read()
start = source.index('                        var users = items.Where(x => x.type == 1)')
end = source.index('                        SettingHelper.SetValue(SettingHelper.VideoDanmaku.SHIELD_REGULAR, ShieldRegulars);', start)
end = source.index('\n', end)
open(sys.argv[2], 'w').write(source[start:end] + '\n')
PY
python3 - "$settings_source" "$load_fragment" <<'PY'
import sys
source = open(sys.argv[1], encoding='utf-8-sig').read()
start = source.index('            ShieldRegulars = SettingHelper.GetValue<ObservableCollection<string>>(')
end = source.index('\n\n            //用户', start)
open(sys.argv[2], 'w').write(source[start:end] + '\n')
PY
python3 - "$settings_source" "$script_dir/regex-validation-helper.csx" <<'PY'
import sys
source = open(sys.argv[1], encoding='utf-8-sig').read()
start = source.index('        private static bool IsValidDanmuRegex(')
brace = source.index('{', start)
depth = 0
for index in range(brace, len(source)):
    if source[index] == '{': depth += 1
    elif source[index] == '}':
        depth -= 1
        if depth == 0:
            end = index + 1
            break
open(sys.argv[2], 'w').write(source[start:end] + '\n')
PY
sed -n '965,1012p' "$player_source" > "$render_fragment"
grep -Fq 'ShieldRegulars.Union(users).Where(IsValidDanmuRegex).ToList()' "$import_fragment"
grep -Fq 'ShieldRegulars.Where(IsValidDanmuRegex).ToList()' "$load_fragment"
grep -Fq 'Regex.IsMatch(x.text, item)' "$render_fragment"
cat > "$harness" <<'CSHARP'
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
    __ACTUAL_REGEX_VALIDATION_HELPER__
    public void LoadCachedRules()
    {
        __ACTUAL_CACHED_REGEX_LOAD__
    }
    public void Import()
    {
        var ShieldRegulars = settingVM.ShieldRegulars;
CSHARP
cat "$import_fragment" >> "$harness"
cat >> "$harness" <<'CSHARP'
    }
    public async Task Render()
    {
CSHARP
cat "$render_fragment" >> "$harness"
cat >> "$harness" <<'CSHARP'
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
CSHARP
python3 - "$harness" "$script_dir/regex-validation-helper.csx" "$load_fragment" <<'PY'
import sys
harness = open(sys.argv[1]).read()
for marker, path in zip(('__ACTUAL_REGEX_VALIDATION_HELPER__', '__ACTUAL_CACHED_REGEX_LOAD__'), sys.argv[2:]):
    harness = harness.replace(marker, open(path).read().strip())
open(sys.argv[1], 'w').write(harness)
PY
export DOTNET_ROOT="$tools_root/dotnet"
export DOTNET_CLI_HOME="$tools_root/cache/dotnet-cli"
export NUGET_PACKAGES="$tools_root/cache/nuget"
export XDG_CACHE_HOME="$tools_root/cache/xdg"
export XDG_DATA_HOME="$tools_root/cache/xdg-data"
exec "$DOTNET_ROOT/dotnet" "$tools_root/tool/dotnet-script-1.6.0/tools/net8.0/any/dotnet-script.dll" --no-cache "$harness"

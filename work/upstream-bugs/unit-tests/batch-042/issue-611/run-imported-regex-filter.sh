#!/usr/bin/env bash
set -euo pipefail
script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../../../../../" && pwd)"
tools_root="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
settings_source="$repo_root/src/BiliLite.UWP/Modules/SettingVM.cs"
player_source="$repo_root/src/BiliLite.UWP/Controls/PlayerControl.xaml.cs"
import_fragment="$script_dir/import-regex-rules.csx"
render_fragment="$script_dir/render-filter-loop.csx"
harness="$script_dir/imported-regex-filter-test.csx"
sed -n '125,134p' "$settings_source" > "$import_fragment"
sed -n '965,1012p' "$player_source" > "$render_fragment"
grep -Fq 'ShieldRegulars.Union(users)' "$import_fragment"
grep -Fq 'Regex.IsMatch(x.text, item)' "$render_fragment"
cat > "$harness" <<'CSHARP'
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
CSHARP
export DOTNET_ROOT="$tools_root/dotnet"
export DOTNET_CLI_HOME="$tools_root/cache/dotnet-cli"
export NUGET_PACKAGES="$tools_root/cache/nuget"
export XDG_CACHE_HOME="$tools_root/cache/xdg"
export XDG_DATA_HOME="$tools_root/cache/xdg-data"
exec "$DOTNET_ROOT/dotnet" "$tools_root/tool/dotnet-script-1.6.0/tools/net8.0/any/dotnet-script.dll" --no-cache "$harness"

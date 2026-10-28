#!/usr/bin/env bash
set -euo pipefail
script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../../../../.." && pwd)"
tools_root="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
source_file="$repo_root/src/BiliLite.UWP/Controls/PlayerControl.xaml.cs"
fragment="$script_dir/player-media-ended.csx"
harness="$script_dir/natural-end-report-test.csx"
sed -n '2374,2451p' "$source_file" > "$fragment"
grep -Fq '//await playerHelper.ReportHistory(CurrentPlayItem, Player.Duration);' "$fragment"
cat > "$harness" <<'CSHARP'
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

enum Visibility { Visible }
sealed class PlayInfo { public bool is_interaction; }
sealed class InteractionInfo { public int is_leaf; }
sealed class InteractionVm { public InteractionInfo Info = new(); }
sealed class DanmuStub { public void PauseDanmaku() { } public void ClearAll() { } }
sealed class ChoiceStub { public Visibility Visibility { get; set; } }
sealed class SelectionStub { public int SelectedIndex; }
sealed class ToggleStub { public bool IsOn; }
sealed class PlayerStub { public double Duration = 100; public void Play() { } }
sealed class HistoryStub
{
    public int Calls;
    public Task ReportHistory(PlayInfo item, double progress) { Calls++; return Task.CompletedTask; }
}
static class Utils { public static void ShowMessageToast(string message) { } }
sealed class PlayerControlHarness
{
    public PlayInfo CurrentPlayItem = new();
    public InteractionVm interactionVideoVM = new();
    public DanmuStub DanmuControl = new();
    public ChoiceStub InteractionChoices = new();
    public SelectionStub PlayerSettingPlayMode = new() { SelectedIndex = 0 };
    public ToggleStub PlayerSettingAutoNext = new();
    public int CurrentPlayIndex;
    public List<PlayInfo> PlayInfos = new() { new PlayInfo() };
    public EventHandler AllMediaEndEvent;
    public bool _autoPlay;
    public PlayerStub Player = new();
    public HistoryStub playerHelper = new();
    public void ChangePlayIndex(int index) { }
    public void ClearSubTitle() { }
    public void InvokePlayerEnd() { Player_PlayMediaEnded(null, EventArgs.Empty); }
CSHARP
cat "$fragment" >> "$harness"
cat >> "$harness" <<'CSHARP'
}

var control = new PlayerControlHarness();
control.InvokePlayerEnd();
Console.WriteLine($"History report calls on natural end: {control.playerHelper.Calls}");
var passed = control.playerHelper.Calls > 0;
Console.WriteLine(passed
    ? "PASS: natural completion reported final progress"
    : "FAIL: natural completion did not report final progress");
Environment.ExitCode = passed ? 0 : 1;
CSHARP
export DOTNET_ROOT="$tools_root/dotnet"
export DOTNET_CLI_HOME="$tools_root/cache/dotnet-cli"
export NUGET_PACKAGES="$tools_root/cache/nuget"
export XDG_CACHE_HOME="$tools_root/cache/xdg"
export XDG_DATA_HOME="$tools_root/cache/xdg-data"
exec "$DOTNET_ROOT/dotnet" "$tools_root/tool/dotnet-script-1.6.0/tools/net8.0/any/dotnet-script.dll" --no-cache "$harness"

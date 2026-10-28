#!/usr/bin/env bash
set -euo pipefail
script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../../../../.." && pwd)"
tools_root="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
source_file="$repo_root/src/BiliLite.UWP/Controls/PlayerControl.xaml.cs"
fragment="$script_dir/dispose-method.csx"
harness="$script_dir/dispose-stop-order-test.csx"
sed -n '/^        public async void Dispose()/,/^        }/p' "$source_file" > "$fragment"
grep -Fq 'await playerHelper.ReportHistory(historyItem, historyPosition);' "$fragment"
cat > "$harness" <<'CSHARP'
using System;
using System.Threading.Tasks;

enum PlayState { Playing, End }
sealed class ChangePlayerEngine { }
sealed class PlayInfo
{
    public long season_id = 1;
    public string ep_id = "fixture-episode";
    public string cid = "fixture-cid";
}
static class SettingHelper
{
    public static string SavedKey;
    public static object SavedValue;
    public static void SetValue<T>(string key, T value) { SavedKey = key; SavedValue = value; }
}
sealed class StubPlayer
{
    public double Position = 42;
    public PlayState PlayState = PlayState.Playing;
    public bool IsDisposed;
    public TaskCompletionSource<bool> DisposedSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public event EventHandler<PlayState> PlayStateChanged;
    public event EventHandler PlayMediaEnded;
    public event EventHandler<string> PlayMediaError;
    public event EventHandler<ChangePlayerEngine> ChangeEngine;
    public void Dispose()
    {
        IsDisposed = true;
        Position = 0;
        PlayState = PlayState.End;
        DisposedSignal.TrySetResult(true);
    }
}
sealed class StubPlayerHelper
{
    public TaskCompletionSource<bool> PendingReport = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public PlayInfo ReportedItem;
    public double ReportedPosition;
    public Task ReportHistory(PlayInfo item, double position)
    {
        ReportedItem = item;
        ReportedPosition = position;
        return PendingReport.Task;
    }
}
sealed class StubTimer { public bool IsStopped; public void Stop() { IsStopped = true; } }
sealed class PlayerControlHarness
{
    public PlayInfo CurrentPlayItem = new();
    public StubPlayer Player = new();
    public StubPlayerHelper playerHelper = new();
    public StubTimer danmuTimer = new();
    public object danmakuPool = new();
    public object dispRequest = new();
    void Player_PlayStateChanged(object sender, PlayState e) { }
    void Player_PlayMediaEnded(object sender, EventArgs e) { }
    void Player_PlayMediaError(object sender, string e) { }
    void Player_ChangeEngine(object sender, ChangePlayerEngine e) { }
CSHARP
cat "$fragment" >> "$harness"
cat >> "$harness" <<'CSHARP'
}

var control = new PlayerControlHarness();
var originalItem = control.CurrentPlayItem;
control.Dispose();
var stoppedBeforeReportCompleted = control.Player.IsDisposed;
Console.WriteLine($"Player disposed while report pending: {stoppedBeforeReportCompleted}");
var historyCaptured = ReferenceEquals(control.playerHelper.ReportedItem, originalItem)
    && control.playerHelper.ReportedPosition == 42
    && SettingHelper.SavedKey == "epfixture-episode"
    && Equals(SettingHelper.SavedValue, 42d)
    && control.danmuTimer == null;
Console.WriteLine($"History and resume position captured before report: {historyCaptured}");
control.playerHelper.PendingReport.TrySetResult(true);
await control.Player.DisposedSignal.Task.WaitAsync(TimeSpan.FromSeconds(2));
Console.WriteLine($"Player disposed after report completed: {control.Player.IsDisposed}");
var passed = stoppedBeforeReportCompleted && historyCaptured;
Console.WriteLine(passed
    ? "PASS: player cleanup completed before captured history report"
    : "FAIL: player cleanup or captured history data was incorrect");
Environment.ExitCode = passed ? 0 : 1;
CSHARP
export DOTNET_ROOT="$tools_root/dotnet"
export DOTNET_CLI_HOME="$tools_root/cache/dotnet-cli"
export NUGET_PACKAGES="$tools_root/cache/nuget"
export XDG_CACHE_HOME="$tools_root/cache/xdg"
export XDG_DATA_HOME="$tools_root/cache/xdg-data"
exec "$DOTNET_ROOT/dotnet" "$tools_root/tool/dotnet-script-1.6.0/tools/net8.0/any/dotnet-script.dll" --no-cache "$harness"

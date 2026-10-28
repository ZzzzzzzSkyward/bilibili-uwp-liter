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
        public async void Dispose()
        {
            var historyItem = CurrentPlayItem;
            var historyPosition = Player.Position;
            var shouldReportHistory = historyItem != null && Player.PlayState != PlayState.End;
            if (historyItem != null)
                SettingHelper.SetValue<double>(historyItem.season_id != 0 ? "ep" + historyItem.ep_id : historyItem.cid, historyPosition);

            Player.PlayStateChanged -= Player_PlayStateChanged;
            Player.PlayMediaEnded -= Player_PlayMediaEnded;
            Player.PlayMediaError -= Player_PlayMediaError;
            Player.ChangeEngine -= Player_ChangeEngine;
            //Player.PlayBufferEnd -= Player_PlayBufferEnd;
            //Player.PlayBufferStart -= Player_PlayBufferStart;
            //Player.PlayBuffering -= Player_PlayBuffering;
            Player.Dispose();
            if (danmuTimer != null)
            {
                danmuTimer.Stop();
                danmuTimer = null;
            }
            danmakuPool = null;
            if (dispRequest != null)
            {
                dispRequest = null;
            }
            //当视频播放结束的话，Position为0
            if (shouldReportHistory)
                await playerHelper.ReportHistory(historyItem, historyPosition);
        }
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

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
    public static void SetValue<T>(string key, T value) { }
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
        DisposedSignal.TrySetResult(true);
    }
}
sealed class StubPlayerHelper
{
    public TaskCompletionSource<bool> PendingReport = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task ReportHistory(PlayInfo item, double position) => PendingReport.Task;
}
sealed class StubTimer { public void Stop() { } }
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
            if (CurrentPlayItem != null)
            {
                SettingHelper.SetValue<double>(CurrentPlayItem.season_id != 0 ? "ep" + CurrentPlayItem.ep_id : CurrentPlayItem.cid, Player.Position);
                //当视频播放结束的话，Position为0
                if (Player.PlayState != PlayState.End)
                    await playerHelper.ReportHistory(CurrentPlayItem, Player.Position);
            }

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
        }
}

var control = new PlayerControlHarness();
control.Dispose();
var stoppedBeforeReportCompleted = control.Player.IsDisposed;
Console.WriteLine($"Player disposed while report pending: {stoppedBeforeReportCompleted}");
control.playerHelper.PendingReport.TrySetResult(true);
await control.Player.DisposedSignal.Task.WaitAsync(TimeSpan.FromSeconds(2));
Console.WriteLine($"Player disposed after report completed: {control.Player.IsDisposed}");
var passed = stoppedBeforeReportCompleted;
Console.WriteLine(passed
    ? "PASS: player stopped before history report completed"
    : "FAIL: player stop is blocked by pending history report");
Environment.ExitCode = passed ? 0 : 1;

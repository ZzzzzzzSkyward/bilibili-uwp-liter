using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlayersessProbe
{
    internal sealed partial class HistoryCheckpointSubject
    {
        public PlayInfo CurrentPlayItem;
        public readonly HistoryPlayer Player = new HistoryPlayer();
        public readonly HistoryReporter playerHelper = new HistoryReporter();
        public HistoryCheckpointSubject() { _hasPlayedCurrentItem = true; }
        public bool Running => _historyCheckpointRunning;
        public bool Pending => _historyPendingItem != null;
        public Task Checkpoint(bool report = true) => CheckpointHistoryAsync(report);
        public Task Flush() => FlushPlaybackAsync();
    }
    internal enum PlayState { Playing, Pause, End }
    internal sealed class LocalPlayInfo { public bool IsManifestPlayback; }
    internal sealed class PlayInfo
    {
        public int season_id;
        public int ep_id;
        public string cid = "42";
        public string avid = "84";
        public LocalPlayInfo LocalPlayInfo;
    }
    internal sealed class HistoryPlayer
    {
        public double Position = 10;
        public PlayState PlayState = PlayState.Playing;
    }
    internal static class SettingHelper
    {
        public static readonly Dictionary<string, double> Saved = new Dictionary<string, double>();
        public static void SetValue<T>(string key, T value) => Saved[key] = Convert.ToDouble(value);
    }
    internal sealed class HistoryReporter
    {
        public readonly List<double> Positions = new List<double>();
        public readonly List<PlayInfo> Items = new List<PlayInfo>();
        public Func<Task> Send = () => Task.CompletedTask;
        public int Active;
        public int MaxActive;
        public bool AllSilent = true;
        public async Task ReportHistory(PlayInfo item, double position, bool reportErrors)
        {
            Positions.Add(position);
            Items.Add(item);
            AllSilent &= !reportErrors;
            Active++;
            MaxActive = Math.Max(MaxActive, Active);
            try { await Send(); }
            finally { Active--; }
        }
    }
    internal static class HistoryCheckpointProbe
    {
        private static TaskCompletionSource<bool> Gate() => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private static HistoryCheckpointSubject Subject() => new HistoryCheckpointSubject { CurrentPlayItem = new PlayInfo() };
        public static async Task Run(Action<bool, string> check)
        {
            SettingHelper.Saved.Clear();
            var subject = Subject();
            var gate = Gate();
            subject.playerHelper.Send = () => gate.Task;
            var first = subject.Checkpoint();
            check(subject.playerHelper.Active == 1 && subject.Running, "actual history report enters one blocked drain");
            subject.Player.Position = 20;
            var middle = subject.Checkpoint();
            subject.Player.Position = 30;
            var latest = subject.Checkpoint();
            check(SettingHelper.Saved["42"] == 30, "actual history saves latest local position while POST is blocked");
            check(subject.playerHelper.Positions.SequenceEqual(new[] { 10d }) && subject.playerHelper.MaxActive == 1,
                "actual history serializes concurrent checkpoints before releasing network gate");
            check(!first.IsCompleted && !middle.IsCompleted && !latest.IsCompleted, "actual checkpoint waiters await the active drain and its tail");
            gate.SetResult(true);
            await Task.WhenAll(first, middle, latest).WaitAsync(TimeSpan.FromSeconds(3));
            check(subject.playerHelper.Positions.SequenceEqual(new[] { 10d, 30d }) && subject.playerHelper.MaxActive == 1,
                "actual history coalesces intermediate positions and reports only latest pending tail");
            check(!subject.Running && !subject.Pending && subject.playerHelper.AllSilent, "actual drain clears ownership and uses silent reports");

            subject = Subject(); gate = Gate();
            subject.playerHelper.Send = () => gate.Task;
            first = subject.Checkpoint();
            latest = subject.Checkpoint();
            gate.SetResult(true);
            await Task.WhenAll(first, latest).WaitAsync(TimeSpan.FromSeconds(3));
            check(subject.playerHelper.Positions.SequenceEqual(new[] { 10d }), "actual history suppresses identical pending tail");

            subject = Subject(); subject.Player.PlayState = PlayState.Pause;
            await subject.Checkpoint();
            check(subject.playerHelper.Positions.Count == 0 && SettingHelper.Saved["42"] == 10, "actual paused timer saves locally without reporting");
            await subject.Flush();
            check(subject.playerHelper.Positions.SequenceEqual(new[] { 10d }), "actual paused close flush reports final position");
            subject.Player.PlayState = PlayState.End;
            await subject.Flush();
            check(subject.playerHelper.Positions.Count == 1, "actual ended close flush does not report again");

            subject = Subject();
            subject.playerHelper.Send = () => Task.FromException(new InvalidOperationException("offline fixture"));
            await subject.Checkpoint().WaitAsync(TimeSpan.FromSeconds(3));
            check(!subject.Running && !subject.Pending && subject.playerHelper.Active == 0, "actual throwing report releases guard and pending state");
            subject.playerHelper.Send = () => Task.CompletedTask;
            subject.Player.Position = 25;
            await subject.Checkpoint();
            check(subject.playerHelper.Positions.SequenceEqual(new[] { 10d, 25d }), "actual history retries normally after report exception");

            subject = Subject(); subject.CurrentPlayItem.avid = ""; subject.CurrentPlayItem.season_id = 1; subject.CurrentPlayItem.ep_id = 7;
            await subject.Checkpoint();
            check(SettingHelper.Saved["ep7"] == 10 && subject.playerHelper.Positions.Count == 0, "actual history uses shared episode key and keeps local-only files offline");
            subject.CurrentPlayItem.LocalPlayInfo = new LocalPlayInfo { IsManifestPlayback = true };
            subject.Player.Position = 40;
            await subject.Flush();
            check(SettingHelper.Saved["ep7"] == 10, "actual manifest playback bypasses remote history and checkpoint persistence");
        }
    }
}

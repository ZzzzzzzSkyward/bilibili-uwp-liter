using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using History = BiliLite.Helpers.VideoPlayHistoryHelper;
using Entry = BiliLite.Helpers.VideoPlayHistoryHelper.ABPlayHistoryEntry;
using Settings = BiliLite.Helpers.SettingHelper;
using Item = BiliLite.Controls.PlayInfo;

namespace PlayersessProbe
{
    internal static class HistoryStorageProbe
    {
        [ThreadStatic] private static bool insideSetCall;
        private static string Key => Settings.Player.PLAYER_ABPLAY_HISTORIES;
        private static Microsoft.Toolkit.Uwp.Helpers.ApplicationDataStorageHelper Store => Settings.storageHelper;
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
        private static TaskCompletionSource<bool> Signal() => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private static Dictionary<string, Entry> Read(string json) => JsonConvert.DeserializeObject<Dictionary<string, Entry>>(json);
        private static Item Cid(string cid) => new Item { cid = cid };
        private static async Task Finish(params Task[] tasks) => await Task.WhenAll(tasks).WaitAsync(Timeout);

        public static async Task Run(Action<bool, string> check)
        {
            // This is the first helper access in the process; a Set must merge saved history.
            Store.Values[Key] = "{\"old\":{\"PointA\":1,\"PointB\":2},\"stored-null\":null}";
            var entered = Signal();
            using (var release = new ManualResetEventSlim(false))
            {
                var attempts = new ConcurrentQueue<string>();
                var active = 0;
                var maxActive = 0;
                var calls = 0;
                Store.BeforeSave = (key, json) =>
                {
                    attempts.Enqueue(json);
                    var current = Interlocked.Increment(ref active);
                    int previous;
                    do { previous = maxActive; if (previous >= current) break; }
                    while (Interlocked.CompareExchange(ref maxActive, current, previous) != previous);
                    if (Interlocked.Increment(ref calls) == 1)
                    {
                        entered.TrySetResult(true);
                        if (!release.Wait(Timeout)) throw new TimeoutException("history Save release");
                    }
                };
                Store.AfterSave = (key, json) => Interlocked.Decrement(ref active);
                var first = Set(Cid("new"), new Entry { PointA = 3, PointB = 4 });
                Task second = Task.CompletedTask;
                try
                {
                    await entered.Task.WaitAsync(Timeout);
                    check(History.FindABPlayHistory(Cid("old"))?.PointB == 2, "history first Set initializes persisted entries");
                    History.LoadABPlayHistories(false);
                    check(History.FindABPlayHistory(Cid("new"))?.PointB == 4, "late startup Load retains accepted memory");
                    second = Set(Cid("new"), new Entry { PointA = 3, PointB = 8 });
                    check(!second.IsCompleted && Volatile.Read(ref calls) == 1, "new history Save waits behind admitted writer");
                }
                finally { release.Set(); await Finish(first, second); Store.BeforeSave = null; Store.AfterSave = null; }
                var payloads = attempts.ToArray();
                check(maxActive == 1, "history has one active primitive Save");
                check(payloads.Length == 2 && Read(payloads[0])["new"].PointB == 4 && Read(payloads[1])["new"].PointB == 8, "history writes accepted payloads in order");
                var saved = Read(Store.Values[Key]);
                check(saved.ContainsKey("old") && saved["new"].PointB == 8, "first-use and late Load retain old plus latest history on disk");
            }

            var mutable = new Entry { PointA = 10, PointB = 20 };
            using (var trace = new SerializationGate("BiliLite.Helpers.VideoPlayHistoryHelper+ABPlayHistoryEntry"))
            {
                var payloads = new ConcurrentQueue<string>();
                var errors = BiliLite.Helpers.LogHelper.Errors.Count;
                Store.AfterSave = (key, json) => payloads.Enqueue(json);
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings { TraceWriter = trace };
                var first = Set(Cid("snapshot"), mutable);
                Task add = Task.CompletedTask, remove = Task.CompletedTask;
                try
                {
                    await trace.Entered.Task.WaitAsync(Timeout);
                    check(ReferenceEquals(mutable, History.FindABPlayHistory(Cid("snapshot"))), "Find preserves caller-owned live entry identity");
                    mutable.PointA = 99;
                    History.FindABPlayHistory(Cid("snapshot")).PointB = 100;
                    add = Set(Cid("snapshot-added"), new Entry { PointA = 5 });
                    remove = Set(Cid("new"), null);
                }
                finally { trace.Release.Set(); await Finish(first, add, remove); JsonConvert.DefaultSettings = null; Store.AfterSave = null; }
                var captured = payloads.Select(Read).FirstOrDefault(value => value.ContainsKey("snapshot") && !value.ContainsKey("snapshot-added"));
                check(captured != null && captured["snapshot"].PointA == 10 && captured["snapshot"].PointB == 20 && captured.ContainsKey("new"), "accepted history snapshot detaches dictionary and both entry values");
                check(captured != null && captured.ContainsKey("stored-null") && captured["stored-null"] == null, "snapshot preserves stored null entries");
                check(BiliLite.Helpers.LogHelper.Errors.Count == errors, "concurrent key changes do not invalidate snapshot enumeration");
                check(ReferenceEquals(mutable, History.FindABPlayHistory(Cid("snapshot"))) && mutable.PointB == 100, "persistence copy preserves live in-process mutations");
            }

            var episode = new Item { season_id = 1, ep_id = 73, cid = "episode-cid" };
            await Set(episode, new Entry { PointA = 6 });
            check(Read(Store.Values[Key])["ep73"].PointB == double.MaxValue && !Read(Store.Values[Key]).ContainsKey("episode-cid"), "episode keys and default B remain compatible");
            await Set(episode, null);
            check(History.FindABPlayHistory(episode) == null && !Read(Store.Values[Key]).ContainsKey("ep73"), "null checkpoint removes the ordinary episode key");

            var prior = Store.Values[Key];
            var priorErrors = BiliLite.Helpers.LogHelper.Errors.Count;
            Store.BeforeSave = (key, json) => throw new System.IO.IOException("injected primitive Save failure");
            try { await Set(Cid("failure"), new Entry { PointB = 11 }); }
            finally { Store.BeforeSave = null; }
            check(Store.Values[Key] == prior && BiliLite.Helpers.LogHelper.Errors.Count == priorErrors + 1, "failed history Save is handled and preserves durable JSON");
            check(History.FindABPlayHistory(Cid("failure"))?.PointB == 11, "failed history Save retains accepted memory");
            await Set(Cid("failure"), new Entry { PointB = 12 });
            check(Read(Store.Values[Key])["failure"].PointB == 12, "failed writer does not poison subsequent history Save");

            // Populate via real setters; cleanup must join the same off-thread FIFO.
            for (var index = 0; index < 205; index++) await Set(Cid("cleanup-" + index), new Entry { PointA = index });
            entered = Signal();
            using (var release = new ManualResetEventSlim(false))
            {
                var calls = 0;
                var onCaller = false;
                Store.BeforeSave = (key, json) =>
                {
                    onCaller |= insideSetCall;
                    if (Interlocked.Increment(ref calls) == 1)
                    { entered.TrySetResult(true); if (!release.Wait(Timeout)) throw new TimeoutException("cleanup Save release"); }
                };
                var pending = Set(Cid("cleanup-latest"), new Entry { PointB = 18 });
                Task cleanup = Task.CompletedTask, sentinel = Task.CompletedTask;
                try
                {
                    await entered.Task.WaitAsync(Timeout);
                    cleanup = Task.Run(() => History.LoadABPlayHistories(true));
                    await cleanup.WaitAsync(Timeout);
                    check(Volatile.Read(ref calls) == 1, "cleanup write queues behind the earlier checkpoint");
                    sentinel = Set(Cid("cleanup-latest"), new Entry { PointB = 19 });
                }
                finally { release.Set(); await Finish(pending, cleanup, sentinel); Store.BeforeSave = null; }
                var saved = Read(Store.Values[Key]);
                check(saved.Count == 200 && saved["cleanup-latest"].PointB == 19, "max200 cleanup and newer checkpoint persist in order");
                check(!onCaller, "settings serialization and Save run off the synchronous Set caller");
            }

            // Actual SettingHelper owns all three fallback forms; no replacement serializer.
            var fallback = new Dictionary<string, Entry>();
            Store.Values["history-fallback"] = "{broken";
            check(ReferenceEquals(Settings.GetValue("history-fallback", fallback), fallback), "malformed setting retains actual SettingHelper fallback");
            Store.Values["history-fallback"] = "";
            check(Settings.GetValue("history-fallback", fallback) == null, "empty setting retains actual SettingHelper null result");
            Store.Values["history-fallback"] = "null";
            check(Settings.GetValue("history-fallback", fallback) == null, "JSON null retains actual SettingHelper null result");
            Store.Values.TryRemove("history-fallback", out _);
            check(ReferenceEquals(Settings.GetValue("history-fallback", fallback), fallback), "missing setting retains actual SettingHelper fallback");
            Console.WriteLine("History storage checks completed using whole helper + SettingHelper.");
        }

        // Observe async-void compatibility through its real completion notification. No helper logic is replaced.
        private static Task Set(Item item, Entry entry)
        {
            var context = new VoidCompletionContext();
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(context);
            try { insideSetCall = true; History.SetABPlayHistory(item, entry); }
            finally { insideSetCall = false; SynchronizationContext.SetSynchronizationContext(previous); }
            return context.Completed.Task;
        }
        private sealed class VoidCompletionContext : SynchronizationContext
        {
            public readonly TaskCompletionSource<bool> Completed = Signal();
            public override void OperationCompleted() => Completed.TrySetResult(true);
            public override void Post(SendOrPostCallback callback, object state) => ThreadPool.QueueUserWorkItem(_ => callback(state));
        }
        private sealed class SerializationGate : ITraceWriter, IDisposable
        {
            private readonly string match;
            private int entered;
            public readonly TaskCompletionSource<bool> Entered = Signal();
            public readonly ManualResetEventSlim Release = new ManualResetEventSlim(false);
            public TraceLevel LevelFilter => TraceLevel.Verbose;
            public SerializationGate(string match) { this.match = match; }
            public void Trace(TraceLevel level, string message, Exception exception)
            {
                if (!message.StartsWith("Started serializing " + match, StringComparison.Ordinal) || Interlocked.Exchange(ref entered, 1) != 0) return;
                Entered.TrySetResult(true);
                if (!Release.Wait(Timeout)) throw new TimeoutException("serializer trace release");
            }
            public void Dispose() { Release.Set(); Release.Dispose(); }
        }
    }
}

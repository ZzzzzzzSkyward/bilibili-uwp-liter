using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ResourceProbe
{
    // UI/network/media are explicit doubles. Timer.Running is a modeled dispatcher root.
    // All control operations below execute complete extracted product methods.
    static class PlayerLifetimeProbe
    {
        static int failures;
        static readonly List<string> caseAssertions = new List<string>();
        static void Require(bool condition, string message)
        {
            // Keep all50 cycles running on the original source; failures still fail the case.
            if (!condition) caseAssertions.Add(message);
        }
        static PlayerInfo Info(bool subtitle = true) => subtitle ? new PlayerInfo { subtitle = new SubtitleInfo { subtitles = new List<SubtitleTrack> { new SubtitleTrack() } } } : new PlayerInfo();
        static void Ready(ControlSubject c)
        {
            c.playerHelper.Quality.TrySetResult(new BiliPlayUrlQualitesInfo());
            c.playerHelper.Info.TrySetResult(Info());
            c.playerHelper.Subtitle.TrySetResult(new SubtitleModel());
        }
        static void Open(ControlSubject c, PlayInfo item = null) => c.InitializePlayInfo(new List<PlayInfo> { item ?? new PlayInfo() }, 0, PlayTargetStart.FromStart);
        static void Clean(ControlSubject c)
        {
            c.Dispose();
            c.Unload();
        }
        static void Case(string name, Action action)
        {
            try
            {
                caseAssertions.Clear();
                action();
                if (caseAssertions.Count != 0)
                    throw new Exception(caseAssertions.Count + " failed assertions: " + string.Join("; ", caseAssertions.Distinct()));
                Console.WriteLine("PLAYER_LIFETIME " + name + " PASS");
            }
            catch (Exception error) { failures++; Console.WriteLine("PLAYER_LIFETIME " + name + " FAIL " + error); }
            finally { DispatcherTimer.StopAll(); InteractionVideoVM.NextGate = null; FileIO.Gate = null; Windows.Storage.Pickers.FileOpenPicker.Gate = null; }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        static WeakReference One(string mode)
        {
            var c = new ControlSubject();
            c.playerHelper.Quality.SetResult(new BiliPlayUrlQualitesInfo());
            if (mode == "subtitle-pending-close")
                c.playerHelper.Info.SetResult(Info());
            Open(c);
            if (mode.StartsWith("late-") || mode == "subtitle-pending-close")
                c.Dispose();
            c.playerHelper.Subtitle.SetResult(new SubtitleModel());
            c.playerHelper.Info.TrySetResult(Info(mode != "late-empty-close"));
            if (mode == "ordinary-close")
            {
                c.Dispose();
                c.Dispose();
            }
            return new WeakReference(c);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        static List<WeakReference> Many(string mode)
        {
            var refs = new List<WeakReference>();
            for (int i = 0; i < 50; i++)
                refs.Add(One(mode));
            return refs;
        }
        static int Collect(List<WeakReference> refs)
        {
            for (int i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            return refs.Count(r => r.IsAlive);
        }
        static void Roots(string mode, int expected)
        {
            var refs = Many(mode);
            int timers = DispatcherTimer.Running.Count, survivors = Collect(refs);
            Console.WriteLine($"ROOT_MODEL mode={mode} cycles=50 timers={timers} survivors={survivors}");
            DispatcherTimer.StopAll();
            int released = Collect(refs);
            Require(timers == expected && survivors == expected && released == 0, "timer/weak-root contract");
        }
        static void PublicBoundaries()
        {
            for (int i = 0; i < 50; i++)
            {
                var shared = new PlayInfo();
                var first = new ControlSubject();
                first.Load();
                Ready(first);
                Open(first, shared);
                first.SignalOpened();
                first.ClickFirstSubtitle();
                Require(first.MediaOpenCalls == 1 && first.PlayCalls == 1 && first.RunningSubtitle == 1, "public full live chain");
                first.Unload();
                first.Load();
                Require(first.DanmakuTimerLive == 1, "live unload/reload");
                Clean(first);
                var before = first.Calls;
                var published = first.Publishes;
                Exception initializeError = null, loadedError = null;
                try { Open(first, shared); } catch (Exception error) { initializeError = error; }
                try { first.Load(); } catch (Exception error) { loadedError = error; }
                first.Unload();
                first.SignalOpened();
                first.StartSubtitle();
                first.ClickFirstSubtitle();
                first.ChangePlayIndex(0, PlayTargetStart.FromStart);
                Require(first.Publishes == published, "closed manual/public entry published");
                Require(initializeError == null && loadedError == null, "closed Initialize/Loaded threw");
                Require(first.Calls == before && first.DanmakuTimerLive == 0, "closed public entry created work");
                var fresh = new ControlSubject();
                fresh.Load();
                Ready(fresh);
                Open(fresh, shared);
                fresh.SignalOpened();
                Require(fresh.MediaOpenCalls == 1 && fresh.PlayCalls == 1 && fresh.RunningSubtitle == 1, "fresh owner same identity");
                Clean(fresh);
            }
        }
        static void QualityClose(bool success)
        {
            int opens = 0, sessions = 0, dialogs = 0;
            for (int i = 0; i < 50; i++)
            {
                var c = new ControlSubject();
                var task = c.StartQuality();
                c.Dispose();
                c.playerHelper.Quality.SetResult(new BiliPlayUrlQualitesInfo { Success = success });
                task.GetAwaiter().GetResult();
                opens += c.MediaOpenCalls;
                sessions += c.CurrentSessionId == 0 ? 0 : 1;
                dialogs += c.Dialogs;
            }
            Console.WriteLine($"LATE_QUALITY success={success} cycles=50 opens={opens} sessions={sessions} dialogs={dialogs}");
            Require(opens == 0 && sessions == 0 && dialogs == 0, "late quality opened/published");
        }
        static void Replacement(bool sameReference, bool oldSuccess, bool oldInfo)
        {
            for (int i = 0; i < 50; i++)
            {
                var a = new PlayInfo();
                var b = sameReference ? a : new PlayInfo { cid = "B", title = "B" };
                var c = new ControlSubject();
                if (oldInfo)
                    c.playerHelper.Quality.SetResult(new BiliPlayUrlQualitesInfo());
                var oldQuality = c.playerHelper.Quality;
                var oldResponse = c.playerHelper.Info;
                Open(c, a);
                c.playerHelper.Quality = new TaskCompletionSource<BiliPlayUrlQualitesInfo>();
                c.playerHelper.Info = new TaskCompletionSource<PlayerInfo>();
                Ready(c);
                Open(c, b);
                var before = c.Calls;
                var oldSession = c.CurrentSessionId;
                if (oldInfo)
                    oldResponse.SetResult(Info(oldSuccess));
                else
                    oldQuality.SetResult(new BiliPlayUrlQualitesInfo { Success = oldSuccess });
                Require(c.Calls == before && c.CurrentSessionId == oldSession && c.RunningSubtitle == 1 && c.CurrentPlayItem == b, "replacement disturbed current chain");
                Clean(c);
            }
        }
        static void InfoAwait(string stage)
        {
            for (int i = 0; i < 50; i++)
            {
                var c = new ControlSubject();
                c.playerHelper.Subtitle.SetResult(new SubtitleModel());
                if (stage == "nodes")
                {
                    InteractionVideoVM.NextGate = new TaskCompletionSource<bool>();
                    c.playerHelper.Info.SetResult(new PlayerInfo { interaction = new InteractionInfo() });
                }
                else
                {
                    c.playerHelper.Info.SetResult(Info(false));
                    c.playerHelper.Online = new TaskCompletionSource<string>();
                }
                var task = c.StartInfo();
                Clean(c);
                var before = c.Publishes;
                if (stage == "nodes")
                    InteractionVideoVM.NextGate.SetResult(true);
                else
                    c.playerHelper.Online.SetResult("late");
                task.GetAwaiter().GetResult();
                Require(c.Publishes == before && c.playerHelper.OnlineCalls == (stage == "nodes" ? 0 : 1), "info continuation published after close");
            }
        }
        static void NodeClose()
        {
            for (int i = 0; i < 50; i++)
            {
                var c = new ControlSubject();
                Ready(c);
                c.SetInteraction();
                InteractionVideoVM.NextGate = new TaskCompletionSource<bool>();
                var task = c.StartNode();
                Clean(c);
                var before = c.Calls;
                InteractionVideoVM.NextGate.SetResult(true);
                task.GetAwaiter().GetResult();
                Require(c.Calls == before, "node minted new work after close");
            }
        }
        static void Picker(bool subtitle, bool replace, bool cancel)
        {
            for (int i = 0; i < 50; i++)
            {
                var c = new ControlSubject();
                var shared = c.CurrentPlayItem;
                Windows.Storage.Pickers.FileOpenPicker.Gate = new TaskCompletionSource<StorageFile>();
                c.StartPicker(subtitle);
                if (replace)
                {
                    Ready(c);
                    Open(c, shared);
                }
                else
                    c.Dispose();
                var before = c.Publishes;
                Windows.Storage.Pickers.FileOpenPicker.Gate.SetResult(cancel ? null : new StorageFile());
                Require(c.Publishes == before && c.RunningSubtitle == (replace ? 1 : 0), "picker completion imported stale file");
                Clean(c);
            }
        }
        static void TrackClose(bool replace)
        {
            for (int i = 0; i < 50; i++)
            {
                var c = new ControlSubject();
                c.CurrentPlayItem.LocalPlayInfo = new LocalInfo { IsManifestPlayback = true };
                c.SetOpenGate();
                var task = c.StartTrack();
                if (replace)
                {
                    c.ReleaseNewOpenGate();
                    Ready(c);
                    Open(c, new PlayInfo());
                }
                else
                    c.Dispose();
                var before = c.MediaOpenCalls;
                var published = c.Publishes;
                c.CompleteOpen(false);
                task.GetAwaiter().GetResult();
                Require(c.MediaOpenCalls == before && c.Publishes == published && c.Dialogs == 0, "track rollback reopened or changed replacement UI");
                Clean(c);
            }
        }
        static void TrackSharedModel(bool fault)
        {
            for (int i = 0; i < 50; i++)
            {
                var c = new ControlSubject();
                var shared = c.CurrentPlayItem;
                shared.LocalPlayInfo = new LocalInfo { IsManifestPlayback = true };
                var original = new LocalMediaPlaybackTrack();
                shared.LocalPlayInfo.SelectedAudioTrack = original;
                c.SetOpenGate();
                var task = c.StartTrack();
                c.ReleaseNewOpenGate();
                Ready(c);
                Open(c, shared);
                var current = new LocalMediaPlaybackTrack();
                shared.LocalPlayInfo.SelectedAudioTrack = current;
                var before = c.MediaOpenCalls;
                var published = c.Publishes;
                if (fault)
                    c.FaultOpen();
                else
                    c.CompleteOpen(false);
                task.GetAwaiter().GetResult();
                Require(ReferenceEquals(shared.LocalPlayInfo.SelectedAudioTrack, current) && c.MediaOpenCalls == before && c.Publishes == published, "obsolete coordinator rolled back shared replacement model");
                Clean(c);
            }
        }
        static void SubtitleReplacement()
        {
            for (int i = 0; i < 50; i++)
            {
                var c = new ControlSubject();
                var shared = new PlayInfo();
                c.playerHelper.Quality.SetResult(new BiliPlayUrlQualitesInfo());
                c.playerHelper.Info.SetResult(Info());
                var old = c.playerHelper.Subtitle;
                Open(c, shared);
                c.playerHelper.Subtitle = new TaskCompletionSource<SubtitleModel>();
                c.playerHelper.Subtitle.SetResult(new SubtitleModel());
                Open(c, shared);
                var before = c.Publishes;
                old.SetResult(new SubtitleModel());
                Require(c.RunningSubtitle == 1 && c.Publishes == before && DispatcherTimer.Running.Count == 1, "old subtitle replaced live same-reference selection");
                Clean(c);
            }
        }
        static void DanmakuContinuation(bool replace)
        {
            for (int i = 0; i < 50; i++)
            {
                var c = new ControlSubject();
                var shared = c.CurrentPlayItem;
                c.playerHelper.Danmaku = new TaskCompletionSource<List<DanmakuModel>>();
                var old = c.playerHelper.Danmaku;
                var task = c.UpdateDanmaku();
                if (replace)
                {
                    c.playerHelper.Danmaku = null;
                    Ready(c);
                    Open(c, shared);
                }
                else
                    c.Dispose();
                var before = c.Calls;
                var published = c.Publishes;
                var toasts = ControlSubject.Toasts;
                old.SetResult(new List<DanmakuModel>());
                task.GetAwaiter().GetResult();
                Require(c.Calls == before && c.Publishes == published && ControlSubject.Toasts == toasts, "old segmented update continued after boundary");
                Clean(c);
            }
        }
        static void PendingInteractionReplacement()
        {
            for (int i = 0; i < 50; i++)
            {
                var c = new ControlSubject(); var shared = new PlayInfo();
                InteractionVideoVM.NextGate = new TaskCompletionSource<bool>();
                c.playerHelper.Quality.SetResult(new BiliPlayUrlQualitesInfo()); c.playerHelper.Subtitle.SetResult(new SubtitleModel()); c.playerHelper.Info.SetResult(new PlayerInfo { interaction = new InteractionInfo() });
                Open(c, shared); var old = c.InteractionOwner;
                Require(c.InteractionAttached(old), "live pending interaction not bound");
                c.playerHelper.Info = new TaskCompletionSource<PlayerInfo>(); c.playerHelper.Info.SetResult(Info(false));
                Open(c, shared); var published = c.Publishes;
                InteractionVideoVM.NextGate.SetResult(true);
                Require(!c.InteractionAttached(old) && c.Publishes == published, "old pending VM remained attached on same-reference refresh"); Clean(c);
            }
        }
        static void InfoFaults()
        {
            for (int i = 0; i < 50; i++)
                foreach (bool cancel in new[] { false, true })
                {
                    var c = new ControlSubject();
                    var task = c.StartInfo();
                    c.Dispose();
                    var before = c.Publishes;
                    if (cancel)
                        c.playerHelper.Info.SetCanceled();
                    else
                        c.playerHelper.Info.SetException(new InvalidOperationException("info-fault"));
                    try
                    {
                        task.GetAwaiter().GetResult();
                        throw new Exception("info exception swallowed");
                    }
                    catch (OperationCanceledException) { Require(cancel, "unexpected cancel"); }
                    catch (InvalidOperationException) { Require(!cancel, "unexpected fault"); }
                    Require(c.Publishes == before && c.playerHelper.SubtitleCalls == 0 && c.Dialogs == 0, "late info fault published");
                }
        }
        static void EngineBoundaries()
        {
            for (int i = 0; i < 50; i++)
            {
                var c = new ControlSubject();
                c.StartEngine().GetAwaiter().GetResult();
                Require(c.MediaOpenCalls == 1 && c.Dialogs == 0, "live engine fallback");
                c.Dispose();
                var before = c.Calls;
                c.SignalEngine(true);
                c.SignalEngine(false);
                c.StartEngine().GetAwaiter().GetResult();
                Require(c.Calls == before && c.Dialogs == 0, "closed engine callback");
                c = new ControlSubject();
                c.SetOpenGate();
                var task = c.StartEngine();
                c.Dispose();
                c.CompleteOpen(false);
                task.GetAwaiter().GetResult();
                Require(c.Dialogs == 0, "late engine error");
            }
        }
        static void LiveAndFaults()
        {
            for (int i = 0; i < 50; i++)
            {
                var c = new ControlSubject();
                c.playerHelper.Quality.SetResult(new BiliPlayUrlQualitesInfo { CurrentQuality = new BiliPlayUrlInfo { PlayUrlType = BiliPlayUrlType.DASH } });
                c.SetEngineResults(false);
                c.StartQuality().GetAwaiter().GetResult();
                Require(c.MediaOpenCalls == 2 && c.Dialogs == 1, "live fallback changed");
                Clean(c);
                c = new ControlSubject();
                c.SetOpenGate();
                c.playerHelper.Quality.SetResult(new BiliPlayUrlQualitesInfo());
                var pending = c.StartQuality();
                c.Dispose();
                c.CompleteOpen(false);
                pending.GetAwaiter().GetResult();
                Require(c.Dialogs == 0, "close during open showed failure");
                foreach (bool cancel in new[] { false, true })
                {
                    c = new ControlSubject();
                    pending = c.StartQuality();
                    c.Dispose();
                    if (cancel)
                        c.playerHelper.Quality.SetCanceled();
                    else
                        c.playerHelper.Quality.SetException(new InvalidOperationException("network"));
                    try
                    {
                        pending.GetAwaiter().GetResult();
                        throw new Exception("network fault swallowed");
                    }
                    catch (OperationCanceledException) { Require(cancel, "unexpected cancellation"); }
                    catch (InvalidOperationException) { Require(!cancel, "unexpected fault"); }
                    Require(c.MediaOpenCalls == 0 && c.Dialogs == 0, "fault path created work");
                }
            }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        static WeakReference Settings()
        {
            var c = new ControlSubject();
            for (int i = 0; i < 50; i++)
            {
                SettingHelper.Values[SettingHelper.Player.PLAYER_VOLUME] = i / 100.0;
                SettingHelper.Values[SettingHelper.Player.SUBTITLE_SIZE] = 30.0 + i;
                SettingHelper.Values[SettingHelper.VideoDanmaku.FONT_ZOOM] = 1.0 + i;
                c.Load();
                Require(c.SettingsRefreshed(i), "one of three initializers skipped refreshed settings");
                c.Unload();
            }
            SettingHelper.Writes.Clear();
            var updates = c.Updates;
            c.ToggleBold();
            c.ChangeVolume();
            c.ChangeDanmaku();
            Console.WriteLine($"SETTINGS loads=50 allHandlersAtMostOnce={c.EverySettingsHandlerSingle} boldHandlers={c.BoldHandlers} toggleUpdates={c.Updates - updates}");
            Require(c.EverySettingsHandlerSingle && c.BoldHandlers == 1 && c.Updates - updates == 1, "subtitle duplicate effect");
            Require(SettingHelper.Writes.GetValueOrDefault(SettingHelper.Player.SUBTITLE_BOLD) == 1 && SettingHelper.Writes.GetValueOrDefault(SettingHelper.Player.PLAYER_VOLUME) == 1 && SettingHelper.Writes.GetValueOrDefault(SettingHelper.VideoDanmaku.MERGE) == 1, "settings duplicate persistence");
            return new WeakReference(c);
        }
        public static void Run()
        {
            failures = 0;
            foreach (var mode in new[] { "active", "ordinary-close", "late-empty-close", "subtitle-pending-close", "late-info-close" })
                Case(mode, () => Roots(mode, mode == "active" ? 50 : 0));
            Case("public-terminal-fresh-live-unload-50", PublicBoundaries);
            foreach (bool success in new[] { false, true })
                Case("quality-close-" + success, () => QualityClose(success));
            foreach (bool same in new[] { false, true })
                foreach (bool success in new[] { false, true })
                    foreach (bool info in new[] { false, true })
                        Case($"replacement-same={same}-success={success}-info={info}", () => Replacement(same, success, info));
            foreach (var stage in new[] { "nodes", "online" })
                Case("info-late-" + stage, () => InfoAwait(stage));
            Case("node-close-50", NodeClose);
            foreach (bool sub in new[] { false, true })
                foreach (bool replace in new[] { false, true })
                    foreach (bool cancel in new[] { false, true })
                        Case($"picker-subtitle={sub}-replace={replace}-cancel={cancel}", () => Picker(sub, replace, cancel));
            foreach (bool replace in new[] { false, true })
                Case("track-rollback-replace=" + replace, () => TrackClose(replace));
            foreach (bool fault in new[] { false, true })
                Case("track-same-reference-fault=" + fault, () => TrackSharedModel(fault));
            Case("subtitle-same-reference-replacement-50", SubtitleReplacement);
            foreach (bool replace in new[] { false, true })
                Case("danmaku-full-chain-replace=" + replace, () => DanmakuContinuation(replace));
            Case("pending-interaction-same-reference-50", PendingInteractionReplacement);
            Case("late-info-fault-cancel-50", InfoFaults);
            Case("engine-live-late-close-public-callback-50", EngineBoundaries);
            Case("live-fallback-open-close-fault-cancel-50", LiveAndFaults);
            Case("settings-three-refresh-once-after-50-loads", () => { var reference = Settings(); Require(Collect(new List<WeakReference> { reference }) == 0, "internal settings graph retained owner"); });
            if (failures != 0) throw new Exception(failures + " actual player lifetime cases failed");
        }
    }
}

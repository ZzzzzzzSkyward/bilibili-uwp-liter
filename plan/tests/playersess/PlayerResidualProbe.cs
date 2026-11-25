using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiliLite.Api;
using BiliLite.Modules.Player;
using BiliLite.Services;

namespace ResidualProbe
{
    static class PlayerResidualProbe
    {
        static int failed;
        static readonly List<string> errors = new();
        static void Check(bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
        static void Case(string finding, string name, Action action)
        {
            errors.Clear();
            try
            {
                action();
                if (errors.Count != 0)
                    throw new Exception(errors.Count + " failed assertions: " + string.Join("; ", errors.Distinct()));
                Console.WriteLine($"RESIDUAL {finding} {name} PASS");
            }
            catch (Exception error) { failed++; Console.WriteLine($"RESIDUAL {finding} {name} FAIL {error}"); }
            finally
            {
                DispatcherTimer.StopAll();
                while (PlayerAPI.Exchanges.Count != 0)
                {
                    var unused = PlayerAPI.Exchanges.Dequeue();
                    unused.Transport.TrySetResult(new Response { Exchange = unused, status = false });
                    unused.Decode.TrySetResult(Nodes("unused"));
                }
                PlayerAPI.Requests.Clear();
            }
        }
        static void Ready(ControlSubject c, bool interactive = false)
        {
            c.playerHelper.Quality.TrySetResult(new BiliPlayUrlQualitesInfo());
            c.playerHelper.Info.TrySetResult(new PlayerInfo { interaction = interactive ? new InteractionInfo() : null });
            c.playerHelper.Subtitle.TrySetResult(new SubtitleModel());
        }
        static PlayInfo Manifest(string id)
        {
            var video = new LocalMediaPlaybackTrack { Track = new LocalTrack { Id = "video", Label = "Video" } };
            var audio = new LocalMediaPlaybackTrack { Track = new LocalTrack { Id = "audio", Label = "Audio" } };
            return new PlayInfo { cid = id, title = id, LocalPlayInfo = new LocalInfo { IsManifestPlayback = true, VideoTracks = new() { video, new LocalMediaPlaybackTrack { Track = new LocalTrack { Id = "video2", Label = "Video 2" } } }, AudioTracks = new() { audio, new LocalMediaPlaybackTrack { Track = new LocalTrack { Id = "audio2", Label = "Audio 2" } }, new LocalMediaPlaybackTrack { Track = new LocalTrack { Id = "audio3", Label = "Audio 3" } } }, SelectedVideoTrack = video, SelectedAudioTrack = audio } };
        }
        static void Open(ControlSubject c, PlayInfo item) => c.ConfigureList(new List<PlayInfo> { item });
        static PlayerOpenResult Success() => new() { result = true };
        static void Finish(TaskCompletionSource<PlayerOpenResult> gate, string outcome)
        {
            if (outcome == "fault")
                gate.SetException(new InvalidOperationException("engine-fault"));
            else if (outcome == "cancel")
                gate.SetCanceled();
            else
                gate.SetResult(new PlayerOpenResult { result = outcome == "success", message = "open-failed" });
        }
        static void SwitchReplacement(bool same, bool ownSwitch, string outcome)
        {
            for (int cycle = 0; cycle < 50; cycle++)
            {
                using var context = new ControlledContext();
                var c = new ControlSubject();
                Ready(c);
                var a = Manifest("1001");
                Open(c, a);
                var aGate = new TaskCompletionSource<PlayerOpenResult>();
                c.OpenWith(_ => aGate.Task);
                var aTask = c.StartTrack(a.LocalPlayInfo.AudioTracks[1]);
                Check(!c.SelectorsEnabled && !c.NextEnabled && c.Switching, "A switch must own disabled state");
                c.OpenWith(_ => Task.FromResult(Success()));
                var b = same ? a : Manifest("2002");
                Open(c, b);
                Check(c.OptionsReady && c.SelectorsEnabled && c.NextEnabled && !c.Switching, "B eligibility must restore before A completes");
                var previousB = b.LocalPlayInfo.SelectedAudioTrack;
                Task bTask = null;
                TaskCompletionSource<PlayerOpenResult> bGate = null;
                if (ownSwitch)
                {
                    bGate = new();
                    bool first = true;
                    c.OpenWith(_ => { if (first) { first = false; return bGate.Task; } return Task.FromResult(Success()); });
                    bTask = c.StartTrack(b.LocalPlayInfo.AudioTracks[2]);
                    c.ShowTransitionPending("Target B");
                    c.ClearTransitionPending();
                    Check(!c.SelectorsEnabled && !c.NextEnabled && c.Switching, "clearing transition released B switch");
                    c.ShowTransitionPending("Target B");
                    Check(c.PendingVisible && c.PendingLabel == "Target B", "transition label/visibility");
                }
                var selected = b.LocalPlayInfo.SelectedAudioTrack;
                var before = c.Publishes;
                var opens = c.MediaOpenCalls;
                var dialogs = c.Dialogs;
                Finish(aGate, outcome);
                context.Settle(aTask);
                Check(c.Publishes == before && c.MediaOpenCalls == opens && c.Dialogs == dialogs && ReferenceEquals(b.LocalPlayInfo.SelectedAudioTrack, selected), "old A outcome changed B UI/model/open/dialog");
                if (ownSwitch)
                {
                    Check(!c.SelectorsEnabled && !c.NextEnabled && c.Switching, "A completion released B switch");
                    Finish(bGate, outcome);
                    context.Settle(bTask);
                    Check(c.SelectorsEnabled && !c.NextEnabled && !c.Switching, "B completion must retain transition restriction");
                    c.ClearTransitionPending();
                    Check(c.NextEnabled, "clearing last reason restores Next");
                    Check(c.Dialogs == dialogs + (outcome == "success" ? 0 : 1), "current switch rollback/error policy");
                    Check(ReferenceEquals(b.LocalPlayInfo.SelectedAudioTrack, outcome == "success" ? selected : previousB), "current failure did not restore its previous track");
                }
                else
                    Check(c.SelectorsEnabled && c.NextEnabled && !c.Switching, "B state changed after A completed");
                c.Dispose();
                c.Unload();
                context.Drain();
                Check(context.Errors.Count == 0, "unhandled switch boundary fault");
            }
        }
        static void TerminalSwitch()
        {
            foreach (var outcome in new[] { "success", "failure", "fault", "cancel" })
                for (int cycle = 0; cycle < 50; cycle++)
                {
                    using var context = new ControlledContext();
                    var c = new ControlSubject();
                    Ready(c);
                    var item = Manifest("1001");
                    Open(c, item);
                    var gate = new TaskCompletionSource<PlayerOpenResult>();
                    c.OpenWith(_ => gate.Task);
                    var task = c.StartTrack(item.LocalPlayInfo.AudioTracks[1]);
                    c.Dispose();
                    var before = c.Publishes;
                    Finish(gate, outcome);
                    context.Settle(task);
                    c.ShowTransitionPending("closed");
                    c.ClearTransitionPending();
                    Check(c.Publishes == before && !c.SelectorsEnabled && !c.NextEnabled, "terminal completion re-enabled controls");
                    c.Unload();
                    context.Drain();
                    Check(context.Errors.Count == 0, "terminal switch fault");
                }
        }
        static void SelectorEntries()
        {
            foreach (bool video in new[] { false, true })
                using (var context = new ControlledContext())
                {
                    var c = new ControlSubject();
                    Ready(c);
                    var item = Manifest("1001");
                    Open(c, item);
                    var gate = new TaskCompletionSource<PlayerOpenResult>();
                    c.OpenWith(_ => gate.Task);
                    var track = video ? item.LocalPlayInfo.VideoTracks[1] : item.LocalPlayInfo.AudioTracks[1];
                    if (video)
                        c.SetVideoSelection(track);
                    else
                        c.SetSelection(track);
                    var action = context.Observe(video ? c.SelectVideo : c.SelectAudio);
                    Check(c.Switching && !c.SelectorsEnabled && !action.IsCompleted, "actual selector entry failed to suspend switch");
                    gate.SetResult(Success());
                    context.Settle(action);
                    Check(c.SelectorsEnabled && c.NextEnabled && !c.Switching, "actual current selector completion");
                    c.OpenWith(_ => Task.FromResult(Success()));
                    Open(c, new PlayInfo());
                    Check(c.SelectorsEnabled && !c.NextVisible, "normal empty/hidden selector policy changed");
                    c.Dispose();
                    var before = c.Publishes;
                    action = context.Observe(video ? c.SelectVideo : c.SelectAudio);
                    context.Settle(action);
                    Check(c.Publishes == before, "terminal selector event changed UI");
                    c.Unload();
                }
        }
        static InteractionEdgeInfoModel Nodes(string title, int edge = 1) => new() { title = title, edge_id = edge, story_list = new() { new InteractionEdgeInfoStoryListModel { edge_id = edge, title = title } } };
        static ChildExchange Exchange()
        {
            var exchange = new ChildExchange();
            PlayerAPI.Exchanges.Enqueue(exchange);
            return exchange;
        }
        static void ChildFinish(ChildExchange exchange, string phase, string outcome, string title)
        {
            if (phase == "transport")
            {
                if (outcome == "fault")
                    exchange.Transport.SetException(new InvalidOperationException("transport-fault"));
                else if (outcome == "cancel")
                    exchange.Transport.SetCanceled();
                else if (outcome == "failure")
                    exchange.Respond(false);
                else
                    exchange.Complete(Nodes(title));
            }
            else
            {
                if (outcome == "fault")
                    exchange.Decode.SetException(new InvalidOperationException("decode-fault"));
                else if (outcome == "cancel")
                    exchange.Decode.SetCanceled();
                else
                {
                    if (outcome == "failure")
                        exchange.Code = -1;
                    exchange.Decode.SetResult(Nodes(title));
                }
            }
            // Unused decode gates still settle after transport failure/cancellation/status failure.
            if (exchange.Transport.Task.IsCompleted && !exchange.DecodeStarted)
                exchange.Decode.TrySetResult(Nodes("unused"));
        }
        static void ChildClose(string phase, string outcome)
        {
            for (int cycle = 0; cycle < 50; cycle++)
            {
                using var context = new ControlledContext();
                var c = new ControlSubject();
                Ready(c, true);
                var exchange = Exchange();
                var task = c.StartInfo();
                var child = (InteractionVideoVM)c.ChildIdentity;
                var internalNotifications = new List<string>();
                child.PropertyChanged += (_, e) => internalNotifications.Add(e.PropertyName);
                Check(c.InteractionAttached(child) && !task.IsCompleted, "real child did not bind/suspend");
                if (phase == "decode")
                {
                    exchange.Respond();
                    context.Until(() => exchange.DecodeStarted);
                }
                int reentries = 0;
                c.DetachReentry(() => { var before = c.Calls; Open(c, new PlayInfo()); c.Load(); Check(c.Calls == before && c.ChildFieldsDetached, "detach callback reauthorized terminal owner"); reentries++; });
                bool detachedAtCallback = false;
                c.AtClose(() => detachedAtCallback = c.ChildIdentityDetached);
                c.Dispose();
                var notifications = c.BoundNotifications;
                var beforePublish = c.Publishes;
                var calls = c.Calls;
                var toasts = ControlSubject.Toasts;
                Check(c.ChildIdentityDetached && detachedAtCallback, "terminal child identity/view detachment must precede close callbacks");
                ChildFinish(exchange, phase, outcome, "late-A");
                context.Settle(task);
                Check(c.ChildIdentityDetached && c.BoundNotifications == notifications && c.Publishes == beforePublish && c.Calls == calls && ControlSubject.Toasts == toasts, "detached child changed parent/bound view/suffix");
                Check(internalNotifications.Contains("Loading"), "finite detached child finally did not execute");
                if (outcome == "success")
                    Check(internalNotifications.Contains("Info") && internalNotifications.Contains("List") && internalNotifications.Contains("Select"), "real successful child publication missing");
                Check(reentries == 2, "both actual view detach callbacks must execute");
                c.Dispose();
                c.Unload();
                context.Drain();
                Check(context.Errors.Count == 0, "real child fault escaped");
            }
        }
        static void ChildReplacement(bool same, bool fresh, string outcome)
        {
            for (int cycle = 0; cycle < 50; cycle++)
            {
                using var context = new ControlledContext();
                var a = new ControlSubject();
                Ready(a, true);
                var shared = a.CurrentPlayItem;
                var oldExchange = Exchange();
                var pending = a.StartInfo();
                var old = a.ChildIdentity;
                if (fresh)
                    a.Dispose();
                var b = fresh ? new ControlSubject() : a;
                b.playerHelper.Info = new();
                Ready(b, true);
                var currentExchange = Exchange();
                currentExchange.Complete(Nodes("B"));
                Open(b, same ? shared : new PlayInfo { cid = "2002" });
                var current = b.ChildIdentity;
                Check(current != null && !ReferenceEquals(old, current) && b.InteractionAttached(current), "live/fresh real B child did not bind");
                var before = b.Publishes;
                var notifications = b.BoundNotifications;
                ChildFinish(oldExchange, "transport", outcome, "late-A");
                context.Settle(pending);
                Check(b.ChildIdentity == current && b.BoundNotifications == notifications && b.Publishes == before && !a.InteractionAttached(old), "late child cleared/rebound B");
                var navigation = Exchange();
                navigation.Complete(Nodes("B-node", 2));
                context.Settle(b.Node(2, "2003"));
                Check(((InteractionVideoVM)b.ChildIdentity).Select.title == "B-node" && b.InteractionAttached(current), "active real node navigation failed");
                b.Dispose();
                b.Unload();
                if (fresh)
                    a.Unload();
                context.Drain();
                Check(context.Errors.Count == 0, "child replacement fault");
            }
        }
        sealed class SegmentGate
        {
            public string Cid; public int Index; public readonly TaskCompletionSource<List<DanmakuModel>> Completion = new();
            public void Finish(string outcome, string cue)
            {
                if (Completion.Task.IsCompleted)
                    return;
                if (outcome == "fault")
                    Completion.SetException(new InvalidOperationException("segment-fault"));
                else if (outcome == "cancel")
                    Completion.SetCanceled();
                else
                    Completion.SetResult(new() { new DanmakuModel { time_s = 400, text = cue } });
            }
        }
        sealed class Segments
        {
            public readonly List<SegmentGate> Requests = new();
            public bool Immediate;
            public Task<List<DanmakuModel>> Request(string cid, int index)
            {
                var gate = new SegmentGate { Cid = cid, Index = index };
                Requests.Add(gate);
                if (Immediate)
                    gate.Finish("success", cid + ":" + index);
                return gate.Completion.Task;
            }
            public SegmentGate Find(string cid, int index) => Requests.LastOrDefault(r => r.Cid == cid && r.Index == index);
            public void FinishAll()
            {
                Immediate = true;
                foreach (var gate in Requests.ToArray())
                    gate.Finish("success", "cleanup");
            }
        }
        static ControlSubject Interactive(ControlledContext context, Segments segments)
        {
            var c = new ControlSubject();
            Ready(c, true);
            var exchange = Exchange();
            exchange.Complete(Nodes("A"));
            segments.Immediate = true;
            c.playerHelper.SegmentRequest = segments.Request;
            Open(c, new PlayInfo { cid = "1001", is_interaction = true });
            Check(c.HasSegment(1) && c.HasCue("1001:1") && c.InteractionAttached(c.ChildIdentity), "accepted initial current pool/real child");
            segments.Immediate = false;
            return c;
        }
        static void NodeLease(string oldOutcome)
        {
            for (int cycle = 0; cycle < 50; cycle++)
            {
                using var context = new ControlledContext();
                var segments = new Segments();
                var c = Interactive(context, segments);
                c.Position(400);
                var oldTick = context.Observe(c.Tick);
                var old = segments.Find("1001", 2);
                Check(old != null && !old.Completion.Task.IsCompleted, "old timer segment not pending");
                var metadata = Exchange();
                var node = c.Node(2, "2002");
                int beforePolling = segments.Requests.Count;
                c.ControlTickState(0);
                var pressureA = context.Observe(c.Tick);
                var pressureB = context.Observe(c.Tick);
                Check(!c.HasCue("1001:1") && !c.HasSegment(1) && segments.Requests.Count == beforePolling && pressureA.IsCompleted && pressureB.IsCompleted && c.ControlTickStateValue==2, "new metadata phase retained cues/segments or polled old position");
                metadata.Complete(Nodes("B", 2));
                context.Until(() => segments.Find("2002", 1) != null || node.IsCompleted);
                var initial = segments.Find("2002", 1);
                Check(initial != null && !initial.Completion.Task.IsCompleted, "B initial segment1 must start before old completion");
                int rendered = c.Rendered;
                old.Finish(oldOutcome, "stale-A");
                context.Settle(oldTick);
                Check(!c.HasCue("stale-A") && c.Rendered == rendered, "old timer continuation rendered/reacquired B work");
                if (initial == null)
                {
                    var retry = c.Segment(1);
                    initial = segments.Find("2002", 1);
                    Check(initial != null, "explicit current retry could not acquire");
                }
                if (initial != null)
                {
                    int count = segments.Requests.Count;
                    int beforeSkip = c.Publishes;
                    var skippedManual = c.UpdateDanmaku();
                    Check(skippedManual.IsCompleted && !c.DanmakuSettled && c.Publishes == beforeSkip, "skipped manual caller changed initial settled/UI state");
                    var skippedA = c.Segment(3);
                    var skippedB = c.Segment(4);
                    var pressure = context.Observe(c.Tick);
                    Check(skippedA.IsCompleted && skippedB.IsCompleted, "same-node caller did not skip held lease");
                    Check(c.LoadBusy && segments.Requests.Count == count, "skipped/old finally unlocked B initial lease");
                    initial.Finish("success", "B-cue");
                    context.Settle(node);
                    segments.FinishAll();
                    context.Drain();
                    Check(c.HasCue("B-cue") && !c.HasCue("stale-A") && c.HasSegment(1), "current B payload/segment did not publish");
                }
                segments.Immediate = true;
                context.Settle(c.Segment(2));
                Check(c.HasSegment(2) && c.HasCue("2002:2"), "later current segment blocked");
                c.Dispose();
                segments.FinishAll();
                context.Drain();
                c.Unload();
                Check(context.Errors.Count == 0, "node/timer fault escaped");
            }
        }
        static void OldTimerAfterCurrentPayload(string outcome)
        {
            for (int cycle = 0; cycle < 50; cycle++)
            {
                using var context = new ControlledContext();
                var segments = new Segments();
                var c = Interactive(context, segments);
                c.Position(400);
                var oldTick = context.Observe(c.Tick);
                var old = segments.Find("1001", 2);
                var metadata = Exchange();
                metadata.Complete(Nodes("B", 2));
                var node = c.Node(2, "2002");
                var initial = segments.Find("2002", 1);
                Check(initial != null, "B request must precede old timer completion");
                if (initial != null)
                {
                    initial.Finish("success", "B-cue");
                    context.Settle(node);
                    var rendered = c.Rendered;
                    var calls = c.Calls;
                    var published = c.Publishes;
                    old.Finish(outcome, "late-A");
                    context.Settle(oldTick);
                    Check(c.Rendered == rendered && c.Calls == calls && c.Publishes == published && !c.HasCue("late-A") && c.HasCue("B-cue"), "old timer recaptured/rendered current B payload");
                }
                else
                {
                    // Keep cleanup and the positive current retry running after the original's failed safe assertion.
                    old.Finish(outcome, "late-A");
                    context.Settle(oldTick);
                    var retry = c.Segment(1);
                    initial = segments.Find("2002", 1);
                    initial.Finish("success", "B-cue");
                    context.Settle(retry);
                }
                int before = c.Rendered;
                segments.Immediate = true;
                context.Settle(context.Observe(c.Tick));
                Check(c.Rendered > before && c.HasSegment(2) && c.HasCue("2002:2"), "fresh current timer/cue rendering suppressed");
                c.Dispose();
                segments.FinishAll();
                context.Drain();
                c.Unload();
                Check(context.Errors.Count == 0, "timer completion fault escaped");
            }
        }
        static void ActiveChildFaultNavigation()
        {
            foreach (var phase in new[] { "transport", "decode" })
                foreach (var outcome in new[] { "failure", "fault", "cancel" })
                    for (int cycle = 0; cycle < 4; cycle++)
                    {
                        using var context = new ControlledContext();
                        var segments = new Segments();
                        var c = Interactive(context, segments);
                        segments.Immediate = true;
                        var owner = c.ChildIdentity;
                        int bound = c.BoundNotifications;
                        var toasts = ControlSubject.Toasts;
                        var exchange = Exchange();
                        var node = c.Node(2, "2002");
                        if (phase == "decode")
                        {
                            exchange.Respond();
                            context.Until(() => exchange.DecodeStarted);
                        }
                        ChildFinish(exchange, phase, outcome, "ignored");
                        context.Settle(node);
                        Check(c.ChildIdentity == owner && c.InteractionAttached(owner) && c.BoundNotifications >= bound + 4 && c.HasSegment(1) && ControlSubject.Toasts == toasts, "active real child handled error/finally/navigation behavior changed");
                        c.Dispose();
                        c.Unload();
                        context.Drain();
                        Check(context.Errors.Count == 0, "handled child failure escaped");
                    }
        }
        static void LeaseFailureAndReplacement()
        {
            foreach (var outcome in new[] { "fault", "cancel" })
                for (int cycle = 0; cycle < 8; cycle++)
                {
                    using var context = new ControlledContext();
                    var segments = new Segments();
                    var c = Interactive(context, segments);
                    var metadata = Exchange();
                    metadata.Complete(Nodes("B", 2));
                    var bNode = c.Node(2, "2002");
                    var b = segments.Find("2002", 1);
                    Check(b != null, "B initial missing");
                    int count = segments.Requests.Count;
                    var skipped = c.Segment(3);
                    var repeated = c.Segment(3);
                    Check(skipped.IsCompleted && repeated.IsCompleted, "same-generation caller did not skip");
                    Check(c.LoadBusy && segments.Requests.Count == count, "same-generation skipped call released lease");
                    b.Finish(outcome, "ignored");
                    context.Settle(bNode);
                    Check(!c.HasSegment(1) && !c.LoadBusy, "failed initial marked loaded/held lease");
                    segments.Immediate = true;
                    c.Position(400);
                    context.Settle(c.UpdateDanmaku());
                    Check(c.HasSegment(1) && c.HasCue("2002:1"), "manual current retry not usable");
                    segments.Immediate = false;
                    var late = c.Segment(2);
                    var lateGate = segments.Find("2002", 2);
                    metadata = Exchange();
                    metadata.Complete(Nodes("C", 3));
                    var cNode = c.Node(3, "3003");
                    var current = segments.Find("3003", 1);
                    Check(current != null && !c.HasCue("2002:1"), "C failed to supersede B pending segment/pool");
                    lateGate.Finish(outcome, "stale-B");
                    context.Settle(late);
                    count = segments.Requests.Count;
                    skipped = c.Segment(4);
                    Check(skipped.IsCompleted, "old finally allowed concurrent request");
                    Check(c.LoadBusy && segments.Requests.Count == count, "B finally unlocked C");
                    if (current == null)
                    {
                        var retry = c.Segment(1);
                        current = segments.Find("3003", 1);
                    }
                    current?.Finish("success", "C-cue");
                    segments.FinishAll();
                    context.Settle(cNode);
                    Check(c.HasCue("C-cue") && !c.HasCue("stale-B") && c.HasSegment(1), "C accepted ownership failed");
                    c.Dispose();
                    segments.FinishAll();
                    context.Drain();
                    c.Unload();
                    Check(context.Errors.Count == 0, "lease fault escaped");
                }
        }
        static void DownloadTimer()
        {
            using var context = new ControlledContext();
            var c = new ControlSubject();
            Ready(c);
            int requests = 0;
            c.playerHelper.SegmentRequest = (cid, index) => { requests++; return Task.FromResult(new List<DanmakuModel>()); };
            Open(c, new PlayInfo { cid = "download", play_mode = VideoPlayType.Download, LocalPlayInfo = new LocalInfo { DanmakuPath = "local.xml" } });
            context.Settle(context.Observe(c.Tick));
            Check(requests == 0, "download timer incorrectly polled online segments after reset");
            c.Dispose();
            c.Unload();
        }
        static void HistoryBoundary(ControlSubject control, TaskCompletionSource<bool> transport)
        {
            // Primitive service boundary: a negative handled result completes normally;
            // an unexpected exception still propagates to the complete actual ended method.
            control.playerHelper.HistoryRequest = async (_, _) =>
            {
                if (!await transport.Task)
                    control.playerHelper.HandledHistoryFailures++;
            };
        }
        static void EndBoundary(string boundary, string outcome)
        {
            for (int cycle = 0; cycle < 50; cycle++)
            {
                using var context = new ControlledContext();
                var c = new ControlSubject();
                Ready(c);
                var a = new PlayInfo { cid = "1001", title = "A" };
                Open(c, a);
                int events = 0;
                c.AllMediaEndEvent += (_, _) => events++;
                var report = new TaskCompletionSource<bool>();
                HistoryBoundary(c, report);
                c.End();
                Check(context.Operations == 1 && c.playerHelper.Reports.Count == 1, "real ended report did not suspend exactly once");
                Check(c.playerHelper.Reports[0].item == a && c.playerHelper.Reports[0].position == 200, "original history identity/duration changed");
                if (boundary == "close")
                    c.Dispose();
                else
                    Open(c, boundary == "same" ? a : new PlayInfo { cid = "2002", title = "B" });
                var calls = c.Calls;
                var published = c.Publishes;
                var endUi = c.EndUiWrites;
                var playing = c.AutoPlaying;
                var toasts = ControlSubject.Toasts;
                if (outcome == "fault")
                    report.SetException(new InvalidOperationException("unexpected-history"));
                else
                    report.SetResult(outcome != "handled");
                context.Drain();
                Check(events == 0 && c.Calls == calls && c.Publishes == published && c.EndUiWrites == endUi && c.AutoPlaying == playing && ControlSubject.Toasts == toasts, "obsolete ended suffix acted on closed/replacement owner");
                Check(c.playerHelper.Reports.Count == 1 && context.Errors.Count == 0 && c.playerHelper.HandledHistoryFailures == (outcome == "handled" ? 1 : 0), "report duplicated, handled result lost or async-void fault escaped");
                if (boundary == "close")
                {
                    c.End();
                    context.Drain();
                    Check(c.playerHelper.Reports.Count == 1 && events == 0, "direct terminal ended entry authorized work");
                }
                c.Dispose();
                c.Unload();
                context.Drain();
            }
        }
        static void EndSuffixModes()
        {
            foreach (var boundary in new[] { "close", "distinct", "same" })
                foreach (var outcome in new[] { "success", "handled", "fault" })
                    foreach (int mode in new[] { 0, 1, 2 })
                        foreach (bool next in new[] { false, true })
                            foreach (int index in new[] { 0, 1 })
                            {
                                using var context = new ControlledContext();
                                var c = new ControlSubject();
                                Ready(c);
                                var original = new PlayInfo { cid = "original" };
                                Open(c, original);
                                int events = 0;
                                c.AllMediaEndEvent += (_, _) => events++;
                                var report = new TaskCompletionSource<bool>();
                                HistoryBoundary(c, report);
                                c.End();
                                if (boundary == "close")
                                    c.Dispose();
                                else
                                    c.ConfigureList(new List<PlayInfo> { boundary == "same" ? original : new PlayInfo { cid = "B0" }, new PlayInfo { cid = "B1" } }, index);
                                c.EndSettings(mode, next);
                                var calls = c.Calls;
                                var ui = c.EndUiWrites;
                                var published = c.Publishes;
                                var playing = c.AutoPlaying;
                                var toasts = ControlSubject.Toasts;
                                var current = c.Index;
                                if (outcome == "fault")
                                    report.SetException(new InvalidOperationException("history-suffix-fault"));
                                else
                                    report.SetResult(outcome != "handled");
                                context.Drain();
                                Check(events == 0 && c.Calls == calls && c.EndUiWrites == ui && c.Publishes == published && c.AutoPlaying == playing && ControlSubject.Toasts == toasts && c.Index == current, "stale ended loop/next/toast/UI suffix was not quiet");
                                Check(c.playerHelper.Reports.Count == 1 && context.Errors.Count == 0 && c.playerHelper.HandledHistoryFailures == (outcome == "handled" ? 1 : 0), "stale suffix report duplication/handled result/unhandled fault");
                                c.Dispose();
                                c.Unload();
                                context.Drain();
                            }
        }
        static void ActiveEndModes()
        {
            foreach (var outcome in new[] { "success", "handled", "fault" })
                foreach (int mode in new[] { 0, 1, 2 })
                    foreach (bool next in new[] { false, true })
                        foreach (int count in new[] { 1, 2 })
                            foreach (int index in Enumerable.Range(0, count))
                            {
                                using var context = new ControlledContext();
                                var c = new ControlSubject();
                                Ready(c);
                                var items = Enumerable.Range(0, count).Select(i => new PlayInfo { cid = "part" + i, title = "part" + i }).ToList();
                                c.ConfigureList(items, index);
                                c.EndSettings(mode, next);
                                c.Position(55);
                                foreach (var item in items)
                                    SettingHelper.Values[item.cid] = 123.0;
                                var report = new TaskCompletionSource<bool>();
                                HistoryBoundary(c, report);
                                int events = 0;
                                c.AllMediaEndEvent += (_, _) => events++;
                                var plays = c.PlayCalls;
                                var toasts = ControlSubject.Toasts;
                                var clears = c.EndUiWrites;
                                var logs = c.HistoryLogs;
                                c.End();
                                if (outcome == "fault")
                                    report.SetException(new InvalidOperationException("unexpected-active-history"));
                                else
                                    report.SetResult(outcome != "handled");
                                context.Drain();
                                bool advances = (mode == 0 && index < count - 1 && next) || (mode == 2 && next && count > 1);
                                bool replay = mode == 1 || (mode == 2 && next && count == 1);
                                int target = mode == 2 && index == count - 1 ? 0 : index + 1;
                                Check(c.playerHelper.Reports.Count == 1 && c.playerHelper.Reports[0].item == items[index] && c.playerHelper.Reports[0].position == 200, "active report identity/duration/count");
                                Check(events == (mode == 0 && index == count - 1 ? 1 : 0), "ordered final event semantics");
                                Check(c.PlayCalls - plays == (replay ? 1 : 0) && c.Index == (advances ? target : index), "active loop/next/wrap behavior");
                                if (advances)
                                    Check(c.StartPosition == 0, "next part must start FromStart rather than saved progress");
                                bool completionToast = (mode == 0 && index < count - 1 && !next) || (mode == 2 && !next);
                                Check(ControlSubject.Toasts - toasts == (completionToast ? 1 : 0), "active preference/completion toast");
                                Check(context.Errors.Count == 0 && (outcome != "fault" || c.HistoryLogs > logs) && c.playerHelper.HandledHistoryFailures == (outcome == "handled" ? 1 : 0), "handled report result or unexpected fault containment/logging");
                                c.Dispose();
                                c.Unload();
                                context.Drain();
                            }
            using (var context = new ControlledContext())
            {
                var c = new ControlSubject();
                Ready(c);
                Open(c, new PlayInfo());
                var report = new TaskCompletionSource<bool>();
                HistoryBoundary(c, report);
                c.EndSettings(0, true);
                var plays = c.PlayCalls;
                c.End();
                c.EndSettings(1, false);
                report.SetResult(true);
                context.Drain();
                Check(c.PlayCalls == plays + 1, "current-owner preference changed during report was frozen");
                c.Dispose();
                c.Unload();
                c = new ControlSubject();
                Ready(c);
                Open(c, Manifest("1001"));
                int events = 0;
                c.AllMediaEndEvent += (_, _) => events++;
                c.End();
                context.Drain();
                Check(c.playerHelper.Reports.Count == 0 && events == 1, "manifest history skip");
                c.Dispose();
                c.Unload();
                c = new ControlSubject();
                Ready(c);
                Open(c, new PlayInfo());
                int toasts = ControlSubject.Toasts;
                c.End();
                context.Drain();
                Check(ControlSubject.Toasts == toasts + 1, "ordered final no-subscriber message");
                c.Dispose();
                c.Unload();
                Check(context.Errors.Count == 0, "active end controls unhandled");
            }
            foreach (int leaf in new[] { 0, 1 })
                using (var context = new ControlledContext())
                {
                    var segments = new Segments();
                    var c = Interactive(context, segments);
                    c.SetLeaf(leaf);
                    int events = 0;
                    c.AllMediaEndEvent += (_, _) => events++;
                    int toasts = ControlSubject.Toasts;
                    var ui = c.EndUiWrites;
                    c.End();
                    context.Drain();
                    Check(c.playerHelper.Reports.Count == 0 && events == 0 && ControlSubject.Toasts - toasts == (leaf == 1 ? 1 : 0) && c.EndUiWrites - ui == (leaf == 0 ? 2 : 0), "interactive leaf/nonleaf behavior");
                    c.Dispose();
                    c.Unload();
                }
        }
        #region RF1 additive actual-click controls
        static void AbsentUpdate(string precondition)
        {
            int unhandled = 0, changed = 0, requests = 0, guards = 0, leases = 0, pools = 0, segments = 0, publications = 0, toasts = 0, suspendedFileTripwires = 0, nullAuthorized = 0;
            var exceptionTypes = new HashSet<string>();
            for (int cycle = 0; cycle < 50; cycle++)
            {
                using var context = new ControlledContext();
                var c = new ControlSubject();
                // Remove the old fixture's synthetic public item to match the real constructor default.
                // Season pending/failed/empty detail leaves this same state; no fake Initialize is called.
                c.CurrentPlayItem = null;
                FileIO.Gate = new TaskCompletionSource<string>();
                var before = c.RefreshState();
                var click = context.Observe(c.UpdateClick);
                Check(click.IsCompleted, "absent click attempted suspended work/file I/O");
                if (!click.IsCompleted) suspendedFileTripwires++;
                FileIO.Gate.TrySetResult("{}");
                context.Settle(click);
                context.Drain();
                var after = c.RefreshState();
                unhandled += context.Errors.Count;
                foreach (var error in context.Errors) exceptionTypes.Add(error.GetType().Name);
                if (before != after) changed++;
                requests += after.Calls - before.Calls;
                guards += after.GuardGeneration - before.GuardGeneration;
                if (!ReferenceEquals(before.Lease, after.Lease)) leases++;
                if (!ReferenceEquals(before.Pool, after.Pool) || before.PoolContents != after.PoolContents) pools++;
                if (!ReferenceEquals(before.Segments, after.Segments) || before.SegmentContents != after.SegmentContents) segments++;
                publications += after.Publications - before.Publications;
                toasts += after.Toasts - before.Toasts;
                if (c.ActualWorkCurrent(null, c.WorkGeneration)) nullAuthorized++;
                Check(context.Errors.Count == 0, "absent actual click posted an unhandled error");
                Check(before == after && c.CurrentPlayItem == null && !c.TerminalOwner,
                    "absent click changed request/guard/lease/pool/segment/settled/view/toast/owner state");
                Check(!c.ActualWorkCurrent(null, c.WorkGeneration), "matching null references authorized playback work");
                c.Dispose();
                c.Unload();
                context.Drain();
                FileIO.Gate = null;
            }
            Console.WriteLine($"RF1_ABSENT precondition={precondition} cycles=50 unhandled={unhandled} exceptionTypes={string.Join(",", exceptionTypes)} stateChanges={changed} requests={requests} guards={guards} leases={leases} pools={pools} segments={segments} publications={publications} toasts={toasts} suspendedFileTripwires={suspendedFileTripwires} nullAuthorized={nullAuthorized}");
        }
        static void EligibleRefresh(ControlSubject c, ControlledContext context, string cid)
        {
            var requested = new List<string>();
            c.playerHelper.SegmentRequest = (currentCid, segment) =>
            {
                requested.Add(currentCid + ":" + segment);
                return Task.FromResult(new List<DanmakuModel>
                {
                    new DanmakuModel { time_s = 400, text = "RF1-update-" + currentCid + ":" + segment }
                });
            };
            c.Position(400);
            int calls = c.playerHelper.DanmakuCalls;
            int toasts = ControlSubject.Toasts;
            int generation = c.WorkGeneration;
            var click = context.Observe(c.UpdateClick);
            context.Settle(click);
            context.Drain();
            Check(context.Errors.Count == 0 && c.playerHelper.DanmakuCalls - calls == 2 &&
                requested.SequenceEqual(new[] { cid + ":2", cid + ":1" }), "initialized actual click must retain two current-cid requests");
            Check(c.HasCue("RF1-update-" + cid + ":1") && c.HasSegment(1) && c.DanmakuSettled &&
                !c.LoadBusy && c.RefreshState().Lease == null && ControlSubject.Toasts == toasts + 1 &&
                c.WorkGeneration == generation, "current click suppressed cue/feedback or retained load ownership");
        }
        static void InitializedUpdate()
        {
            for (int cycle = 0; cycle < 50; cycle++)
            {
                using var context = new ControlledContext();
                var c = new ControlSubject();
                Ready(c);
                var initial = new Segments { Immediate = true };
                c.playerHelper.SegmentRequest = initial.Request;
                Open(c, new PlayInfo { cid = "7101", title = "RF1 current" });
                Check(c.WorkGeneration > 0 && c.CurrentPlayItem.cid == "7101" && c.HasSegment(1) &&
                    c.HasCue("7101:1") && c.MediaOpenCalls == 1, "positive owner did not traverse real public initialization");
                EligibleRefresh(c, context, "7101");
                c.Dispose();
                c.Unload();
            }
        }
        static void UpdateLease()
        {
            for (int cycle = 0; cycle < 50; cycle++)
            {
                using var context = new ControlledContext();
                var segments = new Segments();
                var c = Interactive(context, segments);
                var metadata = Exchange();
                var node = c.Node(2, "2002");
                var before = c.RefreshState();
                context.Settle(context.Observe(c.UpdateClick));
                Check(c.RefreshState() == before, "click stole initial-node metadata ownership");
                metadata.Complete(Nodes("RF1 B", 2));
                var initial = segments.Find("2002", 1);
                Check(initial != null && c.LoadBusy, "current initial lease missing");
                before = c.RefreshState();
                context.Settle(context.Observe(c.UpdateClick));
                Check(c.RefreshState() == before && c.LoadBusy, "click duplicated/released current initial load");
                initial.Finish("success", "RF1-current-B");
                context.Settle(node);
                EligibleRefresh(c, context, "2002");
                c.Dispose();
                segments.FinishAll();
                context.Drain();
                c.Unload();
            }
        }
        static void TerminalUpdate()
        {
            foreach (bool initialized in new[] { false, true })
                for (int cycle = 0; cycle < 50; cycle++)
                {
                    using var context = new ControlledContext();
                    var c = new ControlSubject();
                    if (initialized)
                    {
                        Ready(c);
                        Open(c, new PlayInfo { cid = "7201" });
                    }
                    else c.CurrentPlayItem = null;
                    c.Dispose();
                    var before = c.RefreshState();
                    context.Settle(context.Observe(c.UpdateClick));
                    context.Drain();
                    Check(c.TerminalOwner && c.RefreshState() == before && context.Errors.Count == 0,
                        "terminal empty/initialized click created work or reset ownership");
                    c.Unload();
                }
        }
        static void WorkHelperTruthConditions()
        {
            using var context = new ControlledContext();
            var c = new ControlSubject();
            // Retain the prior fixture's valid non-null generation0 truth condition.
            var current = c.CurrentPlayItem;
            Check(c.WorkGeneration == 0 && c.ActualWorkCurrent(current, 0), "non-null generation0 context rejected");
            Check(!c.ActualWorkCurrent(null, 0) && !c.ActualWorkCurrent(new PlayInfo(), 0) &&
                !c.ActualWorkCurrent(current, 1), "helper accepted null/different reference/stale generation");
            c.Dispose();
            Check(!c.ActualWorkCurrent(current, c.WorkGeneration), "terminal helper accepted matching current item");
            c.Unload();
            foreach (bool manifest in new[] { false, true })
            {
                c = new ControlSubject();
                Ready(c);
                var item = manifest ? Manifest("local-manifest") : new PlayInfo
                {
                    cid = "local-download", play_mode = VideoPlayType.Download, LocalPlayInfo = new LocalInfo()
                };
                Open(c, item);
                Check(c.MediaOpenCalls == 1 && c.ActualWorkCurrent(item, c.WorkGeneration),
                    "real initialized local/manifest owner rejected");
                int oldGeneration = c.WorkGeneration;
                Open(c, item);
                Check(!c.ActualWorkCurrent(item, oldGeneration) && c.ActualWorkCurrent(item, c.WorkGeneration),
                    "same-reference replacement generation truth conditions changed");
                c.Dispose();
                Check(!c.ActualWorkCurrent(item, c.WorkGeneration), "disposed local/manifest owner accepted");
                c.Unload();
            }
            context.Drain();
            Check(context.Errors.Count == 0, "helper/current-owner control fault escaped");
        }
        #endregion

        public static void Run()
        {
            failed = 0;
            foreach (bool same in new[] { false, true })
                foreach (bool own in new[] { false, true })
                    foreach (var outcome in new[] { "success", "failure", "fault", "cancel" })
                        Case("PF1", $"same={same}-Bswitch={own}-{outcome}-50", () => SwitchReplacement(same, own, outcome));
            Case("PF1", "terminal-50-per-outcome", TerminalSwitch);
            Case("PF1", "real-selector-current-and-empty-policy", SelectorEntries);
            foreach (var phase in new[] { "transport", "decode" })
                foreach (var outcome in new[] { "success", "failure", "fault", "cancel" })
                    Case("FA1", $"close-{phase}-{outcome}-50", () => ChildClose(phase, outcome));
            foreach (bool same in new[] { false, true })
                foreach (bool fresh in new[] { false, true })
                    foreach (var outcome in new[] { "success", "fault", "cancel" })
                        Case("FA1", $"replacement-same={same}-fresh={fresh}-{outcome}-50", () => ChildReplacement(same, fresh, outcome));
            Case("FA1", "active-real-child-error-navigation", ActiveChildFaultNavigation);
            foreach (var outcome in new[] { "success", "fault", "cancel" })
                Case("FA2", $"node-timer-lease-{outcome}-50", () => NodeLease(outcome));
            foreach (var outcome in new[] { "success", "fault", "cancel" })
                Case("FA2", $"old-timer-after-current-payload-{outcome}-50", () => OldTimerAfterCurrentPayload(outcome));
            Case("FA2", "same-generation-failure-retry-C-replacement", LeaseFailureAndReplacement);
            Case("FA2", "download-local-timer-policy", DownloadTimer);
            foreach (var boundary in new[] { "close", "distinct", "same" })
                foreach (var outcome in new[] { "success", "handled", "fault" })
                    Case("FA3", $"{boundary}-{outcome}-50", () => EndBoundary(boundary, outcome));
            Case("FA3", "stale-suffix-mode-permutations", EndSuffixModes);
            Case("FA3", "active-modes-current-preference", ActiveEndModes);
            // RF1 keeps every prior58 group above unchanged and adds the actual click boundary.
            foreach (var state in new[] { "constructed", "season-loading", "season-failed", "season-empty" })
                Case("RF1", "absent-" + state + "-50", () => AbsentUpdate(state));
            Case("RF1", "genuine-initialized-two-request-refresh-50", InitializedUpdate);
            Case("RF1", "initial-node-held-lease-and-later-refresh-50", UpdateLease);
            Case("RF1", "terminal-empty-and-initialized-50", TerminalUpdate);
            Case("RF1", "actual-helper-truth-conditions", WorkHelperTruthConditions);
            if (failed != 0)
                throw new Exception(failed + " residual actual-source cases failed");
        }
    }
}

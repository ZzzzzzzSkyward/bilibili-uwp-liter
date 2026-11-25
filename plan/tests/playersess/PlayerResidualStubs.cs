#pragma warning disable CS0067, CS0649, CS0414
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using BiliLite.Services;
using BiliLite.Modules.Player;

namespace BiliLite.Models
{
    public class ReturnModel<T>
    {
        public bool success; public string message;
    }
}
namespace Windows.UI
{
    public struct Color
    {
        public byte R, G, B, A;
    }
}
namespace Windows.UI.Xaml.Media
{
    public class SolidColorBrush
    {
        public SolidColorBrush(Windows.UI.Color color)
        {
        }
    }
}
namespace BiliLite.Api
{
    public class Envelope<T>
    {
        public int code; public T data;
    }
    public class ChildExchange
    {
        public readonly TaskCompletionSource<Response> Transport = new();
        public readonly TaskCompletionSource<InteractionEdgeInfoModel> Decode = new();
        public bool DecodeStarted; public int Code;
        public void Respond(bool status = true) => Transport.SetResult(new Response { Exchange = this, status = status });
        public void Complete(InteractionEdgeInfoModel value)
        {
            Respond();
            Decode.SetResult(value);
        }
    }
    public class Response
    {
        public ChildExchange Exchange; public bool status = true;
        public async Task<Envelope<T>> GetData<T>()
        {
            Exchange.DecodeStarted = true;
            return new Envelope<T> { code = Exchange.Code, data = (T)(object)await Exchange.Decode.Task };
        }
    }
    public class Request
    {
        public Task<Response> RequestTask;
    }
    public static class RequestExtension
    {
        public static Task<Response> Request(this Request request) => request.RequestTask;
    }
    public class PlayerAPI
    {
        public static readonly Queue<ChildExchange> Exchanges = new();
        public static readonly List<(string aid, int version, int edge)> Requests = new();
        public Request InteractionEdgeInfo(string aid, int version, int edge_id)
        {
            Requests.Add((aid, version, edge_id));
            if (Exchanges.Count == 0)
                throw new InvalidOperationException("Missing explicit real-child exchange");
            return new Request { RequestTask = Exchanges.Dequeue().Transport.Task };
        }
    }
}
namespace ResidualWindows.UI.Core
{
    enum CoreDispatcherPriority
    {
        Low
    }
}
namespace ResidualProbe
{
    static class BoundaryLog
    {
        public static readonly List<Exception> Errors = new();
        public static bool IsNetworkError(Exception exception) => false;
        public static void Log(string text, BiliLite.Helpers.LogType type, Exception error) => Errors.Add(error);
    }
    // Async-void OperationStarted/Completed plus queued exception capture, not sleep-based observation.
    sealed class ControlledContext : SynchronizationContext, IDisposable
    {
        readonly SynchronizationContext previous;
        readonly Queue<(SendOrPostCallback callback, object state)> queue = new();
        readonly AutoResetEvent ready = new(false);
        public readonly List<Exception> Errors = new();
        public int Operations;
        readonly AsyncLocal<TaskCompletionSource<bool>> observed = new();
        public Task Observe(Action action)
        {
            var gate = new TaskCompletionSource<bool>();
            var previous = observed.Value;
            observed.Value = gate;
            try
            {
                action();
            }
            finally { observed.Value = previous; }
            return gate.Task;
        }
        public ControlledContext()
        {
            previous = Current;
            SetSynchronizationContext(this);
        }
        public override void OperationStarted() => Interlocked.Increment(ref Operations);
        public override void OperationCompleted()
        {
            lock(queue)
            {
                Interlocked.Decrement(ref Operations);
                observed.Value?.TrySetResult(true);
                ready.Set();
            }
        }
        public override void Post(SendOrPostCallback callback, object state)
        {
            lock (queue)
            {
                queue.Enqueue((callback, state));
                ready.Set();
            }
        }
        public void Until(Func<bool> settled)
        {
            var clock = Stopwatch.StartNew();
            while (true)
            {
                (SendOrPostCallback callback, object state) job = default;
                bool exists;
                lock (queue)
                {
                    exists = queue.Count != 0;
                    if (exists)
                        job = queue.Dequeue();
                }
                if (exists)
                {
                    try
                    {
                        job.callback(job.state);
                    }
                    catch (Exception error) { Errors.Add(error); }
                    continue;
                }
                if (settled())
                    return;
                if (clock.Elapsed > TimeSpan.FromSeconds(10))
                    throw new Exception("Deterministic gate did not settle");
                ready.WaitOne(10);
            }
        }
        public void Settle(Task task)
        {
            Until(() => task.IsCompleted);
            task.GetAwaiter().GetResult();
        }
        public void Drain() => Until(() => Volatile.Read(ref Operations) == 0);
        public void Dispose()
        {
            Drain();
            SetSynchronizationContext(previous);
            lock(queue) ready.Dispose();
        }
    }
    partial class Element
    {
        public Action AfterContextChange; public int BoundNotifications;
        void BoundChanged(object sender, PropertyChangedEventArgs args) => BoundNotifications++;
    }
    partial class LocalInfo
    {
        public List<LocalMediaPlaybackTrack> VideoTracks = new(), AudioTracks = new();
    }
    partial class LocalTrack
    {
        public string Quality;
    }
    partial class StorageFile
    {
        public string Name = "fixture";
    }
    class LocalMediaTrackSelectionOption
    {
        public string Label; public LocalMediaPlaybackTrack Track; public bool IsEmbeddedAudio;
    }
    partial class PlayInfo
    {
        public bool is_interaction;
    }
    partial class PlayerVM
    {
        public Func<string, int, Task<List<DanmakuModel>>> SegmentRequest;
        public int HandledHistoryFailures;
        public Func<PlayInfo, double, Task> HistoryRequest;
        public readonly List<(PlayInfo item, double position)> Reports = new();
    }
    partial class PlayerStub
    {
        public Func<PlayerSessionTicket, Task<PlayerOpenResult>> OpenRequest;
        public double Duration = 200; public Action OnDispose;
    }
    partial class DanmakuStub
    {
        public int Pauses;
        public readonly List<DanmakuModel> Rendered = new();
        public void PauseDanmaku() => Pauses++;
        public void AddDanmu(DanmakuModel model, bool scroll) => Rendered.Add(model);
    }
    class TextBox
    {
    }
    class AutoSuggestBox
    {
    }
    static class FocusManager
    {
        public static object GetFocusedElement() => null;
    }
    sealed class FixtureDispatcher
    {
        public Task RunAsync(ResidualWindows.UI.Core.CoreDispatcherPriority priority, Action action)
        {
            action();
            return Task.CompletedTask;
        }
    }
    sealed class CompareDanmakuModel : IEqualityComparer<DanmakuModel>
    {
        public bool Equals(DanmakuModel left, DanmakuModel right) => left.text == right.text;
        public int GetHashCode(DanmakuModel model) => model.text.GetHashCode();
    }
    static class ToolTipService
    {
        public static void SetToolTip(Element element, string text)
        {
            element.Tag = text;
        }
    }
    static class AutomationProperties
    {
        public static void SetName(Element element, string text)
        {
            element.Content = text;
        }
    }
    partial class ControlSubject
    {
        private string _pendingTargetText, _nextTargetText;
        private bool _hasOuterNextTarget, _updatingLocalTrackSelectors;
        private Element TransitionPendingText = new(), TransitionPendingHost = new();
        private int showControlsFlag = -1; private bool Buffering;
        private Element GridBuffering = new() { Visibility = Visibility.Collapsed };
        private FixtureDispatcher Dispatcher = new();
        private void ShowControl(bool show)
        {
        }
        public event EventHandler AllMediaEndEvent;
        public bool SelectorsEnabled => LocalVideoTrackSelector.IsEnabled && LocalAudioTrackSelector.IsEnabled;
        public bool NextVisible => BottomBtnNext.Visibility == Visibility.Visible;
        public bool NextEnabled => BottomBtnNext.IsEnabled;
        public bool Switching => _localTrackSwitching;
        public bool LoadBusy => loadingDanmaku; public bool DanmakuSettled => _danmakuLoadSettled;
        public int BoundNotifications => NodeList.BoundNotifications + InteractionChoices.BoundNotifications;
        public bool ChildIdentityDetached => interactionVideoVM == null && _interactionVideoItem == null && NodeList.DataContext == null && InteractionChoices.DataContext == null;
        public object ChildIdentity => interactionVideoVM;
        public bool OptionsReady => ((IList<LocalMediaTrackSelectionOption>)LocalVideoTrackSelector.ItemsSource)?.Count > 0 && ((IList<LocalMediaTrackSelectionOption>)LocalAudioTrackSelector.ItemsSource)?.Count > 0;
        public string PendingLabel => TransitionPendingText.Text;
        public bool PendingVisible => TransitionPendingHost.Visibility == Visibility.Visible;
        public int Rendered => DanmuControl.Rendered.Count;
        public bool HasCue(string cue) => danmakuPool != null && System.Linq.Enumerable.Any(System.Linq.Enumerable.SelectMany(danmakuPool.Values, x => x), x => x.text == cue);
        public bool HasSegment(int segment) => danmakuLoadedSegment?.Contains(segment) == true;
        public int CueCount => danmakuPool == null ? 0 : System.Linq.Enumerable.Count(System.Linq.Enumerable.SelectMany(danmakuPool.Values, x => x));
        public Task Segment(int segment) => LoadDanmaku(segment);
        public Task Node(int node, string cid) => ChangedNode(node, cid);
        public void ControlTickState(int value)=>showControlsFlag=value;
        public int ControlTickStateValue=>showControlsFlag;
        public void Tick() => DanmuTimer_Tick(this, EventArgs.Empty);
        public void End() => Player_PlayMediaEnded(this, EventArgs.Empty);
        public void Position(double value) => Player.Position = value;
        public void OpenWith(Func<PlayerSessionTicket, Task<PlayerOpenResult>> request) => Player.OpenRequest = request;
        public void AtClose(Action action) => Player.OnDispose = action;
        public void DetachReentry(Action action)
        {
            NodeList.AfterContextChange = action;
            InteractionChoices.AfterContextChange = action;
        }
        public bool ChildFieldsDetached => interactionVideoVM == null && _interactionVideoItem == null;
        public int HistoryLogs => LogHelper.Calls;
        public double StartPosition => _position;
        public void EndSettings(int mode, bool autoNext)
        {
            PlayerSettingPlayMode.SelectedIndex = mode;
            PlayerSettingAutoNext.IsOn = autoNext;
        }
        public void ConfigureList(List<PlayInfo> items, int index = 0) => InitializePlayInfo(items, index, PlayTargetStart.FromStart);
        public int Index => CurrentPlayIndex;
        public bool AutoPlaying => _autoPlay;
        public int EndUiWrites => InteractionChoices.Changes + DanmuControl.Clears + DanmuControl.Pauses + BorderSubtitle.Changes;
        public void SetLeaf(int leaf)
        {
            interactionVideoVM.Info.is_leaf = leaf;
        }
        public void SetSelection(LocalMediaPlaybackTrack track) => LocalAudioTrackSelector.SelectedItem = new LocalMediaTrackSelectionOption { Track = track };
        public void SetVideoSelection(LocalMediaPlaybackTrack track) => LocalVideoTrackSelector.SelectedItem = new LocalMediaTrackSelectionOption { Track = track };
        public void SelectVideo() => LocalVideoTrackSelector_SelectionChanged(this, new SelectionChangedEventArgs());
        public void SelectAudio() => LocalAudioTrackSelector_SelectionChanged(this, new SelectionChangedEventArgs());
    }
}

// RF1 additive observation only: no product lifetime/generation/lease reset.
namespace ResidualProbe
{
    using System.Linq;
    sealed record RefreshSnapshot(
        object Item, int Generation, bool Terminal, object Lease, object Pool, object Segments,
        object PoolItem, string PoolContents, string SegmentContents, bool Settled, bool Loading,
        int GuardGeneration, object GuardOwner, int Publications, int Calls, int Toasts, int Rendered);
    partial class ControlSubject
    {
        public void UpdateClick() => DanmuSettingUpdateDanmaku_Click(this, new RoutedEventArgs());
        public int WorkGeneration => _playItemGeneration;
        public bool TerminalOwner => _disposed;
        public bool ActualWorkCurrent(PlayInfo item, int generation) => IsPlaybackWorkCurrent(item, generation);
        public RefreshSnapshot RefreshState()
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var guard = _danmakuLoadGuard.GetType();
            return new RefreshSnapshot(CurrentPlayItem, _playItemGeneration, _disposed,
                _danmakuLoadLease, danmakuPool, danmakuLoadedSegment, _danmakuPoolItem,
                danmakuPool == null ? "null" : string.Join("|", danmakuPool.OrderBy(x => x.Key)
                    .SelectMany(x => x.Value.Select(cue => x.Key + ":" + cue.text))),
                danmakuLoadedSegment == null ? "null" : string.Join(",", danmakuLoadedSegment),
                _danmakuLoadSettled, loadingDanmaku,
                (int)guard.GetField("generation", flags).GetValue(_danmakuLoadGuard),
                guard.GetField("owner", flags).GetValue(_danmakuLoadGuard),
                Publishes, Calls, Toasts, Rendered);
        }
    }
}

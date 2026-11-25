#pragma warning disable CS0067, CS0649, CS0414
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BiliLite.Services;
using BiliLite.Services.LocalMedia;
using System.Linq;
using Newtonsoft.Json;
namespace ResourceProbe
{
    enum Visibility
    {
        Visible, Collapsed
    }
    enum VideoPlayType
    {
        Video, Download
    }
    enum PlayState
    {
        End, Playing, Pause
    }
    class Element
    {
        public int Changes; object selectedItem; int selectedIndex; bool isOn; double value; Visibility visibility; string text; object flyout, dataContext, child; bool enabled = true;
        public object DataContext
        {
            get => dataContext; set
            {
                Changes++;
                dataContext = value;
            }
        }
        public string Text
        {
            get => text; set
            {
                Changes++;
                text = value;
            }
        }
        public object Flyout
        {
            get => flyout; set
            {
                Changes++;
                flyout = value;
            }
        }
        public object Child
        {
            get => child; set
            {
                Changes++;
                child = value;
            }
        }
        public Visibility Visibility
        {
            get => visibility; set
            {
                Changes++;
                visibility = value;
            }
        }
        public bool IsEnabled
        {
            get => enabled; set
            {
                Changes++;
                enabled = value;
            }
        }
        public object SelectedItem
        {
            get => selectedItem; set
            {
                if (!ReferenceEquals(selectedItem, value))
                {
                    selectedItem = value;
                    Changes++;
                    SelectionChanged?.Invoke(this, new SelectionChangedEventArgs());
                }
            }
        }
        public int SelectedIndex
        {
            get => selectedIndex; set
            {
                if (selectedIndex != value)
                {
                    selectedIndex = value;
                    Changes++;
                    SelectionChanged?.Invoke(this, new SelectionChangedEventArgs());
                }
            }
        }
        public bool IsOn
        {
            get => isOn; set
            {
                if (isOn != value)
                {
                    isOn = value;
                    Changes++;
                    Toggled?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public double Value
        {
            get => value; set
            {
                if (this.value != value)
                {
                    this.value = value;
                    Changes++;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public string title, Title; public object Tag, ItemsSource, Content; public double Opacity; public Thickness Margin;
        public event SelectionChangedEventHandler SelectionChanged; public event RoutedEventHandler Toggled; public event RangeBaseValueChangedEventHandler ValueChanged;
        public int LargestHandlerCount => Math.Max(Toggled?.GetInvocationList().Length ?? 0, Math.Max(ValueChanged?.GetInvocationList().Length ?? 0, SelectionChanged?.GetInvocationList().Length ?? 0));
        public int ToggleCount => Toggled?.GetInvocationList().Length ?? 0; public void Toggle() => Toggled?.Invoke(this, EventArgs.Empty);
    }

    class MenuFlyout
    {
        public List<ToggleMenuFlyoutItem> Items = new();
    }
    class ToggleMenuFlyoutItem
    {
        public string Text; public object Tag; public bool IsChecked; public event EventHandler<RoutedEventArgs> Click;
    }
    class LocalTrack
    {
        public string Label, Language, Id, Format, Path; public bool IsDefault, EnabledByDefault;
    }
    class LocalMediaPlaybackTrack
    {
        public LocalTrack Track = new(); public StorageFile File = new();
    }
    class LocalInfo
    {
        public string ResumeId = "resume", ManifestCollectionId = "collection", DanmakuPath; public LocalTrack ManifestItem = new(); public BiliPlayUrlInfo Info = new(); public LocalMediaPlaybackTrack SelectedAudioTrack, SelectedVideoTrack; public bool IsManifestPlayback; public List<LocalMediaPlaybackTrack> SubtitleFiles, DanmakuFiles; public Dictionary<string, string> Subtitles;
    }
    class PlayInfo
    {
        public string avid = "1", cid = "1", ep_id = "1", title = "Video"; public int season_id; public LocalInfo LocalPlayInfo; public VideoPlayType play_mode;
    }
    class SubtitleModel
    {
        public List<Cue> body = new() { new Cue() };
    }
    class Cue
    {
        public string content = "cue";
    }
    class PlayerInfo
    {
        public SubtitleInfo subtitle; public InteractionInfo interaction;
    }
    class SubtitleInfo
    {
        public List<SubtitleTrack> subtitles;
    }
    class SubtitleTrack
    {
        public string lan_doc = "English", lan = "en", subtitle_url = "fixture://subtitle";
    }
    class InteractionInfo
    {
        public string graph_version = "1";
    }
    class InteractionVideoVM
    {
        public Element Select = new(); public InteractionVideoVM(string a, string g)
        {
        }
        public static TaskCompletionSource<bool> NextGate; public Task GetNodes(int id = 0) => NextGate?.Task ?? Task.CompletedTask;
    }
    class PlayerVM
    {
        public int QualityCalls, InfoCalls, OnlineCalls, SubtitleCalls, DanmakuCalls; public TaskCompletionSource<string> Online; public TaskCompletionSource<List<DanmakuModel>> Danmaku; public Task<List<DanmakuModel>> GetDanmaku(string cid, int index)
        {
            DanmakuCalls++;
            return Danmaku?.Task ?? Task.FromResult(new List<DanmakuModel>());
        }
        public TaskCompletionSource<BiliPlayUrlQualitesInfo> Quality = new(); public Task<BiliPlayUrlQualitesInfo> GetPlayUrls(PlayInfo p, int q)
        {
            QualityCalls++;
            return Quality.Task;
        }
        public TaskCompletionSource<PlayerInfo> Info = new(); public TaskCompletionSource<SubtitleModel> Subtitle = new(); public Task<PlayerInfo> GetPlayInfo(string a, string c)
        {
            InfoCalls++;
            return Info.Task;
        }
        public Task<SubtitleModel> GetSubtitle(string u)
        {
            SubtitleCalls++;
            return Subtitle.Task;
        }
        public Task<string> GetOnline(string a, string c)
        {
            OnlineCalls++;
            return Online?.Task ?? Task.FromResult("1");
        }
        public Task ReportHistory(PlayInfo i, double p) => Task.CompletedTask;
    }
    class PlayerStub
    {
        public double Volume; public void SetRate(double d)
        {
        }
        public void SetRatioMode(int i)
        {
        }
        public ABEntry ABPlay; public int PlayCalls, CloseCalls; public void Play()
        {
            PlayCalls++;
        }
        public void ClosePlay()
        {
            CloseCalls++;
        }
        public string GetMediaInfo() => "fixture-media-info"; public void SetPosition(double p)
        {
            Position = p;
        }
        public event EventHandler PlayMediaOpened; public void SignalOpened() => PlayMediaOpened?.Invoke(this, EventArgs.Empty); public int OpenCalls; public TaskCompletionSource<PlayerOpenResult> OpenGate; public bool OpenSuccess = true; private async Task<PlayerOpenResult> Open(PlayerSessionTicket s)
        {
            OpenCalls++;
            var result = OpenGate == null ? new PlayerOpenResult { result = OpenSuccess } : await OpenGate.Task;
            if (result.result)
                s.Accept();
            return result;
        }
        public Task<PlayerOpenResult> PlayDashUseNative(DashInfo i, string u, string r, double position, bool isLocal, PlayerSessionTicket session) => Open(session); public Task<PlayerOpenResult> PlayDashUrlUseFFmpegInterop(string i, string u, string r, double positon, PlayerSessionTicket session) => Open(session); public Task<PlayerOpenResult> PlaySingleFlvUseSYEngine(string i, string u, string r, double positon, string epId = null, PlayerSessionTicket session = null) => Open(session); public Task<PlayerOpenResult> PlaySingleFlvUseFFmpegInterop(string i, string u, string r, double positon, PlayerSessionTicket session) => Open(session); public Task<PlayerOpenResult> PlayVideoUseSYEngine(List<FlvInfo> i, string u, string r, double positon, string epId, bool isLocal = false, PlayerSessionTicket session = null) => Open(session); public double Position = 42; public PlayState PlayState = PlayState.Playing; public event EventHandler PlayStateChanged, PlayMediaEnded, PlayMediaError; public event EventHandler<ChangePlayerEngine> ChangeEngine; public void Dispose()
        {
        }
        public Task<PlayerOpenResult> PlayDashUseFFmpegInterop(DashInfo i, string u, string r, double positon, bool isLocal = false, PlayerSessionTicket session = null) => Open(session); public Task<PlayerOpenResult> PlayerSingleMp4UseNativeAsync(string u, double positon, bool isLocal, PlayerSessionTicket session) => Open(session);
    }
    class ToastStub
    {
        public void Show(string k, string v)
        {
        }
        public void Dispose()
        {
        }
    }
    static class PlayerToastService
    {
        public const string Speed = "speed";
    }
    static class SettingHelper
    {
        public static readonly Dictionary<string, object> Values = new(); public static readonly Dictionary<string, int> Writes = new(); public static class VideoDanmaku
        {
            public const string HIDE_TOP = "hideTop", HIDE_BOTTOM = "hideBottom", HIDE_ROLL = "hideRoll", FONT_ZOOM = "zoom", AREA = "area", SPEED = "danmakuSpeed", TOP_MARGIN = "topMargin", OPACITY = "danmakuOpacity", MAX_NUM = "max", SHIELD_LEVEL = "shield", BOLD = "danmakuBold", BORDER_STYLE = "style", MERGE = "merge", SHOW = "show";
        }
        public static class Player
        {
            public const string AUTO_OPEN_AI_SUBTITLE = "ai", DEFAULT_QUALITY = "quality", AUTO_TO_POSITION = "position", PLAYER_VOLUME = "volume", DEFAULT_VIDEO_TYPE = "type", DEFAULT_PLAY_MODE = "mode", USE_OTHER_SITEVIDEO = "other", AUTO_NEXT = "next", RATIO = "ratio", DEFAULT_VIDEO_SPEED = "speed", AUTO_PLAY = "autoplay", SUBTITLE_BOLD = "bold", SUBTITLE_SIZE = "size", SUBTITLE_BORDER_COLOR = "border", SUBTITLE_COLOR = "color", SUBTITLE_ALIGN = "align", SUBTITLE_OPACITY = "opacity", SUBTITLE_BOTTOM = "bottom"; public static List<double> VideoSpeed = new() { 0.5, 1.0, 1.5 };
        }
        public static class Roaming
        {
            public const string TO_SIMPLIFIED = "simple";
        }
        public static T GetValue<T>(string key, T value) => Values.TryGetValue(key, out var stored) ? (T)stored : key == Player.AUTO_PLAY ? (T)(object)true : value; public static void SetValue<T>(string k, T v)
        {
            Values[k] = v;
            Writes[k] = Writes.GetValueOrDefault(k) + 1;
        }
    }
    enum LogType
    {
        INFO, ERROR
    }
    static class LogHelper
    {
        public static void Log(string m, LogType t)
        {
        }
    }
    static class Utils
    {
        public static string ToSimplifiedChinese(string s) => s; public static void ShowMessageToast(string s, string detail = null)
        {
            ControlSubject.Toasts++;
        }
    }
    // Explicit dispatcher-root model. No claim that this implements UWP or measures native memory.
    class DispatcherTimer
    {
        public static readonly HashSet<DispatcherTimer> Running = new(); public TimeSpan Interval; public event EventHandler Tick; public void Start() => Running.Add(this); public void Stop() => Running.Remove(this); public static void StopAll() => Running.Clear();
    }
    partial class ControlSubject
    {
        public readonly PlayerVM playerHelper = new();
        public PlayInfo CurrentPlayItem = new();
        private PlayInfo _interactionVideoItem;
        private InteractionVideoVM interactionVideoVM;
        private readonly HashSet<string> _enabledManifestDanmaku = new();
        private readonly LocalMediaAsyncLoadGuard _subtitleLoadGuard = new(), _danmakuLoadGuard = new();
        private readonly PlayerSessionState _playerSessions = new();
        private string CurrentSubtitleName;
        private int _subtitleTicket, _playItemGeneration;
        private readonly ToastStub playerToasts = new();
        private readonly PlayerStub Player = new();
        private DispatcherTimer subtitleTimer, danmuTimer = new(), _historyCheckpointTimer = new();
        private Dictionary<int, List<DanmakuModel>> danmakuPool = new();
        private PlayInfo _danmakuPoolItem;
        private DisplayRequest dispRequest = new();
        private bool _hasPlayedCurrentItem;
        private SubtitleModel subtitles;
        private string currentSubtitleText = "";
        private Element TopOnline = new(), TopTitle = new(), NodeList = new(), InteractionChoices = new(), BottomBtnSelctSubtitle = new(), LocalSubtitleTracksButton = new(), BorderSubtitle = new(), LocalDanmakuTracksButton = new();
        private Element SubtitleSettingToSimplified = new();
        private static readonly Regex AiSubtitleMarker = new(@"(?<![A-Za-z])ai(?![A-Za-z])", RegexOptions.IgnoreCase | RegexOptions.Compiled);


        private int surfaceChanges; private void ApplySurfaceVisibility()
        {
            surfaceChanges++;
        }
        private void SubtitleTimer_Tick(object s, EventArgs e)
        {
        }
        private void Player_PlayStateChanged(object s, EventArgs e)
        {
        }
        private void Player_PlayMediaEnded(object s, EventArgs e)
        {
        }
        private void Player_PlayMediaError(object s, EventArgs e)
        {
        }

        public Task StartInfo() => GetPlayerInfo();
        public void StartSubtitle() => SetSubTitle("fixture://subtitle");
    }
    class Resource
    {
        public int Disposes; public bool Throw; public object Source = new(); public void Dispose()
        {
            Disposes++;
            if (Throw)
                throw new Exception("injected-dispose");
        }
    }
    class MediaElement
    {
        public Resource MediaPlayer; public void SetMediaPlayer(Resource r) => MediaPlayer = r;
    }
    class PlaybackList
    {
        public List<object> Items = new() { new object() };
    }
    partial class PlayerSubject
    {
        public PlayState PlayState; public double Position, Duration;
        public readonly MediaElement mediaPlayerVideo = new(), mediaPlayerAudio = new();
        public Resource _ffmpegMSSVideo, _ffmpegMSSAudio, _playerVideo, _playerAudio;
        public PlaybackList _mediaPlaybackList;
        public object _mediaTimelineController;
        public List<Resource> _ffmpegMSSItems;
    }
}
namespace ResourceProbe
{
    enum BiliPlayUrlType
    {
        DASH, SingleFLV, MultiFLV
    }
    class FlvInfo
    {
        public string Url = "fixture://video";
    }
    class DashTrack
    {
        public int BandWidth, ID, FrameRate, Height, Width; public string Codecs, Url;
    }
    class DashInfo
    {
        public DashTrack Audio = new(), Video = new(); public double Duration;
    }
    class BiliPlayUrlInfo
    {
        public bool HasPlayUrl = true; public int QualityID = 80; public BiliPlayUrlType PlayUrlType = BiliPlayUrlType.SingleFLV; public string UserAgent, Referer; public DashInfo DashInfo = new(); public double Timelength; public List<FlvInfo> FlvInfo = new() { new() };
    }
    class BiliPlayUrlQualitesInfo
    {
        public bool Success = true; public BiliPlayUrlInfo CurrentQuality = new(); public object Qualites; public string Message;
    }
    class PlayerOpenResult
    {
        public bool result; public string message, detail_message;
    }
    class SelectionChangedEventArgs : EventArgs
    {
    }
    partial class ControlSubject
    {
        private bool _autoPlay; private double _position; private BiliPlayUrlInfo current_quality_info; private BiliPlayUrlQualitesInfo playUrlInfo; private Element VideoLoading = new(), BottomCBQuality = new();
        public int CurrentSessionId => _playerSessions.CurrentId;
        public int MediaOpenCalls => Player.OpenCalls;
        public Task StartQuality() => SetQuality();
        private void BottomCBQuality_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
        }

        public int Dialogs; private void ShowDialog(string a, string b)
        {
            Dialogs++;
        }
        private void ShowErrorDialog(string a)
        {
            Dialogs++;
        }
    }
}
namespace ResourceProbe.Models
{
    class GenerateMPDModel
    {
        public object AudioBandwidth, AudioCodec, AudioID, AudioUrl, Duration, DurationMS, VideoBandwidth, VideoCodec, VideoID, VideoFrameRate, VideoHeight, VideoWidth, VideoUrl;
    }
}
namespace ResourceProbe.Api
{
    class PlayerAPI
    {
        public string GenerateMPD(Models.GenerateMPDModel m) => "fixture://mpd";
    }
}
namespace ResourceProbe
{
    delegate void RoutedEventHandler(object s, EventArgs e);
    delegate void RangeBaseValueChangedEventHandler(object s, EventArgs e);
    delegate void SelectionChangedEventHandler(object s, SelectionChangedEventArgs e);
    class Thickness
    {
        public double Top; public Thickness(double a, double b, double c, double d)
        {
            Top = b;
        }
    }
    partial class ControlSubject
    {
        private Element SubtitleSettingBold = new(), SubtitleSettingSize = new(), SubtitleSettingBorderColor = new(), SubtitleSettingColor = new(), SubtitleSettingAlign = new(), SubtitleSettingOpacity = new(), SubtitleSettingBottom = new();
        private bool miniWin; public int Updates; private void UpdateSubtitle() => Updates++;
        public void LoadSubtitleSettings() => LoadSutitleSetting();
        public int BoldHandlers => SubtitleSettingBold.ToggleCount;
        public void ToggleBold() => SubtitleSettingBold.Toggle();
    }
}
namespace Windows.UI.Core
{
    enum CoreCursorType
    {
        Arrow
    }
    class CoreCursor
    {
        public CoreCursor(CoreCursorType t, int id)
        {
        }
    }
}
namespace ResourceProbe
{
    class RoutedEventArgs : EventArgs
    {
    }
    class DisplayRequest
    {
        public int ActiveCalls; public void RequestActive()
        {
            ActiveCalls++;
        }
    }
    enum DanmakuLocation
    {
        Top, Bottom, Scroll
    }
    enum DanmakuBorderStyle
    {
        None
    }
    class DanmakuStub
    {
        public double DanmakuSizeZoom, DanmakuArea, Opacity; public int DanmakuDuration; public Thickness Margin; public bool DanmakuBold; public DanmakuBorderStyle DanmakuStyle; public Visibility Visibility; public int Hides, Shows; public void HideDanmaku(DanmakuLocation l) => Hides++; public void ShowDanmaku(DanmakuLocation l) => Shows++; public int Clears; public void ClearAll() => Clears++;
    }
    enum FocusState
    {
        Programmatic
    }
    class FocusElement
    {
        public void Focus(FocusState s)
        {
        }
    }
    class CoreWindow
    {
        public object PointerCursor; public event EventHandler KeyDown;
    }
    class Window
    {
        public static Window Current = new(); public CoreWindow CoreWindow = new();
    }
    enum MediaPlaybackType
    {
        Video
    }
    class SystemMediaTransportControlsDisplayUpdater
    {
        public MediaPlaybackType Type; public Element VideoProperties = new(); public void Update()
        {
        }
        public void ClearAll()
        {
        }
    }
    class SystemMediaTransportControls
    {
        public static SystemMediaTransportControls Shared = new(); public static SystemMediaTransportControls GetForCurrentView() => Shared; public bool IsEnabled, IsPlayEnabled, IsPauseEnabled; public event EventHandler ButtonPressed; public SystemMediaTransportControlsDisplayUpdater DisplayUpdater = new();
    }
    enum PlayTargetStart
    {
        Resume, FromStart
    }
    static class PlayContinuationPolicy
    {
        public static bool RestoresPosition(PlayTargetStart s, bool enabled) => s == PlayTargetStart.Resume && enabled;
    }
    class ABEntry
    {
        public double PointA, PointB;
    }
    static class VideoPlayHistoryHelper
    {
        public static ABEntry FindABPlayHistory(PlayInfo p) => null; public static void SetABPlayHistory(PlayInfo p, ABEntry e)
        {
        }
    }
    partial class ControlSubject
    {
        private static ControlSubject _activeInstance;
        private DanmakuStub DanmuControl = new();
        private FocusElement BtnFoucs = new();
        private SystemMediaTransportControls _systemMediaTransportControls;
        private DispatcherTimer timer_focus = new();
        private bool _danmakuLoadSettled, _restoreLocalTrackPositionOnOpen;
        private Element PlayerSettingABPlayMode = new(), PlayerSettingABPlaySetPointA = new(), PlayerSettingABPlaySetPointB = new(), txtInfo = new();
        private List<PlayInfo> PlayInfos;
        private int CurrentPlayIndex;
        public event EventHandler<int> ChangeEpisodeEvent;
        public ControlSubject()
        {
            Player.PlayStateChanged += Player_PlayStateChanged;
            Player.PlayMediaEnded += Player_PlayMediaEnded;
            Player.PlayMediaError += Player_PlayMediaError;
            Player.ChangeEngine += Player_ChangeEngine;
            Player.PlayMediaOpened += Player_PlayMediaOpened;
        }
        private void ReleaseAcceleratingOverlay()
        {
        }
        private void UpdateLocalTrackSelectors()
        {
        }
        private void UpdateBottomBtnNextVisibility()
        {
        }

        private void PlayerControl_KeyDown(object s, EventArgs e)
        {
        }
        private void _systemMediaTransportControls_ButtonPressed(object s, EventArgs e)
        {
        }
        private Element SliderVolume = new(), PlayerSettingMode = new(), PlayerSettingPlayMode = new(), PlayerSettingUseOtherSite = new(), PlayerSettingAutoToPosition = new(), PlayerSettingAutoNext = new(), PlayerSettingRatio = new(), BottomCBSpeed = new();

        public void Load() => PlayerControl_Loaded(this, new RoutedEventArgs());
        public void Unload() => PlayerControl_Unloaded(this, new RoutedEventArgs());
        public void SignalOpened() => Player.SignalOpened();
        public int PlayCalls => Player.PlayCalls;
        public int RunningSubtitle => subtitleTimer != null && DispatcherTimer.Running.Contains(subtitleTimer) ? 1 : 0;
        public int DanmakuTimerLive => danmuTimer != null && DispatcherTimer.Running.Contains(danmuTimer) ? 1 : 0;
        public int DisplayRequests => dispRequest?.ActiveCalls ?? 0;
    }
}

namespace Windows.Storage.Pickers
{
    class FileOpenPicker
    {
        public static System.Threading.Tasks.TaskCompletionSource<ResourceProbe.StorageFile> Gate; public System.Collections.Generic.List<string> FileTypeFilter = new(); public System.Threading.Tasks.Task<ResourceProbe.StorageFile> PickSingleFileAsync() => Gate.Task;
    }
}
namespace ResourceProbe
{
    class StorageFile
    {
        public string Path = "fixture://file"; public static Task<StorageFile> GetFileFromPathAsync(string path) => Task.FromResult(new StorageFile());
    }
    static class FileIO
    {
        public static TaskCompletionSource<string> Gate; public static Task<string> ReadTextAsync(StorageFile f) => Gate?.Task ?? Task.FromResult("{}");
    }
    static class SubtitleTrackParser
    {
        public static SubtitleModel Parse(string t, string f) => new();
    }
    class DanmakuModel
    {
        public int time_s;
    }
    class DanmakuParser
    {
        public Task<List<DanmakuModel>> ParseBiliBili(StorageFile f) => Task.FromResult(new List<DanmakuModel>()); public List<DanmakuModel> ParseBiliBili(string s) => new();
    }
    static class DanmakuTrackLoader
    {
        public static Task<List<DanmakuModel>> LoadAsync(List<LocalMediaPlaybackTrack> tracks, Func<StorageFile, Task<List<DanmakuModel>>> read) => Task.FromResult(new List<DanmakuModel>());
    }
    class LocalMediaTrackLoadException : Exception
    {
        public string Stage, TrackId;
    }
    static class IntExtensions
    {
        public static int ToInt32(this double d) => (int)d;
    }
    class LocalMediaTrackOpenResult
    {
        public bool Success; public string Message, Detail;
    }
    class LocalMediaTrackSwitchOutcome
    {
        public bool Changed, Success; public LocalMediaTrackOpenResult OpenResult, RestoreResult; public Exception OpenException, RestoreException;
    }
    static class LocalMediaTrackSelection
    {
        public static LocalMediaPlaybackTrack GetVideo(LocalInfo i) => i.SelectedVideoTrack; public static LocalMediaPlaybackTrack GetAudio(LocalInfo i) => i.SelectedAudioTrack; public static void SelectAudio(LocalInfo i, LocalMediaPlaybackTrack t) => i.SelectedAudioTrack = t; public static void SelectVideo(LocalInfo i, LocalMediaPlaybackTrack t) => i.SelectedVideoTrack = t; public static void Restore(LocalInfo i, LocalMediaPlaybackTrack v, LocalMediaPlaybackTrack a)
        {
            i.SelectedVideoTrack = v;
            i.SelectedAudioTrack = a;
        }
    }
    partial class ControlSubject
    {
        public static int Toasts;
        private bool _transitionPending;
        private bool _localTrackSwitching, loadingDanmaku; private List<int> danmakuLoadedSegment; private DanmakuParser danmakuParse = new();
        private Element LocalVideoTrackSelector = new(), LocalAudioTrackSelector = new(), BottomBtnNext = new(), TxtDanmuCount = new();
        private Element DanmuSettingHideTop = new(), DanmuSettingHideBottom = new(), DanmuSettingHideRoll = new(), DanmuSettingFontZoom = new(), DanmuSettingArea = new(), DanmuSettingSpeed = new(), DanmuTopMargin = new(), DanmuSettingOpacity = new(), DanmuSettingMaxNum = new(), DanmuSettingShieldLevel = new(), DanmuSettingBold = new(), DanmuSettingStyle = new(), DanmuSettingMerge = new(), DanmuSettingWords = new();
        private SettingVM settingVM = new();
        class SettingVM
        {
            public object ShieldWords;
        }
    }
}

namespace ResourceProbe
{
    partial class ControlSubject
    {
        public int Calls => playerHelper.InfoCalls + playerHelper.QualityCalls + playerHelper.OnlineCalls + playerHelper.SubtitleCalls + playerHelper.DanmakuCalls + Player.OpenCalls + Player.PlayCalls + DisplayRequests;
        public int Publishes => surfaceChanges + TopOnline.Changes + TopTitle.Changes + NodeList.Changes + InteractionChoices.Changes + BottomBtnSelctSubtitle.Changes + BorderSubtitle.Changes + LocalSubtitleTracksButton.Changes + LocalDanmakuTracksButton.Changes + VideoLoading.Changes + BottomCBQuality.Changes + LocalVideoTrackSelector.Changes + LocalAudioTrackSelector.Changes + BottomBtnNext.Changes + TxtDanmuCount.Changes + DanmuControl.Clears;
        public void SetInteraction()
        {
            interactionVideoVM = new InteractionVideoVM("1", "1");
            _interactionVideoItem = CurrentPlayItem;
        }
        public object InteractionOwner => interactionVideoVM;
        public bool InteractionAttached(object owner) => ReferenceEquals(NodeList.DataContext, owner) || ReferenceEquals(InteractionChoices.DataContext, owner);
        public void ClickFirstSubtitle() => Menuitem_Click(((MenuFlyout)BottomBtnSelctSubtitle.Flyout).Items[0], new RoutedEventArgs());
        public Task StartNode() => ChangedNode(2, "2002");
        public void StartPicker(bool subtitle)
        {
            if (subtitle)
                ImportSubtitle_Click(this, new RoutedEventArgs());
            else
                ImportDanmaku_Click(this, new RoutedEventArgs());
        }
        public Task StartTrack(LocalMediaPlaybackTrack track = null) => SwitchLocalTrackAsync(track ?? new LocalMediaPlaybackTrack(), true);
        TaskCompletionSource<PlayerOpenResult> savedOpen;
        public void SetOpenGate()
        {
            savedOpen = new TaskCompletionSource<PlayerOpenResult>();
            Player.OpenGate = savedOpen;
        }
        public void ReleaseNewOpenGate() => Player.OpenGate = null;
        public void CompleteOpen(bool success) => savedOpen.SetResult(new PlayerOpenResult { result = success }); public void FaultOpen() => savedOpen.SetException(new InvalidOperationException("open-fault"));
        public void SetEngineResults(bool success) => Player.OpenSuccess = success;
        public bool SettingsRefreshed(int i) => Player.Volume == i / 100.0 && SubtitleSettingSize.Value == 30.0 + i && DanmuControl.DanmakuSizeZoom == 1.0 + i;
        public void ChangeVolume() => SliderVolume.Value = 0.75;
        public bool EverySettingsHandlerSingle => GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Where(f => f.FieldType == typeof(Element)).Select(f => (Element)f.GetValue(this)).All(e => e.LargestHandlerCount <= 1);
        public Task UpdateDanmaku() => SetDanmaku(true);
        public void ChangeDanmaku() => DanmuSettingMerge.IsOn = !DanmuSettingMerge.IsOn;
    }
}

namespace ResourceProbe
{
    enum PlayMediaType
    {
        Dash, Single
    }
    enum PlayEngine
    {
        FFmpegInteropMSS, SYEngine
    }
    class ChangePlayerEngine : EventArgs
    {
        public bool need_change = true; public string message; public PlayMediaType play_type = PlayMediaType.Dash; public PlayEngine change_engine = PlayEngine.FFmpegInteropMSS;
    }
    partial class ControlSubject
    {
        private bool _engineFallbackRunning;
        public Task StartEngine()
        {
            current_quality_info = new BiliPlayUrlInfo();
            return ChangeEngineAsync(new ChangePlayerEngine());
        }
        public void SignalEngine(bool need)
        {
            current_quality_info = new BiliPlayUrlInfo();
            Player_ChangeEngine(this, new ChangePlayerEngine { need_change = need });
        }
    }
}

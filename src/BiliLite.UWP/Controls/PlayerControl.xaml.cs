using BiliLite.Dialogs;
using BiliLite.Helpers;
using BiliLite.Modules;
using BiliLite.Modules.Player;
using BiliLite.Modules.Player.Playurl;
using BiliLite.Models.Common;
using BiliLite.Models.LocalMedia;
using BiliLite.Services.LocalMedia;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Newtonsoft.Json;
using NSDanmaku.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace BiliLite.Controls
{
    public enum VideoPlayType
    {
        /// <summary>
        /// 视频
        /// </summary>
        Video,

        /// <summary>
        /// 番剧、影视
        /// </summary>
        Season,

        /// <summary>
        /// 下载的视频视频
        /// </summary>
        Download,

        /// <summary>
        /// 本地视频
        /// </summary>
        LoaclFile
    }

    public class PlayInfo
    {
        /// <summary>
        /// 播放模式
        /// </summary>
        public VideoPlayType play_mode { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int order { get; set; }

        /// <summary>
        /// 专题ID
        /// </summary>
        public long season_id { get; set; }

        /// <summary>
        /// 专题类型
        /// </summary>
        public int season_type { get; set; }

        /// <summary>
        /// 专题分集ID
        /// </summary>
        public string ep_id { get; set; }

        /// <summary>
        /// 视频ID
        /// </summary>
        public string avid { get; set; }

        /// <summary>
        /// 必须，视频分集ID
        /// </summary>
        public string cid { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string title { get; set; }

        /// <summary>
        /// 是否VIP
        /// </summary>
        public bool is_vip { get; set; }

        /// <summary>
        /// 是否互动视频
        /// </summary>
        public bool is_interaction { get; set; } = false;

        /// <summary>
        /// 互动视频分支ID
        /// </summary>
        public int node_id { get; set; } = 0;

        /// <summary>
        /// 时长（毫秒）
        /// </summary>
        public int duration { get; set; }

        public LocalPlayInfo LocalPlayInfo { get; set; }
        public object parameter { get; set; }

        public string area { get; set; } = "";
    }

    public class LocalPlayInfo
    {
        public BiliPlayUrlInfo Info { get; set; }
        public IDictionary<string, string> Subtitles { get; set; }
        public string DanmakuPath { get; set; }
        public string Quality { get; set; }

        public LocalMediaItem ManifestItem { get; set; }
        public IList<LocalMediaPlaybackTrack> VideoTracks { get; set; }
        public IList<LocalMediaPlaybackTrack> AudioTracks { get; set; }
        public LocalMediaPlaybackTrack SelectedVideoTrack { get; set; }
        public LocalMediaPlaybackTrack SelectedAudioTrack { get; set; }
        public IList<LocalMediaTrack> SubtitleTracks { get; set; }
        public IList<LocalMediaTrack> DanmakuTracks { get; set; }
        public IList<LocalMediaPlaybackTrack> SubtitleFiles { get; set; }
        public IList<LocalMediaPlaybackTrack> DanmakuFiles { get; set; }
        public string ManifestCollectionId { get; set; }
        public string ResumeId { get; set; }

        public bool IsManifestPlayback => ManifestItem != null;
    }

    public sealed partial class PlayerControl : UserControl, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void DoPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private InteractionVideoVM interactionVideoVM;

        /// <summary>
        /// 铺满窗口事件
        /// </summary>
        public event EventHandler<bool> FullWindowEvent;

        /// <summary>
        /// 全屏事件
        /// </summary>
        public event EventHandler<bool> FullScreenEvent;

        /// <summary>
        /// 全部播放完毕
        /// </summary>
        public event EventHandler AllMediaEndEvent;

        /// <summary>
        /// 切换剧集事件
        /// </summary>

        public event EventHandler<int> ChangeEpisodeEvent;

        /// <summary>
        /// 播放列表
        /// </summary>
        public List<PlayInfo> PlayInfos { get; set; }

        /// <summary>
        /// 当前播放
        /// </summary>
        public int CurrentPlayIndex { get; set; }

        /// <summary>
        /// 当前播放
        /// </summary>
        public PlayInfo CurrentPlayItem { get; set; }

        private readonly PlayerVM playerHelper;
        private readonly NSDanmaku.Helper.DanmakuParse danmakuParse;
        private BiliPlayUrlQualitesInfo _playUrlInfo;

        /// <summary>
        /// 播放地址信息
        /// </summary>
        public BiliPlayUrlQualitesInfo playUrlInfo
        {
            get { return _playUrlInfo; }
            set { _playUrlInfo = value; DoPropertyChanged("playUrlInfo"); }
        }

        private DispatcherTimer danmuTimer;

        /// <summary>
        /// 弹幕信息
        /// </summary>
        private IDictionary<int, List<NSDanmaku.Model.DanmakuModel>> danmakuPool = new Dictionary<int, List<NSDanmaku.Model.DanmakuModel>>();

        private List<int> danmakuLoadedSegment;
        private SettingVM settingVM;
        private DisplayRequest dispRequest;
        private SystemMediaTransportControls _systemMediaTransportControls;
        private DispatcherTimer timer_focus;

        public Player PlayerInstance
        { get { return Player; } }

        /// <summary>
        /// 当前选中的字幕名称
        /// </summary>
        private string CurrentSubtitleName { get; set; } = "无";
        private int _playItemGeneration;
        private int _subtitleTicket;
        private readonly LocalMediaAsyncLoadGuard _subtitleLoadGuard = new LocalMediaAsyncLoadGuard();
        private readonly LocalMediaAsyncLoadGuard _danmakuLoadGuard = new LocalMediaAsyncLoadGuard();
        private readonly HashSet<string> _enabledManifestDanmaku = new HashSet<string>(StringComparer.Ordinal);

        private DisplayInformation displayInformation = DisplayInformation.GetForCurrentView();

        public PlayerControl()
        {
            this.InitializeComponent();
            dispRequest = new DisplayRequest();
            playerHelper = new PlayerVM();
            settingVM = new SettingVM();

            danmakuParse = new NSDanmaku.Helper.DanmakuParse();
            DanmakuListButton.Owner = this;
            //每过2秒就设置焦点
            timer_focus = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(2) };
            timer_focus.Tick += Timer_focus_Tick;
            danmuTimer = new DispatcherTimer();
            danmuTimer.Interval = TimeSpan.FromSeconds(1);
            danmuTimer.Tick += DanmuTimer_Tick;
            this.Loaded += PlayerControl_Loaded;
            this.Unloaded += PlayerControl_Unloaded;
            gestureRecognizer = new GestureRecognizer();
            InitializeGesture();
        }

        private void Timer_focus_Tick(object sender, object e)
        {
            var elent = FocusManager.GetFocusedElement();
            if (elent is Button || elent is AppBarButton || elent is HyperlinkButton || elent is MenuFlyoutItem)
            {
                BtnFoucs.Focus(FocusState.Programmatic);
            }
        }

        private bool runing = false;
        private bool pointer_in_player = false;

        private async void ShowControl(bool show)
        {
            if (runing) return;
            runing = true;
            if (show)
            {
                BottomImageBtnPlay.Margin = new Thickness(24, 24, 24, 100);
                showControlsFlag = 0;
                control.Visibility = Visibility.Visible;
                await control.FadeInAsync(400);
            }
            else
            {
                if (pointer_in_player)
                {
                    Window.Current.CoreWindow.PointerCursor = null;
                }
                BottomImageBtnPlay.Margin = new Thickness(24, 24, 24, 24);
                await control.FadeOutAsync(400);
                control.Visibility = Visibility.Collapsed;
            }
            runing = false;
        }

        private void PlayerControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
            Window.Current.CoreWindow.KeyDown -= PlayerControl_KeyDown;
            if (_systemMediaTransportControls != null)
            {
                _systemMediaTransportControls.ButtonPressed -= _systemMediaTransportControls_ButtonPressed;
                _systemMediaTransportControls.DisplayUpdater.ClearAll();
                _systemMediaTransportControls.IsEnabled = false;
                _systemMediaTransportControls = null;
            }
            timer_focus.Stop();
        }

        private void PlayerControl_Loaded(object sender, RoutedEventArgs e)
        {
            DanmuControl.ClearAll();
            Window.Current.CoreWindow.KeyDown += PlayerControl_KeyDown;
            BtnFoucs.Focus(FocusState.Programmatic);
            _systemMediaTransportControls = SystemMediaTransportControls.GetForCurrentView();
            _systemMediaTransportControls.IsEnabled = true;
            _systemMediaTransportControls.IsPlayEnabled = true;
            _systemMediaTransportControls.IsPauseEnabled = true;
            if (CurrentPlayItem != null)
            {
                SystemMediaTransportControlsDisplayUpdater updater = _systemMediaTransportControls.DisplayUpdater;
                updater.Type = MediaPlaybackType.Video;
                updater.VideoProperties.Title = CurrentPlayItem.title;
                updater.Update();
            }
            _systemMediaTransportControls.ButtonPressed -= _systemMediaTransportControls_ButtonPressed;
            _systemMediaTransportControls.ButtonPressed += _systemMediaTransportControls_ButtonPressed;

            LoadPlayerSetting();
            LoadDanmuSetting();
            LoadSutitleSetting();

            danmuTimer.Start();
            timer_focus.Start();
        }

        private async void _systemMediaTransportControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        Player.Play();
                    });
                    break;

                case SystemMediaTransportControlsButton.Pause:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        Pause();
                    });
                    break;

                default:
                    break;
            }
        }

        //判断是否按下mod键
        private bool IsDown(Windows.System.VirtualKey key)
        {
            return Window.Current.CoreWindow.GetKeyState(key).HasFlag(CoreVirtualKeyStates.Down);
        }

        private bool NoMod()
        {
            return !IsDown(Windows.System.VirtualKey.Shift) &&
                !IsDown(Windows.System.VirtualKey.Control) &&
                !IsDown(Windows.System.VirtualKey.Menu) &&
                !IsDown(Windows.System.VirtualKey.LeftWindows) &&
                !IsDown(Windows.System.VirtualKey.RightWindows);
        }

        // 计算步长
        private double GetStep()
        {
            double step = 0.0;
            if (IsDown(Windows.System.VirtualKey.Control))
            {
                step = 0.5;
            }
            else if (IsDown(Windows.System.VirtualKey.Shift))
            {
                step = 30.0;
            }
            else if (NoMod())
            {
                step = 3.0;
            }
            return step;
        }

        private async void PlayerControl_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            var ele = FocusManager.GetFocusedElement();
            if (ele is TextBox || ele is AutoSuggestBox)
            {
                args.Handled = false;
                return;
            }
            args.Handled = true;
            /*
             * 按键       操作
             * space     播放/暂停
             * left/right进度，ctrl=0.5,none=3,shift=30
             * up/down   音量
             * esc       退出全屏
             * f8/t      小窗
             * f12/w/f11/f/enter     全屏
             * f10       录制
             * o/p  跳过op=90秒
             * f9/d      开关弹幕
             * z/n       上一p
             * x/m       下一p
             * f1/f2     速度
             * f3/v      静音/100音量
             * f4/ctrl+s        下载
             * f5        刷新
             * f7        [系统使用]
             */
            switch (args.VirtualKey)
            {
                case Windows.System.VirtualKey.Space:
                    if (NoMod())
                    {
                        if (Player.PlayState == PlayState.Playing || Player.PlayState == PlayState.End)
                        {
                            Pause();
                        }
                        else
                        {
                            Player.Play();
                        }
                    }
                    break;

                case Windows.System.VirtualKey.Left:
                case Windows.System.VirtualKey.Right:
                    {
                        if (Player.PlayState == PlayState.Playing || Player.PlayState == PlayState.Pause)
                        {
                            var _position = Player.Position;
                            var step = GetStep();
                            if (step != 0)
                            {
                                step = args.VirtualKey == Windows.System.VirtualKey.Left ? -step : step;
                                _position += step;
                                if (_position < 0)
                                {
                                    _position = 0;
                                }
                                Player.Position = _position;
                            }
                            if (step >= 1)
                            {
                                TxtToolTip.Text = "进度:" + TimeSpan.FromSeconds(Player.Position).ToString(@"hh\:mm\:ss");
                                ToolTip.Visibility = Visibility.Visible;
                                await Task.Delay(2000);
                                ToolTip.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                    break;

                case Windows.System.VirtualKey.Up:
                case Windows.System.VirtualKey.Down:
                    if (NoMod())
                    {
                        Player.Volume += args.VirtualKey == Windows.System.VirtualKey.Up ? 0.1 : -0.1;
                        if (Player.Volume == 0)
                        {
                            TxtToolTip.Text = "静音";
                        }
                        else
                        {
                            TxtToolTip.Text = "音量:" + Player.Volume.ToString("P");
                        }
                        ToolTip.Visibility = Visibility.Visible;
                        await Task.Delay(2000);
                        ToolTip.Visibility = Visibility.Collapsed;
                    }
                    break;

                case Windows.System.VirtualKey.Escape:
                    IsFullScreen = false;
                    break;

                case Windows.System.VirtualKey.F8:
                case Windows.System.VirtualKey.T:
                    //小窗播放
                    if (NoMod())
                    { MiniWidnows(!miniWin); }
                    break;

                case Windows.System.VirtualKey.F12:
                case Windows.System.VirtualKey.W:
                case Windows.System.VirtualKey.F11:
                case Windows.System.VirtualKey.F:
                case Windows.System.VirtualKey.Enter:
                    if (NoMod())
                    {
                        IsFullScreen = !IsFullScreen;
                    }
                    break;

                case Windows.System.VirtualKey.F10:
                    if (NoMod())
                        await CaptureVideo();
                    break;

                case Windows.System.VirtualKey.O:
                case Windows.System.VirtualKey.P:
                    if (NoMod())
                    {
                        if (Player.PlayState == PlayState.Playing || Player.PlayState == PlayState.Pause)
                        {
                            var _position = Player.Position + 90;
                            if (_position > Player.Duration)
                            {
                                _position = Player.Duration;
                            }
                            Player.Position = _position;
                            TxtToolTip.Text = "跳过OP(快进90秒)";
                            ToolTip.Visibility = Visibility.Visible;
                            await Task.Delay(2000);
                            ToolTip.Visibility = Visibility.Collapsed;
                        }
                    }
                    break;

                case Windows.System.VirtualKey.F9:
                case Windows.System.VirtualKey.D:
                    if (NoMod())
                    {
                        if (DanmuControl.Visibility == Visibility.Visible)
                        {
                            DanmuControl.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            DanmuControl.Visibility = Visibility.Visible;
                        }
                    }
                    break;

                case Windows.System.VirtualKey.Z:
                case Windows.System.VirtualKey.N:
                case (Windows.System.VirtualKey)188:
                    if (NoMod())
                    {
                        if (EpisodeList.SelectedIndex == 0)
                        {
                            Utils.ShowMessageToast("已经是第一P了");
                        }
                        else
                        {
                            await SetPlayItem(EpisodeList.SelectedIndex - 1);
                        }
                    }
                    break;

                case Windows.System.VirtualKey.X:
                case Windows.System.VirtualKey.M:
                case (Windows.System.VirtualKey)190:
                    if (NoMod())
                    {
                        if (EpisodeList.SelectedIndex == EpisodeList.Items.Count - 1)
                        {
                            Utils.ShowMessageToast("已经是最后一P了");
                        }
                        else
                        {
                            await SetPlayItem(EpisodeList.SelectedIndex + 1);
                        }
                    }
                    break;

                case Windows.System.VirtualKey.F1:
                case (Windows.System.VirtualKey)186:
                    if (NoMod())
                    {
                        //慢速播放
                        if (BottomCBSpeed.SelectedIndex == BottomCBSpeed.Items.Count)
                        {
                            Utils.ShowMessageToast("不能再慢啦");
                            return;
                        }

                        BottomCBSpeed.SelectedIndex += 1;
                    }
                    break;

                case Windows.System.VirtualKey.F2:
                case (Windows.System.VirtualKey)222:
                    if (NoMod())
                    {
                        //加速播放
                        if (BottomCBSpeed.SelectedIndex == 0)
                        {
                            Utils.ShowMessageToast("不能再快啦");
                            return;
                        }
                        BottomCBSpeed.SelectedIndex -= 1;
                    }
                    break;

                case Windows.System.VirtualKey.F3:
                case Windows.System.VirtualKey.V:
                    if (NoMod())
                    {
                        //静音
                        if (Player.Volume >= 0)
                        {
                            Player.Volume = 0;
                        }
                        else
                        {
                            Player.Volume = 1;
                        }
                    }
                    break;

                case Windows.System.VirtualKey.F4:
                    break;

                default:
                    break;
            }
        }

        private void LoadDanmuSetting()
        {
            //顶部
            DanmuSettingHideTop.IsOn = SettingHelper.GetValue<bool>(SettingHelper.VideoDanmaku.HIDE_TOP, false);
            if (DanmuSettingHideTop.IsOn)
            {
                DanmuControl.HideDanmaku(DanmakuLocation.Top);
            }
            DanmuSettingHideTop.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<bool>(SettingHelper.VideoDanmaku.HIDE_TOP, DanmuSettingHideTop.IsOn);
                if (DanmuSettingHideTop.IsOn)
                {
                    DanmuControl.HideDanmaku(DanmakuLocation.Top);
                }
                else
                {
                    DanmuControl.ShowDanmaku(DanmakuLocation.Top);
                }
            });
            //底部
            DanmuSettingHideBottom.IsOn = SettingHelper.GetValue<bool>(SettingHelper.VideoDanmaku.HIDE_BOTTOM, false);
            if (DanmuSettingHideBottom.IsOn)
            {
                DanmuControl.HideDanmaku(DanmakuLocation.Bottom);
            }
            DanmuSettingHideBottom.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<bool>(SettingHelper.VideoDanmaku.HIDE_BOTTOM, DanmuSettingHideBottom.IsOn);
                if (DanmuSettingHideBottom.IsOn)
                {
                    DanmuControl.HideDanmaku(DanmakuLocation.Bottom);
                }
                else
                {
                    DanmuControl.ShowDanmaku(DanmakuLocation.Bottom);
                }
            });
            //滚动
            DanmuSettingHideRoll.IsOn = SettingHelper.GetValue<bool>(SettingHelper.VideoDanmaku.HIDE_ROLL, false);
            if (DanmuSettingHideRoll.IsOn)
            {
                DanmuControl.HideDanmaku(DanmakuLocation.Scroll);
            }
            DanmuSettingHideRoll.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<bool>(SettingHelper.VideoDanmaku.HIDE_ROLL, DanmuSettingHideRoll.IsOn);
                if (DanmuSettingHideRoll.IsOn)
                {
                    DanmuControl.HideDanmaku(DanmakuLocation.Scroll);
                }
                else
                {
                    DanmuControl.ShowDanmaku(DanmakuLocation.Scroll);
                }
            });
            //弹幕大小
            DanmuControl.DanmakuSizeZoom = SettingHelper.GetValue<double>(SettingHelper.VideoDanmaku.FONT_ZOOM, 1);
            DanmuSettingFontZoom.ValueChanged += new RangeBaseValueChangedEventHandler((e, args) =>
            {
                if (miniWin) return;
                SettingHelper.SetValue<double>(SettingHelper.VideoDanmaku.FONT_ZOOM, DanmuSettingFontZoom.Value);
            });
            //弹幕显示区域
            DanmuControl.DanmakuArea = SettingHelper.GetValue<double>(SettingHelper.VideoDanmaku.AREA, 1);
            DanmuSettingArea.ValueChanged += new RangeBaseValueChangedEventHandler((e, args) =>
            {
                if (miniWin) return;
                SettingHelper.SetValue<double>(SettingHelper.VideoDanmaku.AREA, DanmuSettingArea.Value);
            });

            //弹幕速度
            DanmuControl.DanmakuDuration = SettingHelper.GetValue<int>(SettingHelper.VideoDanmaku.SPEED, 10);
            DanmuSettingSpeed.ValueChanged += new RangeBaseValueChangedEventHandler((e, args) =>
            {
                if (miniWin) return;
                SettingHelper.SetValue<double>(SettingHelper.VideoDanmaku.SPEED, DanmuSettingSpeed.Value);
            });
            //弹幕顶部距离
            DanmuControl.Margin = new Thickness(0, SettingHelper.GetValue<int>(SettingHelper.VideoDanmaku.TOP_MARGIN, 0), 0, 0);
            DanmuTopMargin.Value = DanmuControl.Margin.Top;
            DanmuTopMargin.ValueChanged += new RangeBaseValueChangedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<double>(SettingHelper.VideoDanmaku.TOP_MARGIN, DanmuTopMargin.Value);
                DanmuControl.Margin = new Thickness(0, DanmuTopMargin.Value, 0, 0);
            });
            //弹幕透明度
            DanmuControl.Opacity = SettingHelper.GetValue<double>(SettingHelper.VideoDanmaku.OPACITY, 1.0);
            DanmuSettingOpacity.ValueChanged += new RangeBaseValueChangedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<double>(SettingHelper.VideoDanmaku.OPACITY, DanmuSettingOpacity.Value);
            });
            //弹幕最大值
            DanmuSettingMaxNum.Value = SettingHelper.GetValue<double>(SettingHelper.VideoDanmaku.MAX_NUM, 0);
            DanmuSettingMaxNum.ValueChanged += new RangeBaseValueChangedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<double>(SettingHelper.VideoDanmaku.MAX_NUM, DanmuSettingMaxNum.Value);
            });

            //弹幕云屏蔽等级
            DanmuSettingShieldLevel.Value = SettingHelper.GetValue<int>(SettingHelper.VideoDanmaku.SHIELD_LEVEL, 0);
            DanmuSettingShieldLevel.ValueChanged += new RangeBaseValueChangedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<int>(SettingHelper.VideoDanmaku.SHIELD_LEVEL, Convert.ToInt32(DanmuSettingShieldLevel.Value));
            });

            //弹幕加粗
            DanmuControl.DanmakuBold = SettingHelper.GetValue<bool>(SettingHelper.VideoDanmaku.BOLD, false);
            DanmuSettingBold.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<bool>(SettingHelper.VideoDanmaku.BOLD, DanmuSettingBold.IsOn);
            });
            //弹幕样式
            DanmuControl.DanmakuStyle = (DanmakuBorderStyle)SettingHelper.GetValue<int>(SettingHelper.VideoDanmaku.BORDER_STYLE, 2);
            DanmuSettingStyle.SelectionChanged += new SelectionChangedEventHandler((e, args) =>
            {
                if (DanmuSettingStyle.SelectedIndex != -1)
                {
                    SettingHelper.SetValue<int>(SettingHelper.VideoDanmaku.BORDER_STYLE, DanmuSettingStyle.SelectedIndex);
                }
            });
            //合并弹幕
            DanmuSettingMerge.IsOn = SettingHelper.GetValue<bool>(SettingHelper.VideoDanmaku.MERGE, false);
            DanmuSettingMerge.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<bool>(SettingHelper.VideoDanmaku.MERGE, DanmuSettingMerge.IsOn);
            });
            //半屏显示
            //DanmuControl.DanmakuArea = SettingHelper.GetValue<bool>(SettingHelper.VideoDanmaku.DOTNET_HIDE_SUBTITLE, false)?1:.5;
            //DanmuSettingDotHideSubtitle.Toggled += new RoutedEventHandler((e, args) =>
            //{
            //    SettingHelper.SetValue<bool>(SettingHelper.VideoDanmaku.DOTNET_HIDE_SUBTITLE, DanmuSettingDotHideSubtitle.IsOn);
            //});

            //弹幕开关
            DanmuControl.Visibility = SettingHelper.GetValue<Visibility>(SettingHelper.VideoDanmaku.SHOW, Visibility.Visible);
            DanmuSettingWords.ItemsSource = settingVM.ShieldWords;
        }

        private void LoadPlayerSetting()
        {
            //音量
            Player.Volume = SettingHelper.GetValue<double>(SettingHelper.Player.PLAYER_VOLUME, 1.0);
            SliderVolume.ValueChanged += new RangeBaseValueChangedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<double>(SettingHelper.Player.PLAYER_VOLUME, SliderVolume.Value);
            });
            //亮度
            //_brightness = SettingHelper.GetValue<double>(SettingHelper.Player.PLAYER_BRIGHTNESS, 0);
            //BrightnessShield.Opacity = _brightness;

            //播放模式
            PlayerSettingMode.SelectedIndex = SettingHelper.GetValue<int>(SettingHelper.Player.DEFAULT_VIDEO_TYPE, 1);
            PlayerSettingMode.SelectionChanged += new SelectionChangedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<int>(SettingHelper.Player.DEFAULT_VIDEO_TYPE, PlayerSettingMode.SelectedIndex);
            });
            //播放列表
            PlayerSettingPlayMode.SelectedIndex = SettingHelper.GetValue<int>(SettingHelper.Player.DEFAULT_PLAY_MODE, 0);
            PlayerSettingPlayMode.SelectionChanged += new SelectionChangedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<int>(SettingHelper.Player.DEFAULT_PLAY_MODE, PlayerSettingPlayMode.SelectedIndex);
            });
            //使用其他网站视频
            PlayerSettingUseOtherSite.IsOn = SettingHelper.GetValue<bool>(SettingHelper.Player.USE_OTHER_SITEVIDEO, false);
            PlayerSettingUseOtherSite.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<bool>(SettingHelper.Player.USE_OTHER_SITEVIDEO, PlayerSettingUseOtherSite.IsOn);
            });
            //自动跳转
            PlayerSettingAutoToPosition.IsOn = SettingHelper.GetValue<bool>(SettingHelper.Player.AUTO_TO_POSITION, true);
            PlayerSettingAutoToPosition.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<bool>(SettingHelper.Player.AUTO_TO_POSITION, PlayerSettingAutoToPosition.IsOn);
            });
            //自动跳转
            PlayerSettingAutoNext.IsOn = SettingHelper.GetValue<bool>(SettingHelper.Player.AUTO_NEXT, true);
            PlayerSettingAutoNext.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<bool>(SettingHelper.Player.AUTO_NEXT, PlayerSettingAutoNext.IsOn);
            });
            //播放比例
            PlayerSettingRatio.SelectedIndex = SettingHelper.GetValue<int>(SettingHelper.Player.RATIO, 0);
            Player.SetRatioMode(PlayerSettingRatio.SelectedIndex);
            PlayerSettingRatio.SelectionChanged += new SelectionChangedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<int>(SettingHelper.Player.RATIO, PlayerSettingRatio.SelectedIndex);
                Player.SetRatioMode(PlayerSettingRatio.SelectedIndex);
            });
            // 播放倍数
            BottomCBSpeed.SelectedIndex = SettingHelper.Player.VideoSpeed.IndexOf(SettingHelper.GetValue<double>(SettingHelper.Player.DEFAULT_VIDEO_SPEED, 1.0d));
            Player.SetRate(SettingHelper.GetValue<double>(SettingHelper.Player.DEFAULT_VIDEO_SPEED, 1.0d));
            BottomCBSpeed.SelectionChanged += new SelectionChangedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<double>(SettingHelper.Player.DEFAULT_VIDEO_SPEED, SettingHelper.Player.VideoSpeed[BottomCBSpeed.SelectedIndex]);
                Player.SetRate(SettingHelper.Player.VideoSpeed[BottomCBSpeed.SelectedIndex]);
            });

            _autoPlay = SettingHelper.GetValue<bool>(SettingHelper.Player.AUTO_PLAY, false);
            //A-B播放
            PlayerSettingABPlayMode.Toggled += new RoutedEventHandler((e, args) =>
            {
                if (PlayerSettingABPlayMode.IsOn)
                {
                    PlayerSettingABPlaySetPointA.Visibility = Visibility.Visible;
                }
                else
                {
                    Player.ABPlay = null;
                    VideoPlayHistoryHelper.SetABPlayHistory(CurrentPlayItem, null);
                    PlayerSettingABPlaySetPointA.Visibility = Visibility.Collapsed;
                    PlayerSettingABPlaySetPointB.Visibility = Visibility.Collapsed;
                    PlayerSettingABPlaySetPointA.Content = "设置A点";
                    PlayerSettingABPlaySetPointB.Content = "设置B点";
                }
            });
        }

        private void LoadSutitleSetting()
        {
            //字幕加粗
            SubtitleSettingBold.IsOn = SettingHelper.GetValue<bool>(SettingHelper.Player.SUBTITLE_BOLD, true);
            SubtitleSettingBold.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<bool>(SettingHelper.Player.SUBTITLE_BOLD, SubtitleSettingBold.IsOn);
                UpdateSubtitle();
            });

            //字幕大小
            SubtitleSettingSize.Value = SettingHelper.GetValue<double>(SettingHelper.Player.SUBTITLE_SIZE, 40);
            SubtitleSettingSize.ValueChanged += new RangeBaseValueChangedEventHandler((e, args) =>
            {
                if (miniWin) return;
                SettingHelper.SetValue<double>(SettingHelper.Player.SUBTITLE_SIZE, SubtitleSettingSize.Value);
                UpdateSubtitle();
            });
            //字幕边框颜色
            SubtitleSettingBorderColor.SelectedIndex = SettingHelper.GetValue<int>(SettingHelper.Player.SUBTITLE_BORDER_COLOR, 0);
            SubtitleSettingBorderColor.SelectionChanged += new SelectionChangedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<int>(SettingHelper.Player.SUBTITLE_BORDER_COLOR, SubtitleSettingBorderColor.SelectedIndex);
                UpdateSubtitle();
            });
            //字幕颜色
            SubtitleSettingColor.SelectedIndex = SettingHelper.GetValue<int>(SettingHelper.Player.SUBTITLE_COLOR, 0);
            SubtitleSettingColor.SelectionChanged += new SelectionChangedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<int>(SettingHelper.Player.SUBTITLE_COLOR, SubtitleSettingColor.SelectedIndex);
                UpdateSubtitle();
            });

            //字幕对齐
            SubtitleSettingAlign.SelectedIndex = SettingHelper.GetValue<int>(SettingHelper.Player.SUBTITLE_ALIGN, 0);
            SubtitleSettingAlign.SelectionChanged += new SelectionChangedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<int>(SettingHelper.Player.SUBTITLE_ALIGN, SubtitleSettingAlign.SelectedIndex);
                UpdateSubtitle();
            });

            //字幕透明度
            SubtitleSettingOpacity.Value = SettingHelper.GetValue<double>(SettingHelper.Player.SUBTITLE_OPACITY, 1.0);
            SubtitleSettingOpacity.ValueChanged += new RangeBaseValueChangedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<double>(SettingHelper.Player.SUBTITLE_OPACITY, SubtitleSettingOpacity.Value);
            });
            //字幕底部距离
            SubtitleSettingBottom.Value = SettingHelper.GetValue<double>(SettingHelper.Player.SUBTITLE_BOTTOM, 40);
            BorderSubtitle.Margin = new Thickness(0, 0, 0, SubtitleSettingBottom.Value);
            SubtitleSettingBottom.ValueChanged += new RangeBaseValueChangedEventHandler((e, args) =>
            {
                BorderSubtitle.Margin = new Thickness(0, 0, 0, SubtitleSettingBottom.Value);
                SettingHelper.SetValue<double>(SettingHelper.Player.SUBTITLE_BOTTOM, SubtitleSettingBottom.Value);
            });
            //字幕转换
            SubtitleSettingToSimplified.IsOn = SettingHelper.GetValue<bool>(SettingHelper.Roaming.TO_SIMPLIFIED, true);
            SubtitleSettingToSimplified.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue<bool>(SettingHelper.Roaming.TO_SIMPLIFIED, SubtitleSettingToSimplified.IsOn);
                if (SubtitleSettingToSimplified.IsOn)
                {
                    currentSubtitleText = Utils.ToSimplifiedChinese(currentSubtitleText);
                    UpdateSubtitle();
                }
            });
        }

        public void InitializePlayInfo(List<PlayInfo> playInfos, int index)
        {
            //保持屏幕常亮
            dispRequest.RequestActive();

            PlayInfos = playInfos;
            EpisodeList.ItemsSource = PlayInfos;
            if (PlayInfos.Count > 1)
            {
                ShowPlaylistButton = true;
            }
            else if (PlayInfos.Count == 1 && PlayInfos[0].is_interaction)
            {
                ShowPlaylistButton = true;
            }
            else
            {
                ShowPlaylistButton = false;
            }
            EpisodeList.SelectedIndex = index;
        }

        private async void DanmuTimer_Tick(object sender, object e)
        {
            if (showControlsFlag != -1)
            {
                if (showControlsFlag >= 5)
                {
                    var elent = FocusManager.GetFocusedElement();
                    if (!(elent is TextBox) && !(elent is AutoSuggestBox))
                    {
                        ShowControl(false);
                        showControlsFlag = -1;
                    }
                    //FadeOut.Begin();
                    //control.Visibility = Visibility.Collapsed;
                }
                else
                {
                    showControlsFlag++;
                }
            }
            var p = Convert.ToInt32(Player.Position);
            var segIndex = Convert.ToInt32(Math.Ceiling(Player.Position / (60 * 6d)));
            if (segIndex <= 0) segIndex = 1;
            if (CurrentPlayItem?.LocalPlayInfo?.IsManifestPlayback != true && danmakuLoadedSegment != null && !danmakuLoadedSegment.Contains(segIndex))
            {
                await LoadDanmaku(segIndex);
            }
            if (Buffering)
            {
                return;
            }
            if (Player.PlayState != PlayState.Playing || GridBuffering.Visibility == Visibility.Visible)
            {
                return;
            }

            if (DanmuControl.Visibility == Visibility.Collapsed)
            {
                return;
            }
            var needDistinct = DanmuSettingMerge.IsOn;
            var level = DanmuSettingShieldLevel.Value;
            var max = Convert.ToInt32(DanmuSettingMaxNum.Value);
            var poolSnapshot = danmakuPool;
            var itemGeneration = _playItemGeneration;
            await Task.Run(async () =>
            {
                try
                {
                    if (poolSnapshot != null && poolSnapshot.ContainsKey(p))
                    {
                        var data = poolSnapshot[p].Where(x => true);
                        //云屏蔽
                        data = data.Where(x => x.weight >= level);
                        //去重
                        if (needDistinct)
                        {
                            data = data.Distinct(new CompareDanmakuModel());
                        }
                        //关键词
                        foreach (var item in settingVM.ShieldWords)
                        {
                            data = data.Where(x => !x.text.Contains(item));
                        }
                        //用户
                        foreach (var item in settingVM.ShieldUsers)
                        {
                            data = data.Where(x => !x.sendID.Equals(item));
                        }
                        //正则
                        foreach (var item in settingVM.ShieldRegulars)
                        {
                            data = data.Where(x => !Regex.IsMatch(x.text, item));
                        }
                        if (max > 0)
                        {
                            data = data.Take(max);
                        }
                        //加载弹幕
                        foreach (var item in data)
                        {
                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                            {
                                if (itemGeneration == _playItemGeneration && ReferenceEquals(poolSnapshot, danmakuPool))
                                    DanmuControl.AddDanmu(item, false);
                            });
                        }
                        data = null;
                    }
                }
                catch (Exception)
                {
                }
            });
            if (Player.PlayState == PlayState.Pause)
            {
                DanmuControl.PauseDanmaku();
            }
        }

        private async Task SetPlayItem(int index)
        {
            if (PlayInfos == null || PlayInfos.Count == 0)
            {
                return;
            }
            //清空字幕
            _subtitleLoadGuard.Invalidate();
            _danmakuLoadGuard.Invalidate();
            var itemGeneration = ++_playItemGeneration;
            ClearSubTitle();
            Player.ClosePlay();
            if (index >= PlayInfos.Count)
            {
                index = PlayInfos.Count - 1;
            }
            CurrentPlayIndex = index;
            var selectedPlayItem = PlayInfos[index];
            if (CurrentPlayItem == null || CurrentPlayItem.season_id != selectedPlayItem.season_id ||
                CurrentPlayItem.ep_id != selectedPlayItem.ep_id || CurrentPlayItem.cid != selectedPlayItem.cid)
            {
                _hasPlayedCurrentItem = false;
            }
            CurrentPlayItem = selectedPlayItem;
            _subtitleTicket = _subtitleLoadGuard.Begin(selectedPlayItem);
            _danmakuLoadGuard.Begin(selectedPlayItem);
            _enabledManifestDanmaku.Clear();
            if (selectedPlayItem.LocalPlayInfo?.IsManifestPlayback == true)
            {
                foreach (var track in selectedPlayItem.LocalPlayInfo.DanmakuFiles ?? new List<LocalMediaPlaybackTrack>())
                    if (track.Track.EnabledByDefault) _enabledManifestDanmaku.Add(track.Track.Id);
            }
            UpdateLocalTrackSelectors();
            if (CurrentPlayItem.is_interaction)
            {
                ShowPlaylistButton = false;
                ShowPlayNodeButton = true;
            }
            //设置标题
            TopTitle.Text = CurrentPlayItem.title;
            if (_systemMediaTransportControls != null)
            {
                SystemMediaTransportControlsDisplayUpdater updater = _systemMediaTransportControls.DisplayUpdater;
                updater.Type = MediaPlaybackType.Video;
                updater.VideoProperties.Title = CurrentPlayItem.title;
                updater.Update();
            }

            //设置下一集按钮的显示
            if (PlayInfos.Count >= 1 && index != PlayInfos.Count - 1)
            {
                BottomBtnNext.Visibility = Visibility.Visible;
            }
            else
            {
                BottomBtnNext.Visibility = Visibility.Collapsed;
            }
            ChangeEpisodeEvent?.Invoke(this, index);

            playUrlInfo = null;
            //if (CurrentPlayItem.play_mode == VideoPlayType.Season)
            //{
            //   // Player._ffmpegConfig.FFmpegOptions["referer"] = "https://www.bilibili.com/bangumi/play/ep" + CurrentPlayItem.ep_id;
            //}
            if (SettingHelper.GetValue<bool>(SettingHelper.Player.AUTO_TO_POSITION, true))
            {
                _position = SettingHelper.GetValue<double>(CurrentPlayItem.season_id != 0 ? "ep" + CurrentPlayItem.ep_id : CurrentPlayItem.cid, 0);
                //减去两秒防止视频直接结束了
                if (_position >= 2) _position -= 2;
            }
            else
            {
                _position = 0;
            }
            //await playerHelper.ReportHistory(CurrentPlayItem, 0);
            await SetDanmaku();
            if (!ReferenceEquals(CurrentPlayItem, selectedPlayItem) || itemGeneration != _playItemGeneration) return;
            await SetQuality();
            if (!ReferenceEquals(CurrentPlayItem, selectedPlayItem) || itemGeneration != _playItemGeneration) return;
            await GetPlayerInfo();
            if (!ReferenceEquals(CurrentPlayItem, selectedPlayItem) || itemGeneration != _playItemGeneration) return;

            Player.ABPlay = VideoPlayHistoryHelper.FindABPlayHistory(CurrentPlayItem);
            if (Player.ABPlay == null)
            {
                PlayerSettingABPlayMode.IsOn = false;
            }
            else
            {
                PlayerSettingABPlayMode.IsOn = true;
                PlayerSettingABPlaySetPointA.Visibility = Visibility.Visible;
                PlayerSettingABPlaySetPointB.Visibility = Visibility.Visible;
                PlayerSettingABPlaySetPointA.Content = "A: " + TimeSpan.FromSeconds(Player.ABPlay.PointA).ToString(@"hh\:mm\:ss\.fff");
                if (Player.ABPlay.PointB != double.MaxValue)
                    PlayerSettingABPlaySetPointB.Content = "B: " + TimeSpan.FromSeconds(Player.ABPlay.PointB).ToString(@"hh\:mm\:ss\.fff");
            }
        }

        /// <summary>
        /// 字幕文件
        /// </summary>
        private SubtitleModel subtitles;

        /// <summary>
        /// 字幕Timer
        /// </summary>
        private DispatcherTimer subtitleTimer;

        /// <summary>
        /// 当前显示的字幕文本
        /// </summary>
        private string currentSubtitleText = "";

        /// <summary>
        /// 选择字幕
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Menuitem_Click(object sender, RoutedEventArgs e)
        {
            foreach (ToggleMenuFlyoutItem item in (BottomBtnSelctSubtitle.Flyout as MenuFlyout).Items)
            {
                item.IsChecked = false;
            }
            var menuitem = (sender as ToggleMenuFlyoutItem);
            CurrentSubtitleName = menuitem.Text;
            var generation = _subtitleTicket = _subtitleLoadGuard.Begin(CurrentPlayItem);
            if (menuitem.Tag == null)
            {
                ClearSubTitle();
            }
            else if (menuitem.Tag is LocalMediaPlaybackTrack localSubtitle)
            {
                LoadManifestSubtitleAsync(localSubtitle, CurrentPlayItem, generation);
            }
            else
            {
                SetSubTitle(menuitem.Tag.ToString(), generation);
            }
            menuitem.IsChecked = true;
        }

        private static readonly Regex AiSubtitleMarker = new Regex(@"(?<![A-Za-z])ai(?![A-Za-z])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 判断字幕是否由 AI 生成：标签含“自动”、括号内语言码以 ai- 开头，或标签中出现独立的 AI。
        /// </summary>
        private static bool IsAutoGeneratedSubtitle(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) return false;
            if (label.Contains("自动")) return true;
            var open = label.LastIndexOf('(');
            var close = label.LastIndexOf(')');
            if (open >= 0 && close > open + 1)
            {
                var lan = label.Substring(open + 1, close - open - 1);
                if (lan.StartsWith("ai-", StringComparison.OrdinalIgnoreCase)) return true;
            }
            return AiSubtitleMarker.IsMatch(label);
        }

        private void SelectInitialSubtitle(MenuFlyout menu, ToggleMenuFlyoutItem noneItem, bool autoAISubtitle, string source)
        {
            var tracks = menu.Items.OfType<ToggleMenuFlyoutItem>().Where(item => item.Tag != null).ToList();
            var initial = autoAISubtitle
                ? (tracks.FirstOrDefault(item => IsAutoGeneratedSubtitle(item.Text)) ?? tracks.FirstOrDefault())
                : tracks.FirstOrDefault(item => !IsAutoGeneratedSubtitle(item.Text));
            if (initial == null)
            {
                noneItem.IsChecked = true;
                CurrentSubtitleName = noneItem.Text;
            }
            else
            {
                initial.IsChecked = true;
                CurrentSubtitleName = initial.Text;
                SetSubTitle(initial.Tag.ToString());
            }
            LogHelper.Log($"Subtitle initial selection source={source} aiAutoOpen={autoAISubtitle} tracks={tracks.Count} candidate=\"{initial?.Text ?? "none"}\" selected=\"{(initial == null ? "none" : initial.Text)}\"", LogType.INFO);
        }

        private async void LoadManifestSubtitleAsync(LocalMediaPlaybackTrack track, PlayInfo item, int generation)
        {
            var stage = "subtitle-read";
            try
            {
                ClearSubTitle();
                await _subtitleLoadGuard.LoadAndApplyAsync(generation, item, async () =>
                {
                    var text = await FileIO.ReadTextAsync(track.File);
                    stage = "subtitle-parse";
                    var format = track.Track.Format;
                    if (string.IsNullOrWhiteSpace(format)) format = System.IO.Path.GetExtension(track.Track.Path).TrimStart('.');
                    return SubtitleTrackParser.Parse(text, format);
                }, loaded =>
                {
                    subtitles = loaded;
                    if (subtitles.body.Count > 0)
                    {
                        subtitleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                        subtitleTimer.Tick += SubtitleTimer_Tick;
                        subtitleTimer.Start();
                    }
                });
            }
            catch (Exception exception)
            {
                LogHelper.Log($"Local subtitle load failed stage={stage} collectionId={item?.LocalPlayInfo?.ManifestCollectionId} itemId={item?.LocalPlayInfo?.ManifestItem?.Id} trackId={track?.Track?.Id} detail={exception}", LogType.ERROR);
                if (_subtitleLoadGuard.IsCurrent(generation, item)) Utils.ShowMessageToast("加载本地字幕失败");
            }
        }

        private void ClearSubTitle()
        {
            subtitleTimer?.Stop();
            subtitleTimer = null;
            subtitles = null;
            currentSubtitleText = "";
            BorderSubtitle.Visibility = Visibility.Collapsed;
            BorderSubtitle.Child = null;
        }

        /// <summary>
        /// 设置字幕文件
        /// </summary>
        /// <param name="url"></param>
        private void SetSubTitle(string url)
        {
            SetSubTitle(url, _subtitleTicket = _subtitleLoadGuard.Begin(CurrentPlayItem));
        }

        private async void SetSubTitle(string url, int generation)
        {
            var item = CurrentPlayItem;
            try
            {
                ClearSubTitle();
                await _subtitleLoadGuard.LoadAndApplyAsync(generation, item, async () =>
                {
                    var loaded = await playerHelper.GetSubtitle(url);
                    return loaded;
                }, loaded =>
                {
                    subtitles = loaded;
                    if (subtitles == null) return;
                    //转为简体
                    if (SettingHelper.GetValue<bool>(SettingHelper.Roaming.TO_SIMPLIFIED, true) && CurrentSubtitleName != null && CurrentSubtitleName.StartsWith("中文（繁体）", StringComparison.Ordinal))
                    {
                        foreach (var cue in subtitles.body)
                        {
                            cue.content = Utils.ToSimplifiedChinese(cue.content);
                        }
                    }
                    subtitleTimer = new DispatcherTimer();
                    subtitleTimer.Interval = TimeSpan.FromMilliseconds(100);
                    subtitleTimer.Tick += SubtitleTimer_Tick;
                    subtitleTimer.Start();
                });
            }
            catch (Exception)
            {
                if (_subtitleLoadGuard.IsCurrent(generation, item)) Utils.ShowMessageToast("加载字幕失败了");
            }
        }

        private async void SubtitleTimer_Tick(object sender, object e)
        {
            var generation = _subtitleTicket;
            var activeSubtitles = subtitles;
            if (Player.PlayState == PlayState.Playing)
            {
                if (activeSubtitles == null)
                {
                    return;
                }
                var time = Player.Position;
                var first = SubtitleTrackParser.FindCue(activeSubtitles, time);
                if (first != null)
                {
                    if (first.content != currentSubtitleText)
                    {
                        BorderSubtitle.Visibility = Visibility.Visible;
                        var visual = await GenerateSubtitleItem(first.content);
                        if (!_subtitleLoadGuard.TryApply(generation, CurrentPlayItem, () =>
                        {
                            if (!ReferenceEquals(activeSubtitles, subtitles)) return;
                            BorderSubtitle.Child = visual;
                        })) return;
                        if (!ReferenceEquals(activeSubtitles, subtitles)) return;
                        currentSubtitleText = first.content;
                    }
                }
                else
                {
                    BorderSubtitle.Visibility = Visibility.Collapsed;
                    currentSubtitleText = "";
                }
            }
        }

        private async void UpdateSubtitle()
        {
            if (BorderSubtitle.Visibility == Visibility.Visible && currentSubtitleText != "")
            {
                var ticket = _subtitleTicket;
                var item = CurrentPlayItem;
                var activeSubtitles = subtitles;
                var text = currentSubtitleText;
                var visual = await GenerateSubtitleItem(text);
                _subtitleLoadGuard.TryApply(ticket, item, () =>
                {
                    if (ReferenceEquals(activeSubtitles, subtitles) && currentSubtitleText == text)
                        BorderSubtitle.Child = visual;
                });
            }
        }

        private async Task<Grid> GenerateSubtitleItem(string text)
        {
            //行首行尾加空格，防止字体描边超出
            text = " " + text.Replace("\n", " \n ") + " ";

            var fontSize = (float)SubtitleSettingSize.Value;
            var color = Utils.ToColor((SubtitleSettingColor.SelectedItem as ComboBoxItem).Tag.ToString());
            var borderColor = Utils.ToColor((SubtitleSettingBorderColor.SelectedItem as ComboBoxItem).Tag.ToString());

            CanvasHorizontalAlignment canvasHorizontalAlignment = CanvasHorizontalAlignment.Center;
            TextAlignment textAlignment = TextAlignment.Center;
            if (SubtitleSettingAlign.SelectedIndex == 1)
            {
                canvasHorizontalAlignment = CanvasHorizontalAlignment.Left;
                textAlignment = TextAlignment.Left;
            }
            else if (SubtitleSettingAlign.SelectedIndex == 2)
            {
                canvasHorizontalAlignment = CanvasHorizontalAlignment.Right;
                textAlignment = TextAlignment.Right;
            }
            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasTextFormat fmt = new CanvasTextFormat() { FontSize = fontSize, HorizontalAlignment = canvasHorizontalAlignment, };
            var tb = new TextBlock { Text = text, FontSize = fontSize, TextAlignment = textAlignment };
            if (SubtitleSettingBold.IsOn)
            {
                fmt.FontWeight = FontWeights.Bold;
                tb.FontWeight = FontWeights.Bold;
            }

            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            var myBitmap = new CanvasRenderTarget(device, (float)tb.DesiredSize.Width + 4, (float)tb.DesiredSize.Height, displayInformation.LogicalDpi);

            CanvasTextLayout canvasTextLayout = new CanvasTextLayout(device, text, fmt, (float)tb.DesiredSize.Width + 4, (float)tb.DesiredSize.Height);

            CanvasGeometry combinedGeometry = CanvasGeometry.CreateText(canvasTextLayout);

            using (var ds = myBitmap.CreateDrawingSession())
            {
                ds.Clear(Colors.Transparent);
                ds.DrawGeometry(combinedGeometry, borderColor, 4f, new CanvasStrokeStyle()
                {
                    DashStyle = CanvasDashStyle.Solid
                });
                ds.FillGeometry(combinedGeometry, color);
            }
            Image image = new Image();
            BitmapImage im = new BitmapImage();
            using (InMemoryRandomAccessStream oStream = new InMemoryRandomAccessStream())
            {
                await myBitmap.SaveAsync(oStream, CanvasBitmapFileFormat.Png, 1.0f);
                await im.SetSourceAsync(oStream);
            }
            image.Width = tb.DesiredSize.Width;
            image.Source = im;
            image.Stretch = Stretch.Uniform;
            Grid grid = new Grid();

            grid.Tag = text;
            grid.Children.Add(image);

            return grid;
        }

        public void ChangePlayIndex(int index)
        {
            ClearSubTitle();
            DanmuControl.ClearAll();
            EpisodeList.SelectedIndex = index;
        }

        private async void EpisodeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EpisodeList.SelectedItem == null)
            {
                return;
            }
            DanmuControl.ClearAll();
            await SetPlayItem(EpisodeList.SelectedIndex);
        }

        private async void NodeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NodeList.SelectedItem == null || interactionVideoVM.Loading)
            {
                return;
            }
            var item = NodeList.SelectedItem as InteractionEdgeInfoStoryListModel;
            await ChangedNode(item.edge_id, item.cid.ToString());
        }

        private async void SelectChoice_ItemClick(object sender, ItemClickEventArgs e)
        {
            var choice = e.ClickedItem as InteractionEdgeInfoChoiceModel;
            await ChangedNode(choice.id, choice.cid.ToString());
        }

        private async Task ChangedNode(int node_id, string cid)
        {
            InteractionChoices.Visibility = Visibility.Collapsed;
            CurrentPlayItem.cid = cid;

            await interactionVideoVM.GetNodes(node_id);

            TopTitle.Text = interactionVideoVM.Select.title;
            //if ((interactionVideoVM.Info.edges?.questions?.Count ?? 0) <= 0)
            //{
            //    Utils.ShowMessageToast("播放完毕，请点击右下角节点，重新开始");
            //    return;
            //}
            _position = 0;
            _autoPlay = true;
            DanmuControl.ClearAll();
            await SetDanmaku();

            await SetQuality();
        }

        private void UpdateLocalTrackSelectors()
        {
            var localInfo = CurrentPlayItem?.LocalPlayInfo;
            var isManifest = localInfo?.IsManifestPlayback == true;
            LocalTracksPivot.Visibility = isManifest ? Visibility.Visible : Visibility.Collapsed;
            _updatingLocalTrackSelectors = true;
            try
            {
                var videoOptions = LocalMediaTrackSelection.VideoOptions(localInfo);
                var audioOptions = LocalMediaTrackSelection.AudioOptions(localInfo);
                LocalVideoTrackSelector.ItemsSource = isManifest ? videoOptions : null;
                LocalAudioTrackSelector.ItemsSource = isManifest ? audioOptions : null;
                LocalVideoTrackSelector.SelectedItem = videoOptions.FirstOrDefault(option => ReferenceEquals(option.Track, localInfo?.SelectedVideoTrack));
                LocalAudioTrackSelector.SelectedItem = audioOptions.FirstOrDefault(option =>
                    option.IsEmbeddedAudio ? localInfo?.SelectedAudioTrack == null : ReferenceEquals(option.Track, localInfo?.SelectedAudioTrack));
            }
            finally
            {
                _updatingLocalTrackSelectors = false;
            }
        }

        private async void LocalVideoTrackSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_updatingLocalTrackSelectors || _localTrackSwitching || !(LocalVideoTrackSelector.SelectedItem is LocalMediaTrackSelectionOption option) || option.Track == null)
                return;
            await SwitchLocalTrackAsync(option.Track, isAudio: false);
        }

        private async void LocalAudioTrackSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_updatingLocalTrackSelectors || _localTrackSwitching || !(LocalAudioTrackSelector.SelectedItem is LocalMediaTrackSelectionOption option))
                return;
            await SwitchLocalTrackAsync(option.IsEmbeddedAudio ? null : option.Track, isAudio: true);
        }

        private async Task SwitchLocalTrackAsync(LocalMediaPlaybackTrack requestedTrack, bool isAudio)
        {
            var item = CurrentPlayItem;
            var localInfo = item?.LocalPlayInfo;
            if (localInfo?.IsManifestPlayback != true || _localTrackSwitching)
                return;

            var resumeId = localInfo.ResumeId;
            var position = Player.Position;
            var wasPlaying = Player.PlayState == PlayState.Playing;
            var itemId = localInfo.ManifestItem.Id;
            var collectionId = localInfo.ManifestCollectionId;
            var trackId = requestedTrack?.Track?.Id ?? "embedded";
            var stage = isAudio ? "audio-track-switch" : "video-track-switch";
            _localTrackSwitching = true;
            LocalVideoTrackSelector.IsEnabled = false;
            LocalAudioTrackSelector.IsEnabled = false;
            EpisodeList.IsEnabled = false;
            BottomBtnNext.IsEnabled = false;
            try
            {
                var outcome = await LocalMediaTrackSwitchCoordinator.SwitchAsync(
                    localInfo,
                    isAudio,
                    requestedTrack,
                    (savedPosition, resumePlaying) =>
                    {
                        EnsureLocalSwitchItem(item, resumeId);
                        _position = savedPosition;
                        _autoPlay = resumePlaying;
                        _restoreLocalTrackPositionOnOpen = true;
                    },
                    position,
                    wasPlaying,
                    async () =>
                    {
                        EnsureLocalSwitchItem(item, resumeId);
                        var result = await PlayLocalFile(preservePlaybackState: true);
                        EnsureLocalSwitchItem(item, resumeId);
                        return new LocalMediaTrackOpenResult
                        {
                            Success = result.result,
                            Message = result.message,
                            Detail = result.detail_message
                        };
                    });

                if (!ReferenceEquals(CurrentPlayItem, item) || CurrentPlayItem?.LocalPlayInfo?.ResumeId != resumeId)
                    return;
                if (outcome.Success)
                    return;

                var failure = outcome.OpenException?.ToString() ?? FormatLocalTrackSwitchFailure(outcome.OpenResult);
                var recoveryFailed = outcome.RestoreException != null || outcome.RestoreResult?.Success != true;
                if (recoveryFailed)
                    _restoreLocalTrackPositionOnOpen = false;
                var rollback = outcome.RestoreException?.ToString() ?? FormatLocalTrackSwitchFailure(outcome.RestoreResult);
                LogHelper.Log("Local media track switch failed; stage=" + stage + "-rollback-reopen; collectionId=" + collectionId + "; itemId=" + itemId + "; trackId=" + trackId + "; failure=" + failure + "; rollback=" + rollback, LogType.ERROR);
                var message = outcome.OpenResult?.Message;
                if (string.IsNullOrWhiteSpace(message))
                    message = outcome.OpenException?.Message ?? "The selected track could not be opened.";
                ShowDialog(message, isAudio ? "切换音频失败" : "切换视频失败");
            }
            catch (Exception exception)
            {
                if (!IsLocalSwitchItemCurrent(item, resumeId))
                {
                    LogHelper.Log("Local media track switch stopped after item changed; stage=" + stage + "; collectionId=" + collectionId + "; itemId=" + itemId + "; trackId=" + trackId + "; exception=" + exception, LogType.INFO);
                    return;
                }
                LogHelper.Log("Local media track switch failed; stage=" + stage + "; collectionId=" + collectionId + "; itemId=" + itemId + "; trackId=" + trackId + "; exception=" + exception, LogType.ERROR);
                ShowDialog("The selected track could not be opened. " + exception.Message, isAudio ? "切换音频失败" : "切换视频失败");
            }
            finally
            {
                _localTrackSwitching = false;
                LocalVideoTrackSelector.IsEnabled = true;
                LocalAudioTrackSelector.IsEnabled = true;
                EpisodeList.IsEnabled = true;
                BottomBtnNext.IsEnabled = true;
                UpdateLocalTrackSelectors();
            }
        }

        private bool IsLocalSwitchItemCurrent(PlayInfo item, string resumeId)
        {
            return ReferenceEquals(CurrentPlayItem, item) && CurrentPlayItem?.LocalPlayInfo?.ResumeId == resumeId;
        }

        private void EnsureLocalSwitchItem(PlayInfo item, string resumeId)
        {
            if (!IsLocalSwitchItemCurrent(item, resumeId))
                throw new OperationCanceledException("The local playback item changed during the track switch.");
        }

        private static string FormatLocalTrackSwitchFailure(LocalMediaTrackOpenResult result)
        {
            return "message=" + (result?.Message ?? "") + "; detail=" + (result?.Detail ?? "") + "; success=" + (result?.Success.ToString() ?? "null");
        }

        private double _position = 0;
        private bool _autoPlay = false;
        private bool _hasPlayedCurrentItem = false;

        /// <summary>
        /// The comments the player already holds, oldest first, for the danmaku list. Reads the
        /// loaded pool only: opening the list never fetches anything.
        /// </summary>
        public IReadOnlyList<DanmakuListRow> GetDanmakuListSnapshot(DanmakuListMode mode)
        {
            var pool = danmakuPool;
            var loaded = new List<DanmakuListRow>();
            if (pool != null)
            {
                foreach (var group in pool)
                {
                    foreach (var danmaku in group.Value)
                    {
                        if (danmaku == null) continue;
                        loaded.Add(new DanmakuListRow()
                        {
                            RowId = danmaku.rowID,
                            SendId = danmaku.sendID,
                            Text = danmaku.text,
                            Time = danmaku.time,
                        });
                    }
                }
            }
            return DanmakuListSnapshot.Build(loaded, Player.Position, mode, CanLikeDanmakuRows());
        }

        /// <summary>
        /// A like reaches the server only for online playback, with an account, and only for rows
        /// that carry a server id; downloaded, manifest and local XML comments stay copy-only.
        /// </summary>
        private bool CanLikeDanmakuRows()
        {
            var item = CurrentPlayItem;
            if (item == null) return false;
            if (item.play_mode == VideoPlayType.Download || item.play_mode == VideoPlayType.LoaclFile) return false;
            if (item.LocalPlayInfo != null) return false;
            if (string.IsNullOrEmpty(item.cid)) return false;
            return SettingHelper.Account.Logined;
        }

        private async Task SetDanmaku(bool update = false)
        {
            var loadingItem = CurrentPlayItem;
            var loadingTicket = 0;
            try
            {
                if (CurrentPlayItem.LocalPlayInfo?.IsManifestPlayback == true)
                {
                    var item = CurrentPlayItem;
                    var generation = loadingTicket = _danmakuLoadGuard.Begin(item);
                    danmakuPool = new Dictionary<int, List<DanmakuModel>>();
                    DanmuControl.ClearAll();
                    TxtDanmuCount.Text = "0";
                    var tracks = (CurrentPlayItem.LocalPlayInfo.DanmakuFiles ?? new List<LocalMediaPlaybackTrack>())
                        .Where(track => _enabledManifestDanmaku.Contains(track.Track.Id)).ToList();
                    await _danmakuLoadGuard.LoadAndApplyAsync(generation, item,
                        () => DanmakuTrackLoader.LoadAsync(tracks, async file => danmakuParse.ParseBiliBili(await FileIO.ReadTextAsync(file))),
                        list =>
                        {
                            danmakuPool = list.GroupBy(x => x.time_s).ToDictionary(x => x.Key, x => x.ToList());
                            danmakuLoadedSegment = new List<int>();
                            TxtDanmuCount.Text = list.Count.ToString();
                        });
                    return;
                }
                if (CurrentPlayItem.play_mode == VideoPlayType.Download && !update)
                {
                    //不要加载空弹幕
                    var path = CurrentPlayItem.LocalPlayInfo.DanmakuPath;
                    if (string.IsNullOrEmpty(path))
                    {
                        TxtDanmuCount.Text = "无弹幕文件";
                        return;
                    }
                    var ticket = _danmakuLoadGuard.Begin(loadingItem);
                    await _danmakuLoadGuard.LoadAndApplyAsync(ticket, loadingItem, async () =>
                    {
                        var danmakuFile = await StorageFile.GetFileFromPathAsync(path);
                        return danmakuParse.ParseBiliBili(await FileIO.ReadTextAsync(danmakuFile));
                    }, danmuList =>
                    {
                        danmakuPool = danmuList.GroupBy(x => x.time_s).ToDictionary(x => x.Key, x => x.ToList());
                        TxtDanmuCount.Text = danmuList.Count.ToString();
                    });
                }
                else
                {
                    var segIndex = Math.Ceiling(Player.Position / (60 * 6d));
                    if (update)
                    {
                        await LoadDanmaku(segIndex.ToInt32());
                        Utils.ShowMessageToast($"已更新弹幕");
                    }
                    await LoadDanmaku(1);
                    //var danmuList = (await danmakuParse.ParseBiliBili(Convert.ToInt64(CurrentPlayItem.cid)));
                    ////await playerHelper.GetDanmaku(CurrentPlayItem.cid, 1) ;
                    //danmakuPool = danmuList.GroupBy(x=>x.time_s).ToDictionary(x => x.Key, x => x.ToList());
                    //TxtDanmuCount.Text = danmuList.Count.ToString();

                    //danmuList.Clear();
                    //danmuList = null;
                }
            }
            catch (Exception exception)
            {
                if (loadingItem?.LocalPlayInfo?.IsManifestPlayback == true)
                {
                    var trackException = exception as LocalMediaTrackLoadException;
                    LogHelper.Log($"Local danmaku load failed stage={trackException?.Stage ?? "read-or-parse"} collectionId={loadingItem.LocalPlayInfo.ManifestCollectionId} itemId={loadingItem.LocalPlayInfo.ManifestItem?.Id} trackId={trackException?.TrackId ?? "unknown"} detail={exception}", LogType.ERROR);
                }
                if (ReferenceEquals(loadingItem, CurrentPlayItem) &&
                    (loadingItem?.LocalPlayInfo?.IsManifestPlayback != true || _danmakuLoadGuard.IsCurrent(loadingTicket, loadingItem)))
                    Utils.ShowMessageToast("弹幕加载失败");
            }
        }

        private async void ManifestDanmakuTrack_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as ToggleMenuFlyoutItem;
            var track = item?.Tag as LocalMediaPlaybackTrack;
            if (track == null || CurrentPlayItem?.LocalPlayInfo?.IsManifestPlayback != true) return;
            if (item.IsChecked) _enabledManifestDanmaku.Add(track.Track.Id);
            else _enabledManifestDanmaku.Remove(track.Track.Id);
            await SetDanmaku();
        }

        private void LocalDanmakuTracksButton_Click(object sender, RoutedEventArgs e)
        {
            (LocalDanmakuTracksButton.Tag as MenuFlyout)?.ShowAt(LocalDanmakuTracksButton);
        }

        private void LocalSubtitleTracksButton_Click(object sender, RoutedEventArgs e)
        {
            (BottomBtnSelctSubtitle.Flyout as MenuFlyout)?.ShowAt(LocalSubtitleTracksButton);
        }

        private bool loadingDanmaku = false;
        private bool _updatingLocalTrackSelectors;
        private bool _localTrackSwitching;
        private bool _restoreLocalTrackPositionOnOpen;

        private async Task LoadDanmaku(int segmentIndex)
        {
            try
            {
                if (CurrentPlayItem?.LocalPlayInfo?.IsManifestPlayback == true) return;
                if (loadingDanmaku) return;
                loadingDanmaku = true;
                if (segmentIndex <= 1)
                {
                    danmakuPool.Clear();
                    segmentIndex = 1;
                }
                var requestedItem = CurrentPlayItem;
                var requestedGeneration = _playItemGeneration;
                var danmuList = await playerHelper.GetDanmaku(requestedItem.cid, segmentIndex);
                if (!ReferenceEquals(requestedItem, CurrentPlayItem) || requestedGeneration != _playItemGeneration) return;
                foreach (var item in danmuList.GroupBy(x => x.time_s).ToDictionary(x => x.Key, x => x.ToList()))
                {
                    if (danmakuPool.ContainsKey(item.Key))
                    {
                        danmakuPool[item.Key] = item.Value;
                    }
                    else
                    {
                        danmakuPool.Add(item.Key, item.Value);
                    }
                }
                TxtDanmuCount.Text = danmuList.Count.ToString();
                danmuList.Clear();
                if (segmentIndex == 1)
                {
                    danmakuLoadedSegment = new List<int>() { 1 };
                }
                else
                {
                    danmakuLoadedSegment.Add(segmentIndex);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                loadingDanmaku = false;
            }
        }

        private async Task SetQuality()
        {
            VideoLoading.Visibility = Visibility.Visible;
            if (playUrlInfo != null && playUrlInfo.CurrentQuality != null)
            {
                playUrlInfo.CurrentQuality = null;
            }

            if (CurrentPlayItem.play_mode == VideoPlayType.Download || CurrentPlayItem.LocalPlayInfo?.IsManifestPlayback == true)
            {
                BottomCBQuality.Visibility = Visibility.Collapsed;
                if (CurrentPlayItem.LocalPlayInfo?.IsManifestPlayback == true)
                    _autoPlay = true;

                await PlayLocalFile();
                return;
            }

            var qn = SettingHelper.GetValue<int>(SettingHelper.Player.DEFAULT_QUALITY, 80);
            var info = await playerHelper.GetPlayUrls(CurrentPlayItem, qn);

            if (info.Success)
            {
                playUrlInfo = info;
                BottomCBQuality.ItemsSource = playUrlInfo.Qualites;
                BottomCBQuality.SelectionChanged -= BottomCBQuality_SelectionChanged;
                BottomCBQuality.SelectedItem = info.CurrentQuality;
                //SettingHelper.SetValue<int>(SettingHelper.Player.DEFAULT_QUALITY, info.data.current.quality);
                BottomCBQuality.SelectionChanged += BottomCBQuality_SelectionChanged;
                ChangeQuality(info.CurrentQuality);
            }
            else
            {
                ShowDialog($"请求信息:\r\n{info.Message}", "读取视频播放地址失败");
            }
        }

        private async Task<PlayerOpenResult> PlayLocalFile(bool preservePlaybackState = false)
        {
            VideoLoading.Visibility = Visibility.Visible;
            PlayerOpenResult result = new PlayerOpenResult()
            {
                result = false
            };
            var info = CurrentPlayItem.LocalPlayInfo.Info;
            if (info.PlayUrlType == BiliPlayUrlType.DASH)
            {
                //如果设置是系统播放器则优先系统
                var priority = SettingHelper.GetValue<int>("playertype", 0) == 1;
                var hasExternalAudio = CurrentPlayItem.LocalPlayInfo.IsManifestPlayback && CurrentPlayItem.LocalPlayInfo.SelectedAudioTrack != null;
                if (priority && !hasExternalAudio)
                {
                    result = await Player.PlayDashUseNative(info.DashInfo, "", "", position: _position, isLocal: true);
                }
                if (!result.result)
                    result = await Player.PlayDashUseFFmpegInterop(info.DashInfo, "", "", positon: _position, isLocal: true);
            }
            else if (CurrentPlayItem.LocalPlayInfo.Info.PlayUrlType == BiliPlayUrlType.SingleFLV)
            {
                result = await Player.PlayerSingleMp4UseNativeAsync(info.FlvInfo.First().Url, positon: _position, isLocal: true);
            }
            else if (CurrentPlayItem.LocalPlayInfo.Info.PlayUrlType == BiliPlayUrlType.MultiFLV)
            {
                //TODO 本地播放
                result = await Player.PlayVideoUseSYEngine(info.FlvInfo, "", "", positon: _position, epId: CurrentPlayItem.ep_id, isLocal: true);
            }
            if (result.result)
            {
                VideoLoading.Visibility = Visibility.Collapsed;
                if (!preservePlaybackState)
                    Player.Play();
            }
            else
            {
                if (CurrentPlayItem.LocalPlayInfo.IsManifestPlayback)
                {
                    LogHelper.Log("Local media playback failed; stage=player-open; collectionId=" +
                        CurrentPlayItem.LocalPlayInfo.ManifestCollectionId + "; itemId=" +
                        CurrentPlayItem.LocalPlayInfo.ManifestItem.Id + "; detail=" + result.detail_message +
                        "; message=" + result.message,
                        LogType.ERROR);
                }
                if (!preservePlaybackState)
                    ShowErrorDialog(result.message + "[LocalFile]");
            }
            return result;
        }

        private async Task GetPlayerInfo()
        {
            TopOnline.Text = "";
            if (CurrentPlayItem.LocalPlayInfo?.IsManifestPlayback == true)
            {
                var menu = new MenuFlyout();
                var subtitleFiles = CurrentPlayItem.LocalPlayInfo.SubtitleFiles ?? new List<LocalMediaPlaybackTrack>();
                foreach (var track in subtitleFiles)
                {
                    var subtitleItem = new ToggleMenuFlyoutItem
                    {
                        Text = track.Track.Label ?? track.Track.Language ?? track.Track.Id,
                        Tag = track,
                        IsChecked = track.Track.IsDefault
                    };
                    subtitleItem.Click += Menuitem_Click;
                    menu.Items.Add(subtitleItem);
                }
                var selectedSubtitle = subtitleFiles.FirstOrDefault(track => track.Track.IsDefault);
                var noneItem = new ToggleMenuFlyoutItem { Text = "无", IsChecked = selectedSubtitle == null };
                noneItem.Click += Menuitem_Click;
                menu.Items.Add(noneItem);
                CurrentSubtitleName = selectedSubtitle == null ? "无" : (selectedSubtitle.Track.Label ?? selectedSubtitle.Track.Id);
                BottomBtnSelctSubtitle.Flyout = menu;
                BottomBtnSelctSubtitle.Visibility = Visibility.Collapsed;
                LocalSubtitleTracksButton.Visibility = subtitleFiles.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                BorderSubtitle.Visibility = Visibility.Collapsed;
                var danmakuMenu = new MenuFlyout();
                foreach (var track in CurrentPlayItem.LocalPlayInfo.DanmakuFiles ?? new List<LocalMediaPlaybackTrack>())
                {
                    var danmakuItem = new ToggleMenuFlyoutItem
                    {
                        Text = track.Track.Label ?? track.Track.Language ?? track.Track.Id,
                        Tag = track,
                        IsChecked = _enabledManifestDanmaku.Contains(track.Track.Id)
                    };
                    danmakuItem.Click += ManifestDanmakuTrack_Click;
                    danmakuMenu.Items.Add(danmakuItem);
                }
                LocalDanmakuTracksButton.Tag = danmakuMenu;
                if (selectedSubtitle != null)
                {
                    _subtitleTicket = _subtitleLoadGuard.Begin(CurrentPlayItem);
                    LoadManifestSubtitleAsync(selectedSubtitle, CurrentPlayItem, _subtitleTicket);
                }
                TopOnline.Text = "本地媒体";
                return;
            }
            var autoAISubtitle = SettingHelper.GetValue<bool>(SettingHelper.Player.AUTO_OPEN_AI_SUBTITLE, false);
            LocalSubtitleTracksButton.Visibility = Visibility.Collapsed;
            if (CurrentPlayItem.play_mode == VideoPlayType.Download)
            {
                if (CurrentPlayItem.LocalPlayInfo.Subtitles != null && CurrentPlayItem.LocalPlayInfo.Subtitles.Count > 0)
                {
                    var menu = new MenuFlyout();
                    foreach (var item in CurrentPlayItem.LocalPlayInfo.Subtitles)
                    {
                        ToggleMenuFlyoutItem menuitem = new ToggleMenuFlyoutItem() { Text = item.Key, Tag = item.Value };
                        menuitem.Click += Menuitem_Click;
                        menu.Items.Add(menuitem);
                    }
                    ToggleMenuFlyoutItem noneItem = new ToggleMenuFlyoutItem() { Text = "无" };
                    noneItem.Click += Menuitem_Click;
                    menu.Items.Add(noneItem);
                    SelectInitialSubtitle(menu, noneItem, autoAISubtitle, "download");
                    BottomBtnSelctSubtitle.Flyout = menu;
                    BottomBtnSelctSubtitle.Visibility = Visibility.Visible;
                    BorderSubtitle.Visibility = Visibility.Collapsed;
                }
                else
                {
                    var menu = new MenuFlyout();
                    menu.Items.Add(new ToggleMenuFlyoutItem() { Text = "无", IsChecked = true });
                    CurrentSubtitleName = "无";
                    BottomBtnSelctSubtitle.Flyout = menu;
                    BottomBtnSelctSubtitle.Visibility = Visibility.Collapsed;
                    BorderSubtitle.Visibility = Visibility.Collapsed;
                }
                return;
            }
            var player_info = await playerHelper.GetPlayInfo(CurrentPlayItem.avid, CurrentPlayItem.cid);
            if (player_info.subtitle != null && player_info.subtitle.subtitles != null && player_info.subtitle.subtitles.Count != 0)
            {
                var menu = new MenuFlyout();
                foreach (var item in player_info.subtitle.subtitles)
                {
                    ToggleMenuFlyoutItem menuitem = new ToggleMenuFlyoutItem() { Text = $"{item.lan_doc}({item.lan})", Tag = item.subtitle_url };
                    menuitem.Click += Menuitem_Click;
                    menu.Items.Add(menuitem);
                }
                ToggleMenuFlyoutItem noneItem = new ToggleMenuFlyoutItem() { Text = "无" };
                noneItem.Click += Menuitem_Click;
                menu.Items.Add(noneItem);
                SelectInitialSubtitle(menu, noneItem, autoAISubtitle, "api");

                BottomBtnSelctSubtitle.Flyout = menu;
                BottomBtnSelctSubtitle.Visibility = Visibility.Visible;
                BorderSubtitle.Visibility = Visibility.Collapsed;
            }
            else
            {
                var menu = new MenuFlyout();
                menu.Items.Add(new ToggleMenuFlyoutItem() { Text = "无", IsChecked = true });
                CurrentSubtitleName = "无";
                BottomBtnSelctSubtitle.Flyout = menu;
                BottomBtnSelctSubtitle.Visibility = Visibility.Collapsed;
                BorderSubtitle.Visibility = Visibility.Collapsed;
            }

            if (player_info.interaction != null)
            {
                //设置互动视频
                if (interactionVideoVM == null)
                {
                    interactionVideoVM = new InteractionVideoVM(CurrentPlayItem.avid, player_info.interaction.graph_version);
                    NodeList.DataContext = interactionVideoVM;
                    InteractionChoices.DataContext = interactionVideoVM;
                    ShowPlaylistButton = false;
                    await interactionVideoVM.GetNodes();
                    TopTitle.Text = interactionVideoVM.Select.title;
                }
            }

            TopOnline.Text = await playerHelper.GetOnline(CurrentPlayItem.avid, CurrentPlayItem.cid);
        }

        private BiliPlayUrlInfo current_quality_info = null;

        private async Task ChangeQuality(BiliPlayUrlInfo quality)
        {
            VideoLoading.Visibility = Visibility.Visible;
            if (quality == null)
            {
                return;
            }
            current_quality_info = quality;
            if (!quality.HasPlayUrl)
            {
                var info = await playerHelper.GetPlayUrls(CurrentPlayItem, quality.QualityID);
                if (!info.Success)
                {
                    ShowDialog(info.Message, "切换清晰度失败");
                    return;
                }
                if (!info.CurrentQuality.HasPlayUrl)
                {
                    ShowDialog("无法读取到播放地址，试试换个清晰度?", "播放失败");
                    return;
                }
                quality = info.CurrentQuality;
            }
            PlayerOpenResult result = new PlayerOpenResult()
            {
                result = false
            };
            if (quality.PlayUrlType == BiliPlayUrlType.DASH)
            {
                var audio = quality.DashInfo.Audio;
                var video = quality.DashInfo.Video;

                result = await Player.PlayDashUseNative(quality.DashInfo, quality.UserAgent, quality.Referer, position: _position, isLocal: false);

                if (!result.result)
                {
                    var mpd_url = new Api.PlayerAPI().GenerateMPD(new Models.GenerateMPDModel()
                    {
                        AudioBandwidth = audio.BandWidth.ToString(),
                        AudioCodec = audio.Codecs,
                        AudioID = audio.ID.ToString(),
                        AudioUrl = audio.Url,
                        Duration = quality.DashInfo.Duration,
                        DurationMS = quality.Timelength,
                        VideoBandwidth = video.BandWidth.ToString(),
                        VideoCodec = video.Codecs,
                        VideoID = video.ID.ToString(),
                        VideoFrameRate = video.FrameRate.ToString(),
                        VideoHeight = video.Height,
                        VideoWidth = video.Width,
                        VideoUrl = video.Url,
                    });
                    result = await Player.PlayDashUrlUseFFmpegInterop(mpd_url, quality.UserAgent, quality.Referer, positon: _position);
                    //result = await Player.PlayDashUseFFmpegInterop(quality.playUrlInfo.dash_video_url, quality.playUrlInfo.dash_audio_url, quality.HttpHeader, positon: _postion);
                    //if (!result.result)
                    //{
                    //    result = await Player.PlayDashUseFFmpegInterop(quality.playUrlInfo.dash_video_url, quality.playUrlInfo.dash_audio_url, positon: _postion);
                    //}
                }
            }
            else if (quality.PlayUrlType == BiliPlayUrlType.SingleFLV)
            {
                result = await Player.PlaySingleFlvUseSYEngine(quality.FlvInfo.First().Url, quality.UserAgent, quality.Referer, positon: _position, epId: CurrentPlayItem.ep_id);
                if (!result.result)
                {
                    result = await Player.PlaySingleFlvUseFFmpegInterop(quality.FlvInfo.First().Url, quality.UserAgent, quality.Referer, positon: _position);
                }
            }
            //else if (quality.PlayUrlType == VideoPlayMode.SingleMp4)
            //{
            //    result = await Player.PlayerSingleMp4UseNativeAsync(quality.playUrlInfo.url, positon: _postion);
            //    if (!result.result)
            //    {
            //        result = await Player.PlaySingleFlvUseSYEngine(quality.playUrlInfo.url, quality.HttpHeader, positon: _postion);
            //    }
            //}
            else if (quality.PlayUrlType == BiliPlayUrlType.MultiFLV)
            {
                result = await Player.PlayVideoUseSYEngine(quality.FlvInfo, quality.UserAgent, quality.Referer, positon: _position, epId: CurrentPlayItem.ep_id);
            }
            if (result.result)
            {
                //var text = $"AID:{CurrentPlayItem.avid}\r\nCID:{CurrentPlayItem.cid}\r\nSeasonID:{CurrentPlayItem.season_id}\r\n";
                //txtInfo.Text = Player.MediaInfo;
                VideoLoading.Visibility = Visibility.Collapsed;
            }
            else
            {
                ShowErrorDialog(result.message + "[ChangeQuality]");
            }
        }

        private void ShowErrorDialog(string message)
        {
            ShowDialog($@"播放失败:{message}
你可以进行以下尝试:
1、切换视频清晰度
2、到⌈设置⌋-⌈播放⌋中修改⌈优先视频编码⌋选项
3、到⌈设置⌋-⌈播放⌋中打开或关闭⌈替换PCDN链接⌋选项
4、到⌈设置⌋-⌈代理⌋中打开或关闭⌈尝试替换视频的CDN⌋选项
5、如果视频编码选择了HEVC，请检查是否安装了HEVC扩展
6、如果视频编码选择了AV1，请检查是否安装了AV1扩展
7、如果是付费视频，请在手机或网页端购买后观看
8、尝试更新您的显卡驱动或使用核显打开应用", "播放失败");
        }

        private async void ShowDialog(string content, string title)
        {
            MessageDialog dislog = new MessageDialog(content, title);
            await dislog.ShowAsync();
        }

        #region 全屏处理

        public void FullScreen(bool fullScreen)
        {
            ApplicationView view = ApplicationView.GetForCurrentView();
            FullScreenEvent?.Invoke(this, fullScreen);
            if (fullScreen)
            {
                BottomBtnExitFull.Visibility = Visibility.Visible;
                BottomBtnFull.Visibility = Visibility.Collapsed;
                BottomBtnFullWindows.Visibility = Visibility.Collapsed;
                BottomBtnExitFullWindows.Visibility = Visibility.Collapsed;
                //全屏
                if (!view.IsFullScreenMode)
                {
                    view.TryEnterFullScreenMode();
                }
            }
            else
            {
                BottomBtnExitFull.Visibility = Visibility.Collapsed;
                BottomBtnFull.Visibility = Visibility.Visible;
                if (IsFullWindow)
                {
                    BottomBtnFullWindows.Visibility = Visibility.Collapsed;
                    BottomBtnExitFullWindows.Visibility = Visibility.Visible;
                }
                else
                {
                    BottomBtnFullWindows.Visibility = Visibility.Visible;
                    BottomBtnExitFullWindows.Visibility = Visibility.Collapsed;
                }
                //退出全屏
                if (view.IsFullScreenMode)
                {
                    view.ExitFullScreenMode();
                }
            }
            BtnFoucs.Focus(FocusState.Programmatic);
        }

        public void FullWidnow(bool fullWindow)
        {
            if (fullWindow)
            {
                BottomBtnFullWindows.Visibility = Visibility.Collapsed;
                BottomBtnExitFullWindows.Visibility = Visibility.Visible;
            }
            else
            {
                BottomBtnFullWindows.Visibility = Visibility.Visible;
                BottomBtnExitFullWindows.Visibility = Visibility.Collapsed;
            }
            FullWindowEvent?.Invoke(this, fullWindow);
            this.Focus(FocusState.Programmatic);
        }

        private void BottomBtnExitFull_Click(object sender, RoutedEventArgs e)
        {
            IsFullScreen = false;
        }

        private void BottomBtnFull_Click(object sender, RoutedEventArgs e)
        {
            IsFullScreen = true;
        }

        private void BottomBtnExitFullWindows_Click(object sender, RoutedEventArgs e)
        {
            IsFullWindow = false;
        }

        private void BottomBtnFullWindows_Click(object sender, RoutedEventArgs e)
        {
            IsFullWindow = true;
        }

        public bool IsFullScreen
        {
            get { return (bool)GetValue(IsFullScreenProperty); }
            set { SetValue(IsFullScreenProperty, value); }
        }

        public static readonly DependencyProperty IsFullScreenProperty =
            DependencyProperty.Register("IsFullScreen", typeof(bool), typeof(PlayerControl), new PropertyMetadata(false, OnIsFullScreenChanged));

        private static void OnIsFullScreenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as PlayerControl;
            sender.FullScreen((bool)e.NewValue);
        }

        public bool IsFullWindow
        {
            get { return (bool)GetValue(IsFullWindowProperty); }
            set { SetValue(IsFullWindowProperty, value); }
        }

        public static readonly DependencyProperty IsFullWindowProperty =
            DependencyProperty.Register("IsFullWindow", typeof(bool), typeof(PlayerControl), new PropertyMetadata(false, OnIsFullWidnowChanged));

        private static void OnIsFullWidnowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as PlayerControl;
            sender.FullWidnow((bool)e.NewValue);
        }

        #endregion 全屏处理

        public bool ShowPlaylistButton
        {
            get { return (bool)GetValue(ShowPlaylistButtonProperty); }
            set { SetValue(ShowPlaylistButtonProperty, value); }
        }

        public static readonly DependencyProperty ShowPlaylistButtonProperty =
            DependencyProperty.Register("ShowPlaylistButton", typeof(bool), typeof(PlayerControl), new PropertyMetadata(false));

        public bool ShowPlayNodeButton
        {
            get { return (bool)GetValue(ShowPlayNodeButtonProperty); }
            set { SetValue(ShowPlayNodeButtonProperty, value); }
        }

        public static readonly DependencyProperty ShowPlayNodeButtonProperty =
            DependencyProperty.Register("ShowPlayNodeButton", typeof(bool), typeof(PlayerControl), new PropertyMetadata(false));

        private bool _buffering = false;

        public bool Buffering
        {
            get { return _buffering; }
            set { _buffering = value; DoPropertyChanged("Buffering"); }
        }

        private double _BufferingProgress;

        public double BufferingProgress
        {
            get { return _BufferingProgress; }
            set { _BufferingProgress = value; DoPropertyChanged("BufferingProgress"); }
        }

        private void TopBtnOpenDanmaku_Click(object sender, RoutedEventArgs e)
        {
            DanmuControl.Visibility = Visibility.Visible;
            SettingHelper.SetValue<Visibility>(SettingHelper.VideoDanmaku.SHOW, DanmuControl.Visibility);
        }

        private void TopBtnCloseDanmaku_Click(object sender, RoutedEventArgs e)
        {
            DanmuControl.Visibility = Visibility.Collapsed;
            SettingHelper.SetValue<Visibility>(SettingHelper.VideoDanmaku.SHOW, DanmuControl.Visibility);
        }

        #region 播放器手势

        private int showControlsFlag = 0;

        private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            //ShowControl(true);
            ////FadeIn.Begin();
            ////control.Visibility = Visibility.Visible;
            pointer_in_player = true;
            //showControlsFlag = 0;
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            //showControlsFlag = 3;
            pointer_in_player = false;
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
        }

        private void Grid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            //showControlsFlag = 0;
            //ShowControl(true);
            ////control.Visibility = Visibility.Visible;
            if (Window.Current.CoreWindow.PointerCursor == null)
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
            }
            gestureRecognizer.ProcessMoveEvents(e.GetIntermediatePoints(null));
            e.Handled = true;
        }

        private bool tapFlag;

        private async void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            tapFlag = true;
            await Task.Delay(200);
            //if (control.Visibility == Visibility.Visible)
            //{
            //    if (e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse&& !Player.Opening)
            //    {
            //        if (Player.PlayState == PlayState.Pause || Player.PlayState == PlayState.End)
            //        {
            //            Player.Play();
            //        }
            //        else if (Player.PlayState == PlayState.Playing)
            //        {
            //            Pause();
            //        }
            //    }

            //}
            if (!tapFlag) return;
            ShowControl(control.Visibility == Visibility.Collapsed);
        }

        private void Grid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            tapFlag = false;
            var fullScreen = SettingHelper.GetValue<bool>(SettingHelper.Player.DOUBLE_CLICK_FULL_SCREEN, false);
            if (!fullScreen)
            {
                if (Player.PlayState == PlayState.Pause || Player.PlayState == PlayState.End)
                {
                    Player.Play();
                }
                else if (Player.PlayState == PlayState.Playing)
                {
                    Pause();
                }
            }
            else
            {
                IsFullScreen = !IsFullScreen;
            }
        }

        private double ssValue = 0;
        private bool ManipulatingBrightness = false;

        private void HandleSlideProgressDelta(double delta)
        {
            if (Player.PlayState != PlayState.Playing && Player.PlayState != PlayState.Pause)
                return;

            if (delta > 0)
            {
                double dd = delta / this.ActualWidth;
                double d = dd * 90;
                ssValue += d;
                //slider.Value += d;
            }
            else
            {
                double dd = Math.Abs(delta) / this.ActualWidth;
                double d = dd * 90;
                ssValue -= d;
                //slider.Value -= d;
            }
            var pos = Player.Position;
            pos += ssValue;

            if (pos < 0)
                pos = 0;
            else if (pos > Player.Duration)
                pos = Player.Duration;
            //txt_Post.Text = ts.Hours.ToString("00") + ":" + ts.Minutes.ToString("00") + ":" + ts.Seconds.ToString("00") + "/" + mediaElement.MediaPlayer.PlaybackSession.NaturalDuration.TimeSpan.Hours.ToString("00") + ":" + mediaElement.MediaPlayer.PlaybackSession.NaturalDuration.TimeSpan.Minutes.ToString("00") + ":" + mediaElement.MediaPlayer.PlaybackSession.NaturalDuration.TimeSpan.Seconds.ToString("00");

            TxtToolTip.Text = TimeSpan.FromSeconds(pos).ToString(@"hh\:mm\:ss");
            //Utils.ShowMessageToast(ts.Hours.ToString("00") + ":" + ts.Minutes.ToString("00") + ":" + ts.Seconds.ToString("00"), 3000);
        }

        private void HandleSlideVolumeDelta(double delta)
        {
            if (delta > 0)
            {
                double dd = delta / (this.ActualHeight * 0.8);

                //slider_V.Value -= d;
                var volume = Player.Volume - dd;
                Player.Volume = volume;
            }
            else
            {
                double dd = Math.Abs(delta) / (this.ActualHeight * 0.8);
                var volume = Player.Volume + dd;
                Player.Volume = volume;
                //slider_V.Value += d;
            }
            TxtToolTip.Text = "音量:" + Player.Volume.ToString("P");
            //Utils.ShowMessageToast("音量:" +  mediaElement.MediaPlayer.Volume.ToString("P"), 3000);
        }

        private void HandleSlideBrightnessDelta(double delta)
        {
            double dd = Math.Abs(delta) / (this.ActualHeight * 0.8);
            if (delta > 0)
            {
                Brightness = Math.Min(Brightness + dd, 1);
            }
            else
            {
                Brightness = Math.Max(Brightness - dd, 0);
            }
            TxtToolTip.Text = "亮度:" + Math.Abs(Brightness - 1).ToString("P");
        }

        private double _brightness = 0;

        #endregion 播放器手势

        private bool HandlingGesture = false;
        private bool HandlingHolding = false;
        private bool DirectionX = false;
        private bool DirectionY = false;

        private double Brightness
        {
            get => _brightness;
            set
            {
                _brightness = value;
                BrightnessShield.Opacity = value;
                //SettingHelper.SetValue<double>(SettingHelper.Player.PLAYER_BRIGHTNESS, _brightness);
                //}
            }
        }

        private GestureRecognizer gestureRecognizer;

        private void InitializeGesture()
        {
            gestureRecognizer.GestureSettings = GestureSettings.Hold | GestureSettings.HoldWithMouse | GestureSettings.ManipulationTranslateX | GestureSettings.ManipulationTranslateY;

            gestureRecognizer.Holding += OnHolding;
            gestureRecognizer.ManipulationStarted += OnManipulationStarted;
            gestureRecognizer.ManipulationUpdated += OnManipulationUpdated;
            gestureRecognizer.ManipulationCompleted += OnManipulationCompleted;
        }

        private async void OnHolding(GestureRecognizer sender, HoldingEventArgs args)
        {
            if (Player.PlayState != PlayState.Playing)
                return;

            switch (args.HoldingState)
            {
                case HoldingState.Started:
                    {
                        HandlingHolding = true;
                        TxtToolTip.Text = "3x";
                        ToolTip.Visibility = Visibility.Visible;
                        Player.SetRate(3.0d);
                        await Task.Delay(3000);
                        ToolTip.Visibility = Visibility.Collapsed;
                        break;
                    }
                case HoldingState.Completed:
                    {
                        HandlingHolding = false;
                        ToolTip.Visibility = Visibility.Collapsed;
                        Player.SetRate(SettingHelper.GetValue<double>(SettingHelper.Player.DEFAULT_VIDEO_SPEED, 1.0d));
                        break;
                    }
                case HoldingState.Canceled:
                    {
                        HandlingHolding = false;
                        ToolTip.Visibility = Visibility.Collapsed;
                        Player.SetRate(SettingHelper.GetValue<double>(SettingHelper.Player.DEFAULT_VIDEO_SPEED, 1.0d));
                        break;
                    }
            }
        }

        private void OnManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            ssValue = 0;
            //TxtToolTip.Text = "";
            ToolTip.Visibility = Visibility.Visible;

            if (e.Position.X < this.ActualWidth / 2)
                ManipulatingBrightness = true;
            else
                ManipulatingBrightness = false;
        }

        private void OnManipulationUpdated(object sender, ManipulationUpdatedEventArgs e)
        {
            var x = e.Delta.Translation.X;
            var y = e.Delta.Translation.Y;

            if (HandlingHolding)
                return;
            if (HandlingGesture == false)
            {
                if (Math.Abs(x) > Math.Abs(y))
                {
                    HandlingGesture = true;
                    DirectionX = true;

                    HandleSlideProgressDelta(e.Delta.Translation.X);
                }
                else
                {
                    HandlingGesture = true;
                    DirectionY = true;

                    if (ManipulatingBrightness)
                        HandleSlideBrightnessDelta(e.Delta.Translation.Y);
                    else
                        HandleSlideVolumeDelta(e.Delta.Translation.Y);
                }
            }
            else
            {
                if (DirectionX)
                {
                    HandleSlideProgressDelta(x);
                }
                if (DirectionY)
                {
                    if (ManipulatingBrightness)
                        HandleSlideBrightnessDelta(y);
                    else
                        HandleSlideVolumeDelta(y);
                }
            }
        }

        private void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            HandlingGesture = false;
            DirectionX = false;
            DirectionY = false;
            if (ssValue != 0)
            {
                Player.Position = Player.Position + ssValue;
            }
            ToolTip.Visibility = Visibility.Collapsed;
        }

        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var ps = e.GetIntermediatePoints(null);
            if (ps != null && ps.Count > 0 && HandlingGesture != true)
            {
                try
                {
                    var pointerUpdateKind = e.GetCurrentPoint(sender as UIElement).Properties.PointerUpdateKind;
                    if (SettingHelper.GetValue<bool>(SettingHelper.UI.MOUSE_BACK, true)
                        && (pointerUpdateKind == Windows.UI.Input.PointerUpdateKind.XButton1Pressed
                            || pointerUpdateKind == Windows.UI.Input.PointerUpdateKind.MiddleButtonPressed))
                        return;
                    gestureRecognizer.ProcessDownEvent(ps[0]);
                    e.Handled = true;
                }
                catch (Exception ex) { }
            }
        }

        private void Grid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var pointerUpdateKind = e.GetCurrentPoint(sender as UIElement).Properties.PointerUpdateKind;
            if (SettingHelper.GetValue<bool>(SettingHelper.UI.MOUSE_BACK, true)
                && (pointerUpdateKind == Windows.UI.Input.PointerUpdateKind.XButton1Released
                    || pointerUpdateKind == Windows.UI.Input.PointerUpdateKind.MiddleButtonReleased))
                return;
            var ps = e.GetIntermediatePoints(null);
            if (ps != null && ps.Count > 0)
            {
                gestureRecognizer.ProcessUpEvent(ps[0]);
                e.Handled = true;
                gestureRecognizer.CompleteGesture();
            }
        }

        private void BottomBtnList_Click(object sender, RoutedEventArgs e)
        {
            NodeList.Visibility = Visibility.Collapsed;
            EpisodeList.Visibility = Visibility.Visible;
            SettingPivot.SelectedIndex = 0;
            SplitView.IsPaneOpen = true;
        }

        private void BottomBtnNode_Click(object sender, RoutedEventArgs e)
        {
            NodeList.Visibility = Visibility.Visible;
            EpisodeList.Visibility = Visibility.Collapsed;
            SettingPivot.SelectedIndex = 0;
            SplitView.IsPaneOpen = true;
        }

        private void TopBtnSettingDanmaku_Click(object sender, RoutedEventArgs e)
        {
            SettingPivot.SelectedIndex = 2;
            SplitView.IsPaneOpen = true;
        }

        private void TopBtnMore_Click(object sender, RoutedEventArgs e)
        {
            SettingPivot.SelectedIndex = CurrentPlayItem?.LocalPlayInfo?.IsManifestPlayback == true ? 1 : 3;
            SplitView.IsPaneOpen = true;
        }

        private async void BottomCBQuality_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BottomCBQuality.SelectedItem == null)
            {
                return;
            }

            _position = Player.Position;
            var data = BottomCBQuality.SelectedItem as BiliPlayUrlInfo;
            SettingHelper.SetValue<int>(SettingHelper.Player.DEFAULT_QUALITY, data.QualityID);
            _autoPlay = Player.PlayState == PlayState.Playing;
            await ChangeQuality(data);
        }

        private void BottomBtnPause_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Opening)
            {
                return;
            }
            Pause();
        }

        private void BottomBtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Opening)
            {
                return;
            }
            if (Player.PlayState == PlayState.Pause || Player.PlayState == PlayState.End)
            {
                Player.Play();
                DanmuControl.ResumeDanmaku();
            }
        }

        private void BottomBtnNext_Click(object sender, RoutedEventArgs e)
        {
            EpisodeList.SelectedIndex = EpisodeList.SelectedIndex + 1;
        }

        private void KeepScreenOn(bool value = true)
        {
            try
            {
                if (dispRequest != null)
                {
                    if (value)
                    {
                        dispRequest.RequestActive();
                    }
                    else
                    {
                        dispRequest.RequestRelease();
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void Player_PlayStateChanged(object sender, PlayState e)
        {
            if (CurrentPlayItem?.LocalPlayInfo?.IsManifestPlayback == true)
            {
                if (e == PlayState.Playing) _autoPlay = true;
                else if (e == PlayState.Pause && !Player.Opening) _autoPlay = false;
            }
            if (e == PlayState.Playing)
            {
                _hasPlayedCurrentItem = true;
            }
            BottomImageBtnPlay.Visibility = Visibility.Collapsed;
            switch (e)
            {
                case PlayState.Playing:
                    KeepScreenOn(true);
                    break;

                default:
                    KeepScreenOn(false);
                    break;
            }
            switch (e)
            {
                case PlayState.Loading:
                    if (_systemMediaTransportControls != null)
                    {
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Changing;
                    }
                    BottomBtnLoading.Visibility = Visibility.Visible;
                    BottomBtnPlay.Visibility = Visibility.Collapsed;
                    BottomBtnPause.Visibility = Visibility.Collapsed;
                    break;

                case PlayState.Playing:
                    if (_systemMediaTransportControls != null)
                    {
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                    }
                    BottomBtnLoading.Visibility = Visibility.Collapsed;
                    BottomBtnPlay.Visibility = Visibility.Collapsed;
                    BottomBtnPause.Visibility = Visibility.Visible;
                    DanmuControl.ResumeDanmaku();
                    break;

                case PlayState.Pause:
                    if (_systemMediaTransportControls != null)
                    {
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                    }
                    BottomImageBtnPlay.Visibility = Visibility.Visible;
                    BottomBtnLoading.Visibility = Visibility.Collapsed;
                    BottomBtnPlay.Visibility = Visibility.Visible;
                    BottomBtnPause.Visibility = Visibility.Collapsed;
                    DanmuControl.PauseDanmaku();
                    break;

                case PlayState.End:
                    if (_systemMediaTransportControls != null)
                    {
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
                    }
                    BottomBtnLoading.Visibility = Visibility.Collapsed;
                    BottomBtnPlay.Visibility = Visibility.Visible;
                    BottomBtnPause.Visibility = Visibility.Collapsed;
                    break;

                case PlayState.Error:
                    if (_systemMediaTransportControls != null)
                    {
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
                    }
                    BottomBtnLoading.Visibility = Visibility.Visible;
                    BottomBtnPlay.Visibility = Visibility.Collapsed;
                    BottomBtnPause.Visibility = Visibility.Collapsed;
                    break;

                default:
                    break;
            }
        }

        private void Player_PlayBufferStart(object sender, EventArgs e)
        {
            Buffering = true;
            GridBuffering.Visibility = Visibility.Visible;
            TxtBuffering.Text = "正在缓冲...";
            BufferingProgress = 0;
            DanmuControl.PauseDanmaku();
        }

        private void Player_PlayBuffering(object sender, double e)
        {
            Buffering = true;
            GridBuffering.Visibility = Visibility.Visible;
            TxtBuffering.Text = "正在缓冲" + e.ToString("p");
            BufferingProgress = e;
        }

        private void Player_PlayBufferEnd(object sender, EventArgs e)
        {
            GridBuffering.Visibility = Visibility.Collapsed;
            Buffering = false;
            DanmuControl.ResumeDanmaku();
        }

        private async void Player_PlayMediaEnded(object sender, EventArgs e)
        {
            if (CurrentPlayItem.is_interaction)
            {
                if (interactionVideoVM.Info.is_leaf == 1)
                {
                    Utils.ShowMessageToast("播放完毕，请点击右下角节点，重新开始");
                    return;
                }
                DanmuControl.PauseDanmaku();
                InteractionChoices.Visibility = Visibility.Visible;
                return;
            }
            if (CurrentPlayItem.LocalPlayInfo?.IsManifestPlayback != true)
                await playerHelper.ReportHistory(CurrentPlayItem, Player.Duration);
            //列表顺序播放
            if (PlayerSettingPlayMode.SelectedIndex == 0)
            {
                if (CurrentPlayIndex == PlayInfos.Count - 1)
                {
                    if (AllMediaEndEvent != null)
                    {
                        AllMediaEndEvent?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        Utils.ShowMessageToast("播放完毕");
                    }
                }
                else
                {
                    if (PlayerSettingAutoNext.IsOn)
                    {
                        _autoPlay = true;
                        ChangePlayIndex(CurrentPlayIndex + 1);
                    }
                    else
                    {
                        Utils.ShowMessageToast("本P播放完成");
                    }
                }
                return;
            }
            //单P循环
            if (PlayerSettingPlayMode.SelectedIndex == 1)
            {
                ClearSubTitle();
                DanmuControl.ClearAll();
                Player.Play();
                return;
            }
            //列表循环播放
            if (PlayerSettingPlayMode.SelectedIndex == 2)
            {
                if (!PlayerSettingAutoNext.IsOn)
                {
                    Utils.ShowMessageToast("本P播放完成");
                    return;
                }
                //只有一P,重新播放
                if (PlayInfos.Count == 1)
                {
                    ClearSubTitle();
                    DanmuControl.ClearAll();
                    Player.Play();
                    return;
                }
                _autoPlay = true;
                if (CurrentPlayIndex == PlayInfos.Count - 1)
                {
                    ChangePlayIndex(0);
                }
                else
                {
                    ChangePlayIndex(CurrentPlayIndex + 1);
                }
                return;
            }
        }

        private void Player_PlayMediaError(object sender, string e)
        {
            ShowDialog(e, "播放失败");
        }

        private async void DanmuSettingUpdateDanmaku_Click(object sender, RoutedEventArgs e)
        {
            await SetDanmaku(true);
        }

        private async void TopBtnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            await CaptureVideo();
        }

        private async Task CaptureVideo()
        {
            try
            {
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss") + ".jpg";
                StorageFolder applicationFolder = KnownFolders.PicturesLibrary;
                StorageFolder folder = await applicationFolder.CreateFolderAsync("哔哩哔哩截图", CreationCollisionOption.OpenIfExists);
                StorageFile saveFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
                RenderTargetBitmap bitmap = new RenderTargetBitmap();
                await bitmap.RenderAsync(Player);
                var pixelBuffer = await bitmap.GetPixelsAsync();
                using (var fileStream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                    encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Ignore,
                         (uint)bitmap.PixelWidth,
                         (uint)bitmap.PixelHeight,
                         displayInformation.LogicalDpi,
                         displayInformation.LogicalDpi,
                         pixelBuffer.ToArray());
                    await encoder.FlushAsync();
                }
                Utils.ShowMessageToast("截图已经保存至图片库");
            }
            catch (Exception)
            {
                Utils.ShowMessageToast("截图失败");
            }
        }

        private async void Player_ChangeEngine(object sender, ChangePlayerEngine e)
        {
            if (!e.need_change)
            {
                ShowErrorDialog(e.message + "[ChangeEngine]");
                return;
            }
            VideoLoading.Visibility = Visibility.Visible;
            PlayerOpenResult result = new PlayerOpenResult()
            {
                result = false,
                message = ""
            };
            if (e.play_type == PlayMediaType.Dash && e.change_engine == PlayEngine.FFmpegInteropMSS)
            {
                if (CurrentPlayItem.LocalPlayInfo?.IsManifestPlayback == true)
                {
                    var localInfo = CurrentPlayItem.LocalPlayInfo.Info;
                    var position = Player.Position > 0 ? Player.Position : _position;
                    _position = position;
                    result = await Player.PlayDashUseFFmpegInterop(localInfo.DashInfo, "", "", positon: position, isLocal: true);
                }
                else
                {
                    result = await Player.PlayDashUseFFmpegInterop(current_quality_info.DashInfo, current_quality_info.UserAgent, current_quality_info.Referer, positon: _position);
                }
            }
            if (e.play_type == PlayMediaType.Single && e.change_engine == PlayEngine.SYEngine)
            {
                result = await Player.PlaySingleFlvUseSYEngine(current_quality_info.FlvInfo.First().Url, current_quality_info.UserAgent, current_quality_info.Referer, positon: _position);
            }
            if (!result.result)
            {
                if (CurrentPlayItem.LocalPlayInfo?.IsManifestPlayback == true)
                {
                    var localInfo = CurrentPlayItem.LocalPlayInfo;
                    LogHelper.Log("Local media playback failed; stage=engine-change; collectionId=" +
                        localInfo.ManifestCollectionId + "; itemId=" + localInfo.ManifestItem.Id + "; videoTrackId=" +
                        (localInfo.SelectedVideoTrack?.Track?.Id ?? "unknown") + "; audioTrackId=" +
                        (localInfo.SelectedAudioTrack?.Track?.Id ?? "embedded") + "; message=" + result.message + "; detail=" + result.detail_message,
                        LogType.ERROR);
                }
                ShowDialog(result.message, "播放失败");
                return;
            }
        }

        private void Player_PlayMediaOpened(object sender, EventArgs e)
        {
            txtInfo.Text = Player.GetMediaInfo();
            VideoLoading.Visibility = Visibility.Collapsed;
            if (_position != 0 || _restoreLocalTrackPositionOnOpen)
            {
                Player.SetPosition(_position);
            }
            _restoreLocalTrackPositionOnOpen = false;
            if (_autoPlay)
            {
                Player.Play();
            }
        }

        private async void BottomBtnSendDanmakuWide_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPlayItem?.LocalPlayInfo?.IsManifestPlayback == true)
            {
                Utils.ShowMessageToast("本地媒体暂不支持发送弹幕");
                return;
            }
            Pause();
            SendDanmakuDialog sendDanmakuDialog = new SendDanmakuDialog(CurrentPlayItem.avid, CurrentPlayItem.cid, Player.Position);
            sendDanmakuDialog.DanmakuSended += new EventHandler<SendDanmakuModel>((obj, arg) =>
            {
                DanmuControl.AddDanmu(new DanmakuModel()
                {
                    color = NSDanmaku.Utils.ToColor(arg.color),
                    text = arg.text,
                    location = (DanmakuLocation)arg.location,
                    size = 25,
                    time = Player.Position
                }, true);
            });
            await sendDanmakuDialog.ShowAsync();
            Player.Play();
        }

        private async void DanmuSettingSyncWords_Click(object sender, RoutedEventArgs e)
        {
            await settingVM.SyncDanmuFilter();
        }

        private async void DanmuSettingAddWord_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(DanmuSettingTxtWord.Text))
            {
                Utils.ShowMessageToast("关键词不能为空");
                return;
            }
            settingVM.ShieldWords.Add(DanmuSettingTxtWord.Text);
            SettingHelper.SetValue(SettingHelper.VideoDanmaku.SHIELD_WORD, settingVM.ShieldWords);
            var result = await settingVM.AddDanmuFilterItem(DanmuSettingTxtWord.Text, 0);
            DanmuSettingTxtWord.Text = "";
            if (!result)
            {
                Utils.ShowMessageToast("已经添加到本地，但远程同步失败");
            }
        }

        public async void Dispose()
        {
            _subtitleLoadGuard.Invalidate();
            _danmakuLoadGuard.Invalidate();
            _playItemGeneration++;
            ClearSubTitle();
            var historyItem = CurrentPlayItem;
            var historyPosition = Player.Position;
            var shouldReportHistory = historyItem != null && historyItem.LocalPlayInfo?.IsManifestPlayback != true && _hasPlayedCurrentItem && Player.PlayState != PlayState.End;
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

        private void GridViewSelectColor_ItemClick(object sender, ItemClickEventArgs e)
        {
            SendDanmakuColorText.Text = e.ClickedItem.ToString();
        }

        private void SendDanmakuTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                SendDanmaku();
            }
        }

        private void SendDanmakuButton_Click(object sender, RoutedEventArgs e)
        {
            SendDanmaku();
        }

        private async void SendDanmaku()
        {
            if (CurrentPlayItem?.LocalPlayInfo?.IsManifestPlayback == true)
            {
                Utils.ShowMessageToast("本地媒体暂不支持发送弹幕");
                return;
            }
            int modeInt = 1;
            var location = DanmakuLocation.Scroll;
            if (SendDanmakuMode.SelectedIndex == 2)
            {
                modeInt = 4;
                location = DanmakuLocation.Bottom;
            }
            if (SendDanmakuMode.SelectedIndex == 1)
            {
                modeInt = 5;
                location = DanmakuLocation.Top;
            }
            var color = "16777215";
            if (SendDanmakuColorBorder.Background != null)
            {
                color = Convert.ToInt32((SendDanmakuColorBorder.Background as SolidColorBrush).Color.ToString().Replace("#FF", ""), 16).ToString();
            }

            var result = await playerHelper.SendDanmaku(CurrentPlayItem.avid, CurrentPlayItem.cid, SendDanmakuTextBox.Text, Convert.ToInt32(Player.Position), modeInt, color);
            if (result)
            {
                DanmuControl.AddDanmu(new DanmakuModel()
                {
                    color = NSDanmaku.Utils.ToColor(color),
                    text = SendDanmakuTextBox.Text,
                    location = location,
                    size = 25,
                    time = Player.Position
                }, true);
                SendDanmakuTextBox.Text = "";
            }
        }

        private bool miniWin = false;

        private void BottomBtnExitMiniWindows_Click(object sender, RoutedEventArgs e)
        {
            MiniWidnows(false);
        }

        private void BottomBtnMiniWindows_Click(object sender, RoutedEventArgs e)
        {
            MiniWidnows(true);
        }

        public async void MiniWidnows(bool mini)
        {
            miniWin = mini;
            ApplicationView view = ApplicationView.GetForCurrentView();
            FullWindowEvent?.Invoke(this, IsFullWindow);
            if (mini)
            {
                IsFullWindow = true;
                StandardControl.Visibility = Visibility.Collapsed;
                MiniControl.Visibility = Visibility.Visible;
                //处理CC字幕
                if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay))
                {
                    this.Margin = new Thickness(0, -40, 0, 0);
                    await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
                    SubtitleSettingSize.Value = 14;

                    DanmuControl.DanmakuSizeZoom = 0.5;
                    DanmuControl.DanmakuDuration = 6;
                    DanmuControl.ClearAll();
                }
            }
            else
            {
                this.Margin = new Thickness(0, 0, 0, 0);
                StandardControl.Visibility = Visibility.Visible;
                MiniControl.Visibility = Visibility.Collapsed;
                await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
                DanmuControl.DanmakuSizeZoom = SettingHelper.GetValue<double>(SettingHelper.VideoDanmaku.FONT_ZOOM, 1);
                DanmuControl.DanmakuDuration = SettingHelper.GetValue<int>(SettingHelper.VideoDanmaku.SPEED, 10);
                DanmuControl.ClearAll();
                DanmuControl.Visibility = SettingHelper.GetValue<Visibility>(SettingHelper.VideoDanmaku.SHOW, Visibility.Visible);
                SubtitleSettingSize.Value = SettingHelper.GetValue<double>(SettingHelper.Player.SUBTITLE_SIZE, 40);
            }
            BtnFoucs.Focus(FocusState.Programmatic);
            MessageCenter.SetMiniWindow(mini);
        }

        public void Pause()
        {
            DanmuControl.PauseDanmaku();
            Player.Pause();
        }

        public void PlayerSettingABPlaySetPointA_Click(object sender, RoutedEventArgs e)
        {
            if (Player.ABPlay != null)
            {
                Player.ABPlay = null;
                VideoPlayHistoryHelper.SetABPlayHistory(CurrentPlayItem, null);
                PlayerSettingABPlaySetPointA.Content = "设置A点";
                PlayerSettingABPlaySetPointB.Content = "设置B点";
                PlayerSettingABPlaySetPointB.Visibility = Visibility.Collapsed;

                Utils.ShowMessageToast("已取消设置A点");
            }
            else
            {
                Player.ABPlay = new VideoPlayHistoryHelper.ABPlayHistoryEntry()
                {
                    PointA = Player.Position
                };
                PlayerSettingABPlaySetPointA.Content = "A: " + TimeSpan.FromSeconds(Player.ABPlay.PointA).ToString(@"hh\:mm\:ss\.fff");
                PlayerSettingABPlaySetPointB.Visibility = Visibility.Visible;

                Utils.ShowMessageToast("已设置A点, 再次点击可取消设置");
            }
        }

        public void PlayerSettingABPlaySetPointB_Click(object sender, RoutedEventArgs e)
        {
            if (Player.ABPlay.PointB > 0 && Player.ABPlay.PointB != Double.MaxValue)
            {
                Player.ABPlay.PointB = double.MaxValue;
                PlayerSettingABPlaySetPointB.Content = "设置B点";

                Utils.ShowMessageToast("已取消设置B点");
            }
            else
            {
                if (Player.Position <= Player.ABPlay.PointA)
                {
                    Utils.ShowMessageToast("B点必须在A点之后");
                }
                else
                {
                    Player.ABPlay.PointB = Player.Position;
                    VideoPlayHistoryHelper.SetABPlayHistory(CurrentPlayItem, Player.ABPlay);
                    PlayerSettingABPlaySetPointB.Content = "B: " + TimeSpan.FromSeconds(Player.ABPlay.PointB).ToString(@"hh\:mm\:ss\.fff");

                    Utils.ShowMessageToast("已设置B点, 再次点击可取消设置");
                }
            }
        }

        private async void ImportDanmaku_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();

            //目前只允许b站的xml格式，以后可能添加json和ass以及其他xml
            picker.FileTypeFilter.Add(".xml");

            // Show the FileOpenPicker dialog
            var file = await picker.PickSingleFileAsync();

            // If a file was selected, play it in the MediaPlayer
            if (file != null)
            {
                ImportSingleDanmaku(file);
            }
        }

        private async void ImportSubtitle_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();

            //目前只允许b站的json格式，以后可能添加ass
            picker.FileTypeFilter.Add(".json");

            // Show the FileOpenPicker dialog
            var file = await picker.PickSingleFileAsync();

            // If a file was selected, play it in the MediaPlayer
            if (file != null)
            {
                ImportSingleSubtitle(file);
            }
        }

        private async void ImportSingleDanmaku(StorageFile file)
        {
            var item = CurrentPlayItem;
            var ticket = _danmakuLoadGuard.Begin(item);
            try
            {
                if (item?.LocalPlayInfo != null) item.LocalPlayInfo.DanmakuPath = file.Path;
                danmakuPool = new Dictionary<int, List<DanmakuModel>>();
                DanmuControl.ClearAll();
                await _danmakuLoadGuard.LoadAndApplyAsync(ticket, item, () => danmakuParse.ParseBiliBili(file), danmuList =>
                {
                    danmakuPool = danmuList.GroupBy(x => x.time_s).ToDictionary(x => x.Key, x => x.ToList());
                    TxtDanmuCount.Text = danmuList.Count.ToString();
                });
            }
            catch (Exception ex)
            {
                if (_danmakuLoadGuard.IsCurrent(ticket, item)) Utils.ShowMessageToast("加载弹幕文件失败", $"{ex.Message}");
            }
        }

        private async void ImportSingleSubtitle(StorageFile file)
        {
            var item = CurrentPlayItem;
            var ticket = _subtitleTicket = _subtitleLoadGuard.Begin(item);
            try
            {
                ClearSubTitle();
                await _subtitleLoadGuard.LoadAndApplyAsync(ticket, item, async () =>
                {
                    return JsonConvert.DeserializeObject<SubtitleModel>(await FileIO.ReadTextAsync(file));
                }, loaded =>
                {
                    subtitles = loaded;
                    if (subtitles == null) return;
                    //转为简体
                    if (SettingHelper.GetValue<bool>(SettingHelper.Roaming.TO_SIMPLIFIED, true) && CurrentSubtitleName != null && CurrentSubtitleName.StartsWith("中文（繁体）", StringComparison.Ordinal))
                    {
                        foreach (var cue in subtitles.body)
                        {
                            cue.content = Utils.ToSimplifiedChinese(cue.content);
                        }
                    }
                    subtitleTimer = new DispatcherTimer();
                    subtitleTimer.Interval = TimeSpan.FromMilliseconds(100);
                    subtitleTimer.Tick += SubtitleTimer_Tick;
                    subtitleTimer.Start();
                });
            }
            catch (Exception ex)
            {
                if (_subtitleLoadGuard.IsCurrent(ticket, item)) Utils.ShowMessageToast("加载字幕文件失败", $"{ex.Message}");
            }
        }
    }

    public class CompareDanmakuModel : IEqualityComparer<DanmakuModel>
    {
        public bool Equals(DanmakuModel x, DanmakuModel y)
        {
            return x.text == y.text;
        }

        public int GetHashCode(DanmakuModel obj)
        {
            return obj.text.GetHashCode();
        }
    }
}

using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Modules;
using BiliLite.Modules.User;
using System.IO;
using BiliLite.Services;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http.Filters;
using static BiliLite.Helpers.SettingHelper;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliLite.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SettingPage : BasePage
    {
        /// <summary>设置 ⇄ 令牌记录的接线：判定在 RefreshTokenPolicy，这里只读写设置。</summary>
        private static readonly RefreshTokenMirror refreshTokenMirror = new RefreshTokenMirror(
            key => SettingHelper.GetValue(key, ""), (key, value) => SettingHelper.SetValue(key, value));

        SettingVM settingVM;
        /// <summary>按存储值填充控件期间为 true：处理器直接返回，不写存储、不跑副作用。</summary>
        private bool _applyingStoredValues;

        public SettingPage()
        {
            this.InitializeComponent();
            Title = "设置";
            settingVM = new SettingVM();
            BindShieldLists();
            WireEvents();
            ApplyAppearance();
            ApplyPlayer();
            ApplyVideoDanmu();
            ApplyLiveDanmu();
            ApplyDownload();
            ApplyNetwork();
            ApplyDiagnostics();
            RefreshAccountState();
            ApplyAccountFields();
            SetBackground();
        }

        /// <summary>弹幕屏蔽列表由 SettingVM 直接写存储，绑一次即可，切页签不重复赋值。</summary>
        private void BindShieldLists()
        {
            DanmuSettingListWords.ItemsSource = settingVM.ShieldWords;
            DanmuSettingListRegulars.ItemsSource = settingVM.ShieldRegulars;
            DanmuSettingListUsers.ItemsSource = settingVM.ShieldUsers;
            LiveDanmuSettingListWords.ItemsSource = settingVM.LiveWords;
        }

        /// <summary>切回页签时按存储值重填；按 PivotItem 身份判断，不依赖序号。</summary>
        private void SettingPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Pivot 在 InitializeComponent 内就可能触发一次，那时 settingVM 还没建
            if (settingVM == null) return;
            var item = SettingPivot.SelectedItem;
            if (ReferenceEquals(item, AppearancePivotItem)) ApplyAppearance();
            else if (ReferenceEquals(item, PlayerPivotItem)) ApplyPlayer();
            else if (ReferenceEquals(item, VideoDanmuPivotItem)) ApplyVideoDanmu();
            else if (ReferenceEquals(item, LiveDanmuPivotItem)) ApplyLiveDanmu();
            else if (ReferenceEquals(item, DownloadPivotItem)) ApplyDownload();
            else if (ReferenceEquals(item, AccountPivotItem))
            {
                RefreshAccountState();
                ApplyAccountFields();
            }
            else if (ReferenceEquals(item, NetworkPivotItem)) ApplyNetwork();
            else if (ReferenceEquals(item, DiagnosticsPivotItem)) ApplyDiagnostics();
        }

        /// <summary>每次切到账号页都重新读一遍，登录状态可能已被其它页面或后台刷新改掉。</summary>
        private void RefreshAccountState()
        {
            var loggedIn = SettingHelper.Account.Logined;
            var state = "未登录";
            if (loggedIn) state = BiliLite.Services.CookieService.NeedsRelogin ? "已失效" : "已登录";
            AccountStateText.Text = state;
            // 未登录时刷新和退出都没有意义
            RefreshLoginButton.IsEnabled = loggedIn;
            LogoutButton.IsEnabled = loggedIn;
        }

        private async void RefreshLogin_Click(object sender, RoutedEventArgs e)
        {
            RefreshLoginButton.IsEnabled = false;
            try
            {
                await ApiHelper.Cookie.RefreshLoginAsync();
            }
            catch (Exception ex)
            {
                LogHelper.Log("Setting page login refresh failed.", LogType.ERROR, ex);
            }
            RefreshAccountState();
        }

        private async void Relogin_Click(object sender, RoutedEventArgs e)
        {
            await Utils.ShowLoginDialog();
            RefreshAccountState();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            MessageCenter.SendLogout();
            RefreshAccountState();
        }

        /// <summary>手填会话字段是 x:Bind 双向绑定，按当前值重填即可丢弃未提交的文本。</summary>
        private void ApplyAccountFields()
        {
            _applyingStoredValues = true;
            try
            {
                SESSDATATextBox.Text = CookieSESSDATA;
                CookieTextBox.Text = Cookie;
                CookieRefreshTokenTextBox.Text = CookieRefreshToken;
            }
            finally
            {
                _applyingStoredValues = false;
            }
        }

        /// <summary>用户操作入口只在这里挂一次；切页签不再重复订阅，也不再重复写盘。</summary>
        private void WireEvents()
        {
            //外观
            cbTheme.SelectionChanged += new SelectionChangedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(UI.THEME, cbTheme.SelectedIndex);
                Frame rootFrame = Window.Current.Content as Frame;
                switch (cbTheme.SelectedIndex)
                {
                    case 1:
                        rootFrame.RequestedTheme = ElementTheme.Light;
                        break;
                    case 2:
                        rootFrame.RequestedTheme = ElementTheme.Dark;
                        break;
                    case 3:
                        rootFrame.RequestedTheme = (ElementTheme)AppTheme.TransparentDark; break;
                    case 4:
                        rootFrame.RequestedTheme = (ElementTheme)AppTheme.TransparentLight; break;
                    default:
                        rootFrame.RequestedTheme = ElementTheme.Default;
                        break;
                }
                App.ExtendAcrylicIntoTitleBar();
            });
            cbDisplayMode.SelectionChanged += new SelectionChangedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(UI.DISPLAY_MODE, cbDisplayMode.SelectedIndex);
                if (cbDisplayMode.SelectedIndex == 2)
                {
                    Utils.ShowMessageToast("多窗口模式正在开发测试阶段，可能会有一堆问题");
                }
                else
                {
                    Utils.ShowMessageToast("重启生效");
                }
            });
            swPictureQuality.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(UI.ORTGINAL_IMAGE, swPictureQuality.IsOn);
                UI._loadOriginalImage = null;
            });
            swHomeCache.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(UI.CACHE_HOME, swHomeCache.IsOn);
            });
            numRightWidth.ValueChanged += new TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs>((sender, args) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(UI.RIGHT_DETAIL_WIDTH, args.NewValue);
            });
            numImageCornerRadius.ValueChanged += new TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs>((sender, args) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(UI.IMAGE_CORNER_RADIUS, args.NewValue);
                ImageCornerRadiusExample.CornerRadius = new CornerRadius(args.NewValue);
                Application.Current.Resources["ImageCornerRadius"] = new CornerRadius(args.NewValue);
            });
            swVideoDetailShowCover.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(UI.SHOW_DETAIL_COVER, swVideoDetailShowCover.IsOn);
            });
            swPreviewImageNavigateToPage.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(UI.NEW_WINDOW_PREVIEW_IMAGE, swPreviewImageNavigateToPage.IsOn);
            });
            swMouseClosePage.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(UI.MOUSE_BACK, swMouseClosePage.IsOn);
            });
            cbDetailDisplay.SelectionChanged += new SelectionChangedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(UI.DETAIL_DISPLAY, cbDetailDisplay.SelectedIndex);
            });
            cbDynamicDisplayMode.SelectionChanged += new SelectionChangedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(UI.DYNAMIC_DISPLAY_MODE, cbDynamicDisplayMode.SelectedIndex);
            });
            cbRecommendDisplayMode.SelectionChanged += new SelectionChangedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(UI.RECMEND_DISPLAY_MODE, cbRecommendDisplayMode.SelectedIndex);
            });
            swOpenUrlWithBrowser.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(UI.OPEN_URL_BROWSER, swOpenUrlWithBrowser.IsOn);
            });
            swHideBanner.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue("dontloadbanner", swHideBanner.IsOn);
            });
            swShowBGOnDynamic.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue("showbgondynamic", swShowBGOnDynamic.IsOn);
            });

            //播放
            cbVideoType.SelectionChanged += new SelectionChangedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(SettingHelper.Player.DEFAULT_VIDEO_TYPE, cbVideoType.SelectedIndex);
                UpdateVideoCodecHint();
            });
            cbVideoSpeed.SelectionChanged += new SelectionChangedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                if (cbVideoSpeed.SelectedIndex < 0 || cbVideoSpeed.SelectedIndex >= SettingHelper.Player.VideoSpeed.Count) return;
                SettingHelper.SetValue(SettingHelper.Player.DEFAULT_VIDEO_SPEED, SettingHelper.Player.VideoSpeed[cbVideoSpeed.SelectedIndex]);
            });
            swPriorityPlayer.SelectionChanged += new SelectionChangedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue("playertype", swPriorityPlayer.SelectedIndex);
            });
            swAutoPlay.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(SettingHelper.Player.AUTO_PLAY, swAutoPlay.IsOn);
            });
            swAutoNext.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(SettingHelper.Player.AUTO_NEXT, swAutoNext.IsOn);
            });
            swPlayerSettingAutoToPosition.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(SettingHelper.Player.AUTO_TO_POSITION, swPlayerSettingAutoToPosition.IsOn);
            });
            swPlayerSettingAutoFullWindows.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(SettingHelper.Player.AUTO_FULL_WINDOW, swPlayerSettingAutoFullWindows.IsOn);
            });
            swPlayerSettingAutoFullScreen.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(SettingHelper.Player.AUTO_FULL_SCREEN, swPlayerSettingAutoFullScreen.IsOn);
            });
            swPlayerSettingDoubleClickFullScreen.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(SettingHelper.Player.DOUBLE_CLICK_FULL_SCREEN, swPlayerSettingDoubleClickFullScreen.IsOn);
            });
            swPlayerSettingAutoOpenAISubtitle.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(SettingHelper.Player.AUTO_OPEN_AI_SUBTITLE, swPlayerSettingAutoOpenAISubtitle.IsOn);
            });
            RoamingSettingToSimplified.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(Roaming.TO_SIMPLIFIED, RoamingSettingToSimplified.IsOn);
            });

            //视频弹幕
            DanmuSettingState.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(VideoDanmaku.SHOW, DanmuSettingState.IsOn ? Visibility.Visible : Visibility.Collapsed);
            });
            numDanmakuTopMargin.ValueChanged += new TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs>((sender, args) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(VideoDanmaku.TOP_MARGIN, args.NewValue);
            });
            numDanmakuMaxNum.ValueChanged += new TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs>((sender, args) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(VideoDanmaku.MAX_NUM, args.NewValue);
            });

            //直播弹幕
            LiveDanmuSettingState.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(SettingHelper.Live.SHOW, LiveDanmuSettingState.IsOn ? Visibility.Visible : Visibility.Collapsed);
            });

            //下载
            DownloadOpenPath.Click += new RoutedEventHandler(async (sender, args) =>
            {
                if (txtDownloadPath.Text == Download.DEFAULT_PATH)
                {
                    var videosLibrary = KnownFolders.VideosLibrary;
                    videosLibrary = await videosLibrary.CreateFolderAsync("哔哩哔哩下载", CreationCollisionOption.OpenIfExists);

                    await Launcher.LaunchFolderAsync(videosLibrary);
                }
                else
                {
                    await Launcher.LaunchFolderPathAsync(txtDownloadPath.Text);
                }
            });
            DownloadChangePath.Click += new RoutedEventHandler(async (sender, args) =>
            {
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add("*");
                folderPicker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    SettingHelper.SetValue(Download.DOWNLOAD_PATH, folder.Path);
                    txtDownloadPath.Text = folder.Path;
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(folder);
                    DownloadVM.Instance.RefreshDownloaded();
                }
            });
            DownloadOpenOldPath.Click += new RoutedEventHandler(async (sender, args) =>
            {
                if (txtDownloadOldPath.Text == Download.DEFAULT_OLD_PATH)
                {
                    var videosLibrary = KnownFolders.VideosLibrary;
                    videosLibrary = await videosLibrary.CreateFolderAsync("BiliBiliDownload", CreationCollisionOption.OpenIfExists);
                    await Launcher.LaunchFolderAsync(videosLibrary);
                }
                else
                {
                    await Launcher.LaunchFolderPathAsync(txtDownloadOldPath.Text);
                }
            });
            DownloadChangeOldPath.Click += new RoutedEventHandler(async (sender, args) =>
            {
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add("*");
                folderPicker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    SettingHelper.SetValue(Download.OLD_DOWNLOAD_PATH, folder.Path);
                    txtDownloadOldPath.Text = folder.Path;
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(folder);
                }
            });
            swDownloadParallelDownload.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(Download.PARALLEL_DOWNLOAD, swDownloadParallelDownload.IsOn);
                DownloadVM.Instance.UpdateSetting();
            });
            swDownloadAllowCostNetwork.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(Download.ALLOW_COST_NETWORK, swDownloadAllowCostNetwork.IsOn);
                DownloadVM.Instance.UpdateSetting();
            });
            swDownloadSendToast.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(Download.SEND_TOAST, swDownloadSendToast.IsOn);
            });
            cbDownloadVideoType.SelectionChanged += new SelectionChangedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(Download.DEFAULT_VIDEO_TYPE, cbDownloadVideoType.SelectedIndex);
            });
            swDownloadLoadOld.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(Download.LOAD_OLD_DOWNLOAD, swDownloadLoadOld.IsOn);
            });

            //网络与解析
            cbPlayerReplaceCDN.SelectionChanged += new SelectionChangedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(SettingHelper.Player.REPLACE_CDN, cbPlayerReplaceCDN.SelectedIndex);
                UpdateCDNSection();
            });
            RoamingSettingCDNServer.SelectionChanged += new SelectionChangedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                var index = RoamingSettingCDNServer.SelectedIndex;
                // 存储里的节点已不在列表时这里就是 -1，不能拿去索引，也不回写
                if (index < 0 || index >= settingVM.CDNServers.Count) return;
                SettingHelper.SetValue(SettingHelper.Player.CDN_SERVER, settingVM.CDNServers[index].Server);
            });
            RoamingSettingSetDefault.Click += RoamingSettingSetDefault_Click;
            RoamingSettingCustomServer.QuerySubmitted += RoamingSettingCustomServer_QuerySubmitted;
            RoamingSettingCustomServerHK.QuerySubmitted += new TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs>((sender, args) =>
            {
                SaveRegionServer(RoamingSettingCustomServerHK, Roaming.CUSTOM_SERVER_URL_HK, "已取消自定义香港代理服务器");
            });
            RoamingSettingCustomServerTW.QuerySubmitted += new TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs>((sender, args) =>
            {
                SaveRegionServer(RoamingSettingCustomServerTW, Roaming.CUSTOM_SERVER_URL_TW, "已取消自定义台湾代理服务器");
            });
            RoamingSettingCustomServerCN.QuerySubmitted += new TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs>((sender, args) =>
            {
                SaveRegionServer(RoamingSettingCustomServerCN, Roaming.CUSTOM_SERVER_URL_CN, "已取消自定义大陆代理服务器");
            });

            //诊断
            swApiCapture.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(SettingHelper.Diagnostics.API_CAPTURE, swApiCapture.IsOn);
                UpdateApiCaptureSamplesState();
            });
            swApiCaptureSamples.Toggled += new RoutedEventHandler((sender, e) =>
            {
                if (_applyingStoredValues) return;
                SettingHelper.SetValue(SettingHelper.Diagnostics.API_CAPTURE_SAMPLES, swApiCaptureSamples.IsOn);
            });
        }

        private void ApplyAppearance()
        {
            _applyingStoredValues = true;
            try
            {
                cbTheme.SelectedIndex = SettingHelper.GetValue(UI.THEME, 0);
                cbDisplayMode.SelectedIndex = SettingHelper.GetValue(UI.DISPLAY_MODE, 0);
                swPictureQuality.IsOn = SettingHelper.GetValue(UI.ORTGINAL_IMAGE, false);
                swHomeCache.IsOn = SettingHelper.GetValue(UI.CACHE_HOME, true);
                numRightWidth.Value = GetValue<double>(UI.RIGHT_DETAIL_WIDTH, 320);
                numImageCornerRadius.Value = GetValue<double>(UI.IMAGE_CORNER_RADIUS, 0);
                ImageCornerRadiusExample.CornerRadius = new CornerRadius(numImageCornerRadius.Value);
                swVideoDetailShowCover.IsOn = SettingHelper.GetValue(UI.SHOW_DETAIL_COVER, true);
                swPreviewImageNavigateToPage.IsOn = SettingHelper.GetValue(UI.NEW_WINDOW_PREVIEW_IMAGE, false);
                swMouseClosePage.IsOn = SettingHelper.GetValue(UI.MOUSE_BACK, true);
                cbDetailDisplay.SelectedIndex = SettingHelper.GetValue(UI.DETAIL_DISPLAY, 0);
                cbDynamicDisplayMode.SelectedIndex = SettingHelper.GetValue(UI.DYNAMIC_DISPLAY_MODE, 0);
                cbRecommendDisplayMode.SelectedIndex = SettingHelper.GetValue(UI.RECMEND_DISPLAY_MODE, 0);
                swOpenUrlWithBrowser.IsOn = SettingHelper.GetValue(UI.OPEN_URL_BROWSER, false);
                swHideBanner.IsOn = SettingHelper.GetValue("dontloadbanner", false);
                swShowBGOnDynamic.IsOn = SettingHelper.GetValue("showbgondynamic", false);
                gridHomeCustom.ItemsSource = SettingHelper.GetValue(UI.HOEM_ORDER, HomeVM.GetAllNavItems());
                ExceptHomeNavItems();
            }
            finally
            {
                _applyingStoredValues = false;
            }
        }

        private void ApplyPlayer()
        {
            _applyingStoredValues = true;
            try
            {
                cbVideoType.SelectedIndex = SettingHelper.GetValue(SettingHelper.Player.DEFAULT_VIDEO_TYPE, 1);
                cbVideoSpeed.SelectedIndex = SettingHelper.Player.VideoSpeed.IndexOf(SettingHelper.GetValue(SettingHelper.Player.DEFAULT_VIDEO_SPEED, 1.0d));
                swPriorityPlayer.SelectedIndex = SettingHelper.GetValue("playertype", 0);
                swAutoPlay.IsOn = SettingHelper.GetValue(SettingHelper.Player.AUTO_PLAY, false);
                swAutoNext.IsOn = SettingHelper.GetValue(SettingHelper.Player.AUTO_NEXT, true);
                swPlayerSettingAutoToPosition.IsOn = SettingHelper.GetValue(SettingHelper.Player.AUTO_TO_POSITION, true);
                swPlayerSettingAutoFullWindows.IsOn = SettingHelper.GetValue(SettingHelper.Player.AUTO_FULL_WINDOW, false);
                swPlayerSettingAutoFullScreen.IsOn = SettingHelper.GetValue(SettingHelper.Player.AUTO_FULL_SCREEN, false);
                swPlayerSettingDoubleClickFullScreen.IsOn = SettingHelper.GetValue(SettingHelper.Player.DOUBLE_CLICK_FULL_SCREEN, false);
                swPlayerSettingAutoOpenAISubtitle.IsOn = SettingHelper.GetValue(SettingHelper.Player.AUTO_OPEN_AI_SUBTITLE, false);
                RoamingSettingToSimplified.IsOn = SettingHelper.GetValue(Roaming.TO_SIMPLIFIED, true);
            }
            finally
            {
                _applyingStoredValues = false;
            }
            UpdateVideoCodecHint();
        }

        private void ApplyVideoDanmu()
        {
            _applyingStoredValues = true;
            try
            {
                var state = SettingHelper.GetValue(VideoDanmaku.SHOW, Visibility.Visible) == Visibility.Visible;
                DanmuSettingState.IsOn = state;
                numDanmakuTopMargin.Value = GetValue<double>(VideoDanmaku.TOP_MARGIN, 0);
                numDanmakuMaxNum.Value = GetValue<double>(VideoDanmaku.MAX_NUM, 0);
                // 未提交的新条目不算数：输入框回到空，错误提示一并复位
                DanmuSettingTxtWord.Text = "";
                DanmuSettingTxtRegex.Text = "";
                DanmuSettingTxtUser.Text = "";
                HideRegexError();
            }
            finally
            {
                _applyingStoredValues = false;
            }
        }

        private void ApplyLiveDanmu()
        {
            _applyingStoredValues = true;
            try
            {
                var state = SettingHelper.GetValue(SettingHelper.Live.SHOW, Visibility.Visible) == Visibility.Visible;
                LiveDanmuSettingState.IsOn = state;
                LiveDanmuSettingTxtWord.Text = "";
            }
            finally
            {
                _applyingStoredValues = false;
            }
        }

        private void ApplyDownload()
        {
            _applyingStoredValues = true;
            try
            {
                txtDownloadPath.Text = SettingHelper.GetValue(Download.DOWNLOAD_PATH, Download.DEFAULT_PATH);
                txtDownloadOldPath.Text = SettingHelper.GetValue(Download.OLD_DOWNLOAD_PATH, Download.DEFAULT_OLD_PATH);
                cbDownloadVideoType.SelectedIndex = SettingHelper.GetValue(Download.DEFAULT_VIDEO_TYPE, 1);
                swDownloadParallelDownload.IsOn = SettingHelper.GetValue(Download.PARALLEL_DOWNLOAD, true);
                swDownloadAllowCostNetwork.IsOn = SettingHelper.GetValue(Download.ALLOW_COST_NETWORK, false);
                swDownloadSendToast.IsOn = SettingHelper.GetValue(Download.SEND_TOAST, false);
                swDownloadLoadOld.IsOn = SettingHelper.GetValue(Download.LOAD_OLD_DOWNLOAD, false);
            }
            finally
            {
                _applyingStoredValues = false;
            }
        }

        private void ApplyNetwork()
        {
            _applyingStoredValues = true;
            try
            {
                cbPlayerReplaceCDN.SelectedIndex = SettingHelper.GetValue(SettingHelper.Player.REPLACE_CDN, 3);
                var cdnServer = SettingHelper.GetValue(SettingHelper.Player.CDN_SERVER, "upos-sz-mirrorhwo1.bilivideo.com");
                var cdnIndex = settingVM.CDNServers.FindIndex(x => x.Server == cdnServer);
                // 节点已不在列表时保持未选中（-1），原来的存储值不动
                RoamingSettingCDNServer.SelectedIndex = cdnIndex;
                RoamingSettingCustomServer.Text = SettingHelper.GetValue(Roaming.CUSTOM_SERVER_URL, ApiHelper.ROMAING_PROXY_URL);
                RoamingSettingCustomServerHK.Text = SettingHelper.GetValue(Roaming.CUSTOM_SERVER_URL_HK, "");
                RoamingSettingCustomServerTW.Text = SettingHelper.GetValue(Roaming.CUSTOM_SERVER_URL_TW, "");
                RoamingSettingCustomServerCN.Text = SettingHelper.GetValue(Roaming.CUSTOM_SERVER_URL_CN, "");
            }
            finally
            {
                _applyingStoredValues = false;
            }
            UpdateCDNSection();
        }

        private void ApplyDiagnostics()
        {
            _applyingStoredValues = true;
            try
            {
                swApiCapture.IsOn = SettingHelper.GetValue(SettingHelper.Diagnostics.API_CAPTURE, false);
                swApiCaptureSamples.IsOn = SettingHelper.GetValue(SettingHelper.Diagnostics.API_CAPTURE_SAMPLES, false);
            }
            finally
            {
                _applyingStoredValues = false;
            }
            UpdateApiCaptureSamplesState();
        }

        /// <summary>只有选 HEVC / AV1 时才显示对应的扩展说明，其它选项两条都不占位。</summary>
        private void UpdateVideoCodecHint()
        {
            var index = cbVideoType.SelectedIndex;
            var showHevc = index == 2;
            var showAv1 = index == 3;
            VideoCodecHintHevc.Visibility = showHevc ? Visibility.Visible : Visibility.Collapsed;
            VideoCodecHintAv1.Visibility = showAv1 ? Visibility.Visible : Visibility.Collapsed;
            VideoCodecHint.Visibility = (showHevc || showAv1) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>不替换时不占位；只改可见性，已保存的节点和选择都不动。</summary>
        private void UpdateCDNSection()
        {
            var replaceEnabled = cbPlayerReplaceCDN.SelectedIndex > 0;
            CDNNodeSection.Visibility = replaceEnabled ? Visibility.Visible : Visibility.Collapsed;
            CDNReplaceHint.Visibility = replaceEnabled ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>样本开关依赖结构记录：只改可用性和说明，绝不改写已保存的样本值。</summary>
        private void UpdateApiCaptureSamplesState()
        {
            var available = swApiCapture.IsOn;
            swApiCaptureSamples.IsEnabled = available;
            if (available)
            {
                swApiCaptureSamplesHelp.Text = "每份最多 4 KB，名字像凭据的字段会被替换。";
            }
            else
            {
                swApiCaptureSamplesHelp.Text = "需要先开启『记录 API 响应结构』后才会写入。";
            }
        }

        /// <summary>地区服务器留空＝清掉该地区设置，非空则补协议后保存。</summary>
        private void SaveRegionServer(AutoSuggestBox box, string key, string clearedMessage)
        {
            var text = box.Text;
            if (string.IsNullOrEmpty(text))
            {
                Utils.ShowMessageToast(clearedMessage);
                SettingHelper.SetValue(key, "");
                return;
            }
            if (!text.Contains("http"))
            {
                text = "https://" + text;
            }
            SettingHelper.SetValue(key, text);
            box.Text = text;
            Utils.ShowMessageToast("保存成功");
        }

        private void RoamingSettingSetDefault_Click(object sender, RoutedEventArgs e)
        {
            SettingHelper.SetValue(Roaming.CUSTOM_SERVER_URL, ApiHelper.ROMAING_PROXY_URL);
            RoamingSettingCustomServer.Text = ApiHelper.ROMAING_PROXY_URL;
            Utils.ShowMessageToast("保存成功");
        }

        private void RoamingSettingCustomServer_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var text = sender.Text;
            if (text.Length == 0 || !text.Contains("."))
            {
                Utils.ShowMessageToast("输入服务器链接有误");
                sender.Text = SettingHelper.GetValue(Roaming.CUSTOM_SERVER_URL, ApiHelper.ROMAING_PROXY_URL);
                return;
            }
            if (!text.Contains("http"))
            {
                text = "https://" + text;
            }
            SettingHelper.SetValue(Roaming.CUSTOM_SERVER_URL, text);
            sender.Text = text;
            Utils.ShowMessageToast("保存成功");
        }

        private void ExceptHomeNavItems()
        {
            List<HomeNavItem> list = new List<HomeNavItem>();
            var all = HomeVM.GetAllNavItems();
            foreach (var item in all)
            {
                if ((gridHomeCustom.ItemsSource as ObservableCollection<HomeNavItem>).FirstOrDefault(x => x.Title == item.Title) == null)
                {
                    list.Add(item);
                }
            }
            gridHomeNavItem.ItemsSource = list;
        }
        private void gridHomeCustom_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            SettingHelper.SetValue(UI.HOEM_ORDER, gridHomeCustom.ItemsSource as ObservableCollection<HomeNavItem>);
            Utils.ShowMessageToast("更改成功,重启生效");
        }

        private void gridHomeNavItem_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as HomeNavItem;
            (gridHomeCustom.ItemsSource as ObservableCollection<HomeNavItem>).Add(item);
            SettingHelper.SetValue(UI.HOEM_ORDER, gridHomeCustom.ItemsSource as ObservableCollection<HomeNavItem>);
            ExceptHomeNavItems();
            Utils.ShowMessageToast("更改成功,重启生效");
        }

        private void menuRemoveHomeItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as MenuFlyoutItem).DataContext as HomeNavItem;
            if (gridHomeCustom.Items.Count == 1)
            {
                Utils.ShowMessageToast("至少要留一个页面");
                return;
            }
           (gridHomeCustom.ItemsSource as ObservableCollection<HomeNavItem>).Remove(item);
            SettingHelper.SetValue(UI.HOEM_ORDER, gridHomeCustom.ItemsSource as ObservableCollection<HomeNavItem>);
            ExceptHomeNavItems();
            Utils.ShowMessageToast("更改成功,重启生效");
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            try
            {
                version.Text = $"版本 {SystemInformation.Instance.ApplicationVersion.Major}.{SystemInformation.Instance.ApplicationVersion.Minor}.{SystemInformation.Instance.ApplicationVersion.Build}.{SystemInformation.Instance.ApplicationVersion.Revision}";
                txtHelp.Text = await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Text/help.md")));
            }
            catch (Exception)
            {
                //throw;
            }
            try
            {
                Info.Text = await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Text/info.md")));
            }
            catch (Exception)
            {
                //throw;
            }
        }

        private async void DanmuSettingAddWord_Click(object sender, RoutedEventArgs e)
        {
            var word = DanmuSettingTxtWord.Text;
            if (string.IsNullOrWhiteSpace(word))
            {
                Utils.ShowMessageToast("关键词不能为空");
                return;
            }
            if (!await settingVM.AddDanmuFilterItem(word, 0))
            {
                Utils.ShowMessageToast("该关键词已存在");
                return;
            }
            DanmuSettingTxtWord.Text = "";
        }

        private async void DanmuSettingSyncWords_Click(object sender, RoutedEventArgs e)
        {
            await settingVM.SyncDanmuFilter();
        }

        private async void DanmuSettingExportFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.SuggestedFileName = "bilibili.block";
                picker.FileTypeChoices.Add("JSON文件", new List<string> { ".json" });
                var file = await picker.PickSaveFileAsync();
                if (file == null)
                    return;

                await FileIO.WriteTextAsync(file, settingVM.ExportDanmuFilterJson());
                Utils.ShowMessageToast("弹幕屏蔽规则已导出");
            }
            catch (Exception ex)
            {
                LogHelper.Log("导出弹幕屏蔽规则失败", LogType.ERROR, ex);
                Utils.ShowMessageToast("导出弹幕屏蔽规则失败", ex.Message);
            }
        }

        private async void DanmuSettingImportFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".json");
                var file = await picker.PickSingleFileAsync();
                if (file == null)
                    return;

                var text = await FileIO.ReadTextAsync(file);
                var added = settingVM.ImportDanmuFilterJson(text);
                if (added > 0)
                {
                    Utils.ShowMessageToast("已导入" + added + "条弹幕屏蔽规则");
                }
                else
                {
                    Utils.ShowMessageToast("没有可导入的新规则");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log("导入弹幕屏蔽规则失败", LogType.ERROR, ex);
                Utils.ShowMessageToast("导入弹幕屏蔽规则失败", ex.Message);
            }
        }

        private void RemoveDanmuWord_Click(object sender, RoutedEventArgs e)
        {
            var word = (sender as AppBarButton).DataContext as string;
            settingVM.ShieldWords.Remove(word);
            SettingHelper.SetValue(VideoDanmaku.SHIELD_WORD, settingVM.ShieldWords);
        }

        private void RemoveDanmuRegular_Click(object sender, RoutedEventArgs e)
        {
            var word = (sender as AppBarButton).DataContext as string;
            settingVM.ShieldRegulars.Remove(word);
            SettingHelper.SetValue(VideoDanmaku.SHIELD_REGULAR, settingVM.ShieldRegulars);
        }

        private void RemoveDanmuUser_Click(object sender, RoutedEventArgs e)
        {
            var word = (sender as AppBarButton).DataContext as string;
            settingVM.ShieldUsers.Remove(word);
            SettingHelper.SetValue(VideoDanmaku.SHIELD_USER, settingVM.ShieldUsers);
        }

        private async void DanmuSettingAddRegex_Click(object sender, RoutedEventArgs e)
        {
            var txt = DanmuSettingTxtRegex.Text.Trim('/');
            if (string.IsNullOrEmpty(txt))
            {
                ShowRegexError("正则表达式不能为空");
                return;
            }
            if (!DanmuFilterRules.TryValidateRegex(txt, out var error))
            {
                ShowRegexError(error);
                return;
            }
            if (!await settingVM.AddDanmuFilterItem(txt, 1))
            {
                ShowRegexError("该正则表达式已存在");
                return;
            }
            HideRegexError();
            DanmuSettingTxtRegex.Text = "";
        }

        private void ShowRegexError(string message)
        {
            DanmuSettingRegexError.Text = message;
            DanmuSettingRegexError.Visibility = Visibility.Visible;
        }

        private void HideRegexError()
        {
            DanmuSettingRegexError.Text = "";
            DanmuSettingRegexError.Visibility = Visibility.Collapsed;
        }

        private async void DanmuSettingAddUser_Click(object sender, RoutedEventArgs e)
        {
            var user = DanmuSettingTxtUser.Text;
            if (string.IsNullOrWhiteSpace(user))
            {
                Utils.ShowMessageToast("用户ID不能为空");
                return;
            }
            if (!await settingVM.AddDanmuFilterItem(user, 2))
            {
                Utils.ShowMessageToast("该用户标识已存在");
                return;
            }
            DanmuSettingTxtUser.Text = "";
        }


        private async void txtHelp_LinkClicked(object sender, Microsoft.Toolkit.Uwp.UI.Controls.LinkClickedEventArgs e)
        {
            if (e.Link == "OpenLog")
            {
                var path = ApplicationData.Current.LocalFolder.Path + @"\log\";
                await Launcher.LaunchFolderPathAsync(path);
            }
            else
            {
                await Utils.LaunchUri(new Uri(e.Link));
            }

        }

        private async void ExportLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogHelper.Flush();

                var logFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("log", CreationCollisionOption.OpenIfExists);
                var logFiles = (await logFolder.GetFilesAsync())
                    .Where(file => file.FileType.Equals(".log", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(file => file.Name)
                    .ToList();
                var recentLogs = logFiles.Skip(Math.Max(0, logFiles.Count - 7)).ToList();
                var export = new StringBuilder();
                var version = Windows.ApplicationModel.Package.Current.Id.Version;
                export.AppendLine("BiliLite diagnostic log export");
                export.AppendLine("Exported: " + DateTimeOffset.Now.ToString("o"));
                export.AppendLine("Session ID: " + LogHelper.SessionId);
                export.AppendLine("App version: " + version.Major + "." + version.Minor + "." + version.Build + "." + version.Revision);
                export.AppendLine("Included log files: " + recentLogs.Count + " (latest 7)");
                export.AppendLine();

                foreach (var logFile in recentLogs)
                {
                    export.AppendLine("===== " + logFile.Name + " =====");
                    export.AppendLine(await FileIO.ReadTextAsync(logFile));
                    export.AppendLine();
                }

                var picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.SuggestedFileName = "BiliLite-diagnostics-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                picker.FileTypeChoices.Add("文本文件", new List<string> { ".txt" });
                var destination = await picker.PickSaveFileAsync();
                if (destination == null)
                    return;

                await FileIO.WriteTextAsync(destination, export.ToString());
                Utils.ShowMessageToast("诊断日志已导出");
            }
            catch (Exception ex)
            {
                Utils.ShowMessageToast("导出日志失败", ex.Message);
            }
        }

        /// <summary>日志目录可能还没建（应用刚装、还没写过日志），先按 OpenIfExists 拿到再打开。</summary>
        private async void OpenLogFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("log", CreationCollisionOption.OpenIfExists);
                await Launcher.LaunchFolderPathAsync(logFolder.Path);
            }
            catch (Exception ex)
            {
                LogHelper.Log("打开日志目录失败", LogType.ERROR, ex);
                Utils.ShowMessageToast("打开日志目录失败", ex.Message);
            }
        }

        /// <summary>
        /// 一次点击拿到可分享的包：最近 7 个日志 + 响应形状/基线 + manifest。
        /// 采集是开关控制的，所以包里有什么取决于当时开了什么，manifest 里写着。
        /// </summary>
        private async void ExportBundle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("log", CreationCollisionOption.OpenIfExists);
                var logFiles = (await logFolder.GetFilesAsync())
                    .Where(file => file.FileType.Equals(".log", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(file => file.Name)
                    .ToList();
                var recentLogs = logFiles.Skip(Math.Max(0, logFiles.Count - 7)).ToList();
                var apiFolder = await logFolder.TryGetItemAsync("api") as StorageFolder;
                var apiFiles = apiFolder == null
                    ? new List<StorageFile>()
                    : (await apiFolder.GetFilesAsync()).OrderBy(file => file.Name).ToList();

                var entries = new List<Tuple<string, StorageFile>>();
                foreach (var file in recentLogs)
                {
                    entries.Add(Tuple.Create(DiagnosticBundle.EntryPath("logs", file.Name), file));
                }
                foreach (var file in apiFiles)
                {
                    entries.Add(Tuple.Create(DiagnosticBundle.EntryPath("api", file.Name), file));
                }

                var version = Windows.ApplicationModel.Package.Current.Id.Version;
                var manifest = DiagnosticBundle.BuildManifest(
                    version.Major + "." + version.Minor + "." + version.Build + "." + version.Revision,
                    LogHelper.SessionId, DateTimeOffset.Now,
                    SettingHelper.GetValue(SettingHelper.Diagnostics.API_CAPTURE, false),
                    SettingHelper.GetValue(SettingHelper.Diagnostics.API_CAPTURE_SAMPLES, false),
                    entries.Select(entry => entry.Item1).Concat(new[] { DiagnosticBundle.ManifestName }));

                var picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.SuggestedFileName = "BiliLite-diagnostics-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                picker.FileTypeChoices.Add("压缩包", new List<string> { ".zip" });
                var destination = await picker.PickSaveFileAsync();
                if (destination == null)
                    return;

                using (var stream = await destination.OpenAsync(FileAccessMode.ReadWrite))
                {
                    //选中的是已有文件时必须先截断：否则旧的归档尾部会留在新包后面，读出来是坏包
                    stream.Size = 0;
                    stream.Seek(0);
                    using (var archive = new System.IO.Compression.ZipArchive(stream.AsStreamForWrite(), System.IO.Compression.ZipArchiveMode.Create))
                    {
                        var manifestEntry = archive.CreateEntry(DiagnosticBundle.ManifestName);
                        using (var writer = new StreamWriter(manifestEntry.Open()))
                        {
                            await writer.WriteAsync(manifest);
                        }
                        foreach (var entry in entries)
                        {
                            var zipEntry = archive.CreateEntry(entry.Item1);
                            using (var target = zipEntry.Open())
                            using (var source = await entry.Item2.OpenStreamForReadAsync())
                            {
                                await source.CopyToAsync(target);
                            }
                        }
                    }
                }
                Utils.ShowMessageToast("诊断包已导出（" + (entries.Count + 1) + " 个文件）");
            }
            catch (Exception ex)
            {
                Utils.ShowMessageToast("导出诊断包失败", ex.Message);
            }
        }
        private async void MarkProblem_Click(object sender, RoutedEventArgs e)
        {
            var noteBox = new TextBox
            {
                AcceptsReturn = true,
                Height = 100,
                PlaceholderText = "可选：简单描述刚才的操作或现象"
            };
            var dialog = new ContentDialog
            {
                Title = "标记问题发生时刻",
                Content = noteBox,
                PrimaryButtonText = "记录",
                SecondaryButtonText = "取消"
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            var note = (noteBox.Text ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
            LogHelper.Log("User marked a problem; time=" + DateTimeOffset.Now.ToString("o")
                + "; sessionId=" + LogHelper.SessionId + "; note=" + note, LogType.INFO);
            LogHelper.Flush();
            Utils.ShowMessageToast("已在日志中标记问题时刻");
        }

        private void LiveDanmuSettingAddWord_Click(object sender, RoutedEventArgs e)
        {
            var word = LiveDanmuSettingTxtWord.Text;
            if (string.IsNullOrWhiteSpace(word))
            {
                Utils.ShowMessageToast("关键字不能为空");
                return;
            }
            if (!DanmuFilterRules.AddUnique(settingVM.LiveWords, word))
            {
                Utils.ShowMessageToast("该关键词已存在");
                return;
            }
            SettingHelper.SetValue(SettingHelper.Live.SHIELD_WORD, settingVM.LiveWords);
            LiveDanmuSettingTxtWord.Text = "";
        }

        private void RemoveLiveDanmuWord_Click(object sender, RoutedEventArgs e)
        {
            var word = (sender as AppBarButton).DataContext as string;
            settingVM.LiveWords.Remove(word);
            SettingHelper.SetValue(SettingHelper.Live.SHIELD_WORD, settingVM.LiveWords);
        }

        private async void btnCleanImageCache_Click(object sender, RoutedEventArgs e)
        {
            await ImageCache.Instance.ClearAsync();
            Utils.ShowMessageToast("已清除图片缓存");
        }

        private void RoamingSettingTestCDN_Click(object sender, RoutedEventArgs e)
        {
            settingVM.CDNServerDelayTest();
        }
        private async void SetBackground()
        {
            var background = SettingHelper.GetValue(UI.BACKGROUND_IMAGE, AppHelper.BACKGROUND_IAMGE_URL);
            if (background == AppHelper.BACKGROUND_IAMGE_URL)
            {
                BGImage.Source = new BitmapImage(new Uri(background));
            }
            else
            {
                StorageFile file = null;
                try
                {
                    file = await StorageFile.GetFileFromPathAsync(background);
                }
                catch
                {
                    Utils.ShowMessageToast("请授予系统存储权限", 5);
                }
                if (file == null)
                {
                    return;
                }
                var img = new BitmapImage();
                img.SetSource(await file.OpenReadAsync());
                BGImage.Source = img;
            }
        }

        private async void BGImage_Click(object sender, RoutedEventArgs e) {
            FileOpenPicker fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.FileTypeFilter.Add(".png");
            fileOpenPicker.FileTypeFilter.Add(".jpg");
            StorageFile file = await fileOpenPicker.PickSingleFileAsync();
            if (file != null)
            {
                SettingHelper.SetValue(UI.BACKGROUND_IMAGE, file.Path);
                SetBackground();
            }

        }
        private void ShowTokenButton_Click(object sender, RoutedEventArgs e)
        {
            TokenTextBox.Text = SettingHelper.Account.AccessKey;
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public string Cookie
        {
            get
            {
                return ApiHelper.GetCSRF();
            }
            set
            {
                // 重填控件期间的回写不算用户修改
                if (_applyingStoredValues) return;
                if (String.IsNullOrEmpty(value)) return;
                SettingHelper.SetValue<string>("CookieCSRF",value);
                var filter = new HttpBaseProtocolFilter();
                var cookies = filter.CookieManager.GetCookies(new Uri("https://bilibili.com"));
                try
                {
                    var one = cookies.First(x => x.Name == "bili_jct");
                    one.Value = value;
                }
                catch
                {
                    var c = new Windows.Web.Http.HttpCookie("bili_jct", "bilibili.com", "/");
                    c.Value = value;
                    filter.CookieManager.SetCookie(c);
                }
                OnPropertyChanged(nameof(Cookie));
            }
        }
        public string CookieSESSDATA
        {
            get
            {

                var filter = new HttpBaseProtocolFilter();
                var cookies = filter.CookieManager.GetCookies(new Uri("https://bilibili.com"));
                try
                {
                    var one = cookies.First(x => x.Name == "SESSDATA");
                    return one.Value;
                }
                catch
                {
                    return SettingHelper.GetValue<string>("CookieSESSDATA", "");
                }
            }
            set
            {
                // 重填控件期间的回写不算用户修改
                if (_applyingStoredValues) return;
                if (String.IsNullOrEmpty(value)) return;
                var filter = new HttpBaseProtocolFilter();
                var cookies = filter.CookieManager.GetCookies(new Uri("https://bilibili.com"));
                SettingHelper.SetValue<string>("CookieSESSDATA", value);
                try
                {
                    var one = cookies.First(x => x.Name == "SESSDATA");
                    one.Value = value;
                }
                catch
                {
                    var c = new Windows.Web.Http.HttpCookie("SESSDATA", "bilibili.com", "/");
                    c.Value = value;
                    filter.CookieManager.SetCookie(c);
                    OnPropertyChanged(nameof(CookieSESSDATA));
                }
            }
        }

        public string CookieRefreshToken
        {
            get => SettingHelper.GetValue<string>(RefreshTokenMirror.TokenKey, "");
            set
            {
                // 重填控件期间的回写不算用户修改，避免把自动刷新的来源改成 manual
                if (_applyingStoredValues) return;
                // 手填的就是本会话的令牌：来源记 manual、归属记当前 uid，读令牌时不会被当成别人的；
                // 清空输入框＝清掉记录，值、来源、归属一起走（判定和落盘都在 RefreshTokenMirror）
                refreshTokenMirror.RecordManual(value,
                    SettingHelper.GetValue(SettingHelper.Account.USER_ID, 0L));
                OnPropertyChanged(nameof(CookieRefreshToken));
            }
        }
    }
}

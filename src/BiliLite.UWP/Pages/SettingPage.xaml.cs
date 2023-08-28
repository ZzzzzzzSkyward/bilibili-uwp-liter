using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Models;
using BiliLite.Modules;
using BiliLite.Modules.User;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http.Filters;
using ZXing;
using static BiliLite.Helpers.SettingHelper;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliLite.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SettingPage : BasePage
    {
        SettingVM settingVM;
        public SettingPage()
        {
            this.InitializeComponent();
            Title = "设置";
            settingVM = new SettingVM();
            LoadUI();
            LoadAPI();
            LoadPlayer();
            LoadRoaming();
            LoadDanmu();
            LoadLiveDanmu();
            LoadDownlaod();
            api = new Api.AccountApi();
        }
        private void LoadUI()
        {
            //主题
            cbTheme.SelectedIndex = SettingHelper.GetValue(UI.THEME, 0);
            cbTheme.Loaded += new RoutedEventHandler((sender, e) =>
            {
                cbTheme.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
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
            });

            cbColor.SelectedIndex = SettingHelper.GetValue(UI.THEME_COLOR, 0);
            cbColor.Loaded += new RoutedEventHandler((sender, e) =>
            {
                cbColor.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(UI.THEME_COLOR, cbColor.SelectedIndex);
                    Color color = new Color();
                    if (cbColor.SelectedIndex == 0)
                    {
                        var uiSettings = new Windows.UI.ViewManagement.UISettings();
                        color = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent);
                    }
                    else
                    {
                        color = Utils.ToColor((cbColor.SelectedItem as AppThemeColor).color);

                    }
                    (Application.Current.Resources["SystemControlHighlightAltAccentBrush"] as SolidColorBrush).Color = color;
                    (Application.Current.Resources["SystemControlHighlightAccentBrush"] as SolidColorBrush).Color = color;
                    //(App.Current.Resources.ThemeDictionaries["Light"] as ResourceDictionary)["SystemAccentColor"] = Utils.ToColor(item.color);

                });
            });


            //显示模式
            cbDisplayMode.SelectedIndex = SettingHelper.GetValue(UI.DISPLAY_MODE, 0);
            cbDisplayMode.Loaded += new RoutedEventHandler((sender, e) =>
            {
                cbDisplayMode.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
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
            });
            //加载原图
            swPictureQuality.IsOn = SettingHelper.GetValue(UI.ORTGINAL_IMAGE, false);
            swPictureQuality.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swPictureQuality.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(UI.ORTGINAL_IMAGE, swPictureQuality.IsOn);
                    UI._loadOriginalImage = null;
                });
            });
            //缓存页面
            swHomeCache.IsOn = SettingHelper.GetValue(UI.CACHE_HOME, true);
            swHomeCache.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swHomeCache.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(UI.CACHE_HOME, swHomeCache.IsOn);

                });
            });

            //右侧详情宽度
            numRightWidth.Value = GetValue<double>(UI.RIGHT_DETAIL_WIDTH, 320);
            numRightWidth.Loaded += new RoutedEventHandler((sender, e) =>
            {

                numRightWidth.ValueChanged += new TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs>((obj, args) =>
                {
                    SettingHelper.SetValue(UI.RIGHT_DETAIL_WIDTH, args.NewValue);
                });
            });
            //图片圆角半径
            numImageCornerRadius.Value = GetValue<double>(UI.IMAGE_CORNER_RADIUS, 0);
            ImageCornerRadiusExample.CornerRadius = new CornerRadius(numImageCornerRadius.Value);
            numImageCornerRadius.Loaded += new RoutedEventHandler((sender, e) =>
            {
                numImageCornerRadius.ValueChanged += new TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs>((obj, args) =>
                {
                    SettingHelper.SetValue(UI.IMAGE_CORNER_RADIUS, args.NewValue);
                    ImageCornerRadiusExample.CornerRadius = new CornerRadius(args.NewValue);
                    Application.Current.Resources["ImageCornerRadius"] = new CornerRadius(args.NewValue);
                });
            });

            //显示视频封面
            swVideoDetailShowCover.IsOn = SettingHelper.GetValue(UI.SHOW_DETAIL_COVER, true);
            swVideoDetailShowCover.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swVideoDetailShowCover.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(UI.SHOW_DETAIL_COVER, swVideoDetailShowCover.IsOn);
                });
            });

            //新窗口浏览图片
            swPreviewImageNavigateToPage.IsOn = SettingHelper.GetValue(UI.NEW_WINDOW_PREVIEW_IMAGE, false);
            swPreviewImageNavigateToPage.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swPreviewImageNavigateToPage.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(UI.NEW_WINDOW_PREVIEW_IMAGE, swPreviewImageNavigateToPage.IsOn);
                });
            });
            //鼠标侧键返回
            swMouseClosePage.IsOn = SettingHelper.GetValue(UI.MOUSE_BACK, true);
            swMouseClosePage.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swMouseClosePage.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(UI.MOUSE_BACK, swMouseClosePage.IsOn);
                });
            });

            //动态显示
            cbDetailDisplay.SelectedIndex = SettingHelper.GetValue(UI.DETAIL_DISPLAY, 0);
            cbDetailDisplay.Loaded += new RoutedEventHandler((sender, e) =>
            {
                cbDetailDisplay.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(UI.DETAIL_DISPLAY, cbDetailDisplay.SelectedIndex);
                });
            });

            //动态显示
            cbDynamicDisplayMode.SelectedIndex = SettingHelper.GetValue(UI.DYNAMIC_DISPLAY_MODE, 0);
            cbDynamicDisplayMode.Loaded += new RoutedEventHandler((sender, e) =>
            {
                cbDynamicDisplayMode.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(UI.DYNAMIC_DISPLAY_MODE, cbDynamicDisplayMode.SelectedIndex);
                });
            });

            //推荐显示
            cbRecommendDisplayMode.SelectedIndex = SettingHelper.GetValue(UI.RECMEND_DISPLAY_MODE, 0);
            cbRecommendDisplayMode.Loaded += new RoutedEventHandler((sender, e) =>
            {
                cbRecommendDisplayMode.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(UI.RECMEND_DISPLAY_MODE, cbRecommendDisplayMode.SelectedIndex);
                });
            });

            //浏览器打开无法处理的链接
            swOpenUrlWithBrowser.IsOn = SettingHelper.GetValue(UI.OPEN_URL_BROWSER, false);
            swOpenUrlWithBrowser.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swOpenUrlWithBrowser.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(UI.OPEN_URL_BROWSER, swOpenUrlWithBrowser.IsOn);
                });
            });

            //隐藏横幅
            swHideBanner.IsOn = SettingHelper.GetValue("dontloadbanner", false);
            swHideBanner.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swHideBanner.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue("dontloadbanner", swHideBanner.IsOn);
                });
            }); 
            //背景动态
            swShowBGOnDynamic.IsOn = SettingHelper.GetValue("showbgondynamic", false);
            swShowBGOnDynamic.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swShowBGOnDynamic.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue("showbgondynamic", swShowBGOnDynamic.IsOn);
                });
            });

            gridHomeCustom.ItemsSource = SettingHelper.GetValue(UI.HOEM_ORDER, HomeVM.GetAllNavItems());
            ExceptHomeNavItems();

            //背景
            SetBackground();

            //默认主题
            LoadDefaultThemeText();
        }
        private void LoadDefaultThemeText()
        {
            DefaultThemeText.Content = @" <Color x:Key=""HighLightTextColor"">#d0318c</Color>
<Color x:Key=""TopPaneBackground"">#00F7F7F7</Color>
<Color x:Key=""HighLightColor"">#ec407a</Color>
<Color x:Key=""TextColor"">#CC000000</Color>
<Color x:Key=""DefaultTextColor"">#CC000000</Color>
<Color x:Key=""SystemColorHighLightColor"">#ec407a</Color>
<Color x:Key=""CardColor"">#00FFFFFF</Color>
<Color x:Key=""ForegroundGridColor"">#aaaaaa</Color>
<Color x:Key=""ForegroundBorderColor"">#111111</Color>
<Color x:Key=""ForegroundTextColor"">#333333</Color>
<Color x:Key=""ForegroundBackColor"">#555555</Color>
<Color x:Key=""ForegroundTransparentColor"">#aaffffff</Color>
<Color x:Key=""SolidTransparentBackground"">#00ffffff</Color>
<Color x:Key=""SolidHalfTransparentBackground"">#a0ffffff</Color>
<AcrylicBrush x:Key=""TransparentLayer"" TintColor=""White"" TintOpacity=""0.1"" TintLuminosityOpacity=""0.5"" FallbackColor=""#a0ffffff"" BackgroundSource=""Backdrop""></AcrylicBrush>
<AcrylicBrush x:Key=""TransparentBackground""
BackgroundSource=""HostBackdrop""
TintColor=""#ffffff""
TintOpacity=""0.5""
TintLuminosityOpacity=""0.5""
FallbackColor=""#ffffff"" />
";
        }
        private void LoadPlayer()
        {
            //播放类型
            cbVideoType.SelectedIndex = SettingHelper.GetValue(SettingHelper.Player.DEFAULT_VIDEO_TYPE, 1);
            cbVideoType.Loaded += new RoutedEventHandler((sender, e) =>
            {
                cbVideoType.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.Player.DEFAULT_VIDEO_TYPE, cbVideoType.SelectedIndex);
                });
            });
            //视频倍速
            cbVideoSpeed.SelectedIndex = SettingHelper.Player.VideoSpeed.IndexOf(SettingHelper.GetValue(SettingHelper.Player.DEFAULT_VIDEO_SPEED, 1.0d));
            cbVideoSpeed.Loaded += new RoutedEventHandler((sender, e) =>
            {
                cbVideoSpeed.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.Player.DEFAULT_VIDEO_SPEED, SettingHelper.Player.VideoSpeed[cbVideoSpeed.SelectedIndex]);
                });
            });
            //播放器
            swPriorityPlayer.SelectedIndex = SettingHelper.GetValue("playertype", 0);
            swPriorityPlayer.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swPriorityPlayer.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue("playertype", swPriorityPlayer.SelectedIndex);
                });
            });

            //自动播放
            swAutoPlay.IsOn = SettingHelper.GetValue(SettingHelper.Player.AUTO_PLAY, false);
            swAutoPlay.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swAutoPlay.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.Player.AUTO_PLAY, swAutoPlay.IsOn);
                });
            });
            //自动跳转下一P
            swAutoNext.IsOn = SettingHelper.GetValue(SettingHelper.Player.AUTO_NEXT, true);
            swAutoNext.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swAutoNext.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.Player.AUTO_NEXT, swAutoNext.IsOn);
                });
            });
            //使用其他网站
            //swPlayerSettingUseOtherSite.IsOn = SettingHelper.GetValue<bool>(SettingHelper.Player.USE_OTHER_SITEVIDEO, false);
            //swPlayerSettingUseOtherSite.Loaded += new RoutedEventHandler((sender, e) =>
            //{
            //    swPlayerSettingUseOtherSite.Toggled += new RoutedEventHandler((obj, args) =>
            //    {
            //        SettingHelper.SetValue(SettingHelper.Player.USE_OTHER_SITEVIDEO, swPlayerSettingUseOtherSite.IsOn);
            //    });
            //});

            //自动跳转进度
            swPlayerSettingAutoToPosition.IsOn = SettingHelper.GetValue(SettingHelper.Player.AUTO_TO_POSITION, true);
            swPlayerSettingAutoToPosition.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swPlayerSettingAutoToPosition.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.Player.AUTO_TO_POSITION, swPlayerSettingAutoToPosition.IsOn);
                });
            });
            //自动铺满屏幕
            swPlayerSettingAutoFullWindows.IsOn = SettingHelper.GetValue(SettingHelper.Player.AUTO_FULL_WINDOW, false);
            swPlayerSettingAutoFullWindows.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swPlayerSettingAutoFullWindows.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.Player.AUTO_FULL_WINDOW, swPlayerSettingAutoFullWindows.IsOn);
                });
            });
            //自动全屏
            swPlayerSettingAutoFullScreen.IsOn = SettingHelper.GetValue(SettingHelper.Player.AUTO_FULL_SCREEN, false);
            swPlayerSettingAutoFullScreen.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swPlayerSettingAutoFullScreen.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.Player.AUTO_FULL_SCREEN, swPlayerSettingAutoFullScreen.IsOn);
                });
            });


            //双击全屏
            swPlayerSettingDoubleClickFullScreen.IsOn = SettingHelper.GetValue(SettingHelper.Player.DOUBLE_CLICK_FULL_SCREEN, false);
            swPlayerSettingDoubleClickFullScreen.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swPlayerSettingDoubleClickFullScreen.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.Player.DOUBLE_CLICK_FULL_SCREEN, swPlayerSettingDoubleClickFullScreen.IsOn);
                });
            });

            //自动打开AI字幕
            swPlayerSettingAutoOpenAISubtitle.IsOn = SettingHelper.GetValue(SettingHelper.Player.AUTO_OPEN_AI_SUBTITLE, false);
            swPlayerSettingAutoOpenAISubtitle.Loaded += new RoutedEventHandler((sender, e) =>
            {
                swPlayerSettingAutoOpenAISubtitle.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.Player.AUTO_OPEN_AI_SUBTITLE, swPlayerSettingAutoOpenAISubtitle.IsOn);
                });
            });
            //替换CDN
            cbPlayerReplaceCDN.SelectedIndex = SettingHelper.GetValue(SettingHelper.Player.REPLACE_CDN, 3);
            cbPlayerReplaceCDN.Loaded += new RoutedEventHandler((sender, e) =>
            {
                cbPlayerReplaceCDN.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(SettingHelper.Player.REPLACE_CDN, cbPlayerReplaceCDN.SelectedIndex);
                });
            });
            //CDN服务器
            var cdnServer = SettingHelper.GetValue(SettingHelper.Player.CDN_SERVER, "upos-sz-mirrorhwo1.bilivideo.com");
            RoamingSettingCDNServer.SelectedIndex = settingVM.CDNServers.FindIndex(x => x.Server == cdnServer);
            RoamingSettingCDNServer.Loaded += new RoutedEventHandler((sender, e) =>
            {
                RoamingSettingCDNServer.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
                    var server = settingVM.CDNServers[RoamingSettingCDNServer.SelectedIndex];
                    SettingHelper.SetValue(SettingHelper.Player.CDN_SERVER, server.Server);

                });
            });
        }
        private void LoadRoaming()
        {
            //使用自定义服务器
            RoamingSettingSetDefault.Click += RoamingSettingSetDefault_Click;
            RoamingSettingCustomServer.Text = SettingHelper.GetValue(Roaming.CUSTOM_SERVER_URL, ApiHelper.ROMAING_PROXY_URL);
            RoamingSettingCustomServer.Loaded += new RoutedEventHandler((sender, e) =>
            {
                RoamingSettingCustomServer.QuerySubmitted += RoamingSettingCustomServer_QuerySubmitted;
            });

            //自定义HK服务器
            RoamingSettingCustomServerHK.Text = SettingHelper.GetValue(Roaming.CUSTOM_SERVER_URL_HK, "");
            RoamingSettingCustomServerHK.Loaded += new RoutedEventHandler((sender, e) =>
            {
                RoamingSettingCustomServerHK.QuerySubmitted += new TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs>((sender2, args) =>
                 {
                     var text = sender2.Text;
                     if (string.IsNullOrEmpty(text))
                     {
                         Utils.ShowMessageToast("已取消自定义香港代理服务器");
                         SettingHelper.SetValue(Roaming.CUSTOM_SERVER_URL_HK, "");
                         return;
                     }
                     if (!text.Contains("http"))
                     {
                         text = "https://" + text;
                     }
                     SettingHelper.SetValue(Roaming.CUSTOM_SERVER_URL_HK, text);
                     sender2.Text = text;
                     Utils.ShowMessageToast("保存成功");
                 });
            });

            //自定义TW服务器
            RoamingSettingCustomServerTW.Text = SettingHelper.GetValue(Roaming.CUSTOM_SERVER_URL_TW, "");
            RoamingSettingCustomServerTW.Loaded += new RoutedEventHandler((sender, e) =>
            {
                RoamingSettingCustomServerTW.QuerySubmitted += new TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs>((sender2, args) =>
                {
                    var text = sender2.Text;
                    if (string.IsNullOrEmpty(text))
                    {
                        Utils.ShowMessageToast("已取消自定义台湾代理服务器");
                        SettingHelper.SetValue(Roaming.CUSTOM_SERVER_URL_TW, "");
                        return;
                    }
                    if (!text.Contains("http"))
                    {
                        text = "https://" + text;
                    }
                    SettingHelper.SetValue(Roaming.CUSTOM_SERVER_URL_TW, text);
                    sender2.Text = text;
                    Utils.ShowMessageToast("保存成功");
                });
            });

            //自定义大陆服务器
            RoamingSettingCustomServerCN.Text = SettingHelper.GetValue(Roaming.CUSTOM_SERVER_URL_CN, "");
            RoamingSettingCustomServerCN.Loaded += new RoutedEventHandler((sender, e) =>
            {
                RoamingSettingCustomServerCN.QuerySubmitted += new TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs>((sender2, args) =>
                {
                    var text = sender2.Text;
                    if (string.IsNullOrEmpty(text))
                    {
                        Utils.ShowMessageToast("已取消自定义大陆代理服务器");
                        SettingHelper.SetValue(Roaming.CUSTOM_SERVER_URL_CN, "");
                        return;
                    }
                    if (!text.Contains("http"))
                    {
                        text = "https://" + text;
                    }
                    SettingHelper.SetValue(Roaming.CUSTOM_SERVER_URL_CN, text);
                    sender2.Text = text;
                    Utils.ShowMessageToast("保存成功");
                });
            });

            //Akamai
            //RoamingSettingAkamaized.IsOn = SettingHelper.GetValue<bool>(SettingHelper.Roaming.AKAMAI_CDN, false);
            //RoamingSettingAkamaized.Loaded += new RoutedEventHandler((sender, e) =>
            //{
            //    RoamingSettingAkamaized.Toggled += new RoutedEventHandler((obj, args) =>
            //    {
            //        SettingHelper.SetValue(SettingHelper.Roaming.AKAMAI_CDN, RoamingSettingAkamaized.IsOn);
            //    });
            //});
            //转简体
            RoamingSettingToSimplified.IsOn = SettingHelper.GetValue(Roaming.TO_SIMPLIFIED, true);
            RoamingSettingToSimplified.Loaded += new RoutedEventHandler((sender, e) =>
            {
                RoamingSettingToSimplified.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(Roaming.TO_SIMPLIFIED, RoamingSettingToSimplified.IsOn);
                });
            });
            //日志
            EnableLog.IsOn = SettingHelper.GetValue("EnableLog", true);
            EnableLog.Loaded += new RoutedEventHandler((sender, e) =>
            {
                EnableLog.Toggled += new RoutedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue("EnableLog", EnableLog.IsOn);
                });
            });


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

        private void LoadDanmu()
        {
            //弹幕开关
            var state = SettingHelper.GetValue(VideoDanmaku.SHOW, Visibility.Visible) == Visibility.Visible;
            DanmuSettingState.IsOn = state;
            DanmuSettingState.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue(VideoDanmaku.SHOW, DanmuSettingState.IsOn ? Visibility.Visible : Visibility.Collapsed);
            });
            //弹幕关键词
            DanmuSettingListWords.ItemsSource = settingVM.ShieldWords;

            //正则关键词
            DanmuSettingListRegulars.ItemsSource = settingVM.ShieldRegulars;

            //用户
            DanmuSettingListUsers.ItemsSource = settingVM.ShieldUsers;

            //弹幕顶部距离
            numDanmakuTopMargin.Value = GetValue<double>(VideoDanmaku.TOP_MARGIN, 0);
            numDanmakuTopMargin.Loaded += new RoutedEventHandler((sender, e) =>
            {
                numDanmakuTopMargin.ValueChanged += new TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs>((obj, args) =>
                {
                    SettingHelper.SetValue(VideoDanmaku.TOP_MARGIN, args.NewValue);
                });
            });
            //弹幕最大数量
            numDanmakuMaxNum.Value = GetValue<double>(VideoDanmaku.MAX_NUM, 0);
            numDanmakuMaxNum.Loaded += new RoutedEventHandler((sender, e) =>
            {
                numDanmakuMaxNum.ValueChanged += new TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs>((obj, args) =>
                {
                    SettingHelper.SetValue(VideoDanmaku.MAX_NUM, args.NewValue);
                });
            });
        }
        private void LoadLiveDanmu()
        {
            //弹幕开关
            var state = SettingHelper.GetValue(SettingHelper.Live.SHOW, Visibility.Visible) == Visibility.Visible;
            LiveDanmuSettingState.IsOn = state;
            LiveDanmuSettingState.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue(SettingHelper.Live.SHOW, LiveDanmuSettingState.IsOn ? Visibility.Visible : Visibility.Collapsed);
            });
            //弹幕关键词
            LiveDanmuSettingListWords.ItemsSource = settingVM.LiveWords;
        }
        private void LoadAPI()
        {
            LoadSettings();
            InitApi();
        }
        private void LoadSettings()
        {
            appkey_android_1.Text = SettingHelper.GetValue("appkey_android_1", "");
            appkey_android_2.Text = SettingHelper.GetValue("appkey_android_2", "");
            appkey_login_1.Text = SettingHelper.GetValue("appkey_login_1", "");
            appkey_login_2.Text = SettingHelper.GetValue("appkey_login_2", "");
            appkey_video_1.Text = SettingHelper.GetValue("appkey_video_1", "");
            appkey_video_2.Text = SettingHelper.GetValue("appkey_video_2", "");
            appkey_tv_1.Text = SettingHelper.GetValue("appkey_tv_1", "");
            appkey_tv_2.Text = SettingHelper.GetValue("appkey_tv_2", "");
            appkey_web_1.Text = SettingHelper.GetValue("appkey_web_1", "");
            appkey_web_2.Text = SettingHelper.GetValue("appkey_web_2", "");
            api_web_top.Text = SettingHelper.GetValue("api_web_top", "");
            api_web_secondary.Text = SettingHelper.GetValue("api_web_top", "");
        }
        private void SaveSettings()
        {
            SettingHelper.SetValue("appkey_android_1", appkey_android_1.Text);
            SettingHelper.SetValue("appkey_android_2", appkey_android_2.Text);
            SettingHelper.SetValue("appkey_login_1", appkey_login_1.Text);
            SettingHelper.SetValue("appkey_login_2", appkey_login_2.Text);
            SettingHelper.SetValue("appkey_video_1", appkey_video_1.Text);
            SettingHelper.SetValue("appkey_video_2", appkey_video_2.Text);
            SettingHelper.SetValue("appkey_tv_1", appkey_tv_1.Text);
            SettingHelper.SetValue("appkey_tv_2", appkey_tv_2.Text);
            SettingHelper.SetValue("appkey_web_1", appkey_web_1.Text);
            SettingHelper.SetValue("appkey_web_2", appkey_web_2.Text);
            SettingHelper.SetValue("api_web_top", api_web_top.Text);
            SettingHelper.SetValue("api_web_secondary", api_web_secondary.Text);
        }
        private void ApplyAPI_Click(object sender, RoutedEventArgs e)
        {
            InitApi();
        }
        private void InitApi() { 
            var lst = new List<string>();

            lst.Add(appkey_android_1.Text);
            lst.Add(appkey_android_2.Text);
            lst.Add(appkey_login_1.Text);
            lst.Add(appkey_login_2.Text);
            lst.Add(appkey_video_1.Text);
            lst.Add(appkey_video_2.Text);
            lst.Add(appkey_tv_1.Text);
            lst.Add(appkey_tv_2.Text);
            lst.Add(appkey_web_1.Text);
            lst.Add(appkey_web_2.Text);
            lst.Add(api_web_top.Text);
            lst.Add(api_web_secondary.Text);

            ApiHelper.Init(lst);
            SaveSettings();
            Utils.sp = this;
        }
        private void LoadDownlaod()
        {
            //下载路径
            txtDownloadPath.Text = SettingHelper.GetValue(Download.DOWNLOAD_PATH, Download.DEFAULT_PATH);
            DownloadOpenPath.Click += new RoutedEventHandler(async (e, args) =>
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
            DownloadChangePath.Click += new RoutedEventHandler(async (e, args) =>
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
            //旧版下载目录
            txtDownloadOldPath.Text = SettingHelper.GetValue(Download.OLD_DOWNLOAD_PATH, Download.DEFAULT_OLD_PATH);
            DownloadOpenOldPath.Click += new RoutedEventHandler(async (e, args) =>
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
            DownloadChangeOldPath.Click += new RoutedEventHandler(async (e, args) =>
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

            //并行下载
            swDownloadParallelDownload.IsOn = SettingHelper.GetValue(Download.PARALLEL_DOWNLOAD, true);
            swDownloadParallelDownload.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue(Download.PARALLEL_DOWNLOAD, swDownloadParallelDownload.IsOn);
                DownloadVM.Instance.UpdateSetting();
            });
            //付费网络下载
            swDownloadAllowCostNetwork.IsOn = SettingHelper.GetValue(Download.ALLOW_COST_NETWORK, false);
            swDownloadAllowCostNetwork.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue(Download.ALLOW_COST_NETWORK, swDownloadAllowCostNetwork.IsOn);
                DownloadVM.Instance.UpdateSetting();
            });
            //下载完成发送通知
            swDownloadSendToast.IsOn = SettingHelper.GetValue(Download.SEND_TOAST, false);
            swDownloadSendToast.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue(Download.SEND_TOAST, swDownloadSendToast.IsOn);
            });
            //下载类型
            cbDownloadVideoType.SelectedIndex = SettingHelper.GetValue(Download.DEFAULT_VIDEO_TYPE, 1);
            cbDownloadVideoType.Loaded += new RoutedEventHandler((sender, e) =>
            {
                cbDownloadVideoType.SelectionChanged += new SelectionChangedEventHandler((obj, args) =>
                {
                    SettingHelper.SetValue(Download.DEFAULT_VIDEO_TYPE, cbDownloadVideoType.SelectedIndex);
                });
            });
            //加载旧版本下载的视频
            swDownloadLoadOld.IsOn = SettingHelper.GetValue(Download.LOAD_OLD_DOWNLOAD, false);
            swDownloadLoadOld.Toggled += new RoutedEventHandler((e, args) =>
            {
                SettingHelper.SetValue(Download.LOAD_OLD_DOWNLOAD, swDownloadLoadOld.IsOn);
            });
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
                dbginfo.Text = await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Text/dbginfo.md")));
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

        // 在代码中定义一个名为 Log 的方法，用于向日志文本框中添加新的日志
        private readonly int loglength = 10000;
        public void Log(StringBuilder logBuilder)
        {
            int leng = logBuilder.ToString().Length;
            if (leng > loglength * 2)
            {
                logBuilder.Remove(0, leng - loglength);
            }
            //这个地方老是崩
            try
            {
                // 将 StringBuilder 对象中的所有内容显示到日志文本框中
                if (EnableLog.IsOn)
                    logTextBox.Text = logBuilder.ToString();
            }
            catch (Exception e) { }
        }

        private async void DanmuSettingAddWord_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(DanmuSettingTxtWord.Text))
            {
                Utils.ShowMessageToast("关键词不能为空");
                return;
            }
            settingVM.ShieldWords.Add(DanmuSettingTxtWord.Text);
            SettingHelper.SetValue(VideoDanmaku.SHIELD_WORD, settingVM.ShieldWords);
            var result = await settingVM.AddDanmuFilterItem(DanmuSettingTxtWord.Text, 0);
            DanmuSettingTxtWord.Text = "";
            if (!result)
            {
                Utils.ShowMessageToast("已经添加到本地，但远程同步失败");
            }
        }

        private async void DanmuSettingSyncWords_Click(object sender, RoutedEventArgs e)
        {
            await settingVM.SyncDanmuFilter();
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
            if (string.IsNullOrEmpty(DanmuSettingTxtRegex.Text))
            {
                Utils.ShowMessageToast("正则表达式不能为空");
                return;
            }
            var txt = DanmuSettingTxtRegex.Text.Trim('/');
            settingVM.ShieldRegulars.Add(txt);
            SettingHelper.SetValue(VideoDanmaku.SHIELD_REGULAR, settingVM.ShieldRegulars);
            var result = await settingVM.AddDanmuFilterItem(txt, 1);
            DanmuSettingTxtRegex.Text = "";
            if (!result)
            {
                Utils.ShowMessageToast("已经添加到本地，但远程同步失败");
            }
        }

        private async void DanmuSettingAddUser_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(DanmuSettingTxtUser.Text))
            {
                Utils.ShowMessageToast("用户ID不能为空");
                return;
            }
            settingVM.ShieldUsers.Add(DanmuSettingTxtUser.Text);
            SettingHelper.SetValue(VideoDanmaku.SHIELD_WORD, settingVM.ShieldUsers);
            var result = await settingVM.AddDanmuFilterItem(DanmuSettingTxtUser.Text, 2);
            DanmuSettingTxtUser.Text = "";
            if (!result)
            {
                Utils.ShowMessageToast("已经添加到本地，但远程同步失败");
            }
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

        private void LiveDanmuSettingAddWord_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(LiveDanmuSettingTxtWord.Text))
            {
                Utils.ShowMessageToast("关键字不能为空");
                return;
            }
            if (!settingVM.LiveWords.Contains(LiveDanmuSettingTxtWord.Text))
            {
                settingVM.LiveWords.Add(LiveDanmuSettingTxtWord.Text);
                SettingHelper.SetValue(SettingHelper.Live.SHIELD_WORD, settingVM.LiveWords);
            }

            DanmuSettingTxtWord.Text = "";
            SettingHelper.SetValue(SettingHelper.Live.SHIELD_WORD, settingVM.LiveWords);
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
        private async Task<LoginCallbackModel> HandleLoginResult(int code, string message, LoginResultModel result)
        {
            if (code == 0)
            {
                if (result.status == 0)
                {
                    dbgacc.Text = result.token_info.access_token;
                    dbgtoken.Text = result.token_info.refresh_token;
                    dbgmsg.Text = result.token_info.mid + "&" + result.sso + "&" + result.cookie_info;
                    return new LoginCallbackModel()
                    {
                        status = LoginStatus.Success,
                        message = "登录成功"
                    };
                }
                if (result.status == 1 || result.status == 2)
                {
                    dbgmsg.Text = "需要安全验证";
                    return new LoginCallbackModel()
                    {
                        status = LoginStatus.NeedValidate,
                        message = "本次登录需要安全验证",
                        url = result.url
                    };
                }
                dbgmsg.Text = result.message;
                return new LoginCallbackModel()
                {
                    status = LoginStatus.Fail,
                    message = result.message
                };
            }
            else if (code == -105)
            {
                dbgmsg.Text = "需要验证码";
                return new LoginCallbackModel()
                {
                    status = LoginStatus.NeedCaptcha,
                    url = result.url,
                    message = "登录需要验证码"
                };
            }
            else
            {
                dbgmsg.Text = message;
                return new LoginCallbackModel()
                {
                    status = LoginStatus.Fail,
                    message = message
                };
            }

        }
        Api.AccountApi api;
        public async Task<string> EncryptedPassword(string passWord)
        {
            string base64String;
            try
            {
                HttpBaseProtocolFilter httpBaseProtocolFilter = new HttpBaseProtocolFilter();
                httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(Windows.Security.Cryptography.Certificates.ChainValidationResult.Expired);
                httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(Windows.Security.Cryptography.Certificates.ChainValidationResult.Untrusted);
                var jObjects = (await api.GetKey2023().Request()).GetJObject();
                string str = jObjects["data"]["hash"].ToString();
                string str1 = jObjects["data"]["key"].ToString();
                string str2 = string.Concat(str, passWord);
                string str3 = Regex.Match(str1, "BEGIN PUBLIC KEY-----(?<key>[\\s\\S]+)-----END PUBLIC KEY").Groups["key"].Value.Trim();
                byte[] numArray = Convert.FromBase64String(str3);
                AsymmetricKeyAlgorithmProvider asymmetricKeyAlgorithmProvider = AsymmetricKeyAlgorithmProvider.OpenAlgorithm(AsymmetricAlgorithmNames.RsaPkcs1);
                CryptographicKey cryptographicKey = asymmetricKeyAlgorithmProvider.ImportPublicKey(WindowsRuntimeBufferExtensions.AsBuffer(numArray), 0);
                IBuffer buffer = CryptographicEngine.Encrypt(cryptographicKey, WindowsRuntimeBufferExtensions.AsBuffer(Encoding.UTF8.GetBytes(str2)), null);
                base64String = Convert.ToBase64String(WindowsRuntimeBufferExtensions.ToArray(buffer));
            }
            catch (Exception)
            {
                base64String = passWord;
            }
            return base64String;
        }
        private async void DoLogin(object sender, RoutedEventArgs e) {
            var user = dbguser.Text;
            var password = dbgpwd.Text;
            var val=dbgval.Text;
            var cap = dbgcap.Text;
            var challenge = dbgch.Text;
            if (string.IsNullOrEmpty(val))
            {
                //第一步，啥都没
                var pwd = await EncryptedPassword(password);
                var results = await api.Login2023(user, pwd).Request();
                if (results.status)
                {
                    var data = await results.GetData<LoginResultModel>();
                    var result = await HandleLoginResult(data.code, data.message, data.data);
                    HandleResult(result);
                }
                else
                {
                    Utils.ShowMessageToast(results.message);
                }
            }
            else
            {
                //带验证码登录
                var pwd = await EncryptedPassword(password);
                var results = await api.Login2023(user, pwd,challenge,cap,val).Request();
                if (results.status)
                {
                    var data = await results.GetData<LoginResultModel>();
                    var result = await HandleLoginResult(data.code, data.message, data.data);
                    HandleResult(result);
                }
                else
                {
                    Utils.ShowMessageToast(results.message);
                }
            }



        }
        private void HandleResult(LoginCallbackModel result)
        {
            var uri = new Uri(string.IsNullOrEmpty(result.url) ? "https://www.bilibili.com" : result.url);
            switch (result.status)
            {
                case LoginStatus.Success:
                    break;
                case LoginStatus.Fail:
                case LoginStatus.Error:
                    break;
                case LoginStatus.NeedCaptcha:
                    string gt = Regex.Match(uri.Query, "gee_gt=(.*?)&").Groups[1].Value;
                    string cha = Regex.Match(uri.Query, "gee_challenge=(.*?)&").Groups[1].Value;
                    string cap= Regex.Match(uri.Query, "recaptcha_token=(.*?)&").Groups[1].Value;
                    dbggt.Text = gt;
                    dbgch.Text = cha;
                    dbgcap.Text = cap;
                    break;
                case LoginStatus.NeedValidate:
                    dbgcap.Text = result.url;
                    break;
                default:
                    break;
            }
        }
        private void ShowTokenButton_Click(object sender, RoutedEventArgs e)
        {
            TokenTextBox.Text = SettingHelper.Account.AccessKey;
        }

        private async void OpenTheme_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Themes");
                var success = await Windows.System.Launcher.LaunchFolderAsync(folder);
                if (!success)
                {
                    Utils.ShowMessageToast("无法打开Themes文件夹");
                }
            }
            catch(Exception ex)
            {
                Utils.ShowMessageToast("找不到Themes文件夹");
            }
        }

        private async void BurnTokenButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

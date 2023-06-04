﻿using BiliLite.Helpers;
using BiliLite.Modules;
using FontAwesome5;
using Microsoft.Toolkit.Uwp.Connectivity;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliLite.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class HomePage : Page
    {
        DownloadVM downloadVM;
        readonly HomeVM homeVM;
        readonly Account account;
        public HomePage()
        {
            this.InitializeComponent();
            MessageCenter.LoginedEvent += MessageCenter_LoginedEvent;
            MessageCenter.LogoutedEvent += MessageCenter_LogoutedEvent;
            homeVM = new HomeVM();
            account = new Account();
            downloadVM = DownloadVM.Instance;
            this.DataContext = homeVM;
        }
        private void MessageCenter_LogoutedEvent(object sender, EventArgs e)
        {
            LoadUserStatus();
        }

        private void MessageCenter_LoginedEvent(object sender, object e)
        {
            LoadUserStatus();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.New && homeVM.IsLogin && homeVM.Profile == null)
            {
                CheckLoginStatus();
                //await homeVM.LoginUserCard();
            }
        }
        private async void CheckLoginStatus()
        {
            if (!NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable)
            {
                // return;
            }

            if (SettingHelper.Account.Logined)
            {
                try
                {
                    if (await account.CheckLoginState())
                    {
                        await homeVM.LoginUserCard();
                    }
                    else
                    {
                        var result = await account.RefreshToken();
                        if (!result)
                        {
                            homeVM.IsLogin = false;
                            MessageCenter.SendLogout();
                            Utils.ShowMessageToast("登录过期，请重新登录");
                            await Utils.ShowLoginDialog();
                        }
                    }
                }
                catch (Exception ex)
                {
                    homeVM.IsLogin = false;
                    LogHelper.Log("读取access_key信息失败", LogType.INFO, ex);
                    Utils.ShowMessageToast("读取登录信息失败，请重新登录");
                    //throw;
                }

            }
        }


        private async void LoadUserStatus()
        {
            if (SettingHelper.Account.Logined)
            {
                homeVM.IsLogin = true;
                await homeVM.LoginUserCard();
                foreach (var item in homeVM.HomeNavItems)
                {
                    if (!item.Show && item.NeedLogin) item.Show = true;
                }
            }
            else
            {
                homeVM.IsLogin = false;
                foreach (var item in homeVM.HomeNavItems)
                {
                    if (item.Show && item.NeedLogin) item.Show = false;
                }
            }
        }

        private void btnSetting_Click(object sender, RoutedEventArgs e)
        {
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Setting,
                page = typeof(SettingPage),
                title = "设置"
            });
        }

        private void navView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            var item = args.SelectedItem as HomeNavItem;
            frame.Navigate(item.Page, item.Parameters);
            this.UpdateLayout();
        }


        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            var data = await Utils.ShowLoginDialog();
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MessageCenter.SendLogout();
            UserFlyout.Hide();
        }

        private void btnDownlaod_Click(object sender, RoutedEventArgs e)
        {
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Download,
                page = typeof(DownloadPage),
                title = "下载",

            });
        }

        private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
            {
                Utils.ShowMessageToast("关键字不能为空");
                return;
            }

            if (await MessageCenter.HandleUrl(SearchBox.Text))
            {
                return;
            }

            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Find,
                page = typeof(SearchPage),
                title = "搜索:" + SearchBox.Text,
                parameters = new SearchParameter()
                {
                    keyword = SearchBox.Text,
                    searchType = SearchType.Video
                }
            });
        }

        private async void MenuMyFollow_Click(object sender, RoutedEventArgs e)
        {
            if (!SettingHelper.Account.Logined && !await Utils.ShowLoginDialog())
            {
                Utils.ShowMessageToast("请先登录");
                return;
            }
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.OutlineStar,
                page = typeof(User.FavoritePage),
                title = "我的收藏",
                parameters = User.OpenFavoriteType.Video
            });
        }

        private async void MenuMyLive_Click(object sender, RoutedEventArgs e)
        {
            if (!SettingHelper.Account.Logined && !await Utils.ShowLoginDialog())
            {
                Utils.ShowMessageToast("请先登录");
                return;
            }
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Contact,
                page = typeof(Live.LiveCenterPage),
                title = "直播中心",

            });
        }


        private void MenuHistory_Click(object sender, RoutedEventArgs e)
        {
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Clock,
                page = typeof(User.HistoryPage),
                title = "历史记录"
            });
        }

        private void MenuUserCenter_Click(object sender, RoutedEventArgs e)
        {
            if (SettingHelper.Account.Profile is null)
            {
                Utils.ShowMessageToast("无法获取用户名");
                return;
            }
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Contact,
                title = SettingHelper.Account.Profile.name,
                page = typeof(UserInfoPage),
                parameters = SettingHelper.Account.UserID
            });
        }

        private void MenuMessage_Click(object sender, RoutedEventArgs e)
        {
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Message,
                title = "消息中心",
                page = typeof(WebPage),
                parameters = $"https://message.bilibili.com/#whisper"
            });
        }

        private async void MenuWatchlater_Click(object sender, RoutedEventArgs e)
        {
            if (!SettingHelper.Account.Logined && !await Utils.ShowLoginDialog())
            {
                Utils.ShowMessageToast("请先登录");
                return;
            }
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Play,
                page = typeof(User.WatchlaterPage),
                title = "稍后再看",

            });
        }

        private void btnOpenFans_Click(object sender, RoutedEventArgs e)
        {
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Contact,
                title = SettingHelper.Account.Profile.name,
                page = typeof(UserInfoPage),
                parameters = new UserInfoParameter()
                {
                    Mid = SettingHelper.Account.UserID.ToString(),
                    Tab = UserTab.Fans
                }
            });
            //MessageCenter.NavigateToPage(this, new NavigationInfo()
            //{
            //    icon = Symbol.World,
            //    page = typeof(WebPage),
            //    title = "我的好友",
            //    parameters = "https://space.bilibili.com/h5/follow"
            //});
        }
        private void btnOpenAttention_Click(object sender, RoutedEventArgs e)
        {
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Contact,
                title = SettingHelper.Account.Profile.name,
                page = typeof(UserInfoPage),
                parameters = new UserInfoParameter()
                {
                    Mid = SettingHelper.Account.UserID.ToString(),
                    Tab = UserTab.Attention
                }
            });
        }

        private void btnOpenDynamic_Click(object sender, RoutedEventArgs e)
        {
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Contact,
                title = SettingHelper.Account.Profile.name,
                page = typeof(UserInfoPage),
                parameters = new UserInfoParameter()
                {
                    Mid = SettingHelper.Account.UserID.ToString(),
                    Tab = UserTab.Dynamic
                }
            });
        }

        private void btnUser_Click(object sender, RoutedEventArgs e)
        {
            homeVM.LoginUserCard();
        }
        private ElementTheme theme
        {
            get
            {
                return homeVM.ThemeIcon == EFontAwesomeIcon.Regular_Sun ? ElementTheme.Light : ElementTheme.Dark;
            }
            set
            {
                homeVM.ThemeIcon = value == ElementTheme.Dark ? EFontAwesomeIcon.Regular_Moon : EFontAwesomeIcon.Regular_Sun;
            }
        }

        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            var _theme = rootFrame.RequestedTheme;
            var savedtheme = ElementTheme.Dark;
            var apptheme=ApplicationTheme.Dark;
            if (_theme == ElementTheme.Light)
            {
                savedtheme = ElementTheme.Dark;
                apptheme = ApplicationTheme.Dark;
            }
            else
            {
                savedtheme = ElementTheme.Light;
                apptheme=ApplicationTheme.Light;
            }
            theme = savedtheme;
            SettingHelper.SetValue(SettingHelper.UI.THEME, savedtheme == ElementTheme.Light ? 1 : 2);
            rootFrame.RequestedTheme = savedtheme;
            //App.Current.RequestedTheme = apptheme;//报错
            //App.ExtendAcrylicIntoTitleBar();
        }
    }


}

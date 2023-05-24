using BiliLite.Helpers;
using BiliLite.Models;
using BiliLite.Modules;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliLite.Pages.Home
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MoviePage : Page
    {
        private readonly Modules.CinemaVM cinemaVM;

        public MoviePage()
        {
            this.InitializeComponent();
            cinemaVM = new Modules.CinemaVM();
            if (SettingHelper.GetValue<bool>(SettingHelper.UI.CACHE_HOME, true))
            {
                this.NavigationCacheMode = NavigationCacheMode.Enabled;
            }
            else
            {
                this.NavigationCacheMode = NavigationCacheMode.Disabled;
            }
            MessageCenter.LoginedEvent += MessageCenter_LoginedEvent;
            MessageCenter.LogoutedEvent += MessageCenter_LogoutedEvent;
        }

        private void MessageCenter_LogoutedEvent(object sender, EventArgs e)
        {
            cinemaVM.ShowFollows = false;
        }

        private async void MessageCenter_LoginedEvent(object sender, object e)
        {
            cinemaVM.ShowFollows = true;
            await cinemaVM.GetFollows();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.New && cinemaVM.HomeData == null)
            {
                await LoadData();
            }
        }

        private async Task LoadData()
        {
            await cinemaVM.GetCinemaHome();
            if (SettingHelper.Account.Logined)
            {
                cinemaVM.ShowFollows = true;
                await cinemaVM.GetFollows();
            }
        }

        private async void btnLoadMoreFall_Click(object sender, RoutedEventArgs e)
        {
            var element = (sender as HyperlinkButton);
            var data = element.DataContext as CinemaHomeFallModel;
            await cinemaVM.GetFallMore(element.DataContext as CinemaHomeFallModel);
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async void gvFall_ItemClick(object sender, ItemClickEventArgs e)
        {
            var result = await MessageCenter.HandleUrl((e.ClickedItem as CinemaHomeFallItemModel).link);
            if (!result)
            {
                Utils.ShowMessageToast("不支持打开的链接");
            }
        }

        private async void RefreshContainer_RefreshRequested(Microsoft.UI.Xaml.Controls.RefreshContainer sender, Microsoft.UI.Xaml.Controls.RefreshRequestedEventArgs args)
        {
            await LoadData();
        }

        private async void BannerItem_Click(object sender, RoutedEventArgs e)
        {
            var result = await MessageCenter.HandleUrl(((sender as HyperlinkButton).DataContext as CinemaHomeBannerModel).url);
            if (!result)
            {
                Utils.ShowMessageToast("不支持打开的链接");
            }
        }

        private void OpenDocumentaryIndex_Click(object sender, RoutedEventArgs e)
        {
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Filter,
                page = typeof(Bangumi.AnimeIndexPage),
                title = "纪录片索引",
                parameters = new SeasonIndexParameter()
                {
                    type = IndexSeasonType.Documentary
                }
            });
        }

        private void OpenMovieIndex_Click(object sender, RoutedEventArgs e)
        {
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Filter,
                page = typeof(Bangumi.AnimeIndexPage),
                title = "电影索引",
                parameters = new SeasonIndexParameter()
                {
                    type = IndexSeasonType.Movie
                }
            });
        }

        private void OpenTVIndex_Click(object sender, RoutedEventArgs e)
        {
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Filter,
                page = typeof(Bangumi.AnimeIndexPage),
                title = "电视剧索引",
                parameters = new SeasonIndexParameter()
                {
                    type = IndexSeasonType.TV
                }
            });
        }

        private async void btnOpenMyFollow_Click(object sender, RoutedEventArgs e)
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
                parameters = User.OpenFavoriteType.Cinema
            });
        }

        private void OpenVarietyIndex_Click(object sender, RoutedEventArgs e)
        {
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Filter,
                page = typeof(Bangumi.AnimeIndexPage),
                title = "综艺索引",
                parameters = new SeasonIndexParameter()
                {
                    type = IndexSeasonType.Variety
                }
            });
        }

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as PageEntranceModel;
            MessageCenter.NavigateToPage(this, item.NavigationInfo);
        }
    }
}
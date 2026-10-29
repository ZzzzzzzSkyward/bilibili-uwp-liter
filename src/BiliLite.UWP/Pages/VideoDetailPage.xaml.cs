using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using BiliLite.Modules;
using BiliLite.Models.Common;
using Microsoft.UI.Xaml.Controls;
using BiliLite.Helpers;
using Windows.ApplicationModel.DataTransfer;
using BiliLite.Controls;
using Windows.System;
using BiliLite.Dialogs;
using System.Threading.Tasks;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliLite.Pages
{
    public class VideoPlaylist
    {
        public int Index { get; set; }
        public List<VideoPlaylistItem> Playlist { get; set; }
    }
    public class VideoPlaylistItem
    {
        public string ID { get; set; }
        public string Author { get; set; }
        public string Cover { get; set; }
        public string Title { get; set; }
    }
    public class VideoUgcSeasonEpisodeView
    {
        public string Aid { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Cover { get; set; }
        public string Duration { get; set; }
    }
    public sealed partial class VideoDetailPage : PlayPage
    {
        //判断是否按下mod键
        private bool IsDown(Windows.System.VirtualKey key)
        {
            return Window.Current.CoreWindow.GetKeyState(key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        }
        private bool NoMod()
        {
            return !IsDown(Windows.System.VirtualKey.Shift) &&
                !IsDown(Windows.System.VirtualKey.Control) &&
                !IsDown(Windows.System.VirtualKey.Menu) &&
                !IsDown(Windows.System.VirtualKey.LeftWindows) &&
                !IsDown(Windows.System.VirtualKey.RightWindows);
        }
        VideoDetailVM videoDetailVM;
        string avid = "";
        string bvid = "";
        bool is_bvid = false;
        public VideoDetailPage()
        {
            this.InitializeComponent();
            Title = "视频详情";
            this.Loaded += VideoDetailPage_Loaded;
            this.Unloaded += VideoDetailPage_Unloaded;
            this.Player = this.player;
            NavigationCacheMode = NavigationCacheMode.Enabled;
            videoDetailVM = new VideoDetailVM();
            this.DataContext = videoDetailVM;
            this.RightInfo.Width = new GridLength(SettingHelper.GetValue<double>(SettingHelper.UI.RIGHT_DETAIL_WIDTH, 320), GridUnitType.Pixel);

        }
        private async void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            var elent = FocusManager.GetFocusedElement();
            if (elent is TextBox || elent is AutoSuggestBox)
            {
                args.Handled = false;
                return;
            }
            args.Handled = true;
            switch (args.VirtualKey)
            {
                case VirtualKey.F5:
                    if (NoMod())
                    {
                        if (videoDetailVM.Loading) return;
                        await InitializeVideo(_id);
                    }
                    break;
                default:
                    //Utils.ShowMessageToast("按键", args.VirtualKey.ToString());
                    break;
            }
        }


        private void VideoDetailPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
        }
        private void VideoDetailPage_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            SubscribeClosedPage();
        }

        private void SubscribeClosedPage()
        {
            if (this.Parent is MyFrame frame)
            {
                frame.ClosedPage -= VideoDetailPage_ClosedPage;
                frame.ClosedPage += VideoDetailPage_ClosedPage;
            }
        }

        private void UnsubscribeClosedPage()
        {
            if (this.Parent is MyFrame frame)
            {
                frame.ClosedPage -= VideoDetailPage_ClosedPage;
            }
        }

        private void SubscribeShare()
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested -= DataTransferManager_DataRequested;
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;
        }

        private void ReleaseShare()
        {
            DataTransferManager.GetForCurrentView().DataRequested -= DataTransferManager_DataRequested;
        }

        private void VideoDetailPage_ClosedPage(object sender, EventArgs e)
        {
            ClosePage();
            //AddToHistory(_id);
        }
        private void ClosePage()
        {
            UnsubscribeClosedPage();
            ReleaseShare();
            if (videoDetailVM != null)
            {
                videoDetailVM.Loaded = false;
                videoDetailVM.Loading = true;
                videoDetailVM.VideoInfo = null;
            }
            RemoveUgcSeason();
            changedFlag = true;
            player?.FullScreen(false);
            player?.MiniWidnows(false);
            player?.Dispose();
        }
        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;
            request.Data.Properties.Title = videoDetailVM.VideoInfo.title;
            request.Data.SetWebLink(new Uri(videoDetailVM.VideoInfo.short_link));
        }
        VideoPlaylist playlist;
        PivotItem playlistPivotItem;
        PivotItem ugcSeasonPivotItem;
        readonly List<ListView> ugcSeasonEpisodeLists = new List<ListView>();
        string ugcSeasonAid;
        bool flag = false;
        string _id = "";
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SubscribeClosedPage();
            SubscribeShare();

            if (e.NavigationMode == NavigationMode.New)
            {
                if(SettingHelper.GetValue<bool>(SettingHelper.Player.AUTO_FULL_SCREEN, false))
                {
                    player.IsFullScreen = true;
                }
                else
                {
                    player.IsFullWindow = SettingHelper.GetValue<bool>(SettingHelper.Player.AUTO_FULL_WINDOW, false);
                }
                pivot.SelectedIndex = SettingHelper.GetValue<int>(SettingHelper.UI.DETAIL_DISPLAY, 0);
                if (e.Parameter is VideoPlaylist)
                {
                    playlist = e.Parameter as VideoPlaylist;

                  
                    var element = PlayListTpl.GetElement(new Windows.UI.Xaml.ElementFactoryGetArgs()) as PivotItem;
                    element.DataContext = playlist;
                 
                    playlistPivotItem = element;
                    pivot.Items.Insert(0, element);
                    pivot.SelectedIndex = 0;
                    await InitializeVideo(playlist.Playlist[playlist.Index].ID);
                }
                else
                {
                    var id = e.Parameter.ToString();
                    await InitializeVideo(id);
                }

            }
            else
            {
                Title = videoDetailVM?.VideoInfo?.title ?? "视频详情";
                MessageCenter.ChangeTitle(Title); 
            }

        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ReleaseShare();
            UnsubscribeClosedPage();
            base.OnNavigatedFrom(e);
        }
        private async Task InitializeVideo(string id)
        {
            _id = id;
            if (flag) return;
            flag = true;
            if (long.TryParse(id, out var aid))
            {
                avid = id;
                is_bvid = false;
            }
            else
            {
                bvid = id;
                is_bvid = true;
            }
            await videoDetailVM.LoadVideoDetail(id, is_bvid);
            if (this.VideoCover != null)
            {
                this.VideoCover.Visibility = SettingHelper.GetValue<bool>(SettingHelper.UI.SHOW_DETAIL_COVER, true) ? Visibility.Visible : Visibility.Collapsed;
            }
            if (SettingHelper.GetValue<bool>("一键三连提示", true))
            {
                SettingHelper.SetValue("一键三连提示", false);
                Utils.ShowMessageToast("右键或长按点赞按钮可以一键三连哦~", 5);
            }
            if (videoDetailVM.VideoInfo != null)
            {
                avid = videoDetailVM.VideoInfo.aid;
                var desc = ControlHelper.StringToRichText(videoDetailVM.VideoInfo.desc, null);

                contentDesc.Content = desc;
                ChangeTitle(videoDetailVM.VideoInfo.title);
                CreateQR();
                if (!string.IsNullOrEmpty(videoDetailVM.VideoInfo.redirect_url))
                {
                    var result = await MessageCenter.HandelSeasonID(videoDetailVM.VideoInfo.redirect_url);
                    if (!string.IsNullOrEmpty(result))
                    {
                        this.Frame.Navigate(typeof(SeasonDetailPage), result);
                        //从栈中移除当前页面的历史
                        this.Frame.BackStack.Remove(this.Frame.BackStack.FirstOrDefault(x => x.SourcePageType == this.GetType()));
                        return;
                    }
                }
                List<PlayInfo> playInfos = new List<PlayInfo>();
                int i = 0;
                foreach (var item in videoDetailVM.VideoInfo.pages)
                {
                    playInfos.Add(new PlayInfo()
                    {
                        avid = videoDetailVM.VideoInfo.aid,
                        cid = item.cid,
                        duration = item.duration,
                        is_interaction = videoDetailVM.VideoInfo.interaction != null,
                        order = i,
                        play_mode = VideoPlayType.Video,
                        title = "P" + item.page + " " + item.part,
                        area = Utils.ParseArea(videoDetailVM.VideoInfo.title,videoDetailVM.VideoInfo.owner.mid)
                    });
                    i++;
                }
                var index = 0;
                if (videoDetailVM.VideoInfo.history != null)
                {
                    var history = videoDetailVM.VideoInfo.pages.FirstOrDefault(x => x.cid.Equals(videoDetailVM.VideoInfo.history.cid));
                    if (history != null)
                    {
                        SettingHelper.SetValue<double>(history.cid, Convert.ToDouble(videoDetailVM.VideoInfo.history.progress));
                        index = videoDetailVM.VideoInfo.pages.IndexOf(history);
                        //player.InitializePlayInfo(playInfos, );
                    }
                }
                player.InitializePlayInfo(playInfos, index);
                comment.LoadComment(new LoadCommentInfo()
                {
                    CommentMode = (int)Api.CommentApi.CommentType.Video,
                    CommentSort = Api.CommentApi.CommentSort.Hot,
                    Oid = videoDetailVM.VideoInfo.aid
                });
            }
            ApplyUgcSeason();
            flag = false;
        }

        /// <summary>
        /// 合集信息就在详情响应里，这里只刷新页签，不再单独请求
        /// </summary>
        private void ApplyUgcSeason()
        {
            var info = videoDetailVM.VideoInfo;
            if (info == null || !info.showUgcSeason)
            {
                RemoveUgcSeason();
                return;
            }

            // 用详情里的 aid 选中当前视频，按BV打开时也能选中同一行
            ugcSeasonAid = info.aid;
            ugcSeasonEpisodeLists.Clear();

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = info.UgcSeason.Title,
                FontSize = 20,
                FontWeight = Windows.UI.Text.FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(12, 12, 12, 0),
            });
            if (!string.IsNullOrEmpty(info.UgcSeason.Intro))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = info.UgcSeason.Intro,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(12, 8, 12, 0),
                });
            }

            foreach (var section in info.UgcSeason.Sections)
            {
                if (section?.Episodes == null || section.Episodes.Count == 0) continue;

                var header = new TextBlock
                {
                    Text = section.Title,
                    FontSize = 16,
                    FontWeight = Windows.UI.Text.FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(12, 16, 12, 4),
                };
                panel.Children.Add(header);

                var listView = new ListView()
                {
                    ItemsSource = section.Episodes.Select(ToUgcSeasonEpisodeView).ToList(),
                    ItemTemplate = (DataTemplate)Resources["UgcSeasonEpisodeTpl"],
                    ItemContainerStyle = (Style)Resources["UgcSeasonEpisodeItemStyle"],
                    SelectionMode = ListViewSelectionMode.Single,
                };
                // 分集列表整体滚动，不要各自滚动
                ScrollViewer.SetVerticalScrollMode(listView, ScrollMode.Disabled);
                ScrollViewer.SetVerticalScrollBarVisibility(listView, ScrollBarVisibility.Disabled);
                listView.SelectionChanged += UgcSeasonEpisode_SelectionChanged;
                panel.Children.Add(listView);
                ugcSeasonEpisodeLists.Add(listView);
            }

            if (ugcSeasonEpisodeLists.Count == 0)
            {
                RemoveUgcSeason();
                return;
            }

            if (ugcSeasonPivotItem == null)
            {
                ugcSeasonPivotItem = new PivotItem()
                {
                    Margin = new Thickness(0),
                    Header = new TextBlock() { Text = "合集", FontSize = 18 },
                };
            }
            ugcSeasonPivotItem.Content = new ScrollViewer()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = panel,
            };

            // 已经有合集页签时只换内容，避免刷新、切换视频后出现重复页签
            if (!pivot.Items.Contains(ugcSeasonPivotItem))
            {
                var selectedItem = pivot.SelectedItem;
                pivot.Items.Insert(0, ugcSeasonPivotItem);
                if (selectedItem != null) pivot.SelectedItem = selectedItem;
            }

            SelectUgcSeasonEpisode();
        }

        private void SelectUgcSeasonEpisode()
        {
            foreach (var listView in ugcSeasonEpisodeLists)
            {
                var episodes = listView.ItemsSource as List<VideoUgcSeasonEpisodeView>;
                listView.SelectedItem = episodes?.FirstOrDefault(x => x.Aid == ugcSeasonAid && !string.IsNullOrEmpty(x.Aid));
            }
        }

        private void RemoveUgcSeason()
        {
            ugcSeasonAid = null;
            ugcSeasonEpisodeLists.Clear();
            if (ugcSeasonPivotItem == null) return;
            ugcSeasonPivotItem.Content = null;
            if (pivot.Items.Contains(ugcSeasonPivotItem))
            {
                pivot.Items.Remove(ugcSeasonPivotItem);
            }
        }

        private async void UgcSeasonEpisode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = sender as ListView;
            var item = listView?.SelectedItem as VideoUgcSeasonEpisodeView;
            // 选中当前播放的视频时不重新加载，只换一个分集时才播放并清掉其它分区的高亮
            if (item == null || item.Aid == ugcSeasonAid) return;
            if (string.IsNullOrEmpty(item.Aid)) return;
            foreach (var other in ugcSeasonEpisodeLists)
            {
                if (!ReferenceEquals(other, listView)) other.SelectedItem = null;
            }
            await InitializeVideo(item.Aid);
        }

        private static VideoUgcSeasonEpisodeView ToUgcSeasonEpisodeView(VideoUgcSeasonSectionEpisode episode)
        {
            return new VideoUgcSeasonEpisodeView()
            {
                Aid = episode.Aid,
                Title = episode.Title,
                Author = episode.Author?.Name,
                Cover = episode.Cover,
                Duration = FormatDuration(episode.Page?.Duration ?? 0),
            };
        }

        private static string FormatDuration(long seconds)
        {
            if (seconds < 0) seconds = 0;
            var duration = TimeSpan.FromSeconds(seconds);
            return seconds >= 3600 ? duration.ToString(@"hh\:mm\:ss") : duration.ToString(@"mm\:ss");
        }
        private void CreateQR()
        {
            try
            {
                ZXing.BarcodeWriter barcodeWriter = new ZXing.BarcodeWriter();
                barcodeWriter.Format = ZXing.BarcodeFormat.QR_CODE;
                barcodeWriter.Options = new ZXing.Common.EncodingOptions()
                {
                    Margin = 1,
                    Height = 200,
                    Width = 200
                };
                var data = barcodeWriter.Write(videoDetailVM.VideoInfo.short_link);
                imgQR.Source = data;
            }
            catch (Exception ex)
            {
                LogHelper.Log("创建二维码失败avid" + avid, LogType.ERROR, ex);
                Utils.ShowMessageToast("创建二维码失败");
            }

        }
       
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back || e.SourcePageType.Name == "BlankPage")
            {
                ClosePage();
            }
               
            base.OnNavigatingFrom(e);
        }
        public void ChangeTitle(string title)
        {
            if ((this.Parent as Frame).Parent is TabViewItem)
            {
                if (this.Parent != null)
                {
                    ((this.Parent as Frame).Parent as TabViewItem).Header = title;
                }
            }
            else
            {
                MessageCenter.ChangeTitle(title);
            }
        }

        private void txtDesc_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (stDesc.MaxHeight > 80)
            {
                stDesc.MaxHeight = 80;
            }
            else
            {
                stDesc.MaxHeight = 1000.0;
            }
            Utils.ShowMessageToast("右键或长按可以复制内容");
        }

        private void txtDesc_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (Utils.SetClipboard((sender as TextBlock).Text))
            {
                Utils.ShowMessageToast("已将内容复制到剪切板");
            }
            else
            {
                Utils.ShowMessageToast("复制失败");
            }
        }

        private void txtDesc_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (Utils.SetClipboard((sender as TextBlock).Text))
            {
                Utils.ShowMessageToast("已将内容复制到剪切板");
            }
            else
            {
                Utils.ShowMessageToast("复制失败");
            }
        }

        private async void btnAttention_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as Button).DataContext as VideoDetailStaffModel;
            var result = await videoDetailVM.AttentionUP(data.mid, data.attention == 1 ? 2 : 1);
            if (result)
            {
                if (data.attention == 1)
                {
                    data.attention = 0;
                }
                else
                {
                    data.attention = 1;
                }
            }

        }


        private void listRelates_ItemClick(object sender, ItemClickEventArgs e)
        {
            var data = e.ClickedItem as VideoDetailRelatesModel;
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Play,
                page = typeof(VideoDetailPage),
                parameters = data.aid,
                title = data.title
            });

            //this.Frame.Navigate(typeof(VideoDetailPage), data.aid);
        }

        private void AppBarButton_Holding(object sender, HoldingRoutedEventArgs e)
        {
            Utils.ShowMessageToast("长按");
        }

        private void btnLike_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (videoDetailVM.VideoInfo.req_user.like == 0)
            {
                videoDetailVM.DoTriple();
            }
        }

        private void btnTagItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as HyperlinkButton).DataContext as VideoDetailTagModel;
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Find,
                page = typeof(SearchPage),
                parameters = item.tag_name,
                title = "搜索:" + item.tag_name
            });
        }

        private void btnShare_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }
        private void btnShareCopy_Click(object sender, RoutedEventArgs e)
        {
            Utils.SetClipboard($"{videoDetailVM.VideoInfo.title}\r\n{videoDetailVM.VideoInfo.short_link}");
            Utils.ShowMessageToast("已复制内容到剪切板");
        }

        private void btnShareCopyUrl_Click(object sender, RoutedEventArgs e)
        {
            Utils.SetClipboard(videoDetailVM.VideoInfo.short_link);
            Utils.ShowMessageToast("已复制链接到剪切板");
        }


        private void PlayerControl_FullScreenEvent(object sender, bool e)
        {
            if (e)
            {
                this.Margin = new Thickness(0, SettingHelper.GetValue<int>(SettingHelper.UI.DISPLAY_MODE, 0) == 0 ? -40 : -32, 0, 0);
                RightInfo.Width = new GridLength(0, GridUnitType.Pixel);
                BottomInfo.Height = new GridLength(0, GridUnitType.Pixel);
            }
            else
            {
                this.Margin = new Thickness(0);
                RightInfo.Width = new GridLength(SettingHelper.GetValue<double>(SettingHelper.UI.RIGHT_DETAIL_WIDTH, 320), GridUnitType.Pixel);
                BottomInfo.Height = GridLength.Auto;
            }
        }

        private void PlayerControl_FullWindowEvent(object sender, bool e)
        {
            if (e)
            {
                RightInfo.Width = new GridLength(0, GridUnitType.Pixel);
                BottomInfo.Height = new GridLength(0, GridUnitType.Pixel);
            }
            else
            {
                RightInfo.Width = new GridLength(SettingHelper.GetValue<double>(SettingHelper.UI.RIGHT_DETAIL_WIDTH, 320), GridUnitType.Pixel);
                BottomInfo.Height = GridLength.Auto;
            }
        }
        bool changedFlag = false;
        private void PlayerControl_ChangeEpisodeEvent(object sender, int e)
        {

            changedFlag = true;
            listEpisode.SelectedIndex = e;
            changedFlag = false;
        }

        private void listEpisode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (changedFlag || listEpisode.SelectedIndex == -1)
            {
                return;
            }
            player.ChangePlayIndex(listEpisode.SelectedIndex);
        }

        private void listEpisode_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void listEpisode_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void btnOpenUser_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as HyperlinkButton).DataContext;
            if (data is VideoDetailStaffModel)
            {
                MessageCenter.NavigateToPage(this, new NavigationInfo()
                {
                    icon = Symbol.Contact,
                    title = (data as VideoDetailStaffModel).name,
                    page = typeof(UserInfoPage),
                    parameters = (data as VideoDetailStaffModel).mid
                });
            }
            else
            {
                MessageCenter.NavigateToPage(this, new NavigationInfo()
                {
                    icon = Symbol.Contact,
                    title = videoDetailVM.VideoInfo.owner.name,
                    page = typeof(UserInfoPage),
                    parameters = videoDetailVM.VideoInfo.owner.mid
                });
            }
        }

        private void btnLike_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (videoDetailVM.VideoInfo.req_user.like == 0)
            {
                videoDetailVM.DoTriple();
            }
        }

        private void listAddFavorite_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as FavoriteItemModel;
            videoDetailVM.DoFavorite(new List<string>() { item.id }, avid);
        }

        private void BtnAddFavorite_Click(object sender, RoutedEventArgs e)
        {
            videoDetailVM.DoFavorite(videoDetailVM.MyFavorite.Where(x => x.is_fav).Select(x => x.id).ToList(), avid);
        }

        private async void btnOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            var url = videoDetailVM.VideoInfo.short_link;
            var result =await Utils.LaunchUri(url);
            if (!result)
            {
                Utils.ShowMessageToast("打开" + url + "失败");
            }

        }

        private void ImageEx_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (videoDetailVM.VideoInfo?.pic == null) return;
            MessageCenter.OpenImageViewer(new List<string>() {
                videoDetailVM.VideoInfo.pic
            }, 0);
        }

        private async void btnCreateFavBox_Click(object sender, RoutedEventArgs e)
        {
            if (!SettingHelper.Account.Logined && await Utils.ShowLoginDialog())
            {
                Utils.ShowMessageToast("请先登录");
                return;
            }
            CreateFavFolderDialog createFavFolderDialog = new CreateFavFolderDialog();
            await createFavFolderDialog.ShowAsync();
            await videoDetailVM.LoadFavorite(videoDetailVM.VideoInfo.aid);
        }

        private void AddToWatchLater_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as MenuFlyoutItem).DataContext as VideoDetailRelatesModel;
            Modules.User.WatchLaterVM.Instance.AddToWatchlater(data.aid);
        }

        private void btnWatchLater_Click(object sender, RoutedEventArgs e)
        {
            if (videoDetailVM == null || videoDetailVM.VideoInfo == null) return;
            Modules.User.WatchLaterVM.Instance.AddToWatchlater(avid);
        }

        private async void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            var liveView = sender as ListView;
            if (liveView.SelectedItem == null ) return;
            var item = liveView.SelectedItem as VideoPlaylistItem;
           
            playlist.Index = playlist.Playlist.IndexOf(item);
            await InitializeVideo(item.ID);

        }

        private void player_AllMediaEndEvent(object sender, EventArgs e)
        {

            if(playlist==null||playlist.Index== playlist.Playlist.Count - 1)
            {
                Utils.ShowMessageToast("播放完毕");
                return;
            }
            // 合集页签插在最前面，这里按引用取播放列表，不依赖页签顺序
            var listView = playlistPivotItem?.Content as ListView;
            if (listView == null) return;
            listView.SelectedIndex = playlist.Index + 1; 
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (videoDetailVM.VideoInfo == null|| videoDetailVM.VideoInfo.pages==null || videoDetailVM.VideoInfo.pages.Count == 0) return;
            var downloadItem = new DownloadItem() { 
                Cover= videoDetailVM.VideoInfo.pic,
                ID=videoDetailVM.VideoInfo.aid,
                Episodes =new List<DownloadEpisodeItem>(),
                Subtitle= videoDetailVM.VideoInfo.bvid,
                Title=videoDetailVM.VideoInfo.title,
                Type= DownloadType.Video,
                UpMid = videoDetailVM.VideoInfo.owner.mid.ToInt64(),
            };
            int i = 0;
            foreach (var item in videoDetailVM.VideoInfo.pages)
            {
                //检查正在下载及下载完成是否存在此视频
                int state = 0;
                if (DownloadVM.Instance.Downloadings.FirstOrDefault(x=>x.EpisodeID==item.cid)!=null)
                {
                    state = 2;
                }
                if (DownloadVM.Instance.Downloadeds.FirstOrDefault(x => x.Episodes.FirstOrDefault(y=>y.CID==item.cid)!=null) != null)
                {
                    state = 3;
                }
                //如果正在下载state=2,下载完成state=3
                downloadItem.Episodes.Add(new DownloadEpisodeItem() {
                    AVID= videoDetailVM.VideoInfo.aid,
                    BVID= videoDetailVM.VideoInfo.bvid,
                    CID =item.cid,
                    EpisodeID="",
                    Index=i,
                    Title= "P" + item.page + " " + item.part,
                    State= state
                });
                i++;
            }
            
            DownloadDialog downloadDialog = new DownloadDialog(downloadItem);
            await downloadDialog.ShowAsync();
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (videoDetailVM.Loading) return;
            await InitializeVideo(_id);
        }

        private void btnOpenQR_Click(object sender, RoutedEventArgs e)
        {
            qrFlyout.ShowAt(btnMore);
        }

        private void OnGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width < 540)
            {
                ButtonStack.Visibility = Visibility.Collapsed;
            }
            else
            {
                ButtonStack.Visibility = Visibility.Visible;
            }
        }

        private void btnWatchLater2_Click(object sender, RoutedEventArgs e)
        {
            btnWatchLater_Click(sender, e);
        }
    }
}

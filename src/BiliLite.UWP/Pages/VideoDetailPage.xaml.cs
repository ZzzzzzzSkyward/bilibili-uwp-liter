using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
using BiliLite.Services;
using System.Threading.Tasks;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliLite.Pages
{
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
            // 播放器的下一个/上一个只喊一声,目标由本页的顺序状态决定
            player.NextOuterRequested += player_NextOuterRequested;
            player.NextVideoRequested += player_NextVideoRequested;
            player.PreviousOuterRequested += player_PreviousOuterRequested;
            // 目录:一个入口,选详情列还是开播放器抽屉由本页按窗口状态决定
            player.DirectoryRequested += player_DirectoryRequested;
            player.DirectoryRowRequested += player_DirectoryRowRequested;
            player.DirectorySourceRequested += player_DirectorySourceRequested;
            player.DirectoryLoadMoreRequested += player_DirectoryLoadMoreRequested;
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
                        // 用户自己刷新:照旧从这一条视频的历史位置接着看
                        await InitializeVideo(_id, PlayTargetStart.Resume);
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
            // 页签已经关掉,播放器马上释放:先立标记,还在路上的请求回来后按它收手
            _isClosed = true;
            // 详情请求可能还在路上,下一次进入本页(页面是缓存的同一实例)要能重新取
            flag = false;
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
        // 连播换集自己起播:高亮赋值引起的选中事件不是用户选集,不能借它起播
        bool _syncingUgcSelection;
        // 连播选播放列表行自己起播:同上
        bool _syncingPlaylistSelection;
        // 远程 medialist 播放列表:行、翻页状态和页签内容都由这里持有
        readonly MediaListService mediaListService = new MediaListService();
        MediaListPagingState mediaListState;
        // 取页的单飞通道:footer、抽屉和连播边界都走它,同一个在途请求不会取两次
        MediaListPageLoader mediaListLoader;
        ObservableCollection<VideoPlaylistItem> mediaListRows;
        ListView mediaListListView;
        TextBlock mediaListCountText;
        Button mediaListLoadMoreButton;
        bool flag = false;
        // 页签关闭后页面上的一切续作都要收手:关闭路径先立标记,晚到的请求按它放弃
        bool _isClosed;
        string _id = "";
        // 页面自己持有的播放顺序:当前连播来源、当前分P、当前来源里的当前行。
        // 下一个按钮、快捷键和播放结束都按这一份状态判定目标,不再各自猜来源。
        readonly WatchSequenceState sequence = new WatchSequenceState();
        // 上一次推进抽屉的目录签名:来源或行集合没变就不重建行
        string directorySignature;
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // 重新进入本页时整页重建,上一次关闭留下的标记不能拦住这次播放
            _isClosed = false;
            SubscribeClosedPage();
            SubscribeShare();

            if (e.NavigationMode == NavigationMode.New)
            {
                // 重新进入本页:上一次的连播来源和行号不能留着
                sequence.Clear();
                // 上一次留下的目录签名和抽屉也要收掉,等这次详情回来再报
                directorySignature = null;
                player.HideDirectory();
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
                    // 打开时就带着队列:连播先按队列走,直到点了合集分集
                    sequence.SetActiveSource(WatchSequenceSourceKind.Playlist);
                    // 带 medialist ID 的是远程播放列表:先取首页再播放,不走内存列表
                    if (!string.IsNullOrEmpty(playlist.MediaListId))
                    {
                        await InitializeMediaListPlaylist();
                        return;
                    }

                    // 内存队列的下标不一定还对得上列表(点击身份修好前留下的参数仍会传进来):
                    // 空列表或越界时什么都不建,免得播错视频或直接抛越界
                    if (playlist.Playlist == null || playlist.Playlist.Count == 0
                        || playlist.Index < 0 || playlist.Index >= playlist.Playlist.Count)
                    {
                        Utils.ShowMessageToast("播放列表已失效,请重新打开");
                        return;
                    }

                    var element = PlayListTpl.GetElement(new Windows.UI.Xaml.ElementFactoryGetArgs()) as PivotItem;
                    element.DataContext = playlist;
                 
                    playlistPivotItem = element;
                    pivot.Items.Insert(0, element);
                    pivot.SelectedIndex = 0;
                    // 打开时就带着队列:用户自己打开的这一条,照旧续播
                    await InitializeVideo(playlist.Playlist[playlist.Index].ID, PlayTargetStart.Resume);
                }
                else
                {
                    var id = e.Parameter.ToString();
                    // 直接打开链接:用户自己选的视频,照旧续播
                    await InitializeVideo(id, PlayTargetStart.Resume);
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
        private async Task InitializeVideo(string id, PlayTargetStart start)
        {
            // 页签已关:既不取详情也不换标题、不起播
            if (_isClosed) return;
            // 上一次取详情还在路上:这次选择整条丢掉,不改 _id,免得在途请求的对账条件被顶替
            if (flag) return;
            _id = id;
            flag = true;
            try
            {
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
                // 详情回来时页签已经关掉,或者这次请求已被下一次进入本页的取详情顶替:
                // 都不要再改 VM 和播放器(播放器可能已经释放)
                if (_isClosed || _id != id)
                {
                    return;
                }
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
                    // 续播只属于用户自己选的这一条:连播/下一个换过来的目标从第一P的 0 开始,
                    // 不能拿上一条视频的历史位置顺下来
                    if (start == PlayTargetStart.Resume && videoDetailVM.VideoInfo.history != null)
                    {
                        var history = videoDetailVM.VideoInfo.pages.FirstOrDefault(x => x.cid.Equals(videoDetailVM.VideoInfo.history.cid));
                        if (history != null)
                        {
                            SettingHelper.SetValue<double>(history.cid, Convert.ToDouble(videoDetailVM.VideoInfo.history.progress));
                            index = videoDetailVM.VideoInfo.pages.IndexOf(history);
                        }
                    }
                    player.InitializePlayInfo(playInfos, index, start);
                    comment.LoadComment(new LoadCommentInfo()
                    {
                        CommentMode = (int)Api.CommentApi.CommentType.Video,
                        CommentSort = Api.CommentApi.CommentSort.Hot,
                        Oid = videoDetailVM.VideoInfo.aid
                    });
                }
                ApplyUgcSeason();
                // 详情和分P都换好之后,把顺序状态对齐到当前视频
                ApplyDefaultSequenceSource();
                RefreshSequenceState();
            }
            finally
            {
                // 在途标记只归发起这次请求的一方:关页后重新进入再发起的请求不能被这里放开
                if (_id == id) flag = false;
            }
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
            // 页签已关:选中事件不再换集,播放器已经释放
            if (_isClosed) return;
            // 详情还在路上:这次选中整条丢掉,不换高亮也不改连播来源
            if (flag) return;
            // 连播换集由 PlayCollectionRowAsync 自己发起:续播策略要跟着那一拍传下去
            if (_syncingUgcSelection) return;
            var listView = sender as ListView;
            var item = listView?.SelectedItem as VideoUgcSeasonEpisodeView;
            // 选中当前播放的视频时不重新加载，只换一个分集时才播放并清掉其它分区的高亮
            if (item == null || item.Aid == ugcSeasonAid) return;
            if (string.IsNullOrEmpty(item.Aid)) return;
            foreach (var other in ugcSeasonEpisodeLists)
            {
                if (!ReferenceEquals(other, listView)) other.SelectedItem = null;
            }
            // 从合集分集点进来:连播跟合集顺序,打开时的队列原样留着
            sequence.SetActiveSource(WatchSequenceSourceKind.UgcSeason);
            // 用户自己点的分集:沿用这一集保存的进度
            await InitializeVideo(item.Aid, PlayTargetStart.Resume);
        }

        /// <summary>
        /// 把合集的高亮移到某一集,不触发选中处理。连播换集之后由调用方自己起播,
        /// 续播策略才不会在选中事件里丢掉。
        /// </summary>
        private void SelectUgcSeasonRow(ListView owner, VideoUgcSeasonEpisodeView row)
        {
            _syncingUgcSelection = true;
            try
            {
                foreach (var other in ugcSeasonEpisodeLists)
                {
                    if (!ReferenceEquals(other, owner)) other.SelectedItem = null;
                }
                owner.SelectedItem = row;
            }
            finally
            {
                _syncingUgcSelection = false;
            }
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
            return PlayDirectoryBuilder.FormatDuration(seconds);
        }

        /// <summary>
        /// 远程播放列表:建页签、取首页、播放第一行。翻页只追加到同一个列表。
        /// </summary>
        private async Task InitializeMediaListPlaylist()
        {
            // 启动器链接已经取过首页,直接复用同一份状态;内存里传来的链接自己建一份
            mediaListState = playlist.MediaListState ?? new MediaListPagingState(playlist.MediaListId, playlist.MediaListType, MediaListSourceKind.MediaList, playlist.Title);
            playlist.MediaListState = mediaListState;
            playlist.MediaListId = mediaListState.MediaListId;
            playlist.MediaListType = mediaListState.MediaListType;
            // footer、抽屉和连播边界都从这一条通道取页:同一个在途请求只取一次
            mediaListLoader = new MediaListPageLoader(mediaListState, FetchMediaListPageAsync);
            mediaListRows = new ObservableCollection<VideoPlaylistItem>();
            AppendMediaListRows();
            playlist.Playlist = mediaListRows;
            // 下标和已加载的行数对齐:负数归零,越界退到最后已加载行;
            // 一行都没有时留给下面"播放列表是空的"分支,这里只保证不越界
            playlist.Index = mediaListRows.Count == 0
                ? 0
                : Math.Min(Math.Max(0, playlist.Index), mediaListRows.Count - 1);

            mediaListCountText = new TextBlock()
            {
                FontSize = 12,
            };
            mediaListLoadMoreButton = new Button()
            {
                Content = "加载更多",
                Margin = new Thickness(0, 8, 0, 0),
            };
            mediaListLoadMoreButton.Click += MediaListLoadMore_Click;

            var header = new StackPanel() { Margin = new Thickness(12, 4, 12, 4) };
            header.Children.Add(mediaListCountText);
            header.Children.Add(mediaListLoadMoreButton);

            mediaListListView = new ListView()
            {
                Header = header,
                ItemsSource = mediaListRows,
                ItemTemplate = (DataTemplate)Resources["MediaListRowTpl"],
                ItemContainerStyle = (Style)Resources["UgcSeasonEpisodeItemStyle"],
                SelectionMode = ListViewSelectionMode.Single,
            };
            mediaListListView.SelectionChanged += ListView_SelectionChanged;

            // 页签内容是 ListView,播放结束的自动连播照旧按引用取 playList 页签
            var element = new PivotItem()
            {
                Margin = new Thickness(0),
                Header = new TextBlock() { Text = mediaListState.Title, FontSize = 18 },
                Content = mediaListListView,
            };
            playlistPivotItem = element;
            pivot.Items.Insert(0, element);
            pivot.SelectedIndex = 0;
            // 首页可能已经在状态里:先把计数和"加载更多"按已有行显示出来
            RefreshMediaListChrome();

            // 启动器链接进来时首页已经在状态里,不要再请求一次
            if (mediaListState.LoadedCount == 0 && !await LoadMediaListPage())
            {
                // 取首页期间页签被关掉:不再刷新已拆掉的页面
                if (_isClosed) return;
                // 失败已由服务提示,页签保留并显示已加载 0,不会留下一个空白页签
                RefreshMediaListChrome();
                return;
            }

            // 首页在取的过程中页签被关掉:不选行也不起播
            if (_isClosed)
            {
                return;
            }
            // 详情还在路上:不选行也不起播,等在途请求自己回来
            if (flag) return;

            if (mediaListRows.Count == 0)
            {
                Utils.ShowMessageToast("播放列表是空的");
                RefreshMediaListChrome();
                return;
            }
            var index = Math.Min(playlist.Index, mediaListRows.Count - 1);
            playlist.Index = index;
            // 高亮跟着换,起播由这里发起:用户自己打开的播放列表,第一行照旧续播
            SelectPlaylistRow(mediaListListView, index);
            await InitializeVideo(mediaListRows[index].ID, PlayTargetStart.Resume);
        }

        /// <summary>
        /// 详情列 footer 的"加载更多"/"重试"。只补行,不起播。
        /// </summary>
        private async void MediaListLoadMore_Click(object sender, RoutedEventArgs e)
        {
            await RequestMediaListPageAsync();
        }

        /// <summary>
        /// 抽屉里的"加载更多"/"重试"。与 footer 走同一条通道:取页只补行,
        /// 不起播、不改连播来源、不换浏览的页签;取完刷新抽屉和详情列的同一份状态。
        /// </summary>
        private async void player_DirectoryLoadMoreRequested(object sender, EventArgs e)
        {
            await RequestMediaListPageAsync();
        }

        /// <summary>
        /// 用户点"加载更多"/"重试"的唯一入口。footer 和抽屉共用:同一份状态、同一条单飞通道。
        /// </summary>
        private async Task RequestMediaListPageAsync()
        {
            // 页签已关:按钮不再取页
            if (_isClosed || mediaListState == null) return;
            // 失败后按钮就是重试入口,不受 HasMore 限制
            if (!mediaListState.HasMore && !mediaListState.LastLoadFailed) return;

            var request = LoadMediaListPage();
            // 请求已经在路上:先把"加载中"画到两个界面上
            RefreshMediaListChrome();
            RedrawOpenDirectory();
            await request;
            // 取页期间页签被关掉:不再碰已拆掉的页面状态
            if (_isClosed) return;
            RefreshMediaListChrome();
            RedrawOpenDirectory();
            // 手动取回一页后,"下一个目标"也跟着变
            RefreshSequenceState();
        }

        /// <summary>
        /// 取一页。footer、抽屉和连播边界都调这里:单飞通道保证同一个在途请求只取一次,
        /// 后到的调用者等同一个结果。游标来自 MediaListPagingState 记录的上一页最后一条 aid,
        /// 与当前选中的播放行无关;服务层已按返回的 ID 去重。
        /// </summary>
        private async Task<bool> LoadMediaListPage()
        {
            if (mediaListLoader == null) return false;
            var landed = await mediaListLoader.LoadAsync();
            // 页签已关:新行留在状态里,但不再更新已拆掉的页面
            if (_isClosed) return false;
            if (landed) AppendMediaListRows();
            // 取完(或失败)才画:状态从"加载中"回到"还有下一页/重试/已到头"
            RefreshMediaListChrome();
            return landed;
        }

        /// <summary>
        /// 取页通道的取法:游标为空时取首页,否则取下一页。
        /// 这是全页唯一发起播放列表请求的地方。
        /// </summary>
        private Task<MediaListResources> FetchMediaListPageAsync()
        {
            return mediaListState.IsFirstPage
                ? mediaListService.GetMediaList(mediaListState.MediaListId, mediaListState.MediaListType)
                : mediaListService.LoadMoreMediaList(mediaListState);
        }

        /// <summary>
        /// 把状态里还没绑定的行补到集合上,已加载的行不重建。
        /// 集合和状态都是只追加的,行数就是进度。
        /// </summary>
        private void AppendMediaListRows()
        {
            for (var i = mediaListRows.Count; i < mediaListState.LoadedCount; i++)
            {
                mediaListRows.Add(ToPlaylistItem(mediaListState.Rows[i]));
            }
        }

        /// <summary>
        /// 详情列 footer 的数量和按钮。状态和抽屉同源:加载中不给按钮、失败就是"重试"、到头收起。
        /// </summary>
        private void RefreshMediaListChrome()
        {
            if (mediaListState == null) return;
            if (mediaListCountText != null)
            {
                mediaListCountText.Text = "已加载 " + mediaListState.CountText;
            }
            if (mediaListLoadMoreButton != null)
            {
                var state = PlayDirectoryBuilder.PagingOf(mediaListState);
                var actionable = state == PlayDirectoryPagingState.More || state == PlayDirectoryPagingState.Failed;
                mediaListLoadMoreButton.IsEnabled = actionable;
                mediaListLoadMoreButton.Visibility = actionable ? Visibility.Visible : Visibility.Collapsed;
                mediaListLoadMoreButton.Content = state == PlayDirectoryPagingState.Failed ? "重试" : "加载更多";
            }
        }

        private static VideoPlaylistItem ToPlaylistItem(MediaListItem item)
        {
            return new VideoPlaylistItem()
            {
                // 列表资源ID就是视频的 aid
                ID = item.Id.ToString(CultureInfo.InvariantCulture),
                Title = item.Title,
                Cover = item.Cover,
                Author = item.Upper?.Name,
                Duration = FormatDuration(item.Duration),
            };
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
            // 分P换了:顺序状态和"下一个/上一个"的目标跟着换
            RefreshSequenceState();
            // 抽屉开着时"正在播放"要跟着换到新的分P
            RedrawOpenDirectory();
        }

        private void listEpisode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (changedFlag || listEpisode.SelectedIndex == -1)
            {
                return;
            }
            // 用户自己在详情列点了分P:沿用这一P保存的进度
            player.ChangePlayIndex(listEpisode.SelectedIndex, PlayTargetStart.Resume);
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
            // 页签已关:晚到的选中不再起播
            if (_isClosed) return;
            // 详情还在路上:这次选中整条丢掉,不动队列游标
            if (flag) return;
            // 连播选行由 SelectPlaylistRowAsync 自己发起:续播策略要跟着那一拍传下去
            if (_syncingPlaylistSelection) return;
            var liveView = sender as ListView;
            if (liveView.SelectedItem == null ) return;
            var item = liveView.SelectedItem as VideoPlaylistItem;
           
            playlist.Index = playlist.Playlist.IndexOf(item);
            // 从队列里选行:连播回到队列,队列和选中行都不换
            sequence.SetActiveSource(WatchSequenceSourceKind.Playlist);
            // 用户自己点的行:沿用这一条保存的进度
            await InitializeVideo(item.ID, PlayTargetStart.Resume);

        }

        /// <summary>
        /// 当前视频自己的分P播完后才会到这里:下一个目标由页面的顺序状态判定,
        /// 与"下一个"按钮、快捷键走的是同一条路径。
        /// </summary>
        private async void player_AllMediaEndEvent(object sender, EventArgs e)
        {
            // 自动边界:manual=false 时 AdvanceSequenceAsync 按"自动连播"开关决定走不走
            await AdvanceSequenceAsync(false, false);
        }

        /// <summary>
        /// 播放器的"下一个":还有分P就先换分P,最后一P时换到外层来源的下一条
        /// </summary>
        private async void player_NextOuterRequested(object sender, EventArgs e)
        {
            await AdvanceSequenceAsync(false, true);
        }

        /// <summary>
        /// 跳过本视频剩下的分P,直接换到外层来源的下一条
        /// </summary>
        private async void player_NextVideoRequested(object sender, EventArgs e)
        {
            await AdvanceSequenceAsync(true, true);
        }

        /// <summary>
        /// 播放器的"上一个":先回上一个分P,再回外层来源的上一行
        /// </summary>
        private async void player_PreviousOuterRequested(object sender, EventArgs e)
        {
            // 页签已关:播放器已释放,不再换集
            if (_isClosed) return;
            // 详情还在路上:这一拍丢掉,不让它顶掉在途请求
            if (flag) return;
            RefreshSequenceState();
            var step = WatchSequenceOrder.Previous(sequence.Position);
            switch (step.Move)
            {
                case WatchSequenceMove.PlayPart:
                    // 往回走:回到那一P保存的进度
                    player.ChangePlayIndex(step.PartIndex, PlayTargetStart.Resume);
                    return;
                case WatchSequenceMove.PlayRow:
                    await PlaySequenceRowAsync(step.RowIndex, PlayTargetStart.Resume);
                    return;
                default:
                    // 没有上一个目标:说清是第一个,不进任何换集路径
                    Utils.ShowMessageToast("已经是第一P了");
                    return;
            }
        }

        /// <summary>
        /// 按顺序状态推进一个目标。播放结束、下一个按钮和下一个快捷键都只走这一处:
        /// 分P优先,分P走完交给外层来源;远程队列停在最后一行且还有下一页时先取下一页,
        /// 取不到就把"加载更多"留作重试,不跳过任何一行也不当成播完。
        /// </summary>
        private async Task<bool> AdvanceSequenceAsync(bool skipRemainingParts, bool manual = false)
        {
            // 页签已关:播放器已释放,续播不再进行
            if (_isClosed) return false;
            // 详情还在路上:这一拍丢掉,不让它顶掉在途请求
            if (flag) return false;
            // 自动连播关着:播放结束就停在当前这一P,不换下一个视频。分P之间的自动换P
            // 由播放器看同一个开关;用户自己按的下一个/上一个不经过这里
            if (!PlayContinuationPolicy.AdvanceAllowed(manual, SettingHelper.GetValue<bool>(SettingHelper.Player.AUTO_NEXT, true)))
            {
                ShowSequenceEndToast(manual);
                return false;
            }

            // 一页可能全是已加载的行,取完再判定一次;上限只是防止服务端反复返回 has_more
            for (var attempt = 0; attempt < 3; attempt++)
            {
                RefreshSequenceState();
                var step = WatchSequenceOrder.Next(sequence.Position, skipRemainingParts);
                switch (step.Move)
                {
                    case WatchSequenceMove.PlayPart:
                        // 走播放器自己的分P切换;往前走的续播从 0 开始
                        player.ChangePlayIndex(step.PartIndex, PlayTargetStart.FromStart);
                        return !_isClosed;
                    case WatchSequenceMove.PlayRow:
                        await PlaySequenceRowAsync(step.RowIndex, PlayTargetStart.FromStart);
                        return !_isClosed;
                    case WatchSequenceMove.LoadNextPage:
                        if (await LoadMediaListPage())
                        {
                            // 取页期间页签被关掉:新行留在状态里,但不选行也不起播
                            if (_isClosed) return false;
                            // 新行已经追加到同一个列表,重新判定再走
                            continue;
                        }
                        // 页签已关:失败也不再改计数、不再提示重试
                        if (_isClosed) return false;
                        // 失败:已加载行不动,"加载更多"仍然可见,就是重试入口
                        RefreshMediaListChrome();
                        Utils.ShowMessageToast("下一页加载失败,点\"加载更多\"重试");
                        return false;
                    case WatchSequenceMove.RetryNextPage:
                        RefreshMediaListChrome();
                        Utils.ShowMessageToast("下一页加载失败,点\"加载更多\"重试");
                        return false;
                    default:
                        ShowSequenceEndToast(manual);
                        return false;
                }
            }

            Utils.ShowMessageToast("播放列表没有更多可播放的内容");
            return false;
        }

        /// <summary>
        /// 没有下一个目标。手动按"下一个"时仍要说清是最后一P,自动播完只说播放完毕。
        /// </summary>
        private void ShowSequenceEndToast(bool manual)
        {
            var position = sequence.Position;
            var noOuterRow = position.Source == WatchSequenceSourceKind.None || position.RowCount <= 0;
            Utils.ShowMessageToast(manual && noOuterRow ? "已经是最后一P了" : "播放完毕");
        }

        /// <summary>
        /// 执行外层来源的一行:播放列表选行,合集选分集,两者都复用已有的选择处理
        /// </summary>
        private async Task PlaySequenceRowAsync(int rowIndex, PlayTargetStart start)
        {
            if (sequence.Position.Source == WatchSequenceSourceKind.UgcSeason)
            {
                await PlayCollectionRowAsync(rowIndex, start);
                return;
            }
            await SelectPlaylistRowAsync(rowIndex, start);
        }

        private async Task PlayCollectionRowAsync(int rowIndex, PlayTargetStart start)
        {
            var aids = VideoUgcSeasonOrder.AidOrder(videoDetailVM?.VideoInfo?.UgcSeason);
            if (rowIndex < 0 || rowIndex >= aids.Count) return;
            var aid = aids[rowIndex];
            foreach (var listView in ugcSeasonEpisodeLists)
            {
                var episodes = listView.ItemsSource as List<VideoUgcSeasonEpisodeView>;
                var row = episodes?.FirstOrDefault(x => x.Aid == aid);
                if (row == null) continue;
                // 高亮跟着换,但起播由这里发起:续播策略要跟着这一拍传下去,不能让选中事件再猜一次。
                // 合集页签和当前队列都不动。
                SelectUgcSeasonRow(listView, row);
                await InitializeVideo(aid, start);
                return;
            }
        }

        /// <summary>
        /// 连播来源的优先级不变:点过队列行或合集分集就按那个来源,打开时就带着队列的也按队列;
        /// 这些都没有时,详情里的合集才作为兜底的顺序源
        /// </summary>
        private void ApplyDefaultSequenceSource()
        {
            if (sequence.Position.Source != WatchSequenceSourceKind.None) return;
            if (videoDetailVM?.VideoInfo?.UgcSeason != null && !string.IsNullOrEmpty(ugcSeasonAid))
            {
                sequence.SetActiveSource(WatchSequenceSourceKind.UgcSeason);
            }
        }

        /// <summary>
        /// 把播放器当前分P和当前来源的行补进顺序状态,再刷新播放器上的"下一个/上一个"目标。
        /// </summary>
        private void RefreshSequenceState()
        {
            var playInfos = player?.PlayInfos;
            var partCount = playInfos?.Count ?? 0;
            var partIndex = partCount == 0 ? 0 : Math.Min(Math.Max(0, player.CurrentPlayIndex), partCount - 1);
            sequence.SetParts(partIndex, partCount);

            var source = sequence.Position.Source;
            if (source == WatchSequenceSourceKind.Playlist && playlist?.Playlist != null)
            {
                sequence.SetRows(source, playlist.Index, playlist.Playlist.Count,
                    mediaListState != null && mediaListState.HasMore,
                    mediaListState != null && mediaListState.LastLoadFailed);
            }
            else if (source == WatchSequenceSourceKind.UgcSeason)
            {
                var aids = VideoUgcSeasonOrder.AidOrder(videoDetailVM?.VideoInfo?.UgcSeason);
                var rowIndex = aids.IndexOf(videoDetailVM?.VideoInfo?.aid);
                sequence.SetRows(source, rowIndex, aids.Count, false, false);
            }
            else
            {
                sequence.SetRows(WatchSequenceSourceKind.None, -1, 0, false, false);
            }

            player?.UpdateSequenceInfo(
                DescribeStep(WatchSequenceOrder.Next(sequence.Position, false)),
                DescribeStep(WatchSequenceOrder.Previous(sequence.Position)),
                CanSkipToNextVideo());
            // "目录"按钮跟着同一份顺序状态:有没有目录、按钮上写什么
            bool hasDirectory;
            var directoryCaption = DirectoryCaption(out hasDirectory);
            player?.UpdateDirectoryInfo(hasDirectory, directoryCaption);
            RefreshOpenDirectory();
        }

        /// <summary>
        /// 把一次移动写成按钮提示里的目标文字;没有目标时返回空,播放器据此收起"下一个"按钮。
        /// 只写目标本身:是下一个还是上一个由按钮决定。
        /// </summary>
        private string DescribeStep(WatchSequenceStep step)
        {
            if (step == null) return "";
            switch (step.Move)
            {
                case WatchSequenceMove.PlayPart:
                    return PartTitle(step.PartIndex);
                case WatchSequenceMove.PlayRow:
                    return RowTitle(step.RowIndex);
                case WatchSequenceMove.LoadNextPage:
                case WatchSequenceMove.RetryNextPage:
                    return "下一页";
                default:
                    return "";
            }
        }

        private string PartTitle(int index)
        {
            var playInfos = player?.PlayInfos;
            var title = playInfos != null && index >= 0 && index < playInfos.Count ? playInfos[index]?.title : null;
            return string.IsNullOrEmpty(title) ? "P" + (index + 1) : title;
        }

        private string RowTitle(int index)
        {
            if (sequence.Position.Source == WatchSequenceSourceKind.UgcSeason)
            {
                var aids = VideoUgcSeasonOrder.AidOrder(videoDetailVM?.VideoInfo?.UgcSeason);
                if (index < 0 || index >= aids.Count) return "";
                var aid = aids[index];
                foreach (var listView in ugcSeasonEpisodeLists)
                {
                    var episodes = listView.ItemsSource as List<VideoUgcSeasonEpisodeView>;
                    var row = episodes?.FirstOrDefault(x => x.Aid == aid);
                    if (row != null) return row.Title ?? "";
                }
                return "";
            }

            var rows = playlist?.Playlist;
            if (rows == null || index < 0 || index >= rows.Count) return "";
            return rows[index].Title ?? "";
        }

        /// <summary>
        /// 外层来源有下一条,且本视频还有分P没播完:才允许"跳过剩余分P"
        /// </summary>
        private bool CanSkipToNextVideo()
        {
            var position = sequence.Position;
            return position.Source != WatchSequenceSourceKind.None
                && position.RowCount > 0
                && position.PartCount > 0
                && position.PartIndex >= 0
                && position.PartIndex < position.PartCount - 1;
        }

        private async Task SelectPlaylistRowAsync(int index, PlayTargetStart start)
        {
            // 页签已关:不换选中行,也不起播
            if (_isClosed) return;
            // 详情还在路上:这次换行丢掉,队列游标不动
            if (flag) return;
            if (index < 0 || index >= playlist.Playlist.Count) return;

            // 远程和内存列表的页签内容都是这个 ListView:高亮跟着换,起播由这里发起,
            // 续播策略才不会被选中事件丢掉
            var listView = playlistPivotItem?.Content as ListView;
            if (listView != null)
            {
                SelectPlaylistRow(listView, index);
            }
            playlist.Index = index;
            await InitializeVideo(playlist.Playlist[index].ID, start);
        }

        /// <summary>
        /// 把播放列表的高亮移到某一行,不触发行选择处理。
        /// </summary>
        private void SelectPlaylistRow(ListView listView, int index)
        {
            if (listView.SelectedIndex == index) return;
            _syncingPlaylistSelection = true;
            try
            {
                listView.SelectedIndex = index;
            }
            finally
            {
                _syncingPlaylistSelection = false;
            }
        }

        /// <summary>
        /// "目录"只喊一声:普通窗口有详情列就选到对应那页,全屏/铺满/窄窗才在播放器里开抽屉。
        /// 打开目录不停播、不改全屏,也不改连播来源。
        /// </summary>
        private void player_DirectoryRequested(object sender, EventArgs e)
        {
            // 页签已关或详情还在路上:这一拍不打开任何东西
            if (_isClosed || flag) return;
            if (RightInfo.Width.Value > 0 && SelectDetailDirectory()) return;
            // 抽屉开着时再点"目录"只是刷新内容,不把用户浏览的分段拉回连播来源
            player.ShowDirectory(BuildDirectoryModel(!player.IsDirectoryVisible));
        }

        /// <summary>
        /// 普通窗口:选到详情列里当前来源那一页,并把当前行滚进视野。
        /// 只有外层来源没有详情页时才让播放器开抽屉。
        /// </summary>
        private bool SelectDetailDirectory()
        {
            var source = sequence.Position.Source;
            if (source == WatchSequenceSourceKind.Playlist && playlistPivotItem != null && pivot.Items.Contains(playlistPivotItem))
            {
                pivot.SelectedItem = playlistPivotItem;
                var listView = playlistPivotItem.Content as ListView;
                if (listView != null && listView.SelectedItem != null) listView.ScrollIntoView(listView.SelectedItem);
                return true;
            }
            if (source == WatchSequenceSourceKind.UgcSeason && ugcSeasonPivotItem != null && pivot.Items.Contains(ugcSeasonPivotItem))
            {
                pivot.SelectedItem = ugcSeasonPivotItem;
                foreach (var listView in ugcSeasonEpisodeLists)
                {
                    if (listView.SelectedItem == null) continue;
                    listView.ScrollIntoView(listView.SelectedItem);
                    break;
                }
                return true;
            }
            if (detailPivotItem != null && pivot.Items.Contains(detailPivotItem))
            {
                pivot.SelectedItem = detailPivotItem;
                if (listEpisode != null && listEpisode.SelectedItem != null)
                {
                    listEpisode.ScrollIntoView(listEpisode.SelectedItem);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 目录里点了一行:分P走播放器的分P切换,外层行走已有的选行路径;
        /// 选中哪一行就把那个来源设成连播来源,不再自己猜位置。
        /// </summary>
        private async void player_DirectoryRowRequested(object sender, PlayDirectoryRowActivatedEventArgs e)
        {
            await ActivateDirectoryRowAsync(e.Kind, e.Row);
        }

        private async Task ActivateDirectoryRowAsync(PlayDirectorySectionKind kind, PlayDirectoryRow row)
        {
            if (_isClosed || flag || row == null) return;
            var rowIndex = row.Ordinal - 1;
            if (rowIndex < 0) return;
            // 只有真的就是正在播的那一条才不起播:行上的 IsCurrent 可能来自别的时段或别的分段
            if (IsPlayingDirectoryRow(kind, row))
            {
                AdoptDirectoryRowSource(kind, rowIndex);
                return;
            }
            switch (kind)
            {
                case PlayDirectorySectionKind.Parts:
                    // 换分P不动外层来源;高亮由播放器的换集事件刷新。用户自己点的这一P沿用保存的进度
                    player.ChangePlayIndex(rowIndex, PlayTargetStart.Resume);
                    return;
                case PlayDirectorySectionKind.UgcSeason:
                    sequence.SetActiveSource(WatchSequenceSourceKind.UgcSeason);
                    await PlayCollectionRowAsync(rowIndex, PlayTargetStart.Resume);
                    break;
                case PlayDirectorySectionKind.Playlist:
                    sequence.SetActiveSource(WatchSequenceSourceKind.Playlist);
                    await SelectPlaylistRowAsync(rowIndex, PlayTargetStart.Resume);
                    break;
                default:
                    return;
            }
            // 显式选行之后重画抽屉,让"正在播放"跟着走;重画不会再起播
            RedrawOpenDirectory();
        }

        /// <summary>
        /// 这一行是不是正在播的那条视频。分P比 cid,合集和播放列表比 aid:
        /// 队列游标会停在旧的一行,行的 IsCurrent 不能当成"正在播"的证据。
        /// </summary>
        private bool IsPlayingDirectoryRow(PlayDirectorySectionKind kind, PlayDirectoryRow row)
        {
            if (row == null) return false;
            if (kind == PlayDirectorySectionKind.Parts)
            {
                var playInfos = player?.PlayInfos;
                var index = player == null ? -1 : player.CurrentPlayIndex;
                if (playInfos == null || index < 0 || index >= playInfos.Count) return false;
                var info = playInfos[index];
                var cid = info == null ? null : info.cid;
                // 详情没给 cid 时退到位置:分P的行序就是播放器的分P序
                if (string.IsNullOrEmpty(cid)) return row.Ordinal - 1 == index;
                return string.Equals(row.Key, cid, StringComparison.Ordinal);
            }
            var aid = videoDetailVM?.VideoInfo?.aid;
            if (string.IsNullOrEmpty(aid)) return false;
            return string.Equals(row.Key, aid, StringComparison.Ordinal);
        }

        /// <summary>
        /// 点的就是正在播的这一行:只把它所在的来源写成连播来源,播放列表还要把游标移到这一行,
        /// 不重新起播、不重新加载详情。分P不是连播来源,只重画。
        /// </summary>
        private void AdoptDirectoryRowSource(PlayDirectorySectionKind kind, int rowIndex)
        {
            switch (kind)
            {
                case PlayDirectorySectionKind.UgcSeason:
                    sequence.SetActiveSource(WatchSequenceSourceKind.UgcSeason);
                    break;
                case PlayDirectorySectionKind.Playlist:
                    if (playlist?.Playlist == null || rowIndex >= playlist.Playlist.Count) return;
                    playlist.Index = rowIndex;
                    sequence.SetActiveSource(WatchSequenceSourceKind.Playlist);
                    break;
                default:
                    RedrawOpenDirectory();
                    return;
            }
            // 顺序状态一换,开着的抽屉由它按签名决定要不要重画
            RefreshSequenceState();
        }

        /// <summary>
        /// "按合集连播"/"按播放列表连播":只换连播来源,不重播当前视频。
        /// 目标来源里没有当前视频时不换,让用户先在目录里挑一行。
        /// </summary>
        private void player_DirectorySourceRequested(object sender, PlayDirectorySourceEventArgs e)
        {
            if (_isClosed || flag) return;
            SwitchSequenceSource(e.Kind == PlayDirectorySectionKind.UgcSeason
                ? WatchSequenceSourceKind.UgcSeason
                : WatchSequenceSourceKind.Playlist);
        }

        private void SwitchSequenceSource(WatchSequenceSourceKind source)
        {
            var info = videoDetailVM?.VideoInfo;
            if (info == null) return;
            if (source == WatchSequenceSourceKind.UgcSeason)
            {
                if (VideoUgcSeasonOrder.AidOrder(info.UgcSeason).IndexOf(info.aid) < 0)
                {
                    Utils.ShowMessageToast("当前视频不在这个合集里,请在目录里选一行");
                    return;
                }
            }
            else if (source == WatchSequenceSourceKind.Playlist)
            {
                var index = PlaylistIndexOf(info.aid);
                if (index < 0)
                {
                    Utils.ShowMessageToast("当前视频不在这个播放列表里,请在目录里选一行");
                    return;
                }
                // 队列游标跟着当前视频走:只改序号,不选行、不起播
                playlist.Index = index;
            }

            sequence.SetActiveSource(source);
            RefreshSequenceState();
            RedrawOpenDirectory();
        }

        private int PlaylistIndexOf(string aid)
        {
            if (playlist?.Playlist == null) return -1;
            var ids = playlist.Playlist.Select(x => x == null ? null : x.ID).ToList();
            return VideoPlaylistOrder.IndexOfItem(ids, aid);
        }

        /// <summary>
        /// "目录"按钮的短标签和目录页头同源,只算位置不建行。
        /// available 只问有没有目录:没有位置时按钮只剩图标,不因此消失。
        /// </summary>
        private string DirectoryCaption(out bool available)
        {
            available = true;
            var position = sequence.Position;
            if (position.Source == WatchSequenceSourceKind.UgcSeason && position.RowCount > 0)
            {
                return PlayDirectoryBuilder.Caption(PlayDirectorySectionKind.UgcSeason, position.RowIndex + 1, position.RowCount);
            }
            if (position.Source == WatchSequenceSourceKind.Playlist && position.RowCount > 0)
            {
                return PlayDirectoryBuilder.Caption(PlayDirectorySectionKind.Playlist, position.RowIndex + 1, position.RowCount);
            }
            // 一个分P的普通视频没有目录;互动视频例外
            if (position.PartCount > 1 || (position.PartCount == 1 && IsInteractionVideo()))
            {
                return PlayDirectoryBuilder.Caption(PlayDirectorySectionKind.Parts, position.PartIndex + 1, position.PartCount);
            }
            available = false;
            return "";
        }

        private bool IsInteractionVideo()
        {
            return videoDetailVM?.VideoInfo?.interaction != null;
        }

        private List<PlayDirectoryPart> CurrentParts()
        {
            var parts = new List<PlayDirectoryPart>();
            var playInfos = player?.PlayInfos;
            if (playInfos == null) return parts;
            foreach (var info in playInfos)
            {
                if (info == null) continue;
                parts.Add(new PlayDirectoryPart()
                {
                    Cid = info.cid,
                    Title = info.title,
                    // 详情里的分P时长本来就是秒(与合集分集同一个单位),不再换算
                    DurationSeconds = info.duration,
                });
            }
            return parts;
        }

        /// <summary>
        /// 用当前顺序、合集和队列现搭一份目录。行只在打开抽屉或来源/行集合变了时重建。
        /// 队列的当前行认正在播的 aid,不认停在旧位置的队列游标。
        /// </summary>
        private PlayDirectoryModel BuildDirectoryModel(bool freshOpen)
        {
            var info = videoDetailVM?.VideoInfo;
            var parts = PlayDirectoryBuilder.FromParts(CurrentParts(),
                player == null ? 0 : player.CurrentPlayIndex,
                info == null ? Title : info.title,
                IsInteractionVideo());
            var collection = PlayDirectoryBuilder.FromUgcSeason(info == null ? null : info.UgcSeason,
                info == null ? null : info.aid);
            var queue = playlist?.Playlist == null
                ? null
                : PlayDirectoryBuilder.FromPlaylist(playlist, mediaListState, info == null ? null : info.aid);
            var model = PlayDirectoryBuilder.Combine(parts, collection, queue, sequence.Position.Source);
            // 新开目录才让浏览的分段回到连播来源;重画留着用户正在看的那一段
            model.IsFreshOpen = freshOpen;
            return model;
        }

        /// <summary>
        /// 抽屉开着时按"来源 + 行集合"判断要不要重建。换集、自动连播不重建整份列表,
        /// 只有换了来源或行集合真的变了才重建。
        /// </summary>
        private void RefreshOpenDirectory()
        {
            if (player == null || !player.IsDirectoryVisible)
            {
                directorySignature = null;
                return;
            }
            if (DirectorySignature() == directorySignature) return;
            RedrawOpenDirectory();
        }

        /// <summary>
        /// 立刻重画已经打开的抽屉(打开时,以及新一节页取回来之后)
        /// </summary>
        private void RedrawOpenDirectory()
        {
            if (player == null || !player.IsDirectoryVisible)
            {
                directorySignature = null;
                return;
            }
            directorySignature = DirectorySignature();
            player.ShowDirectory(BuildDirectoryModel(false));
        }

        private string DirectorySignature()
        {
            var position = sequence.Position;
            var loaded = mediaListState == null ? 0 : mediaListState.LoadedCount;
            // 翻页状态也算进签名:加载中、失败后的"重试"、到头都要重画
            var paging = (int)PlayDirectoryBuilder.PagingOf(mediaListState);
            var sections = videoDetailVM?.VideoInfo?.UgcSeason?.Sections;
            return (int)position.Source
                + "|" + position.RowCount
                + "|" + loaded
                + "|" + paging
                + "|" + (sections == null ? 0 : sections.Count);
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
            // 用户自己刷新:照旧接着看
            await InitializeVideo(_id, PlayTargetStart.Resume);
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

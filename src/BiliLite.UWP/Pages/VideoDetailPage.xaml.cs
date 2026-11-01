using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
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
using BiliLite.Parsing.MediaList;
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
            player.DirectoryScopeChanged += player_DirectoryScopeChanged;
            player.DirectoryLoadMoreRequested += player_DirectoryLoadMoreRequested;
            player.DirectoryLoadAllRequested += player_DirectoryLoadAllRequested;
            player.DirectoryCancelLoadAllRequested += player_DirectoryCancelLoadAllRequested;
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
            // 目录抽屉开着:键盘归抽屉(方向键浏览、回车激活、Esc 关抽屉),页面快捷键一个都不接
            if (player != null && player.IsDirectoryVisible) return;
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
            // 等待态、断在取页上的那一步和身份一起作废:晚到的结果一张票都对不上
            transition.Close();
            // 这一次启动带来的队列整份拆掉:页签、行、翻页状态和"加载全部"都不留给下一个视频
            RemoveMediaListPlaylist();
            // 顺序状态也清掉:目录按钮不能再报一个已经不存在的播放列表
            sequence.Clear();
            player?.UpdateDirectoryInfo(false, "");
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
            // 等待态也不留在播放器上:页面是缓存实例,下一次进来不该还挂着上一条的目标
            player?.ClearTransitionPending();
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
        // 这一次启动带来的队列:页签里的列表、翻页状态、已加载的行和"加载全部"。
        // 四样一起建、一起拆,页面是缓存实例,漏掉一样就会跟着到下一个视频
        readonly PlayQueueState queue = new PlayQueueState();
        VideoPlaylist playlist
        {
            get { return queue.Playlist; }
            set { queue.Playlist = value; }
        }
        MediaListPagingState mediaListState
        {
            get { return queue.Paging; }
            set { queue.Paging = value; }
        }
        // "加载全部":同一条单飞通道上一次一页,失败/到头/取消就停。它只补行
        PlayDirectoryLoadAll loadAll
        {
            get { return queue.LoadAll; }
            set { queue.LoadAll = value; }
        }
        ObservableCollection<VideoPlaylistItem> mediaListRows
        {
            get { return queue.Rows; }
            set { queue.Rows = value; }
        }
        // 取页的单飞通道:footer、抽屉和连播边界都走它,同一个在途请求不会取两次
        MediaListPageLoader mediaListLoader;
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
        // 换目标的在途状态:一次只走一拍,断在取页上的那一步也记在这里。
        // 身份是"视频 + 播放列表 + 代数",晚到的详情、取页和起播结果据此丢掉
        readonly PlayTransitionState transition = new PlayTransitionState();
        // 上一次推进抽屉的目录签名:来源或行集合没变就不重建行
        string directorySignature;
        // 用户正在浏览的分段:页头跟着它,重画(换集、翻页)时也不换掉
        PlayDirectorySectionKind? browsedDirectoryKind;

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
                // 上一次的等待态、断掉的那一步和身份也不能留着
                transition.Close();
                // 上一次的队列页签、行和"加载全部"整份拆掉:这次没带队列就不能看见它
                RemoveMediaListPlaylist();
                // 上一次留下的目录签名和抽屉也要收掉,等这次详情回来再报
                directorySignature = null;
                browsedDirectoryKind = null;
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
                    SetSequenceSource(WatchSequenceSourceKind.Playlist);
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
        /// <summary>
        /// 取详情并起播。返回值说这一次请求有没有真的起了播:
        /// 晚到的结果(关页、换了视频、新的一拍)既不算成功也不算失败,调用方据此收掉等待态。
        /// </summary>
        private async Task<bool> InitializeVideo(string id, PlayTargetStart start)
        {
            // 页签已关:既不取详情也不换标题、不起播
            if (_isClosed) return false;
            // 上一次取详情还在路上:这次选择整条丢掉,不改 _id,免得在途请求的对账条件被顶替
            if (flag) return false;
            // 用户开了另一条视频:断在取页上的那一步不再作数
            transition.DropAdvance();
            // 换了播放目标:后台把列表取完这件事让给它,手上这一页走完就停
            if (loadAll != null) loadAll.RequestCancel();
            _id = id;
            flag = true;
            // 这一拍的身份:视频、播放列表和代数。结果回来时三项都得还是发起时的那一份
            var ticket = transition.Ticket();
            var played = false;
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
                // 详情回来时页签已经关掉,这次请求已被下一次进入本页的取详情顶替,
                // 或者已经开了新的一拍:都不要再改 VM 和播放器(播放器可能已经释放)
                if (_isClosed || _id != id || !transition.IsCurrent(ticket))
                {
                    return false;
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
                            return false;
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
                    // 起播了:身份换成这一条视频,等待态和旧身份上的记录一起收掉
                    transition.Commit(videoDetailVM.VideoInfo.aid, MediaListIdText());
                    played = true;
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
            return played;
        }

        /// <summary>
        /// 当前播放列表 id。固定内存队列没有 id,写空串:身份里它就一直是同一份
        /// </summary>
        private string MediaListIdText()
        {
            return mediaListState == null ? "" : mediaListState.MediaListId ?? "";
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
            // 一拍换目标还在路上:这次选中也丢掉,不排队
            if (transition.IsPending) return;
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
            SetSequenceSource(WatchSequenceSourceKind.UgcSeason);
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
        /// 拆掉这一次启动带来的播放队列:页签连同里面的列表和按钮一起摘下来,取页通道断开,
        /// 队列状态、翻页状态和行集合一起放掉。关页、重新进入本页都走这一处,拆过再拆什么都不做。
        /// </summary>
        private void RemoveMediaListPlaylist()
        {
            // 在途取页跟着断:通道换成空,晚到的那一页回不到这一份状态上
            mediaListLoader = null;
            if (mediaListLoadMoreButton != null)
            {
                mediaListLoadMoreButton.Click -= MediaListLoadMore_Click;
                mediaListLoadMoreButton = null;
            }
            mediaListCountText = null;
            if (mediaListListView != null)
            {
                // 先摘处理器再撤行:撤行引起的选中事件不再进播放路径
                mediaListListView.SelectionChanged -= ListView_SelectionChanged;
                mediaListListView.ItemsSource = null;
                mediaListListView = null;
            }
            if (playlistPivotItem != null)
            {
                playlistPivotItem.Content = null;
                playlistPivotItem.DataContext = null;
                if (pivot.Items.Contains(playlistPivotItem))
                {
                    pivot.Items.Remove(playlistPivotItem);
                }
                playlistPivotItem = null;
            }
            // "加载全部"在这里收手,启动带来的队列、翻页状态和行集合一起放掉
            queue.Clear();
        }

        /// <summary>
        /// 远程播放列表:建页签、取首页、播放第一行。翻页只追加到同一个列表。
        /// </summary>
        private async Task InitializeMediaListPlaylist()
        {
            // 换了一份播放列表:上一轮的"加载全部"取的已经不是这一份,手上这页走完就停
            if (loadAll != null) loadAll.RequestCancel();
            // 启动器链接已经取过首页,直接复用同一份状态;内存里传来的链接自己建一份
            mediaListState = playlist.MediaListState ?? new MediaListPagingState(playlist.MediaListId, playlist.MediaListType, MediaListSourceKind.MediaList, playlist.Title);
            // 这一次建起来的是哪一份:等页回来时要认它还在不在
            var initState = mediaListState;
            playlist.MediaListState = mediaListState;
            // 身份里的播放列表换成这一份:上一条列表断掉的换目标不属于现在
            transition.SetMediaList(mediaListState.MediaListId);
            playlist.MediaListId = mediaListState.MediaListId;
            playlist.MediaListType = mediaListState.MediaListType;
            // footer、抽屉和连播边界都从这一条通道取页:同一个在途请求只取一次
            mediaListLoader = new MediaListPageLoader(mediaListState, FetchMediaListPageAsync);
            // "加载全部"绑在这一份状态上:换了一份播放列表,上一轮取的就是别人的页
            var loadAllState = mediaListState;
            loadAll = new PlayDirectoryLoadAll(
                () => HaveMoreMediaListPages(loadAllState),
                () => loadAllState.IsLoading,
                () => LoadAllOneMediaListPageAsync(loadAllState));
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
                // 取首页期间本页被重新打开:这一份已经不是现在这份,不刷别人的计数
                if (!ReferenceEquals(mediaListState, initState)) return;
                // 失败已由服务提示,页签保留并显示已加载 0,不会留下一个空白页签
                RefreshMediaListChrome();
                return;
            }

            // 首页在取的过程中页签被关掉:不选行也不起播
            if (_isClosed)
            {
                return;
            }
            // 等页期间本页被重新打开:这一份队列已经拆掉换掉了,不选行也不起播
            if (!ReferenceEquals(mediaListState, initState)) return;
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
        /// 抽屉里的"加载全部":一页一页把剩下的取完,行一页一页出现在已经开着的抽屉里。
        /// 跑着的时候它自己就是这条通道唯一的使用者,再点一次被忽略(footer 上那时只有"取消")。
        /// </summary>
        private async void player_DirectoryLoadAllRequested(object sender, EventArgs e)
        {
            var runner = loadAll;
            if (runner == null || _isClosed) return;

            if (!runner.TryStart())
            {
                // 已经在跑、通道上还有一页、或者已经到头:这一次什么也不做
                RedrawOpenDirectory();
                return;
            }

            // footer 换成"取消",详情列那个按钮先让路
            RefreshMediaListChrome();
            RedrawOpenDirectory();
            await runner.RunAsync();
            // 页签已关:行留在状态里,不再碰拆掉的页面
            if (_isClosed) return;
            // 停下来了(到头、失败或取消):footer 回到加载更多/重试,数量是最后取到的那个
            RefreshMediaListChrome();
            RedrawOpenDirectory();
        }

        /// <summary>
        /// 抽屉里在"加载全部"跑着时点了"取消":手上这一页走完就停,已经取回来的行都留着。
        /// </summary>
        private void player_DirectoryCancelLoadAllRequested(object sender, EventArgs e)
        {
            if (loadAll == null) return;
            loadAll.RequestCancel();
        }

        /// <summary>
        /// "加载全部"取一页。走的是同一条单飞通道,一页回来才进来第二次:
        /// 任何时刻只有一页在路上。换了一份播放列表或页签关掉时立刻收手,不碰别人的状态。
        /// </summary>
        private async Task<bool> LoadAllOneMediaListPageAsync(MediaListPagingState state)
        {
            if (_isClosed || !ReferenceEquals(mediaListState, state)) return false;
            var landed = await LoadMediaListPage();
            if (_isClosed || !ReferenceEquals(mediaListState, state)) return false;
            // 新行进了抽屉,页头的数量也是进度;"有没有下一页"跟着这一页变
            RefreshSequenceState();
            return landed;
        }

        /// <summary>
        /// 还有页可取:还有下一页,或者上一次取失败了(重试入口)。
        /// </summary>
        private static bool HaveMoreMediaListPages(MediaListPagingState state)
        {
            var paging = PlayDirectoryBuilder.PagingOf(state);
            return paging == PlayDirectoryPagingState.More || paging == PlayDirectoryPagingState.Failed;
        }

        /// <summary>
        /// 用户点"加载更多"/"重试"的唯一入口。footer 和抽屉共用:同一份状态、同一条单飞通道。
        /// </summary>
        private async Task RequestMediaListPageAsync()
        {
            // 页签已关:按钮不再取页
            if (_isClosed || mediaListState == null) return;
            // "加载全部"跑着时由它一个人用这条通道:手动取页让路,两个请求不叠在一起
            if (loadAll != null && loadAll.IsRunning) return;
            // 失败后按钮就是重试入口,不受 HasMore 限制
            if (!mediaListState.HasMore && !mediaListState.LastLoadFailed) return;

            // 发起时的那一份:等页期间队列被拆掉或换掉,这一次就不再往下走
            var state = mediaListState;
            var request = LoadMediaListPage();
            // 请求已经在路上:先把"加载中"画到两个界面上
            RefreshMediaListChrome();
            RedrawOpenDirectory();
            var landed = await request;
            // 取页期间页签被关掉:不再碰已拆掉的页面状态
            if (_isClosed) return;
            // 取页期间换了一份队列:这一页不是现在这份的,不刷别人的界面也不推进
            if (!ReferenceEquals(mediaListState, state)) return;
            RefreshMediaListChrome();
            RedrawOpenDirectory();
            // 手动取回一页后,"下一个目标"也跟着变
            RefreshSequenceState();
            // 上一次换目标就断在这一页上:取到了就自动接着走,用户不必再按一次下一个
            await ResumeInterruptedAdvanceAsync(landed);
        }

        /// <summary>
        /// 取一页。footer、抽屉和连播边界都调这里:单飞通道保证同一个在途请求只取一次,
        /// 后到的调用者等同一个结果。游标来自 MediaListPagingState 记录的上一页最后一条 aid,
        /// 与当前选中的播放行无关;服务层已按返回的 ID 去重。
        /// </summary>
        private async Task<bool> LoadMediaListPage()
        {
            // 发起这一页时的那一份:回来之后要认还是不是同一份
            var loader = mediaListLoader;
            var state = mediaListState;
            var rows = mediaListRows;
            if (loader == null || state == null || rows == null) return false;
            var landed = await loader.LoadAsync();
            // 页签已关:新行留在状态里,但不再更新已拆掉的页面
            if (_isClosed) return false;
            // 等页期间换了一份队列:这一页属于旧的那一份,不写进现在这份
            if (!ReferenceEquals(mediaListState, state) || !ReferenceEquals(mediaListRows, rows)) return false;
            if (landed) AppendMediaListRows();
            // 取完(或失败)才画:状态从"加载中"回到"还有下一页/重试/已到头"
            RefreshMediaListChrome();
            return landed;
        }

        /// <summary>
        /// 取页通道的取法:游标为空时取首页,否则取下一页。
        /// 这是全页唯一发起播放列表请求的地方,也是一次失败的提示点——
        /// 单飞通道保证同一个在途请求只有一个发起者,所以这里提示一次就是一次。
        /// </summary>
        private async Task<MediaListResources> FetchMediaListPageAsync()
        {
            var page = mediaListState.IsFirstPage
                ? await mediaListService.GetMediaList(mediaListState.MediaListId, mediaListState.MediaListType)
                : await mediaListService.LoadMoreMediaList(mediaListState);
            if (page.IsSuccess) return page.Value;

            // 解析结论带着类别:传输失败、业务拒绝、形状不对在这里才分得开
            var message = MediaListFailureText.For(page);
            if (!_isClosed) Utils.ShowMessageToast(message);
            LogHelper.Log("[MediaList] " + page.Failure + " " + page.Stage + " " + message, LogType.ERROR);
            return null;
        }

        /// <summary>
        /// 把状态里还没绑定的行补到集合上,已加载的行不重建。
        /// 集合和状态都是只追加的,行数就是进度。
        /// </summary>
        private void AppendMediaListRows()
        {
            // 队列已经拆掉:没有可追加的集合
            if (mediaListState == null || mediaListRows == null) return;
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
                // "加载全部"跑着时这条通道归它一个人用:这个按钮点了也只会让路
                var running = loadAll != null && loadAll.IsRunning;
                mediaListLoadMoreButton.IsEnabled = actionable && !running;
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
            // 一拍换目标还在路上:用户点分P也丢掉,不排队
            if (transition.IsPending) return;
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
            // 一拍换目标还在路上:这次选中也丢掉,不排队
            if (transition.IsPending) return;
            // 连播选行由 SelectPlaylistRowAsync 自己发起:续播策略要跟着那一拍传下去
            if (_syncingPlaylistSelection) return;
            var liveView = sender as ListView;
            if (liveView.SelectedItem == null ) return;
            var item = liveView.SelectedItem as VideoPlaylistItem;
           
            playlist.Index = playlist.Playlist.IndexOf(item);
            // 从队列里选行:连播回到队列,队列和选中行都不换
            SetSequenceSource(WatchSequenceSourceKind.Playlist);
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
            // 一拍还在路上:再按上一个也丢掉,不排队。用户可以继续看,但不能开第二个换目标
            if (transition.IsPending) return;
            RefreshSequenceState();
            var step = WatchSequenceOrder.Previous(sequence.Position);
            switch (step.Move)
            {
                case WatchSequenceMove.PlayPart:
                    // 往回走:回到那一P保存的进度
                    player.ChangePlayIndex(step.PartIndex, PlayTargetStart.Resume);
                    return;
                case WatchSequenceMove.PlayRow:
                    // 往回换的是另一条视频:同样先摆等待态,起播或失败后收起
                    if (!BeginTransition(RowTitle(step.RowIndex), RowKey(step.RowIndex))) return;
                    try
                    {
                        await PlaySequenceRowAsync(step.RowIndex, PlayTargetStart.Resume);
                    }
                    finally
                    {
                        EndTransition();
                    }
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
        /// 远端的一步会摆出等待态;一次只走一拍,等待中再来的请求在这里被挡掉。
        /// </summary>
        private async Task<bool> AdvanceSequenceAsync(bool skipRemainingParts, bool manual = false)
        {
            // 页签已关:播放器已释放,续播不再进行
            if (_isClosed) return false;
            // 详情还在路上:这一拍丢掉,不让它顶掉在途请求
            if (flag) return false;
            // 一拍还在路上:第二次下一个/上一个直接丢掉,不排队。判定只在这一个地方
            if (transition.IsPending) return false;
            // 自动连播关着:播放结束就停在当前这一P,不换下一个视频。分P之间的自动换P
            // 由播放器看同一个开关;用户自己按的下一个/上一个不经过这里
            if (!PlayContinuationPolicy.AdvanceAllowed(manual, SettingHelper.GetValue<bool>(SettingHelper.Player.AUTO_NEXT, true)))
            {
                ShowSequenceEndToast(manual);
                return false;
            }

            // 等待态从第一次落到远端的一步开始,一直到这一拍走完才收
            var pending = false;
            try
            {
                // 一页可能全是已加载的行,取完再判定一次;上限只是防止服务端反复返回 has_more
                for (var attempt = 0; attempt < 3; attempt++)
                {
                    RefreshSequenceState();
                    var step = WatchSequenceOrder.Next(sequence.Position, skipRemainingParts);
                    switch (step.Move)
                    {
                        case WatchSequenceMove.PlayPart:
                            // 分P就在当前这一条视频里:换P是立刻的,不摆等待态
                            player.ChangePlayIndex(step.PartIndex, PlayTargetStart.FromStart);
                            return !_isClosed;
                        case WatchSequenceMove.PlayRow:
                            if (pending)
                            {
                                // 取到页了:等待的文字从"下一页"换成真正的目标行
                                RetitleTransition(RowTitle(step.RowIndex), RowKey(step.RowIndex));
                            }
                            else if (!BeginTransition(RowTitle(step.RowIndex), RowKey(step.RowIndex)))
                            {
                                // 已经有一拍在路上:这一拍不再叠加
                                return false;
                            }
                            pending = true;
                            return await PlaySequenceRowAsync(step.RowIndex, PlayTargetStart.FromStart) && !_isClosed;
                        case WatchSequenceMove.LoadNextPage:
                            if (!pending)
                            {
                                // 还不知道会落在哪一行:等待态先写这一步的目标
                                if (!BeginTransition(DescribeStep(step), "")) return false;
                                pending = true;
                            }
                            // 取页的结果只对发起它的那一拍算数
                            var ticket = transition.Ticket();
                            if (await LoadMediaListPage())
                            {
                                // 取页期间页签被关掉:新行留在状态里,但不选行也不起播
                                if (_isClosed) return false;
                                // 取页期间换了视频或播放列表:这一拍不属于现在,也不再往下推
                                if (!transition.IsCurrent(ticket)) return false;
                                // 新行已经追加到同一个列表,重新判定再走
                                continue;
                            }
                            // 页签已关:失败也不再改计数、不再提示重试
                            if (_isClosed) return false;
                            // 取页期间换了来源、视频或播放列表:这一页的结果不属于现在,
                            // 也不能被记成"断在这一步",否则重试推进的就是用户切到的那个来源
                            if (!transition.IsCurrent(ticket)) return false;
                            // 失败:已加载行不动,"加载更多"仍然可见,就是重试入口。
                            // 本来要走的这一步记下来,重试取到页就自动接着走
                            RefreshMediaListChrome();
                            RememberInterruptedAdvance(skipRemainingParts, manual);
                            Utils.ShowMessageToast("下一页加载失败,点\"加载更多\"重试");
                            return false;
                        case WatchSequenceMove.RetryNextPage:
                            RefreshMediaListChrome();
                            RememberInterruptedAdvance(skipRemainingParts, manual);
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
            finally
            {
                // 这一拍走完(起播、失败、关页)就把等待态收掉;代数让晚到的结果自己作废
                if (pending) EndTransition();
            }
        }

        /// <summary>
        /// 摆出等待态:播放器一条(转圈 + 目标标题),抽屉里那一行一条。
        /// 已经有一拍在路上时返回 false:这一次请求直接丢掉,不排队。
        /// </summary>
        private bool BeginTransition(string targetTitle, string pendingKey)
        {
            if (!transition.Begin(targetTitle, pendingKey)) return false;
            // 有更新的一拍要走:在后台把列表取完这件事让路,手上这一页走完就停
            if (loadAll != null) loadAll.RequestCancel();
            player.ShowTransitionPending(targetTitle);
            RedrawOpenDirectory();
            return true;
        }

        /// <summary>
        /// 同一次换目标换了目标文字:只改播放器和抽屉上的那一条,不重开一拍
        /// </summary>
        private void RetitleTransition(string targetTitle, string pendingKey)
        {
            if (!transition.Retitle(targetTitle, pendingKey)) return;
            player.ShowTransitionPending(targetTitle);
            RedrawOpenDirectory();
        }

        /// <summary>
        /// 这一拍结束:等待态收起,抽屉回到当前行
        /// </summary>
        private void EndTransition()
        {
            transition.Settle();
            player.ClearTransitionPending();
            RedrawOpenDirectory();
        }

        /// <summary>
        /// 换目标断在取页上:记下本来要走的那一步,重试取到页后自动接着走,不必再按一次下一个
        /// </summary>
        private void RememberInterruptedAdvance(bool skipRemainingParts, bool manual)
        {
            var position = sequence.Position;
            transition.RememberAdvance(position.Source, position.RowIndex,
                RowKeyIn(position.Source, position.RowIndex), skipRemainingParts, manual);
        }

        /// <summary>
        /// 用户点了"重试/加载更多":上一次换目标就断在这一页上时接着把它走完。
        /// 取页又失败就留着记录;列表到头就取消并给结束提示;
        /// 来源、那一行或者身份变了就只补行,不推进——记录里的下一步是那个来源的下一步。
        /// </summary>
        private async Task ResumeInterruptedAdvanceAsync(bool landed)
        {
            var record = transition.PendingAdvance;
            if (record == null) return;
            var decision = transition.DecideResume(landed, PlayDirectoryBuilder.PagingOf(mediaListState),
                sequence.Position.Source, RowKeyIn(record.Source, record.RowIndex));
            if (decision == PlayAdvanceResume.Cancel)
            {
                // 列表到头:这一次推进取消,只留原来那句结束提示,不额外再报一次
                ShowSequenceEndToast(record.Manual);
                RedrawOpenDirectory();
                return;
            }
            if (decision != PlayAdvanceResume.Resume) return;
            // 走的是原来那一步,不重新猜:用户按的是"重试",不是又一个下一个
            await AdvanceSequenceAsync(record.SkipRemainingParts, record.Manual);
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
        private async Task<bool> PlaySequenceRowAsync(int rowIndex, PlayTargetStart start)
        {
            if (sequence.Position.Source == WatchSequenceSourceKind.UgcSeason)
            {
                return await PlayCollectionRowAsync(rowIndex, start);
            }
            return await SelectPlaylistRowAsync(rowIndex, start);
        }

        private async Task<bool> PlayCollectionRowAsync(int rowIndex, PlayTargetStart start)
        {
            var aids = VideoUgcSeasonOrder.AidOrder(videoDetailVM?.VideoInfo?.UgcSeason);
            if (rowIndex < 0 || rowIndex >= aids.Count) return false;
            var aid = aids[rowIndex];
            foreach (var listView in ugcSeasonEpisodeLists)
            {
                var episodes = listView.ItemsSource as List<VideoUgcSeasonEpisodeView>;
                var row = episodes?.FirstOrDefault(x => x.Aid == aid);
                if (row == null) continue;
                // 高亮跟着换,但起播由这里发起:续播策略要跟着这一拍传下去,不能让选中事件再猜一次。
                // 合集页签和当前队列都不动。
                SelectUgcSeasonRow(listView, row);
                return await InitializeVideo(aid, start);
            }
            return false;
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
                SetSequenceSource(WatchSequenceSourceKind.UgcSeason);
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
        /// 外层来源第 index 行的身份:合集是那一集的 aid,播放列表是那一行的 aid。
        /// 抽屉的行也按这个 Key 认人,等待态就挂在这一行上。
        /// </summary>
        private string RowKey(int index)
        {
            return RowKeyIn(sequence.Position.Source, index);
        }

        /// <summary>
        /// 某个来源里第 index 行的身份。按来源查而不是按当前来源查:待推进记录里那一行
        /// 属于它自己记下的来源,换来源之后还得能问它原来那一行还在不在
        /// </summary>
        private string RowKeyIn(WatchSequenceSourceKind source, int index)
        {
            if (source == WatchSequenceSourceKind.UgcSeason)
            {
                var aids = VideoUgcSeasonOrder.AidOrder(videoDetailVM?.VideoInfo?.UgcSeason);
                return index >= 0 && index < aids.Count ? aids[index] ?? "" : "";
            }
            if (source != WatchSequenceSourceKind.Playlist) return "";
            var rows = playlist?.Playlist;
            return rows != null && index >= 0 && index < rows.Count ? rows[index].ID ?? "" : "";
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

        private async Task<bool> SelectPlaylistRowAsync(int index, PlayTargetStart start)
        {
            // 页签已关:不换选中行,也不起播
            if (_isClosed) return false;
            // 详情还在路上:这次换行丢掉,队列游标不动
            if (flag) return false;
            if (index < 0 || index >= playlist.Playlist.Count) return false;

            // 远程和内存列表的页签内容都是这个 ListView:高亮跟着换,起播由这里发起,
            // 续播策略才不会被选中事件丢掉
            var listView = playlistPivotItem?.Content as ListView;
            if (listView != null)
            {
                SelectPlaylistRow(listView, index);
            }
            playlist.Index = index;
            return await InitializeVideo(playlist.Playlist[index].ID, start);
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
            // 页签已关:这一拍不打开任何东西
            if (_isClosed) return;
            // 详情还在路上:等它回来再开;只是换目标在等的话照样能开,用户要能继续浏览
            if (flag && !transition.IsPending) return;
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
            var before = pivot == null ? null : pivot.SelectedItem;
            if (!SelectDetailTarget()) return false;
            // 页签本来就停在目标页:切完看不出任何变化,给详情列一次可见的回应
            if (before != null && ReferenceEquals(before, pivot.SelectedItem)) HighlightDetailColumn();
            return true;
        }

        /// <summary>
        /// 把详情列切到当前来源那一页并滚到当前行。返回 false 表示外层来源根本没有详情页
        /// </summary>
        private bool SelectDetailTarget()
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

        // 详情列的那一次回应只挂一个覆盖层,重复点击换的只是它上面的动画
        private Border _detailColumnHighlight;

        /// <summary>
        /// 页签已经停在目标页,切换不会带来任何变化:把整列详情闪一下,告诉用户这一下点到了
        /// </summary>
        private void HighlightDetailColumn()
        {
            var host = pivot == null ? null : pivot.Parent as Panel;
            if (host == null) return;
            if (_detailColumnHighlight == null || !ReferenceEquals(_detailColumnHighlight.Parent, host))
            {
                _detailColumnHighlight = new Border
                {
                    Name = "DetailColumnHighlight",
                    IsHitTestVisible = false,
                    Opacity = 0,
                    Background = Utils.GetBrush("HighLightColor"),
                };
                host.Children.Add(_detailColumnHighlight);
            }
            var duration = TimeSpan.FromMilliseconds(DirectoryDrawerMotion.HighlightMilliseconds);
            var fade = new DoubleAnimationUsingKeyFrames { Duration = duration };
            fade.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = 0 });
            fade.KeyFrames.Add(new LinearDoubleKeyFrame
            {
                KeyTime = TimeSpan.FromMilliseconds(DirectoryDrawerMotion.HighlightMilliseconds * 0.35),
                Value = 0.3,
            });
            fade.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = duration, Value = 0 });
            Storyboard.SetTarget(fade, _detailColumnHighlight);
            Storyboard.SetTargetProperty(fade, "Opacity");
            var storyboard = new Storyboard();
            storyboard.Children.Add(fade);
            storyboard.Begin();
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
            // 一拍换目标还在路上:这次换行丢掉。用户还能继续浏览目录,只是不能同时开第二个换目标
            if (transition.IsPending) return;
            // 用户自己选了行:断在取页上的那一步不再作数,不再自动推进
            transition.DropAdvance();
            // 行的身份是 Key:抽屉里搜过之后 Ordinal 还是来源位置,行在列表里的位置却变了
            var rowIndex = DirectorySourceIndex(kind, row);
            if (rowIndex < 0)
            {
                // 这一行在来源里已经没有了:重画抽屉让它消失,不起播
                RedrawOpenDirectory();
                return;
            }
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
                    SetSequenceSource(WatchSequenceSourceKind.UgcSeason);
                    await PlayCollectionRowAsync(rowIndex, PlayTargetStart.Resume);
                    break;
                case PlayDirectorySectionKind.Playlist:
                    SetSequenceSource(WatchSequenceSourceKind.Playlist);
                    await SelectPlaylistRowAsync(rowIndex, PlayTargetStart.Resume);
                    break;
                default:
                    return;
            }
            // 显式选行之后重画抽屉,让"正在播放"跟着走;重画不会再起播
            RedrawOpenDirectory();
        }

        /// <summary>
        /// 行 → 它所在来源里的下标,按分段查各自的 Key 序列:分P查 cid,合集查分集 aid,
        /// 播放列表查队列行 aid。找不到就是过期行(返回 -1)。Ordinal 是展示位置,
        /// 过滤之后不能拿它当来源下标。
        /// </summary>
        private int DirectorySourceIndex(PlayDirectorySectionKind kind, PlayDirectoryRow row)
        {
            if (row == null) return -1;
            switch (kind)
            {
                case PlayDirectorySectionKind.Parts:
                    return PlayDirectoryBuilder.SourceIndexOf(row.Key, PartKeys());
                case PlayDirectorySectionKind.UgcSeason:
                    return PlayDirectoryBuilder.SourceIndexOf(row.Key,
                        VideoUgcSeasonOrder.AidOrder(videoDetailVM?.VideoInfo?.UgcSeason));
                default:
                    return PlayDirectoryBuilder.SourceIndexOf(row.Key, PlaylistIds());
            }
        }

        private List<string> PartKeys()
        {
            var keys = new List<string>();
            var playInfos = player?.PlayInfos;
            if (playInfos == null) return keys;
            for (var i = 0; i < playInfos.Count; i++)
            {
                var info = playInfos[i];
                // 和建目录同一条身份规则:cid,没有 cid 时退到这个位置
                keys.Add(PlayDirectoryBuilder.PartKey(info == null ? null : info.cid, i));
            }
            return keys;
        }

        private List<string> PlaylistIds()
        {
            var ids = new List<string>();
            var rows = playlist?.Playlist;
            if (rows == null) return ids;
            foreach (var item in rows) ids.Add(item == null ? null : item.ID);
            return ids;
        }

        /// <summary>
        /// 这一行是不是正在播的那条视频:先按 Key 回查到来源下标,再和这个来源现在的下标比。
        /// 队列游标会停在旧的一行,行的 IsCurrent 不能当成"正在播"的证据。
        /// </summary>
        private bool IsPlayingDirectoryRow(PlayDirectorySectionKind kind, PlayDirectoryRow row)
        {
            var index = DirectorySourceIndex(kind, row);
            if (index < 0) return false;
            if (kind == PlayDirectorySectionKind.Parts)
            {
                return player != null && index == player.CurrentPlayIndex;
            }
            var aid = videoDetailVM?.VideoInfo?.aid;
            if (string.IsNullOrEmpty(aid)) return false;
            if (kind == PlayDirectorySectionKind.UgcSeason)
            {
                return index == VideoUgcSeasonOrder.AidOrder(videoDetailVM?.VideoInfo?.UgcSeason).IndexOf(aid);
            }
            return index == PlaylistIndexOf(aid);
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
                    SetSequenceSource(WatchSequenceSourceKind.UgcSeason);
                    break;
                case PlayDirectorySectionKind.Playlist:
                    if (playlist?.Playlist == null || rowIndex >= playlist.Playlist.Count) return;
                    playlist.Index = rowIndex;
                    SetSequenceSource(WatchSequenceSourceKind.Playlist);
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

        /// <summary>
        /// 用户只是换了浏览的分段:页头跟着换成那一段的来源,不换连播来源、不起播、不重取页。
        /// </summary>
        private void player_DirectoryScopeChanged(object sender, PlayDirectorySourceEventArgs e)
        {
            if (_isClosed || player == null || !player.IsDirectoryVisible) return;
            browsedDirectoryKind = e.Kind;
            player.ShowDirectory(BuildDirectoryModel(false));
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

            SetSequenceSource(source);
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
        /// 页面上换连播来源的唯一入口。真换了来源时,断在取页上的那一步和还在路上的取页一起作废:
        /// 那一步是旧来源的下一步,重演它等于把用户切到的这个来源换掉。
        /// 只是浏览抽屉(换看的页签)不经过这里,浏览不受影响。
        /// </summary>
        private void SetSequenceSource(WatchSequenceSourceKind source)
        {
            var changed = sequence.Position.Source != source;
            sequence.SetActiveSource(source);
            if (!changed) return;
            transition.SourceChanged();
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
            // 队列已经拆掉时不再报播放列表的位置:那个列表已经不在了
            if (position.Source == WatchSequenceSourceKind.Playlist && position.RowCount > 0
                && playlist?.Playlist != null)
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
            var playlistSection = playlist?.Playlist == null
                ? null
                : PlayDirectoryBuilder.FromPlaylist(playlist, mediaListState, info == null ? null : info.aid);
            var model = PlayDirectoryBuilder.Combine(parts, collection, playlistSection, sequence.Position.Source);
            // 正在换到的那一行挂转圈;它还不是正在播放的行,两条标记分开
            PlayDirectoryBuilder.ApplyPending(model, transition.PendingKey);
            // "加载全部"跑到哪一步:footer 据此在加载更多/重试和"取消"之间换
            model.LoadAll = loadAll == null ? LoadAllState.Idle : loadAll.State;
            // 新开目录才让浏览的分段回到连播来源;重画留着用户正在看的那一段
            model.IsFreshOpen = freshOpen;
            // 新开目录从连播来源那一段看起,页头也是它
            if (freshOpen) browsedDirectoryKind = null;
            // 页头写用户正在浏览的那一段:重画(换集、翻页)不把这一段的名字换掉
            if (browsedDirectoryKind.HasValue) PlayDirectoryBuilder.WithHeader(model, browsedDirectoryKind.Value);
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
            // 翻页状态也算进签名:加载中、失败后的"重试"、到头都要重画;
            // 等待的那一行换了也要重画,才能把转圈挂到新的目标上
            var paging = (int)PlayDirectoryBuilder.PagingOf(mediaListState);
            var sections = videoDetailVM?.VideoInfo?.UgcSeason?.Sections;
            // "加载全部"开始和停下来都要重画 footer,这一项跟着变
            var loadAllState = loadAll == null ? (int)LoadAllState.Idle : (int)loadAll.State;
            return (int)position.Source
                + "|" + position.RowCount
                + "|" + loaded
                + "|" + paging
                + "|" + loadAllState
                + "|" + (sections == null ? 0 : sections.Count)
                + "|" + (transition.PendingKey ?? "");
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

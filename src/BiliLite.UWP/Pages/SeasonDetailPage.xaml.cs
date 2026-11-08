using BiliLite.Controls;
using BiliLite.Dialogs;
using BiliLite.Helpers;
using BiliLite.Modules;
using BiliLite.Models.Common;
using BiliLite.Modules.Season;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliLite.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SeasonDetailPage : PlayPage
    {
        SeasonDetailVM seasonDetailVM;
        SeasonReviewVM seasonReviewVM;
        string season_id = "";
        string ep_id = "";
        bool selectPreview = false;
        // 用户正在浏览的目录分段(正片/预告)。页头跟着它,重画时不换掉
        PlayDirectorySectionKind? browsedDirectoryKind;
        public SeasonDetailPage()
        {
            this.InitializeComponent();
            Title = "剧集详情";
            this.Loaded += SeasonDetailPage_Loaded;
            this.Player = this.player;
            // 目录只在播放器抽屉里:正片和预告各成一段,页面回答抽屉的事件
            player.DirectoryRequested += Player_DirectoryRequested;
            player.DirectoryRowRequested += Player_DirectoryRowRequested;
            player.DirectoryScopeChanged += Player_DirectoryScopeChanged;
            NavigationCacheMode = NavigationCacheMode.Enabled;
            seasonDetailVM = new SeasonDetailVM();
            seasonReviewVM = new SeasonReviewVM();
            RightInfo.Width = new GridLength(SettingHelper.GetValue<double>(SettingHelper.UI.RIGHT_DETAIL_WIDTH, 320), GridUnitType.Pixel);
        }

        private void SeasonDetailPage_Loaded(object sender, RoutedEventArgs e)
        {
            SubscribeClosedPage();
        }

        private void SubscribeClosedPage()
        {
            if (this.Parent is MyFrame frame)
            {
                frame.ClosedPage -= SeasonDetailPage_ClosedPage;
                frame.ClosedPage += SeasonDetailPage_ClosedPage;
            }
        }

        private void UnsubscribeClosedPage()
        {
            if (this.Parent is MyFrame frame)
            {
                frame.ClosedPage -= SeasonDetailPage_ClosedPage;
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

        private void SeasonDetailPage_ClosedPage(object sender, EventArgs e)
        {
            ClosePage();
        }
        private void ClosePage()
        {
            UnsubscribeClosedPage();
            ReleaseShare();
            if (seasonDetailVM != null)
            {
                seasonDetailVM.Loaded = false;
                seasonDetailVM.Loading = true;
                seasonDetailVM.Detail = null;
            }
            changedFlag = true;
            // 缓存页关掉时不留下一次的分段和按钮标签
            browsedDirectoryKind = null;
            player?.UpdateDirectoryInfo(false, "");
            player?.FullScreen(false);
            player?.MiniWidnows(false);
            player?.Dispose();
        }
        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;
            request.Data.Properties.Title = seasonDetailVM.Detail.title;
            request.Data.SetWebLink(new Uri("http://b23.tv/ss" + season_id));
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SubscribeClosedPage();
            SubscribeShare();

            if (e.NavigationMode == NavigationMode.New)
            {
                if (SettingHelper.GetValue<bool>(SettingHelper.Player.AUTO_FULL_SCREEN, false))
                {
                    player.IsFullScreen = true;
                }
                else
                {
                    player.IsFullWindow = SettingHelper.GetValue<bool>(SettingHelper.Player.AUTO_FULL_WINDOW, false);
                }
                pivot.SelectedIndex = SettingHelper.GetValue<int>(SettingHelper.UI.DETAIL_DISPLAY, 0);
                if (e.Parameter is object[])
                {
                    var pars = e.Parameter as object[];
                    season_id = pars[0].ToString();
                    ep_id = pars[1].ToString();
                }
                else
                {
                    season_id = e.Parameter.ToString();
                }

                await InitSeasonDetail();
            }
            else
            {
                Title = seasonDetailVM?.Detail?.title ?? "视频详情";
                MessageCenter.ChangeTitle(Title);
            }

        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ReleaseShare();
            UnsubscribeClosedPage();
            base.OnNavigatedFrom(e);
        }

        private async Task InitSeasonDetail()
        {
            await seasonDetailVM.LoadSeasonDetail(season_id);

            if (seasonDetailVM.Detail != null)
            {
                ChangeTitle(seasonDetailVM.Detail.title);


                seasonReviewVM.MediaID = seasonDetailVM.Detail.media_id;

                InitializePlayInfo();
                CreateQR();

            }
        }

        private void InitializePlayInfo()
        {
            if (seasonDetailVM.NothingPlay) return;
            selectPreview = !seasonDetailVM.ShowEpisodes;

            var index = 0;
            /*
            if (string.IsNullOrEmpty(ep_id) && seasonDetailVM.Detail.user_status?.progress != null)
            {
                ep_id = seasonDetailVM.Detail.user_status.progress.last_ep_id.ToString();
                SettingHelper.SetValue<double>("ep" + ep_id, Convert.ToDouble(seasonDetailVM.Detail.user_status.progress.last_time));
            }**/
            var selectItem = (selectPreview?seasonDetailVM.Previews: seasonDetailVM.Episodes).FirstOrDefault(x => x.id.ToString() == ep_id);
            if (selectItem != null)
            {
                index = (selectPreview ? seasonDetailVM.Previews : seasonDetailVM.Episodes).IndexOf(selectItem);
            }
            // 链接里的分集不在列表里时从第一集起播:正在播的分集 id 要跟着这一集,
            // 否则目录上的当前行和位置标签会停在链接里那个不存在的 id 上
            var playing = selectPreview ? seasonDetailVM.Previews : seasonDetailVM.Episodes;
            if (index >= 0 && index < playing.Count) ep_id = playing[index].id.ToString();
            if (selectPreview)
            {
                UpdatePlayInfoToPreview(index);
            }
            else
            {
                UpdatePlayInfoToEpisode(index);
            }
            RefreshSeasonDirectory();
        }
        private void UpdatePlayInfoToEpisode(int index)
        {
            List<PlayInfo> playInfos = new List<PlayInfo>();
            int i = 0;
            foreach (var item in seasonDetailVM.Episodes)
            {
                playInfos.Add(new PlayInfo()
                {
                    avid = item.aid,
                    cid = item.cid,
                    duration = 0,
                    season_id = seasonDetailVM.Detail.season_id,
                    season_type = seasonDetailVM.Detail.type,
                    ep_id = item.id.ToString(),
                    is_vip = item.status != 2,
                    is_interaction = false,
                    order = i,
                    play_mode = VideoPlayType.Season,
                    title = item.title + " " + item.long_title,
                    area = Utils.ParseArea(seasonDetailVM.Detail.title, seasonDetailVM.Detail.up_info?.mid??0)
                });
                i++;
            }
            player.InitializePlayInfo(playInfos, index, PlayTargetStart.Resume);
            comment.LoadComment(new LoadCommentInfo()
            {
                CommentMode = (int)Api.CommentApi.CommentType.Video,
                CommentSort = Api.CommentApi.CommentSort.Hot,
                Oid = playInfos[index].avid
            });
        }
        private void UpdatePlayInfoToPreview(int index)
        {
            List<PlayInfo> playInfos = new List<PlayInfo>();
            int i = 0;
            foreach (var item in seasonDetailVM.Previews)
            {
                playInfos.Add(new PlayInfo()
                {
                    avid = item.aid,
                    cid = item.cid,
                    duration = 0,
                    season_id = seasonDetailVM.Detail.season_id,
                    season_type = seasonDetailVM.Detail.type,
                    ep_id = item.id.ToString(),
                    is_vip = item.status != 2,
                    is_interaction = false,
                    order = i,
                    play_mode = VideoPlayType.Season,
                    title = item.title + " " + item.long_title,
                    area=Utils.ParseArea(seasonDetailVM.Detail.title, seasonDetailVM.Detail.up_info?.mid ?? 0)
                });
                i++;
            }
            player.InitializePlayInfo(playInfos, index, PlayTargetStart.Resume);
            comment.LoadComment(new LoadCommentInfo()
            {
                CommentMode = (int)Api.CommentApi.CommentType.Video,
                CommentSort = Api.CommentApi.CommentSort.Hot,
                Oid = playInfos[index].avid
            });
        }
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if(e.NavigationMode== NavigationMode.Back||e.SourcePageType.Name == "BlankPage")
            {
                ClosePage();
            }
            
            base.OnNavigatingFrom(e);
        }
        public void ChangeTitle(string title)
        {
            Title = title;
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
        private void btnShare_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }
        private void btnShareCopy_Click(object sender, RoutedEventArgs e)
        {
            Utils.SetClipboard($"{seasonDetailVM.Detail.title}\r\nhttp://b23.tv/ss{season_id}");
            Utils.ShowMessageToast("已复制内容到剪切板");
        }

        private void btnShareCopyUrl_Click(object sender, RoutedEventArgs e)
        {
            Utils.SetClipboard("http://b23.tv/ss" + season_id);
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
            var aid = "";
            if (selectPreview)
            {
                listPreview.SelectedIndex = e;
                ep_id = seasonDetailVM.Previews[e].id.ToString();
                aid = seasonDetailVM.Previews[e].aid;
            }
            else
            {
                listEpisode.SelectedIndex = e;
                ep_id = seasonDetailVM.Episodes[e].id.ToString();
                aid = seasonDetailVM.Episodes[e].aid;
            }
           
            CreateQR();
            comment.LoadComment(new LoadCommentInfo()
            {
                CommentMode = (int)Api.CommentApi.CommentType.Video,
                CommentSort = Api.CommentApi.CommentSort.Hot,
                Oid = aid
            });
            changedFlag = false;
            // 换集是播放器发起的:目录的当前行、位置标签和开着的抽屉都跟着这一集走
            RefreshSeasonDirectory();
        }

        private void listEpisode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (changedFlag || listEpisode.SelectedIndex == -1)
            {
                return;
            }
            if (selectPreview)
            {
                listPreview.SelectedIndex = -1;
                selectPreview = false;
                UpdatePlayInfoToEpisode(listEpisode.SelectedIndex);
            }
            player.ChangePlayIndex(listEpisode.SelectedIndex, PlayTargetStart.Resume);
            ep_id =seasonDetailVM.Episodes[listEpisode.SelectedIndex].id.ToString();
            comment.LoadComment(new LoadCommentInfo()
            {
                CommentMode = (int)Api.CommentApi.CommentType.Video,
                CommentSort = Api.CommentApi.CommentSort.Hot,
                Oid = seasonDetailVM.Episodes[listEpisode.SelectedIndex].aid
            });
            CreateQR();
        }
        private void listPreview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (changedFlag || listPreview.SelectedIndex == -1)
            {
                return;
            }
            if (!selectPreview)
            {
                listEpisode.SelectedIndex = -1;
                selectPreview = true;
                UpdatePlayInfoToPreview(listPreview.SelectedIndex);
            }
            player.ChangePlayIndex(listPreview.SelectedIndex, PlayTargetStart.Resume);
            ep_id = seasonDetailVM.Previews[listPreview.SelectedIndex].id.ToString();
            comment.LoadComment(new LoadCommentInfo()
            {
                CommentMode = (int)Api.CommentApi.CommentType.Video,
                CommentSort = Api.CommentApi.CommentSort.Hot,
                Oid = seasonDetailVM.Previews[listPreview.SelectedIndex].aid
            });
            CreateQR();
        }


        /// <summary>
        /// 只有一集、又没有预告的剧集没有目录可翻:按钮不出现,抽屉也开不出来
        /// </summary>
        private bool HasSeasonDirectory()
        {
            var episodes = seasonDetailVM?.Episodes == null ? 0 : seasonDetailVM.Episodes.Count;
            var previews = seasonDetailVM?.Previews == null ? 0 : seasonDetailVM.Previews.Count;
            return PlayDirectoryBuilder.HasSeasonDirectory(episodes, previews);
        }

        /// <summary>
        /// "目录"按钮上的短标签:只写正在播的这一段里的位置,段名在页签和页头上
        /// </summary>
        private string SeasonDirectoryCaption()
        {
            var preview = selectPreview;
            var kind = preview ? PlayDirectorySectionKind.SeasonPreviews : PlayDirectorySectionKind.SeasonEpisodes;
            var count = preview
                ? (seasonDetailVM?.Previews == null ? 0 : seasonDetailVM.Previews.Count)
                : (seasonDetailVM?.Episodes == null ? 0 : seasonDetailVM.Episodes.Count);
            return PlayDirectoryBuilder.Caption(kind, CurrentSeasonIndex() + 1, count);
        }

        /// <summary>
        /// 正在播的这一集在它自己那一段里的下标,按分集 id 找;找不到返回 -1
        /// </summary>
        private int CurrentSeasonIndex()
        {
            var list = selectPreview ? seasonDetailVM?.Previews : seasonDetailVM?.Episodes;
            if (list == null) return -1;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].id.ToString() == ep_id) return i;
            }
            return -1;
        }

        /// <summary>
        /// 一段的分集身份:分集 id。目录行的 Key 和这里用的是同一条规则
        /// </summary>
        private List<string> SeasonEpisodeKeys(bool preview)
        {
            var keys = new List<string>();
            var list = preview ? seasonDetailVM?.Previews : seasonDetailVM?.Episodes;
            if (list == null) return keys;
            foreach (var item in list) keys.Add(item == null ? "" : item.id.ToString());
            return keys;
        }

        private List<PlayDirectorySeasonEpisode> SeasonEpisodeRows(bool preview)
        {
            var rows = new List<PlayDirectorySeasonEpisode>();
            var list = preview ? seasonDetailVM?.Previews : seasonDetailVM?.Episodes;
            if (list == null) return rows;
            foreach (var item in list)
            {
                if (item == null) continue;
                rows.Add(new PlayDirectorySeasonEpisode()
                {
                    EpId = item.id.ToString(),
                    Title = EpisodeRowTitle(item),
                    Cover = item.cover,
                });
            }
            return rows;
        }

        /// <summary>
        /// 行标题和播放列表里同一条的写法一致:集号加长标题,缺一半时不留空格
        /// </summary>
        private static string EpisodeRowTitle(SeasonDetailEpisodeModel item)
        {
            var title = (item.title ?? "").Trim();
            var longTitle = (item.long_title ?? "").Trim();
            if (title.Length == 0) return longTitle;
            if (longTitle.Length == 0) return title;
            return title + " " + longTitle;
        }

        /// <summary>
        /// 现搭一份剧集目录:正片、预告各用自己那份列表和自己那个当前分集 id,
        /// 两段的行永远不混在一起。正在播的那一段才算"活动段"。
        /// </summary>
        private PlayDirectoryModel BuildSeasonDirectory(bool freshOpen)
        {
            var title = seasonDetailVM?.Detail?.title;
            var episodes = PlayDirectoryBuilder.FromSeasonSection(PlayDirectorySectionKind.SeasonEpisodes,
                SeasonEpisodeRows(false), selectPreview ? "" : ep_id, title);
            var previews = PlayDirectoryBuilder.FromSeasonSection(PlayDirectorySectionKind.SeasonPreviews,
                SeasonEpisodeRows(true), selectPreview ? ep_id : "", title);
            var model = PlayDirectoryBuilder.CombineSeason(episodes, previews, selectPreview);
            model.IsFreshOpen = freshOpen;
            // 新开目录从正在播的那一段看起,页头也是它
            if (freshOpen) browsedDirectoryKind = null;
            // 页头写用户正在浏览的那一段:重画(换集)不把这一段的名字换掉
            if (browsedDirectoryKind.HasValue) PlayDirectoryBuilder.WithHeader(model, browsedDirectoryKind.Value);
            return model;
        }

        /// <summary>
        /// 目录按钮和开着的抽屉跟着正在播的这一集走;没有目录时按钮收起,抽屉也收起来
        /// </summary>
        private void RefreshSeasonDirectory()
        {
            if (player == null) return;
            var available = HasSeasonDirectory();
            player.UpdateDirectoryInfo(available, available ? SeasonDirectoryCaption() : "");
            if (!available) return;
            RedrawSeasonDirectory();
        }

        private void RedrawSeasonDirectory()
        {
            if (player == null || !player.IsDirectoryVisible) return;
            player.ShowDirectory(BuildSeasonDirectory(false));
        }

        /// <summary>
        /// "目录"按钮:剧集页的选集列把正片和预告叠在一起,抽屉是唯一分开看两段的地方,
        /// 所以这里直接开抽屉。打开目录不停播、不改全屏。
        /// </summary>
        private void Player_DirectoryRequested(object sender, EventArgs e)
        {
            if (player == null || seasonDetailVM?.Detail == null) return;
            if (!HasSeasonDirectory()) return;
            player.ShowDirectory(BuildSeasonDirectory(!player.IsDirectoryVisible));
        }

        /// <summary>
        /// 用户只是换了浏览的段:页头跟着换,不改正在播的那一段,也不重新起播
        /// </summary>
        private void Player_DirectoryScopeChanged(object sender, PlayDirectorySourceEventArgs e)
        {
            if (player == null || !player.IsDirectoryVisible) return;
            browsedDirectoryKind = e.Kind;
            player.ShowDirectory(BuildSeasonDirectory(false));
        }

        /// <summary>
        /// 目录里点了一行:按分集 id 找回它在自己那一段里的下标,再交给页面原有的选集路径
        /// (换段、重建播放列表、按保存的进度起播)。点的就是正在播的那一条时不起播。
        /// </summary>
        private void Player_DirectoryRowRequested(object sender, PlayDirectoryRowActivatedEventArgs e)
        {
            if (e.Row == null || !PlayDirectoryBuilder.IsSeasonKind(e.Kind)) return;
            var preview = e.Kind == PlayDirectorySectionKind.SeasonPreviews;
            var index = PlayDirectoryBuilder.SourceIndexOf(e.Row.Key, SeasonEpisodeKeys(preview));
            if (index < 0)
            {
                // 这一集在详情里已经没有了:重画让它消失,不起播
                RedrawSeasonDirectory();
                return;
            }
            // 正在播的就是这一条:只把抽屉重画一遍,不重新起播
            if (preview == selectPreview && index == CurrentSeasonIndex())
            {
                RedrawSeasonDirectory();
                return;
            }
            // 选中行由那两个列表自己触发换集;列表选中项已经是这一行时先清掉,事件才会来
            if (preview)
            {
                if (listPreview.SelectedIndex == index) listPreview.SelectedIndex = -1;
                listPreview.SelectedIndex = index;
            }
            else
            {
                if (listEpisode.SelectedIndex == index) listEpisode.SelectedIndex = -1;
                listEpisode.SelectedIndex = index;
            }
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
                var data = barcodeWriter.Write("https://b23.tv/ep" + ep_id);
                imgQR.Source = data;
            }
            catch (Exception ex)
            {
                LogHelper.Log("创建二维码失败epid" + ep_id, LogType.ERROR, ex);
                Utils.ShowMessageToast("创建二维码失败");
            }

        }

        private void SeasonList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SeasonList.SelectedItem == null)
            {
                return;
            }
            var item = SeasonList.SelectedItem as SeasonDetailSeasonItemModel;
            if (item.season_id.ToString() != season_id)
            {
                
                MessageCenter.NavigateToPage(this, new NavigationInfo()
                {
                    icon = Symbol.Play,
                    page = typeof(SeasonDetailPage),
                    parameters = item.season_id,
                    title = item.title
                });
                //this.Frame.Navigate(typeof(SeasonDetailPage), item.season_id);
            }
        }

        private void btnOpenIndexWithArea_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as HyperlinkButton).DataContext as SeasonDetailAreaItemModel;
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Filter,
                page = typeof(Bangumi.AnimeIndexPage),
                title = data.name,
                parameters = new SeasonIndexParameter()
                {
                    type = (IndexSeasonType)seasonDetailVM.Detail.type,
                    area = data.id
                }
            });
        }

        private void btnOpenIndexWithStyle_Click(object sender, RoutedEventArgs e)
        {

            var data = (sender as HyperlinkButton).DataContext as SeasonDetailStyleItemModel;
            //防止报错
            if (string.IsNullOrEmpty(data.id)) return;
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Filter,
                page = typeof(Bangumi.AnimeIndexPage),
                title = data.name,
                parameters = new SeasonIndexParameter()
                {
                    type = (IndexSeasonType)seasonDetailVM.Detail.type,
                    style = data.id
                }
            });
        }

        private async void pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pivot.SelectedIndex == 3 && (seasonReviewVM.Items == null || seasonReviewVM.Items.Count==0))
            {
                await seasonReviewVM.GetItems();
            }
        }

        private void btnReviewLike_Click(object sender, RoutedEventArgs e)
        {
            var item= (sender as HyperlinkButton).DataContext as SeasonShortReviewItemModel;
            seasonReviewVM.Like(item);
        }

       
        private void btnReviewDislike_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as HyperlinkButton).DataContext as SeasonShortReviewItemModel;
            seasonReviewVM.Dislike(item);
        }

        private async void btnSendReview_Click(object sender, RoutedEventArgs e)
        {
            if (seasonReviewVM == null || seasonReviewVM.MediaID == 0) return;
            if (!SettingHelper.Account.Logined && !await Utils.ShowLoginDialog())
            {
                Utils.ShowMessageToast("请先登录后再操作");
                return;
            }
            
            SendReviewDialog sendReviewDialog = new SendReviewDialog(seasonReviewVM.MediaID);
            await sendReviewDialog.ShowAsync();
        }

        private async void btnOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            await Utils.LaunchUri(new Uri("http://b23.tv/ss" + season_id));
        }

        private void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (seasonDetailVM.Detail?.cover == null) return;
            MessageCenter.OpenImageViewer(new List<string>() {
                seasonDetailVM.Detail.cover
            }, 0);
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (seasonDetailVM.Detail == null || seasonDetailVM.Detail.episodes == null || seasonDetailVM.Detail.episodes.Count == 0) return;
            var downloadItem = new DownloadItem()
            {
                Cover = seasonDetailVM.Detail.cover,
               SeasonID= seasonDetailVM.Detail.season_id,
               SeasonType=seasonDetailVM.Detail.type,
                Episodes = new List<DownloadEpisodeItem>(),
                Subtitle = seasonDetailVM.Detail.subtitle,
                Title = seasonDetailVM.Detail.title,
                UpMid=seasonDetailVM.Detail.up_info?.mid??0,
                Type = DownloadType.Season
            };
            int i = 0;
            foreach (var item in seasonDetailVM.Detail.episodes)
            {
                // 检查正在下载及下载完成是否存在此视频
                int state = 0;
                if (DownloadVM.Instance.Downloadings.FirstOrDefault(x => x.EpisodeID == item.id.ToString()) != null)
                {
                    state = 2;
                }
                if (DownloadVM.Instance.Downloadeds.FirstOrDefault(x => x.Episodes.FirstOrDefault(y => y.EpisodeID == item.id.ToString()) != null) != null)
                {
                    state = 3;
                }

                //如果正在下载state=2,下载完成state=3
                downloadItem.Episodes.Add(new DownloadEpisodeItem()
                {
                    CID = item.cid,
                    EpisodeID = item.id.ToString(),
                    Index = i,
                    Title = item.title+" "+ item.long_title,
                    State = state,
                    AVID=item.aid,
                    BVID=item.bvid,
                    ShowBadge= item.show_badge,
                    Badge=item.badge,
                    IsPreview=item.IsPreview
                });
                i++;
            }

            DownloadDialog downloadDialog = new DownloadDialog(downloadItem);
            await downloadDialog.ShowAsync();
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (seasonDetailVM.Loading) return;
            await InitSeasonDetail();
        }

        private void btnOpenQR_Click(object sender, RoutedEventArgs e)
        {
            qrFlyout.ShowAt(btnMore);
        }
    }
}

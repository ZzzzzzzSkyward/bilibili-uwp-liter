using BiliLite.Controls;
using BiliLite.Helpers;
using BiliLite.Models.Common;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliLite.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class LocalPlayerPage : PlayPage
    {
        // 下载/合集的多集都在 PlayInfos 里,本地播放这一页的目录只有分P一段
        private const string LocalDirectorySourceTitle = "本地播放";
        private LocalPlayInfo _playInfo;
        private bool _isClosed;
        private string _directorySignature;

        public LocalPlayerPage()
        {
            this.InitializeComponent();
            Title = "本地播放";
            this.Loaded += LocalPlayerPage_Loaded;
            this.Player = this.player;
            player.FullScreenEvent += Player_FullScreenEvent;
            // 目录和换集由本页接:播放器只把请求喊出来,列表内容归本页
            SubscribePlayerDirectory();
        }

        private void SubscribePlayerDirectory()
        {
            player.DirectoryRequested -= Player_DirectoryRequested;
            player.DirectoryRequested += Player_DirectoryRequested;
            player.DirectoryRowRequested -= Player_DirectoryRowRequested;
            player.DirectoryRowRequested += Player_DirectoryRowRequested;
            player.ChangeEpisodeEvent -= Player_ChangeEpisodeEvent;
            player.ChangeEpisodeEvent += Player_ChangeEpisodeEvent;
        }

        private void UnsubscribePlayerDirectory()
        {
            player.DirectoryRequested -= Player_DirectoryRequested;
            player.DirectoryRowRequested -= Player_DirectoryRowRequested;
            player.ChangeEpisodeEvent -= Player_ChangeEpisodeEvent;
        }

        private void Player_FullScreenEvent(object sender, bool e)
        {
            if (e)
            {
                this.Margin = new Thickness(0, SettingHelper.GetValue<int>(SettingHelper.UI.DISPLAY_MODE, 0) == 0 ? -40 : -32, 0, 0);

            }
            else
            {
                this.Margin = new Thickness(0);

            }
        }

        private void LocalPlayerPage_Loaded(object sender, RoutedEventArgs e)
        {
            SubscribeClosedPage();
        }

        private void SubscribeClosedPage()
        {
            if (this.Parent is MyFrame frame)
            {
                frame.ClosedPage -= LocalPlayerPage_ClosedPage;
                frame.ClosedPage += LocalPlayerPage_ClosedPage;
            }
        }

        private void UnsubscribeClosedPage()
        {
            if (this.Parent is MyFrame frame)
            {
                frame.ClosedPage -= LocalPlayerPage_ClosedPage;
            }
        }

        private void LocalPlayerPage_ClosedPage(object sender, EventArgs e)
        {
            _isClosed = true;
            UnsubscribePlayerDirectory();
            Player_FullScreenEvent(this, false);
            player.MiniWidnows(false);
            player?.Dispose();

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SubscribeClosedPage();
            SubscribePlayerDirectory();
            _isClosed = false;
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
                var data = e.Parameter as LocalPlayInfo;
                if (data == null) return;
                _playInfo = data;
                player.InitializePlayInfo(data.PlayInfos, data.Index, PlayTargetStart.Resume);
                // 目录按钮和开着的抽屉都要按这一次的列表报位置
                RefreshDirectoryState();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            UnsubscribeClosedPage();
            UnsubscribePlayerDirectory();
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back || e.SourcePageType == typeof(BlankPage))
            {
                Player_FullScreenEvent(this, false);
                player.MiniWidnows(false);
                player?.Dispose();
            }
            base.OnNavigatingFrom(e);
        }

        /// <summary>
        /// 本地播放的目录只有分P一段:每个 PlayInfo 一条,标题/身份都照播放器的列表。
        /// 时长的单位是毫秒,目录要秒。
        /// </summary>
        private List<PlayDirectoryPart> LocalParts()
        {
            var parts = new List<PlayDirectoryPart>();
            var playInfos = _playInfo?.PlayInfos;
            if (playInfos == null) return parts;
            foreach (var info in playInfos)
            {
                if (info == null) continue;
                parts.Add(new PlayDirectoryPart()
                {
                    Cid = info.cid,
                    Title = info.title,
                    DurationSeconds = info.duration / 1000,
                });
            }
            return parts;
        }

        private int LocalPartCount()
        {
            var playInfos = _playInfo?.PlayInfos;
            if (playInfos == null) return 0;
            var count = 0;
            foreach (var info in playInfos)
            {
                if (info != null) count++;
            }
            return count;
        }

        /// <summary>
        /// 用这一页的列表现搭一份目录:只有分P,没有合集也没有播放列表。
        /// </summary>
        private PlayDirectoryModel BuildDirectoryModel(bool freshOpen)
        {
            var model = PlayDirectoryBuilder.FromParts(LocalParts(),
                player == null ? 0 : player.CurrentPlayIndex,
                LocalDirectorySourceTitle,
                false);
            model.IsFreshOpen = freshOpen;
            return model;
        }

        /// <summary>
        /// "目录"按钮报位置:只有一集时整条收起,和以前一样没有目录。
        /// </summary>
        private void RefreshDirectoryState()
        {
            if (_isClosed || player == null) return;
            var available = LocalPartCount() > 1;
            // 没有目录就不建行:按钮只剩图标,抽屉也不会开
            var model = available ? BuildDirectoryModel(false) : null;
            player.UpdateDirectoryInfo(available, model == null ? "" : model.Caption);
            RefreshOpenDirectory(model);
        }

        /// <summary>
        /// 抽屉开着时按"当前下标 + 条数"判断要不要重建:换集只改正在播放那行,
        /// 这一拍没变就不重画。
        /// </summary>
        private void RefreshOpenDirectory(PlayDirectoryModel model)
        {
            if (player == null || !player.IsDirectoryVisible)
            {
                _directorySignature = null;
                return;
            }
            var signature = DirectorySignature();
            if (signature == _directorySignature) return;
            _directorySignature = signature;
            player.ShowDirectory(model ?? BuildDirectoryModel(false));
        }

        private string DirectorySignature()
        {
            return (player == null ? 0 : player.CurrentPlayIndex) + "|" + LocalPartCount();
        }

        /// <summary>
        /// "目录"只喊一声:打开抽屉,或者刷新已经开着的那一份,不停播、不改全屏。
        /// </summary>
        private void Player_DirectoryRequested(object sender, EventArgs e)
        {
            if (_isClosed || player == null) return;
            player.ShowDirectory(BuildDirectoryModel(!player.IsDirectoryVisible));
        }

        /// <summary>
        /// 目录里点了一行:本地播放只有分P一段,点第几行就换到第几P。
        /// 换集由播放器负责,这里不重画;正在播的那一行点了不重播。
        /// </summary>
        private void Player_DirectoryRowRequested(object sender, PlayDirectoryRowActivatedEventArgs e)
        {
            if (_isClosed || player == null || e == null || e.Row == null) return;
            if (e.Kind != PlayDirectorySectionKind.Parts) return;
            var index = e.Row.Ordinal - 1;
            if (index < 0 || index >= LocalPartCount()) return;
            if (index == player.CurrentPlayIndex) return;
            player.ChangePlayIndex(index, PlayTargetStart.Resume);
        }

        /// <summary>
        /// 换集:按钮上的位置和开着的抽屉都跟着换到新的这一P。
        /// </summary>
        private void Player_ChangeEpisodeEvent(object sender, int e)
        {
            if (_isClosed) return;
            RefreshDirectoryState();
        }
    }
    public class LocalPlayInfo
    {
        public List<PlayInfo> PlayInfos { get; set; }
        public int Index { get; set; } = 0;
    }
}

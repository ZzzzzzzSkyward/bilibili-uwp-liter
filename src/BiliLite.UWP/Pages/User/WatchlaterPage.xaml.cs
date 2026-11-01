using BiliLite.Helpers;
using BiliLite.Models.Common;
using BiliLite.Modules.User;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliLite.Pages.User
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class WatchlaterPage : BasePage
    {
        WatchLaterVM watchLaterVM;
        public WatchlaterPage()
        {
            this.InitializeComponent();
            Title = "稍后再看";
            watchLaterVM = new WatchLaterVM();
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if(e.NavigationMode== NavigationMode.New)
            {
               await watchLaterVM.LoadData();
            }
        }

        private void Video_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (watchLaterVM.Videos == null) return;
            var data = e.ClickedItem as WatchlaterItemModel;
            List<VideoPlaylistItem> items = new List<VideoPlaylistItem>();
            // 队列剔掉了"已失效视频",队列下标和取数据时的列表下标不是一回事,
            // 所以这里顺手把队列的 aid 记下来,点击时按 aid 定位要播的那一行
            List<string> queueAids = new List<string>();
            foreach (var item in watchLaterVM.Videos)
            {
                if (item.title != "已失效视频")
                {
                    items.Add(new VideoPlaylistItem()
                    {
                        Cover = item.pic,
                        Author = item.owner.name,
                        ID = item.aid,
                        Title = item.title
                    });
                    queueAids.Add(item.aid);
                }

            }
            var index = VideoPlaylistOrder.IndexOfItem(queueAids, data == null ? null : data.aid);
            if (index < 0)
            {
                // 失效视频的详情页还能点进来,但它不在队列里;列表也可能刚被清过
                Utils.ShowMessageToast("视频不在稍后再看列表里");
                return;
            }
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Play,
                page = typeof(VideoDetailPage),
                title = "视频播放",
                parameters = new VideoPlaylist()
                {
                    Index = index,
                    Playlist = items
                }
            });
        }
    }
}

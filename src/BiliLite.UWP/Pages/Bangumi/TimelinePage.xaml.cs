﻿using BiliLite.Helpers;
using BiliLite.Modules;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliLite.Pages.Bangumi
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class TimelinePage : BasePage
    {
        Modules.AnimeTimelineVM timelineVM;
        public TimelinePage()
        {
            this.InitializeComponent();
            Title = "番剧时间表";
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if(e.NavigationMode== NavigationMode.New)
            {
                timelineVM = new AnimeTimelineVM((AnimeType)e.Parameter);
                this.DataContext = timelineVM;
                // timeLine.ItemsSource = e.Parameter as List<AnimeTimelineModel>;
                // timeLine.SelectedItem = (e.Parameter as List<AnimeTimelineModel>).FirstOrDefault(x => x.is_today);
            }
        }

        private async void cbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbType.SelectedItem == null)
            {
                return;
            }
            timelineVM.animeType = (cbType.SelectedItem as AnimeTypeItem).AnimeType;
            await timelineVM.GetTimeline();
        }

        private void AdaptiveGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as AnimeTimelineItemModel;
            MessageCenter.NavigateToPage(sender, new NavigationInfo()
            {
                icon = Symbol.Play,
                page = typeof(Pages.SeasonDetailPage),
                parameters = item.season_id,
                title = item.title
            });
        }
    }
}

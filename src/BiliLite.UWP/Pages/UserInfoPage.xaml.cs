﻿using BiliLite.Helpers;
using BiliLite.Modules.User;
using BiliLite.Modules.User.UserDetail;
using BiliLite.Pages.User;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliLite.Pages
{
    public enum UserTab
    {
        SubmitVideo = 0,
        Dynamic = 1,
        Article = 2,
        Favorite = 3,
        Attention = 4,
        Fans = 5,
    }
    public class UserInfoParameter
    {
        public string Mid { get; set; }
        public UserTab Tab { get; set; } = UserTab.SubmitVideo;
    }
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class UserInfoPage : BasePage
    {
        readonly DynamicVM dynamicVM;
        UserDetailVM userDetailVM;
        UserSubmitVideoVM userSubmitVideoVM;
        UserSubmitArticleVM userSubmitArticleVM;
        UserFavlistVM userFavlistVM;
        UserFollowVM fansVM;
        UserFollowVM followVM;
        private bool IsStaggered { get; set; } = false;
        bool isSelf = false;
        public UserInfoPage()
        {
            this.InitializeComponent();
            Title = "用户中心";
            userDetailVM = new Modules.User.UserDetailVM();
            userSubmitVideoVM = new UserSubmitVideoVM();
            userSubmitArticleVM = new UserSubmitArticleVM();
            userFavlistVM = new UserFavlistVM();
            dynamicVM = new DynamicVM();
            fansVM = new UserFollowVM(true);
            followVM = new UserFollowVM(false);
            dynamicVM.OpenCommentEvent += DynamicVM_OpenCommentEvent;
            splitView.PaneClosed += SplitView_PaneClosed;
        }
        private void SplitView_PaneClosed(SplitView sender, object args)
        {
            comment.ClearComment();
            repost.dynamicRepostVM.Clear();
        }
        string dynamic_id;
        private void DynamicVM_OpenCommentEvent(object sender, Controls.Dynamic.DynamicItemDisplayModel e)
        {
            //splitView.IsPaneOpen = true;
            dynamic_id = e.DynamicID;
            pivotRight.SelectedIndex = 1;
            repostCount.Text = e.ShareCount.ToString();
            commentCount.Text = e.CommentCount.ToString();
            Api.CommentApi.CommentType commentType = Api.CommentApi.CommentType.Dynamic;
            var id = e.ReplyID;
            switch (e.Type)
            {

                case Controls.Dynamic.DynamicDisplayType.Photo:
                    commentType = Api.CommentApi.CommentType.Photo;
                    break;
                case Controls.Dynamic.DynamicDisplayType.Video:

                    commentType = Api.CommentApi.CommentType.Video;
                    break;
                case Controls.Dynamic.DynamicDisplayType.Season:
                    id = e.OneRowInfo.AID;
                    commentType = Api.CommentApi.CommentType.Video;
                    break;
                case Controls.Dynamic.DynamicDisplayType.ShortVideo:
                    commentType = Api.CommentApi.CommentType.MiniVideo;
                    break;
                case Controls.Dynamic.DynamicDisplayType.Music:
                    commentType = Api.CommentApi.CommentType.Song;
                    break;
                case Controls.Dynamic.DynamicDisplayType.Article:
                    commentType = Api.CommentApi.CommentType.Article;
                    break;
                case Controls.Dynamic.DynamicDisplayType.MediaList:
                    if (e.OneRowInfo.Tag != "收藏夹")
                        commentType = Api.CommentApi.CommentType.Video;
                    break;
                default:
                    id = e.DynamicID;
                    break;
            }
            Utils.ShowComment(id, (int)commentType, Api.CommentApi.CommentSort.Hot);
            //comment.LoadComment(new Controls.LoadCommentInfo()
            //{
            //    CommentMode = (int)commentType,
            //    CommentSort = Api.CommentApi.commentSort.Hot,
            //    Oid = id
            //});
        }
        protected  override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SetStaggered();
            if (e.NavigationMode == NavigationMode.New)
            {
                var mid = "";
                var tabIndex = 0;
                if (e.Parameter is UserInfoParameter)
                {
                    var par = e.Parameter as UserInfoParameter;
                    mid = par.Mid;
                    tabIndex = (int)par.Tab;
                }
                else
                {
                    mid = e.Parameter.ToString();
                }
                userDetailVM.mid = mid;
                userSubmitVideoVM.mid = mid;
                userSubmitArticleVM.mid = mid;
                userFavlistVM.mid = mid;
                fansVM.mid = mid;
                followVM.mid = mid;
                if (userDetailVM.mid == SettingHelper.Account.UserID.ToString())
                {
                    isSelf = true;
                    appBar.Visibility = Visibility.Collapsed;
                    followHeader.Visibility = Visibility.Visible;
                }
                else
                {
                    isSelf = false;
                    followHeader.Visibility = Visibility.Collapsed;
                }
                dynamicVM.DynamicType = DynamicType.Space;
                dynamicVM.Uid = mid;
                userDetailVM.GetUserInfo();

                if (tabIndex != 0)
                {
                    pivot.SelectedIndex = tabIndex;
                }
               
            }
        }

        private void SubmitVideo_ItemClick(object sender, ItemClickEventArgs e)
        {
            var data = e.ClickedItem as SubmitVideoItemModel;
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Play,
                page = typeof(VideoDetailPage),
                title = data.title,
                parameters = data.aid
            });
        }

        private void btnLiveRoom_Click(object sender, RoutedEventArgs e)
        {
            if (userDetailVM.UserInfo == null) return;
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Video,
                page = typeof(LiveDetailPage),
                title = userDetailVM.UserInfo.name + "的直播间",
                parameters = userDetailVM.UserInfo.live_room.roomid
            });
        }

        private void btnChat_Click(object sender, RoutedEventArgs e)
        {
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Message,
                title = "消息中心",
                page = typeof(WebPage),
                parameters = $"https://message.bilibili.com/#whisper/mid{ userDetailVM.mid}"
            });
        }

        private void searchVideo_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            userSubmitVideoVM.Refresh();
        }

        private void comVideoOrder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            userSubmitVideoVM?.Refresh();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (userSubmitVideoVM != null && userSubmitVideoVM.CurrentTid != userSubmitVideoVM.SelectTid.tid)
            {

                userSubmitVideoVM?.Refresh();
            }

        }

        private void pivotRight_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pivotRight.SelectedIndex == 0 && splitView.IsPaneOpen && (repost.dynamicRepostVM.Items == null || repost.dynamicRepostVM.Items.Count == 0))
            {
                repost.LoadData(dynamic_id);
            }
        }

        void SetStaggered()
        {
            var staggered = SettingHelper.GetValue<int>(SettingHelper.UI.DYNAMIC_DISPLAY_MODE, 0) == 1;
            if (staggered != IsStaggered)
            {
                IsStaggered = staggered;
                if (staggered)
                {
                    btnGrid_Click(this, null);
                }
                else
                {
                    btnList_Click(this, null);
                }
            }
        }

        private void btnGrid_Click(object sender, RoutedEventArgs e)
        {
            SettingHelper.SetValue<int>(SettingHelper.UI.DYNAMIC_DISPLAY_MODE, 1);
            IsStaggered = true;
            btnGrid.Visibility = Visibility.Collapsed;
            btnList.Visibility = Visibility.Visible;
            //XAML
            list.ItemsPanel = (ItemsPanelTemplate)this.Resources["GridPanel"];
        }

        private void btnList_Click(object sender, RoutedEventArgs e)
        {
            IsStaggered = false;
            //右下角按钮
            btnGrid.Visibility = Visibility.Visible;
            btnList.Visibility = Visibility.Collapsed;
            //设置
            SettingHelper.SetValue<int>(SettingHelper.UI.DYNAMIC_DISPLAY_MODE, 0);
            //XAML
            list.ItemsPanel = (ItemsPanelTemplate)this.Resources["ListPanel"];
        }

        private void btnTop_Click(object sender, RoutedEventArgs e)
        {
            list.ScrollIntoView(list.Items[0]);
        }

        private async void pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pivot.SelectedIndex == 0 && userSubmitVideoVM.SubmitVideoItems == null)
            {
                await userSubmitVideoVM.GetSubmitVideo();
            }
            if (pivot.SelectedIndex == 1 && dynamicVM.Items == null)
            {
                await dynamicVM.GetDynamicItems();
            }
            if (pivot.SelectedIndex == 2 && userSubmitArticleVM.SubmitArticleItems == null)
            {
                await userSubmitArticleVM.GetSubmitArticle();
            }
            if (pivot.SelectedIndex == 3 && userFavlistVM.Items == null)
            {
                await userFavlistVM.Get();
            }
            if (pivot.SelectedIndex == 4 && followVM.Items == null)
            {
                if (isSelf)
                {
                    await followVM.GetTags();
                }
                
                await followVM.Get();
            }
            if (pivot.SelectedIndex == 5 && fansVM.Items == null)
            {
                await fansVM.Get();
            }
        }

        private void SubmitArticle_ItemClick(object sender, ItemClickEventArgs e)
        {
            var data = e.ClickedItem as SubmitArticleItemModel;
            MessageCenter.NavigateToPage(null, new NavigationInfo()
            {
                icon = Symbol.Document,
                page = typeof(WebPage),
                title = data.title,
                parameters = "https://www.bilibili.com/read/cv" + data.id
            });
        }

        private void comArticleOrder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            userSubmitArticleVM?.Refresh();
        }

        private void FavList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var data = e.ClickedItem as FavFolderItemModel;
            MessageCenter.NavigateToPage(null, new NavigationInfo()
            {
                icon = Symbol.OutlineStar,
                page = typeof(FavoriteDetailPage),
                title = "收藏夹",
                parameters = new FavoriteDetailArgs()
                {
                    Id = data.id.ToString(),
                }
            });
        }

        private void UserList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var data = e.ClickedItem as UserFollowItemModel;
            MessageCenter.NavigateToPage(null, new NavigationInfo()
            {
                icon = Symbol.Contact,
                page = typeof(UserInfoPage),
                title = data.uname,
                parameters = data.mid
            });
        }

        private void AddToWatchLater_Click(object sender, RoutedEventArgs e)
        {
            var data = (sender as MenuFlyoutItem).DataContext as SubmitVideoItemModel;
            Modules.User.WatchLaterVM.Instance.AddToWatchlater(data.aid);
        }

        private void comFollowOrder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            followVM.Refresh();
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (followVM != null && followVM.CurrentTid != followVM.SelectTid.tagid)
            {
                if (followVM.SelectTid.tagid == -1)
                {
                    searchFollow.Visibility = Visibility.Visible;
                }
                else
                {
                    searchFollow.Visibility = Visibility.Collapsed;
                }
                followVM.Refresh();
            }

        }

        private void searchFollow_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            followVM.Refresh();
        }
    }
}

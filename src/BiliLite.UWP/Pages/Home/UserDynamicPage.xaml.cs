using BiliLite.Helpers;
using BiliLite.Modules.User;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliLite.Pages.Home
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class UserDynamicPage : Page
    {
        readonly DynamicVM dynamicVM;
        private bool IsStaggered { get; set; } =false;
        public UserDynamicPage()
        {
            var stage = "viewmodel-create";
            LogHelper.Log("Main dynamics stage=constructor-start", LogType.INFO);
            try
            {
                dynamicVM = new DynamicVM();
                LogHelper.Log("Main dynamics stage=viewmodel-created initialType=" + dynamicVM.UserDynamicType, LogType.DEBUG);
                stage = "xaml-initialize";
                this.InitializeComponent();
                LogHelper.Log("Main dynamics stage=xaml-initialized", LogType.INFO);
                dynamicVM.OpenCommentEvent += DynamicVM_OpenCommentEvent;
                splitView.PaneClosed += SplitView_PaneClosed;
                HookDynamicListAnchor();
                this.DataContext = dynamicVM;
                if (SettingHelper.GetValue<bool>(SettingHelper.UI.CACHE_HOME, true))
                {
                    this.NavigationCacheMode = NavigationCacheMode.Enabled;
                }
                else
                {
                    this.NavigationCacheMode = NavigationCacheMode.Disabled;
                }
                SetBackground();
                LogHelper.Log("Main dynamics stage=constructor-complete", LogType.INFO);
            }
            catch (Exception ex)
            {
                LogHelper.Log("Main dynamics stage=" + stage + " failed", LogType.ERROR, ex);
                throw;
            }
        }

        private async void SetBackground()
        {
            var canshow = SettingHelper.GetValue("showbgondynamic", false);
            LogHelper.Log("Main dynamics stage=background-start enabled=" + canshow, LogType.DEBUG);
            if(!canshow) { return; }
            try
            {
                var background = SettingHelper.GetValue(SettingHelper.UI.BACKGROUND_IMAGE, AppHelper.BACKGROUND_IAMGE_URL);
                if (background == AppHelper.BACKGROUND_IAMGE_URL)
                {
                    BGImage.Source = new BitmapImage(new Uri(background));
                }
                else
                {
                    StorageFile file = null;
                    try
                    {
                        file = await StorageFile.GetFileFromPathAsync(background);
                    }
                    catch
                    {
                        Utils.ShowMessageToast("请授予系统存储权限", 5);
                    }
                    if (file == null)
                    {
                        return;
                    }
                    var img = new BitmapImage();
                    img.SetSource(await file.OpenReadAsync());
                    BGImage.Source = img;
                    BGImage.Visibility = Visibility.Visible;
                }
                LogHelper.Log("Main dynamics stage=background-complete", LogType.DEBUG);
            }
            catch (Exception ex)
            {
                LogHelper.Log("Main dynamics stage=background-failed", LogType.ERROR, ex);
                throw;
            }
        }

        private void SplitView_PaneClosed(SplitView sender, object args)
        {
            comment.ClearComment();
            repost.dynamicRepostVM.Clear();
        }
        string dynamic_id;
        private void DynamicVM_OpenCommentEvent(object sender, Controls.Dynamic.DynamicItemDisplayModel e)
        {
            // forbidden=true 是服务端明确的"不许评论"：不能把按钮当成一个会成功的动作
            if (!e.CanComment)
            {
                Utils.ShowMessageToast("这条动态已关闭评论");
                return;
            }
           // splitView.IsPaneOpen = true;
            dynamic_id = e.DynamicID;
            pivotRight.SelectedIndex = 1;
            repostCount.Text = e.ShareCount.ToString();
            commentCount.Text = e.CommentCount.ToString();
            var target = e.CommentTarget;
            if (target != null)
            {
                // 适配器给的是真实的一对 (oid, 评论类型)：直接用，不按显示类型再猜
                Utils.ShowComment(target.Oid, target.Type, Api.CommentApi.CommentSort.Hot);
                return;
            }
            Api.CommentApi.CommentType commentType= Api.CommentApi.CommentType.Dynamic;
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
                    {
                        if (e.OneRowInfo.Tag == "合集")
                            id = e.OneRowInfo.AID;
                        commentType = Api.CommentApi.CommentType.Video;
                    }
                    break;
                default:
                    id = e.DynamicID;
                    break;
            }

            //comment.LoadComment(new Controls.LoadCommentInfo()
            //{
            //    commentMode = (int)commentType,
            //    commentSort = Api.CommentApi.commentSort.Hot,
            //    oid = id
            //});
            Utils.ShowComment(id, (int)commentType, Api.CommentApi.CommentSort.Hot);
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            var stage = "navigated-to";
            LogHelper.Log("Main dynamics stage=navigated-to mode=" + e.NavigationMode + " hasItems=" + (dynamicVM.Items != null), LogType.INFO);
            try
            {
                base.OnNavigatedTo(e);
                stage = "set-layout";
                SetStaggered();
                LogHelper.Log("Main dynamics stage=layout-set staggered=" + IsStaggered, LogType.DEBUG);
                if (e.NavigationMode== NavigationMode.New && dynamicVM.Items == null)
                {
                    stage = "load-items";
                    LogHelper.Log("Main dynamics stage=load-items-begin", LogType.INFO);
                    await dynamicVM.GetDynamicItems();
                    LogHelper.Log("Main dynamics stage=load-items-returned count=" + (dynamicVM.Items?.Count ?? 0), LogType.INFO);
                    if (SettingHelper.GetValue<bool>("动态切换提示", true) && SettingHelper.GetValue<int>(SettingHelper.UI.DYNAMIC_DISPLAY_MODE, 0) != 1)
                    {
                        SettingHelper.SetValue("动态切换提示", false);
                        Utils.ShowMessageToast("右下角可以切换成瀑布流显示哦~", 5);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log("Main dynamics stage=" + stage + " failed", LogType.ERROR, ex);
                throw;
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

        private void pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var stage = "pivot-selection";
            try
            {
                LogHelper.Log("Main dynamics stage=pivot-selection index=" + pivot.SelectedIndex + " type=" + dynamicVM.UserDynamicType, LogType.DEBUG);
                if ((int)dynamicVM.UserDynamicType != pivot.SelectedIndex)
                {
                    dynamicVM.UserDynamicType = (Api.User.DynamicAPI.UserDynamicType)pivot.SelectedIndex;
                    LogHelper.Log("Main dynamics stage=pivot-type-changed type=" + dynamicVM.UserDynamicType, LogType.INFO);
                    dynamicVM.Refresh();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log("Main dynamics stage=" + stage + " failed", LogType.ERROR, ex);
                throw;
            }
          
        }

        private void btnGrid_Click(object sender, RoutedEventArgs e)
        {
            SettingHelper.SetValue<int>(SettingHelper.UI.DYNAMIC_DISPLAY_MODE, 1);
            IsStaggered = true;
            btnGrid.Visibility = Visibility.Collapsed;
            btnList.Visibility = Visibility.Visible;
            //顶部
            gridTopBar.MaxWidth = double.MaxValue;
            gridTopBar.Margin = new Thickness(0,0,0,4);
            borderTopBar.CornerRadius = new CornerRadius(0);
            borderTopBar.Margin = new Thickness(0);
           
            //XAML
//            var tmp = @" <controls:StaggeredPanel DesiredColumnWidth='450' HorizontalAlignment='Stretch' ColumnSpacing='-12' RowSpacing='8' />";
//            var xaml = $@"<ItemsPanelTemplate 
//xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' 
//xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' 
//xmlns:controls='using:Microsoft.Toolkit.Uwp.UI.Controls'>
//                   {tmp}
//               </ItemsPanelTemplate>";
            //list.ItemsPanel = (ItemsPanelTemplate)XamlReader.Load(xaml);
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
            //顶部
            gridTopBar.MaxWidth = 800;
            gridTopBar.Margin = new Thickness(8, 0,8,0);
            borderTopBar.CornerRadius = new CornerRadius(4);
            borderTopBar.Margin = new Thickness(12,4,12,4);
            //XAML
//            var tmp = @" <ItemsStackPanel/>";
//            var xaml = $@"<ItemsPanelTemplate 
//xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' 
//xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
//                   {tmp}
//               </ItemsPanelTemplate>";
            //list.ItemsPanel = (ItemsPanelTemplate)XamlReader.Load(xaml);
            list.ItemsPanel = (ItemsPanelTemplate)this.Resources["ListPanel"];
        }

        private void btnTop_Click(object sender, RoutedEventArgs e)
        {
            list.ScrollIntoView(list.Items[0]);
        }

        private void pivotRight_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pivotRight.SelectedIndex == 0&&splitView.IsPaneOpen&& (repost.dynamicRepostVM.Items==null|| repost.dynamicRepostVM.Items.Count==0))
            {
                repost.LoadData(dynamic_id);
            }
        }

        //滚动页窗口（CARD_PLAN §9）的页面一侧：把当前可见的第一条动态记成锚点（只写 DynamicID，不持有卡片模型），
        //窗口淘汰页之后再把锚点项滚回顶端，用户看到的位置不变。
        private ScrollViewer listScrollViewer;
        private ObservableCollection<Controls.Dynamic.DynamicItemDisplayModel> subscribedItems;

        private void HookDynamicListAnchor()
        {
            list.Loaded += DynamicList_Loaded;
            dynamicVM.PropertyChanged += DynamicVM_PropertyChanged;
            SubscribeAnchorItems();
        }

        private void DynamicVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Items")
            {
                return;
            }
            //Items 整体换过（首屏/换源/刷新）：换成新的集合继续听
            SubscribeAnchorItems();
        }

        private void SubscribeAnchorItems()
        {
            if (subscribedItems != null)
            {
                subscribedItems.CollectionChanged -= DynamicVM_ItemsChanged;
                subscribedItems = null;
            }
            subscribedItems = dynamicVM.Items;
            if (subscribedItems != null)
            {
                subscribedItems.CollectionChanged += DynamicVM_ItemsChanged;
            }
        }

        private void DynamicList_Loaded(object sender, RoutedEventArgs e)
        {
            if (listScrollViewer != null)
            {
                return;
            }
            listScrollViewer = FindScrollViewer(list);
            if (listScrollViewer == null)
            {
                //模板还没应用：等一次布局再找
                list.LayoutUpdated += DynamicList_LayoutUpdated;
                return;
            }
            listScrollViewer.ViewChanged += ListScrollViewer_ViewChanged;
        }

        private void DynamicList_LayoutUpdated(object sender, object e)
        {
            listScrollViewer = FindScrollViewer(list);
            if (listScrollViewer == null)
            {
                return;
            }
            list.LayoutUpdated -= DynamicList_LayoutUpdated;
            listScrollViewer.ViewChanged += ListScrollViewer_ViewChanged;
        }

        private static ScrollViewer FindScrollViewer(DependencyObject root)
        {
            if (root == null)
            {
                return null;
            }
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var viewer = child as ScrollViewer;
                if (viewer != null)
                {
                    return viewer;
                }
                var nested = FindScrollViewer(child);
                if (nested != null)
                {
                    return nested;
                }
            }
            return null;
        }

        private void ListScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var panel = list.ItemsPanelRoot as ItemsStackPanel;
            if (panel == null)
            {
                //瀑布流面板没有 FirstVisibleIndex：锚点保持上一次的值
                return;
            }
            var items = dynamicVM.Items;
            var index = panel.FirstVisibleIndex;
            if (items == null || index < 0 || index >= items.Count)
            {
                return;
            }
            dynamicVM.AnchorDynamicId = items[index].DynamicID;
        }

        private void DynamicVM_ItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Remove)
            {
                return;
            }
            var anchorId = dynamicVM.AnchorDynamicId;
            if (string.IsNullOrEmpty(anchorId))
            {
                return;
            }
            var anchorItem = FindItemByDynamicId(anchorId);
            if (anchorItem == null)
            {
                //锚点项已经不在列表里：这时不动滚动位置，免得跳到别处
                return;
            }
            list.ScrollIntoView(anchorItem, ScrollIntoViewAlignment.Leading);
        }

        private Controls.Dynamic.DynamicItemDisplayModel FindItemByDynamicId(string dynamicId)
        {
            var items = dynamicVM.Items;
            if (items == null)
            {
                return null;
            }
            foreach (var item in items)
            {
                if (item != null && string.Equals(item.DynamicID, dynamicId, StringComparison.Ordinal))
                {
                    return item;
                }
            }
            return null;
        }
    }
}

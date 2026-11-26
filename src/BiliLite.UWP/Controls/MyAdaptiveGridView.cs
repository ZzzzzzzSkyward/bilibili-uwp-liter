using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Specialized;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace BiliLite.Controls
{
    public class MyAdaptiveGridView: AdaptiveGridView
    {

        private ICommand _LoadMoreCommand;
        public ICommand LoadMoreCommand
        {
            get { return _LoadMoreCommand; }
            set { _LoadMoreCommand = value; }
        }
        public bool CanLoadMore
        {
            get { return (bool)GetValue(CanLoadMoreProperty); }
            set { SetValue(CanLoadMoreProperty, value); }
        }

        public static readonly DependencyProperty CanLoadMoreProperty =
            DependencyProperty.Register("CanLoadMore", typeof(bool), typeof(MyAdaptiveGridView), new PropertyMetadata(false));

        public double LoadMoreBottomOffset
        {
            get { return Convert.ToDouble(GetValue(LoadMoreBottomOffsetProperty)); }
            set { SetValue(LoadMoreBottomOffsetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LoadMoreBottomOffset.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoadMoreBottomOffsetProperty =
            DependencyProperty.Register("LoadMoreBottomOffset", typeof(double), typeof(MyAdaptiveGridView), new PropertyMetadata(100));

        public new bool Loading
        {
            get { return (bool)GetValue(LoadingProperty); }
            set { SetValue(LoadingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Loading.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoadingProperty =
            DependencyProperty.Register("Loading", typeof(bool), typeof(MyAdaptiveGridView), new PropertyMetadata(true));

        public MyAdaptiveGridView()
        {
            RegisterPropertyChangedCallback(LoadingProperty, Loading_Changed);
            RegisterPropertyChangedCallback(ItemsSourceProperty, ItemsSource_Changed);
        }

        ScrollViewer scrollViewer;
        readonly AutoLoadMoreGate autoFillGate = new AutoLoadMoreGate();
        INotifyCollectionChanged observedItems;

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (scrollViewer != null)
            {
                scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
            }
            scrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
            }
            ObserveItemsSource();
        }

        private void ItemsSource_Changed(DependencyObject sender, DependencyProperty dp)
        {
            ObserveItemsSource();
        }

        /// <summary>
        /// 内容长度变了才重新给自动翻页上膛；ItemsSource 换了（刷新会把集合整个换掉）要重新挂监听。
        /// </summary>
        private void ObserveItemsSource()
        {
            if (observedItems != null)
            {
                observedItems.CollectionChanged -= ItemsSource_CollectionChanged;
                observedItems = null;
            }
            observedItems = ItemsSource as INotifyCollectionChanged;
            if (observedItems != null)
            {
                observedItems.CollectionChanged += ItemsSource_CollectionChanged;
            }
            autoFillGate.OnContentChanged();
        }

        private void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            autoFillGate.OnContentChanged();
        }

        private void Loading_Changed(DependencyObject sender, DependencyProperty dp)
        {
            if (Loading)
            {
                return;
            }
            if (autoFillGate.TryTake(CanAutoFill()))
            {
                LoadMoreCommand?.Execute(null);
            }
        }

        /// <summary>
        /// 自动补内容的前提。页面被切走（缓存页不在可视树里）时 ScrollableHeight 会停在 0，
        /// 只看 ScrollableHeight 会一直判定「没填满」，把它当条件之前必须先确认控件真的在树上。
        /// </summary>
        private bool CanAutoFill()
        {
            return LoadMoreCommand != null
                && CanLoadMore
                && IsLoaded
                && ActualHeight > 0
                && scrollViewer != null
                && scrollViewer.ScrollableHeight <= 0;
        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            // 空列表时 VerticalOffset 恒为 0、ScrollableHeight 也是 0，会被误判成「已经滚到底」，
            // 这里必须排除没有可滚动内容的情况，只让用户真的往下滚来触发翻页。
            // 不看 Loading：只有 4 个页面绑了 Loading，其余页面的 Loading 一直停在默认值 true，
            // 用它会把这些页面的滚动翻页整条堵死。
            if (LoadMoreCommand == null || !CanLoadMore || scrollViewer == null || scrollViewer.ScrollableHeight <= 0)
            {
                return;
            }
            if (scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight - LoadMoreBottomOffset)
            {
                return;
            }
            LoadMoreCommand.Execute(null);
        }
    }
}

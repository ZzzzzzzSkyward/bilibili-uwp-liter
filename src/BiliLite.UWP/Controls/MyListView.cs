using System;
using System.Windows.Input;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace BiliLite.Controls
{
    public class MyListView : ListView
    {

        public MyListView()
        {
            Loaded += MyListView_Loaded;
            SizeChanged += MyListView_SizeChanged;
            RegisterPropertyChangedCallback(ItemsControl.ItemsSourceProperty, MyListView_ItemsSourceChanged);
        }

        private ICommand _LoadMoreCommand;
        public ICommand LoadMoreCommand
        {
            get { return _LoadMoreCommand; }
            set { _LoadMoreCommand = value; }
        }
        private bool _CanLoadMore = false;
        public bool CanLoadMore
        {
            get { return _CanLoadMore; }
            set
            {
                if (_CanLoadMore == value)
                {
                    return;
                }
                _CanLoadMore = value;
                if (value)
                {
                    // false->true 是一个"请求页已完成"信号，必定要有新的一轮短视口检查：
                    // 服务端页可能被映射器过滤成 0 张可见卡片（广告/横幅），此时没有任何 item 或
                    // ItemsSource 变化，光靠 _itemsVersion 永远等不到下一次触发，分页就停在空页上。
                    // 这里把完成的页也算作一次内容版本变化，使已消费过的版本不再挡住这次检查。
                    _itemsVersion++;
                    QueueShortViewportCheck();
                }
            }
        }
        public double LoadMoreBottomOffset
        {
            get { return Convert.ToDouble(GetValue(LoadMoreBottomOffsetProperty)); }
            set { SetValue(LoadMoreBottomOffsetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LoadMoreBottomOffset.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoadMoreBottomOffsetProperty =
            DependencyProperty.Register("LoadMoreBottomOffset", typeof(double), typeof(MyListView), new PropertyMetadata(100d));



        ScrollViewer scrollViewer;

        // A short-viewport load-more may run once per content version. Any item or ItemsSource
        // change bumps the version, and so does a completed request page (CanLoadMore false->true),
        // because a page can publish zero visible cards while still advancing the server cursor.
        // CanLoadMore is set false synchronously at request start and only restored when a page is
        // published, so the rising edge can never happen while a request is still in flight: the
        // rising edge is therefore exactly one completed page, and the version bump it causes
        // re-arms one trigger for that page without allowing a second one.
        private int _itemsVersion;
        private int _triggeredItemsVersion = -1;
        private bool _checkQueued;
        private bool _itemsHooked;
        private bool _layoutHookActive;

        protected override void OnApplyTemplate()
        {
            if (scrollViewer != null)
            {
                scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
            }
            scrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
            }
            HookItems();
            QueueShortViewportCheck();
            base.OnApplyTemplate();
        }

        private void MyListView_Loaded(object sender, RoutedEventArgs e)
        {
            HookItems();
            QueueShortViewportCheck();
        }

        private void MyListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            QueueShortViewportCheck();
        }

        private void HookItems()
        {
            if (_itemsHooked)
            {
                return;
            }
            var items = Items;
            if (items == null)
            {
                return;
            }
            items.VectorChanged += Items_VectorChanged;
            _itemsHooked = true;
        }

        private void Items_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs e)
        {
            _itemsVersion++;
            QueueShortViewportCheck();
        }

        private void MyListView_ItemsSourceChanged(DependencyObject sender, DependencyProperty dp)
        {
            // Refresh replaces the whole collection instead of mutating it.
            _itemsVersion++;
            QueueShortViewportCheck();
        }

        // Queues one check after the current dispatcher work, so the layout pass triggered by the
        // item change has produced real ScrollViewer measurements when the check runs.
        // The Low priority also gives the page-completion signal its required ordering: the VM
        // publishes a page and clears Loading in the same synchronous continuation (its finally
        // block), and a queued Low callback cannot preempt that continuation, so by the time the
        // check executes Loading is already false and LoadMore no longer returns early.
        private void QueueShortViewportCheck()
        {
            if (_checkQueued)
            {
                return;
            }
            var dispatcher = Dispatcher;
            if (dispatcher == null)
            {
                RunShortViewportCheck(false);
                return;
            }
            _checkQueued = true;
            dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                _checkQueued = false;
                RunShortViewportCheck(true);
            });
        }

        private void RunShortViewportCheck(bool allowLayoutFallback)
        {
            if (scrollViewer == null || LoadMoreCommand == null)
            {
                return;
            }
            // False while a request is in flight, after a failed page, when the server ended pagination,
            // when the cursor stopped advancing and when the consecutive-empty-page cap was hit.
            if (!CanLoadMore)
            {
                return;
            }
            if (scrollViewer.ActualHeight <= 0)
            {
                // Not measured yet (collapsed or pre-layout): scroll metrics are meaningless.
                if (allowLayoutFallback)
                {
                    HookLayoutUpdatedOnce();
                }
                return;
            }
            if (scrollViewer.ScrollableHeight > 0)
            {
                // Content overflows the viewport; ScrollViewer_ViewChanged owns load-more.
                return;
            }
            if (_triggeredItemsVersion == _itemsVersion)
            {
                // Already triggered for this content, and still nothing to scroll.
                return;
            }
            _triggeredItemsVersion = _itemsVersion;
            LoadMoreCommand.Execute(null);
        }

        private void HookLayoutUpdatedOnce()
        {
            if (_layoutHookActive)
            {
                return;
            }
            _layoutHookActive = true;
            LayoutUpdated += MyListView_LayoutUpdated;
        }

        private void MyListView_LayoutUpdated(object sender, object e)
        {
            LayoutUpdated -= MyListView_LayoutUpdated;
            _layoutHookActive = false;
            // No re-hook here: SizeChanged/Loaded queue again once the element gets a real size.
            RunShortViewportCheck(false);
        }


        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - LoadMoreBottomOffset && CanLoadMore)
            {
                LoadMoreCommand?.Execute(null);
            }

        }
    }
}

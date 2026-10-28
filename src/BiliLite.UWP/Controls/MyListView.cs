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
        // change bumps the version, so another trigger needs new content.
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
            // False while a request is in flight, after a failed page and when the server ended pagination.
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

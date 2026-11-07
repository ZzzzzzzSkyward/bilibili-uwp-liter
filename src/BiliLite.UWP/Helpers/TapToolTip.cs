using System;
using System.Runtime.CompilerServices;
using Windows.Devices.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace BiliLite.Helpers
{
    /// <summary>
    /// 点击打开 tooltip：ToolTipService 只在指针悬停时自动打开，触摸/手柄没有 hover，
    /// 设备上就看不到被截断的标题全文。
    /// 悬停那一份保持原样（XAML 里的字符串照旧绑定，虚拟化复用时绑定还能更新）；
    /// 点击用每个元素自己的一份 ToolTip，每次点击都读当前文字再打开，回收复用的标题不会显示上一条。
    /// PointerExited 只在鼠标上关闭 —— 触摸抬手的 PointerExited 常常晚于 Tapped，会把刚打开的提示立刻关掉；
    /// 触摸/笔靠 4 秒超时与 Unloaded 收尾。
    /// </summary>
    public static class TapToolTip
    {
        private static readonly TimeSpan AutoCloseDelay = TimeSpan.FromSeconds(4);

        // 元素到点击用 ToolTip 的映射：ToolTipService.ToolTip 的值不能改写（改写会顶掉绑定/复用时的新值）
        private static readonly ConditionalWeakTable<FrameworkElement, ToolTip> TapTooltips =
            new ConditionalWeakTable<FrameworkElement, ToolTip>();

        // 每个 ToolTip 一个计时器（DispatcherTimer 没有 Tag，不能把两者互挂）
        private static readonly ConditionalWeakTable<ToolTip, DispatcherTimer> AutoCloseTimers =
            new ConditionalWeakTable<ToolTip, DispatcherTimer>();

        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached(
                "Enabled",
                typeof(bool),
                typeof(TapToolTip),
                new PropertyMetadata(false, OnEnabledChanged));

        public static bool GetEnabled(DependencyObject element)
        {
            return (bool)element.GetValue(EnabledProperty);
        }

        public static void SetEnabled(DependencyObject element, bool value)
        {
            element.SetValue(EnabledProperty, value);
        }

        private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if (element == null)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                element.Tapped += OnTapped;
                element.PointerExited += OnPointerExited;
                element.Unloaded += OnUnloaded;
            }
            else
            {
                element.Tapped -= OnTapped;
                element.PointerExited -= OnPointerExited;
                element.Unloaded -= OnUnloaded;
                CloseFor(element);
            }
        }

        private static void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var tooltip = Resolve(element);
            if (tooltip == null)
            {
                return;
            }

            // 连点时重启同一个计时器，而不是再起一个：ToolTip 只有一个 IsOpen
            var timer = TimerFor(tooltip);
            timer.Stop();
            tooltip.IsOpen = true;
            timer.Start();
        }

        private static void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            // 触摸抬手的 PointerExited 会把刚打开的提示立刻关掉，只有鼠标离开才是"不想看"
            if (e.Pointer.PointerDeviceType != PointerDeviceType.Mouse)
            {
                return;
            }
            CloseFor(sender as FrameworkElement);
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CloseFor(sender as FrameworkElement);
        }

        /// <summary>
        /// XAML 给的是 ToolTip 就用它；给的是字符串就把它此刻的值写进元素自己的点击专用 ToolTip。
        /// 这里绝不回写 ToolTipService.ToolTip：列表回收复用时绑定还要靠它继续更新。
        /// </summary>
        private static ToolTip Resolve(FrameworkElement element)
        {
            if (element == null)
            {
                return null;
            }

            var current = ToolTipService.GetToolTip(element);
            var tooltip = current as ToolTip;
            if (tooltip != null)
            {
                return tooltip;
            }

            var text = current as string;
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            tooltip = TapTooltips.GetValue(element, CreateTapTooltip);
            tooltip.Content = text;
            return tooltip;
        }

        private static ToolTip CreateTapTooltip(FrameworkElement element)
        {
            return new ToolTip { PlacementTarget = element };
        }

        private static DispatcherTimer TimerFor(ToolTip tooltip)
        {
            DispatcherTimer timer;
            if (AutoCloseTimers.TryGetValue(tooltip, out timer))
            {
                return timer;
            }

            timer = new DispatcherTimer { Interval = AutoCloseDelay };
            timer.Tick += (sender, e) => Close(tooltip);
            AutoCloseTimers.Add(tooltip, timer);
            return timer;
        }

        private static void CloseFor(FrameworkElement element)
        {
            if (element == null)
            {
                return;
            }

            ToolTip tap;
            if (TapTooltips.TryGetValue(element, out tap))
            {
                Close(tap);
            }

            var attached = ToolTipService.GetToolTip(element) as ToolTip;
            if (attached != null)
            {
                Close(attached);
            }
        }

        private static void Close(ToolTip tooltip)
        {
            DispatcherTimer timer;
            if (AutoCloseTimers.TryGetValue(tooltip, out timer))
            {
                timer.Stop();
            }
            tooltip.IsOpen = false;
        }
    }
}

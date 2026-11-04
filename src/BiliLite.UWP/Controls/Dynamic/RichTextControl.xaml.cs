using System;
using BiliLite.Helpers;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace BiliLite.Controls.Dynamic
{
    /// <summary>
    /// 正文宿主：一条正文只有一个宿主元素，行数上限只是视觉策略。
    /// 1. 挂载结果由当前 Content/Text 全量决定（RichTextBodyState），空正文收起整个正文区域。
    /// 2. 折叠用 MaxLines，正文本身不被截断：完整纯文本随块带走，选中/复制/读屏仍是整条正文。
    /// 3. "展开/收起"只在实测内容超出行数上限时出现；展开态按动态 id 记忆。
    /// 4. 复用到另一张卡片时先把上一条正文移出宿主，再挂新正文；一个块只会有一个父元素，
    ///    旧块的 inlines 与超链接处理器随它的卡片一起离开可视树。
    /// </summary>
    public sealed partial class RichTextControl : UserControl
    {
        public RichTextControl()
        {
            this.InitializeComponent();
            this.Loaded += OnBodyLoaded;
            this.SizeChanged += OnBodySizeChanged;
            this.DataContextChanged += OnBodyDataContextChanged;
        }

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(RichTextBlock), typeof(RichTextControl), new PropertyMetadata(null, OnContentPropertyChanged));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(RichTextControl), new PropertyMetadata(string.Empty, OnTextPropertyChanged));

        /// <summary>正文所在位置；默认信息流，详情页把它设成 Detail 即不设上限并默认展开。</summary>
        public static readonly DependencyProperty BodyRoleProperty =
            DependencyProperty.Register("BodyRole", typeof(RichTextBodyRole), typeof(RichTextControl), new PropertyMetadata(RichTextBodyRole.Feed, OnBodyRolePropertyChanged));

        /// <summary>展开态的键（动态 id）；留空时取 DataContext 里的卡片 id。</summary>
        public static readonly DependencyProperty BodyIdProperty =
            DependencyProperty.Register("BodyId", typeof(string), typeof(RichTextControl), new PropertyMetadata(string.Empty, OnBodyIdPropertyChanged));

        public RichTextBlock Content
        {
            get { return (RichTextBlock)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public RichTextBodyRole BodyRole
        {
            get { return (RichTextBodyRole)GetValue(BodyRoleProperty); }
            set { SetValue(BodyRoleProperty, value); }
        }

        public string BodyId
        {
            get { return (string)GetValue(BodyIdProperty); }
            set { SetValue(BodyIdProperty, value); }
        }

        // 静态属性更改回调
        // 两个属性谁后到都只是一次"重算"：挂载结果永远由当前属性全量决定，与到达顺序无关。
        private static void OnContentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RichTextControl)d).RefreshBody();
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RichTextControl)d).RefreshBody();
        }

        private static void OnBodyRolePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RichTextControl)d).RefreshBody();
        }

        private static void OnBodyIdPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RichTextControl)d).RefreshBody();
        }

        // 挂载状态只描述 BodyHost 里现在是什么，和控件一起复用
        private readonly RichTextBodyState _bodyState = new RichTextBodyState();

        private RichTextBodyRole _role = RichTextBodyRole.Feed;

        private bool _applying;

        private bool _loaded;

        private void OnBodyLoaded(object sender, RoutedEventArgs e)
        {
            _loaded = true;
            RefreshBody();
        }

        private void OnBodySizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 宽度变了折行就变，行数上限的判定要重算（可见性变化时不再触发，靠 _applying 挡住）
            if (_loaded && e.NewSize.Width != e.PreviousSize.Width)
            {
                ApplyLineCap();
            }
        }

        private void OnBodyDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // 复用到另一张卡片时数据上下文先于正文属性变化：换身份意味着展开态与旧挂载都要复位
            RefreshBody();
        }

        /// <summary>
        /// 从当前 Content/Text 重算并重挂正文。空 RichTextBlock 与 null 一样按"没有富文本"处理；
        /// 结果为空时这里就是复位点：先清空 BodyHost，再把整个正文区域收起，绝不留下上一条卡片的正文。
        /// </summary>
        private void RefreshBody()
        {
            _role = RichTextBodyPolicy.ResolveRole(DeclaredRole(), CardDepth());
            var identityChanged = _bodyState.BeginIdentity(ResolveBodyId(), _role);

            var richContent = Content != null && Content.Blocks.Count > 0 ? Content : null;
            var plan = _bodyState.Apply(richContent, Text);

            if (!RichTextBodyPolicy.NeedsClear(identityChanged, plan.Changed))
            {
                ApplyLineCap();
                return;
            }

            Clear();
            if (plan.Kind == RichTextBodyKind.RichText)
            {
                Add((RichTextBlock)richContent);
            }
            else if (plan.Kind == RichTextBodyKind.PlainText)
            {
                Add(new TextBlock { Text = plan.Text });
            }

            // 空正文收起整个区域：不占行的间距，也不写"无内容"
            this.Visibility = RichTextBodyPolicy.HidesRegion(plan.Kind) ? Visibility.Collapsed : Visibility.Visible;
            SetBodyStyle();
            ApplyLineCap();
        }

        private RichTextBodyRole DeclaredRole()
        {
            // 共享模板里正文位置由承载的卡片外壳决定（详情页用隐式 Style 设 Detail），自身声明只是兜底
            var element = VisualTreeHelper.GetParent(this);
            while (element != null)
            {
                var card = element as DynamicItemControl;
                if (card != null)
                {
                    return card.BodyRole;
                }
                element = VisualTreeHelper.GetParent(element);
            }
            return BodyRole;
        }

        private string ResolveBodyId()
        {
            if (!string.IsNullOrEmpty(BodyId))
            {
                return BodyId;
            }
            var model = DataContext as DynamicItemDisplayModel;
            return model?.DynamicID ?? string.Empty;
        }

        /// <summary>卡片嵌套层级：转发原动态自己的正文是第二层卡片里的正文（四行上限）。</summary>
        private int CardDepth()
        {
            var depth = 0;
            var element = VisualTreeHelper.GetParent(this);
            while (element != null)
            {
                if (element is DynamicItemControl)
                {
                    depth++;
                }
                element = VisualTreeHelper.GetParent(element);
            }
            return depth > 0 ? depth : 1;
        }

        /// <summary>
        /// 清空正文宿主。上一条卡片的块连同它的 inlines 与超链接处理器一起离开这棵可视树：
        /// 一个块只属于它那张卡片的数据模型（Add 里有单父保护），不会被另一张卡片重新挂上来，
        /// 也就不会带着旧身份的处理器出现在新身份的卡片里。
        /// 这里不能清块内的 Blocks——模型缓存着这个块，同一条动态再滚回来时还要用它重画。
        /// </summary>
        private void Clear()
        {
            BodyHost.Children.Clear();
            HideToggle();
        }

        private void Add(TextBlock r)
        {
            BodyHost.Children.Add(r);
        }

        private void Add(RichTextBlock r)
        {
            if (r == null)
            {
                return;
            }
            // 一个 RichTextBlock 只能有一个父元素：已经被别处挂走的块不再抢
            if (r.Parent != null && !ReferenceEquals(r.Parent, BodyHost))
            {
                return;
            }
            if (BodyHost.Children.Contains(r))
            {
                return;
            }
            BodyHost.Children.Add(r);
        }

        /// <summary>
        /// 行数上限：只有内容实测超上限时才折叠并露出切换，展开态按动态 id 记忆。
        /// 判定前先放开上限量一次完整高度，量完立刻恢复到当前折叠态。
        /// </summary>
        private void ApplyLineCap()
        {
            if (_applying)
            {
                return;
            }

            var block = BodyHost.Children.Count > 0 ? BodyHost.Children[0] as RichTextBlock : null;
            var text = BodyHost.Children.Count > 0 ? BodyHost.Children[0] as TextBlock : null;
            var cap = RichTextBodyPolicy.LineCap(_role);
            if (block == null && text == null)
            {
                HideToggle();
                return;
            }

            var width = block != null ? block.ActualWidth : text.ActualWidth;
            if (width <= 0)
            {
                width = ActualWidth;
            }
            if (width <= 0)
            {
                return; // 还没有可用的宽度，等下一次布局再判
            }

            _applying = true;
            try
            {
                double desiredHeight;
                double lineHeight;
                if (block != null)
                {
                    lineHeight = Math.Max(block.LineHeight, BodyLineHeight);
                    block.MaxLines = RichTextBodyPolicy.UnlimitedLines;
                    block.Measure(new Size(width, double.PositiveInfinity));
                    desiredHeight = block.DesiredSize.Height;
                }
                else
                {
                    lineHeight = Math.Max(text.LineHeight, BodyLineHeight);
                    text.MaxLines = 0;
                    text.Measure(new Size(width, double.PositiveInfinity));
                    desiredHeight = text.DesiredSize.Height;
                }

                var overflow = RichTextBodyPolicy.Overflows(
                    RichTextBodyPolicy.RenderedLines(desiredHeight, lineHeight), cap);
                var clamped = overflow && !_bodyState.IsExpanded ? cap : RichTextBodyPolicy.UnlimitedLines;
                if (block != null)
                {
                    block.MaxLines = clamped;
                }
                else
                {
                    text.MaxLines = clamped;
                }

                if (overflow)
                {
                    ShowToggle(!_bodyState.IsExpanded);
                }
                else
                {
                    HideToggle();
                }
            }
            finally
            {
                _applying = false;
            }
        }

        private void ShowToggle(bool collapsed)
        {
            ExpandToggle.Content = collapsed ? "展开" : "收起";
            ExpandToggle.Visibility = Visibility.Visible;
        }

        private void HideToggle()
        {
            ExpandToggle.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 展开/收起会让正文高度变化。先记下正文锚点在滚动内容里的位置，改完布局再按位移补偿滚动偏移，
        /// 这样卡片下方的正文挪动时阅读位置不跳。
        /// </summary>
        private void OnExpandToggleClick(object sender, RoutedEventArgs e)
        {
            var scroll = FindAncestorScrollViewer();
            var anchorBefore = AnchorY(scroll);
            var offsetBefore = scroll == null ? 0 : scroll.VerticalOffset;

            _bodyState.SetExpanded(!_bodyState.IsExpanded);
            ApplyLineCap();
            UpdateLayout();

            if (scroll == null)
            {
                return;
            }
            var delta = RichTextBodyPolicy.ScrollCompensation(anchorBefore, AnchorY(scroll));
            if (Math.Abs(delta) < 0.5)
            {
                return;
            }
            scroll.ChangeView(null, RichTextBodyPolicy.CompensatedOffset(offsetBefore, delta), null, true);
        }

        private double AnchorY(ScrollViewer scroll)
        {
            if (scroll == null)
            {
                return 0;
            }
            var content = scroll.Content as UIElement;
            if (content == null)
            {
                return 0;
            }
            try
            {
                return TransformToVisual(content).TransformPoint(new Point(0, 0)).Y;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private ScrollViewer FindAncestorScrollViewer()
        {
            DependencyObject element = this;
            while (element != null)
            {
                var scroll = element as ScrollViewer;
                if (scroll != null)
                {
                    return scroll;
                }
                element = VisualTreeHelper.GetParent(element);
            }
            return null;
        }

        /// <summary>
        /// 正文度量,对应 DynamicItemControl.xaml 顶部令牌表的“正文/正文行高”两行(计划 §5)。
        /// 行高用 MaxHeight 策略:24 是最小值而不是固定盒高,放大字号或表情时行会长高而不是被切。
        /// 阅读列宽由卡片外壳负责,正文不再自带 800 的宽度假设。
        /// </summary>
        public const double BodyFontSize = 16;

        public const double BodyLineHeight = 24;

        private void SetBodyStyle()
        {
            // 正文样式对两种宿主同样生效：复用后新挂上的元素也要拿到同一套度量
            foreach (var child in BodyHost.Children)
            {
                var rich = child as RichTextBlock;
                if (rich != null)
                {
                    rich.TextLineBounds = TextLineBounds.Full;
                    rich.TextAlignment = TextAlignment.Left;
                    rich.FontSize = BodyFontSize;
                    rich.LineHeight = Math.Max(rich.LineHeight, BodyLineHeight);
                    rich.LineStackingStrategy = LineStackingStrategy.MaxHeight;
                    rich.Visibility = Visibility.Visible;
                    rich.Margin = new Thickness(0);
                    rich.Foreground = Utils.GetBrush("ForegroundTextColor");
                    rich.IsTextSelectionEnabled = true;
                    rich.TextWrapping = TextWrapping.Wrap;
                    rich.HorizontalAlignment = HorizontalAlignment.Left;
                    rich.HorizontalTextAlignment = TextAlignment.Left;
                    continue;
                }

                var plain = child as TextBlock;
                if (plain == null)
                {
                    continue;
                }
                plain.TextLineBounds = TextLineBounds.Full;
                plain.TextAlignment = TextAlignment.Left;
                plain.FontSize = BodyFontSize;
                plain.LineHeight = Math.Max(plain.LineHeight, BodyLineHeight);
                plain.LineStackingStrategy = LineStackingStrategy.MaxHeight;
                plain.Visibility = Visibility.Visible;
                plain.Margin = new Thickness(0);
                plain.Foreground = Utils.GetBrush("ForegroundTextColor");
                plain.IsTextSelectionEnabled = true;
                plain.TextWrapping = TextWrapping.Wrap;
                plain.HorizontalAlignment = HorizontalAlignment.Left;
                plain.HorizontalTextAlignment = TextAlignment.Left;
            }
        }
    }
}

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace BiliLite.Controls.Dynamic
{
    public sealed partial class DynamicItemControl : UserControl
    {
        /// <summary>
        /// 响应式断点(计划 §5):按卡片自身的可用宽度判断,而不是窗口宽度,侧栏占位也算在可用宽度里。
        /// </summary>
        private const double NarrowWidthThreshold = 480;

        /// <summary>
        /// 宽态留白就是 DynamicItemControl.xaml 令牌表里的字面量;窄态值集中写在这里,设备联调时和令牌表一起改。
        /// </summary>
        private static readonly Thickness WideCardMargin = new Thickness(12, 0, 12, 4);
        private static readonly Thickness NarrowCardMargin = new Thickness(8, 0, 8, 4);
        private static readonly Thickness WideCardPadding = new Thickness(16, 8, 16, 8);
        private static readonly Thickness NarrowCardPadding = new Thickness(12, 8, 12, 8);

        private bool? _isNarrow;

        public DynamicItemControl()
        {
            this.InitializeComponent();
            this.SizeChanged += OnCardSizeChanged;
        }

        /// <summary>
        /// 卡片切宽/窄两态:窄态收紧留白,把装扮和头部操作收进一个溢出入口;正文与媒体在任何宽度下都占满整宽。
        /// 只在真正跨过断点时赋值,滚动/复用时不重复写属性。
        /// </summary>
        private void ApplyWidthState(double availableWidth)
        {
            var narrow = availableWidth < NarrowWidthThreshold;
            if (_isNarrow == narrow)
            {
                return;
            }
            _isNarrow = narrow;
            CardRoot.Margin = narrow ? NarrowCardMargin : WideCardMargin;
            CardSurface.Padding = narrow ? NarrowCardPadding : WideCardPadding;
            DecorationSlot.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;
            HeaderActions.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;
            HeaderOverflow.Visibility = narrow ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnCardSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyWidthState(e.NewSize.Width);
        }

        public DynamicItemDisplayModel Model
        {
            get { return (DynamicItemDisplayModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(DynamicItemDisplayModel), typeof(DynamicItemControl), new PropertyMetadata(new DynamicItemDisplayModel()));



        public FrameworkElement CardContent
        {
            get { return (FrameworkElement)GetValue(CardContentProperty); }
            set { SetValue(CardContentProperty, value); }
        }

        public static readonly DependencyProperty CardContentProperty =
            DependencyProperty.Register("CardContent", typeof(FrameworkElement), typeof(DynamicItemControl), new PropertyMetadata(null));

        /// <summary>
        /// 正文位置（CARD_PLAN §4 展开策略）：模板是共享的，所以由承载它的页面决定——
        /// 详情页在自己的 Resources 里用隐式 Style 把它设成 Detail，正文就不再设行数上限并默认展开。
        /// 转发原动态的内层卡片仍由嵌套层级识别，无需页面干预。
        /// </summary>
        public RichTextBodyRole BodyRole
        {
            get { return (RichTextBodyRole)GetValue(BodyRoleProperty); }
            set { SetValue(BodyRoleProperty, value); }
        }

        public static readonly DependencyProperty BodyRoleProperty =
            DependencyProperty.Register("BodyRole", typeof(RichTextBodyRole), typeof(DynamicItemControl), new PropertyMetadata(RichTextBodyRole.Feed));

    }
}

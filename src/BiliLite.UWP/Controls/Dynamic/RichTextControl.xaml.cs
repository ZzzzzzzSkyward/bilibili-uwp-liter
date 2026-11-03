using BiliLite.Helpers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace BiliLite.Controls.Dynamic
{
    public sealed partial class RichTextControl : UserControl
    {
        public RichTextControl()
        {
            this.InitializeComponent();
        }
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(RichTextBlock), typeof(RichTextControl), new PropertyMetadata(null, OnContentPropertyChanged));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(RichTextControl), new PropertyMetadata(string.Empty, OnTextPropertyChanged));

        // 公共属性
        public RichTextBlock Rich = new RichTextBlock();
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

        // 挂载状态只描述 Parent.Children 里现在是什么，和控件一起复用
        private readonly RichTextBodyState _bodyState = new RichTextBodyState();

        /// <summary>
        /// 从当前 Content/Text 重算并重挂正文。空 RichTextBlock 与 null 一样按"没有富文本"处理；
        /// 结果为空时这里就是复位点：先清空 Parent，再什么都不挂，绝不留下上一条卡片的正文。
        /// </summary>
        private void RefreshBody()
        {
            var richContent = Content != null && Content.Blocks.Count > 0 ? Content : null;
            var plan = _bodyState.Apply(richContent, Text);
            if (!plan.Changed)
            {
                return;
            }

            Clear();
            if (plan.Kind == RichTextBodyKind.RichText)
            {
                Add((RichTextBlock)richContent);
                SetRichTextBlockStyle();
            }
            else if (plan.Kind == RichTextBodyKind.PlainText)
            {
                Add(new TextBlock { Text = plan.Text });
                SetRichTextBlockStyle();
            }
        }

        private void Clear()
        {
            Parent.Children.Clear();
        }

        private void Add(TextBlock r)
        {
            Parent.Children.Add(r);
        }

        private void Add(RichTextBlock r)
        {
            Parent.Children.Add(r);
        }

        /// <summary>
        /// 正文度量,对应 DynamicItemControl.xaml 顶部令牌表的“正文/正文行高”两行(计划 §5)。
        /// 行高用 MaxHeight 策略:24 是最小值而不是固定盒高,放大字号或表情时行会长高而不是被切。
        /// 阅读列宽由卡片外壳负责,正文不再自带 800 的宽度假设。
        /// </summary>
        public const double BodyFontSize = 16;

        public const double BodyLineHeight = 24;

        private void SetRichTextBlockStyle()
        {
            // 遍历 Grid 中的所有子元素
            foreach (var child in Parent.Children)
            {
                // 如果子元素是 RichTextBlock，则应用预设样式
                if (child is RichTextBlock tb)
                {
                    tb.TextLineBounds = TextLineBounds.Full;
                    tb.TextAlignment = TextAlignment.Left;
                    tb.FontSize = BodyFontSize;
                    tb.LineHeight = BodyLineHeight;
                    tb.LineStackingStrategy = LineStackingStrategy.MaxHeight;
                    tb.Visibility = Visibility.Visible;
                    tb.Margin = new Thickness(0);
                    tb.Foreground = Utils.GetBrush("ForegroundTextColor");
                    tb.IsTextSelectionEnabled = true;
                    tb.TextWrapping = TextWrapping.Wrap;
                    tb.HorizontalAlignment = HorizontalAlignment.Left;
                    tb.HorizontalTextAlignment = TextAlignment.Left;
                }
                else if (child is TextBlock tb2)
                {
                    tb2.TextLineBounds = TextLineBounds.Full;
                    tb2.TextAlignment = TextAlignment.Left;
                    tb2.FontSize = BodyFontSize;
                    tb2.LineHeight = BodyLineHeight;
                    tb2.LineStackingStrategy = LineStackingStrategy.MaxHeight;
                    tb2.Visibility = Visibility.Visible;
                    tb2.Margin = new Thickness(0);
                    tb2.Foreground = Utils.GetBrush("ForegroundTextColor");
                    tb2.IsTextSelectionEnabled = true;
                    tb2.TextWrapping = TextWrapping.Wrap;
                    tb2.HorizontalAlignment = HorizontalAlignment.Left;
                    tb2.HorizontalTextAlignment = TextAlignment.Left;
                }
            }
        }
    }
}
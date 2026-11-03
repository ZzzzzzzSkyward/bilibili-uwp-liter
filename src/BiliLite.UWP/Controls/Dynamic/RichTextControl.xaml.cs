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
        private static void OnContentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (RichTextControl)d;
            control.UpdateContent((RichTextBlock)e.NewValue);
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (RichTextControl)d;
            control.UpdateText((string)e.NewValue);
        }
        public bool rich = false;
        private void UpdateContent(RichTextBlock r)
        {
            rich = false;
            if (Content != null && Content.Blocks.Count > 0)
            {
                rich = true;

                // 移除所有现有的 RichTextBlock
                Clear();

                // 添加新的 RichTextBlock
                Add(r);
                SetRichTextBlockStyle();
            }
        }
        private void UpdateText(string text)
        {
            // 优先使用富文本内容
            if (rich) return;
            Clear();
            // 没有正文就留空：纯图片/纯视频卡片不该被塞进一句"无内容"
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            // 如果没有富文本内容，则使用纯文本，并套同一套正文样式
            Add(new TextBlock { Text = text });
            SetRichTextBlockStyle();
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
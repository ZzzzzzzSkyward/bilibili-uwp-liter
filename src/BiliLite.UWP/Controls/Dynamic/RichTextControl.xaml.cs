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
            if (!string.IsNullOrEmpty(text))
            {
                // 如果没有富文本内容，则使用纯文本
                Add(new TextBlock { Text = text });
                return;
            }
            Add(new TextBlock { Text = "无内容" });
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
        private void SetRichTextBlockStyle()
        {
            // 遍历 Grid 中的所有子元素
            foreach (var child in Parent.Children)
            {
                // 如果子元素是 RichTextBlock，则应用预设样式
                if (child is RichTextBlock tb)
                {
                    tb.MaxWidth = 800;
                    tb.MaxHeight = 10000;
                    tb.TextLineBounds = TextLineBounds.Full;
                    tb.TextAlignment = TextAlignment.Left;
                    tb.MinWidth = 100;
                    tb.MinHeight = 12;
                    tb.FontSize = 14;
                    tb.Visibility = Visibility.Visible;
                    tb.Margin = new Thickness(0);
                    tb.Foreground = Utils.GetBrush("ForegroundTextColor");
                    tb.IsTextSelectionEnabled = true;
                    tb.TextWrapping = TextWrapping.Wrap;
                    tb.TextTrimming = TextTrimming.CharacterEllipsis;
                    tb.HorizontalAlignment = HorizontalAlignment.Left;
                    tb.HorizontalTextAlignment = TextAlignment.Left;
                }
                else if (child is TextBlock tb2)
                {
                    tb2.MaxWidth = 800;
                    tb2.MaxHeight = 10000;
                    tb2.TextLineBounds = TextLineBounds.Full;
                    tb2.TextAlignment = TextAlignment.Left;
                    tb2.MinWidth = 100;
                    tb2.MinHeight = 12;
                    tb2.FontSize = 14;
                    tb2.Visibility = Visibility.Visible;
                    tb2.Margin = new Thickness(0);
                    tb2.Foreground = Utils.GetBrush("ForegroundTextColor");
                    tb2.IsTextSelectionEnabled = true;
                    tb2.TextWrapping = TextWrapping.Wrap;
                    tb2.TextTrimming = TextTrimming.CharacterEllipsis;
                    tb2.HorizontalAlignment = HorizontalAlignment.Left;
                    tb2.HorizontalTextAlignment = TextAlignment.Left;
                }
            }
        }
    }
}
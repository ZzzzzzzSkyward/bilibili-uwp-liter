using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace BiliLite.Controls.Dynamic
{
    /// <summary>
    /// 预览比例框(CARD_PLAN §6):先按比例占位,再往里填图。
    /// 尺寸规则集中在这里,模板只传上限与比例来源,不再各自写死 MaxHeight。
    /// </summary>
    public sealed partial class DynamicPreviewRow : UserControl
    {
        /// <summary>宽态媒体列上限,DynamicTemplate.xaml 的 OneRow 列宽也是 160(令牌表)。</summary>
        private const double DefaultMaxFrameWidth = 160;
        /// <summary>宽度缺失时的中性比例:正方形,不会把缺尺寸的图撑成怪比例。</summary>
        private const double NeutralAspectRatio = 1;
        private const double MinAspectRatio = 0.2;
        private const double MaxAspectRatio = 3;
        private const double SixteenByNineRatio = 9d / 16d;
        /// <summary>卡片宽度低于这个值走 96×54 档(CARD_PLAN §6 的小封面档)。</summary>
        private const double CompactCardWidth = 360;
        /// <summary>小封面档允许比列宽多出的像素,再窄也不和标题列重叠。</summary>
        private const double CompactOverflowAllowance = 12;

        private DynamicItemControl _card;

        public DynamicPreviewRow()
        {
            this.InitializeComponent();
            this.SizeChanged += OnFrameSizeChanged;
            this.Loaded += OnFrameLoaded;
        }

        private void OnFrameSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateFrameSize();
        }

        private void OnFrameLoaded(object sender, RoutedEventArgs e)
        {
            UpdateFrameSize();
        }

        /// <summary>
        /// 控件本身左右拉满(拿到宿主列的实际宽度),框靠左并按算出来的宽高定尺寸。
        /// </summary>
        private void UpdateFrameSize()
        {
            var available = ActualWidth;
            if (double.IsNaN(available) || available <= 0)
            {
                available = MaxFrameWidth > 0 ? MaxFrameWidth : DefaultMaxFrameWidth;
            }
            var width = MaxFrameWidth > 0 ? Math.Min(available, MaxFrameWidth) : available;
            if (CompactFrameWidth > 0 && GetCardWidth() < CompactCardWidth)
            {
                width = Math.Min(Math.Max(width, CompactFrameWidth), available + CompactOverflowAllowance);
            }
            if (MinFrameWidth > 0)
            {
                width = Math.Max(width, Math.Min(MinFrameWidth, available));
            }
            var ratio = ResolveAspectRatio();
            var height = width * ratio;
            if (MaxFrameHeight > 0 && height > MaxFrameHeight)
            {
                height = MaxFrameHeight;
                width = height / ratio;
            }
            Frame.Width = Math.Round(width);
            Frame.Height = Math.Round(height);
        }

        private double ResolveAspectRatio()
        {
            if (ForceSixteenByNine)
            {
                return SixteenByNineRatio;
            }
            if (NaturalWidth <= 0 || NaturalHeight <= 0)
            {
                return NeutralAspectRatio;
            }
            var ratio = (double)NaturalHeight / NaturalWidth;
            if (ratio < MinAspectRatio)
            {
                return MinAspectRatio;
            }
            if (ratio > MaxAspectRatio)
            {
                return MaxAspectRatio;
            }
            return ratio;
        }

        /// <summary>
        /// 小封面档看的是卡片宽度,不是媒体列宽度(媒体列本身就按卡片宽度分出来的)。
        /// 找不到宿主卡片时返回无穷大,即永远走宽态档。
        /// </summary>
        private double GetCardWidth()
        {
            if (_card == null)
            {
                DependencyObject current = VisualTreeHelper.GetParent(this);
                while (current != null && !(current is DynamicItemControl))
                {
                    current = VisualTreeHelper.GetParent(current);
                }
                _card = current as DynamicItemControl;
            }
            return _card == null ? double.PositiveInfinity : _card.ActualWidth;
        }

        private static void OnMeasurePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var row = (DynamicPreviewRow)d;
            if (row.Frame != null)
            {
                row.UpdateFrameSize();
            }
        }

        /// <summary>预览图地址;直接交给 ImageEx(它按地址做缓存与占位),这里只负责空地址清掉。</summary>
        public string Source
        {
            get { return (string)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(string), typeof(DynamicPreviewRow), new PropertyMetadata(null, OnSourceChanged));

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DynamicPreviewRow)d).ApplySource(e.NewValue as string);
        }

        private void ApplySource(string url)
        {
            PreviewImage.Source = string.IsNullOrEmpty(url) ? null : url;
        }

        /// <summary>图片原始宽,0 表示未知(用中性比例)。</summary>
        public int NaturalWidth
        {
            get { return (int)GetValue(NaturalWidthProperty); }
            set { SetValue(NaturalWidthProperty, value); }
        }

        public static readonly DependencyProperty NaturalWidthProperty =
            DependencyProperty.Register("NaturalWidth", typeof(int), typeof(DynamicPreviewRow), new PropertyMetadata(0, OnMeasurePropertyChanged));

        /// <summary>图片原始高,0 表示未知(用中性比例)。</summary>
        public int NaturalHeight
        {
            get { return (int)GetValue(NaturalHeightProperty); }
            set { SetValue(NaturalHeightProperty, value); }
        }

        public static readonly DependencyProperty NaturalHeightProperty =
            DependencyProperty.Register("NaturalHeight", typeof(int), typeof(DynamicPreviewRow), new PropertyMetadata(0, OnMeasurePropertyChanged));

        /// <summary>忽略图片原始比例,固定 16:9(预告封面本来就是 16:9 缩略图)。</summary>
        public bool ForceSixteenByNine
        {
            get { return (bool)GetValue(ForceSixteenByNineProperty); }
            set { SetValue(ForceSixteenByNineProperty, value); }
        }

        public static readonly DependencyProperty ForceSixteenByNineProperty =
            DependencyProperty.Register("ForceSixteenByNine", typeof(bool), typeof(DynamicPreviewRow), new PropertyMetadata(false, OnMeasurePropertyChanged));

        /// <summary>框宽上限,0 表示不限制(只受可用宽度约束)。</summary>
        public double MaxFrameWidth
        {
            get { return (double)GetValue(MaxFrameWidthProperty); }
            set { SetValue(MaxFrameWidthProperty, value); }
        }

        public static readonly DependencyProperty MaxFrameWidthProperty =
            DependencyProperty.Register("MaxFrameWidth", typeof(double), typeof(DynamicPreviewRow), new PropertyMetadata(DefaultMaxFrameWidth, OnMeasurePropertyChanged));

        /// <summary>框高上限(单图 500),0 表示不限制;超过上限时按比例反推宽度,不压扁。</summary>
        public double MaxFrameHeight
        {
            get { return (double)GetValue(MaxFrameHeightProperty); }
            set { SetValue(MaxFrameHeightProperty, value); }
        }

        public static readonly DependencyProperty MaxFrameHeightProperty =
            DependencyProperty.Register("MaxFrameHeight", typeof(double), typeof(DynamicPreviewRow), new PropertyMetadata(0d, OnMeasurePropertyChanged));

        /// <summary>小封面档宽度(OneRow 传 96),0 表示不启用小封面档。</summary>
        public double CompactFrameWidth
        {
            get { return (double)GetValue(CompactFrameWidthProperty); }
            set { SetValue(CompactFrameWidthProperty, value); }
        }

        public static readonly DependencyProperty CompactFrameWidthProperty =
            DependencyProperty.Register("CompactFrameWidth", typeof(double), typeof(DynamicPreviewRow), new PropertyMetadata(0d, OnMeasurePropertyChanged));

        /// <summary>框宽下限,0 表示不设下限。</summary>
        public double MinFrameWidth
        {
            get { return (double)GetValue(MinFrameWidthProperty); }
            set { SetValue(MinFrameWidthProperty, value); }
        }

        public static readonly DependencyProperty MinFrameWidthProperty =
            DependencyProperty.Register("MinFrameWidth", typeof(double), typeof(DynamicPreviewRow), new PropertyMetadata(88d, OnMeasurePropertyChanged));

        /// <summary>叠在框上的角标层(标签/封面文案/长图),由模板提供,坐标系就是框本身。</summary>
        public object OverlayContent
        {
            get { return GetValue(OverlayContentProperty); }
            set { SetValue(OverlayContentProperty, value); }
        }

        public static readonly DependencyProperty OverlayContentProperty =
            DependencyProperty.Register("OverlayContent", typeof(object), typeof(DynamicPreviewRow), new PropertyMetadata(null, OnOverlayContentChanged));

        private static void OnOverlayContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DynamicPreviewRow)d).OverlayHost.Content = e.NewValue;
        }
    }
}

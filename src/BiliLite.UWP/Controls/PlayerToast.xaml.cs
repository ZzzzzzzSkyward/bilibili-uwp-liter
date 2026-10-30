using FontAwesome5;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace BiliLite.Controls
{
    /// <summary>
    /// 播放器内的一条浮层提示，尺寸随播放器尺寸等比缩放
    /// </summary>
    public sealed partial class PlayerToast : UserControl
    {
        private const double BaseFontSize = 20;
        private const double BasePaddingX = 16;
        private const double BasePaddingY = 8;
        private const double BaseIconSpacing = 10;
        private const double BaseCornerRadius = 8;

        public PlayerToast()
        {
            this.InitializeComponent();
        }

        public bool IsToastVisible => ToastRoot.Visibility == Visibility.Visible;

        public void SetContent(string text, EFontAwesomeIcon icon)
        {
            TxtToast.Text = text ?? "";
            IconGlyph.Icon = icon;
            IconGlyph.Visibility = icon == EFontAwesomeIcon.None ? Visibility.Collapsed : Visibility.Visible;
        }

        public void ApplyScale(double scale)
        {
            if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0)
            {
                scale = 1;
            }
            TxtToast.FontSize = BaseFontSize * scale;
            IconGlyph.FontSize = BaseFontSize * scale;
            IconGlyph.Margin = new Thickness(0, 0, BaseIconSpacing * scale, 0);
            ToastRoot.Padding = new Thickness(BasePaddingX * scale, BasePaddingY * scale, BasePaddingX * scale, BasePaddingY * scale);
            ToastRoot.CornerRadius = new CornerRadius(BaseCornerRadius * scale);
        }

        public void ShowToast()
        {
            if (!IsToastVisible)
            {
                ToastRoot.Visibility = Visibility.Visible;
            }
        }

        public void HideToast()
        {
            if (IsToastVisible)
            {
                ToastRoot.Visibility = Visibility.Collapsed;
            }
        }
    }
}

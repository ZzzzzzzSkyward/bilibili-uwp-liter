using FontAwesome5;
using FontAwesome5.Extensions;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

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

        //淡出期间元素必须留在布局里，否则居中的父级会重新居中，正在变透明的浮层会跟着往右挪
        private static readonly TimeSpan ShowFadeDuration = TimeSpan.FromMilliseconds(150);
        private static readonly TimeSpan HideFadeDuration = TimeSpan.FromMilliseconds(250);

        private readonly Storyboard _showFade;
        private readonly Storyboard _hideFade;

        private bool _hiding;

        public PlayerToast()
        {
            this.InitializeComponent();

            _showFade = CreateFadeStoryboard(new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(ShowFadeDuration),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

            _hideFade = CreateFadeStoryboard(new DoubleAnimation
            {
                To = 0,
                Duration = new Duration(HideFadeDuration),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            });
            _hideFade.Completed += HideFade_Completed;
        }

        /// <summary>浮层当前是否显示着（淡出期间仍算显示）</summary>
        public bool IsToastVisible => ToastRoot.Visibility == Visibility.Visible;

        public void SetContent(string text, EFontAwesomeIcon icon)
        {
            TxtToast.Text = text ?? "";
            if (icon == EFontAwesomeIcon.None)
            {
                IconGlyph.Text = "";
                IconGlyph.Visibility = Visibility.Collapsed;
                return;
            }
            IconGlyph.FontFamily = icon.GetFontFamily();
            IconGlyph.Text = icon.GetUnicode();
            IconGlyph.Visibility = Visibility.Visible;
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
            if (_hiding)
            {
                //淡出被重新显示打断：停在完全不透明，不重新从 0 淡入（那会闪一下）
                _hiding = false;
                _hideFade.Stop();
                ToastRoot.Opacity = 1;
                return;
            }
            if (IsToastVisible)
            {
                return;
            }
            ToastRoot.Visibility = Visibility.Visible;
            _showFade.Begin();
        }

        public void HideToast()
        {
            if (!IsToastVisible || _hiding)
            {
                return;
            }
            _hiding = true;
            _showFade.Stop();
            _hideFade.Begin();
        }

        private Storyboard CreateFadeStoryboard(DoubleAnimation animation)
        {
            var storyboard = new Storyboard();
            Storyboard.SetTarget(animation, ToastRoot);
            Storyboard.SetTargetProperty(animation, "Opacity");
            storyboard.Children.Add(animation);
            return storyboard;
        }

        private void HideFade_Completed(object sender, object e)
        {
            //淡出期间又被显示：这次完成回调已经作废
            if (!_hiding)
            {
                return;
            }
            _hiding = false;
            ToastRoot.Visibility = Visibility.Collapsed;
            //停掉停在 0 的动画，下次显示从基础值开始
            _hideFade.Stop();
        }
    }
}

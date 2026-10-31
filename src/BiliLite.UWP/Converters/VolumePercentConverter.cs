using BiliLite.Services;
using System;
using Windows.UI.Xaml.Data;

namespace BiliLite.Converters
{
    /// <summary>
    /// 音量滑块的气泡数值：Player.Volume 是 0-1，显示成 0-100 的整数百分比。
    /// </summary>
    public class VolumePercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return PlayerToastOverlay.FormatPercent(0);
            }
            double volume;
            try
            {
                volume = System.Convert.ToDouble(value);
            }
            catch (Exception)
            {
                return value.ToString();
            }
            return PlayerToastOverlay.FormatPercent(volume);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}

using BiliLite.Helpers;
using System;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace BiliLite.Converters
{
    public class ImageSourceConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null||string.IsNullOrEmpty(value.ToString()))
            {
                return new BitmapImage(new Uri("ms-appx:///Assets/Thumbnails/Placeholde.png"));
            }
            if (SettingHelper.UI.LoadOriginalImage)
            {
                return new BitmapImage(new Uri(value.ToString()));
            }
            if (value.ToString().Contains("@"))
            {
                return new BitmapImage(new Uri(value.ToString()));
            }
            // 没给缩放参数就按原图绑定:调用方只提供地址,不等于是地址写错
            if (parameter == null || string.IsNullOrEmpty(parameter.ToString()))
            {
                return new BitmapImage(new Uri(value.ToString()));
            }
            if (parameter.ToString().Contains("."))
            {
                return new BitmapImage(new Uri(value.ToString() + "@" + parameter.ToString()));
            }
            var url = value.ToString() + "@" + parameter.ToString() + ".jpg";
            return new BitmapImage(new Uri(url));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}

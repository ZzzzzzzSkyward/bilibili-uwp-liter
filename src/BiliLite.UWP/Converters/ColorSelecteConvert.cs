using BiliLite.Helpers;
using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
namespace BiliLite.Converters
{
    public class ColorSelecteConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value==null)
            {
                return Utils.GetBrush("TextColor");
            }
            if (value.ToString()== parameter.ToString())
            {
                return Utils.GetBrush("HighLightColor");
            }
            else
            {
                return Utils.GetBrush("DefaultTextColor");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return Colors.Black;
        }
    }
}

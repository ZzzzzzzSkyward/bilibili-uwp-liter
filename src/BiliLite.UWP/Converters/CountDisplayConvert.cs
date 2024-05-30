using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace BiliLite.Converters
{
    public class CountDisplayConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
           if(value is int)
            {
                if((int)value > 0)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
    public class CountGreaterThanOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int count)
            {
                return count > 1;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

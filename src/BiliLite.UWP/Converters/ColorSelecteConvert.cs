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
            if (value == null)
            {
                return new SolidColorBrush((Color)App.Current.Resources["TextColor"]);
            }
            if (value.ToString() == parameter.ToString())
            {
                var themeDictionaries = App.Current.Resources.ThemeDictionaries;
                var requestedTheme = ((FrameworkElement)Window.Current.Content).RequestedTheme;
                var themeKey = requestedTheme == ElementTheme.Light ? "Light" : "Dark";
                if (themeDictionaries.TryGetValue(themeKey, out object theme))
                {
                    var highLightColor = (Color)((ResourceDictionary)theme)["HighLightColor"];
                    return new SolidColorBrush(highLightColor);
                }
                else
                {
                    // 处理主题字典中未找到指定的键的情况
                    return new SolidColorBrush(Colors.Transparent);
                }
            }
            else
            {
                return (SolidColorBrush)App.Current.Resources["DefaultTextColor"];
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return Colors.Black;
        }
    }
}
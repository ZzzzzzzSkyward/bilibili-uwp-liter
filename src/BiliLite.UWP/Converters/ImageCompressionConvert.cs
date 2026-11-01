using BiliLite.Helpers;
using System;
using Windows.UI.Xaml.Data;

namespace BiliLite.Converters
{
    public class ImageCompressionConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return "ms-appx:///Assets/Thumbnails/Placeholde.png";
            }

            //http:// 和 // 开头的地址在这里统一成 https://，评论图片和表情都走这一条
            var url = CommentRichText.NormalizeImageUrl(value.ToString());
            if (url.Length == 0)
            {
                return "ms-appx:///Assets/Thumbnails/Placeholde.png";
            }

            if (SettingHelper.UI.LoadOriginalImage)
            {
                return url;
            }
            if (url.Contains("@"))
            {
                return url;
            }
            if (parameter == null) return url;
            return url + "@" + parameter.ToString() + ".jpg";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}

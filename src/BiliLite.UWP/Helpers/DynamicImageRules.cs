using System;

namespace BiliLite.Helpers
{
    /// <summary>
    /// 动态图片的尺寸判定与缩略图参数。刻意不引用 XAML 类型，探针才能直接执行这层规则。
    /// </summary>
    public static class DynamicImageRules
    {
        /// <summary>
        /// 只有宽高都有效才比较长宽比：服务端偶尔只回高不回宽，缺失尺寸的图不能被判成长图。
        /// </summary>
        public static bool IsLongImage(int width, int height)
        {
            return width > 0 && height > 0 && height > (long)width * 2;
        }

        /// <summary>
        /// 按长宽比挑缩略图：长图取头部裁切，竖图取等比缩小，横图和尺寸未知的图统一取宽度。
        /// </summary>
        public static string WithThumbnailParams(string url, int width, int height)
        {
            if (string.IsNullOrEmpty(url))
            {
                return url;
            }
            if (width <= 0 || height <= 0)
            {
                return url + "@400w.jpg";
            }
            if (IsLongImage(width, height))
            {
                return url + "@240w_320h_!header.webp";
            }
            if (height > width * 1.3)
            {
                return url + "@240w_320h_1e_1c.jpg";
            }
            return url + "@400w.jpg";
        }
    }
}

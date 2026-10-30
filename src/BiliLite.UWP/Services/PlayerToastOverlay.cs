using System;
using System.Globalization;

namespace BiliLite.Services
{
    /// <summary>
    /// 播放器浮层的显示规则。不引用任何 XAML 类型，所以能在没有 UI 树的探针里直接断言。
    /// </summary>
    public static class PlayerToastOverlay
    {
        /// <summary>
        /// 0-1 音量显示为整数百分比（0-100，四舍五入，.5 远离零）。
        /// </summary>
        public static string FormatVolume(double volume)
        {
            var percent = Math.Round(ClampVolume(volume) * 100, MidpointRounding.AwayFromZero);
            return "音量:" + percent.ToString("0", CultureInfo.InvariantCulture) + "%";
        }

        private static double ClampVolume(double volume)
        {
            if (double.IsNaN(volume) || volume <= 0)
            {
                return 0;
            }
            return volume >= 1 ? 1 : volume;
        }
    }
}

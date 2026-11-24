namespace BiliLite.Services
{
    /// <summary>
    /// 首次启动时的首页推荐布局：屏幕比 16:9 更宽才用列表。规则单独成一处，
    /// 因为 <c>DisplayInformation</c> 的像素宽高是 <c>UInt32</c>，直接写 <c>width / height &gt; 16 / 9</c>
    /// 时两侧都在截断：右侧是 1，左侧是地板除法，判据实际变成「宽高比 ≥ 2」，16:9 到 2:1 之间的屏幕
    /// 全被当成网格；高为 0 时还会抛除零。整数交叉相乘没有这两个问题。
    /// </summary>
    public static class FirstRunDisplayMode
    {
        public const long MinimumWidth = 1920;

        /// <summary>宽高比严格大于 16:9 且宽度至少 1920 像素时返回 true（1 = 列表，0 = 网格）。</summary>
        public static bool PrefersList(long widthInRawPixels, long heightInRawPixels)
        {
            if (heightInRawPixels <= 0)
            {
                return false;
            }
            //交叉相乘代替浮点除法：没有精度问题，也不会因为整数除法把 16/9 算成 1
            return widthInRawPixels >= MinimumWidth && widthInRawPixels * 9 > heightInRawPixels * 16;
        }
    }
}

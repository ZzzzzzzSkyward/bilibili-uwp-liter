namespace BiliLite.Helpers
{
    /// <summary>
    /// 鼠标返回开关的判定：开关关闭时，后退侧键和中键都不再生效。
    /// 页面只负责把指针事件映射成这两个布尔值。
    /// </summary>
    public static class MouseBackPolicy
    {
        public static bool ShouldHandle(bool mouseBackEnabled, bool isXButton1, bool isMiddle)
        {
            return mouseBackEnabled && (isXButton1 || isMiddle);
        }
    }
}

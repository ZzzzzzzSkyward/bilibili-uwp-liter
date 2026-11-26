namespace BiliLite.Controls
{
    /// <summary>
    /// 「列表没填满一屏就自动翻页」的闸门。
    ///
    /// 自动翻页只允许在上一轮请求真的让内容变长之后再触发一次：失败页、空页都不会改变
    /// 内容长度，闸门保持卸膛状态，自动翻页就此停下。
    /// 2026-11-15 那次 43 秒 966 次 show/popular/index 就是缺这道闸：Loading 每次 true→false
    /// 都重新触发一次「没填满就翻页」，翻页失败也照样回到 false，循环因此没有出口。
    /// </summary>
    internal sealed class AutoLoadMoreGate
    {
        private bool armed = true;

        /// <summary>内容长度发生变化（首屏、翻页成功、刷新）后重新允许自动翻页。</summary>
        public void OnContentChanged()
        {
            armed = true;
        }

        /// <summary>
        /// canAutoFill 表示当前这轮是否可以补内容（控件在树上、高度非 0、没有可滚动内容、允许翻页）。
        /// 放行后立即卸膛，必须等 <see cref="OnContentChanged"/> 才会再放行。
        /// </summary>
        public bool TryTake(bool canAutoFill)
        {
            if (!armed || !canAutoFill)
            {
                return false;
            }
            armed = false;
            return true;
        }
    }
}

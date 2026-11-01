using System;

namespace BiliLite.Models.Common
{
    /// <summary>
    /// 目录抽屉的三个状态。Closing 是"关门动画在跑、画面还没收掉",等着某个完成回调把它变成 Closed
    /// </summary>
    public enum DirectoryDrawerState
    {
        Closed = 0,
        Open = 1,
        Closing = 2,
    }

    /// <summary>
    /// 目录抽屉开关的判定,只做决定不碰界面,好让关门竞态在 probe 里直接跑。
    /// 关门动画的完成回调带着 BeginClose 发的令牌回来;期间用户又点开了抽屉,
    /// 令牌就作废,迟到的回调不会把刚打开的抽屉藏掉。
    /// 关掉系统动画时不发故事板也不发令牌:开关当场生效,没有完成回调可等,也就没有竞态。
    /// </summary>
    public sealed class DirectoryDrawerMotion
    {
        /// <summary>打开:右侧滑入 380 → 0 的时长</summary>
        public const double OpenDurationMilliseconds = 180;

        /// <summary>收起的时长。比打开略短,关门不拖泥带水</summary>
        public const double CloseDurationMilliseconds = 140;

        /// <summary>遮罩淡入淡出的时长,跟着抽屉一起走</summary>
        public const double ScrimFadeMilliseconds = 140;

        /// <summary>抽屉宽度,滑入的起点和收起动画的终点</summary>
        public const double DrawerWidth = 380;

        /// <summary>"目录"按钮自己的按下反馈:按下缩一下再弹回</summary>
        public const double ButtonPressMilliseconds = 160;

        /// <summary>详情列停在目标页时那一次高亮的总时长</summary>
        public const double HighlightMilliseconds = 460;

        private int m_lastToken;
        private int m_pendingClose;

        public DirectoryDrawerMotion(bool animationsEnabled)
        {
            AnimationsEnabled = animationsEnabled;
        }

        /// <summary>
        /// 系统动画开关(UISettings.AnimationsEnabled)。每次开关抽屉前刷新,只影响下一次 BeginOpen/BeginClose。
        /// </summary>
        public bool AnimationsEnabled { get; set; }

        public DirectoryDrawerState State { get; private set; }

        /// <summary>
        /// 抽屉是否还占着画面。收起动画跑着也算:此时它还在屏幕上,页面照旧把内容推进来。
        /// </summary>
        public bool IsShown
        {
            get { return State != DirectoryDrawerState.Closed; }
        }

        /// <summary>
        /// 遮罩只在抽屉真占画面时出现,抽屉完全收起就没有可点的空白
        /// </summary>
        public bool IsScrimVisible
        {
            get { return State != DirectoryDrawerState.Closed; }
        }

        /// <summary>
        /// 收起的路上:此时画面还在,要等这一个令牌的完成回调才能藏
        /// </summary>
        public bool IsClosing
        {
            get { return State == DirectoryDrawerState.Closing; }
        }

        /// <summary>
        /// 打开。返回 true 表示要跑滑入故事板(起点只对动画有意义);返回 false 表示当场就位、不用等回调
        /// </summary>
        public bool BeginOpen()
        {
            // 之前那次关门作废:它的完成回调再来也不能关门
            m_pendingClose = 0;
            State = DirectoryDrawerState.Open;
            return AnimationsEnabled;
        }

        /// <summary>
        /// 开始收起。动画关掉时直接到 Closed 并返回 0(令牌 0 永远不被 CanHide 接受);
        /// 否则返回本次关门的令牌,完成回调必须拿着它回来。
        /// </summary>
        public int BeginClose()
        {
            if (!AnimationsEnabled)
            {
                m_pendingClose = 0;
                State = DirectoryDrawerState.Closed;
                return 0;
            }
            m_pendingClose = ++m_lastToken;
            State = DirectoryDrawerState.Closing;
            return m_pendingClose;
        }

        /// <summary>
        /// 这个令牌还能不能收掉抽屉:只有"最近一次关门"的令牌、并且抽屉仍在关门的路上才算数
        /// </summary>
        public bool CanHide(int token)
        {
            return token > 0 && token == m_pendingClose && State == DirectoryDrawerState.Closing;
        }

        /// <summary>
        /// 关门动画结束。令牌作废就什么都不做并返回 false,抽屉保持打开。
        /// </summary>
        public bool CompleteClose(int token)
        {
            if (!CanHide(token)) return false;
            m_pendingClose = 0;
            State = DirectoryDrawerState.Closed;
            return true;
        }

        /// <summary>
        /// 页面把它的按钮收走、或者换了视频时不用动画地收掉抽屉
        /// </summary>
        public void Reset()
        {
            m_pendingClose = 0;
            State = DirectoryDrawerState.Closed;
        }
    }
}

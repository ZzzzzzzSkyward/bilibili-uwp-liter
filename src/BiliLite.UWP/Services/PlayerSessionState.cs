using System;
using System.Collections.Generic;

namespace BiliLite.Services
{
    /// <summary>
    /// 这一次打开媒体是为了什么。只用来诊断和 probe 断言,不参与判定。
    /// </summary>
    public enum PlayerSessionReason
    {
        Open = 0,
        LocalOpen = 1,
        QualityChange = 2,
        EngineFallback = 3,
    }

    /// <summary>
    /// 一次打开的凭证。会话被关页、换视频、换清晰度、换引擎取代之后,拿着它回来的回调一律不再被认。
    /// </summary>
    public sealed class PlayerSessionTicket
    {
        private readonly PlayerSessionState owner;

        internal PlayerSessionTicket(PlayerSessionState owner, int id, PlayerSessionReason reason)
        {
            this.owner = owner;
            Id = id;
            Reason = reason;
        }

        /// <summary>会话 id:每次 Begin 递增,不复用</summary>
        public int Id { get; }

        public PlayerSessionReason Reason { get; }

        /// <summary>这一次打开还是当前的那一次</summary>
        public bool IsCurrent
        {
            get { return owner.IsCurrent(Id); }
        }

        /// <summary>资源已经交给播放器(_ffmpegMSSVideo 这些字段),此后由 ClosePlay/Dispose 负责</summary>
        public bool Accept()
        {
            return owner.Accept(Id);
        }

        /// <summary>
        /// 把还没交给播放器的资源挂到这次打开名下。返回 false 表示会话已经作废,
        /// 资源已经被就地释放,调用方不要再碰它。
        /// </summary>
        public bool Hold(IDisposable resource)
        {
            return owner.Hold(Id, resource);
        }

        /// <summary>
        /// 这一次尝试失败、但这次打开还要换引擎再试一次:释放本次尝试挂着、还没交给播放器的资源,
        /// 票据保持当前、还没结束,回退的那次尝试继续用它。已经 Accept 过的会话不受影响。
        /// </summary>
        public bool Abandon()
        {
            return owner.Abandon(Id);
        }

        /// <summary>
        /// 整次打开到此为止、后面没有回退:释放还挂着的资源并且让票据作废。
        /// 还要在同一个票据上重试时用 Abandon(Release 会让票据作废,回退就用不了它了)。
        /// </summary>
        public bool Release()
        {
            return owner.Release(Id);
        }
    }

    /// <summary>
    /// 播放器打开/替换/关闭的归属判定。一次打开一个会话 id:
    /// 打开途中建出来的资源先挂在会话名下,会话被取代时在这里释放;资源进字段那一刻才 Accept。
    /// 一次打开里可以有好几次引擎尝试(原生失败换 FFmpeg):尝试失败用 Abandon 只放掉它自己的那份,
    /// 票据还是当前的,回退的那次接着用;整次打开才 Release。
    /// 只做判定,不碰界面、不发事件、不认识播放器类型,所以这些竞态能在 probe 里直接跑。
    /// 只在 UI 线程用:一次 Begin/Accept/Invalidate/Release/Abandon/Hold 之间不会插进别的线程,因此没有加锁。
    /// </summary>
    public sealed class PlayerSessionState
    {
        private enum SessionStatus
        {
            Pending = 0,
            Accepted = 1,
            Invalidated = 2,
        }

        private readonly List<IDisposable> held = new List<IDisposable>();
        private int nextId = 1;
        private PlayerSessionTicket current;
        private SessionStatus status;
        // 当前会话挂着的资源已经释放过:保证"作废时释放一次",迟到的第二次不再动这些对象
        private bool released;

        /// <summary>当前会话;没打开过或者已经关掉时是 null</summary>
        public PlayerSessionTicket Current
        {
            get { return current; }
        }

        /// <summary>当前会话的 id;没有当前会话时是 0</summary>
        public int CurrentId
        {
            get { return current == null ? 0 : current.Id; }
        }

        /// <summary>这一次打开还没结束:既没有交给播放器,也没有被作废</summary>
        public bool IsPending(int id)
        {
            return current != null && current.Id == id && status == SessionStatus.Pending;
        }

        /// <summary>
        /// 开一次新打开。在飞的上一次打开在这里作废,它建出来但还没交给播放器的资源当场释放。
        /// </summary>
        public PlayerSessionTicket Begin(PlayerSessionReason reason)
        {
            Invalidate();
            current = new PlayerSessionTicket(this, nextId++, reason);
            status = SessionStatus.Pending;
            released = false;
            return current;
        }

        /// <summary>这个 id 还是当前的那一次打开</summary>
        public bool IsCurrent(int id)
        {
            return current != null && current.Id == id && status != SessionStatus.Invalidated;
        }

        /// <summary>
        /// 这次打开到这里为止建的资源都进播放器字段了:所有权交给播放器,会话不再释放它们。
        /// 返回 false 表示会话已经作废,这次打开不算数。
        /// </summary>
        public bool Accept(int id)
        {
            if (!IsCurrent(id) || status != SessionStatus.Pending) return false;
            status = SessionStatus.Accepted;
            held.Clear();
            return true;
        }

        /// <summary>
        /// 关页/换视频/换清晰度:当前会话作废,它挂着的资源在这里释放一次。
        /// 没有当前会话(或者已经作废)时返回 false。
        /// </summary>
        public bool Invalidate()
        {
            if (current == null) return false;
            var wasLive = status != SessionStatus.Invalidated;
            ReleaseHeld();
            released = true;
            status = SessionStatus.Invalidated;
            current = null;
            return wasLive;
        }

        /// <summary>
        /// 这一次尝试在 Accept 之前就失败了:只释放它自己挂着的资源,票据还是当前的、还没结束,
        /// 同一个票据上的下一次尝试(换引擎回退)接着用它。
        /// 已经 Accept 过的会话不在这里释放(那些资源归播放器字段),所以失败路径上统一调它不会误伤。
        /// 一次尝试只会释放一次,迟到的调用不再动已经释放过的对象。返回是否真的释放了东西。
        /// </summary>
        public bool Abandon(int id)
        {
            if (current == null || current.Id != id) return false;
            if (status != SessionStatus.Pending) return false;
            return ReleaseHeld();
        }

        /// <summary>
        /// 释放这个会话还挂着的资源。作废/替换时由 Invalidate 调;整次打开失败、后面没有回退时由调用方调
        /// (还要在同一个票据上换引擎再试就调 Abandon)。同一个会话只会释放一次,迟到的调用不再动已经释放过的对象。
        /// 返回是否真的释放了东西。
        /// </summary>
        public bool Release(int id)
        {
            if (current == null || current.Id != id || released) return false;
            released = true;
            var disposed = ReleaseHeld();
            if (status == SessionStatus.Pending) status = SessionStatus.Invalidated;
            return disposed;
        }

        /// <summary>
        /// 把资源挂到这次打开名下。返回 false 表示会话已经不认这个 id:
        /// 资源在这里当场释放(那次打开遗留的最后一份东西),调用方不要再碰它。
        /// </summary>
        public bool Hold(int id, IDisposable resource)
        {
            if (resource == null) return false;
            if (!IsCurrent(id))
            {
                resource.Dispose();
                return false;
            }
            held.Add(resource);
            return true;
        }

        private bool ReleaseHeld()
        {
            if (held.Count == 0) return false;
            foreach (var resource in held)
            {
                try
                {
                    resource.Dispose();
                }
                catch (Exception)
                {
                    // 释放失败不能挡住后面的资源:关页时该清干净
                }
            }
            held.Clear();
            return true;
        }
    }
}

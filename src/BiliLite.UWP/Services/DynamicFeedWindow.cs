using System;
using System.Collections.Generic;
using BiliLite.Controls.Dynamic;

namespace BiliLite.Services
{
    /// <summary>
    /// 被移出窗口的一页：只留"重新取这一页"需要的定位信息，卡片模型与图片引用一概不留。
    /// CursorBeforePage 是当时请求这一页用的游标，重新加载时原样回传。
    /// </summary>
    public sealed class DynamicFeedPageCheckpoint
    {
        public int PageNumber { get; set; }

        public string CursorBeforePage { get; set; }

        /// <summary>这一页当时给出的下一页游标；重新加载后插回窗口时用得到。</summary>
        public string CursorAfterPage { get; set; }

        public string FirstDynamicId { get; set; }

        public int Count { get; set; }

        /// <summary>该页的动态 ID：淘汰时按 ID 从列表里摘掉对应行；卡片模型本身已丢弃。</summary>
        public IList<string> DynamicIds { get; set; }
    }

    /// <summary>窗口里仍保留的一页。</summary>
    public sealed class DynamicFeedWindowPage
    {
        public int PageNumber { get; set; }

        public string CursorBeforePage { get; set; }

        public string CursorAfterPage { get; set; }

        public IList<DynamicItemDisplayModel> Items { get; set; }

        public int Count
        {
            get { return Items == null ? 0 : Items.Count; }
        }
    }

    /// <summary>
    /// 滚动页窗口（CARD_PLAN §9）：列表只保留最近若干页，超出的整页从最前端淘汰，
    /// 淘汰的页只留检查点供"重新加载更早的页"。
    ///
    /// 三条不变量：
    /// 1. 只整页淘汰、只从最前端淘汰；含 anchorId 的页永不淘汰（宁可超出上限）。
    /// 2. 去重集合与窗口同寿命：页被淘汰时它的 ID 一起释放，否则重载旧页会被当成重复而丢弃。
    /// 3. 分页进度不在这里算：CanContinuePaging 拿的是解析页的条目数，不是去重后的幸存数。
    ///
    /// 纯逻辑，不引用 WinRT，可直接被 plan/tests/g3 的探针编译。
    /// </summary>
    public class DynamicFeedWindow
    {
        /// <summary>窗口上限（CARD_PLAN §9 的起始值：10 页）。</summary>
        public const int MaxRetainedPages = 10;

        private readonly List<DynamicFeedWindowPage> _pages = new List<DynamicFeedWindowPage>();
        private readonly List<DynamicFeedPageCheckpoint> _evicted = new List<DynamicFeedPageCheckpoint>();
        private readonly HashSet<string> _seenIds = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>当前窗口里的页，按页序号从早到晚。</summary>
        public IList<DynamicFeedWindowPage> Pages
        {
            get { return _pages; }
        }

        public int PageCount
        {
            get { return _pages.Count; }
        }

        /// <summary>窗口里最后一页的页序号；窗口为空时为 0。</summary>
        public int LastPageNumber
        {
            get { return _pages.Count == 0 ? 0 : _pages[_pages.Count - 1].PageNumber; }
        }

        /// <summary>已移出窗口的页数（列表顶部提示用）。</summary>
        public int EvictedPageCount
        {
            get { return _evicted.Count; }
        }

        public bool HasEvictedPages
        {
            get { return _evicted.Count > 0; }
        }

        /// <summary>已移出窗口的页检查点，按淘汰顺序（最早的在前）。</summary>
        public IList<DynamicFeedPageCheckpoint> EvictedPages
        {
            get { return _evicted; }
        }

        /// <summary>锚点所在页永不淘汰；由页面在滚动时写入。</summary>
        public string AnchorDynamicId { get; set; }

        /// <summary>去重集合当前记住的 ID 数（探针用）。</summary>
        public int RetainedIdCount
        {
            get { return _seenIds.Count; }
        }

        /// <summary>刷新/换分类/换用户：窗口、检查点、去重集合整体作废。</summary>
        public void Reset()
        {
            _pages.Clear();
            _evicted.Clear();
            _seenIds.Clear();
            AnchorDynamicId = null;
        }

        /// <summary>
        /// 按动态 ID 去重：已出现过的 ID 丢弃，同一页内部的重复也只留第一次。
        /// 幸存者当场登记进去重集合，因此必须在页真正上屏时调用一次（每次一页一次）。
        /// 没有 DynamicID 的卡片无法去重，原样放过。
        /// </summary>
        public IList<DynamicItemDisplayModel> AcceptItems(IEnumerable<DynamicItemDisplayModel> items)
        {
            var accepted = new List<DynamicItemDisplayModel>();
            if (items == null)
            {
                return accepted;
            }
            foreach (var item in items)
            {
                if (item == null)
                {
                    continue;
                }
                var id = item.DynamicID;
                if (string.IsNullOrEmpty(id))
                {
                    accepted.Add(item);
                    continue;
                }
                if (!_seenIds.Add(id))
                {
                    continue;
                }
                accepted.Add(item);
            }
            return accepted;
        }

        /// <summary>
        /// 登记一页（items 必须是已过 AcceptItems 的幸存者），返回本次被淘汰的页检查点。
        /// 锚点页不淘汰，因此窗口可以暂时超过 MaxRetainedPages。
        /// </summary>
        public IList<DynamicFeedPageCheckpoint> AddPage(int pageNumber, IList<DynamicItemDisplayModel> items,
            string cursorBeforePage, string cursorAfterPage)
        {
            var evicted = new List<DynamicFeedPageCheckpoint>();
            _pages.Add(new DynamicFeedWindowPage
            {
                PageNumber = pageNumber,
                CursorBeforePage = cursorBeforePage,
                CursorAfterPage = cursorAfterPage,
                Items = items ?? new List<DynamicItemDisplayModel>()
            });
            while (_pages.Count > MaxRetainedPages)
            {
                var oldest = _pages[0];
                if (!string.IsNullOrEmpty(AnchorDynamicId) && ContainsId(oldest.Items, AnchorDynamicId))
                {
                    // 锚点还在这页上：淘汰它会把用户正在看的位置抽走，宁可让窗口超出上限
                    break;
                }
                _pages.RemoveAt(0);
                var checkpoint = Evict(oldest);
                evicted.Add(checkpoint);
                // 检查点同时留在窗口上：顶部提示与"重新加载"都要读它
                _evicted.Add(checkpoint);
            }
            return evicted;
        }

        /// <summary>
        /// 重新加载回来的早页插回窗口最前端；不参与本次淘汰（否则插进去立刻又被淘汰）。
        /// 这些 ID 在淘汰时已被释放，这里重新登记，窗口与去重集合保持同一份内容。
        /// </summary>
        public void ReinsertPage(int pageNumber, IList<DynamicItemDisplayModel> items,
            string cursorBeforePage, string cursorAfterPage)
        {
            var pageItems = items ?? new List<DynamicItemDisplayModel>();
            foreach (var item in pageItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.DynamicID))
                {
                    _seenIds.Add(item.DynamicID);
                }
            }
            _pages.Insert(0, new DynamicFeedWindowPage
            {
                PageNumber = pageNumber,
                CursorBeforePage = cursorBeforePage,
                CursorAfterPage = cursorAfterPage,
                Items = pageItems
            });
        }

        /// <summary>
        /// 看最近淘汰的一页但不取走：请求失败时检查点要留着。
        /// 淘汰是从窗口顶部逐页往前的，所以最近淘汰的那一页正是紧邻当前窗口的那一页；
        /// 先补它，补回来的页再插到最前面，列表顺序才与原始分页一致（1,2,3,…）。
        /// </summary>
        public bool TryPeekCheckpoint(out DynamicFeedPageCheckpoint checkpoint)
        {
            if (_evicted.Count == 0)
            {
                checkpoint = null;
                return false;
            }
            checkpoint = _evicted[_evicted.Count - 1];
            return true;
        }

        /// <summary>取走最近淘汰的一个检查点：这一页已经重新进窗口。</summary>
        public bool TryTakeCheckpoint(out DynamicFeedPageCheckpoint checkpoint)
        {
            if (_evicted.Count == 0)
            {
                checkpoint = null;
                return false;
            }
            checkpoint = _evicted[_evicted.Count - 1];
            _evicted.RemoveAt(_evicted.Count - 1);
            return true;
        }

        private DynamicFeedPageCheckpoint Evict(DynamicFeedWindowPage page)
        {
            var ids = new List<string>();
            if (page.Items != null)
            {
                foreach (var item in page.Items)
                {
                    if (item == null || string.IsNullOrEmpty(item.DynamicID))
                    {
                        continue;
                    }
                    ids.Add(item.DynamicID);
                    // 页走了，它的 ID 也忘了：重载同一页时这些卡片要能重新被接受
                    _seenIds.Remove(item.DynamicID);
                }
            }
            return new DynamicFeedPageCheckpoint
            {
                PageNumber = page.PageNumber,
                CursorBeforePage = page.CursorBeforePage,
                CursorAfterPage = page.CursorAfterPage,
                FirstDynamicId = ids.Count == 0 ? null : ids[0],
                Count = page.Count,
                DynamicIds = ids
            };
        }

        private static bool ContainsId(IList<DynamicItemDisplayModel> items, string id)
        {
            if (items == null || string.IsNullOrEmpty(id))
            {
                return false;
            }
            foreach (var item in items)
            {
                if (item != null && string.Equals(item.DynamicID, id, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

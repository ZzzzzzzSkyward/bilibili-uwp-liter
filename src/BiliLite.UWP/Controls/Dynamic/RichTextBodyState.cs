using System;

namespace BiliLite.Controls.Dynamic
{
    /// <summary>正文该挂什么：富文本、纯文本回退，或什么都不挂。</summary>
    public enum RichTextBodyKind
    {
        Empty,
        RichText,
        PlainText,
    }

    /// <summary>正文所在的位置：决定初始行数上限与是否默认展开（CARD_PLAN §4 展开策略）。</summary>
    public enum RichTextBodyRole
    {
        /// <summary>信息流正文：默认六行。</summary>
        Feed = 0,

        /// <summary>转发原动态正文：默认四行。</summary>
        RepostOrigin = 1,

        /// <summary>动态详情正文：不限行，默认展开。</summary>
        Detail = 2,
    }

    /// <summary>
    /// 正文的视觉策略（不含任何 UI 类型，可在探测里直接断言）。
    ///
    /// 1. 行数上限只是视觉策略：信息流六行、转发原动态四行、详情不限；不截断片段，纯文本始终完整。
    /// 2. 只在内容真的超出上限时才显示"展开"，正好占满上限不算溢出；判定留半行容差吸收行高度量舍入。
    /// 3. 展开态按动态 id 记忆：同 id 重渲染保留，换 id 复位。
    /// 4. 空正文隐藏整个正文区域，不塞"无内容"占位。
    /// 5. 高度变化后按锚点位移补偿滚动偏移，且不越过顶部。
    /// </summary>
    public static class RichTextBodyPolicy
    {
        /// <summary>信息流正文的初始行数。</summary>
        public const int FeedLineCap = 6;

        /// <summary>转发原动态正文的初始行数。</summary>
        public const int RepostOriginLineCap = 4;

        /// <summary>不限行（详情或已展开）。</summary>
        public const int UnlimitedLines = 0;

        /// <summary>不足半行的度量抖动不算溢出。</summary>
        private const double HalfLineSlack = 0.5;

        public static int LineCap(RichTextBodyRole role)
        {
            switch (role)
            {
                case RichTextBodyRole.RepostOrigin:
                    return RepostOriginLineCap;
                case RichTextBodyRole.Detail:
                    return UnlimitedLines;
                default:
                    return FeedLineCap;
            }
        }

        /// <summary>只有详情默认展开；信息流与转发原动态默认收起。</summary>
        public static bool StartsExpanded(RichTextBodyRole role)
        {
            return role == RichTextBodyRole.Detail;
        }

        /// <summary>声明位置优先；没声明时，转发原动态靠嵌套层级识别（卡片外壳里再嵌一层卡片）。</summary>
        public static RichTextBodyRole ResolveRole(RichTextBodyRole declared, int cardDepth)
        {
            if (declared == RichTextBodyRole.Detail)
            {
                return RichTextBodyRole.Detail;
            }
            return cardDepth > 1 ? RichTextBodyRole.RepostOrigin : RichTextBodyRole.Feed;
        }

        public static bool Overflows(int renderedLines, int cap)
        {
            if (cap <= UnlimitedLines)
            {
                return false;
            }
            return renderedLines > cap;
        }

        /// <summary>
        /// 实测高度换算成行数。半行容差吸收行高度量舍入：正好占满上限的内容不会被误判成溢出。
        /// </summary>
        public static int RenderedLines(double desiredHeight, double lineHeight)
        {
            if (lineHeight <= 0 || desiredHeight <= 0)
            {
                return 0;
            }
            return (int)Math.Ceiling(desiredHeight / lineHeight - HalfLineSlack);
        }

        /// <summary>空正文收起整个正文区域，不占位、也不写占位词。</summary>
        public static bool HidesRegion(RichTextBodyKind kind)
        {
            return kind == RichTextBodyKind.Empty;
        }

        /// <summary>换身份或换内容都要先清旧 inlines/处理器；都不变才复用已挂载的正文。</summary>
        public static bool NeedsClear(bool identityChanged, bool contentChanged)
        {
            return identityChanged || contentChanged;
        }

        /// <summary>锚点在滚动内容里的位移，就是需要补给滚动偏移的量。</summary>
        public static double ScrollCompensation(double anchorBefore, double anchorAfter)
        {
            return anchorAfter - anchorBefore;
        }

        /// <summary>补偿后的滚动偏移；收缩时不能越过顶部。</summary>
        public static double CompensatedOffset(double offset, double delta)
        {
            var target = offset + delta;
            return target < 0 ? 0 : target;
        }
    }

    /// <summary>一次正文重算的结果：当前属性该挂的内容，以及它是否与已挂载的不同。</summary>
    public struct RichTextBodyPlan
    {
        public RichTextBodyKind Kind;

        /// <summary>Kind == PlainText 时为空时回退用的文本；其余情况为空字符串。</summary>
        public string Text;

        /// <summary>true 表示结果与已挂载的不一致，控制层必须重挂；false 表示可以原样留着。</summary>
        public bool Changed;
    }

    /// <summary>
    /// 正文挂载规则（不含任何 UI 类型，可在探测里直接断言）。
    ///
    /// 模板复用时挂载的子元素必须由当前内容唯一决定，不能由 DP 回调到达的先后顺序决定：
    /// 1. 有非空 RichTextBlock 就挂它，纯文本不参与（富文本优先）。
    /// 2. 没有富文本时用纯文本回退；纯文本为空就留空，不塞"无内容"占位。
    /// 3. 每次重算都从当前两个属性算出全量结果，上一条卡片的正文不会被继承：
    ///    富文本清空即降级为空或纯文本，纯文本清空即清空挂载。
    /// 4. Changed=false 只表示"结果与已挂载的一致"，控制层据此跳过重挂（同一段纯文本重复下发不重建 TextBlock）。
    ///
    /// 状态只描述"当前正文宿主里挂着什么"，因此控制层清空/挂载必须成对走 Apply，
    /// 不能绕过它改宿主；BeginIdentity 报告换了身份时，控制层要先清旧 inlines 与处理器再挂新正文。
    /// </summary>
    public sealed class RichTextBodyState
    {
        private RichTextBodyKind _mountedKind = RichTextBodyKind.Empty;
        private string _mountedText = string.Empty;
        private object _mountedRich;

        private string _bodyId;
        private bool _expanded;

        public RichTextBodyKind MountedKind
        {
            get { return _mountedKind; }
        }

        public string MountedText
        {
            get { return _mountedText; }
        }

        public object MountedRich
        {
            get { return _mountedRich; }
        }

        /// <summary>当前记住展开态的动态 id；null 表示还没开始过一张卡片。</summary>
        public string BodyId
        {
            get { return _bodyId; }
        }

        /// <summary>当前这张卡片的正文是否展开。</summary>
        public bool IsExpanded
        {
            get { return _expanded; }
        }

        /// <summary>
        /// 开始渲染一张卡片的正文。id 变了就是复用到另一张卡片：先丢掉上一张的挂载记录与展开态，
        /// 再让控制层清空旧 inlines/处理器（返回值就是"要清"的信号），这样同一份内容不会被误判成"已挂载"。
        /// </summary>
        /// <returns>true 表示换了身份，控制层必须先清除再挂新正文。</returns>
        public bool BeginIdentity(string bodyId, RichTextBodyRole role)
        {
            var id = bodyId ?? string.Empty;
            var changed = !string.Equals(id, _bodyId ?? string.Empty, StringComparison.Ordinal);
            if (changed)
            {
                _bodyId = id;
                _expanded = RichTextBodyPolicy.StartsExpanded(role);
                ResetMounted();
            }
            return changed;
        }

        /// <summary>用户点了"展开/收起"：只记当前 id 的展开态。</summary>
        public void SetExpanded(bool expanded)
        {
            _expanded = expanded;
        }

        private void ResetMounted()
        {
            _mountedKind = RichTextBodyKind.Empty;
            _mountedText = string.Empty;
            _mountedRich = null;
        }

        /// <param name="richContent">当前可挂的富文本内容；没有正文（null 或空 RichTextBlock）时传 null。</param>
        /// <param name="text">纯文本回退内容。</param>
        public RichTextBodyPlan Apply(object richContent, string text)
        {
            RichTextBodyKind kind;
            string planText;
            if (richContent != null)
            {
                kind = RichTextBodyKind.RichText;
                planText = string.Empty;
            }
            else if (!string.IsNullOrEmpty(text))
            {
                kind = RichTextBodyKind.PlainText;
                planText = text;
            }
            else
            {
                kind = RichTextBodyKind.Empty;
                planText = string.Empty;
            }

            var changed = kind != _mountedKind;
            if (!changed)
            {
                if (kind == RichTextBodyKind.RichText)
                {
                    changed = !ReferenceEquals(richContent, _mountedRich);
                }
                else if (kind == RichTextBodyKind.PlainText)
                {
                    changed = !string.Equals(planText, _mountedText, StringComparison.Ordinal);
                }
            }

            _mountedKind = kind;
            _mountedText = kind == RichTextBodyKind.PlainText ? planText : string.Empty;
            _mountedRich = richContent;

            return new RichTextBodyPlan
            {
                Kind = kind,
                Text = planText,
                Changed = changed,
            };
        }
    }
}

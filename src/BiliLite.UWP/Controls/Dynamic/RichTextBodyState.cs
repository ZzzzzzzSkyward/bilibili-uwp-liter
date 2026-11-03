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
    /// 状态只描述"当前 Parent.Children 里挂着什么"，因此控制层清空/挂载必须成对走 Apply，
    /// 不能绕过它改 Parent。
    /// </summary>
    public sealed class RichTextBodyState
    {
        private RichTextBodyKind _mountedKind = RichTextBodyKind.Empty;
        private string _mountedText = string.Empty;
        private object _mountedRich;

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

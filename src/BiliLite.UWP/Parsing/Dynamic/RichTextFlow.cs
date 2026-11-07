using System;
using System.Collections.Generic;
using System.Text;
using BiliLite.Modules;

namespace BiliLite.Parsing.Dynamic
{
    /// <summary>正文片段种类；渲染层按种类决定用 Run / Hyperlink / Emoji 容器。</summary>
    public enum RichTextSegmentKind
    {
        Text,
        LineBreak,
        Mention,
        Topic,
        Link,
        Emoji,
    }

    /// <summary>
    /// 表情语义档位。wire 的 size 只是档位标签（普通/大表情），不是像素倍数：
    /// 任何取值都落在 Normal / Large 两档，渲染尺寸由 body em 倍数派生，不能按 size 无限放大。
    /// </summary>
    public enum RichTextEmojiVariant
    {
        Normal = 0,
        Large = 1,
    }

    /// <summary>
    /// 正文里的一个可渲染片段。只带文本、目标与图片地址，不带任何 UIElement，
    /// 所以同一份卡片数据可以给 feed 与详情各自建一次可视图。
    /// </summary>
    public sealed class RichTextSegment
    {
        private RichTextSegment(RichTextSegmentKind kind)
        {
            Kind = kind;
        }

        public RichTextSegmentKind Kind { get; private set; }

        /// <summary>显示文本；Link 这里是标签，不是目标地址</summary>
        public string Text { get; private set; }

        /// <summary>跳转目标：Mention 是 mid，Topic/Link 是地址</summary>
        public string Target { get; private set; }

        /// <summary>Emoji 图片地址</summary>
        public string ImageUrl { get; private set; }

        /// <summary>Emoji 语义档位（wire 的 size），渲染层按档位取固定倍数，不把它当像素</summary>
        public int EmojiSize { get; private set; }

        /// <summary>Emoji 档位：普通 1.2 em，大表情 1.5 em；wire size 只决定这一档</summary>
        public RichTextEmojiVariant EmojiVariant { get; private set; }

        internal static RichTextSegment Plain(RichTextSegmentKind kind, string text)
        {
            return new RichTextSegment(kind) { Text = text ?? "" };
        }

        internal static RichTextSegment Targetted(RichTextSegmentKind kind, string text, string target)
        {
            return new RichTextSegment(kind) { Text = text ?? "", Target = target ?? "" };
        }

        internal static RichTextSegment Emoji(string text, string imageUrl, int size)
        {
            return new RichTextSegment(RichTextSegmentKind.Emoji)
            {
                Text = text ?? "",
                ImageUrl = imageUrl ?? "",
                EmojiSize = size,
                EmojiVariant = RichTextFlow.EmojiVariantFor(size),
            };
        }
    }

    /// <summary>
    /// 正文节点 → 渲染片段。句子流只在这里决定：
    /// 1. 按节点顺序追加，节点类型切换（文本↔@↔链接↔表情）不插入空格，也不另起段落。
    /// 2. CRLF 先归一成 LF，只有来源里的 LF 才产生 LineBreak（含连续空行）。
    /// 3. 只在源文本为空时用 fallbackText，不拿"无内容"补位；纯空白正文仍是空正文。
    /// 4. 缺目标（没有 mid/地址）的 @ 与链接降级成普通文本，不生成点不动的按钮。
    /// 5. 链接标签优先用来源给的标签，其次用目标地址；目标地址不进正文。
    /// 6. 不截断、不按字符数限长。
    /// 7. 表情的图片地址先归一（补全 // 前缀）再判可用；不可用的图按普通文本渲染，绝不建空图。
    /// </summary>
    public static class RichTextFlow
    {
        private const string NodeAt = "RICH_TEXT_NODE_TYPE_AT";
        private const string NodeTopic = "RICH_TEXT_NODE_TYPE_TOPIC";
        private const string NodeWeb = "RICH_TEXT_NODE_TYPE_WEB";

        /// <summary>wire 里大表情的最小 size；比它大也只当同一档，不做像素放大。</summary>
        public const int LargeEmojiWireSize = 2;

        /// <summary>普通表情相对正文的 em 倍数。</summary>
        public const double NormalEmojiEm = 1.2;

        /// <summary>大表情的上限 em 倍数；档位再多也不超过它。</summary>
        public const double LargeEmojiEm = 1.5;

        /// <summary>表情下沉到文本基线的 descender 比例（相对正文 em）。</summary>
        private const double EmojiDescenderRatio = 0.2;

        public static RichTextEmojiVariant EmojiVariantFor(int wireSize)
        {
            return wireSize >= LargeEmojiWireSize ? RichTextEmojiVariant.Large : RichTextEmojiVariant.Normal;
        }

        /// <summary>档位对应的 em 倍数。上限固定在 <see cref="LargeEmojiEm"/>，与 wire size 大小无关。</summary>
        public static double EmojiEm(RichTextEmojiVariant variant)
        {
            return variant == RichTextEmojiVariant.Large ? LargeEmojiEm : NormalEmojiEm;
        }

        /// <summary>表情边长：正文 em 的倍数取整。1.2 em 普通 / 1.5 em 大表情。</summary>
        public static double EmojiSide(double bodyFontSize, RichTextEmojiVariant variant)
        {
            if (bodyFontSize <= 0)
            {
                return 0;
            }
            return Math.Round(bodyFontSize * EmojiEm(variant), MidpointRounding.AwayFromZero);
        }

        /// <summary>表情下沉量：让图片底边落在文本基线上，而不是压在字上。</summary>
        public static double EmojiBaselineOffset(double bodyFontSize)
        {
            if (bodyFontSize <= 0)
            {
                return 0;
            }
            return Math.Round(bodyFontSize * EmojiDescenderRatio, MidpointRounding.AwayFromZero);
        }

        /// <summary>行高只在表情真正高过一行时才长，不预先拉大行距。</summary>
        public static double EmojiLineHeight(double bodyFontSize, double bodyLineHeight, RichTextEmojiVariant variant)
        {
            var needed = EmojiSide(bodyFontSize, variant) + EmojiBaselineOffset(bodyFontSize);
            return Math.Max(bodyLineHeight, Math.Ceiling(needed));
        }

        /// <summary>一段正文需要的行高：所有表情档位里最费空间的那个，没有表情时就是基准行高。</summary>
        public static double RequiredLineHeight(IEnumerable<RichTextSegment> segments, double bodyFontSize, double bodyLineHeight)
        {
            var lineHeight = bodyLineHeight;
            if (segments == null)
            {
                return lineHeight;
            }
            foreach (var segment in segments)
            {
                if (segment == null || segment.Kind != RichTextSegmentKind.Emoji)
                {
                    continue;
                }
                lineHeight = Math.Max(lineHeight, EmojiLineHeight(bodyFontSize, lineHeight, segment.EmojiVariant));
            }
            return lineHeight;
        }

        /// <summary>表情图地址归一成绝对地址；协议相对地址补 https:// 前缀（web 源给的就是 //host/path）。</summary>
        public static string NormalizeEmojiImageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return "";
            }
            var trimmed = url.Trim();
            if (trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                trimmed = "https:" + trimmed;
            }
            return IsUsableEmojiImage(trimmed) ? trimmed : "";
        }

        /// <summary>只有绝对 http/https 地址能当图片源：其余（相对、javascript:、data:）一律按文本渲染。</summary>
        public static bool IsUsableEmojiImage(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                return false;
            }
            return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }

        public static List<RichTextSegment> Build(IEnumerable<DynamicCardModel2024_node> nodes, string fallbackText)
        {
            var segments = new List<RichTextSegment>();
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    if (node == null)
                    {
                        continue;
                    }
                    AppendNode(segments, node);
                }
            }

            if (segments.Count == 0 && !string.IsNullOrWhiteSpace(fallbackText))
            {
                AppendText(segments, fallbackText);
            }

            return segments;
        }

        /// <summary>可选中/可复制的完整纯文本：表情用它的可读文本，换行还原成 LF。</summary>
        public static string PlainText(IEnumerable<RichTextSegment> segments)
        {
            if (segments == null)
            {
                return "";
            }
            var text = new StringBuilder();
            foreach (var segment in segments)
            {
                if (segment.Kind == RichTextSegmentKind.LineBreak)
                {
                    text.Append('\n');
                }
                else
                {
                    text.Append(segment.Text);
                }
            }
            return text.ToString();
        }

        private static void AppendNode(List<RichTextSegment> segments, DynamicCardModel2024_node node)
        {
            var text = node.text;
            if (string.IsNullOrEmpty(text))
            {
                text = node.orig_text;
            }

            var emoji = node.emoji;
            if (emoji != null)
            {
                // 表情只在图片真的可用时才是图片；否则退回可读文本（gRPC 的 icon_url 为空时这里拿到的就是 "[笑]"）。
                if (string.IsNullOrEmpty(text))
                {
                    text = emoji.text;
                }
                var icon = NormalizeEmojiImageUrl(emoji.icon_url);
                if (icon.Length > 0)
                {
                    segments.Add(RichTextSegment.Emoji(text, icon, emoji.size));
                    return;
                }
                AppendText(segments, text);
                return;
            }

            switch (node.type)
            {
                case NodeAt:
                    if (!string.IsNullOrEmpty(node.rid))
                    {
                        segments.Add(RichTextSegment.Targetted(RichTextSegmentKind.Mention, text, node.rid));
                        return;
                    }
                    break;
                case NodeTopic:
                    if (!string.IsNullOrEmpty(node.jump_url))
                    {
                        segments.Add(RichTextSegment.Targetted(RichTextSegmentKind.Topic, text, node.jump_url));
                        return;
                    }
                    break;
                case NodeWeb:
                    if (!string.IsNullOrEmpty(node.jump_url))
                    {
                        var label = string.IsNullOrEmpty(text) ? node.jump_url : text;
                        segments.Add(RichTextSegment.Targetted(RichTextSegmentKind.Link, label, node.jump_url));
                        return;
                    }
                    break;
            }

            AppendText(segments, text);
        }

        private static void AppendText(List<RichTextSegment> segments, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = normalized.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    segments.Add(RichTextSegment.Plain(RichTextSegmentKind.LineBreak, ""));
                }
                if (lines[i].Length > 0)
                {
                    segments.Add(RichTextSegment.Plain(RichTextSegmentKind.Text, lines[i]));
                }
            }
        }
    }
}

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
            };
        }
    }

    /// <summary>
    /// 正文节点 → 渲染片段。句子流只在这里决定：
    /// 1. 按节点顺序追加，节点类型切换（文本↔@↔链接↔表情）不插入空格，也不另起段落。
    /// 2. CRLF 先归一成 LF，只有来源里的 LF 才产生 LineBreak（含连续空行）。
    /// 3. 只在源文本为空时用 fallbackText，不拿"无内容"补位。
    /// 4. 缺目标（没有 mid/地址）的 @ 与链接降级成普通文本，不生成点不动的按钮。
    /// 5. 链接标签优先用来源给的标签，其次用目标地址；目标地址不进正文。
    /// 6. 不截断、不按字符数限长。
    /// </summary>
    public static class RichTextFlow
    {
        private const string NodeAt = "RICH_TEXT_NODE_TYPE_AT";
        private const string NodeTopic = "RICH_TEXT_NODE_TYPE_TOPIC";
        private const string NodeWeb = "RICH_TEXT_NODE_TYPE_WEB";

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

            if (segments.Count == 0)
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
            if (emoji != null && !string.IsNullOrEmpty(emoji.icon_url))
            {
                // 表情的 icon 不可用时按普通文本处理：gRPC 的 icon_url 为空时这里拿到的就是 "[笑]"
                if (string.IsNullOrEmpty(text))
                {
                    text = emoji.text;
                }
                segments.Add(RichTextSegment.Emoji(text, emoji.icon_url, emoji.size));
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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BiliLite.Helpers
{
    /// <summary>
    /// 评论富文本的纯逻辑部分：把已经转义过的文本拼成 XamlReader 能吃的一串标记。
    /// 这里不引用任何 UWP 类型，plan/tests/cm1 直接跑这个文件。
    /// </summary>
    public static class CommentRichText
    {
        /// <summary>
        /// 评论图片地址的唯一规范化入口。接口里会混进 http:// 和 // 开头的地址，
        /// 这两种在 ImageSource 上都会静默加载失败，所以统一成 https:// 并去掉首尾空白。
        /// </summary>
        public static string NormalizeImageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return "";
            }

            var trimmed = url.Trim();
            if (trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                return "https:" + trimmed;
            }

            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                return "https://" + trimmed.Substring("http://".Length);
            }

            return trimmed;
        }

        /// <summary>
        /// 按 链接 → av/BV/cv → 表情 的顺序拼标记。
        /// 表情必须最后拼：表情标记自带 https 地址，先拼会被链接正则改写成 HyperlinkButton，
        /// 图片节点就没了。
        /// </summary>
        public static string BuildMarkup(string input, JObject emote, Func<string, string> urlDisplay,
            Action<string> onSkippedEmote = null)
        {
            if (input == null)
            {
                return null;
            }

            var text = InsertLinks(input, urlDisplay);
            text = InsertVideoIds(text);
            text = InsertEmotes(text, emote, onSkippedEmote);
            return text;
        }

        /// <summary>
        /// 生成一条表情的图片标记；表情数据不完整时返回 null，让调用方保留 [名称] 原文。
        /// </summary>
        public static string BuildEmoteMarkup(JObject emote)
        {
            if (emote == null)
            {
                return null;
            }

            var url = NormalizeImageUrl(emote.Value<string>("url"));
            if (url.Length == 0)
            {
                return null;
            }

            var meta = emote["meta"] as JObject;
            var size = meta?["size"];
            if (size == null || size.Type == JTokenType.Null)
            {
                return null;
            }

            var sizeValue = 0;
            if (!int.TryParse(size.ToString(), out sizeValue))
            {
                return null;
            }

            var side = sizeValue == 1 ? "20" : "36";
            return string.Format(
                @"<InlineUIContainer><Border  Margin=""0 -4 4 -4""><Image Source=""{0}"" Width=""{1}"" Height=""{1}"" /></Border></InlineUIContainer>",
                EscapeAttribute(url), side);
        }

        private static string InsertEmotes(string input, JObject emote, Action<string> onSkippedEmote)
        {
            if (emote == null)
            {
                return input;
            }

            MatchCollection mc = Regex.Matches(input, @"\[.*?\]");
            foreach (Match item in mc)
            {
                var token = item.Groups[0].Value;
                var entry = emote[token] as JObject;
                if (entry == null)
                {
                    continue;
                }

                var markup = BuildEmoteMarkup(entry);
                if (markup == null)
                {
                    if (onSkippedEmote != null)
                    {
                        onSkippedEmote(token);
                    }

                    continue;
                }

                input = input.Replace(token, markup);
            }

            return input;
        }

        private static string InsertVideoIds(string input)
        {
            List<string> keyword = new List<string>();
            //如果是链接就不处理了
            if (Regex.IsMatch(input, @"/[aAbBcC][vV]([a-zA-Z0-9]+)"))
            {
                return input;
            }

            MatchCollection av = Regex.Matches(input, @"[aA][vV](\d+)");
            foreach (Match item in av)
            {
                if (keyword.Contains(item.Groups[0].Value))
                {
                    continue;
                }
                keyword.Add(item.Groups[0].Value);
                input = input.Replace(item.Groups[0].Value, BuildVideoIdMarkup(item.Groups[0].Value, "bilibili://video/" + item.Groups[0].Value));
            }

            MatchCollection bv = Regex.Matches(input, @"[bB][vV]([a-zA-Z0-9]{8,})");
            foreach (Match item in bv)
            {
                if (keyword.Contains(item.Groups[0].Value))
                {
                    continue;
                }
                keyword.Add(item.Groups[0].Value);
                input = input.Replace(item.Groups[0].Value, BuildVideoIdMarkup(item.Groups[0].Value, "bilibili://video/" + item.Groups[0].Value));
            }

            MatchCollection cv = Regex.Matches(input, @"[cC][vV](\d+)");
            foreach (Match item in cv)
            {
                if (keyword.Contains(item.Groups[0].Value))
                {
                    continue;
                }
                keyword.Add(item.Groups[0].Value);
                input = input.Replace(item.Groups[0].Value, BuildVideoIdMarkup(item.Groups[0].Value, "bilibili://article/" + item.Groups[1].Value));
            }

            return input;
        }

        private static string BuildVideoIdMarkup(string text, string commandParameter)
        {
            return @"<InlineUIContainer><HyperlinkButton Command=""{Binding LaunchUrlCommand}""  IsEnabled=""True"" Margin=""0 -4 0 -4"" Padding=""0"" " +
                string.Format(@" CommandParameter=""{1}"" ><TextBlock>{0}</TextBlock></HyperlinkButton></InlineUIContainer>", text, commandParameter);
        }

        private static string InsertLinks(string input, Func<string, string> urlDisplay)
        {
            List<string> keyword = new List<string>();
            MatchCollection url = Regex.Matches(input, @"(https?|ftp|file)://[-A-Za-z0-9+&@#/%?=~_|!:,.;]+[-A-Za-z0-9+&@#/%=~_|]");
            foreach (Match item in url)
            {
                var u = item.Groups[0].Value;
                if (keyword.Contains(u))
                {
                    continue;
                }
                keyword.Add(u);
                var display = urlDisplay == null ? u : urlDisplay(u);
                var data = @"<InlineUIContainer><HyperlinkButton x:Name=""btn"" Command=""{Binding LaunchUrlCommand}""  IsEnabled=""True"" Margin=""0 -4 0 -4"" Padding=""0"" " +
                    string.Format(@" CommandParameter=""{0}"" ><TextBlock>🔗{1}</TextBlock></HyperlinkButton></InlineUIContainer>", u, display);
                input = input.Replace(u, data);
            }

            return input;
        }

        private static string EscapeAttribute(string value)
        {
            return value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}

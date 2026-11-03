using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BiliLite.Modules;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.Dynamic
{
    /// <summary>
    /// 两种来源共用的标量读写：web JSON 与 protobuf 映射都要"拿不到就当没有、别把失败读成 0/false"这套规则，
    /// 所以放在解析层，两个 Mapper 都用 `using static` 引用，调用点不区分来源。
    /// </summary>
    public static class WireScalars
    {
        /// <summary>
        /// 正文节点类型归一：web 的 RICH_TEXT_NODE_TYPE_* 与 gRPC DescType 的枚举名（ToString 出来是
        /// Aite/Topic/Web/Emoji 这类不带 desc_type_ 前缀的名字）都落到同一套名字上，渲染层只认这一套。
        /// 先剥掉两边各自的固定前缀再精确匹配，不用子串猜：名字里碰巧含 "at" 的类型不该被当成 @。
        /// 没见过的类型返回 TEXT，正文照常可读。
        /// </summary>
        public static string MapDescriptionType(string type)
        {
            const string richPrefix = "RICH_TEXT_NODE_TYPE_";
            const string descPrefix = "DESC_TYPE_";

            var key = (type ?? "").Trim().ToUpperInvariant();
            if (key.StartsWith(richPrefix, StringComparison.Ordinal))
            {
                key = key.Substring(richPrefix.Length);
            }
            else if (key.StartsWith(descPrefix, StringComparison.Ordinal))
            {
                key = key.Substring(descPrefix.Length);
            }

            switch (key)
            {
                case "AT":
                case "AITE":
                    return "RICH_TEXT_NODE_TYPE_AT";
                case "TOPIC":
                    return "RICH_TEXT_NODE_TYPE_TOPIC";
                case "WEB":
                    return "RICH_TEXT_NODE_TYPE_WEB";
                case "EMOJI":
                    return "RICH_TEXT_NODE_TYPE_EMOJI";
                case "VOTE":
                    return "RICH_TEXT_NODE_TYPE_VOTE";
                case "LOTTERY":
                    return "RICH_TEXT_NODE_TYPE_LOTTERY";
                case "GOODS":
                    return "RICH_TEXT_NODE_TYPE_GOODS";
                case "BV":
                    return "RICH_TEXT_NODE_TYPE_BV";
                case "AV":
                    return "RICH_TEXT_NODE_TYPE_AV";
                case "CV":
                    return "RICH_TEXT_NODE_TYPE_CV";
                case "VC":
                    return "RICH_TEXT_NODE_TYPE_VC";
                case "USER":
                    return "RICH_TEXT_NODE_TYPE_USER";
                case "TAOBAO":
                    return "RICH_TEXT_NODE_TYPE_TAOBAO";
                case "MAIL":
                    return "RICH_TEXT_NODE_TYPE_MAIL";
                case "OGV_SEASON":
                    return "RICH_TEXT_NODE_TYPE_OGV_SEASON";
                case "OGV_EP":
                    return "RICH_TEXT_NODE_TYPE_OGV_EP";
                case "SEARCH_WORD":
                    return "RICH_TEXT_NODE_TYPE_SEARCH_WORD";
                default:
                    return "RICH_TEXT_NODE_TYPE_TEXT";
            }
        }

        public static int ToInt(long value)
        {
            return value > int.MaxValue ? int.MaxValue : value < int.MinValue ? int.MinValue : (int)value;
        }
        public static int ToInt(string value)
        {
            int result;
            return int.TryParse(value, out result) ? result : 0;
        }

        public static long ToLong(string value)
        {
            long result;
            return long.TryParse(value, out result) ? result : 0;
        }

        public static bool ToBool(JToken token)
        {
            return token != null && token.Type == JTokenType.Boolean && token.Value<bool>();
        }

        public static List<string> ReadStringArray(JArray source)
        {
            if (source == null)
            {
                return new List<string>();
            }
            return source
                .Select(x => x?.ToString())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();
        }

        /// <summary>
        /// 几段可选说明拼成一行显示：丢掉 null 与空串，中间恰好一个空格，不产生前导/尾随空格。
        /// 服务端把同一句填进多个槽位时（预约卡的 desc1/desc2/desc3 常见）只保留第一次出现的，
        /// 重复不是信息，且不能让标签变成 "x x x"。只用于显示拼接，来源文本本身不动。
        /// </summary>
        public static string JoinText(params string[] parts)
        {
            if (parts == null)
            {
                return "";
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var kept = new List<string>(parts.Length);
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part) || !seen.Add(part))
                {
                    continue;
                }
                kept.Add(part);
            }
            return string.Join(" ", kept);
        }

        /// <summary>把 "475.6万" / "1.2亿" 这类紧凑计数还原成整数；解析不出来就是 0。</summary>
        public static int ParseCount(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            double multiplier = 1;
            var digits = text;
            if (text.EndsWith("万", StringComparison.Ordinal))
            {
                multiplier = 10000;
                digits = text.Substring(0, text.Length - 1);
            }
            else if (text.EndsWith("亿", StringComparison.Ordinal))
            {
                multiplier = 100000000;
                digits = text.Substring(0, text.Length - 1);
            }

            double value;
            if (!double.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return 0;
            }

            var count = value * multiplier;
            return count > int.MaxValue ? int.MaxValue : (int)count;
        }

        /// <summary>把 "24:02" / "1:02:03" 还原成秒数；解析不出来就是 0。</summary>
        public static int ParseDuration(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            var seconds = 0;
            foreach (var part in text.Split(':'))
            {
                int value;
                if (!int.TryParse(part, out value))
                {
                    return 0;
                }
                seconds = seconds * 60 + value;
            }
            return seconds;
        }

        /// <summary>web 卡片的跳转地址是协议相对的（//www.bilibili.com/...），补成完整地址。</summary>
        public static string NormalizeWebUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return "";
            }
            return url.StartsWith("//", StringComparison.Ordinal) ? "https:" + url : url;
        }

        public static DynamicCardModel2024 MissingCard(string dynamicId)
        {
            return new DynamicCardModel2024
            {
                type = "DYNAMIC_TYPE_NONE",
                id_str = dynamicId ?? "",
                basic = new DynamicCardModel2024_basic
                {
                    rid_str = dynamicId ?? "",
                    comment_id_str = dynamicId ?? "",
                },
                modules = new DynamicCardModel2024_modules
                {
                    module_dynamic = new DynamicCardModel2024_dynamic
                    {
                        desc = new DynamicCardModel2024_desc
                        {
                            text = "原动态已不可见",
                            rich_text_nodes = new List<DynamicCardModel2024_node>(),
                        },
                        major = new DynamicCardModel2024_major(),
                    }
                },
                visible = true,
            };
        }    }
}

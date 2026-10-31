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
        public static string MapDescriptionType(string type)
        {
            var normalized = (type ?? "").ToLowerInvariant();
            if (normalized.Contains("aite") || normalized.Contains("at")) return "RICH_TEXT_NODE_TYPE_AT";
            if (normalized.Contains("topic")) return "RICH_TEXT_NODE_TYPE_TOPIC";
            if (normalized.Contains("web") || normalized.Contains("link")) return "RICH_TEXT_NODE_TYPE_WEB";
            return "RICH_TEXT_NODE_TYPE_TEXT";
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

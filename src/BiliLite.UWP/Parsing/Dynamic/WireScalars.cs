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
        /// <summary>正文纯文本节点的归一类型名。</summary>
        public const string NodeText = "RICH_TEXT_NODE_TYPE_TEXT";

        /// <summary>
        /// 正文节点类型归一：web 的 RICH_TEXT_NODE_TYPE_* 与 gRPC DescType 的枚举名（proto 的
        /// desc_type_ogv_season 在 C# 里生成为 OgvSeason：PascalCase、没有下划线）都落到同一套名字上，
        /// 渲染层只认这一套。先剥掉两边各自的固定前缀再精确查表，不做子串匹配：名字里碰巧含 "at"
        /// 的类型不该被当成 @。表里没有的类型原样返回，正文照常当普通文本可读。
        /// </summary>
        public static string MapDescriptionType(string type)
        {
            const string richPrefix = "RICH_TEXT_NODE_TYPE_";
            const string descPrefix = "DESC_TYPE_";

            var raw = (type ?? "").Trim();
            if (raw.Length == 0)
            {
                return NodeText;
            }

            var key = raw;
            if (key.StartsWith(richPrefix, StringComparison.OrdinalIgnoreCase))
            {
                key = key.Substring(richPrefix.Length);
            }
            else if (key.StartsWith(descPrefix, StringComparison.OrdinalIgnoreCase))
            {
                key = key.Substring(descPrefix.Length);
            }
            key = key.Replace("_", "").ToUpperInvariant();

            switch (key)
            {
                case "TEXT":
                case "NONE":
                    return NodeText;
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
                case "OGVSEASON":
                    return "RICH_TEXT_NODE_TYPE_OGV_SEASON";
                case "OGVEP":
                    return "RICH_TEXT_NODE_TYPE_OGV_EP";
                case "SEARCHWORD":
                    return "RICH_TEXT_NODE_TYPE_SEARCH_WORD";
                default:
                    // 未知语义保留来源类型名：渲染层不认识的种类一律当文本，信息不丢也不猜成 @
                    return raw;
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

        /// <summary>网页来源的已知基址：根相对地址（/video/BV...）用它补全。</summary>
        public const string BilibiliWebBase = "https://www.bilibili.com";

        /// <summary>
        /// 地址只归一一次，不猜：web 卡片的跳转地址常见协议相对形式（//www.bilibili.com/...），
        /// 按已知的网页基址补成完整地址。
        /// </summary>
        public static string NormalizeWebUrl(string url)
        {
            return NormalizeWebUrl(url, BilibiliWebBase);
        }

        /// <summary>
        /// 1. 协议相对 //host/path 补成 HTTPS；
        /// 2. http(s) 与 bilibili:// 这类完整地址原样保留：应用内地址不是网页，不能拼成 https:bilibili://；
        /// 3. 根相对 /path 只在有已知来源基址时补全，没有基址就没有目的地；
        /// 4. 其余（无 scheme、也不以 / 开头）不是可点击目标。
        /// 返回空串表示"没有目的地"：调用方保留可读文本，而不是生成一个点不动的链接。
        /// </summary>
        public static string NormalizeWebUrl(string url, string sourceBase)
        {
            var value = (url ?? "").Trim();
            if (value.Length == 0)
            {
                return "";
            }

            if (value.StartsWith("//", StringComparison.Ordinal))
            {
                // 只认 //host[/path]：///evil 这种不是主机名，补成 https:///evil 就是个坏地址
                var authority = value.Substring(2);
                return authority.Length == 0 || authority[0] == '/'
                    ? ""
                    : "https://" + authority;
            }

            if (HasScheme(value))
            {
                return value;
            }

            if (value[0] == '/')
            {
                var baseUrl = HttpBase(sourceBase);
                if (baseUrl.Length == 0)
                {
                    return "";
                }
                var path = value.TrimStart('/');
                return path.Length == 0 ? "" : baseUrl + "/" + path;
            }

            // 既没有 scheme 也不以 / 开头：无法判定地址，"https:/" 之类的拼接一律不做
            return "";
        }

        /// <summary>完整地址才有 scheme：字母开头，后面是字母/数字/+/-/.，并以 ':' 结束。</summary>
        private static bool HasScheme(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c == ':')
                {
                    return i > 0;
                }
                if (i == 0 ? !char.IsLetter(c) : !(char.IsLetterOrDigit(c) || c == '+' || c == '-' || c == '.'))
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>已知来源基址只接受 http(s) 绝对地址，其余（空、相对、应用内 scheme）视为没有基址。</summary>
        private static string HttpBase(string sourceBase)
        {
            var value = (sourceBase ?? "").Trim().TrimEnd('/');
            if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
            return "";
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
        }
    }

    /// <summary>
    /// 评论目标：定位一条评论要用 (oid, type) 这一对。type 是评论接口的类型编号
    /// （1 视频 / 5 小视频 / 11 图片 / 12 专栏 / 14 音频 / 17 动态 / 19 歌单），
    /// 与显示类型不是一回事：OPUS 按图片/文字分类，也不决定评论命名空间。
    /// 两个来源的映射都用它，所以放在解析层，调用点不区分来源。
    /// </summary>
    public sealed class DynamicCommentTarget
    {
        /// <summary>评论接口认可的 oid 类型编号；不在这里的数字不是一对可用的目标。</summary>
        private static readonly HashSet<int> KnownTypes = new HashSet<int> { 1, 5, 11, 12, 14, 17, 19 };

        private DynamicCommentTarget(string oid, int type)
        {
            Oid = oid;
            Type = type;
        }

        public string Oid { get; private set; }
        public int Type { get; private set; }

        /// <summary>
        /// 适配器给出的一对；oid 为空或类型不在已知编号里时返回 null ——
        /// 调用方这时沿用按显示类型的老映射，而不是拿一个占位目标去请求评论。
        /// </summary>
        public static DynamicCommentTarget From(string oid, int type)
        {
            if (string.IsNullOrEmpty(oid) || !KnownTypes.Contains(type))
            {
                return null;
            }
            return new DynamicCommentTarget(oid, type);
        }
    }
}

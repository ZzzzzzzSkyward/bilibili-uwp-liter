using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace BiliLite.Services
{
    /// <summary>
    /// 一次响应正文的"形状"：路径 → 类型名。形状而不是正文才能长期留在日志里，
    /// 也才能在字段变了类型时直接指出是哪一层。不引用任何 UWP 类型。
    /// </summary>
    public static class ApiShape
    {
        /// <summary>树的深度上限：再深就不是响应契约，而是正文内容了</summary>
        public const int MaxDepth = 8;

        /// <summary>每个对象最多记多少个键：异常大的字典只需要看出前几个就够了</summary>
        public const int MaxKeys = 64;

        /// <summary>数组里最多记几种元素类型</summary>
        public const int MaxArrayShapes = 4;

        public static SortedDictionary<string, string> Collect(JToken token)
        {
            var shape = new SortedDictionary<string, string>(StringComparer.Ordinal);
            Walk(token, "$", 0, shape);
            return shape;
        }

        /// <summary>两份形状的差异：新增、消失、类型改变，按路径排序。空列表表示没有漂移。</summary>
        public static List<string> Diff(IDictionary<string, string> baseline, IDictionary<string, string> current)
        {
            var changes = new List<string>();
            var before = baseline ?? new Dictionary<string, string>();
            var now = current ?? new Dictionary<string, string>();
            var paths = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var key in before.Keys)
            {
                paths.Add(key);
            }
            foreach (var key in now.Keys)
            {
                paths.Add(key);
            }
            foreach (var path in paths)
            {
                string oldType;
                string newType;
                var hadOld = before.TryGetValue(path, out oldType);
                var hasNew = now.TryGetValue(path, out newType);
                if (hadOld && !hasNew)
                {
                    changes.Add("-" + path + ":" + oldType);
                }
                else if (!hadOld && hasNew)
                {
                    changes.Add("+" + path + ":" + newType);
                }
                else if (!string.Equals(oldType, newType, StringComparison.Ordinal))
                {
                    changes.Add(path + ":" + oldType + "→" + newType);
                }
            }
            return changes;
        }

        /// <summary>一行日志用的紧凑形状，长响应只留前若干层路径。</summary>
        public static string Render(IDictionary<string, string> shape, int maxEntries = 40)
        {
            if (shape == null || shape.Count == 0)
            {
                return "";
            }
            var builder = new StringBuilder();
            var written = 0;
            foreach (var pair in shape.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                if (written >= maxEntries)
                {
                    builder.Append(",…+").Append(shape.Count - maxEntries);
                    break;
                }
                if (builder.Length > 0)
                {
                    builder.Append(',');
                }
                builder.Append(pair.Key).Append(':').Append(pair.Value);
                written++;
            }
            return builder.ToString();
        }

        private static void Walk(JToken token, string path, int depth, SortedDictionary<string, string> shape)
        {
            shape[path] = Describe(token);

            if (depth >= MaxDepth)
            {
                return;
            }
            var obj = token as JObject;
            if (obj != null)
            {
                var written = 0;
                foreach (var property in obj.Properties())
                {
                    if (written >= MaxKeys)
                    {
                        return;
                    }
                    Walk(property.Value, path + "." + property.Name, depth + 1, shape);
                    written++;
                }
                return;
            }
            var array = token as JArray;
            if (array != null)
            {
                // 数组按"元素类型各取一个样本"展开，形状里用 [] 表示元素层
                var seen = new List<string>();
                foreach (var element in array)
                {
                    var type = Describe(element);
                    if (seen.Contains(type))
                    {
                        continue;
                    }
                    seen.Add(type);
                    if (seen.Count > MaxArrayShapes)
                    {
                        return;
                    }
                    Walk(element, path + "[]", depth + 1, shape);
                }
            }
        }

        /// <summary>JSON 类型名：整数与浮点都记 number，null 记 null。</summary>
        public static string Describe(JToken token)
        {
            if (token == null)
            {
                return "null";
            }
            switch (token.Type)
            {
                case JTokenType.Object:
                    return "object";
                case JTokenType.Array:
                    return "array";
                case JTokenType.Integer:
                case JTokenType.Float:
                    return "number";
                case JTokenType.Boolean:
                    return "boolean";
                case JTokenType.String:
                    return "string";
                case JTokenType.Null:
                    return "null";
                case JTokenType.Undefined:
                    return "undefined";
                default:
                    return token.Type.ToString().ToLowerInvariant();
            }
        }
    }
}

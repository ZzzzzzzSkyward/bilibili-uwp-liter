using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BiliLite.Modules
{
    /// <summary>
    /// 弹幕屏蔽规则的合并与校验，只用 BCL 类型，便于脱离 UWP 单独验证。
    /// 合并结果始终是一个独立快照：调用方随后清空可观察集合不会影响它。
    /// </summary>
    public static class DanmuFilterRules
    {
        /// <summary>
        /// 合并本地规则与外来规则：本地在前并原样保留，外来只补本地没有的项。
        /// </summary>
        public static List<string> Merge(IEnumerable<string> existing, IEnumerable<string> incoming)
        {
            return Merge(existing, incoming, null);
        }

        /// <summary>
        /// 同上，acceptIncoming 只决定外来规则是否参与合并（例如正则有效性），
        /// 本地已存规则不因这个过滤器被丢弃。
        /// </summary>
        public static List<string> Merge(IEnumerable<string> existing, IEnumerable<string> incoming, Func<string, bool> acceptIncoming)
        {
            var merged = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (existing != null)
            {
                foreach (var item in existing)
                {
                    if (string.IsNullOrWhiteSpace(item))
                        continue;
                    if (seen.Add(item))
                        merged.Add(item);
                }
            }
            if (incoming != null)
            {
                foreach (var item in incoming)
                {
                    if (string.IsNullOrWhiteSpace(item))
                        continue;
                    if (acceptIncoming != null && !acceptIncoming(item))
                        continue;
                    if (seen.Add(item))
                        merged.Add(item);
                }
            }
            return merged;
        }

        /// <summary>
        /// 校验正则表达式，失败时给出可以直接显示在输入框旁的原因。
        /// </summary>
        public static bool TryValidateRegex(string pattern, out string error)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                error = "正则表达式不能为空";
                return false;
            }
            try
            {
                new Regex(pattern);
            }
            catch (ArgumentException ex)
            {
                error = "正则表达式无效：" + ex.Message;
                return false;
            }
            error = null;
            return true;
        }

        /// <summary>
        /// 去重后加入集合，返回是否真的加入（空值和已存在都返回 false）。
        /// </summary>
        public static bool AddUnique(ICollection<string> target, string value)
        {
            if (target == null || string.IsNullOrWhiteSpace(value))
                return false;
            foreach (var item in target)
            {
                if (string.Equals(item, value, StringComparison.Ordinal))
                    return false;
            }
            target.Add(value);
            return true;
        }
    }
}

using System.Collections.Generic;
using System.Linq;

namespace BiliLite.Models
{
    /// <summary>浏览器只有一份 cookie jar：登录写进去的令牌，刷新链换个 host 也得能读回来。</summary>
    public static class CookieOrigins
    {
        /// <summary>读取顺序：根域在最前，其余按登录链路展开，第一个带值的解析结果就是答案。</summary>
        public static readonly IReadOnlyList<string> All = new[]
        {
            "https://bilibili.com/",
            "https://www.bilibili.com/",
            "https://passport.bilibili.com/",
            "https://api.bilibili.com/"
        };

        /// <summary>写入只覆盖根域和 www：子域主机按域继承，重复写只会给同名 cookie 造副本。</summary>
        public static readonly IReadOnlyList<string> WriteHosts = new[] { "bilibili.com", "www.bilibili.com" };

        /// <summary>空串和纯空白都当作没记住，免得用空值盖掉设置里还活着的会话。</summary>
        public static string FirstValue(IEnumerable<string> values)
        {
            if (values == null) return "";
            var found = values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            return found ?? "";
        }
    }
}

using System.Collections.Generic;
using Newtonsoft.Json;

namespace BiliLite.Models.Api.MediaList
{
    /// <summary>
    /// /x/v2/history/toview 的 data。整份列表一次返回（ps=100），没有游标，也没有 has_more。
    /// aid 按数字收：线上是数字，数字字符串 Newtonsoft 也能落到 long；页面模型里的 string 由 mapper 定。
    /// </summary>
    public class WatchLaterListDto
    {
        /// <summary>这一份稍后再看；缺失/null = 空，不是形状错误</summary>
        [JsonProperty("list")]
        public List<WatchLaterItemDto> List { get; set; }
    }

    public class WatchLaterItemDto
    {
        [JsonProperty("aid")]
        public long Aid { get; set; }

        [JsonProperty("videos")]
        public int Videos { get; set; }

        [JsonProperty("tname")]
        public string Tname { get; set; }

        [JsonProperty("pic")]
        public string Pic { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }

        [JsonProperty("dynamic")]
        public string Dynamic { get; set; }

        [JsonProperty("cid")]
        public long Cid { get; set; }

        [JsonProperty("add_at")]
        public long AddAt { get; set; }

        [JsonProperty("owner")]
        public WatchLaterOwnerDto Owner { get; set; }

        [JsonProperty("progress")]
        public int Progress { get; set; }

        /// <summary>秒</summary>
        [JsonProperty("duration")]
        public int Duration { get; set; }
    }

    public class WatchLaterOwnerDto
    {
        [JsonProperty("face")]
        public string Face { get; set; }

        [JsonProperty("mid")]
        public long Mid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}

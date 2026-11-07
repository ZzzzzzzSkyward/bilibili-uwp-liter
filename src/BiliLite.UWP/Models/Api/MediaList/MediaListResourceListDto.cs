using System.Collections.Generic;
using Newtonsoft.Json;

namespace BiliLite.Models.Api.MediaList
{
    /// <summary>
    /// /x/v2/medialist/resource/list 的 data（签名与未登录两个取法返回同一个形状）。
    /// 宽度照线上保留：id 是 long，total_count 是可缺失的 int；has_more / total_count 用可空类型，
    /// 因为"服务端没报"和"报了 0"要分开，归一化交给 MediaListResourceListMapper。
    /// </summary>
    public class MediaListResourceListDto
    {
        [JsonProperty("media_list")]
        public List<MediaListResourceItemDto> MediaList { get; set; }

        /// <summary>还有下一页；缺失按 false（今天就是这样：没报就当到头）</summary>
        [JsonProperty("has_more")]
        public bool? HasMore { get; set; }

        /// <summary>服务端报的总数；缺失 = 未知，不能拿已加载行数顶上</summary>
        [JsonProperty("total_count")]
        public int? TotalCount { get; set; }
    }

    /// <summary>media_list 里的一条：资源 ID 就是播放列表里那个视频的 aid</summary>
    public class MediaListResourceItemDto
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("cover")]
        public string Cover { get; set; }

        [JsonProperty("upper")]
        public MediaListResourceUpperDto Upper { get; set; }

        /// <summary>秒</summary>
        [JsonProperty("duration")]
        public long Duration { get; set; }
    }

    public class MediaListResourceUpperDto
    {
        /// <summary>线上键是 name；旧模型靠 Newtonsoft 不区分大小写才读得到</summary>
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}

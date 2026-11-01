using System.Collections.Generic;
using Newtonsoft.Json;

namespace BiliLite.Models.Api.Video
{
    /// <summary>
    /// medialist/gateway/base/created 的 data：这个视频能进哪些"我创建的收藏夹"。
    /// id / fid / mid 按字符串保留，和收藏夹页面模型一致。
    /// </summary>
    public class FavoriteCreatedListDto
    {
        [JsonProperty("count")]
        public int Count { get; set; }

        /// <summary>收藏夹列表；缺失/null = 一个都没有，不是形状错误</summary>
        [JsonProperty("list")]
        public List<FavoriteCreatedListItemDto> List { get; set; }
    }

    public class FavoriteCreatedListItemDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("fid")]
        public string Fid { get; set; }

        [JsonProperty("mid")]
        public string Mid { get; set; }

        [JsonProperty("attr")]
        public int Attr { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("cover")]
        public string Cover { get; set; }

        [JsonProperty("intro")]
        public string Intro { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("media_count")]
        public int MediaCount { get; set; }

        [JsonProperty("fav_state")]
        public int FavState { get; set; }

        [JsonProperty("like_state")]
        public int LikeState { get; set; }
    }
}

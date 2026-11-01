using System.Collections.Generic;
using Newtonsoft.Json;

namespace BiliLite.Models.Api.MediaList
{
    /// <summary>
    /// /x/v3/fav/resource/list 的 data（收藏夹当成播放队列）。
    /// mid / fid / id 一律按字符串保留：这一路页面模型本来就是 string，不在这里转数字。
    /// </summary>
    public class FavoriteResourceListDto
    {
        [JsonProperty("info")]
        public FavoriteResourceInfoDto Info { get; set; }

        /// <summary>这一页的视频；缺失/null = 这一页没有视频，不是形状错误</summary>
        [JsonProperty("medias")]
        public List<FavoriteResourceItemDto> Medias { get; set; }

        /// <summary>还有下一页；今天页面用 media_count 判翻页，这个值只原样带上，不改路由</summary>
        [JsonProperty("has_more")]
        public bool? HasMore { get; set; }
    }

    public class FavoriteResourceInfoDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("fid")]
        public string Fid { get; set; }

        [JsonProperty("mid")]
        public string Mid { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("cover")]
        public string Cover { get; set; }

        [JsonProperty("attr")]
        public int Attr { get; set; }

        [JsonProperty("fav_state")]
        public int FavState { get; set; }

        [JsonProperty("like_state")]
        public int LikeState { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("media_count")]
        public int MediaCount { get; set; }

        [JsonProperty("upper")]
        public FavoriteResourceUpperDto Upper { get; set; }
    }

    public class FavoriteResourceItemDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("cover")]
        public string Cover { get; set; }

        [JsonProperty("intro")]
        public string Intro { get; set; }

        [JsonProperty("upper")]
        public FavoriteResourceUpperDto Upper { get; set; }

        [JsonProperty("cnt_info")]
        public FavoriteResourceStatDto CntInfo { get; set; }
    }

    public class FavoriteResourceUpperDto
    {
        [JsonProperty("face")]
        public string Face { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("mid")]
        public string Mid { get; set; }
    }

    public class FavoriteResourceStatDto
    {
        [JsonProperty("coin")]
        public int Coin { get; set; }

        [JsonProperty("collect")]
        public int Collect { get; set; }

        [JsonProperty("danmaku")]
        public int Danmaku { get; set; }

        [JsonProperty("play")]
        public int Play { get; set; }

        [JsonProperty("reply")]
        public int Reply { get; set; }

        [JsonProperty("share")]
        public int Share { get; set; }
    }
}

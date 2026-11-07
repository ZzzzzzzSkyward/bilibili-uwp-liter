using System.Collections.Generic;
using Newtonsoft.Json;

namespace BiliLite.Models.Api.Season.Web
{
    /// <summary>
    /// pgc/view/web/ep/list 的 result。宽度照线上实测保留：season_id 是 int，section[].id 是 long，
    /// 归一化交给 WebSeasonEpListMapper，这里不改形状。
    /// </summary>
    public class WebSeasonEpListDto
    {
        [JsonProperty("season_id")]
        public int SeasonId { get; set; }

        [JsonProperty("season_title")]
        public string SeasonTitle { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("origin_name")]
        public string OriginName { get; set; }

        [JsonProperty("cover")]
        public string Cover { get; set; }

        [JsonProperty("square_cover")]
        public string SquareCover { get; set; }

        [JsonProperty("evaluate")]
        public string Evaluate { get; set; }

        [JsonProperty("alias")]
        public string Alias { get; set; }

        [JsonProperty("badge")]
        public string Badge { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("subtitle")]
        public string Subtitle { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("short_link")]
        public string ShortLink { get; set; }

        [JsonProperty("media_id")]
        public int MediaId { get; set; }

        [JsonProperty("mode")]
        public int Mode { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        /// <summary>服务端只拿它做提示，不决定要不要发布；缺失按 0</summary>
        [JsonProperty("total")]
        public int? Total { get; set; }

        [JsonProperty("up_info")]
        public WebSeasonUpInfoDto UpInfo { get; set; }

        [JsonProperty("areas")]
        public List<WebSeasonAreaDto> Areas { get; set; }

        [JsonProperty("episodes")]
        public List<WebSeasonEpisodeDto> Episodes { get; set; }

        [JsonProperty("seasons")]
        public List<WebSeasonItemDto> Seasons { get; set; }

        [JsonProperty("section")]
        public List<WebSeasonSectionDto> Section { get; set; }

        [JsonProperty("rating")]
        public WebSeasonRatingDto Rating { get; set; }

        [JsonProperty("publish")]
        public WebSeasonPublishDto Publish { get; set; }

        [JsonProperty("stat")]
        public WebSeasonStatDto Stat { get; set; }

        [JsonProperty("user_status")]
        public WebSeasonUserStatusDto UserStatus { get; set; }

        [JsonProperty("payment")]
        public WebSeasonPaymentDto Payment { get; set; }
    }

    public class WebSeasonEpisodeDto
    {
        /// <summary>投稿 aid，线上是数字；应用模型仍按字符串携带</summary>
        [JsonProperty("aid")]
        public long? Aid { get; set; }

        [JsonProperty("bvid")]
        public string Bvid { get; set; }

        [JsonProperty("cid")]
        public long? Cid { get; set; }

        /// <summary>正片里就是 ep_id，互动/番外走 ep_id 字段，两者可能只有一个</summary>
        [JsonProperty("id")]
        public int? Id { get; set; }

        [JsonProperty("ep_id")]
        public int? EpId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("long_title")]
        public string LongTitle { get; set; }

        [JsonProperty("index")]
        public string Index { get; set; }

        [JsonProperty("index_title")]
        public string IndexTitle { get; set; }

        [JsonProperty("cover")]
        public string Cover { get; set; }

        [JsonProperty("badge")]
        public string Badge { get; set; }

        [JsonProperty("badge_type")]
        public int? BadgeType { get; set; }

        [JsonProperty("episode_status")]
        public int? EpisodeStatus { get; set; }

        /// <summary>页面用 status != 2 判断大会员集；有些响应只给 episode_status</summary>
        [JsonProperty("status")]
        public int? Status { get; set; }
    }

    public class WebSeasonSectionDto
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>花絮/预告分区 id，线上比 season_id 宽</summary>
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("episodes")]
        public List<WebSeasonEpisodeDto> Episodes { get; set; }
    }

    public class WebSeasonItemDto
    {
        [JsonProperty("season_id")]
        public int SeasonId { get; set; }

        [JsonProperty("cover")]
        public string Cover { get; set; }

        [JsonProperty("season_title")]
        public string SeasonTitle { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }

    public class WebSeasonUpInfoDto
    {
        [JsonProperty("mid")]
        public long Mid { get; set; }

        [JsonProperty("uname")]
        public string Uname { get; set; }
    }

    public class WebSeasonAreaDto
    {
        [JsonProperty("id")]
        public int? Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class WebSeasonRatingDto
    {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("score")]
        public double Score { get; set; }
    }

    public class WebSeasonPublishDto
    {
        [JsonProperty("is_finish")]
        public int IsFinish { get; set; }

        [JsonProperty("is_started")]
        public int IsStarted { get; set; }

        [JsonProperty("weekday")]
        public int Weekday { get; set; }

        [JsonProperty("pub_time")]
        public string PubTime { get; set; }

        [JsonProperty("pub_time_show")]
        public string PubTimeShow { get; set; }

        [JsonProperty("release_date_show")]
        public string ReleaseDateShow { get; set; }

        [JsonProperty("time_length_show")]
        public string TimeLengthShow { get; set; }
    }

    public class WebSeasonStatDto
    {
        [JsonProperty("coins")]
        public int Coins { get; set; }

        [JsonProperty("danmakus")]
        public int Danmakus { get; set; }

        [JsonProperty("favorites")]
        public int Favorites { get; set; }

        /// <summary>播放/追番数是展示文本（1.2万），保持字符串</summary>
        [JsonProperty("followers")]
        public string Followers { get; set; }

        [JsonProperty("play")]
        public string Play { get; set; }

        [JsonProperty("reply")]
        public int Reply { get; set; }

        [JsonProperty("share")]
        public int Share { get; set; }

        [JsonProperty("views")]
        public int Views { get; set; }
    }

    public class WebSeasonUserStatusDto
    {
        [JsonProperty("follow")]
        public int Follow { get; set; }

        [JsonProperty("follow_bubble")]
        public int FollowBubble { get; set; }

        [JsonProperty("follow_status")]
        public int FollowStatus { get; set; }

        [JsonProperty("pay")]
        public int Pay { get; set; }

        [JsonProperty("vip")]
        public int Vip { get; set; }

        [JsonProperty("sponsor")]
        public int Sponsor { get; set; }

        [JsonProperty("vip_frozen")]
        public int VipFrozen { get; set; }

        [JsonProperty("progress")]
        public WebSeasonUserStatusProgressDto Progress { get; set; }
    }

    public class WebSeasonUserStatusProgressDto
    {
        [JsonProperty("last_ep_index")]
        public string LastEpIndex { get; set; }

        [JsonProperty("last_ep_id")]
        public int LastEpId { get; set; }

        [JsonProperty("last_time")]
        public int LastTime { get; set; }
    }

    public class WebSeasonPaymentDto
    {
        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("tv_price")]
        public string TvPrice { get; set; }

        [JsonProperty("dialog")]
        public WebSeasonPaymentDialogDto Dialog { get; set; }
    }

    public class WebSeasonPaymentDialogDto
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }
    }
}

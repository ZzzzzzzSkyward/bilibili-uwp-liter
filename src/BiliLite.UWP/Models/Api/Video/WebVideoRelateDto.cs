using Newtonsoft.Json;

namespace BiliLite.Models.Api.Video
{
    /// <summary>
    /// x/web-interface/archive/related 的 data[] 一项（推荐/相关视频）。
    /// aid 按字符串保留：页面跳转和播放队列都按 string 传 id。
    /// </summary>
    public class WebVideoRelateDto
    {
        [JsonProperty("aid")]
        public string Aid { get; set; }

        [JsonProperty("pic")]
        public string Pic { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("owner")]
        public WebVideoOwnerDto Owner { get; set; }

        [JsonProperty("stat")]
        public WebVideoStatDto Stat { get; set; }
    }
}

using Newtonsoft.Json;

namespace BiliLite.Models.Api.Video
{
    /// <summary>
    /// x/web-interface/archive/coins 的 data：详情页只取 multiply（已投币枚数）。
    /// archive/has/like 没有 DTO：它的 data 是 0/1 数字，不是对象。
    /// </summary>
    public class WebVideoCoinStateDto
    {
        [JsonProperty("multiply")]
        public int Multiply { get; set; }
    }

    /// <summary>
    /// x/v2/fav/video/favoured 的 data。
    /// </summary>
    public class WebVideoFavouredStateDto
    {
        [JsonProperty("favoured")]
        public bool Favoured { get; set; }
    }
}

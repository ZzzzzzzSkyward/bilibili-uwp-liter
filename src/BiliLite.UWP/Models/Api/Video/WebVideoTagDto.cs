using Newtonsoft.Json;

namespace BiliLite.Models.Api.Video
{
    /// <summary>
    /// x/tag/archive/tags 的 data[] 一项。
    /// </summary>
    public class WebVideoTagDto
    {
        [JsonProperty("tag_id")]
        public int TagId { get; set; }

        [JsonProperty("tag_name")]
        public string TagName { get; set; }
    }
}

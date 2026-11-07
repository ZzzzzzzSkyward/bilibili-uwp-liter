using Newtonsoft.Json;

namespace BiliLite.Models.Api.Video
{
    /// <summary>
    /// x/relation/stat 的 data：详情页只取粉丝数（详情响应里没有 owner_ext）。
    /// </summary>
    public class WebRelationStatDto
    {
        [JsonProperty("follower")]
        public int Follower { get; set; }
    }

    /// <summary>
    /// x/relation 的 data：详情页只取 attribute（0=未关注, 2=已关注, 6=已互关, 128=已拉黑）。
    /// </summary>
    public class WebRelationDto
    {
        [JsonProperty("attribute")]
        public int Attribute { get; set; }
    }
}

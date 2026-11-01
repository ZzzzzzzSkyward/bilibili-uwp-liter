using Newtonsoft.Json;

namespace BiliLite.Models.Api.Comment
{
    /// <summary>
    /// x/v2/reply/{action,add,del} 的整份响应：只有 code/message，没有 data 载荷。
    /// 解码在 Parsing/Comment/WebCommentActionParser.cs（code 先按 token 类型查，再落这个类型）。
    /// </summary>
    public class WebCommentActionResultDto
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        /// <summary>成功时线上是 "0"；拒绝时是给人看的那句话</summary>
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}

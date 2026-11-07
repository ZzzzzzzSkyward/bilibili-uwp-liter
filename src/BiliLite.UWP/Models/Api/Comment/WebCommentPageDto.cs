using System.Collections.Generic;
using Newtonsoft.Json;

namespace BiliLite.Models.Api.Comment
{
    /// <summary>
    /// x/v2/reply/wbi/main 的 data（游标源）。只收这一层真正要用的字段：这一页的行、两条置顶来源、
    /// 游标源自己的 next_offset。data.page（acount/count/num/size）没人读，不收。
    /// 宽窄照线上：评论 id / oid / mid 都是数字，member 里的 mid 是字符串，见 WebCommentReplyDto。
    /// </summary>
    public class WebCommentPageDto
    {
        /// <summary>这一页的评论；缺失/null = 空页，不是形状错误</summary>
        [JsonProperty("replies")]
        public List<WebCommentReplyDto> Replies { get; set; }

        /// <summary>另一种置顶来源（通常只有第一页给）；缺失/null = 没有这类置顶</summary>
        [JsonProperty("top_replies")]
        public List<WebCommentReplyDto> TopReplies { get; set; }

        /// <summary>UP 主；这里只有 upper.top 是置顶评论，upper 本身不是</summary>
        [JsonProperty("upper")]
        public WebCommentUpperDto Upper { get; set; }

        /// <summary>游标源自己的翻页游标；pn 源（x/v2/reply）没有这个字段</summary>
        [JsonProperty("cursor")]
        public WebCommentCursorDto Cursor { get; set; }
    }

    /// <summary>upper：mid 是真正的 UP 主 id，top 是那条置顶评论</summary>
    public class WebCommentUpperDto
    {
        [JsonProperty("mid")]
        public long Mid { get; set; }

        [JsonProperty("top")]
        public WebCommentReplyDto Top { get; set; }
    }

    /// <summary>cursor：这一层只认 pagination_reply；is_end 和服务端统计没人读</summary>
    public class WebCommentCursorDto
    {
        [JsonProperty("pagination_reply")]
        public WebCommentPaginationReplyDto PaginationReply { get; set; }
    }

    public class WebCommentPaginationReplyDto
    {
        /// <summary>下一页要原样回传的 offset；线上可能给数字，Newtonsoft 落到 string。空串按"没有下一页"归一</summary>
        [JsonProperty("next_offset")]
        public string NextOffset { get; set; }
    }
}

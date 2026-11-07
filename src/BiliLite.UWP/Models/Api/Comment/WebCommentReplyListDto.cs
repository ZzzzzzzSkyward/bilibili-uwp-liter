using System.Collections.Generic;
using Newtonsoft.Json;

namespace BiliLite.Models.Api.Comment
{
    /// <summary>
    /// x/v2/reply/reply 的 data（楼中楼）。只收 replies：data.page、data.upper 这一路都不读，
    /// 翻页用控件自己的 loadpage（pn），没有游标。
    /// </summary>
    public class WebCommentReplyListDto
    {
        /// <summary>这一页的回复；缺失/null = 这一楼没有更多回复，不是形状错误</summary>
        [JsonProperty("replies")]
        public List<WebCommentReplyDto> Replies { get; set; }
    }
}

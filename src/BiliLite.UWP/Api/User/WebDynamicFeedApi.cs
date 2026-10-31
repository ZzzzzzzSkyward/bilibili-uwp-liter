using BiliLite.Models;

namespace BiliLite.Api.User
{
    /// <summary>
    /// web 聚合动态流的请求构造（x/polymer/web-dynamic/v1/feed/all）。
    /// 与专栏的 feed/nav 是两个端点，名字按端点取，不再借用 ArticleV2。
    /// </summary>
    public static class WebDynamicFeedApi
    {
        /// <summary>
        /// type 取 all/video/article；offset 是上一页响应的 data.offset，null 表示第一页。
        /// timezone_offset 是客户端时区（分钟，东八区为 -480），服务端用它算 pub_time 文案，与 web 端一致。
        /// </summary>
        public static ApiModel CreateFeedRequest(int page = 1, string offset = null, string type = "all")
        {
            var api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = "https://api.bilibili.com/x/polymer/web-dynamic/v1/feed/all",
                parameter = $"type={type}&page={page}&timezone_offset=-480" +
                    "&features=itemOpusStyle,listOnlyfans,opusBigCover,onlyfansVote,decorationCard,onlyfansAssetsV2,forwardListHidden,ugcDelete,onlyfansQaCard,commentsNewVersion,avatarAutoTheme,sunflowerStyle,cardsEnhance,eva3CardOpus,eva3CardVideo,eva3CardComment",
                need_cookie = true,
            };
            if (offset != null)
            {
                api.parameter += $"&offset={offset}";
            }
            return api;
        }
    }
}

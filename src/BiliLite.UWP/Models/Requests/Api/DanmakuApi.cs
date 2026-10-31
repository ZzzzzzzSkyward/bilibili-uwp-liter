using BiliLite.Api;

namespace BiliLite.Models.Requests.Api
{
    public class DanmakuApi : BaseApi
    {
        /// <summary>Builds the Bilibili endpoint request to like or unlike a danmaku.</summary>
        public ApiModel Like(string dmid, string oid, int op)
        {
            return new ApiModel
            {
                method = RestSharp.Method.Post,
                baseUrl = ApiBaseUrl + "/x/v2/dm/thumbup/add",
                body = "&dmid=" + dmid + "&oid=" + oid + "&op=" + op + "&csrf=" + Cookie.GetCSRF(),
                need_cookie = true
            };
        }
    }
}

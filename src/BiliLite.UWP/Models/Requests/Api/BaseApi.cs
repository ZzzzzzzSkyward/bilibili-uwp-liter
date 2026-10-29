using System.Collections.Generic;
using BiliLite.Api;

namespace BiliLite.Models.Requests.Api
{
    /// <summary>Shared target configuration and request helpers for incrementally migrated endpoints.</summary>
    public abstract class BaseApi
    {
        protected global::CookieService Cookie => ApiHelper.Cookie;
        protected string ApiBaseUrl => ApiHelper.API_BASE_URL;
        protected ApiKeyInfo AndroidKey => ApiHelper.AndroidKey;

        protected ApiModel CreateGet(string url, string parameters = null,
            IDictionary<string, string> headers = null, bool needCookie = false)
        {
            return new ApiModel
            {
                method = RestSharp.Method.Get,
                baseUrl = url,
                parameter = parameters,
                headers = headers,
                need_cookie = needCookie
            };
        }
    }
}

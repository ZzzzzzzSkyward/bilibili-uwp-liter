// Minimal request metadata/platform seams. All form construction is real CommentApi code.
// ApiHelper.GetCSRF(bool) returns the raw CookieService token (ApiHelper.cs:70);
// the compatibility boolean does not prepend a form key in the product.
namespace RestSharp
{
    public enum Method { Get, Post }
}

namespace BiliLite.Api
{
    public sealed class ApiModel
    {
        public RestSharp.Method method { get; set; }
        public string baseUrl { get; set; }
        public string parameter { get; set; }
        public string body { get; set; }
        public bool need_cookie { get; set; }
    }

    public static class ApiHelper
    {
        public const string API_BASE_URL = "https://api.bilibili.com";
        public const string api2 = "/x/v2";
        public const string replyreply = "/reply/reply";
        public const string replycomment = "/reply/add";
        public const string comment = "/reply/add";
        public static string RawCsrf { get; set; }
        public static string GetCSRF(bool withParameter = false) { return RawCsrf; }
        public static System.Threading.Tasks.Task<string> GetWbiSign(string parameters)
        {
            throw new System.NotSupportedException("No signing/network operation is used by this request probe.");
        }
    }
}

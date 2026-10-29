using BiliLite.Helpers;

namespace BiliLite.Api.Home
{
    public class CinemaAPI
    {
        public ApiModel CinemaHome()
        {
            var baseUrl = SettingHelper.GetValue("BiliLiteWebApiBaseUrl", "http://www.xiaotao2333.top:50233/");
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"{baseUrl}api/cinema/home"
            };
            return api;
        }
        public ApiModel CinemaFallMore(int wid, long cursor = 0)
        {
            var baseUrl = SettingHelper.GetValue("BiliLiteWebApiBaseUrl", "http://www.xiaotao2333.top:50233/");
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"{baseUrl}api/cinema/falls",
                parameter = $"wid={wid}&cursor={cursor}"
            };
            return api;
        }
     
    }
}

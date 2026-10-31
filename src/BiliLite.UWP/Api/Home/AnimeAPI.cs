using BiliLite.Helpers;

namespace BiliLite.Api.Home
{

    public class AnimeAPI
    {

        public ApiModel BangumiHome()
        {
            var baseUrl = SettingHelper.GetValue("BiliLiteWebApiBaseUrl", "http://www.xiaotao2333.top:50233/");
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"{baseUrl}api/anime/bangumi"
            };
            return api;
        }
        public ApiModel GuochuangHome()
        {
            var baseUrl = SettingHelper.GetValue("BiliLiteWebApiBaseUrl", "http://www.xiaotao2333.top:50233/");
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"{baseUrl}api/anime/guochuang"
            };
            return api;
        }
        public ApiModel Timeline(int type)
        {
            var baseUrl = SettingHelper.GetValue("BiliLiteWebApiBaseUrl", "http://www.xiaotao2333.top:50233/");
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"{baseUrl}api/anime/timeline",
                parameter = "type=" + type
            };
            return api;
        }
        public ApiModel AnimeFallMore(int wid, long cursor = 0)
        {
            var baseUrl = SettingHelper.GetValue("BiliLiteWebApiBaseUrl", "http://www.xiaotao2333.top:50233/");
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"{baseUrl}api/anime/bangumiFalls",
                parameter = $"wid={wid}&cursor={cursor}"
            };
            return api;
        }

       
    }
}

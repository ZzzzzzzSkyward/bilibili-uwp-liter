namespace BiliLite.Api
{
    public class RegionAPI
    {
        public ApiModel Regions()
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"https://app.bilibili.com{ApiHelper.api2}/region/index",
                parameter = ApiHelper.MustParameter(ApiHelper.AndroidKey, true)
            };
            api.parameter += ApiHelper.GetSign(api.parameter, ApiHelper.AndroidKey);
            return api;
        }

        public ApiModel RegionDynamic(long rid)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"https://app.bilibili.com{ApiHelper.api2}/region/dynamic",
                parameter = ApiHelper.MustParameter(ApiHelper.AndroidKey, false) + $"&rid={rid}"
            };
            api.parameter += ApiHelper.GetSign(api.parameter, ApiHelper.AndroidKey);
            return api;
        }

        public ApiModel RegionDynamic(long rid, string next_aid)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"https://app.biliapi.net{ApiHelper.api2}/region/dynamic/list",
                parameter = ApiHelper.MustParameter(ApiHelper.AndroidKey, false) + $"&rid={rid}&ctime={next_aid}&pull=false"
            };
            api.parameter += ApiHelper.GetSign(api.parameter, ApiHelper.AndroidKey);
            return api;
        }

        public ApiModel RegionChildDynamic(long rid, long tag_id = 0)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"https://app.biliapi.net{ApiHelper.api2}/region/dynamic/child",
                parameter = ApiHelper.MustParameter(ApiHelper.AndroidKey, false) + $"&rid={rid}&tag_id={tag_id}&pull=true"
            };
            api.parameter += ApiHelper.GetSign(api.parameter, ApiHelper.AndroidKey);
            return api;
        }

        public ApiModel RegionChildDynamic(long rid, string next, int tag_id = 0)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"https://app.bilibili.com{ApiHelper.api2}/region/dynamic/child/list",
                parameter = ApiHelper.MustParameter(ApiHelper.AndroidKey, false) + $"&rid={rid}&tag_id={tag_id}&pull=false&ctime={next}"
            };
            api.parameter += ApiHelper.GetSign(api.parameter, ApiHelper.AndroidKey);
            return api;
        }

        public ApiModel RegionChildList(long rid, string order, int page, int tag_id = 0)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"https://app.biliapi.net{ApiHelper.api2}/region/show/child/list",
                parameter = ApiHelper.MustParameter(ApiHelper.AndroidKey, false) + $"&order={order}&pn={page}&ps=20&rid={rid}&tag_id={tag_id}"
            };
            api.parameter += ApiHelper.GetSign(api.parameter, ApiHelper.AndroidKey);
            return api;
        }
    }
}
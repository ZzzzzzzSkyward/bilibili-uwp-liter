using System;

namespace BiliLite.Api
{
    public class VideoAPI
    {
        public ApiModel Detail(string id,bool isbvid)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"https://app.bilibili.com/x/v2/view",
                parameter = ApiHelper.MustParameter(ApiHelper.AndroidKey, true) + $"&{(isbvid ? "bvid=" : "aid=")}{id}&plat=0"
            };
            api.parameter += ApiHelper.GetSign(api.parameter, ApiHelper.AndroidKey);
            return api;
        }

        public ApiModel DetailWebInterface(string id, bool isBvId)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = "https://api.bilibili.com/x/web-interface/view",
                parameter = $"&{(isBvId ? "bvid=" : "aid=")}{id}",
                need_cookie = true,
            };
            return api;
        }

        public ApiModel RelatesWebInterface(string id, bool isBvId)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = "https://api.bilibili.com/x/web-interface/archive/related",
                parameter = $"&{(isBvId ? "bvid=" : "aid=")}{id}"
            };
            return api;
        }

        /// <summary>
        /// 查询视频是否被点赞(Web端详情接口不含此状态, 需登录Cookie)
        /// </summary>
        public ApiModel HasLike(string aid, string bvid)
        {
            return new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = "https://api.bilibili.com/x/web-interface/archive/has/like",
                parameter = $"&aid={aid}&bvid={bvid}",
                need_cookie = true,
            };
        }

        /// <summary>
        /// 查询视频是否被投币(Web端详情接口不含此状态, 需登录Cookie)
        /// </summary>
        public ApiModel HasCoin(string aid, string bvid)
        {
            return new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = "https://api.bilibili.com/x/web-interface/archive/coins",
                parameter = $"&aid={aid}&bvid={bvid}",
                need_cookie = true,
            };
        }

        /// <summary>
        /// 查询视频是否被收藏(Web端详情接口不含此状态, 需登录Cookie)
        /// </summary>
        public ApiModel HasFav(string aid, string bvid)
        {
            return new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = "https://api.bilibili.com/x/v2/fav/video/favoured",
                parameter = $"&aid={aid}&bvid={bvid}",
                need_cookie = true,
            };
        }

        /// <summary>
        /// 查询用户粉丝数(Web端详情接口不含owner_ext, 需登录Cookie)
        /// </summary>
        public ApiModel RelationStat(string mid)
        {
            return new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = "https://api.bilibili.com/x/relation/stat",
                parameter = $"&vmid={mid}",
                need_cookie = true,
            };
        }

        public ApiModel DetailProxy(string id, bool isbvid)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = "https://app.bilibili.com/x/v2/view",
                parameter = ApiHelper.MustParameter(ApiHelper.AndroidKey, true) + $"&{(isbvid ? "bvid=" : "aid=")}{id}&plat=0"
            };
            api.parameter += ApiHelper.GetSign(api.parameter, ApiHelper.AndroidKey);
            var apiUrl = Uri.EscapeDataString(api.url);
            api.baseUrl = "https://biliproxy.iill.moe/app.php";
            api.parameter = "url=" + apiUrl;
            return api;
        }
        /// <summary>
        ///点赞
        /// </summary>
        /// <param name="dislike"> 当前dislike状态</param>
        /// <param name="like">当前like状态</param>
        /// <returns></returns>
        public ApiModel Like(string aid, int dislike,int like)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Post,
                baseUrl = $"https://app.bilibili.com{ApiHelper.api2}/view/like",
                body = ApiHelper.MustParameter(ApiHelper.AndroidKey, true) + $"&aid={aid}&dislike={dislike}&from=7&like={like}"
            };
            api.body += ApiHelper.GetSign(api.body, ApiHelper.AndroidKey);
            return api;
        }
        /// <summary>
        ///点赞
        /// </summary>
        /// <param name="dislike"> 当前dislike状态</param>
        /// <param name="like">当前like状态</param>
        /// <returns></returns>
        public ApiModel Dislike(string aid, int dislike, int like)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Post,
                baseUrl = $"https://app.biliapi.net{ApiHelper.api2}/view/dislike",
                body = ApiHelper.MustParameter(ApiHelper.AndroidKey, true) + $"&aid={aid}&dislike={dislike}&from=7&like={like}"
            };
            api.body+= ApiHelper.GetSign(api.body, ApiHelper.AndroidKey);
            return api;
        }

        /// <summary>
        ///一键三连
        /// </summary>
        /// <returns></returns>
        public ApiModel Triple(string aid)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Post,
                baseUrl = $"https://app.bilibili.com{ApiHelper.api2}/view/like/triple",
                body = ApiHelper.MustParameter(ApiHelper.AndroidKey, true) + $"&aid={aid}"
            };
            api.body += ApiHelper.GetSign(api.body, ApiHelper.AndroidKey);
            return api;
        }

        public ApiModel Coin(string aid, int num)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Post,
                baseUrl = $"https://app.biliapi.net{ApiHelper.api2}/view/coin/add",
                body = ApiHelper.MustParameter(ApiHelper.AndroidKey, true) + $"&aid={aid}&multiply={num}&platform=android&select_like=0"
            };
            api.body += ApiHelper.GetSign(api.body, ApiHelper.AndroidKey);
            return api;
        }
        /// <summary>
        /// 关注
        /// </summary>
        /// <param name="mid">用户ID</param>
        /// <param name="mode">1为关注，2为取消关注</param>
        /// <returns></returns>
        public ApiModel Attention(string mid,string mode)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Post,
                baseUrl = $"{ApiHelper.API_BASE_URL}/x/relation/modify",
                body = ApiHelper.MustParameter(ApiHelper.AndroidKey, true) + $"&act={mode}&fid={mid}&re_src=32"
            };
            api.body += ApiHelper.GetSign(api.body, ApiHelper.AndroidKey);
            return api;
        }

        public ApiModel Tags(string aid)
        {
            return new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"{ApiHelper.API_BASE_URL}/x/tag/archive/tags",
                parameter = $"&aid={aid}",
                need_cookie = true,
            };
        }

        public ApiModel GetMediaList(string mediaListId, string lastAid, int typeId = 1, int pagesize = 20)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = "https://api.bilibili.com/x/v2/medialist/resource/list",
                parameter = $"{ApiHelper.MustParameter(ApiHelper.AndroidKey, true)}&type={typeId}&biz_id={mediaListId}&oid={lastAid}&otype=2&ps={pagesize}&direction=false&desc=true&sort_field=1&tid=0&with_current=false",
            };
            api.parameter += ApiHelper.GetSign(api.parameter, ApiHelper.AndroidKey);
            return api;
        }

        public ApiModel GetMediaListNotLogin(string mediaListId, string lastAid, int typeId = 1, int pagesize = 20)
        {
            return new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = "https://api.bilibili.com/x/v2/medialist/resource/list",
                parameter = $"type={typeId}&biz_id={mediaListId}&oid={lastAid}&otype=2&ps={pagesize}&direction=false&desc=true&sort_field=1&tid=0&with_current=false",
            };
        }


    }
}

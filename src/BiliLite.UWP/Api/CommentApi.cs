﻿using Newtonsoft.Json;
using System;
namespace BiliLite.Api
{
    public class CommentApi
    {
        
        public enum CommentType
        {
            Video=1,
            Dynamic=17,
            Article=12,
            Photo =11,
            MiniVideo=5,
            SongMenu=19,
            Song=14
        }
        public enum CommentSort
        {
            New = 0,
            Hot =1,        
        }
        /// <summary>
        /// 读取评论
        /// </summary>
        /// <param name="oid">ID</param>
        /// <param name="sort">1=最新，2=最热</param>
        /// <param name="pn">页数</param>
        /// <param name="type">类型，1=视频，17=动态，11=图片，5=小视频，19=歌单，14=歌曲</param>
        /// <param name="ps">每页数量</param>
        /// <returns></returns>
        public ApiModel Comment(string oid, CommentSort sort,int pn, int type, int ps = 30,string offsetStr=null)
        {
            string csrf = ApiHelper.GetCSRF(true);
            var mode = sort == CommentSort.Hot ? 3 : 2;
            var pagination = new
            {
                offset = offsetStr
            };
            var paginationStr = JsonConvert.SerializeObject(pagination);
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"{ApiHelper.API_BASE_URL}{ApiHelper.api2}{ApiHelper.readcomment}",
                parameter = $"oid={oid}&ps={ps}&mode={mode}&type={type}&csrf={csrf}&pagination_str={paginationStr}",
                need_cookie = true
            };
            //api.parameter += ApiHelper.GetSign(api.parameter, ApiHelper.AndroidKey);
            return api;
        }
        public ApiModel Reply(string oid,string root, int pn, int type, int ps = 30)
        {
            string csrf = ApiHelper.GetCSRF(true);
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"{ApiHelper.API_BASE_URL}{ApiHelper.api2}{ApiHelper.replyreply}",
                parameter = $"oid={oid}&plat=2&pn={pn}&ps={ps}&root={root}&type={type}{csrf}",
                need_cookie=true
            };
            //api.parameter += ApiHelper.GetSign(api.parameter, ApiHelper.AndroidKey);
            return api;
        }

        public ApiModel Like(string oid, string root, int action, int type)
        {
            string csrf = ApiHelper.GetCSRF(true);
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Post,
                baseUrl = $"{ApiHelper.API_BASE_URL}{ApiHelper.api2}/reply/action",
                body =  $"&oid={oid}&rpid={root}&action={action}&type={type}{csrf}",
                need_cookie = true
            };
            //api.body += ApiHelper.GetSign(api.body, ApiHelper.AndroidKey);
            return api;
        }

        public ApiModel ReplyComment(string oid, string root, string parent, string message, int type)
        {
            string csrf = ApiHelper.GetCSRF(true);
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Post,
                baseUrl = $"{ApiHelper.API_BASE_URL}{ApiHelper.api2}{ApiHelper.replycomment}",
                body =  $"&oid={oid}&root={root}&parent={parent}&type={type}&message={message}{csrf}"
            };
           // api.body += ApiHelper.GetSign(api.body, ApiHelper.AndroidKey);
            return api;
        }
        public ApiModel DeleteComment(string oid, string rpid, int type)
        {
            string csrf = ApiHelper.GetCSRF();
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Post,
                baseUrl = $"{ApiHelper.API_BASE_URL}{ApiHelper.api2}/reply/del",
                body =  $"&oid={oid}&rpid={rpid}&type={type}&csrf={csrf}"
            };
           // api.body += ApiHelper.GetSign(api.body, ApiHelper.AndroidKey);
            return api;
        }
        public ApiModel AddComment(string oid, CommentType type,string message)
        {
            string csrf = ApiHelper.GetCSRF(true);
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Post,
                baseUrl = $"{ApiHelper.API_BASE_URL}{ApiHelper.api2}{ApiHelper.comment}",
                body = $"&oid={oid}&type={(int)type}&message={Uri.EscapeDataString(message)}{csrf}"
            };
            //api.body += ApiHelper.GetSign(api.body, ApiHelper.AndroidKey);
            return api;
        }
    }
}

using BiliLite.Helpers;
using BiliLite.Models;
using BiliLite.Modules.Player.Playurl;
using System;
using System.Collections.Generic;

namespace BiliLite.Api
{
    public class PlayerAPI
    {
        public static Dictionary<string, string> VideoHeader = new Dictionary<string, string>{
            {"Referer","https://www.bilibili.com/" }
        };
        public ApiModel VideoPlayUrl(string aid, string cid, int qn,bool dash,bool proxy=false,string area="")
        {
            var baseUrl = ApiHelper.API_BASE_URL;

            if (proxy)
            {
                baseUrl = Utils.ChooseProxyServer(area);
            }
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"{baseUrl}/x/player/wbi/playurl",
                parameter = $"avid={aid}&cid={cid}&qn={qn}&type=&otype=json",
                need_cookie = !ApiHelper.need_refresh_cookie,
                headers = VideoHeader,
            };
            var fnval = 0;
            if (dash)
            {
                fnval += 16;//dash
                fnval += 128;//4k as fourk
                var CodecMode = (PlayUrlCodecMode)SettingHelper.GetValue<int>(SettingHelper.Player.DEFAULT_VIDEO_TYPE, 1);
                if (CodecMode == PlayUrlCodecMode.DASH_AV1)
                {
                    fnval += 2048;//av1
                }
            }
            else
            {
                fnval += 1;//mp4
            }
            api.parameter += $"&fourk=1&fnver=0&fnval={fnval}";
            if (SettingHelper.Account.Logined)
            {
                api.parameter += $"&access_key={SettingHelper.Account.AccessKey}&mid={SettingHelper.Account.Profile.mid}";
            }
            if (proxy)
            {
                api.parameter += $"&area={area}";
            }
            else
            {
                //api.parameter += ApiHelper.GetSign(api.parameter,ApiHelper.AndroidKey);
                api.parameter += ApiHelper.GetWbiSign(api.parameter);
            }
            return api;
        }

       

        public ApiModel SeasonPlayUrl(string aid, string cid, string ep_id, int qn,int season_type, bool dash, bool proxy = false, string area = "")
        {
            var baseUrl = ApiHelper.API_BASE_URL;
            if (proxy)
            {
                baseUrl = Utils.ChooseProxyServer(area);
            }
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"{baseUrl}/pgc/player/web/playurl",
                parameter = $"appkey={ApiHelper.WebVideoKey.Appkey}&cid={cid}&ep_id={ep_id}&qn={qn}&type=&otype=json&module=bangumi&season_type={season_type}"
            };
            if (SettingHelper.Account.Logined)
            {
                api.parameter += $"&access_key={SettingHelper.Account.AccessKey}&mid={SettingHelper.Account.Profile.mid}";
            }
            if (dash)
            {
                api.parameter += "&fourk=1&fnver=0&fnval=4048";
            }
            
            api.parameter += ApiHelper.GetSign(api.parameter, ApiHelper.WebVideoKey);
            if (proxy)
            {
                api.parameter += $"&area={area}";
            }
            return api;
        }
        public ApiModel SeasonAndroidPlayUrl(string aid, string cid, int qn, int season_type, bool dash)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"{ApiHelper.API_BASE_URL}/pgc/player/web/playurl",
                parameter = $"appkey={ApiHelper.AndroidKey.Appkey}&cid={cid}&qn={qn}&type=&otype=json&module=bangumi&season_type={season_type}"
            };
            if (SettingHelper.Account.Logined)
            {
                api.parameter += $"&access_key={SettingHelper.Account.AccessKey}&mid={SettingHelper.Account.Profile.mid}";
            }
            if (dash)
            {
                api.parameter += "&fourk=1&fnver=0&fnval=4048";
            }
            api.parameter += ApiHelper.GetSign(api.parameter, ApiHelper.WebVideoKey);
            return api;
        }

        public ApiModel LivePlayUrl(string cid, int qn=0)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"https://api.live.bilibili.com/room/v1/Room/playUrl",
                parameter =$"cid={cid}&qn={qn}&platform=web"
            };
            //api.parameter += ApiHelper.GetSign(api.parameter, ApiHelper.AndroidVideoKey);
            return api;
        }
        public class LiveInfo {
            public const int flv = 0;
            public const int ts = 1;
            public const int mp4 = 2;
        }
        public class ProtocolInfo {
            public const int http_stream = 0;
            public const int http_hls = 1;
        }
        public class CodecInfo {
            public const int AVC = 0;
            public const int HEVC = 1;
        }
        public List<int> videofmt = new List<int> { LiveInfo.flv, LiveInfo.mp4 };//flv and ts/m3u8 is not supported by ffmpeg 
        public List<int> codecfmt = new List<int> { CodecInfo.AVC, CodecInfo.HEVC };
        public List<int> protocolfmt = new List<int> { ProtocolInfo.http_stream, ProtocolInfo.http_hls };
        public string JoinFmt(List<int> fmts)
        {
            return string.Join(",", fmts);
        }
        public string protocolstr
        {
            get
            {
                return JoinFmt(protocolfmt);
            }
        }
        public string videostr
        {
            get
            {
                return JoinFmt(videofmt);
            }
        } 
        public string codecstr
        {
            get
            {
                return JoinFmt(codecfmt);
            }
        }
        public ApiModel LivePlayUrlv2(string room_id, int qn = 0)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"https://api.live.bilibili.com/xlive/web-room/v2/index/getRoomPlayInfo",
                parameter = $"room_id={room_id}&qn={qn}&protocol={protocolstr}&format={videostr}&codec={codecstr}"
            };
            return api;
        }

        /// <summary>
        /// 互动视频信息
        /// </summary>
        /// <param name="aid"></param>
        /// <param name="graph_version"></param>
        /// <param name="edge_id"></param>
        /// <returns></returns>
        public ApiModel InteractionEdgeInfo(string aid,int graph_version,long edge_id=0)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"{ApiHelper.API_BASE_URL}/x/stein/edgeinfo_v2",
                parameter = ApiHelper.MustParameter(ApiHelper.AndroidKey, true) + $"&aid={aid}&graph_version={graph_version}&edge_id={edge_id}"
            };
            api.parameter += ApiHelper.GetSign(api.parameter, ApiHelper.AndroidKey);
            return api;
        }

        /// <summary>
        /// 番剧播放记录上传
        /// </summary>
        /// <param name="aid">AVID</param>
        /// <param name="cid">CID</param>
        /// <param name="sid">SID</param>
        /// <param name="epid">EPID</param>
        /// <param name="type">类型 3=视频，4=番剧</param>
        /// <param name="progress">进度/秒</param>
        /// <returns></returns>
        public ApiModel SeasonHistoryReport(string aid,string cid, int progress, int sid=0,string epid="0",int type=3)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Post,
                baseUrl = $"{ApiHelper.API_BASE_URL}{ApiHelper.api2}/history/report",
                body = ApiHelper.MustParameter(ApiHelper.AndroidVideoKey, true) + $"&aid={aid}&cid={cid}&epid={epid}&sid={sid}&progress={progress}&realtime={progress}&sub_type=1&type={type}"
            };
            api.body += ApiHelper.GetSign(api.body, ApiHelper.AndroidVideoKey);
            return api;
        }
       /// <summary>
       /// 发送弹幕
       /// </summary>
       /// <param name="aid">AV</param>
       /// <param name="cid">CID</param>
       /// <param name="color">颜色(10进制)</param>
       /// <param name="msg">内容</param>
       /// <param name="position">位置</param>
       /// <param name="mode">类型</param>
       /// <param name="plat">平台</param>
       /// <returns></returns>
        public ApiModel SendDanmu(string aid,string cid,string color,string msg,int position,int mode=1,int plat=2)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Post,
                baseUrl = $"{ApiHelper.API_BASE_URL}{ApiHelper.api2}/dm/post",
                parameter= ApiHelper.MustParameter(ApiHelper.AndroidVideoKey, true)+$"&aid={aid}",
                body =   $"msg={Uri.EscapeDataString(msg)}&mode={mode}&screen_state=1&color={color}&pool=0&progress={Convert.ToInt32(position*1000)}&fontsize=25&rnd={Utils.GetTimestampS()}&from=7&oid={cid}&plat={plat}&type=1"
            };
            api.parameter += ApiHelper.GetSign(api.parameter, ApiHelper.AndroidVideoKey);
            return api;
        }
        /// <summary>
        /// 读取播放信息
        /// </summary>
        /// <param name="aid">AV</param>
        /// <param name="cid">CID</param>
        /// <returns></returns>
        public ApiModel GetPlayerInfo(string aid, string cid,string bvid)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"{ApiHelper.API_BASE_URL}/x/player/v2",
                parameter = $"cid={cid}&aid={aid}&bvid={bvid}",
            };
            return api;
        }
        /// <summary>
        /// 读取视频在线人数
        /// </summary>
        /// <param name="aid">AV</param>
        /// <param name="cid">CID</param>
        /// <returns></returns>
        public ApiModel GetPlayerOnline(string aid, string cid, string bvid)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"{ApiHelper.API_BASE_URL}/x/player/online/total",
                parameter = $"cid={cid}&aid={aid}&bvid={bvid}",
            };
            return api;
        }

        /// <summary>
        /// 弹幕关键词
        /// </summary>
        /// <returns></returns>
        public ApiModel GetDanmuFilterWords()
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"{ApiHelper.API_BASE_URL}/x/dm/filter/user",
                parameter = ApiHelper.MustParameter(ApiHelper.AndroidVideoKey, true) 
            };
            api.parameter += ApiHelper.GetSign(api.parameter, ApiHelper.AndroidVideoKey);
            return api;
        }
        /// <summary>
        /// 添加弹幕屏蔽关键词
        /// </summary>
        /// <param name="word">关键词</param>
        /// <param name="type">类型，0=关键字，1=正则，2=用户</param>
        /// <returns></returns>
        public ApiModel AddDanmuFilterWord(string word,int type)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Post,
                baseUrl = $"{ApiHelper.API_BASE_URL}/x/dm/filter/user/add",
                body = ApiHelper.MustParameter(ApiHelper.AndroidVideoKey, true)+ $"&filter={Uri.EscapeDataString(word)}&type={type}"
            };
            api.body += ApiHelper.GetSign(api.parameter, ApiHelper.AndroidVideoKey);
            return api;
        }
        /// <summary>
        /// 分段弹幕
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="segment_index"></param>
        /// <returns></returns>
        public ApiModel SegDanmaku(string oid, int segment_index)
        {
            ApiModel api = new ApiModel()
            {
                method = RestSharp.Method.Get,
                baseUrl = $"http://api.bilibili.com{ApiHelper.api2}/dm/list/seg.so",
                parameter = $"type=1&oid={oid}&segment_index={segment_index}"
            };
            return api;
        }

        /// <summary>
        /// 生成一个MPD文件链接
        /// </summary>
        /// <param name="generate"></param>
        /// <returns></returns>
        public string GenerateMPD(GenerateMPDModel generate)
        {
            var par=Newtonsoft.Json.JsonConvert.SerializeObject(generate);
            return $"{ApiHelper.IL_BASE_URL}/api/player/generatempd?par={Uri.EscapeDataString(par)}";
        }


    }
}

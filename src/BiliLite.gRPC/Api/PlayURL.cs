using System;
using System.Threading.Tasks;
using BiliLite.gRPC;
using Bilibili.App.Playurl.V1;

namespace BiliLite.gRPC.Api
{
    public class PlayURL
    {
        /// <summary>
        /// 视频播放地址
        /// </summary>
        public async Task<PlayViewReply> VideoPlayView(long aid, long cid, int qn, int fnval, CodeType codeType, GrpcBiliUserInfo userInfo)
        {
            var message = new PlayViewReq()
            {
                Aid = aid,
                Cid = cid,
                Qn = qn,
                Fnval = fnval,
                Fourk = true,
                PreferCodecType = codeType
            };
            var result = await GrpcRequest.Instance.SendMessage("https://app.bilibili.com/bilibili.app.playurl.v1.PlayURL/PlayView", message, userInfo);
            if (result.status)
            {
                return PlayViewReply.Parser.ParseFrom(result.results);
            }
            else
            {
                throw new Exception(result.message);
            }
        }

        /// <summary>
        /// 番剧播放地址
        /// </summary>
        public async Task<PlayViewReply> BangumiPlayView(long epid, long cid, int qn, int fnval, CodeType codeType, GrpcBiliUserInfo userInfo)
        {
            var message = new PlayViewReq()
            {
                Aid = epid,
                Cid = cid,
                Qn = qn,
                Fnval = fnval,
                Fourk = true,
                PreferCodecType = codeType
            };
            var result = await GrpcRequest.Instance.SendMessage("https://app.bilibili.com/bilibili.pgc.gateway.player.v1.PlayURL/PlayView", message, userInfo);
            if (result.status)
            {
                return PlayViewReply.Parser.ParseFrom(result.results);
            }
            else
            {
                throw new Exception(result.message);
            }
        }
    }
}

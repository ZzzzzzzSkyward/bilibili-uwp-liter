using System;
using System.Threading.Tasks;
using Bilibili.App.Dynamic.V2;
using BiliLite.Api;
using BiliLite.gRPC;
using BiliLite.gRPC.Api;
using BiliLite.Helpers;

namespace BiliLite.Services
{
    public class GrpcService
    {
        private const string DynamicGrpcBaseUrl = "https://grpc.biliapi.net:443/bilibili.app.dynamic.v2.Dynamic/";

        public async Task<DynAllReply> GetDynAll(int page = 1, string historyOffset = null)
        {
            var request = new DynAllReq
            {
                Page = page,
            };
            if (historyOffset != null)
            {
                request.Offset = historyOffset;
                request.RefreshType = Refresh.History;
            }

            var result = await GrpcRequest.Instance.SendMessage(
                DynamicGrpcBaseUrl + "DynAll",
                request,
                GetRequestUserInfo());
            if (result.status)
            {
                return DynAllReply.Parser.ParseFrom(result.results);
            }

            throw new Exception(result.message);
        }

        public async Task<DynVideoReply> GetDynVideo(int page, string historyOffset, string updateBaseline)
        {
            var request = new DynVideoReq
            {
                LocalTime = 8,
            };
            if (page > 1)
            {
                request.Offset = historyOffset;
                request.UpdateBaseline = updateBaseline;
                request.Page = page;
                request.RefreshType = Refresh.History;
            }

            var result = await GrpcRequest.Instance.SendMessage(
                DynamicGrpcBaseUrl + "DynVideo",
                request,
                GetRequestUserInfo());
            if (result.status)
            {
                return DynVideoReply.Parser.ParseFrom(result.results);
            }

            throw new Exception(result.message);
        }

        private static GrpcBiliUserInfo GetRequestUserInfo()
        {
            return new GrpcBiliUserInfo(
                SettingHelper.Account.AccessKey,
                SettingHelper.Account.UserID,
                ApiHelper.LoginKey.Appkey);
        }
    }
}

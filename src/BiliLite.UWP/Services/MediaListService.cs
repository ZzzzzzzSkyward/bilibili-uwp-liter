using System;
using System.Threading.Tasks;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Models;
using BiliLite.Models.Common;

namespace BiliLite.Services
{
    /// <summary>
    /// 远程播放列表(medialist)的首页与翻页读取。直接 new 即可用,不参与依赖注入,
    /// 端点仍由 Api/VideoAPI.cs 的同名方法负责,这里不重复构造请求。
    /// </summary>
    public class MediaListService
    {
        public const int DefaultMediaListType = 1;
        public const int DefaultPageSize = 20;

        private readonly VideoAPI m_videoAPI = new VideoAPI();

        public Task<MediaListResources> GetMediaList(string mediaListId, int mediaListType = DefaultMediaListType, int pageSize = DefaultPageSize)
        {
            // 首页 oid 为空
            return GetMediaListPage(mediaListId, "", mediaListType, pageSize);
        }

        /// <summary>
        /// 用状态里记录的上一页最后一条 aid 作为 oid,取下一页
        /// </summary>
        public Task<MediaListResources> LoadMoreMediaList(MediaListPagingState state, int pageSize = DefaultPageSize)
        {
            if (state == null) return Task.FromResult<MediaListResources>(null);
            return GetMediaListPage(state.MediaListId, state.NextCursor, state.MediaListType, pageSize);
        }

        private async Task<MediaListResources> GetMediaListPage(string mediaListId, string lastAid, int mediaListType, int pageSize)
        {
            if (string.IsNullOrEmpty(mediaListId))
            {
                ReportFailure("播放列表ID无效");
                return null;
            }

            var reply = await RequestAsync(m_videoAPI.GetMediaList(mediaListId, lastAid, mediaListType, pageSize));
            if (reply.Succeeded && reply.Resources != null && reply.Resources.MediaList != null)
            {
                return reply.Resources;
            }
            // 请求本身失败(已提示)时不再打第二个请求,只有签名接口取不到 video 列表才换未登录接口
            if (!reply.Succeeded) return null;

            reply = await RequestAsync(m_videoAPI.GetMediaListNotLogin(mediaListId, lastAid, mediaListType, pageSize));
            if (!reply.Succeeded) return null;
            if (reply.Resources == null || reply.Resources.MediaList == null)
            {
                ReportFailure("读取播放列表失败：响应中没有视频列表");
                return null;
            }
            return reply.Resources;
        }

        private static async Task<MediaListReply> RequestAsync(ApiModel api)
        {
            var reply = new MediaListReply();
            try
            {
                var results = await api.Request();
                if (results == null)
                {
                    ReportFailure("读取播放列表失败");
                    return reply;
                }
                if (!results.status)
                {
                    ReportFailure(results.message);
                    return reply;
                }

                var data = await results.GetData<MediaListResources>();
                if (data == null || !data.success)
                {
                    ReportFailure(data == null ? "读取播放列表失败" : data.message);
                    return reply;
                }

                reply.Succeeded = true;
                reply.Resources = data.data;
                return reply;
            }
            catch (Exception ex)
            {
                ReportFailure(LogHelper.IsNetworkError(ex) ? "请检查你的网络连接" : "出现了一个未处理错误，已记录", ex);
                return reply;
            }
        }

        private static void ReportFailure(string message, Exception ex = null)
        {
            if (string.IsNullOrEmpty(message)) message = "读取播放列表失败";
            Utils.ShowMessageToast(message);
            LogHelper.Log("[MediaList] " + message, LogType.ERROR, ex);
        }

        /// <summary>
        /// 区分"请求失败"和"请求成功但没有 media_list":后者才触发未登录重试
        /// </summary>
        private sealed class MediaListReply
        {
            public bool Succeeded { get; set; }

            public MediaListResources Resources { get; set; }
        }
    }
}

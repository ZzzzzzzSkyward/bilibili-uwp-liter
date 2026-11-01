using System;
using System.Threading.Tasks;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Models.Common;
using BiliLite.Parsing;
using BiliLite.Parsing.MediaList;

namespace BiliLite.Services
{
    /// <summary>
    /// medialist 接口的两个取法：已登录（带签名）与未登录。
    /// </summary>
    public enum MediaListEndpointKind
    {
        Signed = 0,
        Unsigned = 1,
    }

    /// <summary>
    /// 远程播放列表(medialist)的首页与翻页读取。直接 new 即可用,不参与依赖注入,
    /// 端点仍由 Api/VideoAPI.cs 的同名方法负责,这里不重复构造请求。
    ///
    /// 本层只做三件事:发一个请求、把响应交给解析层、在两步取法之间做取舍。
    /// 1. 不解码字段:信封、载荷形状、行字段都在 Parsing/MediaList/MediaListResourceListParser 与 Mapper。
    /// 2. 不弹提示、不记日志、不碰界面:失败以 ApiParseResult 的类别返回,由发布者决定说什么。
    /// 3. 换源规则只有一条,就在 ShouldRetryUnsigned:签名接口拿到了响应但没有 video 列表,
    ///    才换未登录接口再取一次;传输失败、业务拒绝、正文解不出来都不换(换一次也是同样的结果)。
    ///    GetPageAsync 只取一个源,两步顺序在 LoadPageAsync,调用方也可以只调其中一个。
    /// </summary>
    public class MediaListService
    {
        public const int DefaultMediaListType = 1;
        public const int DefaultPageSize = 20;

        private readonly VideoAPI m_videoAPI = new VideoAPI();

        /// <summary>
        /// 首页:oid 为空。签名取不到列表时自动换未登录,结论(含失败类别)原样返回。
        /// </summary>
        public Task<ApiParseResult<MediaListResources>> GetMediaList(string mediaListId, int mediaListType = DefaultMediaListType, int pageSize = DefaultPageSize)
        {
            return LoadPageAsync(mediaListId, "", mediaListType, pageSize);
        }

        /// <summary>
        /// 用状态里记录的上一页最后一条 aid 作为 oid,取下一页
        /// </summary>
        public Task<ApiParseResult<MediaListResources>> LoadMoreMediaList(MediaListPagingState state, int pageSize = DefaultPageSize)
        {
            if (state == null) return Task.FromResult(InvalidId());
            return LoadPageAsync(state.MediaListId, state.NextCursor, state.MediaListType, pageSize);
        }

        /// <summary>
        /// 只从一个源取一页,发一个请求。源由调用方选,顺序由 LoadPageAsync 决定。
        /// </summary>
        public async Task<ApiParseResult<MediaListResources>> GetPageAsync(MediaListEndpointKind endpoint,
            string mediaListId, string lastAid, int mediaListType = DefaultMediaListType, int pageSize = DefaultPageSize)
        {
            if (string.IsNullOrEmpty(mediaListId)) return InvalidId();

            var api = endpoint == MediaListEndpointKind.Signed
                ? m_videoAPI.GetMediaList(mediaListId, lastAid, mediaListType, pageSize)
                : m_videoAPI.GetMediaListNotLogin(mediaListId, lastAid, mediaListType, pageSize);

            HttpResults response;
            try
            {
                response = await api.Request();
            }
            catch (Exception ex)
            {
                //传输层的异常就是传输失败:不升级成"响应形状不对",也不在这里提示用户。
                //这里记一笔是给日志留现场——解码路径本身不写日志。
                LogHelper.Log("[MediaList] 读取播放列表失败", LogType.ERROR, ex);
                var message = LogHelper.IsNetworkError(ex) ? MediaListFailureText.TransportText
                    : "出现了一个未处理错误，已记录";
                return ApiParseResult<MediaListResources>.Fail(ApiParseFailure.Transport, "transport.exception",
                    message);
            }

            var parsed = MediaListResourceListParser.Parse(response);
            if (!parsed.IsSuccess)
            {
                return ApiParseResult<MediaListResources>.FromFailure(parsed);
            }
            return ApiParseResult<MediaListResources>.Success(MediaListResourceListMapper.Map(parsed.Value));
        }

        /// <summary>
        /// 只有"响应拿到了、业务也通过了,但没有 video 列表"才换未登录接口。
        /// 其余失败类别都返回 false:换源不会让它们变好,只会多一个请求。
        /// </summary>
        public static bool ShouldRetryUnsigned(ApiParseResult<MediaListResources> signed)
        {
            return signed != null
                && !signed.IsSuccess
                && signed.Failure == ApiParseFailure.Payload
                && signed.Stage == MediaListResourceListParser.NoMediaListStage;
        }

        /// <summary>签名取一页;拿不到列表就换未登录再取一页。两步都失败时给第二步的结论。</summary>
        private async Task<ApiParseResult<MediaListResources>> LoadPageAsync(string mediaListId, string lastAid,
            int mediaListType, int pageSize)
        {
            var signed = await GetPageAsync(MediaListEndpointKind.Signed, mediaListId, lastAid, mediaListType, pageSize);
            if (signed.IsSuccess || !ShouldRetryUnsigned(signed)) return signed;

            return await GetPageAsync(MediaListEndpointKind.Unsigned, mediaListId, lastAid, mediaListType, pageSize);
        }

        private static ApiParseResult<MediaListResources> InvalidId()
        {
            //没带播放列表 ID 就不发请求:这不是服务端结论,定位到调用方少给的那个参数
            return ApiParseResult<MediaListResources>.Fail(ApiParseFailure.Envelope, MediaListFailureText.InvalidIdStage,
                "播放列表ID无效");
        }
    }
}

namespace BiliLite.Parsing.Video
{
    /// <summary>
    /// 解析结论 → 用户能看到的一句话。解析层不弹提示、不记日志：发布者拿这句话去弹或放到错误面板，
    /// 于是"传输失败 / 业务拒绝 / 形状不对"在同一处界面上仍然分得开。
    ///
    /// 1. 服务端自己给了话（业务拒绝、HTTP 状态）就直接说，不再盖一层"读取失败"。
    /// 2. "根本没有响应"只有诊断文字，用户看到的是"请检查你的网络连接"。
    /// 3. 信封/形状不对不解释细节：那是本机要修的，用户看到的是"视频详情读取失败"。
    /// </summary>
    public static class VideoFailureText
    {
        /// <summary>ApiEnvelope / VideoEnvelope 给"根本没有响应"用的定位；它那句话是诊断，不是说给用户听的</summary>
        private const string NoResponseStage = "transport";

        public const string TransportText = "请检查你的网络连接";
        public const string GenericText = "视频详情读取失败";

        /// <summary>成功时返回空串：发布者不需要为成功造一句话。</summary>
        public static string For<T>(ApiParseResult<T> result)
        {
            if (result == null) return GenericText;
            if (result.IsSuccess) return "";

            switch (result.Failure)
            {
                case ApiParseFailure.Transport:
                    //有几句已经写成人话（HTTP 状态、传输层异常）就直接用；只给了定位的那句换成通用提示
                    return string.IsNullOrEmpty(result.Message) || result.Stage == NoResponseStage
                        ? TransportText
                        : result.Message;
                case ApiParseFailure.Rejected:
                    return string.IsNullOrEmpty(result.Message) ? GenericText : result.Message;
                default:
                    return GenericText;
            }
        }
    }
}

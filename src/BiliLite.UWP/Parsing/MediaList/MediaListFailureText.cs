namespace BiliLite.Parsing.MediaList
{
    /// <summary>
    /// 解析结论 → 用户能看到的一句话。解析层不弹提示：发布者拿这句话去弹，
    /// 于是"传输失败 / 业务拒绝 / 形状不对"在同一处界面上仍然分得开。
    ///
    /// 1. 三条源共用同一张文案表：同一个失败类别在三个页面上说同一句话。
    /// 2. 服务端自己给了话（业务拒绝、HTTP 状态）就直接说，不再盖一层"读取失败"；
    ///    "根本没有响应"只有诊断文字，用户看到的是"请检查你的网络连接"。
    /// 3. 没有 video 列表是签名接口特有的一句：改造前它就是这样提示的，保留原文。
    /// </summary>
    public static class MediaListFailureText
    {
        /// <summary>服务层认定的"播放列表ID无效"：不带 id 时不发请求</summary>
        public const string InvalidIdStage = "medialist.id";

        /// <summary>ApiEnvelope 给"根本没有响应"用的定位；它那句话是诊断，不是说给用户听的</summary>
        private const string NoResponseStage = "transport";

        public const string TransportText = "请检查你的网络连接";
        public const string GenericText = "读取播放列表失败";
        public const string NoMediaListText = "读取播放列表失败：响应中没有视频列表";

        /// <summary>成功时返回空串：发布者不需要为成功造一句话。</summary>
        public static string For<T>(ApiParseResult<T> result)
        {
            if (result == null) return GenericText;
            if (result.IsSuccess) return "";

            if (result.Stage == MediaListResourceListParser.NoMediaListStage) return NoMediaListText;
            if (result.Stage == InvalidIdStage) return result.Message;

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

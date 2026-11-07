using System;

namespace BiliLite.Modules.Player.Playurl
{
    /// <summary>
    /// web playurl 与 gRPC 的选择顺序。会话已死时 web 只会给到 480p 上限，那份结果不是结论；
    /// 但 gRPC 也拿不到时它仍然是唯一能播的东西，提示归提示，播放不能因此消失。
    /// </summary>
    public static class PlayUrlFallback
    {
        /// <summary>
        /// 只有 web 失败、或者会话已死且列表里没有请求画质时，才值得再问一次 gRPC；
        /// 健康会话的 480p 可能是真的 480p 稿件，不额外多打一次请求。
        /// </summary>
        public static bool NeedsGrpc(bool webSuccess, bool cappedOnDeadSession) => !webSuccess || cappedOnDeadSession;

        /// <summary>gRPC 成功就用它；否则回落到之前那份 web 结果，哪怕它更糊。</summary>
        public static BiliPlayUrlQualitesInfo Choose(BiliPlayUrlQualitesInfo webResult, BiliPlayUrlQualitesInfo grpcResult)
        {
            if (grpcResult != null && grpcResult.Success) return grpcResult;
            return webResult;
        }
    }
}

using System;
using BiliLite.Api;
using BiliLite.gRPC;
using BiliLite.Helpers;

namespace BiliLite.Services
{
    /// <summary>
    /// 选择器的应用侧接线:进程级的共享实例、上下文捕获、结果分类、记录与诊断日志。
    /// 选择器本身只认操作与上下文,不知道账号从哪来,也不知道 HTTP 结果长什么样。
    /// </summary>
    internal static class DynamicApiSelection
    {
        //六个操作共用一份历史:桶键里带着操作,所以首页视频流与用户视频流不会互相串
        private static readonly DynamicApiSelector Selector = DynamicApiSelector.CreateInMemory();

        /// <summary>
        /// 捕获桶键需要的上下文:账号、登录 app key、旧版 Android app key。
        /// 必须在第一个 await 之前调用,之后换账号也只影响下一次请求。
        /// </summary>
        public static DynamicApiContext CaptureContext()
        {
            var accountId = SettingHelper.Account.Logined ? SettingHelper.Account.UserID : 0L;
            return new DynamicApiContext(accountId, ApiHelper.LoginKey.Appkey, ApiHelper.AndroidKey.Appkey);
        }

        /// <summary>新的一屏/详情边界:由选择器决定这次用哪一路,并允许恢复试用</summary>
        public static DynamicApiDecision Choose(DynamicApiContext context, DynamicApiOperation operation, bool allowRecoveryTrial, string boundary)
        {
            var decision = Selector.Choose(context, operation, allowRecoveryTrial);
            LogHelper.Log("Dynamic api stage=choose boundary=" + boundary + " op=" + operation + " " + decision.Describe(), LogType.INFO);
            return decision;
        }

        /// <summary>沿用已固定的来源翻页:只拿一个绑定本次上下文的尝试对象,不重新打分也不改偏好</summary>
        public static DynamicApiAttempt CreateAttempt(DynamicApiContext context, DynamicApiOperation operation, DynamicApiSource source)
        {
            return Selector.CreateAttempt(context, operation, source);
        }

        /// <summary>一次尝试只记一条:分类在请求回来后立刻做,映射/转换的失败不会再补一条</summary>
        public static void Record(DynamicApiAttempt attempt, DynamicApiOutcome outcome, TimeSpan elapsed, string stage, int businessCode)
        {
            Selector.Record(attempt, outcome, elapsed, businessCode);
            LogHelper.Log("Dynamic api stage=record where=" + stage + " op=" + attempt.Operation
                + " src=" + SourceName(attempt.Source) + " outcome=" + outcome
                + " ms=" + (int)elapsed.TotalMilliseconds
                + (businessCode != 0 ? " code=" + businessCode : ""), LogType.DEBUG);
        }

        public static void Record(DynamicApiAttempt attempt, DynamicApiOutcome outcome, TimeSpan elapsed, string stage)
        {
            Record(attempt, outcome, elapsed, stage, 0);
        }

        /// <summary>
        /// gRPC 请求失败:只看结构化状态码与失败类型,不解析 message 文本。
        /// 返回 IgnoredFailure 表示原因与端点可用性无关(取消/未知异常),调用方应交回原有错误呈现。
        /// </summary>
        public static DynamicApiOutcome ClassifyRequestFailure(Exception exception)
        {
            var requestException = exception as GrpcRequestException;
            if (requestException != null)
                return DynamicApiOutcomeClassifier.FromGrpcException(requestException);
            if (exception is OperationCanceledException)
                return DynamicApiOutcome.IgnoredFailure;
            if (exception is Google.Protobuf.InvalidProtocolBufferException)
                return DynamicApiOutcome.MalformedResponse;
            return DynamicApiOutcome.IgnoredFailure;
        }

        /// <summary>
        /// REST 请求结果:HTTP 状态与 JSON 业务码分开判断。
        /// businessCode 传 null 表示 HTTP 成功但没拿到可用的业务码(信封无效)。
        /// </summary>
        public static DynamicApiOutcome ClassifyLegacy(HttpResults response, int? businessCode)
        {
            if (response == null)
                return DynamicApiOutcome.TransportFailure;
            if (!response.status)
                return DynamicApiOutcomeClassifier.FromHttpStatus(response.code);
            if (!businessCode.HasValue)
                return DynamicApiOutcome.MalformedResponse;
            return DynamicApiOutcomeClassifier.FromBilibiliCode(businessCode.Value);
        }

        /// <summary>换源/备用来源的结论:一行,不含条目内容</summary>
        public static void LogFallback(DynamicApiOperation operation, DynamicApiSource from, DynamicApiSource to, DynamicApiOutcome outcome, string stage)
        {
            LogHelper.Log("Dynamic api stage=fallback where=" + stage + " op=" + operation
                + " from=" + SourceName(from) + " to=" + SourceName(to) + " outcome=" + outcome, LogType.INFO);
        }

        /// <summary>两路都失败:保留原有错误呈现前记一条</summary>
        public static void LogExhausted(DynamicApiOperation operation, DynamicApiOutcome first, DynamicApiOutcome second, string stage)
        {
            LogHelper.Log("Dynamic api stage=sources-exhausted where=" + stage + " op=" + operation
                + " first=" + first + " second=" + second, LogType.INFO);
        }

        public static string SourceName(DynamicApiSource source)
        {
            return source == DynamicApiSource.Grpc ? "grpc" : "legacy";
        }

        public static DynamicApiSource Opposite(DynamicApiSource source)
        {
            return source == DynamicApiSource.Grpc ? DynamicApiSource.Legacy : DynamicApiSource.Grpc;
        }
    }
}

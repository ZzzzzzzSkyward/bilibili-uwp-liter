using System;

namespace BiliLite.gRPC
{
    /// <summary>
    /// gRPC 请求失败时抛出的异常。Message 与旧的 Exception(result.message) 逐字相同,
    /// 另外把 HTTP 状态、gRPC 状态、B 站业务码与传输失败类型作为数据带出,
    /// 调用方不必再解析错误文本就能判断该不该换源。
    /// </summary>
    public class GrpcRequestException : Exception
    {
        public GrpcRequestException(GrpcResult result)
            : base(result == null ? string.Empty : result.message)
        {
            Result = result;
            if (result == null)
                return;
            Code = result.code;
            HttpStatus = result.httpStatus;
            GrpcStatus = result.grpcStatus;
            BiliStatusCode = result.biliStatusCode;
            FailureKind = result.failureKind;
        }

        /// <summary>原始失败结果,便于诊断日志带出完整文本</summary>
        public GrpcResult Result { get; private set; }

        /// <summary>原先写在 GrpcResult.code 里的码</summary>
        public int Code { get; private set; }

        /// <summary>HTTP 状态码,没收到响应时为 null</summary>
        public int? HttpStatus { get; private set; }

        /// <summary>响应头 grpc-status</summary>
        public int? GrpcStatus { get; private set; }

        /// <summary>响应头 bili-status-code(接口拒绝时常见 -663/-352/-412)</summary>
        public int? BiliStatusCode { get; private set; }

        /// <summary>传输层/HTTP 层失败类型</summary>
        public GrpcFailureKind FailureKind { get; private set; }
    }
}

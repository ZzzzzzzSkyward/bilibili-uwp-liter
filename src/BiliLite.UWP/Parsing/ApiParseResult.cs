using System;

namespace BiliLite.Parsing
{
    /// <summary>
    /// 一次解码的失败类别。解析层只报类别，换源语义（哪些类别值得换路）由 Services 决定。
    /// </summary>
    public enum ApiParseFailure
    {
        None = 0,
        /// <summary>连接层/HTTP 状态就没拿到</summary>
        Transport = 1,
        /// <summary>信封不可用：正文不是 JSON、没有 code、没有 data</summary>
        Envelope = 2,
        /// <summary>服务端业务码拒绝</summary>
        Rejected = 3,
        /// <summary>信封合法但字段形状不对</summary>
        Payload = 4,
    }

    /// <summary>
    /// 解析结论：要么一个值，要么一个带定位的失败（阶段、字段路径、业务码、HTTP 状态）。
    /// 不做网络请求、不弹提示、不改状态。
    /// </summary>
    public sealed class ApiParseResult<T>
    {
        private ApiParseResult()
        {
        }

        public T Value { get; private set; }

        public ApiParseFailure Failure { get; private set; }

        /// <summary>失败的定位，例如 envelope.code / data.items；成功时为空串</summary>
        public string Stage { get; private set; }

        public string Message { get; private set; }

        public int BusinessCode { get; private set; }

        public int HttpStatus { get; private set; }

        public bool IsSuccess
        {
            get { return Failure == ApiParseFailure.None; }
        }

        public static ApiParseResult<T> Success(T value)
        {
            return new ApiParseResult<T>
            {
                Value = value,
                Failure = ApiParseFailure.None,
                Stage = "",
                Message = "",
            };
        }

        /// <summary>信封层的结果换个值类型继续往下传：失败信息原样保留。</summary>
        public static ApiParseResult<T> FromFailure<TSource>(ApiParseResult<TSource> source)
        {
            if (source == null)
            {
                return Fail(ApiParseFailure.Transport, "transport", "没有响应");
            }
            if (source.IsSuccess)
            {
                throw new InvalidOperationException("a successful result has no failure to copy");
            }
            return Fail(source.Failure, source.Stage, source.Message, source.BusinessCode, source.HttpStatus);
        }
        public static ApiParseResult<T> Fail(ApiParseFailure failure, string stage, string message)
        {
            return Fail(failure, stage, message, 0, 0);
        }

        public static ApiParseResult<T> Fail(ApiParseFailure failure, string stage, string message, int businessCode, int httpStatus)
        {
            if (failure == ApiParseFailure.None)
            {
                throw new ArgumentOutOfRangeException(nameof(failure));
            }
            return new ApiParseResult<T>
            {
                Failure = failure,
                Stage = stage ?? "",
                Message = message ?? "",
                BusinessCode = businessCode,
                HttpStatus = httpStatus,
            };
        }
    }
}

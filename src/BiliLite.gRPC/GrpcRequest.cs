using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using BiliLite.gRPC.Api;
using System.Diagnostics;
using System.Threading;

namespace BiliLite.gRPC
{
    public class GrpcRequest
    {
        public static Action<string, Exception> DiagnosticLogger { get; set; }
        private static int _requestCounter;

        // 每次 new HttpClient() 都会自带一套连接池/句柄,不释放就只能等终结器回收,
        // 动态流每刷新一页都会漏一份,所以进程内共用一个实例。
        // 该实例只保留超时,所有请求头都随 app-key 逐请求挂到 HttpRequestMessage 上,
        // 并发请求各用各的头,不存在 DefaultRequestHeaders 竞态。
        private static readonly HttpClient SharedHttpClient = CreateSharedHttpClient();

        // app-key 未识别/为空(含用户自定义 key)时一律回落到 Android 档
        private static readonly AndroidGrpcRequestHeader DefaultGrpcRequestHeader = new AndroidGrpcRequestHeader();

        private static HttpClient CreateSharedHttpClient()
        {
            return new HttpClient { Timeout = TimeSpan.FromSeconds(100) };
        }

        internal static IGrpcRequestHeader ResolveHeader(GrpcBiliUserInfo userInfo)
        {
            return GrpcRequestHeaderFactory.GetGrpcRequestHeader(userInfo == null ? null : userInfo.AppKey)
                ?? DefaultGrpcRequestHeader;
        }

        private static void LogDiagnostic(string message, Exception ex = null)
        {
            try
            {
                DiagnosticLogger?.Invoke(message, ex);
            }
            catch (Exception loggingException)
            {
                Debug.WriteLine("gRPC diagnostic logging failed: " + loggingException.Message);
            }
        }

        private static string NewRequestId()
        {
            return "grpc-" + Interlocked.Increment(ref _requestCounter).ToString("D6");
        }

        private static string DescribeEndpoint(string url)
        {
            Uri uri;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri))
                return uri.GetLeftPart(UriPartial.Path);
            return "unparsed-endpoint";
        }

        // netstandard2.0 上拿不到 gRPC trailers,只能读取服务端放到响应头里的状态
        private static readonly string[] ServerMetadataHeaders = { "grpc-status", "grpc-message", "bili-status-code" };

        /// <summary>
        /// 读取响应头中可用的服务端状态,没有时返回 null。
        /// grpc-status 与 bili-status-code 分开解析,供调用方做结构化失败分类,不再解析 message 文本。
        /// </summary>
        private static string ReadServerMetadata(HttpResponseMessage response, out int? serverCode, out int? grpcStatus, out int? biliStatusCode)
        {
            serverCode = null;
            grpcStatus = null;
            biliStatusCode = null;
            var parts = new List<string>();
            foreach (var name in ServerMetadataHeaders)
            {
                IEnumerable<string> values;
                if (!response.Headers.TryGetValues(name, out values) || values == null)
                    continue;
                var value = string.Join(",", values.Where(x => !string.IsNullOrEmpty(x)).ToArray());
                if (value.Length == 0)
                    continue;
                parts.Add(name + "=" + value);
                if (name == "grpc-message")
                    continue;
                int parsed;
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                    continue;
                if (serverCode == null)
                    serverCode = parsed;
                if (name == "grpc-status")
                {
                    if (grpcStatus == null)
                        grpcStatus = parsed;
                }
                else if (name == "bili-status-code")
                {
                    if (biliStatusCode == null)
                        biliStatusCode = parsed;
                }
            }
            return parts.Count == 0 ? null : string.Join("; ", parts);
        }

        private static string DescribeFailure(HttpResponseMessage response, string metadata, int responseBytes)
        {
            var builder = new StringBuilder();
            builder.Append("请求失败,HTTP ").Append((int)response.StatusCode);
            var reason = response.ReasonPhrase == null ? "" : response.ReasonPhrase.Trim();
            if (reason.Length > 0)
                builder.Append(' ').Append(reason);
            // 服务端给了 grpc-status/bili-status-code 就是明确拒绝,空响应体只在读不到元数据时才提
            if (metadata != null)
                builder.Append(",服务端拒绝: ").Append(metadata);
            else if (response.IsSuccessStatusCode)
                builder.Append(",没有数据返回");
            builder.Append(",responseBytes=").Append(responseBytes);
            if (metadata == null)
                builder.Append(",未返回grpc-status等服务端状态");
            return builder.ToString();
        }

        // 失败分类只看结构化信号(HTTP 状态与响应头),绝不解析 message 文本
        private static GrpcFailureKind ClassifyFailure(HttpResponseMessage response, int? grpcStatus, int? biliStatusCode)
        {
            // 服务端在响应头里给了非 0 状态码就是明确拒绝(含 bili-status-code=-663)
            if (grpcStatus.HasValue && grpcStatus.Value != 0)
                return GrpcFailureKind.EndpointRejection;
            if (biliStatusCode.HasValue && biliStatusCode.Value != 0)
                return GrpcFailureKind.EndpointRejection;
            var status = (int)response.StatusCode;
            if (status >= 500)
                return GrpcFailureKind.HttpServerError;
            if (status >= 400)
                return GrpcFailureKind.EndpointRejection;
            // 2xx/3xx 却拿不到完整 gRPC 帧,且服务端没给出状态:畸形响应
            return GrpcFailureKind.MalformedResponse;
        }

        // message 文本与旧路径逐字一致,额外把 HTTP/gRPC/B 站状态与传输失败类型作为数据带回
        private static GrpcResult FailureResult(int code, string message, HttpResponseMessage response, int? grpcStatus, int? biliStatusCode)
        {
            return new GrpcResult()
            {
                code = code,
                status = false,
                message = message,
                httpStatus = (int)response.StatusCode,
                grpcStatus = grpcStatus,
                biliStatusCode = biliStatusCode,
                failureKind = ClassifyFailure(response, grpcStatus, biliStatusCode),
            };
        }

        private static GrpcRequest _instance;
        public static GrpcRequest Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GrpcRequest();
                }
                return _instance;
            }
        }


        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="url">路径/URL</param>
        /// <param name="message">请求头</param>
        /// <param name="access_key">access_key</param>
        /// <returns>处理后的byte[]</returns>
        public Task<GrpcResult> SendMessage(string url, IMessage message, string access_key = "")
        {
            return SendMessageCore(url, message, new GrpcBiliUserInfo(access_key, 0, null));
        }

        public Task<GrpcResult> SendMessage(string url, IMessage message, GrpcBiliUserInfo userInfo)
        {
            return SendMessageCore(url, message, userInfo);
        }

        private async Task<GrpcResult> SendMessageCore(string url, IMessage message, GrpcBiliUserInfo userInfo)
        {
            // 归一化一次:null 用户信息按匿名 Android 请求走。
            // access key 用空串而不是 null,因为 protobuf 的 string setter 拒绝 null
            // (Metadata.AccessKey 走 ProtoPreconditions.CheckNotNull),null 会在填头时抛异常。
            userInfo = userInfo ?? new GrpcBiliUserInfo(string.Empty, 0, null);
            var requestId = NewRequestId();
            var endpoint = DescribeEndpoint(url);
            var stopwatch = Stopwatch.StartNew();
            LogDiagnostic("gRPC requestId=" + requestId + " method=POST endpoint=" + endpoint + " started");
            try
            {
                var messageBytes = message.ToByteArray();
                //校验用?第五位为数组长度
                var stateBytes = new byte[] { 0, 0, 0, 0, (byte)messageBytes.Length };
                //合并两个字节数组
                byte[] bodyBytes = new byte[5 + messageBytes.Length];
                stateBytes.CopyTo(bodyBytes, 0);
                messageBytes.CopyTo(bodyBytes, 5);

                ByteArrayContent byteArrayContent = new ByteArrayContent(bodyBytes);
                byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/grpc");
                byteArrayContent.Headers.ContentLength = bodyBytes.Length;
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    // 整个档位的头逐请求挂上:同一个共享 client 同时服务 Android / iOS 与不同账号
                    foreach (var header in ResolveHeader(userInfo).GetHeaders(userInfo))
                    {
                        if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value)
                            && string.Equals(header.Key, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                        {
                            // Transfer-Encoding 是受限头,TryAddWithoutValidation 拒绝时用强类型属性补上
                            request.Headers.TransferEncodingChunked = true;
                        }
                    }
                    request.Content = byteArrayContent;
                    using (var response = await SharedHttpClient.SendAsync(request))
                    {
                        int? serverCode;
                        int? grpcStatus;
                        int? biliStatusCode;
                        var metadata = ReadServerMetadata(response, out serverCode, out grpcStatus, out biliStatusCode);
                        LogDiagnostic("gRPC requestId=" + requestId + " method=POST endpoint=" + endpoint + " status=" + (int)response.StatusCode
                            + " metadata=" + (metadata ?? "none") + " durationMs=" + stopwatch.ElapsedMilliseconds);
                        var data = await response.Content.ReadAsByteArrayAsync();
                        if (!response.IsSuccessStatusCode)
                        {
                            LogDiagnostic("gRPC requestId=" + requestId + " method=POST endpoint=" + endpoint + " rejected httpStatus=" + (int)response.StatusCode
                                + " metadata=" + (metadata ?? "none") + " responseBytes=" + data.Length);
                            return FailureResult(
                                serverCode ?? (int)response.StatusCode,
                                DescribeFailure(response, metadata, data.Length),
                                response, grpcStatus, biliStatusCode);
                        }
                        if (data.Length > 5)
                        {
                            return new GrpcResult()
                            {
                                code = 0,
                                status = true,
                                message = "请求成功",
                                results = data.Skip(5).ToArray(),
                                httpStatus = (int)response.StatusCode,
                            };
                        }
                        else
                        {
                            LogDiagnostic("gRPC requestId=" + requestId + " method=POST endpoint=" + endpoint + " emptyFrame responseBytes=" + data.Length
                                + " metadata=" + (metadata ?? "none"));
                            return FailureResult(
                                serverCode ?? -102,
                                DescribeFailure(response, metadata, data.Length),
                                response, grpcStatus, biliStatusCode);
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                LogDiagnostic("gRPC requestId=" + requestId + " method=POST endpoint=" + endpoint + " failed durationMs=" + stopwatch.ElapsedMilliseconds, ex);
                return new GrpcResult()
                {
                    code = ex.HResult,
                    status = false,
                    message = "发送gRPC请求失败" + ex.Message,
                    // HttpClient 超时表现为取消/超时异常,其余按传输失败处理
                    failureKind = ex is OperationCanceledException || ex is TimeoutException
                        ? GrpcFailureKind.Timeout
                        : GrpcFailureKind.Transport,
                };
            }
        }

        public async Task<GrpcResult> Get(string url)
        {
            var requestId = NewRequestId();
            var endpoint = DescribeEndpoint(url);
            var stopwatch = Stopwatch.StartNew();
            LogDiagnostic("HTTP requestId=" + requestId + " method=GET endpoint=" + endpoint + " started");
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    using (var response = await httpClient.GetAsync(url))
                    {
                        int? serverCode;
                        int? grpcStatus;
                        int? biliStatusCode;
                        var metadata = ReadServerMetadata(response, out serverCode, out grpcStatus, out biliStatusCode);
                        var data = await response.Content.ReadAsByteArrayAsync();
                        LogDiagnostic("HTTP requestId=" + requestId + " method=GET endpoint=" + endpoint + " status=" + (int)response.StatusCode
                            + " metadata=" + (metadata ?? "none") + " durationMs=" + stopwatch.ElapsedMilliseconds + " responseBytes=" + data.Length);
                        if (!response.IsSuccessStatusCode)
                        {
                            LogDiagnostic("HTTP requestId=" + requestId + " method=GET endpoint=" + endpoint + " rejected httpStatus=" + (int)response.StatusCode
                                + " metadata=" + (metadata ?? "none") + " responseBytes=" + data.Length);
                            return FailureResult(
                                serverCode ?? (int)response.StatusCode,
                                DescribeFailure(response, metadata, data.Length),
                                response, grpcStatus, biliStatusCode);
                        }
                        if (data.Length > 5)
                        {
                            return new GrpcResult()
                            {
                                code = 0,
                                status = true,
                                message = "请求成功",
                                results = data.Skip(5).ToArray(),
                                httpStatus = (int)response.StatusCode,
                            };
                        }
                        else
                        {
                            LogDiagnostic("HTTP requestId=" + requestId + " method=GET endpoint=" + endpoint + " emptyFrame responseBytes=" + data.Length
                                + " metadata=" + (metadata ?? "none"));
                            return FailureResult(
                                serverCode ?? -102,
                                DescribeFailure(response, metadata, data.Length),
                                response, grpcStatus, biliStatusCode);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogDiagnostic("HTTP requestId=" + requestId + " method=GET endpoint=" + endpoint + " failed durationMs=" + stopwatch.ElapsedMilliseconds, ex);
                return new GrpcResult()
                {
                    code = ex.HResult,
                    status = false,
                    message = "发送Http请求失败" + ex.Message,
                    failureKind = ex is OperationCanceledException || ex is TimeoutException
                        ? GrpcFailureKind.Timeout
                        : GrpcFailureKind.Transport,
                };
            }
        }
    }

    public enum GrpcFailureKind
    {
        None = 0,
        /// <summary>服务端明确拒绝:非 0 的 grpc-status/bili-status-code,或 4xx</summary>
        EndpointRejection = 1,
        /// <summary>连接/发送阶段失败,没有拿到响应</summary>
        Transport = 2,
        /// <summary>请求被取消或超时</summary>
        Timeout = 3,
        /// <summary>HTTP 5xx</summary>
        HttpServerError = 4,
        /// <summary>响应完整但拿不到有效 gRPC 帧</summary>
        MalformedResponse = 5,
    }

    public class GrpcResult
    {
        public int code { get; set; }
        public string message { get; set; }
        public byte[] results { get; set; }
        public bool status { get; set; }
        /// <summary>HTTP 状态码,没收到响应时为 null</summary>
        public int? httpStatus { get; set; }
        /// <summary>响应头 grpc-status</summary>
        public int? grpcStatus { get; set; }
        /// <summary>响应头 bili-status-code(接口拒绝时常见 -663/-352/-412)</summary>
        public int? biliStatusCode { get; set; }
        /// <summary>传输层/HTTP 层失败类型,status 为 true 时为 None</summary>
        public GrpcFailureKind failureKind { get; set; }
    }
}

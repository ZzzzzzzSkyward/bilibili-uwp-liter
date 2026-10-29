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
        /// 读取响应头中可用的服务端状态,没有时返回 null
        /// </summary>
        private static string ReadServerMetadata(HttpResponseMessage response, out int? serverCode)
        {
            serverCode = null;
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
                if (serverCode == null && name != "grpc-message")
                {
                    int parsed;
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                        serverCode = parsed;
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
                        var metadata = ReadServerMetadata(response, out serverCode);
                        LogDiagnostic("gRPC requestId=" + requestId + " method=POST endpoint=" + endpoint + " status=" + (int)response.StatusCode
                            + " metadata=" + (metadata ?? "none") + " durationMs=" + stopwatch.ElapsedMilliseconds);
                        var data = await response.Content.ReadAsByteArrayAsync();
                        if (!response.IsSuccessStatusCode)
                        {
                            LogDiagnostic("gRPC requestId=" + requestId + " method=POST endpoint=" + endpoint + " rejected httpStatus=" + (int)response.StatusCode
                                + " metadata=" + (metadata ?? "none") + " responseBytes=" + data.Length);
                            return new GrpcResult()
                            {
                                code = serverCode ?? (int)response.StatusCode,
                                status = false,
                                message = DescribeFailure(response, metadata, data.Length),
                            };
                        }
                        if (data.Length > 5)
                        {
                            return new GrpcResult()
                            {
                                code = 0,
                                status = true,
                                message = "请求成功",
                                results = data.Skip(5).ToArray()
                            };
                        }
                        else
                        {
                            LogDiagnostic("gRPC requestId=" + requestId + " method=POST endpoint=" + endpoint + " emptyFrame responseBytes=" + data.Length
                                + " metadata=" + (metadata ?? "none"));
                            return new GrpcResult()
                            {
                                code = serverCode ?? -102,
                                status = false,
                                message = DescribeFailure(response, metadata, data.Length),
                            };
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
                    message = "发送gRPC请求失败" + ex.Message
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
                        var metadata = ReadServerMetadata(response, out serverCode);
                        var data = await response.Content.ReadAsByteArrayAsync();
                        LogDiagnostic("HTTP requestId=" + requestId + " method=GET endpoint=" + endpoint + " status=" + (int)response.StatusCode
                            + " metadata=" + (metadata ?? "none") + " durationMs=" + stopwatch.ElapsedMilliseconds + " responseBytes=" + data.Length);
                        if (!response.IsSuccessStatusCode)
                        {
                            LogDiagnostic("HTTP requestId=" + requestId + " method=GET endpoint=" + endpoint + " rejected httpStatus=" + (int)response.StatusCode
                                + " metadata=" + (metadata ?? "none") + " responseBytes=" + data.Length);
                            return new GrpcResult()
                            {
                                code = serverCode ?? (int)response.StatusCode,
                                status = false,
                                message = DescribeFailure(response, metadata, data.Length),
                            };
                        }
                        if (data.Length > 5)
                        {
                            return new GrpcResult()
                            {
                                code = 0,
                                status = true,
                                message = "请求成功",
                                results = data.Skip(5).ToArray()
                            };
                        }
                        else
                        {
                            LogDiagnostic("HTTP requestId=" + requestId + " method=GET endpoint=" + endpoint + " emptyFrame responseBytes=" + data.Length
                                + " metadata=" + (metadata ?? "none"));
                            return new GrpcResult()
                            {
                                code = serverCode ?? -102,
                                status = false,
                                message = DescribeFailure(response, metadata, data.Length),
                            };
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
                    message = "发送Http请求失败" + ex.Message
                };
            }
        }
    }

    public class GrpcResult
    {
        public int code { get; set; }
        public string message { get; set; }
        public byte[] results { get; set; }
        public bool status { get; set; }
    }
}

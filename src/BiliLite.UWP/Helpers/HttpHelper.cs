using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using Windows.Web.Http;
using Windows.Storage.Streams;
using Windows.Web.Http.Filters;
using BiliLite.Models;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using System.Timers;
using System.Threading;

namespace BiliLite.Helpers
{

    /// <summary>
    /// 网络请求方法封装
    /// </summary>
    public static class HttpHelper
    {
        private static int _requestCounter;

        private static string NewRequestId()
        {
            return "http-" + Interlocked.Increment(ref _requestCounter).ToString("D6");
        }

        private static string DescribeEndpoint(string url)
        {
            Uri uri;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri))
                return uri.GetLeftPart(UriPartial.Path);
            return "unparsed-endpoint";
        }

        private static void LogRequest(string requestId, string method, string endpoint, string detail, Exception ex = null)
        {
            var message = "HTTP requestId=" + requestId + " method=" + method + " endpoint=" + endpoint + " " + detail;
            LogHelper.Log(message, ex == null ? LogType.DEBUG : LogType.ERROR, ex);
            Utils.AddALog("[HTTP]" + message);
        }

        private static void LogApiFailure(string requestId, string method, string endpoint, string content)
        {
            try
            {
                var response = JObject.Parse(content);
                var code = response["code"];
                if (code != null && code.Type != JTokenType.Null && code.ToString() != "0")
                {
                    LogRequest(requestId, method, endpoint, "API failure code=" + code + " message=" + (response["message"] ?? response["msg"]));
                }
            }
            catch (Exception ex)
            {
                LogRequest(requestId, method, endpoint, "API response envelope parse failed", ex);
            }
        }

        /// <summary>
        /// 发送一个GET请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public static DateTime lasttime = System.DateTime.Now;
        public static TimeSpan min_dt = new TimeSpan(1);
        public static int concurrent_get_count = 1;
        public static TimeSpan delay = new TimeSpan(0, 0, 0, 0, 1);
        public async static Task<HttpResults> Get(string url)
        {
            return await Get(url, null);
        }
        public async static Task<HttpResults> Get(string url, IDictionary<string, string> headers)
        {
            var requestId = NewRequestId();
            var endpoint = DescribeEndpoint(url);
            var stopwatch = Stopwatch.StartNew();
            //wait 
            var t = DateTime.Now;
            if (t - lasttime < min_dt)
            {
                concurrent_get_count++;
                await Task.Delay(delay * concurrent_get_count);
                concurrent_get_count--;
            }
            Debug.WriteLine("GET:" + endpoint);
            Utils.AddALog("[GET]" + requestId + " " + endpoint);
            try
            {
                HttpBaseProtocolFilter fiter = new HttpBaseProtocolFilter();

                fiter.IgnorableServerCertificateErrors.Add(Windows.Security.Cryptography.Certificates.ChainValidationResult.Expired);
                using (var client = new HttpClient(fiter))
                {
                    if (headers != null)
                    {
                        foreach (var item in headers)
                        {
                            client.DefaultRequestHeaders.Add(item.Key, item.Value);
                        }
                    }
                    if (url.Contains("bilibili.com") || url.Contains("pgc/player/") || url.Contains("x/web-interface"))
                    {
                        if(!client.DefaultRequestHeaders.Keys.Contains("User-Agent"))
                            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/113.0");
                    }
                    Console.WriteLine(endpoint);
                    var response = await client.GetAsync(new Uri(url));
                    LogRequest(requestId, "GET", endpoint, "status=" + (int)response.StatusCode + " durationMs=" + stopwatch.ElapsedMilliseconds);
                    if (!response.IsSuccessStatusCode)
                    {
                        LogRequest(requestId, "GET", endpoint, "HTTP failure status=" + (int)response.StatusCode + " " + response.StatusCode);
                        return new HttpResults()
                        {
                            code = (int)response.StatusCode,
                            status = false,
                            message = StatusCodeToMessage((int)response.StatusCode),
                            RequestId = requestId,
                            Method = "GET",
                            Endpoint = endpoint
                        };
                    }

                    //response.EnsureSuccessStatusCode();
                    var buffer = await response.Content.ReadAsBufferAsync();
                    var byteArray = buffer.ToArray();
                    HttpResults httpResults = new HttpResults()
                    {
                        RequestId = requestId,
                        Method = "GET",
                        Endpoint = endpoint,
                        code = (int)response.StatusCode,
                        status = response.StatusCode == HttpStatusCode.Ok,
                        results = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length),
                        message = "",
                    };
                    LogApiFailure(requestId, "GET", endpoint, httpResults.results);
                    return httpResults;
                }
            }
            catch (Exception ex)
            {
                LogRequest(requestId, "GET", endpoint, "failed durationMs=" + stopwatch.ElapsedMilliseconds, ex);
                return new HttpResults()
                {
                    RequestId = requestId,
                    Method = "GET",
                    Endpoint = endpoint,
                    code = ex.HResult,
                    status = false,
                    message = "网络请求出现错误(GET)"
                };
            }
        }

        public async static Task<HttpResults> GetWithWebCookie(string url, IDictionary<string, string> headers = null, IDictionary<string, string> extraCookies = null)
        {
            var endpoint = DescribeEndpoint(url);
            Debug.WriteLine("GET:" + endpoint);
            Utils.AddALog("[GET WITH COOKIE]" + endpoint);
            try
            {
                headers = headers == null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(headers);
                HttpBaseProtocolFilter fiter = new HttpBaseProtocolFilter();
                if (!headers.Keys.Contains("User-Agent"))
                {
                    //headers.Add("User-Agent", "Mozilla/5.0 BiliDroid/4.4.4 (bbcallen@gmail.com)");
                }

                var cookieHeaderKey = headers.Keys.FirstOrDefault(x => string.Equals(x, "Cookie", StringComparison.OrdinalIgnoreCase));
                var cookieValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (cookieHeaderKey != null)
                {
                    foreach (var pair in headers[cookieHeaderKey].Split(';'))
                    {
                        var separator = pair.IndexOf('=');
                        if (separator > 0)
                        {
                            cookieValues[pair.Substring(0, separator).Trim()] = pair.Substring(separator + 1).Trim();
                        }
                    }
                }
                else
                {
                    var cookies = fiter.CookieManager.GetCookies(new Uri("https://www.bilibili.com"));
                    //没有Cookie
                    if (cookies == null || cookies.Count == 0)
                    {
                        //访问一遍bilibili.com
                        await Get("https://www.bilibili.com");
                        cookies = fiter.CookieManager.GetCookies(new Uri("https://www.bilibili.com"));
                    }

                    if (cookies != null)
                    {
                        foreach (var item in cookies)
                        {
                            cookieValues[item.Name] = item.Value;
                        }
                    }
                }

                if (extraCookies != null)
                {
                    foreach (var item in extraCookies)
                    {
                        cookieValues[item.Key] = item.Value;
                    }
                }

                if (cookieHeaderKey != null || extraCookies != null || cookieValues.Count > 0)
                {
                    var cookie = string.Join(";", cookieValues.Select(x => $"{x.Key}={x.Value}"));
                    if (cookieHeaderKey == null)
                    {
                        headers.Add("Cookie", cookie);
                    }
                    else
                    {
                        headers[cookieHeaderKey] = cookie;
                    }
                }
                return await Get(url, headers);
            }
            catch (Exception ex)
            {
                LogHelper.Log("HTTP method=GET endpoint=" + endpoint + " stage=cookie-preparation failed", LogType.ERROR, ex);
                return new HttpResults()
                {
                    code = ex.HResult,
                    status = false,
                    message = "网络请求出现错误(GET)"
                };
            }
        }

        /// <summary>
        /// 发送一个GET请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public async static Task<Stream> GetStream(string url, IDictionary<string, string> headers = null)
        {
            var requestId = NewRequestId();
            var endpoint = DescribeEndpoint(url);
            var stopwatch = Stopwatch.StartNew();
            Debug.WriteLine("GET:" + endpoint);
            Utils.AddALog("[GET STREAM]" + requestId + " " + endpoint);
            try
            {
                using (var client = new HttpClient())
                {
                    if (headers != null)
                    {
                        foreach (var item in headers)
                        {
                            client.DefaultRequestHeaders.Add(item.Key, item.Value);
                        }
                    }

                    var response = await client.GetAsync(new Uri(url));
                    LogRequest(requestId, "GET", endpoint, "status=" + (int)response.StatusCode + " durationMs=" + stopwatch.ElapsedMilliseconds);
                    response.EnsureSuccessStatusCode();
                    return (await response.Content.ReadAsInputStreamAsync()).AsStreamForRead();
                }


            }
            catch (Exception ex)
            {
                LogRequest(requestId, "GET", endpoint, "stream failed durationMs=" + stopwatch.ElapsedMilliseconds, ex);
                return null;

            }



        }
        /// <summary>
        /// 发送一个GET请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public async static Task<IBuffer> GetBuffer(string url, IDictionary<string, string> headers = null)
        {
            var requestId = NewRequestId();
            var endpoint = DescribeEndpoint(url);
            var stopwatch = Stopwatch.StartNew();
            Debug.WriteLine("GET:" + endpoint);
            Utils.AddALog("[GET BUFFER]" + requestId + " " + endpoint);
            try
            {
                using (var client = new HttpClient())
                {
                    if (headers != null)
                    {
                        foreach (var item in headers)
                        {
                            client.DefaultRequestHeaders.Add(item.Key, item.Value);
                        }
                    }

                    var response = await client.GetAsync(new Uri(url));
                    LogRequest(requestId, "GET", endpoint, "status=" + (int)response.StatusCode + " durationMs=" + stopwatch.ElapsedMilliseconds);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsBufferAsync();
                }


            }
            catch (Exception ex)
            {
                LogRequest(requestId, "GET", endpoint, "buffer failed durationMs=" + stopwatch.ElapsedMilliseconds, ex);
                return null;

            }



        }
        /// <summary>
        /// 发送一个GET请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public async static Task<String> GetString(string url, IDictionary<string, string> headers = null, IDictionary<string, string> cookie = null)
        {
            var requestId = NewRequestId();
            var endpoint = DescribeEndpoint(url);
            var stopwatch = Stopwatch.StartNew();
            Debug.WriteLine("GET:" + endpoint);
            Utils.AddALog("[GET]" + requestId + " " + endpoint);
            try
            {
                using (var client = new HttpClient())
                {
                    if (headers != null)
                    {
                        foreach (var item in headers)
                        {
                            client.DefaultRequestHeaders.Add(item.Key, item.Value);
                        }
                    }

                    var response = await client.GetAsync(new Uri(url));
                    LogRequest(requestId, "GET", endpoint, "status=" + (int)response.StatusCode + " durationMs=" + stopwatch.ElapsedMilliseconds);
                    response.EnsureSuccessStatusCode();
                    var buffer = await response.Content.ReadAsBufferAsync();
                    return Encoding.UTF8.GetString(buffer.ToArray());
                }


            }
            catch (Exception ex)
            {
                LogRequest(requestId, "GET", endpoint, "string failed durationMs=" + stopwatch.ElapsedMilliseconds, ex);

                return null;

            }



        }

        /// <summary>
        /// 发送一个POST请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <param name="headers"></param>
        /// <param name="cookie"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public async static Task<HttpResults> Post(string url, string body, IDictionary<string, string> headers = null, string contentType = "application/x-www-form-urlencoded")
        {
            var requestId = NewRequestId();
            var endpoint = DescribeEndpoint(url);
            var stopwatch = Stopwatch.StartNew();
            Debug.WriteLine("POST:" + endpoint);
            Utils.AddALog("[POST]" + requestId + " " + endpoint);
            try
            {
                using (var client = new HttpClient())
                {
                    if (headers != null)
                    {
                        foreach (var item in headers)
                        {
                            client.DefaultRequestHeaders.Add(item.Key, item.Value);
                        }
                    }
                    var response = await client.PostAsync(new Uri(url), new HttpStringContent(body, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded"));
                    LogRequest(requestId, "POST", endpoint, "status=" + (int)response.StatusCode + " durationMs=" + stopwatch.ElapsedMilliseconds);
                    if (!response.IsSuccessStatusCode)
                    {
                        LogRequest(requestId, "POST", endpoint, "HTTP failure status=" + (int)response.StatusCode + " " + response.StatusCode);
                        return new HttpResults()
                        {
                            code = (int)response.StatusCode,
                            status = false,
                            message = StatusCodeToMessage((int)response.StatusCode),
                            RequestId = requestId,
                            Method = "POST",
                            Endpoint = endpoint
                        };
                    }
                    var buffer = await response.Content.ReadAsBufferAsync();
                    var byteArray = buffer.ToArray();

                    HttpResults httpResults = new HttpResults()
                    {
                        RequestId = requestId,
                        Method = "POST",
                        Endpoint = endpoint,
                        code = (int)response.StatusCode,
                        status = response.StatusCode == HttpStatusCode.Ok,
                        results = Encoding.UTF8.GetString(byteArray),
                        message = ""
                    };
                    LogApiFailure(requestId, "POST", endpoint, httpResults.results);
                    return httpResults;
                }
            }
            catch (Exception ex)
            {
                LogRequest(requestId, "POST", endpoint, "failed durationMs=" + stopwatch.ElapsedMilliseconds, ex);
                return new HttpResults()
                {
                    RequestId = requestId,
                    Method = "POST",
                    Endpoint = endpoint,
                    code = ex.HResult,
                    status = false,
                    message = "网络请求出现错误(POST)"
                };
            }
        }

        private static string StatusCodeToMessage(int code)
        {

            switch (code)
            {
                case 0:
                case 200:
                    return "请求成功";
                case 412:
                    return "请求太频繁，请稍后再试（412）";
                case 504:
                    return "请求超时了";
                case 301:
                case 302:
                case 303:
                case 305:
                case 306:
                case 400:
                case 401:
                case 402:
                case 403:
                case 404:
                case 500:
                case 501:
                case 502:
                case 503:
                case 505:
                    return "网络请求失败，响应代码:" + code;
                case -2147012867:
                case -2147012889:
                    return "请检查的网络连接";
                default:
                    return "未知错误,响应代码：" + code;
            }
        }
    }

    public class HttpResults
    {
        public string RequestId { get; set; }
        public string Method { get; set; }
        public string Endpoint { get; set; }
        public int code { get; set; }
        public string message { get; set; }
        public string results { get; set; }
        public bool status { get; set; }
        //处理两个json的情况
        public JObject FindLongestValidJsonString(string input)
        {
            JObject obj = null;
            int start = 0;
            int end = 0;
            int maxLength = 0;
            int depth = 0;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '{' || input[i] == '[')
                {
                    depth++;

                    if (depth == 1)
                    {
                        start = i;
                    }
                }
                else if (input[i] == '}' || input[i] == ']')
                {
                    depth--;

                    if (depth <= 0)
                    {
                        end = i;

                        try
                        {
                            int sublength = end - start + 1;
                            if (sublength <= maxLength) continue;
                            string substring = input.Substring(start, sublength);
                            obj = JObject.Parse(substring);
                            maxLength = substring.Length;
                        }
                        catch (Exception)
                        {
                            // Ignore invalid JSON strings
                        }
                    }
                }
            }
            return obj;
        }
        public async Task<T> GetJson<T>()
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(results);
            }
            catch
            {

            }
            try
            {
                return FindLongestValidJsonString(results).ToObject<T>();
            }
            catch (Exception e)
            {
                var message = "HTTP response parse failed requestId=" + RequestId + " method=" + Method + " endpoint=" + Endpoint;
                LogHelper.Log(message, LogType.ERROR, e);
                Utils.AddALog("[JSON][FAIL]" + message);
                Console.WriteLine("解析json失败", e.ToString());
            }
            return default(T);
        }
        public JObject GetJObject()
        {
            JObject obj = JObject.Parse("{}");
            try
            {
                obj = JObject.Parse(results);
                var serialized = obj.ToString();
                return obj;
            }
            catch (Exception e)
            {
            }
            try
            {
                obj = FindLongestValidJsonString(results);
                var serialized = obj.ToString();
                return obj;
            }
            catch (Exception e)
            {
                Utils.ShowMessageToast("解析json对象失败", e.ToString());
                Console.WriteLine(obj.ToString());
                Utils.AddALog("[JSON][FAIL]" + obj.ToString());
                return null;
            }
        }
        public async Task<ApiDataModel<T>> GetData<T>()
        {
            try
            {
            return await GetJson<ApiDataModel<T>>();
        }
            catch (Exception)
            {
                return null;
            }

        }
        public async Task<ApiResultModel<T>> GetResult<T>()
        {
            try
            {
            return await GetJson<ApiResultModel<T>>();
            }
            catch (Exception)
            {
                return null;
            }

        }
    }
}

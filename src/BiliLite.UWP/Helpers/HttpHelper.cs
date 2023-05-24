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

namespace BiliLite.Helpers
{

    /// <summary>
    /// 网络请求方法封装
    /// </summary>
    public static class HttpHelper
    {
        /// <summary>
        /// 发送一个GET请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public static DateTime lasttime=System.DateTime.Now;
        public static TimeSpan dt = new TimeSpan(1);
        public async static Task<HttpResults> Get( string url)
        {
            return await Get(url, null);
        }
        public async static Task<HttpResults> Get( string url, IDictionary<string, string> headers)
        {
            //wait 
            var t=DateTime.Now;
            if (t - lasttime < dt)
            {
                await Task.Delay(dt + (lasttime - t));
            }
            Debug.WriteLine("GET:" + url);
            Utils.AddALog("[GET]" + url);
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
                        client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/113.0");
                    }
                    Console.WriteLine(url);
                    var response = await client.GetAsync(new Uri(url));
                    if (!response.IsSuccessStatusCode)
                    {
                        return new HttpResults()
                        {
                            code = (int)response.StatusCode,
                            status = false,
                            message = StatusCodeToMessage((int)response.StatusCode)
                        };
                    }

                    //response.EnsureSuccessStatusCode();
                    var buffer = await response.Content.ReadAsBufferAsync();
                    var byteArray = buffer.ToArray();
                    HttpResults httpResults = new HttpResults()
                    {
                        code = (int)response.StatusCode,
                        status = response.StatusCode == HttpStatusCode.Ok,
                        results = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length),
                        message = "",
                    };
                    return httpResults;
                }



            }
            catch (Exception ex)
            {
                LogHelper.Log("GET请求失败" + url, LogType.ERROR, ex);
                return new HttpResults()
                {
                    code = ex.HResult,
                    status = false,
                    message = "网络请求出现错误(GET)"
                };
            }
        }

        public async static Task<HttpResults> GetWithWebCookie(string url, IDictionary<string, string> headers = null)
        {
            Debug.WriteLine("GET:" + url);
            Utils.AddALog("[GET]" + url);
            try
            {
                if (headers == null)
                {
                    headers = new Dictionary<string, string>();
                }
                HttpBaseProtocolFilter fiter = new HttpBaseProtocolFilter();
                var cookies = fiter.CookieManager.GetCookies(new Uri("http://bilibili.com"));
                //没有Cookie
                if(cookies==null|| cookies.Count == 0)
                {
                    //访问一遍bilibili.com
                    await Get("https://www.bilibili.com");
                }
                cookies = fiter.CookieManager.GetCookies(new Uri("http://bilibili.com"));
                string cookie = "";
                foreach (var item in cookies)
                {
                    cookie += $"{item.Name}={item.Value};";
                }
                headers.Add("Cookie", cookie);
                return await Get(url, headers);
            }
            catch (Exception ex)
            {
                LogHelper.Log("GET请求失败" + url, LogType.ERROR, ex);
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
            Debug.WriteLine("GET:" + url);
            Utils.AddALog("[GET STREAM]" + url);
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
                    response.EnsureSuccessStatusCode();
                    return (await response.Content.ReadAsInputStreamAsync()).AsStreamForRead();
                }


            }
            catch (Exception ex)
            {
                LogHelper.Log("GET请求Stream失败" + url, LogType.ERROR, ex);
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
            Debug.WriteLine("GET:" + url);
            Utils.AddALog("[GET STREAM]" + url);
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
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsBufferAsync();
                }


            }
            catch (Exception ex)
            {
                LogHelper.Log("GET请求Stream失败" + url, LogType.ERROR, ex);
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
            Debug.WriteLine("GET:" + url);
            Utils.AddALog("[GET]" + url);
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
                    response.EnsureSuccessStatusCode();
                    var buffer = await response.Content.ReadAsBufferAsync();
                    return Encoding.UTF8.GetString(buffer.ToArray());
                }


            }
            catch (Exception ex)
            {
                LogHelper.Log("GET请求String失败" + url, LogType.ERROR, ex);

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
            Debug.WriteLine("POST:" + url + "\r\nBODY:" + body);
            Utils.AddALog("[POST]" + url + body);
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
                    if (!response.IsSuccessStatusCode)
                    {
                        Utils.AddALog("[POST][FAIL]"+url+response.StatusCode.ToString());
                        return new HttpResults()
                        {
                            code = (int)response.StatusCode,
                            status = false,
                            message = StatusCodeToMessage((int)response.StatusCode)
                        };
                    }
                    var buffer = await response.Content.ReadAsBufferAsync();
                    var byteArray = buffer.ToArray();

                    HttpResults httpResults = new HttpResults()
                    {
                        code = (int)response.StatusCode,
                        status = response.StatusCode == HttpStatusCode.Ok,
                        results = Encoding.UTF8.GetString(byteArray),
                        message = ""
                    };
                    return httpResults;
                }



            }
            catch (Exception ex)
            {
                LogHelper.Log("POST请求失败" + url, LogType.ERROR, ex);
                return new HttpResults()
                {
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
        public int code { get; set; }
        public string message { get; set; }
        public string results { get; set; }
        public bool status { get; set; }

        //处理两个json的情况
        public JObject FindLongestValidJsonString(string input)
        {
            JObject obj=null;
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
                 catch(Exception e)
                 {
                     Console.WriteLine("解析json失败", e.ToString());
                     Utils.AddALog("[JSON][FAIL]" + results.ToString().Substring(0,100));
                 }
                 return default(T);

        }
        public JObject GetJObject()
        {
            JObject obj=JObject.Parse("{}");
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
            catch(Exception e)
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

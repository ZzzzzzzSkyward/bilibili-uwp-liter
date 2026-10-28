using BiliLite.Api;
using BiliLite.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace BiliLite.Services
{
    public interface ICookieStore
    {
        void Write(string name, string value);
        string Read(string name, string defaultValue);
    }

    public sealed class WindowsCookieStore : ICookieStore
    {
        private readonly HttpBaseProtocolFilter filter;

        public WindowsCookieStore() { filter = new HttpBaseProtocolFilter(); }

        public void Write(string name, string value)
        {
            filter.CookieManager.SetCookie(new HttpCookie(name, "bilibili.com", "/") { Value = value });
        }

        public string Read(string name, string defaultValue)
        {
            var cookies = filter.CookieManager.GetCookies(new Uri("https://bilibili.com"));
            return cookies?.FirstOrDefault(cookie => cookie.Name == name)?.Value ?? defaultValue;
        }
    }

    /// <summary>Owns browser cookies and the existing browser-cookie refresh lifecycle.</summary>
    public class CookieService
    {
        private readonly ICookieStore cookieStore;
        private readonly Func<string, string> readSetting;
        private readonly Action<string, string> writeSetting;
        private readonly Func<bool> isLoggedIn;
        private readonly Func<ApiModel, Task<HttpResults>> send;
        private readonly Action<string> notify;
        private readonly Action<string, Exception> log;
        private readonly object stateLock = new object();
        private Task<bool> refreshCheck;
        private bool hasInitializedRefreshState;
        private bool needsCookieRefresh;
        private short refreshCheckAttempts;
        private Timer refreshTimer;
        private bool refreshWarningShown;

        public CookieService()
            : this(new WindowsCookieStore(), key => SettingHelper.GetValue<string>(key, ""),
                (key, value) => SettingHelper.SetValue(key, value), () => SettingHelper.Account.Logined,
                api => api.Request(), message => Utils.ShowMessageToast(message),
                (message, exception) => LogHelper.Log(message, LogType.ERROR, exception))
        {
        }

        public CookieService(ICookieStore cookieStore, Func<string, string> readSetting,
            Action<string, string> writeSetting, Func<bool> isLoggedIn,
            Func<ApiModel, Task<HttpResults>> send = null,
            Action<string> notify = null, Action<string, Exception> log = null)
        {
            this.cookieStore = cookieStore ?? throw new ArgumentNullException(nameof(cookieStore));
            this.readSetting = readSetting ?? throw new ArgumentNullException(nameof(readSetting));
            this.writeSetting = writeSetting ?? throw new ArgumentNullException(nameof(writeSetting));
            this.isLoggedIn = isLoggedIn ?? throw new ArgumentNullException(nameof(isLoggedIn));
            this.send = send ?? (api => api.Request());
            this.notify = notify ?? (_ => { });
            this.log = log ?? ((_, __) => { });
        }

        public long CookieTimestamp { get; set; }

        public bool NeedRefreshCookie
        {
            get
            {
                lock (stateLock)
                {
                    if (hasInitializedRefreshState) return needsCookieRefresh;
                }
                _ = NeedRefreshCookieAsync();
                return false;
            }
        }

        public void WriteCookie(string name, string value) => cookieStore.Write(name, value);
        public string ReadCookie(string name, string defaultValue) => cookieStore.Read(name, defaultValue);

        public string SyncCookie(string cookieName, string settingName)
        {
            var cookieValue = ReadCookie(cookieName, "");
            var settingValue = readSetting(settingName);
            if (string.IsNullOrEmpty(cookieValue) && !string.IsNullOrEmpty(settingValue))
            {
                WriteCookie(cookieName, settingValue);
                return settingValue;
            }
            if (!string.IsNullOrEmpty(cookieValue) && string.IsNullOrEmpty(settingValue))
                writeSetting(settingName, cookieValue);
            return cookieValue;
        }

        public string GetCSRF() => SyncCookie("bili_jct", "CookieCSRF");

        public void LoadCookieFromSetting()
        {
            SyncCookie("SESSDATA", "CookieSESSDATA");
            SyncCookie("bili_jct", "CookieCSRF");
        }

        public string GetRefreshToken() => readSetting("CookieRefreshToken");

        public Task<bool> NeedRefreshCookieAsync()
        {
            TaskCompletionSource<bool> producer = null;
            Task<bool> result;
            lock (stateLock)
            {
                if (hasInitializedRefreshState) return Task.FromResult(needsCookieRefresh);
                if (refreshCheck != null) return refreshCheck;
                producer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                refreshCheck = producer.Task;
                result = refreshCheck;
            }
            _ = CheckCookieRefreshStateAsync(producer);
            return result;
        }

        private async Task CheckCookieRefreshStateAsync(TaskCompletionSource<bool> producer)
        {
            var resultValue = false;
            try
            {
                if (!isLoggedIn())
                {
                    SetRefreshState(true, false);
                    return;
                }

                LoadCookieFromSetting();
                var response = await send(new ApiModel
                {
                    need_cookie = true,
                    method = RestSharp.Method.Get,
                    baseUrl = "https://passport.bilibili.com/x/passport-login/web/cookie/info"
                });
                if (response?.code == 200)
                {
                    var body = JObject.Parse(response.results ?? "");
                    var apiCode = (int?)body["code"];
                    if (!apiCode.HasValue)
                        throw new FormatException("Cookie refresh-state response was missing its API status code.");
                    if (apiCode == -101)
                    {
                        SetRefreshState(true, false);
                        return;
                    }
                    if (apiCode.HasValue && apiCode.Value != 0)
                        throw new FormatException("Cookie refresh-state response contained a non-success API code.");
                    var data = body["data"];
                    var timestamp = (long?)data?["timestamp"];
                    var refreshFlag = (bool?)data?["refresh"];
                    if (data == null || !timestamp.HasValue || timestamp.Value <= 0 || !refreshFlag.HasValue)
                        throw new FormatException("Cookie refresh-state response contained incomplete data.");
                    CookieTimestamp = timestamp ?? 0;
                    SetRefreshState(true, refreshFlag.Value);
                    resultValue = NeedRefreshCookieValue();
                    return;
                }
                if (response?.code == -101)
                {
                    SetRefreshState(true, false);
                    return;
                }

                lock (stateLock)
                {
                    refreshCheckAttempts++;
                    if (refreshCheckAttempts > 5)
                    {
                        hasInitializedRefreshState = true;
                        needsCookieRefresh = false;
                    }
                }
                resultValue = NeedRefreshCookieValue();
            }
            catch (Exception exception)
            {
                log("Cookie refresh-state check failed.", exception);
                lock (stateLock)
                {
                    refreshCheckAttempts++;
                    if (refreshCheckAttempts > 5)
                    {
                        hasInitializedRefreshState = true;
                        needsCookieRefresh = false;
                    }
                }
                resultValue = NeedRefreshCookieValue();
            }
            finally
            {
                lock (stateLock)
                {
                    if (ReferenceEquals(refreshCheck, producer.Task)) refreshCheck = null;
                }
                producer.TrySetResult(resultValue);
            }
        }

        private bool NeedRefreshCookieValue()
        {
            lock (stateLock) return needsCookieRefresh;
        }

        private void SetRefreshState(bool initialized, bool needed)
        {
            lock (stateLock)
            {
                hasInitializedRefreshState = initialized;
                needsCookieRefresh = needed;
            }
        }

        public async Task RefreshCookieAsync()
        {
            var timestamp = CookieTimestamp;
            if (timestamp == 0)
            {
                log("Cookie refresh was skipped because its timestamp is unavailable.", null);
                if (!refreshWarningShown)
                {
                    refreshWarningShown = true;
                    notify("啊哦，cookie过气了，\n而且没有刷新令牌，\n需要重新登录了呢");
                }
                return;
            }

            var correspondPath = GetCorrespondPath(timestamp);
            CookieTimestamp = 0;
            var correspond = await send(new ApiModel
            {
                method = RestSharp.Method.Get,
                baseUrl = "https://www.bilibili.com/correspond/1/" + correspondPath,
                need_cookie = true
            });
            var csrf = "";
            if (correspond?.code == 200)
            {
                var match = Regex.Match(correspond.results ?? "", @"<div id=""1-name"">([^<]+)</div>");
                if (match.Success) csrf = match.Groups[1].Value;
                else log("Cookie refresh token page did not contain its CSRF element.", null);
            }
            if (string.IsNullOrEmpty(csrf)) return;

            var token = GetRefreshToken();
            if (string.IsNullOrEmpty(token)) return;
            var refresh = await send(new ApiModel
            {
                need_cookie = true,
                method = RestSharp.Method.Post,
                baseUrl = "https://passport.bilibili.com/x/passport-login/web/cookie/refresh",
                body = $"csrf={GetCSRF()}&refresh_csrf={csrf}&source=main_web&refresh_token={token}"
            });
            var newToken = "";
            if (refresh?.code == 200)
            {
                var body = JObject.Parse(refresh.results ?? "");
                var apiCode = (int?)body["code"];
                if (apiCode == 0)
                    newToken = (string)body["data"]?["refresh_token"] ?? "";
                else if (apiCode == -101) log("Cookie refresh reported that the account is logged out.", null);
                else if (apiCode == -111) log("Cookie refresh reported a CSRF validation failure.", null);
            }
            if (string.IsNullOrEmpty(newToken)) return;

            writeSetting("CookieRefreshToken", newToken);
            var confirm = await send(new ApiModel
            {
                need_cookie = true,
                method = RestSharp.Method.Post,
                baseUrl = "https://passport.bilibili.com/x/passport-login/web/confirm/refresh",
                body = $"csrf={GetCSRF()}&refresh_token={token}"
            });
            if (confirm?.code == 200)
            {
                var confirmBody = JObject.Parse(confirm.results ?? "{}");
                var confirmCode = (int?)confirmBody["code"];
                if (confirmCode == 0)
                {
                    SetRefreshState(true, false);
                    return;
                }
            }
            log("Cookie refresh confirmation did not return a successful response.", null);
        }

        private static string GetCorrespondPath(long timestamp)
        {
            var data = Encoding.UTF8.GetBytes("refresh_" + timestamp);
            const string modulus = "y4HdjgJHBlbaBN04VERG4qNBIFHP6a3GozCl75AihQloSWCXC5HDNgyinEnhaQ_4-gaMud_GF50elYXLlCToR9se9Z8z433U3KjM-3Yx7ptKkmQNAMggQwAVKgq3zYAoidNEWuxpkY_mAitTSRLnsJW-NCTa0bqBFF6Wm1MxgfE";
            const string exponent = "AQAB";
            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(new RSAParameters { Modulus = DecodeBase64Url(modulus), Exponent = DecodeBase64Url(exponent) });
                return BitConverter.ToString(rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256)).Replace("-", "").ToLowerInvariant();
            }
        }

        private static byte[] DecodeBase64Url(string value)
        {
            var base64 = value.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '='));
        }

        public void StartAutoRefresh(TimeSpan? interval = null)
        {
            lock (stateLock)
            {
                if (refreshTimer != null) return;
                hasInitializedRefreshState = false;
                refreshTimer = new Timer((interval ?? TimeSpan.FromDays(1)).TotalMilliseconds)
                {
                    AutoReset = true,
                    Enabled = true
                };
                refreshTimer.Elapsed += (_, __) =>
                {
                    lock (stateLock) hasInitializedRefreshState = false;
                };
            }
        }
    }
}

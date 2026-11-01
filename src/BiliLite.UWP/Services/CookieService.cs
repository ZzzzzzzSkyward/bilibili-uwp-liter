using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Models;
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
            // 根域和 www 各落一份：写根域时 WinRT 不会顺手给 www 补副本
            foreach (var host in CookieOrigins.WriteHosts)
                filter.CookieManager.SetCookie(new HttpCookie(name, host, "/") { Value = value });
        }

        public string Read(string name, string defaultValue)
        {
            // 登录在 www 上发的 host-only cookie 只有按 www 查询才看得见，根域查不到
            foreach (var origin in CookieOrigins.All)
            {
                var cookies = filter.CookieManager.GetCookies(new Uri(origin));
                var value = CookieOrigins.FirstValue(cookies?.Select(cookie => cookie.Name == name ? cookie.Value : null));
                if (!string.IsNullOrEmpty(value)) return value;
            }
            return defaultValue;
        }
    }

    /// <summary>Owns browser cookies and the existing browser-cookie refresh lifecycle.</summary>
    public class CookieService
    {
        private readonly ICookieStore cookieStore;
        private readonly AccountApi accountApi = new AccountApi();
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
        private Task refreshFlow;
        // 会话是否已死是进程级事实：多个 CookieService 实例（ApiHelper、设置页）看到的是同一个结论
        private static readonly object reloginLock = new object();
        private static bool needsRelogin;
        private static EventHandler reloginRequired;

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

        /// <summary>服务端已经判定这个会话不可用（-101/-111 或刷新令牌缺失），只能重新登录。</summary>
        public static bool NeedsRelogin
        {
            get { lock (reloginLock) return needsRelogin; }
        }

        /// <summary>只在 NeedsRelogin 由 false 变 true 时触发一次。</summary>
        public static event EventHandler ReloginRequired
        {
            add { lock (reloginLock) reloginRequired += value; }
            remove { lock (reloginLock) reloginRequired -= value; }
        }

        /// <summary>重新登录成功后清掉失效结论，下一次失效仍能再触发一次事件。</summary>
        public static void ClearReloginRequired()
        {
            lock (reloginLock) needsRelogin = false;
        }

        private void MarkRelogin(string reason)
        {
            SetRefreshState(true, false);
            log("Cookie session unusable: " + reason, null);
            EventHandler handler;
            lock (reloginLock)
            {
                if (needsRelogin) return;
                needsRelogin = true;
                handler = reloginRequired;
            }
            if (handler == null)
            {
                // 没有宿主订阅（探针、无 UI 进程）时才由服务自己提示
                notify("登录已失效");
                return;
            }
            try
            {
                handler(null, EventArgs.Empty);
            }
            catch (Exception exception)
            {
                log("Re-login notification handler failed.", exception);
            }
        }

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
            if (string.IsNullOrEmpty(cookieValue))
            {
                if (string.IsNullOrEmpty(settingValue)) return "";
                // 浏览器里还没有这项时，用手动填写的设置补一个
                WriteCookie(cookieName, settingValue);
                return settingValue;
            }
            // cookie 才是权威来源：刷新出来的新会话必须覆盖设置里的旧值
            if (!string.Equals(settingValue, cookieValue, StringComparison.Ordinal))
                writeSetting(settingName, cookieValue);
            return cookieValue;
        }

        public string GetCSRF() => SyncCookie("bili_jct", "CookieCSRF");

        public void LoadCookieFromSetting()
        {
            SyncCookie("SESSDATA", "CookieSESSDATA");
            SyncCookie("bili_jct", "CookieCSRF");
            MirrorRefreshToken();
        }

        /// <summary>ac_time_value 只存在于 cookie 里时才算真正拿到刷新令牌，登录成功后必须落到设置。</summary>
        public void MirrorRefreshToken()
        {
            var cookieValue = ReadCookie("ac_time_value", "");
            if (string.IsNullOrEmpty(cookieValue)) return;
            if (string.Equals(readSetting("CookieRefreshToken"), cookieValue, StringComparison.Ordinal)) return;
            writeSetting("CookieRefreshToken", cookieValue);
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
                var response = await send(accountApi.CheckCookies(GetCSRF()));
                if (response?.code == 200)
                {
                    var body = JObject.Parse(response.results ?? "");
                    var apiCode = (int?)body["code"];
                    if (!apiCode.HasValue)
                        throw new FormatException("Cookie refresh-state response was missing its API status code.");
                    if (apiCode == -101 || apiCode == -111)
                    {
                        MarkRelogin("cookie/info code=" + apiCode);
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
                    // 服务端认这个会话，之前记下的失效结论作废
                    ClearReloginRequired();
                    // 有效的会话原本没有任何日志，刷新链能不能起来只能靠这一行判断
                    var refreshToken = GetRefreshToken();
                    log("Cookie refresh state refresh=" + refreshFlag.Value + " timestamp=" + timestamp.Value +
                        "; token=" + (string.IsNullOrEmpty(refreshToken) ? "missing" : "present"), null);
                    SetRefreshState(true, refreshFlag.Value);
                    resultValue = NeedRefreshCookieValue();
                    return;
                }
                if (response?.code == -101 || response?.code == -111)
                {
                    MarkRelogin("cookie/info transport code=" + response?.code);
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
                refreshCheckAttempts = 0;
            }
        }

        /// <summary>单飞：并发调用者等同一次刷新，刷新一结束（成功或失败）就释放槽位，之后的重试仍会重新走完整流程。</summary>
        public Task RefreshCookieAsync()
        {
            TaskCompletionSource<bool> producer;
            Task result;
            lock (stateLock)
            {
                if (refreshFlow != null) return refreshFlow;
                producer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                refreshFlow = producer.Task;
                result = refreshFlow;
            }
            _ = RunRefreshCookieAsync(producer);
            return result;
        }

        /// <summary>校验会话状态并在需要时刷新，供启动、恢复和设置页“刷新登录”共用。</summary>
        public async Task RefreshLoginAsync()
        {
            lock (stateLock)
            {
                hasInitializedRefreshState = false;
            }
            if (await NeedRefreshCookieAsync()) await RefreshCookieAsync();
        }

        private async Task RunRefreshCookieAsync(TaskCompletionSource<bool> producer)
        {
            try
            {
                await RefreshCookieCoreAsync();
            }
            catch (Exception exception)
            {
                log("Cookie refresh failed.", exception);
            }
            finally
            {
                // 先释放槽位再放行等待者，否则紧接着的重试会拿到刚完成的旧任务
                lock (stateLock)
                {
                    if (ReferenceEquals(refreshFlow, producer.Task)) refreshFlow = null;
                }
                producer.TrySetResult(true);
            }
        }

        private async Task RefreshCookieCoreAsync()
        {
            var timestamp = CookieTimestamp;
            if (timestamp == 0)
            {
                MarkRelogin("refresh timestamp unavailable");
                return;
            }

            var correspondPath = GetCorrespondPath(timestamp);
            var correspond = await send(accountApi.RefreshCsrf(correspondPath));
            var csrf = "";
            if (correspond?.code == 200)
            {
                var match = Regex.Match(correspond.results ?? "", @"<div id=""1-name"">([^<]+)</div>");
                if (match.Success) csrf = match.Groups[1].Value;
                else log("Cookie refresh token page did not contain its CSRF element.", null);
            }
            if (string.IsNullOrEmpty(csrf)) return;

            var token = GetRefreshToken();
            if (string.IsNullOrEmpty(token))
            {
                // cookie 里可能已经有 ac_time_value，只是没落到设置
                MirrorRefreshToken();
                token = GetRefreshToken();
            }
            if (string.IsNullOrEmpty(token))
            {
                MarkRelogin("refresh token missing");
                return;
            }

            var refresh = await send(accountApi.RefreshCookie(GetCSRF(), csrf, token));
            var newToken = "";
            if (refresh?.code == 200)
            {
                var body = JObject.Parse(refresh.results ?? "");
                var apiCode = (int?)body["code"];
                if (apiCode == 0)
                    newToken = (string)body["data"]?["refresh_token"] ?? "";
                else if (apiCode == -101 || apiCode == -111)
                {
                    MarkRelogin("cookie/refresh code=" + apiCode);
                    return;
                }
            }
            else if (refresh?.code == -101 || refresh?.code == -111)
            {
                MarkRelogin("cookie/refresh transport code=" + refresh.code);
                return;
            }
            if (string.IsNullOrEmpty(newToken)) return;

            writeSetting("CookieRefreshToken", newToken);
            var confirm = await send(accountApi.ConfirmRefreshCookie(GetCSRF(), token));
            if (confirm?.code == 200)
            {
                var confirmBody = JObject.Parse(confirm.results ?? "{}");
                var confirmCode = (int?)confirmBody["code"];
                if (confirmCode == 0)
                {
                    CookieTimestamp = 0;
                    ClearReloginRequired();
                    SetRefreshState(true, false);
                    return;
                }
                if (confirmCode == -101 || confirmCode == -111)
                {
                    MarkRelogin("confirm/refresh code=" + confirmCode);
                    return;
                }
            }
            else if (confirm?.code == -101 || confirm?.code == -111)
            {
                MarkRelogin("confirm/refresh transport code=" + confirm.code);
                return;
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

    /// <summary>一次播放地址请求（含多轮代理重试）只预刷新一次 cookie。</summary>
    public sealed class CookiePreflight
    {
        private bool done;

        public async Task EnsureAsync(Func<Task<bool>> needRefresh, Func<Task> refresh)
        {
            if (done) return;
            done = true;
            if (await needRefresh()) await refresh();
        }
    }
}

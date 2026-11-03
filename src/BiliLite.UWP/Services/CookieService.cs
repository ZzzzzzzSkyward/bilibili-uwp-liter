using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
        private readonly RefreshTokenMirror refreshTokenMirror;
        private readonly Func<bool> isLoggedIn;
        private readonly Func<ApiModel, Task<HttpResults>> send;
        private readonly Action<string> notify;
        private readonly Action<string, Exception> log;
        private readonly IWebRefreshStateStore webRefreshState;
        private readonly Action<WebRefreshPhase> observePhase;
        private readonly object stateLock = new object();
        private Task<bool> refreshCheck;
        private bool hasInitializedRefreshState;
        private bool needsCookieRefresh;
        private short refreshCheckAttempts;
        private Timer refreshTimer;
        private Task refreshFlow;
        private Task<bool> resumeFlow;
        // 一次启动内的确认次数：结果未知才会有下一次，重启后重新计数，避免持久记录把刷新永久钉死
        private int confirmAttempts;
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
            Action<string> notify = null, Action<string, Exception> log = null,
            IWebRefreshStateStore webRefreshState = null, Action<WebRefreshPhase> observePhase = null)
        {
            this.cookieStore = cookieStore ?? throw new ArgumentNullException(nameof(cookieStore));
            this.readSetting = readSetting ?? throw new ArgumentNullException(nameof(readSetting));
            this.writeSetting = writeSetting ?? throw new ArgumentNullException(nameof(writeSetting));
            this.refreshTokenMirror = new RefreshTokenMirror(this.readSetting, this.writeSetting);
            this.isLoggedIn = isLoggedIn ?? throw new ArgumentNullException(nameof(isLoggedIn));
            this.send = send ?? (api => api.Request());
            this.notify = notify ?? (_ => { });
            this.log = log ?? ((_, __) => { });
            this.webRefreshState = webRefreshState ?? new LocalFolderWebRefreshStateStore();
            this.observePhase = observePhase ?? (_ => { });
        }

        public long CookieTimestamp { get; set; }

        /// <summary>服务端已经判定这个会话不可用（-101/-111 或刷新令牌缺失），只能重新登录。</summary>
        public static bool NeedsRelogin
        {
            get { lock (reloginLock) return needsRelogin; }
        }

        /// <summary>
        /// 会话已死时 web playurl 只会给到 480p 上限，code 0 照样算成功；
        /// 列表里没有请求的画质就说明这份结果不是结论，得让 gRPC 再说一次。
        /// </summary>
        public static bool WebPlayUrlCappedOnDeadSession(int requestedQuality, IEnumerable<int> availableQualities)
        {
            if (!NeedsRelogin || availableQualities == null) return false;
            return !availableQualities.Contains(requestedQuality);
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

        /// <summary>
        /// ac_time_value 只存在于 cookie 里时才算真正拿到刷新令牌，登录成功后必须落到设置。
        /// 但它是旧会话的迁移来源：设置里已有响应令牌、有挂起确认或归属对不上时，这里一个键都不动。
        /// </summary>
        public RefreshTokenDecision MirrorRefreshToken()
        {
            return refreshTokenMirror.MirrorFromCookie(ReadCookie("ac_time_value", ""),
                RefreshTokenPolicy.ParseOwner(ReadCookie("DedeUserID", "")), ReadSessionOwnerId(), HasPendingConfirmation());
        }

        /// <summary>挂起确认记录是不是这一代会话的：判定与“读失败即当有”都由 WebRefreshTransaction 一处负责。</summary>
        private bool HasPendingConfirmation()
            => WebRefreshTransaction.HasPendingConfirmation(webRefreshState, ReadSessionOwnerId(), log);

        /// <summary>会话归属就是设置里的 uid：cookie 迁移和响应令牌落盘都按它记。</summary>
        private long ReadSessionOwnerId() => RefreshTokenPolicy.ParseOwner(readSetting(SettingHelper.Account.USER_ID));

        /// <summary>
        /// 提交路径的唯一取令牌口：归属和当前会话对不上就不交（由镜像按 uid 判定），
        /// 所以 cookie/refresh 永远不会带上一个不属于这个账号的令牌。
        /// </summary>
        public string GetRefreshToken() => refreshTokenMirror.ReadToken(ReadSessionOwnerId());

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
            // 有挂起确认就先做完那一代：既不开新一轮轮换，也不假装它已经成功
            if (await TryResumePendingConfirmationAsync()) return;
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
                if (await TryResumePendingConfirmationAsync()) return;
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

            OnPhase(WebRefreshPhase.Checked);
            confirmAttempts = 0;

            var refresh = await send(accountApi.RefreshCookie(GetCSRF(), csrf, token));
            var refreshVerdict = WebRefreshTransaction.ClassifyConfirmation(refresh?.code ?? 0, refresh?.results);
            if (refreshVerdict.Outcome == ConfirmationOutcome.Rejected)
            {
                MarkRelogin("cookie/refresh " + refreshVerdict.Detail);
                return;
            }

            // 响应的令牌和 jar 里装上的替代会话一起验收：半份响应不算一次换代，也就什么都不落盘
            WebRefreshReplacement replacement;
            string refusal;
            if (!WebRefreshTransaction.TryAcceptReplacement(refresh?.results, cookieStore, ReadSessionOwnerId(),
                    out replacement, out refusal))
            {
                log("Cookie refresh did not produce a usable replacement: " + refusal + ".", null);
                return;
            }
            OnPhase(WebRefreshPhase.ReplacementReceived);

            var newToken = replacement.Token;
            var ownerId = ReadSessionOwnerId();
            var record = WebRefreshTransaction.CreatePendingRecord(newToken, replacement.UserId, replacement.CsrfDigest,
                token, ownerId, WebRefreshPhase.ReplacementCommitted, DateTimeOffset.UtcNow);

            // 先落挂起记录再动本地令牌：崩在这两步之间也能从记录里把这一代补回来
            TrySaveRecord(record, "replacement");

            // 响应里的令牌对这个会话是权威的：来源和归属一起落盘，jar 里旧的 ac_time_value 之后顶不掉它
            refreshTokenMirror.RecordResponse(newToken, ownerId);
            OnPhase(WebRefreshPhase.ReplacementCommitted);

            record.Phase = WebRefreshTransaction.FormatPhase(WebRefreshPhase.ConfirmationPending);
            TrySaveRecord(record, "confirmation-pending");
            OnPhase(WebRefreshPhase.ConfirmationPending);

            await ConfirmPendingAsync(record);
        }

        /// <summary>
        /// 单飞：并发调用者等同一次续做，续完（成功、拒绝或未知）才释放槽位。
        /// 返回值是「本来有挂起确认」，调用方据此不再开新一轮轮换。
        /// </summary>
        private Task<bool> TryResumePendingConfirmationAsync()
        {
            TaskCompletionSource<bool> producer;
            Task<bool> result;
            lock (stateLock)
            {
                if (resumeFlow != null) return resumeFlow;
                producer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                resumeFlow = producer.Task;
                result = resumeFlow;
            }
            _ = RunResumeAsync(producer);
            return result;
        }

        private async Task RunResumeAsync(TaskCompletionSource<bool> producer)
        {
            var pending = true;
            try
            {
                pending = await ResumePendingConfirmationAsync();
            }
            catch (Exception exception)
            {
                // 续做本身出错时宁可当成「还有挂起」：开始新一轮轮换才是不可逆的那一步
                log("Cookie refresh resume failed.", exception);
            }
            finally
            {
                lock (stateLock)
                {
                    if (ReferenceEquals(resumeFlow, producer.Task)) resumeFlow = null;
                }
                producer.TrySetResult(pending);
            }
        }

        /// <summary>
        /// 把挂起确认做完。记录里存着换代前的令牌，所以这里既不用重新刷新，也不会换一枚别的令牌去确认。
        /// </summary>
        private async Task<bool> ResumePendingConfirmationAsync()
        {
            WebRefreshPendingRecord record;
            try
            {
                record = WebRefreshTransaction.Load(webRefreshState);
            }
            catch (FormatException exception)
            {
                // 读不出来的记录已经没法续，留着只会把刷新挡死
                log("Pending cookie confirmation record was unreadable; dropping it.", exception);
                ClearPendingRecord("unreadable");
                return false;
            }
            catch (Exception exception)
            {
                log("Pending cookie confirmation record could not be read; not starting a new rotation.", exception);
                return true;
            }

            if (record == null) return false;
            if (!WebRefreshTransaction.IsPendingPhase(record.Phase))
            {
                log("Pending cookie confirmation record was in phase " + record.Phase + "; dropping it.", null);
                ClearPendingRecord("stale-phase");
                return false;
            }
            if (string.IsNullOrEmpty(record.PreviousToken) || string.IsNullOrEmpty(record.ReplacementToken))
            {
                log("Pending cookie confirmation record was incomplete; dropping it.", null);
                ClearPendingRecord("incomplete");
                return false;
            }

            var ownerId = ReadSessionOwnerId();
            if (!WebRefreshTransaction.BelongsToSession(record, ownerId))
            {
                // 另一个账号的挂起确认：不发确认、不删记录、也不替它开始轮换
                log("Pending cookie confirmation belongs to another session; leaving it untouched.", null);
                return true;
            }

            // 崩在本地落盘之前时补上这一代；这一段会话已经有别的令牌就说明记录属于被换掉的旧一代
            var saved = refreshTokenMirror.ReadSaved();
            var committed = string.Equals(saved.Token, record.ReplacementToken, StringComparison.Ordinal);
            if (!committed && (string.IsNullOrEmpty(saved.Token) ||
                string.Equals(saved.Token, record.PreviousToken, StringComparison.Ordinal)))
            {
                refreshTokenMirror.RecordResponse(record.ReplacementToken, ownerId);
                OnPhase(WebRefreshPhase.ReplacementCommitted);
            }
            else if (!committed)
            {
                log("Pending cookie confirmation was superseded by a newer token; dropping it.", null);
                ClearPendingRecord("superseded");
                return false;
            }

            record.Phase = WebRefreshTransaction.FormatPhase(WebRefreshPhase.ConfirmationPending);
            if (record.Attempts == 0) TrySaveRecord(record, "resume");
            OnPhase(WebRefreshPhase.ConfirmationPending);

            await ConfirmPendingAsync(record);
            return true;
        }

        /// <summary>
        /// 用替代会话自己的 CSRF 和换代前的那枚令牌确认。记录只在服务端明确成功时清；
        /// 服务端明确说会话已死时清掉记「要重新登录」；其余一切（传输失败、残缺响应、别的业务码）都留着记录。
        /// </summary>
        private async Task ConfirmPendingAsync(WebRefreshPendingRecord record)
        {
            if (confirmAttempts >= WebRefreshTransaction.MaxConfirmationAttempts)
            {
                log("Cookie refresh confirmation retry limit reached for this run; keeping the pending record.", null);
                return;
            }

            string mismatch;
            if (!WebRefreshTransaction.MatchesReplacementSession(cookieStore, record, out mismatch))
            {
                log("Cookie refresh confirmation was not sent: " + mismatch + ". Keeping the pending record.", null);
                return;
            }

            confirmAttempts++;
            record.Attempts++;
            TrySaveRecord(record, "attempt");

            var confirm = await send(accountApi.ConfirmRefreshCookie(GetCSRF(), record.PreviousToken));
            var verdict = WebRefreshTransaction.ClassifyConfirmation(confirm?.code ?? 0, confirm?.results);
            if (verdict.Outcome == ConfirmationOutcome.Confirmed)
            {
                ClearPendingRecord("confirmed");
                CookieTimestamp = 0;
                ClearReloginRequired();
                SetRefreshState(true, false);
                OnPhase(WebRefreshPhase.Confirmed);
                return;
            }
            if (verdict.Outcome == ConfirmationOutcome.Rejected)
            {
                ClearPendingRecord("rejected");
                MarkRelogin("confirm/refresh " + verdict.Detail);
                return;
            }
            log("Cookie refresh confirmation outcome is uncertain (" + verdict.Detail +
                "); keeping the pending record for the next run.", null);
        }

        private bool TrySaveRecord(WebRefreshPendingRecord record, string reason)
        {
            try
            {
                WebRefreshTransaction.Save(webRefreshState, record);
                return true;
            }
            catch (Exception exception)
            {
                log("Pending cookie confirmation record could not be written (" + reason + ").", exception);
                return false;
            }
        }

        private void ClearPendingRecord(string reason)
        {
            try
            {
                WebRefreshTransaction.Clear(webRefreshState);
            }
            catch (Exception exception)
            {
                log("Pending cookie confirmation record could not be cleared (" + reason + ").", exception);
            }
        }

        private void OnPhase(WebRefreshPhase phase)
        {
            try
            {
                observePhase(phase);
            }
            catch (Exception exception)
            {
                log("Cookie refresh phase observer failed.", exception);
            }
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

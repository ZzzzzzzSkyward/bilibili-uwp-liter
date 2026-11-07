using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BiliLite.Services
{
    /// <summary>
    /// 一次 web 刷新换代的阶段，顺序就是枚举顺序：检查过、拿到替代、替代已落盘、挂起确认、已确认。
    /// 终局只有三种：服务端确认成功、服务端明确拒绝（-101/-111）、结果未知。
    /// </summary>
    public enum WebRefreshPhase
    {
        /// <summary>cookie/info 的 timestamp 和 correspond 页的 CSRF 都拿到了，换代还值不值得做的判断有了依据。</summary>
        Checked,

        /// <summary>刷新响应里的令牌和 jar 里装上的替代会话一起校验通过。</summary>
        ReplacementReceived,

        /// <summary>替代令牌按 D4 的归属规则落了本地设置（response 来源 + 会话 uid）。</summary>
        ReplacementCommitted,

        /// <summary>挂起确认记录已落盘，这一代随时可以从记录里续上。</summary>
        ConfirmationPending,

        /// <summary>服务端明确回了成功，记录已清。</summary>
        Confirmed,
    }

    /// <summary>确认这一步的三种结论：成功、服务端说不行、不知道。</summary>
    public enum ConfirmationOutcome
    {
        Confirmed,
        Rejected,
        Uncertain,
    }

    /// <summary>确认结论和它的依据，依据里只有状态码，没有任何令牌内容。</summary>
    public struct ConfirmationVerdict
    {
        public ConfirmationOutcome Outcome;
        public string Detail;
    }

    /// <summary>
    /// 换回来的那一代：响应 JSON 里的替代令牌 + jar 里装上的替代会话（uid 和它自己的 CSRF）。
    /// 两个都必须成立，缺一个都不能算换代完成。
    /// </summary>
    public sealed class WebRefreshReplacement
    {
        public string Token { get; set; }
        public long UserId { get; set; }
        public string CsrfDigest { get; set; }
    }

    /// <summary>
    /// 挂起确认的持久记录。令牌只进文件、不进日志；确认时要用到的替代会话状态记成摘要，
    /// 这样重启之后还能核对 jar 里那一代是不是同一次换代的，而不用把 CSRF 另存一份。
    /// </summary>
    public sealed class WebRefreshPendingRecord
    {
        public int Version { get; set; }

        /// <summary>WebRefreshTransaction.FormatPhase 的写法，只有挂起的两种是可续的。</summary>
        public string Phase { get; set; }

        /// <summary>这条记录的会话归属：别的会话不能拿它去确认。</summary>
        public long UserId { get; set; }

        /// <summary>换代前的那一枚 web 刷新令牌，确认请求必须用它。</summary>
        public string PreviousToken { get; set; }

        /// <summary>响应换回来的替代令牌：崩在本地落盘之前时，靠它补上这一代。</summary>
        public string ReplacementToken { get; set; }

        /// <summary>替代会话的 uid（jar 里的 DedeUserID）。</summary>
        public long ReplacementUserId { get; set; }

        /// <summary>替代会话 CSRF 的摘要：resume 时确认 jar 里还是这一代。</summary>
        public string CsrfDigest { get; set; }

        /// <summary>这条记录已经发出过几次确认请求（诊断用，不是持久化的重试上限）。</summary>
        public int Attempts { get; set; }

        public string RecordedAt { get; set; }
    }

    /// <summary>挂起确认记录的存储口：生产是 LocalFolder 里的一个 JSON 文件，探针是内存或临时目录。</summary>
    public interface IWebRefreshStateStore
    {
        string Read();
        void Write(string json);
        void Clear();
    }

    /// <summary>ApplicationData.Current.LocalFolder 下的独立小文件：不占用 SettingHelper 的任何键。</summary>
    public sealed class LocalFolderWebRefreshStateStore : IWebRefreshStateStore
    {
        public string Read()
        {
            var path = FilePath();
            return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null;
        }

        public void Write(string json)
        {
            var path = FilePath();
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            // 先写临时文件再替换：一半写坏的记录比没有记录更难判断
            var staging = path + ".tmp";
            File.WriteAllText(staging, json ?? "", new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(staging, path, null);
            else
            {
                File.Move(staging, path);
            }
        }

        public void Clear()
        {
            var path = FilePath();
            // 写坏的半份记录（.tmp）也带着令牌，清记录就是两份一起清
            foreach (var file in new[] { path, path + ".tmp" })
                if (File.Exists(file)) File.Delete(file);
        }

        private static string FilePath() =>
            Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, WebRefreshTransaction.FileName);
    }

    /// <summary>
    /// web 刷新的换代状态机：替代会话的校验、确认结论的分类、挂起记录的读写都收在这里。
    /// 只认委托和 ICookieStore，不认 UWP UI，探针能拿真代码跑完整条链路。
    /// </summary>
    public static class WebRefreshTransaction
    {
        /// <summary>LocalFolder 下的文件名：一条会话一次换代，同一时间最多一条。</summary>
        public const string FileName = "cookie-refresh-pending.json";

        public const int Version = 1;

        /// <summary>一次启动内最多发几次确认：结果未知时才有下一次，服务端拒绝不重试。</summary>
        public const int MaxConfirmationAttempts = 3;

        public static string FormatPhase(WebRefreshPhase phase)
        {
            switch (phase)
            {
                case WebRefreshPhase.Checked: return "checked";
                case WebRefreshPhase.ReplacementReceived: return "replacement-received";
                case WebRefreshPhase.ReplacementCommitted: return "replacement-committed";
                case WebRefreshPhase.ConfirmationPending: return "confirmation-pending";
                case WebRefreshPhase.Confirmed: return "confirmed";
                default: throw new ArgumentOutOfRangeException(nameof(phase));
            }
        }

        /// <summary>只有这两种落盘阶段还需要确认：其余阶段要么还没换代，要么已经收尾。</summary>
        public static bool IsPendingPhase(string phase) =>
            string.Equals(phase, "replacement-committed", StringComparison.Ordinal) ||
            string.Equals(phase, "confirmation-pending", StringComparison.Ordinal);

        /// <summary>
        /// 记录能不能被这个会话接手。归属未知（0）是旧版本留下的，不知道就不拦；
        /// 两个 uid 都已知且不同，就绝不能拿别人的记录去发确认。
        /// </summary>
        public static bool BelongsToSession(WebRefreshPendingRecord record, long sessionOwnerId) =>
            record != null && !(record.UserId > 0 && sessionOwnerId > 0 && record.UserId != sessionOwnerId);

        /// <summary>
        /// 响应和替代会话一起验收：响应必须是成功状态码带上可用的 refresh_token，确认要用的 CSRF 必须
        /// 现在就取得到，jar 里的 DedeUserID 和当前会话的 uid 不许冲突 —— 半份响应不算一次换代。
        /// </summary>
        public static bool TryAcceptReplacement(string responseBody, ICookieStore cookies, long expectedUserId,
            out WebRefreshReplacement replacement, out string detail)
        {
            if (cookies == null) throw new ArgumentNullException(nameof(cookies));
            replacement = null;

            var apiCode = ReadApiCode(responseBody);
            if (!apiCode.HasValue)
            {
                detail = "response body carried no API status code";
                return false;
            }
            if (apiCode.Value != 0)
            {
                detail = "response code=" + apiCode.Value;
                return false;
            }

            string token;
            try
            {
                // 和密码登录的令牌交换是同一个契约：code 0 加 data.refresh_token
                token = PasswordLoginSessionValidator.ParseExchangeRefreshToken(responseBody);
            }
            catch (FormatException)
            {
                detail = "response carried no usable replacement token";
                return false;
            }

            var csrf = cookies.Read("bili_jct", "");
            if (string.IsNullOrEmpty(csrf))
            {
                detail = "replacement session is missing bili_jct";
                return false;
            }
            // uid 缺失时按「身份未知」处理（旧版本或只有设置里那份 CSRF 的会话），两种 uid 都已知才比对
            var userId = RefreshTokenPolicy.ParseOwner(cookies.Read("DedeUserID", ""));
            if (expectedUserId > 0 && userId > 0 && userId != expectedUserId)
            {
                detail = "replacement session belongs to another user";
                return false;
            }

            replacement = new WebRefreshReplacement
            {
                Token = token,
                UserId = userId,
                CsrfDigest = Digest(csrf),
            };
            detail = "";
            return true;
        }

        /// <summary>
        /// 续上一条挂起记录之前核对 jar：SESSDATA/bili_jct/uid 必须还是记录里那一代替代会话。
        /// 对不上说明 jar 已经被别的会话占了，拿它去确认等于替别人确认。
        /// </summary>
        public static bool MatchesReplacementSession(ICookieStore cookies, WebRefreshPendingRecord record, out string detail)
        {
            if (cookies == null) throw new ArgumentNullException(nameof(cookies));
            if (record == null) throw new ArgumentNullException(nameof(record));

            var csrf = cookies.Read("bili_jct", "");
            if (string.IsNullOrEmpty(csrf))
            {
                detail = "replacement session lost its bili_jct";
                return false;
            }
            if (!string.Equals(Digest(csrf), record.CsrfDigest ?? "", StringComparison.Ordinal))
            {
                detail = "replacement session csrf no longer matches the pending record";
                return false;
            }
            var userId = RefreshTokenPolicy.ParseOwner(cookies.Read("DedeUserID", ""));
            if (record.ReplacementUserId > 0 && userId > 0 && userId != record.ReplacementUserId)
            {
                detail = "replacement session user id no longer matches the pending record";
                return false;
            }
            detail = "";
            return true;
        }

        /// <summary>
        /// 确认响应的三种结论。HTTP 层的 -101/-111 和 body 里的 -101/-111 都是服务端说「这个会话不行」，
        /// 其余的传输失败、残缺 body、别的业务码都算「不知道」—— 不知道就不许清记录。
        /// </summary>
        public static ConfirmationVerdict ClassifyConfirmation(int transportCode, string body)
        {
            if (transportCode == -101 || transportCode == -111)
                return new ConfirmationVerdict { Outcome = ConfirmationOutcome.Rejected, Detail = "transport code=" + transportCode };
            if (transportCode != 200)
                return new ConfirmationVerdict { Outcome = ConfirmationOutcome.Uncertain, Detail = "http=" + transportCode };

            var apiCode = ReadApiCode(body);
            if (!apiCode.HasValue)
                return new ConfirmationVerdict { Outcome = ConfirmationOutcome.Uncertain, Detail = "unreadable response body" };
            if (apiCode.Value == 0)
                return new ConfirmationVerdict { Outcome = ConfirmationOutcome.Confirmed, Detail = "code=0" };
            if (apiCode.Value == -101 || apiCode.Value == -111)
                return new ConfirmationVerdict { Outcome = ConfirmationOutcome.Rejected, Detail = "code=" + apiCode.Value };
            return new ConfirmationVerdict { Outcome = ConfirmationOutcome.Uncertain, Detail = "code=" + apiCode.Value };
        }

        /// <summary>读挂起记录：没有返回 null；有但不合法抛 FormatException（调用方据此判定这份记录不可续）。</summary>
        public static WebRefreshPendingRecord Load(IWebRefreshStateStore store)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            var json = store.Read();
            if (string.IsNullOrWhiteSpace(json)) return null;

            WebRefreshPendingRecord record;
            try
            {
                record = JsonConvert.DeserializeObject<WebRefreshPendingRecord>(json);
            }
            catch (JsonException ex)
            {
                throw new FormatException("Pending cookie confirmation record was not valid JSON.", ex);
            }
            if (record == null || string.IsNullOrWhiteSpace(record.Phase))
                throw new FormatException("Pending cookie confirmation record carried no phase.");
            return record;
        }

        public static void Save(IWebRefreshStateStore store, WebRefreshPendingRecord record)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (record == null) throw new ArgumentNullException(nameof(record));
            store.Write(JsonConvert.SerializeObject(record, Formatting.Indented));
        }

        public static void Clear(IWebRefreshStateStore store)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            store.Clear();
        }

        /// <summary>
        /// 这个会话名下有没有一条在途的确认：阶段是挂起的那两种，且记录的归属就是它自己。
        /// 记录读不出来时算「有」—— 宁可挡住一次 cookie 迁移，也不猜记录已经没了。
        /// </summary>
        public static bool HasPendingConfirmation(IWebRefreshStateStore store, long sessionOwnerId,
            Action<string, Exception> log = null)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            try
            {
                var record = Load(store);
                return record != null && IsPendingPhase(record.Phase) && BelongsToSession(record, sessionOwnerId);
            }
            catch (Exception exception)
            {
                if (log != null) log("Pending cookie confirmation state could not be read.", exception);
                return true;
            }
        }

        /// <summary>
        /// 退出登录时作废挂起确认：记录属于刚刚离开的那个会话，留着就会在下次登录时冒充在途确认，
        /// 让下一次刷新拿一枚已经死掉的旧令牌去确认。调用方只管清自己的设置，文件不归它碰。
        /// </summary>
        public static bool DiscardPending(Action<string, Exception> log = null)
        {
            try
            {
                new LocalFolderWebRefreshStateStore().Clear();
                return true;
            }
            catch (Exception exception)
            {
                if (log != null) log("Pending cookie confirmation record could not be discarded on logout.", exception);
                return false;
            }
        }

        /// <summary>
        /// 换账号的落盘已经成功：上一个会话的挂起确认随会话一起作废。
        /// 判据是落盘前后两个已知 uid（和令牌镜像同一把尺子），不看记录自己写的归属 ——
        /// 旧版本留下的记录归属是 0，按归属判断根本认不出它是别人的。
        /// 同账号、旧值未知（0）或没有记录时一个字节都不动，调用方可以照常续做自己的确认。
        /// </summary>
        public static bool DiscardPendingOnAccountSwitch(long previousOwnerId, long sessionOwnerId,
            IWebRefreshStateStore store = null, Action<string, Exception> log = null)
        {
            if (!RefreshTokenPolicy.OwnerConflicts(previousOwnerId, sessionOwnerId)) return false;
            var state = store ?? new LocalFolderWebRefreshStateStore();
            try
            {
                if (string.IsNullOrWhiteSpace(state.Read())) return false;
                state.Clear();
                if (log != null)
                    log("Pending cookie confirmation from the previous session was retired on account switch (uid " +
                        previousOwnerId.ToString(CultureInfo.InvariantCulture) + " → " +
                        sessionOwnerId.ToString(CultureInfo.InvariantCulture) + ").", null);
                return true;
            }
            catch (Exception exception)
            {
                if (log != null) log("Pending cookie confirmation record could not be discarded on account switch.", exception);
                return false;
            }
        }

        /// <summary>新建一条挂起记录：换代前的令牌 + 替代会话的身份，阶段从本地落盘开始。</summary>
        public static WebRefreshPendingRecord CreatePendingRecord(string replacementToken, long replacementUserId,
            string csrfDigest, string previousToken, long ownerUserId, WebRefreshPhase phase, DateTimeOffset recordedAt) =>
            new WebRefreshPendingRecord
            {
                Version = Version,
                Phase = FormatPhase(phase),
                UserId = ownerUserId,
                PreviousToken = previousToken ?? "",
                ReplacementToken = replacementToken ?? "",
                ReplacementUserId = replacementUserId,
                CsrfDigest = csrfDigest ?? "",
                Attempts = 0,
                RecordedAt = recordedAt.ToString("o", CultureInfo.InvariantCulture),
            };

        private static int? ReadApiCode(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            try
            {
                return (int?)JObject.Parse(body)["code"];
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string Digest(string value)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? "")))
                    .Replace("-", "")
                    .ToLowerInvariant();
        }
    }
}

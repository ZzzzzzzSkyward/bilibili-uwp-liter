using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Services;
using Newtonsoft.Json.Linq;

internal static class Program
{
    // 换代前的令牌（确认请求必须用它）和响应换回来的替代令牌
    private const string PreviousToken = "rt-previous-generation";
    private const string ReplacementToken = "rt-replacement-generation";
    private const string PreviousCsrf = "csrf-previous";
    private const string ReplacementCsrf = "csrf-replacement";

    private static int assertions;
    private static readonly List<string> Log = new List<string>();
    private static readonly List<WebRefreshPhase> Trace = new List<WebRefreshPhase>();

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception("WEB-REFRESH failed: " + message);
    }

    private static void Main()
    {
        // 每个用例单独兜住：RED 基线要能点名每一条挂掉的钉子，而不是只报第一条
        var cases = new[]
        {
            ("happy path 的阶段顺序", (Action)HappyPathPinsThePhaseSequence),
            ("确认中断后续做同一代", InterruptedConfirmationResumesTheSameTransition),
            ("崩在本地落盘前由记录补完", CrashBeforeLocalCommitIsFinishedFromTheRecord),
            ("明确拒绝清记录并要求重新登录", ExplicitRejectionClearsTheRecordAndMarksRelogin),
            ("结果未知留着可重试的记录", TransportUncertaintyKeepsARetryableRecord),
            ("一次启动内的重试有上限", RetriesAreBoundedWithinOneRun),
            ("别人的挂起确认不许续做", OwnershipMismatchIsNeverConfirmed),
            ("别人的挂起记录不挡这一代的续期", ForeignPendingRecordDoesNotBlockTheRenewal),
            ("换账号作废上个会话的挂起确认", AccountSwitchRetiresThePendingConfirmation),
            ("残缺响应不算一次换代", PartialResponseIsNotACommit),
            ("挂起确认挡住 cookie 迁移", PendingRecordBlocksTheCookieMigration),
            ("退出登录作废挂起确认", LogoutDiscardsThePendingRecord),
            ("登录镜像在挂起确认期间不许改写", LoginMirrorRefusesWhileConfirmationPending),
            ("别人的挂起记录不挡登录镜像", ForeignPendingRecordDoesNotBlockTheMirror),
            ("读不出来的记录让镜像拒绝改写", UnreadableRecordMakesTheMirrorRefuse),
            ("令牌不进日志", TokensNeverReachTheLog),
        };

        var failures = new List<string>();
        foreach (var testCase in cases)
        {
            try
            {
                testCase.Item2();
            }
            catch (Exception exception)
            {
                failures.Add(testCase.Item1 + " → " + exception.Message);
            }
        }

        if (failures.Count > 0)
        {
            foreach (var failure in failures) Console.WriteLine("RED " + failure);
            throw new Exception("WEB-REFRESH failed: " + failures.Count + " of " + cases.Length + " cases");
        }

        Console.WriteLine("PASS " + assertions + " assertions");
    }

    /// <summary>
    /// happy path：检查过 → 拿到替代 → 替代落盘 → 挂起确认 → 已确认，
    /// 确认用的是替代会话自己的 CSRF 加上换代前的那枚令牌。
    /// </summary>
    private static void HappyPathPinsThePhaseSequence()
    {
        var fixture = NewFixture();

        fixture.Service.CookieTimestamp = 1700000000;
        fixture.Service.RefreshCookieAsync().GetAwaiter().GetResult();

        Check(Trace.SequenceEqual(new[]
        {
            WebRefreshPhase.Checked, WebRefreshPhase.ReplacementReceived,
            WebRefreshPhase.ReplacementCommitted, WebRefreshPhase.ConfirmationPending, WebRefreshPhase.Confirmed,
        }), "the happy path runs the explicit phase order; trace=" + Join(Trace));
        Check(RefreshCount(fixture) == 1, "one refresh request starts the rotation");
        Check(ConfirmCount(fixture) == 1, "the rotation is confirmed once");
        Check(!File.Exists(RecordPath()), "an explicit success leaves no pending record");
        Check(!BiliLite.Services.CookieService.NeedsRelogin, "a successful rotation is not a dead session");

        var confirm = ConfirmRequests(fixture).Single();
        Check(confirm.body.Contains("refresh_token=" + PreviousToken),
            "the confirmation carries the previous refresh token; body=" + confirm.body);
        Check(!confirm.body.Contains(ReplacementToken),
            "the confirmation never submits the replacement token as the token to confirm");
        Check(confirm.body.Contains("csrf=" + ReplacementCsrf),
            "the confirmation uses the replacement session's own CSRF; body=" + confirm.body);
        Check(Setting(fixture, RefreshTokenMirror.TokenKey) == ReplacementToken &&
            Setting(fixture, RefreshTokenMirror.SourceKey) == "response" &&
            Setting(fixture, RefreshTokenMirror.OwnerKey) == "42",
            "D4 still lands the replacement token through RecordResponse with the session owner");
    }

    /// <summary>
    /// 确认丢了（响应没回来）：留下可续的记录；下一次调用续做同一代，
    /// 用的是记录里那枚换代前的令牌，而不是开一次新的刷新。
    /// </summary>
    private static void InterruptedConfirmationResumesTheSameTransition()
    {
        var fixture = NewFixture();
        fixture.Confirm = _ => new HttpResults { code = 500, status = false, results = "" };

        fixture.Service.CookieTimestamp = 1700000000;
        fixture.Service.RefreshCookieAsync().GetAwaiter().GetResult();

        Check(File.Exists(RecordPath()), "an interrupted confirmation writes a pending record");
        var record = LoadRecord();
        Check((string)record["Phase"] == "confirmation-pending", "the record names its phase; phase=" + (string)record["Phase"]);
        Check((string)record["PreviousToken"] == PreviousToken,
            "the record keeps the previous token the confirmation needs");
        Check((string)record["ReplacementToken"] == ReplacementToken,
            "the record keeps the replacement so a crash before the local commit can still finish it");
        Check((long)record["UserId"] == 42L && (long)record["ReplacementUserId"] == 42L,
            "the record keeps the owning user id and the replacement session's id");
        Check((string)record["CsrfDigest"] != null && (string)record["CsrfDigest"] != ReplacementCsrf &&
            (string)record["CsrfDigest"] == Digest(ReplacementCsrf),
            "the CSRF is stored as a digest, not as a second credential");
        Check((int)record["Attempts"] == 1, "the record counts the confirmation attempt that was sent");

        var refreshesBefore = RefreshCount(fixture);
        fixture.Confirm = _ => Ok();
        Trace.Clear();
        fixture.Service.RefreshLoginAsync().GetAwaiter().GetResult();

        Check(RefreshCount(fixture) == refreshesBefore,
            "the next call resumes the pending transition instead of starting another refresh");
        Check(ConfirmCount(fixture) == 2, "the resumed call sends the confirmation");
        var resumed = ConfirmRequests(fixture).Last();
        Check(resumed.body.Contains("refresh_token=" + PreviousToken),
            "the resumed confirmation still uses the previous token; body=" + resumed.body);
        Check(Trace.SequenceEqual(new[] { WebRefreshPhase.ConfirmationPending, WebRefreshPhase.Confirmed }),
            "resume pins pending → confirmed; trace=" + Join(Trace));
        Check(!File.Exists(RecordPath()), "the record is cleared only after the explicit success");
        Check(Setting(fixture, RefreshTokenMirror.TokenKey) == ReplacementToken, "the replacement stays the saved token");
    }

    /// <summary>
    /// 崩在「替代落盘」之前：记录里存着替代令牌，续做时先补上再确认，不重新刷新一轮。
    /// </summary>
    private static void CrashBeforeLocalCommitIsFinishedFromTheRecord()
    {
        var fixture = NewFixture(token: PreviousToken);
        InstallReplacementCookies(fixture);
        WriteRecord("replacement-committed", PreviousToken, ReplacementToken, ReplacementCsrf, 42, 0);

        fixture.Service.RefreshLoginAsync().GetAwaiter().GetResult();

        Check(RefreshCount(fixture) == 0, "recovering a crash does not start a new rotation");
        Check(Trace.SequenceEqual(new[]
        {
            WebRefreshPhase.ReplacementCommitted, WebRefreshPhase.ConfirmationPending, WebRefreshPhase.Confirmed,
        }), "crash recovery pins committed → pending → confirmed; trace=" + Join(Trace));
        Check(Setting(fixture, RefreshTokenMirror.TokenKey) == ReplacementToken,
            "the uncommitted replacement is completed from the record");
        Check(Setting(fixture, RefreshTokenMirror.SourceKey) == "response", "the recovered token keeps its provenance");
        Check(!File.Exists(RecordPath()), "the recovered transition is cleared on success");
    }

    /// <summary>服务端明确说不行（-101/-111）：清记录、标重新登录，不是「不知道」。</summary>
    private static void ExplicitRejectionClearsTheRecordAndMarksRelogin()
    {
        var body = NewFixture();
        body.Confirm = _ => new HttpResults { code = 200, status = true, results = "{\"code\":-101}" };
        body.Service.CookieTimestamp = 1700000000;
        body.Service.RefreshCookieAsync().GetAwaiter().GetResult();

        Check(Trace.SequenceEqual(new[]
        {
            WebRefreshPhase.Checked, WebRefreshPhase.ReplacementReceived,
            WebRefreshPhase.ReplacementCommitted, WebRefreshPhase.ConfirmationPending,
        }), "a rejection ends the run at the confirmation phase; trace=" + Join(Trace));
        Check(!File.Exists(RecordPath()), "an explicit -101 rejection clears the pending record");
        Check(BiliLite.Services.CookieService.NeedsRelogin, "an explicit -101 rejection marks the session as needing relogin");
        Check(Log.Any(line => line.Contains("confirm/refresh") && line.Contains("-101")),
            "the rejection is logged with its code");

        var transport = NewFixture();
        transport.Confirm = _ => new HttpResults { code = -111, status = false, results = "" };
        transport.Service.CookieTimestamp = 1700000000;
        transport.Service.RefreshCookieAsync().GetAwaiter().GetResult();

        Check(!File.Exists(RecordPath()), "a transport-level -111 rejection also clears the pending record");
        Check(BiliLite.Services.CookieService.NeedsRelogin, "a transport-level -111 rejection is a dead session, not uncertainty");
    }

    /// <summary>
    /// 「不知道」：传输失败、残缺响应、别的业务码都留着记录，命还在，下一次还能再试。
    /// </summary>
    private static void TransportUncertaintyKeepsARetryableRecord()
    {
        var fixture = NewFixture();
        fixture.Confirm = _ => new HttpResults { code = 200, status = true, results = "{\"code\":-352}" };

        fixture.Service.CookieTimestamp = 1700000000;
        fixture.Service.RefreshCookieAsync().GetAwaiter().GetResult();

        Check(File.Exists(RecordPath()), "an unknown business code keeps the record");
        Check((string)LoadRecord()["Phase"] == "confirmation-pending", "the kept record is still a resumable phase");
        Check(!BiliLite.Services.CookieService.NeedsRelogin, "an unknown outcome is not treated as a dead session");
        Check(Log.Any(line => line.Contains("outcome is uncertain") && line.Contains("-352")),
            "the unknown outcome is logged as uncertain together with its code");

        fixture.Confirm = _ => new HttpResults { code = 200, status = true, results = "not json at all" };
        fixture.Service.RefreshLoginAsync().GetAwaiter().GetResult();
        Check(File.Exists(RecordPath()), "an unreadable confirmation response keeps the record");
        Check(ConfirmCount(fixture) == 2, "the next call retries the confirmation");

        fixture.Confirm = _ => new HttpResults { code = 500, status = false, results = "" };
        fixture.Service.RefreshLoginAsync().GetAwaiter().GetResult();
        Check(File.Exists(RecordPath()), "a transport failure keeps the record");
        Check(ConfirmCount(fixture) == 3, "a transport failure is retryable");
        Check(!BiliLite.Services.CookieService.NeedsRelogin, "no uncertain attempt marks the session dead");
    }

    /// <summary>一次启动内的重试有上限：到上限就不发，但记录还在，下次启动照旧能续。</summary>
    private static void RetriesAreBoundedWithinOneRun()
    {
        var fixture = NewFixture();
        fixture.Confirm = _ => new HttpResults { code = 500, status = false, results = "" };

        fixture.Service.CookieTimestamp = 1700000000;
        fixture.Service.RefreshCookieAsync().GetAwaiter().GetResult();
        fixture.Service.RefreshLoginAsync().GetAwaiter().GetResult();
        fixture.Service.RefreshLoginAsync().GetAwaiter().GetResult();
        Check(ConfirmCount(fixture) == 3, "the run sends three confirmation attempts; sent=" + ConfirmCount(fixture));

        fixture.Service.RefreshLoginAsync().GetAwaiter().GetResult();
        Check(ConfirmCount(fixture) == 3, "the run stops sending at the retry limit");
        Check(File.Exists(RecordPath()), "hitting the limit keeps the transition instead of dropping it");
        Check(Log.Any(line => line.Contains("retry limit")), "the retry limit is stated in the log");

        fixture.Confirm = _ => Ok();
        var restarted = fixture.NewService();
        restarted.RefreshLoginAsync().GetAwaiter().GetResult();
        Check(ConfirmCount(fixture) == 4, "the next launch retries the kept transition");
        Check(!File.Exists(RecordPath()), "the retried transition confirms and clears");
    }

    /// <summary>别人的挂起确认：不续做、不发确认、也不删记录；但不许当成本会话的在途确认。</summary>
    private static void OwnershipMismatchIsNeverConfirmed()
    {
        var fixture = NewFixture();
        WriteRecord("confirmation-pending", "rt-other-account", "rt-other-replacement", "csrf-other", 424242, 1);
        var before = File.ReadAllText(RecordPath());

        fixture.Service.RefreshLoginAsync().GetAwaiter().GetResult();

        Check(ConfirmRequests(fixture).Count() == 0, "another session's pending record is never confirmed");
        Check(RefreshCount(fixture) == 0, "the record's own transition is not resumed on its behalf");
        Check(File.ReadAllText(RecordPath()) == before, "the other session's record is left byte-identical");
        Check(Log.Any(line => line.Contains("belongs to another session")), "the refusal is stated in the log");
    }

    /// <summary>
    /// 换账号后用 B 登录：A 的挂起确认若还留着，B 的每一次续期都会被当成「确认在途」而提前返回，
    /// 重启也救不回来（文件在盘上）。所以 B 的续期必须真的开始。
    /// </summary>
    private static void ForeignPendingRecordDoesNotBlockTheRenewal()
    {
        var fixture = NewFixture();
        fixture.CheckCookieInfoBody = CookieInfo(refresh: true, timestamp: 1700000000);
        WriteRecord("confirmation-pending", "rt-account-a", "rt-account-a-replacement", "csrf-account-a", 424242, 2);

        fixture.Service.RefreshLoginAsync().GetAwaiter().GetResult();

        Check(fixture.Requests.Any(api => api.baseUrl.EndsWith("/cookie/info", StringComparison.Ordinal)),
            "another session's pending record does not block this account's cookie renewal");
        Check(RefreshCount(fixture) == 1,
            "the current account still starts its own rotation; requests=" +
            string.Join(" | ", fixture.Requests.Select(api => api.baseUrl)));
        Check(ConfirmRequests(fixture).All(api => !api.body.Contains("rt-account-a")),
            "the other account's previous token is never confirmed");
        Check(ConfirmRequests(fixture).Single().body.Contains("refresh_token=" + PreviousToken),
            "the rotation confirms this session's own previous token");
        Check(Log.Any(line => line.Contains("belongs to another session") &&
            line.Contains("not blocking this session's renewal")),
            "the foreign record is named in the log as not blocking the renewal");
    }

    /// <summary>
    /// 换账号的提交点：令牌落盘成功之后、镜像新会话之前，上一个会话的挂起确认随会话作废。
    /// 判据是落盘前后的两个已知 uid，不是记录自己写的归属 —— 旧版本写的记录归属是 0。
    /// 同账号、旧值未知、落盘失败都不许动它。
    /// </summary>
    private static void AccountSwitchRetiresThePendingConfirmation()
    {
        NewFixture();
        var store = new LocalFolderWebRefreshStateStore();
        var log = new List<string>();

        // A（424242）留下的挂起确认
        WriteRecord("confirmation-pending", "rt-account-a", "rt-account-a-replacement", "csrf-account-a", 424242, 1);
        Check(WebRefreshTransaction.DiscardPendingOnAccountSwitch(424242, 42, store, (message, exception) => log.Add(message)),
            "a successful account switch retires the previous session's pending confirmation");
        Check(!File.Exists(RecordPath()), "no confirmation from the left session survives the switch");
        Check(log.Any(line => line.Contains("account switch") && line.Contains("424242")),
            "the retirement names the session that left; log=" + string.Join(" || ", log));
        Check(log.All(line => !line.Contains("rt-account-a")),
            "the retirement log carries no token");

        // 旧版本写的记录没有归属：按归属判断认不出它是别人的，按 uid 变化就必须作废
        WriteRecord("confirmation-pending", "rt-legacy", "rt-legacy-replacement", "csrf-legacy", 0, 1);
        Check(WebRefreshTransaction.DiscardPendingOnAccountSwitch(424242, 42, store, (message, exception) => log.Add(message)),
            "a record without an owner is retired on the same evidence as an ownerless token");
        Check(!File.Exists(RecordPath()), "the legacy record does not outlive the switch");

        // 同一账号登录：一个字节都不动，D3 的续做照旧
        WriteRecord("confirmation-pending", PreviousToken, ReplacementToken, ReplacementCsrf, 42, 1);
        var sameAccount = File.ReadAllText(RecordPath());
        Check(!WebRefreshTransaction.DiscardPendingOnAccountSwitch(42, 42, store, (message, exception) => log.Add(message)),
            "a same-account login retires nothing");
        Check(File.ReadAllText(RecordPath()) == sameAccount, "the same session's resumable record is left byte-identical");

        // 旧值未知（退出登录后 uid 键还在 0）：不猜，记录留着
        Check(!WebRefreshTransaction.DiscardPendingOnAccountSwitch(0, 42, store, (message, exception) => log.Add(message)),
            "an unknown previous uid does not retire anything");
        Check(File.ReadAllText(RecordPath()) == sameAccount, "the record survives an unknown previous uid");

        // 没有记录：不算失败
        File.Delete(RecordPath());
        Check(!WebRefreshTransaction.DiscardPendingOnAccountSwitch(424242, 42, store, (message, exception) => log.Add(message)),
            "a switch with no pending record is not an error");

        // 提交点接线：作废必须排在「令牌落盘失败就直接返回」之后、镜像新会话之前
        var persist = Body(ReadSource("src/BiliLite.UWP/Modules/User/Account.cs"),
            "private static CredentialCommit PersistCredentials(", "private static void LogCommitFailure(");
        Check(persist.Length > 0, "the probe found PersistCredentials in Account");
        var commitGuard = persist.IndexOf("if (!commit.TokenSaved) return commit;", StringComparison.Ordinal);
        var retire = persist.IndexOf("DiscardPendingOnAccountSwitch(previousUserId, userid", StringComparison.Ordinal);
        var mirror = persist.IndexOf("MirrorRefreshTokenFromJar();", StringComparison.Ordinal);
        Check(commitGuard >= 0 && retire > commitGuard && mirror > retire,
            "the switch cleanup runs only after the commit succeeded and before the mirror; guard=" +
            commitGuard + " retire=" + retire + " mirror=" + mirror);
        Check(persist.IndexOf("previousUserId", StringComparison.Ordinal) < retire,
            "the cleanup is driven by the uid read before the commit, not by the record's own field");
    }

    /// <summary>
    /// 响应有令牌但 jar 里没装上替代会话：这一代不算落盘，令牌不覆盖、确认不发、记录不写。
    /// </summary>
    private static void PartialResponseIsNotACommit()
    {
        // jar 里没有替代会话（刷新响应的 Set-Cookie 没装上）：响应有令牌也不能算换代完成
        var fixture = NewFixture(sessionCookies: false);
        fixture.InstallReplacementCookies = false;
        fixture.Service.CookieTimestamp = 1700000000;
        fixture.Service.RefreshCookieAsync().GetAwaiter().GetResult();

        Check(Setting(fixture, RefreshTokenMirror.TokenKey) == PreviousToken,
            "a response without its replacement session does not overwrite the saved token");
        Check(ConfirmCount(fixture) == 0, "a partial response is never confirmed");
        Check(!File.Exists(RecordPath()), "a partial response writes no pending record");
        Check(Log.Any(line => line.Contains("did not produce a usable replacement")),
            "the refused replacement is logged with its reason");

        var tokenless = NewFixture();
        tokenless.RefreshBody = "{\"code\":0,\"data\":{}}";
        tokenless.Service.CookieTimestamp = 1700000000;
        tokenless.Service.RefreshCookieAsync().GetAwaiter().GetResult();
        Check(Setting(tokenless, RefreshTokenMirror.TokenKey) == PreviousToken &&
            ConfirmCount(tokenless) == 0 && !File.Exists(RecordPath()),
            "a success code without a refresh token commits nothing");

        var foreignSession = NewFixture();
        foreignSession.ReplacementUserId = 77;
        foreignSession.Service.CookieTimestamp = 1700000000;
        foreignSession.Service.RefreshCookieAsync().GetAwaiter().GetResult();
        Check(Setting(foreignSession, RefreshTokenMirror.TokenKey) == PreviousToken &&
            ConfirmCount(foreignSession) == 0,
            "a replacement session for another user is refused");
    }

    /// <summary>D4 的判定单元要能从持久记录里读到「确认在途」，jar 里的另一代不许改写设置。</summary>
    private static void PendingRecordBlocksTheCookieMigration()
    {
        var fixture = NewFixture(token: "rt-typed-by-hand", source: "manual");
        WriteRecord("confirmation-pending", "rt-typed-by-hand", ReplacementToken, ReplacementCsrf, 42, 1);
        fixture.Jar.Write("ac_time_value", "ac-from-jar");

        var decision = fixture.Service.MirrorRefreshToken();
        Check(decision.Reason == "confirmation-pending",
            "a pending confirmation keeps the jar out of the settings; reason=" + decision.Reason);
        Check(Setting(fixture, RefreshTokenMirror.TokenKey) == "rt-typed-by-hand",
            "the hand-entered token survives the migration attempt");

        File.Delete(RecordPath());
        Check(fixture.Service.MirrorRefreshToken().Reason != "confirmation-pending",
            "the same input is decided by the other rules once no confirmation is pending");
    }

    /// <summary>
    /// 退出登录：挂起确认记录必须一起作废（它属于刚刚离开的那个会话），令牌来历两键也回到「没有记录」。
    /// MessageCenter 是 UWP 文件，探针只能读它的源码确认 SendLogout 还在做这两件事；删除本身跑真实现。
    /// </summary>
    private static void LogoutDiscardsThePendingRecord()
    {
        WriteRecord("confirmation-pending", PreviousToken, ReplacementToken, ReplacementCsrf, 42, 1);
        File.WriteAllText(RecordPath() + ".tmp", "{\"Phase\":\"confirmation-pending\"}");
        Check(File.Exists(RecordPath()), "the fixture planted a pending record");

        Check(WebRefreshTransaction.DiscardPending(), "discarding the pending record succeeds");
        Check(!File.Exists(RecordPath()), "logout leaves no pending confirmation behind");
        Check(!File.Exists(RecordPath() + ".tmp"), "the staging copy that also carries tokens is gone");
        Check(WebRefreshTransaction.DiscardPending(), "a second logout with no record left is not an error");
        Check(!WebRefreshTransaction.HasPendingConfirmation(new LocalFolderWebRefreshStateStore(), 42),
            "a logged-out session has nothing in flight");

        var sendLogout = Body(ReadSource("src/BiliLite.UWP/Helpers/MessageCenter.cs"),
            "public static void SendLogout()", "private static void ClearCookie()");
        Check(sendLogout.Length > 0, "the probe found SendLogout in MessageCenter");
        Check(sendLogout.Contains("WebRefreshTransaction.DiscardPending"),
            "SendLogout discards the persisted confirmation");
        Check(sendLogout.Contains("ACCESS_KEY_SOURCE"), "SendLogout clears the recorded app-key provenance");
        Check(sendLogout.Contains("ACCESS_KEY_EXPIRE_ESTIMATED"), "SendLogout clears the estimated-expiry flag");
        Check(!sendLogout.Contains("File.") && !sendLogout.Contains("Directory.") &&
            !sendLogout.Contains("LocalFolderWebRefreshStateStore"),
            "SendLogout does no file IO of its own; the record's owner deletes it");
    }

    /// <summary>
    /// 登录完成后的镜像：这个会话名下有在途确认时，jar 里的 ac_time_value 不许改写设置里的那一代。
    /// </summary>
    private static void LoginMirrorRefusesWhileConfirmationPending()
    {
        // 保存的那一代是 manual：只有真的去问记录才会落到 confirmation-pending 这条判定上
        var typed = NewFixture(token: "rt-typed-by-hand", source: "manual");
        WriteRecord("confirmation-pending", PreviousToken, ReplacementToken, ReplacementCsrf, 42, 1);
        typed.Jar.Write("ac_time_value", "ac-from-jar");

        var decision = MirrorLikeLogin(typed);
        Check(decision.Reason == "confirmation-pending",
            "a same-session confirmation keeps the login mirror off the settings; reason=" + decision.Reason);
        Check(Setting(typed, RefreshTokenMirror.TokenKey) == "rt-typed-by-hand",
            "the login mirror leaves the saved token untouched");
        Check(Setting(typed, RefreshTokenMirror.SourceKey) == "manual",
            "the provenance of the saved token is untouched as well");

        // 令牌被清掉、记录还在（修复前的退出登录就是这副样子）：镜像不许把 jar 那一代搬进设置
        var cleared = NewFixture(token: "");
        WriteRecord("confirmation-pending", PreviousToken, ReplacementToken, ReplacementCsrf, 42, 1);
        cleared.Jar.Write("ac_time_value", "ac-from-jar");

        var refused = MirrorLikeLogin(cleared);
        Check(!refused.Write && Setting(cleared, RefreshTokenMirror.TokenKey) == "",
            "the login mirror does not adopt the jar generation while a confirmation is pending; reason=" + refused.Reason);

        var account = Body(ReadSource("src/BiliLite.UWP/Modules/User/Account.cs"),
            "private static void MirrorRefreshTokenFromJar()", "private static string ReadJarCookie(");
        Check(account.Contains("HasPendingConfirmation(new LocalFolderWebRefreshStateStore(), sessionOwnerId"),
            "the login mirror asks the persisted record for the session being logged in");
        Check(!account.Contains("还没有持久化记录"),
            "the comment claiming the pending state was not persisted is gone");
    }

    /// <summary>别人的挂起记录既不是这个会话的确认，也就不许挡住它的镜像。</summary>
    private static void ForeignPendingRecordDoesNotBlockTheMirror()
    {
        var fixture = NewFixture(token: "");
        WriteRecord("confirmation-pending", "rt-other", "rt-other-replacement", "csrf-other", 424242, 1);
        fixture.Jar.Write("ac_time_value", "ac-from-jar");

        Check(!WebRefreshTransaction.HasPendingConfirmation(new LocalFolderWebRefreshStateStore(), 42),
            "another user's pending record is not this session's confirmation");
        var decision = MirrorLikeLogin(fixture);
        Check(decision.Write && decision.Reason == "legacy-adoption",
            "a foreign record does not block the login mirror; reason=" + decision.Reason);
        Check(Setting(fixture, RefreshTokenMirror.TokenKey) == "ac-from-jar",
            "the session's own jar generation is adopted");
        Check(File.Exists(RecordPath()), "the other user's record is left in place");
    }

    /// <summary>读不出来的记录不许当成「没有记录」：镜像拒绝改写，等下一次能读清楚再说。</summary>
    private static void UnreadableRecordMakesTheMirrorRefuse()
    {
        var fixture = NewFixture(token: "rt-typed-by-hand", source: "manual");
        fixture.Jar.Write("ac_time_value", "ac-from-jar");
        File.WriteAllText(RecordPath(), "{\"Phase\":\"confirmation-pending\",\"UserId\":42");

        Check(WebRefreshTransaction.HasPendingConfirmation(new LocalFolderWebRefreshStateStore(), 42),
            "an unreadable record counts as a confirmation in flight instead of guessing it is gone");
        var decision = MirrorLikeLogin(fixture);
        Check(decision.Reason == "confirmation-pending",
            "an unreadable record keeps the login mirror off the settings; reason=" + decision.Reason);
        Check(Setting(fixture, RefreshTokenMirror.TokenKey) == "rt-typed-by-hand",
            "the saved token survives an unreadable record");
        Check(Log.Any(line => line.Contains("could not be read")),
            "the unreadable record is logged as unreadable, not as absent");
    }

    /// <summary>
    /// 照 Account.MirrorRefreshTokenFromJar 的接线跑真判定：会话 uid 取刚落盘的设置值，
    /// 挂起状态取持久记录。Account 是 UWP 文件，接线的源码本身由上面的用例盯住。
    /// </summary>
    private static RefreshTokenDecision MirrorLikeLogin(Fixture fixture)
    {
        var sessionOwnerId = fixture.Settings.Read(SettingHelper.Account.USER_ID, 0L);
        var mirror = new RefreshTokenMirror(key => fixture.Settings.Read(key, ""),
            (key, value) => fixture.Settings.Write(key, value));
        return mirror.MirrorFromCookie(fixture.Jar.Read("ac_time_value", ""),
            RefreshTokenPolicy.ParseOwner(fixture.Jar.Read("DedeUserID", "")),
            sessionOwnerId,
            WebRefreshTransaction.HasPendingConfirmation(new LocalFolderWebRefreshStateStore(), sessionOwnerId,
                (message, exception) => Log.Add(message)));
    }

    /// <summary>日志只写阶段、状态码和原因：令牌、CSRF 的值一个都不许出现。</summary>
    private static void TokensNeverReachTheLog()
    {
        Check(Log.Count > 0, "the batch produced log lines to inspect");
        Check(Log.All(line => !line.Contains(PreviousToken) && !line.Contains(ReplacementToken) &&
            !line.Contains(PreviousCsrf) && !line.Contains(ReplacementCsrf)),
            "no log line carries a token or a CSRF value; log=" + string.Join(" || ", Log));
        Check(Log.Any(line => line.Contains("outcome is uncertain")), "the confirmation outcome is logged by name");
        Check(Log.Any(line => line.Contains("belongs to another session")), "the ownership refusal is logged");

        var folder = WebRefreshFolder();
        Check(Directory.Exists(folder), "the pending record lives in the application's local folder");
        Check(Directory.GetFiles(folder).All(file => !Path.GetFileName(file).Contains("SettingHelper")),
            "the pending record is its own file, not a settings key");
    }

    private static BiliLite.Services.CookieService NewService(Fixture fixture)
    {
        var settings = fixture.Settings;
        var jar = fixture.Jar;
        Func<ApiModel, Task<HttpResults>> send = api => Task.FromResult(fixture.Handle(api));
#if PRE_FIX
        // 修复前的 CookieService 没有状态存储和阶段观察这两个口，RED 基线只能用它的 7 参数构造
        return new BiliLite.Services.CookieService(jar, key => settings.Read(key, ""),
            (key, value) => settings.Write(key, value), () => true, send, _ => { }, (message, exception) => Log.Add(message));
#else
        return new BiliLite.Services.CookieService(jar, key => settings.Read(key, ""),
            (key, value) => settings.Write(key, value), () => true, send, _ => { }, (message, exception) => Log.Add(message),
            null, phase => Trace.Add(phase));
#endif
    }

    private static Fixture NewFixture(string token = PreviousToken, string source = "response", bool sessionCookies = true)
    {
        ResetFolder();
        BiliLite.Services.CookieService.ClearReloginRequired();
        Trace.Clear();

        var fixture = new Fixture();
        fixture.Settings.Write(SettingHelper.Account.USER_ID, 42L);
        fixture.Settings.Write(RefreshTokenMirror.TokenKey, token);
        fixture.Settings.Write(RefreshTokenMirror.SourceKey, source);
        fixture.Settings.Write(RefreshTokenMirror.OwnerKey, "42");
        if (sessionCookies)
        {
            fixture.Jar.Write("SESSDATA", "sess-previous");
            fixture.Jar.Write("bili_jct", PreviousCsrf);
            fixture.Jar.Write("DedeUserID", "42");
        }
        fixture.Jar.Write("ac_time_value", "ac-previous");
        fixture.Service = NewService(fixture);
        return fixture;
    }

    private static void InstallReplacementCookies(Fixture fixture)
    {
        fixture.Jar.Write("SESSDATA", "sess-replacement");
        fixture.Jar.Write("bili_jct", ReplacementCsrf);
        fixture.Jar.Write("DedeUserID", fixture.ReplacementUserId.ToString());
    }

    private static int RefreshCount(Fixture fixture) => fixture.Requests.Count(api => api.baseUrl.EndsWith("/cookie/refresh", StringComparison.Ordinal));

    private static int ConfirmCount(Fixture fixture) => ConfirmRequests(fixture).Count();

    private static IEnumerable<ApiModel> ConfirmRequests(Fixture fixture) =>
        fixture.Requests.Where(api => api.baseUrl.EndsWith("/confirm/refresh", StringComparison.Ordinal));

    private static HttpResults Ok() => new HttpResults { code = 200, status = true, results = "{\"code\":0}" };

    /// <summary>cookie/info 的响应：refresh=true 表示服务端认可这个会话并且要换代。</summary>
    private static string CookieInfo(bool refresh, long timestamp) =>
        "{\"code\":0,\"data\":{\"timestamp\":" + timestamp + ",\"refresh\":" + (refresh ? "true" : "false") + "}}";

    private static string Setting(Fixture fixture, string key) => fixture.Settings.Read(key, "");

    private static JObject LoadRecord() => JObject.Parse(File.ReadAllText(RecordPath()));

    /// <summary>按生产记录的形状写一条挂起记录：探针只用 JSON 文件，不走任何设置键。</summary>
    private static void WriteRecord(string phase, string previousToken, string replacementToken, string csrf,
        long userId, int attempts)
    {
        var record = new JObject
        {
            ["Version"] = 1,
            ["Phase"] = phase,
            ["UserId"] = userId,
            ["PreviousToken"] = previousToken,
            ["ReplacementToken"] = replacementToken,
            ["ReplacementUserId"] = userId,
            ["CsrfDigest"] = Digest(csrf),
            ["Attempts"] = attempts,
            ["RecordedAt"] = "2026-01-01T00:00:00.0000000+00:00",
        };
        File.WriteAllText(RecordPath(), record.ToString());
    }

    private static string Digest(string value)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "").ToLowerInvariant();
    }

    private static string Join(IEnumerable<WebRefreshPhase> phases) =>
        string.Join(",", phases.Select(WebRefreshTransaction.FormatPhase));

    private static string WebRefreshFolder() => Windows.Storage.ApplicationData.Current.LocalFolder.Path;

    private static string RecordPath() => Path.Combine(WebRefreshFolder(), WebRefreshTransaction.FileName);

    /// <summary>UWP 侧的接线只能读源码：Account/MessageCenter 编不进 net8.0 探针。</summary>
    private static string ReadSource(string relative)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null &&
               !Directory.Exists(Path.Combine(directory.FullName, "src", "BiliLite.UWP")))
            directory = directory.Parent;
        if (directory == null) throw new InvalidOperationException("repository root not found from " + AppContext.BaseDirectory);
        return File.ReadAllText(Path.Combine(directory.FullName, relative.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string Body(string text, string start, string stop)
    {
        var from = text.IndexOf(start, StringComparison.Ordinal);
        if (from < 0) return "";
        var to = text.IndexOf(stop, from, StringComparison.Ordinal);
        return to < 0 ? text.Substring(from) : text.Substring(from, to - from);
    }

    /// <summary>每个用例一个干净的 LocalFolder：上一用例的记录不会跑进下一用例，跑完整批删掉。</summary>
    private static void ResetFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(),
            "bili-probe-webrefresh-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        Folders.Add(folder);
        Windows.Storage.ApplicationData.Current.LocalFolder.Path = folder;
    }

    private static readonly List<string> Folders = new List<string>();

    static Program()
    {
        // 正常退出和探针失败（未处理异常）两条路都要删：.NET 只在正常退出时才跑 ProcessExit
        AppDomain.CurrentDomain.ProcessExit += (sender, args) => DeleteFolders();
        AppDomain.CurrentDomain.UnhandledException += (sender, args) => DeleteFolders();
    }

    private static void DeleteFolders()
    {
        foreach (var folder in Folders)
        {
            try
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
            catch (Exception)
            {
                // 删不掉不影响结论：目录名带 GUID，下一次运行不会再撞上这一份
            }
        }
    }

    private sealed class Fixture
    {
        public readonly MemorySettings Settings = new MemorySettings();
        public readonly MemoryCookieStore Jar = new MemoryCookieStore();
        public readonly List<ApiModel> Requests = new List<ApiModel>();

        public string RefreshBody = "{\"code\":0,\"data\":{\"refresh_token\":\"" + ReplacementToken + "\"}}";
        public string CheckCookieInfoBody;
        public bool InstallReplacementCookies = true;
        public long ReplacementUserId = 42;
        public Func<ApiModel, HttpResults> Confirm = _ => new HttpResults { code = 200, status = true, results = "{\"code\":0}" };
        public BiliLite.Services.CookieService Service;

        public HttpResults Handle(ApiModel api)
        {
            Requests.Add(api);
            if (api.baseUrl.Contains("/correspond/", StringComparison.Ordinal))
                return new HttpResults { code = 200, status = true, results = "<div id=\"1-name\">page-csrf</div>" };
            if (api.baseUrl.EndsWith("/cookie/refresh", StringComparison.Ordinal))
            {
                if (InstallReplacementCookies)
                {
                    Jar.Write("SESSDATA", "sess-replacement");
                    Jar.Write("bili_jct", ReplacementCsrf);
                    Jar.Write("DedeUserID", ReplacementUserId.ToString());
                }
                return new HttpResults { code = 200, status = true, results = RefreshBody };
            }
            if (api.baseUrl.EndsWith("/confirm/refresh", StringComparison.Ordinal)) return Confirm(api);
            if (api.baseUrl.EndsWith("/cookie/info", StringComparison.Ordinal))
                return new HttpResults { code = 200, status = true, results = CheckCookieInfoBody ?? "{}" };
            return new HttpResults { code = 200, status = true, results = "{}" };
        }

        public BiliLite.Services.CookieService NewService() => Program.NewService(this);
    }

    private sealed class MemorySettings
    {
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();

        public T Read<T>(string key, T fallback)
        {
            if (!values.TryGetValue(key, out var value) || value == null) return fallback;
            if (value is T typed) return typed;
            if (typeof(T) == typeof(string)) return (T)(object)Convert.ToString(value);
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public void Write<T>(string key, T value) => values[key] = value;
    }

    private sealed class MemoryCookieStore : ICookieStore
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);

        public void Write(string name, string value) => values[name] = value;

        public string Read(string name, string defaultValue) =>
            values.TryGetValue(name, out var value) ? value : defaultValue;
    }
}

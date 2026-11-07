using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BiliLite.Helpers;
using BiliLite.Models;
using BiliLite.Services;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace AUTH4
{
    /// <summary>
    /// AUTH-04（D1）：cookie → ACCESS_KEY 的兑换、恢复决策表、来历与估算到期时间。
    /// 决策表用纯函数跑全矩阵；兑换用委托注入跑编排，再用假网络跑一次真实实现。
    /// AUTH4_MODE=legacy 时被测实现换成修复前规则的转录（Home 的 SessionDead→退出、启动只认 OAuth），
    /// 用来证明这些断言在修复前是红的。
    /// </summary>
    internal static class Program
    {
        private static int assertions;
        private static readonly List<string> failures = new List<string>();
        private static readonly List<string> log = new List<string>();
        private static bool legacy;

        private static async Task<int> Main()
        {
            legacy = string.Equals(Environment.GetEnvironmentVariable("AUTH4_MODE"), "legacy",
                StringComparison.OrdinalIgnoreCase);
            Console.WriteLine("AUTH-04 D1：cookie 兑换与恢复决策" +
                (legacy ? "（LEGACY：被测实现＝修复前规则转录）" : ""));

            DecisionTable();
            OAuthTable();
            ConversionTable();
            LegacyRuleReproduction();
            ExpiryGate();

            if (!legacy)
            {
                await BridgeOrchestration();
                await RealBridge();
                SourceChecks();
            }

            foreach (var failure in failures) Console.WriteLine("FAIL " + failure);
            if (failures.Count == 0)
            {
                Console.WriteLine("PASS " + assertions + " assertions");
                return 0;
            }
            Console.WriteLine("FAIL " + failures.Count + "/" + assertions + " assertions");
            return 1;
        }

        // ── 决策表：key 可用性 × 来历 × OAuth 令牌 × web 会话 ─────────────────────────────
        private static void DecisionTable()
        {
            // key 可用：任何来历、任何令牌、web 会话好坏都不许动它 —— 网页会刷新不等于要重新兑换
            foreach (var provenance in Provenances())
            foreach (var oauth in new[] { true, false })
            foreach (var web in new[] { true, false })
                Eq(AppKeyRecoveryAction.Continue, Decide(true, provenance, oauth, web),
                    "key 可用时一律继续（来历=" + provenance + " oauth=" + oauth + " web=" + web + "）");

            // key 不可用：12 个组合逐一钉死
            Eq(AppKeyRecoveryAction.Convert, Decide(false, AppKeyProvenance.CookieConversion, true, true),
                "cookie 来历 + 有 app 刷新令牌 + web 可用 → 先兑换（它自己那条路）");
            Eq(AppKeyRecoveryAction.RenewOAuth, Decide(false, AppKeyProvenance.CookieConversion, true, false),
                "cookie 来历 + 有 app 刷新令牌 + web 失效 → 只剩 OAuth 可试");
            Eq(AppKeyRecoveryAction.Convert, Decide(false, AppKeyProvenance.CookieConversion, false, true),
                "cookie 来历 + 没有 app 刷新令牌 + web 可用 → 兑换（修复前在这里退出）");
            Eq(AppKeyRecoveryAction.Logout, Decide(false, AppKeyProvenance.CookieConversion, false, false),
                "cookie 来历 + 没有 app 刷新令牌 + web 失效 → 没有可试的路，退出");

            Eq(AppKeyRecoveryAction.RenewOAuth, Decide(false, AppKeyProvenance.AppResponse, true, true),
                "app 响应来历 + 有 app 刷新令牌 + web 可用 → 优先 OAuth");
            Eq(AppKeyRecoveryAction.RenewOAuth, Decide(false, AppKeyProvenance.AppResponse, true, false),
                "app 响应来历 + 有 app 刷新令牌 + web 失效 → OAuth");
            Eq(AppKeyRecoveryAction.Convert, Decide(false, AppKeyProvenance.AppResponse, false, true),
                "app 响应来历 + 没有 app 刷新令牌 + web 可用 → 退回兑换");
            Eq(AppKeyRecoveryAction.Logout, Decide(false, AppKeyProvenance.AppResponse, false, false),
                "app 响应来历 + 没有 app 刷新令牌 + web 失效 → 退出");

            Eq(AppKeyRecoveryAction.RenewOAuth, Decide(false, AppKeyProvenance.UnknownLegacy, true, true),
                "旧会话来历不明 + 有 app 刷新令牌 + web 可用 → OAuth");
            Eq(AppKeyRecoveryAction.RenewOAuth, Decide(false, AppKeyProvenance.UnknownLegacy, true, false),
                "旧会话来历不明 + 有 app 刷新令牌 + web 失效 → OAuth");
            Eq(AppKeyRecoveryAction.Convert, Decide(false, AppKeyProvenance.UnknownLegacy, false, true),
                "旧会话来历不明 + 没有 app 刷新令牌 + web 可用 → 兑换");
            Eq(AppKeyRecoveryAction.Logout, Decide(false, AppKeyProvenance.UnknownLegacy, false, false),
                "旧会话来历不明 + 两条路都不适用 → 退出");
        }

        // ── OAuth 续期结论 × web 会话 ────────────────────────────────────────────────
        private static void OAuthTable()
        {
            foreach (var web in new[] { true, false })
            {
                Eq(AppKeyRecoveryAction.Continue, AfterOAuth(RenewalOutcome.Committed, web),
                    "OAuth 整组落盘 → 继续（web=" + web + "）");
                Eq(AppKeyRecoveryAction.Continue, AfterOAuth(RenewalOutcome.PartlyCommitted, web),
                    "OAuth 只落了令牌 → 继续（web=" + web + "）");
                Eq(AppKeyRecoveryAction.Retry, AfterOAuth(RenewalOutcome.TransportFailed, web),
                    "OAuth 传输失败 → 重试，凭证保持（web=" + web + "）");
                Eq(AppKeyRecoveryAction.Retry, AfterOAuth(RenewalOutcome.Uncommitted, web),
                    "OAuth 未落盘 → 重试（web=" + web + "）");
            }

            Eq(AppKeyRecoveryAction.Convert, AfterOAuth(RenewalOutcome.NotAttempted, true),
                "没有 app 刷新令牌 + web 可用 → 兑换，不是退出（D1 的核心缺陷）");
            Eq(AppKeyRecoveryAction.Logout, AfterOAuth(RenewalOutcome.NotAttempted, false),
                "没有 app 刷新令牌 + web 也不可用 → 退出");
            Eq(AppKeyRecoveryAction.Convert, AfterOAuth(RenewalOutcome.Rejected, true),
                "OAuth 被拒 + web 可用 → 先兑换再谈退出");
            Eq(AppKeyRecoveryAction.Logout, AfterOAuth(RenewalOutcome.Rejected, false),
                "OAuth 被拒 + web 不可用 → 退出");
        }

        // ── 兑换结论 ────────────────────────────────────────────────────────────────
        private static void ConversionTable()
        {
            Eq(AppKeyRecoveryAction.Continue, AfterConversion(AppKeyConversionOutcome.Converted),
                "兑换成功 → 继续");
            Eq(AppKeyRecoveryAction.Retry, AfterConversion(AppKeyConversionOutcome.UserMismatch),
                "回调用户对不上 → 重试（没发布过 key，也没证据说会话已死）");
            Eq(AppKeyRecoveryAction.Logout, AfterConversion(AppKeyConversionOutcome.Rejected),
                "服务端明确拒绝 → 这条路到头，退出");
            Eq(AppKeyRecoveryAction.Retry, AfterConversion(AppKeyConversionOutcome.TransportFailed),
                "兑换传输失败 → 重试");
            Eq(AppKeyRecoveryAction.Retry, AfterConversion(AppKeyConversionOutcome.Uncommitted),
                "兑换未落盘 → 重试");
            Eq(AppKeyRecoveryAction.Retry, AfterConversion(AppKeyConversionOutcome.NotAttempted),
                "兑换前置条件不成立 → 重试，不清会话");
        }

        // ── 修复前规则的转录：证明上面的断言在修复前确实是红的 ────────────────────────
        private static void LegacyRuleReproduction()
        {
            // 修复前 HomePage.CheckLoginStatus 只有「SessionDead → 退出」，NotAttempted 也算会话到此为止
            Eq(AppKeyRecoveryAction.Logout, LegacyAfterOAuth(RenewalOutcome.NotAttempted, true),
                "转录：修复前没有 app 刷新令牌时直接退出（web 会话可用也一样）");
            Eq(AppKeyRecoveryAction.Logout, LegacyAfterOAuth(RenewalOutcome.Rejected, true),
                "转录：修复前 OAuth 被拒就退出，不试兑换");
            Eq(AppKeyRecoveryAction.Continue, LegacyDecide(true, false),
                "转录：修复前启动不认 cookie 兑换这条路（没有令牌就什么都不做）");

            // 新决策表与转录的差异就是 D1 要修的部分
            Check(Decide(false, AppKeyProvenance.CookieConversion, false, true) !=
                  LegacyDecide(false, false),
                "cookie 来历 + web 可用：新决策选择兑换，修复前什么都不做");
            Check(AfterOAuth(RenewalOutcome.NotAttempted, true) != LegacyAfterOAuth(RenewalOutcome.NotAttempted, true),
                "NotAttempted + web 可用：新决策不退出，修复前退出");
        }

        private static void ExpiryGate()
        {
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);
            Check(AccessKeyRenewal.ShouldRenew(false, "rt", now.AddHours(23), now, false),
                "真实到期时间一天内就该续期");
            Check(!AccessKeyRenewal.ShouldRenew(false, "rt", now.AddHours(23), now, true),
                "估算出来的到期时间不是服务端承诺，不能拿它当续期理由");
            Check(AccessKeyRenewal.ShouldRenew(true, "rt", now.AddDays(30), now, true),
                "会话确实失效时，估算与否都要续");
            Check(!AccessKeyRenewal.ShouldRenew(true, "", now.AddDays(-3), now, true),
                "没有刷新令牌就不发请求");
            Check(AccessKeyRenewal.ShouldRenew(false, "rt", now.AddHours(-1), now),
                "不传估算标记时保持修复前的行为（真实到期即续）");
            Check(!AccessKeyRenewal.ShouldRenew(false, "rt", now.AddDays(9), now),
                "离到期还早就不续");
        }

        // ── 兑换编排（委托注入，不联网）──────────────────────────────────────────────
        private static async Task BridgeOrchestration()
        {
            Action<string, Exception> quiet = (message, exception) => log.Add(message);

            // 单次尝试：可重试的失败不自己重发
            var calls = 0;
            var published = 0;
            var retried = await CookieAppKeyBridge.ConvertAsync(100,
                () => Task.FromResult(Session(100)),
                session => { calls++; return Result(AppKeyConversionOutcome.TransportFailed); },
                (session, conversion) => published++,
                quiet);
            Eq(AppKeyConversionOutcome.TransportFailed, retried.Outcome, "exchange 的传输失败原样带回");
            Eq(1, calls, "一次兑换只发一次 exchange（不自动重试）");
            Eq(0, published, "失败的兑换一次都不发布");
            Eq("", retried.AccessKey, "非 Converted 的结论不带 AccessKey");

            // 回调用户与 cookie 会话用户对不上：拒绝发布
            calls = 0;
            published = 0;
            var mismatch = await CookieAppKeyBridge.ConvertAsync(100,
                () => Task.FromResult(Session(100)),
                session => { calls++; return Converted(200); },
                (session, conversion) => published++,
                quiet);
            Eq(AppKeyConversionOutcome.UserMismatch, mismatch.Outcome, "回调 uid 与 cookie 会话 uid 不一致时拒绝发布");
            Eq("", mismatch.AccessKey, "拒绝发布时 AccessKey 为空");
            Eq(0, published, "拒绝发布时持久化回调一次都不跑");
            Eq(1, calls, "身份校验在拿到响应之后、发布之前");
            Eq(200L, mismatch.UserId, "拒绝时带回对不上的 uid，日志能说清是谁");

            // 本机账号与停滞会话就对不上：连请求都不该发
            calls = 0;
            var expectedMismatch = await CookieAppKeyBridge.ConvertAsync(300,
                () => Task.FromResult(Session(100)),
                session => { calls++; return Converted(100); },
                (session, conversion) => { },
                quiet);
            Eq(AppKeyConversionOutcome.UserMismatch, expectedMismatch.Outcome, "停滞会话属于别的账号时也不发布");
            Eq(0, calls, "账号对不上时请求一个都不发");

            // 成功：发布一次，uid 取回调值
            published = 0;
            AppKeyConversion publishedResult = null;
            var converted = await CookieAppKeyBridge.ConvertAsync(100,
                () => Task.FromResult(Session(100)),
                session => Converted(100, "ak-1"),
                (session, conversion) => { published++; publishedResult = conversion; },
                quiet);
            Eq(AppKeyConversionOutcome.Converted, converted.Outcome, "身份一致时兑换成功");
            Eq("ak-1", converted.AccessKey, "成功时带回 access_key");
            Eq(1, published, "成功只发布一次");
            Eq("ak-1", publishedResult?.AccessKey, "发布的正是校验过的那把 key");
            Eq(100L, publishedResult?.UserId, "发布时带上确认过的 uid");

            // exchange 抛异常 → 可重试，不发布
            published = 0;
            var thrown = await CookieAppKeyBridge.ConvertAsync(100,
                () => Task.FromResult(Session(100)),
                session => throw new InvalidOperationException("network"),
                (session, conversion) => published++,
                quiet);
            Eq(AppKeyConversionOutcome.TransportFailed, thrown.Outcome, "exchange 抛异常算传输失败");
            Eq(0, published, "异常时一次都不发布");

            // 停滞会话读不出来 → 可重试
            var stageFailed = await CookieAppKeyBridge.ConvertAsync(100,
                () => throw new InvalidOperationException("jar"),
                session => Converted(100),
                (session, conversion) => { },
                quiet);
            Eq(AppKeyConversionOutcome.TransportFailed, stageFailed.Outcome, "本地会话读不出来算传输失败");

            // 本地 cookie 不全 → 没发请求，结论是没试
            calls = 0;
            var missing = await CookieAppKeyBridge.ConvertAsync(100,
                () => Task.FromResult(Session(100, "bili_jct")),
                session => { calls++; return Converted(100); },
                (session, conversion) => { },
                quiet);
            Eq(AppKeyConversionOutcome.NotAttempted, missing.Outcome, "本地 cookie 不全时不算尝试过");
            Eq(0, calls, "cookie 不全时一个请求都不发");

            // 落盘失败 → 未落盘（可重试），AccessKey 不许外泄成「成功」
            var uncommitted = await CookieAppKeyBridge.ConvertAsync(100,
                () => Task.FromResult(Session(100)),
                session => Converted(100, "ak-2"),
                (session, conversion) => throw new InvalidOperationException("settings"),
                quiet);
            Eq(AppKeyConversionOutcome.Uncommitted, uncommitted.Outcome, "发布失败算未落盘");
            Eq("", uncommitted.AccessKey, "未落盘时结论不带 AccessKey");

            // 回调没给 uid：以 cookie 会话的 uid 为准
            var noMid = await CookieAppKeyBridge.ConvertAsync(100,
                () => Task.FromResult(Session(100)),
                session => Converted(0, "ak-3"),
                (session, conversion) => { },
                quiet);
            Eq(AppKeyConversionOutcome.Converted, noMid.Outcome, "回调缺 mid 时仍以 cookie 会话身份为准");
            Eq(100L, noMid.UserId, "结论 uid 落到 cookie 会话的 uid");

            // 恢复顺序：先按需刷新 web 会话，再兑换
            var sequence = new List<string>();
            var ordered = await CookieAppKeyBridge.ConvertWithWebSessionAsync(100,
                () => { sequence.Add("refresh"); return Task.CompletedTask; },
                () => false,
                (id, logger) => { sequence.Add("convert"); return Converted(100); },
                quiet);
            Eq("refresh,convert", string.Join(",", sequence), "恢复路径先刷新 web 会话再兑换");
            Eq(AppKeyConversionOutcome.Converted, ordered.Outcome, "顺序对了才拿得到新 key");

            // 刷新后 web 会话仍不可用：不兑换
            sequence.Clear();
            var dead = await CookieAppKeyBridge.ConvertWithWebSessionAsync(100,
                () => { sequence.Add("refresh"); return Task.CompletedTask; },
                () => true,
                (id, logger) => { sequence.Add("convert"); return Converted(100); },
                quiet);
            Eq("refresh", string.Join(",", sequence), "web 会话不可用时不发兑换请求");
            Eq(AppKeyConversionOutcome.NotAttempted, dead.Outcome, "web 会话不可用时结论是没试");

            // 刷新本身失败：可重试，不兑换
            sequence.Clear();
            var refreshFailed = await CookieAppKeyBridge.ConvertWithWebSessionAsync(100,
                () => { sequence.Add("refresh"); throw new InvalidOperationException("refresh"); },
                () => false,
                (id, logger) => { sequence.Add("convert"); return Converted(100); },
                quiet);
            Eq("refresh", string.Join(",", sequence), "刷新失败后不再兑换");
            Eq(AppKeyConversionOutcome.TransportFailed, refreshFailed.Outcome, "刷新失败算传输失败");
        }

        // ── 真实实现 + 假网络 + 假 jar ───────────────────────────────────────────────
        private static async Task RealBridge()
        {
            Action<string, Exception> quiet = (message, exception) => log.Add(message);
            var requests = new List<string>();

            Func<HttpRequestMessage, Task<HttpResponseMessage>> happy = request =>
            {
                requests.Add(request.RequestUri.AbsolutePath);
                switch (request.RequestUri.AbsolutePath)
                {
                    case "/login/app/third":
                        return Task.FromResult(Ok(
                            "{\"code\":0,\"data\":{\"confirm_uri\":\"https://passport.bilibili.com/login/confirm?ticket=t\"}}"));
                    case "/login/confirm":
                        var redirect = new HttpResponseMessage(System.Net.HttpStatusCode.Redirect);
                        redirect.Headers.Location =
                            new Uri("http://link.acg.tv/forum.php?api=1&access_key=ak%2B1&mid=100&signature=s");
                        return Task.FromResult(redirect);
                    default:
                        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
                }
            };

            // 成功：只动 ACCESS_KEY / 估算到期时间 / 来历，web 刷新凭证和 uid 原样保留
            SeedJar(100);
            SettingHelper.Reset();
            SettingHelper.SetValue(SettingHelper.Account.REFRESH_KEY, "");
            SettingHelper.SetValue("CookieRefreshToken", "web-token");
            HttpClient.Handler = happy;
            requests.Clear();
            var converted = await CookieAppKeyBridge.ConvertAsync(100, quiet);
            Eq(AppKeyConversionOutcome.Converted, converted.Outcome, "本地 cookie 会话能兑换出 access_key");
            Eq("ak+1", converted.AccessKey, "回调里的 access_key 原样带回（含转义）");
            Eq(100L, converted.UserId, "回调里的 mid 成为结论 uid");
            Eq("ak+1", SettingHelper.Account.AccessKey, "兑换成功才写 ACCESS_KEY");
            Eq("cookie-conversion", SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY_SOURCE, ""),
                "来历记成 cookie 兑换");
            Eq(true, SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_ESTIMATED, false),
                "240 小时到期时间被标成估算");
            Eq("", SettingHelper.GetValue(SettingHelper.Account.REFRESH_KEY, ""),
                "cookie 兑换不写 app 刷新令牌");
            Eq("web-token", SettingHelper.GetValue("CookieRefreshToken", ""),
                "web 刷新凭证原样保留");
            Eq(0L, SettingHelper.GetValue(SettingHelper.Account.USER_ID, 0L), "兑换不改写账号 uid");
            Eq(2, requests.Count, "一次兑换只发 app/third 与确认两次请求");
            Check(!requests.Contains("/forum.php"), "回调地址只解析、不抓取");

            // 回调属于别人：不发布，旧 key 不动
            HttpClient.Handler = request =>
            {
                if (request.RequestUri.AbsolutePath == "/login/app/third")
                    return Task.FromResult(Ok(
                        "{\"code\":0,\"data\":{\"confirm_uri\":\"https://passport.bilibili.com/login/confirm?ticket=t\"}}"));
                var redirect = new HttpResponseMessage(System.Net.HttpStatusCode.Redirect);
                redirect.Headers.Location = new Uri("http://link.acg.tv/forum.php?access_key=other&mid=200");
                return Task.FromResult(redirect);
            };
            var foreign = await CookieAppKeyBridge.ConvertAsync(100, quiet);
            Eq(AppKeyConversionOutcome.UserMismatch, foreign.Outcome, "真实路径里回调用户不一致也被拒");
            Eq("ak+1", SettingHelper.Account.AccessKey, "拒绝发布时 ACCESS_KEY 保持原值");
            Eq("cookie-conversion", SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY_SOURCE, ""),
                "拒绝发布时来历也不动");

            // 服务端明确拒绝会话：Rejected（这条路到头）
            HttpClient.Handler = request => Task.FromResult(Ok("{\"code\":-101}"));
            var rejected = await CookieAppKeyBridge.ConvertAsync(100, quiet);
            Eq(AppKeyConversionOutcome.Rejected, rejected.Outcome, "-101 是明确拒绝，映射成 Rejected");

            // 其它业务码只是这一次没成：不许当成会话已死
            HttpClient.Handler = request => Task.FromResult(Ok("{\"code\":-1}"));
            var transient = await CookieAppKeyBridge.ConvertAsync(100, quiet);
            Eq(AppKeyConversionOutcome.TransportFailed, transient.Outcome, "非 -101/-111 的业务码只算可重试");

            // 本地 cookie 不全：一个请求都不发
            HttpBaseProtocolFilter.Jar.Reset();
            HttpBaseProtocolFilter.Jar.SetCookie(new HttpCookie("SESSDATA", "bilibili.com", "/") { Value = "sess" });
            requests.Clear();
            var incomplete = await CookieAppKeyBridge.ConvertAsync(100, quiet);
            Eq(AppKeyConversionOutcome.NotAttempted, incomplete.Outcome, "缺 bili_jct/DedeUserID 时不算尝试过");
            Eq(0, requests.Count, "cookie 不全时一个请求都不发");
        }

        // ── 源码契约：写入路径、抽取完整性、web 刷新链不碰 key ─────────────────────────
        private static void SourceChecks()
        {
            var root = FindRepoRoot();

            var home = Body(Read(root, "src/BiliLite.UWP/Pages/HomePage.xaml.cs"),
                "private async void CheckLoginStatus()", "private async void LoadUserStatus()");
            Eq(1, Regex.Matches(home, @"MessageCenter\.SendLogout\(\)").Count,
                "Home 只有决策表判定退出时才 SendLogout");
            Check(home.Contains("AppKeyRecovery.AfterOAuth("), "Home 把续期结论交给恢复决策表");
            Check(home.Contains("CookieAppKeyBridge.ConvertWithWebSessionAsync("),
                "Home 在 web 会话可用时兑换 cookie 来源的 key");
            Check(!home.Contains("if (renewal.SessionDead)"), "修复前的 SessionDead→退出 写法已经消失");
            Check(home.IndexOf("CheckLoginState", StringComparison.Ordinal) <
                  home.IndexOf("AppKeyRecovery.AfterOAuth(", StringComparison.Ordinal),
                "兑换只发生在 key 校验失败之后");

            var preflight = Body(Read(root, "src/BiliLite.UWP/App.xaml.cs"),
                "private static async Task PreflightLoginAsync()",
                "private void RegisterExceptionHandlingSynchronizationContext()");
            Eq(1, Regex.Matches(preflight, @"AccessKeyRenewal\.ShouldRenew\(").Count, "启动只过一次 OAuth 闸门");
            Eq(1, Regex.Matches(preflight, @"\.RefreshToken\(\)").Count, "启动最多续期一次");
            Check(!preflight.Contains("SendLogout"), "启动不做退出决定");
            Check(!Regex.IsMatch(preflight, @"SetValue\(SettingHelper\.Account\.ACCESS_KEY,"),
                "启动不直接改写 ACCESS_KEY");
            Check(preflight.Contains("AppKeyRecovery.Decide(") &&
                  preflight.Contains("CookieAppKeyBridge.ConvertWithWebSessionAsync("),
                "启动对 cookie 来源的 key 用同一张决策表并兑换一次");
            Check(!preflight.Contains("while (") && !preflight.Contains("for ("), "启动不循环重试");

            var accountText = Read(root, "src/BiliLite.UWP/Modules/User/Account.cs");
            var persist = Body(accountText, "private static CredentialCommit PersistCredentials(",
                "private static void LogCommitFailure(");
            Check(persist.Contains("RecordProvenance(AppKeyProvenance.AppResponse)"),
                "app 令牌响应落盘时记 app-response 来历");
            Check(persist.Contains("ACCESS_KEY_EXPIRE_ESTIMATED, false"),
                "响应给了真实有效期就不再标估算");
            Check(!persist.Contains("CookieConversion"), "app 响应落盘不会把自己标成 cookie 兑换");

            var cookieLogin = Body(accountText, "private static void ApplyCookieLogin(",
                "private static void DeleteJarRefreshToken()");
            Check(cookieLogin.Contains("RecordProvenance(AppKeyProvenance.CookieConversion)"),
                "cookie 登录落盘时记 cookie-conversion 来历");
            Check(cookieLogin.Contains("ACCESS_KEY_EXPIRE_ESTIMATED, true"),
                "cookie 兑换的 240 小时标成估算");
            Check(cookieLogin.Contains("SetValue(SettingHelper.Account.REFRESH_KEY, \"\")"),
                "cookie 登录不拿 web 令牌冒充 app 刷新令牌");

            Check(Body(accountText, "private static CookieLoginSnapshot CaptureCookieLoginSnapshot(",
                    "private static void ApplyCookieLogin(").Contains("ACCESS_KEY_SOURCE"),
                "cookie 登录快照带上来历，回滚时一起退回");
            Check(Body(accountText, "private static void RestoreCookieLoginSnapshot(",
                    "private static void RestoreSetting<T>(").Contains("ACCESS_KEY_SOURCE"),
                "回滚把来历和估算标记一起退回");
            Check(Body(accountText, "public async Task<LoginCallbackModel> LoginV2(",
                    "public async Task<LoginCallbackModel> LoginV3(")
                .Contains("RecordProvenance(AppKeyProvenance.AppResponse)"),
                "LoginV2 直接落盘也要记来历");

            var transport = Read(root, "src/BiliLite.UWP/Services/IsolatedPasswordLoginTransport.cs");
            Check(transport.Contains("CookieAppKeyBridge.ExchangeStagedAsync("),
                "密码登录复用同一个兑换实现");
            Check(!transport.Contains("FollowConfirmAsync") && !transport.Contains("GetCookieToAccessKey") &&
                  !transport.Contains("confirm_uri"),
                "app/third 与确认回调已搬走，密码登录里不再有第二份实现");

            var bridgeText = Read(root, "src/BiliLite.UWP/Services/CookieAppKeyBridge.cs");
            Check(!bridgeText.Contains("PwdLoginExchangeCookie") &&
                  !bridgeText.Contains("IsolatedPasswordLoginTransport"),
                "兑换不另造端点，也不借密码登录的验证码交换");
            Eq(2, Regex.Matches(bridgeText, @"GetCookieToAccessKey\(").Count,
                "兑换只有 app/third 与确认两个地址构造点");
            var publish = Body(bridgeText, "private static void PersistConvertedKey(",
                "private static bool IsSessionRefusal(");
            Check(publish.Contains("ACCESS_KEY_EXPIRE_ESTIMATED, true") &&
                  publish.Contains("AppKeyProvenance.CookieConversion"),
                "兑换落盘写入估算标记与 cookie 来历");
            Check(!publish.Contains("REFRESH_KEY"),
                "兑换落盘不写 app 刷新令牌（保持空）");

            var cookieService = Read(root, "src/BiliLite.UWP/Services/CookieService.cs");
            Check(!cookieService.Contains("CookieAppKeyBridge"),
                "web cookie 刷新链不碰 app key：刷新网页会话不会顺带重新兑换");

            // 来历读侧：没有 key 就没有来历
            SettingHelper.Reset();
            SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY_SOURCE,
                AppKeyProvenancePolicy.Format(AppKeyProvenance.CookieConversion));
            Eq(AppKeyProvenance.UnknownLegacy, CookieAppKeyBridge.ReadProvenance(),
                "没有 ACCESS_KEY 时来历一律当未知");
            SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY, "ak");
            Eq(AppKeyProvenance.CookieConversion, CookieAppKeyBridge.ReadProvenance(),
                "有 key 时读回记下的来历");
            CookieAppKeyBridge.RecordProvenance(AppKeyProvenance.AppResponse);
            Eq(AppKeyProvenance.AppResponse, CookieAppKeyBridge.ReadProvenance(),
                "RecordProvenance 写的值读得回来");
            Eq(AppKeyProvenance.UnknownLegacy, AppKeyProvenancePolicy.Parse(""),
                "空缺的来历是 unknown-legacy");
            Eq(AppKeyProvenance.AppResponse,
                AppKeyProvenancePolicy.Parse(AppKeyProvenancePolicy.Format(AppKeyProvenance.AppResponse)),
                "来历格式往返一致");
        }

        // ── 被测策略的接入口（legacy 模式下换成修复前规则的转录）──────────────────────
        private static AppKeyRecoveryAction Decide(bool appKeyUsable, AppKeyProvenance provenance,
            bool hasOAuthRefreshToken, bool webSessionUsable) =>
            legacy
                ? LegacyDecide(appKeyUsable, hasOAuthRefreshToken)
                : AppKeyRecovery.Decide(appKeyUsable, provenance, hasOAuthRefreshToken, webSessionUsable);

        private static AppKeyRecoveryAction AfterOAuth(RenewalOutcome outcome, bool webSessionUsable) =>
            legacy ? LegacyAfterOAuth(outcome, webSessionUsable) : AppKeyRecovery.AfterOAuth(outcome, webSessionUsable);

        private static AppKeyRecoveryAction AfterConversion(AppKeyConversionOutcome outcome) =>
            legacy ? AppKeyRecoveryAction.Retry : AppKeyRecovery.AfterConversion(outcome);

        /// <summary>修复前 App.xaml.cs 的启动规则：key 可用就不动；不可用时只有 app 刷新令牌这条路。</summary>
        private static AppKeyRecoveryAction LegacyDecide(bool appKeyUsable, bool hasOAuthRefreshToken) =>
            appKeyUsable
                ? AppKeyRecoveryAction.Continue
                : (hasOAuthRefreshToken ? AppKeyRecoveryAction.RenewOAuth : AppKeyRecoveryAction.Continue);

        /// <summary>修复前 HomePage.CheckLoginStatus 的规则：SessionDead（含根本没有令牌）就退出。</summary>
        private static AppKeyRecoveryAction LegacyAfterOAuth(RenewalOutcome outcome, bool webSessionUsable)
        {
            var saved = outcome == RenewalOutcome.Committed || outcome == RenewalOutcome.PartlyCommitted;
            var result = new RenewalResult(outcome, saved, false, false);
            if (result.SessionDead) return AppKeyRecoveryAction.Logout;
            if (result.Retryable) return AppKeyRecoveryAction.Retry;
            return AppKeyRecoveryAction.Continue;
        }

        // ── 杂项 ────────────────────────────────────────────────────────────────────
        private static IEnumerable<AppKeyProvenance> Provenances() => new[]
        {
            AppKeyProvenance.UnknownLegacy,
            AppKeyProvenance.AppResponse,
            AppKeyProvenance.CookieConversion,
        };

        private static AppKeyCookieSession Session(long userId, params string[] missing) => new AppKeyCookieSession
        {
            Cookies = new System.Net.CookieContainer(),
            UserId = userId,
            HasRequiredCookies = missing.Length == 0,
            Missing = string.Join(",", missing),
        };

        private static Task<AppKeyConversion> Converted(long userId, string accessKey = "ak") =>
            Task.FromResult(new AppKeyConversion(AppKeyConversionOutcome.Converted, accessKey, userId, "fixture"));

        private static Task<AppKeyConversion> Result(AppKeyConversionOutcome outcome) =>
            Task.FromResult(new AppKeyConversion(outcome, "never-published", 100, "fixture"));

        private static void SeedJar(long userId)
        {
            HttpBaseProtocolFilter.Jar.Reset();
            HttpBaseProtocolFilter.Jar.SetCookie(new HttpCookie("SESSDATA", "bilibili.com", "/") { Value = "sess" });
            HttpBaseProtocolFilter.Jar.SetCookie(new HttpCookie("bili_jct", "bilibili.com", "/") { Value = "csrf" });
            HttpBaseProtocolFilter.Jar.SetCookie(new HttpCookie("DedeUserID", "bilibili.com", "/")
            {
                Value = userId.ToString(),
            });
        }

        private static HttpResponseMessage Ok(string body) =>
            new HttpResponseMessage(System.Net.HttpStatusCode.OK, body);

        private static string FindRepoRoot()
        {
            var directory = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null &&
                   !System.IO.Directory.Exists(System.IO.Path.Combine(directory.FullName, "src", "BiliLite.UWP")))
                directory = directory.Parent;
            if (directory == null) throw new InvalidOperationException("repo root not found");
            return directory.FullName;
        }

        private static string Read(string root, string relative) =>
            System.IO.File.ReadAllText(System.IO.Path.Combine(root, relative.Replace('/', System.IO.Path.DirectorySeparatorChar)));

        private static string Body(string text, string start, string stop)
        {
            var from = text.IndexOf(start, StringComparison.Ordinal);
            if (from < 0) return "";
            var to = text.IndexOf(stop, from, StringComparison.Ordinal);
            return to < 0 ? text.Substring(from) : text.Substring(from, to - from);
        }

        private static void Check(bool condition, string what)
        {
            assertions++;
            if (!condition) failures.Add(what);
        }

        private static void Eq(object expected, object actual, string what) =>
            Check(Equals(expected, actual), what + " [expected=" + expected + ", actual=" + actual + "]");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Models;
using BiliLite.Modules.User;
using BiliLite.Services;
using Windows.Web.Http;

internal static class AUTH01BLoginVMProbe
{
    private static int assertions;
    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception("AUTH-01b LoginVM failed: " + message);
    }

    private static LoginResultModel ChallengeResult() => new LoginResultModel
    {
        code = 0,
        data = new LoginResultModel { status = 1, url = "https://passport.bilibili.com/verify?tmp_token=tmp%2Btoken&request_id=req%2Fid" }
    };

    private static LoginResultModel SuccessResult() => new LoginResultModel
    {
        code = 0,
        data = new LoginResultModel { status = 0, token_info = new LoginTokenInfo { access_token = "legacy-access", refresh_token = "legacy-refresh", mid = 123 } }
    };

    private static void ConfigureChallengeRequests()
    {
        AuthRequestTestHooks.RequestHandler = api => Task.FromResult(new HttpResults { code = 200, status = true, Data = ChallengeResult() });
        Windows.Web.Http.HttpClient.Handler = (filter, request, content) =>
        {
            if (request.RequestUri.AbsolutePath == "/x/safecenter/captcha/pre")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK,
                    "{\"code\":0,\"data\":{\"gee_gt\":\"gee\",\"gee_challenge\":\"challenge\",\"recaptcha_token\":\"recap\"}}"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        };
    }

    private static async Task TestCancelledPasswordResponseAndRetry()
    {
        var encryption = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var preRequestVm = new LoginVM { LoginType = 0, UserName = "user", Password = "password" };
        preRequestVm.account.Encryption = encryption.Task;
        var prematureRequests = 0;
        AuthRequestTestHooks.RequestHandler = _ =>
        {
            prematureRequests++;
            return Task.FromResult(new HttpResults { code = 200, status = true, Data = ChallengeResult() });
        };
        var preRequest = preRequestVm.DoPasswordLogin();
        preRequestVm.CancelPasswordVerification();
        encryption.SetResult("encrypted");
        await preRequest.WaitAsync(TimeSpan.FromSeconds(3));
        Check(prematureRequests == 0, "cancel before encryption completes prevents the password API request");
        preRequestVm.EndLoginDialog();

        var vm = new LoginVM { LoginType = 0, UserName = "user", Password = "password" };
        var requestSeen = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var responseGate = new TaskCompletionSource<HttpResults>(TaskCreationOptions.RunContinuationsAsynchronously);
        var loginRequests = 0;
        AuthRequestTestHooks.RequestHandler = api =>
        {
            loginRequests++;
            requestSeen.TrySetResult(true);
            return responseGate.Task;
        };
        var cancelledPassword = vm.DoPasswordLogin();
        await requestSeen.Task.WaitAsync(TimeSpan.FromSeconds(3));
        var passwordDataReads = HttpResults.GetDataCalls;
        vm.CancelPasswordVerification();
        responseGate.SetResult(new HttpResults { code = 200, status = true, Data = ChallengeResult() });
        await cancelledPassword.WaitAsync(TimeSpan.FromSeconds(3));
        Check(loginRequests == 1 && HttpResults.GetDataCalls == passwordDataReads,
            "cancel before password response prevents parsing or starting secondary verification");
        Check(!vm.PasswordVerificationPending && !vm.PasswordVerificationCaptchaActive && vm.LoginType == 0,
            "password cancellation clears the secondary-verification UI state");
        vm.EndLoginDialog();

        var retryVm = new LoginVM { LoginType = 0, UserName = "user", Password = "password" };
        var retryCount = 0;
        AuthRequestTestHooks.RequestHandler = _ =>
        {
            retryCount++;
            return Task.FromResult(retryCount == 1
                ? new HttpResults { code = 503, status = false, message = "offline" }
                : new HttpResults { code = 200, status = true, Data = SuccessResult() });
        };
        var closes = 0;
        var closed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        retryVm.CloseDialog += (_, __) => { closes++; closed.TrySetResult(true); };
        await retryVm.DoPasswordLogin().WaitAsync(TimeSpan.FromSeconds(3));
        Check(retryCount == 1 && closes == 0, "failed password request leaves the dialog open for retry");
        await retryVm.DoPasswordLogin().WaitAsync(TimeSpan.FromSeconds(3));
        await closed.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Check(retryCount == 2 && retryVm.account.SaveLoginCalls == 1 && closes == 1,
            "password retry can succeed and closes its dialog once");
        retryVm.EndLoginDialog();
        Check(closes == 1, "ending a successfully closed password dialog does not emit another close");
    }

    private static async Task<LoginVM> StartVerificationFlow()
    {
        ConfigureChallengeRequests();
        var vm = new LoginVM { LoginType = 0, UserName = "user", Password = "password" };
        var opened = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.OpenWebView += (_, uri) => opened.TrySetResult(uri);
        var passwordTask = vm.DoPasswordLogin();
        var uri = await opened.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await passwordTask.WaitAsync(TimeSpan.FromSeconds(3));
        Check(uri.Scheme == "ms-appx-web" && uri.Query.Contains("operation=" + vm.PasswordVerificationOperation),
            "password security challenge carries the current operation token");
        Check(vm.PasswordVerificationCaptchaActive && vm.PasswordVerificationOperation > 0,
            "secondary verification becomes active only after captcha setup succeeds");
        Check(!vm.PrimaryButtonEnable, "primary action stays disabled while the active captcha is displayed");
        return vm;
    }

    private static async Task TestTypeSwitchDuringMaskedPhoneRequest()
    {
        var vm = await StartVerificationFlow();
        var phoneSeen = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var phoneResponse = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var smsRequests = 0;
        Windows.Web.Http.HttpClient.Handler = (filter, request, content) =>
        {
            if (request.RequestUri.AbsolutePath == "/x/safecenter/user/info")
            {
                phoneSeen.TrySetResult(true);
                return phoneResponse.Task;
            }
            if (request.RequestUri.AbsolutePath == "/x/safecenter/common/sms/send") smsRequests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        };
        var operation = vm.PasswordVerificationOperation;
        var phoneOperation = vm.CompletePasswordVerificationCaptcha("challenge", "validate", "seccode", "recaptcha", operation);
        await phoneSeen.Task.WaitAsync(TimeSpan.FromSeconds(3));
        vm.ChangeLoginType(1);
        phoneResponse.SetResult(new HttpResponseMessage(HttpStatusCode.OK,
            "{\"code\":0,\"data\":{\"account_info\":{\"hide_tel\":\"138****0000\"}}}"));
        await phoneOperation.WaitAsync(TimeSpan.FromSeconds(3));
        Check(vm.LoginType == 1 && vm.HideTel == "" && !vm.PasswordVerificationPending && smsRequests == 0,
            "switching to SMS login during masked-phone lookup blocks stale SMS send and UI update");
        vm.EndLoginDialog();
    }

    private static async Task TestTypeSwitchDuringSmsResponse()
    {
        var vm = await StartVerificationFlow();
        var smsSeen = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var smsResponse = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var smsCalls = 0;
        Windows.Web.Http.HttpClient.Handler = (filter, request, content) =>
        {
            var path = request.RequestUri.AbsolutePath;
            if (path == "/x/safecenter/user/info")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK,
                    "{\"code\":0,\"data\":{\"account_info\":{\"hide_tel\":\"138****0000\"}}}"));
            if (path == "/x/safecenter/common/sms/send")
            {
                smsCalls++;
                smsSeen.TrySetResult(true);
                return smsResponse.Task;
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        };
        var smsOperation = vm.CompletePasswordVerificationCaptcha("challenge", "validate", "seccode", "recaptcha", vm.PasswordVerificationOperation);
        await smsSeen.Task.WaitAsync(TimeSpan.FromSeconds(3));
        vm.ChangeLoginType(1);
        smsResponse.SetResult(new HttpResponseMessage(HttpStatusCode.OK, "{\"code\":0,\"data\":{\"captcha_key\":\"late-key\"}}"));
        await smsOperation.WaitAsync(TimeSpan.FromSeconds(3));
        Check(smsCalls == 1 && vm.LoginType == 1 && !vm.PasswordVerificationPending && vm.HideTel == "" && vm.EnableSendSMS,
            "switching login type while the SMS request is pending rejects its late verification state");
        vm.EndLoginDialog();
    }

    private static async Task TestLogoutRejectsLateSmsResponse()
    {
        var vm = await StartVerificationFlow();
        var smsSeen = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var smsResponse = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var smsCalls = 0;
        Windows.Web.Http.HttpClient.Handler = (filter, request, content) =>
        {
            var path = request.RequestUri.AbsolutePath;
            if (path == "/x/safecenter/user/info")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK,
                    "{\"code\":0,\"data\":{\"account_info\":{\"hide_tel\":\"138****0000\"}}}"));
            if (path == "/x/safecenter/common/sms/send")
            {
                smsCalls++;
                smsSeen.TrySetResult(true);
                return smsResponse.Task;
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        };
        var operation = vm.CompletePasswordVerificationCaptcha("challenge", "validate", "seccode", "recaptcha", vm.PasswordVerificationOperation);
        await smsSeen.Task.WaitAsync(TimeSpan.FromSeconds(3));
        MessageCenter.RaiseLogout();
        smsResponse.SetResult(new HttpResponseMessage(HttpStatusCode.OK, "{\"code\":0,\"data\":{\"captcha_key\":\"late-key\"}}"));
        await operation.WaitAsync(TimeSpan.FromSeconds(3));
        Check(smsCalls == 1 && !vm.PasswordVerificationPending && vm.HideTel == "" && vm.PasswordVerificationOperation == 0,
            "logout invalidates the active verification operation before its SMS response arrives");
        Check(vm.account.CookieCommits == 0, "late SMS response after logout cannot commit a cookie session");
        vm.EndLoginDialog();
    }

    private static async Task TestLogoutRejectsLateSmsConfirmation()
    {
        var vm = await StartVerificationFlow();
        Windows.Web.Http.HttpClient.Handler = (filter, request, content) =>
        {
            var path = request.RequestUri.AbsolutePath;
            if (path == "/x/safecenter/user/info")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK,
                    "{\"code\":0,\"data\":{\"account_info\":{\"hide_tel\":\"138****0000\"}}}"));
            if (path == "/x/safecenter/common/sms/send")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK, "{\"code\":0,\"data\":{\"captcha_key\":\"captcha-key\"}}"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        };
        await vm.CompletePasswordVerificationCaptcha("challenge", "validate", "seccode", "recaptcha", vm.PasswordVerificationOperation);
        Check(vm.PasswordVerificationPending, "verification flow reaches the code-confirmation stage before logout test");
        vm.Code = "123456";

        var verifySeen = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var verifyResponse = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var exchangeRequests = 0;
        Windows.Web.Http.HttpClient.Handler = (filter, request, content) =>
        {
            if (request.RequestUri.AbsolutePath == "/x/safecenter/login/tel/verify")
            {
                verifySeen.TrySetResult(true);
                return verifyResponse.Task;
            }
            if (request.RequestUri.AbsolutePath == "/x/passport-login/web/exchange_cookie") exchangeRequests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        };
        var closes = 0;
        vm.CloseDialog += (_, __) => closes++;
        var confirmation = vm.InvokeCompletePasswordLoginCheckAsync();
        await verifySeen.Task.WaitAsync(TimeSpan.FromSeconds(3));
        MessageCenter.RaiseLogout();
        verifyResponse.SetResult(new HttpResponseMessage(HttpStatusCode.OK, "{\"code\":0,\"data\":{\"code\":\"late-exchange-code\"}}"));
        await confirmation.WaitAsync(TimeSpan.FromSeconds(3));
        Check(vm.account.CookieCommits == 0 && exchangeRequests == 0 && closes == 0,
            "logout before SMS confirmation response prevents exchange, session commit, and dialog close");
        vm.EndLoginDialog();
    }

    private static async Task TestSmsRetryAndCookieLoginSuccess()
    {
        var vm = await StartVerificationFlow();
        var smsCalls = 0;
        Windows.Web.Http.HttpClient.Handler = (filter, request, content) =>
        {
            var path = request.RequestUri.AbsolutePath;
            if (path == "/x/safecenter/user/info")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK,
                    "{\"code\":0,\"data\":{\"account_info\":{\"hide_tel\":\"138****0000\"}}}"));
            if (path == "/x/safecenter/common/sms/send")
            {
                smsCalls++;
                return Task.FromResult(smsCalls == 2
                    ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable, "offline")
                    : new HttpResponseMessage(HttpStatusCode.OK, "{\"code\":0,\"data\":{\"captcha_key\":\"captcha-key\"}}"));
            }
            if (path == "/x/safecenter/login/tel/verify")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK, "{\"code\":0,\"data\":{\"code\":\"exchange-code\"}}"));
            if (path == "/x/passport-login/web/exchange_cookie")
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK, "{\"code\":0,\"data\":{\"refresh_token\":\"cookie-refresh\"}}");
                response.Headers.Add("Set-Cookie", "DedeUserID=123; Domain=.bilibili.com; Path=/; Secure, SESSDATA=sess; Domain=.bilibili.com; Path=/; Secure, bili_jct=csrf; Domain=.bilibili.com; Path=/; Secure");
                return Task.FromResult(response);
            }
            if (path == "/login/app/third")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK,
                    "{\"code\":0,\"data\":{\"confirm_uri\":\"https://passport.bilibili.com/login/confirm?ticket=t\"}}"));
            if (path == "/login/confirm")
            {
                var response = new HttpResponseMessage(HttpStatusCode.Redirect);
                response.Headers.Location = new Uri("https://link.acg.tv/forum.php?access_key=app-access&mid=123&signature=s");
                return Task.FromResult(response);
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        };

        await vm.CompletePasswordVerificationCaptcha("challenge", "validate", "seccode", "recaptcha", vm.PasswordVerificationOperation).WaitAsync(TimeSpan.FromSeconds(3));
        Check(smsCalls == 1 && !vm.EnableSendSMS && vm.HideTel == "138****0000",
            "successful first SMS response publishes masked phone and starts the resend countdown");
        vm.EnableSendSMS = true;
        await vm.SendPasswordVerificationSMS().WaitAsync(TimeSpan.FromSeconds(3));
        Check(smsCalls == 2 && vm.PasswordVerificationPending, "failed resend preserves the verification state and permits retry");
        await vm.SendPasswordVerificationSMS().WaitAsync(TimeSpan.FromSeconds(3));
        Check(vm.PasswordVerificationPending, "successful resend keeps the secondary verification panel active");

        var closes = 0;
        var closed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.CloseDialog += (_, __) => { closes++; closed.TrySetResult(true); };
        vm.Code = "123456";
        await vm.InvokeCompletePasswordLoginCheckAsync().WaitAsync(TimeSpan.FromSeconds(3));
        await closed.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Check(vm.account.CookieCommits == 1 && vm.account.CommittedSession.AccessKey == "app-access" &&
            vm.account.CommittedSession.CookieRefreshToken == "cookie-refresh" && vm.account.ProfileFinishes == 1,
            "validated SMS confirmation runs isolated cookie exchange and commits one staged app session");
        Check(closes == 1 && !vm.PasswordVerificationPending, "successful cookie login closes once and clears the verification panel");
        await vm.InvokeCompletePasswordLoginCheckAsync().WaitAsync(TimeSpan.FromSeconds(3));
        Check(vm.account.CookieCommits == 1 && closes == 1, "duplicate completion cannot recommit or close the dialog again");
        vm.EndLoginDialog();
    }

    private static async Task TestFreshPasswordAttemptClearsFailedSecondaryFlow()
    {
        var vm = await StartVerificationFlow();
        Windows.Web.Http.HttpClient.Handler = (filter, request, content) =>
        {
            var path = request.RequestUri.AbsolutePath;
            if (path == "/x/safecenter/user/info")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK,
                    "{\"code\":0,\"data\":{\"account_info\":{\"hide_tel\":\"138****0000\"}}}"));
            if (path == "/x/safecenter/common/sms/send")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable, "first SMS failed"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        };
        await vm.CompletePasswordVerificationCaptcha("challenge", "validate", "seccode", "recaptcha", vm.PasswordVerificationOperation);
        Check(vm.IsPasswordVerificationFlow && !vm.PasswordVerificationPending && !vm.PasswordVerificationCaptchaActive &&
            vm.PasswordVerificationCaptchaKeyForProbe == null,
            "failed first SMS leaves an incomplete secondary flow that must be abandoned by a fresh attempt");

        var encryption = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.account.Encryption = encryption.Task;
        AuthRequestTestHooks.RequestHandler = _ => Task.FromResult(new HttpResults { code = 200, status = true, Data = SuccessResult() });
        var closes = 0;
        var closed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.CloseDialog += (_, __) => { closes++; closed.TrySetResult(true); };
        var fresh = vm.DoPasswordLogin();
        Check(!vm.IsPasswordVerificationFlow && vm.PasswordVerificationCaptchaKeyForProbe == null &&
            !vm.PasswordVerificationPending && !vm.PasswordVerificationCaptchaActive,
            "fresh password attempt clears stale temporary token, SMS key, and verification UI state before encryption");
        encryption.SetResult("new-encrypted-password");
        await fresh.WaitAsync(TimeSpan.FromSeconds(3));
        await closed.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Check(vm.account.SaveLoginCalls == 1 && vm.account.CookieCommits == 0 && closes == 1,
            "fresh password attempt follows ordinary login success without reusing secondary credentials");
        vm.EndLoginDialog();
    }

    private static async Task Main()
    {
        await TestCancelledPasswordResponseAndRetry();
        await TestTypeSwitchDuringMaskedPhoneRequest();
        await TestTypeSwitchDuringSmsResponse();
        await TestLogoutRejectsLateSmsResponse();
        await TestLogoutRejectsLateSmsConfirmation();
        await TestSmsRetryAndCookieLoginSuccess();
        await TestFreshPasswordAttemptClearsFailedSecondaryFlow();
        Console.WriteLine("AUTH-01b actual-source LoginVM lifecycle probes passed: " + assertions + " (Account persistence, AccountApi builders, XAML events, and Windows HTTP are boundary stubs.)");
    }
}

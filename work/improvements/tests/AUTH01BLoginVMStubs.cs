using System;
using System.Threading.Tasks;
using BiliLite.Models;
using BiliLite.Services;

namespace BiliLite.Modules
{
    public sealed class Account
    {
        public Task<string> Encryption { get; set; } = Task.FromResult("encrypted");
        public int EncryptCalls { get; private set; }
        public int SaveLoginCalls { get; private set; }
        public int CookieCommits { get; private set; }
        public int ProfileFinishes { get; private set; }
        public PasswordLoginSession CommittedSession { get; private set; }
        public bool CommitResult { get; set; } = true;
        public Task<string> EncryptedPassword(string password) { EncryptCalls++; return Encryption; }
        public Task<bool> SaveLogin(string access, string refresh, int expires, long userId, object sso, object cookies)
        {
            SaveLoginCalls++;
            return Task.FromResult(true);
        }
        public bool CommitCookieLogin(PasswordLoginSession session) { CookieCommits++; CommittedSession = session; return CommitResult; }
        public Task FinishCookieLoginAsync(PasswordLoginSession session) { ProfileFinishes++; return Task.CompletedTask; }
    }
}

namespace BiliLite.Models
{
    public enum LoginStatus { Success, Fail, Error, NeedCaptcha, NeedValidate }
    public sealed class LoginCallbackModel
    {
        public LoginStatus status { get; set; }
        public string message { get; set; }
        public string url { get; set; }
    }
    public sealed class LoginResultModel
    {
        public int code { get; set; }
        public string message { get; set; }
        public LoginResultModel data { get; set; }
        public int status { get; set; }
        public string url { get; set; }
        public LoginTokenInfo token_info { get; set; }
        public object sso { get; set; }
        public object cookie_info { get; set; }
    }
    public sealed class LoginTokenInfo
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public int expires_in { get; set; }
        public long mid { get; set; }
    }
    public sealed class GeetestRequestModel
    {
        public string gee_gt { get; set; }
        public string gee_challenge { get; set; }
        public string recaptcha_token { get; set; }
    }
}

namespace BiliLite.Helpers
{
    public static class Utils
    {
        public static void ShowMessageToast(string message) { }
        public static void LaunchUri(Uri uri) { }
        public static void SetClipboard(string value) { }
    }

    public static class MessageCenter
    {
        public static event EventHandler LogoutedEvent;
        public static void RaiseLogout() => LogoutedEvent?.Invoke(null, EventArgs.Empty);
    }
}

namespace BiliLite.Api
{
    public static class ApiHelper { public static ApiKeyInfo LoginKey { get; } = new ApiKeyInfo { Appkey = "fixture", Secret = "fixture" }; }
}

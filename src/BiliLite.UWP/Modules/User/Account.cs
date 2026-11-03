using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Models;
using BiliLite.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.Web.Http.Filters;
using System.Linq;

namespace BiliLite.Modules
{
    public class Account
    {
        /// <summary>设置 ⇄ jar 的接线：判定在 RefreshTokenPolicy，这里只读 jar、读写设置。</summary>
        private static readonly RefreshTokenMirror refreshTokenMirror = new RefreshTokenMirror(
            key => SettingHelper.GetValue(key, ""), (key, value) => SettingHelper.SetValue(key, value));

        private SettingVM settingVM;
        public AccountApi accountApi;
        string guid = "";
        public Account()
        {
            accountApi = new AccountApi();
            settingVM = new SettingVM();
            guid = Guid.NewGuid().ToString();
        }
        public async Task<string> EncryptedPassword(string passWord)
        {
            string base64String;
            try
            {
                HttpBaseProtocolFilter httpBaseProtocolFilter = new HttpBaseProtocolFilter();
                httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(Windows.Security.Cryptography.Certificates.ChainValidationResult.Expired);
                httpBaseProtocolFilter.IgnorableServerCertificateErrors.Add(Windows.Security.Cryptography.Certificates.ChainValidationResult.Untrusted);
                var jObjects = (await accountApi.GetKey().Request()).GetJObject();
                string str = jObjects["data"]["hash"].ToString();
                string str1 = jObjects["data"]["key"].ToString();
                string str2 = string.Concat(str, passWord);
                string str3 = Regex.Match(str1, "BEGIN PUBLIC KEY-----(?<key>[\\s\\S]+)-----END PUBLIC KEY").Groups["key"].Value.Trim();
                byte[] numArray = Convert.FromBase64String(str3);
                AsymmetricKeyAlgorithmProvider asymmetricKeyAlgorithmProvider = AsymmetricKeyAlgorithmProvider.OpenAlgorithm(AsymmetricAlgorithmNames.RsaPkcs1);
                CryptographicKey cryptographicKey = asymmetricKeyAlgorithmProvider.ImportPublicKey(WindowsRuntimeBufferExtensions.AsBuffer(numArray), 0);
                IBuffer buffer = CryptographicEngine.Encrypt(cryptographicKey, WindowsRuntimeBufferExtensions.AsBuffer(Encoding.UTF8.GetBytes(str2)), null);
                base64String = Convert.ToBase64String(WindowsRuntimeBufferExtensions.ToArray(buffer));
            }
            catch (Exception)
            {
                base64String = passWord;
            }
            return base64String;
        }

        /// <summary>
        /// 登录V2
        /// </summary>
        /// <returns></returns>
        public async Task<LoginCallbackModel> LoginV2(string username, string password, string captcha = "")
        {
            try
            {

                var results = await accountApi.LoginV2(username, await EncryptedPassword(password), captcha).Request();
                var m = await results.GetJson<LoginV2Model>();
                if (m.code == 0)
                {
                    m.data.expires_datetime = Utils.TimestampToDatetime(m.ts).AddSeconds(m.data.expires_in);
                    //设置登录状态
                    SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY, m.data.access_token);
                    SettingHelper.SetValue(SettingHelper.Account.USER_ID, m.data.mid);
                    SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.Now.AddSeconds(m.data.expires_in));
                    SettingHelper.SetValue(SettingHelper.Account.REFRESH_KEY, m.data.refresh_token);

                    //执行SSO
                    //await accountApi.SSO(m.data.access_token).Request();
                    //读取个人资料
                    await GetProfile();

                    //发送登录成功事件
                    MessageCenter.SendLogined();

                    return new LoginCallbackModel()
                    {
                        status = LoginStatus.Success,
                        message = "登录成功"
                    };
                }
                else if (m.code == -2100)
                {
                    return new LoginCallbackModel()
                    {
                        status = LoginStatus.NeedValidate,
                        url = m.url,
                        message = "登录需要验证"
                    };
                }
                else if (m.code == -105)
                {
                    return new LoginCallbackModel()
                    {
                        status = LoginStatus.NeedCaptcha,
                        message = "登录需要验证码"
                    };
                }
                else
                {
                    return new LoginCallbackModel()
                    {
                        status = LoginStatus.Fail,
                        message = m.message
                    };
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log("登录错误", LogType.ERROR, ex);
                return new LoginCallbackModel()
                {
                    status = LoginStatus.Error,
                    message = "登录出现小问题,请重试"
                };
            }
        }

        /// <summary>
        /// 登录V3
        /// </summary>
        /// <returns></returns>
        public async Task<LoginCallbackModel> LoginV3(string username, string password, string seccode = "", string validate = "", string recaptcha_token = "", string challenge = "", int gee_type = 10)
        {
            try
            {

                var results = await accountApi.LoginV3(username, await EncryptedPassword(password), seccode, validate, challenge, recaptcha_token).Request();
                if (results.status)
                {
                    var m = await results.GetData<LoginDataV3Model>();
                    if (m.code == 0)
                    {
                        if (m.data.status == 0)
                        {
                            await SaveLogin(m.data.token_info.access_token, m.data.token_info.refresh_token, m.data.token_info.expires_in, m.data.token_info.mid, m.data.sso,m.data.cookie_info, AppkeyIdentity.Login);
                            return new LoginCallbackModel()
                            {
                                status = LoginStatus.Success,
                                message = "登录成功"
                            };
                        }
                        if (m.data.status == 1 || m.data.status == 2)
                        {
                            return new LoginCallbackModel()
                            {
                                status = LoginStatus.NeedValidate,
                                message = "本次登录需要安全验证",
                                url = m.data.url
                            };
                        }
                        return new LoginCallbackModel()
                        {
                            status = LoginStatus.Fail,
                            message = m.message
                        };
                    }
                    else if (m.code == -105)
                    {
                        return new LoginCallbackModel()
                        {
                            status = LoginStatus.NeedCaptcha,
                            url = m.data.url,
                            message = "登录需要验证码"
                        };
                    }
                    else
                    {
                        return new LoginCallbackModel()
                        {
                            status = LoginStatus.Fail,
                            message = m.message
                        };
                    }
                }
                else
                {
                    LogHelper.Log("登录V3请求失败错误", LogType.ERROR);
                    return new LoginCallbackModel()
                    {
                        status = LoginStatus.Error,
                        message = results.message
                    };
                }

            }
            catch (Exception ex)
            {
                LogHelper.Log("登录V3错误", LogType.ERROR, ex);
                return new LoginCallbackModel()
                {
                    status = LoginStatus.Error,
                    message = "登录出现小问题,请重试"
                };
            }
        }
        
        /// <summary>
        /// 登录V3 2023
        /// </summary>
        /// <returns></returns>
        public async Task<LoginCallbackModel> Login2023(string username, string password)
        {
            try
            {

                var results = await accountApi.LoginV3(username, await EncryptedPassword(password)).Request();
                if (results.status)
                {
                    var m = await results.GetData<LoginDataV3Model>();
                    if (m.code == 0)
                    {
                        if (m.data.status == 0)
                        {
                            await SaveLogin(m.data.token_info.access_token, m.data.token_info.refresh_token, m.data.token_info.expires_in, m.data.token_info.mid, m.data.sso,m.data.cookie_info, AppkeyIdentity.Login);
                            return new LoginCallbackModel()
                            {
                                status = LoginStatus.Success,
                                message = "登录成功"
                            };
                        }
                        if (m.data.status == 1 || m.data.status == 2)
                        {
                            return new LoginCallbackModel()
                            {
                                status = LoginStatus.NeedValidate,
                                message = "本次登录需要安全验证",
                                url = m.data.url
                            };
                        }
                        return new LoginCallbackModel()
                        {
                            status = LoginStatus.Fail,
                            message = m.message
                        };
                    }
                    else if (m.code == -105)
                    {
                        return new LoginCallbackModel()
                        {
                            status = LoginStatus.NeedCaptcha,
                            url = m.data.url,
                            message = "登录需要验证码"
                        };
                    }
                    else
                    {
                        return new LoginCallbackModel()
                        {
                            status = LoginStatus.Fail,
                            message = m.message
                        };
                    }
                }
                else
                {
                    LogHelper.Log("登录V3 2023请求失败错误", LogType.ERROR);
                    return new LoginCallbackModel()
                    {
                        status = LoginStatus.Error,
                        message = results.message
                    };
                }

            }
            catch (Exception ex)
            {
                LogHelper.Log("登录V3 2023错误", LogType.ERROR, ex);
                return new LoginCallbackModel()
                {
                    status = LoginStatus.Error,
                    message = "登录出现小问题,请重试"
                };
            }
        }


        /// <summary>
        /// 安全验证后保存状态。acquiredWith 记录这一对令牌是哪把配置 appkey 换来的，续期必须用同一把；
        /// 没记来历的调用方保持旧语义（UnknownLegacy），续期沿用修复前的 TV 键。
        /// </summary>
        /// <param name="access_key"></param>
        /// <param name="refresh_token"></param>
        /// <param name="expires"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        public async Task<bool> SaveLogin(string access_key, string refresh_token, int expires, long userid, List<string> sso,LoginCookieInfo cookie, AppkeyIdentity acquiredWith = AppkeyIdentity.UnknownLegacy)
        {
            // sso 参数只是历史遗留：OpenAPI 的 SSO 调用早就不发了，原来的默认地址既没被读过也没被发出去
            var commit = PersistCredentials(access_key, refresh_token, expires, userid, cookie);
            if (!commit.TokenSaved) return false;
            // 只有令牌真的落盘才记来历：写失败时旧凭证明明还在，来历也不能跟着改
            SettingHelper.SetValue(AppkeyPolicy.SettingKey, AppkeyPolicy.Format(acquiredWith));
            await AnnounceLoginAsync();
            return true;
        }

        /// <summary>
        /// 令牌四件套整组落盘，cookie 单独装 jar，两件事分开报。
        /// 写入中途抛出就把四件套整组退回旧值，宁可全旧也不留半套凭证；
        /// 但这只是本机回滚 —— 服务端已经轮换掉的 refresh_token 回不来，只能下次重新续期。
        /// </summary>
        private static CredentialCommit PersistCredentials(string access_key, string refresh_token, int expires,
            long userid, LoginCookieInfo cookie)
        {
            var commit = new CredentialCommit
            {
                CookieOffered = cookie?.cookies != null && cookie.cookies.Count > 0,
            };
            var previousAccessKey = SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY, "");
            var previousRefreshToken = SettingHelper.GetValue(SettingHelper.Account.REFRESH_KEY, "");
            var previousUserId = SettingHelper.GetValue(SettingHelper.Account.USER_ID, 0L);
            var previousExpiry = SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.MinValue);

            try
            {
                //设置登录状态
                SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY, access_key);
                SettingHelper.SetValue(SettingHelper.Account.USER_ID, userid);
                SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.Now.AddSeconds(expires));
                SettingHelper.SetValue(SettingHelper.Account.REFRESH_KEY, refresh_token);
                commit.TokenSaved = true;
            }
            catch (Exception ex)
            {
                LogHelper.Log("安全验证后设置保存信息失败，整组退回旧值 exceptionType=" + ex.GetType().FullName, LogType.ERROR);
                RestoreCredentialSettings(previousAccessKey, previousRefreshToken, previousUserId, previousExpiry);
                return commit;
            }

            if (commit.CookieOffered)
            {
                //设置Cookie：每个 cookie 单独算成败，装失败不算令牌没换
                var installed = 0;
                var failed = 0;
                try
                {
                    HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();
                    foreach (var domain in cookie.domains ?? new List<string>())
                    {
                        foreach (var cookieItem in cookie.cookies)
                        {
                            try
                            {
                                filter.CookieManager.SetCookie(new Windows.Web.Http.HttpCookie(cookieItem.name, domain, "/")
                                {
                                    HttpOnly=cookieItem.http_only==1,
                                    Secure=cookieItem.secure==1,
                                    Expires=Utils.TimestampToDatetime(cookieItem.expires),
                                    Value=cookieItem.value,
                                });
                                installed++;
                            }
                            catch (Exception ex)
                            {
                                failed++;
                                LogHelper.Log("装 cookie 失败 exceptionType=" + ex.GetType().FullName, LogType.ERROR);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    LogHelper.Log("装 cookie 失败 exceptionType=" + ex.GetType().FullName, LogType.ERROR);
                }

                commit.CookieInstalled = failed == 0 && installed > 0;
            }

            // ac_time_value 只随 cookie 下发，登录成功当场落到设置，否则刷新链永远缺令牌；
            // 但响应令牌已存时它不许覆盖（判定在 RefreshTokenPolicy）
            MirrorRefreshTokenFromJar();
            return commit;
        }

        private static void RestoreCredentialSettings(string accessKey, string refreshToken, long userId,
            DateTime expiry)
        {
            try
            {
                SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY, accessKey);
                SettingHelper.SetValue(SettingHelper.Account.USER_ID, userId);
                SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, expiry);
                SettingHelper.SetValue(SettingHelper.Account.REFRESH_KEY, refreshToken);
            }
            catch (Exception ex)
            {
                LogHelper.Log("凭证回滚失败 exceptionType=" + ex.GetType().FullName, LogType.ERROR);
            }
        }

        /// <summary>读资料和广播都不属于凭证结论：资料拿不到只是没有头像，登录事件没人接也不该回滚令牌。</summary>
        private async Task AnnounceLoginAsync()
        {
            try
            {
                await GetProfile();
            }
            catch (Exception ex)
            {
                LogHelper.Log("读取个人资料失败 exceptionType=" + ex.GetType().FullName, LogType.ERROR);
            }
            try
            {
                MessageCenter.SendLogined();
            }
            catch (Exception ex)
            {
                LogHelper.Log("登录事件广播失败 exceptionType=" + ex.GetType().FullName, LogType.ERROR);
            }
        }

        public bool CommitCookieLogin(PasswordLoginSession session)
        {
            return PasswordLoginSessionCommitter.TryCommit(session, () => CaptureCookieLoginSnapshot(session),
                ApplyCookieLogin, RestoreCookieLoginSnapshot,
                message => LogHelper.Log(message, LogType.ERROR));
        }

        public async Task FinishCookieLoginAsync(PasswordLoginSession session)
        {
            MyProfileModel profile = null;
            try { profile = await FetchProfileAsync(); }
            catch (Exception ex) { LogHelper.Log("Cookie login profile fetch failed. exceptionType=" + ex.GetType().FullName, LogType.ERROR); }
            if (session == null || SettingHelper.GetValue(SettingHelper.Account.USER_ID, 0L) != session.UserId ||
                !string.Equals(SettingHelper.Account.AccessKey, session.AccessKey, StringComparison.Ordinal)) return;
            if (profile != null && profile.mid == session.UserId)
            {
                try { SettingHelper.SetValue(SettingHelper.Account.USER_PROFILE, profile); }
                catch (Exception ex) { LogHelper.Log("Cookie login profile persistence failed. exceptionType=" + ex.GetType().FullName, LogType.ERROR); }
            }
            try { MessageCenter.SendLogined(); }
            catch (Exception ex) { LogHelper.Log("Cookie login notification failed. exceptionType=" + ex.GetType().FullName, LogType.ERROR); }
        }

        private static CookieLoginSnapshot CaptureCookieLoginSnapshot(PasswordLoginSession session)
        {
            var filter = new HttpBaseProtocolFilter();
            var origins = CookieLoginOrigins();
            var cookies = new Dictionary<string, Windows.Web.Http.HttpCookie>(StringComparer.OrdinalIgnoreCase);
            foreach (var origin in origins)
                foreach (var cookie in filter.CookieManager.GetCookies(origin))
                {
                    var key = CookieKey(cookie.Name, cookie.Domain, cookie.Path);
                    if (!cookies.ContainsKey(key)) cookies.Add(key, cookie);
                }

            return new CookieLoginSnapshot
            {
                Cookies = cookies.Values.ToList(),
                StagedCookieKeys = new HashSet<string>(session.Cookies.Select(x => CookieKey(x.Name, x.Domain, x.Path)), StringComparer.OrdinalIgnoreCase),
                AccessKey = SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY, ""),
                UserId = SettingHelper.GetValue(SettingHelper.Account.USER_ID, 0L),
                Expiry = SettingHelper.GetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.MinValue),
                OAuthRefreshToken = SettingHelper.GetValue(SettingHelper.Account.REFRESH_KEY, ""),
                CookieRefreshToken = SettingHelper.GetValue(RefreshTokenMirror.TokenKey, ""),
                CookieRefreshTokenSource = SettingHelper.GetValue(RefreshTokenMirror.SourceKey, ""),
                CookieRefreshTokenOwner = RefreshTokenPolicy.ParseOwner(SettingHelper.GetValue(RefreshTokenMirror.OwnerKey, "")),
                CookieSession = SettingHelper.GetValue("CookieSESSDATA", ""),
                CookieCsrf = SettingHelper.GetValue("CookieCSRF", ""),
                Profile = SettingHelper.GetValue<MyProfileModel>(SettingHelper.Account.USER_PROFILE, null)
            };
        }

        private static void ApplyCookieLogin(PasswordLoginSession session)
        {
            var filter = new HttpBaseProtocolFilter();
            var origins = CookieLoginOrigins();
            var replacedNames = CookieLoginReplacedNames();
            foreach (var origin in origins)
                foreach (var current in filter.CookieManager.GetCookies(origin).ToList())
                    if (replacedNames.Contains(current.Name)) filter.CookieManager.DeleteCookie(current);

            foreach (var cookie in session.Cookies)
            {
                var staged = new Windows.Web.Http.HttpCookie(cookie.Name, cookie.Domain, cookie.Path)
                {
                    Value = cookie.Value,
                    HttpOnly = cookie.HttpOnly,
                    Secure = cookie.Secure
                };
                if (cookie.Expires.HasValue) staged.Expires = cookie.Expires.Value.UtcDateTime;
                if (!filter.CookieManager.SetCookie(staged))
                    throw new InvalidOperationException("Windows rejected a staged Bilibili session cookie.");
            }

            var sessdata = session.Cookies.First(x => x.Name == "SESSDATA").Value;
            var csrf = session.Cookies.First(x => x.Name == "bili_jct").Value;
            SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY, session.AccessKey);
            SettingHelper.SetValue(SettingHelper.Account.USER_ID, session.UserId);
            // Browser-cookie refresh tokens are separate from the target's OAuth refresh token.
            SettingHelper.SetValue(SettingHelper.Account.REFRESH_KEY, "");
            SettingHelper.SetValue(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.Now.AddHours(240));
            // 交换回来的令牌来自响应 JSON：来源和 uid 一起记，紧接着的 cookie 镜像就不会拿旧 ac_time_value 覆盖它
            refreshTokenMirror.RecordResponse(session.CookieRefreshToken, session.UserId);
            SettingHelper.SetValue("CookieSESSDATA", sessdata);
            SettingHelper.SetValue("CookieCSRF", csrf);
            SettingHelper.SetValue<MyProfileModel>(SettingHelper.Account.USER_PROFILE, null);
            // 交换回来的令牌为空时，cookie 里的 ac_time_value 才是唯一来源
            MirrorRefreshTokenFromJar();
        }

        /// <summary>
        /// jar 里的 ac_time_value 只作为旧会话的迁移来源：设置里已有响应令牌、有挂起确认或归属对不上时，
        /// 一个键都不动。QR 登录这类拿不到响应令牌的会话靠它补齐。
        /// </summary>
        private static void MirrorRefreshTokenFromJar()
        {
            try
            {
                refreshTokenMirror.MirrorFromCookie(ReadJarCookie("ac_time_value"),
                    RefreshTokenPolicy.ParseOwner(ReadJarCookie("DedeUserID")),
                    SettingHelper.GetValue(SettingHelper.Account.USER_ID, 0L),
                    // 挂起确认（D3）还没有持久化记录，眼下没有可读的确认在途
                    false);
            }
            catch (Exception ex)
            {
                LogHelper.Log("ac_time_value 同步失败 exceptionType=" + ex.GetType().FullName, LogType.ERROR);
            }
        }

        private static string ReadJarCookie(string name)
        {
            var filter = new HttpBaseProtocolFilter();
            foreach (var origin in CookieLoginOrigins())
            {
                var cookie = filter.CookieManager.GetCookies(origin)
                    .FirstOrDefault(x => x.Name == name);
                if (cookie != null && !string.IsNullOrEmpty(cookie.Value)) return cookie.Value;
            }
            return "";
        }

        private static void RestoreCookieLoginSnapshot(CookieLoginSnapshot snapshot)
        {
            var filter = new HttpBaseProtocolFilter();
            var failures = new List<string>();
            var origins = CookieLoginOrigins();
            var stagedNames = CookieLoginReplacedNames();
            try
            {
                foreach (var origin in origins)
                    foreach (var current in filter.CookieManager.GetCookies(origin).ToList())
                        if (stagedNames.Contains(current.Name) || snapshot.StagedCookieKeys.Contains(CookieKey(current.Name, current.Domain, current.Path)))
                            filter.CookieManager.DeleteCookie(current);
            }
            catch (Exception ex) { failures.Add("cookie-delete=" + ex.GetType().FullName); }

            foreach (var cookie in snapshot.Cookies)
            {
                try
                {
                    if (!filter.CookieManager.SetCookie(cookie)) failures.Add("cookie-restore-rejected");
                }
                catch (Exception ex) { failures.Add("cookie-restore=" + ex.GetType().FullName); }
            }

            RestoreSetting(SettingHelper.Account.ACCESS_KEY, snapshot.AccessKey, failures);
            RestoreSetting(SettingHelper.Account.USER_ID, snapshot.UserId, failures);
            RestoreSetting(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, snapshot.Expiry, failures);
            RestoreSetting(SettingHelper.Account.REFRESH_KEY, snapshot.OAuthRefreshToken, failures);
            RestoreSetting(RefreshTokenMirror.TokenKey, snapshot.CookieRefreshToken, failures);
            RestoreSetting(RefreshTokenMirror.SourceKey, snapshot.CookieRefreshTokenSource, failures);
            RestoreSetting(RefreshTokenMirror.OwnerKey, snapshot.CookieRefreshTokenOwner.ToString(System.Globalization.CultureInfo.InvariantCulture), failures);
            RestoreSetting("CookieSESSDATA", snapshot.CookieSession, failures);
            RestoreSetting("CookieCSRF", snapshot.CookieCsrf, failures);
            RestoreSetting(SettingHelper.Account.USER_PROFILE, snapshot.Profile, failures);
            if (failures.Count > 0) throw new InvalidOperationException("Cookie login rollback had incomplete steps: " + string.Join(",", failures));
        }

        private static void RestoreSetting<T>(string key, T value, List<string> failures)
        {
            try { SettingHelper.SetValue(key, value); }
            catch (Exception ex) { failures.Add("setting-" + key + "=" + ex.GetType().FullName); }
        }

        private static List<Uri> CookieLoginOrigins() => CookieOrigins.All.Select(x => new Uri(x)).ToList();

        private static HashSet<string> CookieLoginReplacedNames() => new HashSet<string>(new[]
        {
            "SESSDATA", "bili_jct", "DedeUserID", "DedeUserID__ckMd5", "sid", "sid_guard", "ac_time_value"
        }, StringComparer.OrdinalIgnoreCase);

        private static string CookieKey(string name, string domain, string path) =>
            (name ?? "") + "\n" + (domain ?? "") + "\n" + (path ?? "/");

        private sealed class CookieLoginSnapshot
        {
            public List<Windows.Web.Http.HttpCookie> Cookies { get; set; }
            public HashSet<string> StagedCookieKeys { get; set; }
            public string AccessKey { get; set; }
            public long UserId { get; set; }
            public DateTime Expiry { get; set; }
            public string OAuthRefreshToken { get; set; }
            public string CookieRefreshToken { get; set; }
            public string CookieRefreshTokenSource { get; set; }
            public long CookieRefreshTokenOwner { get; set; }
            public string CookieSession { get; set; }
            public string CookieCsrf { get; set; }
            public MyProfileModel Profile { get; set; }
        }

        public async Task<MyProfileModel> GetProfile()
        {
            try
            {
                var profile = await FetchProfileAsync();
                if (profile != null) SettingHelper.SetValue(SettingHelper.Account.USER_PROFILE, profile);
                return profile;
            }
            catch (Exception ex)
            {
                LogHelper.Log("读取个人资料失败", LogType.ERROR, ex);
                return null;
            }
        }

        private async Task<MyProfileModel> FetchProfileAsync()
        {
            try
            {
                var req = await accountApi.UserProfile().Request();
                var obj = req.GetJObject();
                if (req.status && obj["code"].ToInt32() == 0)
                {
                    return JsonConvert.DeserializeObject<MyProfileModel>(obj["data"].ToString());
                }
                else
                {
                    if (obj != null)
                    {
                        LogHelper.Log((string)obj["message"], LogType.INFO);
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log("读取个人资料失败", LogType.ERROR, ex);
                return null;
            }
        }

        public async Task<HomeUserCardModel> GetHomeUserCard()
        {

            try
            {
                var mine_api = accountApi.MineProfile();
                var mine_result = await mine_api.Request();
                if (!mine_result.status)
                {
                    return null;
                }
                var mine_obj = mine_result.GetJObject();
                if (mine_obj["code"].ToInt32() != 0)
                {
                    return null;
                }
                var space_api = accountApi.Space(SettingHelper.Account.UserID.ToString());
                var space_result = await space_api.Request();
                if (!space_result.status)
                {
                    return null;
                }
                var space_obj = space_result.GetJObject();
                if (space_obj["code"].ToInt32() != 0)
                {
                    return null;
                }
                var data = new HomeUserCardModel();
                data.current_exp = space_obj["data"]["card"]["level_info"]["current_exp"].ToInt32();
                data.next_exp = space_obj["data"]["card"]["level_info"]["next_exp"].ToInt32();
                data.dynamic = mine_obj["data"]["dynamic"].ToInt32();
                data.face = mine_obj["data"]["face"].ToString();
                data.fans = mine_obj["data"]["follower"].ToInt32();
                data.follow = mine_obj["data"]["following"].ToInt32();
                data.level = mine_obj["data"]["level"].ToInt32();
                data.mid = mine_obj["data"]["mid"].ToString();
                data.name = mine_obj["data"]["name"].ToString();
                data.pendant = space_obj["data"]["card"]["pendant"]["image"].ToString();
                if (data.pendant == "")
                {
                    data.pendant = AppHelper.TRANSPARENT_IMAGE;
                }
                data.vip_type = mine_obj["data"]["vip_type"].ToInt32();
                return data;

            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取二维码登录信息
        /// </summary>
        /// <returns></returns>
        public async Task<ReturnModel<QRAuthInfo>> GetQRAuthInfo()
        {
            try
            {
                var result = await accountApi.QRLoginAuthCode(guid).Request();
                if (result.status)
                {
                    var data = await result.GetData<QRAuthInfo>();
                    if (data.success)
                    {
                        return new ReturnModel<QRAuthInfo>()
                        {
                            success = true,
                            data = data.data
                        };
                    }
                    else
                    {
                        return new ReturnModel<QRAuthInfo>()
                        {
                            success = false,
                            message = data.message
                        };

                    }
                }
                else
                {
                    return new ReturnModel<QRAuthInfo>()
                    {
                        success = false,
                        message = result.message
                    };
                }
            }
            catch (Exception ex)
            {
                return new ReturnModel<QRAuthInfo>()
                {
                    success = false,
                    message = ex.Message
                };
            }
        }
        /// <summary>
        /// 轮询二维码扫描信息
        /// </summary>
        /// <returns></returns>
        public async Task<LoginCallbackModel> PollQRAuthInfo(string auth_code)
        {
            try
            {
                var result = await accountApi.QRLoginPoll(auth_code, guid).Request();
                if (result.status)
                {
                    var data = await result.GetData<LoginDataV3Model>();
                    if (data.success)
                    {
                        
                        await SaveLogin(data.data.token_info.access_token, data.data.token_info.refresh_token, data.data.token_info.expires_in, data.data.token_info.mid, data.data.sso,data.data.cookie_info, AppkeyIdentity.Tv);
                        return new LoginCallbackModel()
                        {
                            status = LoginStatus.Success,
                            message = "登录成功"
                        };
                    }
                    else
                    {
                        return new LoginCallbackModel()
                        {
                            status = LoginStatus.Fail,
                            message = data.message
                        };

                    }
                }
                else
                {
                    return new LoginCallbackModel()
                    {
                        status = LoginStatus.Fail,
                        message = result.message
                    };
                }
            }
            catch (Exception ex)
            {
                return new LoginCallbackModel()
                {
                    status = LoginStatus.Fail,
                    message = ex.Message
                };
            }
        }

        /// <summary>
        /// 检查登录状态
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckLoginState()
        {
            try
            {
                var req = await accountApi.GetOAuth2Info().Request();
                if (req.status)
                {
                    var obj = req.GetJObject();
                    return obj["code"].ToInt32() == 0;
                }
                else
                {
                    throw new Exception(req.message);
                    //return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
                //LogHelper.Log("读取access_key信息失败", LogType.ERROR, ex);
                //return LogHelper.IsNetworkError(ex);
            }
        }
        /// <summary>
        /// 刷新Token。解析出来的令牌和 cookie_info 一起交给落盘，jar、ACCESS_KEY、轮换后的 REFRESH_KEY
        /// 和到期时间才会同步换新；结果如实带回调用方，网络失败和会话失效不是同一件事。
        /// </summary>
        /// <returns></returns>
        public async Task<RenewalResult> RefreshToken()
        {
            return await AccessKeyRenewal.RunAsync(
                SettingHelper.Account.AccessKey,
                SettingHelper.GetValue<string>(SettingHelper.Account.REFRESH_KEY, ""),
                (accessKey, refreshToken) => accountApi.RefreshToken(accessKey, refreshToken).Request(),
                SaveRenewedLoginAsync,
                (message, exception) => LogHelper.Log(message, exception == null ? LogType.INFO : LogType.ERROR, exception));
        }

        /// <summary>续期的落盘回调：只报令牌和 cookie 有没有写成功，读资料留在凭证之外。</summary>
        private async Task<CredentialCommit> SaveRenewedLoginAsync(LoginTokenInfo token, LoginCookieInfo cookie)
        {
            var userId = token.mid > 0 ? token.mid : SettingHelper.GetValue(SettingHelper.Account.USER_ID, 0L);
            var commit = PersistCredentials(token.access_token, token.refresh_token, token.expires_in, userId, cookie);
            if (!commit.TokenSaved) return commit;
            await AnnounceLoginAsync();
            return commit;
        }
    }

    public class HomeUserCardModel
    {
        public string name { get; set; }
        public string face { get; set; }
        public string mid { get; set; }
        public int fans { get; set; }
        public int vip_type { get; set; }
        public int follow { get; set; }
        public int dynamic { get; set; }
        public string pendant { get; set; }

        public int level { get; set; }
        public int current_exp { get; set; }
        public int next_exp { get; set; }

        public bool show_vip
        {
            get
            {
                return vip_type != 0;
            }
        }

        public string vip
        {
            get
            {

                return vip_type == 2 ? "年度大会员" : "月度大会员";
            }
        }
    }

    public class QRAuthInfo
    {
        public string url { get; set; }
        public string auth_code { get; set; }
    }

}

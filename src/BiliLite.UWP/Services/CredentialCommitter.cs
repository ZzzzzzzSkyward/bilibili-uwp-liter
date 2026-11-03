using BiliLite.Helpers;
using System;
using System.Collections.Generic;

namespace BiliLite.Services
{
    /// <summary>一次 cookie 写入的纯数据形状：不带 WinRT 类型，探针可以拿假 jar 驱动它。</summary>
    public sealed class CookieWrite
    {
        public string Name { get; set; }

        public string Domain { get; set; }

        public string Path { get; set; }

        public string Value { get; set; }

        public bool HttpOnly { get; set; }

        public bool Secure { get; set; }

        public DateTime Expires { get; set; }
    }

    /// <summary>设置项的读写口：产品走 SettingHelper，探针拿内存字典。</summary>
    public interface ICredentialSettings
    {
        T Read<T>(string key, T fallback);

        void Write<T>(string key, T value);
    }

    /// <summary>
    /// 令牌四件套和 cookie 的落盘判定。两者恢复的是不同能力，所以分开算：
    /// cookie 的每一下写入单独计数，jar 返回 false 就是 Windows 拒收，和抛异常一样算失败，
    /// 不能因为写调用没抛就把这一位填成「装好了」。
    /// </summary>
    public static class CredentialCommitter
    {
        public static CredentialCommit Commit(string accessKey, string refreshToken, DateTime expiry, long userId,
            bool cookieOffered, IList<CookieWrite> cookieWrites, ICredentialSettings settings,
            Func<CookieWrite, bool> setCookie, Action<string, Exception> log)
        {
            var commit = new CredentialCommit
            {
                CookieOffered = cookieOffered,
            };
            var previousAccessKey = settings.Read(SettingHelper.Account.ACCESS_KEY, "");
            var previousRefreshToken = settings.Read(SettingHelper.Account.REFRESH_KEY, "");
            var previousUserId = settings.Read(SettingHelper.Account.USER_ID, 0L);
            var previousExpiry = settings.Read(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, DateTime.MinValue);

            try
            {
                settings.Write(SettingHelper.Account.ACCESS_KEY, accessKey);
                settings.Write(SettingHelper.Account.USER_ID, userId);
                settings.Write(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, expiry);
                settings.Write(SettingHelper.Account.REFRESH_KEY, refreshToken);
                commit.TokenSaved = true;
            }
            catch (Exception ex)
            {
                log("安全验证后设置保存信息失败，整组退回旧值 exceptionType=" + ex.GetType().FullName, ex);
                Restore(settings, previousAccessKey, previousRefreshToken, previousUserId, previousExpiry, log);
                return commit;
            }

            if (commit.CookieOffered)
            {
                //设置Cookie：每个 cookie 单独算成败，装失败不算令牌没换
                var installed = 0;
                var failed = 0;
                foreach (var write in cookieWrites)
                {
                    try
                    {
                        if (setCookie(write)) installed++;
                        else
                        {
                            failed++;
                            log("装 cookie 被拒绝 name=" + write.Name + " domain=" + write.Domain, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        log("装 cookie 失败 exceptionType=" + ex.GetType().FullName, ex);
                    }
                }

                commit.CookieInstalled = failed == 0 && installed > 0;
            }

            return commit;
        }

        private static void Restore(ICredentialSettings settings, string accessKey, string refreshToken, long userId,
            DateTime expiry, Action<string, Exception> log)
        {
            try
            {
                settings.Write(SettingHelper.Account.ACCESS_KEY, accessKey);
                settings.Write(SettingHelper.Account.USER_ID, userId);
                settings.Write(SettingHelper.Account.ACCESS_KEY_EXPIRE_DATE, expiry);
                settings.Write(SettingHelper.Account.REFRESH_KEY, refreshToken);
            }
            catch (Exception ex)
            {
                log("凭证回滚失败 exceptionType=" + ex.GetType().FullName, ex);
            }
        }
    }
}

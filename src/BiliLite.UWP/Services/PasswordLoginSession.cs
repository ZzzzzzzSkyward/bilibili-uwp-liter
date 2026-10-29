using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BiliLite.Services
{
    public sealed class PasswordLoginCookie
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Domain { get; set; }
        public string Path { get; set; }
        public bool HttpOnly { get; set; }
        public bool Secure { get; set; }
        public DateTimeOffset? Expires { get; set; }
    }

    public sealed class PasswordLoginSession
    {
        public string AccessKey { get; set; }
        public string CookieRefreshToken { get; set; }
        public long UserId { get; set; }
        public IReadOnlyList<PasswordLoginCookie> Cookies { get; set; }
    }

    public static class PasswordLoginSessionValidator
    {
        private static readonly Uri[] CookieOrigins =
        {
            new Uri("https://bilibili.com/"),
            new Uri("https://www.bilibili.com/"),
            new Uri("https://passport.bilibili.com/"),
            new Uri("https://api.bilibili.com/")
        };

        public static string ParseExchangeRefreshToken(string responseBody)
        {
            JObject body;
            try { body = JObject.Parse(responseBody ?? ""); }
            catch { throw new FormatException("Password login cookie exchange returned invalid JSON."); }
            if ((int?)body["code"] != 0)
                throw new FormatException("Password login cookie exchange did not return success.");
            var token = (string)body["data"]?["refresh_token"];
            if (string.IsNullOrWhiteSpace(token))
                throw new FormatException("Password login cookie exchange response was missing its refresh token.");
            return token;
        }

        public static CookieContainer ParseCookies(IEnumerable<string> setCookieHeaders, Uri responseOrigin)
        {
            if (responseOrigin == null || !responseOrigin.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !(responseOrigin.Host.Equals("bilibili.com", StringComparison.OrdinalIgnoreCase) || responseOrigin.Host.EndsWith(".bilibili.com", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("A Bilibili HTTPS response origin is required.", nameof(responseOrigin));
            var container = new CookieContainer();
            var count = 0;
            foreach (var header in setCookieHeaders ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(header)) continue;
                try { container.SetCookies(responseOrigin, header); }
                catch { throw new FormatException("Password login cookie exchange returned malformed cookies."); }
                count++;
            }

            if (count == 0) throw new FormatException("Password login cookie exchange response did not include cookies.");
            return container;
        }

        public static IReadOnlyList<PasswordLoginCookie> ReadCookies(CookieContainer container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            var cookies = new Dictionary<string, PasswordLoginCookie>(StringComparer.OrdinalIgnoreCase);
            foreach (var origin in CookieOrigins)
            {
                foreach (Cookie cookie in container.GetCookies(origin))
                {
                    var key = cookie.Name + "\n" + cookie.Domain + "\n" + cookie.Path;
                    cookies[key] = new PasswordLoginCookie
                    {
                        Name = cookie.Name,
                        Value = cookie.Value,
                        Domain = cookie.Domain,
                        Path = string.IsNullOrEmpty(cookie.Path) ? "/" : cookie.Path,
                        HttpOnly = cookie.HttpOnly,
                        Secure = cookie.Secure,
                        Expires = cookie.Expires == DateTime.MinValue ? (DateTimeOffset?)null : new DateTimeOffset(cookie.Expires.ToUniversalTime())
                    };
                }
            }
            return cookies.Values.ToList();
        }

        public static PasswordLoginSession CreateSession(string accessKey, string refreshToken, CookieContainer cookies, string redirectMid = null)
        {
            if (string.IsNullOrWhiteSpace(accessKey)) throw new FormatException("Password login did not produce an access key.");
            if (string.IsNullOrWhiteSpace(refreshToken)) throw new FormatException("Password login did not produce a cookie refresh token.");
            if (cookies == null) throw new ArgumentNullException(nameof(cookies));
            var stagedCookies = ReadCookies(cookies);
            var idCookie = stagedCookies.FirstOrDefault(x => string.Equals(x.Name, "DedeUserID", StringComparison.Ordinal));
            long userId;
            if (idCookie == null || !long.TryParse(idCookie.Value, NumberStyles.None, CultureInfo.InvariantCulture, out userId))
                throw new FormatException("Password login cookies were missing a valid user id.");
            var session = new PasswordLoginSession
            {
                AccessKey = accessKey,
                CookieRefreshToken = refreshToken,
                UserId = userId,
                Cookies = stagedCookies
            };
            ValidateSession(session, redirectMid);
            return session;
        }

        public static void ValidateSession(PasswordLoginSession session, string redirectMid = null)
        {
            if (session == null || string.IsNullOrWhiteSpace(session.AccessKey) || string.IsNullOrWhiteSpace(session.CookieRefreshToken) ||
                session.UserId <= 0 || session.Cookies == null || session.Cookies.Count == 0)
                throw new FormatException("Password login did not produce a complete session.");
            if (session.Cookies.Any(x => !IsAllowedCookie(x)))
                throw new FormatException("Password login returned a cookie outside the Bilibili session scope.");
            var stagedCookies = session.Cookies;
            var idCookies = stagedCookies.Where(x => string.Equals(x.Name, "DedeUserID", StringComparison.Ordinal)).ToList();
            var userIds = new HashSet<long>();
            foreach (var idCookie in idCookies)
            {
                long parsed;
                if (string.IsNullOrWhiteSpace(idCookie.Value) ||
                    !long.TryParse(idCookie.Value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed) || parsed <= 0)
                    throw new FormatException("Password login cookies were missing a valid user id.");
                if (idCookie.Expires.HasValue && idCookie.Expires.Value <= DateTimeOffset.UtcNow)
                    throw new FormatException("Password login cookies contained an expired user id.");
                userIds.Add(parsed);
            }
            if (userIds.Count != 1)
                throw new FormatException("Password login cookies were missing a valid user id.");
            var userId = userIds.Single();
            if (userId != session.UserId)
                throw new FormatException("Password login cookie user did not match the staged user id.");
            long returnedMid;
            if (!string.IsNullOrWhiteSpace(redirectMid) &&
                (!long.TryParse(redirectMid, NumberStyles.None, CultureInfo.InvariantCulture, out returnedMid) || returnedMid != userId))
                throw new FormatException("Password login confirmation user did not match the staged cookie user.");
            if (string.IsNullOrWhiteSpace(session.AccessKey) || string.IsNullOrWhiteSpace(session.CookieRefreshToken))
                throw new FormatException("Password login did not produce complete session tokens.");
            if (!HasUsableCookie(stagedCookies, "SESSDATA") || !HasUsableCookie(stagedCookies, "bili_jct"))
                throw new FormatException("Password login cookies were missing required session fields.");
            if (!HasUsableCookie(stagedCookies, "DedeUserID"))
                throw new FormatException("Password login cookies were missing a usable user id cookie.");
        }

        private static bool HasUsableCookie(IEnumerable<PasswordLoginCookie> cookies, string name)
        {
            var matches = cookies.Where(x => string.Equals(x.Name, name, StringComparison.Ordinal)).ToList();
            return matches.Count > 0 && matches.All(x => !string.IsNullOrWhiteSpace(x.Value) &&
                string.Equals(x.Path, "/", StringComparison.Ordinal) &&
                string.Equals(x.Domain.TrimStart('.'), "bilibili.com", StringComparison.OrdinalIgnoreCase) &&
                (!x.Expires.HasValue || x.Expires.Value > DateTimeOffset.UtcNow));
        }

        private static bool IsAllowedCookie(PasswordLoginCookie cookie)
        {
            if (cookie == null || string.IsNullOrWhiteSpace(cookie.Name) || string.IsNullOrWhiteSpace(cookie.Value) ||
                string.IsNullOrWhiteSpace(cookie.Domain) || !string.Equals(cookie.Path, "/", StringComparison.Ordinal)) return false;
            return string.Equals(cookie.Domain.TrimStart('.'), "bilibili.com", StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class PasswordLoginSessionCommitter
    {
        public static bool TryCommit<TSnapshot>(PasswordLoginSession session, Func<TSnapshot> capture,
            Action<PasswordLoginSession> apply, Action<TSnapshot> restore, Action<string> log)
        {
            if (capture == null) throw new ArgumentNullException(nameof(capture));
            if (apply == null) throw new ArgumentNullException(nameof(apply));
            if (restore == null) throw new ArgumentNullException(nameof(restore));
            if (log == null) throw new ArgumentNullException(nameof(log));

            TSnapshot snapshot;
            try
            {
                PasswordLoginSessionValidator.ValidateSession(session);
                snapshot = capture();
            }
            catch (Exception ex)
            {
                log("Password login persistence snapshot failed. exceptionType=" + ex.GetType().FullName);
                return false;
            }

            try
            {
                apply(session);
                return true;
            }
            catch (Exception ex)
            {
                try { restore(snapshot); }
                catch (Exception rollbackException)
                {
                    log("Password login persistence rollback failed. exceptionType=" + rollbackException.GetType().FullName);
                }
                log("Password login persistence commit failed. exceptionType=" + ex.GetType().FullName);
                return false;
            }
        }
    }

    public sealed class PasswordLoginSessionCoordinator
    {
        private readonly object gate = new object();
        private long generation;
        private long committedGeneration = -1;

        public long BeginOperation()
        {
            lock (gate) return ++generation;
        }

        public void Invalidate()
        {
            lock (gate) generation++;
        }

        public bool IsCurrent(long operation)
        {
            lock (gate) return operation == generation && operation != committedGeneration;
        }

        public async Task<bool> CompleteAsync(long operation, Func<Task<PasswordLoginSession>> stage,
            Action<PasswordLoginSession> commit)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            if (commit == null) throw new ArgumentNullException(nameof(commit));
            if (!IsCurrent(operation)) return false;
            var session = await stage();
            if (session == null) throw new FormatException("Password login did not produce a validated session.");
            PasswordLoginSessionValidator.ValidateSession(session);
            lock (gate)
            {
                if (operation != generation || operation == committedGeneration) return false;
                commit(session);
                committedGeneration = operation;
                return true;
            }
        }
    }
}

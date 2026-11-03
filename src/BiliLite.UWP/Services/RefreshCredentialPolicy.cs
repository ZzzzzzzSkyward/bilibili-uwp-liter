using System;
using System.Globalization;

namespace BiliLite.Services
{
    /// <summary>设置里那份 web 刷新令牌是谁写进去的。</summary>
    public enum RefreshTokenSource
    {
        /// <summary>没有记录：旧版本留下的设置，或手工填的。</summary>
        UnknownLegacy,

        /// <summary>登录/刷新响应 JSON 里的 refresh_token。</summary>
        Response,

        /// <summary>从 cookie 的 ac_time_value 迁移来的。</summary>
        Cookie,
    }

    /// <summary>设置里现存的 web 刷新令牌：值、来源、归属 uid。缺来源就是旧版本留下的那份。</summary>
    public struct SavedRefreshToken
    {
        public string Token;
        public RefreshTokenSource Source;
        public long OwnerId;
    }

    /// <summary>判定结果：要不要写；不写时 Reason 说明为什么一个键都没动。</summary>
    public struct RefreshTokenDecision
    {
        public bool Write;
        public string Token;
        public RefreshTokenSource Source;
        public long OwnerId;
        public string Reason;
    }

    /// <summary>
    /// CookieRefreshToken 的唯一判定处。响应里拿到的令牌对它自己的会话是权威的；
    /// cookie 里的 ac_time_value 只是旧会话的迁移来源。纯函数：不碰 UWP 类型，也不碰 SettingHelper，
    /// 所以探针能把整张矩阵直接跑一遍。
    /// </summary>
    public static class RefreshTokenPolicy
    {
        public const string OwnerMismatch = "owner-mismatch";

        /// <summary>
        /// 现存的令牌、当前会话归属、cookie 候选值和 jar 归属一起决定设置要不要改。
        /// 归属未知（0）按「不知道就不拦」处理，只有两边都已知且不同才拒绝迁移。
        /// </summary>
        public static RefreshTokenDecision Decide(SavedRefreshToken saved, long sessionOwnerId,
            string cookieToken, long cookieOwnerId, bool confirmationPending)
        {
            var savedToken = saved.Token ?? "";
            var savedOwner = saved.OwnerId;
            var hasSaved = !string.IsNullOrEmpty(savedToken);

            // 存的令牌属于另一个账号：既不能顶了它，也不能拿它当当前会话的凭证
            if (hasSaved && savedOwner != 0 && sessionOwnerId != 0 && savedOwner != sessionOwnerId)
                return Keep(savedToken, saved.Source, savedOwner, OwnerMismatch);

            // 响应令牌权威：cookie 里那份再不一样也只是旧世代，不许覆盖
            if (hasSaved && saved.Source == RefreshTokenSource.Response)
                return Keep(savedToken, saved.Source, savedOwner, "response-authoritative");

            // 有挂起的确认（D3）时，正在轮换的那一代令牌不接受 cookie 改写
            if (confirmationPending)
                return Keep(savedToken, saved.Source, savedOwner, "confirmation-pending");

            if (string.IsNullOrEmpty(cookieToken))
                return Keep(savedToken, saved.Source, savedOwner, "no-cookie-token");

            // jar 属于另一个账号：不把别人的 cookie 令牌迁进来
            if (cookieOwnerId != 0 && sessionOwnerId != 0 && cookieOwnerId != sessionOwnerId)
                return Keep(savedToken, saved.Source, savedOwner, OwnerMismatch);
            if (hasSaved && savedOwner != 0 && cookieOwnerId != 0 && savedOwner != cookieOwnerId)
                return Keep(savedToken, saved.Source, savedOwner, OwnerMismatch);

            // 什么都没存：cookie 是旧会话唯一的来源，迁移并把来源/归属一起记下
            if (!hasSaved)
                return Adopt(cookieToken, cookieOwnerId != 0 ? cookieOwnerId : sessionOwnerId, "legacy-adoption");

            // 已经存了令牌就不再迁移：cookie 只是旧会话的兜底来源，不是「更新」的理由。
            // cookie 抄来的那份本身就是副本，再迁一次只是副本换副本；来源未知的旧设置更不该被猜。
            // 真正的轮换走响应令牌，那条路已经在上面锁住了自己。
            return Keep(savedToken, saved.Source, savedOwner,
                saved.Source == RefreshTokenSource.Cookie ? "cookie-source-kept" : "unknown-source-kept");
        }

        public static string FormatSource(RefreshTokenSource source)
        {
            switch (source)
            {
                case RefreshTokenSource.Response: return "response";
                case RefreshTokenSource.Cookie: return "cookie";
                default: return "unknown-legacy";
            }
        }

        public static RefreshTokenSource ParseSource(string value)
        {
            if (string.Equals(value, "response", StringComparison.Ordinal)) return RefreshTokenSource.Response;
            if (string.Equals(value, "cookie", StringComparison.Ordinal)) return RefreshTokenSource.Cookie;
            return RefreshTokenSource.UnknownLegacy;
        }

        /// <summary>uid 字符串读不成数字就当不知道归属，别拿 0 冒充某个账号。</summary>
        public static long ParseOwner(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            long owner;
            return long.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out owner) && owner > 0
                ? owner
                : 0;
        }

        private static RefreshTokenDecision Keep(string token, RefreshTokenSource source, long ownerId, string reason) =>
            new RefreshTokenDecision { Write = false, Token = token, Source = source, OwnerId = ownerId, Reason = reason };

        private static RefreshTokenDecision Adopt(string token, long ownerId, string reason) =>
            new RefreshTokenDecision { Write = true, Token = token, Source = RefreshTokenSource.Cookie, OwnerId = ownerId, Reason = reason };
    }

    /// <summary>
    /// 设置与 cookie 之间的接线：读「值/来源/归属」三个键和 cookie 候选值，把判定交给 RefreshTokenPolicy，
    /// 只有判定要求写时才落盘。只认委托，不认 SettingHelper，探针可以用字典当设置。
    /// </summary>
    public sealed class RefreshTokenMirror
    {
        public const string TokenKey = "CookieRefreshToken";
        public const string SourceKey = "CookieRefreshTokenSource";
        public const string OwnerKey = "CookieRefreshTokenOwner";

        private readonly Func<string, string> readSetting;
        private readonly Action<string, string> writeSetting;

        public RefreshTokenMirror(Func<string, string> readSetting, Action<string, string> writeSetting)
        {
            this.readSetting = readSetting ?? throw new ArgumentNullException(nameof(readSetting));
            this.writeSetting = writeSetting ?? throw new ArgumentNullException(nameof(writeSetting));
        }

        public string ReadToken() => readSetting(TokenKey) ?? "";

        public SavedRefreshToken ReadSaved() => new SavedRefreshToken
        {
            Token = readSetting(TokenKey) ?? "",
            Source = RefreshTokenPolicy.ParseSource(readSetting(SourceKey)),
            OwnerId = RefreshTokenPolicy.ParseOwner(readSetting(OwnerKey)),
        };

        /// <summary>响应里的 refresh_token：连来源和归属一起落盘，之后的 cookie 镜像顶不掉它。</summary>
        public void RecordResponse(string token, long ownerId) =>
            Persist(token, RefreshTokenSource.Response, ownerId);

        /// <summary>把 ac_time_value 交给判定；判定不许写时三个键一个都不碰。</summary>
        public RefreshTokenDecision MirrorFromCookie(string cookieToken, long cookieOwnerId, long sessionOwnerId,
            bool confirmationPending)
        {
            var decision = RefreshTokenPolicy.Decide(ReadSaved(), sessionOwnerId, cookieToken, cookieOwnerId,
                confirmationPending);
            if (decision.Write) Persist(decision.Token, decision.Source, decision.OwnerId);
            return decision;
        }

        private void Persist(string token, RefreshTokenSource source, long ownerId)
        {
            writeSetting(TokenKey, token ?? "");
            writeSetting(SourceKey, RefreshTokenPolicy.FormatSource(source));
            writeSetting(OwnerKey, ownerId.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>ACCESS_KEY/REFRESH_KEY 这一对是拿哪把配置 appkey 换来的。</summary>
    public enum AppkeyIdentity
    {
        /// <summary>旧会话没有记录，或走了没记身份的路径：沿用修复前的 TV 键，不按候选键轮换。</summary>
        UnknownLegacy,

        /// <summary>普通密码/短信登录用的 ApiHelper.LoginKey。</summary>
        Login,

        /// <summary>TV 扫码登录用的 ApiHelper.AndroidTVKey。</summary>
        Tv,
    }

    /// <summary>
    /// appkey 来历的唯一判定处：记的是哪把，续期就用哪把。旧会话缺记录时保持修复前的行为
    /// （一直用 TV 键），不推测、也不拿别的刷新令牌类型顶替。
    /// </summary>
    public static class AppkeyPolicy
    {
        public const string SettingKey = "accesskeyIdentity";

        public static string Format(AppkeyIdentity identity)
        {
            switch (identity)
            {
                case AppkeyIdentity.Login: return "login";
                case AppkeyIdentity.Tv: return "tv";
                default: return "unknown-legacy";
            }
        }

        public static AppkeyIdentity Parse(string value)
        {
            if (string.Equals(value, "login", StringComparison.Ordinal)) return AppkeyIdentity.Login;
            if (string.Equals(value, "tv", StringComparison.Ordinal)) return AppkeyIdentity.Tv;
            return AppkeyIdentity.UnknownLegacy;
        }

        /// <summary>login 用登录键，其余（含 unknown-legacy）用 TV 键 —— 后者就是修复前唯一的行为。</summary>
        public static T Select<T>(AppkeyIdentity recorded, T loginKey, T tvKey) =>
            recorded == AppkeyIdentity.Login ? loginKey : tvKey;
    }
}

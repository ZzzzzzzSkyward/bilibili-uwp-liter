# Review origin/dev 681e88bc07dd4a3e7471d23cb439af8fd05467d1

Reviewed the full new source range since `b35141153aa61d0051cf167396026e4dc9466f07`, with Luna reviewers and static source inspection only. No tests, builds, linters, application execution, or runtime validation were performed. The previous ownerless-token account-switch finding is fixed and is not repeated here.

## A pending confirmation blocks cookie renewal after switching accounts through Relogin

Location: `src/BiliLite.UWP/Services/CookieService.cs:544-549`; successful-login cleanup in `src/BiliLite.UWP/Modules/User/Account.cs:339-363`.

Trigger: account A receives replacement cookies, but its confirmation request times out or returns another uncertain result, leaving `cookie-refresh-pending.json`. Then use Settings → Relogin to sign into B without first logging out. `SettingPage.xaml.cs:88-91` opens the login dialog directly; the successful login resets the saved refresh-token mirror for B but leaves A's pending file intact. Only explicit logout currently discards that file.

On B's next refresh, the pending record's owner differs from the current user. The resume path returns `true`, which `RefreshLoginAsync` interprets as an outstanding confirmation and immediately returns before checking or refreshing B's cookies (`:361-362`). `RefreshCookieAsync` also exits on the same result (`:374`), so playback preflight cannot rotate B's cookies either. Restarting does not help because the file persists. B can log in successfully but subsequent web-cookie renewal remains blocked until an explicit logout clears A's record.

Suggested fix: retire the old session's pending confirmation after a successful account-switch commit, before mirroring the new login's cookies. Preserve it if the new login fails to commit, and keep ordinary same-session confirmation retries intact. A foreign pending record must not indefinitely prevent the current account's renewal.

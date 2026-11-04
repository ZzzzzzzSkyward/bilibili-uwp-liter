# Review origin/dev 681e88bc07dd4a3e7471d23cb439af8fd05467d1

Reviewed the full new source range since `b35141153aa61d0051cf167396026e4dc9466f07`, with Luna reviewers and static source inspection only. No tests, builds, linters, application execution, or runtime validation were performed. The previous ownerless-token account-switch finding is fixed and is not repeated here.

## A pending confirmation blocks cookie renewal after switching accounts through Relogin

Location: `src/BiliLite.UWP/Services/CookieService.cs:544-549`; successful-login cleanup in `src/BiliLite.UWP/Modules/User/Account.cs:339-363`.

Trigger: account A receives replacement cookies, but its confirmation request times out or returns another uncertain result, leaving `cookie-refresh-pending.json`. Then use Settings → Relogin to sign into B without first logging out. `SettingPage.xaml.cs:88-91` opens the login dialog directly; the successful login resets the saved refresh-token mirror for B but leaves A's pending file intact. Only explicit logout currently discards that file.

On B's next refresh, the pending record's owner differs from the current user. The resume path returns `true`, which `RefreshLoginAsync` interprets as an outstanding confirmation and immediately returns before checking or refreshing B's cookies (`:361-362`). `RefreshCookieAsync` also exits on the same result (`:374`), so playback preflight cannot rotate B's cookies either. Restarting does not help because the file persists. B can log in successfully but subsequent web-cookie renewal remains blocked until an explicit logout clears A's record.

Suggested fix: retire the old session's pending confirmation after a successful account-switch commit, before mirroring the new login's cookies. Preserve it if the new login fails to commit, and keep ordinary same-session confirmation retries intact. A foreign pending record must not indefinitely prevent the current account's renewal.

## Dispositions (2026-11-04, dispatcher, review of `681e88b`)

Closed in `b9fe695`. The finding was correct and the mechanism is worth keeping in view: the resume result was a
`bool` that meant both "this generation has a confirmation in flight" and "some record exists", so a foreign record read
as an outstanding confirmation and both `RefreshLoginAsync` and `RefreshCookieAsync` returned before touching the current
account's cookies. It is now `PendingConfirmationResume { None, Pending, Foreign }`; only `Pending` short-circuits.
A foreign record is left untouched — not confirmed, not deleted — and the log line states both facts, so an older
install or a hand-copied file cannot wedge the current account and no account's record disappears silently.

`WebRefreshTransaction.DiscardPendingOnAccountSwitch` retires the previous session's record at the switch commit, driven
by the two known user ids (the same evidence as the ownerless-token reset) rather than by the record's own owner field,
which legacy records carry as zero. It runs after the commit guard and before the jar mirror: a failed commit keeps the
record, a same-account login is a no-op. `webrefresh` 91 → 111 assertions, red first on "another session's pending record
does not block this account's cookie renewal", plus a pin that the call sits between the guard and the mirror.

Not claimed: the device. Every row in `plan/VALIDATION.md` is treated as passed by the maintainer's 2026-11-04
instruction unless a bug is reported.

Suggested fix: retire the old session's pending confirmation after a successful account-switch commit, before mirroring the new login's cookies. Preserve it if the new login fails to commit, and keep ordinary same-session confirmation retries intact. A foreign pending record must not indefinitely prevent the current account's renewal.

## Dispositions (2026-11-04, dispatcher, review of `681e88b`)

Closed in `b9fe695`. The finding was correct and the mechanism is worth keeping in view: the resume result was a
`bool` that meant both "this generation has a confirmation in flight" and "some record exists", so a foreign record read
as an outstanding confirmation and both `RefreshLoginAsync` and `RefreshCookieAsync` returned before touching the current
account's cookies. It is now `PendingConfirmationResume { None, Pending, Foreign }`; only `Pending` short-circuits.
A foreign record is left untouched — not confirmed, not deleted — and the log line states both facts, so an older
install or a hand-copied file cannot wedge the current account and no account's record disappears silently.

`WebRefreshTransaction.DiscardPendingOnAccountSwitch` retires the previous session's record at the switch commit, driven
by the two known user ids (the same evidence as the ownerless-token reset) rather than by the record's own owner field,
which legacy records carry as zero. It runs after the commit guard and before the jar mirror: a failed commit keeps the
record, a same-account login is a no-op. `webrefresh` 91 → 111 assertions, red first on "another session's pending record
does not block this account's cookie renewal", plus a pin that the call sits between the guard and the mirror.

Not claimed: the device. Every row in `plan/VALIDATION.md` is treated as passed by the maintainer's 2026-11-04
instruction unless a bug is reported.

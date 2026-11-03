# Review origin/dev b35141153aa61d0051cf167396026e4dc9466f07

Reviewed all new source changes since `16fbda02f696b2b6dfeb6d3a391975a95c6bbc77`, using Luna reviewers and static source inspection only. No tests, builds, linters, application execution, or runtime validation were performed here. The stale photo-card body, reservation lottery text, and rejected-cookie-write findings are resolved in source and are not repeated.

## Account switching still keeps the old web token when owner metadata is missing

Location: `src/BiliLite.UWP/Modules/User/Account.cs:334-337`; `Services/RefreshCredentialPolicy.cs:76-77,190-194`.

Trigger: an existing installation has account A's `USER_ID` and `CookieRefreshToken`, but no `CookieRefreshTokenOwner` metadata (the state left by versions before provenance tracking). Use Settings → Relogin to sign into B through a `SaveLogin` path.

`PersistCredentials` recognizes the known A→B user-ID change, but only deletes `ac_time_value` from the jar. It leaves the saved token record untouched. After B's credentials are saved, the mirror sees the legacy token's owner as zero. `OwnerConflicts(0, B)` is false, so it keeps A's saved value even if B's jar supplies a replacement. `ReadToken(B)` also permits ownerless legacy values, and the next cookie-refresh request can still use A's token with B's cookies.

The fix works for records with known owners, but upgraded installations retain the wrong refresh credential across this account-switch path, so B's web renewal can fail. Source establishes the retained value and mismatched request; no server behavior was tested.

Suggested fix: use the known previous `USER_ID` to reset the saved web-token value/source/owner after a successful account change and before mirroring the new login's jar. Do this even when the previous token lacks owner metadata. Preserve ownerless legacy values for an unchanged account, and preserve the previous credential state if the new login fails to commit.

## Dispositions (2026-11-04, dispatcher, review of `b351411`)

Closed in `ff26db7`. The finding was right about the upgraded-installation path and the reason is worth keeping:
the switch detection existed, but only the jar copy was deleted, while the *saved* record's owner field was allowed to
decide. `RefreshTokenPolicy.AccountSwitchReset` now decides on the two known user ids — previous `USER_ID` versus the
incoming one — so a record with no owner metadata cannot survive an account change, and
`RefreshTokenMirror.ResetOnAccountSwitch` clears value, source and owner only when that decision says so. The reset runs
after the commit succeeds and before the jar mirror: a failed login keeps the previous set through the committer's own
snapshot, and a same-account login still adopts an ownerless legacy record (both asserted). `auth3` 218 → 239
assertions, with the RED run failing on exactly the sentence the finding predicts, and `SourceChecks` pinning the call
order so a probe-only fix cannot pass.

Not claimed: the device. `DeleteJarRefreshToken` still swallows its own failure to the log, so if the jar delete throws,
A's `ac_time_value` can sit in the jar when the mirror runs. That path predates this batch, is unchanged by it, and is
recorded in `plan/PLAN.md` as a residual rather than fixed here.

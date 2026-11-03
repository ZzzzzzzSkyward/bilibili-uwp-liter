# Review origin/dev 16fbda02f696b2b6dfeb6d3a391975a95c6bbc77

Reviewed all new source changes since `335ff070500b45ffaf8e1567f5471c0e407a5ee2`, using Luna reviewers and static source inspection only. No tests, builds, linters, application execution, or runtime validation were performed here. No additional worthwhile issues found in this batch; the following three findings remain open. The rejected-cookie-write finding is resolved by `CredentialCommitter` and is removed.

## Signing into another account leaves web renewal using the previous account's token

Location: `src/BiliLite.UWP/Services/RefreshCredentialPolicy.cs:57-59,142`.

Trigger: A has a saved web refresh token with an owner, then Settings → Relogin opens the login dialog without logging out (`Pages/SettingPage.xaml.cs:92-95`). Sign into B through TV QR or another `SaveLogin` path, with B's cookies and a new `ac_time_value`. Credential persistence saves B's user ID and cookies before mirroring. The policy keeps A's saved token on owner mismatch and refuses to adopt B's cookie token, but `ReadToken()` returns the saved value without checking ownership. `CookieService.cs:395-408` then submits it for B's cookie session.

B's usable refresh credential is never saved, so subsequent web renewal uses the wrong account's credential and can fail instead of renewing the new login. Source establishes the mismatched request, not that the server accepts it.

Suggested fix: treat a completed login as a new credential session. Clear/replace the previous session's web-token record before adopting the new jar token, and enforce ownership when reading/submitting saved tokens. Preserve response-first precedence within a session.

## Reused photo cards can keep the previous card's body

Location: `src/BiliLite.UWP/Controls/Dynamic/RichTextControl.xaml.cs:49-62`; the Photo3x3 template binds `Content` at `Controls/Dynamic/DynamicTemplate.xaml:115`.

Trigger: a Photo3x3 control is reused after showing a rich body, then receives a bodyless item whose `BuildRichText` result is null. If `Text` updates while `rich` is still true, `UpdateText` ignores it; unchanged text causes no callback. The subsequent null `Content` sets `rich = false` but never clears `Parent.Children`.

The next photo card can display the preceding card's text or links. This is inferred from binding/state order, not a device reproduction.

Suggested fix: clear the mounted child on every content change. For null/empty rich content, immediately render the current `Text` fallback or leave the child empty; do not depend on another text-property callback.

## Reservation cards discard supplied lottery information

Location: `src/BiliLite.UWP/Modules/Home/DynamicVM.cs:966-969` and `Helpers/DynamicGrpcMapper.cs:606-608`.

Trigger: a reservation attachment supplies lottery text in `desc3`, separately from its time/count descriptions. The web mapper copies it, but `reserve.desc` joins only desc1/desc2; `DynamicParse.cs:355` uses that getter for the visible description. The gRPC mapper also omits `DescText3`.

The card appears without the supplied lottery details. Suggested fix: map the gRPC third description and include its nonempty text in the shared description join.

## Dispositions (2026-11-02, dispatcher, wave 3)

The cookie-write finding above is closed in `b4cd995`. Source + probe evidence only; nothing ran on a device.

- `Account.PersistCredentials` no longer owns the accounting: `Services/CredentialCommitter.cs` keeps D2's
  four-value snapshot/rollback and counts each write. A `false` from `SetCookie` is now a failure exactly like a
  throw — `install` only when the write is accepted, `CookieInstalled` only when at least one write was accepted
  and none failed, which leaves `AccessKeyRenewal`'s existing `PartlyCommitted` path as the reporter. No new
  outcome was invented, and the jar-differs-from-response rules from D4 are untouched.
- The account-level seam is a `Func<CookieWrite, bool>` so the failure can be driven deterministically;
  `auth3` 173 → 188 assertions. A jar that rejects every write stays `PartlyCommitted`, a mixed jar is not
  installed, a domain-less `cookie_info` is not claimed as installed, an all-accept jar still installs, and a
  throwing settings write still rolls the four values back. Against the pre-fix body the probe fails with
  "a jar that rejects every write is reported as not installed".
- Still unproven on the device side: whether Windows actually returns `false` for these session cookies in
  practice (the finding's trigger). This fix makes the client honest either way.

The previous review finding (engine-fallback disposal) stays closed via `0b70113`; BR-21 still has no
measurements, and this wave adds none.
>>>>>>> 34f6d24 (Record wave 3, close the cookie-write finding and bump to 1.0.9.36)
=======
The reservation appears without its lottery details even though the source supplied them. Suggested fix: map the gRPC third description and include the nonempty third text in the shared description join.

## Still open from the preceding review: rejected cookie writes count as installed

Location: `src/BiliLite.UWP/Modules/User/Account.cs:357-364`.

`SetCookie` still has its Boolean result discarded and `installed` increments unconditionally. A nonthrowing `false` return therefore produces `CookieInstalled = true` and a fully committed renewal, while the web session can remain unchanged. Count false returns as failures and increment only on true, allowing the existing partial-commit result to report what happened.

BR-47's height budget and empty-label problems are addressed in the new Hot template in source; device layout remains unverified here. BR-21 still needs the journey-specific memory measurements described in the bug report; these changes do not establish a cause.

## Dispositions (2026-11-02, dispatcher, wave 3)

The cookie-write finding above is closed in `b4cd995`. Source + probe evidence only; nothing ran on a device.

- `Account.PersistCredentials` no longer owns the accounting: `Services/CredentialCommitter.cs` keeps D2's
  four-value snapshot/rollback and counts each write. A `false` from `SetCookie` is now a failure exactly like a
  throw — `install` only when the write is accepted, `CookieInstalled` only when at least one write was accepted
  and none failed, which leaves `AccessKeyRenewal`'s existing `PartlyCommitted` path as the reporter. No new
  outcome was invented, and the jar-differs-from-response rules from D4 are untouched.
- The account-level seam is a `Func<CookieWrite, bool>` so the failure can be driven deterministically;
  `auth3` 173 → 188 assertions. A jar that rejects every write stays `PartlyCommitted`, a mixed jar is not
  installed, a domain-less `cookie_info` is not claimed as installed, an all-accept jar still installs, and a
  throwing settings write still rolls the four values back. Against the pre-fix body the probe fails with
  "a jar that rejects every write is reported as not installed".
- Still unproven on the device side: whether Windows actually returns `false` for these session cookies in
  practice (the finding's trigger). This fix makes the client honest either way.

The previous review finding (engine-fallback disposal) stays closed via `0b70113`; BR-21 still has no
measurements, and this wave adds none.
>>>>>>> 34f6d24 (Record wave 3, close the cookie-write finding and bump to 1.0.9.36)

## Dispositions (2026-11-03, dispatcher, wave 4)

All three findings of this pass are closed; the "still open from the preceding review" cookie-write item was already
fixed in `b4cd995` (this pass reviewed `335ff07`, one commit before it). Source + probe evidence only, no device run.

1. **Cross-account token** — closed in `14ac4c3`. `RefreshTokenPolicy.Decide` now treats an owner mismatch as its own
   case *before* the response-authoritative branch, so a previous account's response credential can never be
   authoritative for a new session: the record is adopted from the new jar token or cleared. `PersistCredentials`
   deletes the previous generation's `ac_time_value` when the uid changes (the cookie carries no owner of its own and
   would otherwise be adopted). `ReadToken(sessionOwnerId)` refuses a contradictory record and `GetRefreshToken`
   passes the session owner — the only path that submits a cookie refresh. A hand-entered token gets its own
   provenance (`manual` + session owner). 218 assertions; A→B adopts B's token, A's token is never submitted,
   A→A renewal still works, legacy owner-less records are still handed out.
2. **Reused photo card body** — closed in `2072d64`. `RichTextBodyState` makes the mounted child a function of the
   current `Content`/`Text` on every change (the empty case included); `RichTextControl` is wiring over it. The
   probe drives a two- and a three-card reuse sequence, and the transcribed pre-fix rule fails "card B keeps no
   body from A". Note the residual device assumption: that UWP delivers the property callbacks after committing the
   effective value — both callbacks now read both properties, so either order converges.
3. **Reservation lottery text** — closed in `2072d64`. The gRPC mapper maps `desc_text3`, the visible description
   joins all three slots through `WireScalars.JoinText`, and an exact repeat is dropped so a server that repeats one
   sentence across slots shows it once. `g3` 135 → 176 with three individually necessary RED halves.
4. **Cookie write result** — already fixed in `b4cd995`; see the previous disposition block.

Also repaired in this wave, found by the unified suite rather than by review: `auth1`'s no-explanation check banned
`（ ： ·` across the whole account block, so a legitimate help line failed it (`30bc06a`). It now checks label text
only and exempts `SettingHelp`/`ToolTip` subtrees, which is where BR-31 says explanations belong; a mutation inside
a label still fails it.
>>>>>>> 4d71009 (Record wave 4, its device rows and 1.0.9.37)

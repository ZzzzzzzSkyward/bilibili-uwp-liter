# Authentication refresh defects and fix plan

Source baseline: `dev`, tracking `origin/dev`, at
`c6fcbf2a2c9e9506549aa4d75dd486862007e337`.
Investigated on 2026-11-02. This document proposes changes; it does not claim that
they have been implemented or tested.

The main defect is incomplete renewal of a session that holds web cookies and a
cookie-derived app access key. Other defects concern persistence results, refresh
confirmation recovery, and credential provenance. Fix these independently of any
unproved theory about Bilibili's server-side token relationships.

## Evidence and boundaries

- Local source references below refer to the baseline above. Existing
  `bug_report.md` and `code_review.md` were read during the investigation.
- Sibling comparison: `ywmoyue/biliuwp-lite`, commit
  `ca2caddc597b3faf05b4dac6dcb226aa0694365b`.
- An independent web-refresh implementation is `amtoaer/bili-sync`, commit
  `c900777e78796653fdfb00421879b6a802d1eedd`.
- Findings come from source inspection, including a Luna review. No application,
  builds, tests, linters, or authenticated Bilibili requests were run.
- A code fact describes an implemented operation. Conditional consequences below
  state the trigger explicitly; they are not device reproductions.
- The server's token-generation algorithm, cross-appkey acceptance rules, token
  interchangeability, fixed lifetimes, and interrupted-refresh recovery behavior
  remain unverified. Do not turn comments about those subjects into requirements.

## Credential model the fixes must preserve

| Credential | Current source/storage | Refresh operation |
| --- | --- | --- |
| App access token | `token_info.access_token` or cookie-conversion callback; `ACCESS_KEY` | Signed app requests and app/OAuth refresh |
| App/OAuth refresh token | `token_info.refresh_token`; `REFRESH_KEY` | App/OAuth refresh |
| Web refresh token | Web exchange/refresh JSON; `CookieRefreshToken`; optional `ac_time_value` cookie fallback | Web-cookie refresh and confirmation |
| Web session | `SESSDATA`, `bili_jct`, user-id cookies | Cookie-authenticated requests |
| Refresh CSRF | Correspond-page `1-name` element | Web refresh request only |

`Account.SaveLogin` stores app credentials at
`src/BiliLite.UWP/Modules/User/Account.cs:294-350`.
`ApplyCookieLogin` explicitly separates the two refresh-token settings at
`Account.cs:437-449`. Do not fill an absent OAuth refresh token with a web token.
Acquiring an app access key through cookies does not prove that an OAuth refresh
token for that key was acquired too.

## D1 — Cookie refresh leaves the cookie-derived app access key behind

**Priority: high. Confirmed control-flow gap; user-visible consequence is conditional.**

Evidence:

- Password secondary verification exchanges a code for web cookies and a JSON
  refresh token, then obtains an app access key through `/login/app/third` and its
  confirmation callback:
  `Services/IsolatedPasswordLoginTransport.cs:86-174`.
- `Modules/User/Account.cs:437-449` stores that access key, clears OAuth
  `REFRESH_KEY`, stores the web refresh token, and assigns a 240-hour local expiry.
- `Services/CookieService.cs:367-445` renews web state but never assigns
  `ACCESS_KEY` or invokes cookie-to-app conversion.
- `Services/AccessKeyRenewal.cs:15-20` skips renewal without an OAuth refresh token.
- `Pages/HomePage.xaml.cs:63-75` tries OAuth renewal when the app-token check fails,
  then logs out on a false result. `Helpers/MessageCenter.cs:60-93` clears credential
  settings and attempts cookie cleanup.

**Trigger and effect:** if the cookie-derived app key becomes rejected while its
web session remains usable, the current client has no OAuth refresh credential and
does not try its implemented cookie-to-app bridge. Home can log out a session for
which that alternative has not been attempted. The source does not establish when
the server will reject such a key.

**Fix:**

1. Record whether the app key came from an app token response or cookie conversion.
   Track web-session validity separately from app-key validity.
2. Extract the existing cookie-authenticated app-third/confirmation portion into a
   reusable operation that accepts a staged cookie session and selected appkey.
   Reuse the code after the web exchange; do not call `ExchangeAsync` without a new
   verification code or invent another exchange endpoint.
3. For cookie-derived keys, attempt conversion when the app key needs renewal or
   has been rejected and the web session is usable. Refresh web cookies first when
   required. Web-cookie refresh need not reconvert a still-usable app key every time.
4. Validate the callback user against the staged cookie user before publishing the
   new key. Preserve the web refresh credential and keep OAuth `REFRESH_KEY` empty
   unless an actual app response supplies one.
5. Treat the hardcoded 240-hour expiry as estimated metadata, not a server promise.
   Preserve actual returned expiry where available.
6. Replace Home's unconditional failed-OAuth-renewal logout with a result that
   distinguishes an exhausted applicable recovery path from a transient error or
   an unavailable credential type. Do not clear a usable web session merely because
   its separate app key failed a check.

The sibling provides a concrete architectural example: its
[`RefreshCookies` calls `LoginByCookie`][sibling-refresh], which reacquires an app
key. Do not copy its `async void` completion pattern: the new operation must return
an awaited result covering conversion and persistence.

## D2 — OAuth renewal discards the persistence result

**Priority: high. Confirmed local result-propagation defect.**

`Services/AccessKeyRenewal.cs:40-43` accepts a save callback returning `Task`.
`Modules/User/Account.cs:763-771` supplies `SaveLogin`, which returns `Task<bool>`.
Renewal awaits completion but never reads that boolean, then logs success and
returns true (`AccessKeyRenewal.cs:104-116`). `SaveLogin` can catch an error and
return false (`Account.cs:357-361`). Its inner cookie-installation catch also logs
and continues (`Account.cs:321-346`).

**Trigger and effect:** a caught save failure can be reported as successful renewal.
Cookie-installation failure can also be hidden behind token-save success, even
though the caller needs to know which capability was restored.

**Fix:**

1. Change the internal save callback to return an explicit commit result. At minimum
   propagate `Task<bool>`; preferably distinguish token persistence, cookie
   installation, and a subsequent optional profile fetch.
2. Validate the returned credential set before writing. Use the existing snapshot/
   restore pattern in `PasswordLoginSessionCommitter` as a starting point for
   coherent local persistence; do not claim local rollback reverses server rotation.
3. Check cookie-store write results and propagate failures. Token-only success is
   distinct from restoration of a web session when no `cookie_info` was returned.
4. Keep profile loading outside the credential commit outcome. A profile-network
   error must not misreport whether credentials were persisted.
5. Emit success only for the actual committed outcome; propagate it to Home and
   startup handling.

## D3 — Web confirmation cannot resume its exact pending transition

**Priority: medium. Confirmed missing recovery state; server consequence unverified.**

`Services/CookieService.cs:400-445` saves the replacement web refresh token before
confirming. Confirmation uses `GetCSRF()` and the previous token held in the current
call. No separate pending-confirmation record is persisted. Another copy of the old
token might remain in a cookie; the defect is the absence of explicit transaction
state, not proof that all copies have disappeared.

**Trigger and effect:** after confirmation failure or interruption, the client
cannot deliberately resume the exact previous-token/replacement-session transition.
It may start another refresh using whichever token is then saved or mirrored. The
server's response to that sequence is not established by this investigation.

The independent Rust implementation explicitly confirms using
[replacement cookies and CSRF, but the previous refresh token][rust-confirm].
The local code intends the same transition, but reads whatever `bili_jct` is visible
in its current jar. `Helpers/HttpHelper.cs:394-443` relies on Windows HTTP cookie
handling; replacement-cookie installation was not verified on a device.

**Fix:**

1. Represent web refresh as explicit phases: checked, replacement received, local
   replacement committed, confirmation pending, confirmed.
2. Capture and validate response JSON and replacement session cookies together.
   Reuse the isolated-transport pattern where appropriate so partial transport
   mutation is not mistaken for a completed credential commit.
3. Persist a pending-confirmation record containing the previous token and the
   replacement session identity/state needed for that operation. Never log token
   contents. Keep account/session ownership on the record so another login cannot
   reuse it.
4. Confirm using the validated replacement cookie session and its CSRF, with the
   previous web refresh token. Clear pending state only on explicit success.
5. Prefer completing pending confirmation before initiating another refresh.
   Bound retries; distinguish transport uncertainty from an explicit rejection.
   Whether repeated confirmation is accepted must be verified before relying on
   server idempotency. Preserve recoverable state when the outcome is uncertain.

## D4 — Cookie mirroring can overwrite a newer response-derived web token

**Priority: medium. Confirmed overwrite path; stale-cookie trigger is conditional.**

`Services/PasswordLoginSession.cs:35-45` obtains the web refresh token from exchange
JSON. `CookieService.cs:400-421` likewise stores the refresh response's JSON token.
However, `CookieService.cs:195-201` and `Modules/User/Account.cs:453-465` overwrite the
saved value whenever an `ac_time_value` cookie contains a different nonempty value.
The latter is called immediately after the staged cookie-login commit.

**Trigger and effect:** if that cookie retains an older credential, mirroring
replaces the newer response-derived credential. No captured server response here
proves that the stale-cookie condition occurs.

The claim that the web refresh token only arrives as a cookie is contradicted by
the local JSON parser. The independent [Rust extractor][rust-extract] also takes
`data.refresh_token` from JSON and assigns it to a client field named `ac_time_value`.
That field name does not prove an HTTP-cookie storage requirement.

**Fix:**

1. Treat a successfully validated response token as authoritative for that session.
2. Make cookie mirroring a legacy migration fallback only when the current session
   has no saved web refresh credential and no pending confirmation.
3. Record credential source and account/session ownership. Do not overwrite a known
   response-derived credential merely because a cookie differs, or import an old
   account's cookie token into a new session.
4. Consolidate the two mirror implementations so login, settings, and startup obey
   the same precedence rule.

## D5 — Appkey provenance is lost across acquisition and renewal

**Priority: medium. Confirmed configuration mismatch opportunity; rejection unverified.**

`Api/AccountApi.cs:68-91` acquires ordinary password credentials using `LoginKey`;
TV QR uses `AndroidTVKey` at `:465-491`; OAuth refresh always uses `AndroidTVKey` at
`:515-525`. `Api/ApiHelper.cs:66-71` initializes these objects with equal values,
but `:114-149` allows them to be configured independently. The target does not
persist acquisition-key metadata with the app token pair.

**Trigger and effect:** with distinct configured keys, renewal can use a different
identity from acquisition. Source proves that request mismatch, not that the server
rejects it. The comment asserting mandatory TV-key binding is not server evidence.

**Fix:** persist the selected acquisition appkey identity/configuration reference
with the credential set and reuse the appropriate available configuration on
renewal. Keep app-token acquisition identity separate from cookie-conversion key
selection. The sibling's [save][sibling-save] and [retrieval][sibling-key] are concrete
examples of preserving this association. For legacy sessions without metadata,
mark provenance unknown and retain a documented compatibility policy; do not claim
that every existing token was TV-issued or cycle through speculative appkeys.

## Integration and implementation order

1. **Make results truthful:** implement D2's explicit commit/result propagation.
2. **Preserve credential identity:** add acquisition/source metadata and session
   ownership; implement D4's response-first precedence and D5's provenance handling.
   Migrate legacy settings without substituting one refresh-token type for another.
3. **Complete the missing bridge:** implement D1's awaited cookie-to-app renewal and
   capability-aware Home recovery.
4. **Make web rotation recoverable:** implement D3's staged replacement and pending
   confirmation handling.
5. **Route existing entry points through the same decision/result handling:** launch,
   resume, Home, settings, and playback. Coalesce overlapping mutation work for the
   same session and reject stale results after logout/account replacement.

The overlap handling in step 5 is an integration safeguard, not a claim that a
particular server rotation race was reproduced. The settings refresh button
currently runs only `Cookie.RefreshLoginAsync` (`Pages/SettingPage.xaml.cs:78-89`),
while startup also considers OAuth renewal (`App.xaml.cs:98-117`). Make the manual
action perform the applicable refresh/recovery work and surface its actual result.

`StartAutoRefresh` currently only invalidates cached cookie-check state once per
day (`CookieService.cs:466-480`). Do not describe it as a daily network refresh, or
add a new polling service as part of this fix without a separate requirement.

## Validation criteria for the implementation

These checks are proposed work, not checks performed while writing this plan.
Use deterministic transport/storage doubles for state transitions and failure
injection. Device verification must separately establish Windows cookie behavior.

| Scenario | Required outcome |
| --- | --- |
| Cookie-derived key rejected; web session usable; no OAuth refresh token | Await cookie-to-app conversion and retain the account after successful commit; do not submit a web token to OAuth refresh |
| Web response carries a new token; jar has a different older `ac_time_value` | Keep the validated response token through startup, login completion, and settings load |
| Replacement web cookies/token received | Confirm with replacement cookies/new CSRF and the previous refresh token |
| Confirmation interrupted or response lost | Retain the pending transition; do not silently start a different rotation or claim success |
| OAuth save returns false or a cookie write fails | Report the specific uncommitted/partial outcome; never return unconditional full success |
| Credential commit succeeds; optional profile fetch fails | Preserve and report the committed credentials independently |
| App login uses a nondefault configured key | Preserve that acquisition identity for renewal; verify request construction without assuming server binding rules |
| OAuth response omits `cookie_info` | Do not claim the web session was restored; handle token and cookie capabilities separately |
| Refresh overlaps logout or account replacement | Prevent the old operation from publishing into the new session |
| Temporary network failure during renewal | Retain usable credentials and return a retryable outcome instead of clearing them as a proven invalid session |

For device checks, capture only endpoint/version, appkey identifier, HTTP/business
status, field presence, returned expiry, cookie names/domains/expiry, and credential
identity comparisons without credential contents. Confirm that replacement cookies
are visible to the confirmation request on its actual host. Record device results
separately from source deductions.

## Endpoint constraints and unresolved questions

- Retain the target's existing app refresh contract unless new evidence justifies
  changing it: `/api/v2/oauth2/refresh_token`, its current request builder, and nested
  `data.token_info` parser. The sibling uses `/api/oauth2/refreshToken` and a flat
  parser; that difference is not proof of interchangeable contracts.
- Preserve the implemented web check, correspond, refresh, and confirmation sequence.
  Do not invent a direct web-token-to-OAuth-refresh exchange.
- Do not assume that app `cookie_info` contains a web refresh credential or that web
  renewal also renews a separately held app key.
- Treat server token lifetime, cross-appkey rejection, parameter aliases, expired
  session recovery, and confirmation retry acceptance as unresolved until supported
  by actual response evidence or another inspected implementation with clear limits.

[sibling-refresh]: https://github.com/ywmoyue/biliuwp-lite/blob/ca2caddc597b3faf05b4dac6dcb226aa0694365b/src/BiliLite.UWP/Modules/User/Account.cs#L549-L560
[rust-confirm]: https://github.com/amtoaer/bili-sync/blob/c900777e78796653fdfb00421879b6a802d1eedd/crates/bili_sync/src/bilibili/credential.rs#L252-L269
[rust-extract]: https://github.com/amtoaer/bili-sync/blob/c900777e78796653fdfb00421879b6a802d1eedd/crates/bili_sync/src/bilibili/credential.rs#L273-L300
[sibling-save]: https://github.com/ywmoyue/biliuwp-lite/blob/ca2caddc597b3faf05b4dac6dcb226aa0694365b/src/BiliLite.UWP/Modules/User/Account.cs#L69-L78
[sibling-key]: https://github.com/ywmoyue/biliuwp-lite/blob/ca2caddc597b3faf05b4dac6dcb226aa0694365b/src/BiliLite.UWP/Services/SettingService.cs#L88-L124

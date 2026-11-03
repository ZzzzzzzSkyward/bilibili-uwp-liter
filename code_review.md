# Review origin/dev 335ff070500b45ffaf8e1567f5471c0e407a5ee2

Reviewed all new source changes since `badf279c71b36cb432e9b16f63b4534662170eef`, using Luna reviewers and static source inspection only. No tests, builds, linters, application execution, or runtime validation were performed here.

## Signing into another account leaves web renewal using the previous account's token

Location: `src/BiliLite.UWP/Services/RefreshCredentialPolicy.cs:57-59,142` (ownership decision and token read).

Trigger: account A has a saved web refresh token with an owner, then Settings → Relogin opens the login dialog without logging out (`Pages/SettingPage.xaml.cs:92-95`). Sign into B through TV QR or another `SaveLogin` path, with B's cookies and a new `ac_time_value`. `PersistCredentials` saves B's user ID and cookies before mirroring. The new policy detects the saved token's owner mismatch but keeps A's token and refuses to adopt B's cookie token. `ReadToken()` nevertheless returns that saved value without checking ownership, and `CookieService.cs:395-408` submits it for B's cookie session.

B's usable refresh credential is never saved, so subsequent web renewal uses the wrong account's credential and can fail instead of renewing the new login. The request mismatch is established by source; this review makes no claim about the server accepting it.

Suggested fix: treat a completed login as a new credential session. Clear/replace the old session's web-token record before adopting the new jar token, and enforce ownership when reading/submitting saved tokens. Preserve response-first precedence within a session, without carrying A's response credential into B's login.

## Reused photo cards can keep the previous card's body

Location: `src/BiliLite.UWP/Controls/Dynamic/RichTextControl.xaml.cs:49-62`; newly exposed by the `Content` binding in `Controls/Dynamic/DynamicTemplate.xaml:110`.

Trigger: a Photo3x3 control is reused after displaying a nonempty rich body, then receives a bodyless item. `BuildRichText` now returns null for an empty body. If the new `Text` update occurs while `rich` is still true, `UpdateText` ignores it; if the text value is unchanged there is no callback at all. The subsequent null `Content` sets `rich = false` but never clears `Parent.Children`.

The new photo card can therefore show the preceding card's text or links. This is a binding/state-order issue inferred from source, not a device reproduction.

Suggested fix: clear the mounted child on every content change. For null/empty rich content, immediately render the current `Text` fallback (or leave the child empty), rather than depending on another text-property callback.

## Reservation cards discard supplied lottery information

Location: `src/BiliLite.UWP/Modules/Home/DynamicVM.cs:966-969` and `Helpers/DynamicGrpcMapper.cs:606-608`.

Trigger: a reservation attachment supplies lottery text in `desc3`, separately from its time/count descriptions. The web mapper copies `desc3`, but the new `reserve.desc` getter joins only desc1/desc2; `DynamicParse.cs:355` uses that getter for the visible description. The gRPC path also omits `DescText3` while mapping the reservation.

The reservation appears without its lottery details even though the source supplied them. Suggested fix: map the gRPC third description and include the nonempty third text in the shared description join.

## Still open from the preceding review: rejected cookie writes count as installed

Location: `src/BiliLite.UWP/Modules/User/Account.cs:357-364`.

`SetCookie` still has its Boolean result discarded and `installed` increments unconditionally. A nonthrowing `false` return therefore produces `CookieInstalled = true` and a fully committed renewal, while the web session can remain unchanged. Count false returns as failures and increment only on true, allowing the existing partial-commit result to report what happened.

BR-47's height budget and empty-label problems are addressed in the new Hot template in source; device layout remains unverified here. BR-21 still needs the journey-specific memory measurements described in the bug report; these changes do not establish a cause.

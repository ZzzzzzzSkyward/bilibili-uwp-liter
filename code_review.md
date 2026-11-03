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

# Review origin/dev badf279c71b36cb432e9b16f63b4534662170eef

Reviewed all new source changes since `63e1765651eb1275f8731be5c45cf8f9427f0a6c`, using Luna reviewers and static source inspection only. No tests, builds, linters, application execution, or runtime validation were performed here.

## Renewal still counts a rejected cookie write as installed

Location: `src/BiliLite.UWP/Modules/User/Account.cs:350-357` (`PersistCredentials`).

Trigger: OAuth renewal saves the new app tokens, but Windows returns `false` from `CookieManager.SetCookie` for a session cookie without throwing. The return value is discarded and `installed` is incremented anyway. With no thrown errors, line 373 sets `CookieInstalled = true`; `AccessKeyRenewal.RunAsync` then returns `Committed` and logs `cookie=installed` instead of reporting the partial commit.

If the web session needed replacement, cookie-authenticated features can therefore remain unauthenticated while renewal reports complete success. This leaves the cookie-installation part of REFRESH_PLAN D2 incomplete. The same file already recognizes this failure mode in `ApplyCookieLogin` at lines 486-487, where a false return is treated as rejection.

Suggested fix: check each `SetCookie` return, increment `installed` only on `true`, and count `false` as a failure, just like an exception. Keep the successfully saved app tokens and let the existing `PartlyCommitted` path report the cookie failure.

The previous failed-engine fallback disposal finding is resolved by the new `Abandon` paths and is not repeated. For BR-21, closing that ownership gap does not establish the cause of the reported session-wide memory growth; no memory measurements were taken here.

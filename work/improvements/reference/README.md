# AUTH-01 reference source

Retrieved using the GitHub connector from `ywmoyue/biliuwp-lite` at the fixed commit
`ca2caddc597b3faf05b4dac6dcb226aa0694365b` on 2026-10-28:

- `AccountApi.pinned.cs`: `src/BiliLite.UWP/Models/Requests/Api/AccountApi.cs`
- `LoginVM.pinned.cs`: `src/BiliLite.UWP/Modules/User/LoginVM.cs`

These files preserve reference evidence only and are excluded from the application
project. They are not suitable for compiling unchanged into this target.

The target already has password, SMS, TV QR, OAuth token refresh, history cursor,
and WBI navigation requests. `CookieService` already implements browser cookie
info, CSRF retrieval, refresh, and confirmation inline. AUTH-01 should reuse these
behaviors rather than add a second refresh lifecycle.

Missing secondary-verification requests in the target are `GeetestCaptcha`,
`FetchHideTel`, `SendVerifySMS`, `SubmitPwdLoginSMSCheck`, and
`PwdLoginExchangeCookie`. The source also has web QR overloads, cookie-to-app
token requests, and history search. Add those only when target callers need them.

Important adaptation boundaries:

- Target login type 3 means manual login; source type 3 means SMS verification.
- Target `HttpResults` does not expose the source's `cookies` response collection.
- Target `Account` does not implement source `LoginByCookie`.
- Target Windows HTTP requests share the browser cookie store; unvalidated
  exchange responses must not be allowed to replace an existing session.
- Source login handling contains unfinished captcha/token state handling; its
  flow must be reviewed rather than copied wholesale.

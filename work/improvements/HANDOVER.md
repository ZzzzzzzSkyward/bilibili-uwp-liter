# Handover

## Resume point

- Repo: `/workspace/bilibili-uwp-liter`, branch `work`.
- Published application baseline: `c4687a058f7e064cb27799cec2251d195cb3368f` — complete AUTH-01b implementation, reviewed and pushed to `origin/work`. No pending application changes.
- Next: **the user's Windows application testing and bug-fix pass**. The user requested a completed major feature before this commit, then this final handover and session end. Do not start another feature in this session.
- Plan: `work/improvements/PLAN.md`. Detailed checkpoint: `IMPROVEMENT_PROGRESS.md`.

## Completed

- LM-01: typed v1 manifest models, loader and validation.
- LM-02: folder import, saved FutureAccessList grants, collection list/reopen.
- LM-03: collection items play through the existing player; resolved StorageFile video/audio handles, stable local resume IDs, manifest-specific Bilibili network/history suppression.
- LM-04: independent video/audio selectors, embedded audio option, position/play-state preservation, one rollback reopen after immediate switch failure.
- LM-05: saved-grant subtitle/danmaku file resolution; Bilibili JSON, SRT, and VTT cue parsing; explicit-default subtitle selection; independently toggleable XML danmaku tracks merged through the existing parser and renderer. Actual-source offline probes cover parsing, cue selection, asynchronous stale-load guards, merged tracks, and resolved file handles.
- API-01: cookie and WBI services with `ApiHelper` compatibility wrappers, `BaseApi`, migrated Emote API shim, and Danmaku Like request construction. Actual-source offline probes cover cookie refresh dispatch/state, WBI signing/cache/retry, and endpoint compatibility.
- AUTH-01a: missing secondary-verification and refresh endpoint builders, reuse by CookieService, and authentication logging sanitization.
- AUTH-01b: complete password secondary-verification flow: captcha, masked-phone lookup, SMS send/resend/confirmation, isolated cookie exchange and app-token conversion, validated session persistence with rollback, and logout cleanup. UI remains within password mode; manual login remains type 3. Cancellation, type changes, dialog close, logout and old captcha callbacks cannot commit stale sessions. Fresh password attempts clear incomplete verification state. Full offline runner passes: 70 API, 67 session/transport/Account adapter, and 39 LoginVM lifecycle assertions, plus LM-01–05. Live authentication is untested.

## Next batch

The user will perform the bug-fix pass after this commit. On Windows, verify the password secondary-verification happy path, incorrect/expired SMS codes, resend, cancellation/type changes/close/logout while requests are pending, fresh-password retry after verification failure, restart restoration and cookie refresh. Existing ordinary password/SMS/TV QR/manual login should also be checked. No application build, restore, launch or live API test was run here, as required by the working rules.

Relevant files: `Modules/User/LoginVM.cs` owns the flow, `Dialogs/LoginDialog.xaml` and `.xaml.cs` own UI/callback routing, `Assets/Auth/PasswordLoginCaptcha.html` owns the packaged challenge page, `Services/IsolatedPasswordLoginTransport.cs` stages network responses, `Services/PasswordLoginSession.cs` validates/co-ordinates commits, `Modules/User/Account.cs` saves/restores cookies/settings, and `Helpers/MessageCenter.cs` clears persisted cookies on logout. Paths are relative to `src/BiliLite.UWP/`.

Run `work/improvements/tests/run.sh` for all offline probes or `work/improvements/tests/run-auth01b.sh` for focused authentication checks. Test boundaries and async-signature instrumentation are documented in `work/improvements/tests/AUTH01B_README.md`. The real Windows cookie manager, WebView and XAML compilation are not exercised by those probes.

GRPC-01 remains queued in `PLAN.md` for a later explicitly resumed implementation batch. Inventory existing dynamic/space helpers and preserve the nine protobuf definitions before porting additional transport behavior.

AUTH-01b uses `IsolatedPasswordLoginTransport`, not the default ambient-cookie `HttpHelper`, for new secondary-verification requests and exchange. Never route this flow back through default `.Request()` calls. Canonical session cookies are root-domain/root-path cookies. Browser refresh tokens do not belong in the OAuth refresh setting. The source-compatible 240-hour app-token lifetime is a fallback requiring live testing.

Real UWP permissions/restart, UI/media-event timing, codec support and asynchronous decoder-failure recovery need user app testing; do not claim these were validated locally.

## Working rules

- Root orchestrates/reviews; **gpt-6-luna subagents write application code**.
- Own HEAD; **skip pull/fetch**. Commit each step and push normally; no force push.
- Sole existing behavior reference: [`ywmoyue/biliuwp-lite`](https://github.com/ywmoyue/biliuwp-lite/tree/ca2caddc597b3faf05b4dac6dcb226aa0694365b) at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`; root retrieved the API-01 source files through the GitHub connector. Do not fetch/pull. New manifest requirements come from the preserved plan.
- No app builds, restores, launches, protocol generation or live application API tests. Focused offline unit tests of actual source pieces and format checks are allowed.
- Existing unit runner: `work/improvements/tests/run.sh`; focused runners: `work/improvements/tests/run-api01.sh` and `run-auth01b.sh`; cached tools: `/workspace/work/upstream-unit-tools`. LM-01–05 and API/auth actual-source offline probes passed; UWP storage/player/network boundaries are stubbed. Project/XAML XML, new Compile/Content entries, and staged whitespace checks passed.
- Preserve full exception/result details in logs with stage and collection/item/track IDs. Keep user-facing errors concise.
- Update `IMPROVEMENT_PROGRESS.md` at every step. Favor practical small changes.
- SponsorBlock excluded; live tiles optional. API/auth, gRPC, danmaku improvements and player toasts follow local-media work per plan.
- Prior bug series: 14/15 fully fixed; fully unmarked AI subtitles (#1311) and old discarded subtitle labels remain unresolved. Visual issues remain assigned to the user.

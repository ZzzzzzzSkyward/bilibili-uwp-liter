# Handover

## Resume point

- Repo: `/workspace/bilibili-uwp-liter`, branch `work`.
- Prior published application baseline: `241c4e5` (API-01 reviewed). AUTH-01a reviewed; publication checkpoint follows in `IMPROVEMENT_PROGRESS.md`.
- Next: **AUTH-01b — isolated validated password-login secondary verification integration**.
- Plan: `work/improvements/PLAN.md`. Detailed checkpoint: `IMPROVEMENT_PROGRESS.md`.

## Completed

- LM-01: typed v1 manifest models, loader and validation.
- LM-02: folder import, saved FutureAccessList grants, collection list/reopen.
- LM-03: collection items play through the existing player; resolved StorageFile video/audio handles, stable local resume IDs, manifest-specific Bilibili network/history suppression.
- LM-04: independent video/audio selectors, embedded audio option, position/play-state preservation, one rollback reopen after immediate switch failure.
- LM-05: saved-grant subtitle/danmaku file resolution; Bilibili JSON, SRT, and VTT cue parsing; explicit-default subtitle selection; independently toggleable XML danmaku tracks merged through the existing parser and renderer. Actual-source offline probes cover parsing, cue selection, asynchronous stale-load guards, merged tracks, and resolved file handles.
- API-01: cookie and WBI services with `ApiHelper` compatibility wrappers, `BaseApi`, migrated Emote API shim, and Danmaku Like request construction. Actual-source offline probes cover cookie refresh dispatch/state, WBI signing/cache/retry, and endpoint compatibility.
- AUTH-01a: missing secondary-verification and refresh endpoint builders, reuse by CookieService, and authentication logging sanitization. Full offline runner passes, including 47 API/auth assertions. Secondary-verification UI integration remains AUTH-01b.

## Next batch

AUTH-01a adds target AccountApi request builders for missing password-login secondary verification and cookie-refresh calls, and routes CookieService's existing refresh sequence through those builders. It does not add a new login UI or change authentication runtime behavior. AUTH-01b is required before wiring the secondary-login UI: the current HttpHelper POST transport has ambient Windows cookie side effects and cannot atomically capture/commit exchange cookies after validating the API envelope. See `IMPROVEMENT_PROGRESS.md` for the implementation and test boundary.

Real UWP permissions/restart, UI/media-event timing, codec support and asynchronous decoder-failure recovery need user app testing; do not claim these were validated locally.

## Working rules

- Root orchestrates/reviews; **gpt-6-luna subagents write application code**.
- Own HEAD; **skip pull/fetch**. Commit each step and push normally; no force push.
- Sole existing behavior reference: [`ywmoyue/biliuwp-lite`](https://github.com/ywmoyue/biliuwp-lite/tree/ca2caddc597b3faf05b4dac6dcb226aa0694365b) at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`; root retrieved the API-01 source files through the GitHub connector. Do not fetch/pull. New manifest requirements come from the preserved plan.
- No app builds, restores, launches, protocol generation or live application API tests. Focused offline unit tests of actual source pieces and format checks are allowed.
- Existing unit runner: `work/improvements/tests/run.sh`; focused API-01 runner: `work/improvements/tests/run-api01.sh`; cached tools: `/workspace/work/upstream-unit-tools`. LM-01–05 and API-01 actual-source offline probes passed; UWP storage/player/network boundaries are stubbed. Whitespace checks passed.
- Preserve full exception/result details in logs with stage and collection/item/track IDs. Keep user-facing errors concise.
- Update `IMPROVEMENT_PROGRESS.md` at every step. Favor practical small changes.
- SponsorBlock excluded; live tiles optional. API/auth, gRPC, danmaku improvements and player toasts follow local-media work per plan.
- Prior bug series: 14/15 fully fixed; fully unmarked AI subtitles (#1311) and old discarded subtitle labels remain unresolved. Visual issues remain assigned to the user.

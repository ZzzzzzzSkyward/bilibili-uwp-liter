# Handover

## Resume point

- Repo: `/workspace/bilibili-uwp-liter`, branch `work`.
- Published application baseline: `8106491` (LM-05 reviewed). No pending application changes.
- Next: **API-01 — API compatibility layer and service organization**.
- Plan: `work/improvements/PLAN.md`. Detailed checkpoint: `IMPROVEMENT_PROGRESS.md`.

## Completed

- LM-01: typed v1 manifest models, loader and validation.
- LM-02: folder import, saved FutureAccessList grants, collection list/reopen.
- LM-03: collection items play through the existing player; resolved StorageFile video/audio handles, stable local resume IDs, manifest-specific Bilibili network/history suppression.
- LM-04: independent video/audio selectors, embedded audio option, position/play-state preservation, one rollback reopen after immediate switch failure.
- LM-05: saved-grant subtitle/danmaku file resolution; Bilibili JSON, SRT, and VTT cue parsing; explicit-default subtitle selection; independently toggleable XML danmaku tracks merged through the existing parser and renderer. Actual-source offline probes cover parsing, cue selection, asynchronous stale-load guards, merged tracks, and resolved file handles.

## Next batch

API-01: separate cookie handling and WBI signing into services while retaining the existing `ApiHelper` compatibility wrappers and public callers. Check the pinned sibling before porting existing behavior; the sibling was absent in this restored workspace. Inventory current implementations first to avoid duplicating endpoints already present.

Real UWP permissions/restart, UI/media-event timing, codec support and asynchronous decoder-failure recovery need user app testing; do not claim these were validated locally.

## Working rules

- Root orchestrates/reviews; **gpt-6-luna subagents write application code**.
- Own HEAD; **skip pull/fetch**. Commit each step and push normally; no force push.
- Sole existing behavior reference: `/workspace/biliuwp-lite` at `ca2caddc597b3faf05b4dac6dcb226aa0694365b` when available; it was absent during LM-05. New manifest requirements come from the preserved plan.
- No app builds, restores, launches, protocol generation or live application API tests. Focused offline unit tests of actual source pieces and format checks are allowed.
- Existing unit runner: `work/improvements/tests/run.sh`; cached tools: `/workspace/work/upstream-unit-tools`. LM-01–05 actual-source probes passed; UWP storage/player boundaries are stubbed. Whitespace checks passed.
- Preserve full exception/result details in logs with stage and collection/item/track IDs. Keep user-facing errors concise.
- Update `IMPROVEMENT_PROGRESS.md` at every step. Favor practical small changes.
- SponsorBlock excluded; live tiles optional. API/auth, gRPC, danmaku improvements and player toasts follow local-media work per plan.
- Prior bug series: 14/15 fully fixed; fully unmarked AI subtitles (#1311) and old discarded subtitle labels remain unresolved. Visual issues remain assigned to the user.

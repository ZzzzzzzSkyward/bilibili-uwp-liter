# Upstream logic bug fix progress

## Current phase: batch 003 complete; awaiting user build/testing

The user authorized implementation fixes and commits. Batch 001 fixed #31, #105, and #121; batch 002 fixed #146, #275, and #358. Batch 003 is implementing the next three findings in order. Root reviews and pushes completed batches. The assessment catalog and its historical disposition records remain unchanged; implementation status is tracked here.

Batch 003 started from target branch `work` at `87cf0d1` (clean). Per user instruction, no pull/fetch was performed. Behavior reference: sibling `/workspace/biliuwp-lite`, pinned at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`.

## Constraints

- No application builds, restores, runs, protocol generation, or live application API calls.
- Isolated probes may execute actual extracted target source with stubbed boundaries, using `/workspace/work/upstream-unit-tools`; do not replace these with manually mirrored algorithms.
- Formatting checks are permitted. No broad refactors or unrelated issue changes.
- Do not claim end-to-end app behavior from isolated probes or source review.

## Batch state

| #378 | Fixed | Target model now preserves absolute URLs and maps malformed, null, and empty values to the pinned sibling fallback. Extracted actual nested class probe passes all four cases; no API/UI runtime. Commit recorded below. |
| #404 | Fixed | Empty `area_entrance_v2` now skips area parsing while the populated first-entry path remains intact. Extracted actual block probe passes empty and populated shapes. Commit recorded below. |
| #598 | Fixed | Natural-end handler awaits one history report for the current item at `Player.Duration` after interaction early returns and before mode/auto-next handling. Updated actual-handler probe passes exact item, duration, and once assertions. Commit recorded below. |

| Issue | State | Evidence / commit |
|---|---|---|
| #31 | Fixed | Extracted-source probe passes: confirm-refresh POST dispatched. Commit `e70c4867083294e849f39e3535bdc34f94c3f7c9`. |
| #105 | Fixed | Extracted `Dispose` method probe passes: player/timer cleanup completes while the captured history report is pending, and the original item and position survive player teardown. Fix commit `41c39ff0f7eea587c7fbdd0ff7d5dc5c481b5cef`; probe-strengthening commit `0f7414f1c57a05a0142cf15a2bf596b16bcce65a`. |
| #121 | Fixed | Source review confirms enabled middle/XButton1 presses remain unhandled for the existing MainPage bubbling route; matching releases bypass the gesture recognizer. Format and whitespace checks pass. Commit `71668eae37ef5e88c60a521e6c9f92caecbf6346`. |
| #146 | Fixed | Episode list preview key-down/up events now stop focused-list arrow navigation and reach the player seek path, matching the pinned sibling. Source review and `git diff --check`; no UI runtime. Commit `d019842805fd406aa2d39d707bf86af03f61c09e`. |
| #275 | Fixed | `top_replies` now deserializes as the target display model and is prepended as pinned comments; old `upper.top` remains supported, normal first replies are preserved, and duplicate IDs are suppressed. Actual insertion-block probe passes pinned-first/visible, same normal-first and nested objects, duplicate suppression, and pinned-only cases; UWP controls and full model are stubbed. Commits `839d0c8b8712d2c5631dbb4d9bd27c65d5e00746` and corrective follow-up `ade587708588bac3d9607c13b62e7cbe1fe860c8`. |
| #358 | Fixed | Active history page migrated to cookie-authenticated `/x/web-interface/history/cursor`, with cursor pagination, response mapping to existing item bindings/actions, refresh reset, empty-page end behavior, and supported archive/PGC/live/article navigation. Full actual `HistoryVM` and DTO source plus actual extracted `HistoryWbi` method pass the isolated recording-boundary probe; no app runtime. Commit `94e65a935c44c02e5f3ce96cda33d2f5265b12f1`. |

## Batch 003 issue commits

- #378: `408169f`.
- #404: `2932bb8`.
- #598: `0ab19b9`.

## Batch 003 verification

- #378: `work/upstream-bugs/unit-tests/batch-003/issue-378/run.sh` — passes against the extracted actual nested target class; absolute URL is preserved and malformed, null, and empty values use the sibling fallback.
- #404: `work/upstream-bugs/unit-tests/batch-003/issue-404/run.sh` — passes against the extracted actual target block; empty area arrays skip indexing and populated first-entry lists are loaded. Shape stubs only; no live API or UI execution.
- #598: `work/upstream-bugs/unit-tests/batch-041/issue-598/run-natural-end-report.sh` — passes against the current actual handler, extracted by declaration and balanced braces; verifies exactly one report with the same item reference and exact `Player.Duration` (100 fixture). Report boundary is a local stub.
- Batch 003 started at `87cf0d1` after the prior batch 002 push; root reviews and handles pushes. No pull/fetch, application build, restore, run, protocol generation, or live API test was performed.

## Verification record

- #31: `work/upstream-bugs/unit-tests/batch-002/run-refresh-cookie-dispatch.sh` — passes against the extracted target source; confirms the confirm-refresh POST dispatch only.
- #105: `work/upstream-bugs/unit-tests/batch-008/issue-105/run-dispose-stop-order.sh` — passes against the extracted target method; confirms immediate player/timer cleanup and preserved captured item/position with stubbed boundaries.
- #121: reviewed the target event handlers and MainPage bubbling handler; `dotnet format src/BiliLite.UWP/BiliLite.UWP.csproj --no-restore --verify-no-changes --include Controls/PlayerControl.xaml.cs` and `git diff --check` pass.
- #146: source review and whitespace checks only. #275: `work/upstream-bugs/unit-tests/fix-batch-002/issue-275/run.sh` — passes against the extracted insertion block; confirms pin-first visibility, normal-first and nested object preservation, duplicate suppression, and pinned-only insertion with minimal model/list stubs.
- #358: `work/upstream-bugs/unit-tests/fix-batch-002/issue-358/run.sh` — passes against the full actual `HistoryVM` and actual extracted `AccountApi.HistoryWbi`, with real Newtonsoft deserialization of fixture JSON and a recording request boundary. Confirms initial GET shape/cookie flag, cursor append, archive/PGC item mapping and `kid` deletion key apart from nested OID, empty terminal page, and refresh reset. This executes no UWP navigation, actual transport, or live API. The earlier format-only command could not start because the available .NET tool failed initialization (`/home/agent/.dotnet/8.0.427.toolpath.sentinel` was read-only, then NuGet migration mutex initialization failed with `DOTNET_CLI_HOME` redirected to `/tmp`).
- No application build, restore, run, protocol generation, live app API call, or end-to-end behavior check was performed in batch 002.

Batch 002 application changes were reviewed and pushed to `origin/work` through `f6c8a4fe19cf1b073b53c7dba200b6f6c246decf`, including the history navigation fallback follow-up. Both isolated probes passed after that commit; temporary compiler outputs are excluded from Git. Nine of the 15 assessed findings have implementation fixes, including batch 003. User build/testing is pending; next finding: #611.

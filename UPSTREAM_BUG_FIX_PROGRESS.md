# Upstream logic bug fix progress

## Current phase: batch 002 complete; root review and push pending

The user authorized implementation fixes and commits. Batch 001 fixed #31, #105, and #121. Batch 002 fixed the next three findings, #146, #275, and #358, in order. Root reviews and pushes completed batches. The assessment catalog and its historical disposition records remain unchanged; implementation status is tracked here.

Batch 002 started from target branch `work` at `9fe3b5ac8e652ef553f42c837b931650adced7ff` (clean). Behavior reference: sibling `/workspace/biliuwp-lite`, pinned at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`.

## Constraints

- No application builds, restores, runs, protocol generation, or live application API calls.
- Isolated probes may execute actual extracted target source with stubbed boundaries, using `/workspace/work/upstream-unit-tools`; do not replace these with manually mirrored algorithms.
- Formatting checks are permitted. No broad refactors or unrelated issue changes.
- Do not claim end-to-end app behavior from isolated probes or source review.

## Batch state

| Issue | State | Evidence / commit |
|---|---|---|
| #31 | Fixed | Extracted-source probe passes: confirm-refresh POST dispatched. Commit `e70c4867083294e849f39e3535bdc34f94c3f7c9`. |
| #105 | Fixed | Extracted `Dispose` method probe passes: player/timer cleanup completes while the captured history report is pending, and the original item and position survive player teardown. Fix commit `41c39ff0f7eea587c7fbdd0ff7d5dc5c481b5cef`; probe-strengthening commit `0f7414f1c57a05a0142cf15a2bf596b16bcce65a`. |
| #121 | Fixed | Source review confirms enabled middle/XButton1 presses remain unhandled for the existing MainPage bubbling route; matching releases bypass the gesture recognizer. Format and whitespace checks pass. Commit `71668eae37ef5e88c60a521e6c9f92caecbf6346`. |
| #146 | Fixed | Episode list preview key-down/up events now stop focused-list arrow navigation and reach the player seek path, matching the pinned sibling. Source review and `git diff --check`; no UI runtime. Commit `d019842805fd406aa2d39d707bf86af03f61c09e`. |
| #275 | Fixed | `top_replies` now deserializes as the target display model and is prepended as pinned comments; old `upper.top` remains supported, normal first replies are preserved, and duplicate IDs are suppressed. Source review and `git diff --check`; no isolated probe. Commits `839d0c8b8712d2c5631dbb4d9bd27c65d5e00746` and corrective follow-up `ade587708588bac3d9607c13b62e7cbe1fe860c8`. |
| #358 | Fixed | Active history page migrated to cookie-authenticated `/x/web-interface/history/cursor`, with cursor pagination, response mapping to existing item bindings/actions, refresh reset, empty-page end behavior, and supported archive/PGC/live/article navigation. Source review, old-call-site audit, and `git diff --check`; no isolated probe or app runtime. Commit `94e65a935c44c02e5f3ce96cda33d2f5265b12f1`. |

## Verification record

- #31: `work/upstream-bugs/unit-tests/batch-002/run-refresh-cookie-dispatch.sh` — passes against the extracted target source; confirms the confirm-refresh POST dispatch only.
- #105: `work/upstream-bugs/unit-tests/batch-008/issue-105/run-dispose-stop-order.sh` — passes against the extracted target method; confirms immediate player/timer cleanup and preserved captured item/position with stubbed boundaries.
- #121: reviewed the target event handlers and MainPage bubbling handler; `dotnet format src/BiliLite.UWP/BiliLite.UWP.csproj --no-restore --verify-no-changes --include Controls/PlayerControl.xaml.cs` and `git diff --check` pass.
- #146 and #275: source review and whitespace checks only.
- #358: source review confirmed the cursor request shape and cookie flag, response field mapping, cursor reset/advancement, and target page navigation parameter semantics. `rg` audit found no remaining `AccountApi.History` consumer; other history routes found belong to live-center history, playback reporting, and watch-later. `git diff --check` passes. The format-only command could not start because the available .NET tool failed initialization (`/home/agent/.dotnet/8.0.427.toolpath.sentinel` was read-only, then NuGet migration mutex initialization failed with `DOTNET_CLI_HOME` redirected to `/tmp`).
- No application build, restore, run, protocol generation, live app API call, or end-to-end behavior check was performed in batch 002.

Batch 002 is complete at `94e65a935c44c02e5f3ce96cda33d2f5265b12f1`. Next finding: #378. Root review and push remain pending.

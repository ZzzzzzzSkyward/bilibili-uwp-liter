# Upstream logic bug fix progress

## Current phase: batch 001 complete

The user authorized implementation fixes and commits. The orchestrated first batch addressed the oldest three findings in `work/upstream-bugs/VALIDATED_LOGIC_BUGS.md`, in order: #31, #105, and #121. This batch stops after #121; #146 is next. Root will handle the previously authorized push after review. The catalog assessment, historical findings, and disposition records remain assessment history and must not be rewritten to imply the fixes were part of the assessment.

Baseline target checkout: branch `work`, HEAD `52cb42021eacf8a057a63d43e3f4ff494e31ed15` (clean when this batch began). Behavior reference: sibling `/workspace/biliuwp-lite`, pinned at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`.

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

## Verification record

- #31: `work/upstream-bugs/unit-tests/batch-002/run-refresh-cookie-dispatch.sh` — passes against the extracted target source; confirms the confirm-refresh POST dispatch only.
- #105: `work/upstream-bugs/unit-tests/batch-008/issue-105/run-dispose-stop-order.sh` — passes against the extracted target method; confirms immediate player/timer cleanup and preserved captured item/position with stubbed boundaries.
- #121: reviewed the target event handlers and MainPage bubbling handler; `dotnet format src/BiliLite.UWP/BiliLite.UWP.csproj --no-restore --verify-no-changes --include Controls/PlayerControl.xaml.cs` and `git diff --check` pass.
- No application build, restore, run, protocol generation, live app API call, or end-to-end behavior check was performed.

The batch stops after #121; #146 remains untouched. Latest completed fix commit: `71668eae37ef5e88c60a521e6c9f92caecbf6346`.

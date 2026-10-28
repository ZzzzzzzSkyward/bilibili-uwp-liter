# Upstream logic bug fix progress

## Current phase: authorized fixes, batch 001

The user authorized implementation fixes and commits for the oldest three findings in `work/upstream-bugs/VALIDATED_LOGIC_BUGS.md`, in order: #31, #105, and #121. Stop after #121; #146 is the next item and is outside this batch. Commits are authorized; pushing is not part of this batch. The catalog assessment, historical findings, and disposition records remain assessment history and must not be rewritten to imply the fixes were part of the assessment.

Baseline target checkout: branch `work`, HEAD `52cb42021eacf8a057a63d43e3f4ff494e31ed15` (clean when this batch began). Behavior reference: sibling `/workspace/biliuwp-lite`, pinned at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`.

## Constraints

- No application builds, restores, runs, protocol generation, or live application API calls.
- Isolated probes may execute actual extracted target source with stubbed boundaries, using `/workspace/work/upstream-unit-tools`; do not replace these with manually mirrored algorithms.
- Formatting checks are permitted. No broad refactors or unrelated issue changes.
- Do not claim end-to-end app behavior from isolated probes or source review.

## Batch state

| Issue | State | Evidence / commit |
|---|---|---|
| #31 | In progress | Existing extracted-source probe needs its source-line extraction updated to follow the current method; fix and probe pending. |
| #105 | Not started | Existing extracted-method disposal probe; fix and probe pending. |
| #121 | Not started | Source change and formatting check; no fabricated UWP input test. |

No application changes or commits have been made yet. Latest completed commit: none.

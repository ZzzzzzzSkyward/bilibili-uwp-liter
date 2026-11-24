# Implementation-review assignments (launch after execution stage completes)

Read repo/cloud instructions, memory index, controlling requirements/reviews/amendment, actual immutable commit diffs and execution evidence. All findings belong in the assigned final-review files. Review only; no source/test/build/docs/history mutation. No softened acceptance or Windows/device claim.

## Source reviewer — logic_review, gpt-6.1-sol / xhigh

Input: `logic-data-errors/execution.md` and both slice records. U1 source/test commit `5eec54ecd094bdb51ec43b42d9f901d8fae7c993`, PB1 `edbd8739d894b8ab59400e097eb96a9a0a1ac592`; metadata follow-up `10dd84857c748f6638e187d891d6a6649f887bb6`. Compare against their actual parents, not other owners' later working-tree edits.

Own `logic-data-errors/final-review.md`, plus per-slice files under u1/pb1 if useful. Confirm exact form/caller encoding and credential blast radius, multiplicity-preserving real-source regressions/red evidence, shared bounded code reader across four paths, BigInteger/UInt64 handling and unchanged payload/error categories. Inspect original assertion preservation and evidence. Run affected checks where helpful against a stable source state; do not race later ownership. Record compiler/runtime platform limits. PASS/CHANGES with actionable P0/P1/P2 and explicit source/data-structure quality conclusion; no broad cleanup demand absent a defect.

## Quality reviewer — quality_review, same model/effort

Input: finalized `verification/execution.md`, source extraction/mapping/negative-control evidence, exact quality commit SHA and full-suite records. Own `verification/final-review.md`.

Confirm only approved harness files changed; all real dependency/extraction decisions and actual global-wrapper type retained; phase-aware credential fixture changes match reviewed contracts, not fake success; no unauthorized assertion/scenario reductions. Check actual history method/field source fidelity, serialized execution context, overlapped-report/latest-tail/duplicate/paused-flush/exception cases and failing serialization negative control. Verify unique real g3 dependency, LM05 models/import, both AUTH01B executables, and complete aggregate 43-directory/seven-logical-probe (51 executable-stage) results without hidden prefixes. Reconcile setup corrections and exact source/test SHA with baseline, shared-stub dependents and artifact assertions. Windows/platform gates stay FAILED/BLOCKED.

## Cross-stream closure

Main docs remain reserved for one later owner; missing disposition updates are queued under that explicit owner, not grounds for concurrent reviewer writes. Next-wave changes must not invalidate reviewed snapshots: bind review/test conclusions to exact commits and inspect their own later diffs separately. Any P0/P1 finding returns to its original executor with an explicit file-based fix request and re-review. Root orchestrator verifies final branch/remote/evidence/worktree after review.

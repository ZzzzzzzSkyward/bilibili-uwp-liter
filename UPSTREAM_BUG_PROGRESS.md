# Upstream issue collection and triage

## Scope and permissions

- Source catalog: complete GitHub REST issue snapshot for `ywmoyue/biliuwp-lite`, 757 actual issues (open and closed), after excluding 293 pull requests. Original classification retains all 757 rows; 477 eligible bug candidates remain in their original creation order and 96 batch groups.
- Target app source snapshot remains `cfa40a8508c6d6e7401885bf6bb081bc5c90b173`; the sole behavior reference is `/workspace/biliuwp-lite` at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`.
- Do not change application code, build or restore the app/solution, generate protocol code, call live application APIs, or run the app. User direction permits isolated unit execution for a concrete piece of source logic; it does not permit application/project/solution builds, app restores, or runtime validation. The completed #31 isolated unit probe is documented in `work/upstream-bugs/VALIDATED_LOGIC_BUGS.md` and `work/upstream-bugs/unit-tests/batch-002/README.md`.
- This aggregation changes only local catalog and documentation data. Do not alter upstream issues or raw collection caches.

## Current policy queue

- `work/upstream-bugs/candidate-queue.json` preserves all 477 candidate rows, their IDs/order, classifications, and original 96 batch IDs. Each row retains its previous target assessment in `history.target_assessment_before_policy` and exposes current ownership/disposition separately.
- `work/upstream-bugs/policy-queue.json` is the complete 477-row current ownership and disposition overlay. `work/upstream-bugs/logic-validation-queue.json` contains only oldest-first pending logic work. `work/upstream-bugs/user-visual-review.json` and `USER_VISUAL_REVIEW.md` keep the 59 visual assignments with the user as the local catalog owner; no GitHub issue was edited.
- Visual ownership: 59 reports total, 54 visual-only and 5 mixed. The visual portion of all 59 is assigned to the user. The five functional portions (#89, #549, #562, #981, #1469) remain in pending logic review.
- Current counts from the machine-readable records: 477 candidate rows; 59 visual assignments; 409 pending logic issue rows (including the five mixed functional portions); one confirmed source logic defect (#31); two wholly addressed issues (#22, #42), plus the addressed overflow subclaim in #46; eight issues skipped wholly as skeptical, plus the skipped v2-route subclaim in #46; two issues are skipped as non-logic.
- `work/upstream-bugs/VALIDATED_LOGIC_BUGS.md` records #31 only. The isolated target-source probe confirms the wrong request object is dispatched, not that this caused the reported playback-quality symptom. No app fix has been made.
- `work/upstream-bugs/SKIPPED_ISSUES.json` records skipped issue/subitem rationales. Historical details for the original assessments remain in `batches/001.md` through `batches/003.md` and `policy-dispositions-002.json` / `policy-dispositions-003.json`.

## Completed source assessments

- Batch 001: #5 is not applicable on available target source evidence; #9, #14, and #21 are skipped as skeptical; #22's reported overflow is addressed by the target's 64-bit field.
- Batch 002: #24 and #28 skipped skeptical; #27 skipped as documentation; #31 has the confirmed wrong-dispatch defect described above; all three functional claims in #36 are skipped skeptical.
- Batch 003: #38 skipped skeptical; #42's exact Int32 overflow is addressed; #46's reported ID overflow is addressed while its v2 endpoint claim is skipped skeptical; #51 is assigned to the user as visual-only; #56 skipped as non-logic.

## Resume point

- Resume original **Batch 004** in `work/upstream-bugs/batch-index.md`: #61, #62, #65, #67, #69. #61 and #67 are pending logic assessment; #62, #65, and #69 are already assigned as user-owned visuals.
- The next five pending logic IDs after omitting visual-only rows are #61, #67, #74, #82, and #87. Mixed issue logic subitems stay in `logic-validation-queue.json`.
- Keep assessment evidence conservative: use the pinned sibling only as a behavior reference, separate source defects from symptom causality, and skip uncertain claims rather than promoting them to confirmed defects.

## Current continuation setup

- This round started from clean `work` HEAD `b4a3cb78266091d34801d3e2d3ae958184d553b9`. `git pull --ff-only origin work` reported `Already up to date`.
- The target application source snapshot remains `cfa40a8508c6d6e7401885bf6bb081bc5c90b173`; later commits in the current branch update only the local issue catalog and assessment documentation. The pinned sibling remains clean at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`. No `AGENTS.md` was found under `/workspace`.
- Continue source assessment from original Batch 004. #61 and #67 are the pending logic candidates in that batch; #62, #65, and #69 remain assigned as user-owned visual reports. The next oldest pending logic IDs after those are #74, #82, and #87.
- Available prior isolated-unit tooling is documented at `work/upstream-bugs/unit-tests/batch-002/README.md`; the runner is `work/upstream-bugs/unit-tests/batch-002/run-refresh-cookie-dispatch.sh`, and its SDK/tool caches are under `/workspace/work/upstream-unit-tools` outside Git. This setup step did not run the unit or change application source.

## Validation and repository state

- The prior aggregation did not inspect or modify application source or unit-test scripts and did not run tests, builds, restore, app/API calls, or runtime checks.
- After writing these catalog/docs files, run only `git diff --check`, explicitly stage only those files, and commit the aggregation. Runtime/unit tooling and caches live outside Git under `/workspace/work` and must not be staged.

## Live assessment checkpoint

- Original Batch 004 source assessment is complete: #61 is skipped as skeptical on both subclaims; #67 is addressed by the target source. #62, #65, and #69 remain assigned to the user as visual reports.
- Original candidate accounting remains at the 409 pending-logic baseline until final queue aggregation. The completed Batch 004 dispositions are recorded separately in `work/upstream-bugs/policy-dispositions-004.json`; details are in `work/upstream-bugs/batches/004.md`.
- Original Batch 005 is complete: #74's reported missing WBI handling is already addressed in the target; #82's visual mask is assigned to the user and its unsupported crash claim is skipped as skeptical; #70, #77, and #81 are assigned to the user as visual reports. The source report and dispositions are in `work/upstream-bugs/batches/005.md` and `work/upstream-bugs/policy-dispositions-005.json`.
- Original Batch 006 is complete: #87's save-on-close path is present, while its quality claim is skipped as skeptical; #89's reported collection path is absent from the target and its visual-text subclaim is assigned to the user; #90 is skipped as skeptical because the report supplies no pagination evidence; #83 and #88 are assigned to the user as visual reports. The source report and dispositions are in `work/upstream-bugs/batches/006.md` and `work/upstream-bugs/policy-dispositions-006.json`.
- The active source-assessment step is original Batch 007: #91 is pending logic assessment; #95, #96, #100, and #102 remain assigned as user-owned visual reports. No application code changed; no app build, restore, API request, runtime check, or unit test was run for Batches 004–006.

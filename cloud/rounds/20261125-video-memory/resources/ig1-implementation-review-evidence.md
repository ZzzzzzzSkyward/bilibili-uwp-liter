# IG1 implementation review evidence

Verdict: **PASS / RELEASED**. Exclusive scratch: `/workspace/work/video-memory/player-ig1-review/`. Final Control `262e0719cf4589ebd9e493937f377bcc95d43e092962aad30dbfc137275bcb89`; captured baseline b382 `b3823bdb9fe15e7f90465a78446f6480fc81c8c92832de6f72d1762621a0a3b7`.

Read accepted IG1 review/PF1 contract, exact fix/implementation-review grants, writer execution/evidence, actual two public methods/helper/selector/switch consumers, unchanged ux1d Body/read/check logic, actual baseline/final stage logs/reports and immutable first failed full receipt. No source/test/cache/ref change or executable replay was performed by this reviewer; exact receipt reuse is appropriate for the two verified replacements and unchanged fixtures.

## Exact source and preservation checks

Private `binding-check.json` records46 frozen read-only fixture/source/tooling inputs plus pinned SDK/runtime/reference/Newtonsoft DLL hashes and validates every value against actual current files. Baseline full source was independently matched to the pre-edit original snapshot. Removing only the complete Show/Clear spans from old and final raw bytes yields identical remainders, including all other code/comments/fields. Both changed spans match preservation hashes.

Stronger checks reconstruct each new body by substituting only its old `UpdatePlaybackControlEligibility();` call with the exact approved executable direct assignment/conditional. No other body byte changes. The first non-whitespace statement remains `if (_disposed) return;`; neither new body contains a selector write or helper call. Existing pending/target/host/accessibility/name and label operations remain identical. Show's false is null-safe and executes on the normal pending branch; Clear's true is reachable only on the no-current-switch branch, with false otherwise. No new current-item requirement was introduced, so transition UI still works before item initialization.

Unchanged critical span hashes were independently checked against both actual sources: UpdatePlaybackControlEligibility, RF1 current-work predicate, SetPlayItem, SwitchLocalTrackAsync, renderer, HistoryKey, checkpoint/drain/flush and Dispose. Renderer remains `5789d03300d297cb7bc6115ee0622417b5773960544b3b0899ec2bb8167fbcb7`. All earlier terminal/live/fresh, PF1/FA1/FA2/FA3/RF1, session/history/settings and G1 contracts are preserved outside these exact transition statements.

Private `method-fidelity.json` independently validates both run extraction manifests:51 projected complete control method/property bodies per side match the appropriate baseline/final actual source, including Show/Clear. Baseline manifests point to the then-current repository path, so the archived b382 snapshot was used for their actual body comparison, rather than accidentally comparing old hashes against final262e. All generated output hashes match their frozen raw files. No body substitution in a fixture supplies the correction.

## Actual selected pair and maintained source checks

The writer's actual unchanged selected command on each source was:

```bash
PLAYER_LIFETIME_EVIDENCE_DIR=<private-baseline-or-final-extraction> \
bash plan/tests/run-all.sh --tools-root /workspace/work/maintenance-foundation/quality \
  --only batch:playersess --only batch:ux1d --output <private-side-report>
```

Reports/logs/binaries and all recorded hashes were independently read and checked, not rerun or relabeled. Raw pair lives in `/workspace/work/video-memory/player-ig1-fix/`:

| Artifact | SHA256 / observation |
| --- | --- |
| baseline-report/results.json | `f087213ca5d70b231f0dbb225464bf857f8716f5e0fd6d6d8c70014c891a6d91`; exit1, FAIL, complete_required=false. |
| final-report/results.json | `98734a1dd7b2d22af17e2b157ae9492ed29b9bf4495040c1db315db5922a134e`; exit0, PARTIAL_PASS, complete_required=false. |
| baseline ux1d stage |54 actual ok assertions then maintained Show direct-false check FAIL. |
| final ux1d stage |107 actual ok assertions and `PASS 107 assertions`; includes the previously unreached Clear direct-true check and entire suffix. |
| playersess both stages |66 residual PASS,39 lifetime PASS,five graphics PASS, original240 plus graphics dispatch=241 checks. |

The ux1d binary itself is identical on both sides (its runtime source reads differ); unchanged Program.cs SHA256 is `2785156302529adf2c61c0f8f60815639095fe973236d062a1d9ef2a05411561`. Its Body reader includes the actual nested null/eligibility branches, and the required false/true statements are executable source, not comments or literals. Other source checks/assertions remain untouched.

Existing actual-method PF1 cases already interleave Show/Clear with B's pending track switch, exercise both current disable reasons, settle current success/failure/fault/cancel recovery, reject obsolete A mutation/model rollback, verify labels/visibility and invoke terminal transition entries. Reusing that meaningful positive evidence with the inspected direct branch truth table is sufficient; no extra test/assertion or repeated graphics/full suite is needed for review.

The helper still computes selectors from `!_localTrackSwitching` and Next from `!_localTrackSwitching && !_transitionPending` for item/switch callers. Show has already set pending=true, so false is the exact Next effect. Clear has already cleared its pending reason, so only the current switch determines eligibility. Transition methods therefore need no redundant selector writes, and neither completion path releases the other current restriction. Current-item null safety from RF1 is byte-identical and not incorrectly imposed on transition-only UI.

## Immutable failure and scope limits

The first complete gate remains untouched: results `aee9bff87ec5fd891b40b6a89f4289d4e1b298a9f5d4504fe5f7b3be34426a17`, ux1d log `3296315f77d40ab1471d05180ae70c36665d171e5ade7f7273c1c56300da91ce`. Its status is still FAIL with51/52 executable,50/51 logical PASS. New partial selected green is not complete integration. The unchanged failed hashes were rechecked before release.

Scoped git diff --check exits0. No new field/helper/policy/project/stage, changed assertion, source outside two methods, selected-budget change, full run, separate graphics/native test, Git/index/ref/main-document/cache or publication operation occurred in this review. All scoped actual source/fixture/tool/document inputs were rechecked unchanged. No moving whole-tree/API equality claim is made; disjoint investigation authority does not change player acceptance.

This is maintained wiring compatibility with preserved runtime ownership. Linux source/model receipts do not establish native Windows UWP build, pointer/XAML delivery, cache/dispatcher/SMTC/display/codec/media/graphics/pixels/RAM/GPU/service/session/signing/install/startup or hosted CI. No memory saving or fifty watched-video reproduction is claimed. Root owns both review releases, the next actual stable combined51/52 gate and later authorized publication/docs/archive. Latest stop/freeze/push-start remains08:29:30UTC/16:29:30Asia/Shanghai; this PASS does not reset that window or grant implementation afterward. Read custody is fully released.

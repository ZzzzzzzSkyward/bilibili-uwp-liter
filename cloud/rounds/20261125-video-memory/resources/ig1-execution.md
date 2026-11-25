# IG1 transition correction — FROZEN / RELEASED

Same sole owner: memory_player_executor, gpt-6.1-sol/high. Exact grant `../ig1-fix-dispatch.md`; independent IG1 design PASS under retained PF1. Original b382 was snapshotted before edits in `/workspace/work/video-memory/player-ig1-fix/original/`. Runtime reconnect resumed the same owner; no clock reset, duplicate writer, Git/index/ref/pull/checkout/reset/stash/rebase/staging/commit/push/main-document/API or other-owner mutation occurred. No scope blocker remains.

Original Control SHA256: `b3823bdb9fe15e7f90465a78446f6480fc81c8c92832de6f72d1762621a0a3b7`.
Final Control SHA256: `262e0719cf4589ebd9e493937f377bcc95d43e092962aad30dbfc137275bcb89`.

**Exactly one product file, two public method bodies changed.** ShowTransitionPending now directly assigns false to a non-null BottomBtnNext. ClearTransitionPending uses a real null-safe conditional: true only when !_localTrackSwitching; otherwise false. Both retain the first terminal guard, transition fields/targets, overlay/accessibility/labels and ApplyNextLabel. Neither calls the selector eligibility helper or adds a current-item requirement. The unchanged helper/item/switch paths still own selectors and current switch eligibility, and still prevent matching switch completion from releasing a pending transition. No comment/string/unreachable-code workaround or redundant selector writes.

Raw source bytes outside these two spans are identical. Every test/stub/extractor/project/runner/assertion stays byte-identical; unchanged critical helper/RF1/renderer/history/item/switch/dispose spans are rebound in [preservation.json](ig1-implementation-evidence/preservation.json). Earlier RF1/residual execution and evidence receipts remain exact. No new file/stage/helper/dependency/product API, page/XAML or unrelated source change.

## Unchanged pair, genuine red → green

Private mode driver commands:

```bash
python3 /workspace/work/video-memory/player-ig1-fix/run-mode.py baseline
python3 /workspace/work/video-memory/player-ig1-fix/run-mode.py final
```

Each driver exits0 only for its expected selected result, while invoking unchanged selected argv:

```bash
PLAYER_LIFETIME_EVIDENCE_DIR=/workspace/work/video-memory/player-ig1-fix/<baseline-or-final>-extraction \
bash plan/tests/run-all.sh --tools-root /workspace/work/maintenance-foundation/quality \
  --only batch:playersess --only batch:ux1d \
  --output /workspace/work/video-memory/player-ig1-fix/<baseline-or-final>-report
```

The baseline ran actual unchanged b382 before editing: playersess PASS/exit0; ux1d FAIL/exit1 after54 `ok` assertions at `source: the next button does not stay enabled-but-dead while pending`. Selected exit1/suite FAIL, complete_required=false. This is the reproduced maintained source-wiring failure, not a compiler failure or runtime ownership failure.

The final source runs the identical fixtures/tool/dependency bytes: playersess PASS/exit0; complete ux1d PASS/exit0 with107 assertions, including `source: ending the wait restores the overlay and the button`, which the first failure previously prevented from executing. Selected exit0, **PARTIAL_PASS**, complete_required=false; two selected logical/executable stages. Both sources preserve66/66 residual groups,39/39 lifetime groups,5/5 graphics groups and all original240 assertions (existing aggregate241 checks). Full actual-method PF1 interleavings retain both disable reasons, current B recovery, old A noninterference, labels and terminal guards.

[bindings.json](ig1-implementation-evidence/bindings.json) records exact commands/environments, source before/after, immutable46 fixture/source/tooling inputs, pinned SDK8.0.427/runtime/reference8.0.33 and actual Newtonsoft DLL before/after equality, logs/reports/private binary hashes. Both complete51 projected control method/property bodies are verified against their actual run source; tests and extraction bodies are not altered. `git diff --check -- src/BiliLite.UWP/Controls/PlayerControl.xaml.cs` exits0. Raw snapshots/generated sources/binaries/PDBs/logs stay exclusively in `/workspace/work/video-memory/player-ig1-fix/`.

## Failed full gate retained; limits and custody

The first actual full gate remains immutable **FAIL51/52 executable,50/51 logical PASS**, sole ux1d failure. Its original results SHA256 `aee9bff87ec5fd891b40b6a89f4289d4e1b298a9f5d4504fe5f7b3be34426a17` and ux1d log SHA256 `3296315f77d40ab1471d05180ae70c36665d171e5ade7f7273c1c56300da91ce` were verified before/after this correction. No receipt is overwritten or relabeled. [Evidence map](ig1-implementation-evidence/README.md) and [behavior summary](ig1-implementation-evidence/behavior-summary.json) distinguish it from the new partial pair.

No full common gate, separate graphics/runner/package/sampler gate or native test was run. This addresses maintained wiring while preserving runtime ownership; it is not a claim of a native playback/graphics/crash/RAM improvement. Windows/UWP/UI/cache/dispatcher/media/pixel/GPU/service/account/session/hosted limitations remain unchanged. Root owns independent owning/fresh IG1 review and a later new complete stable combined51/52 gate, justified by actual source change.

The authoritative project stop/freeze/push-start remains **2026-11-25 08:29:30UTC /16:29:30Asia/Shanghai**, not a new4-hour window. No publication right is used here. The bounded correction is ready for further authorized review/integration while root continues the maintenance window.

**FROZEN / RELEASED:** source/test-build/evidence custody released for owning/fresh IG1 review; no further executable edits. No blocker or additional scope request.

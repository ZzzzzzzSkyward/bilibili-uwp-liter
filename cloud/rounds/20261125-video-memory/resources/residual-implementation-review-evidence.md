# Residual player implementation re-review evidence

Status: **PASS / RELEASED**, PF1 closed; no new actionable P0/P1/P2 finding. Reviewer-only scratch: `/workspace/work/video-memory/player-residual-review/`. Bound final Control `284be647a5f633b4a39532d26b4a00856bab20eb901963ebafd545a8d52367c0`; old residual Control `8682d1505752a2dfd99e835259d45429cf78ee3e358caf39553b1375a7d4a3cc`. The initial PF1 review and original unsafe proofs remain untouched.

## Source/fixture review and fidelity

Read ACTIVE residual implementation review dispatch, accepted original/residual contracts, writer grant/execution and all final receipt maps. Inspected the actual final source rather than relying on private development diffs, plus all final residual probe/stub/extractor/project/runner changes. Relevant consumers include complete selector/transition helpers, full interaction child/base, current pool/lease/timer chain and ended/history/page consumer. Accepted child Loading/choice-collapse preconditions and finite detached work are retained; arbitrary overlapping same-live-child latest-wins behavior is not introduced.

Private `binding-check.json` verifies all eight changed/new paths, full runnable fixture/read-only source inputs, source-wiring files read at runtime, compiler/runtime/reference/Newtonsoft DLL hashes and regular-file modes. No symlink or unexpected product path was added. Source snapshots and private generated/binary outputs stay outside the repository.

Exact Program.cs becomes the old8682 Program after removing only the added residual try/catch dispatch and blank line. Its original240 checks and prior39/five dispatch are not rewritten. PlayerLifetimeProbe, all three historical history fixtures and all three graphics fixture files remain exact bytes. PlayerLifetimeStubs differs only by `_transitionPending` declaration consumed by actual new eligibility code. No old source helper is fabricated by the extractor.

Private `fidelity-and-receipt-check.json` records independent full-span checks:47 old/50 final complete control method/property bodies, plus one complete renderer span on each side. Every body matches the actual source and generated body; every raw generated output hash matches. The full real InteractionVideoVM is directly compiled; generated IModules equals the normalized actual full source after removing only `using LogHelper = ResidualProbe.BoundaryLog;`. Coordinator/option/label methods have only the declared LocalPlayInfo→LocalInfo type substitution. The generated platform namespace/header aliases do not rewrite control decisions.

Five unchanged product spans were independently compared old→final: GenerateSubtitleItem, HistoryKey, CheckpointHistoryAsync, DrainHistoryReportsAsync and FlushPlaybackAsync. Source and fixtures preserve terminal/live/fresh behavior, accepted/pending session ownership, same-reference rollback protection, settings refresh/dedup and G1 ownership. No new product dependency/helper file/runner inventory entry was introduced.

## Owning-stage independent replay

Private snapshot: `/workspace/work/video-memory/player-residual-review/snapshot`. All copied actual compiled/read inputs are recorded before/after. Exact commands/source hashes/log hashes are in `owning-commands.json`:

```bash
UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality \
PLAYER_LIFETIME_SOURCE=<old8682-or-private-final-Control> \
PLAYER_HISTORY_SOURCE=<same-Control> \
PLAYER_LIFETIME_EVIDENCE_DIR=<private-side-extraction> \
bash plan/tests/playersess/run.sh
```

Same final snapshot fixture/stub/extractor/runtime/dependency bytes run on both sources. Outputs are private `old8682.log` and `final.log`. Old source compiles, exits1 with49 unsafe residual groups across all four findings; no compiler/extractor error establishes red. Final source exits0 with58/58 safe residual groups. Counts independently match the released receipt: PF1 old1/17→final18/0; FA1 old7/14→21/0; FA2 old1/7→8/0; FA3 old0/11→11/0. The final replay log SHA256 is `cfa83d41f2a761558fca96860a51172c3c6efcf252ee0f05aad79bb42fca28d3`, matching the writer's final log exactly. Original240/prior39/five graphics pass on both sides; final aggregate prints241, since residual groups remain inside the same executable.

PF1 observations include usable actual video/audio option lists, complete selector entry handlers/UpdateLocalTrackSelectors/Show/ClearTransitionPending, and50-cycle distinct/same-reference A→B switches for success/failed result/fault/cancel. B restores eligibility before A settles; A never changes B's selected model/UI/open/dialog. B-current-switch remains disabled through A completion; B settlement releases selectors but retains pending transition; clearing the last transition restores Next. Terminal completion produces no re-enable. This closes the exact earlier PF1 counterexample without restoring its unsafe old finally.

FA1 uses full real child and base, independent transport/decode gates and bound-view PropertyChanged observers. Active/same-reference/distinct/fresh B controls publish normally. Fifty-cycle close conditions permit old internal detached notifications but require zero later bound-view/parent publication, and both context-set reentry callbacks observe terminal eligibility before attempting public work. Current child handled-error navigation remains usable with its previously loaded active child.

FA2 uses distinct cid/segment/payload gates, complete timer/node/load methods and old timer A held across node metadata/new initial B. Timer control-visibility counters still progress during metadata; old pool/segment ownership is cleared. B segment1 requests before A completes. A success/fault/cancel and same-generation non-acquiring callers cannot release B; manual skipped caller leaves settled/UI state alone. After B payload, A's resumed timer cannot recapture/render B. Positive current timer rendering, later segments, initial fault/cancel→manual retry, C superseding pending B and downloaded-local polling all execute. The service boundary models propagated task fault/cancel; the unchanged real PlayerVM's handled-empty-list outcome is not a new typed error API or transport-error classification claim.

FA3 tests50-cycle valid ended entry→blocked history→terminal/distinct/same-reference replacement followed by report success/handled failure/unexpected fault. Exact original item/duration/count remains, obsolete suffix event/open/index/loop/choice/auto-play/completion-message effects are zero, direct terminal entry is quiet. Active mode permutations retain ordered final event/no-subscriber message, next-part AUTO_NEXT/FromStart, single/list replay/wrap, manifest skip, interaction leaf/nonleaf and a still-current preference change during report. Unexpected propagated history errors are logged/contained; service-owned handled errors remain a separate boundary count.

Async-void observation is deterministic: OperationStarted/Completed, AsyncLocal tickets and synchronized callback queue; queue emptiness/task completion drive success. Bounded waits only report liveness failure. All started transport/decode/engine/segment/history gates are completed/faulted/canceled, modeled timers stop, and no pending job is used as root evidence. The prior explicit weak-root fixture is unchanged; residual tests add no GC/RSS claim.

## Other checks and scoped preservation

Missing/duplicate mandatory timer anchors were independently exercised against private modified input copies with the released extractor; each exits1 before compilation. Commands/logs are private `extractor-{missing,duplicate}.log` and fidelity JSON. `git diff --check` on only PlayerControl/playersess exits0.

The selected seven-stage results.json hash independently matches `149998259e72a551c26f16936dd97d2d0f86577a8b5dbe5ae2346b8c43792889`: PARTIAL_PASS, complete_required=false. No selected/full suite rerun was done by this reviewer. Nineteen actual accepted season source/parser/shell/fixture hashes, including all seven season1 fixture files, were independently compared to its released binding receipt and match; its141 original/497 lifetime result remains separately recorded. No common51/52, runner14, package15, source publication or moving whole-tree/API equality claim is made.

All scoped compiled/runtime-read sources and fixture/dependency hashes were rechecked after replay; the historical initial CHANGES review and accepted requirement bytes remain frozen. Only assigned re-review Markdown and exclusive private scratch were written. Fresh residual audit and graphics rebinding use disjoint review outputs; no other owner source was edited.

## External limits

Linux source/model proof does not run UWP/XAML/COM/native dispatch, codecs, actual decoded media, resolved Win2D/package/device behavior or subtitle pixels. Borrowed device/output and five owned graphics temporaries remain under the unchanged G1 span/fixture and separate reviewer. Native private commit/working set/GPU/handles, fifty watched-video reproduction and memory reduction remain unverified. Finite detached interaction work and legacy service result/error policy remain permitted. R4/adaptive/native-stream/display/SMTC/global/protocol/API work receives no expansion from this PASS. Root still owns the later stable common integration/publication/documentation/archive and external Windows gates.

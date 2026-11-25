# Residual player requirements review evidence

Current verdict: **PASS / RELEASED**, no requirements corrections. Sole private scope: `/workspace/work/video-memory/resources-residual-review/`. No implementation was performed. Frozen control source SHA256 `8682d1505752a2dfd99e835259d45429cf78ee3e358caf39553b1375a7d4a3cc`; HEAD `7356b94893377558191b0d70576a52bae704c1f9`.

## Read and bound inputs

Read ACTIVE residual-review-dispatch, original accepted R1/R2/R3/RR1/M1/G1 and terminal policy, frozen owning PF1 implementation review, fresh final audit and all three linked counterexamples. Read the released residual requirement/evidence in full and actual affected methods/consumers.

Private `binding-check.json` independently records/matches:

- All10 frozen player source/test paths against implementation-evidence/bindings.json.
- Full real InteractionVideoVM, IModules, PlayerVM history method, local coordinator and VideoDetail consumer source hashes against the author's recorded inputs.
- Twelve complete current control methods against actual source bodies/line/hash: SetPlayItem, SwitchLocalTrackAsync, UpdateLocalTrackSelectors, Show/ClearTransitionPending, ChangedNode, LoadDanmaku, SetDanmaku, DanmuTimer_Tick, GetPlayerInfo, Dispose, Player_PlayMediaEnded.
- Accepted contract/counterexample document hashes plus the released residual requirement/evidence/dispatch hashes.
- All four existing raw counterexample log hashes and their explicitly recorded meanings.
- Three historical player history fixture hashes, current separately accepted Season page/VM source hashes and the complete G1 renderer method hash for later preservation checks.

No owner scratch driver was invoked or mutated; they can rewrite their private proof inputs. No owning/full suite or unchanged released audit was rerun. Existing evidence is reused by exact binding, with source facts independently inspected. This review does not claim the future final safe four-group red→green fixture already exists.

## Counterexample assessment and contract fit

| Finding | Independently checked source/log | Accepted decisive requirement |
| --- | --- | --- |
| PF1/P1 | Complete actual switch disables selectors/Next; replacement clears the switching flag but never restores enabled state; stale finally correctly refuses writes. Actual selector helper does not set IsEnabled. My earlier source-bound targeted original/final proof remains unchanged: current source leaves False/False/False and desired-safe assertion exits1. | B becomes usable without A settling; B's own switch and current pending transition keep their independent restrictions; old success/failure/fault/cancel cannot modify B/shared model. |
| FA1/P2 | GetPlayerInfo binds real child before its GetNodes await. Actual Dispose leaves identities/contexts. Full real GetNodes publishes Info/List/Select/Loading after transport/decode; actual IModules.HandelError only returns/logs. Bound contexts survive close in all three recorded repetitions while parent online suffix correctly stays0. | Terminal synchronous detach before other teardown; finite detached old-VM work/notifications permitted. Bound old/current-view notifications0, active child publication positive, parent suffix still rejected. |
| FA2/P2 | ChangedNode changes generation/cid but retains old pool/list/lock. LoadDanmaku returns inside try if loading=true; its finally can release a same-generation lock not acquired by that invocation. Actual timer skips loaded segments and uses the pool. Recorded trace makes only old `1:2`, retains old cue/segment1 and rejects the stale response. | Reset/transfer complete node-owned data plus an acquired lease. New cidB segment1 must request/apply while A pending; stale/non-acquirer finalizers cannot unlock B. Initial timer pressure, retry/manual/current later segment and C replacement are complete-method controls. |
| FA3/P1 | Complete ended handler awaits ReportHistory, then reads live replacement list/index/settings and can emit AllMediaEndEvent. VideoDetail's real callback calls AdvanceSequenceAsync using its current sequence. Recorded valid-entry A→pending history→live B produces one callback observing B. | Capture/check original item+generation, retain A report and current active modes, reject obsolete suffix; async-void fault containment and deterministic completion observation are mandatory. |

Raw log bindings reused:

- PF1 `/workspace/work/video-memory/player-final-review/track-ui/final.log`: `93fc99e0ef31cc98dff0f5262dda828adab48c633fdf6d645c5fb91bc99ba00c`; desired-safe exit1.
- FA1 `fresh-audit/interaction-proof/run.log`: `83c1fa03f361777fa46220fce4b0952dd61b56fb7b7b4200cd8434f0cfac3447`; recorded discovery exit0 confirms unsafe close plus active/replacement controls.
- FA2 `fresh-audit/interaction-proof/node-run.log`: `fc88f04bf68c0b47e4e5315f8f0c2ab2c4f7de800a145036bf04d43a7063ad99`; recorded discovery exit0 confirms skipped current load and rejected stale response.
- FA3 `fresh-audit/ended-proof/run.log`: `4b53cfdd2e70ec0e93e55ac4ec0b25cdcd18e821707ee58958be2687d17bb873`; recorded discovery exit0 confirms active, replacement and close control-boundary callbacks.

The existing FA2 proof executes the complete real node/load/child chain but reads the timer as producer/consumer; it does not yet execute the complete timer at that boundary. The amendment explicitly requires the full timer in final regression. Likewise the previous inert GetNodes/ended/selector stubs were adequate for some parent-only tests, not these effects; the new separate full-child/residual fixture must close those gaps while preserving all existing39 cases.

## Important semantics checked

Terminal latch precedes detachment so ordinary reentrant context/selection notifications cannot authorize work. Required Unloaded detachment is not suppressed. Old detached child Loading/finally/error notifications are permitted; that is distinct from their publication while bound to a closed/current view. Current active interaction and actual ChangedNode navigation remain positive controls. No new VM cancel/generation API or assumption of arbitrary overlapping same-child latest-wins behavior is accepted.

The selector/Next policy composes facts rather than old snapshots: current switch owns its temporary disabling; pending page transition independently restricts Next. Source ShowTransitionPending/ClearTransitionPending are real competing writers and therefore part of the test/repair scope. Neither current switch settlement nor transition clear may release the other's restriction; empty/hidden options do not acquire a new invented global disable rule.

Danmaku requests need per-request cid/payload/gates. A single task shared across nodes would hide wrong ownership. A skipped same-generation loader must not clear another acquired lease. Complete timer tests must preserve original pre-await identity through resumed dispatch/render work, including terminal close and old-position pressure; reading B's current state after awaiting A is not authorization. Successful initial/current later segments and failure→retry controls prevent a repair that simply suppresses all loads.

Actual PlayerVM.ReportHistory constructs its request from the provided PlayInfo/progress before its await, handles normal errors internally and may show the existing reportErrors toast. The amendment preserves that service policy and A's report; it suppresses obsolete ended suffix/completion effects. It does not require zero service-owned logging/error handling from already-started work. Propagated unexpected errors at the touched async-void handler are contained/logged; still-current handled error may continue normal end behavior after revalidation. Current play-mode changes during the report remain legitimate. Interactive leaf/nonleaf history/action behavior, manifest skip, FromStart, exact next/loop/event counts, synchronous local save and checkpoint drain semantics remain unchanged acceptance.

## Custody and limits

The scope is PlayerControl-only plus additive existing playersess fixtures/extractor wiring. Real child/base/coordinator/history inputs stay read-only; optional generated boundary logger alias cannot change their method decisions. No graphics renderer/stub, Season, tooling, dependency, stage, protocol, native player/SMTC/display/stream/R4 or API product expansion is authorized. The initial owning implementation review remains frozen CHANGES until repaired final bytes are independently accepted; this requirements PASS does not erase it.

No source/test/tooling/main-document/index/ref was written; only these two review artifacts and exclusive scratch. All recorded frozen source/fixture/released-input bindings were rechecked before release. No whole-tree stability claim is made. Linux source evidence does not establish UWP build, resolved Win2D/package behavior, native cache/root counts, pixels, decoder/GPU/handles/RAM or fifty actual watched-video leak reproduction. Those remain the previously accepted external gates.

# Residual ownership amendment evidence and release

Status: **RELEASED for independent amendment review**. Requirements: [residual-requirements.md](residual-requirements.md). All findings are source correctness/lifetime failures, not measured Windows RAM attribution. Current executable source remains frozen; this author wrote only the two granted resource Markdown files and exclusive `/workspace/work/video-memory/resources-residual-requirements/` scratch.

## Input fidelity and commands actually performed

Read the complete relevant current control methods, full `InteractionVideoVM.cs`/`IModules.cs`, actual local-track coordinator and selector writers, PlayerVM.ReportHistory, actual end-event consumer/page policy, accepted resource/RR1/M1/G1 contracts and all four released counterexample records. The current10 player source/fixture paths exactly match `resources/implementation-evidence/bindings.json`; in particular Control SHA256 is `8682d1505752a2dfd99e835259d45429cf78ee3e358caf39553b1375a7d4a3cc`. Source/fixture/test integrity checks passed. Accepted/frozen requirements were not revised in place.

Private artifacts:

- `input-bindings.json`: all10 frozen owner input comparisons, accepted contract/counterexample hashes and additional real child/base/history/coordinator/consumer source hashes.
- `inspected-methods.json` plus12 `.cs.txt` copies: complete current SetPlayItem, switch, selector, transition, node/load/timer/info/Dispose/ended method source spans/hashes used for inspection. No method decisions were rewritten.
- `reviewed-counterexamples.json`: exact PF1/FA1/FA2/FA3 raw log text/hash and recorded result bindings.

Only source reads, SHA256 comparisons and scratch extraction were executed for this amendment. **No owning test/full suite or released audit was rerun.** Existing independently executed counterexamples are reused by exact source/log bindings. Their private drivers mutate their own old scratch projects, so this author did not invoke them under another owner's directory. Future final regression must bind its own unchanged safe fixture to frozen→repaired source; this evidence does not claim that future red/green already exists.

## Four findings and conservative disposition

| Finding | Released evidence reviewed against current source | Chosen amendment and decisive boundary |
| --- | --- | --- |
| **PF1/P1** | [Owning implementation review](final-review.md). Current switch disables three controls at2241–2243; SetPlayItem1724 clears `_localTrackSwitching` but not enabled state; old finally2309 correctly refuses B writes. Complete actual selector method2190+ never restores enabled state. Current-source desired-safe log ends `ASSERT_REPLACEMENT_TRACK_UI_ENABLED_FAILED`; original pre-R1 source passes only because old finally wrongly writes B. | Restore current eligibility from accepted replacement/current state, preserving stale-finally/model guards. Next also has actual ShowTransitionPending/ClearTransitionPending writers at1288–1313; compose both restrictions rather than letting one clear the other. Do not revert the generation protection to reproduce the old apparent UI recovery. |
| **FA1/P2** | [Full child proof](../audit-evidence/interaction-child.md). Actual GetPlayerInfo binds both DataContexts before real GetNodes await at2799–2804. Terminal Dispose4184+ leaves those bindings/identities attached. Three repetitions record `childBoundBeforeCompletion=True childBoundAfter=True childLoaded=True lateNotifications=Info,List,Select,Loading onlineCalls=0`. Active suffix1 and existing replacement detachment controls distinguish it. | Terminal parent detaches both views and child/item identities before other teardown callbacks/await. Explicitly permit finite detached GetNodes publication; no new child API/product file required. Full actual GetNodes and IModules must remain in the final test. A parent post-await guard alone cannot prevent mutations to an already bound real child. |
| **FA2/P2** | [Node/load proof](../audit-evidence/node-danmaku.md). Current ChangedNode2155+ advances generation and cid but keeps prior `loadingDanmaku`, pool and loaded list. New LoadDanmaku(1) returns at2520 while old request pending. Exact trace: new cid2002, only request `1:2`, old cue/loaded1 remain, stale response correctly excluded. | Reset/transfer the complete node-owned pool/list/load/initial eligibility. New cid segment1 is requested independently of old request; old/non-acquirer finally cannot unlock newer work. Keep the real rejection of the stale response. Timer initial-phase pressure must not recreate the skipped-load outcome. |
| **FA3/P1** | [Complete ended-handler proof/consumer](../audit-evidence/ended-history.md). Handler3927–4006 is current on entry, awaits actual history at3942, then reads live replacement controls/list/index. Raw trace active→one expected callback; replacement→one callback observing titleB; close→one callback at control boundary. Actual VideoDetail consumer1319–1323 advances its current sequence at1385+, without an A identity. Existing owning fixture had an inert ended handler. | Capture current item/generation/terminal ownership before awaiting and validate after it before suffix. Keep A's already-started history and existing save/drain/error policy. Do not emit obsolete completion/loop/next/UI work. Test full handler and active mode positives; this does not rely on an unproved obsolete native event. |

Current `LoadDanmaku` also places its `loadingDanmaku` early-return inside try while finally clears the flag for a current generation. Consequently a skipped same-generation caller can release a lease it did not acquire; the FA2 final lease acceptance explicitly covers this source-visible edge. It needs only the same local load-ownership repair, not a parallel scheduler or new network policy. No separate native/heap diagnosis is inferred.

### Child error/finite-work basis

Full real InteractionVideoVM.GetNodes57–95 sets Loading, awaits request and decode, updates Info/List/Select on accepted service data, catches through HandelError, and sets Loading=false in finally. Full real IModules.HandelError returns a network error value or logs another exception; it does not call Utils.ShowMessageToast or mutate a PlayerControl. Therefore detached completion/error logging can conservatively remain finite. New tests must distinguish internal old-VM notifications from publication while bound to a current/closed player view. No promise of cancellation or zero post-close child work is supported by this choice.

### History/error basis

Full real PlayerVM.ReportHistory61–87 creates A's history request before awaiting it and handles its normal transport errors internally. That method and history storage/drain sources are outside scope. FA3's change is the handler's obsolete suffix check, plus exception containment at the touched async-void boundary if a report unexpectedly propagates. Its successful/handled-failure active policy, reportErrors behavior and original synchronous final save/latest drain tail must remain. An end event valid on entry becoming obsolete at its own await is stronger evidence than the old generic suspicion about native callback lifetime.

## Exact reused log bindings and reproduction references

| Finding | Existing private raw log | SHA256 / recorded result |
| --- | --- | --- |
| PF1 | `/workspace/work/video-memory/player-final-review/track-ui/final.log` | Binding captured in this author's `reviewed-counterexamples.json`; owning commands.json records final source exit1, desired-safe assertion. Original/actual selector body binding independently retained. |
| FA1 | `/workspace/work/video-memory/fresh-audit/interaction-proof/run.log` | `83c1fa03f361777fa46220fce4b0952dd61b56fb7b7b4200cd8434f0cfac3447`; setup0/proof0 confirms unsafe counterexample plus controls. |
| FA2 | `/workspace/work/video-memory/fresh-audit/interaction-proof/node-run.log` | `fc88f04bf68c0b47e4e5315f8f0c2ab2c4f7de800a145036bf04d43a7063ad99`; setup0/proof0 confirms unsafe counterexample plus controls. |
| FA3 | `/workspace/work/video-memory/fresh-audit/ended-proof/run.log` | `4b53cfdd2e70ec0e93e55ac4ec0b25cdcd18e821707ee58958be2687d17bb873`; setup0/proof0 confirms unsafe counterexample plus controls. |

All three FA log hashes were recomputed and matched `/workspace/work/video-memory/fresh-audit/new-control-results.json`. Exact existing commands/drivers and boundaries are in the linked audit evidence; these0 exits are **not** safe-source PASSes. PF1 already has a behavioral desired-safe failure on this frozen source. The forthcoming owning fixture must assert safe results for all four, run all groups on frozen source despite failures, then pass on final source without changing assertions/stubs/tool inputs. Compiler/missing-anchor failures do not count.

## Preserved scope and limitations

No source grant is sought for InteractionVideoVM, PlayerVM/history, local coordinator, Season, graphics/Win2D, native engines, streams, display/SMTC or tooling. Exact original240 player checks,39 lifetime groups, five graphics groups and original141 season checks plus accepted M1 safety remain prerequisites. New tests must replace the inadequate child/ended/selector/load boundaries **only where proving their specific effects**; their existing caller controls and assertions stay meaningful. One existing PlayerControl/playersess writer is sufficient; main docs and later common integration remain separately owned.

The borrowed-device/output/temporary G1 ownership and inspected API contract, terminal/live/fresh owner policy, native resource uncertainty and actual Windows cache preconditions remain unchanged. Linux models do not decode fifty videos, measure private commit/working set/GPU/WinRT roots, certify pixels or determine native disposal timing. Native50-cycle reproduction remains external. R4 disposal-fault isolation and parked global/background work remain outside this amendment.

Custody released: only [residual-requirements.md](residual-requirements.md) and this companion evidence are new normative/discovery outputs. Independent amendment review must precede root's same-owner fix grant; owning and fresh re-review must close PF1 and all three FA findings before the stable common gate/publication.

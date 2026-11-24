# Independent final caller review — CHANGES

**CHANGES — FA-09/P2 remains open. FA-05/P1, FA-06/07/08/P2 and P2-CR1 are CLOSED on the final frozen bytes. The fresh review accepts the RF2 repair; its independent owning verdict remains in recovery/final-review.md.** The original FA-01/03/04 and P1-RF1 closures remain accepted. This reviewer did not author requirements or implementation. No new source grant follows from this finding.

Read repository/cloud instructions, memory INDEX/current checkpoint, the GRANTED final review dispatch, original requirements/reviews/amendments, revised caller amendment/PASS, initial/post-fix/final owning receipts, all prior fresh findings/closure, the CR1 actual-handler/official collection-contract proof, and the preserved FA-09 pointer/proof. Inspected complete final dialog/item models, helper/coordinator and recommendation VM, current/legacy scan/publication/observer/cleanup paths, actual playurl model/mapper/forwarder/DTO/login bodies, actual UI callers/XAML, permanent project/extractor/Program/stubs, immutable red/green source trees and raw logs. No released reproducer, owning batch, full suite, control or package gate was rerun during this continuation.

## FA-09 / P2 — a dialog exception permanently suppresses later logged-out login retries

Location: `src/BiliLite.UWP/Helpers/Utils.cs:240–248`, complete actual `ShowLoginDialog`. The helper sets its static `dialogShowing=true`, awaits `LoginDialog.ShowAsync`, then clears the flag only on the normal path. If construction of the async delivery or its awaited completion throws after the assignment, the flag remains true. Current `RecommendVM.Dislike:154–203` correctly contains that first exception and leaves the action/row unchanged. A later logged-out retry reaches the same helper, skips ShowAsync because the flag is still true and returns false. It cannot present the now-available login dialog; the caller reports `请先登录` and performs no action. The repaired FA-08 no-action-on-fault semantics therefore hold while the shared helper's retry usability fails.

The preserved bounded proof runs four controls and then the two-call continuation **in the same process**, using full current RecommendVM and the complete byte-exact helper body/backing field. Native LoginDialog delivery only counts calls, delivers success/cancel/fault and sets primitive account presence; actual product code decides the bool, row ownership and request. The fixture never reads/writes/resets the product flag and uses no reflection. It first proves cancel→later successful login and already-logged-in feedback, then delivers one dialog exception and a subsequent available success:

```text
PASS normal cancel leaves helper usable and row owned
PASS normal later successful login opens and performs action
PASS already logged in bypasses dialog and retains feedback fields
PASS dialog exception is contained with no action or row mutation
AFTER_EXCEPTION show_calls=3;requests=0;rows=1;logined=False
AFTER_RETRY show_calls=3;requests=0;rows=1;logined=False;toast=请先登录
FAIL later logged-out retry reopens helper and permits successful action
LOGIN_CONTINUATION failures=1;pending_deliveries=0;transfers=0;product_flag_reset=false
```

Successful compilation, **exit1 / four passing controls / one genuine retry failure**. This is a deterministic source continuation under a delivered exception, not evidence that a particular native Windows exception occurred. No transfer, held task or background observer exists in this focused proof; all deliveries complete.

Private proof survives at `/workspace/work/recovery-integrity/fresh-caller-final/login-continuation/`. Equivalent reproducible command, **not repeated** here:

```bash
DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet \
DOTNET_CLI_HOME=/workspace/work/maintenance-foundation/quality/cache/dotnet-cli \
NUGET_PACKAGES=/workspace/work/maintenance-foundation/quality/cache/nuget \
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/recovery-integrity/fresh-caller-final/login-continuation/LoginContinuation.csproj --nologo
```

Required minimal follow-up: root obtains a concise requirements amendment and independent PASS, then grants the original recovery owner only exception-safe **owned** show-lifecycle release in Utils and acceptance additions in the same recovery executable/seam. Clear the flag in finally only for the invocation that claimed it; preserve exception propagation to existing callers and existing true-success/false-cancel/already-logged-in/concurrent-call behavior. A non-owning concurrent call must not clear an earlier pending owner's flag. Require same-process exception→next successful logged-out retry, ordinary success/cancel, an awaited and/or synchronous delivery fault, and held-owner/concurrent controls with all gates drained. No credential, account parser, native login UI, schema or authentication-policy redesign is needed. Utils is outside the frozen nine-file grant: **no source expansion or mutation is authorized by this audit**.

## Final repaired findings

| Finding | Current actual-source assessment and bound observation | Disposition |
| --- | --- | --- |
| FA-05/P1 | Actual ParseJson keeps durl list order; exact PlayerVM forwarder returns that object; full dialog advances `i++` once per appended MultiFLV entry. Actual helper/SaveInfo operations and ordered VideoPath agree on `0.blv,1.blv,2.blv`, including differing wire-order values. SingleFLV/video-only DASH remain `0.blv`; DASH with audio remains `video.m4s,audio.m4s`. Headers, URL order, transfer group/cost and collision policies are unchanged and pass. | CLOSED. |
| FA-06/P2 | Actual empty-durl still maps to successful MultiFLV/zero entries, then the actual helper rejects it before creator admission, revision/evidence/generation changes or target writes. It clones the input, rejects the whole null/empty/invalid required list and validates the same transfer URI and existing child-name rules before its lease. No filtering/vacuous success is introduced. Coordinator completion additionally requires Required>0. Old partial bytes/settings/stamps/proof/recovery remain exact; failed row stays open/enabled. Rejected empty retains valid old cleanup authority. Later nonempty retry remains pending after one sibling and recovers only after all current required transfers succeed; stale cleanup remains unable to delete it. | CLOSED; positive-media and cleanup controls retained. |
| FA-07/P2 | Typed failure now sets `hide=false`, retains State99/exact error and sends `PropertyChanged("ErrorMessage")`, matching actual XAML. Mixed success stays discoverable while failed rows remain available; no success close/toast occurs. Explicit retry processes State0/99 only, skips State2 siblings and creates no duplicate successful operation. Eventual success, zero selection and preparation-exception controls pass. | CLOSED. |
| P2-CR1 | The full actual primary event captures an ordered concrete list of selected episode **objects** before its first await. Real unchecked clear/third selection/hide-preview during held info or preparation cannot change that invocation. Outer catch covers capture/iteration/discovery/reporting, attempts admitted-sibling discovery and useful failure feedback; finally restores primary enabled. No batching or exception policy is faked in wrappers. Permanent mixed/reselection/explicit retry/reporting-fault cases pass. Original exact CR1 driver changes escaped InvalidOperationException/enabledFalse/one admitted row to errors0/enabledTrue/two admitted original rows, with actual observers settled/validated cleanup/active0. | CLOSED for documented IList/iterator source contract; no native iterator/crash claim. |
| FA-08/P2 | The caller captures collection/row before login and now proceeds only when the complete actual helper returns true. Cancel/fault sends no action and mutates no row. The same collection/object must still be current before request construction and again after acknowledgement. Held-login collection replacement or selected-object removal sends no old request; held-ack same-idx replacement remains unchanged. Pending/error/no-progress state, empty removal and explicit append retry retain their current owner. Real success/feedback fields and already-logged-in bypass pass. Original complete login driver is identical across pre-fix red→repaired green. | CLOSED for caller contract; FA-09 separately covers helper continuation. |
| FA-02 / P2-RF2 | Both current and legacy scans record each enumerated exact episode path/stamp before metadata/media/size exits, carry those candidates with their staged title, and perform final no-await classification. Missing/unusable metadata, missing/zero media and races across cancellation/new pending preparation cannot lose known ownership merely because no row was constructed. Only an affected empty title is withheld; valid siblings remain and status retains cleanup explanation. Validated cleanup/current full positive-media completion still recover, while unrelated tolerant empty/legacy behavior stays unchanged. The exact original initial-zero driver is now titles0/episodes0/cleanup1 with explicit partial status, exit0. | Fresh assessment CLOSED; authoritative independent owning review is separate. |

For the held-login replacement case, the permanent fixture directly replaces the public Items collection with a different same-idx object while the actual helper is held. The separately retained real GetRecommend refresh path publishes its newly parsed collection through that same setter; source inspection confirms the pre-request check depends solely on current collection/object identity. Existing real-refresh-during-ack controls and all pending/error/no-progress paths remain. No substitute request/auth/ownership algorithm is present.

FA-01's complete service/LM02 source is byte-identical to its prior accepted repair and original exact page proof. RF1's ReconcileAbsent, Handel and DeleteCleanupAsync bodies are independently byte-identical to the accepted post-fix source, retaining failed-cancellation/source ownership. FA-04 GetProgress is byte-identical and remains total0→finite0. FA-03's accepted current-feed state/removal tail is preserved; the only action changes are pre-login target capture, the local bool guard and pre-request ownership check. The identical original208 prefix runs unchanged, aside from an additive primitive-hook reset line; every complete original test body/loop/assertion remains.

## Independent exact provenance and primitive boundaries

Read/hash-only binding command, exit0:

```bash
python3 /workspace/work/recovery-integrity/fresh-caller-final/bind-final.py
# source_count915; changed9; old_predicates208; red_additive PASS61/FAIL43;
# green PASS312/FAIL0; exact_slices16; unchanged_tools5073;
# FA09 controls4/failures1; no build, repro or gate rerun
```

Independent receipt `/workspace/work/recovery-integrity/fresh-caller-final/final-binding.json` SHA256 **bc93e352589b60855d84b19e52a250d4030913380afdad3a3fa2e74251a49ee9** verifies:

- All915 current inputs and immutable final-green inputs match final manifest **30c64c85a40d051722627977959043bbc02ffdc88ec79048f8c87006891d9b9a**. Exactly the nine granted caller paths differ from the915 baseline; every pre-fix product matches baseline. Every red-source input except the four final fixture files is baseline product code.
- Complete Program, Stubs, project and extractor bytes are identical in pre-fix red, repaired green and current workspace. Complete original208 setup/test bodies remain, with only new inert primitive-hook cleanup in Reset; old208 outputs are the same ordered PASS list on both sides. Added104 semantic predicates are43 genuine pre-fix failures plus61 controls, then104 PASS. Compilation failures/setup attempts are excluded. Final raw total is312/failures43→312/failures0. Selected g5 remains335 PASS.
- The permanent closure binds22 product source inputs and16 unique complete slices. Independently re-extracted each whole body/type from current source and verified the declared hash and exact single occurrence in red, green and FA-09 wrappers; all seven original slices retain their hashes. Whole actual dialog/model/VM/helper/coordinator/parser/request files are project links. No product naming, mapper acceptance, auth return, selected-batch, hide/error, generation/classification or ownership algorithm is implemented in stubs.
- Primitive seams deliver ordinary IList selection/generated-XAML fields, native dialog outcomes, storage/transfer timing/faults, request responses and observations. The exact forwarder uses passive delivery; the complete mapper/DTO/HandleUrl executes ordinary nonproxy URLs. Unexpected network-only CDN use throws a sentinel. Product helper decides URI validation, names/paths/metadata, preparation/observer lifetime and generation evidence.
- All5073 installed tool/package bytes match the released immutable receipt. Current runner/control/package consumers retain exact previously accepted hashes. Full original RF2/login/CR1 Programs and all their red/green input manifests/log hashes are independently reverified, including byte-identical driver bodies and explicit pre-fix exit1/repaired exit0 receipts. No redundant rebuild was required.
- Permanent held info/file/read/login/ack gates are tracked and completed. SettleCaller drains actual singleton observer rows; old208 cleanup uses real CaptureCleanup/DeleteCleanup over fixture scopes. The pending-generation race waits for actual completion proof before reset; CR1 driver explicitly reports actual-observers-settled/validated-cleanup/active0. The terminal owner login exception does leave product flag state, openly documented in its receipt; **that is not teardown evidence for login retry**, and FA-09 tests the missing continuation without resetting it.

Key retained artifact hashes:

| Artifact | SHA256 |
| --- | --- |
| Final owning red log | `e05d3d353ccc251b4b4a360ef39ee47d212865ee470522d8f4f79d5ea3a6e91d` |
| Final recovery green log | `ac7f840923eacb8e2598a0654d12fe1bab75b58ddf090e8f991b689af733fcfd` |
| Final selected results | `e49c1a9bf3226e91e87a673458695e089fccd84c7af058ae5becfcca0ce111a9` |
| FA-09 Program | `1490319d2b58da511b0dafe6ec815d5ff935544fe684d5bda1f3f1296a4f9039` |
| FA-09 inputs | `ffce5f6e266cd7baaa9c60ca7caf34297387036746dcd9b77091b5e75671cb6b` |
| FA-09 exact wrapper | `2ca7a42f4aed3a26fc0a482deb9284d0f0d1cbcdecf90d0f6a658df1a7be6118` |
| FA-09 run log | `33a0c18085f9b0cdd2fc5e56b4dc73784720853cb755c43a722a4ab7027dedb7` |
| Actual unchanged Utils | `55c71e2fd7df5ed1046f994ae38b22ddb25c12190d3f7d07e02eb0684ba295e3` |
| Exact ShowLoginDialog | `8e682965e4aebf1e2cb5781a688fa771f2d5388d67ff4dd790398f2aae6a1a68` |

## Structure, ordinary user recovery and remaining gates

Touched structure is coherent: selection/feedback remain in the real dialog/model, required input validation in the existing creator, exact classification/generation in the existing coordinator/VM, and recommendation action ownership in its existing VM. No orphan or duplicate product helper, copied business algorithm in primitives, new source registration, schema, scheduler, executable or net8-only product API was found. Existing coordinator/parser have exactly one UWP Compile entry each and direct real consumers. Common C#/UWP-compatible APIs used by the delta preserve existing namespaces/project ownership. The specific Utils lifecycle defect needs a small local finally; it does not justify structural or credential redesign.

README/features and existing entry flows establish the ordinary needs here: retry a failed recommendation request/action; download supported multi/single/DASH; keep failure rows available; select a stable batch; preserve current feed state during async login/ack; cancel/delete with clear confirmation and retained cleanup; keep valid local media/siblings discoverable; recover registry faults without losing saved bytes/grants. Repaired schedules now meet those inspected needs, but a failed login dialog still blocks later login-dependent actions until process reset. This is actionable source behavior and cannot be dismissed to meet the user's about90% goal. Existing broader browse/play/resume/import/export/bulk/settings/diagnostics work and maintainer device policy remain as previously assessed; this bounded audit does not reopen unrelated parked work.

Final selected run is **PARTIAL_PASS /2 logical and2 executable PASS /50 NOT_SELECTED /complete_required=false**. The original complete51/52 is pre-fix history. After FA-09's separately reviewed repair and both independent closures, root must activate one complete changed-source51 logical/52 executable gate, then exact publication/docs/ref/clean-tree reconciliation. Unchanged14-control/package15 proofs remain separate and reusable only by exact relevant input hashes. HEAD remains dev/e928bf0 with dirty/unpublished recovery bytes and an empty index; no final recovery commit or publication is claimed.

Windows/VS/UWP/XAML compilation/rendering/focus/Narrator, native BackgroundTransfer/file locks, restart/session/real login/media/playback, signing/package/device and actual hosted CI are unverified. Old hosted evidence cannot certify final source. No native prerequisite prevented this source assessment. This reviewer wrote only assigned final-audit/audit-evidence Markdown and exclusive private scratch; no product/test/main-doc/index/object/ref/API write occurred.

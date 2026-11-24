# Recovery final implementation review — PASS

**PASS — FA-09/P2 CLOSED; no open actionable scoped P0/P1/P2.** RF1/RF2, FA-02–08 and P2-CR1 acceptance is retained. The helper now releases only its owning show attempt in a branch-local finally while preserving construction/claim order, exception/cancellation/token propagation, current-account bool results and immediate nonowner behavior. A later same-process logged-out retry works without product-state reset.

[Final lifetime evidence](review-evidence/login-lifetime-final.md) independently binds all915 source inputs to `0a80ac1cf14e76c9274564a999a88522c23460be6a85e077e093d5e28cd55639`, exactly Utils/Program/extractor deltas from accepted312, identical old/fixed fixture, actual22-input/16-slice extraction and precise two-complete-helper-body allowance. All prior312/original208 complete predicates/order remain; genuine old-helper348/21 semantic failures become348/348 PASS, unchangedg5335. Held-owner/nonowner, normal success/cancel, synchronous/held fault and known-token cancellation retain natural release and later real-action usability; every native/acknowledgement gate settles. Structure and UWP-compatible product forms are accepted.

No probe/suite/control/concern was rerun for a label; accepted executions were reused after byte binding, including5073 unchanged tool files. HEADe928bf0 remains dirty/local/unpublished, index empty. Selected348/g5 is PARTIAL_PASS; root’s final changed-source51/52, separately reviewed exact publication/docs/ref/clean closure, actual hosted and native gates remain separate. Reviewer wrote only assigned review/evidence and exclusive private read/hash scratch, with no product/test/main-doc/index/object/ref/API mutation.

## Earlier caller review — historical, superseded above

# Recovery final caller implementation review — CHANGES / FA-09

**P2-RF2 CLOSED; P1-RF1 and FA-02–04 remain CLOSED. The frozen FA-05–08/P2-CR1 caller repairs PASS. Overall closure remains CHANGES for supported FA-09/P2 in the unchanged actual Utils login helper.** A native dialog delivery exception leaves dialogShowing=true; the next logged-out retry never opens the now-available dialog. The caller correctly sends no action on the original exception, but shared-helper retry usability requires a separate minimal reviewed amendment and exact source grant.

[Final owning evidence](review-evidence/caller-final.md) binds all915 inputs to `30c64c85a40d051722627977959043bbc02ffdc88ec79048f8c87006891d9b9a`, exactly nine authorized changes, identical final fixture red→green, all original208 predicates/seven slices, recovery312/g5335 and5073 unchanged tool files. The original RF2 driver is identical red→green; prior RF1/FA-03/04 acceptance is retained through exact complete-function binding. Current/legacy skipped episode stamps now survive every eligibility await/exit to the final publication check; valid siblings, cleanup and genuinely complete current nonempty replacement remain usable. Touched structure/UWP-compatible source forms are accepted, subject to the explicit native gates.

I independently read/hash-bound the existing fresh FA-09 same-process proof, with four passing controls and one genuine exception→retry failure, without rerunning it. Required next action: separately review/grant the original owner a Utils-only owning-flag finally lifecycle repair plus additive same-executable exception/retry/concurrency acceptance, preserving all312 predicates. No credential/parser/UI expansion is justified. See the exact source/proof hashes and actionable contract in the linked evidence and [fresh caller evidence](../audit-evidence/caller-final.md).

Custody resumed after reconnect as `recovery_final_review_continue`; source remained frozen. No source/test/main-doc/index/object/ref/API mutation or duplicate suite/control/concern run occurred. HEADe928bf0 remains dirty/local/unpublished. Selected312/g5 is PARTIAL_PASS; final changed-source51/52, publication/docs and actual hosted/native gates remain separate and inactive pending source closure.

## Earlier owning review — historical, superseded above

# Recovery post-fix implementation review — CHANGES

**P1-RF1 CLOSED; FA-03/04 CLOSED; FA-02 remains CHANGES/P2-RF2.** The same208-check fixture is genuinely red→green with every original139 predicate preserved, and the unchanged original RF1 driver independently passes against current source. One supported FA-02 edge remains: cancelling before media bytes arrive followed by failed cleanup and a completed scan publishes an empty completed title and no cleanup-pending status, because zero/missing-media early exits bypass the new classification.

Detailed finding, exact current18 recovery input hashes,915-input binding, independent commands/results, accepted structure/exception/UWP assumptions and limits: [post-fix evidence](review-evidence/post-fix.md). Return P2-RF2 to the original recovery owner within the already approved scan/classification scope, preserve all208 predicates, and prove initial-zero/missing-media remnant red→green plus successful recovery/sibling controls. No scope/schema/new-stage expansion or native prerequisite is needed.

Current full source manifest SHA256 `3cb4d3c1ca57b935a738ab882943f1086c6f280587c56d58c61ccf1667842ff7`; current DownloadVM `17ad1184406daa75b2ddbd0f476b631908a728314a0f3dd4dd03863226336740`. Recovery208/g5335 is local selected PASS, not a final common gate/publication. Initial common51/52 is pre-fix history; root defers the next complete run until source closure. HEADe928bf0 remains dirty/local/unpublished; native/hosted gates remain separate. No open actionable P0/P1 was found in this owning post-fix scope.

## Original pre-fix review — retained historical evidence

The original P1-RF1 below is superseded by the verified closure above; its old source hashes and common-gate statements identify pre-fix bytes only.

# Independent recovery implementation review — CHANGES

**CHANGES — P1-RF1: retry can delete files after a failed cancellation without cancelling the still-current transfer.** Return this finding to `recovery_executor`; no source or acceptance expansion is needed. No P0 or additional actionable P2 was established in this review. The existing 139-check recovery program and common51/52 gate pass, but do not cover this continuation after their existing cancellation-fault case.

## P1-RF1 — a cancelled CTS does not retry a failed cancellation callback

Source: `src/BiliLite.UWP/Modules/DownloadVM.cs:756–777`, especially the unconditional repeat `target.Sources[guid].Cancel()` at768, and retained retry at814–824. Source SHA256 is `3b76bf88b3b1413368eec75a9316c2b51cf7c8afee97fefd5d1f726d5728b364`.

Concrete user path: cancel an episode, the attached cancellation callback throws while its transfer remains pending, then confirm the retained cleanup retry. The first command correctly retains the failure and performs no storage deletion. However, real `CancellationTokenSource.Cancel()` sets its cancellation-requested state before invoking callbacks. A second call does not run those callbacks again. The retry therefore gets a successful no-op from the same source and proceeds to `PermanentDelete`, removes the recovery target and reports success while the same operation is still current and pending. A newly captured command against that still-current source has the same risk. Revision and GUID/source identity checks cannot distinguish this poisoned cancellation state.

I reproduced this against the **full current product DownloadVM** and the existing unchanged permanent primitive stubs. It uses the same `DownloadOperation.ThrowCancel` primitive already accepted by `plan/tests/recovery/Program.cs:237–238`, extending only its schedule through the actual `RetryCleanupCommand`. The CTS is the actual .NET framework implementation; no cancellation/retry algorithm is supplied by the new driver. No product/test file was modified. Command:

```bash
DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet \
DOTNET_CLI_HOME=/workspace/work/maintenance-foundation/quality/cache/dotnet-cli \
NUGET_PACKAGES=/workspace/work/maintenance-foundation/quality/cache/nuget \
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/recovery-integrity/recovery-final-review/cancellation-retry/RetryConcern.csproj \
  -c Debug --no-restore --nologo
```

Exit1 (successful build; deliberately failing safety predicate):

```text
FIRST failed_targets=1 deleted=0 token_requested=True transfer_pending=True
RETRY failed_targets=0 confirmation_count=2
RETRY deleted=1 transfer_pending=True cancellation_fault_still_active=True
ACTUAL_SOURCE_CANCELLATION_RETRY_UNSAFE=True
```

This is a deterministic allowed primitive failure schedule, not a claim that Windows will permit deletion of an open native file. Relying on a possible file lock does not satisfy requirements §4: “A cancellation failure must not proceed to deletion while that transfer may still write.”

Required correction: preserve per-GUID knowledge of a failed cancellation attempt while that operation remains current. A retained retry **and a fresh cleanup command** must fail closed before folder lookup/deletion until that operation has settled/disappeared, or use an independently supported cancellation operation whose success is established. Do not treat `IsCancellationRequested` or a repeated no-op `Cancel()` as success, replace the source already attached to the observer, recapture newer work into old permission, or wait indefinitely for all transfers. Keep per-target partial-cancellation reporting truthful if one sibling was cancelled before another failed. Add the exact existing-fault → retained-retry and fresh-command cases to the one permanent recovery program, preserving all prior139 predicates; show this same bounded driver red→green and safe cleanup after the faulted operation settles. Then run owning recovery/g5, applicable controls if tooling changes, and the common gate on the changed exact inputs under root's coordination.

Scratch evidence is `/workspace/work/recovery-integrity/recovery-final-review/cancellation-retry/`:

| Artifact | SHA256 |
| --- | --- |
| `Program.cs` | `3ed0d172eadec76bef86f14225144957c656da983e82df19ef91a0e3f2ea9600` |
| `RetryConcern.csproj` | `053e79a3af412942fadefddba0fa25aa47ff1337364e5470506e7ddd16a51af2` |
| `inputs.json` | `1e85d8cfc65c5b08b3076ef06dc347db06b8246112a52ee56111a0b053a62f87` |
| `run.log` | `b08b136373ac68d78a59b200855af61c9f52d2457e05c03bfe5838960d4948fc` |

The driver settles its operation at teardown. Its copied `Stubs.cs` and generated exact-source wrapper match current permanent inputs; all linked full product source paths are explicit in its project. `inputs.json` records those bindings. The observed compiler warning categories are the same inherited model/HttpResults/RemoveItem/RelayCommand warnings recorded by the common verifier.

## Accepted implementation and evidence

Read repository/cloud instructions, memory INDEX and relevant decisions/pitfalls/run, round plan/inventory/ownership/execution dispatch, revised requirements/review/P1-RV1 revision, executor execution/red-green/input bindings, current product diff/full VMs/helper/coordinator/parser, both actual completed-delete handlers and callers, the permanent project/driver/stubs/extractor, original frozen17-check and six-check assertion bodies/logs, and full integration/control receipts. Reviewed requirements SHA256 `f25b1855591b58b215e2b3e1726360670bf8b62395502216403a6b12302cbfbf`; requirements review `2688ea67649e052da2a60cc974896842675a7bc3e030f276921b6d924fae76b6`.

- Recommendation state now stages required mapping before publication, preserves exact existing collections/banner on failure, keeps accepted continuation separate from a failed request, distinguishes initial/refresh/append retry, and closes null/empty command and initial single-flight failures. Actual ApiEnvelope/HttpResults decoding, strict integer code, optional-banner tolerance/salvage, feedback/dislike builder selection and code0/no-data acknowledgment remain source-owned. XAML binds persistent status/retry/automatic eligibility; the top-button body is guarded.
- Completed-library required root failure retains prior rows and resets busy in finally; optional disk/legacy/child errors produce a partial state while valid rows survive. Actual observer CTS identity is GUID-bound across scans; late retirement is GUID-owned. Season in-progress uses CID while completed EpisodeID remains unchanged.
- P1-RV1's creator/deletion races are closed in the inspected paths: actual AddDownload captures a deep value copy, validates children before target mutation, acquires its episode lease before parent/episode preparation, awaits every required child/metadata/SaveInfo settlement, dispatches StartAsync under ownership and detaches only media completion. Snapshot ownership is released before confirmation; destructive ownership covers final revision/reconciliation/cancellation and both storage awaits. Every cooperating PermanentDelete caller uses the same episode/subtree coordinator, including completed title deletion of unseen descendants. New same-CID GUIDs have separate actual sources. The new P1 above concerns cancellation failure state, not a reopening of the accepted path admission/revision design.
- The permanent batch links full current VMs, helper, coordinator, parser, envelope/parse result, models and request builder. Its seven generated slices are unique exact source blocks for real decoder/request/scalar/page/season/top-button behavior; no copied recovery/ownership/parser algorithm was found in primitive stubs. Original13-red/four-control and creator-five-red/lifetime-control evidence remains available, and permanent predicates retain those behavioral requirements while adapting the repaired public seams. The final optional-banner correction has its added139th predicate.
- File/function structure remains within the reviewed ownership: one feature parser, one path coordinator, one recovery program, two explicit UWP Compile entries and existing VM/page callers. Searches found consumers for both new helpers and the cleanup APIs, and no duplicate path-ownership or retry algorithm in stubs. The new product code uses existing UWP-compatible C# patterns and framework APIs; no net8-only product convenience API was identified. XML/source wiring checks establish neither XAML compilation nor rendered binding correctness.
- Runner changes add exactly one real `batch:recovery`, current51 logical/52 executable, retaining historical50/51 and selected `PARTIAL_PASS` semantics. The runner/control diff changes only the required inventory/count/text expectations; all fourteen failure/missing/deadline/signal/teardown/log/protocol/selector controls remain. No original owning batch assertion was removed.

I independently rehashed all915 current source/tooling/wiring inputs against integration's `source-before.json`: **915 match; changed=[]**. All18 owned recovery inputs in [verification recovery binding](../verification/evidence/recovery-inputs.md) match. Important bindings include coordinator `1794aa73628d063b20489e597c35f8115174309331b04f50d89d2fd2f3e14947`, helper `8aef39bca29b3aebf0954ad66a46326287e37cdbb2b53413540d1b7d2069d2cf`, RecommendVM `aba526dc929b08fa88ac187bc64399b872c9b6b8ee219036f91a64ee73e83020`, parser `fae8ec36361069bbb130557e8b8230b01a0c68dbe81689ada9a9b5da09b57f2d`, recovery Program `eb755fe0cbbd7e25f577851467bd2f38a717be834fab7688c2a0289b7a2f9688`, and stubs `7c4041d20520641b3438e289109eff93d775e1f142eed3ae9b59701a466c2aa8`.

Reused the exact completed integration run; no redundant full suite was run for this label. Raw `aggregate/results.json` confirms all52 rows PASS,51/51 logical, complete_required=true, and recovery log ends139 checks/failures0. Current controls are14/14 PASS. Exact bindings:

| Artifact | SHA256 |
| --- | --- |
| common `aggregate/results.json` | `d18fee55c607410264c870155f83e1ab8a34ea728c6f646ecc212705857b4d10` |
| common `source-before.json` / identical after | `42a24dba9e30d91308ce174f5a66a66fa87177550fced38bcda02d172cd4f91e` |
| common `tools-before.json` / identical after | `66f538a02d410e16fb24e6ac8024606e5112ef1bda1eb5e674c996affcc68267` |
| current `controls.json` | `427d42beffae5b83956bd731a96f7474740f3b916812dfefcaf2de171600448e` |

Raw common artifacts reside at `/workspace/work/recovery-integrity/final-verification/gate-20261124T175239Z`; controls at `../controls-current-20261124T1753`. The verifier's source/tool stability, generated-output Git-revision explanation and warning classification are consistent with these receipts. The current common PASS applies to the inspected bytes and existing assertions; P1-RF1 requires new source and new verification bindings before implementation PASS.

## Publication and platform gates

At review, HEAD is published storage receipt `e928bf0e818869c03485f8ef6f31fa47e98ac0f0`; recovery is **locally uncommitted/unpublished**, with truthful dirty provenance. Empty cached path output confirms this reviewer did not stage anything. The ordinary transport preflight failed before recovery commit, as recorded in execution/commit-window; remote publication remains a separate root-managed gate until actual supported publication exists. Do not call the old HEAD a recovery source commit or claim clean-at-HEAD/hosted success. If identical bytes later publish, bind the immutable SHA separately; the P1 correction will necessarily require new hashes and owning/common verification.

Windows `-Fast -NoVersionBump`, UWP/XAML compilation/rendering, focus/Narrator, native BackgroundTransfer cancellation/file-handle behavior, locked-folder cleanup and real session/media flows remain unavailable follow-up prerequisites. Concrete touched-flow checks are in requirements §6; historical accepted device policy is preserved. No native or successful startup claim follows from this Linux review. Reviewer changed only this final-review Markdown and scratch concern artifacts, with no source/test/main-doc/index/ref/remote mutation.

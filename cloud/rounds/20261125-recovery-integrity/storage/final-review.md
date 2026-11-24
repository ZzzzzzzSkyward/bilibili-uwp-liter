# Storage implementation review

**PASS — source implementation and exact local regression evidence.** No unresolved actionable P0/P1/P2 finding in this storage scope. S1–S4 are repaired within the reviewed process-local contract. This does not certify Windows/UWP/native behavior or close the round's separate publication/documentation gates.

Reviewer: `storage_final_review`. Reviewed source commit **`bdba9ebcddb3de146c4fdf92919c5a3c57eaf90f`**, evidence-only receipt **`e928bf0e818869c03485f8ef6f31fa47e98ac0f0`**. Inputs: instructions/CONTRACT/REPO and memory INDEX/current-round memory; [requirements](requirements.md), [requirements review](review.md), [discovery evidence](evidence.md), [execution](execution.md), [red/green evidence](implementation-evidence/red-green.md), [ownership dispatch](../execution-dispatch.md), [inventory decision](../inventory-decision.md), [final review dispatch](../implementation-review-dispatch.md), [common execution](../verification/execution.md), [common evidence](../verification/evidence/common-gate.md), and [remote verification correction](../remote-verification.md).

## Acceptance assessment

| Contract | Source and behavioral assessment | Result |
| --- | --- | --- |
| S1: detached values and ordered persistence | `VideoPlayHistoryHelper.cs:28–112` owns initialization, dictionary mutation, cleanup and snapshot capture under one monitor. `QueueABPlayHistoryWrite` copies every key and each non-null entry's two primitive values, preserving stored nulls. Its task tail is assigned while that same monitor is held, so each admitted snapshot awaits its predecessor before off-thread real SettingHelper serialization/Save. Later entry/key mutations cannot alter that snapshot. No monitor spans an await. | PASS |
| S2: first-use and delayed startup Load | Find and Set initialize the saved cache before use; Load shares that initialization flag and cannot reload over accepted memory. Cleanup preserves the existing max200/key-removal policy and joins the same write queue. The stage's first helper access is Set, with durable old/null entries already seeded. The held first Save, delayed Load, second Set and final JSON assertions establish old-plus-new retention. | PASS |
| S1/S2 compatibility and failure recovery | Public methods, cid/episode keys, entry properties/default B, null removal and live returned-entry identity remain. Actual PlayerControl cancel-B mutates its live entry without Set; that behavior is preserved. A failing primitive Save leaves durable JSON intact, logs the failure, retains accepted memory and permits the next queued save. Public async-void Set still catches its own failures. Cleanup saves also run off the synchronous caller. | PASS |
| S3: registry transaction ownership | `LocalMediaLibraryService.cs:72–118` acquires one static async gate before reading the registry and holds it through provisional grant, latest-registry mutation, the existing fixed-temp staged commit, commit marker, retirement and rollback. Release is unconditional in finally. Manifest/asset validation remains outside this critical section. The held-A/B/list tests establish that a second transaction cannot read or stage early; both distinct successful imports and exactly their reachable grants survive. | PASS |
| S3: replacement and failure semantics | Existing schema/file name/token format, ordinal-ignore-case id-plus-root identity and typed stages remain. Same-root replacement commits before retiring prior grants; different id/root combinations remain distinct. Partial staging-write, pre-replace and registry-read faults preserve committed bytes/prior access, release ownership and allow retry. Corrupt JSON remains typed `registry-read` for import/list/reopen without overwrite or a new grant. Broken entries remain available for repair. | PASS |
| S4: admitted reopen and readers | List maps detached rows under the shared gate. Reopen holds it through registry read, selection **and awaited GetFolderAsync**, then releases before manifest/asset work. Separate read/folder holds prove replacement waits and the admitted reopen succeeds; the manifest hold proves replacement may proceed after acquisition. Completed replacement still invalidates old/unknown tokens with `access-restore`. No nested gate acquisition exists. | PASS |

The whole source diff is bounded to the two existing owners. The old ungated registry-read name has no remaining reference; its clearly named private replacement has exactly the three gated callers. All new history helpers are used through the common state ownership path. There is no duplicate persistence/token algorithm, orphan product file, new public/test-only product API, schema migration, new stage, global settings change, or media-file write.

## Fixture fidelity and genuine red/green

Inspected the complete actual helper, SettingHelper and registry service; the new history and LM02 fixture bodies; the original playersess Program/checkpoint assertions and extraction script; and unchanged LM03/LM05 assertion bodies and compile inputs. The playersess project links the **whole actual helper and SettingHelper**, using Newtonsoft13.0.3. Legacy LM02 compiles the whole actual service with real manifest models/loader/validator. No extraction replaces either repaired storage algorithm.

History substitutes contain only DTO shapes, logging and a primitive key/string store. The actual Newtonsoft trace callback pauses serialization rather than serializing substitute business state. The completion SynchronizationContext observes the real async-void Set; it does not implement persistence. Registry hooks only delay/fail/trace primitive I/O/FAL operations; defaults are inert. The access-list lock makes that primitive dictionary safe and does not serialize registry transactions for the product. Pre-replace faults occur before the Linux stub's delete/move; native atomicity is not inferred.

Independently compared all six final added/modified fixture files with `red-tests/`: **byte-identical**. The baseline helper/SettingHelper/service and four saved old test inputs match immutable `077ef35` Git objects. Original playersess Program is byte-identical after removing its single added invocation; original LM02 Assert statements remain equal/in order and its original helper tail is byte-identical. The checkpoint extraction script, HistoryCheckpointProbe, LM03, LM05 and legacy run.sh remain byte-identical to baseline. The existing checkpoint extraction still selects five fields and four unique complete actual methods and retains its fail-fast guard.

Read the actual red/green logs under `/workspace/work/recovery-integrity/storage-execution/`:

- Identical playersess fixture against original product: exit1, **7 failures /240 assertions**; fixed product: exit0, **240/240**, including the original217 and new23 checks.
- Identical LM02 fixture against original product: nonzero exit134 after **17 additive failures**; fixed product: exit0, all **50 additive checks** plus the original assertions pass. The original typed rollback/read/replace controls also pass on red source. Discovery's separately retained stale-writer/lost-import acceptance reds corroborate the interleavings.
- Unchanged LM03 and LM05 shared-stub consumers: exit0; LM05 retains22 checks. Their only logged warning class here is existing CS1701 Newtonsoft reference unification.

Event gates use explicit entry/release signals and bounded waits, without sleeps or live service/network operations. Held operations release in finally and initiated operations are drained before hook reset/fixture reuse; LM02 finally resets hooks, clears grants and deletes its temporary fixture tree. History drains async-void completion and restores serializer settings/hooks. The inspected successful/red paths leave no pending writer or held registry operation.

## Independent byte binding and common gate

Reviewer independently rehashed **all30 storage inputs**, **all915 common source inputs**, and **all5073 exact installed tool/package files**: zero missing paths or mismatches. Common before/after source manifests are identical. Each of the eight owned product/test paths equals both its source-commit and receipt Git blob and the executed common manifest. Primary SHA256 bindings:

| Input | SHA256 |
| --- | --- |
| `VideoPlayHistoryHelper.cs` | `1d1ffb66031083e35a58bc90a05f9ae529319b0b9ee0b136738140228349961e` |
| actual `SettingHelper.cs` | `64983dea3c3f861214401aba4138bcdff6184c3e1cc73155d217db120a519f69` |
| `LocalMediaLibraryService.cs` | `335d35363f65fa03e57fc94193ce9e2c08aafd58cea83a40e24a0833e19a2dd0` |
| `HistoryStorageProbe.cs` | `b1fb6568851ab4c15b689d4fa89af1a121a59eb6b0a8a05568758f438367ad1f` |
| `HistoryStorageStubs.cs` | `e865e8a598ac7e5b5b391e9dd63a131f9d0e7c3ad3305d5536c0caec37432acd` |
| `LM02.cs` | `d558e088fc9f20d28712af97c0a6725ebc1e2bf948cba6fac9c3671d93ca6b6b` |
| shared `LocalMediaStorageStubs.cs` | `a1c38806369b348a089032cc559b165cd941b4fb46a8c242b58024456e2c9caf` |
| playersess project | `044d66d18456b2599a7e24a22a608ab285b2ba16f28a68220522f531a682037c` |
| playersess Program | `fdd1850ecbe5fbea500ffff4cbc41f375144209a8ce0d24ef39c8a6a92715838` |

Evidence SHA256: storage `green-inputs.sha256` **`f4424094fd7416b3a6f337743358100377c1989d01b1f7fdd68ab4d8497db99f`**; `red-inputs.sha256` **`cae6064b3bee766f0cd805fdf7b63ca08edbd13128ac8e5e69496df89e88a1fc`**. Playersess red/green logs respectively **`7ca504471e9a0e2b2262df19df8b6f48a0315ea84ba3f4d85d31f43aa4d50f7d`** / **`7a40495a5957fa40c3775006db4657276aca59030388129c28b9867705fa2490`**; LM02 red/green logs **`570342e4030cdce50ab64ba408f47c7a3aa6585a94476d4bd082bd73b0a1d2f0`** / **`7a827149542814728dc8d4b4f0916586a84e4843b33efdfcf2e639b3f477e31c`**.

Reused the exact completed common execution, with no unnecessary new suite run: `gate-20261124T175239Z/aggregate/results.json` has SHA256 **`d18fee55c607410264c870155f83e1ab8a34ea728c6f646ecc212705857b4d10`** and reports finished/PASS, **51/51 logical /52/52 executable**, complete_required=true, all52 selected PASS, no inventory/prerequisite errors or other status. Inspected the actual playersess/LM02/LM03/LM05 rows and logs, all exit0 without truncation. Current14/14 control cases also PASS; controls SHA256 **`427d42beffae5b83956bd731a96f7474740f3b916812dfefcaf2de171600448e`**. Historical50/51 remains provenance, not the current denominator.

Common source manifest SHA256 **`42a24dba9e30d91308ce174f5a66a66fa87177550fced38bcda02d172cd4f91e`** and tools manifest **`66f538a02d410e16fb24e6ac8024606e5112ef1bda1eb5e674c996affcc68267`** bind the unchanged SDK8.0.427/runtime/ref8.0.33 execution. The common gate truthfully ran at HEAD e928bf0 with **dirty=true**, including recovery's11 modified/seven new inputs. It is not a clean/final published recovery-HEAD claim. Expected gRPC informational-version-derived outputs are documented separately by the common verifier.

## Structure, publication and remaining limits

The new production constructs—monitor, Task tail/Task.Run/ConfigureAwait(false), LINQ value copy and SemaphoreSlim.WaitAsync/finally—fit the project's existing C#/UWP API level. No new modern test-only API appears in product code. Existing UWP Compile entries for both storage files occur exactly once; no storage XAML binding or product project addition is necessary. Test namespaces keep checkpoint substitutes separate from real `BiliLite.Helpers`/`BiliLite.Controls`. File and responsibility boundaries are coherent.

Read-only reviewer checks confirm local HEAD and origin/dev=e928bf0, empty index, and clean exact storage source/test/execution-evidence paths. Actual server publication is supported by the executor's push receipt and root's independently documented connected Git ref read; this reviewer did not repeat the failing terminal authentication operation or mutate refs. Global worktree dirtiness is intentional recovery/root work, and this review artifact is an additional owned output. Only this final-review Markdown was written by the reviewer.

The later single documentation owner must synchronize reusable main-doc counts/status; old reusable50/51 text is not treated as current completion. Storage has no remaining source repair request. Recovery publication, final tree binding, main documentation and round/ref/worktree closure remain root-owned separate gates.

Windows/VS UWP/XAML compile/rendering, native FutureAccessList revocation/folder-object lifetime and staged replacement, actual multi-tab/restart persistence and A–B playback controls remain **unverified platform prerequisites**. The recorded Linux exact toolchain has no Windows UWP workload; no native build/device test was run or passed here. Next native action is the documented Windows compile followed by parallel import/reimport/admitted-reopen, restart and A–B control checks, under the existing maintainer device policy. No cross-process/crash-consistency or hosted CI claim is made; old hosted run169b2b0 cannot certify this source.

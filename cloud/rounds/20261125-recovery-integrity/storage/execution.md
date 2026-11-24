# Storage execution

Current amendment status: **FA-01 LOCAL PASS, final bytes frozen/released for re-verification, unstaged and unpublished**. Fix-only review-fix-dispatch grant changed only service entry validation and additive LM02 checks. Same final fixture:214 genuine red predicates on pre-fix source →653/653 additive green checks (50 prior +603 FA-01); original LM02 bodies preserved. Exact page-refresh composition also red→green with one typed log/toast and retained rows/bytes/grants. Source SHA256 `28f03d444c40b79da8fd93f034f00ef6be5f2265355451799d7807bdf6aff514`. See [FA-01 evidence](implementation-evidence/fa01-red-green.md). No index/ref/auth/publication operation was performed. Root's common gate and independent FA-01 re-review remain pending.

Original S1–S4 publication receipt follows (historical source/receipt SHAs do not include this unstaged amendment):

Status: **source/test PASS and published; final evidence-only receipt**. This is the final owned edit. Source/test/evidence/index ownership releases after this receipt is pushed and HEAD/origin/dev/actual remote equality is verified; the final executor response certifies that completed release. No further receipt edit is required. Baseline product source `077ef35ae194d3ae85aa0621daeac07dc29e29ab`; startup `git pull --ff-only` succeeded while preserving declared parallel dirty paths. Source dispatch and independent requirements review authorize only the paths below. See [implementation evidence](implementation-evidence/red-green.md).

## Implementation and exact scope

- `src/BiliLite.UWP/Helpers/VideoPlayHistoryHelper.cs`: first-use initialization and dictionary mutation/snapshot capture share one state monitor; delayed startup Load cannot replace accepted current-process state. Persistence copies every key and non-null entry's PointA/PointB, retaining stored nulls and live cache entry identity. One FIFO Task tail orders off-thread serialization/Save, including cleanup; each write handles/logs its failure before its successors proceed. Public async-void Set compatibility remains exception-safe. Existing key/default/removal/max200 behavior and actual SettingHelper remain unchanged.
- `src/BiliLite.UWP/Services/LocalMedia/LocalMediaLibraryService.cs`: one static async gate owns registry read/modify/provisional-grant/staged-write/retirement and rollback. List readers share the gate; reopen owns registry selection through awaited grant-to-folder acquisition, releasing before manifest/asset validation. All paths release in finally. Existing staging path, schema, identity rules and typed stages remain.
- `plan/tests/playersess/PlayersessProbe.csproj`, `Program.cs`, new `HistoryStorageProbe.cs`, new `HistoryStorageStubs.cs`: extend the existing executable with whole real helper + SettingHelper and Newtonsoft13.0.3, primitive storage/logging/DTO fixtures and completion-observed async-void operations. Original Program is byte-identical after removing the single added invocation; checkpoint probe/extraction guard remain unchanged.
- `plan/tests/LM02.cs`, `plan/tests/LocalMediaStorageStubs.cs`: additive registry concurrency/reopen/fault assertions; primitive hooks default inert and reset between cases. Original LM02 assertions and helper bodies remain intact. LM03/LM05 shared-stub consumers pass unchanged.

No new stage, product source file, product project entry, global runner, main document, workflow, package/identity/live data or protected asset change. Other owners' files remain untouched.

## Verification

Exact toolchain: `/workspace/work/maintenance-foundation/quality`, SDK8.0.427/runtime/ref8.0.33. Raw logs and immutable whole-source/test snapshots: `/workspace/work/recovery-integrity/storage-execution/`.

| Command (`UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality`) | Unchanged-product red | Fixed-product result |
| --- | --- | --- |
| `bash plan/tests/playersess/run.sh` | exit1, seven new storage failures; original217 assertions preserved | exit0, **240/240** (217 original +23 storage) |
| `bash plan/tests/run.sh --stage LM02` | exit134/nonzero after17 new failures; all existing checks run first; typed rollback controls pass | exit0; original plus new concurrency/reopen/failure checks PASS |
| `bash plan/tests/run.sh --stage LM03` | existing consumer unchanged | exit0 |
| `bash plan/tests/run.sh --stage LM05` | existing consumer unchanged | exit0,22 existing checks |

`git diff --check` for all owned paths PASS. Assertion-preservation script PASS. All30 final whole-source/test input hashes verify. Settings/load tests use real SettingHelper/default Newtonsoft serialization; trace gates only pause that serializer. Every held operation is released in finally, initiated Tasks/async-void operations are drained, storage hooks reset, and registry temporary fixture directories deleted. No sleeps, live network or polling assertions.

Selected success is **partial coverage** of the global current51 logical/52 executable inventory. Root's final integrated gate and independent implementation review remain pending. No Windows/UWP/native/session/hosted PASS is inferred.

## Publication and limits

Root granted `storage/commit-window.md` after boundary source1b6af65/receipt9beadcc publication. Exact tested input hashes still matched before publication (`prepublication-inputs-check.log`). The cached list contained exactly eight assigned product/test paths plus these two owned Markdown artifacts; cached whitespace passed. No recovery/root/other-owner path was staged.

Source commit: **`bdba9ebcddb3de146c4fdf92919c5a3c57eaf90f`** (`fix(storage): serialize history snapshots and library transactions`), pushed to dev. Independently read local HEAD, origin/dev and actual `git ls-remote origin refs/heads/dev`: all three exactly matched that SHA. Index and every owned source/test/evidence path were clean after source push. The final receipt changes only this file and implementation-evidence/red-green.md; its commit is externally identified in the final executor response to avoid a self-reference/dirty-receipt loop. Source/test hashes and targeted results remain unchanged; no repeated product test is justified by evidence-only edits.

After final receipt push, verify exact HEAD/origin/dev/actual remote equality and empty index/clean owned paths, then release all ownership. Independent implementation review and root's common integration gate remain pending.

Windows VS/UWP compilation, native FAL/replacement semantics, real multi-tab/restart persistence and A–B playback controls remain unavailable. Next native action: compile on Windows, exercise parallel imports/reimports/reopen and restart/A–B controls. This source repair claims process-local ordering/ownership, with no cross-process or crash-consistency guarantee.

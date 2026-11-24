# Storage execution

Status: **implemented; owning checks PASS; root storage publication window GRANTED and active**. Product/test ownership retained through source and final receipt publication. No ownership release yet. Baseline product source `077ef35ae194d3ae85aa0621daeac07dc29e29ab`; startup `git pull --ff-only` succeeded while preserving declared parallel dirty paths. Source dispatch and independent requirements review authorize only the paths below. See [implementation evidence](implementation-evidence/red-green.md).

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

Root granted `storage/commit-window.md` after boundary source1b6af65/receipt9beadcc publication. Exact tested input hashes still match. Stage only the exact owned argv list, inspect cached names/whitespace, commit/push dev, verify actual remote SHA, then publish the final evidence-only receipt. Source/test/index ownership remains held until both pushes and remote verification complete.

Windows VS/UWP compilation, native FAL/replacement semantics, real multi-tab/restart persistence and A–B playback controls remain unavailable. Next native action: compile on Windows, exercise parallel imports/reimports/reopen and restart/A–B controls. This source repair claims process-local ordering/ownership, with no cross-process or crash-consistency guarantee.

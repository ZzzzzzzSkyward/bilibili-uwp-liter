# Storage integrity requirements

Status: **ready for independent requirements review**. Round: 20261125-recovery-integrity. Owner: storage_requirements. Source: `077ef35ae194d3ae85aa0621daeac07dc29e29ab`. Date: 2026-11-25 Asia/Shanghai. Evidence and commands: [evidence.md](evidence.md).

## 1. Findings and bounded objective

The two prior P2 hypotheses are now confirmed by **whole actual-source** scratch probes, with two acceptance executables genuinely exiting1 on current code. The practical need is to retain a user's latest A–B setting and every successful local collection import, and to permit a concurrent open to finish acquiring its folder before a replacement retires its grant. Treat all four as **P2 confirmed source integrity/recovery failures**, not a claimed Windows/device reproduction:

| ID | Confirmed failure | Minimal repair |
| --- | --- | --- |
| S1 | A delayed older A–B Save overwrites a newer saved checkpoint; dictionary/value aliases expose pending serialization to later mutations and dictionary-enumeration exceptions. | Detached value snapshots plus one ordered, exception-safe write path. |
| S2 | A checkpoint can be accepted before delayed startup Load; Load then replaces new memory and the pending early write can remove old persisted entries. | One initialized cache before the first Find/Set; startup Load cannot replace accepted current-process state. |
| S3 | Two valid imports read the same registry; both return success, but the later stale commit loses the earlier successful import and leaves its grant orphaned. | Shared async ownership of the registry read/modify/grant/write/retire transaction. |
| S4 | A reopen has captured old registry bytes; a reimport commits and retires its old token before the reopen resumes and acquires the folder. | Reader ownership through entry selection **and** grant-to-folder acquisition. |

Repair these together under one storage executor, using the existing helpers and stages. Preserve successful failure recovery already proved. No schema migration, journal/backup/cache framework, automatic media deletion, new import UI, account/settings-wide changes, forced background refresh or cross-process guarantee. File contents/assets are not modified by these operations. No unrelated PlayerControl progress-report/network/session algorithm change.

## 2. A–B history contract

### Data and compatibility

Keep `VideoPlayHistoryHelper` public class, `LoadABPlayHistories(bool)`, `FindABPlayHistory(PlayInfo)`, `SetABPlayHistory(PlayInfo,ABPlayHistoryEntry)`, entry property names/defaults, setting key `PlayerABPlayHistories`, ordinary cid/`ep`+ep_id key choice, null removal and current max200 cleanup semantics. Keep current empty/malformed/null settings fallback through actual SettingHelper; no global SettingHelper changes. There is no caller awaiting Set today; preserve its exception-safe public async-void compatibility wrapper if a Task core is introduced.

**Do not gratuitously change live-entry identity.** Find currently returns the stored mutable entry; Player uses it as `Player.ABPlay`, and cancel-B changes that object without Set. That existing in-process behavior may remain. The new ownership boundary is a **deep detached persistence snapshot**: copy the dictionary and each non-null entry's PointA/PointB primitive values before handing a write to a background task. A shallow dictionary copy is insufficient. Later mutation of a supplied/returned entry or later addition/removal of a key must never change an already accepted write's serialized content. Preserve null entry compatibility if encountered in stored JSON rather than crashing a new snapshot-copy loop.

### Initialization, ordering and failure

- Ensure persisted cache initialization under a common private state lock before the first Find or Set, so a first Set merges into existing saved entries rather than starting from the static empty fallback. The later startup Load may perform requested bounded cleanup, but must not reload over accepted process-local mutations. Only this helper writes the key in the inspected shipped code.
- All dictionary reads/rebinding/mutations/snapshot capture/cleanup share that lock. Do not hold a monitor across an await. Keep the static non-null fallback available.
- Submit each snapshot in accepted call order to one writer. Once newer state has persisted, an older write may never overwrite it. Include cleanup writes in this rule. A FIFO Task chain or equally small ordered drain is sufficient; a semaphore acquired independently inside Task.Run is **not** an invocation-order guarantee.
- Keep settings I/O off the UI caller's synchronous path except the existing bounded initialization read; preserve rapid player controls. Do not move global settings serialization onto a new policy or add unconditional retry loops.
- Preserve logging/catch behavior on Save failure. The prior durable JSON stays as supplied by the primitive store, accepted memory remains usable, and later snapshots can save. A failed predecessor must not fault-poison or permanently hold the write chain. Every async-void failure is handled/logged; no unhandled exception channel.
- New private Task-returning core/helpers are acceptable when naturally representing queued work, with the public wrapper retained. Do not add test-only production methods or a public storage abstraction merely to reach private state.

## 3. Local registry contract

Keep the JSON file name/schema/private entry fields, import/reimport identity rule (`CollectionId`+`RootPath`, ordinal-ignore-case), token format, manifests/validation/path handling, return metadata, and existing typed exception stage values. Public method signatures stay unchanged. No CancellationToken is currently accepted; this slice adds no cancellation API and does not confuse a canceled folder picker with a failed service transaction.

Use one process-wide async gate in `LocalMediaLibraryService`:

1. Manifest reading/asset resolution/validation may remain outside it, preserving parallel validation. Acquire before `registry-read`. Release in an unconditional finally, including read/validation/write/rollback exceptions.
2. Inside ownership, read the latest registry, identify superseded entries, add the provisional grant, mutate the private registry, stage/write/replace, mark registrySaved, and retire superseded grants. Another import cannot read an intermediate registry transaction or touch its fixed `.tmp` file.
3. Keep the existing staged replace path; single transaction ownership gives the constant temporary filename one writer. A unique temp name alone would not fix stale read/modify/write loss. No direct overwrite of the committed JSON and no speculative redesign of UWP replace behavior.
4. A precommit failure removes only that operation's provisional token; previous registry bytes and prior tokens remain. A successful replacement retains its new token and only then retires prior same-id/same-root tokens. Different id/path imports survive together. A failed transaction releases ownership so another import/retry can proceed.
5. `GetSavedCollectionsAsync` takes the same gate for its registry snapshot/mapping, then returns detached collection rows as today. It need not hold it after return or promise a token remains valid forever after future reimport.
6. `ReopenCollectionAsync` takes the same gate for registry read, entry selection and `FutureAccessList.GetFolderAsync(token)`. Release once the folder object is acquired, before slower manifest/asset validation. This makes an already admitted reopen finish acquiring its valid folder before a reimport retires the old grant. Do not call a gate-taking Read helper while already holding the same semaphore; use a clearly named ungated private read or one caller-owned critical section.
7. Preserve typed `registry-read` for malformed storage and do not silently replace corrupt data with an empty list. Preserve broken collection registry entries so file repair/reimport still works. Retain `access-restore` for truly absent/stale input tokens; the fix does not resurrect any old token after a completed reimport.

A simple private `SemaphoreSlim(1,1)` with `WaitAsync`/finally Release and existing private helpers is sufficient. No new product source file or UWP project Compile entry is required. Process-local serialization is the claimed scope; crash consistency, concurrent separate app processes and native replacement guarantees remain separate evidence questions.

## 4. Executor ownership and real-source regression seams

Allocate **one storage executor** these exact candidate files; it must not overlap the recovery executor's DownloadVM/Page work:

| Area | Exclusive candidate ownership |
| --- | --- |
| Product | `src/BiliLite.UWP/Helpers/VideoPlayHistoryHelper.cs`; `src/BiliLite.UWP/Services/LocalMedia/LocalMediaLibraryService.cs` only. |
| A–B tests in existing stage | `plan/tests/playersess/HistoryStorageProbe.cs` (new); `HistoryStorageStubs.cs` (new, if separate primitive fixtures help); `PlayersessProbe.csproj` links real helper + real SettingHelper and Newtonsoft13.0.3 as in adjacent batches; `Program.cs` adds only a call to new checks. Existing checkpoint/session assertion bodies remain intact. `playersess/run.sh` only if a documented actual-source override/extraction seam is required, not to duplicate another executable stage. |
| Registry tests in existing stage | `plan/tests/LM02.cs`, `plan/tests/LocalMediaStorageStubs.cs`. Existing runner already compiles whole real source; no new stage or run.sh inventory change needed. Shared stubs also serve LM03; hooks default inert and test state is reset. |
| Evidence | Storage execution artifact under this directory; raw outputs/source substitutions stay under `/workspace/work/recovery-integrity/storage/` or executor-owned scratch. |

Whole source is preferred. Compile actual `SettingHelper.cs`, not a fake serializer; provide only `ApplicationDataStorageHelper` primitive string store, logging, PlayInfo/MyProfile DTO shape and UWP storage/access substitutes. Namespace-local playersess fixtures do not collide with `BiliLite.Helpers`/`BiliLite.Controls`. Optional exact, bounded real-method extraction/accessors must document ranges/hash and signature-only instrumentation, not recreate a persistence algorithm. A naturally introduced Task core can be awaited directly or through a narrow access seam; do not replace Set decisions with test logic.

The existing **50 logical/51 executable** inventory remains unchanged. Extend playersess and LM02; do not add a physical stage and silently retain the denominator. No original assertion removal, weakened expectation or bypass of the current checkpoint extraction guard.

## 5. Acceptance and red/green plan

The scratch observations and two exit1 acceptance logs bind current source. Convert their event gates into regressions checking desired outcomes. No arbitrary sleep, network, Windows UI simulation or “eventually retry until green.” Every held event is released in a finally and all initiated operations are awaited/drained before resetting fixture state.

### A–B checks

- First-use case runs **before any helper access** in the stage (or a fresh isolated process): seed durable old history, accept a new key during delayed startup load, finish all writes; old+new entries survive in memory and disk. Later explicit startup Load cannot roll them back. Preserve ordinary cid/episode keys, null removal, empty/null/malformed fallback and >200 startup cleanup.
- Hold actual serialization with trace-only gates: mutate incoming/Find-returned entry after Set, and add/remove another key while the first value is being serialized. An accepted snapshot retains its captured PointA/PointB and keys, without enumeration exceptions. Prove live cache entry behavior remains compatible separately from detached persistence values.
- Force the previous stale-write sequence using a blocked primitive Save/controlled writer completion; verify one active Save, accepted ordering and latest final JSON. Use a naturally awaitable Task core/real-method seam for completion observation where needed. Assert payload values/order rather than testing only for a lock's presence. Current source must fail the added latest-state/mutation assertions; retain the existing deterministic acceptance-red log as independent reproduction.
- Seed prior durable data, inject a Save failure, assert handled/logged failure and preserved previous JSON, then accept another checkpoint and verify it saves. Include cleanup/load writes in ordering. No queue remains stuck after failures.

### Registry checks

- Hold import A at primitive CreateFileAsync after it reads the registry; call B with all other primitive Tasks completing synchronously. B must wait for transaction ownership (no second staging creation), then release A and await both. Final registry contains both distinct collections and exactly their two reachable grants; both reopen. On the old source B completes early and the final registry has only one, giving a deterministic red.
- Repeat same-root/same-id reimports: final registry has one replacement entry and one live grant; all superseded tokens are retired **after** replacement; no orphan or missing reachable token. Different-root/same-id and different-id/same-root combinations preserve the existing deduplication rule.
- Hold an earlier Reopen's captured registry-read completion; begin same-folder reimport. Reimport must wait until reopen has selected/acquired its folder. Release the reader, await successful reopen and subsequent replacement. The current source instead completes reimport early and then fails the old reopen at `access-restore`. Unknown token remains a typed failure.
- Partial staging WriteText failure during reimport: typed registry-write; committed bytes unchanged; new grant removed; old grant reopens. Retry replaces leftover temp and succeeds. Read/replace failures likewise release the gate and keep precommit ownership semantics. Test replacement failure **before** primitive replacement; do not rely on the existing Linux stub's delete-then-move to claim UWP atomicity.
- Corrupt registry read remains typed, preserves bytes/grants and prevents new provisional grants. Existing invalid/missing asset/broken-entry repair and original sequential LM02 assertions stay intact.

### Commands and gates

Run owning `playersess` and LM02 plus LM03 for shared-stub changes with exact toolchain. Suggested commands:

```bash
UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality bash plan/tests/playersess/run.sh
UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality bash plan/tests/run.sh --stage LM02
UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality bash plan/tests/run.sh --stage LM03
```

If playersess adds a Newtonsoft reference, keep its version consistent and use the already installed/cached exact toolchain. Then root's single integrated owner runs `bash plan/tests/run-all.sh --tools-root /workspace/work/maintenance-foundation/quality` once all streams stabilize. Selected success remains PARTIAL_PASS. Independent reviewer verifies exact product/test source binding, original assertion preservation, cleanup and no new stage. No full unchanged suite is required during requirements discovery.

## 6. Risks, native limits and next action

Queue initialization/order and semaphore finally placement are the main implementation risks; regressions above target failure recovery and actual caller lifetimes. Retaining live cache entries avoids an incidental PlayerControl change while isolating disk snapshots. Holding only registry/token acquisition, not media validation, bounds the local critical section. Existing user actions and file schema remain compatible; rollback is removal of the two source fixes and their added checks, with no data migration.

Windows/VS/UWP compile and native FAL/file replacement, actual multiple tabs, restart persistence and A–B playback controls require Windows acceptance later. They are unavailable here and are **not** passed. Safe source fixes and whole-source Linux regressions are available now and must proceed after independent requirements PASS. Later single documentation owner can close the evidenced ROADMAP P2 hypothesis and record precise native acceptance limits; storage executor must not edit docs/index/plan history outside its grant.

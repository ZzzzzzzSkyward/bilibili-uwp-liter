# Storage discovery evidence

Status: **confirmed P2 source failures; requirements ready for independent review**. Source inspected and compiled at `077ef35ae194d3ae85aa0621daeac07dc29e29ab`; no product/test/build/main-doc/history edits. Scratch root: `/workspace/work/recovery-integrity/storage`. Requirements owner: storage_requirements. Date: 2026-11-25 Asia/Shanghai.

## Executable checkpoint

`bash /workspace/work/recovery-integrity/storage/run-probes.sh` exits **0**, compiling the **whole real** `VideoPlayHistoryHelper.cs` + `SettingHelper.cs`, and whole real local-media service/loader/validator/models. Exact SDK **8.0.427**, runtime/ref **8.0.33** from `/workspace/work/maintenance-foundation/quality`. Raw output: `probes.log`; fixture source: `History.cs`, `Registry.cs`, `RegistryStubs.cs`; source and fixture hashes: `source-inputs.sha256`, `probe-inputs.sha256` in that scratch root.

Primitive substitutes are storage helper key/string persistence, logging, PlayInfo/MyProfile DTO shape, and UWP Storage/FutureAccessList. History uses **real Newtonsoft default serialization and real SettingHelper.SetValue**. A trace writer only pauses actual serialization; it does not replace a serializer/converter. Registry stubs derive from existing `plan/tests/LocalMediaStorageStubs.cs`, with explicit incomplete Task gates at file operations. No business/history/registry decision is recreated.

Confirmed observations:

1. **S1 — stale and mutable A–B history writes.** First `storageHelper.Save` is held after actual serialization of PointB=2; a subsequent checkpoint PointB=4 saves successfully; releasing the first Save persists 2, while memory remains 4. A trace-only gate at actual dictionary serialization proves a caller mutation after Set changes the purported snapshot. A gate at its first value proves adding another key invalidates the first writer's dictionary enumeration; the real async-void catch logs InvalidOperationException.
2. **S2 — late initialization loses history.** A fresh History process begins with the real empty static field and prior durable `cid-old`. A new checkpoint `cid-new` is held at Save; `LoadABPlayHistories(true)` replaces memory with old state, removing accepted new state; releasing the old pending write leaves durable new-only state, removing `cid-old`. This is possible in the actual startup ordering because `App.InitBili` awaits `AppHelper.SetRegions` before Load and is itself not awaited by App initialization.
3. **S3 — successful local import lost.** Import A reads an empty registry and waits before creating its staging file. B reads the same empty registry and fully commits. A then creates its own staging file and fully commits. Both calls return success; final registry contains only A, while both access grants remain. B's returned token fails Reopen at `access-restore`. This does **not** depend on overlapping temp handles or delete-before-move stub semantics.
4. **S4 — reopen/reimport interleaving.** A reopen reads and captures old registry bytes but its read Task continuation is held. Reimport of the same folder commits a replacement and retires the old grant. Releasing the read makes the earlier admitted reopen fail `access-restore` using its stale entry. Gates delay primitive completion, not service logic.

Actual acceptance-red commands (after compiling with run-probes.sh):

```bash
/workspace/work/maintenance-foundation/quality/dotnet/dotnet /workspace/work/recovery-integrity/storage/History.dll --acceptance
/workspace/work/maintenance-foundation/quality/dotnet/dotnet /workspace/work/recovery-integrity/storage/Registry.dll --acceptance
```

Both exit **1**. `history-acceptance-red.log`: `expected latest PointB=4; actual=2`. `registry-acceptance-red.log`: `expected 2 successful imports; actual=1`. These are genuine current-source failures, not new desired behavior that was merely assumed absent. Observational probe mode intentionally exits 0 when it confirms the failure conditions.

## Preserved/rejected hypotheses

- **Ordinary failed staging write does not destroy the existing registry.** Inject a partial `.tmp` write followed by IOException. The real service reports `registry-write`, removes the new grant, keeps the previous registry byte-identical and previous grant usable, and a later retry replaces the leftover temp file and deduplicates the prior grant. Preserve this behavior; there is no basis for a new journal, backup or schema.
- Corrupt JSON yields typed `registry-read`, retains corrupt bytes and existing grants, and does not allocate a new grant. Do not silently replace it with an empty registry.
- Failed settings Save is caught/logged, preserves prior durable JSON, and a subsequent checkpoint can save. Preserve this with any write queue; a failed predecessor must not poison later writes.
- `var snapshot = ABPlayHistories` does capture which dictionary object exists before a later Load; it does **not** detach the dictionary or mutable entries and does not serialize writes. The dictionary-reference capture is not itself a null/static-rebinding bug.
- `ImportCollectionButton.IsEnabled=false` protects one page's picker/import, not the static service. `HomePage.btnDownlaod_Click` opens a new DownloadPage via MessageCenter; `MainPage.NavigationHelper_NavigateToPageEvent` creates a new tab/frame each time. Two supported tabs can import across awaits. No runtime tab/E2E claim is made.
- There is no confirmed Windows file-system replace atomicity/crash guarantee, cross-process contention result, FAL capacity failure or media-asset corruption. The lost-update reproducer intentionally keeps both final replacements sequential, so those unknowns do not invalidate it.

## Existing probe and source ownership inspection

`UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality bash plan/tests/run.sh --stage LM02` exits **0** (`lm02-baseline.log`). Its unchanged original assertions cover import/reopen metadata, invalid/missing assets, broken-entry repair, and sequential duplicate replacement; all primitive tasks currently complete synchronously, so it cannot expose the proven overlaps. LM02 is the owning registry stage; the shared Storage stubs also compile LM03 and need inert defaults/reset hooks.

No existing test compiles VideoPlayHistoryHelper. `playersess` owns adjacent history behavior, with 217 current assertions, including whole actual session state and four exact extracted PlayerControl checkpoint/report methods. New A–B storage checks should extend that **same executable stage**, without editing the existing assertion bodies. Its namespace-local SettingHelper/PlayInfo fixtures do not collide with the real `BiliLite.Helpers`/`BiliLite.Controls` types.

## Exact source/caller locations

- `Helpers/VideoPlayHistoryHelper.cs:12–13,24–39,47–49,57–78`: shared dictionary, rebinding/cleanup, live-reference read, shared entry insert/reference snapshot/background Save/catch.
- `Helpers/SettingHelper.cs:11–26`: JSON read fallback and synchronous real serialization before primitive Save. Key is `PlayerABPlayHistories` at line412.
- `Controls/PlayerControl.xaml.cs:989,1614,4080–4118`: clear on mode/A reset; Find assigns live entry to Player.ABPlay; completed B persists that mutable entry. Cancel B mutates PointB to max without a new Set; preserve existing live-reference behavior while detaching **persistence snapshots**.
- `App.xaml.cs:287,343–362`, `Helpers/AppHelper.cs:37–75`: fire-and-forget initialization and network-backed region await before history Load.
- `Services/LocalMedia/LocalMediaLibraryService.cs:69–108,112–128,173–201`: unguarded registry read/modify/grant/replace/retire transaction; readers acquire no shared transaction ownership.
- `Pages/DownloadPage.xaml.cs:49–59,64–95,98–110`; `Pages/HomePage.xaml.cs:194–201`; `MainPage.xaml.cs:96–113`: actual list/import/reopen callers and multiple tab instances.

Primary hashes at this checkpoint: VideoPlayHistoryHelper `18965f3afd87de378a06ccf0c6a786fcfd145b6a9f3455bb8093c7a348cb01d2`; SettingHelper `64983dea3c3f861214401aba4138bcdff6184c3e1cc73155d217db120a519f69`; LocalMediaLibraryService `675826adf31c9589b8cfe190226420d4ecddb187f5b8cd3ab521f0aa44885596`. Complete input list is in scratch, not committed raw output.

## Limits

Linux real-source behavior establishes data ownership and ordering failures and can validate their source repairs. It does not pass VS/UWP compilation, native FutureAccessList/storage behavior, actual multi-tab interaction, playback or suspension/device acceptance. Those are explicit later gates; they do not block the available helper/service fix and regression work.

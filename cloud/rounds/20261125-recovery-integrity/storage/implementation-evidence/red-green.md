# Storage implementation evidence

Status: owning source regressions PASS; source commit `bdba9ebcddb3de146c4fdf92919c5a3c57eaf90f` published and verified against local/origin/actual remote. Final evidence-only receipt; release condition and external receipt-SHA binding are in execution.md. Raw artifacts are scratch evidence, not committed logs. Requirements/review/discovery evidence remain immutable.

## Genuine red source binding

Before product edits, executor copied original sources and old test bodies into `/workspace/work/recovery-integrity/storage-execution/baseline/`, added desired-behavior checks, then ran the actual owning executables. `red-inputs.sha256` binds the three whole original helper/SettingHelper/service sources plus the added test inputs. Their product hashes match discovery/review:

- History helper: `18965f3afd87de378a06ccf0c6a786fcfd145b6a9f3455bb8093c7a348cb01d2`.
- Actual SettingHelper: `64983dea3c3f861214401aba4138bcdff6184c3e1cc73155d217db120a519f69`.
- Registry service: `675826adf31c9589b8cfe190226420d4ecddb187f5b8cd3ab521f0aa44885596`.

`playersess-red.log`: exit1,240 assertions, seven failures (first-use persisted-entry merge, late-load memory retention, detached snapshot values/keys/null compatibility, safe enumeration, queued cleanup and latest cleanup result). `lm02-red.log`: nonzero exit134 after 17 additive failures, including completed concurrent import/list operations while an earlier transaction is held, lost successful import/orphan grant, competing replacements, and admitted reopens losing their token while registry read or folder acquisition is held. Existing original LM02 checks execute before these additions. Failed-write/read/replacement controls pass on original source. Red terminates only after releasing/draining all admitted operations and final fixture cleanup.

The prior independent discovery acceptance logs remain additional deterministic evidence of the exact stale history write (`PointB expected4/actual2`) and successful-import lost update (`expected2/actual1`). New tests keep the first writer held, submit successor snapshots, then release/drain and assert payload order/max-one Save/latest durable state. The whole source is compiled; no helper persistence or registry/token decision is reproduced in fixtures.

## Green behavior and fixture seams

`playersess-green.log`: exit0,240/240 checks (217 original,23 added). `lm02-green.log`: exit0,50 additive storage checks plus all original LM02 assertions. `lm03-green.log`: exit0 unchanged consumer. `lm05-green.log`: exit0,22 unchanged consumer checks. Commands/toolchain are in execution.md; warnings in legacy CSC builds are the existing CS1701 Newtonsoft reference warnings, not failures.

- History first access is Set, seeded with prior durable ordinary-key and null-entry data. A held primitive Save separates this from delayed Load and a subsequent accepted value. JSON payloads/keys and final durable state are checked. Later explicit Load retains process memory.
- Real Newtonsoft trace pauses the first actual entry serialization. Supplied/Find-returned live values mutate and other keys are added/removed while the serializer is held. The admitted persistence snapshot retains its primitive values/keys; null entries remain null; real serializer logs no enumeration exception. Live entry reference identity and mutable in-memory behavior remain.
- Episode key/default-B/null removal, actual SettingHelper malformed/empty/null/missing fallback behavior, handled failed Save/prior JSON/accepted memory/later Save recovery, and >200 queued cleanup/latest write are checked. No test-only production API or extraction replaces helper code.
- Async-void completion is observed using SynchronizationContext.OperationCompleted for each real public Set, with continuation callbacks posted to the thread pool. Gates have10-second failure bounds, unconditional releases and awaited drains. The completion context implements no settings/history logic.
- Import A is held at primitive staging creation after registry-read; B and saved-list reader must wait. Final distinct rows and exactly reachable grants both reopen. Same-root concurrent replacements retire each prior grant only after committed JSON stops referencing it. Same-id case comparison and different-id/root combinations remain compatible.
- Reopen is held separately after registry bytes are read and before actual grant lookup. Reimport waits until both boundaries finish. A third hold after folder acquisition proves manifest validation releases the gate, allowing reimport while reopen successfully uses its acquired folder. Completed replacement leaves old/unknown tokens typed access-restore.
- Partial staging write, pre-replace and registry-read faults preserve committed bytes/prior token; retry proves gate release and leftover-temp recovery. Corrupt JSON remains typed on import/list/reopen without new grant or byte overwrite. Existing broken-entry repair and sequential checks remain.
- Shared storage hooks default null; primitive access-list dictionary is synchronized only to make the substitute primitive safe under real concurrent callers. Hook decisions are file-level delay/fault/trace only. Replace failure is injected before delete/move; this does not claim native replace atomicity.

## Preservation and input receipts

`assertion-preservation.log` records byte-identical original playersess Program after removing the one added call, equal original LM02 Main Assert lines in order, byte-identical original LM02 helper tail, unchanged HistoryCheckpointProbe and unchanged extraction-guard run.sh. The playersess checkpoint extraction still enforces five fields/four complete exact methods; no run.sh edit or new executable stage.

`green-inputs/` contains immutable copies of all30 whole source/test inputs for playersess, LM02, LM03 and LM05, including actual PlayerControl source for the pre-existing checked checkpoint extraction. `green-inputs.sha256` and `green-inputs-check.log` bind/verify every file. Primary final hashes:

- `src/BiliLite.UWP/Helpers/VideoPlayHistoryHelper.cs`: `1d1ffb66031083e35a58bc90a05f9ae529319b0b9ee0b136738140228349961e`.
- `src/BiliLite.UWP/Helpers/SettingHelper.cs`: `64983dea3c3f861214401aba4138bcdff6184c3e1cc73155d217db120a519f69`.
- `src/BiliLite.UWP/Services/LocalMedia/LocalMediaLibraryService.cs`: `335d35363f65fa03e57fc94193ce9e2c08aafd58cea83a40e24a0833e19a2dd0`.
- `plan/tests/playersess/HistoryStorageProbe.cs`: `b1fb6568851ab4c15b689d4fa89af1a121a59eb6b0a8a05568758f438367ad1f`.
- `plan/tests/LocalMediaStorageStubs.cs`: `a1c38806369b348a089032cc559b165cd941b4fb46a8c242b58024456e2c9caf`.
- `plan/tests/LM02.cs`: `d558e088fc9f20d28712af97c0a6725ebc1e2bf948cba6fac9c3671d93ca6b6b`.

No product/test input changed after this green binding. All work remains within the execution-dispatch grant; stage counts and aggregate tooling are untouched. The final current51/52 integrated gate, independent source review, and native Windows/UWP/FAL/restart/playback acceptance are separate pending gates.

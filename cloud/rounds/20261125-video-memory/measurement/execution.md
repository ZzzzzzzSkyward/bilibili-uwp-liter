# External video-memory tooling execution

Status: **FROZEN / RELEASED FOR INDEPENDENT IMPLEMENTATION REVIEW**; supported portable checks PASS, native Windows checks BLOCKED. Exact release binding is in [implementation-evidence/release-binding.json](implementation-evidence/release-binding.json). Authorized baseline: `7356b94893377558191b0d70576a52bae704c1f9`; explicit dirty continuation under [execution-dispatch.md](../execution-dispatch.md). No Git mutation, publication, product/main-document change or aggregate inventory change is part of this delivery.

## Delivered scope

- `tools/measure-video-memory.ps1`: read-only external Windows PowerShell 5.1-compatible `Collect`, `Checkpoint`, `Mark`, `ValidateMetadata` interface.
- `tools/tests/video-memory/run.py`, `controlled.ps1`, `markers.ps1`, `revisions.ps1`: separate portable controls, with explicitly synthetic input/artifact/process classes and real public marker entry checks. They are not an aggregate stage.
- This file and `implementation-evidence/**`: execution/independent-review inputs. Generated outputs, raw commands and synthetic run directories stay under `/workspace/work/video-memory/tooling-fix/`.

The optional summarizer is deliberately not delivered. There is no untested slope/replicate implementation or automated leak verdict. The reviewed protocol's raw interval calculation and three-repeat reporting remain the Windows analyst's follow-up.

## Operator interface

Prepare the verified target/artifact and fixed fixture using [windows-protocol.md](windows-protocol.md). The script never discovers or guesses a package/PID, changes settings, calls APIs, launches/kills the target, performs UI actions, collects GC, trims working set or purges caches. Artifact SHA256 and the metadata's explicit process binding are checked; the operator must establish the installed-manifest/executable/artifact relationship and record its verification method. Metadata validation alone does not establish that relationship or confer E2.

Use a metadata input file **outside** a not-yet-existing dedicated run directory. First create the run with the prewarm checkpoint, then start continuous collection in another measurement shell. An existing empty/unbound directory is refused; an existing bound run accepts only the same byte-for-byte metadata and tool hash. Each invocation also verifies the artifact file hash. Do not edit metadata/tool during a run; put later deviations in manual event notes and preserve the original input.

```powershell
# Variables are observed values from protocol preflight, never a guessed process name.
$tool = '.\tools\measure-video-memory.ps1'
$metadataFile = 'C:\captures\fixture.json' # prepared before the timed run
$runDirectory = 'C:\captures\run-01'      # must not exist on first invocation
$target = @{ TargetProcessId=$observedProcessId; ExpectedProcessStartUtc=$boundStartUtc.ToString('o') }
& $tool -Mode ValidateMetadata -MetadataPath $metadataFile
& $tool -Mode Checkpoint -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 0 -Phase prewarm
# Start this in a second shell, with the same tool, metadata, directory and target:
& $tool -Mode Collect -MetadataPath $metadataFile -RunDirectory $runDirectory @target -IntervalSeconds 2 -DurationSeconds 2400
# Back in the marker/checkpoint shell, after the five warm-up closes and 60s idle:
& $tool -Mode Checkpoint -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 0 -Phase baseline
```

`Collect` takes its first sample immediately, then samples at the requested positive cadence while its monotonic elapsed duration is below `DurationSeconds`. Overrun does not fabricate missed measurements. Duration bounds one sampling window and does not complete the operator's journey. It can start only once per run; `Ctrl+C`/failure preserves flushed rows and releases owned handles/locks through `finally`. A force-terminated shell can leave a lock file; preserve that incomplete run and begin a fresh directory. Do not remove an active collector's lock or resume its stream. `Checkpoint` and `Mark` use separate files/locks and may run alongside `Collect` after initialization.

At each attempted cycle use the actual cycle number; mark the operator-observed request/start/close/return and engine/quality as appropriate. `CompletedCycles` is explicit; failed/time-out/active/suspended observations remain distinct. Unknown tab/player counts and unspecified event observations are null. Passing zero records a real operator-observed zero. Marking has no native/UI dependency and can run on Linux for schema testing; it never verifies that a close or media start occurred.

```powershell
# Example for the first real cycle; run at the observed actions, not consecutively as automation.
& $tool -Mode Mark -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 1 -Phase opened -Action open-request -CompletedCycles 0 -Status incomplete -MediaLabel $preparedMediaLabel -MediaPart $preparedPart
& $tool -Mode Mark -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 1 -Phase opened -Action media-start -CompletedCycles 0 -Status active -ActualEngine $observedEngine -ActualQuality $observedQuality
& $tool -Mode Mark -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 1 -Phase post-close -Action close-action -CompletedCycles 0 -Status incomplete
& $tool -Mode Mark -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 1 -Phase settled -Action starting-view -CompletedCycles 0 -Status ok -ObservedPlayers 0 -Note 'Operator observed return and audio stopped.'
& $tool -Mode Mark -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 1 -Phase settled -Action cycle-complete -CompletedCycles 1 -Status ok
# Use checkpoints at actual cycles 5/10/25/50: opened, post-close, settled, then final idle60/idle300.
& $tool -Mode Checkpoint -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 5 -Phase opened
# Only after all fifty actual cycle-complete events and final observations:
& $tool -Mode Mark -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 50 -Phase idle300 -Action run-complete -CompletedCycles 50 -Status ok -Note 'Operator journey complete; profiler/device acceptance remains separate.'
```

All interfaces share eleven allowed phases: `prewarm`, `baseline`, `opened`, `post-close`, `settled`, `idle60`, `idle300`, `background`, `minimized`, `restored`, `feed-step`. Invalid phases fail parameter binding before reading metadata or any process. Checkpoint warm-up phases require cycle 0; final idle phases require cycle 50. Duplicate checkpoints and backward checkpoint/cycle/time sequences are refused. Marker keys are cycle/phase/action, with each `cycle-complete` additionally unique and sequential across phases. A failed attempt cannot silently replace a completed cycle. A `run-complete` marker requires fifty distinct ordered cycle-complete events and writes `STOPPED.json` with **operator-complete**, not native acceptance. Other files/checkpoint coverage still require analyst review; no samples/checkpoints are invented from events. A stopped run refuses future public writes. An already-running sampler checks the stop marker before its next observation.

Native `Collect` and `Checkpoint` explicitly refuse non-Windows hosts before output initialization/process reads. Target exit/reused PID/start getter failure preserves a fatal/null-metric row, writes a stopped marker when possible, and throws. Other getter failures preserve successful metrics, null the failed field and report partial plus a field-specific base exception. The observer Process wrapper is disposed on success/failure, without terminating the target. All process fields are current refreshed values; there are no peak fields. Real zero and empty thread collections remain zero/ok. The implementation invokes the actual CLR property getter through reflection; strict mode alone is not relied on.

## Metadata v1 input contract

Use UTF-8 JSON objects, arrays and numbers. Explicit UTC strings must have `Z` or `+00:00`, with no local time assumption. Metadata's evidence level is the **intended** E2/E3/E4 collection; availability/acceptance still depends on real observations. Required top-level fields:

| Field | Required shape |
| --- | --- |
| `schemaVersion`, `runId`, `evidenceLevel`, `createdUtc` | `1`; 1–80 ASCII letters/digits/underscore/hyphen beginning with a letter/digit; E2/E3/E4; explicit UTC ISO8601 |
| `sourceCommit`, `sourceDirty` | Full 40-hex commit or `unknown`; JSON boolean |
| `artifactPath`, `artifactSha256` | Absolute path to real observed delivery artifact; matching 64-hex SHA256 |
| `appVersion`, `configuration`, `architecture`, `runtime` | Nonempty observations, or `{ "status":"unavailable", "reason":"..." }` |
| `package` | Nonempty observed `fullName`, `familyName`, `appId`, `executable` |
| `process` | Integer `id` 1..2147483647, explicit `startUtc`, observed nonempty `path`, `verificationMethod` |
| `device` | `osBuild`, `ram`, `cpu`, `gpu`, `driver`, `power`, `display`, `scaling`; nonempty observations or unavailable+reason |
| `fixture` | Fields below; all controls recorded, no credentials/user IDs |
| `profiler` | `mode`, `version`; nonempty observations or unavailable+reason |
| `units` | Exactly measured names: privateBytes/workingSetBytes=`bytes`, handleCount/threadCount=`count`, cpuTotalMs=`milliseconds` |
| `outputs` | samples=`samples.ndjson`, checkpoints=`checkpoints.ndjson`, events=`events.ndjson` |
| `deviations` | Array of strings, empty allowed |

`fixture` requires journey I/F/S/D/SD/DD/A/M; positive integer `replicate` and `repeatOrder`; `mediaOrder` with fifty nonempty prepared item/part labels (or fixed control-step labels); `mediaListSha256`; `expectedCycles` as integers 1 through 50 in exact order; positive finite numbers `playbackSeconds`, `betweenCloseSeconds`, `timeoutSeconds`, `warmupCycles`, `warmupIdleSeconds`. I/A/M use prepared matched elapsed/transition step labels rather than fabricated video opens. Record the difference in `journey`/deviations.

Also required are nonempty observations or unavailable+reason for `comparisonGroup`, `shell`, `accountLabel`, `network`, `requestedEngine`, `actualEngine`, `requestedQuality`, `actualQuality`, `codec`, `resolution`, `frameRate`, `playbackRate`, `volume`, `subtitles`, `danmaku`, `autoplay`, `apiCapture`, `entryRoute`, `startingPosition`, `cacheState`. Record all API capture switches in `apiCapture`; all display dimensions/scaling/text size and window state in `device.display`/`scaling`; frame rate/codec/resolution/quality and playback controls in the named fixture fields. Never put credentials, cookies, API bodies, user IDs or process command lines in labels or notes. Unknown fields may be supplied for additional declared details; they do not replace required fields.

The list hash binds the exact ordered labels, independently of the metadata byte hash: SHA256 of the compact JSON string array encoded as UTF-8, without BOM or newline. Use the same PowerShell serialization as the tool, including non-ASCII escaping behavior:

```powershell
$listJson = ConvertTo-Json -InputObject @($preparedOrderedLabels) -Compress
$sha = [Security.Cryptography.SHA256]::Create()
try { $mediaListSha256 = ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($listJson)))).Replace('-','').ToLowerInvariant() }
finally { $sha.Dispose() }
```

The committed Python test generator creates a complete **synthetic** example metadata file in its fresh scratch directory. It is a schema example, never a runnable native fixture or a real installed artifact binding.

## Output and preservation contract

The script copies input bytes to `metadata.json`, and writes `binding.json` with run ID, metadata SHA256, actual tool SHA256/version, artifact hash, PID/start UTC and creation UTC. Each stream has its own sequence beginning at one. Samples have exactly the reviewed fifteen process-record fields. Checkpoints add cycle/phase. Events have the reviewed nineteen event fields; their optional observation fields are null when unspecified. UTC timestamps and monotonic ticks/frequency are retained independently; invariant JSON integer bytes/counts preserve values above 32 bits. CPU time is finite nonnegative milliseconds. There is no unit conversion to MB.

Append operations hold exclusive create-new per-file `.lock` ownership, validate existing binding/sequence/time/types/status, and flush/dispose after each line. Bad input/metadata, output collision/write failure, duplicate/out-of-order observations or identity failure exits nonzero and preserves prior output. No file is truncated or silently recovered; malformed/truncated lines block later append. PID/start failure ends the whole run; lock/write/metadata failures preserve the incomplete state for review. A successfully elapsed collection window does not mark the journey complete. After all collection writers have ended, hash the retained files with `Get-FileHash -Algorithm SHA256` and preserve those receipts alongside the raw capture/profiler/diagnostic exports as required by the protocol.

## Verification and release

The final command is:

```bash
python3 tools/tests/video-memory/run.py --powershell /workspace/work/maintenance-foundation/package-layout/powershell/pwsh --output-directory /workspace/work/video-memory/tooling-fix/t1-t3/final-supported
```

It exits **0**, recording **46 outer CLI/controlled invocations**, the original **21 controlled groups**, and **106 T1/T2 revision checks** (groups intentionally contain multiple assertions, not a fabricated flat assertion count). Raw exact commands/exit/stdout/stderr are in `/workspace/work/video-memory/tooling-fix/t1-t3/final-supported/commands.json`; summary and tool hash are in its `results.json`. A counting forwarder delegates every call unchanged to the real public Mark interface. The unchanged marker helper makes **55** calls: initial marker, rejected skip, fifty cycle markers, rejected cross-phase duplicate, completion, rejected terminal append. It emits exactly 52 accepted event rows, including explicit synthetic zero tab/player values. These are synthetic operator statements, never playback observations.

Supported coverage: metadata missing/type/unit/hash/order/nonfinite errors; explicit Windows host rejection for Collect and each of eleven Checkpoint phases without output creation; invalid phase rejected before missing metadata reads; invalid cadence/duration; valid marker sequence and null observations; exact target/run/artifact/script binding; duplicate/case-variant/backward/incomplete completion protection; exclusive writer ownership; existing unbound directory refusal; tampered metadata and truncated-line preservation. Actual function AST extents execute controlled CLR getters under default and strict/Stop modes; null and throwing getters retain successful fields and null/error the failed metric; >32-bit byte roundtrips and all true zeros/empty threads remain exact; identity mismatch, retrieval failure, refresh/exit and identity-getter failure preserve fatal observations. Controlled native-loop tests cover real implementation cadence/no-resume, all phases, invalid phase before dependency access, terminal identity failure with prior row retention/no later process reads, occupied outputs, observer disposal, locks, fixed record fields/types/order and empty/unterminated stream refusal. No production host bypass/test switch exists.

Fresh parser receipts use `/workspace/work/video-memory/tooling-fix/t1-t3/parse-revised.ps1` and `parse-revised.json` (four real PowerShell files, zero AST errors). The available interpreter is **PowerShell Core 7.7.0 / Unix**, not Windows PowerShell 5.1. The inherited MR1/MR2 release-binding manifest was verified against all fourteen actual files; see `/workspace/work/video-memory/tooling-fix/inherited-input-verification.json`. Existing author/reviewer original-getter sensitivity evidence is reused by hash, not rerun or counted as new-tool tests.

Intermediate scratch runs remain private. `run-01` initially failed the test author's incorrect expected field count (16 instead of the reviewed 15); it was replaced by exact expected-field names, preserving the reviewed schema. `run-02` through `run-05` then passed supported checks while subsequent inspection added capture-loop controls, completion uniqueness, empty/trailing-line refusal, canonical enum spelling, record/type guards and observer disposal. The original receipts are historical; the revised T1–T3 receipt below binds current code. There is no historical product failing-original/fixed-source claim because this tool and its tests are new.

### T1–T3 correction and genuine same-fixture regression

Independent implementation review found the missed final-idle ordering and scalar-string coercion holes. Original tool/tests/receipts and the reviewer's actual counterexamples were copied **before edits** into `/workspace/work/video-memory/tooling-fix/t1-t3/original/` and `review-counterexamples/`, with `original-binding.json`. No original receipt was relabeled as a corrected pass.

The same byte-identical `revisions.ps1` executes the actual captured function ASTs and public validator/marker against the frozen original and corrected tool. It runs a single sixteen-checkpoint stream (prewarm/baseline; opened/post-close/settled at 5/10/25/50; idle60/idle300), then checks duplicate/backward final idle rejection, including an independent idle300→idle60 stream with no earlier idle60 to isolate the ordering guard and final idle after all seven permitted cycle-50 ordinary/control phases. It wraps each required scalar-string metadata leaf in a single-item array and calls the real public metadata validator. Samples replay and actual checkpoint/event append controls cover string bindings/enums/nullable text, raw run-binding fields and terminal records, retaining prior bytes, no append and no new process read on rejection. Supported arrays, unavailable objects, valid scalars and true zeros continue through the original controls.

| Exact tool | Same 106 checks | Actual bounded observation |
| --- | --- | --- |
| Frozen original `6b1b67a713309de0279c87de05408d3ab6a942b0ae3d35ac1931757f655144cc` | exit 1, 38 failed expectations | 14 journey rows; final idle calls rejected; scalar-array holes reproduced in metadata, replay/append, bindings and terminal handling |
| Corrected `668b5c06ff519d6f9500a9e087889e5504099cd84c0fb0fcf6bade4f2011474f` | exit 0, 106 PASS | 16 ordered journey rows; final idle after every cycle-50 control; malformed arrays rejected with bytes unchanged/no append or process read |

Exact commands differ only in tool path and fresh output directory:

```bash
/workspace/work/maintenance-foundation/package-layout/powershell/pwsh -NoProfile -File tools/tests/video-memory/revisions.ps1 -Tool /workspace/work/video-memory/tooling-fix/t1-t3/original/tools/measure-video-memory.ps1 -MetadataPath /workspace/work/video-memory/tooling-fix/final-supported/synthetic-metadata.json -OutputDirectory /workspace/work/video-memory/tooling-fix/t1-t3/original-red-final
/workspace/work/maintenance-foundation/package-layout/powershell/pwsh -NoProfile -File tools/tests/video-memory/revisions.ps1 -Tool /workspace/bilibili-uwp-liter/tools/measure-video-memory.ps1 -MetadataPath /workspace/work/video-memory/tooling-fix/final-supported/synthetic-metadata.json -OutputDirectory /workspace/work/video-memory/tooling-fix/t1-t3/fixed-green-final
```

T1 final ranks are 1021/1022, above every permitted cycle-50 ordinary/control rank (1001–1007). T2 raw `[object]` checks precede scalar coercion/comparison/membership; stored binding/terminal records are validated before reuse/early stop. The reflection getter, metric/null/zero behavior and public native host guard remain unchanged. T3 corrects the earlier erroneous 56 claim to the measured **55 calls / 52 accepted rows**; no dummy call was added. The 55 calls are initial marker + rejected skip + fifty cycle-complete markers + rejected cross-phase duplicate + completion + rejected terminal append. The prior 56 claim was an arithmetic reporting error, preserved only in historical receipts.

[Correction receipt](implementation-evidence/t1-t3-corrections.json) binds the fixture, exact original/fixed inputs, commands/exits and result digests. The supported runner preserves all former 45 invocations and 21 groups, adds the revision fixture as one meaningful invocation, and derives the helper count from actual execution. These are model/dependency controls of the real script, not a Windows native experiment. No full product suite/build/package/control run or publication was performed.

[Supported-checks receipt](implementation-evidence/supported-checks.json) binds exact retained artifacts and command exits; [release binding](implementation-evidence/release-binding.json) freezes delivered files and reviewed inputs. Frozen means no further tool/test changes until an on-disk independent review finding or renewed root grant. No commit/push was made or authorized during this local phase; publication remains root-serialized.

Linux PowerShell Core checks are portable parser/schema/dependency controls only. Windows PowerShell 5.1 execution, a harmless actual Windows PID sample, UWP playback, actual fifty-video process values, heap/native/GPU attribution and device acceptance remain **BLOCKED/UNVERIFIED**. No E2/E3/E4 result or BR-21 closure is claimed. No unchanged full suite/build was run for this isolated new tool; aggregate remains 51 logical/52 executable stages with separate runner14/package15 controls.

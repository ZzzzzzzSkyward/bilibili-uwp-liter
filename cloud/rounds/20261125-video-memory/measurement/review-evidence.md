# Independent measurement review evidence

Status: **PASS — RELEASED TO ROOT**, 2026-11-25, after MR1/MR2. Reviewer scratch: `/workspace/work/video-memory/measurement-review/revision-mr1-mr2/`. Source/test/tooling/main documents and all author artifacts were read-only. No source/owning/full-suite rerun or native process measurement occurred.

## Revised release binding

The author’s frozen [release-binding.json](/workspace/work/video-memory/measurement/revision-mr1-mr2/release-binding.json) was checked against actual bytes for all **14** listed files. The independent runner also checked all **15** current source bindings and all **32** reuse-manifest current artifact hashes; **28** unchanged reused artifacts retain identical prior/current hashes. Source HEAD remains `7356b94893377558191b0d70576a52bae704c1f9`.

| Released input | Independently matched SHA-256 |
| --- | --- |
| `measurement/requirements.md` | `f6e6eb823cd8e34dcdf4e815a2b060f182a5af3349dcd526f93448a929065dd4` |
| `measurement/evidence.md` | `6c716ef495893bcf528ac863d76d9895e97c02a6471125f07e568cf02c2661a5` |
| `measurement/windows-protocol.md` | `ebf596457b179158d95335cfdb05b989e6b364706bb348e50e004e29e18f5e77` |
| Private `Measure-Checkpoint.ps1` | `ed765e055c6db2a4a52b25d4839fe7c0f019db393b7e24cacc7e1320477015eb` |
| Exact revised inline and standalone cores | Both `71cb4e1aa3e45d70705564180f7f9f80b8f9aaf091eba0b437834a0c0745f84c` |

I extracted the helper/checkpoint functions directly from the revised protocol block and standalone prototype into reviewer-owned scratch and compared them to the author’s released exact cores. All four cores match. The reflection accessor calls the actual CLR getter, rejects raw null before casts and preserves empty collections; `ICollection.Count` supplies an explicit CLR count contract. Both function and standalone phase lists include all published phases. The requirements now explicitly require the previously missing throwing-CLR-getter controls and retain the existing null/error/native acceptance.

## Independent executable re-review

Command: `python3 /workspace/work/video-memory/measurement-review/revision-mr1-mr2/independent-recheck.py`.

Result: **PASS**, exit 0. [Runner](/workspace/work/video-memory/measurement-review/revision-mr1-mr2/independent-recheck.py), [summary log](/workspace/work/video-memory/measurement-review/revision-mr1-mr2/independent-recheck.log), [raw command/stdout/stderr records](/workspace/work/video-memory/measurement-review/revision-mr1-mr2/independent-commands.json), [checked results and host/parser output](/workspace/work/video-memory/measurement-review/revision-mr1-mr2/independent-results.json).

PowerShell was `/workspace/work/maintenance-foundation/package-layout/powershell/pwsh`, Core 7.7/Linux. The inspected author dependency fixture was copied byte-for-byte into reviewer scratch; it contains a controlled CLR process-like class, throwing/null getters and a synthetic `Get-Process` override. The runner supplied fresh reviewer-owned outputs, extracted actual cores and its own expectations. It never called a real process API with the synthetic PID.

| Independent check | Actual result |
| --- | --- |
| Original exact published function with the same CLR WorkingSet64 getter throwing UnauthorizedAccessException, strict/Stop | Original defect reproduced: zero/ok/error-empty, normal return. Historical failing core is retained; assertions were not relaxed. |
| Revised inline and standalone, ok and all-zero cases | Exact `4294967297`/`9876543210` byte values preserved; true zeros, including an empty thread collection and zero CPU time, remain zero/ok/error-empty. |
| Both revised cores, throwing getter under default and strict/Stop, plus null-returning getter | Failed working-set value null, status partial, exactly the correct field/base-exception error; private bytes/handles/threads/CPU remain intact. No false zero. |
| Both cores, replacement/process exit/throwing identity getter | Fatal row preserved with the correct identity status and null metrics, then throw; identity getter failure keeps start time null. |
| Both cores, output path occupied by a directory | Writer failure throws; no success row. |
| Each revised core, all eleven phases and one invalid phase | Valid phases record correct values/phase; invalid phase throws with zero process calls and no row. |
| Actual standalone interface without removing/replacing its native guard, all eleven valid phases | Each reaches Windows-required rejection, nonzero exit, no sample output; prewarm no longer fails argument binding. |
| Actual standalone invalid phase | Rejected by parameter validation, nonzero exit, no output. |
| Fresh AST parse of all five published PowerShell blocks plus actual standalone | Six PASS. Syntax only; not Windows PowerShell 5.1 execution. |

There are **43** independent synthetic controls: one original-failure sensitivity control plus 21 for each revised core; **12** standalone phase/host checks; **six** parser checks. Raw NDJSON run/PID/schema/sequence/monotonic fields and UTC offset were checked directly, avoiding the reporting harness’s JSON date reformatting. The author’s own 43/12 results and validator were also inspected; the independent pass does not depend on their PASS labels.

## Unchanged evidence and boundaries retained

The initial [CHANGES review](/workspace/work/video-memory/measurement-review/revision-mr1-mr2/initial-review-changes.md) and [initial evidence](/workspace/work/video-memory/measurement-review/revision-mr1-mr2/initial-review-evidence.md) preserve the original findings and source/reference inspection. Original reviewer controls remain under `/workspace/work/video-memory/measurement-review/`. The unchanged current-source and 240/session50/4325/mutant proof artifacts were hash-verified and reused, not rerun or relabeled.

Current x64 project configuration disables .NET Native; an installed artifact’s effective runtime still requires its own binding. Actual close/unload/history ordering, renderer-off danmaku loading and preexisting suspension GC remain as previously inspected. Existing log/diagnostic exports support correlation, not a process/object census. The source-session proof exercises actual session code with fake resources; the retained-list mutant is a sensitivity control, not an original product defect or native memory result.

[Previously checked official reference notes](/workspace/work/video-memory/measurement-review/official-reference-checks.md) continue to support WPR argument/query/setup/start/stop forms, package full-name distinction and VS native/mixed enable/restart guidance. Native commands/configuration prose did not require revision. No WPR setting or recording was changed, and no documentation lookup became a claim of native validation.

Windows PowerShell 5.1, UWP playback/process metrics, managed attach, native allocation traces, GPU counters and device acceptance remain **BLOCKED/UNVERIFIED**. No E2/E3/E4 evidence or BR-21 closure is created by these corrections. Optional summarization stays optional; future tool implementation still needs its separately granted schema/metadata/event/output checks. Measurement review is released; execution/publication remains subject to root’s all-stream gate and exact ownership grant.

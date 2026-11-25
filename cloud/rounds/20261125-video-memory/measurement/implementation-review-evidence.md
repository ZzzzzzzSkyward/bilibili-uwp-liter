# Independent external tooling implementation re-review evidence

Status: **PASS — RELEASED TO ROOT**, 2026-11-25, after T1–T3. Sole scratch `/workspace/work/video-memory/tooling-final-review/t1-t3/`. Only the two assigned review Markdown files were written in the repository. No executable/source/test/main-document/ref edits, whole-product suite or native measurement occurred.

## Frozen inputs and unchanged contracts

At review start, all **eight** implementation files and **nine** reviewed inputs in [release-binding.json](implementation-evidence/release-binding.json) matched actual SHA-256 values. The preserved original release manifest and all **1003** files in the bound revision-artifact manifest also matched. HEAD remained `7356b94893377558191b0d70576a52bae704c1f9`. [Independent binding verification](/workspace/work/video-memory/tooling-final-review/t1-t3/input-bindings.json), [review-start release snapshot](/workspace/work/video-memory/tooling-final-review/t1-t3/released-input-manifest.json). The two prior CHANGES review files were archived before replacing them: [initial review](/workspace/work/video-memory/tooling-final-review/t1-t3/initial-changes-review.md), [initial evidence](/workspace/work/video-memory/tooling-final-review/t1-t3/initial-changes-evidence.md). Their hashes in the author’s release are historical review inputs, not hashes of this new verdict.

| Final delivered file | SHA-256 |
| --- | --- |
| `tools/measure-video-memory.ps1` | `668b5c06ff519d6f9500a9e087889e5504099cd84c0fb0fcf6bade4f2011474f` |
| `tools/tests/video-memory/run.py` | `20d38a64ba753c25663497e0304dcd70e752a7450266ff6a307736cfffc8de44` |
| `tools/tests/video-memory/revisions.ps1` | `62d788ea675302f7f915e787d51f7e5e14817413e1a42df4bd7baa4055116701` |
| `controlled.ps1`, unchanged | `26031002342388927e02d216955109a02efeb648f072ea05b60b7db4b48ffd93` |
| `markers.ps1`, unchanged | `9b3371eb53083c583ff6493457bbaebfaad96a08f0e3d7eec108eab9591e4ccb` |

The actual original/final diff, complete new regression fixture, revised runner wiring, execution contract and correction/support receipts were inspected. Final idle ranks exceed every allowed cycle-50 control rank. `Assert-ScalarString` takes raw object values; checks precede coercion/equality/membership for the affected metadata, observation status, stored records, bindings and terminal fields. Existing supported arrays and nullable observations are not normalized or narrowed away.

[Independent unchanged-core check](/workspace/work/video-memory/tooling-final-review/t1-t3/unchanged-core-check.json) confirms byte-identical `Get-UtcInstant`, `Read-RequiredClrProperty`, `New-MemorySample`, `Write-NewJsonFile`, `Enter-OutputLock`, `Write-Record` extents and the public entry block. The only capture-loop change validates an existing terminal record before stopping. Original 21-group and marker fixtures are unchanged; no historical assertion was relaxed. Tooling file/API responsibilities remain bounded; no new product instrumentation or optional analysis file exists.

## Independent exact same-input regression

The actual frozen `revisions.ps1` was executed twice, changing only tool path and fresh output directory. Both invocations used `/workspace/work/video-memory/tooling-fix/final-supported/synthetic-metadata.json`. Interpreter: `/workspace/work/maintenance-foundation/package-layout/powershell/pwsh -NoProfile -File tools/tests/video-memory/revisions.ps1` with the recorded Tool/MetadataPath/OutputDirectory arguments.

| Tool | Exit | Checks / failed expectations | Actual journey rows |
| --- | --- | --- | --- |
| Frozen original `6b1b67a713309de0279c87de05408d3ab6a942b0ae3d35ac1931757f655144cc` | 1 | 106 / **38** | 14 |
| Final `668b5c06ff519d6f9500a9e087889e5504099cd84c0fb0fcf6bade4f2011474f` | 0 | 106 / **0** | 16 |

The 106 check names and order are identical. [Same-input comparison](/workspace/work/video-memory/tooling-final-review/t1-t3/same-input-red-green.json), [original log](/workspace/work/video-memory/tooling-final-review/t1-t3/original-red.log), [original results](/workspace/work/video-memory/tooling-final-review/t1-t3/original-red/revision-results.json), [fixed log](/workspace/work/video-memory/tooling-final-review/t1-t3/fixed-green.log), [fixed results](/workspace/work/video-memory/tooling-final-review/t1-t3/fixed-green/revision-results.json).

These checks execute actual function ASTs/public validator/marker, with explicitly synthetic CLR process dependencies. They cover the complete 16-checkpoint stream; duplicate and isolated backward idle rejection; final idle after all seven cycle-50 ordinary/control phases; every scalar-string metadata leaf wrapped as a one-item array; process/checkpoint replay and actual checkpoint/event append refusal; stored binding/terminal raw types; preserved bytes/no append/no process reads; and valid scalar terminal stop. No public native-host guard is removed or replaced.

The original reviewer’s own counterexamples were also rerun using output-only path substitutions:

- [Actual capture journey](/workspace/work/video-memory/tooling-final-review/t1-t3/checkpoint-journey.ps1) now produces [16 rows and successful idle calls](/workspace/work/video-memory/tooling-final-review/t1-t3/checkpoint-journey.json), with ranks 1003/1021/1022 and released locks.
- [Replay schema controls](/workspace/work/video-memory/tooling-final-review/t1-t3/schema-controls.json) accept the valid scalar record and reject an unknown metric, array runId and array status.
- [Actual public metadata commands](/workspace/work/video-memory/tooling-final-review/t1-t3/metadata-scalar-controls.json) accept valid scalar metadata and reject both array evidenceLevel and array journey, nonzero exits.

## Independent complete tooling entry and counts

Command: `python3 tools/tests/video-memory/run.py --powershell /workspace/work/maintenance-foundation/package-layout/powershell/pwsh --output-directory /workspace/work/video-memory/tooling-final-review/t1-t3/supported`.

Result: **PASS**, exit 0: **46 outer invocations**, original **21 controlled groups**, **106 revision checks**, **55 actual forwarded public helper calls / 52 accepted synthetic rows**, final tool hash above. [Console log](/workspace/work/video-memory/tooling-final-review/t1-t3/supported.log), [commands/stdout/stderr](/workspace/work/video-memory/tooling-final-review/t1-t3/supported/commands.json), [results](/workspace/work/video-memory/tooling-final-review/t1-t3/supported/results.json), [counter](/workspace/work/video-memory/tooling-final-review/t1-t3/supported/marker-public-call-count.txt), [controlled groups](/workspace/work/video-memory/tooling-final-review/t1-t3/supported/controlled/controlled-results.json). The unchanged helper has 52 accepted calls plus three intended rejections; current receipts correctly distinguish those from the outer invocation/group counts. No dummy call was added.

Fresh AST checks of the tool and all three PowerShell test files: **four PASS**, exit 0. [Parser script](/workspace/work/video-memory/tooling-final-review/t1-t3/parser-check.ps1), [output](/workspace/work/video-memory/tooling-final-review/t1-t3/parser-check.log). Available runtime remains **PowerShell Core 7.7/Linux**; these are not Windows PowerShell 5.1 semantics/native execution.

## Preservation and proof limits

Retained controls verify exact >32-bit byte roundtrips; true zeros/empty thread collection; throwing/null CLR getter partial errors under default and strict/Stop settings; preserved successful fields; fatal replacement/missing/refresh/start-getter rows; observer disposal; output locks/create-new/no-resume behavior; partial/empty/unterminated-output preservation; fixed fields/types and extra-metric rejection; artifact/tool/metadata/PID/start/run binding; actual native-host rejection without output initialization; marker null versus zero observations; ordered fifty completion statements and terminal append refusal. T1–T3 corrections do not change target behavior or add a synthetic public collection bypass.

Metadata validates an operator’s declared package/artifact/process relationship and intended evidence level; it does not independently establish installed identity or E2. All process classes, PID values, artifact examples and accepted marker statements here are synthetic. No fifty UWP videos, actual private-byte trend, managed/native/GPU retention or repair improvement was observed. Optional summarization remains absent and optional; aggregate inventory is unchanged.

**Windows PowerShell 5.1, harmless actual Windows PID sampling, UWP playback, E2/E3/E4 memory evidence, managed/native/GPU attribution, build/device acceptance and BR-21 measurement remain BLOCKED/UNVERIFIED.** Source-owner integration and publication remain root’s later gates; this is the released external-tooling component review only.

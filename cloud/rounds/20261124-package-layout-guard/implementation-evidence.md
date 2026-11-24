# Permanent package-layout probe execution evidence

Status: **OFFLINE PASS / COMMITTED AND PUSHED**. Implementation `8f559681c700453e681b28bd295ad56b2cdcf132` was pushed under this round's explicit commit-window.md and immediately verified equal to origin/dev. Verified source/probe/manifest hashes matched again before staging, without retesting or implementation changes. Final publication/status and ownership release: execution.md. Source intake HEAD `7ff548b24dc70a335c6d0d44c4ebc86a984ce96c`; other owners' edits/commits were preserved. All raw logs, original source snapshot, loader-only mutants and integrity JSON are under `/workspace/work/maintenance-foundation/package-layout/execution/`.

## Exact source loading and scope

The permanent `plan/tests/package-layout-probe.ps1` parses the entire selected source file and rejects parse errors, missing/duplicate expected function definitions and expected functions nested below the root AST. It defines only the two original FunctionDefinitionAst extents. No full script is dot-sourced/invoked. A separate literal-path source copy with a top-level file-write sentinel passed all 15 cases without creating the sentinel. Only the ZIP assembly is loaded; fixtures use .NET ZIP/path APIs, inert zero bytes and a manifest string. No Python/.NET SDK/NuGet/network/app requirement is introduced by this probe.

`build-release-x64.ps1` differs from original bytes only inside Test-FullPackageLayout: one explanatory comment, root ContainsKey check, and Int64 entry-length comparison `-ge 64KB`. Existing checks/diagnostics remain first; reader/caller/all other source bytes and the original UTF-8 BOM are unchanged. No Int32 narrowing, payload decompression, normalization or signature/PE check is added. The probe also has a UTF-8 BOM so Chinese diagnostics remain decodable under Windows PowerShell 5.1; its syntax/APIs target 5.1, whose execution is unavailable here.

| Input / extracted extent | Original SHA-256 | Repaired SHA-256 |
| --- | --- | --- |
| Complete build script | `25b67d8d4fdbf979c531397e76ab31c74b7b81a71f79685e0126caeab6c9ef00` | `479656b5e95d1af66d8262d453ea8b4db6735571431a0f038c52bd054a10ef10` |
| Actual Read-AppxPackageLayout, lines 407–424 | `512315f016cfdc5fc11a6d7e922b3d9892fcaccf90aaf6e0bac51ca0c82812f0` | unchanged |
| Actual Test-FullPackageLayout, original 426–433 / repaired 426–436 | `ea9f95d6db4744cca8a46f29daea2c310d8e99923474d42890e919ec1b376282` | `05a2f7f00f6f7cb7fb8237565429cc539a3e19180c9b8a330fad617420040698` |
| Final permanent probe, same bytes for final red and green | `0d39001c69b9594b3a0ddd9c2f1d4098a5dcbc49e751daeb0e8255311595d14a` | unchanged |
| UWP Package.appxmanifest | `ee64a3b24c7014a5b3682afb7f63025ed84dc056d6f8bfb35229715da9aba921` | unchanged |
| Packages Package.appxmanifest | `2e655dd7b626ba17e53f6594783d29213ef13823476c8f3567ec3ed5a94d59ab` | unchanged |

Function hashes use SHA-256 over UTF-8 AST Extent.Text (without any file BOM); complete-file hashes cover exact bytes. `initial-integrity.json` / `final-integrity.json` record those comparisons. A byte-level replacement comparison proves all build-script bytes outside the one function remain identical. Both manifests were checked before original-source red and after repaired-source green: no version mutation.

## Commands, actual exits and mandatory cases

Verified existing local tool: `/workspace/work/maintenance-foundation/package-layout/powershell/pwsh`, **PowerShell 7.7.0 Core / Linux**. No tool installation was needed by this executor. The permanent probe was created/run against the original source before the root guard edit: genuine exit **1**, 10 PASS / 5 FAIL. Final BOM-preserving probe bytes were then replayed unchanged against original/repaired inputs:

```bash
/workspace/work/maintenance-foundation/package-layout/powershell/pwsh -NoLogo -NoProfile -File plan/tests/package-layout-probe.ps1 -BuildScriptPath /workspace/work/maintenance-foundation/package-layout/execution/original-build-release-x64.ps1 -ScratchRoot '/workspace/work/maintenance-foundation/package-layout/execution/probe runs [literal]'
# exit 1; CASES=15 PASSED=10 FAILED=5; permanent-red-final.log
/workspace/work/maintenance-foundation/package-layout/powershell/pwsh -NoLogo -NoProfile -File plan/tests/package-layout-probe.ps1 -BuildScriptPath ./build-release-x64.ps1 -ScratchRoot '/workspace/work/maintenance-foundation/package-layout/execution/probe runs [literal]'
# exit 0; CASES=15 PASSED=15 FAILED=0; permanent-green-final.log
/workspace/work/maintenance-foundation/package-layout/powershell/pwsh -NoLogo -NoProfile -File ./plan/tests/package-layout-probe.ps1 -ScratchRoot '/workspace/work/maintenance-foundation/package-layout/execution/probe runs [literal]'
# default source selection: exit 0; CASES=15 PASSED=15 FAILED=0; default-source-green.log
```

| Mandatory named archive case | Original source | Repaired source |
| --- | --- | --- |
| Canonical root 8,192 | PASS: null | PASS: null |
| Canonical root 65,535 + reader metadata | PASS: null; System.Int64 / 65535 | PASS: same |
| Zero root, limited-predicate characterization | PASS: null | PASS: null; no bootability claim |
| Root absent | FAIL: falsely accepted | PASS: `缺少根目录 BiliLiter.exe` |
| Nested-only root | FAIL: falsely accepted | PASS: same missing-root diagnostic |
| Root exactly 65,536 | FAIL: falsely accepted | PASS: `根目录 BiliLiter.exe 必须小于 64 KB` |
| Root 65,537 | FAIL: falsely accepted | PASS: same size diagnostic |
| Highly compressed root 4 MiB | FAIL: falsely accepted | PASS: same size diagnostic |
| Entrypoint absent | PASS: `缺少 entrypoint/BiliLiter.exe` | PASS: exact same |
| Companion absent | PASS: `缺少 BiliLite.Win32Tools` | PASS: exact same |
| Manifest absent | PASS: `清单里没有 fullTrustProcess 扩展` | PASS: exact same |
| Manifest declaration absent | PASS: same declaration diagnostic | PASS: exact same |
| Root + entrypoint both absent | PASS: old entrypoint diagnostic wins | PASS: exact same priority |
| Corrupt physical archive | PASS: MethodInvocationException | PASS: exception preserved |
| Missing physical archive, bracket/space path | PASS: MethodInvocationException | PASS: exception preserved |

Both actual functions are extracted in each run, with source hash/line/hash output. Boundary reader length is asserted as System.Int64 and 65,535. The 4 MiB root contains 4,194,304 **uncompressed** bytes, compressed to **4,080** bytes by .NET; this fixture's compression metadata is validated independently before invoking the actual predicate. The five original-source failures are exactly the requested root false accepts, not parser/setup/exception substitutes. All original assertion expectations remain unchanged for green.

## Source-loader, failure-code and cleanup controls

Each scratch mutant is passed through the same permanent probe's `-BuildScriptPath`; no repository source mutation is involved:

| Control / raw log | Actual result |
| --- | --- |
| Append unclosed function (`loader-invalid-syntax.log`) | Exit **2**, explicit SETUP_FAILURE, CASES=0; no extraction/execution. |
| Rename expected predicate (`loader-missing-function.log`) | Exit **2**, missing-function failure, CASES=0. |
| Duplicate expected predicate (`loader-duplicate-function.log`) | Exit **2**, duplicate-function failure, CASES=0. |
| Move sole expected predicate into enclosing function (`loader-nested-function.log`) | Exit **2**, not-top-level failure, CASES=0. |
| Repaired source under `build [literal] guard.ps1` plus top-level sentinel (`loader-sentinel.log`) | Exit **0**, 15/15 PASS; exact function-only loading, sentinel absent. |
| ScratchRoot is an existing file (`fixture-setup-failure.log`) | Exit **2**, explicit directory setup failure, CASES=0; original file preserved. |

These are additional source-loading/setup controls, not additional archive cases or application probe batches. Assertions fail with exit 1, extraction/parser/setup/cleanup failures use exit 2, and only complete success uses exit 0. The final parent scratch directory still exists and is empty; only uniquely generated child directories were deleted. Streams/archives/readers/writers are disposed in finally blocks. Literal filesystem APIs preserve bracket/space paths.

Separate safe syntax command `/workspace/work/maintenance-foundation/package-layout/powershell/pwsh -NoLogo -NoProfile -File /workspace/work/maintenance-foundation/package-layout/execution/parse-inputs.ps1` exits **0**: full build script and probe each `ERRORS=0`; it parses files without executing either release script body. Raw `syntax-only.log`. `git diff --check -- build-release-x64.ps1 plan/tests/package-layout-probe.ps1` exits **0**.

## Inventory and platform boundaries

This adds **one named PowerShell gate with 15 mandatory archive cases**, separately from the historical 43 directory batches + seven legacy logical probes / 50 logical entries / 51 executable stages. No directory batch, aggregate runner, CI, dependency or main document was changed; prior offline inventory results are not represented as rerun by this package-only task.

Windows PowerShell 5.1 execution, full Windows VS/UWP/XAML build, real Packages MSIX creation/signing/layout, installation/launch, converter and device playback remain **FAILED/BLOCKED — Windows environment unavailable**. Synthetic inert ZIPs named .msix are neither signed/installable packages nor validated executable contents. Zero-byte-root acceptance is the approved existence/upper-bound limitation. Full release script execution, manifest/version bump, signing/install/publish/master/live-data operation was not performed or authorized.

# Package-layout guard implementation review

Status: **PASS — bounded offline implementation accepted.** No P0, P1 or P2 finding remains in the assigned source/probe scope. Windows and release/runtime gates remain **FAILED/BLOCKED — Windows environment unavailable**; this PASS does not close them.

Reviewer: `package_final_review`, 2026-11-24. Read `AGENTS.md`, `cloud/CONTRACT.md`, `cloud/REPO.md`, memory index, this round's plan/requirements/PASS review/dispatches/commit window, execution and evidence. Reviewed immutable implementation commit `8f559681c700453e681b28bd295ad56b2cdcf132` against its parent, plus the evidence-only publication commit `6a285d6ae409c16c149b6ef37bc1d909fa3def33`. Only this `final-review.md` is written by the reviewer; no implementation, requirements, main docs, history or other owner's files are changed. No whole-worktree-clean or independent remote-publication claim is made here.

## Source scope and behavior

- The source/test diff consists of three added lines in `Test-FullPackageLayout` and the new 189-line `plan/tests/package-layout-probe.ps1`. The implementation commit's other two paths are this round's execution/evidence Markdown; no API/search source, generated tracked asset, aggregate runner, CI, dependency, manifest, main document or package-selection change is present.
- Independently compared exact committed bytes: replacing only the repaired function extent with its parent's extent reproduces the entire original build script byte for byte. The reader, caller, all other script text and UTF-8 BOM are preserved. Current source/probe bytes equal the reviewed commit; the original scratch snapshot equals the commit parent's script.
- Existing entrypoint, companion and fullTrustProcess checks/diagnostics retain their order. The root-key check and then the size check occur after them and before the existing `$null` return. The caller still raises its existing `完整包布局不对...` error for a nonempty problem.
- `Read-AppxPackageLayout` still stores `ZipArchiveEntry.Length`, an uncompressed `System.Int64`. The new comparison is directly against `64KB` / **65,536 bytes**, with no Int32 conversion, payload decompression or compressed/container-size substitution: 65,535 passes; 65,536 and above fail. A nested-only root fails under the unchanged lookup semantics. Reader exceptions and disposal remain unchanged.
- No minimum-size, PE, signature, archive-normalization or duplicate-entry policy is added. Zero-byte-root acceptance is the explicitly approved existence/upper-bound limitation, not a usable launcher or bootable-package result.

## Permanent probe and evidence

The probe uses the PowerShell parser, rejects parse errors and missing/duplicate expected definitions, and verifies each selected `FunctionDefinitionAst` belongs directly to the root script's named block. It defines only the exact two selected extents, emitting complete-source and function hashes/line boundaries. It does not invoke or dot-source the complete release script, substitute a stub, or copy the predicate algorithm. Fixture expectations are separate from the actual reader/predicate.

The fixture helpers, source loader and assertion loop have distinct responsibilities and clear names. The two-file placement is appropriate; no orphan component, duplicated production implementation or unnecessary framework is introduced. Syntax/APIs inspected are consistent with the intended PS5.1 target, with UTF-8 BOMs preserving Chinese source decoding; actual PS5.1 execution remains unverified.

Read the decisive raw executor logs under `/workspace/work/maintenance-foundation/package-layout/execution/logs/`, including final permanent red/green, default-source green, syntax-only, all five loader controls and fixture-setup failure; also checked original source, mutant inputs, integrity JSON and probe hashes. The earlier red probe hash differs only from the final BOM-bearing artifact; final red/green logs identify the final exact source/function inputs, and the reviewer replay below uses the final committed probe unchanged for both runs.

Independently replayed the final committed probe with `/workspace/work/maintenance-foundation/package-layout/powershell/pwsh -NoLogo -NoProfile -File ./plan/tests/package-layout-probe.ps1`, varying only `-BuildScriptPath` between the original snapshot and repaired script, and using a literal space/bracket scratch root `/workspace/work/maintenance-foundation/package-layout/reviewer runs [literal]`:

| Independent check | Verified result |
| --- | --- |
| Parent/original source | Actual process exit **1**, `CASES=15 PASSED=10 FAILED=5`; only missing root, nested-only root, 65,536, 65,537 and compressed 4 MiB root falsely accepted. |
| Repaired source, same probe bytes/assertions | Actual process exit **0**, `CASES=15 PASSED=15 FAILED=0`. |
| Boundary reader metadata | `ROOT_LENGTH_TYPE=System.Int64 ROOT_LENGTH=65535`. |
| Oversize compression control | 4,194,304 uncompressed root bytes versus 4,080 compressed bytes; repaired predicate rejects it. |
| Existing diagnostics, combined failure, corrupt/missing archive | All exact old diagnostics/priority and both expected reader exceptions pass in red and green. |
| Invalid syntax, missing, duplicate, nested expected definition | Each actual process exit **2**, explicit `SETUP_FAILURE`, `CASES=0`; no fixture success substituted for loader failure. |
| Literal bracket/space source plus top-level file-write sentinel | Exit **0**, 15/15; sentinel absent before and after, proving no top-level sentinel execution. |
| ScratchRoot is an existing file | Exit **2**, setup failure; original file preserved. |
| Default source, launched from `/workspace` | Exit **0**, 15/15; source selection is independent of current directory. |
| Cleanup and integrity | Supplied scratch parent exists and is empty; source/probe/both manifest hashes unchanged around independent loader/default runs. |
| Safe parser-only command | Full build script and probe each `ERRORS=0`; exit **0**, PowerShell **7.7.0 Core**. |
| `git diff --check 8f559681^ 8f559681 -- build-release-x64.ps1 plan/tests/package-layout-probe.ps1` | Exit **0**. |

All 15 mandatory archive cases remain present, including the zero-byte characterization and strict-boundary acceptance control. Assertions use exit 1; parser/extraction/setup/cleanup failures use exit 2; only complete success exits 0. Archive/entry/manifest streams are disposed through `finally`. The probe creates a unique child fixture directory and deletes only that child using literal .NET path/filesystem APIs. No full release-script execution or version mutation occurred during review.

## Independent integrity and acceptance limits

The reviewer verified these complete-file SHA-256 values against both working bytes and the reviewed commit where applicable:

| File | SHA-256 |
| --- | --- |
| Original build script / committed parent | `25b67d8d4fdbf979c531397e76ab31c74b7b81a71f79685e0126caeab6c9ef00` |
| Repaired build script | `479656b5e95d1af66d8262d453ea8b4db6735571431a0f038c52bd054a10ef10` |
| Final permanent probe | `0d39001c69b9594b3a0ddd9c2f1d4098a5dcbc49e751daeb0e8255311595d14a` |
| UWP manifest, unchanged from parent | `ee64a3b24c7014a5b3682afb7f63025ed84dc056d6f8bfb35229715da9aba921` |
| Packages manifest, unchanged from parent | `2e655dd7b626ba17e53f6594783d29213ef13823476c8f3567ec3ed5a94d59ab` |

Independent output also matches reader AST hash `512315f016cfdc5fc11a6d7e922b3d9892fcaccf90aaf6e0bac51ca0c82812f0` unchanged, and predicate hashes `ea9f95d6db4744cca8a46f29daea2c310d8e99923474d42890e919ec1b376282` → `05a2f7f00f6f7cb7fb8237565429cc539a3e19180c9b8a330fad617420040698`.

Verified platform is **PowerShell Core 7.7.0 on Linux**, exercising actual selected functions against inert ZIP fixtures. This adds **one named PowerShell gate with 15 internal archive cases**, separately from the historical 43 directory batches + seven logical legacy probes / 50 logical entries / 51 executable stages. It does not count as 15 application batches or rerun the earlier inventory; aggregate-runner/CI work is outside this review.

Windows PowerShell 5.1 execution, VS/UWP/XAML compilation, real Packages MSIX production/signing/layout, installation/launch, converter execution, playback and release readiness remain **FAILED/BLOCKED — Windows environment unavailable**. Synthetic ZIPs named `.msix` contain inert bytes and do not establish valid executable contents, installability or bootability. The existing full Windows build/version/package/installed-launch gates still apply before any future authorized release.

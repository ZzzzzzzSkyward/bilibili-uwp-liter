# Root-launcher package-layout guard requirements

Status: **READY FOR INDEPENDENT REQUIREMENTS REVIEW; source/probe implementation not started.**
Owner: `quality_requirements`; inspected HEAD `10dd84857c748f6638e187d891d6a6649f887bb6`, 2026-11-24.
Inputs: this round's `plan.md`, repository/cloud instructions and memory index, maintenance-foundation QF3 requirements/evidence, actual build functions and `document/BUILD.md`. Actual-function red evidence: [evidence.md](evidence.md).

## 1. Problem and priority

`AGENTS.md:163–165` requires the installable Packages MSIX to contain `entrypoint/BiliLiter.exe` and a root `BiliLiter.exe` **under 64 KB**. The build script's current `Test-FullPackageLayout` (`build-release-x64.ps1:426–432`) checks entrypoint, Win32Tools and a fullTrustProcess declaration but ignores root existence and size.
This is a confirmed incomplete guard, not a newly reproduced startup defect. Real actual-function fixtures accepted a missing root, a nested-only root, a 65,536-byte root, a 65,537-byte root and a 4 MiB root. The requested rejection checks therefore fail on the unmodified function.
Priority: **P1 before a future release**, bounded offline hardening available now after PASS. Preserve existing package selection and platform boundaries; no release is authorized.
Goal: reject that narrowly defined invalid root layout while keeping all existing checks, return/error conventions and unrelated build behavior intact.

## 2. Exact structural contract

`Read-AppxPackageLayout` opens the archive read-only and stores each `ZipArchiveEntry.Length` in `Entries[FullName]` (`:409–419`). The observed value is **System.Int64 uncompressed bytes**; it is not compressed size, archive size or a binary-signature verdict.

| Condition | Required Test-FullPackageLayout result |
| --- | --- |
| Existing entrypoint missing | Preserve exact string `缺少 entrypoint/BiliLiter.exe`. |
| Existing Win32Tools entry missing | Preserve exact string `缺少 BiliLite.Win32Tools`. |
| Manifest absent or existing fullTrustProcess string check fails | Preserve exact string `清单里没有 fullTrustProcess 扩展`. |
| All existing checks pass, root key `BiliLiter.exe` absent | New nonempty string `缺少根目录 BiliLiter.exe`. Nested `nested/BiliLiter.exe` does not satisfy the root requirement. |
| All existing checks pass, root uncompressed length **>=65,536** | New nonempty string `根目录 BiliLiter.exe 必须小于 64 KB`. Exactly 65,536 fails. |
| Existing checks pass, root exists with length **<65,536** | Return `$null`, preserving the function's success convention. 8,192 and 65,535 bytes pass this structural predicate. |
| Missing/corrupt physical archive cannot be opened | Preserve the reader's exception behavior; do not convert it to `$null`/success or invent a different file. |

Here **64 KB = PowerShell's `64KB` = 65,536 bytes**, not 64,000; the upper bound is strict. Compare the existing Int64 length numerically without truncating it, converting to Int32, measuring the compressed entry or reading/decompressing the whole root executable.
Add the two root checks **after the existing entrypoint/companion/manifest checks and before the final `$null` return**. This preserves current first-error priority when a package violates both an old condition and the new root condition.
Use the existing Entries lookup behavior. Do not add filename normalization, case-policy changes, duplicate-ZIP-entry rules, extraction or new host/filesystem assumptions.
This slice deliberately imposes no new minimum size, PE-header validation or actual-executable/signature check. A zero-byte root entry satisfies existence plus the specified upper-bound predicate; the fixture records this as a **limitation characterization**, not proof of a usable launcher or valid MSIX.
The caller at `:435–438` still wraps a nonempty problem in its existing `完整包布局不对...` exception. Do not change that integration, parameter interface or return type.

## 3. Explicit non-goals

No modification to `Read-AppxPackageLayout`, fullTrustProcess matching semantics, package selection, UWP/Packages output naming, signing identity, certificates, binary assets, versioned manifests, version-bump behavior, output directories, restore/build/retry/cleanup logic, deployment, installation, master or git history.
Do not execute/dot-source the complete release script on Linux, even with no-bump/fast flags. It performs unrelated top-level work and may mutate manifests or clean byproducts. Parsing its syntax/AST is permitted; execution is restricted to the two extracted function definitions.
No broad packaging rewrite, “repair the archive” path, UWP-package recheck, package rebuilding, telemetry change or unrelated historical-prose correction. `document/BUILD.md:28–31` contains older collision/recheck prose; current functions and the scoped contract above control this change.
Synthetic fixtures are ZIP containers named .msix with inert bytes and a small manifest string. They are not signed/installable packages, valid PE executables or a substitute for actual package production.

## 4. Exact implementation ownership proposal

After independent PASS and explicit dispatch, one executor owns:

- **`build-release-x64.ps1`**: only the `Test-FullPackageLayout` body for the two root checks and narrowly necessary explanatory comment; preserve the reader and all other script text/behavior.
- **New `plan/tests/package-layout-probe.ps1`**: one root-level PowerShell fixture probe; no new C# batch directory, packages, workflow or aggregate-runner modification.
- Its assigned round `execution.md`: source/test diff, command/exit/case results, red/green evidence, actual commit/remote SHA and blocked Windows gates. Exact ownership is recorded by the orchestrator before dispatch.

Main PLAN/ROADMAP/VALIDATION/DEVELOPMENT/BUILD documentation belongs to the one later documentation owner, not this executor. Current source/test owners must release their commit/push window before this independently reviewed commit; never stage another owner's edits.
No product/test/build/main-doc change has been made during these requirements. Scratch installation, fixtures and raw logs stay only in `/workspace/work/maintenance-foundation/package-layout/`.

## 5. Probe interface and safe actual-function loading

The new probe targets Windows PowerShell **5.1-compatible syntax/APIs** and PowerShell Core. It takes optional `-BuildScriptPath` (default repository `build-release-x64.ps1`, derived from `$PSScriptRoot`) and `-ScratchRoot` (default system temp root).
It creates a uniquely named child fixture directory beneath ScratchRoot, uses try/finally to dispose archive/entry/manifest streams, and deletes only that child. Never delete the supplied ScratchRoot, modify the selected build script or create product artifacts. Use literal path/.NET path APIs so spaces and bracket characters are not treated as wildcard syntax.
Parse BuildScriptPath using `System.Management.Automation.Language.Parser`; fail closed on parse errors or a missing/duplicate expected definition. Select exactly the top-level `Read-AppxPackageLayout` and `Test-FullPackageLayout` FunctionDefinitionAst extents, then define **only those exact extents** in the probe process. Verify both names/counts and retain source hash/boundary evidence.
Do not dot-source the full file, invoke its script block, copy the validation algorithm into the probe, or accept a stubbed Test-FullPackageLayout. Fixture creation and expected outcomes may be test code; the predicate/reader being tested must be actual selected source.
Load only the archive assembly required by these functions. The probe needs no dotnet SDK, UWP installation, Python, network, NuGet restore or installed app. Generate ZIPs through .NET archive APIs supported by both targeted PowerShell editions; avoid PS7-only operators/features.
Print one named PASS/FAIL outcome per case and `CASES=n PASSED=p FAILED=f`. Exit **0 only when every required case passes**, **1 for any assertion failure**, **2 for source-extraction/parser/fixture-setup failure**; do not hide unexpected exceptions behind a successful summary. Missing/corrupt archive cases explicitly expect an exception.

```powershell
# Windows PowerShell 5.1, when a Windows host is available
powershell -NoProfile -ExecutionPolicy Bypass -File .\plan\tests\package-layout-probe.ps1
# PowerShell Core; override source/temp roots for isolated fixture evidence
/workspace/work/maintenance-foundation/package-layout/powershell/pwsh -NoLogo -NoProfile -File ./plan/tests/package-layout-probe.ps1 -BuildScriptPath ./build-release-x64.ps1 -ScratchRoot /workspace/work/maintenance-foundation/package-layout/probe-runs
```

Raw stdout/stderr belongs in assigned scratch logs; execution.md records concise case outcomes and the command's real exit. A custom BuildScriptPath supports original/repaired source snapshots without editing the checked-out build script during red evidence.

## 6. Required fixtures and regression evidence

All ordinary fixtures contain entrypoint, companion and a manifest with the existing declaration string unless the case explicitly removes one. Use inert bytes, unique filenames and independent archives.

| Fixture | Expected result |
| --- | --- |
| Canonical root 8,192 bytes | `$null`; normal structural acceptance control. |
| Canonical root 65,535 bytes | `$null`; strict upper-bound control. |
| Canonical zero-byte root | `$null`; explicitly labeled limited-predicate characterization, never “bootable package.” |
| Root absent | New missing-root diagnostic. |
| Root exists only under nested/ | New missing-root diagnostic. |
| Root exactly 65,536 bytes | New size diagnostic. |
| Root 65,537 bytes | New size diagnostic. |
| Root 4 MiB, highly compressed to <64 KB | New size diagnostic; proves validation uses uncompressed entry length. |
| Entrypoint absent | Unchanged old entrypoint diagnostic. |
| Companion absent | Unchanged old companion diagnostic. |
| Manifest absent | Unchanged old declaration diagnostic. |
| Manifest lacks fullTrustProcess | Unchanged old declaration diagnostic. |
| Root and entrypoint both absent | Unchanged entrypoint diagnostic wins, preserving previous check order. |
| Corrupt archive bytes | Throws; never success. |
| Missing physical archive, including bracket/space path | Throws; never success. |

**15 fixture cases** are the starting mandatory denominator. If more cases are added, record the expanded denominator rather than dropping old controls. Inspect and assert the reader's root length type/value on a boundary archive; it must represent Int64 uncompressed length. Preserve its stream disposal behavior.
The unmodified actual functions already produced **10 PASS / 5 FAIL**, with all five false accepts isolated to the newly requested root checks. First add/run the permanent probe against original source and retain that red result, then repair only the function and rerun **unchanged assertions**: all 15 must pass.
The fixture probe's original-source failures are meaningful negative controls; retain separate per-case diagnostics so a source-loader failure or unrelated archive exception cannot masquerade as the desired red. Before/after source/manifests hashes must show no version mutation caused by the probe.
Run parser checks and fixture validation under the available local PowerShell Core, plus `git diff --check` on the owned diff. Windows PowerShell 5.1 execution is a separate compatibility check when available; absence is recorded explicitly, not inferred from PS7 success.
This is **one additional named package-layout gate**, counted separately from the existing **43 directory batches + seven legacy logical probes = 50 logical entries / 51 executable stages**. Do not silently increment the historical directory count, change QF2's plan-only status or claim the fixture cases are 15 new application probe batches.

## 7. Environment limits, risks and closure

A user-level PowerShell **7.7.0 Core** is available in assigned scratch after official download/digest verification. Linux can parse the full script without executing it, load its exact two functions and run these archive fixtures. This establishes feasibility and structural guard behavior only.
Windows PowerShell 5.1, VS2022 UWP/XAML toolchain, real MSIX generation, signing/install/launch, converter execution and device playback are **FAILED/BLOCKED — Windows environment unavailable**. The complete release script was not run and must not be run here. A local pwsh binary does not supply those capabilities.
Before any future authorized release, the existing full Windows build/version/package-layout/installed-launch requirements still apply to an actual Packages artifact. Offline fixture PASS may close this bounded guard repair after implementation review; it cannot close a release/runtime gate or establish valid executable contents.
Risks: checking compressed/container size, truncating Int64, accepting a nested root, changing old diagnostic priority, executing top-level build code during test loading, mislabeling synthetic ZIPs as usable packages, and touching concurrently owned files. Exact source extraction, boundary/compression fixtures, scoped function-only diff, hash evidence and serialized ownership address these risks.
Preparation is **READY FOR REVIEW**; implementation remains NOT STARTED. Independent PASS, explicit two-file ownership, permanent-probe red/green evidence, separate commit/push window and independent implementation review are required before this item is marked implemented. No release/master/deployment action is included.

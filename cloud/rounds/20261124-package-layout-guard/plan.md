# Package-layout maintenance guard

Scope: the known QF3 static discrepancy between `AGENTS.md`'s root-launcher requirement and `build-release-x64.ps1`'s current layout check. This is a bounded offline build-guard repair, not a release, package installation, manifest/version change, full UWP build or proof of bootability. Actual Windows release gates remain BLOCKED here.

Requirements owner: `quality_requirements`, `gpt-6.1-sol` / `xhigh`. Read repo/cloud instructions, memory index, maintenance-foundation verification requirements QF3 and its evidence, actual Read-AppxPackageLayout/Test-FullPackageLayout functions and build playbook. Write only this round's `requirements.md` and concise Markdown evidence. Scratch/dependency work only under `/workspace/work/maintenance-foundation/package-layout/`; read/reuse Cloud runtime Packages guidance before installing any user-level PowerShell tool. Never run the whole release build script on Linux or mutate versioned manifests.

Define a minimal owned source change enforcing root `BiliLiter.exe` existence and documented `<64 KB` size along with all existing entrypoint/companion/fullTrustProcess checks. Inspect actual entry-size representation. Define meaningful good/bad archive fixtures invoking **exact actual functions extracted without executing the full build script**; reject missing root, oversized/threshold root and existing invalid layouts. Do not copy the algorithm into a test. Determine what can actually run on this host and record installation/command evidence separately from Windows packaging/runtime gaps.

Propose one narrowly named PowerShell probe (prefer a root-level file under plan/tests rather than adding a new C# batch directory); name exact file ownership, invocation and how its new gate is counted separately from the 43-directory/seven-legacy-probe inventory. Preserve existing build behavior, parameters, package selection, identity, assets, version rules and error conventions. No broad packaging rewrite or unrelated old prose fix.

Requirements → independent expert PASS → explicit executor (`gpt-6.1-sol` / `high`) source/probe ownership → red/green fixture evidence → separate commit/push window after active quality/source owners → independent implementation review. Main docs remain one later owner. No product/build/test edit before requirements PASS. All agent communication/results in round files; final chat artifact/status only.

Requirements and independent review now PASS. Existing quality_executor is assigned the exact two-file scope and deferred commit window in `execution-dispatch.md`; no release or full-build execution is authorized.

## Final disposition — 2026-11-25 Asia/Shanghai

Source `8f55968` adds only the root-launcher existence/strictly-under-65536-byte guard to the actual build function and its narrowly named archive probe. Genuine red 10/15 passing cases became **15/15 PASS** using AST-extracted real functions under user-level PowerShell Core. Independent implementation review in final-review.md is **PASS**, with no open scoped P0/P1/P2; all source/test/index ownership is released. This separate package gate is excluded from the 50/51 aggregate.

**Windows PowerShell 5.1, actual UWP compile/full signed MSIX/install/startup remain FAILED/BLOCKED — Linux has no VS/UWP/Windows SDK/runtime.** The zero-byte fixture characterizes this narrow size predicate; it never certifies executable validity. The tracked expired public certificate differs from the configured signer; release-signing-evidence.md records the actual read-only distinction and uninspected Windows store. No manifest/version/certificate/thumbprint/identity or release action occurred. Main docs record exact validation commands and release prerequisites.

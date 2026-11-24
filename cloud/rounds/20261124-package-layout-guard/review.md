# Package-layout guard requirements review

Status: **PASS** — [requirements.md](requirements.md) is sufficient for the bounded two-file implementation after explicit executor dispatch. No P0/P1/P2 requirement finding remains. Implementation, green probe results and Windows acceptance are not claimed by this review.

Reviewed `AGENTS.md`, `cloud/CONTRACT.md`, `cloud/memory/INDEX.md`, this round's `plan.md`, requirements and [evidence.md](evidence.md), maintenance-foundation QF3, `document/BUILD.md`, actual build-script functions/caller, and the scratch fixture generator/runner/inventory/output. Current HEAD at review was `858b97105ac8b46492af3810e5352afd9efbe8d5`; the build script remains unchanged with SHA-256 `25b67d8d4fdbf979c531397e76ab31c74b7b81a71f79685e0126caeab6c9ef00`. Only this review file is owned/written by the reviewer.

## Confirmed design and preserved contracts

- **Real source seam:** `build-release-x64.ps1:407–424` reads the ZIP in a try/finally and stores `ZipArchiveEntry.Length`; `:426–433` is the actual predicate. The new test must parse source and load only the exact two top-level function extents, reject parse errors/missing/duplicate definitions, and record source hash/boundaries. It must never dot-source or invoke the complete build script, whose top-level version/build/cleanup actions are visible at `:49–100` and elsewhere. The preliminary runner's recursive AST search is sufficient evidence for the current two visible top-level definitions; the permanent probe must implement the stricter top-level selection explicitly required by §5.
- **Numeric boundary:** the root key is exactly `BiliLiter.exe` under the reader's existing lookup semantics. Test its stored Int64 **uncompressed** length against `64KB` / 65,536 bytes: 65,535 passes; 65,536 fails. Nested-only root and missing root fail. No Int32 narrowing, compressed/container-size substitution or executable-content reading is authorized. The zero-byte case is expressly a limited-predicate characterization, with no minimum-size, PE, signature or bootability guarantee.
- **Existing diagnostics/order:** retain exact entrypoint, companion and manifest-declaration checks/strings and reader exceptions. Add root-existence then root-size checks only after those old checks and before `$null`. The caller at `:435–438`, package selection and return/error integration remain unchanged. The missing-root-plus-missing-entrypoint fixture pins the old diagnostic's priority; static review of placement preserves the other old checks' priority too.
- **Meaningful fixture gate:** all 15 named cases invoke the actual reader/predicate against independent inert archives, including normal/boundary roots, nested/missing roots, the strict limit and compressed oversize, all old invalid layouts, combined failure, missing archive and corrupt archive. The permanent probe must first reproduce original-source red, then pass unchanged assertions after the minimal repair; parser/setup failure must not count as the intended red. Int64/type evidence, source/manifest integrity, stream disposal, a unique child scratch directory and literal-path handling are explicit requirements.
- **Minimal ownership:** source/test scope is exactly the `Test-FullPackageLayout` body in `build-release-x64.ps1` and new `plan/tests/package-layout-probe.ps1`, plus assigned round execution evidence. No reader rewrite, package-selection/case/duplicate-entry policy, manifest/version mutation, broad build logic, dependency/CI/runner change or unrelated documentation update is granted. Separate dispatch and commit/push ownership remain required.
- **Platform/counting truth:** PowerShell 5.1-compatible syntax/APIs are a target, while local Core execution proves only the observed Core behavior. PS5.1 execution and actual Windows UWP build/sign/install/launch/converter/device gates remain explicitly unavailable. Synthetic ZIPs are not installable MSIX packages. This is one additional named package-layout gate with 15 internal fixture cases, reported separately from the historical 43 directories + seven logical legacy probes / 51 executable stages; QF2 remains PLAN ONLY.

## Independent evidence

After reading the scratch runner to verify it defines only extracted functions, independently replayed:

```text
/workspace/work/maintenance-foundation/package-layout/powershell/pwsh -NoLogo -NoProfile -File /workspace/work/maintenance-foundation/package-layout/run-fixtures.ps1
EXTRACTED=2 BUILD_SCRIPT_PARSE_ONLY=YES
ROOT_LENGTH_TYPE=System.Int64 ROOT_LENGTH=65535
CASES=15 PASSED=10 FAILED=5
exit 1
```

All five failures were the intended false accepts: missing root, nested-only root, 65,536 bytes, 65,537 bytes and the 4 MiB compressed root. The normal/boundary/zero characterization, existing rejection/priority and missing/corrupt-archive controls passed. No parser/setup failure was substituted for this red evidence. A before/after SHA-256 comparison around this replay confirmed the build script and both versioned manifests were unchanged; hashes matched `evidence.md`'s integrity record. The replay wrapper printed the child probe's actual exit **1**; its own bookkeeping command completed successfully and does not turn the red probe green.

The scratch harness is feasibility/red evidence, not the proposed permanent probe. That permanent probe's exact diagnostic assertions, top-level extraction, required 0/1/2 exit behavior, .NET fixture creation, scratch cleanup and original/repaired-source results remain implementation-review gates. PASS authorizes no release or full-script execution here.

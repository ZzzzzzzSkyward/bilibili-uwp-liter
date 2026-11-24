# FA-01 amendment evidence

Status: **requirements evidence complete; source unchanged**. Read the controlling iteration plan, final-audit/findings/review-verification, original storage requirements/final review, the complete actual registry service, actual DownloadPage refresh/callers, LM02, shared primitive stubs and legacy LM02 compile seam. This actor wrote only the two assigned amendment Markdown files in the repository; reproduction inputs/outputs are scratch under `/workspace/work/recovery-integrity/storage-amendment/`. No source/test/index/ref mutation or new stage.

## Actual-source observation and acceptance red

The existing independent audit already proves `[null]` faults list/reopen/page outside the intended typed boundary, `[{}]` exposes a null-token row, and null/missing Entries are intentionally empty. Its `RegistryAudit` is observational (exit0); that result was not treated as acceptance PASS.

An additional whole-source scratch harness uses the same real service/models/loader/validator/Newtonsoft13.0.3 and existing `LocalMediaStorageStubs.cs`. It seeds a **usable first entry with an existing-token grant**, then appends null, `{}`, or one of Token/CollectionId/RootPath missing/null/empty/whitespace: 14 malformed fixtures × list/reopen/import. The service is never patched, extracted or replaced. Only the complete actual page refresh method is extracted into a wrapper with primitive log/toast counters; its source text is byte-bound below. That body uniquely occurs once in the current page. The wrapper's fields/counters contain no registry decision.

Commands:

```bash
DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet \
DOTNET_CLI_HOME=/workspace/work/recovery-integrity/storage-amendment/cli \
NUGET_PACKAGES=/workspace/work/maintenance-foundation/quality/cache/nuget \
DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 \
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/recovery-integrity/storage-amendment/IdentityAudit.csproj -c Debug
```

The first observational run exited0 and recorded42 unmet repair predicates. The final acceptance harness retains the same predicates and exits1 when any is unmet; `acceptance-red.log` is a genuine **exit1**, summary `ACCEPTANCE reds=42; malformed fixtures=14; original source unchanged`:

- Null trailing entry: list throws untyped NullReferenceException; valid first-token reopen succeeds before reaching that entry; import is already typed `registry-read` through its outer stage catch. This shows why validation must cover the complete snapshot in the guarded read rather than just changing list mapping or reopen's selected row.
- `{}` and each of the12 missing/null/blank required-field cases: list and valid-token reopen succeed; import succeeds and changes committed bytes/grants. These are41 failed malformed-entry predicates out of42; null-entry import is the existing passing control.
- The actual page refresh faults NullReferenceException with logs0/toasts0, adding the42nd failed predicate.
- Eight compatibility controls pass: deserialized null registry, missing/null/empty Entries; non-GUID token with omitted Title/ItemCount lists and reopens; identity-complete missing asset stays listed with typed `asset-resolution`, then restoring the file permits reopen without rewriting the registry. These delimit the new identity check.

Every service operation is awaited sequentially; primitive Tasks complete synchronously here, so there are no held gates/observers/background operations to drain. Fixture hooks/grants and the temporary fixture tree are cleaned in finally. The acceptance program sets its exit status only after observing predicates; cleanup still runs. No full common suite or unchanged control suite was run for requirements discovery. This scratch red is bounded evidence, not a substitute for the executor's final added LM02 bytes red→green or the subsequent full gate.

## Byte bindings

All10 repository input hashes in scratch `inputs.json` were checked again after both runs: **zero mismatches**. Relevant current inputs:

| Input | SHA256 |
| --- | --- |
| `LocalMediaLibraryService.cs` | `335d35363f65fa03e57fc94193ce9e2c08aafd58cea83a40e24a0833e19a2dd0` |
| `LM02.cs` | `d558e088fc9f20d28712af97c0a6725ebc1e2bf948cba6fac9c3671d93ca6b6b` |
| `LocalMediaStorageStubs.cs` | `a1c38806369b348a089032cc559b165cd941b4fb46a8c242b58024456e2c9caf` |
| `DownloadPage.xaml.cs` | `c80f7267f7db8000c6f97f3dab1bfb37f631291c463a791a9f78db9e2423e56f` |
| Exact refresh signature/body text, leading indentation included, no trailing newline | `49cbc84da98359c46d233d396e71bf8a079180642bf5540e1a324bd81c5d34f2` |

The earlier auditor's method hash uses a different text boundary; this extraction's declared exact byte boundary is signature start through its matching closing brace, as embedded verbatim in `PageRefresh.cs`. No method-body rewrite/instrumentation was applied.

| Scratch artifact | SHA256 |
| --- | --- |
| `IdentityAudit.cs` (final acceptance program) | `3ee149dc7f431d6cade5fe6b651bf59dde1b430d5ce8738b4f80d37624d0e93e` |
| `IdentityAudit.csproj` | `ee1dcffb643deb557eec1c3e7ff2345feb82a8c0cb5ea66f33bfece8a4ea47c4` |
| `PageRefresh.cs` | `0b5239660ba5b4349b8eab2209878b3790c5621ddba20a221045f3f02964d7d8` |
| `inputs.json` | `2a77cf5debc2bab36206b07d9643efec9a4b68950cdc3a4694f23b8bc304795d` |
| `identity-audit.log` (observational run) | `43dab6d65c347dbf8a8b66d9d8939aaa2e0890656dd352477e6724575780ccfa` |
| `acceptance-red.log` | `d5c5fb0d05ef023b45ea25296437c3f80a2942256db6266a7c69500714a93513` |

The existing service hash remains the reviewed storage source blob at bdba9eb/e928bf0; parallel recovery/root dirtiness is intentionally outside this actor's ownership. This is a source-level repro, with no native FAL/UWP/rendered-toast or Windows startup claim. The minimal amendment is ready for independent fresh_audit review.

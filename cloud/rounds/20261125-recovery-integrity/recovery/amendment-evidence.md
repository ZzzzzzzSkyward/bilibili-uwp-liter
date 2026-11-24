# Recovery amendment evidence — frozen source, reproduced residuals

Status: **requirements ready for independent review; no product/test/index/ref/remote changes by this author.** Owned writes are this file and [amendment.md](amendment.md); raw reproductions are scratch only. Read original final requirements/review/execution, actual full VMs/helper/coordinator, the139-check program/primitive stubs/project/extractor, RF1 final review and fresh FA-02–04 findings/structure evidence. No implementation PASS or new complete-gate result is asserted.

## Exact input and seam verification

All915 common source/tooling/wiring inputs matched the accepted common manifest before and after scratch runs: changed=[]; before.json and after.json both SHA256 `42a24dba9e30d91308ce174f5a66a66fa87177550fced38bcda02d172cd4f91e`. This includes the original139-check harness and unchanged inventory/controls. HEAD remains published storage receipt e928bf0; recovery source is locally unpublished.

| Current input | SHA256 |
| --- | --- |
| `Modules/DownloadVM.cs` | `3b76bf88b3b1413368eec75a9316c2b51cf7c8afee97fefd5d1f726d5728b364` |
| `Modules/Home/RecommendVM.cs` | `aba526dc929b08fa88ac187bc64399b872c9b6b8ee219036f91a64ee73e83020` |
| `Helpers/DownloadHelper.cs` | `8aef39bca29b3aebf0954ad66a46326287e37cdbb2b53413540d1b7d2069d2cf` |
| `Services/DownloadPathCoordinator.cs` | `1794aa73628d063b20489e597c35f8115174309331b04f50d89d2fd2f3e14947` |
| `plan/tests/recovery/Program.cs` | `eb755fe0cbbd7e25f577851467bd2f38a717be834fab7688c2a0289b7a2f9688` |
| `plan/tests/recovery/Stubs.cs` | `7c4041d20520641b3438e289109eff93d775e1f142eed3ae9b59701a466c2aa8` |

The project links full current VMs/helper/coordinator/parser and real request/envelope/model code. Scratch copied stubs are byte-identical to the permanent primitive stubs. Reran the existing source extractor **in scratch**, changing only its root/output location:18 inputs/seven unique exact slices; generated wrapper is byte-identical to both permanent obj/RecoverySource.cs and copied RF1 wrappers. No repository generated output was rewritten. Receipt: `/workspace/work/recovery-integrity/recovery-amendment/verification.json`, SHA256 `f4fe1ed77b9478a74be437befe66cadee53d9a2cf885164f475344678e2f1b42`.

Source observations constrain the amendment:

- DownloadVM: LoadDownloadedAsync204–225 checks nonzero media but does not consult known cancellation/retained cleanup. Handel614–621 retires by GUID; the retained failure is independent of active rows. DeleteCleanupAsync768 repeats Cancel and sets WasCancelled only after the entire loop, exposing both poisoned-CTS and partial-sibling reporting gaps. RetryCleanup820 retains original target permission; fresh capture739 has no failed-attempt memory.
- The existing coordinator already normalizes/contains episode paths, changes creation revision at admission and leases episode/subtree mutation. Its revision alone records that creation occurred, not successful preparation/completion. Actual AddDownload knows required preparations/SaveInfo and detached transfer tasks; these existing paths can provide the narrow successful-generation evidence needed to supersede a known partial path without a disk format change. A completed single sibling is insufficient when another preparation or transfer failed.
- RecommendVM accepted action removes from current Items after await without checking collection ownership or recomputing state. Existing request/continuation/retry/no-progress state must remain authoritative during action completion. DownloadVM656 divides0/0; both actual scan531 and progress644 call it.

## Rerun commands and observed results

Scratch root: `/workspace/work/recovery-integrity/recovery-amendment/`. Copied original independent RF1 and fresh-audit projects retain full actual product links. Commands used the exact SDK/runtime and cached Newtonsoft package; each project has a separate bin/obj/CLI directory:

```bash
DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet \
DOTNET_CLI_HOME=/workspace/work/recovery-integrity/recovery-amendment/rf1/cli \
NUGET_PACKAGES=/workspace/work/maintenance-foundation/quality/cache/nuget \
DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 \
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/recovery-integrity/recovery-amendment/rf1/RetryConcern.csproj \
  -c Debug --nologo
# Same environment with matching CLI/project paths:
# audit/RecoveryAudit.csproj
# rf1-extended/RetryConcern.csproj
```

Original RF1 rerun exits1 after successful build, reproducing the safety red:

```text
FIRST failed_targets=1 deleted=0 token_requested=True transfer_pending=True
RETRY failed_targets=0 confirmation_count=2
RETRY deleted=1 transfer_pending=True cancellation_fault_still_active=True
ACTUAL_SOURCE_CANCELLATION_RETRY_UNSAFE=True
```

The added bounded `rf1-extended/Program.cs` uses the same real CTS and unchanged ThrowCancel primitive, actual DeleteItemAsync/RetryCleanupCommand/LoadDownloading, and independent retained/fresh/settled schedules. It exits1 with **two genuine failing safety predicates and six passing controls**:

```text
PASS retained first callback fault retains target without deleting
FAIL retained fails closed before lookup/delete while failed GUID remains current
PASS retained actual observer source remains attached across rescan
PASS fresh first callback fault retains target without deleting
FAIL fresh fails closed before lookup/delete while failed GUID remains current
PASS fresh actual observer source remains attached across rescan
PASS settled first callback fault retains target without deleting
PASS settled explicit retry succeeds after actual operation settlement and disappearance
RF1_EXTENDED_FAILURES=2
```

Each extended scenario actually settles/removes its owned operation and awaits observer row retirement before reset/exit. It supplies no replacement cancellation/cleanup algorithm. These minimal public-seam predicates should be preserved in the permanent red→green cases, with the amendment's additional bulk/partial-sibling/generation/context predicates added before fixes.

The unchanged audit driver exits0 because it prints observations rather than asserting repair success. Its output independently reconfirms all three residuals and existing finite/zero-file controls:

```text
PROGRESS 0/0=NaN;finite=False
PROGRESS 0/10=0;finite=True
PROGRESS 5/10=50;finite=True
PROGRESS 10/10=100;finite=True
PROGRESS 20/10=0;finite=True
AFTER_LAST_DISLIKE rows=0;status='';auto=True;automatic_load_sent=0
CANCEL_FAILURE_SCAN cancelled=True;active=0;cleanup=1;completed_episodes=1;status=''
ZERO_FILE_CONTROL completed_episodes=0
```

The partial-file schedule uses actual1/10 operation progress, successful cancellation, failed DeleteAsync, explicitly pumped observer retirement and actual completed scan. This proves the composed classification defect; it does not assert every nonzero file is incomplete. The recommendation schedule loads an actual one-card feed and sends real action acknowledgement. Linux invalid progress proves a source value defect only.

| Scratch artifact relative to root | SHA256 |
| --- | --- |
| `rf1/run.log` | `88a4ba3f9f8b54fba9795f7d149170a65a25a03f20be1fd7135ca98813415e0b` |
| `audit/run.log` | `50d3313c23c6c9bdee0db050024a55ee04167cc66869de5c9c7c50c16e4467f2` |
| `rf1-extended/Program.cs` | `65565c4eda68276be902a4518286a561232ec9b34350f8124b04c5437baacabf` |
| `rf1-extended/run.log` | `80f31170d643f71b069f5ddc799bdc9f02775e19b4a90344896a342be9c4aa39` |

No full unchanged suite/control/package run was repeated. Existing common51/52 and14 controls remain valid for their old predicates/bytes, not evidence closing these defects. Amendment review PASS precedes any executor fix; root then coordinates exact source ownership and the one new integrated gate. Native UWP/XAML/BackgroundTransfer/file locks/restart remain unverified, and no publication/CI/native/satisfaction claim is made.

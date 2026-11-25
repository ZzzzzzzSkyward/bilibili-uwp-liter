# AF1 independent requirements-review evidence

2026-11-25; independent reviewer `api_requirements_review`. Supports [af1-review.md](af1-review.md). No source/test/tool/main-doc/index/ref mutation or full-suite run. Earlier AS1–AS3 proofs, requirements reviews and scheduling review remain frozen; this evidence concerns only AF1's property publication amendment.

## Actual source and caller inspection

Read ACTIVE AF1 review/amendment dispatches, contracts/af1-requirements/evidence, final-audit and audit-evidence/residual-finding, accepted AS3 table and favorite implementation review. Inspected complete actual property/commands/base/relay and caller/XAML, not only the auditor's description:

| Source | Independent observation |
| --- | --- |
| FavoriteDetailVM.cs:44–65 | Setter writes `_videos=value` before `_videos!=value`; after first initialization that test cannot detect any replacement. Getter mutates stack_will_overflow. Both flags are private and occur only in the declarations and this property. |
| IModules.cs:30–35 | Actual synchronous PropertyChanged event invocation, no suppression or alternate collection policy. Changed collection must be stored before invoking it so a listener sees the new reference. |
| RelayCommand.cs:40–65; VM constructor23–30 | RefreshCommand executes the actual `Refresh` Action; it is not a fake command or a model-only test path. |
| VM118–188,250–284 | Complete real builder/request/parser/mapper/publisher runs. Nonempty first page assigns a new mapped collection; append Add mutates the existing one. Search/Refresh guard Loading, reset Page1/info/Videos, then await actual LoadFavoriteInfo. A failed append returns before replacing rows/info/Page; finally clears Loading. |
| FavoriteDetailPage.xaml:75,87 | AdaptiveGridView.ItemsSource is TwoWay x:Bind to Videos; refresh button binds RefreshCommand. Count-based drag/reorder bindings are separate native behavior and not proved here. |
| FavoriteDetailPage.xaml.cs:33–44,64–67 | Initial navigation calls actual LoadFavoriteInfo; search calls actual Search(searchBox.Text). |
| Repository rg for both flags | Only FavoriteDetailVM's old property/declarations and separately owned MyFollowVideoVM contain them. No field use, external owner or lifetime contract justifies retaining the FavoriteDetailVM flags after identity-based notification. No other VM correction is granted. |

All getter reads must leave property state unchanged. A reference-only setter means null→null and sameA→sameA do not notify; null→A/A→B/B→null each store then notify exactly once. A same-reference callback echo sees the already-stored reference and stops. A later distinct assignment must remain observable regardless of prior getter/echo history. The requirement deliberately does not invent a global policy for callbacks assigning different references.

This is a local notification owner defect: backend request/row parsing and accepted AS3 paging already work. The listener cannot discover the replacement by polling `vm.Videos`; its retained collection reference must change in response to the actual inherited event. CollectionChanged attachment/detachment is an observation primitive, not a copy of the product's notification algorithm.

## Frozen AS3-fixed input verification

Author receipt: `/workspace/work/api-shape-comparison/contracts/af1/receipt.json`; complete snapshot manifest: `as3-fixed-source-manifest.json`. Independently checked and copied all **30** manifest inputs into exclusive `/workspace/work/api-shape-comparison/requirements-review/af1-review/snapshot/`, preserving file bytes and relative project graph. This includes full VM, IModules, RelayCommand, FavoriteApi, parser/mapper/envelope, complete HttpResults, c2 original/action/paging sources and stubs, exact residual Program and caller/XAML read evidence. No author/auditor output directory was built or modified.

Key verified hashes:

| Input | SHA-256 |
| --- | --- |
| Complete AS3-fixed VM and current main VM (equal) | `9e75a062fbee0bcfba9bff2a94aabd398894fdf21a1fe37307dd2028813875c1` |
| Exact ResidualProbe.cs | `93f0b8123245e4b95b1e3572bdafd6ddb6ca84d620a4f6599ce48ef478d90af6` |
| Author copied-input manifest | `cef0769789c80db4333130cba38a0f5ef5c6e3c7da01e643adbf6eb3c3ee0505` |
| Full actual HttpResults class extraction | `6df7b868c05bfa188a13c15601c17215f5c81437ccae7214aaa7e0d70a22f4d2` |
| Existing c2 delivery/platform/signing stubs | `ccccf197a344e6fbe7d5d597a769d4cc1fcf19d976d68f34e3f6b856b66fc8b9` |
| Read-only Newtonsoft13.0.3 net6.0 DLL | `22c649f75fce5be7c7ccda8880473b634ef69ecf33f5d1ab8ad892caf47d5a07` |
| Author replay-red.log | `9227e480346adb6e2c7156243bb1b5ee2ab2424555144dd9f05337272d0c6394` |

Snapshot compiled sources/stubs/read evidence match current main where those files exist. The sole expected current-main difference is the snapshot C2Probe.csproj's private replay adaptation: ResidualProbe StartupObject and a direct reference to the same frozen Newtonsoft DLL instead of the main project's package reference. Complete production methods and every original fixture/stub are unchanged; ResidualProbe exists only in private proof, not the main tree. Private `input-check.json` records each comparison. The AS3 functional hasMore decision at VM169–176 remains exactly present in this baseline, so AF1 red is not obtained by undoing AS3.

The author's and auditor's original pre-API and AS3-fixed results both report the same10 active positives/2 binding failures, supporting a preexisting residual rather than an AS3-introduced defect. This review independently replayed the AS3-fixed snapshot only; it did not rerun earlier accepted AS1–AS3 proofs or claim new old-baseline results.

## Independent actual-source replay

From the copied private project, with unchanged ResidualProbe/project/source/stub/decoder/DLL bytes:

```bash
export DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet
export DOTNET_CLI_HOME=/workspace/work/api-shape-comparison/requirements-review/af1-review/cli-home
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
"$DOTNET_ROOT/dotnet" run --project /workspace/work/api-shape-comparison/requirements-review/af1-review/snapshot/plan/tests/c2/C2Probe.csproj
```

SDK8.0.427/runtime8.0.33; no tool/package installation or repinning. **Exit1, RESIDUAL passed=10 failed=2.** Private log: `af1-review/independent-red.log`.

- Search: initial publication signal PASS; actual two-request command completion/state PASS; model accepts distinct matching rows PASS; raw `C++&literal%2B` encoded once/pn1 request PASS. Desired-safe binding check FAIL: signals1, bound view=initial, model=matching.
- RefreshCommand: the same four active controls PASS; desired-safe binding check FAIL: signals1, bound view=initial, model=updated.
- Append: existing collection and row signal identity/order PASS; accepted terminal Page2/Load-more/Loading/two-request state PASS.

The transport returns completed Tasks in this exact residual proof, so Search/RefreshCommand/LoadMoreCommand execute synchronously through their full async-void call paths before the assertions. It does not use sleeps or infer quiescence from elapsed time. The proof does not yet exercise suspended command completion; the AF1 final fixture must add deterministic TaskCompletionSource gates and actual async-void completion/fault observation for pending/reset/guard/failure/retry cases as explicitly required. The existing c2 FavoriteActionChecks.ActionCompletion (OperationStarted/Completed, pending Post tracking, captured errors and Done task) is an available primitive pattern, not permission to substitute fake commands or publication logic.

## Required final evidence and retained semantics

The requirements correctly distinguish:

- Idle loaded Search/Refresh: an existing nonnull→null assignment now emits one Videos event and detaches the old collection before awaiting; accepted new nonempty rows emit one replacement event. Repeated journeys must continue to work on one VM.
- Loading guard: no second request, model reset or Videos event. Pending-response tests must wait until the actual command completes, with bounded timeout/fault checks, not treat blocked work as success.
- Failed search/refresh: existing reset leaves model/binding null, current Page1/error/completion semantics persist, and a subsequent retry can publish. Failed append retains the accepted collection/rows/info/Page without a Videos replacement event.
- Successful append/remove/reorder: one retained collection and its own CollectionChanged events. Distinct equal-content collections still require a property event. Old detached collection mutations must not appear as current bound rows.
- First-empty/missing-info/normal bool/absent/null/Type21/count outcomes: preserve accepted AS3 assignments, row/info identity and timing; only notifications of real existing reference assignments change. Do not rewrite LoadFavoriteInfo, commands, requests, parser or mapper to fix publication.

The final additive fixture must preserve the released ResidualProbe body/assertions (entrypoint/name adaptation only), all10 positives, and add the exact full property table, bounded echo/getter reads, actual command/error/pending controls. Same complete frozen AS3-fixed original versus repaired source, identical final fixture/stub/tool/dependency bytes and genuine compiled old behavior failure are mandatory. c2+c3 after repair and later implementation/fresh review remain required. No original/additive assertion, AS1–AS3 table or51/52 inventory may be relaxed.

## Custody and scope

Prior review/as3/scheduling artifacts were hashed at entry and left untouched. The source/property, author snapshot/manifest/log, exact residual fixture, complete decoder and DLL remain unchanged at finish. No product/tests/tools/main docs/index/refs/cache writes or full suite occurred; sole repository outputs are `af1-review.md` and `af1-review-evidence.md`.

All AF1 artifact/source/snapshot/fixture read custody is **RELEASED**. Private review scratch is frozen. PASS permits only root's later exact same-owner AF1 grant; it is not source/index/ref/publication authority. Source notifier proof establishes neither native XAML/Windows/UWP behavior nor service/session/live/account/write acceptance; all existing account/live/proto/native gates remain intact.

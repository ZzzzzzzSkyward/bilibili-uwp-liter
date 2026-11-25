# AF2 independent review evidence

2026-11-25, independent `api_requirements_review`. Supports [af2-review.md](af2-review.md). Read ACTIVE review/discovery dispatches, AF2 requirements/evidence, actual full VM/property/base/relay/API/decoder/helper/models and FavoritePage XAML/callers. AS1–AS3/AF1 remain accepted and frozen; no full suite or unchanged earlier proof was rerun.

## Source and proposed dependency boundary

- `Modules/User/MyFollowVideoVM.cs:31–51` has the same assign-before-compare bug as AF1: `_myFavorite=value` precedes its comparison and inited_favorite suppresses later events. Getter mutates stack_will_overflow. Repository search now finds these two private flags only in this VM's declarations/property; there is no external owner requiring them.
- `CollectFavorite:54–59` has its own working unconditional notifier. `Loading:23–27`, HasMore60–65 and private Page66 are independent state. AF2 must not change them or add new single-flight/cancellation/paging policy.
- `FavoritePage.xaml:80,97,107` binds MyFavorite TwoWay, CollectFavorite OneWay and visible RefreshCommand. Code-behind64–71 loads the selected tab;120–139 invokes Refresh after create/edit/delete;141–161 delegates sorting and has unreachable binding-switch code after return. No actual dialog/write was performed.
- Complete real `IModules.DoPropertyChanged` and `RelayCommand.Execute` are already c2 inputs. Full MyFollowVideoVM also supplies the actual FavoriteItemModel, so no duplicate folder model or parser stub is needed.
- `FavoriteAPI.MyFavorite:17–25` and MyCreatedFavoriteList27–37 remain existing signed app-family calls with their current access-key/request conventions. Synthetic `space_infos` fixtures are evidence of this legacy caller's code path, not server schema/liveness or a route migration requirement.
- Actual `Utils.DeserializeJson<T>` at Utils.cs287–293 awaits Task.Run and calls JsonConvert.DeserializeObject. It must run, not be reimplemented as a synchronous stub. The helper has one exact signature occurrence and its complete body matches the discovery wrapper. Complete actual HttpResults matches the extracted final class, including legacy GetJson/GetData/salvage behavior.

The snapshot's only C2Stubs change is `public static class Utils`→`public static partial class Utils`, independently compared against main bytes; all method bodies/declarations remain. Snapshot C2Probe.csproj adapts only a private StartupObject/direct frozen Newtonsoft reference and adds the real VM Compile link. The proposed eventual main project/runner changes are accordingly bounded: real VM/generated helper Compile entries, partial declaration, unique complete-body fail-closed extraction. No product helper, decoder, notification or default-folder algorithm may move into a stub. Missing/duplicate/incomplete helper boundaries must fail, and exact complete body equality must be bound in final evidence.

## Independent original fixture replay

Verified AF2 receipt SHA `e7408ffb075dbd04c493b3799d711d06962ba83ef702e2d52c337dbaaf8c8fed`, all33 compiled/runnable input records, five tool/dependency records,32 source snapshot records and original result hash. Copied the exact inputs into exclusive reviewer `af2-review/snapshot/`, preserving the project/source/helper/stub/fixture bytes; no author snapshot/output was built or changed.

Important bindings:

| Input | SHA-256 |
| --- | --- |
| Current main and frozen pre-AF2 MyFollowVideoVM | `98a1ef21f38816cb020eb7512fdd4c05fbfb0abd168c7f9e729edf23f59bcabe` |
| AF2Probe13 assertions | `dbe4ccfc7069a3bb0e70aef8ce6f31380abffe814913549848793990bece5600` |
| Source-manifest.json | `3ea1ce583a92fe963a5b10dbbbacd53c3f1a84f5fd1be3134a9f8c2ccecf01e9` |
| Exact generated DeserializeJson wrapper | `6e731a090e6537fb03635bc5360ad50ba35936a870c12698fc4ff696fb0136da` |
| Complete HttpResults extraction | `6df7b868c05bfa188a13c15601c17215f5c81437ccae7214aaa7e0d70a22f4d2` |
| Existing read-only Newtonsoft13.0.3 DLL | `22c649f75fce5be7c7ccda8880473b634ef69ecf33f5d1ab8ad892caf47d5a07` |
| Author original-red.log | `70e2f715173bb72ffb93480095833a81aa9b15d8899bf1930f37d120c4b5a7ad` |

The copied AF1-fixed FavoriteDetailVM hash is `daf13e90f68cff339e4a9dc9fc345187e09beeda5c27f4053383efdd2d7aba26`; it is not reverted to get AF2 red. Existing FavoritePublicationProbe, paging/action assertions and accepted AS3 sources are copied unchanged. Private `input-check.json` records all bindings, helper/decoder equality and sole partial-stub adaptation.

Command, with SDK8.0.427/runtime8.0.33 and a private CLI home:

```bash
export DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet
export DOTNET_CLI_HOME=/workspace/work/api-shape-comparison/requirements-review/af2-review/cli-home
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
"$DOTNET_ROOT/dotnet" run --project /workspace/work/api-shape-comparison/requirements-review/af2-review/snapshot/plan/tests/c2/C2Probe.csproj
```

Independent **exit1; AF2 passed=11 failed=2**, log `af2-review/independent-red.log`. Initial created/default order and one default Insert event pass; collected/HasMore/Loading and actual signed-family route/identity seam pass. Pending actual RefreshCommand nulls both model properties and starts its request, with the working CollectFavorite reset signal, but the bound MyFavorite fails to clear. Real Task.Run decoding then installs updated/default-created model rows, but the listener still has signals1/old folders. CollectFavorite replacement/completion remains active. Real LoadCreateList preserves collection identity and emits one add with exact large string ID, unchanged collected state and actual Page2/ps20 request. The observer changes its retained reference only on actual MyFavorite events; no model polling repairs it.

The fixture uses a delayed response TaskCompletionSource and Loading=false milestone with a5-second bound. Actual LoadFavorite has no semantic work after its finally Loading=false, and Refresh only awaits that task, so the milestone supports the stated data/publication result. Final enlarged async-void tests must additionally observe operation/continuation settlement and captured faults deterministically (as required), rather than treating a blocked path or elapsed sleep as completed. No native dispatcher/XAML behavior is claimed.

## New actual-source failure-boundary controls

This review found that the proposed unconditional failure-retention promises are false for the unchanged legacy methods. A separate exclusive `af2-review/boundary-snapshot/` copies the exact same complete source/helper/stub/project inputs and adds only reviewer `FailureBoundaryProbe.cs`. It invokes actual awaited LoadFavorite/LoadCreateList; it does not copy a parser, loading or notification algorithm. Private-field reflection reads Page only; no flags/state are reset or written. No async-void command is needed for these Task-returning boundary checks.

Command uses the same environment/tool graph:

```bash
"$DOTNET_ROOT/dotnet" run --project /workspace/work/api-shape-comparison/requirements-review/af2-review/boundary-snapshot/plan/tests/c2/C2Probe.csproj -p:StartupObject=FailureBoundaryProbe
```

**Exit0, BOUNDARY passed=7 failed=0**; log `af2-review/failure-boundary.log`. These are unchanged-current-behavior controls, not desired new failures or live payload captures.

| Actual complete-method input after an accepted initial list | Observed outcome |
| --- | --- |
| Direct LoadFavorite: valid new created list, missing default-folder detail | MyFavorite is already a different reference containing `created-partial`; error toast1; Page1, HasMore=false, Loading=false; accepted old CollectFavorite remains. |
| Direct LoadFavorite: valid new created/default data, `has_more:"not-bool"` | MyFavorite already contains `default-partial,created-partial` in a new collection; same error/reset state; old collected reference remains. |
| LoadCreateList: valid new row followed by `has_more:"not-bool"` | Same created collection already contains the new row; error toast1; Page2 stays, HasMore=false; collected reference unchanged. |
| Successful same-Page retry after that append failure | Both requests use pn2/ps20; row is appended again, Page becomes3. No transactional rollback or dedup exists in this method. |

Exact source explains these outcomes: LoadFavorite assigns at82, default Insert at87, has_more cast88, Page++89, collected assignment93; catch106–109 reports the error after any prior mutation. LoadCreateList appends135, has_more cast137, Page++138; catch150–153 similarly does not undo rows. Refresh resets both references193–194 before calling this same LoadFavorite; a later created-list assignment can therefore leave it nonnull even if a subsequent step fails. The source cannot promise all failed refreshes stay null, all failed direct loads keep the old reference, or all failed appends keep old rows.

The current property setter's missing notifications can hide these already-published model references from the binding. The intended identity setter will make real assignments observable; retaining unqualified null/old-reference failure promises would force forbidden transactional changes or false tests. Revision should distinguish pre-publication transport/business/decode failures from post-assignment/append errors, explicitly preserve current partial-publication limitations, and add unchanged-green controls for both. No rollback, parser migration or duplicate-row repair is inferred from this requirements finding.

## Accepted contract and remaining gate

Identity/null/first/same/distinct/equal-content reference table, pure getter and removal of only this VM's two obsolete flags are appropriate. Same-reference two-way echo must stay bounded with a failure-recording guard; later distinct references still notify. CollectFavorite remains byte-identical and unconditionally notifies. Created/default/collected ordering, in-place mutation/reorder signals, original13 assertions/11 active positives, all AS1–AS3/AF1 bytes and source proofs remain mandatory. Final same-fixture original-red→repaired-green must execute the actual complete helper/decoder/VM/commands and preserve corrected current-behavior failure controls; c2+c3 and later stable51/52 remain required.

Only P1 error-boundary text requires changes. This is not evidence for expanding the repair into endpoint/parser/auth/action/paging work. Native FavoritePage/dialog/reorder, UWP build, service schema/liveness, real writes/accounts/cookies/session/liveU2 and Windows memory remain unverified under existing gates.

## Custody

All author inputs/results, copied original proof files, source/helper/decoder/dependencies and prior AF1/AS3 contracts/reviews are unchanged at finish. Reviewer outputs are only af2-review.md/af2-review-evidence.md plus exclusive private scratch; no main product/tests/project/stubs/runner/tools/docs/index/refs/cache mutation occurred. No full suite, old proof-label rerun, Bilibili request or foreign execution.

All AF2 artifact/source/snapshot/fixture read custody is **RELEASED**. Private review scratch is frozen. Requirements author must correct the P1 and release for independent re-review before root's precise same-owner grant; this review opens no code/publication window.

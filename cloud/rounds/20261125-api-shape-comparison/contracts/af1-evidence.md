# AF1 evidence — accepted favorite rows do not reach the binding

Status: **REQUIREMENTS EVIDENCE COMPLETE / released for review**, 2026-11-25 UTC. Author grant is [af1-amendment-dispatch.md](../af1-amendment-dispatch.md). This is a new source-notification residual, independently proved by the fresh auditor after AS3. It is not a sibling-server claim or AS3 regression. Main source/test/docs/tools/index/refs and previous requirements/proofs were not edited. Auditor scratch was read and copied into exclusive AF1 scratch; it was not mutated or executed in place.

## Exact source and callers

| Actual source | Observation |
| --- | --- |
| FavoriteDetailVM.cs:44–65 | Videos stores _videos=value before testing _videos!=value, so that comparison is always false; inited_favorite permits only the first event. Getter resets stack_will_overflow. A later distinct collection changes the model without inherited Videos notification. Both private flags' uses in this VM are entirely within this property/its declarations; they have no lifecycle, API, paging or external owner. |
| IModules.cs:30–35 | Real PropertyChanged event and DoPropertyChanged synchronously invoke subscribers with PropertyChangedEventArgs(name). The notifier is active; no shared-base replacement is needed. |
| FavoriteDetailVM.cs:118–183 | Successful nonempty first-page API→typed parser→mapper assigns Videos=data.medias. Append mutates existing Videos with Add. AS3's accepted paging decision follows publication and remains unchanged by AF1. |
| FavoriteDetailVM.cs:249–285 | Refresh and Search first guard Loading, reset Page/FavoriteInfo/Videos, then await real LoadFavoriteInfo; constructor maps RefreshCommand to the real RelayCommand(Refresh). Null reset and new nonempty assignment both need publication when their reference changes. |
| FavoriteDetailPage.xaml:75,87 and code-behind:33–47,64–67 | AdaptiveGridView.ItemsSource uses x:Bind favoriteDetailVM.Videos, Mode=TwoWay; navigation performs initial LoadFavoriteInfo; search input invokes Search(searchBox.Text); refresh binds RefreshCommand. Row-count drag/reorder bindings and native rendering are separate from the offline notifier proof. |

Source field scope was checked with rg for stack_will_overflow/inited_favorite across src/BiliLite.UWP. Another VM has separately owned similarly named fields; no grant or fix for that class is inferred. Removing the two FavoriteDetailVM flags follows from the proposed pure getter/identity setter and bounded same-reference echo contract, not a global observable cleanup.

## Independent reproduction on the frozen AS3-fixed complete source

Read the auditor's [residual-finding.md](../audit-evidence/residual-finding.md), full ResidualProbe.cs, C2Probe project, snapshot.log and original-snapshot.log. Auditor's identical fixture executes both AS3-fixed and pre-API-round complete VM and records10 positive checks/2 binding-publication failures on each. Its initial load and in-place append positives prove the event/collection boundary is active. Those receipts establish preexisting behavior, not a new AS3 paging regression.

For independent author replay, copied every explicit production Compile input, the full actual HttpResults extraction, local c2 fixture/stub sources and exact ResidualProbe bytes from the auditor's frozen AS3-fixed snapshot into /workspace/work/api-shape-comparison/contracts/af1/replay/. Copied XAML/caller source for read evidence; no foreign implementation was involved. Own replay project retains full production VM/getter/setter/Search/RefreshCommand/IModules/RelayCommand/API/parser/mapper/envelope and exact decoder, with only existing c2 platform/delivery/signing primitives. There is no copied publication/paging policy in a stub or observer.

Command: bash /workspace/work/api-shape-comparison/contracts/af1/run-replay.sh

Outcome: **exit1;10 positive checks PASS, exactly2 desired-safe binding checks FAIL**. Search and refresh each complete two real builder/parser/mapper requests, accept a distinct nonempty model collection, preserve encoded keyword/pn and terminal state, but listener records signals=1 and retains initial rows while model has matching/updated rows. The positive append control keeps the same bound collection and receives one CollectionChanged add. Own private log is af1/replay-red.log; auditor logs and scratch remain untouched.

| Frozen binding | SHA-256 |
| --- | --- |
| AS3-fixed complete FavoriteDetailVM.cs | 9e75a062fbee0bcfba9bff2a94aabd398894fdf21a1fe37307dd2028813875c1 |
| Exact ResidualProbe.cs fixture | 93f0b8123245e4b95b1e3572bdafd6ddb6ca84d620a4f6599ce48ef478d90af6 |
| Complete copied source/stub/caller input manifest | cef0769789c80db4333130cba38a0f5ef5c6e3c7da01e643adbf6eb3c3ee0505 |
| Own replay-red.log | 9227e480346adb6e2c7156243bb1b5ee2ab2424555144dd9f05337272d0c6394 |

Private af1/as3-fixed-source-manifest.json binds all copied inputs; af1/receipt.json binds fixture/project graph, exact current-main VM equality, script/log/package and SDK8.0.427/runtime8.0.33. Existing Newtonsoft13.0.3 DLL is read-only; no dependency/tool installation or repinning occurred. No main-tree probe was run. The current-main VM equals the copied AS3-fixed VM at proof time; a later executor must verify this baseline rather than revert AS3.

## Required closing proof and limits

[af1-requirements.md](af1-requirements.md) states the exact property identity/notification table, same-reference bounded echo, pure getter, null reset/new reference, repeated real search/refresh, pending response/failure/retry and append controls. First null→null no-op is an explicit minimal reference-property decision; first nonnull publication remains required. The prior flags' first-event exception cannot suppress later notifications. A binding listener must update only on the actual Videos event and must track CollectionChanged subscriptions/retained row IDs; polling model data would miss the reported defect.

The final additive c2 fixture must preserve this exact residual proof's assertions and10 active positives, add all bounded table/command controls, then execute unchanged against this AS3-fixed complete baseline (genuine behavior red) and repaired source (all green). Requirements do not claim the enlarged final fixture or repaired source has already passed. Existing c2 action/paging/nullable/first-empty/Type21/failure/ID checks and AS1/AS2 proofs remain frozen and mandatory. No account/live/session/native or real XAML rendering pass follows from source notification acceptance.

All AF1 requirement/artifact/snapshot/replay read custody is **RELEASED** for independent api_requirements_review. Private AF1 inputs/results are frozen. Existing reviewer/auditor read custody remains theirs until separately released; this author does not grant a code window. Only contracts/af1-requirements.md and contracts/af1-evidence.md were written in the repository; all other writes stayed under exclusive contracts/af1 scratch.

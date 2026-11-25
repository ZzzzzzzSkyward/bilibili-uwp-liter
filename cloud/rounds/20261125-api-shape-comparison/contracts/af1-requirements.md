# AF1 — favorite collection reference publication

Status: **READY FOR INDEPENDENT REQUIREMENTS REVIEW**, 2026-11-25 UTC. Requirements-only amendment under [af1-amendment-dispatch.md](../af1-amendment-dispatch.md); no code grant. Read [af1-evidence.md](af1-evidence.md) and the fresh [residual finding](../audit-evidence/residual-finding.md). Original AS1–AS3 requirements, accepted paging table and every prior fixture/assertion remain frozen. AF1 is a preexisting P1 reproduced both before and after AS3; it does not reopen AS3's source/nullable/empty decisions.

## Problem and smallest supported change

The actual FavoriteDetailPage ItemsSource binds TwoWay to FavoriteDetailVM.Videos. Initial accepted rows notify once, but subsequent successful Search/Refresh replace the model collection without notifying the binding; it can continue showing the initial rows. The setter stores the incoming value before comparing it, so the comparison is always equal once its initialization flag is set. The getter also mutates the recursion flag. Actual Search and RefreshCommand reset Videos to null and then assign a newly mapped collection after a successful nonempty first page. Same-reference append correctly uses ObservableCollection.CollectionChanged and must remain.

Repair only the Videos reference property's ownership/notification: compare old versus incoming reference before assigning; a real reference change stores the new reference before emitting exactly one inherited DoPropertyChanged("Videos"). The getter returns the stored collection without mutating property state. Identical-reference assignment is a no-op even during a notification; a same-reference two-way echo therefore cannot start another notification. No global event suppression, asynchronous notification queue, broad reentrancy framework, collection copying or shared IModules/XAML change is supported by this evidence. The two private flags stack_will_overflow and inited_favorite have no use outside this property's old getter/setter in FavoriteDetailVM and become obsolete under identity-based notification; remove that dead state rather than retain a competing policy. Similarly named state in another VM is outside this grant.

## Exact observable contract

Reference identity, rather than equal row contents or collection count, determines replacement. Here A/B/C are distinct ObservableCollection instances; the default stored value is null. Tests observe the real inherited PropertyChanged event and the reference/rows retained by a binding listener, not just vm.Videos.

| Input/state | Required property notification and bound outcome |
| --- | --- |
| Fresh null; getter read repeatedly | Returns null; no event or state change. |
| Null → null, including first explicit null assignment | No reference change, no Videos event; bound value stays null. This removes the old initialization-only null event; no caller depends on it and no existing rows need clearing. |
| Null → first nonnull A | Store A; exactly one Videos event, getter already returns A during callback, listener retains A. |
| A → same A, with/without intervening getter reads | No event; preserve A and row identities/content. |
| Videos callback reads current A and writes same A back | No nested Videos event; one event for the outer real change. Bound observer keeps A. Use a bounded echo counter/guard to detect recursion without risking stack overflow. |
| A → distinct B (including identical row contents) | Store B; exactly one event; listener unsubscribes A and subscribes/retains B. Later distinct assignments remain observable. |
| Nonnull B → null | Store null; exactly one event; listener clears and detaches B. |
| Null → null again → new C | Repeated null emits none; new C emits exactly one and reaches listener. Prior initialization/echo history cannot suppress it. |
| Existing collection append/remove/reorder | Keep same collection; its CollectionChanged reaches the bound listener. No extra Videos property event is required for an in-place mutation. |

The bounded supported reentrancy is a listener reading/writing the same current reference, matching a two-way binding echo. No behavior change for a listener that intentionally assigns a different reference inside a callback is requested; do not add a global policy for that unobserved case.

## Actual command and error outcomes

Run the complete actual Search(string), RefreshCommand→RelayCommand.Execute→Refresh, LoadFavoriteInfo, Videos getter/setter, IModules.DoPropertyChanged, API, parser and mapper. Preserve command scheduling/ownership and raw keyword/paging semantics. While Loading=true, Search/Refresh retain their current no-op guard: no new request, reset or Videos event. For an idle loaded collection, Search/Refresh's existing nonnull→null reset must now emit one event and clear the listener before the pending response; a later accepted nonempty collection emits one replacement event and reaches the listener. Repeated successful search/refresh journeys must keep working without reconstructing the VM.

A failed search/refresh after its existing reset leaves the model and binding null, clears Loading, retains current categorized notice and command Page1 state, and permits an existing retry path to accept/publish a new collection. A failed append preserves the accepted reference/rows/info/Page and does not emit a Videos replacement event; retain the existing hidden-load-more/Nothing/Loading/notice behavior. Successful append keeps one collection with one collection-add signal per accepted row. First-empty and missing-info publication/order/rows behavior stays exactly as accepted AS3, including its pre-call Videos identity; Search/Refresh's preceding explicit reset is still the existing caller behavior. Nullable/Type21/count fallback, string IDs, optional payload and action acknowledgement rules do not change.

## Acceptance and exact proposed ownership

Root may grant the existing sole api_favorite_executor, after every affected reader releases and independent AF1 requirements PASS, only:

- Product: src/BiliLite.UWP/Modules/Detail/FavoriteDetailVM.cs — Videos property's getter/setter and its two obsolete private flags. Keep the accepted AS3 LoadFavoriteInfo/paging bytes and all command/request/parser logic unchanged.
- Existing c2 probe: additive plan/tests/c2/FavoritePublicationProbe.cs and one additive dispatch in Program.cs. Current c2 project already includes local source files. Existing project/stubs/runner/AS3 fixtures stay byte-identical unless a separately evidenced primitive boundary extension is explicitly granted; no implementation policy may move into a stub.
- The owner's exact execution/evidence artifact paths granted separately by root. No XAML, shared IModules/RelayCommand, transport/parser, other VM, global observable refactor, main docs or Git window is inferred.

The final additive fixture must preserve the released full-source ResidualProbe assertions/expected outcomes (entrypoint/name adaptation only), then add every property-table boundary and actual command/error outcome above. The binding observer must attach/detach CollectionChanged on property notification, keep its own retained collection, and assert its displayed row IDs; it cannot poll the model to replace its bound collection without an event. Include same-reference echo during callback, getter-before/after repeats, distinct same-content collection, null clear/repeated null/new reference, multiple search/refresh cycles, pending-response clear, Loading guards, failure/retry and append positives. Completion of async-void commands must be observed through existing deterministic boundary scheduling/completion primitives, with bounded guards/timeouts; a blocked or uncompleted path is not a pass.

Capture the **same final fixture/stub/tool/dependency bytes** against the frozen complete AS3-fixed VM and repaired VM. The former must genuinely fail binding-publication assertions after executing actual methods (not fail compilation); the latter must pass all assertions. Frozen independent residual fixture currently reproduces10 positives/2 desired-safe failures; its initial publication and same-reference append positives stay. New expected-safe null/reset/distinct cases may add genuine old-source failures; unchanged positives cannot be weakened. Bind all compiled sources, complete HttpResults decoder and caller-read/XAML evidence with hashes. AF1 original means AS3-fixed pre-AF1 source, so original AF1 red may not regress AS3 to obtain failure.

Run owning c2 plus c3 affected regression after repair and preserve original c2 Program/action/AS3 assertions and all AS1–AS3 proofs. No new aggregate stage/denominator or whole-tree run is requested during requirements/audit. The eventual single stable51-logical/52-executable gate follows all final writers/reviewers. Notifier/command source acceptance does not certify Windows/UWP XAML rendering, focus/reorder behavior, compilation, live service, cookies/session or native execution. AccountP4/P5 and liveU2 gates remain unchanged.

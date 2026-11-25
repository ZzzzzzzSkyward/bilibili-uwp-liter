# Later PGC order and terminal-page requirements

Status: **FROZEN / RELEASED FOR INDEPENDENT REQUIREMENTS REVIEW**. Author `later_pgc_requirements`, 2026-11-25 UTC. This discovery supports exactly two bounded proposals, **FI2** and **FI3**. It grants no implementation, shared-build, source/test/main-document or Git authority. The sole authoritative deadline remains **2026-11-25 08:29:30 UTC / 16:29:30 Asia/Shanghai**; timer19 is nonpersistent and does not replace the deadline file.

Both findings are reproduced against immutable archive baseline **A `7fc3c524fde4139d01e7ecd362647ca771c64ea8`**, copied from Git objects into exclusive private scratch. No moving FI1/FD1 candidate was read as evidence. The complete-source fixture has **140 checks:127 active positives pass,11 new desired-safe regressions fail, and2 separately labeled old-A FI1 fresh-navigation regressions fail**. Commands, exact input/fixture/tool hashes, source custody and limitations are in [evidence.md](evidence.md). No repaired-green result is claimed.

## Shared existing interface and boundaries

The real chain is `AnimeIndexPage.OnNavigatedTo` → `SeasonIndexVM.LoadConditions` → real `SeasonIndexAPI`/`ApiHelper` app signer → response delivery → complete real `HttpResults.GetJObject` → the VM's existing `JsonConvert`/JToken reads → selected `Conditions` → real result builder/signer → legacy `data.list` publication → `MyAdaptiveGridView` loading/scroll callbacks → `LoadMoreCommand`. The XAML binds order labels through `DisplayMemberPath=name`, selected values through `current`, and grid loading to the VM. Neither proposal changes that chain's ownership or creates a typed index parser.

| Interface/data | Contract retained |
| --- | --- |
| Navigation | `SeasonIndexParameter` retains its existing types/defaults: type Anime1, area/style/year/month `-1`, order string **`3`**. Twelve explicit real navigation constructions across five callers leave order at that default. |
| Condition request | Existing GET against `ApiHelper.API_BASE_URL`, `/pgc/season/index/condition`; existing LoginKey/MustParameter with `needAccesskey=false`, signature and no-cookie mode; exact season_type/type0. |
| Condition response | Existing legacy code check and **data.filter/data.order**, existing casts and filter selection. Each order's wire `field` becomes `keyword`; its `name` remains display text. No `data ?? result` fallback, salvage change, allowlist, decoder replacement or new empty-order recovery. |
| Result request | Existing `/pgc/season/index/result`, GET/no cookies and same app fields/signature; **page/pagesize24**, season_type/type0, existing field names and once-escaped selected keywords, existing sort0. No host/auth/cookie/route/signature/page-size migration. |
| Result response | Existing legacy code check, **data.list** decoding, row models and data-only payload path; add only FI3's narrowly recognized optional **data.has_next** terminal information. Keep missing list's current empty-page meaning. |
| Ownership/readiness | FI1's reviewed independent condition/result ownership, metadata readiness, rejection, successful notification order and valid page2 autofill remain normative. FI2/FI3 do not reopen or replace those requirements. |

## FI2 — select an exact requested order code before existing fallbacks

**Observed defect.** In `LoadConditions`, wire `order[].field` is stored in `keyword`, but selection compares `order[].name` with `Parameter.order`. If the returned catalog lists field4 first and field3 second, ordinary default order3 selects/sends order4. An explicit nonfirst order4 likewise sends order3. A display label equal to `3` can defeat the actual field3 entry. The full real page/VM/API/signer fixture reproduces all of these and label-renaming/opaque-code/numeric-field cases: **6 desired-safe failures**. Current public selection already sends `current.keyword` correctly; it is a positive control and needs no page change.

**Required behavior.** First choose the first decoded order whose **keyword exactly equals `Parameter.order`**. If no exact code matches, retain the existing label-match fallback and then the existing first-order fallback, in the same order. These fallback behaviors are existing compatibility, not evidence that labels are wire codes. Keep string equality and decoded order order; do not trim, case-fold, parse numeric codes, canonicalize, normalize, restrict known values or create another fallback. Preserve the default `3`, existing field-to-string casts and all filter selection. Empty/missing/malformed order catalogs retain their current error/old-metadata behavior.

| Selection input/catalog | Required outcome |
| --- | --- |
| Default `3`, field3 returned after field4 | Select field3 and send order3. |
| Explicit `4`, field4 returned after field3 | Select field4 and send order4. |
| A different item's label equals requested code | Exact field/keyword match has priority over that label. |
| Matching field with changed/localized display text | Same exact code selection and signed query value. |
| Unknown but exact matching string field, including spaces/Unicode/query characters | Select that exact field; preserve existing value encoding. No numeric/known-code policy. |
| No matching code but matching legacy label | Preserve existing label selection. |
| No code/label match, null or whitespace-variant request | Preserve existing first-order fallback; no normalization. |
| User changes `current` through the existing selection path | Existing page1 reload sends selected keyword, independent of unchanged navigation Parameter.order. |

**Pinned field-specific support.** S1's condition models distinguish `field`, `name` and filter `keyword`; its real index controller sends order.field, and its real view displays name while comparing/submitting field. S3's exact index request and Order enum distinguish code3 followers/code4 score from descriptions. These support the field/code boundary, not adoption of either sibling's defaults, transport or UI. Our label fallback is retained solely for local compatibility. No current service capture is asserted.

**Scope.** `SeasonIndexVM.cs` order-current selection only. Same index owner's additive season1 tests only. No API/page/grid/model/parser source edit.

## FI3 — respect integer zero on an accepted nonempty terminal result

**Observed defect.** `LoadResult` never reads index `data.has_next`; only an empty list sets its internal `CanLoadMore=false`. A successful nonempty page with integer has_next0 therefore still dispatches another page through the actual public LoadMore command. A short real grid also immediately requests page2 during Loading=false after terminal page1. The fixture proves **4 command/row/page failures plus1 real-grid failure**. This concerns the PGC index endpoint's integer flag, not favorite or polymer boolean flags.

**Required behavior.** For a successful response whose existing list decoder accepts a nonempty list, recognize **JSON Integer value exactly zero** as terminal and commit the internal no-more state for that accepted result before Loading=false can invoke the real grid. Publish/append all accepted rows and advance Page once, just as for an existing nonempty success. Emit no new toast or public/property notification. A later LoadMore/direct append must decline dispatch. Compute recognition without an unchecked numeric conversion or any operation that can throw for unsupported values after partial publication.

Only this integer-zero terminal fact is added. Integer1 retains normal continuation. Absent/null, other integers, strings, booleans, floating values, objects and arrays retain the current list-based behavior; they cause neither rejection nor new notices. In particular false, `"0"` and floating0.0 must not become terminal. A huge integer cannot overflow into a failure. Do not impose S1's separate absent/null-terminal policy on this already shipped client.

| Result/entry | Required compatible publication, paging and next action |
| --- | --- |
| First accepted nonempty list, integer0 | Replace rows, advance Page1→2 once, mark terminal before result-owner completion; no next append request/new end toast. |
| Accepted nonempty append, integer0 | Retain old rows and append in order, advance once, mark terminal; subsequent append leaves rows/Page unchanged. |
| Accepted nonempty list, integer1 | Existing continuation; short-grid page2 autofill remains permitted after page1 publication/Page advancement. |
| Nonempty list with absent/null flag | Existing continuation; absent remains an active real-grid autofill positive. |
| Nonempty list with string0/1, true/false, float0/1, other/negative/huge integer, object/array/arbitrary string | Ignore unrecognized optional flag, retain list-based continuation/no new errors. No scalar coercion or favorite-policy reuse. |
| Empty/null or currently missing list, any flag including integer1 | Existing empty-page stop, no Page advance, existing “加载完了” notice; append retains old rows. |
| Business rejection, transport failure, wrong payload key or list decode failure, even with has_next0 text present | Retain existing notice/clear policy; an unaccepted response cannot commit terminal state. Append retains old rows and same retry Page. |
| Legitimate public retry after append failure | Same Page requested once through the existing command; successful rows then publish normally. |
| First-page/Refresh failure | Preserve existing Result=null/Page1 clearing and existing notices; later actual Refresh can retry. No transactional-row redesign. |
| Refresh after terminal result | Existing Page1 reset and first-page continuation reset; replace rows, then permit append if the new accepted response is nonterminal. Keep usable Conditions; no redundant metadata request. |
| Successful completion notification | Keep existing row/collection publication and Page progression order. Terminal fact must already be committed when FI1 releases the result owner and emits Loading=false. Do not gate/suppress all callbacks: integer1/absent must still allow legitimate held page2 autofill. |

**Pinned endpoint-specific support.** S1's exact `/pgc/season/index/result` request decodes data into an `int? hasNext` from `has_next`; its real `PgcIndexController.getDataList` consumes integer0 as terminal. S1 also treats null as terminal; that extra behavior is explicitly not adopted. S2's integer follow-list flag belongs to `/pgc/app/follow/v2/{type}` and supplies no index guarantee. Other boolean fields sharing the name has_next supply no generic typing rule. S3 supplies no independent exact index flag decoder/caller. This proposal rests on the exact S1 index decoder/caller plus the actual local counterexample, without fabricated cross-source or live-service corroboration.

**Scope.** `SeasonIndexVM.cs` accepted-result internal paging policy only, retaining FI1's owner and error boundaries. Same index owner's additive season1 tests. No grid/page/API/parser source, public state or generation architecture change.

## Concrete acceptance and execution dependency

1. Independent expert review must approve these frozen requirements/evidence before root selects either proposal. No current FI1 grant includes them.
2. Before same-owner execution, bind all verification to the **exact frozen FI1 candidate** and its complete maintained test inputs. The frozen old-A fixture remains unchanged. No moving SeasonIndexVM copy is acceptance evidence. A later repaired-green replay requires exact candidate hashes and independent review; this discovery does not supply it.
3. Preserve original118/89-positive, supplement34/22-positive and G4179 proofs byte-identically. Preserve all FI1 maintained fresh-navigation/ownership/readiness/reentrancy/page2 tests and old season1 assertions/lifetime extractor/scripts. Keep FD1's disjoint owner and source-binding requirements. Do not change any old assertion or assign lifecycle flags to manufacture a starting state.
4. The private later-PGC140 fixture must remain unchanged for candidate replay. With only accepted FI1, its2 labeled FI1 history failures should close; the11 FI2/FI3 failures remain until those separate proposals execute. After accepted FI1+FI2+FI3, all140 must pass, including all127 current positives. Add each selected proposal's meaningful assertions to the existing season1 surface; do not add a runner/batch/dependency.
5. Maintain **actual complete** VM/page/grid and real API/ApiHelper/HttpResults coverage, with delivery/platform primitives only. Fresh-navigation history must start at the constructor. The separate post-navigation public Refresh precondition reaches old A's already shipped successful-result state; it is never described as a constructor fix or substituted for initial FI1 acceptance.
6. Validate code-priority, legacy label/first fallbacks, exact keyword query round-trip, all current season types, real signer/host/cookie/page/pagesize/type/sort controls; integer0 first/append/manual and actual short-grid stop; integer1/absent legitimate grid autofill; all flag compatibility/failure/retry/refresh cases above. Retain successful notification order and append rows on failures.

Prospective product owner is the **existing same index owner**, `SeasonIndexVM.cs` only. Minimal maintained additive test surface remains `plan/tests/season1/{Season1Probe.csproj,Program.cs,Season1Stubs.cs,SeasonIndexProbe.cs}`; use the FI1 complete-source wiring and extend its new probe. Original141+497 assertions and existing lifetime source/scripts remain unchanged. A new implementation amendment must name the exact frozen FI1 starting candidate and granted paths; no other source/test/main-document/Git grant follows from this discovery.

Non-goals: FI1 redesign, FD1 changes, general parser/envelope/scalar migration, new order/default/filter normalization or fallback, strict flag rejection, generic has_next/has_more policy, auth/signing/cookie/host/page changes, foreign implementation transfer, live/server/capture/retirement assertions, shared/full-suite/native/device/hosted acceptance. Both proposals and private writers are frozen/released for independent review; no production repair occurred.

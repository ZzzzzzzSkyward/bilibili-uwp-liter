# Follow-up evidence — frozen actual-source discovery

Owner `api_followup_requirements`; 2026-11-25 UTC. Repository base HEAD observed read-only: `7356b94893377558191b0d70576a52bae704c1f9`; current authorized worktree edits were not staged, committed, fetched, checked out, built in shared projects or modified. Only this file and `requirements.md` were written inside the repository. Private scratch is exclusively `/workspace/work/api-shape-comparison/followup-discovery/` (below, **W**). All fixtures are synthetic; there are no live/raw/account captures.

## Inputs and exact source binding

Read AGENTS.md, cloud CONTRACT/REPO, memory INDEX/current API/timeboxed memory, fixed deadline, current API checkpoint, active followup plan and released sibling inventory/E5/E6. Fixed checkpoint15:41:29 and hard stop16:51:29 Asia/Shanghai were retained. Source remained stable during the private runs: W/binding.json records exact SHA256 for25 current product/caller inputs, all fixture/tool inputs, sibling custody and summaries; `source_stable=true` at freeze.

| Actual input | SHA256 |
| --- | --- |
| `Api/User/WebDynamicFeedApi.cs` | `775ac5004bde97d6a5d02e16d52792af1272ee4d4901f0402a59e297f9ea2efb` |
| `Modules/Season/SeasonIndexVM.cs` | `e7eef5dd3c69852f9b17c676122c4c1bec19eae729e49fb2e4c2ad02b19e14c3` |
| `Api/SeasonIndexAPI.cs` | `2f52e361f3f7635f33cdff670be5530f1e4465ea08c2311b4b31e1abe6987f0b` |
| `Api/ApiHelper.cs` | `dfb07f377db95023aebce4c68101c6d334444722cf79b07d7d39a6126ba302d9` |
| `Helpers/HttpHelper.cs` | `b12e457a75a133e9298fb77ad982b33986c2f95da2562b4f031b1fd692424d13` |
| `Parsing/Dynamic/WebDynamicFeedParser.cs` | `af784f82b10658104bf44eb4504f5f7425aaf621ea9bf82200d02e41163e2ae7` |

Paths in that table are under `src/BiliLite.UWP/`. W/repro/source-manifest.json binds the complete12 compiled product files. HttpResults is extracted **byte-for-byte from its unique class start through the final namespace brace** in the unchanged HttpHelper.cs, with namespace/usings prefix only, matching the existing c1/c2/comments probe approach. It retains the complete real class, GetJObject/typed reads and longest-valid-JSON salvage; no miniature parser was substituted. ApiHelper, SeasonIndexAPI, SeasonIndexVM, WebDynamicFeedApi, DynamicAPI, ApiEnvelope and WebDynamicFeedParser are full unmodified source copies, not method slices. Stubs replace only platform/delivery primitives, settings, logger and inert cookie/WBI services; FI1 executes actual app GetSign but no WBI/account operation. The delivery hook captures ApiModel and serves in-memory JSON; no HTTP request is issued.

The real transport path was inspected rather than executed: ApiHelper.cs236–278 ApiModel.url concatenation → Utils.cs35–49 GET/need_cookie dispatch → HttpHelper.cs167–242 cookie preparation delegates the **same URL** → HttpHelper.cs116 `GetAsync(new Uri(url))`. No component re-encodes offset as a named value. The fixture checks that final Uri query using runtime8.0.33 `HttpUtility.ParseQueryString`; this establishes conventional URI/query boundary corruption, not a captured service-side decoding result.

## Pinned corroboration, including actual callers

The three released caches remain detached, clean and exactly pinned at freeze (ordinary rev-parse/status reads only):

| ID | Full immutable commit | Field-specific evidence used |
| --- | --- | --- |
| S1 PiliPlus | `217fb54f51f6764176319bbdedab2a7522d75401` | `lib/http/dynamics.dart:35–70` accepts String offset and sends a named query value; `models/dynamics/result.dart:15–20,50–95` keeps offset string/has_more boolean; `pages/dynamics_tab/controller.dart:32–54` refreshes/reset and reuses accepted offset. `http/pgc.dart:15–56` requests index condition/result, page/pagesize and decodes data; `models_new/pgc/pgc_index_condition/{data,sort,value}.dart` separate field/name/keyword; `models_new/pgc/pgc_index_result/data.dart` list/has_next; actual `pages/pgc_index/controller.dart:29–70` awaits conditions, builds named filter values and then queries results. |
| S2 bilimiao2 | `d60a9477e7b3a3d1bfe7570e43ab9370803a8a6b` | `.../apis/BangumiAPI.kt:21–35` app V2 season versus web section; existing E6 traces real `ResponseData<SeasonV2Info>` versus `ResponseResult<SeasonSectionInfo>` screen consumers. Family distinction only; no exact index/string-offset corroboration invented. |
| S3 nooblong/bilibili-api | `4789508a93f3a39b7d3c896273309db892a9cc8b` | `bilibili_api/bangumi.py:941–998`, `data/api/bangumi.json:13–24`: exact index result route and named page/pagesize/order/sort/season_type. Its type1 and generic payload fallback are not copied. `dynamic.py:1218–1256` same feed route, page/timezone/features, but inherited Optional[int] offset annotation is weaker than S1's string request/DTO/caller and does not justify narrowing. |

Full per-file hashes are in W/binding.json; cache licenses/activity qualifications and source-only custody stay in `../siblings/inventory.md`. S1 HTTP named query map reaches Dio in `http/init.dart`; map encoding is field-specific corroboration of the value boundary. S1's recursively fetching filtered empty pages is not adopted. S3's narrow maintenance qualification is retained. No foreign source/script/build/test/dependency was executed or installed and no embedded public signing constants were copied into the repository Markdown artifacts.

## FI1 — actual full-VM counterexample

Current shipped chain: AnimeIndexPage.cs24–47 → SeasonIndexVM.LoadConditions84–153 → SeasonIndexAPI.Condition → real app signer → in-memory delivery → real HttpResults.GetJObject → condition JsonConvert; then SeasonIndexVM.LoadResult156–226 → Result builder/signer → legacy data.list read. There is no typed index parser/mapper between these VM reads; typed web-season result parsers belong to different routes.

Fixture conditions contain one area filter (`keyword=-1`) and two orders (`field=3,name=追番人数`; `field=4,name=最高评分`) with code0/data. Result contains season101, title synthetic season, order42/order_type follow, list length1 and has_next1. This deliberately puts default order3 first to keep the unrelated name-versus-keyword issue out of FI1.

1. Fresh actual VM, Parameter=new SeasonIndexParameter; await LoadConditions then LoadResult exactly as navigation does. Conditions decode and ConditionsLoading clears, but result dispatch count is0, Result=null, Page1. Desired: one result request, one row, Page2. **2 failures**.
2. Full actual VM with Loading=false is a positive control: same fixture sends result page1/pagesize24/season_type1/type0/order3/sort0, publishes row and Page2; real GetSign verifies. Successful append adds season102 and Page3. Rejected, malformed-list and transport append retain rows/Page3 and release Loading; empty append stops requests without increment. These controls all pass.
3. Idle actual VM starts one result call held by TaskCompletionSource. Loading is true (positive). A second awaited LoadResult returns without sending, but clears Loading false. A third call consequently sends a second result request. Completing the shared held response causes both actual continuations to publish: two rows/Page3. Desired: owner stays loading, only one dispatch, final one row/Page2/Loading=false. **3 failures**. No source mutation or async-void timing guess is used.

## FD1 — actual parser→builder→URI counterexamples

Synthetic code0/data envelopes with items[], has_more=true and each offset below pass the real strict parser and preserve its exact string. For all/video/article, call the real WebDynamicFeedApi.CreateFeedRequest(3,parsed.NextCursor,type), construct the real ApiModel.url Uri and decode its query once.

| Accepted offset input | Actual next request after one query decode | Consequence |
| --- | --- | --- |
| `C++` | offset `C  ` | Plus becomes spaces. |
| `literal%2Bvalue` | offset `literal+value` | Literal percent escape interpreted early. |
| `cursor&features=changed` | offset `cursor`; two features values (original, changed) | Query field split/control corruption. |
| `cursor#fragment` | offset `cursor`; URI fragment `#fragment` | Token suffix leaves query. |
| `中文 + % &page=999` | offset `中文   % `; page values3 and999 | Token truncated/plus altered; duplicate page. |
| `token&offset=other` | two offset values (`token`, `other`) | Opaque value becomes multiple fields. |

Each exact-token/one-offset assertion fails for each type: **18 failures**. The features/page single-value checks additionally fail in the two injection cases per type: **6 failures**. Parser success/exact string, cookie GET host/type/timezone/no-sign controls still pass. Plain and numeric-text9007199254740993 round-trip. Null omits offset; empty includes one empty value. Unicode/quotes/backslash/CRLF remain active ordinary controls (CRLF already round-trips through this runtime's Uri and must not be falsely reported failing). The legacy DyanmicNew actual builder correctly percent-encodes `C++%2B&x=1`, preserves one exact offset and signed type_list8; no legacy repair is proposed. Wrong code/key/items/has_more are typed failures, and accepted large numeric scalar offset remains exact text.

The active caller trace is Home DynamicVM.cs180–245 and User DynamicVM.cs933–1017, article path620–665 and earlier-page replay1630–1649. They pass only their accepted polymer/article cursor. User resets gRPC/rest state after successful polymer publication; parsing/mapping failures return before that publication. SpaceHistoryV2 is declared but has no shipped caller found by rg, so it is excluded from FD1 despite superficially similar raw text.

## Commands, counts and private artifact hashes

Existing toolchain reused without installation: `/workspace/work/maintenance-foundation/quality/dotnet`, SDK8.0.427/runtime8.0.33; cached Newtonsoft13.0.3. W/dotnet-info.txt and W/binding.json contain exact executable/compiler/runtime URI/HttpUtility/JSON DLL hashes. No shared obj/bin was written.

```bash
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project /workspace/work/api-shape-comparison/followup-discovery/repro/Followup.csproj -c Debug
# expected nonzero1: CHECKS=118 FAILURES=29
# PGC-initial-red2, PGC-owner-red3, dynamic-roundtrip-red18, dynamic-controls-red6

/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project /workspace/work/api-shape-comparison/followup-discovery/controls/plan/tests/g4/G4Probe.csproj -c Debug
# exit0: PASS179 assertions
```

G4's unchanged Program/Stubs and full current Home DynamicVM/parser/mapper/rule closure are copied to exclusive W/controls; all current G4 source-text inputs (including User DynamicVM and GrpcService) are copied unchanged. Its project uses private copies of the already-present local BiliLite.gRPC/Google.Protobuf/Grpc DLLs instead of a shared gRPC ProjectReference build. W/controls/source-manifest.json binds every product/test/reference input. The original G4 fixture is unchanged; only the private project wiring removes project/package restore in favor of local references. It actively verifies actual Home VM polymer append, malformed leg failure, legacy fallback and failed-append cursor retry alongside typed parser/source separation controls. User VM contract checks in G4 are source-text checks, not a full User VM runtime or WinRT test.

| Private artifact under W | SHA256 |
| --- | --- |
| `repro/Program.cs` | `fd3ac1f728b26c07e4cd4a47c6882d936095558d85630ead75e254e2718c7e6f` |
| `repro/Stubs.cs` | `6b67ae0af9500c16d979620aa41acb9773dd55bc371fbca4c1a22aa2f70bc201` |
| `repro/Followup.csproj` | `81d345c8d661694d49c2bd6d8e1c469aefd34a0e8d14ef6cd3e8d4e86e4bc02a` |
| `repro/HttpResults.actual.cs` | `8aa4c8734aed85517df1dc3863a479fc076a262e775a7c657e45e9f8b9bc6a93` |
| `repro/run.log` | `764e745eee7c663aa95d6623325ec1c6208682db441e3d4c07b79600cbbd90c7` |
| `repro/bin/Debug/net8.0/receipt.json` | `c7279ac8188488a57ded0352ab73a3afc01ec6943f88dd45337cbf5dfaa23854` |
| `controls/source-manifest.json` | `b694304bc2e3254b6e11cb890fe36949aaaf34cf013244e0c6878a42bca83d1e` |
| `controls/run.log` | `47f416f0a7ae8138b1456de37abe33f37e0324e5c8e8ae183a4d5c99fbc5d7fe` |

Final W/binding.json SHA256 is `9096bc1ed204467a13b2e73ee5e03bf377ded53edac101557b8cfab2ec8eb7fe`. W/returncodes.json records an explicit final unchanged `--no-build` replay with exits1/0 after the private compile runs above. Do not infer repaired green: no product fix was made. Expected red is evidence, not an accepted test-suite result. The89 other private assertions and G4's179 controls are actual current-source green. There is no native/client/server/hosted acceptance claim and no whole-tree suite run.

## Release

Requirements/evidence and all scratch are frozen and released for root read/selection/review/archive. Source caches retain their released clean pins. No other repository output or Git index/ref operation belongs to this owner. Future execution waits the current combined source/docs/publication/review release and a fresh explicit owner grant. The checkpoint/hard deadline was not restarted or shifted.

## FI1-REV1 author amendment — separate supplemental proof

A fresh author-only grant in `revision-dispatch.md` permits only requirements/evidence plus `fi1-amendment.md` and exclusive revision scratch. The independent CHANGES finding and its unsafe proof are retained unchanged as history. This amendment changes the FI1 specification; it does not change current source or claim a repaired outcome. FD1's accepted section is byte-identical (verified by its separately recorded payload hash), and **every original W/binding.json artifact still matches**, including original118 Program/Stubs/log/receipt and G4 inputs/log. Original source/tool hashes still match too. The original release.json remains the historical first release; revised custody is separately recorded under W/revision/.

New private fixture **S** = `W/revision/supplement/`. It copies all complete original repro sources unchanged, then adds the **complete unchanged AnimeIndexPage.xaml.cs and MyAdaptiveGridView.cs**. The same real SeasonIndexVM, IModules, request builders/app signer and complete extracted HttpResults execute. `Platform.cs` provides generic managed navigation/selection/base-page/InitializeComponent, dependency-property storage/change callbacks, a template ScrollViewer and AdaptiveGridView shell. It does not copy conditions/result ownership, request, decode, page, selection-handler or grid-autofill policy. The actual full grid's OnApplyTemplate/Loading callback and the actual complete page navigation/selection methods execute. IModules.PropertyChanged is the actual synchronous invocation. Binding a VM Loading notification to grid.Loading models that declared x:Bind delivery only; real native XAML rendering/timing is still unmeasured.

To isolate the later scheduling hazards from the already proven initial constructor bug **without assigning any lifecycle flag or private field**, the supplement calls the actual public LoadResult once before metadata exists. On the unchanged old source its existing guarded finally makes the real VM idle; on a corrected source this must be a harmless not-ready entry. This is an explicit old-source scheduling setup, not the fresh-navigation acceptance and not a copied ownership policy. The frozen original118 fixture still owns the untouched fresh-navigation proof; maintained fresh-navigation tests must start directly from the constructor with no such setup.

```bash
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project /workspace/work/api-shape-comparison/followup-discovery/revision/supplement/Followup.csproj -c Debug
# exit1: CHECKS=34 FAILURES=12
# ready-red3, conditions-red5, usable-red2, result-red2;22 positive controls PASS
```

The same existing hash-bound SDK8.0.427/runtime8.0.33/compiler/URI/HttpUtility/Newtonsoft references were reused, with no installation, source repair or shared build. An explicit final `--no-build` replay has exit1 in S/returncode.json. The complete receipt records every assertion and actual observations. These are desired-safe old-source failures; **34/34 repaired green has not occurred**.

| Supplemental branch | Actual old-source observation | Required disposition / active positive |
| --- | --- | --- |
| Null Parameter and direct load/Refresh entries | No request, but Page4 becomes1; one result acquisition and3 generic error notices. With Parameter present but absent Conditions, result ownership/error also occurs. | Guard before acquisition, page reset and errors;3 desired-safe failures. No request and absent-Conditions page retention are existing positives. |
| Actual navigation holds conditions; then direct LoadConditions, Refresh, LoadMore and LoadResult | **3 conditions requests**, no result yet, one premature result acquisition/error. After response drain, **3 Conditions publications and3 result requests** occur. | One metadata owner, no premature owner/error/republication, then one result;5 desired-safe failures. Held initial dispatch and absence of premature result/page movement are existing positives. |
| Business/transport/malformed condition failure, then actual Refresh retry | Each failure settles without usable Conditions/result; current notice counts are1/0/1. Refresh retries conditions once, actual synchronous Conditions selection stays guarded, exactly one result is held and then one row/Page2 publishes. | **12 active positive assertions** preserve existing error/decode/retry behavior. |
| Decoded area values omit requested -1 | Real metadata publishes with current=null; no result request, but result acquisition adds a generic error. Refresh sees a nonnull collection and does not retry conditions (0 conditions/0 results), acquiring result again. | Reject unusable selected metadata before result ownership/error; Refresh retries once.2 desired-safe failures; preserving decoded unmatched selection/no dispatched result is positive. No new selection fallback is proposed. |
| Result=null PropertyChanged synchronously invokes real RefreshCommand once | Owner is **false** at notification;2 result requests publish2 rows/Page3. | Acquire owner before observable result reset;2 desired-safe failures. Actual synchronous delivery is a positive. This synthetic adversarial observer tests the public reentry boundary; it is not claimed to be an automatic native Result binding action. |
| Full actual grid attached to VM Loading; first result completes while ScrollableHeight0 | Actual grid callback requests pages **[1,2]**, one row remains published, Page2 is held with Loading=true. After viewport fills and page2 completes:2 total requests,2 ordered rows/Page3. | **4 active positive assertions** preserve legitimate page2 autofill. This is not a duplicate page1 finding and must not be suppressed. |

The table's counts sum to34 assertions:22 positive and12 desired-safe failures. Requirements now carry the explicit conditions/result ownership table, nonnull Parameter/structurally usable selected metadata guard, notification order, quiet pending rejection, existing condition failure notices and legitimate bounded Refresh retry. Exact real request/casts/data/sort/filter defaults are preserved; there is no new parser, API/page/control product owner, generation, cancellation or fallback policy.

| Revision source/artifact | SHA256 |
| --- | --- |
| Full `Controls/MyAdaptiveGridView.cs` | `7e5d833bcc8223ad128979ea731b8f055b53bdfb41a4297655c00f92460622c8` |
| Full `Pages/Bangumi/AnimeIndexPage.xaml.cs` | `89e2d7b0e9aa4d827db38cb54ca5945473ce3781545a70932f5c1dceb579d8cc` |
| S/Program.cs | `eec24a88aa1c831aa6dfc492a601c35ed1075d18c25df01e7335cc5c906eac9f` |
| S/Platform.cs | `f46e98b366acadd242fde08e60024228a09ffa4e54792647de0bf3de57aa01b0` |
| S/run.log | `417aa8525fb8167341960cc0df9e7ce68e7b37e7c8adf4105e46bc4888c5504f` |
| S/bin/Debug/net8.0/receipt.json | `9c764dd52dcca3cf9e73ec1f828c533f0767b337cec5e2d31d99afc89bf57a73` |
| W/revision/binding.json | `d0dfac6cb670d84bef99ffe8a352b3384b338b4555caca90c395adda558895cc` |

W/revision/binding.json includes all15 complete source/input hashes, supplemental project/stubs/returncode, every original artifact unchanged check, unchanged FD1 payload, unchanged tools and15-source stability. `unsafe-review-history/` preserves the independent Program/Platform/project/log byte-identically, including reviewer Program SHA256 `4fa2c4f826c7e333813354b2fcd1a07e6369b03042d5d5a0d16c3e1dc51b4926` and its7-check/1-failure log. That independent unsafe history was not rewritten into a new expected-green test.

Revised requirements/evidence/fi1-amendment and all supplemental writers are frozen/released for the same reviewer. Future execution still waits current combined source/docs/archive release, fresh safe dev binding and exact root grants. There is no main source/test/project/docs/index/ref change, dependency/foreign execution, live/account/native run or expanded acceptance claim. Fixed checkpoint07:41:29UTC and hard stop08:51:29UTC remain unchanged.

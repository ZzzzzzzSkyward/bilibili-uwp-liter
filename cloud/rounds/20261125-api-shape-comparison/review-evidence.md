# Independent requirements-review evidence

Reviewer-owned evidence, 2026-11-25. This document supports [review.md](review.md). All substantive findings are in these two owned files. No foreign code was executed, no Bilibili API call was made, and no main-repository Git mutation or source/test/tooling/main-document change occurred.

## Inputs and byte binding

Read AGENTS.md, cloud/CONTRACT.md, cloud/REPO.md, memory INDEX plus the API maintenance/comparison run memories, round plan/checkpoint/discovery/requirements-review dispatch, root scope-decision, and all six released `siblings/{inventory,evidence,requirements}.md` and `contracts/{matrix,evidence,requirements}.md` artifacts. Current main HEAD remained `7356b94893377558191b0d70576a52bae704c1f9`; authorized memory-round dirt was preserved and was not treated as an API source mismatch.

`source-manifest.json` independently verified **14/14** hashes: twelve current API/DTO/parser/transport sources and both released fixture Programs. All twelve `contracts/baseline-source/` snapshots match their recorded source hashes. Private receipts:

- `/workspace/work/api-shape-comparison/requirements-review/source-hash-check.json`
- `/workspace/work/api-shape-comparison/requirements-review/snapshot-check.json`
- `/workspace/work/api-shape-comparison/requirements-review/linked-source-manifest.json` — all linked production/stub inputs in both replay projects, beyond the twelve-source author manifest.

The favorite fixture's `HttpResults.actual.cs` contains the complete exact current production class from `Helpers/HttpHelper.cs:499` through its closing brace, including salvage and legacy methods; only namespace/usings wrappers differ. Verified by extracting that exact final class region and comparing bytes after trailing-wrapper whitespace removal. It is not an inert safe decoder replacement.

## Independent sibling source/activity/license checks

Executed own public read-only GitHub repository metadata GETs using Python standard library and inspected existing caches with `git rev-parse HEAD`, `git symbolic-ref -q HEAD`, `git status --porcelain`, `git show --format=fuller --stat`, and relevant source/diff reads. No fetch/checkout/cache mutation occurred. Each pin matches, symbolic-ref exits1 (detached), status is empty. All report `archived=false`, `disabled=false` and SPDX GPL-3.0. Private JSON receipt: `independent-pin-metadata.json`; activity/license receipt: `activity-license-receipt.json`, both under reviewer scratch.

| Source | Independently verified exact pin | Inspected substantive recent activity | Qualification |
| --- | --- | --- | --- |
| S1 bggRGjQaUbCoE/PiliPlus | `217fb54f51f6764176319bbdedab2a7522d75401`, 2026-11-14 | `c741279080fa90571d59ff9558640beb88861492`, 2026-10-31; actual `fav.dart`, favorite/sub DTO, controller/view page-order diff | Active Flutter implementation; named values and decoder/consumer observations are source usage, not live acceptance. |
| S2 10miaomiao/bilimiao2 | `d60a9477e7b3a3d1bfe7570e43ab9370803a8a6b`, 2026-11-20 | `8ea47ff800d7c5927a180f129e236f43737dde8f`, 2026-09-09; actual gRPC module/API extraction, BiliGRPCHttp/PlayListStore adapter changes | Active independent Kotlin client; recent implementation maintenance does not date each inherited REST endpoint. |
| S3 nooblong/bilibili-api | `4789508a93f3a39b7d3c896273309db892a9cc8b`, 2026-09-28 | `425f5200f5fa17a316b93cdf21643513d9a3075f`, 2026-08-28; inspected the two comparator guard insertions (four added lines) for missing stream quality | Narrow 89-day maintenance qualification. GitHub parent Nemo2011/bilibili-api verified; one lineage, not an extra independent Nemo source. Whole API catalog freshness is not established. |

Read each actual LICENSE header/text. License SHA-256: S1 `3972dc9744f6499f0f9b2dbf76696f2ae7ad8af9b23dde66d6af86c9dfb36986`; S2 `8b1ba204bb69a0ade2bfcf65ef294a920f6bb361b317dba43c7ef29d96332b9b`; S3 `81cbae84a29ce7e770bf2bc7b178e50bda0ce8de6067aba661b0bc7b05b562f8`. No foreign implementation was transplanted. Old ywmoyue same-lineage research was not counted again.

Pinned field-level source checks:

- S1 `lib/http/member.dart:349–388,690–719` passes named keyword/name with mid/vmid/pn/ps/order into WBI and GET; `lib/utils/wbi_sign.dart:62–74` sorts, filters five characters, encodes components and hashes; `lib/http/init.dart:200–217,256–267` binds api host and Dio query map. Actual signed values and transmitted map can differ in S1, so its entire sanitizer/transport algorithm is not copied.
- S3 `bilibili_api/user.py:429–468`, `data/api/user.json:132–146`, `utils/network.py:1932–1942,2228–2265` independently corroborate keyword as a named signed query value. Its wbi2/webid/ticket extras are not a local repair requirement. S2 `UserRelationApi.kt:47–59`, `BiliApiService.kt:6–16`, `ApiHelper.kt:112–127` corroborate the following name boundary with different app-signing transport, not identical auth.
- S3 `comment.py:469–506`, `data/api/common.json:124–137` corroborate the exact `/x/v2/reply/wbi/main` route, opaque next_offset provenance, JSON `pagination_str` query value, hot/new mode mapping and WBI declaration. S1 `reply.dart:21–55` is a different non-WBI/numbered family. Both foreign manual quote-only JSON constructions are unsuitable algorithms to copy; ours already uses a JSON serializer.
- S1 `fav.dart:53–81`, `models_new/fav/fav_detail/data.dart:4–25`, `pages/fav_search/controller.dart:40–56`, `pages/fav_detail/controller.dart:125–167` use exact normal route/keyword and nullable bool `has_more`. Its detail count guard is explicitly additional. S2 `UserApi.kt:206–219`, `entity/media/MediaDetailInfo.kt:6–10`, `UserFavouriteDetailContent.kt:107–156` decodes bool, maps null medias to empty, sets finished on `!has_more || empty`, and only accepts page after success. It defaults absent bool true, unlike our correct nullable parser. These prove source-specific signal use, not universal permissive coercion or live response captures.

No endpoints were declared retired, no auth/session/protocol rule was inferred from route parity, and no unsupported live/server guarantee was added. E5–E8 remain bounded comparison results, outside AS1–AS3 execution.

## Actual production callers and compatibility/error boundaries

| Repair | Independently traced production path | Boundary preserved |
| --- | --- | --- |
| AS1 space | `Pages/UserInfoPage.xaml:172`, code-behind191–194 → `UserSubmitVideoVM.cs:90–129` → `UserDetailAPI.cs:75–96` → real signer | Direct REST plus REST after gRPC exception. Keep current gRPC selection/reset and fallback. Text raw at line81 is the defect, not route/cookies. |
| AS1 following | XAML376/code-behind368–371 → `UserFollowVM.cs:113–124` → `UserDetailAPI.cs:136–156` | `tid==-1 && keyword!=""` selects search; null currently selects search and interpolates an empty name, whereas empty does not. tid0/positive selects tag; order_type depends on enum. Preserve these exact branches. |
| AS2 comment | `Controls/CommentControl.xaml.cs:148–208,223–260` → `CommentApi.cs:46–66` → real signer; accepted parser/mapper offset back to next request | Null/empty offset JSON currently comes from proper serialization. Signer parses raw query before sanitizing and percent-encoding; AS2 fixes that boundary only. Transport/negative business fallback and accepted cursor ownership stay unchanged. `WebCommentPageParser.cs:84–91` existing whitespace-only offset→null policy and mapper source distinction remain unchanged; do not add trimming to opaque nonblank strings. |
| AS3 favorite | `Pages/User/FavoriteDetailPage.xaml.cs:33–47,62–65` → `FavoriteDetailVM.cs:118–183` → `FavoriteAPI.cs:168–188` → strict parser/mapper | Normal keyword is already escaped once; Type21 is a distinct signed app season source. Bool absence stays nullable, nonbool present remains Payload. First-page missing info rejects publication. Existing append rows/info/Page survive failed parsing and Loading clears. No broad decoder, scalar, envelope or transport rewrite. |

`WbiKeyService.cs:191–208` is the complete parser/sorter/sanitizer/encoder/digest path. `ApiHelper` seeds only dummy valid keys in the harness; real signing executes. `Utils.cs:35–47` dispatches GET to current cookie/noncookie transport, POST to form transport; `HttpHelper.cs:394–445` is form body transport. `ApiEnvelope.cs:25–102` preserves strict bounded integer code and endpoint-declared object payload. No field/value fix requires changing these owners.

Original regression wiring was inspected: api02 Program28–35 runs original12 plus SearchRequestChecks (includes Unicode/reserved input, five-character sanitizer, actual digest and saved-BUVID/app-order controls); c2 already links full FavoriteDetailVM/API/parser/mapper plus actual HttpResults extraction and favorite action checks; c3 asserts actual parser/mapper/failure wiring and distinct media-list queue behavior. Existing comments/action/boundary regressions remain required. Fixed inventory51logical/52executable is explicit. Only isolated reproductions ran here, not the suite.

## Independent unchanged-fixture replays

Copied only our own released scratch fixture files/projects into exclusive reviewer scratch, retaining every Program byte and the original absolute Compile links. Copied already-cached Newtonsoft13.0.3 to a reviewer-local NuGet directory; used SDK8.0.427/runtime8.0.33 with an empty offline source and private CLI home. No author scratch output or repo probe output was written. Commands for each project from reviewer scratch:

```bash
export DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet
export DOTNET_CLI_HOME=/workspace/work/api-shape-comparison/requirements-review/cli-home
export NUGET_PACKAGES=/workspace/work/api-shape-comparison/requirements-review/nuget
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
"$DOTNET_ROOT/dotnet" restore query-repro/QueryRepro.csproj --source /workspace/work/api-shape-comparison/requirements-review/empty-source
"$DOTNET_ROOT/dotnet" run --project query-repro/QueryRepro.csproj --no-restore
"$DOTNET_ROOT/dotnet" restore favorite-repro/FavoriteRepro.csproj --source /workspace/work/api-shape-comparison/requirements-review/empty-source
"$DOTNET_ROOT/dotnet" run --project favorite-repro/FavoriteRepro.csproj --no-restore
```

| Fixture | SHA-256 of unchanged Program | Independent observed result |
| --- | --- | --- |
| query-repro | `c0e4afcf0346c18ff17860edd0649b02ec3dad180ed2c283fd19593cfcf19405` | Exit1, **15 failures/25 checks**: AS1 ten failures, AS2 five. C++→spaces; A&B truncates; literal%2B loses its literal escape; injected pn/order changes controls; comment ampersand breaks JSON/ps. Independent known digest vector passes. |
| favorite-repro | `1a76dffdafe67a074209293e8413ed004d545636ae5f4cd5326fd21eecf72169` | Exit1, **6 failures/9 checks**: false/count42 first page incorrectly offers more/Page2, true/count1 incorrectly stops/Page1, false/count42 append incorrectly offers more/Page3. All three real caller/request/completion controls pass. |

Private logs: `query-repro/independent-red.log`, `favorite-repro/independent-red.log`. These are genuine desired-safe failures through unchanged complete actual methods/builders/VM, not copied algorithms. Favorite ApiHelper seam is inert only for signing not used by the selected normal GET; the actual builder and full caller parse/map/publication execute, and the seam never repairs paging. Query fixture executes the full real signer and primitive MD5, not a stub signature. No green implementation is claimed. Executors still need the final extended fixture unchanged against original complete source and repaired complete source, including all specified positive/negative controls.

## Independent boundary controls and AS3 contradiction

Additional reviewer-only `boundary-repro/Boundary.csproj` reuses the complete favorite production Compile links and unchanged decoder with a separate reviewer Program. SHA-256 `3a70a62f5576fb69dcd8a70db238cbb7b50261f0e28b1bf6c418c8d040786e0c`. Same offline tool command shape: restore this project with the empty source, then `dotnet run --project boundary-repro/Boundary.csproj --no-restore`. **Exit0, 18 current-behavior controls PASS**; log `boundary-repro/result.log`.

| Original-source append Page2 state | Actual load-more / Page / rows after one request |
| --- | --- |
| Normal, absent flag, one new row, prior1/count42 | true /3 /2 |
| Normal, null flag, one new row, prior1/count2 | false /2 /2 |
| Normal, absent flag, empty page, prior1/count42 | **true /3 /1** |
| Normal, null flag, empty page, prior1/count42 | **true /3 /1** |
| Normal, true flag, empty page, prior1/count42 | true /3 /1 |
| Type21, true flag, empty page, prior1/count42 | true /3 /1 |
| Type21, false flag, one new row, prior1/count42 | true /3 /2 |

Each route-family control confirms one actual FavoriteApi request. Separate transport503, rejected code-1, code0 missing data and string `has_more:"true"` append controls retain the same existing row object, info object and Page2, hide Load more as current behavior does, clear Loading and publish one categorized failure toast. Nothing here proposes restoring Load more on failure or changing last-good semantics.

The two bold normal absent/null empty rows cannot both remain original-green compatibility controls and become terminal-empty behavior. AS3 must explicitly decide that precedence. Type21 remains the unchanged count family. This is the sole requirements blocker; it does not invalidate the corroborated three source defects or authorize additional scope.

## Final custody receipt

All main API input hashes still match at completion; both original fixture Programs remain exact. S1/S2/S3 cache refs/content were not mutated. Released author artifacts stayed untouched. Reviewer sole repository outputs: `review.md`, `review-evidence.md`; private outputs exclusively under `/workspace/work/api-shape-comparison/requirements-review/`.

All artifact/cache/snapshot/fixture read custody is **RELEASED** on completion. Source/test/tooling/index/ref/publication remain blocked pending AS3 requirements correction/re-review, memory safe integration and explicit root baseline/file grants. Native/live/session acceptance remains unverified.

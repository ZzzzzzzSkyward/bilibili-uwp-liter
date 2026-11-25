# Requirements — three bounded API shape repairs

Status: **READY FOR INDEPENDENT REQUIREMENTS REVIEW**, 2026-11-25 UTC. This proposal is read-only discovery, not an execution grant. Read [evidence.md](evidence.md), [matrix.md](matrix.md), the released [sibling pins](../siblings/inventory.md), plan.md and discovery-dispatch.md. Main product/test/Git mutation remains frozen until memory-round closure, safe serialized integration and independent requirements PASS. No source/test repair has been made by this owner.

## Background, goal and non-goals

Three maintained independent source lineages corroborate named query values and favorite paging semantics. Our real current-source offline counterexamples show that existing personal-space search fields and comment cursors are parsed as query syntax before WBI signing, while favorite detail ignores the nullable boolean has_more it already decodes. Repair those existing flows while retaining current hosts/routes/client family/signing/cookies, strict envelopes, optional payload policy, accepted regressions and account/live gates. Foreign usage is evidence, not guaranteed server truth; no route-count parity target or catalog import is authorized.

The current API files used for red are byte-bound in `/workspace/work/api-shape-comparison/contracts/source-manifest.json` and preserved privately in `baseline-source/`. Discovery HEAD7356b948 is not permission to reset the shared repository: the memory round's unrelated edits stay intact. Before execution root binds the post-memory baseline and checks these API source hashes or reviews any relevant intervening changes.

## AS1 — personal-space search text remains one field (P1)

**Trigger and user outcome.** UserInfoPage's existing video/following search text can include `C++`, `A&B` or literal percent escapes. UserSubmitVideoVM.GetSubmitVideo uses SubmitVideosWbi for direct REST branches and gRPC search failure fallback; UserFollowVM.Get uses Followings for following searches. The current UserDetailAPI81/148 injects text raw, then the real WBI signer parses it: terms change and embedded `&pn=...` can change paging controls. Existing global SearchAPI's seven web paths already encode text and are not the defect.

**Required behavior.** In UserDetailAPI.SubmitVideos (including SubmitVideosWbi alias) and only the existing name-bearing search branch of Followings, escape raw free-text once before it enters the query parser. Preserve literal `+`, `&`, `=`, `%`/`%2B`, Unicode/emoji and whitespace in the decoded final signed field, subject to the existing WBI five-character sanitation. Null text retains the current interpolation-equivalent empty value; no new null exception. Existing route selection for null/empty keyword, tid=-1/0/positive, order/order_type, headers, cookies, mid/vmid/pn/ps/tid and aliases remain. No changes to ApiHelper/WbiKeyService policy or gRPC search selection.

**Acceptance.** Link real UserDetailAPI, complete real ApiHelper/WbiKeyService and unchanged primitive seams in the existing `api02` batch. Require one keyword/name value, no injected controls, unchanged configured host and correct route/auth/referer/alias behavior. Recompute actual final WBI signature at the generated wts using dummy keys; retain the independent known digest vector. Cover plain/empty/null/plus/ampersand/literal-percent/non-ASCII/injection-like text, all enum orders, direct REST alias, search/nonsearch/tag branches. Existing API02 original12 + search1258 assertions and global app-order/BUVID behavior remain intact. Current scratch fixture contributes10 genuine failures; final added fixture must run unchanged against original real sources and repaired sources.

## AS2 — comment pagination JSON remains one signed value (P1)

**Trigger and user outcome.** CommentControl passes parser/mapper next_offset into CommentApi.CommentV2. The builder correctly JSON-serializes `{offset: value}`, then interpolates that JSON raw into the query. The real signer decodes plus/percent and splits ampersands, corrupting the JSON or its offset and possibly ps. S3 corroborates this exact WBI route; S1/S2 alternative comment routes are not replacements.

**Required behavior.** Encode the already serialized pagination JSON once as the pagination_str query value before WBI signing. Keep its JSON serialization, null/empty offset treatment, mode hot3/new2, oid/type/ps, login cookie/optional csrf behavior, asynchronous signing and caller source fallback/cursor ownership. Preserve accepted WBI `!'()*` filtering; this proposal does not define a new arbitrary-cursor character policy or modify the global signer. Existing U1 Add/Reply message ownership and encoded csrf write forms remain untouched.

**Acceptance.** Real CommentApi + ApiHelper/WbiKeyService run in the same API02-owned request harness as AS1 (single owner prevents test overlap). Decode final signed query, parse pagination_str JSON and assert offset exactly equals `C++`, `literal%2Bvalue`, `cursor&ps=999`, and JSON-text-with-quotes/plus/percent/ampersand fixtures; assert exactly one pagination_str/ps/mode/oid/type, preserved chosen ps, and no injected field. Include null/empty/normal/double-quote/backslash/Unicode offsets without reinterpreting them as query syntax, hot/new and login true/false, and actual final WBI signature checks. The unchanged current scratch fixture has5 genuine failures. Run existing `comments` regression too; do not change cursor parser/model or fallback to avoid the red.

## AS3 — favorite detail consumes its endpoint's has_more (P1)

**Trigger and user outcome.** FavoriteApi.FavoriteInfo calls GET `/x/v3/fav/resource/list` with encoded keyword/pn/ps20. FavoriteResourceListParser already validates has_more as nullable bool, separately from info.media_count; mapper drops it and FavoriteDetailVM169 uses only `Videos.Count != FavoriteInfo.media_count`. Filtered terminal pages keep offering more; an explicit nonterminal page whose current rows equal stale folder metadata stops early. S1 and S2 independently decode/use that exact route's boolean flag. No captured shape establishes Type21 season-list compatibility, so its existing count-based behavior stays separate.

**Required behavior and precedence.** [AS3 amendment](as3-amendment.md) closes the independent review's sole P1: preserve every existing absent/null and Type21 fallback, including empty appends. The new empty-page termination rule applies only to the normal source with a present valid boolean. Keep the existing first-page empty short circuit before the common paging decision. No generic empty-page early return may bypass the preserved append fallback. Do not invent false for an absent/null flag, coerce a malformed flag, or change `!=` into another count rule.

Normal source means Type !=21 → GET `/x/v3/fav/resource/list` (cookie, keyword/pn/ps20). Type21 means GET `/x/space/fav/season/list` (existing signed app parameters). Let N be decoded new rows after mapper null-row filtering; let `C = Videos.Count != FavoriteInfo.media_count` after successful row publication/append using the accepted info. `ON` means ShowLoadMore=true and Page increments exactly once. `OFF` means ShowLoadMore=false and Page stays at the requested page. `COUNT` means ON when C, otherwise OFF, preserving the exact existing rule. `FIRST-EMPTY` means the existing first-page behavior: publish valid info and derived ownership/collect fields, set Nothing=true, leave the pre-call Videos reference/content unchanged, ShowLoadMore=false, Page1 unchanged, and finish Loading=false without evaluating COUNT.

| Actual source | has_more token | N | First page with valid info | Append with accepted existing info/rows |
| --- | --- | --- | --- | --- |
| Normal | true | >0 | Publish info/replace rows; ON | Append rows, retain info; ON |
| Normal | true | 0 | FIRST-EMPTY | Retain rows/info; OFF |
| Normal | false | >0 | Publish info/replace rows; OFF | Append rows, retain info; OFF |
| Normal | false | 0 | FIRST-EMPTY | Retain rows/info; OFF |
| Normal | absent | >0 | Publish info/replace rows; COUNT | Append rows, retain info; COUNT |
| Normal | absent | 0 | FIRST-EMPTY | Retain rows/info; COUNT |
| Normal | null | >0 | Publish info/replace rows; COUNT | Append rows, retain info; COUNT |
| Normal | null | 0 | FIRST-EMPTY | Retain rows/info; COUNT |
| Type21 | true | >0 | Publish info/replace rows; COUNT | Append rows, retain info; COUNT |
| Type21 | true | 0 | FIRST-EMPTY | Retain rows/info; COUNT |
| Type21 | false | >0 | Publish info/replace rows; COUNT | Append rows, retain info; COUNT |
| Type21 | false | 0 | FIRST-EMPTY | Retain rows/info; COUNT |
| Type21 | absent | >0 | Publish info/replace rows; COUNT | Append rows, retain info; COUNT |
| Type21 | absent | 0 | FIRST-EMPTY | Retain rows/info; COUNT |
| Type21 | null | >0 | Publish info/replace rows; COUNT | Append rows, retain info; COUNT |
| Type21 | null | 0 | FIRST-EMPTY | Retain rows/info; COUNT |

For both families, first-page missing info rejects publication: retain pre-call info/rows and Page1, ShowLoadMore=false/Nothing=false/Loading=false, with the existing failure notice. Successful append ignores incoming info and preserves accepted info; Nothing=false and Loading clears. Failed transport/business/envelope/payload appends retain pre-call rows/info/Page, keep the current ShowLoadMore=false/Nothing=false/Loading=false outcome and categorized notice; the new paging decision does not run. Retry requests the same Page before a later successful response chooses ON/OFF/COUNT. Preserve string IDs, null-row filtering, optional payload rules, action acknowledgements and current encoded keyword/pn requests.

The nullable value can be consumed from the typed parser result inside the VM; adding parallel paging fields or a new service is unnecessary. No request builder change, transport change or broad parser tolerance is required. Update adjacent comments that currently say the caller uses only media_count if they become stale.

**Acceptance.** Extend existing `c2` using full actual FavoriteDetailVM/API/parser/mapper/HttpResults, replacing only delivery/platform primitives as current action tests do. Preserve unchanged current three-case fixture (6 genuine failures): false/count42/one-row first page; true/count1/one-row first page; false/count42/one-row append Page2. Verify ShowLoadMore/Page, exact route/keyword/pn request, data/row order and retained info/IDs. Add the full outcome table as actual-source controls: separate normal absent-empty and null-empty Page2/count42/prior-one-row cases must remain original-green at ShowLoadMore=true/Page3/one same row/same info; normal present-true and present-false empty append at that state must become OFF/Page2; Type21 true/false/absent/null equivalents retain COUNT. Include COUNT equality/non-equality, nonempty fallback, all first-empty outcomes (including preexisting Videos identity), first-page missing-info rejection, malformed boolean (payload failure), transport/business/envelope failure, retry of the same Page and successful next-page progression. Frozen supplementary scratch controls establish124 unchanged-green assertions; a separate present-boolean empty fixture establishes2 desired-safe original failures/8 checks. These supplement the original6/9 AS3 red fixture, which stays byte-identical. Existing c2 folder/action and c3 queue assertions remain unchanged. This is favorite numbered pagination only; media-list oid, watchlater whole-list and dynamic cursors remain distinct.

## Proposed exact execution owners and verification

| Proposed owner (root to bind after PASS/release) | Repairs | Product surface | Existing probe surface |
| --- | --- | --- | --- |
| `api_query_executor`, gpt-6.1-sol/high | AS1 + AS2 | `src/BiliLite.UWP/Api/User/UserDetailAPI.cs`; `src/BiliLite.UWP/Api/CommentApi.cs` | `plan/tests/api02/{API02.csproj,Program.cs}` plus one dedicated added query-shape checks file in that batch; existing API01Stubs only if a primitive seam truly needs extension. No global signer/transport edits. Run api02 + comments + relevant auth1/API01 regression. |
| `api_favorite_executor`, gpt-6.1-sol/high | AS3 | `src/BiliLite.UWP/Modules/Detail/FavoriteDetailVM.cs`; comment-only consistency edits permitted in `Models/Api/MediaList/FavoriteResourceListDto.cs`, `Parsing/MediaList/FavoriteResourceList{Parser,Mapper}.cs` | `plan/tests/c2/` existing project/stubs/program plus focused new caller checks file. Run c2+c3. No other VM or shared project file ownership. |

Root grants exact paths after independent requirements PASS. A separate doc owner later reconciles current dispositions/provenance in main docs, preserving old research counts. Executors preserve all original assertions, capture same-fixture old-source nonzero → current-source zero with exact source/fixture hashes, and record meaningful categories rather than a green compile alone. Changes are independent product paths but shared Git staging/commit/push windows remain serialized. Stable final 51 logical/52 executable aggregate is required after actual repairs; no new batch/denominator or moving whole-tree suite during discovery.

## Risk and verification limits

Encoding belongs before the signer parses strings; escaping twice would change literal text, so decoded values plus actual digest checks are required. Null coalescing must retain current empty behavior and branch selection. Favorite boolean absence is not false; wrong type remains typed failure and Type21's unknown response contract remains unmodified. Count disagreement fixtures are synthetic local robustness evidence, not claimed live captures.

Independent review must verify actual callers, route/family differences, complete production methods, fixture hashes and every original regression. Linux source/behavior PASS does not establish native XAML focus/rendering, UWP compilation, Bilibili live behavior, cookies/account-session compatibility or released package startup. Preserve account P4/P5 hold, newer OAuth v2 refresh/session invariants and U2 BUVID/token/host/auth/protocol/session gates. No live write/auth request is part of this grant. Remaining native/read-only live checks may be recorded as blocked/unverified with concrete scenarios rather than claimed PASS.

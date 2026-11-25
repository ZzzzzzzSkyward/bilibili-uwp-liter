# Pinned sibling API usage evidence

Status: discovery evidence COMPLETE, 2026-11-25 UTC. Source IDs, exact commit pins, licenses, activity qualification and frozen cache roots are in [inventory.md](inventory.md). All paths below are relative to the corresponding **immutable** S1/S2/S3 cache; line numbers refer to that pin. No foreign code was executed. These are implementation observations, not captures of current service behavior.

## Evidence levels and limits

- **U — implementation usage**: real request and decoder/caller traced in a pinned working client, or an API implementation function plus actual transport. Strong evidence of what that client expects, not proof the server accepted it recently.
- **D — declaration**: DTO/schema/catalog entry without a currently traced consuming path. Useful to construct a hypothesis, weaker than U.
- **C — corroborated usage**: comparable exact contract fields independently used in two or more lineages. Corroboration is field-specific; shared route alone does not corroborate auth, payload types or response shape.
- **V — service-verified**: none in this discovery. No live Bilibili responses or Windows/client runs were requested or observed. Synthetic counterexamples proposed below must be labeled synthetic.

Recent repository maintenance does not date every endpoint. S3's inherited catalog is especially easy to overstate: only its narrow stream comparator fix is recent. S2 still contains some REST builders for features whose current Compose screens use gRPC.

## E1 — favorite resource list: named keyword, nullable collection, source-specific paging

**C/U, exact shared route and request fields.**

| Source | Request | Decoder and actual source caller |
| --- | --- | --- |
| S1 | `lib/http/fav.dart:53–81`, `Api.favResourceList` in `lib/http/api.dart:101`: GET `https://api.bilibili.com/x/v3/fav/resource/list`, query `media_id` integer, `pn`/`ps` integer, `keyword` string, `order` enum name (`mtime` etc.), `type` integer, `tid=0`, `platform=web`. Request goes through Dio query parameters, not raw string concatenation. | On `code==0`, decode `data` via `lib/models_new/fav/fav_detail/data.dart:11–25`: nullable `info`, nullable `medias` list, nullable **boolean** `has_more`; list reversal is a local `pageDesc` option, not a wire parameter. `lib/pages/fav_search/controller.dart:40–56` passes actual edit-box text and stops at `hasMore==false`; `lib/pages/fav_detail/controller.dart:124–167` also consumes the response. |
| S2 | `bilimiao-comm/.../apis/UserApi.kt:206–219`: same GET path, `media_id` string, `keyword` string, `pn`/`ps` decimal strings. `network/BiliApiService.kt:6–17` calls `ApiHelper.createParams` then UTF-8 URL encoding; this adds app-family parameters/signing even to the api host. | `entity/media/MediaDetailInfo.kt:5–10`: required `info`, nullable `medias`, boolean `has_more`. `bilimiao-compose/.../pages/user/content/UserFavouriteDetailContent.kt:107–155` parses `ResponseData<MediaDetailInfo>`, checks `code==0`, maps null `medias` to empty, ends on `!has_more || empty`, updates accepted page only on success. `bilimiao-comm/.../store/PlayListStore.kt:240–270` consumes the same endpoint for playlist assembly. |
| S3 | `bilibili_api/favorite_list.py:239–286` and `data/api/favorite-list.json:24–39`: same GET path, `media_id`, `pn`, fixed `ps=20`, `order`, `tid`, `type`, `platform=web`, `web_location`; adds **`keyword` by name** only if non-None. | API returns transport-decoded `data`/`result` dictionary rather than a typed list mapper. `FavoriteList.get_content` at `favorite_list.py:127–167` calls the function. `tests/test_favorite_list.py:22` is an unexecuted foreign example, not a validation result. |

What follows: `keyword` is a named query value, Unicode/reserved characters must survive one encoding boundary, null `medias` is an expected client-handled empty-list possibility, and `has_more` has boolean client consumers. What does **not** follow: every listed optional parameter is mandatory; S2's app signing belongs on our web variant; a different page size is rejected; arbitrary scalar/collection coercion is acceptable. Current favorite repairs must be checked before opening a defect.

S1 `media.dart:38–58` reads integer `id`, `type`, `duration`, alternate `bvid`/`bv_id`, optional `ugc`/`ogv`; `cnt_info.dart:11–14` expects integer counters. S2 `MediasInfo.kt:5–38` declares string `id`, `ugc.first_cid`, `ogv.season_id`, `cnt_info.play`/`danmaku` and long `duration`. **D-level variant evidence only**: the DTO disagreement does not prove numeric and quoted wire forms both appeared in current server responses. Avoid global scalar broadening from these declarations alone.

## E2 — favorite/comment actions: body encoding and acknowledgement are independent

**U; web form payload and code-only acceptance.** S1 `lib/http/fav.dart:639–658` posts to `Api.favVideo` = `/x/v3/fav/resource/batch-deal` (`api.dart:112`) with form fields `resources`, comma-separated `add_media_ids`/`del_media_ids`, `csrf`, explicitly `application/x-www-form-urlencoded`. It returns success based on `code==0`, without requiring `data`. `lib/pages/fav_detail/controller.dart:52–72` calls this for removal and mutates the visible rows only after success. This **batch-deal** contract is not interchangeable with a single-resource **deal** request using `rid`/`type`.

S1 `lib/http/reply.dart:115–137` posts `/x/v2/reply/action` with form `type`, `oid`, `rpid`, numeric `action`, `csrf`; success is also `code==0` only. S2 `apis/CommentApi.kt:55–69` builds the same action fields through its app `ApiHelper`/form transport. Actual S2 consumer `bilimiao-compose/.../pages/community/MainReplyViewModel.kt:166–180` parses `MessageInfo`, checks success, then adjusts the displayed like count. S3 `utils/network.py:2215–2240` requires credentials for verified operations and injects CSRF into dictionary form data; the implementation does not globally make every POST a JSON request (`2267–2270` is conditional `json_body`).

Synthetic acceptance hypothesis: an acknowledgement such as `{"code":0,"message":"0"}` can be valid for these action implementations even with missing/null `data`; `{"message":"ok"}` is not success. This is not a measured service fixture. Do not loosen a data-bearing endpoint's required-payload contract to accommodate acknowledgements. Do not make any write request to investigate it.

## E3 — comments have several real request families, not one route migration

**U/C for distinction; paging-family corroboration, not a universal preferred route.**

- S1 `lib/http/reply.dart:21–57`: unauthenticated branch GET `/x/v2/reply/main` with `oid`, `type`, **`pagination_str` containing a JSON object whose `offset` value is an opaque string**, and `mode=sort+2`. Authenticated branch uses `/x/v2/reply` with `sort`, `pn`, `ps=20`. `options` at `:16–19` explicitly binds `NoAccount`/empty cookie for the anonymous check. `models_new/reply/data.dart:41–69` decodes `data.replies`/`top_replies` as nullable lists and nullable `cursor`; `cursor.dart:31–47` has boolean `is_end`, integer `next` and distinct `pagination_reply`. This REST path has an actual current caller in `lib/utils/reply_utils.dart:173–220` for comment visibility checking. It is **not** the main threaded-comment screen's current gRPC source.
- S1 nested replies use GET `/x/v2/reply/reply`, `oid`, `root`, `pn`, `type`, `sort=1`, optional CSRF (`reply.dart:59–89`). `root` identifies the root comment; it is not a page/cursor.
- S3 `bilibili_api/comment.py:435–466` uses `/x/v2/reply` with page `pn`/`sort`; `:469–506` uses `/x/v2/reply/wbi/main` with `mode`, `pagination_str` and WBI. `data/api/common.json:86–137` separately declares page, non-WBI lazy `next`, and WBI session/offset families. Documentation in the function specifically points to `cursor.pagination_reply.next_offset` as the next opaque offset, not `cursor.next`.
- S2 `apis/CommentApi.kt:12–49` contains `/x/v2/reply/main` builders with `pn`/`sort`, including a root variant. No current Compose caller for those two REST list builders was found. Actual S2 screen `pages/community/MainReplyViewModel.kt:92–139` calls `ReplyGRPC.mainList`, builds a protobuf `CursorReq(mode,next)`, accepts `res.cursor` after success and ends on `cursor.isEnd`. S1 current main screen likewise uses gRPC: `lib/pages/video/reply/controller.dart:78–91` and `lib/pages/common/reply/reply_controller.dart:165–188`.

Important source limitation: S1 `reply.dart:35–36` and S3 `comment.py:495–496` manually escape **quotes only** when constructing embedded JSON. That is source usage, **not a sound escaping algorithm to copy**; arbitrary backslash/control characters require a JSON serializer before URI/form encoding. No current source evidence says the server intentionally restricts offsets to quote-only content. Round fixes must keep source-bound cursor state and preserve the last good cursor after rejected/malformed responses.

No evidence here authorizes migrating our app comments to web WBI/gRPC, changing anonymous/account policy, or mixing `pn`, `next`, `next_offset`, gRPC cursors or dynamic IDs.

## E4 — web search: WBI query and typed result containers

**C/U for S1+S3 web search request; S2 app/gRPC is a distinct contract.**

- S1 `lib/http/search.dart:48–121`, paths in `api.dart:269–271`, and `models/common/search/search_type.dart:5–33`: GET `https://api.bilibili.com/x/web-interface/wbi/search/type` or `/all/v2`. `search_type` is the web enum name (`video`, `media_bangumi`, `media_ft`, `bili_user`, `live_room`, `article`), not app numeric `type`; `keyword` is a string; pagination is `page`, plus `page_size=20`. Optional filters include `order`, `duration`, `tids`, `order_sort`, `user_type`, `category_id`, second timestamps `pubtime_begin_s`/`pubtime_end_s`. Query map is passed through WBI signer then Dio. Explicit search origin/referer/UA accompany the request. `data.v_voucher` causes an error state even with outer `code==0` (`:95–101`); challenge bypass was not attempted.
- S1 actual caller `lib/pages/search_panel/controller.dart:104–119` passes screen keyword/page/filter state. Decoder `lib/models/search/result.dart:24–88`: typed-video search expects `data.result` list; all-search expects groups keyed by `result_type`, each with `data` list. It skips other groups, reads numeric `numResults`, and uses `SearchVideoItemModel` for video data. `SearchStat` at `:231–236` calls `safeToInt` for `play`, `danmaku`, `favorite`, `review`, `like`; `utils/parse_int.dart:1–6` handles int/string/numeric/null. This is evidence of deliberately tolerated scalar variants, **not proof formatted Chinese counters should become numbers**.
- S3 `bilibili_api/data/api/search.json:2–22` declares the same web routes. `search.py:155–169,172–255` builds named keyword/`search_type`/page/filter values and uses `Api(...,wbi=True)`. This independently corroborates web `keyword`, `page`, `page_size`, `search_type`, `tids`, `duration` and WBI use. S3 returns a dictionary; it does not corroborate S1's typed response decoder.
- S2 `apis/SearchApi.kt:20–75` contains **app** `https://app.bilibili.com/x/v2/search` and `/type` requests with **`pn`/`ps`** and numeric `type=7/2`; current `pages/search/content/{SearchAllContent,SearchByTypeContent}.kt` instead call `bilibili.polymer.app.search.v1.SearchGRPC` (`:128` / `:114`). Do not copy web `page`/WBI fields into our app request or treat presence of the old app builder as server validation.

S1 signing at `utils/wbi_sign.dart:61–74` creates `wts`, sorts names, filters the known WBI punctuation set for signature input, URI-encodes components and computes `w_rid`; key discovery at `:77–92` uses `data.wbi_img` from nav. S3's signer is at `utils/network.py:1920–1942`. These establish family-specific signing dependence; this discovery does not authorize replacing our transport or account refresh flow.

## E5 — dynamic feed: opaque offset survives to the next accepted request

**U, with weaker inherited S3 declaration disagreement.** S1 `lib/http/api.dart:227–228` + `lib/http/dynamics.dart:35–70`: GET `/x/polymer/web-dynamic/v1/feed/all`, query `type` or `host_mid`, optional **string** `offset`, `features`; envelope is `code` + `data`. Decoder `lib/models/dynamics/result.dart:15–20,50–95` treats `has_more` as nullable boolean, `items` nullable list, `offset` string, individual `id_str` distinct from offset. Current `lib/pages/dynamics_tab/controller.dart:32–54` resets offset on refresh and passes accepted response offset into the next call. The HTTP implementation has a special recursive fetch after local filtering removes all rows; do not copy that unbounded strategy.

S3 `bilibili_api/dynamic.py:1218–1256` and `data/api/dynamic.json:254–267` use the same route but annotate offset as optional integer / dynamic ID. That annotation is **not** corroboration that opaque web offsets should be parsed as numbers. Prefer exact source-bound tokens; no evidence supports mixing legacy dynamic IDs, web offsets or gRPC `historyOffset/updateBaseline`.

## E6 — PGC response key is endpoint-specific

**U, intentionally different routes/families.**

| Source path | Request | Observed expected payload / consumer |
| --- | --- | --- |
| S1 `lib/http/search.dart:153–168` + `api.dart:288` | GET `/pgc/view/web/season`, optional `season_id` / `ep_id` | `code==0`, **`result`** decoded as `PgcInfoModel`. Actual caller `lib/pages/video/introduction/pgc/controller.dart:531` and `lib/utils/page_utils.dart:620`. |
| S1 `lib/http/search.dart:171–186` | GET `/pugv/view/web/season`, optional `season_id` / `ep_id` | Same broad domain model but **`data`** rather than `result`. No consumer trace asserted for this row. |
| S1 `lib/http/pgc.dart:15–56`, routes `api.dart:721–723` | GET `/pgc/season/index/result` / `/condition`; named filters, `page` / **`pagesize`**, not search `page_size` | **`data`**, dedicated DTO. This is an implementation request/decoder observation; no screen caller trace asserted here. |
| S2 `apis/BangumiAPI.kt:21–35` | GET `/pgc/view/v2/app/season`; GET `/pgc/web/season/section` | Current `pages/bangumi/BangumiDetailPage.kt:149–152` consumes app V2 as **`ResponseData<SeasonV2Info>`**, while `:191–193` consumes web section as **`ResponseResult<SeasonSectionInfo>`**. Same web section family caller also at `BangumiEpisodesPage.kt:139–141`. |

Do not make `data ?? result` the universal parser policy because S3's generic SDK `_process_response` happens to do that (`utils/network.py:2314–2320`). Our endpoint-declared envelope policy is more precise. Wrong/missing required key should remain typed failure and invoke existing fallback, not silently produce an empty success model.

## E7 — online count is display text; download qualities can be absent

**U/D, bounded.** S1 `lib/http/video.dart:861–876` GET `/x/player/online/total` uses `aid` or `bvid` plus `cid`, returns `data.total` as **String**. Actual repeating caller `lib/pages/common/common_intro_controller.dart:103–115` assigns that string to display state only on success. This is a concrete string-consumer observation, not a mandate to parse it as a precise integer. S3 `video.py:1368–1395` returns the same endpoint's dictionary with aid/bvid and cid; no typed `total` decoder corroboration.

S3 recent maintenance commit `425f5200f5fa17a316b93cdf21643513d9a3075f` explicitly handles `None` qualities in `VideoDownloadURLDataDetecter` comparators (`video.py:2645–2673`). This establishes a client-side absent-quality compatibility case. It does not establish our transport/session/native player must change or that absent quality implies an accepted playable track. Main-repo player/native memory fixes remain separately owned.

## E8 — transport/auth observation prevents false positives

- S1 base REST host `https://api.bilibili.com` (`lib/http/constants.dart:3`, `init.dart:200–217`), separate app/live/passport hosts. `utils/accounts/account_manager/account_mgr.dart:44–95` binds a request's account, skips cookies for `NoAccount`, handles app/gRPC separately, adds app `access_key`/signature to app request fields, otherwise reads cookies for the request URI. The same path string under different host/account options is not necessarily the same contract.
- S2 `BiliApiService.kt:6–20` routes to api/app/bangumi hosts through a shared signed parameter builder; `ApiHelper.kt:112–127,150–173` filters blank values, URL-encodes UTF-8 strings, adds app metadata/token where available and signs the sorted query. `MiaoHttp.kt:27–50` adds UA/referer, URI cookies, and encodes `formBody` as `application/x-www-form-urlencoded`. Those cross-cutting transforms must be included before comparing builder literals. Public embedded app constants are not user credentials and were not copied into any artifact.
- S3 `utils/network.py:2215–2242` verifies session/CSRF requirements, WBI signs only marked operations, creates cookie config; `:2296–2313` rejects missing/nonzero `code` (with separate `OK`-family logic). `:2314–2320` may return None for acknowledgements. Its special `OK` and envelope fallback behavior is an SDK design choice, not evidence for changing every main-repo REST endpoint.

## Read-custody verification

Source-only commands used: public metadata GETs; `git clone`, detached checkout of exact obtained pin, `git log/show/rev-parse/status`; `rg`, `nl`, `sed`, `cat`, directory listings. Main repo was read and assigned round Markdown written only. No external dependency, script or executable was launched from a clone. No credential/account data borrowed, no Bilibili mutation/read session attempted, no service challenge bypassed. Local metadata/clone caches stay outside the repo; no foreign code or raw captures are committed.

Before any implementation, the contract owner must bind E1–E8 to our actual shipped builder → transport → envelope/DTO/parser → caller/probe path, classify already-correct behavior, and produce unchanged-fixture actual-source red→green. These sibling observations alone are not a defect list.

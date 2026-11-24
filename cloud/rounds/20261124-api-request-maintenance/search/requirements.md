# Search identity and ordering requirements — U5 / U8

Status: **READY FOR INDEPENDENT REQUIREMENTS REVIEW; implementation not started.**
Owner: `quality_requirements`; source baseline `41dc562fba6e45896b0c4ba604f9ec38f4661e73`, 2026-11-24.
Current metadata checkpoint HEAD: `3317305f5c606cd4ca33f21a611c23f15a18dada`; inspected product inputs still match the baseline hashes.
Inputs: this round's `plan.md`, repository cloud instructions/memory index, preceding logic/UI requirements and verification baseline, API research, actual request/transport/signing/caller sources and existing API/auth probes. Evidence: [evidence.md](evidence.md).

## 1. Confirmed behavior, priority and goals

**U5 / P1, reproduced offline:** seven web search builders unconditionally set `ExtraCookies["buvid3"]="temp"` (`Api/SearchAPI.cs:63,85,107,129,151,173,195`). Actual GET cookie preparation applies extras after saved or explicitly supplied Cookie-header values (`Helpers/HttpHelper.cs:183–220`), so direct requests overwrite an available identity. This is not unused metadata; no live rejection was reproduced.
**U8 / P2, reproduced offline:** app `SearchAPI.Search` appends order only when null/empty (`:29–31`); a requested nonempty order is omitted before signature generation (`:41`). Existing SearchVM calls web builders (`Modules/SearchVM.cs:234,357,439–442,560,643,726`); the targeted search found no active direct app-Search caller. Do not claim this fixes the current web UI's sort behavior.
A scratch probe compiles real SearchAPI/ApiHelper/WbiKeyService and exact extracted cookie/proxy methods: **523 checks pass, 62 intended checks fail**, exit 1. Failures distinguish no-area identity loss from the existing area-proxy compatibility path and nonempty app-order omission; see evidence for denominators and limits.
Goals: retain existing identity on no-area web requests, preserve explicit-area compatibility, include app order accurately before signing, and prove every affected builder with real-source assertions.
Execute behind U1/PB1 and after this stream's PASS; U5/U8 share one SearchAPI owner. Neither finding authorizes a route migration, broader search redesign or live account operation.

## 2. Scope and invariants

Product scope is only `src/BiliLite.UWP/Api/SearchAPI.cs`: its seven web builders and app `Search`. Preserve public signatures, returned ApiModel shape, verbs, routes, query/filter semantics and asynchronous WBI flow.
No changes to global ApiHelper, WbiKeyService, GET cookie-merge precedence, credentials/cookie storage, settings, SearchVM/UI, pagination, parsers, SearchSuggest, dependencies, protos, manifests, binary assets, live JSON or git history.
Do not acquire/generate a BUVID, copy another identity, guess a real-looking value, introduce a query BUVID, or add credential fallback. Synthetic dummy values are permitted only inside offline fixtures and never sent to a service.
Existing server cookie bootstrap may still obtain genuine cookies through the unchanged transport; removing a local override does not promise that a missing jar becomes authenticated or that search must succeed on the server.

## 3. U5 mode, destination and identity contract

Use the existing mode discriminator **`!string.IsNullOrEmpty(area)`**, rather than guessing proxy mode from a hostname or configured API value. SearchVM exposes default empty area plus cn/hk/tw (`Modules/SearchVM.cs:144–151`).

| Inputs | Destination and area | Cookie metadata required |
| --- | --- | --- |
| `area=null` or `""`, default API base | Current `ApiHelper.API_BASE_URL` + `/x/web-interface/wbi/search/type`; no area query field | `need_cookie=true`; no BUVID entry in ExtraCookies, case-insensitively. Null or empty ExtraCookies is acceptable. |
| `area=null` or `""`, configured/nondefault API base | Retain that exact configured base; no ChooseProxyServer call, no area query field | Same no-override policy, even when the configured base looks like a proxy. An alternate base alone is not explicit area selection. |
| Nonempty `cn`, `hk` or `tw` | Call existing `Utils.ChooseProxyServer(area)`; preserve chosen base; include supplied area before WBI signing | Retain the existing request-local single `buvid3=temp` compatibility override, with `need_cookie=true`. |
| Other nonempty area, including whitespace | Retain existing nonempty-area branch and ChooseProxyServer fallback; include supplied area under current WBI normalization | Same existing area override; no new validation, trimming, inference or replacement area. |

`Utils.ChooseProxyServer` (`Helpers/Utils.cs:489–507`) uses a region-specific configured server when present, otherwise the general configured/default proxy. Leave that selection policy untouched.
The discriminator remains explicit area even if a configured proxy resolves to the official API host; host-based reclassification is outside this slice. Likewise, no-area with an alternate API base must preserve saved identity. This pins existing mode semantics rather than assuming a server's trust or behavior from its URL.
All seven no-area builders must omit the override regardless of whether the jar currently contains BUVID. The builder must not read/mutate the jar to construct extras. A saved/header BUVID remains available to the unchanged transport; absence remains absence of a fabricated builder override.
All unrelated cookies, explicit header values, cookie-jar bootstrap/error behavior and global extra-cookie precedence remain unchanged. The request-local area override must not mutate any saved BUVID or caller-owned dictionary.

### All seven route/type/filter contracts

All are GET, `need_cookie=true`, and target `/x/web-interface/wbi/search/type` on the mode-selected base. Preserve context, refresh/highlight/single-column and current empty auxiliary fields.

| Builder | `search_type` | Caller inputs/other fields to preserve |
| --- | --- | --- |
| WebSearchVideo | `video` | keyword, page=pn, order, duration, tids=region; empty category_id/tids_2 |
| WebSearchAnime | `media_bangumi` | keyword, page=pn; empty order/category_id |
| WebSearchMovie | `media_ft` | keyword, page=pn; empty order/category_id |
| WebSearchUser | `bili_user` | keyword, page=pn, caller order/order_sort and user_type fragments; changing=mid; empty category_id |
| WebSearchLive | `live` | keyword, page=pn, cover_type=user_cover, changing=mid |
| WebSearchArticle | `article` | keyword, page=pn, order, category_id=region |
| WebSearchTopic | `topic` | keyword, page=pn; empty order/category_id |

WebSearchUser's order/type arguments are **existing query fragments**, e.g. `&order=fans&order_sort=1` and `&user_type=1` (`Modules/SearchVM.cs:506–518`); do not escape the entire fragment into a single value or convert its interface in this slice.
Construct the full existing query, including area where applicable, then call the real `ApiHelper.GetWbiSign` once. Cookies are not WBI query fields. No post-sign query mutation, double leading `?`, duplicate area or signature field is permitted; `ApiModel.url` already inserts `?` (`ApiHelper.cs:271–275`).
Preserve existing keyword escaping and WBI canonicalization (`WbiKeyService.cs:191–209`): decoded values lose the signer's existing `!'()*` characters, are sorted and escaped, and receive wts/w_rid. Tests must distinguish that existing sanitizer policy from unintended encoding loss; do not change it under “query preservation.”

## 4. U8 app-order, defaults and signature contract

Retain GET `https://app.bilibili.com` + current `ApiHelper.api2` + `/search`; app Search continues to ignore API_BASE_URL and area/proxy selection. Preserve `MustParameter(LoginKey,true)` and the current fixed playback/search flags.
The `order` input is a raw string value. **Always retain exactly one order field**, decoding to the supplied value when nonempty; encode it once with URI escaping before app signing. For null/empty retain the existing explicit `order=` default, with no new exception. Unknown/non-enumerated order values are passed as encoded values, not interpreted as query fragments or rejected locally.
Preserve keyword exactly once encoded, local_time, pn/ps, duration/rid inclusion only when nonzero, appkey/build/platform/mobi_app/ts and the existing logged-in access_key behavior. Default pn=1/ps=20 and zero duration/rid remain unchanged. Do not round opaque string values or add a new cookie requirement to the app builder.
Build every field, including order, **before** `ApiHelper.GetSign(parameter, LoginKey)`, and append exactly one sign. Recomputing the existing signer over final unsigned bytes must match it; the sign must cover the encoded order, not an unescaped or subsequently appended value.
No key identity, secret selection, timestamp source, signer sorting/canonicalization or access-key provenance change is permitted.

## 5. Error and compatibility boundary

WBI key-fetch/sign failures continue to propagate through the existing async builder/caller path; do not return an unsigned “successful” model, silently retry through app/proxy, swallow the error or synthesize identity to bypass it.
Logged-out web calls retain cookie-enabled/bootstrap semantics; app calls retain the existing absence of access_key when logged out. The slice does not convert failed network/server searches into empty success or change UI retry/loading state.
Keep existing null-keyword/invalid configuration behavior; no new input-validation framework or normalization is authorized. Tests for supported keyword/order punctuation must decode once and compare exact values, with WBI's existing forbidden-character rule explicitly accounted for.

## 6. Minimal implementation and probe ownership proposal

One later executor owns `src/BiliLite.UWP/Api/SearchAPI.cs` for both U5 and U8; use narrow local changes, not a general cookie/form abstraction.
Use the existing **api02** console batch, whose project already links the real ApiHelper, CookieService, WbiKeyService and refresh dependencies. API01 is the related signer/request probe but is currently compile-failed and awaits QF1; g2 covers archive mapping, not these request builders. No new probe directory is needed.
Proposed exact test files: `plan/tests/api02/API02.csproj` (link real SearchAPI), `api02/Program.cs` (invoke additional checks without reducing its original 12 assertions), new `api02/SearchRequestChecks.cs`, and shared `plan/tests/API01Stubs.cs` for a thin configured proxy-selection seam and primitive capture hooks only.
The proxy seam returns fixture URLs and records the supplied area; it must not duplicate the builder's mode decision or fake WBI/app signing. Use real ApiHelper and signers; the existing extracted actual ChooseProxyServer/GetWithWebCookie scratch seam supplies independent merge/fallback evidence without changing production transport or adding another product implementation.
`API01Stubs.cs` is shared with auth1/API01 and owned by the preceding QF1 credential repair. **No concurrent writer**: wait for that owner's release, adapt the proposed test diff to its final contract without rewriting history, then assign one writer and rerun its dependents. If another actual file is needed, amend/review ownership before editing it.
Shared PLAN/ROADMAP/VALIDATION/DEVELOPMENT disposition belongs to the orchestrator's one later docs owner. This requirements PASS grants none of those files to the search executor and does not authorize an invented device BR.

## 7. Required real-source tests and pre/post evidence

| Case | Required assertions |
| --- | --- |
| Seven builders × empty/null area × default/configured base | Correct base/route/type/page; zero proxy-selector calls; no area field; no case-insensitive BUVID override; cookie flag true; all previous query/filter fields retained. |
| Seven builders × cn/hk/tw/unknown nonempty area | Exactly selected fixture base and selector input, one area value, one request-local temp override, unchanged other cookies; complete query signed after area insertion. Include empty regional-server fallback using exact actual Utils extraction in scratch. |
| Identity present / absent / explicit header | Exact actual cookie-preparation seam retains saved/header BUVID without an override; no fabricated BUVID when other saved cookies exist but BUVID does not; explicit area keeps temp compatibility; unrelated cookies and input dictionaries remain intact. Platform substitution is declared. |
| App order null, empty, view, pubdate, punctuation/Unicode | Exactly one order field, one decoded intended value, no injected extra field from `&`, `=`, `+`, `%` or emoji. Defaults and nonzero duration/rid/page/size remain; test guest and logged-in branches. |
| WBI/app signing | Real known WBI vector stays valid; wts/w_rid occur once; full final unsigned fields recompute to the real returned signature using fixture keys/captured timestamp. App sign covers encoded order and logged-in fields. Restore mutable fixture hooks/public settings afterward and isolate cache-sensitive failure tests. |
| Encoding/filter regressions | Keyword `中文 a&b=2+% 😀` round-trips once; video order/duration/tids, article order/category and user fragment field pairs retain their distinct values. Existing WBI forbidden-character behavior remains explicit. |

Add permanent assertions first and run them against the original SearchAPI: observe U5/U8 red failures, with routes/filters/signers as passing controls. Keep exactly those assertions for the product repair's green run. The preexisting scratch **523/62** result is supporting red evidence, not a substitute for the eventual committed test diff's red run.
Re-run the exact extracted real cookie/proxy seam against the changed real SearchAPI and retain before/after inputs, source hashes and command output in scratch. It must remain byte-faithful to production method boundaries, with network/storage primitives substituted; a handwritten merge simulation is insufficient.
Record U5/U8-specific failure categories/counts separately so a signature/control failure cannot be hidden by an expected aggregate nonzero exit. If additional failures emerge, diagnose and report them; do not update expected requests to match broken output.
Run `bash plan/tests/api02/run.sh`; after shared stub edits run auth1, and run independent API01 after QF1 restores it to preserve WBI/cache/credential controls. The original API02 assertions stay green. Record exact source/test SHA, command, SDK and exit; keep raw logs in scratch, concise results in the execution artifact. Do not conflate a changed shared stub with a new account-parser migration.
The approved source/test executor commits/pushes only explicitly owned files under the orchestrator's serialized commit procedure. Independent implementation review checks diff scope, real-source links, assertion preservation and red/green evidence.

## 8. Platform limits, risks and terminal status

Offline request construction, exact extracted merge policy and signatures are verifiable on Linux with the existing SDK 8.0.427/runtime/reference pack 8.0.33. No live search, outgoing WinRT wire cookies, rendered filter order, proxy availability or authenticated session success has been tested.
Required Windows compile: `powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore`; current host lacks PowerShell/VS-UWP/Windows SDK, so this gate is **FAILED/BLOCKED — environment unavailable**, not a compile success. No full build/MSIX/device/release claim follows from console probes.
Optional maintainer regression for touched search: default-area video/user/article plus anime/movie/live/topic page transitions; saved-cookie identity retained; explicit configured-area search still selects that server and filters; nonempty app order only through an existing authorized caller if one exists. Capture version/time/redacted shape and outcomes; no test-created BUVID or credential values in logs, no invented app UI path.
Main risks are misclassifying no-area configured API as area-proxy mode, deleting all area compatibility overrides, escaping user query fragments, app-order injection/double encoding, signing before final fields, stale shared stubs and overclaiming server/device outcomes. The mode table, multiplicity/round-trip/sign tests and exclusive ownership control these risks.
Terminal preparation status: **requirements and red evidence READY FOR REVIEW**. U5/U8 product work remains NOT STARTED pending review and release of QF1 shared-test ownership; Windows/session gates remain explicitly blocked. No product/test/build/main-doc/identity/sparse/history change was made for this requirements assignment.

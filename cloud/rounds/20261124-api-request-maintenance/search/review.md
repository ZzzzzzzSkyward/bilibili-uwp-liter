# 审查：U5 / U8 search identity and ordering requirements

- 轮次：`20261124-api-request-maintenance`
- 审查类型：需求
- 审查员：`quality_review`
- 结论：**PASS — requirements only; shared-test release and implementation gates remain**
- Bound product source: `41dc562fba6e45896b0c4ba604f9ec38f4661e73`.

**PASS for the proposed U5/U8 behavior, seven-builder coverage, real-source seams and shared-file ownership.** The sole preceding CHANGES item was a cross-stream correction to my earlier verification review; it is now closed at the requirements boundary by the independently approved [QF1 amendment review](../../20261124-maintenance-foundation/verification/amendment-review.md), corrected requirements/dispatch and recorded plan correction. QF1's actual restoration/test/implementation-review work remains pending its own evidence. Search cannot take shared `API01Stubs.cs` until that owner explicitly releases it. No active implementation file was edited by this reviewer.

## Focused re-review closure

Read the upstream ownership amendment, independent PASS, executor conflict/compiler log and controlling foundation requirements `:120`, execution dispatch `:30`, and plan `:54`. They now correctly preserve/revert both original real global-wrapper constructions wherever assigning `ApiHelper.Cookie`; the erroneous namespace-change instruction is withdrawn. The wrapper-assigned fixture uses its actual supported constructor and an isolated real LocalFolder-backed state seam; other named fixtures may use the actual base service without a derived-field assignment. No fake-success decision, product change or assertion weakening is granted. My prior foundation review now records the superseding factual correction.

At this read, HEAD is `10dd84857c748f6638e187d891d6a6649f887bb6`; U1/PB1 have separately owned commits and the QF1 API01 diff is still in flight. This is **requirements PASS, not a claim the two bad substitutions have already been restored or that API01/the suite passed**. No search semantics or test ownership was broadened. Current search source contracts and all previously checked evidence remain the same. No further P0/P1 search-requirement finding remains.

## 审查范围与证据

Read repository/cloud instructions, memory index, this round's plan/dispatch and all three requirements/evidence pairs, plus the preceding reviewed U1/PB1/UI/quality contracts. Inspected current unchanged SearchAPI, ApiHelper, WbiKeyService, HttpHelper cookie preparation, Utils proxy selection, SearchVM modes/filter callers, api02 compile list/12 original assertions, API01 primitive seams, and the scratch search preparation script/project/program/wrappers/logs.

Independent read-only checks:

- Recomputed the search scratch manifest: **12/12 source-input hashes match**, zero mismatches. This includes SearchAPI, ApiHelper, actual signers, credential dependencies, Utils/HttpHelper and the original shared API01 stubs. Active U1/QF1 changes elsewhere are not treated as a stable final snapshot.
- Compared the complete extracted `ChooseProxyServer` and `GetWithWebCookie` method text with current original source: **both exact method bodies are present unchanged in scratch wrappers**. Only network/storage/settings primitives terminate the operation. The compiler links real SearchAPI/ApiHelper/signers; no duplicate search or merge algorithm was used.
- Read retained `logs/repro-expanded.txt`: **585 checks = 523 pass / 62 fail**; 56 web-identity and six app-order failures. This matches the evidence's 42 + 7 + 7 + 6 classification. Scratch controls include all seven builders/modes, filters, route/page, actual WBI/app signature recomputation and independent known WBI vector.
- Current source search finds all seven web callers in SearchVM (`:234,357,439–442,560,643,726`). No active app-Search UI path is established, so U8 remains request-builder correctness rather than a claimed web UI sort repair.
- Read `git show 41dc562:src/BiliLite.UWP/Api/ApiHelper.cs` and current source `:9–28`; both contain the global compatibility class and the global-typed `ApiHelper.Cookie` field. Read the active API01 diff only to identify the upstream contract conflict; it is not an implementation review of unfinished QF1.

## P0（必须修复，阻断）

None in this search scope. U1 remains the preceding priority work.

## P1（必须修复，可本轮闭环）

### P1-01 — CLOSED at requirements boundary: incorrect upstream namespace amendment withdrawn

**This corrects my earlier review error, not a defect introduced by the search requirements author or executor.** The prior review inspected `Services/CookieService.cs` but missed the compatibility class at the top of the already-linked real `ApiHelper.cs`:

- `ApiHelper.cs:15` declares **global `CookieService : BiliLite.Services.CookieService`** with the matching injectable constructor (`:18–22`).
- `ApiHelper.Cookie` is explicitly **`global::CookieService`** (`:28`).
- Original `API01.cs:147,232` therefore intentionally construct that real compatibility type. They were not unresolved namespace references.
- The active QF1 diff replaces both with the base `BiliLite.Services.CookieService`. In particular, baseline line 232 then assigns a base instance to the global derived-typed field; that is a compile-time type mismatch. Changing ApiHelper's field/product wrapper to accommodate it would violate the harness-only scope.

Required owner action before claiming QF1 green or releasing its shared-test ownership:

1. The orchestrator/requirements owner records a correction withdrawing only the erroneous namespace-only exception in preceding `verification/requirements.md`, `execution-dispatch.md` and review disposition. Keep the valid real dependency, LM05, fixture, rollback and session-coverage requirements.
2. The **existing QF1 test owner**, not this reviewer or future search owner, restores the two original `new global::CookieService(...)` constructions while retaining any independently reviewed fixture enhancements. Do not add a fake global service or modify product ApiHelper/credential contracts.
3. Run independent API01 plus its existing owning chain/dependent gates and record real compile/assertion results. No final whole-suite claim follows from reverting two names alone; other active QF1 work still needs its own review.

Disposition: the controlling requirement/dispatch correction is independently PASS and recorded, so the sole requirements CHANGES item is closed. The implementation acceptance above remains the QF1 executor/reviewer's responsibility: actual compatibility type retained, API01 compiled against its field type, original/newly reviewed behaviors run, and explicit shared-stub ownership released. No search product design revision is needed. This output grants no writer access to active QF1 files.

## P2（记录到 ROADMAP）

No additional search product work is required for this slice. Preserve the existing source-confirmed U5/P1 and U8/P2 dispositions. Do not turn unused app-order exposure, proxy availability, real BUVID acquisition or general search UI recovery into new implementation scope.

## 验收标准核对

| Contract | Review result / controlling acceptance |
| --- | --- |
| Explicit mode | Correct. Nonempty `area` selects existing ChooseProxyServer policy; null/empty retains exact API_BASE_URL, including configured/nondefault bases. Hostname resemblance cannot redefine the mode. Unknown/whitespace nonempty area retains the existing fallback contract. |
| No-area identity | Correct. All seven builders must set `need_cookie=true` and omit any case-insensitive BUVID override without consulting/mutating saved cookies. Existing saved/explicit-header identity passes through unchanged; missing identity is not replaced with a fabricated value. |
| Area compatibility | Correct. All seven explicit-area paths retain one request-local `buvid3=temp`, selected base and area field before WBI signing. This preserves an existing compatibility behavior; it does not claim a genuine BUVID or certify a proxy/service. |
| Actual transport effect | Verified from source `HttpHelper.cs:183–220`: explicit Cookie header wins over jar selection, then extras override its parsed values. The source clones input headers; tests must retain dictionary immutability and unrelated cookies. Empty-jar bootstrap/error policy stays unchanged and is not conflated with missing BUVID in a nonempty jar. |
| Seven builders/query data | Complete table covers video, anime, movie, user, live, article and topic. Preserve type/page/keyword, video order/duration/tids, article order/category, user order/order_sort/user_type fragments, context/highlight/refresh/changing/cover and current empty auxiliary fields. |
| User fragment boundary | Correct. SearchVM `:506–518` passes prebuilt order/type query fragments. Escaping the entire fragment or changing its interface would break existing callers and is excluded. |
| WBI | Full query including area goes through the real signer once; no cookies in query or post-sign mutation. Preserve existing parse/sort/escape and removal of `!'()*` (`WbiKeyService.cs:191–208`). Check single area/wts/w_rid and exact signed output; a signature-shaped suffix alone is insufficient. |
| App order | Correct: one encoded raw order value before real LoginKey signing; null/empty keep one explicit empty default. Punctuation/Unicode injection/round-trip cases and guest/logged-in branches are specified. Preserve current app route, keyword, timestamps, pn/ps, optional duration/rid and fixed flags. No new cookie requirement or app UI claim. |
| Errors | Existing WBI fetch/sign exceptions propagate; no unsigned model, hidden proxy/app fallback or synthesized identity. Server/transport/UI retry semantics remain outside the local request correction. |
| Exact product ownership | Only `src/BiliLite.UWP/Api/SearchAPI.cs`, both U5/U8 under one owner. No shared helper, signer, transport, settings, credential, parser, UI, manifest/dependency/asset/live-data edits. |
| Exact test ownership | `api02/API02.csproj`, `api02/Program.cs`, new `api02/SearchRequestChecks.cs`, shared `plan/tests/API01Stubs.cs` only for declared primitive/proxy-capture seam. Link real SearchAPI/ApiHelper/signers. Keep original 12 api02 assertions and existing auth/API controls. No new probe directory or unowned runner rewrite. |
| Active ownership/dependencies | Correctly waits for preceding U1/PB1 and QF1 shared-stub release, then adapts to final current stub contract. Search cannot overwrite/revert QF1 in-flight work. Favorites uses c2, comment recovery uses comments after release; main docs remain a separate single owner. No conflict is authorized by the requirements. |
| Meaningful red/green | New permanent owning-batch assertions must compile and fail against real old SearchAPI, then pass unchanged after source repair. The 523/62 scratch result is corroboration only. Classify U5/U8 failures separately from signing/control failures; rerun exact extracted merge/proxy seam with source hashes after the change. |
| Regression commands | Bash api02; auth1 plus independent API01 after shared-stub changes/QF1 restoration. Restore mutable static login/key/URL/hook state and isolate WBI cache-sensitive failures. Source/test SHA, exits, denominators and unresolved suffixes stay explicit. |

The permanent test matrix is stronger than the existing scratch sample: it requires default/configured base crossed with both null and empty area, all explicit regional/unknown modes and selector-call evidence, missing/present/header identity, both guest/app sessions and each order/encoding/signing boundary. Preserve those acceptance cases; the retained 585 checks are not a substitute for omitted required combinations.

## 测试/构建证据

No reviewer mutation/test execution was performed against the shared active checkout. Supporting requirements-stage evidence is source/hash-bound and internally consistent: 523 controls pass, 62 intended behavioral checks fail, actual source methods/signers execute through declared primitive substitutes. This proves construction/merge-policy defects locally; it establishes no live search rejection, saved Windows cookie installation or rendered UI result.

Linux SDK 8.0.427/runtime/ref pack 8.0.33 is available under the preceding scratch root. Use declared Bash scripts and per-owner scratch/cache or an identified immutable snapshot. Preceding whole-suite baseline remains FAILED until QF1 closes; no search-only result can relabel it green.

Windows compile remains required and **FAILED/BLOCKED — environment unavailable**: PowerShell/VS-UWP/Windows SDK are absent here. Actual WinRT wire cookies, configured proxy connectivity, real sessions/search UI and device acceptance are unverified. Do not create a BUVID, make live calls with synthetic credentials, fabricate a device BR or claim release readiness. No release/master/deployment authority is granted.

**Final disposition: PASS for U5/U8 requirements, with no open P0/P1 requirements findings.** Execution still waits for the preceding dependencies and explicit QF1 shared-test release. Final implementation review must check real-source red/green, original assertion preservation, exact query/identity/signature behavior, shared ownership release and honest blocked gates. Windows/session/release readiness is not established.

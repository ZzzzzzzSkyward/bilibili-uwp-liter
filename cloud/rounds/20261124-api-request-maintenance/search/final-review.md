# 实现审查：U5 / U8 search requests

- 轮次：`20261124-api-request-maintenance`
- 审查类型：实现
- 审查员：`quality_review`
- 结论：**PASS — source and offline behavior approved; Windows/session gates remain FAILED/BLOCKED**
- Immutable implementation: `f594c9b58f6ed2e086329b1d2a221b1bc0fdf95e`, reviewed against parent `5210148`.
- Final execution metadata: `01c503f`; later package/runner changes are outside this review.

The implementation matches the passed mode/identity, encoding and signing contracts. All seven web builders retain saved/header identity for no-area calls and preserve explicit-area compatibility. App Search includes one encoded order before signing while preserving null/empty defaults. No open P0/P1/P2 finding exists in this diff. Offline approval establishes neither live-service success nor UWP/device/release readiness.

## 审查范围与证据

Read repository/cloud instructions and memory index, passed search requirements/review/evidence, execution/commit ownership, implementation-review dispatch and finalized execution record. Inspected the actual immutable source/test diff, complete new permanent matrix, original API02 tests, real ApiHelper/WBI/app signers, exact extracted cookie/proxy methods, scratch project/preparation/fixtures and retained red/green/dependent/platform logs.

Independent provenance/scope checks:

- The commit changes **only five approved source/test files plus `search/execution.md`**. Product scope is solely `Api/SearchAPI.cs`; no transport/helper/signer/credential/UI/parser/settings/build/manifest/dependency/main-doc change. Later search metadata changes only its execution record. `git diff --check f594c9b^ f594c9b` passes.
- Verified all **seven final source/test/helper hashes** against both committed bytes and the current relevant files: **zero mismatches**. Original SearchAPI hash matches the exact parent content and saved old-source evidence.
- Red/green expanded scratch manifests differ **only in SearchAPI**. Exact ChooseProxyServer/GetWithWebCookie bodies match original source and their recorded hashes; both wrapper hashes and the identical expanded scratch Program hash match. The platform/network capture supplies no alternate merge, selector policy or signer algorithm.
- Original API02 assertions and method bodies are untouched; Program changes only its result label and invokes the new checks. Shared API01Stubs adds only proxy-selector callback/input capture; its credential/WBI primitives and request behavior remain unchanged.

Reviewer reruns used a separate source/test copy from immutable `f594c9b` under `/workspace/work/maintenance-foundation/search-review-_ns6260f`, preserving modes. For the red run, only that scratch copy's SearchAPI was replaced with its exact parent bytes; the test files were identical for red/green. No shared source/test/build/docs/history was edited or output raced with other owners.

## P0（必须修复，阻断）

None.

## P1（必须修复，可本轮闭环）

None. The preceding actual compatibility-wrapper contract is retained; search does not touch either API01 wrapper construction or production ApiHelper. Its shared-stub regressions pass.

## P2（记录到 ROADMAP）

None introduced or left unresolved within this change. Broader search UI recovery, genuine BUVID acquisition, proxy/service availability, CI and dependency/package work remain separately scoped. Main-document dispositions stay with the designated single owner.

## 验收标准核对

| Contract | Actual result |
| --- | --- |
| Seven no-area paths | Each existing unconditional temp override moved inside its existing nonempty-area branch. Null/empty area leaves ExtraCookies unset, `need_cookie=true`, exact API_BASE_URL/route and no area field. Configured/nondefault API base remains direct mode; no hostname inference, selector call, jar read/write or fabricated BUVID is added. |
| Explicit area | All seven cn/hk/tw/unknown/whitespace paths retain the existing selector call, selected base, area field and one request-local `buvid3=temp` before WBI signing. Explicit mode remains decisive even when the selected destination is the official host. Actual selector regional fallback remains unchanged and is exercised in scratch. |
| Cookie semantics | Exact actual GetWithWebCookie extraction demonstrates saved/header BUVID survives no-area calls; missing BUVID in an otherwise nonempty jar remains missing. Explicit-area temp remains the request-local override. Saved BUVID, unrelated cookies, caller header dictionary and extras remain unchanged. This does not redefine empty-jar bootstrap or claim actual WinRT domain/path/wire behavior. |
| Web query/filter contracts | Existing verb/route/type/page/keyword/context/refresh/highlight/single-column and type-specific fields remain. Video order/duration/tids, article category/order, user order/order_sort/user_type fragments, live changing/cover and empty auxiliary fields are preserved. User fragments are not escaped into a single value. |
| WBI | Query and area are finalized before the unchanged real signer. Unique delimiter/area/wts/w_rid, real signature recomputation and an independent known WBI vector pass. Existing `!'()*` stripping is explicitly tested rather than mislabeled as new encoding loss. No query BUVID, post-sign mutation, unsigned fallback or swallowed key-fetch failure. |
| App order | One `Uri.EscapeDataString(order ?? string.Empty)` field is added before existing GetSign. Null/empty yield the prior explicit `order=` default; nonempty/unknown raw values round-trip once and cannot inject fields. LoginKey, common access-key policy, endpoint, keyword, timestamps, pn/ps, optional duration/rid and fixed flags remain unchanged. No new cookie or app UI path is invented. |
| Permanent coverage | Seven builders × two API bases × seven area inputs = **98** requests, plus official-host selector cases. Tests assert mode independently via selector call count/input, exact request fields and identity metadata. Guest/logged-in app cases cover empty/null/view/pubdate/unknown/punctuation/Unicode, defaults, encoded order injection and actual signed fields. |
| Test seams and cleanup | New shared stub is only a configurable selector return/input recorder; real SearchAPI owns mode decisions and real signers execute. Public base/key/current-WBI/login/access-key/request-handler/selector/capture values are restored in finally, including failure paths. Private WBI cache is process-bound, and the failure case runs before its first seed in this fresh console process. No product reset or successful-credential substitute is added. |
| Source/structure quality | Small local edits preserve public signatures and existing flow. Extra cookies remain per-request; no shared mutable identity structure or new production abstraction is introduced. New SearchRequestChecks is compiled in the existing owning api02 batch; no orphan file or duplicate production algorithm. Touched source/test structure is clear and acceptable. |

## 测试/构建证据

Reviewer independently observed the following using SDK **8.0.427**, runtime/ref pack **8.0.33**, existing package cache and isolated CLI/output state:

| Command / input | Observed result |
| --- | --- |
| `bash plan/tests/api02/run.sh`, exact old SearchAPI plus committed tests | Exit **134** after intentional probe assertion exception: original **12/12 PASS**; search **1222 PASS / 36 FAIL**, comprising **28 U5 / 8 U8 / 0 controls**. This is meaningful behavioral red, not a compile failure or app/runtime crash claim. |
| Same command/tests, committed repaired SearchAPI | Exit **0**: original **12/12 PASS**; search **1258 PASS / 0 FAIL**, all categories zero. |
| `bash plan/tests/auth1/run.sh` | Exit **0**, **35 PASS**, after restoring the exact required XAML source input to the review snapshot. |
| `bash plan/tests/run-api01.sh` | Exit **0**, **76 PASS**. |
| Existing scratch `dotnet run --project /workspace/work/maintenance-foundation/search-execution/probe/SearchRequestProbe.csproj -c Debug` | Exit **0**, **881 PASS / 0 FAIL** over linked real builders/signers and exact extracted proxy/cookie methods. |

The first reviewer auth1 attempt failed because the review snapshot's extension filter omitted `Pages/SettingPage.xaml`, which auth1 reads for source-wiring checks. Direct captured output confirms FileNotFoundException at that path/SourceChecks, rather than a product assertion failure. Adding that **exact committed input only**, with no test/source changes, made the unchanged owning command pass. This setup correction is not hidden or attributed to the repository.

Retained executor evidence independently agrees with these results. Its expanded extracted-method red has **805 PASS / 76 FAIL** (56 no-area metadata/merge + seven missing-identity + seven header-identity + six app-order failures); identical expanded harness green has **881/0**. The earlier smaller scratch totals and pre-behavior duplicate-Compile setup error are explicitly separated. Permanent red/green are unchanged-test runs; no signature/control failure is hidden by the expected red exit.

Existing warnings remain visible, including generated-name CS8981, auth1 CS1998 and csc SDK Newtonsoft.Json CS1701. No assertion removal, warning suppression, source migration or widened product change was used. This affected-scope verification does not imply a newly rerun whole-repository suite.

Windows compile remains **FAILED/BLOCKED — environment unavailable**: the recorded exact compile command exits **127**, `powershell: command not found`; this Linux environment lacks VS/UWP/Windows SDK/runtime. Actual outgoing WinRT cookies, real Bilibili session/search acceptance, configured proxy connectivity, rendered sort/filter behavior and device journeys remain unverified. No synthetic identity left the offline fixtures, no live mutation/release was performed, and the unused app builder's repair is not described as a verified web UI sort fix.

**Final disposition: PASS within the reviewed source/offline scope.** Search ownership and the shared index/stub window were explicitly released in final execution metadata; later work still needs its own assigned owner. Main docs and Windows/session follow-up retain their existing owners/gates. No broader readiness or repository stop-condition claim follows.
